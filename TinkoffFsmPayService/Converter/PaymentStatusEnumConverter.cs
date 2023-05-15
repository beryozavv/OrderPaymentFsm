using System.Text.Json;
using System.Text.Json.Serialization;
using TinkoffFsmPayService.Tinkoff;

namespace TinkoffFsmPayService.Converter
{
    internal class PaymentStatusEnumConverter : JsonConverter<PaymentStatus>
    {
        public override PaymentStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                throw new ArgumentOutOfRangeException(nameof(PaymentStatus), "Cannot unmarshal type PaymentStatus");

            var stringValue = string.Empty;
            try
            {
                stringValue = reader.GetString();

                return stringValue switch
                {
                    "NEW" => PaymentStatus.NEW,
                    "CANCELED" => PaymentStatus.CANCELED,
                    "REVERSED" => PaymentStatus.REVERSED,
                    "AUTHORIZED" => PaymentStatus.AUTHORIZED,
                    "DEADLINE_EXPIRED" => PaymentStatus.DEADLINE_EXPIRED,
                    "REJECTED" => PaymentStatus.REJECTED,
                    "PARTIAL_REVERSED" => PaymentStatus.PARTIAL_REVERSED,
                    "PARTIAL_REFUNDED" => PaymentStatus.PARTIAL_REFUNDED,
                    "REFUNDED" => PaymentStatus.REFUNDED,
                    "CONFIRMED" => PaymentStatus.CONFIRMED,
                    "FORM_SHOWED" => PaymentStatus.FORM_SHOWED,
                    _ => throw new ArgumentOutOfRangeException(nameof(stringValue), stringValue, "Cannot unmarshal type PaymentStatus")
                };
            }
            catch (InvalidOperationException)
            {
                if (reader.TryGetInt32(out var intStatus))
                {
                    return (PaymentStatus) intStatus;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(stringValue), stringValue, "Cannot unmarshal type PaymentStatus");
        }

        public override void Write(Utf8JsonWriter writer, PaymentStatus value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case PaymentStatus.NEW:
                    writer.WriteStringValue(PaymentStatus.NEW.ToString());
                    break;
                case PaymentStatus.CANCELED:
                    writer.WriteStringValue(PaymentStatus.CANCELED.ToString());
                    break;
                case PaymentStatus.AUTHORIZED:
                    writer.WriteStringValue(PaymentStatus.AUTHORIZED.ToString());
                    break;
                case PaymentStatus.REVERSED:
                    writer.WriteStringValue(PaymentStatus.REVERSED.ToString());
                    break;
                case PaymentStatus.DEADLINE_EXPIRED:
                    writer.WriteStringValue(PaymentStatus.DEADLINE_EXPIRED.ToString());
                    break;
                case PaymentStatus.REJECTED:
                    writer.WriteStringValue(PaymentStatus.REJECTED.ToString());
                    break;
                case PaymentStatus.PARTIAL_REVERSED:
                    writer.WriteStringValue(PaymentStatus.PARTIAL_REVERSED.ToString());
                    break;
                case PaymentStatus.CONFIRMED:
                    writer.WriteStringValue(PaymentStatus.CONFIRMED.ToString());
                    break;
                case PaymentStatus.PARTIAL_REFUNDED:
                    writer.WriteStringValue(PaymentStatus.PARTIAL_REFUNDED.ToString());
                    break;
                case PaymentStatus.REFUNDED:
                    writer.WriteStringValue(PaymentStatus.REFUNDED.ToString());
                    break;
                case PaymentStatus.FORM_SHOWED:
                    writer.WriteStringValue(PaymentStatus.FORM_SHOWED.ToString());
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Cannot unmarshal type PaymentStatus");
            }
        }

        public static readonly PaymentStatusEnumConverter Singleton = new();
    }
}