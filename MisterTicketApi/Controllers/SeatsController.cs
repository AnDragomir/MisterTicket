using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.DTOs;
using MisterTicketApi.Services;

namespace MisterTicketApi.Controllers;

[ApiController]
[Route("api/venues/{venueId:int}/seats")]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seatService;

    public SeatsController(ISeatService seatService)
    {
        _seatService = seatService;
    }

    // GET: api/venues/3/seats
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SeatDTO>>> GetByVenue(int venueId)
    {
        var seats = await _seatService.GetByVenueAsync(venueId);
        if (seats is null)
            return NotFound(new { message = $"Venue {venueId} does not exist." });

        return Ok(seats);
    }

    // POST: api/venues/3/seats  -> generates a block of rows
    [HttpPost]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<IEnumerable<SeatDTO>>> CreateRows(int venueId, SeatBulkCreateDTO dto)
    {
        try
        {
            var created = await _seatService.CreateRowsAsync(venueId, dto);
            if (created is null)
                return NotFound(new { message = $"Venue {venueId} does not exist." });

            return CreatedAtAction(nameof(GetByVenue), new { venueId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE: api/venues/3/seats/42
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Delete(int venueId, int id)
    {
        try
        {
            var deleted = await _seatService.DeleteAsync(venueId, id);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE: api/venues/3/seats  -> clears the whole layout
    [HttpDelete]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> DeleteAll(int venueId)
    {
        try
        {
            var count = await _seatService.DeleteAllAsync(venueId);
            if (count is null)
                return NotFound(new { message = $"Venue {venueId} does not exist." });

            return Ok(new { deleted = count });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}