using System.ComponentModel.DataAnnotations;

namespace TinkoffFsmPayService
{
    internal sealed class TinkoffApiOptions
    {
        [Required]
        public string ApiUrl { get; init; } = null!;

        [Required]
        public string TerminalKey { get; init; } = null!;

        [Required]
        public string Password { get; init; } = null!;

        [Required]
        public int[] PermanentErrors { get; init; } = null!;

        [Required]
        public InitPaymentOptions InitPaymentOptions { get; set; } = null!;
    }
}