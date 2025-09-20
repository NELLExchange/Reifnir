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
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record QuarantineUserCommand(CommandContext Ctx, DiscordMember Member, string? Reason)
    : BotCommandCommand(Ctx);

public class QuarantineUserHandler : IRequestHandler<QuarantineUserCommand>
{
    private const string ModalTextInputId = "modal-text-input";
    private readonly InteractivityExtension _interactivityExtension;
    private readonly BotOptions _options;
    private readonly QuarantineService _quarantineService;

    public QuarantineUserHandler(
        IOptions<BotOptions> options,
        QuarantineService quarantineService,
        InteractivityExtension interactivityExtension)
    {
        _quarantineService = quarantineService;
        _interactivityExtension = interactivityExtension;
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

        int maxAgeHours = _options.ValhallKickMaxMemberAgeInHours;

        if (guildAge.TotalHours >= maxAgeHours)
        {
            var content =
                $"You cannot quarantine this user. They have been a member of the server for more than {maxAgeHours} hours.";

            await ctx.TryRespondEphemeral(content);

            return;
        }

        bool userAlreadyQuarantined = targetMember.Roles.Any(r => r.Id == _options.QuarantineRoleId);

        if (userAlreadyQuarantined)
        {
            await ctx.TryRespondEphemeral("User is already quarantined");
        }

        DiscordInteraction? modalInteraction = null;
        string? quarantineReason = null;

        if (ctx is SlashCommandContext slashCtx && request.Reason == null)
        {
            ModalSubmittedEventArgs modalSubmissionResult = await ShowGetReasonModal(slashCtx);

            modalInteraction = modalSubmissionResult.Interaction;

            quarantineReason = modalSubmissionResult.Values[ModalTextInputId];

            await modalInteraction.DeferAsync(ephemeral: true);
        }

        quarantineReason = quarantineReason.NullOrWhiteSpaceTo("/shrug");

        await _quarantineService.QuarantineMember(targetMember, currentMember, quarantineReason);

        await ctx.TryRespondEphemeral("User quarantined successfully", modalInteraction);
    }

    private async Task<ModalSubmittedEventArgs> ShowGetReasonModal(SlashCommandContext ctx)
    {
        var modalId = $"get-reason-modal-{Guid.NewGuid()}";

        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithTitle("Quarantine user")
            .WithCustomId(modalId)
            .AddTextInputComponent(
                new DiscordTextInputComponent(
                    "Reason",
                    ModalTextInputId,
                    "Write a reason for quarantining",
                    string.Empty,
                    required: true,
                    DiscordTextInputStyle.Paragraph,
                    min_length: 0,
                    DiscordConstants.MaxAuditReasonLength));

        await ctx.RespondWithModalAsync(interactionBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync(modalId, DiscordConstants.MaxDeferredInteractionWait);

        return modalSubmission.Result;
    }
}
