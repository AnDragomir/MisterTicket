using MisterTicketApi.Entities;

namespace MisterTicketApi.Services.ServicesInterfaces
{
    public interface IVenueService
    {
        public Venue Create(Venue venue);
        public Venue Get(int id);
        public List<Venue> GetAll();
        public Venue Update(Venue venue);
        public bool Delete(int id);
    }
}
