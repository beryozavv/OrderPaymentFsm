using System.Text;

namespace TinkoffFsmPayService.Extensions
{
    internal static class ByteArrayExtensions
    {
        public static string ToHex(this byte[] bytes, bool upperCase)
        {
            var result = new StringBuilder(bytes.Length * 2);

            for (var i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }
    }
}