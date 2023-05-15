using System.Text.Json.Serialization;
using TinkoffFsmPayService.Converter;

namespace TinkoffFsmPayService.Tinkoff
{
    public class BasePaymentResponse
    {
        public bool Success { get; set; }

        [JsonConverter(typeof(StringToLongConverter))]
        public long ErrorCode { get; set; }

        public string? Message { get; set; }

        public string? Details { get; set; }

        public string TerminalKey { get; set; } = null!;

        public PaymentStatus Status { get; set; }

        [JsonConverter(typeof(StringToLongConverter))]
        public long PaymentId { get; set; }

        [JsonConverter(typeof(StringToLongConverter))]
        public long OrderId { get; set; }
    }
}