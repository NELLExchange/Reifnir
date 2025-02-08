using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record ValhallKickUserCommand(CommandContext Ctx, DiscordMember Member, string Reason)
    : BotCommandCommand(Ctx);

public class ValhallKickUserHandler : IRequestHandler<ValhallKickUserCommand>
{
    private readonly BotOptions _options;

    public ValhallKickUserHandler(IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    public async Task Handle(ValhallKickUserCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMember currentMember = ctx.Member ?? throw new Exception("Member is null");
        DiscordMember targetMember = request.Member;

        if (ctx.Member?.Id == targetMember.Id)
        {
            await TryRespondEphemeral(ctx, "Hmm");
            return;
        }

        TimeSpan guildAge = DateTimeOffset.UtcNow - targetMember.JoinedAt;

        int maxAgeHours = _options.ValhallKickMaxMemberAgeInHours;

        if (guildAge.TotalHours >= maxAgeHours)
        {
            var content =
                $"You cannot vkick this user. They have been a member of the server for more than {maxAgeHours} hours.";

            await TryRespondEphemeral(ctx, content);

            return;
        }

        var kickReason =
            $"Kicked on behalf of {currentMember.DisplayName}. Reason: {request.Reason.NullOrWhiteSpaceTo("/shrug")}";

        await targetMember.RemoveAsync(kickReason);

        await TryRespondEphemeral(ctx, "User vkicked successfully");
    }

    private static async Task TryRespondEphemeral(CommandContext ctx, string successMessage)
    {
        if (ctx is SlashCommandContext slashCtx)
            await slashCtx.RespondAsync(successMessage, true);
        else
            await ctx.RespondAsync(successMessage);
    }
}
