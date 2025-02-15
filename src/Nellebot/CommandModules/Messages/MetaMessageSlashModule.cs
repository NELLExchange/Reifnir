using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Nellebot.Attributes;

namespace Nellebot.CommandModules.Messages;

[BaseCommandCheck]
[RequireTrustedMember]
[Command("meta-msg")]
[Description("Manage meta messages")]
public class MetaMessageSlashModule
{
    private readonly InteractivityExtension _interactivityExtension;

    public MetaMessageSlashModule(InteractivityExtension interactivityExtension)
    {
        _interactivityExtension = interactivityExtension;
    }

    [Command("add")]
    [Description("Add a new meta message to the current channel or a specified channel.")]
    public async Task AddMessage(
        SlashCommandContext ctx,
        [Description("Optional channel to add the message to. Defaults to the current channel.")]
        DiscordChannel? channel = null)
    {
        var modalTextInputKey = $"add-message-input-{Guid.NewGuid()}";

        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithTitle("Add a message")
            .WithCustomId("add-message")
            .AddComponents(
                new DiscordTextInputComponent(
                    "Message",
                    modalTextInputKey,
                    "Write a message...",
                    string.Empty,
                    true,
                    DiscordTextInputStyle.Paragraph));

        await ctx.RespondWithModalAsync(interactionBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync("add-message", TimeSpan.FromMinutes(15));

        await modalSubmission.Result.Interaction.DeferAsync(true);

        string addedMessageText = modalSubmission.Result.Values[modalTextInputKey];

        if (string.IsNullOrWhiteSpace(addedMessageText))
        {
            throw new Exception("Message was empty!");
        }

        DiscordChannel destinationChannel = channel ?? ctx.Channel;

        DiscordMessage sentMessage = await destinationChannel.SendMessageAsync(addedMessageText);

        DiscordFollowupMessageBuilder followupBuilder = new DiscordFollowupMessageBuilder()
            .WithContent($"Message added successfully! [Jump to message]({sentMessage.JumpLink})").AsEphemeral();
        await modalSubmission.Result.Interaction.CreateFollowupMessageAsync(followupBuilder);
    }

    [Command("edit")]
    [Description("Edit meta message")]
    public async Task EditMessage(SlashCommandContext ctx, DiscordMessage message)
    {
        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithTitle("Edit the message")
            .WithCustomId("edit-message")
            .AddComponents(
                new DiscordTextInputComponent(
                    "Message",
                    "message",
                    "Write a message...",
                    message.Content,
                    true,
                    DiscordTextInputStyle.Paragraph));

        await ctx.RespondWithModalAsync(interactionBuilder);

        InteractivityResult<ModalSubmittedEventArgs> modalSubmission =
            await _interactivityExtension.WaitForModalAsync("edit-message", TimeSpan.FromMinutes(15));

        await modalSubmission.Result.Interaction.DeferAsync(true);

        KeyValuePair<string, string> editedMessageText = modalSubmission.Result.Values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(editedMessageText.Value))
        {
            throw new Exception("Message was empty!");
        }

        await message.ModifyAsync(editedMessageText.Value);

        DiscordFollowupMessageBuilder followup = new DiscordFollowupMessageBuilder()
            .WithContent($"Message edited successfully! [Jump to message]({message.JumpLink})")
            .AsEphemeral();

        await modalSubmission.Result.Interaction.CreateFollowupMessageAsync(followup);
    }
}
