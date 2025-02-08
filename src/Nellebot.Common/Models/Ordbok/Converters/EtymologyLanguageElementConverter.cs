using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Common.Models.Ordbok.Converters;

public class EtymologyLanguageElementConverter : JsonConverter<EtymologyLanguageElement>
{
    public override EtymologyLanguageElement? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        string typeDiscriminator = TypeElementConverterHelper.GetTypeDiscriminator(ref reader);

        EtymologyLanguageElement? result;

        switch (typeDiscriminator.ToLower())
        {
            case "language":
            case "grammar":
            case "relation":
                result = JsonSerializer.Deserialize<EtymologyLanguageIdElement>(ref reader, options);
                break;
            case "usage":
                result = JsonSerializer.Deserialize<EtymologyLanguageTextElement>(ref reader, options);
                break;
            default:
                // Read and throw away object so that the reader reaches EndObject token
                _ = JsonSerializer.Deserialize<object>(ref reader, options);
                result = null;
                break;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, EtymologyLanguageElement value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case EtymologyLanguageIdElement language:
                JsonSerializer.Serialize(writer, language);
                break;
            case EtymologyLanguageTextElement usage:
                JsonSerializer.Serialize(writer, usage);
                break;
            default:
                throw new JsonException($"Unknown subclass of {value.GetType().FullName}");
        }
    }
}
