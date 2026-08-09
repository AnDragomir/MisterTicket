using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Entities;

namespace MisterTicketApi.Database
{
    public class MisterTicketContext : DbContext
    {
        public MisterTicketContext(DbContextOptions<MisterTicketContext> options) : base (options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Seat> Seats { get; set; }

    }
}
