using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.Entities;

/// <summary>
/// A physical seat in a venue. It has no state of its own: availability always
/// depends on an event, and lives on EventSeat.
/// </summary>
public class Seat
{
    public int Id { get; set; }

    [Required, MaxLength(10)]
    public string RowLabel { get; set; } = null!;   // "A", "B", ...

    [Range(1, int.MaxValue)]
    public int Number { get; set; }                 // 1, 2, 3, ...

    // FK
    public int VenueId { get; set; }
    public int PricingZoneId { get; set; }

    // Navigation
    public Venue Venue { get; set; } = null!;
    public PricingZone PricingZone { get; set; } = null!;
    public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
}