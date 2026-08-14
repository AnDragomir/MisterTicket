using System.ComponentModel.DataAnnotations;
using System.Data;

namespace MisterTicketApi.Entities;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(120)]
    public string LastName { get; set; } = null!;

    [Required, MaxLength(180), EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = null!;

    public Role Role { get; set; } = Role.Client;

    // Navigation
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}