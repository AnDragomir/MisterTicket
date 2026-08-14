using MisterTicketApi.DTOs;

namespace MisterTicketApi.Services;

public interface ISeatService
{
    /// <returns>Null if the venue does not exist.</returns>
    Task<IEnumerable<SeatDTO>?> GetByVenueAsync(int venueId);

    /// <summary>Generates a block of seats (rows x seats per row) in a single save.</summary>
    /// <returns>The created seats, or null if the venue does not exist.</returns>
    /// <exception cref="InvalidOperationException">
    /// The venue already has events, the zone is invalid, the rows go past Z,
    /// or one of the generated seats already exists.
    /// </exception>
    Task<IEnumerable<SeatDTO>?> CreateRowsAsync(int venueId, SeatBulkCreateDTO dto);

    /// <returns>False if the seat does not exist or does not belong to this venue.</returns>
    /// <exception cref="InvalidOperationException">The venue already has events.</exception>
    Task<bool> DeleteAsync(int venueId, int seatId);

    /// <summary>Deletes every seat of the venue (useful while designing the layout).</summary>
    /// <returns>Number of deleted seats, or null if the venue does not exist.</returns>
    /// <exception cref="InvalidOperationException">The venue already has events.</exception>
    Task<int?> DeleteAllAsync(int venueId);
}