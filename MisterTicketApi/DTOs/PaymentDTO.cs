using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.DTOs;

/// <summary>
/// What the client sends to pay. Nothing here is a real credential: the card
/// number never leaves the browser, only its last four digits are kept so the
/// receipt can show "•••• 4242".
/// </summary>
public class PaymentCreateDTO
{
    /// <summary>"Visa", "Mastercard", "Bancontact" or "QrCode".</summary>
    [Required, MaxLength(50)]
    public string Method { get; set; } = null!;

    /// <summary>Last four digits of the card, when a card was used.</summary>
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Expected exactly four digits.")]
    public string? CardLastFour { get; set; }
}

public class PaymentDTO
{
    public int Id { get; set; }
    public string Reference { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string? Method { get; set; }
    public string? CardLastFour { get; set; }
    public DateTime? PaidAt { get; set; }
}