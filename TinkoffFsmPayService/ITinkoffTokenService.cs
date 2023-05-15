using TinkoffFsmPayService.Tinkoff;

namespace TinkoffFsmPayService
{
    internal interface ITinkoffTokenService
    {
        string CalculateToken(CreatePaymentRequest request);
        string CalculateToken(GetStatePaymentRequest request);
        string CalculateToken(CancelPaymentRequest request);
        string CalculateToken(TinkoffCallbackRequest request);
    }
}