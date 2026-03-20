using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Handles relaxed conversion of integers from JSON
/// </summary>
internal sealed class RelaxedInt32Converter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.TryGetInt32(out var number) ? number : 0;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0;
            }

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return 0;
        }

        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
