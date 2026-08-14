using MisterTicketApi.DTOs;

namespace MisterTicketApi.Services;

public interface IAuthService
{
    /// <summary>Registers a new Client account.</summary>
    /// <exception cref="InvalidOperationException">The email is already taken.</exception>
    Task<AuthResponseDTO> RegisterAsync(RegisterDTO dto);

    /// <returns>Null when the email is unknown or the password is wrong.</returns>
    Task<AuthResponseDTO?> LoginAsync(LoginDTO dto);

    /// <returns>Null if no user has this id.</returns>
    Task<UserDTO?> GetCurrentUserAsync(int userId);
}