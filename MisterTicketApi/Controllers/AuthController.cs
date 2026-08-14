using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.DTOs;
using MisterTicketApi.Services;

namespace MisterTicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDTO>> Register(RegisterDTO dto)
    {
        try
        {
            var response = await _authService.RegisterAsync(dto);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO dto)
    {
        var response = await _authService.LoginAsync(dto);

        // Same answer for "unknown email" and "wrong password":
        // telling them apart lets anyone find out which emails have an account.
        if (response is null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(response);
    }

    // GET: api/auth/me  -> lets the Angular app restore the session from a stored token
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDTO>> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user is null)
            return NotFound();

        return Ok(user);
    }
}