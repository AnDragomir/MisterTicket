using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.DTOs;
using MisterTicketApi.Services;

namespace MisterTicketApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricingZonesController : ControllerBase
{
    private readonly IPricingZoneService _zoneService;

    public PricingZonesController(IPricingZoneService zoneService)
    {
        _zoneService = zoneService;
    }

    // GET: api/pricingzones
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PricingZoneDTO>>> GetAll()
    {
        return Ok(await _zoneService.GetAllAsync());
    }

    // GET: api/pricingzones/7
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<PricingZoneDTO>> GetById(int id)
    {
        var zone = await _zoneService.GetByIdAsync(id);
        if (zone is null)
            return NotFound();

        return Ok(zone);
    }

    // POST: api/pricingzones
    [HttpPost]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<PricingZoneDTO>> Create(PricingZoneCreateDTO dto)
    {
        try
        {
            var created = await _zoneService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT: api/pricingzones/7
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<PricingZoneDTO>> Update(int id, PricingZoneUpdateDTO dto)
    {
        try
        {
            var updated = await _zoneService.UpdateAsync(id, dto);
            if (updated is null)
                return NotFound();

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE: api/pricingzones/7
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _zoneService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}