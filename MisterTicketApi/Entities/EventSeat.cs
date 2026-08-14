using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MisterTicketApi.Entities;

/// <summary>
/// One seat, for one event. This is what the seat map displays and what a
/// reservation actually books. Created when an event is created (one row per
/// seat of the venue).
/// </summary>
public class EventSeat
{
    public int Id { get; set; }

    public SeatStatus Status { get; set; } = SeatStatus.Free;

    /// <summary>Price for this seat at this event (initialised from PricingZone.BasePrice).</summary>
    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 100000)]
    public decimal Price { get; set; }

    // FK
    public int EventId { get; set; }
    public int SeatId { get; set; }
    public int? ReservationId { get; set; }   // null when the seat is free

    // Navigation
    public Event Event { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
    public Reservation? Reservation { get; set; }
}