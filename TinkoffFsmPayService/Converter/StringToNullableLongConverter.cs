using System.Text.Json;
using System.Text.Json.Serialization;

namespace TinkoffFsmPayService.Converter
{
    public class StringToNullableLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null!;

            try
            {
                var value = reader.GetString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return default(long);
                }

                if (long.TryParse(value, out var l))
                {
                    return l;
                }

                return null;
            }
            catch (InvalidOperationException)
            {
                if (reader.TryGetInt64(out var longValue))
                {
                    return longValue;
                }
                else
                {
                    throw;
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.ToString());
        }
    }
}