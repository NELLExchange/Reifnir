using System;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.CommandHandlers;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.Services.Loggers;

public class DiscordErrorLogger : IDiscordErrorLogger
{
    private readonly DiscordLogChannel _channel;
    private readonly BotOptions _options;

    public DiscordErrorLogger(IOptions<BotOptions> options, DiscordLogChannel channel)
    {
        _channel = channel;
        _options = options.Value;
    }

    public void LogCommandError(CommandContext ctx, string errorMessage)
    {
        var user = $"{ctx.User.Username}#{ctx.User.Discriminator}";
        string channelName = ctx.Channel.Name;
        string guildName = ctx.Guild?.Name ?? "Unknown guild";

        string command = ctx switch
        {
            TextCommandContext textCtx => EscapeTicks(textCtx.Message.Content),
            SlashCommandContext slashCtx => slashCtx.Command.FullName,
            _ => "Unknown command",
        };

        var contextMessage = $"`{command}` by `{user}` in `{channelName}`(`{guildName}`)";
        var escapedErrorMessage = $"`{EscapeTicks(errorMessage)}`";

        var fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMessage}";

        LogError("Failed command", fullErrorMessage);
    }

    public void LogEventError(EventContext ctx, string errorMessage)
    {
        string user = ctx.User != null ? $"{ctx.User.Username}#{ctx.User.Discriminator}" : "Unknown user";
        string channelName = ctx.Channel?.Name ?? "Unknown channel";
        string eventName = ctx.EventName;
        string message = ctx.Message != null ? EscapeTicks(ctx.Message.Content) : string.Empty;

        var contextMessage = $"`{eventName}` by `{user}` in `{channelName}`";

        if (!string.IsNullOrWhiteSpace(message)) contextMessage += $"{Environment.NewLine}Message: `{message}`";

        var escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

        var fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

        LogError("Failed event", fullErrorMessage);
    }

    public void LogError(Exception ex, string message)
    {
        LogError(message, ex.ToString());
    }

    public void LogError(string errorMessage)
    {
        LogError("Error", errorMessage);
    }

    public void LogError(string error, string errorMessage)
    {
        SendErrorLogChannelEmbed(error, errorMessage, DiscordConstants.ErrorEmbedColor);
    }

    public void LogWarning(string warning, string warningMessage)
    {
        SendErrorLogChannelEmbed(warning, warningMessage, DiscordConstants.WarningEmbedColor);
    }

    private static string EscapeTicks(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : value.Replace('`', '\'');
    }

    private void SendErrorLogChannelEmbed(string title, string message, int color)
    {
        ulong guildId = _options.GuildId;
        ulong errorLogChannelId = _options.ErrorLogChannelId;

        DiscordEmbed messageEmbed = EmbedBuilderHelper.BuildSimpleEmbed(title, message, color);

        var discordLogItem = new DiscordLogItem<DiscordEmbed>(messageEmbed, guildId, errorLogChannelId);

        _ = _channel.Writer.TryWrite(discordLogItem);
    }
}
