using System.Text.Json.Serialization;
using Nellebot.Common.Models.Ordbok.Converters;

namespace Nellebot.Common.Models.Ordbok.Api;

[JsonConverter(typeof(TupleishConverter))]
public class Tupleish
{
    public required string Item1 { get; set; }

    public required string[] Item2 { get; set; }
}
