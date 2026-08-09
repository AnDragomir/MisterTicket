using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.Entities
{
    public class Event
    {
        public int Id {  get; set; }
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }

        [MaxLength(100)]
        public string Description { get; set; } = string.Empty;
        public int VenueId { get; set; } // clé étrangère Venue (FK)
    }
}
