using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TinkoffFsmPayService.Converter;

namespace TinkoffFsmPayService.Tinkoff
{
    public class CreatePaymentRequest
    {
        public string TerminalKey { get; set; }

        [JsonConverter(typeof(StringToLongConverter))]
        public long Amount { get; set; }

        [MaxLength(36)]
        public string OrderId { get; set; }

        public string Description { get; set; }

        public string Token { get; set; }

        [JsonPropertyName("NotificationURL")]
        public string NotificationUrl { get; set; }

        [JsonPropertyName("SuccessURL")]
        public string SuccessUrl { get; set; }

        [JsonPropertyName("FailURL")]
        public string FailUrl { get; set; }

        [JsonPropertyName("DATA")]
        public Data? Data { get; set; }

        public Receipt Receipt { get; set; }
    }

    public class Data
    {
        public string Phone { get; set; }

        public string Email { get; set; }
    }

    public class Receipt
    {
        public string Email { get; set; }

        public string Phone { get; set; }

        public string Taxation { get; set; }

        public string Customer { get; set; }

        [JsonConverter(typeof(StringToLongConverter))]
        public long CustomerInn { get; set; }

        public Payments Payments { get; set; }

        public AgentData AgentData { get; set; }

        public ReceiptSupplierInfo SupplierInfo { get; set; }

        public Item[] Items { get; set; }
    }

    public class AgentData
    {
        public string AgentSign { get; set; }

        public string OperationName { get; set; }

        public string[] Phones { get; set; }

        public string[] ReceiverPhones { get; set; }

        public string[] TransferPhones { get; set; }

        public string OperatorName { get; set; }

        public string OperatorAddress { get; set; }

        public string OperatorInn { get; set; }
    }

    public class Item
    {
        public AgentData AgentData { get; set; }

        public ItemSupplierInfo SupplierInfo { get; set; }

        public string Name { get; set; }

        public long Price { get; set; }

        public double Quantity { get; set; }

        public long Amount { get; set; }

        public string Tax { get; set; }

        public string Ean13 { get; set; }

        [JsonConverter(typeof(StringToNullableLongConverter))]
        public long? ShopCode { get; set; }

        public string MeasurementUnit { get; set; }
    }

    public class ItemSupplierInfo
    {
        public string[] Phones { get; set; }

        public string Name { get; set; }

        public string Inn { get; set; }
    }

    public class Payments
    {
        public long Electronic { get; set; }

        public long Cash { get; set; }

        public long AdvancePayment { get; set; }

        public long Credit { get; set; }

        public long Provision { get; set; }
    }

    public class ReceiptSupplierInfo
    {
        public string[] Phones { get; set; }
    }
}