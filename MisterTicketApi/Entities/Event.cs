using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.Entities;

/// <summary>
/// A theatre play (or any cultural/sports show) taking place in a venue at a given date.
/// </summary>
public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartsAt { get; set; }

    //[Required]
    //public int Duration { get; set; }

    // FK
    public int VenueId { get; set; }
    public int OrganizerId { get; set; }

    // Navigation
    public Venue Venue { get; set; } = null!;
    public User Organizer { get; set; } = null!;
    public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}