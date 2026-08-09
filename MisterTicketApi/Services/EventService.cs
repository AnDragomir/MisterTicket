using MisterTicketApi.Database;
using MisterTicketApi.Entities;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Services
{
    public class EventService : IEventService
    {
        private readonly MisterTicketContext _dbContext;

        public EventService(MisterTicketContext context)
        {
            _dbContext = context;
        }

        public Event Create(Event evnt)
        {
            // Optional: check that the Venue exists before creating the Event
            var venue = _dbContext.Venues.Find(evnt.VenueId);
            if (venue == null)
                throw new KeyNotFoundException($"Venue with id {evnt.VenueId} not found");

            _dbContext.Events.Add(evnt);
            _dbContext.SaveChanges();
            return evnt;
        }

        public Event Get(int id)
        {
            // Include Venue info for convenience
            var evnt = _dbContext.Events
                .FirstOrDefault(e => e.Id == id);

            if (evnt == null)
                throw new KeyNotFoundException($"Event with id {id} not found");

            return evnt;
        }

        public List<Event> GetAll()
        {
            var events = _dbContext.Events.ToList();

            return events;
        }

        public Event Update(Event evnt)
        {
            var existingEvent = _dbContext.Events.Find(evnt.Id);
            if (existingEvent == null)
                throw new KeyNotFoundException($"Event with id {evnt.Id} not found");

            // Optional: validate VenueId again if changed
            var venue = _dbContext.Venues.Find(evnt.VenueId);
            if (venue == null)
                throw new KeyNotFoundException($"Venue with id {evnt.VenueId} not found");

            // Update properties
            existingEvent.Name = evnt.Name;
            existingEvent.DateTime = evnt.DateTime;
            existingEvent.Description = evnt.Description;
            existingEvent.VenueId = evnt.VenueId;

            _dbContext.SaveChangesAsync(); //async?
            return existingEvent;
        }

        public bool Delete(int id)
        {
            var evnt = _dbContext.Events.Find(id);
            if (evnt == null)
                return false;

            _dbContext.Events.Remove(evnt);
            _dbContext.SaveChanges();

            return true;
        }

    }
}
