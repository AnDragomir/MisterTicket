using MisterTicketApi.DTOs;

namespace MisterTicketApi.Services.ServicesInterfaces;

public interface IReservationService
{
    /// <summary>How long seats stay held, counted from the first click.</summary>
    static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(15);

    /// <param name="userId">Null for anonymous visitors: no seat is marked as theirs.</param>
    /// <returns>Null if the event does not exist.</returns>
    Task<SeatMapDTO?> GetSeatMapAsync(int eventId, int? userId);

    /// <summary>
    /// The basket the user is currently filling for this event, if any.
    /// Lets the page restore its state after a reload.
    /// </summary>
    /// <returns>Null when the user holds nothing for this event.</returns>
    Task<ReservationDTO?> GetActiveAsync(int eventId, int userId);

    /// <summary>
    /// Claims one seat. Creates the pending reservation on the first seat and
    /// adds to it afterwards; the 15 minutes start with that first seat and are
    /// not extended by later ones.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The seat does not belong to the event, or somebody else claimed it first.
    /// </exception>
    Task<ReservationDTO> ClaimSeatAsync(int eventId, int eventSeatId, int userId);

    /// <summary>
    /// Gives one seat back. Cancels the whole reservation when it was the last
    /// seat in the basket.
    /// </summary>
    /// <returns>The reservation, or null once it holds nothing and was cancelled.</returns>
    /// <exception cref="InvalidOperationException">The seat is not held by this user.</exception>
    Task<ReservationDTO?> ReleaseSeatAsync(int eventId, int eventSeatId, int userId);

    /// <returns>Null if the reservation does not exist or belongs to someone else.</returns>
    Task<ReservationDTO?> GetByIdAsync(int reservationId, int userId);

    /// <summary>Reservation history for the signed-in user, most recent first.</summary>
    Task<IEnumerable<ReservationDTO>> GetMineAsync(int userId);

    /// <returns>False if the reservation does not exist or belongs to someone else.</returns>
    /// <exception cref="InvalidOperationException">The reservation is already paid.</exception>
    Task<bool> CancelAsync(int reservationId, int userId);

    /// <summary>
    /// Frees the seats of pending reservations whose hold has run out.
    /// Called before reading a map, before claiming, and by the background sweeper.
    /// </summary>
    /// <returns>Number of reservations that expired.</returns>
    Task<int> ReleaseExpiredAsync(int? eventId = null);
}