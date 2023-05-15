using OrderPaymentFsm.Enums;

namespace OrderPaymentFsm.Models.Payments
{
    public record FsmPayResponse
    {
        public bool? IsFullPricePromo { get; set; }
        public Uri? WebViewLink { get; set; }
        public long? PayOrderId { get; set; }
        public PaymentStatus? PayStatus { get; set; }
        public long? Amount { get; set; }
    }
}