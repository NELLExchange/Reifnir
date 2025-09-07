using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record QuarantineUserCommand(CommandContext Ctx, DiscordMember Member, string Reason)
    : BotCommandCommand(Ctx);

public class QuarantineUserHandler : IRequestHandler<QuarantineUserCommand>
{
    private readonly QuarantineService _quarantineService;
    private readonly BotOptions _options;

    public QuarantineUserHandler(IOptions<BotOptions> options, QuarantineService quarantineService)
    {
        _quarantineService = quarantineService;
        _options = options.Value;
    }

    public async Task Handle(QuarantineUserCommand request, CancellationToken cancellationToken)
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
                $"You cannot quarantine this user. They have been a member of the server for more than {maxAgeHours} hours.";

            await TryRespondEphemeral(ctx, content);

            return;
        }

        bool userAlreadyQuarantined = targetMember.Roles.Any(r => r.Id == _options.QuarantineRoleId);

        if (userAlreadyQuarantined)
        {
            await TryRespondEphemeral(ctx, "User is already quarantined");
        }

        string quarantineReason = request.Reason.NullOrWhiteSpaceTo("/shrug");

        await _quarantineService.QuarantineMember(targetMember, currentMember, quarantineReason);

        await TryRespondEphemeral(ctx, "User quarantined successfully");
    }

    private static async Task TryRespondEphemeral(CommandContext ctx, string successMessage)
    {
        if (ctx is SlashCommandContext slashCtx)
            await slashCtx.RespondAsync(successMessage, ephemeral: true);
        else
            await ctx.RespondAsync(successMessage);
    }
}
