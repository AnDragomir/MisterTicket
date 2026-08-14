using MisterTicketApi.DTOs;

namespace MisterTicketApi.Services.ServicesInterfaces;

public interface IReservationService
{
    /// <summary>How long seats stay held before they are released.</summary>
    static readonly TimeSpan HoldDuration = TimeSpan.FromSeconds(1);

    /// <param name="userId">Null for anonymous visitors: no seat is marked as theirs.</param>
    /// <returns>Null if the event does not exist.</returns>
    Task<SeatMapDTO?> GetSeatMapAsync(int eventId, int? userId);

    /// <summary>Holds the chosen seats for 15 minutes.</summary>
    /// <exception cref="InvalidOperationException">
    /// The event does not exist, a seat does not belong to it, or a seat was
    /// taken by someone else in the meantime.
    /// </exception>
    Task<ReservationDTO> CreateAsync(ReservationCreateDTO dto, int userId);

    /// <returns>Null if the reservation does not exist or belongs to someone else.</returns>
    Task<ReservationDTO?> GetByIdAsync(int reservationId, int userId);

    /// <summary>Reservation history for the signed-in user, most recent first.</summary>
    Task<IEnumerable<ReservationDTO>> GetMineAsync(int userId);

    /// <returns>False if the reservation does not exist or belongs to someone else.</returns>
    /// <exception cref="InvalidOperationException">The reservation is already paid.</exception>
    Task<bool> CancelAsync(int reservationId, int userId);

    /// <summary>
    /// Frees the seats of pending reservations whose hold has run out.
    /// Called before reading a map or creating a reservation.
    /// </summary>
    /// <returns>Number of reservations that expired.</returns>
    Task<int> ReleaseExpiredAsync(int? eventId = null);
}