using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.DTOs;
using MisterTicketApi.Services;
using MisterTicketApi.Services.ServicesInterfaces;
using System.Security.Claims;

namespace MisterTicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    // GET: api/events?venueId=3
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<EventDTO>>> GetAll([FromQuery] int? venueId)
    {
        var events = await _eventService.GetAllAsync(venueId);
        return Ok(events);
    }

    // GET: api/events/5
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDetailDTO>> GetById(int id)
    {
        var found = await _eventService.GetByIdAsync(id);
        if (found is null)
            return NotFound();

        return Ok(found);
    }

    // GET: api/events/5/seats  -> the seat map, public so visitors can look first
    [HttpGet("{id:int}/seats")]
    [AllowAnonymous]
    public async Task<ActionResult<SeatMapDTO>> GetSeatMap(int id, [FromServices] IReservationService reservationService)
    {
        // Anonymous visitors get the map without any seat marked as theirs.
        int? userId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : null;

        var map = await reservationService.GetSeatMapAsync(id, userId);
        if (map is null)
            return NotFound();

        return Ok(map);
    }

    // POST: api/events
    [HttpPost]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<EventDetailDTO>> Create(EventCreateDTO dto)
    {
        try
        {
            var created = await _eventService.CreateAsync(dto, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/events/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<EventDetailDTO>> Update(int id, EventUpdateDTO dto)
    {
        var updated = await _eventService.UpdateAsync(id, dto);
        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // DELETE: api/events/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _eventService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Id of the authenticated user, taken from the JWT "sub"/NameIdentifier claim.</summary>
    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(value!);
    }
}