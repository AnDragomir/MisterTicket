using MisterTicketApi.DTOs;

namespace MisterTicketApi.Services;

/// <summary>
/// Zones form a catalogue shared by every venue: a seat in any venue can point
/// at any zone.
/// </summary>
public interface IPricingZoneService
{
    Task<IEnumerable<PricingZoneDTO>> GetAllAsync();

    /// <returns>Null if no zone has this id.</returns>
    Task<PricingZoneDTO?> GetByIdAsync(int id);

    /// <exception cref="InvalidOperationException">A zone with this name already exists.</exception>
    Task<PricingZoneDTO> CreateAsync(PricingZoneCreateDTO dto);

    /// <returns>Null if no zone has this id.</returns>
    /// <exception cref="InvalidOperationException">Another zone already has that name.</exception>
    Task<PricingZoneDTO?> UpdateAsync(int id, PricingZoneUpdateDTO dto);

    /// <returns>False if no zone has this id.</returns>
    /// <exception cref="InvalidOperationException">Seats (in any venue) still use this zone.</exception>
    Task<bool> DeleteAsync(int id);
}