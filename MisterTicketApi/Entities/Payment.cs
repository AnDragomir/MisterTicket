using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MisterTicketApi.Entities;

/// <summary>
/// Fake payment attached to a reservation (one-to-one).
/// </summary>
public class Payment
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Reference { get; set; } = null!;   // e.g. "PAY-2026-000123"

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 1000000)]
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [MaxLength(50)]
    public string? Method { get; set; }              // "TestCard", "QrCode", ...

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    // FK
    public int ReservationId { get; set; }

    // Navigation
    public Reservation Reservation { get; set; } = null!;
}