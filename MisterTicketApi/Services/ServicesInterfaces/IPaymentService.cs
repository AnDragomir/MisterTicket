using MisterTicketApi.DTOs;

namespace MisterTicketApi.Services.ServicesInterfaces;

public interface IPaymentService
{
    /// <summary>
    /// Simulates a payment: the reservation becomes Paid and its seats stop
    /// being held. Always succeeds when the reservation is still valid.
    /// </summary>
    /// <returns>Null if the reservation does not exist or belongs to someone else.</returns>
    /// <exception cref="InvalidOperationException">
    /// The reservation is already paid, was cancelled, or its hold ran out.
    /// </exception>
    Task<ReservationDTO?> PayAsync(int reservationId, PaymentCreateDTO dto, int userId);

    /// <returns>Null if the reservation does not exist or belongs to someone else.</returns>
    Task<PaymentDTO?> GetForReservationAsync(int reservationId, int userId);
}