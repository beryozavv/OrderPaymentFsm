using OrderPaymentFsm.Models;
using OrderPaymentFsm.Models.Payments;

namespace OrderPaymentFsm.Services
{
    public interface IFsmPayService
    {
        Task<FsmServiceResponse<FsmPayResponse>> InitPay(PayRequest request, CancellationToken cancellationToken);
        Task<FsmServiceResponse> CancelNewPay(long? payOrderId, CancellationToken cancellationToken);
    }
}