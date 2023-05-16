﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Api;

public record OrdbokSearchResponse
{
    [JsonPropertyName("meta")]
    public Dictionary<string, ArticleMeta> Meta { get; set; } = new Dictionary<string, ArticleMeta>();

    [JsonPropertyName("articles")]
    public Dictionary<string, List<int>> Articles { get; set; } = new Dictionary<string, List<int>>();
}

public record ArticleMeta
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
}
