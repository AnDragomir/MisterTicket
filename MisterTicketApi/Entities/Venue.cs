using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.Entities;

/// <summary>
/// A theatre, concert hall or stadium. Owns its physical seats.
/// </summary>
public class Venue
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? City { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    // Navigation
    // Zones are not owned by the venue: they are reached through the seats.
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}