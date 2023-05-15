using OrderPaymentFsm.Enums;
using OrderPaymentFsm.Models.Payments;
using OrderPaymentFsm.Models.Tickets;

namespace OrderPaymentFsm.Models
{
    public record OrderStateParams
    {
        public OrderStateParams()
        {
        }

        public OrderStateParams(Guid orderId, Guid userId, OrderState state, CreateOrderRequest request)
        {
            OrderId = orderId;
            State = state;
            UserId = userId;
            Request = request;
        }

        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public CreateOrderRequest Request { get; set; } = null!;
        public long? TicketOrderId { get; set; }
        public OrderState State { get; set; }

        public BookedTicketItem? BookedTicket { get; set; }

        public FsmPayResponse? PayResponse { get; set; }

        public int TransientErrCount { get; set; }

        public string? ErrorMessage { get; set; }
    }
}