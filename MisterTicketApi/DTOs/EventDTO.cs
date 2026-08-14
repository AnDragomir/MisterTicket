using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.DTOs;

/// <summary>Event as shown on the home page list.</summary>
public class EventDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public int VenueId { get; set; }
    public string VenueName { get; set; } = null!;
    public string? VenueCity { get; set; }
}

/// <summary>Event detail page: description + seat availability summary.</summary>
public class EventDetailDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }

    public int VenueId { get; set; }
    public string VenueName { get; set; } = null!;
    public string? VenueCity { get; set; }

    public string OrganizerName { get; set; } = null!;

    public int TotalSeats { get; set; }
    public int FreeSeats { get; set; }
}

public class EventCreateDTO
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartsAt { get; set; }

    /// <summary>Venue whose seats will be copied into EventSeats.</summary>
    [Range(1, int.MaxValue)]
    public int VenueId { get; set; }
}


/// <summary>
/// The venue is not updatable: changing it would invalidate every EventSeat
/// (and therefore existing reservations). Delete and recreate instead.
/// </summary>
public class EventUpdateDTO
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartsAt { get; set; }
}

