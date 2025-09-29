using System;
using System.Collections.Generic;
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

    public AddMetaMessageCommand(SlashCommandContext ctx, DiscordChannel channel, DiscordMessage targetMessage)
        : base(ctx)
    {
        Channel = channel;
        TargetMessage = targetMessage;
    }

    public DiscordChannel Channel { get; }

    public DiscordMessage? TargetMessage { get; set; }
}

public class AddMetaMessageHandler : IRequestHandler<AddMetaMessageCommand>
{
    private const string ModalTextInputId = "modal-text-input";
    private const int FollowupMessageDelayMs = 500;

    private readonly InteractivityExtension _interactivityExtension;
    private readonly DiscordLogger _discordLogger;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly BotOptions _options;

    public AddMetaMessageHandler(
        IOptions<BotOptions> options,
        InteractivityExtension interactivityExtension,
        DiscordLogger discordLogger,
        IDiscordErrorLogger discordErrorLogger,
        DiscordResolver discordResolver)
    {
        _interactivityExtension = interactivityExtension;
        _discordLogger = discordLogger;
        _discordErrorLogger = discordErrorLogger;
        _discordResolver = discordResolver;
        _options = options.Value;
    }

    public async Task Handle(AddMetaMessageCommand request, CancellationToken cancellationToken)
    {
        SlashCommandContext ctx = request.Ctx;
        DiscordMessage? targetMessage = request.TargetMessage;

        if (!_options.MetaChannelIds.Contains(request.Channel.Id))
        {
            await ctx.RespondAsync("This channel is not a meta channel!", ephemeral: true);
            return;
        }

        ModalSubmittedEventArgs modalSubmissionResult = await ShowAddMessageModal(ctx);

        DiscordInteraction modalInteraction = modalSubmissionResult.Interaction;

        await modalInteraction.DeferAsync(true);

        try
        {
            var addMessageText = modalSubmissionResult.GetValue<string>(ModalTextInputId);

            if (string.IsNullOrWhiteSpace(addMessageText))
            {
                throw new InteractionException(modalInteraction, "Message was empty!");
            }

            var guild = _discordResolver.ResolveGuild().ToAppDiscordGuild();

            string encodedMessageText = DiscordMentionEncoder.EncodeMentions(guild, addMessageText);

            DiscordMessage addedMessage = await request.Channel.SendSuppressedMessageAsync(encodedMessageText);

            LogActivityMessage(request, modalInteraction, encodedMessageText);

            var followupMessageText = $"Message added successfully! [Jump to message]({addedMessage.JumpLink})";
            DiscordMessage followupMessage = await SendFollowupMessageAsync(modalInteraction, followupMessageText);

            if (targetMessage is not null)
            {
                await MoveMessagesAfter(
                    targetMessage,
                    followupMessage,
                    modalInteraction,
                    addedMessage.Id,
                    ctx.Client.CurrentUser.Id,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to add message: {ex.Message}";
            throw new InteractionException(modalInteraction, errorMessage, ex);
        }
    }

    private static async Task MoveMessagesAfter(
        DiscordMessage targetMessage,
        DiscordMessage followupMessage,
        DiscordInteraction modalInteraction,
        ulong addedMessageId,
        ulong botUserId,
        CancellationToken cancellationToken)
    {
        DiscordChannel channel = targetMessage.Channel
                                 ?? throw new InvalidOperationException("Channel of targetMessage was null!");

        var messagesAfter = new List<DiscordMessage>();

        await foreach (DiscordMessage message in channel.GetMessagesAfterAsync(
                           targetMessage.Id,
                           limit: 20,
                           cancellationToken))
        {
            // If we've reached (or somehow passed) the newly added message, stop moving messages
            if (message.Id >= addedMessageId)
                break;

            // Skip messages not sent by the bot
            if (message.Author is null || message.Author.Id != botUserId)
                continue;

            messagesAfter.Add(message);
        }

        if (messagesAfter.Count == 0)
            return;

        // Add a small delay to give the user time to read the followup message,
        // otherwise it looks pretty gnarly when the messages start moving
        await Task.Delay(FollowupMessageDelayMs, cancellationToken);

        var pleaseWaitText = $"\nMoving {messagesAfter.Count} message(s) after the target message. Please wait...";
        DiscordMessage pleaseWaitMessage =
            await AppendToFollowupMessageAsync(modalInteraction, followupMessage, pleaseWaitText);

        await Task.Delay(FollowupMessageDelayMs, cancellationToken);
        foreach (DiscordMessage message in messagesAfter)
        {
            // Re-send the message
            await channel.SendSuppressedMessageAsync(message.Content);

            // Delete the original message
            await channel.DeleteMessageAsync(message, "Moved");
        }

        await Task.Delay(FollowupMessageDelayMs, cancellationToken);
        await AppendToFollowupMessageAsync(modalInteraction, pleaseWaitMessage, "\nDone!");
    }

    private static async Task<DiscordMessage> SendFollowupMessageAsync(
        DiscordInteraction modalInteraction,
        string followupMessageText)
    {
        DiscordFollowupMessageBuilder followupBuilder = new DiscordFollowupMessageBuilder()
            .WithContent(followupMessageText)
            .AsEphemeral();

        return await modalInteraction.CreateFollowupMessageAsync(followupBuilder);
    }

    private static async Task<DiscordMessage> AppendToFollowupMessageAsync(
        DiscordInteraction modalInteraction,
        DiscordMessage followupMessage,
        string followupMessageText)
    {
        var newFollowupMessageText = $"{followupMessage.Content}\n{followupMessageText}";

        DiscordWebhookBuilder followupBuilder = new DiscordWebhookBuilder()
            .WithContent(newFollowupMessageText);

        return await modalInteraction.EditFollowupMessageAsync(followupMessage.Id, followupBuilder);
    }

    private async Task<ModalSubmittedEventArgs> ShowAddMessageModal(SlashCommandContext ctx)
    {
        var modalId = $"add-message-modal-{Guid.NewGuid()}";

        DiscordModalBuilder modalBuilder = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Add a message")
            .AddTextInput(
                new DiscordTextInputComponent(
                    ModalTextInputId,
                    "Write a message...",
                    string.Empty,
                    required: true,
                    DiscordTextInputStyle.Paragraph,
                    min_length: 0,
                    DiscordConstants.MaxMessageLength),
                "Message");

        await ctx.RespondWithModalAsync(modalBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync(modalId, DiscordConstants.MaxDeferredInteractionWait);

        return modalSubmission.Result;
    }

    private void LogActivityMessage(
        AddMetaMessageCommand request,
        DiscordInteraction modalInteraction,
        string encodedMessageText)
    {
        try
        {
            DiscordMember interactionAuthor = modalInteraction.User as DiscordMember
                                              ?? throw new InteractionException(
                                                  modalInteraction,
                                                  "Interaction author is not a member!");

            var activityMessageText =
                $"Meta message added in {request.Channel.Mention} by {interactionAuthor.Mention}";
            DiscordMessageBuilder activityMessageBuilder = new DiscordMessageBuilder()
                .WithContent(activityMessageText)
                .AddEmbed(EmbedBuilderHelper.BuildSimpleEmbed(encodedMessageText));
            _discordLogger.LogActivityMessage(activityMessageBuilder);
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, "Failed to log activity message for add meta message command");
        }
    }
}
