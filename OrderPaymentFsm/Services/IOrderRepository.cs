using OrderPaymentFsm.Models;

namespace OrderPaymentFsm.Services
{
    public interface IOrderRepository
    {
        Task<OrderStateParams?> GetOrderStateParams(Guid id, CancellationToken cancellationToken);

        Task<OrderStateParams?> GetOrderStateParams(long ticketOrderId, CancellationToken cancellationToken);

        Task<Guid> GetOrderId(long ticketOrderId, CancellationToken cancellationToken);

        Task SaveOrderStateParams(OrderStateParams stateParams, CancellationToken cancellationToken);
    }
}