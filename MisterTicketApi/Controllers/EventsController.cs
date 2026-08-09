using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.Entities;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]  // This makes it api/events
    public class EventsController : ControllerBase
    {
        private readonly IEventService eventService;

        public EventsController(IEventService eventService)
        {
            this.eventService = eventService;
        }

        // GET: api/events/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var evnt = await Task.Run(() => eventService.Get(id));
                if (evnt is null)
                    return NotFound("Event not found");
                return Ok(evnt);
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

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var events = await Task.Run(() => eventService.GetAll());
                if (events is null || !events.Any())
                    return NotFound("No events found");
                return Ok(events);
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

        // POST: api/events
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Event newEvent)
        {
            try
            {
                var result = await Task.Run(() => eventService.Create(newEvent));
                return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
            }
            catch (KeyNotFoundException ex)
            {
                // e.g., invalid VenueId
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // PUT: api/events/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Event updatedEvent)
        {
            try
            {
                if (id != updatedEvent.Id)
                    return BadRequest("ID mismatch");

                var updated = await Task.Run(() => eventService.Update(updatedEvent));
                if (updated is null)
                    return NotFound("Event not found");
                return Ok(updated);
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

        // DELETE: api/events/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await Task.Run(() => eventService.Delete(id));
                if (!result)
                    return NotFound("Event not found");
                return NoContent(); // 204 No Content is standard for successful DELETE
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
