using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models.Modmail;
using Nellebot.Common.Utils;
using Nellebot.Data.Repositories;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class RequestModmailTicketHandler : IRequestHandler<RequestModmailTicketCommand>
{
    private const string RealNameButtonId = "realNameButton";
    private const string PseudonymButtonId = "pseudonymButton";
    private const string CancelButtonId = "cancelButton";
    private const string CancelMessageToken = "cancel";
    private readonly ModmailTicketRepository _modmailTicketRepo;

    private readonly BotOptions _options;
    private readonly DiscordResolver _resolver;

    public RequestModmailTicketHandler(
        IOptions<BotOptions> options,
        DiscordResolver resolver,
        ModmailTicketRepository modmailTicketRepo)
    {
        _options = options.Value;
        _resolver = resolver;
        _modmailTicketRepo = modmailTicketRepo;
    }

    public async Task Handle(RequestModmailTicketCommand request, CancellationToken cancellationToken)
    {
        BaseContext ctx = request.Ctx;
        DiscordChannel channel = ctx.Channel;
        DiscordMessage? requestMessage = request.RequestMessage;

        DiscordMember? member = await _resolver.ResolveGuildMember(ctx.User.Id);

        if (member is null)
        {
            bool isDmFromNonGuildMember = channel.IsPrivate;

            if (isDmFromNonGuildMember)
            {
                var messageContent = "Members only!";

                await channel.SendMessageAsync(messageContent);

                return;
            }

            throw new Exception("Couldn't fetch member");
        }

        ModmailTicket? existingTicket =
            await _modmailTicketRepo.GetActiveTicketByRequesterId(ctx.User.Id, cancellationToken);

        if (existingTicket != null)
        {
            var messageContent = "You already have an open ticket! Just type your message here and I will pass it on.";

            await member.SendMessageAsync(messageContent);

            return;
        }

        string choice = await CollectIdentityChoice(member, cancellationToken);

        if (choice == CancelButtonId)
        {
            await HandleCancellation(member, null, cancellationToken);

            return;
        }

        bool requesterIsAnonymous = choice == PseudonymButtonId;

        string requesterDisplayName;

        if (!requesterIsAnonymous)
        {
            requesterDisplayName = member.DisplayName;
        }
        else
        {
            requesterDisplayName = PseudonymGenerator.GetRandomPseudonym();
        }

        ModmailTicket stub = await CreateStubModmailTicket(
            ctx,
            requesterDisplayName,
            requesterIsAnonymous,
            cancellationToken);

        await member.SendMessageAsync($"Understood. Your message will be sent as **{requesterDisplayName}**.");

        DiscordMessage messageToRelay;

        if (requestMessage is not null)
        {
            bool confirmed = await CollectRequestMessageConfirmation(requestMessage, member);

            if (!confirmed)
            {
                await HandleCancellation(member, stub, cancellationToken);

                return;
            }

            messageToRelay = requestMessage;
        }
        else
        {
            DiscordMessage? collectedMessage = await CollectRequestMessage(member);

            if (collectedMessage is null ||
                collectedMessage.Content.Equals(CancelMessageToken, StringComparison.InvariantCultureIgnoreCase))
            {
                await HandleCancellation(member, stub, cancellationToken);

                return;
            }

            messageToRelay = collectedMessage;
        }

        await PostStubModmailTicket(stub, messageToRelay, cancellationToken);

        await messageToRelay.CreateSuccessReactionAsync();

        var ticketCreateMessage = """
                                  Thanks! A staff member will get back to you soon.
                                  If there is something you wish to add do so by sending a new message as I won't be checking for updates to your messages.
                                  """;

        await member.SendMessageAsync(ticketCreateMessage);
    }

    /// <summary>
    ///     Collect a request message if the user didn't provide one in the command.
    /// </summary>
    /// <returns>The collected <see cref="DiscordMessage" />.</returns>
    private static async Task<DiscordMessage?> CollectRequestMessage(DiscordMember member)
    {
        var messageContent =
            "Please type your message here and I will pass it on. If you want to cancel just type `cancel`.";

        DiscordMessage promptForMessageMessage = await member.SendMessageAsync(messageContent);

        InteractivityResult<DiscordMessage> promptInteractivityResult =
            await promptForMessageMessage.Channel.GetNextMessageAsync(member);

        if (promptInteractivityResult.TimedOut)
        {
            return null;
        }

        return promptInteractivityResult.Result;
    }

    /// <summary>
    ///     Collect confirmation from user if they provided a request message in the command.
    /// </summary>
    /// <returns>The collected <see cref="DiscordMessage" />.</returns>
    private static async Task<bool> CollectRequestMessageConfirmation(DiscordMessage message, DiscordMember member)
    {
        var messageContent = $"""
                              The following message will be sent to the moderators.
                              {message.GetQuotedContent()}
                              Please react with {EmojiMap.WhiteCheckmark} to confirm or {EmojiMap.RedX} to cancel.
                              """;

        DiscordMessage promptForConfirmMessage = await member.SendMessageAsync(messageContent);

        await promptForConfirmMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        await promptForConfirmMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));

        InteractivityResult<MessageReactionAddedEventArgs> promptInteractivityResult =
            await promptForConfirmMessage.WaitForReactionAsync(member);

        if (promptInteractivityResult.TimedOut)
        {
            return false;
        }

        string choice = promptInteractivityResult.Result.Emoji.Name;

        await promptForConfirmMessage.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        await promptForConfirmMessage.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));

        return choice == EmojiMap.WhiteCheckmark;
    }

    /// <summary>
    ///     Collect choice of identity from user i.e. use real name or pseudonym.
    /// </summary>
    /// <returns>Returns true if the user chooses to be anonymous.</returns>
    private static async Task<string> CollectIdentityChoice(DiscordMember member, CancellationToken cancellationToken)
    {
        string realName = member.DisplayName;

        var introMessageContent = $"""
                                   Hello and welcome to Modmail!
                                   Would you like to send the message as **{realName}** or do you want to use a random pseudonym?
                                   """;

        var realNameButton = new DiscordButtonComponent(DiscordButtonStyle.Primary, RealNameButtonId, realName);
        var pseudonymButton = new DiscordButtonComponent(
            DiscordButtonStyle.Primary,
            PseudonymButtonId,
            "Random pseudonym");
        var cancelButton = new DiscordButtonComponent(DiscordButtonStyle.Secondary, CancelButtonId, "Cancel");

        DiscordMessageBuilder introMessageBuilder = new DiscordMessageBuilder()
            .WithContent(introMessageContent)
            .AddComponents(realNameButton, pseudonymButton, cancelButton);

        DiscordMessage introMessage = await member.SendMessageAsync(introMessageBuilder);

        InteractivityResult<ComponentInteractionCreatedEventArgs> interactionResult =
            await introMessage.WaitForButtonAsync(cancellationToken);

        if (interactionResult.TimedOut)
        {
            return CancelButtonId; // Assume cancelled
        }

        var choiceInteractionResponseBuilder = new DiscordInteractionResponseBuilder(introMessageBuilder);
        choiceInteractionResponseBuilder.ClearComponents();

        await interactionResult.Result.Interaction.CreateResponseAsync(
            DiscordInteractionResponseType.UpdateMessage,
            choiceInteractionResponseBuilder);

        return interactionResult.Result.Id;
    }

    private async Task HandleCancellation(
        DiscordMember member,
        ModmailTicket? stub = null,
        CancellationToken cancellationToken = default)
    {
        if (stub != null) await CancelStubModmailTicket(stub, cancellationToken);

        await member.SendMessageAsync("Understood. You can always request a ticket again later.");
    }

    private Task<ModmailTicket> CreateStubModmailTicket(
        BaseContext ctx,
        string requesterDisplayName,
        bool requesterIsAnonymous,
        CancellationToken cancellationToken)
    {
        var modmailTicket = new ModmailTicket
        {
            IsAnonymous = requesterIsAnonymous,
            RequesterId = ctx.User.Id,
            RequesterDisplayName = requesterDisplayName,
        };

        return _modmailTicketRepo.CreateTicket(modmailTicket, cancellationToken);
    }

    private Task CancelStubModmailTicket(ModmailTicket ticket, CancellationToken cancellationToken)
    {
        return _modmailTicketRepo.DeleteTicket(ticket, cancellationToken);
    }

    private async Task PostStubModmailTicket(
        ModmailTicket ticket,
        DiscordMessage requestMesage,
        CancellationToken cancellationToken)
    {
        ulong modmailChannelId = _options.ModmailChannelId;

        var modmailChannel = (DiscordForumChannel)_resolver.ResolveGuild().Channels[modmailChannelId];

        string modmailPostTitle = ticket.IsAnonymous
            ? $"Ticket from anonymous user {ticket.RequesterDisplayName}"
            : $"Ticket from server member {ticket.RequesterDisplayName}";

        var relayMessageContent = $"""
                                   {ticket.RequesterDisplayName} says
                                   {requestMesage.GetQuotedContent()}
                                   """;

        DiscordMessageBuilder modmailPostMessage = new DiscordMessageBuilder()
            .WithContent(relayMessageContent);

        ForumPostBuilder fpBuilder = new ForumPostBuilder()
            .WithName(modmailPostTitle)
            .WithMessage(modmailPostMessage);

        // Refresh the ticket in case it was updated while waiting for the user to type their message.
        ModmailTicket refreshedTicket =
            await _modmailTicketRepo.GetActiveTicketByRequesterId(ticket.RequesterId, cancellationToken)
            ?? throw new Exception("Ticket's gone");

        if (refreshedTicket.TicketPost != null)
        {
            throw new Exception("Ticket already posted");
        }

        DiscordForumPostStarter forumPost = await modmailChannel.CreateForumPostAsync(fpBuilder);

        ModmailTicket postedTicket = refreshedTicket with
        {
            TicketPost = new ModmailTicketPost(forumPost.Channel.Id, forumPost.Message.Id),
        };

        _ = await _modmailTicketRepo.UpdateTicketPost(postedTicket, cancellationToken);
    }
}
