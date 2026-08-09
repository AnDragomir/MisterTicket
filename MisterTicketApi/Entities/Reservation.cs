namespace MisterTicketApi.Entities
{
    public class Reservation
    {
        public int Id { get; set; }
        public required User User { get; set; }
        public List<Seat> SelectedSeats { get; set; } = new List<Seat>();
        public ReservationStatus Status { get; set; }
        public DateTime Date {  get; set; }
    }

    public enum ReservationStatus
    {
        InProgress,
        Paid,
        Cancelled
    }
   
}


