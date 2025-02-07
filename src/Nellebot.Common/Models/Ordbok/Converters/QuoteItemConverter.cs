using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Common.Models.Ordbok.Converters;

public class QuoteItemConverter : JsonConverter<QuoteItem>
{
    public override QuoteItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string typeDiscriminator = TypeElementConverterHelper.GetTypeDiscriminator(ref reader);

        QuoteItem? result;

        switch (typeDiscriminator.ToLower())
        {
            case "relation":
                result = JsonSerializer.Deserialize<QuoteIdItem>(ref reader, options);
                break;
            case "usage":
                result = JsonSerializer.Deserialize<QuoteTextItem>(ref reader, options);
                break;
            default:
                // Read and throw away object so that the reader reaches EndObject token
                _ = JsonSerializer.Deserialize<object>(ref reader, options);
                result = null;
                break;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, QuoteItem value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case QuoteIdItem idElement:
                JsonSerializer.Serialize(writer, idElement);
                break;
            case QuoteTextItem textElement:
                JsonSerializer.Serialize(writer, textElement);
                break;
            default:
                throw new JsonException($"Unknown subclass of {value.GetType().FullName}");
        }
    }
}
