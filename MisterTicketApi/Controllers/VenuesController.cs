using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.Entities;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VenuesController : ControllerBase
    {
        private readonly IVenueService venueService;

        public VenuesController(IVenueService venueService)
        {
            this.venueService = venueService;
        }

        // GET: api/venues/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var venue = await Task.Run(() => venueService.Get(id));
                if (venue is null) return NotFound("Venue Not Found");
                return Ok(venue);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // GET: api/venues
        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var venues = await Task.Run(() => venueService.GetAll());
                if (venues is null || !venues.Any())
                    return NotFound("No venues found");
                return Ok(venues);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // POST: api/venues
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Venue newVenue)
        {
            try
            {
                var result = await Task.Run(() => venueService.Create(newVenue));
                return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // PUT: api/venues/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Venue newVenue)
        {
            try
            {
                if (id != newVenue.Id)
                    return BadRequest("ID mismatch");

                var updatedVenue = await Task.Run(() => venueService.Update(newVenue));
                if (updatedVenue is null) return NotFound("Venue Not Found");
                return Ok(updatedVenue);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // DELETE: api/venues/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await Task.Run(() => venueService.Delete(id));
                if (!result) return BadRequest("Something went wrong");
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
