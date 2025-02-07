using System;

namespace Nellebot.Common.Models.Ordbok.Store;

public record OrdbokArticleStore
{
    public string Dictionary { get; init; } = null!;

    public string WordClass { get; init; } = null!;

    public int ArticleCount { get; set; }

    public int[] ArticleList { get; set; } = Array.Empty<int>();
}
