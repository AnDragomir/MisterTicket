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

    // --------------------------------------------------------- claim seats

    public async Task<ReservationDTO?> GetActiveAsync(int eventId, int userId)
    {
        await ReleaseExpiredAsync(eventId);

        var active = await FindPendingAsync(eventId, userId);

        return active is null ? null : await GetByIdAsync(active.Id, userId);
    }

    public async Task<ReservationDTO> ClaimSeatAsync(int eventId, int eventSeatId, int userId)
    {
        await ReleaseExpiredAsync(eventId);

        var seatBelongs = await _context.EventSeats
            .AnyAsync(es => es.Id == eventSeatId && es.EventId == eventId);

        if (!seatBelongs)
            throw new InvalidOperationException("That seat does not belong to this event.");

        var reservation = await FindPendingAsync(eventId, userId);
        var isNewBasket = reservation is null;

        if (reservation is null)
        {
            var now = DateTime.UtcNow;

            reservation = new Reservation
            {
                UserId = userId,
                EventId = eventId,
                Status = ReservationStatus.Pending,
                CreatedAt = now,
                // The clock starts on the first seat and is never extended.
                ExpiresAt = now.Add(IReservationService.HoldDuration),
                TotalAmount = 0
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
        }

        // The status check lives inside the UPDATE, so the database decides who
        // wins when two clients click the same seat: exactly one row matches.
        var claimed = await _context.EventSeats
            .Where(es => es.Id == eventSeatId
                      && es.EventId == eventId
                      && es.Status == SeatStatus.Free)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(es => es.Status, SeatStatus.Reserved)
                .SetProperty(es => es.ReservationId, reservation.Id));

        if (claimed == 0)
        {
            // Do not leave an empty basket behind if this was the first click.
            if (isNewBasket)
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }

            throw new InvalidOperationException("That seat was just taken by someone else.");
        }

        await UpdateTotalAsync(reservation.Id);
        await BroadcastStatusAsync(eventId, new[] { eventSeatId }, SeatStatus.Reserved);

        return (await GetByIdAsync(reservation.Id, userId))!;
    }

    public async Task<ReservationDTO?> ReleaseSeatAsync(int eventId, int eventSeatId, int userId)
    {
        var reservation = await FindPendingAsync(eventId, userId)
            ?? throw new InvalidOperationException("You are not holding any seat for this event.");

        var released = await _context.EventSeats
            .Where(es => es.Id == eventSeatId
                      && es.EventId == eventId
                      && es.ReservationId == reservation.Id
                      && es.Status == SeatStatus.Reserved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(es => es.Status, SeatStatus.Free)
                .SetProperty(es => es.ReservationId, (int?)null));

        if (released == 0)
            throw new InvalidOperationException("That seat is not one of yours.");

        await BroadcastStatusAsync(eventId, new[] { eventSeatId }, SeatStatus.Free);

        var remaining = await _context.EventSeats.CountAsync(es => es.ReservationId == reservation.Id);

        if (remaining == 0)
        {
            // An empty basket is not a reservation any more.
            reservation.Status = ReservationStatus.Cancelled;
            reservation.TotalAmount = 0;
            await _context.SaveChangesAsync();
            return null;
        }

        await UpdateTotalAsync(reservation.Id);

        return await GetByIdAsync(reservation.Id, userId);
    }

    // ---------------------------------------------------------- reservation

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
        reservation.TotalAmount = 0;

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

    // ------------------------------------------------------------- helpers

    /// <summary>The basket this user is filling for this event, if there is one.</summary>
    private async Task<Reservation?> FindPendingAsync(int eventId, int userId)
    {
        return await _context.Reservations
            .FirstOrDefaultAsync(r => r.EventId == eventId
                                   && r.UserId == userId
                                   && r.Status == ReservationStatus.Pending);
    }

    /// <summary>Recomputes the basket total from the seats it currently holds.</summary>
    private async Task UpdateTotalAsync(int reservationId)
    {
        var total = await _context.EventSeats
            .Where(es => es.ReservationId == reservationId)
            .SumAsync(es => es.Price);

        var reservation = await _context.Reservations.FindAsync(reservationId);
        if (reservation is null)
            return;

        reservation.TotalAmount = total;
        await _context.SaveChangesAsync();
    }

    // ----------------------------------------------------------- broadcast

    /// <summary>Pushes the new status of these seats to everyone on the event's map.</summary>
    private async Task BroadcastAsync(int eventId, IEnumerable<EventSeat> seats)
    {
        await SendAsync(eventId, seats.Select(es => new SeatStatusChangeDTO
        {
            EventSeatId = es.Id,
            Status = es.Status.ToString()
        }));
    }

    /// <summary>
    /// Same, for seats changed with ExecuteUpdate: no entity is loaded, so the
    /// payload is built from the ids and the status we just wrote.
    /// </summary>
    private async Task BroadcastStatusAsync(int eventId, IEnumerable<int> eventSeatIds, SeatStatus status)
    {
        await SendAsync(eventId, eventSeatIds.Select(id => new SeatStatusChangeDTO
        {
            EventSeatId = id,
            Status = status.ToString()
        }));
    }

    private async Task SendAsync(int eventId, IEnumerable<SeatStatusChangeDTO> changes)
    {
        var payload = new SeatsChangedDTO
        {
            EventId = eventId,
            Seats = changes.ToList()
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