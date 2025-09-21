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

public record EditMetaMessageCommand : BotSlashCommand
{
    public EditMetaMessageCommand(SlashCommandContext ctx, DiscordMessage message)
        : base(ctx)
    {
        Message = message;
    }

    public DiscordMessage Message { get; }
}

public class EditMetaMessageHandler : IRequestHandler<EditMetaMessageCommand>
{
    private const string ModalTextInputId = "modal-text-input";
    private readonly DiscordLogger _discordLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly InteractivityExtension _interactivityExtension;
    private readonly BotOptions _options;

    public EditMetaMessageHandler(
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

    public async Task Handle(EditMetaMessageCommand request, CancellationToken cancellationToken)
    {
        SlashCommandContext ctx = request.Ctx;
        DiscordMessage message = request.Message;
        DiscordChannel channel = message.Channel ?? throw new Exception("Message channel is null!");

        if (!_options.MetaChannelIds.Contains(message.ChannelId))
        {
            await ctx.RespondAsync("The message is not in a meta channel!", ephemeral: true);
            return;
        }

        DiscordUser messageAuthor = message.Author ?? throw new Exception("Message author is null!");

        if (messageAuthor.Id != ctx.Client.CurrentUser.Id)
        {
            await ctx.RespondAsync("The message is not from the bot!", ephemeral: true);
            return;
        }

        var guild = _discordResolver.ResolveGuild().ToAppDiscordGuild();

        string decodedMessageContent = DiscordMentionEncoder.DecodeMentions(guild, message.Content);

        ModalSubmittedEventArgs modalSubmissionResult =
            await ModalSubmittedEventArgs(ctx, decodedMessageContent);

        DiscordInteraction modalInteraction = modalSubmissionResult.Interaction;

        await modalInteraction.DeferAsync(ephemeral: true);

        try
        {
            var editedMessageText = modalSubmissionResult.GetValue<string>(ModalTextInputId);

            if (string.IsNullOrWhiteSpace(editedMessageText))
            {
                throw new InteractionException(modalInteraction, "Message was empty!");
            }

            string encodedMessageText = DiscordMentionEncoder.EncodeMentions(guild, editedMessageText);

            await message.ModifyAsync(encodedMessageText);

            DiscordMember interactionAuthor = modalInteraction.User as DiscordMember
                                              ?? throw new InteractionException(
                                                  modalInteraction,
                                                  "Interaction author is not a member!");

            var activityMessageText = $"Meta message edited in {channel.Mention} by {interactionAuthor.Mention}";
            DiscordMessageBuilder activityMessageBuilder = new DiscordMessageBuilder()
                .WithContent(activityMessageText)
                .AddEmbed(EmbedBuilderHelper.BuildSimpleEmbed(encodedMessageText));
            _discordLogger.LogExtendedActivityMessage(activityMessageBuilder);

            DiscordFollowupMessageBuilder followup = new DiscordFollowupMessageBuilder()
                .WithContent($"Message edited successfully! [Jump to message]({message.JumpLink})")
                .AsEphemeral();

            await modalInteraction.CreateFollowupMessageAsync(followup);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to edit message: {ex.Message}";
            throw new InteractionException(modalInteraction, errorMessage, ex);
        }
    }

    private async Task<ModalSubmittedEventArgs> ModalSubmittedEventArgs(SlashCommandContext ctx, string messageContent)
    {
        var modalId = $"add-message-modal-{Guid.NewGuid()}";

        DiscordModalBuilder modalBuilder = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Edit the message")
            .AddTextInput(
                new DiscordTextInputComponent(
                    ModalTextInputId,
                    "Write a message...",
                    messageContent,
                    required: true,
                    DiscordTextInputStyle.Paragraph,
                    min_length: 1,
                    DiscordConstants.MaxMessageLength),
                "Message");

        await ctx.RespondWithModalAsync(modalBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync(modalId, DiscordConstants.MaxDeferredInteractionWait);

        return modalSubmission.Result;
    }
}
