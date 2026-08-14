using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.DTOs;

/// <summary>What the client sends when confirming a seat selection.</summary>
public class ReservationCreateDTO
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    /// <summary>EventSeat ids picked on the map.</summary>
    [Required, MinLength(1)]
    public List<int> EventSeatIds { get; set; } = new();
}

/// <summary>One booked seat inside a reservation.</summary>
public class ReservationSeatDTO
{
    public int EventSeatId { get; set; }
    public string RowLabel { get; set; } = null!;
    public int Number { get; set; }
    public decimal Price { get; set; }
    public string PricingZoneName { get; set; } = null!;
}

public class ReservationDTO
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public string EventName { get; set; } = null!;
    public string VenueName { get; set; } = null!;
    public DateTime StartsAt { get; set; }

    /// <summary>"Pending", "Paid" or "Cancelled".</summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    /// <summary>When a pending hold is released. The front-end counts down to this.</summary>
    public DateTime ExpiresAt { get; set; }

    public decimal TotalAmount { get; set; }

    public List<ReservationSeatDTO> Seats { get; set; } = new();
}