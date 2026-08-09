using System.ComponentModel.DataAnnotations;

namespace MisterTicketApi.Entities
{
    public class User
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public required string FirstName { get; set; } = string.Empty;
        [MaxLength(50)]
        public required string LastName { get; set; } = string.Empty;
        [MaxLength(100)]
        public required string Email { get; set; } = string.Empty;
        public Role Role { get; set; }
        public required string Password; //??
        public List<Reservation> ReservationsHistory { get; set; } = new List<Reservation>();
    }

    public enum Role
    {
        Admin,
        Organizer,
        Client
    }
}
