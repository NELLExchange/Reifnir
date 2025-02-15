using System;
using System.Collections.Generic;
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
public class MetaMessageModule
{
    private readonly InteractivityExtension _interactivityExtension;

    public MetaMessageSlashModule(InteractivityExtension interactivityExtension)
    {
        _interactivityExtension = interactivityExtension;
    }

    [Command("add")]
    public async Task AddMessage(SlashCommandContext ctx)
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

        DiscordMessage sentMessage = await ctx.Channel.SendMessageAsync(addedMessageText);

        DiscordFollowupMessageBuilder followupBuilder = new DiscordFollowupMessageBuilder()
            .WithContent($"Message added successfully! [Jump to message]({sentMessage.JumpLink})").AsEphemeral();
        await modalSubmission.Result.Interaction.CreateFollowupMessageAsync(followupBuilder);
    }

    [Command("edit")]
    public async Task EditMessage(SlashCommandContext ctx, ulong messageId)
    {
        DiscordMessage? message;

        try
        {
            message = await ctx.Channel.GetMessageAsync(messageId, true);
        }
        catch (Exception)
        {
            await ctx.RespondAsync("Message not found!", true);
            return;
        }

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
