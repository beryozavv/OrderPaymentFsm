using System.Text.Json.Serialization;
using TinkoffFsmPayService.Converter;

// ReSharper disable InconsistentNaming

namespace TinkoffFsmPayService.Tinkoff
{
    [JsonConverter(typeof(PaymentStatusEnumConverter))]
    public enum PaymentStatus
    {
        NEW,
        CANCELED,
        AUTHORIZED,
        REVERSED,
        DEADLINE_EXPIRED,
        REJECTED,
        PARTIAL_REVERSED,
        CONFIRMED,
        PARTIAL_REFUNDED,
        REFUNDED,
        FORM_SHOWED

        // todo all statuses https://www.tinkoff.ru/kassa/develop/api/payments/
    }
}