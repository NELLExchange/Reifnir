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

public record ValhallKickUserCommand(CommandContext Ctx, DiscordMember Member, string? Reason)
    : BotCommandCommand(Ctx);

public class ValhallKickUserHandler : IRequestHandler<ValhallKickUserCommand>
{
    private readonly InteractivityExtension _interactivityExtension;
    private const string ModalTextInputId = "modal-text-input";
    private readonly BotOptions _options;

    public ValhallKickUserHandler(IOptions<BotOptions> options, InteractivityExtension interactivityExtension)
    {
        _interactivityExtension = interactivityExtension;
        _options = options.Value;
    }

    public async Task Handle(ValhallKickUserCommand request, CancellationToken cancellationToken)
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
                $"You cannot vkick this user. They have been a member of the server for more than {maxAgeHours} hours.";

            await ctx.TryRespondEphemeral(content);

            return;
        }

        DiscordInteraction? modalInteraction = null;
        string? kickReason = request.Reason;

        if (ctx is SlashCommandContext slashCtx && request.Reason == null)
        {
            ModalSubmittedEventArgs modalSubmissionResult = await ShowGetReasonModal(slashCtx);

            modalInteraction = modalSubmissionResult.Interaction;

            modalSubmissionResult.TryGetValue(ModalTextInputId, out kickReason);

            await modalInteraction.DeferAsync(ephemeral: true);
        }

        kickReason = kickReason.NullOrWhiteSpaceTo("/shrug");

        var onBehalfOfReason =
            $"Kicked on behalf of {currentMember.DisplayName}. Reason: {kickReason}";

        await targetMember.RemoveAsync(onBehalfOfReason);

        await ctx.TryRespondEphemeral("User vkicked successfully", modalInteraction);
    }

    private async Task<ModalSubmittedEventArgs> ShowGetReasonModal(SlashCommandContext ctx)
    {
        var modalId = $"get-reason-modal-{Guid.NewGuid()}";

        DiscordModalBuilder interactionBuilder = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Valhall kick user")
            .AddTextInput(
                new DiscordTextInputComponent(
                    ModalTextInputId,
                    "Write a reason for kicking",
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
