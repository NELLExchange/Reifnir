using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Common.Models.Ordbok.Converters;

public class TupleishConverter : JsonConverter<Tupleish>
{
    public override Tupleish? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = JsonSerializer.Deserialize<JsonArray>(ref reader, options);

        if (result is null) return null;

        var item1 = (result[0] ?? string.Empty).ToString();
        string[] item2 = (result[1] as JsonArray ?? [])
            .Select(x => (x ?? string.Empty)
                .ToString())
            .ToArray();

        return new Tupleish { Item1 = item1, Item2 = item2 };
    }

    public override void Write(Utf8JsonWriter writer, Tupleish value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
