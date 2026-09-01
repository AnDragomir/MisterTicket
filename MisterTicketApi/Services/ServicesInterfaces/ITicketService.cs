namespace MisterTicketApi.Services.ServicesInterfaces;

public interface ITicketService
{
    /// <summary>
    /// Builds the PDF ticket of a paid reservation: event details, seats, and a
    /// QR code carrying the payment reference.
    /// </summary>
    /// <returns>Null if the reservation does not exist or belongs to someone else.</returns>
    /// <exception cref="InvalidOperationException">The reservation is not paid.</exception>
    Task<byte[]?> BuildPdfAsync(int reservationId, int userId);
}