using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MisterTicketApi.Entities;

/// <summary>
/// A basket of seats held by a client for one event.
/// While Status is Pending the seats are blocked until ExpiresAt.
/// </summary>
public class Reservation
{
    public int Id { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>End of the temporary hold; after this the seats are released.</summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 1000000)]
    public decimal TotalAmount { get; set; }

    // FK
    public int UserId { get; set; }
    public int EventId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
    public Payment? Payment { get; set; }
}