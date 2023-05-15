using System.Text.Json.Serialization;

namespace TinkoffFsmPayService.Tinkoff
{
    public class PaymentResponse : BasePaymentResponse
    {
        public long Amount { get; set; }

        [JsonPropertyName("PaymentURL")]
        public Uri PaymentUrl { get; set; } = null!;
    }
}