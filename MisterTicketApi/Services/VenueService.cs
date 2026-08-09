using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Database;
using MisterTicketApi.Entities;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Services
{
    public class VenueService : IVenueService
    {
        private readonly MisterTicketContext _dbContext;

        public VenueService(MisterTicketContext context)
        {
            _dbContext = context;
        }

        public Venue Create(Venue venue)
        {
            _dbContext.Venues.Add(venue);
            _dbContext.SaveChanges();
            return venue;
        }

        /*public List<Venue> GetAll()
        {
            var venues = _dbContext.Venues.ToList();

            return venues;
        }*/

        public List<Venue> GetAll()
        {
            var venues = _dbContext.Venues.ToList();
            return venues;
        }

        public bool Delete(int id)
        {
            var venue = _dbContext.Venues.Find(id);
            if (venue == null)
                return false;

            _dbContext.Venues.Remove(venue);
            _dbContext.SaveChanges();

            return true;
        }

        /*public Venue Get(int id)
        {
            var venue = _dbContext.Venues.Find(id);
            if (venue == null)
            {
                throw new KeyNotFoundException($"Venue with id {id} not found");
            }

            return venue;
        }*/

        public Venue Get(int id)
        {
            var venue = _dbContext.Venues.FirstOrDefault(v => v.Id == id);

            if (venue == null)
            {
                throw new KeyNotFoundException($"Venue with id {id} not found");
            }

            return venue;
        }

        public Venue Update(Venue venue)
        {
            var existingVenue = _dbContext.Venues.Find(venue.Id);
            if (existingVenue == null)
            {
                throw new KeyNotFoundException($"Venue with id {venue.Id} not found");
            }

            // Update properties
            existingVenue.Name = venue.Name;
            existingVenue.Capacity = venue.Capacity;
            _dbContext.SaveChanges();

            return existingVenue;
        }
    }
}
