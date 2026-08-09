using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.Entities
{
    public class Venue
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        //Zones tarifaires
        //Disposition des sieges
    }
}
