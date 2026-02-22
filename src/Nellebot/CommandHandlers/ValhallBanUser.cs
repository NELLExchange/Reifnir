using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record ValhallBanUserCommand(CommandContext Ctx, DiscordMember Member, string? Reason)
    : BotCommandCommand(Ctx);

public class ValhallBanUserHandler : IRequestHandler<ValhallBanUserCommand>
{
    private const string ModalTextInputId = "modal-text-input";
    private const int DeleteMessagesPeriodInDays = 7; // Max number of days allowed by API

    private readonly InteractivityExtension _interactivityExtension;
    private readonly BotOptions _options;

    public ValhallBanUserHandler(IOptions<BotOptions> options, InteractivityExtension interactivityExtension)
    {
        _interactivityExtension = interactivityExtension;
        _options = options.Value;
    }

    public async Task Handle(ValhallBanUserCommand request, CancellationToken cancellationToken)
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

        int maxAgeDays = _options.ValhallBanMaxMemberAgeInDays;

        if (guildAge.TotalDays >= maxAgeDays)
        {
            var content =
                $"You cannot vban this user. They have been a member of the server for more than {maxAgeDays} days.";

            await ctx.TryRespondEphemeral(content);

            return;
        }

        DiscordInteraction? modalInteraction = null;
        string? banReason = request.Reason;

        if (ctx is SlashCommandContext slashCtx && request.Reason == null)
        {
            ModalSubmittedEventArgs modalSubmissionResult = await ShowGetReasonModal(slashCtx);

            modalInteraction = modalSubmissionResult.Interaction;

            modalSubmissionResult.TryGetValue(ModalTextInputId, out banReason);

            await modalInteraction.DeferAsync(ephemeral: true);
        }

        banReason = banReason.NullOrWhiteSpaceTo("/shrug");

        var onBehalfOfReason =
            $"Banned on behalf of {currentMember.DisplayName}. Reason: {banReason}";

        await targetMember.BanAsync(TimeSpan.FromDays(DeleteMessagesPeriodInDays), onBehalfOfReason);

        await ctx.TryRespondEphemeral("User vbanned successfully", modalInteraction);
    }

    private async Task<ModalSubmittedEventArgs> ShowGetReasonModal(SlashCommandContext ctx)
    {
        var modalId = $"get-reason-modal-{Guid.NewGuid()}";

        DiscordModalBuilder interactionBuilder = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Valhall ban user")
            .AddTextInput(
                new DiscordTextInputComponent(
                    ModalTextInputId,
                    "Write a reason for banning",
                    string.Empty,
                    required: true,
                    DiscordTextInputStyle.Short,
                    min_length: 0,
                    DiscordConstants.MaxAuditReasonLength),
                "Reason");

        await ctx.RespondWithModalAsync(interactionBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync(modalId, DiscordConstants.MaxDeferredInteractionWait);

        return modalSubmission.Result;
    }
}
