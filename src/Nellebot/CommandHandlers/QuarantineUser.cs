using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.AppDiscordModels;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record QuarantineUserCommand(CommandContext Ctx, DiscordMember Member, string? Reason)
    : BotCommandCommand(Ctx);

public class QuarantineUserHandler : IRequestHandler<QuarantineUserCommand>
{
    private const string ModalTextInputId = "modal-text-input";
    private readonly InteractivityExtension _interactivityExtension;
    private readonly AuthorizationService _authService;
    private readonly BotOptions _options;
    private readonly QuarantineService _quarantineService;

    public QuarantineUserHandler(
        IOptions<BotOptions> options,
        QuarantineService quarantineService,
        InteractivityExtension interactivityExtension,
        AuthorizationService authService)
    {
        _quarantineService = quarantineService;
        _interactivityExtension = interactivityExtension;
        _authService = authService;
        _options = options.Value;
    }

    public async Task Handle(QuarantineUserCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMember currentMember = ctx.Member ?? throw new Exception("Member is null");
        DiscordMember targetMember = request.Member;

        if (ctx.Member?.Id == targetMember.Id)
        {
            await ctx.TryRespondEphemeral("Hmm");
            return;
        }

        TimeSpan guildAge = DateTimeOffset.UtcNow - targetMember.JoinedAt;

        int maxAgeDays = _options.QuarantineMaxMemberAgeInDays;

        AppDiscordMember appCurrentMember = DiscordMemberMapper.Map(currentMember);

        bool canBypassAgeRestriction = _authService.IsAdminOrMod(appCurrentMember);

        if (!canBypassAgeRestriction && guildAge.TotalDays >= maxAgeDays)
        {
            var content =
                $"You cannot quarantine this user. They have been a member of the server for more than {maxAgeDays} days.";

            await ctx.TryRespondEphemeral(content);

            return;
        }

        bool userAlreadyQuarantined = targetMember.Roles.Any(r => r.Id == _options.QuarantineRoleId);

        if (userAlreadyQuarantined)
        {
            await ctx.TryRespondEphemeral("User is already quarantined");
        }

        DiscordInteraction? modalInteraction = null;
        string? quarantineReason = request.Reason;

        if (ctx is SlashCommandContext slashCtx && request.Reason == null)
        {
            ModalSubmittedEventArgs modalSubmissionResult = await ShowGetReasonModal(slashCtx);

            modalInteraction = modalSubmissionResult.Interaction;

            modalSubmissionResult.TryGetValue(ModalTextInputId, out quarantineReason);

            await modalInteraction.DeferAsync(ephemeral: true);
        }

        quarantineReason = quarantineReason.NullOrWhiteSpaceTo("/shrug");

        await _quarantineService.QuarantineMember(targetMember, currentMember, quarantineReason);

        await ctx.TryRespondEphemeral("User quarantined successfully", modalInteraction);
    }

    private async Task<ModalSubmittedEventArgs> ShowGetReasonModal(SlashCommandContext ctx)
    {
        var modalId = $"get-reason-modal-{Guid.NewGuid()}";

        DiscordModalBuilder modalBuilder = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Quarantine user")
            .AddTextInput(
                new DiscordTextInputComponent(
                    ModalTextInputId,
                    "Write a reason for quarantining",
                    string.Empty,
                    required: true,
                    DiscordTextInputStyle.Short,
                    min_length: 0,
                    DiscordConstants.MaxAuditReasonLength),
                "Reason");

        await ctx.RespondWithModalAsync(modalBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync(modalId, DiscordConstants.MaxDeferredInteractionWait);

        return modalSubmission.Result;
    }
}
