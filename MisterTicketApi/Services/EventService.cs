using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Database;
using MisterTicketApi.DTOs;
using MisterTicketApi.Entities;

namespace MisterTicketApi.Services;

public class EventService : IEventService
{
    private readonly MisterTicketContext _context;

    public EventService(MisterTicketContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EventDTO>> GetAllAsync(int? venueId = null)
    {
        var query = _context.Events.AsNoTracking();

        if (venueId.HasValue)
            query = query.Where(e => e.VenueId == venueId.Value);

        return await query
            .OrderBy(e => e.StartsAt)
            .Select(e => new EventDTO
            {
                Id = e.Id,
                Name = e.Name,
                StartsAt = e.StartsAt,
                VenueId = e.VenueId,
                VenueName = e.Venue.Name,
                VenueCity = e.Venue.City
            })
            .ToListAsync();
    }

    public async Task<EventDetailDTO?> GetByIdAsync(int id)
    {
        return await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventDetailDTO
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                StartsAt = e.StartsAt,
                VenueId = e.VenueId,
                VenueName = e.Venue.Name,
                VenueCity = e.Venue.City,
                OrganizerName = e.Organizer.FirstName + " " + e.Organizer.LastName,
                TotalSeats = e.EventSeats.Count,
                FreeSeats = e.EventSeats.Count(es => es.Status == SeatStatus.Free)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EventDetailDTO> CreateAsync(EventCreateDTO dto, int organizerId)
    {
        var venue = await _context.Venues
            .Include(v => v.Seats)
                .ThenInclude(s => s.PricingZone)
            .FirstOrDefaultAsync(v => v.Id == dto.VenueId);

        if (venue is null)
            throw new InvalidOperationException($"Venue {dto.VenueId} does not exist.");

        if (venue.Seats.Count == 0)
            throw new InvalidOperationException("This venue has no seats yet; add seats before creating an event.");

        var newEvent = new Event
        {
            Name = dto.Name,
            Description = dto.Description,
            StartsAt = dto.StartsAt,
            VenueId = venue.Id,
            OrganizerId = organizerId
        };

        // One EventSeat per physical seat, priced from its zone.
        foreach (var seat in venue.Seats)
        {
            newEvent.EventSeats.Add(new EventSeat
            {
                SeatId = seat.Id,
                Status = SeatStatus.Free,
                Price = seat.PricingZone.BasePrice
            });
        }

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(newEvent.Id))!;
    }

    public async Task<EventDetailDTO?> UpdateAsync(int id, EventUpdateDTO dto)
    {
        var existing = await _context.Events.FindAsync(id);
        if (existing is null)
            return null;

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.StartsAt = dto.StartsAt;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Events.FindAsync(id);
        if (existing is null)
            return false;

        var hasReservations = await _context.Reservations.AnyAsync(r => r.EventId == id);
        if (hasReservations)
            throw new InvalidOperationException("This event has reservations and cannot be deleted.");

        // EventSeats are removed by the cascade configured in the DbContext.
        _context.Events.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}