namespace MisterTicketApi.Entities;

public enum Role
{
    Client = 0,
    Organizer = 1,
    Admin = 2
}

public enum SeatStatus
{
    Free = 0,
    Reserved = 1,   // temporarily held by a pending reservation
    Paid = 2
}

public enum ReservationStatus
{
    Pending = 0,    // seats held, waiting for payment
    Paid = 1,
    Cancelled = 2
}

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2
}