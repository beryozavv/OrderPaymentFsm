using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TinkoffFsmPayService.Extensions;
using TinkoffFsmPayService.Tinkoff;

namespace TinkoffFsmPayService
{
    internal class TinkoffTokenService : ITinkoffTokenService
    {
        private readonly TinkoffApiOptions _options;

        public TinkoffTokenService(IOptions<TinkoffApiOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options.Value;
        }

        public string CalculateToken(CreatePaymentRequest request)
        {
            request.TerminalKey = _options.TerminalKey;
            var password = _options.Password;

            var requestString = $"{request.Amount}{request.Description}{request.FailUrl}{request.NotificationUrl}{request.OrderId}{password}" +
                                $"{request.SuccessUrl}{request.TerminalKey}";

            return GetHashString(requestString);
        }

        public string CalculateToken(GetStatePaymentRequest request)
        {
            request.TerminalKey = _options.TerminalKey;
            var password = _options.Password;

            var requestString = $"{password}{request.PaymentId}{request.TerminalKey}";

            return GetHashString(requestString);
        }

        public string CalculateToken(CancelPaymentRequest request)
        {
            request.TerminalKey = _options.TerminalKey;
            var password = _options.Password;

            var requestString = $"{password}{request.PaymentId}{request.TerminalKey}";

            return GetHashString(requestString);
        }

        public string CalculateToken(TinkoffCallbackRequest request)
        {
            request.TerminalKey = _options.TerminalKey;
            var password = _options.Password;

            var requestString =
                $"{request.Amount}{request.CardId}{request.ErrorCode}{request.ExpDate}{request.OrderId}{request.Pan}{password}{request.PaymentId}{request.RebillId}{request.Status}{request.Success.ToString().ToLowerInvariant()}{request.TerminalKey}";

            return GetHashString(requestString);

            // new SortedDictionary<string, string> // todo?
            // {
            //     {"Amount",request.Amount.ToString()},
            //     {"CardId", request.CardId.ToString()}
            // }
        }

        private static string GetHashString(string requestString)
        {
            using var sha256 = SHA256.Create();
            var requestBytes = Encoding.UTF8.GetBytes(requestString);
            var hash = sha256.ComputeHash(requestBytes);
            var hashStr = hash.ToHex(false);

            if (string.IsNullOrEmpty(hashStr))
            {
                throw new InvalidOperationException("Ошибка вычисления хеша");
            }

            return hashStr;
        }
    }
}