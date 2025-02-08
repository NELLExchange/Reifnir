using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Common.Models.Ordbok.Converters;

public class EtymologyReferenceElementConverter : JsonConverter<EtymologyReferenceElement>
{
    public override EtymologyReferenceElement? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        string typeDiscriminator = TypeElementConverterHelper.GetTypeDiscriminator(ref reader);

        EtymologyReferenceElement? result;

        switch (typeDiscriminator.ToLower())
        {
            case "relation":
            case "entity":
            case "grammar":
                result = JsonSerializer.Deserialize<EtymologyReferenceIdElement>(ref reader, options);
                break;
            case "article_ref":
                result = JsonSerializer.Deserialize<EtymologyReferenceArticleRef>(ref reader, options);
                break;
            default:
                // Read and throw away object so that the reader reaches EndObject token
                _ = JsonSerializer.Deserialize<object>(ref reader, options);
                result = null;
                break;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, EtymologyReferenceElement value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case EtymologyReferenceIdElement relation:
                JsonSerializer.Serialize(writer, relation);
                break;
            case EtymologyReferenceArticleRef articleRef:
                JsonSerializer.Serialize(writer, articleRef);
                break;
            default:
                throw new JsonException($"Unknown subclass of {value.GetType().FullName}");
        }
    }
}
