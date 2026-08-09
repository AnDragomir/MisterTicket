using MisterTicketApi.Entities;

namespace MisterTicketApi.Services.ServicesInterfaces
{
    public interface IEventService
    {
        public Event Create(Event evnt);
        public Event Get(int id);
        public List<Event> GetAll();
        public Event Update(Event evnt);
        public bool Delete(int id);

    }
}
