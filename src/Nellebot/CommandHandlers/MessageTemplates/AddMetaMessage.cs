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
    private readonly InteractivityExtension _interactivityExtension;
    private readonly BotOptions _options;

    public AddMetaMessageHandler(IOptions<BotOptions> options, InteractivityExtension interactivityExtension)
    {
        _interactivityExtension = interactivityExtension;
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

        const string modalId = "add-message";
        var modalTextInputId = $"add-message-input-{Guid.NewGuid()}";

        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithTitle("Add a message")
            .WithCustomId(modalId)
            .AddComponents(
                new DiscordTextInputComponent(
                    "Message",
                    modalTextInputId,
                    "Write a message...",
                    string.Empty,
                    required: true,
                    DiscordTextInputStyle.Paragraph,
                    min_length: 1,
                    DiscordConstants.MaxMessageLength));

        await ctx.RespondWithModalAsync(interactionBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync(modalId, TimeSpan.FromMinutes(15));

        DiscordInteraction modalInteraction = modalSubmission.Result.Interaction;
        await modalInteraction.DeferAsync(true);

        try
        {
            string addedMessageText = modalSubmission.Result.Values[modalTextInputId];

            if (string.IsNullOrWhiteSpace(addedMessageText))
            {
                throw new InteractionException(modalInteraction, "Message was empty!");
            }

            DiscordMessage sentMessage = await request.Channel.SendMessageAsync(addedMessageText);

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
}
