﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Helpers;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class RelayMessageHandlers : IRequestHandler<RelayRequesterMessageCommand>,
                                    IRequestHandler<RelayModeratorMessageCommand>
{
    private readonly BotOptions _options;
    private readonly DiscordResolver _resolver;

    public RelayMessageHandlers(IOptions<BotOptions> options, DiscordResolver resolver)
    {
        _options = options.Value;
        _resolver = resolver;
    }

    /// <summary>
    /// Relay requester's message to the modmail thread.
    /// </summary>
    /// <param name="request">The <see cref="RelayRequesterMessageCommand"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Handle(RelayRequesterMessageCommand request, CancellationToken cancellationToken)
    {
        var ticket = request.Ticket;
        var messageToRelay = request.Ctx.Message;

        var forumPostChannelId = ticket.ForumPostChannelId
            ?? throw new Exception("The ticket does not have a post channelId");

        var threadChannel = _resolver.ResolveThread(forumPostChannelId)
            ?? throw new Exception("Could not resolve thread channel");

        var relayMessageContent = $"""
            Message from {ticket.RequesterDisplayName}:
            {messageToRelay.Content}
            """;

        await threadChannel.SendMessageAsync(relayMessageContent);

        await messageToRelay.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
    }

    /// <summary>
    /// Relay moderator's message as a dm to the requester.
    /// </summary>
    /// <param name="request">The <see cref="RelayModeratorMessageCommand"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Handle(RelayModeratorMessageCommand request, CancellationToken cancellationToken)
    {
        var member = (await _resolver.ResolveGuildMember(request.Ctx.User.Id))
                    ?? throw new Exception("Could not resolve member");

        var messageToRelay = request.Ctx.Message;

        if (!member.Roles.Any(r => r.Id == _options.AdminRoleId))
        {
            await messageToRelay.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
            return;
        }

        var relayMessageContent = $"""
            Message from moderator:
            {messageToRelay.Content}
            """;

        var relayedMessage = await member.SendMessageAsync(relayMessageContent);

        await messageToRelay.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
    }
}