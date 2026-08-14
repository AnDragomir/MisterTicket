using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Database;
using MisterTicketApi.DTOs;
using MisterTicketApi.Entities;

namespace MisterTicketApi.Services;

public class SeatService : ISeatService
{
    private readonly MisterTicketContext _context;

    public SeatService(MisterTicketContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SeatDTO>?> GetByVenueAsync(int venueId)
    {
        if (!await _context.Venues.AnyAsync(v => v.Id == venueId))
            return null;

        return await _context.Seats
            .AsNoTracking()
            .Where(s => s.VenueId == venueId)
            .OrderBy(s => s.RowLabel).ThenBy(s => s.Number)
            .Select(s => new SeatDTO
            {
                Id = s.Id,
                RowLabel = s.RowLabel,
                Number = s.Number,
                PricingZoneId = s.PricingZoneId,
                PricingZoneName = s.PricingZone.Name,
                PricingZoneColor = s.PricingZone.ColorHex
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<SeatDTO>?> CreateRowsAsync(int venueId, SeatBulkCreateDTO dto)
    {
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == venueId);
        if (venue is null)
            return null;

        await EnsureLayoutIsEditableAsync(venueId);

        // Zones are a shared catalogue: any venue may use any zone.
        var zone = await _context.PricingZones.FindAsync(dto.PricingZoneId);

        if (zone is null)
            throw new InvalidOperationException($"Pricing zone {dto.PricingZoneId} does not exist.");

        var rowLabels = BuildRowLabels(dto.FirstRow, dto.RowCount);

        // Existing seats of those rows, to reject duplicates before saving.
        var existing = await _context.Seats
            .Where(s => s.VenueId == venueId && rowLabels.Contains(s.RowLabel))
            .Select(s => new { s.RowLabel, s.Number })
            .ToListAsync();

        var taken = existing
            .Select(s => $"{s.RowLabel}{s.Number}")
            .ToHashSet();

        var newSeats = new List<Seat>();

        foreach (var row in rowLabels)
        {
            for (var i = 0; i < dto.SeatsPerRow; i++)
            {
                var number = dto.FirstSeatNumber + i;

                if (taken.Contains($"{row}{number}"))
                    throw new InvalidOperationException($"Seat {row}{number} already exists in this venue.");

                newSeats.Add(new Seat
                {
                    VenueId = venueId,
                    PricingZoneId = zone.Id,
                    RowLabel = row,
                    Number = number
                });
            }
        }

        _context.Seats.AddRange(newSeats);

        // Keep the stored capacity in sync with the real layout.
        venue.Capacity = await _context.Seats.CountAsync(s => s.VenueId == venueId) + newSeats.Count;

        await _context.SaveChangesAsync();

        return newSeats
            .Select(s => new SeatDTO
            {
                Id = s.Id,
                RowLabel = s.RowLabel,
                Number = s.Number,
                PricingZoneId = zone.Id,
                PricingZoneName = zone.Name,
                PricingZoneColor = zone.ColorHex
            })
            .ToList();
    }

    public async Task<bool> DeleteAsync(int venueId, int seatId)
    {
        var seat = await _context.Seats
            .FirstOrDefaultAsync(s => s.Id == seatId && s.VenueId == venueId);

        if (seat is null)
            return false;

        await EnsureLayoutIsEditableAsync(venueId);

        _context.Seats.Remove(seat);

        var venue = await _context.Venues.FirstAsync(v => v.Id == venueId);
        venue.Capacity = await _context.Seats.CountAsync(s => s.VenueId == venueId) - 1;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int?> DeleteAllAsync(int venueId)
    {
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == venueId);
        if (venue is null)
            return null;

        await EnsureLayoutIsEditableAsync(venueId);

        var seats = await _context.Seats.Where(s => s.VenueId == venueId).ToListAsync();
        _context.Seats.RemoveRange(seats);

        venue.Capacity = 0;

        await _context.SaveChangesAsync();
        return seats.Count;
    }

    /// <summary>
    /// Once an event exists, its EventSeats point at these seats: changing the
    /// layout would break existing (possibly paid) reservations.
    /// </summary>
    private async Task EnsureLayoutIsEditableAsync(int venueId)
    {
        var hasEvents = await _context.Events.AnyAsync(e => e.VenueId == venueId);
        if (hasEvents)
            throw new InvalidOperationException("This venue already has events; its seat layout can no longer be changed.");
    }

    /// <summary>Builds ["C", "D", "E"] from firstRow "C" and rowCount 3.</summary>
    private static List<string> BuildRowLabels(string firstRow, int rowCount)
    {
        var start = firstRow[0];

        if (start + rowCount - 1 > 'Z')
            throw new InvalidOperationException($"Rows would go past Z: start at {firstRow} with at most {'Z' - start + 1} rows.");

        return Enumerable.Range(0, rowCount)
            .Select(i => ((char)(start + i)).ToString())
            .ToList();
    }
}