using OrderPaymentFsm.Models;
using OrderPaymentFsm.Models.Tickets;

namespace OrderPaymentFsm.Services
{
    public interface IFsmTicketsOrderService
    {
        Task<FsmServiceResponse> FindAndSaveReservation(OrderStateParams stateParams, CancellationToken cancellationToken);
        Task<FsmServiceResponse<CreatedOrderId>> CreateTicketOrder(CreateOrderRequest request, CancellationToken cancellationToken);
        Task<FsmServiceResponse<CreatedOrderId>> FindRecentOrder(BookedTicketItem bookedTicket, CancellationToken cancellationToken);
        Task<FsmServiceResponse<FsmOrderInfo>> GetOrderInfo(long ticketOrderId, CancellationToken cancellationToken);
        Task<FsmServiceResponse> ConfirmSellOrder(Guid orderId, long ticketOrderId,  long ticketsSum, CancellationToken cancellationToken);
        Task<FsmServiceResponse> ConfirmCancelOrder(long ticketOrderId, CancellationToken cancellationToken);
    }
}