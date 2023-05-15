namespace TinkoffFsmPayService.Tinkoff
{
    public class GetStatePaymentRequest
    {
        public long PaymentId { get; set; }
        public string TerminalKey { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}