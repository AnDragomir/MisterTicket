using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.DTOs;

public class PricingZoneDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ColorHex { get; set; } = null!;
    public decimal BasePrice { get; set; }
}

public class PricingZoneCreateDTO
{
    /// <summary>Unique across the catalogue, e.g. "VIP", "Balcony".</summary>
    [Required, MaxLength(80)]
    public string Name { get; set; } = null!;

    [Required]
    [RegularExpression("^#([0-9a-fA-F]{6})$", ErrorMessage = "ColorHex must look like #RRGGBB.")]
    public string ColorHex { get; set; } = "#CCCCCC";

    /// <summary>Default price used to seed EventSeat.Price on new events.</summary>
    [Range(0, 100000)]
    public decimal BasePrice { get; set; }
}


public class PricingZoneUpdateDTO
{
    [Required, MaxLength(80)]
    public string Name { get; set; } = null!;

    [Required]
    [RegularExpression("^#([0-9a-fA-F]{6})$", ErrorMessage = "ColorHex must look like #RRGGBB.")]
    public string ColorHex { get; set; } = null!;

    /// <summary>
    /// Only affects events created from now on: EventSeat.Price was copied at
    /// event creation and is not recalculated.
    /// </summary>
    [Range(0, 100000)]
    public decimal BasePrice { get; set; }
}

