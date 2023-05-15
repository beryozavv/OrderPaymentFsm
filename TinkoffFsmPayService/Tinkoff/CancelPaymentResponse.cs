namespace TinkoffFsmPayService.Tinkoff
{
    public class CancelPaymentResponse : BasePaymentResponse
    {
        public long OriginalAmount { get; set; }
        public long NewAmount { get; set; }
    }
}