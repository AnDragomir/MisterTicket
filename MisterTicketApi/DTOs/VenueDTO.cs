using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.DTOs;

/// <summary>Venue as shown in a list.</summary>
public class VenueDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? City { get; set; }
    public int Capacity { get; set; }
    public int SeatCount { get; set; }
}

/// <summary>Venue with the zones actually used by its seats, for the detail page.</summary>
public class VenueDetailDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? City { get; set; }
    public int Capacity { get; set; }
    public int SeatCount { get; set; }

    /// <summary>Derived from the seats: a venue has a zone because seats use it.</summary>
    public List<VenueZoneDTO> PricingZones { get; set; } = new();
}

/// <summary>A shared zone, plus how many seats of this venue belong to it.</summary>
public class VenueZoneDTO : PricingZoneDTO
{
    public int SeatCount { get; set; }
}


public class VenueCreateDTO
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? City { get; set; }

    [Range(1, 100000)]
    public int Capacity { get; set; }
}
public class VenueUpdateDTO
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? City { get; set; }

    [Range(1, 100000)]
    public int Capacity { get; set; }
}



