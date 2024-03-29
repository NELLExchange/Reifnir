﻿using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nellebot.Attributes;
using Nellebot.Utils;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[Group("utils")]
public class UtilsModule : BaseCommandModule
{
    private readonly DiscordResolver _discordResolver;

    public UtilsModule(DiscordResolver discordResolver)
    {
        _discordResolver = discordResolver;
    }

    [Command("role-id")]
    public async Task GetRoleId(CommandContext ctx, string roleName)
    {
        TryResolveResult<DiscordRole> resolveResult = _discordResolver.TryResolveRoleByName(ctx.Guild, roleName);

        if (!resolveResult.Resolved)
        {
            await ctx.RespondAsync(resolveResult.ErrorMessage);
            return;
        }

        await ctx.RespondAsync($"Role {roleName} has id {resolveResult.Value.Id}");
    }

    [Command("emoji-code")]
    public async Task GetEmojiCode(CommandContext ctx, DiscordEmoji emoji)
    {
        bool isUnicodeEmoji = emoji.Id == 0;

        if (!isUnicodeEmoji)
        {
            await ctx.RespondAsync("Not a unicode emoji");
            return;
        }

        var unicodeEncoding = new UnicodeEncoding(true, false);

        byte[] bytes = unicodeEncoding.GetBytes(emoji.Name);

        var sb = new StringBuilder();
        for (var i = 0; i < bytes.Length; i++) sb.AppendFormat("{0:X2}", bytes[i]);

        var bytesAsString = sb.ToString();

        var formattedSb = new StringBuilder();

        for (var i = 0; i < sb.Length; i += 4) formattedSb.Append($"\\u{bytesAsString.Substring(i, 4)}");

        var result = formattedSb.ToString();

        await ctx.RespondAsync($"`{result}`");
    }
}
