﻿using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nellebot.Attributes;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[Group("prep")]
public class PrepModule : BaseCommandModule
{
    [Command("me")]
    public Task GetRandomPrep(CommandContext ctx)
    {
        var prep = GetRandomPrep();
        var scale = GetRandomScale();

        var message = $"You have rolled the preposition `{prep}`";
        message += $"\r\nOn a scale of 1 to {scale}, how satisfied are you with your preposition?";

        return ctx.RespondAsync(message);
    }

    [GroupCommand]
    public Task GetRandomPrep(CommandContext ctx, DiscordUser user)
    {
        var prep = GetRandomPrep();
        var scale = GetRandomScale();

        var mention = user.Mention;

        var message = $"Here's a preposition for you {mention}: `{prep}`";
        message += $"\r\nOn a scale of 1 to {scale}, how satisfied are you with your preposition?";

        return ctx.RespondAsync(message);
    }

    private string GetRandomPrep()
    {
        var preps = new string[] { "På", "For", "Av", "Til", "Om", "I", "Mot" };

        var prepIdx = new Random().Next(0, preps.Length);

        return preps[prepIdx];
    }

    private string GetRandomScale()
    {
        var scaleLimit = new string[] { "Helvete", "Kva faen", "Kjempebra", "Uff a meg" };

        var scaleLimitIdx = new Random().Next(0, scaleLimit.Length);

        return scaleLimit[scaleLimitIdx];
    }
}
