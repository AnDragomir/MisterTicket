using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Database;
using MisterTicketApi.DTOs;
using MisterTicketApi.Entities;

namespace MisterTicketApi.Services;

public class VenueService : IVenueService
{
    private readonly MisterTicketContext _context;

    public VenueService(MisterTicketContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<VenueDTO>> GetAllAsync()
    {
        return await _context.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VenueDTO
            {
                Id = v.Id,
                Name = v.Name,
                City = v.City,
                Capacity = v.Capacity,
                SeatCount = v.Seats.Count
            })
            .ToListAsync();
    }

    public async Task<VenueDetailDTO?> GetByIdAsync(int id)
    {
        return await _context.Venues
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new VenueDetailDTO
            {
                Id = v.Id,
                Name = v.Name,
                City = v.City,
                Capacity = v.Capacity,
                SeatCount = v.Seats.Count,
                // Zones are shared, so we derive this venue's zones from its seats.
                PricingZones = v.Seats
                    .GroupBy(s => s.PricingZone)
                    .Select(g => new VenueZoneDTO
                    {
                        Id = g.Key.Id,
                        Name = g.Key.Name,
                        ColorHex = g.Key.ColorHex,
                        BasePrice = g.Key.BasePrice,
                        SeatCount = g.Count()
                    })
                    .OrderBy(z => z.Name)
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<VenueDetailDTO> CreateAsync(VenueCreateDTO dto)
    {
        var venue = new Venue
        {
            Name = dto.Name,
            City = dto.City,
            Capacity = dto.Capacity
        };

        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(venue.Id))!;
    }

    public async Task<VenueDetailDTO?> UpdateAsync(int id, VenueUpdateDTO dto)
    {
        var venue = await _context.Venues.FindAsync(id);
        if (venue is null)
            return null;

        venue.Name = dto.Name;
        venue.City = dto.City;
        venue.Capacity = dto.Capacity;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var venue = await _context.Venues.FindAsync(id);
        if (venue is null)
            return false;

        var isUsed = await _context.Events.AnyAsync(e => e.VenueId == id);
        if (isUsed)
            throw new InvalidOperationException("This venue is used by at least one event and cannot be deleted.");

        _context.Venues.Remove(venue);
        await _context.SaveChangesAsync();
        return true;
    }
}