using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TinkoffFsmPayService.Tinkoff
{
    public record TinkoffCallbackRequest : IValidatableObject
    {
        [JsonPropertyName("Amount")]
        public long Amount { get; set; }

        [JsonPropertyName("CardId")]
        public long CardId { get; set; }

        [JsonPropertyName("ErrorCode")]
        public int? ErrorCode { get; set; }

        [JsonPropertyName("ExpDate")]
        public string ExpDate { get; set; } = null!;

        [JsonPropertyName("OrderId")]
        public string OrderId { get; set; } = null!;

        [JsonPropertyName("Pan")]
        public string Pan { get; set; } = null!;

        [JsonPropertyName("PaymentId")]
        public long PaymentId { get; set; }

        [JsonPropertyName("RebillId")]
        public long? RebillId { get; set; }

        [JsonPropertyName("Status")]
        public PaymentStatus Status { get; set; }

        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("TerminalKey")]
        public string TerminalKey { get; set; } = null!;

        [JsonPropertyName("Token")]
        public string Token { get; set; } = null!;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var tinkoffTokenService = validationContext.GetRequiredService<ITinkoffTokenService>();
            var calculatedToken = tinkoffTokenService.CalculateToken(this);
            if (calculatedToken != Token)
            {
                var logger = validationContext.GetRequiredService<ILogger<TinkoffCallbackRequest>>();
                logger.LogError($"InvalidTinkoffToken: {this}");
                yield return new ValidationResult("InvalidTinkoffToken", new[] {nameof(Token)}); // todo const InvalidTinkoffToken
            }
        }
    }
}