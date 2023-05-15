using OrderPaymentFsm.Enums;

namespace OrderPaymentFsm.Models.Tickets
{
    public record FsmOrderInfo
    {
        public FsmOrderInfo()
        {
        }

        public FsmOrderInfo(long orderId, long ticketsSum, TicketOrderStatusEnum ticketOrderStatus)
        {
            OrderId = orderId;
            TicketsSum = ticketsSum;
            TicketOrderStatus = ticketOrderStatus;
        }

        public long OrderId { get; set; }

        public long TicketsSum { get; set; }

        public TicketOrderStatusEnum TicketOrderStatus { get; set; }

        public string? Description { get; set; }
        public BookedTicketItem[]? Tickets { get; set; }
    };
}