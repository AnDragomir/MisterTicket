using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Database;
using MisterTicketApi.DTOs;
using MisterTicketApi.Entities;

namespace MisterTicketApi.Services;

public class PricingZoneService : IPricingZoneService
{
    private readonly MisterTicketContext _context;

    public PricingZoneService(MisterTicketContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PricingZoneDTO>> GetAllAsync()
    {
        return await _context.PricingZones
            .AsNoTracking()
            .OrderBy(z => z.Name)
            .Select(z => new PricingZoneDTO
            {
                Id = z.Id,
                Name = z.Name,
                ColorHex = z.ColorHex,
                BasePrice = z.BasePrice
            })
            .ToListAsync();
    }

    public async Task<PricingZoneDTO?> GetByIdAsync(int id)
    {
        return await _context.PricingZones
            .AsNoTracking()
            .Where(z => z.Id == id)
            .Select(z => new PricingZoneDTO
            {
                Id = z.Id,
                Name = z.Name,
                ColorHex = z.ColorHex,
                BasePrice = z.BasePrice
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PricingZoneDTO> CreateAsync(PricingZoneCreateDTO dto)
    {
        await EnsureNameIsFreeAsync(dto.Name, excludedZoneId: null);

        var zone = new PricingZone
        {
            Name = dto.Name,
            ColorHex = dto.ColorHex,
            BasePrice = dto.BasePrice
        };

        _context.PricingZones.Add(zone);
        await _context.SaveChangesAsync();

        return ToDto(zone);
    }

    public async Task<PricingZoneDTO?> UpdateAsync(int id, PricingZoneUpdateDTO dto)
    {
        var zone = await _context.PricingZones.FindAsync(id);
        if (zone is null)
            return null;

        await EnsureNameIsFreeAsync(dto.Name, excludedZoneId: id);

        zone.Name = dto.Name;
        zone.ColorHex = dto.ColorHex;
        zone.BasePrice = dto.BasePrice;

        await _context.SaveChangesAsync();

        return ToDto(zone);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var zone = await _context.PricingZones.FindAsync(id);
        if (zone is null)
            return false;

        // A zone is shared, so it may be used by seats of several venues.
        var seatCount = await _context.Seats.CountAsync(s => s.PricingZoneId == id);
        if (seatCount > 0)
            throw new InvalidOperationException($"{seatCount} seat(s) still use this zone; reassign or delete them first.");

        _context.PricingZones.Remove(zone);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task EnsureNameIsFreeAsync(string name, int? excludedZoneId)
    {
        var taken = await _context.PricingZones
            .AnyAsync(z => z.Name == name && (excludedZoneId == null || z.Id != excludedZoneId));

        if (taken)
            throw new InvalidOperationException($"A zone named \"{name}\" already exists.");
    }

    /// <summary>In-memory mapping only: EF cannot translate a method call inside a query.</summary>
    private static PricingZoneDTO ToDto(PricingZone zone) => new()
    {
        Id = zone.Id,
        Name = zone.Name,
        ColorHex = zone.ColorHex,
        BasePrice = zone.BasePrice
    };
}