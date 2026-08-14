using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.DTOs;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    // POST: api/reservations  -> holds the picked seats for 15 minutes
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReservationDTO>> Create(ReservationCreateDTO dto)
    {
        try
        {
            var created = await _reservationService.CreateAsync(dto, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            // 409: the seats were free when the map was drawn, not any more.
            return Conflict(new { message = ex.Message });
        }
    }

    // GET: api/reservations/12
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ReservationDTO>> GetById(int id)
    {
        var reservation = await _reservationService.GetByIdAsync(id, GetCurrentUserId());
        if (reservation is null)
            return NotFound();

        return Ok(reservation);
    }

    // GET: api/reservations/mine  -> history for the profile page
    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ReservationDTO>>> GetMine()
    {
        return Ok(await _reservationService.GetMineAsync(GetCurrentUserId()));
    }


    // DELETE: api/reservations/12  -> gives the seats back
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var cancelled = await _reservationService.CancelAsync(id, GetCurrentUserId());
            return cancelled ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}