using System.Text.Json;
using System.Text.Json.Serialization;

namespace TinkoffFsmPayService.Converter
{
    public class StringToLongConverter : JsonConverter<long>
    {
        private readonly StringToNullableLongConverter _converter;

        public StringToLongConverter()
        {
            _converter = new StringToNullableLongConverter();
        }

        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var longValue = _converter.Read(ref reader, typeToConvert, options);

            return longValue ?? default;
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            _converter.Write(writer, value, options);
        }
    }
}