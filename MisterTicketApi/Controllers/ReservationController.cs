using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.DTOs;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    // GET: api/reservations/events/5/active  -> the basket being filled, if any
    [HttpGet("events/{eventId:int}/active")]
    public async Task<ActionResult<ReservationDTO?>> GetActive(int eventId)
    {
        var active = await _reservationService.GetActiveAsync(eventId, GetCurrentUserId());

        // No basket is a normal answer, not an error.
        return Ok(active);
    }

    // POST: api/reservations/events/5/seats/42  -> claim one seat
    [HttpPost("events/{eventId:int}/seats/{eventSeatId:int}")]
    public async Task<ActionResult<ReservationDTO>> ClaimSeat(int eventId, int eventSeatId)
    {
        try
        {
            var reservation = await _reservationService.ClaimSeatAsync(eventId, eventSeatId, GetCurrentUserId());
            return Ok(reservation);
        }
        catch (InvalidOperationException ex)
        {
            // 409: somebody else clicked that seat first.
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE: api/reservations/events/5/seats/42  -> give one seat back
    [HttpDelete("events/{eventId:int}/seats/{eventSeatId:int}")]
    public async Task<ActionResult<ReservationDTO?>> ReleaseSeat(int eventId, int eventSeatId)
    {
        try
        {
            // Null means that was the last seat: the basket is gone.
            var reservation = await _reservationService.ReleaseSeatAsync(eventId, eventSeatId, GetCurrentUserId());
            return Ok(reservation);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // GET: api/reservations/12
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDTO>> GetById(int id)
    {
        var reservation = await _reservationService.GetByIdAsync(id, GetCurrentUserId());
        if (reservation is null)
            return NotFound();

        return Ok(reservation);
    }

    // GET: api/reservations/mine  -> history for the profile page
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<ReservationDTO>>> GetMine()
    {
        return Ok(await _reservationService.GetMineAsync(GetCurrentUserId()));
    }

    // DELETE: api/reservations/12  -> gives every seat of the basket back
    [HttpDelete("{id:int}")]
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