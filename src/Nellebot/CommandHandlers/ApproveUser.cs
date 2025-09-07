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

namespace Nellebot.CommandHandlers;

public record ApproveUserCommand(CommandContext Ctx, DiscordMember Member)
    : BotCommandCommand(Ctx);

public class ApproveUserHandler : IRequestHandler<ApproveUserCommand>
{
    private readonly QuarantineService _quarantineService;
    private readonly BotOptions _options;

    public ApproveUserHandler(IOptions<BotOptions> options, QuarantineService quarantineService)
    {
        _quarantineService = quarantineService;
        _options = options.Value;
    }

    public async Task Handle(ApproveUserCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMember currentMember = ctx.Member ?? throw new Exception("Member is null");
        DiscordMember targetMember = request.Member;

        if (ctx.Member?.Id == targetMember.Id)
        {
            await TryRespondEphemeral(ctx, "Hmm");
            return;
        }

        bool userIsQuarantined = targetMember.Roles.Any(r => r.Id == _options.QuarantineRoleId);

        if (!userIsQuarantined)
        {
            await TryRespondEphemeral(ctx, "User is not quarantined");

            return;
        }

        await _quarantineService.ApproveMember(targetMember, currentMember);

        await TryRespondEphemeral(ctx, "User approved successfully");
    }

    private static async Task TryRespondEphemeral(CommandContext ctx, string successMessage)
    {
        if (ctx is SlashCommandContext slashCtx)
            await slashCtx.RespondAsync(successMessage, ephemeral: true);
        else
            await ctx.RespondAsync(successMessage);
    }
}
