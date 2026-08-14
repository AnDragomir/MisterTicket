using MisterTicketApi.DTOs;

namespace MisterTicketApi.Services;

public interface IEventService
{
    /// <param name="venueId">Optional filter.</param>
    Task<IEnumerable<EventDTO>> GetAllAsync(int? venueId = null);

    /// <returns>The event, or null if no event has this id.</returns>
    Task<EventDetailDTO?> GetByIdAsync(int id);

    /// <param name="organizerId">Id of the authenticated organizer/admin.</param>
    /// <exception cref="InvalidOperationException">The venue does not exist or has no seats.</exception>
    Task<EventDetailDTO> CreateAsync(EventCreateDTO dto, int organizerId);

    /// <returns>The updated event, or null if no event has this id.</returns>
    Task<EventDetailDTO?> UpdateAsync(int id, EventUpdateDTO dto);

    /// <returns>False if no event has this id.</returns>
    /// <exception cref="InvalidOperationException">The event already has reservations.</exception>
    Task<bool> DeleteAsync(int id);
}