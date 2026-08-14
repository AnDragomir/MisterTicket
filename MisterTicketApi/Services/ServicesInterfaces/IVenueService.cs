using MisterTicketApi.DTOs;

namespace MisterTicketApi.Services;

public interface IVenueService
{
    Task<IEnumerable<VenueDTO>> GetAllAsync();

    /// <returns>The venue, or null if no venue has this id.</returns>
    Task<VenueDetailDTO?> GetByIdAsync(int id);

    Task<VenueDetailDTO> CreateAsync(VenueCreateDTO dto);

    /// <returns>The updated venue, or null if no venue has this id.</returns>
    Task<VenueDetailDTO?> UpdateAsync(int id, VenueUpdateDTO dto);

    /// <returns>False if no venue has this id.</returns>
    /// <exception cref="InvalidOperationException">The venue is still used by an event.</exception>
    Task<bool> DeleteAsync(int id);
}