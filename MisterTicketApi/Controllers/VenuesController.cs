using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.DTOs;
using MisterTicketApi.Services;

namespace MisterTicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    // GET: api/venues
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<VenueDTO>>> GetAll()
    {
        var venues = await _venueService.GetAllAsync();
        return Ok(venues);
    }

    // GET: api/venues/5
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<VenueDetailDTO>> GetById(int id)
    {
        var venue = await _venueService.GetByIdAsync(id);
        if (venue is null)
            return NotFound();

        return Ok(venue);
    }

    // POST: api/venues
    [HttpPost]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<VenueDetailDTO>> Create(VenueCreateDTO dto)
    {
        var created = await _venueService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT: api/venues/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<VenueDetailDTO>> Update(int id, VenueUpdateDTO dto)
    {
        var updated = await _venueService.UpdateAsync(id, dto);
        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // DELETE: api/venues/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _venueService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}