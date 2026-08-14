using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Entities;

namespace MisterTicketApi.Database;

/// <summary>
/// Fills an empty database with demo data. Does nothing if users already exist,
/// so it is safe to run at every startup.
/// </summary>
public static class DbSeeder
{
    private const string DemoPassword = "Password123!";

    public static async Task SeedAsync(MisterTicketContext context, IPasswordHasher<User> passwordHasher)
    {
        if (await context.Users.AnyAsync())
            return;

        var users = SeedUsers(context, passwordHasher);
        var zones = SeedPricingZones(context);

        // Ids are needed by the seats and events, so save before going further.
        await context.SaveChangesAsync();

        var venues = SeedVenues(context, zones);
        await context.SaveChangesAsync();

        var events = SeedEvents(context, venues, users.Organizer);
        await context.SaveChangesAsync();

        SeedReservations(context, events, users.Client, users.SecondClient);
        await context.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- users

    private static (User Admin, User Organizer, User Client, User SecondClient) SeedUsers(
        MisterTicketContext context, IPasswordHasher<User> passwordHasher)
    {
        var admin = NewUser("Alice", "Admin", "admin@misterticket.be", Role.Admin);
        var organizer = NewUser("Olivier", "Organisateur", "orga@misterticket.be", Role.Organizer);
        var client = NewUser("Chloe", "Client", "client@misterticket.be", Role.Client);
        var secondClient = NewUser("Bram", "Peeters", "bram@misterticket.be", Role.Client);

        foreach (var user in new[] { admin, organizer, client, secondClient })
            user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);

        context.Users.AddRange(admin, organizer, client, secondClient);

        return (admin, organizer, client, secondClient);
    }

    private static User NewUser(string firstName, string lastName, string email, Role role) => new()
    {
        FirstName = firstName,
        LastName = lastName,
        Email = email,          // already lowercase, like AuthService stores them
        Role = role,
        PasswordHash = string.Empty
    };

    // ---------------------------------------------------------------- zones

    private static (PricingZone Vip, PricingZone Orchestra, PricingZone Balcony) SeedPricingZones(
        MisterTicketContext context)
    {
        var vip = new PricingZone { Name = "VIP", ColorHex = "#E63946", BasePrice = 75m };
        var orchestra = new PricingZone { Name = "Orchestre", ColorHex = "#457B9D", BasePrice = 45m };
        var balcony = new PricingZone { Name = "Balcon", ColorHex = "#2A9D8F", BasePrice = 28m };

        context.PricingZones.AddRange(vip, orchestra, balcony);

        return (vip, orchestra, balcony);
    }

    // --------------------------------------------------------------- venues

    private static (Venue Royal, Venue Moliere) SeedVenues(
        MisterTicketContext context,
        (PricingZone Vip, PricingZone Orchestra, PricingZone Balcony) zones)
    {
        // 24 + 84 + 48 = 156 seats
        var royal = new Venue { Name = "Theatre Royal", City = "Gent" };
        AddRows(royal, zones.Vip, firstRow: 'A', rowCount: 2, seatsPerRow: 12);
        AddRows(royal, zones.Orchestra, firstRow: 'C', rowCount: 6, seatsPerRow: 14);
        AddRows(royal, zones.Balcony, firstRow: 'I', rowCount: 4, seatsPerRow: 12);

        // 96 + 48 = 144 seats
        var moliere = new Venue { Name = "Salle Moliere", City = "Bruxelles" };
        AddRows(moliere, zones.Orchestra, firstRow: 'A', rowCount: 6, seatsPerRow: 16);
        AddRows(moliere, zones.Balcony, firstRow: 'G', rowCount: 4, seatsPerRow: 12);

        foreach (var venue in new[] { royal, moliere })
            venue.Capacity = venue.Seats.Count;

        context.Venues.AddRange(royal, moliere);

        return (royal, moliere);
    }

    /// <summary>Same rectangular generation as SeatService.CreateRowsAsync.</summary>
    private static void AddRows(Venue venue, PricingZone zone, char firstRow, int rowCount, int seatsPerRow)
    {
        for (var r = 0; r < rowCount; r++)
        {
            var rowLabel = ((char)(firstRow + r)).ToString();

            for (var number = 1; number <= seatsPerRow; number++)
            {
                venue.Seats.Add(new Seat
                {
                    RowLabel = rowLabel,
                    Number = number,
                    PricingZone = zone
                });
            }
        }
    }

    // --------------------------------------------------------------- events

    private static (Event Cyrano, Event Misanthrope, Event Godot) SeedEvents(
        MisterTicketContext context,
        (Venue Royal, Venue Moliere) venues,
        User organizer)
    {
        var cyrano = NewEvent(
            "Cyrano de Bergerac",
            "Le classique de Rostand, dans une mise en scene contemporaine.",
            DateTime.UtcNow.AddDays(21).Date.AddHours(20),
            venues.Royal, organizer);

        var misanthrope = NewEvent(
            "Le Misanthrope",
            "Moliere en costumes d'epoque, duree 2h sans entracte.",
            DateTime.UtcNow.AddDays(35).Date.AddHours(19),
            venues.Moliere, organizer);

        var godot = NewEvent(
            "En attendant Godot",
            "Beckett, dans une version epuree pour quatre comediens.",
            DateTime.UtcNow.AddDays(50).Date.AddHours(20).AddMinutes(30),
            venues.Royal, organizer);

        context.Events.AddRange(cyrano, misanthrope, godot);

        return (cyrano, misanthrope, godot);
    }

    private static Event NewEvent(string name, string description, DateTime startsAt, Venue venue, User organizer)
    {
        var newEvent = new Event
        {
            Name = name,
            Description = description,
            StartsAt = startsAt,
            Venue = venue,
            Organizer = organizer
        };

        // One EventSeat per seat of the venue, priced from its zone,
        // exactly like EventService.CreateAsync does.
        foreach (var seat in venue.Seats)
        {
            newEvent.EventSeats.Add(new EventSeat
            {
                Seat = seat,
                Status = SeatStatus.Free,
                Price = seat.PricingZone.BasePrice
            });
        }

        return newEvent;
    }

    // --------------------------------------------------------- reservations

    private static void SeedReservations(
        MisterTicketContext context,
        (Event Cyrano, Event Misanthrope, Event Godot) events,
        User client,
        User secondClient)
    {
        // A paid reservation: two VIP seats with their payment.
        var paidSeats = events.Cyrano.EventSeats
            .Where(es => es.Seat.RowLabel == "A" && es.Seat.Number <= 2)
            .ToList();

        var paidReservation = new Reservation
        {
            User = client,
            Event = events.Cyrano,
            Status = ReservationStatus.Paid,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            ExpiresAt = DateTime.UtcNow.AddDays(-3).AddMinutes(15),
            TotalAmount = paidSeats.Sum(es => es.Price)
        };

        foreach (var seat in paidSeats)
        {
            seat.Status = SeatStatus.Paid;
            seat.Reservation = paidReservation;
        }

        paidReservation.Payment = new Payment
        {
            Reference = "PAY-DEMO-0001",
            Amount = paidReservation.TotalAmount,
            Status = PaymentStatus.Succeeded,
            Method = "TestCard",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            PaidAt = DateTime.UtcNow.AddDays(-3)
        };

        // A pending reservation: seats held, timer still running.
        var heldSeats = events.Cyrano.EventSeats
            .Where(es => es.Seat.RowLabel == "D" && es.Seat.Number <= 3)
            .ToList();

        var pendingReservation = new Reservation
        {
            User = secondClient,
            Event = events.Cyrano,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            TotalAmount = heldSeats.Sum(es => es.Price)
        };

        foreach (var seat in heldSeats)
        {
            seat.Status = SeatStatus.Reserved;
            seat.Reservation = pendingReservation;
        }

        context.Reservations.AddRange(paidReservation, pendingReservation);
    }
}