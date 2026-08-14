using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MisterTicketApi.Entities;

/// <summary>
/// A coloured price category shared by every venue (e.g. "VIP", "Balcony").
/// A venue "has" a zone through its seats, not through a foreign key.
/// </summary>
public class PricingZone
{
    public int Id { get; set; }

    /// <summary>Unique across the whole catalogue.</summary>
    [Required, MaxLength(80)]
    public string Name { get; set; } = null!;

    /// <summary>Hex colour used by the seat map, e.g. "#E63946".</summary>
    [Required, MaxLength(7)]
    public string ColorHex { get; set; } = "#CCCCCC";

    /// <summary>
    /// Default price, copied into EventSeat.Price when an event is created.
    /// The event keeps its own prices afterwards.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 100000)]
    public decimal BasePrice { get; set; }

    // Navigation
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}