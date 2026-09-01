using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MisterTicketApi.Entities;

namespace MisterTicketApi.Database;

public class MisterTicketContext : DbContext
{
    public MisterTicketContext(DbContextOptions<MisterTicketContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Venue> Venues { get; set; }
    public DbSet<PricingZone> PricingZones { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventSeat> EventSeats { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Payment> Payments { get; set; }



    // Lengths, required columns and decimal precision are declared with data
    // annotations on the entities. Only what annotations cannot express is
    // configured here: relationships, delete behaviours and unique indexes.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQL Server does not store the DateTimeKind, so values come back as
        // Unspecified and serialise without the "Z". Mark them UTC on read so the
        // browser parses them as UTC instead of local time.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            write => write,
            read => DateTime.SpecifyKind(read, DateTimeKind.Utc));

        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            write => write,
            read => read.HasValue ? DateTime.SpecifyKind(read.Value, DateTimeKind.Utc) : read);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(utcConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableUtcConverter);
            }
        }

        // ---------- User ----------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // ---------- PricingZone ----------
        modelBuilder.Entity<PricingZone>(entity =>
        {
            // Zones are shared by every venue, so the name is globally unique
            entity.HasIndex(z => z.Name).IsUnique();
        });

        // ---------- Seat ----------
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasOne(s => s.Venue)
                  .WithMany(v => v.Seats)
                  .HasForeignKey(s => s.VenueId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Deleting a zone that is still used by seats must fail.
            entity.HasOne(s => s.PricingZone)
                  .WithMany(z => z.Seats)
                  .HasForeignKey(s => s.PricingZoneId)
                  .OnDelete(DeleteBehavior.Restrict);

            // A seat number is unique inside a venue
            entity.HasIndex(s => new { s.VenueId, s.RowLabel, s.Number }).IsUnique();
        });

        // ---------- Event ----------
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Venue)
                  .WithMany(v => v.Events)
                  .HasForeignKey(e => e.VenueId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Organizer)
                  .WithMany()
                  .HasForeignKey(e => e.OrganizerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- EventSeat ----------
        modelBuilder.Entity<EventSeat>(entity =>
        {
            entity.HasOne(es => es.Event)
                  .WithMany(e => e.EventSeats)
                  .HasForeignKey(es => es.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(es => es.Seat)
                  .WithMany(s => s.EventSeats)
                  .HasForeignKey(es => es.SeatId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Cancelling a reservation frees the seats instead of deleting them
            entity.HasOne(es => es.Reservation)
                  .WithMany(r => r.EventSeats)
                  .HasForeignKey(es => es.ReservationId)
                  .OnDelete(DeleteBehavior.SetNull);

            // One row per (event, seat)
            entity.HasIndex(es => new { es.EventId, es.SeatId }).IsUnique();
        });

        // ---------- Reservation ----------
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasOne(r => r.User)
                  .WithMany(u => u.Reservations)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Reservations)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Payment ----------
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(p => p.Reference).IsUnique();

            // One payment per reservation
            entity.HasOne(p => p.Reservation)
                  .WithOne(r => r.Payment)
                  .HasForeignKey<Payment>(p => p.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }


}