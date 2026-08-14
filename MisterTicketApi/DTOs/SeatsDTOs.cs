using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.DTOs;

public class SeatDTO
{
    public int Id { get; set; }
    public string RowLabel { get; set; } = null!;
    public int Number { get; set; }

    public int PricingZoneId { get; set; }
    public string PricingZoneName { get; set; } = null!;
    public string PricingZoneColor { get; set; } = null!;
}

/// <summary>
/// Generates a rectangular block of seats in one call.
/// Example: FirstRow "A", RowCount 6, SeatsPerRow 20 => rows A to F, seats 1 to 20.
/// </summary>
public class SeatBulkCreateDTO
{
    /// <summary>Zone all generated seats belong to. Must belong to the same venue.</summary>
    [Range(1, int.MaxValue)]
    public int PricingZoneId { get; set; }

    /// <summary>Label of the first row: a single letter A-Z.</summary>
    [Required]
    [RegularExpression("^[A-Z]$", ErrorMessage = "FirstRow must be a single capital letter A-Z.")]
    public string FirstRow { get; set; } = "A";

    [Range(1, 26)]
    public int RowCount { get; set; } = 1;

    [Range(1, 100)]
    public int SeatsPerRow { get; set; } = 10;

    /// <summary>Number of the first seat of each row (usually 1).</summary>
    [Range(1, 1000)]
    public int FirstSeatNumber { get; set; } = 1;
}

public class EventSeatDTO
{
    public int Id { get; set; }              // EventSeat id: what a reservation refers to
    public string RowLabel { get; set; } = null!;
    public int Number { get; set; }

    /// <summary>"Free", "Reserved" or "Paid".</summary>
    public string Status { get; set; } = null!;

    public decimal Price { get; set; }

    public int PricingZoneId { get; set; }
    public string PricingZoneName { get; set; } = null!;
    public string PricingZoneColor { get; set; } = null!;

    /// <summary>True when the seat is held or paid by the signed-in user.</summary>
    public bool IsMine { get; set; }
}

/// <summary>The whole map for one event, rows ordered from the stage backwards.</summary>
public class SeatMapDTO
{
    public int EventId { get; set; }
    public string EventName { get; set; } = null!;
    public string VenueName { get; set; } = null!;
    public DateTime StartsAt { get; set; }

    public List<EventSeatDTO> Seats { get; set; } = new();
}

/// <summary>One seat whose status changed.</summary>
public class SeatStatusChangeDTO
{
    public int EventSeatId { get; set; }

    /// <summary>"Free", "Reserved" or "Paid".</summary>
    public string Status { get; set; } = null!;
}

/// <summary>Payload pushed to everyone watching an event's seat map.</summary>
public class SeatsChangedDTO
{
    public int EventId { get; set; }
    public List<SeatStatusChangeDTO> Seats { get; set; } = new();
}

