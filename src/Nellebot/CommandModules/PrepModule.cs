using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Nellebot.Attributes;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[Command("Prep")]
[Description("Get a random preposition")]
[AllowedProcessors(typeof(TextCommandProcessor))]
public class PrepModule
{
    [Command("me")]
    [Description("Help yourself to a random preposition")]
    public ValueTask GetRandomPrep(CommandContext ctx)
    {
        string prep = GetRandomPrep();
        string scale = GetRandomScale();

        var message = $"You have rolled the preposition `{prep}`";
        message += $"\r\nOn a scale of 1 to {scale}, how satisfied are you with your preposition?";

        return ctx.RespondAsync(message);
    }

    [DefaultGroupCommand]
    [Command("user")]
    public ValueTask GetRandomPrep(CommandContext ctx, DiscordUser user)
    {
        string prep = GetRandomPrep();
        string scale = GetRandomScale();

        string mention = user.Mention;

        var message = $"Here's a preposition for you {mention}: `{prep}`";
        message += $"\r\nOn a scale of 1 to {scale}, how satisfied are you with your preposition?";

        return ctx.RespondAsync(message);
    }

    private static string GetRandomPrep()
    {
        var preps = new[] { "På", "For", "Av", "Til", "Om", "I", "Mot" };

        int prepIdx = new Random().Next(0, preps.Length);

        return preps[prepIdx];
    }

    private static string GetRandomScale()
    {
        var scaleLimit = new[] { "Helvete", "Kva faen", "Kjempebra", "Uff a meg" };

        int scaleLimitIdx = new Random().Next(0, scaleLimit.Length);

        return scaleLimit[scaleLimitIdx];
    }
}
