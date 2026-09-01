using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Database;
using MisterTicketApi.DTOs;
using MisterTicketApi.Entities;
using MisterTicketApi.Hubs;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Services;

public class PaymentService : IPaymentService
{
    private readonly MisterTicketContext _context;
    private readonly IHubContext<SeatHub> _hub;
    private readonly IReservationService _reservationService;

    public PaymentService(
        MisterTicketContext context,
        IHubContext<SeatHub> hub,
        IReservationService reservationService)
    {
        _context = context;
        _hub = hub;
        _reservationService = reservationService;
    }

    public async Task<ReservationDTO?> PayAsync(int reservationId, PaymentCreateDTO dto, int userId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.EventSeats)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

        if (reservation is null)
            return null;

        if (reservation.Status == ReservationStatus.Paid)
            throw new InvalidOperationException("This reservation has already been paid.");

        if (reservation.Status == ReservationStatus.Cancelled)
            throw new InvalidOperationException("This reservation was cancelled; its seats are back on sale.");

        if (reservation.ExpiresAt <= DateTime.UtcNow)
        {
            // The sweeper has not run yet, but the hold is over: free the seats now.
            await _reservationService.ReleaseExpiredAsync(reservation.EventId);
            throw new InvalidOperationException("The 15 minutes ran out; the seats have been released.");
        }

        if (reservation.EventSeats.Count == 0)
            throw new InvalidOperationException("This reservation holds no seat.");

        var now = DateTime.UtcNow;

        // The "payment service": no gateway, no risk of failure.
        reservation.Payment = new Payment
        {
            Reference = BuildReference(),
            Amount = reservation.TotalAmount,
            Status = PaymentStatus.Succeeded,
            Method = BuildMethodLabel(dto),
            CreatedAt = now,
            PaidAt = now
        };

        foreach (var seat in reservation.EventSeats)
            seat.Status = SeatStatus.Paid;

        reservation.Status = ReservationStatus.Paid;

        await _context.SaveChangesAsync();

        // The map turns those seats red for everyone watching.
        await BroadcastPaidAsync(reservation.EventId, reservation.EventSeats);

        return await _reservationService.GetByIdAsync(reservation.Id, userId);
    }

    public async Task<PaymentDTO?> GetForReservationAsync(int reservationId, int userId)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.ReservationId == reservationId && p.Reservation.UserId == userId)
            .Select(p => new PaymentDTO
            {
                Id = p.Id,
                Reference = p.Reference,
                Amount = p.Amount,
                Status = p.Status.ToString(),
                Method = p.Method,
                PaidAt = p.PaidAt
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>"PAY-20260901-8F3A2C": readable, and unique enough for a demo.</summary>
    private static string BuildReference()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
    }

    /// <summary>"Visa •••• 4242" or just "QrCode".</summary>
    private static string BuildMethodLabel(PaymentCreateDTO dto)
    {
        return string.IsNullOrWhiteSpace(dto.CardLastFour)
            ? dto.Method
            : $"{dto.Method} •••• {dto.CardLastFour}";
    }

    private async Task BroadcastPaidAsync(int eventId, IEnumerable<EventSeat> seats)
    {
        var payload = new SeatsChangedDTO
        {
            EventId = eventId,
            Seats = seats
                .Select(es => new SeatStatusChangeDTO
                {
                    EventSeatId = es.Id,
                    Status = SeatStatus.Paid.ToString()
                })
                .ToList()
        };

        if (payload.Seats.Count == 0)
            return;

        await _hub.Clients
            .Group(SeatHub.GroupFor(eventId))
            .SendAsync("SeatsChanged", payload);
    }
}