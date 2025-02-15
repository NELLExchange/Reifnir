using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Localization;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;

namespace Nellebot.CommandModules.Messages;

public class MetaMessageMenuModule
{
    private readonly BotOptions _options;
    private readonly InteractivityExtension _interactivityExtension;

    public MetaMessageMenuModule(IOptions<BotOptions> options, InteractivityExtension interactivityExtension)
    {
        _options = options.Value;
        _interactivityExtension = interactivityExtension;
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("Add new meta message")]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public async Task AddMessage(SlashCommandContext ctx, DiscordMessage _)
    {
        if (!_options.MetaChannelIds.Contains(ctx.Channel.Id))
        {
            await ctx.RespondAsync("This channel is not a meta channel!", true);
            return;
        }

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

    [Command("Edit meta message")]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public async Task EditMessage(SlashCommandContext ctx, DiscordMessage message)
    {
        if (!_options.MetaChannelIds.Contains(message.ChannelId))
        {
            await ctx.RespondAsync("The message is not in a meta channel!", true);
            return;
        }

        DiscordUser messageAuthor = message.Author ?? throw new Exception("Message author is null!");

        if (messageAuthor.Id != ctx.Client.CurrentUser.Id)
        {
            await ctx.RespondAsync("The message is not from the bot!", true);
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

    private class Localizer : IInteractionLocalizer
    {
        public ValueTask<IReadOnlyDictionary<DiscordLocale, string>> TranslateAsync(string fullSymbolName)
        {
            return ValueTask.FromResult<IReadOnlyDictionary<DiscordLocale, string>>(
                new Dictionary<DiscordLocale, string>()
                {
                    {
                        DiscordLocale.en_US, "Blop burp"
                    },
                });
        }
    }
}
