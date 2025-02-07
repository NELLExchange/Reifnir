using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.CommandModules.Roles;

public class TrustedMemberModule
{
    private readonly CommandParallelQueueChannel _commandQueue;
    private readonly BotOptions _options;

    public TrustedMemberModule(CommandParallelQueueChannel commandQueue, IOptions<BotOptions> options)
    {
        _commandQueue = commandQueue;
        _options = options.Value;
    }

    // TODO register as menu command
    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("vkick")]
    public async Task ValhallKick(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
    {
        await _commandQueue.Writer.WriteAsync(new ValhallKickUserCommand(ctx, member, reason));
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
            .Where(
                c => c.Type == DiscordChannelType.Category
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
