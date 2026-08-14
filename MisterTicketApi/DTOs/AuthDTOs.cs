using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.DTOs;

public class RegisterDTO
{
    [Required, MaxLength(120)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(120)]
    public string LastName { get; set; } = null!;

    [Required, MaxLength(180), EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = null!;
}

public class LoginDTO
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

/// <summary>
/// Returned by register and login. The Angular app stores the token and reads
/// the user to know which menus to show.
/// </summary>
public class AuthResponseDTO
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public UserDTO User { get; set; } = null!;
}

/// <summary>Public view of a user: never exposes the password hash.</summary>
public class UserDTO
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
}

