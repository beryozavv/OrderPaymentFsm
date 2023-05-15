namespace TinkoffFsmPayService.Exceptions
{
    internal sealed class TinkoffApiException : Exception
    {
        public int? ErrorCode { get; }

        public TinkoffApiException(string? errorMessage, int? errorCode) : this(errorMessage)
        {
            ErrorCode = errorCode;

            Data[nameof(errorCode)] = errorCode;
        }

        public TinkoffApiException(string? errorMessage, Exception? innerException = null) : base(errorMessage,
            innerException?.InnerException)
        {
        }
    }
}