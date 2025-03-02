using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.MessageTemplates;

public record AddMetaMessageCommand : BotSlashCommand
{
    public AddMetaMessageCommand(SlashCommandContext ctx, DiscordChannel channel)
        : base(ctx)
    {
        Channel = channel;
    }

    public DiscordChannel Channel { get; }
}

public class AddMetaMessageHandler : IRequestHandler<AddMetaMessageCommand>
{
    private const string ModalId = "add-message";
    private readonly InteractivityExtension _interactivityExtension;
    private readonly DiscordLogger _discordLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly BotOptions _options;

    public AddMetaMessageHandler(
        IOptions<BotOptions> options,
        InteractivityExtension interactivityExtension,
        DiscordLogger discordLogger,
        DiscordResolver discordResolver)
    {
        _interactivityExtension = interactivityExtension;
        _discordLogger = discordLogger;
        _discordResolver = discordResolver;
        _options = options.Value;
    }

    public async Task Handle(AddMetaMessageCommand request, CancellationToken cancellationToken)
    {
        SlashCommandContext ctx = request.Ctx;

        if (!_options.MetaChannelIds.Contains(request.Channel.Id))
        {
            await ctx.RespondAsync("This channel is not a meta channel!", ephemeral: true);
            return;
        }

        var modalTextInputId = $"add-message-input-{Guid.NewGuid()}";

        ModalSubmittedEventArgs modalSubmissionResult = await ShowAddMessageModal(ctx, modalTextInputId);

        DiscordInteraction modalInteraction = modalSubmissionResult.Interaction;

        await modalInteraction.DeferAsync(true);

        try
        {
            string addMessageText = modalSubmissionResult.Values[modalTextInputId];

            if (string.IsNullOrWhiteSpace(addMessageText))
            {
                throw new InteractionException(modalInteraction, "Message was empty!");
            }

            var guild = _discordResolver.ResolveGuild().ToAppDiscordGuild();

            string encodedMessageText = DiscordMentionEncoder.EncodeMentions(guild, addMessageText);

            DiscordMessage sentMessage = await request.Channel.SendSuppressedMessageAsync(encodedMessageText);

            DiscordMember interactionAuthor = modalInteraction.User as DiscordMember
                                              ?? throw new InteractionException(
                                                  modalInteraction,
                                                  "Interaction author is not a member!");

            var activityMessageText =
                $"Meta message added in {request.Channel.Mention} by {interactionAuthor.Mention}";
            DiscordMessageBuilder activityMessageBuilder = new DiscordMessageBuilder()
                .WithContent(activityMessageText)
                .AddEmbed(EmbedBuilderHelper.BuildSimpleEmbed(encodedMessageText));
            _discordLogger.LogExtendedActivityMessage(activityMessageBuilder);

            DiscordFollowupMessageBuilder followupBuilder = new DiscordFollowupMessageBuilder()
                .WithContent($"Message added successfully! [Jump to message]({sentMessage.JumpLink})").AsEphemeral();
            await modalInteraction.CreateFollowupMessageAsync(followupBuilder);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to add message: {ex.Message}";
            throw new InteractionException(modalInteraction, errorMessage, ex);
        }
    }

    private async Task<ModalSubmittedEventArgs> ShowAddMessageModal(SlashCommandContext ctx, string modalTextInputId)
    {
        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithTitle("Add a message")
            .WithCustomId(ModalId)
            .AddComponents(
                new DiscordTextInputComponent(
                    "Message",
                    modalTextInputId,
                    "Write a message...",
                    string.Empty,
                    required: true,
                    DiscordTextInputStyle.Paragraph,
                    min_length: 0,
                    DiscordConstants.MaxMessageLength));

        await ctx.RespondWithModalAsync(interactionBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync(ModalId, DiscordConstants.MaxDeferredInteractionWait);

        return modalSubmission.Result;
    }
}
