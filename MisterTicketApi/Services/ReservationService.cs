using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Database;
using MisterTicketApi.DTOs;
using MisterTicketApi.Entities;
using MisterTicketApi.Hubs;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Services;

public class ReservationService : IReservationService
{
    private readonly MisterTicketContext _context;
    private readonly IHubContext<SeatHub> _hub;

    public ReservationService(MisterTicketContext context, IHubContext<SeatHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    // ------------------------------------------------------------- seat map

    public async Task<SeatMapDTO?> GetSeatMapAsync(int eventId, int? userId)
    {
        // Expired holds must disappear before the map is drawn.
        await ReleaseExpiredAsync(eventId);

        var eventInfo = await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => new { e.Id, e.Name, e.StartsAt, VenueName = e.Venue.Name })
            .FirstOrDefaultAsync();

        if (eventInfo is null)
            return null;

        var seats = await _context.EventSeats
            .AsNoTracking()
            .Where(es => es.EventId == eventId)
            .OrderBy(es => es.Seat.RowLabel).ThenBy(es => es.Seat.Number)
            .Select(es => new EventSeatDTO
            {
                Id = es.Id,
                RowLabel = es.Seat.RowLabel,
                Number = es.Seat.Number,
                Status = es.Status.ToString(),
                Price = es.Price,
                PricingZoneId = es.Seat.PricingZoneId,
                PricingZoneName = es.Seat.PricingZone.Name,
                PricingZoneColor = es.Seat.PricingZone.ColorHex,
                IsMine = userId != null
                         && es.Reservation != null
                         && es.Reservation.UserId == userId
            })
            .ToListAsync();

        return new SeatMapDTO
        {
            EventId = eventInfo.Id,
            EventName = eventInfo.Name,
            VenueName = eventInfo.VenueName,
            StartsAt = eventInfo.StartsAt,
            Seats = seats
        };
    }

    // ---------------------------------------------------------- reservation

    public async Task<ReservationDTO> CreateAsync(ReservationCreateDTO dto, int userId)
    {
        await ReleaseExpiredAsync(dto.EventId);

        var eventExists = await _context.Events.AnyAsync(e => e.Id == dto.EventId);
        if (!eventExists)
            throw new InvalidOperationException($"Event {dto.EventId} does not exist.");

        var requestedIds = dto.EventSeatIds.Distinct().ToList();

        // A transaction so two clients cannot both walk away with the same seat.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var seats = await _context.EventSeats
            .Where(es => requestedIds.Contains(es.Id) && es.EventId == dto.EventId)
            .ToListAsync();

        if (seats.Count != requestedIds.Count)
            throw new InvalidOperationException("Some seats do not belong to this event.");

        var alreadyTaken = seats.Where(es => es.Status != SeatStatus.Free).ToList();
        if (alreadyTaken.Count > 0)
            throw new InvalidOperationException(
                $"{alreadyTaken.Count} of the seats you picked have just been taken. Reload the map and try again.");

        var now = DateTime.UtcNow;

        var reservation = new Reservation
        {
            UserId = userId,
            EventId = dto.EventId,
            Status = ReservationStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.Add(IReservationService.HoldDuration),
            TotalAmount = seats.Sum(es => es.Price)
        };

        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Reserved;
            seat.Reservation = reservation;
        }

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Everyone watching this event sees the seats go orange.
        await BroadcastAsync(dto.EventId, seats);

        return (await GetByIdAsync(reservation.Id, userId))!;
    }

    public async Task<ReservationDTO?> GetByIdAsync(int reservationId, int userId)
    {
        return await BuildQuery(userId)
            .Where(r => r.Id == reservationId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ReservationDTO>> GetMineAsync(int userId)
    {
        return await BuildQuery(userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> CancelAsync(int reservationId, int userId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.EventSeats)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

        if (reservation is null)
            return false;

        if (reservation.Status == ReservationStatus.Paid)
            throw new InvalidOperationException("A paid reservation cannot be cancelled here.");

        // Keep a handle on the seats: releasing detaches them from the reservation.
        var freed = reservation.EventSeats.ToList();

        ReleaseSeats(reservation);
        reservation.Status = ReservationStatus.Cancelled;

        await _context.SaveChangesAsync();

        // Everyone watching sees the seats go green again.
        await BroadcastAsync(reservation.EventId, freed);

        return true;
    }

    // -------------------------------------------------------------- expiry

    public async Task<int> ReleaseExpiredAsync(int? eventId = null)
    {
        var now = DateTime.UtcNow;

        var query = _context.Reservations
            .Include(r => r.EventSeats)
            .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt <= now);

        if (eventId.HasValue)
            query = query.Where(r => r.EventId == eventId.Value);

        var expired = await query.ToListAsync();
        if (expired.Count == 0)
            return 0;

        // Group per event: one broadcast per event, not per reservation.
        var freedByEvent = new Dictionary<int, List<EventSeat>>();

        foreach (var reservation in expired)
        {
            if (!freedByEvent.TryGetValue(reservation.EventId, out var list))
            {
                list = new List<EventSeat>();
                freedByEvent[reservation.EventId] = list;
            }
            list.AddRange(reservation.EventSeats);

            ReleaseSeats(reservation);
            reservation.Status = ReservationStatus.Cancelled;
        }

        await _context.SaveChangesAsync();

        foreach (var (id, seats) in freedByEvent)
            await BroadcastAsync(id, seats);

        return expired.Count;
    }

    // ----------------------------------------------------------- broadcast

    /// <summary>Pushes the new status of these seats to everyone on the event's map.</summary>
    private async Task BroadcastAsync(int eventId, IEnumerable<EventSeat> seats)
    {
        var payload = new SeatsChangedDTO
        {
            EventId = eventId,
            Seats = seats
                .Select(es => new SeatStatusChangeDTO
                {
                    EventSeatId = es.Id,
                    Status = es.Status.ToString()
                })
                .ToList()
        };

        if (payload.Seats.Count == 0)
            return;

        await _hub.Clients
            .Group(SeatHub.GroupFor(eventId))
            .SendAsync("SeatsChanged", payload);
    }

    /// <summary>Puts the seats back on the market and detaches them from the reservation.</summary>
    private static void ReleaseSeats(Reservation reservation)
    {
        foreach (var seat in reservation.EventSeats)
        {
            seat.Status = SeatStatus.Free;
            seat.ReservationId = null;
        }
    }

    /// <summary>Reservations of one user, projected into DTOs.</summary>
    private IQueryable<ReservationDTO> BuildQuery(int userId)
    {
        return _context.Reservations
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => new ReservationDTO
            {
                Id = r.Id,
                EventId = r.EventId,
                EventName = r.Event.Name,
                VenueName = r.Event.Venue.Name,
                StartsAt = r.Event.StartsAt,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                ExpiresAt = r.ExpiresAt,
                TotalAmount = r.TotalAmount,
                Seats = r.EventSeats
                    .OrderBy(es => es.Seat.RowLabel).ThenBy(es => es.Seat.Number)
                    .Select(es => new ReservationSeatDTO
                    {
                        EventSeatId = es.Id,
                        RowLabel = es.Seat.RowLabel,
                        Number = es.Seat.Number,
                        Price = es.Price,
                        PricingZoneName = es.Seat.PricingZone.Name
                    })
                    .ToList()
            });
    }
}