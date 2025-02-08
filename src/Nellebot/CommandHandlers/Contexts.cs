using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace Nellebot.CommandHandlers;

#pragma warning disable SA1402 // File may only contain a single type
public class BaseContext
{
    public DiscordUser User { get; init; } = null!;

    public DiscordChannel Channel { get; init; } = null!;

    public DiscordGuild? Guild { get; init; }

    public static BaseContext FromCommandContext(CommandContext ctx)
    {
        var newCtx = new BaseContext
        {
            Channel = ctx.Channel,
            Guild = ctx.Guild,
            User = ctx.User,
        };

        return newCtx;
    }
}

public class MessageContext : BaseContext
{
    public DiscordMessage Message { get; init; } = null!;
}

public class EventContext : BaseContext
{
    public string EventName { get; init; } = string.Empty;

    public DiscordMessage? Message { get; init; }
}
