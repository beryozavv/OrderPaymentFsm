using Refit;
using TinkoffFsmPayService.Tinkoff;

namespace TinkoffFsmPayService
{
    internal interface ITinkoffApiClient
    {
        [Post("/Init")]
        Task<PaymentResponse> InitPayment([Body] CreatePaymentRequest body, CancellationToken cancellationToken = default);

        [Post("/Cancel")]
        Task<CancelPaymentResponse> CancelPayment([Body] CancelPaymentRequest body, CancellationToken cancellationToken = default);

        [Post("/GetState")]
        Task<CancelPaymentResponse> GetState([Body] GetStatePaymentRequest body, CancellationToken cancellationToken = default);
    }
}