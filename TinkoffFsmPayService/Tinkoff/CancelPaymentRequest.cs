namespace TinkoffFsmPayService.Tinkoff
{
    public class CancelPaymentRequest
    {
        public long PaymentId { get; set; }
        public string TerminalKey { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}