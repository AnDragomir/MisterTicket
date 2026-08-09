namespace MisterTicketApi.Entities
{
    public class Seat
    {
        public int Id { get; set; }
        public SeatStatus Status { get; set; }
        public int Price { get; set; }
    }

    public enum SeatStatus
    {
        Free,
        Reserved,
        Paid
    }
}
