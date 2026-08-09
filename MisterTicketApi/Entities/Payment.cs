namespace MisterTicketApi.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public required Reservation Reference { get; set; }
        public int Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
