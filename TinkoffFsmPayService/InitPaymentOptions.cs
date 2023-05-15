namespace TinkoffFsmPayService
{
    internal record InitPaymentOptions
    {
        public string NotificationUrl { get; set; } = null!;
        public string SuccessUrl { get; set; } = null!;
        public string FailUrl { get; set; } = null!;
    }
}