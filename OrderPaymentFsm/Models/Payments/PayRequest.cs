using OrderPaymentFsm.Models.Tickets;

namespace OrderPaymentFsm.Models.Payments
{
    public record PayRequest
    {
        public long TicketOrderId { get; set; }

        public string? Description { get; set; }
        public long Amount { get; set; }

        public BookedTicketItem[]? Tickets { get; set; } // todo убираем
    }
}