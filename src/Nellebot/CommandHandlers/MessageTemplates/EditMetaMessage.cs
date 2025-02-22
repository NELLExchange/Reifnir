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
    private readonly InteractivityExtension _interactivityExtension;
    private readonly DiscordLogger _discordLogger;
    private readonly BotOptions _options;

    public EditMetaMessageHandler(
        IOptions<BotOptions> options,
        InteractivityExtension interactivityExtension,
        DiscordLogger discordLogger)
    {
        _interactivityExtension = interactivityExtension;
        _discordLogger = discordLogger;
        _options = options.Value;
    }

    public async Task Handle(EditMetaMessageCommand request, CancellationToken cancellationToken)
    {
        SlashCommandContext ctx = request.Ctx;
        DiscordMessage message = request.Message;
        DiscordChannel channel = message.Channel ?? throw new Exception(message: "Message channel is null!");

        if (!_options.MetaChannelIds.Contains(message.ChannelId))
        {
            await ctx.RespondAsync("The message is not in a meta channel!", ephemeral: true);
            return;
        }

        DiscordUser messageAuthor = message.Author ?? throw new Exception(message: "Message author is null!");

        if (messageAuthor.Id != ctx.Client.CurrentUser.Id)
        {
            await ctx.RespondAsync("The message is not from the bot!", ephemeral: true);
            return;
        }

        const string modalId = "edit-message";
        const string modalTextInputId = "message";

        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithTitle(title: "Edit the message")
            .WithCustomId(modalId)
            .AddComponents(
                new DiscordTextInputComponent(
                    "Message",
                    modalTextInputId,
                    "Write a message...",
                    message.Content,
                    required: true,
                    DiscordTextInputStyle.Paragraph,
                    min_length: 1,
                    DiscordConstants.MaxMessageLength));

        await ctx.RespondWithModalAsync(interactionBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync(modalId, TimeSpan.FromMinutes(value: 15));

        DiscordInteraction modalInteraction = modalSubmission.Result.Interaction;

        await modalInteraction.DeferAsync(ephemeral: true);

        try
        {
            string editedMessageText = modalSubmission.Result.Values[modalTextInputId];

            if (string.IsNullOrWhiteSpace(editedMessageText))
            {
                throw new InteractionException(modalInteraction, "Message was empty!");
            }

            await message.ModifyAsync(editedMessageText);

            DiscordMember interactionAuthor = modalInteraction.User as DiscordMember
                                              ?? throw new InteractionException(
                                                  modalInteraction,
                                                  "Interaction author is not a member!");

            var activityMessageText = $"Meta message edited in {channel.Mention} by {interactionAuthor.Mention}";
            DiscordMessageBuilder activityMessageBuilder = new DiscordMessageBuilder()
                .WithContent(activityMessageText)
                .AddEmbed(EmbedBuilderHelper.BuildSimpleEmbed(editedMessageText));
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
}
