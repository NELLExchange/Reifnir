using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

public class TrustedMemberModule
{
    private readonly CommandParallelQueueChannel _commandQueue;
    private readonly BotOptions _options;

    public TrustedMemberModule(CommandParallelQueueChannel commandQueue, IOptions<BotOptions> options)
    {
        _commandQueue = commandQueue;
        _options = options.Value;
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("vban")]
    [Description("Valhall ban user")]
    public async Task ValhallBan(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
    {
        await _commandQueue.Writer.WriteAsync(new ValhallBanUserCommand(ctx, member, reason));
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("VBan user")]
    [Description("Valhall ban user")]
    [SlashCommandTypes(DiscordApplicationCommandType.UserContextMenu)]
    public async Task ValhallBanMenu(SlashCommandContext ctx, DiscordMember member)
    {
        await _commandQueue.Writer.WriteAsync(new ValhallBanUserCommand(ctx, member, Reason: null));
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("quarantine")]
    [Description("Quarantine user")]
    public async Task QuarantineUser(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
    {
        await _commandQueue.Writer.WriteAsync(new QuarantineUserCommand(ctx, member, reason));
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("Quarantine user")]
    [Description("Quarantine user")]
    [SlashCommandTypes(DiscordApplicationCommandType.UserContextMenu)]
    public async Task QuarantineUserMenu(SlashCommandContext ctx, DiscordMember member)
    {
        await _commandQueue.Writer.WriteAsync(new QuarantineUserCommand(ctx, member, Reason: null));
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("approve")]
    [Description("Approve user")]
    public async Task ApproveUser(CommandContext ctx, DiscordMember member)
    {
        await _commandQueue.Writer.WriteAsync(new ApproveUserCommand(ctx, member));
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("Approve user")]
    [Description("Approve user")]
    [SlashCommandTypes(DiscordApplicationCommandType.UserContextMenu)]
    public async Task ApproveUserMenu(SlashCommandContext ctx, DiscordMember member)
    {
        await _commandQueue.Writer.WriteAsync(new ApproveUserCommand(ctx, member));
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("list-award-channels")]
    public async Task ListCookieChannels(CommandContext ctx)
    {
        ctx.Guild.ThrowIfNull();

        ulong[] groupIds = _options.AwardVoteGroupIds;

        var sb = new StringBuilder();

        IReadOnlyList<DiscordChannel> guildChannels = await ctx.Guild!.GetChannelsAsync();

        IEnumerable<DiscordChannel> categoryChannels = guildChannels
            .Where(c => c.Type == DiscordChannelType.Category
                        && groupIds.Contains(c.Id));

        foreach (DiscordChannel category in categoryChannels)
        {
            sb.AppendLine($"**{category.Name}**");

            IEnumerable<DiscordChannel> textChannelsForCategory =
                guildChannels.Where(c => c.Type == DiscordChannelType.Text && c.ParentId == category.Id);

            foreach (DiscordChannel channel in textChannelsForCategory)
            {
                sb.AppendLine($"#{channel.Name}");
            }

            sb.AppendLine();
        }

        await ctx.RespondAsync(sb.ToString());
    }
}
