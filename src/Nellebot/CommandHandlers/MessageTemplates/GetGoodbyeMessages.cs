using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.MessageTemplates;

public record GetGoodbyeMessagesCommand(CommandContext Ctx) : BotCommandCommand(Ctx);

public class GetGoodbyeMessagesHandler : IRequestHandler<GetGoodbyeMessagesCommand>
{
    private const string GoodbyeMessageTemplateType = "goodbye";
    private const string UserToken = "$USER";
    private const int MessageTemplatesCacheDurationMinutes = 5;
    private const int MessagesPerPage = 5;
    private readonly SharedCache _cache;
    private readonly DiscordResolver _discordResolver;

    private readonly MessageTemplateRepository _messageTemplateRepo;

    public GetGoodbyeMessagesHandler(
        MessageTemplateRepository messageTemplateRepo,
        SharedCache cache,
        DiscordResolver discordResolver)
    {
        _messageTemplateRepo = messageTemplateRepo;
        _cache = cache;
        _discordResolver = discordResolver;
    }

    public async Task Handle(GetGoodbyeMessagesCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMember author = ctx.Member ?? throw new Exception("Member was null");

        List<MessageTemplate> goodbyeMessages = (await _cache.LoadFromCacheAsync(
                                                     SharedCacheKeys.GoodbyeMessages,
                                                     async () =>
                                                         await _messageTemplateRepo
                                                             .GetAllMessageTemplates(GoodbyeMessageTemplateType),
                                                     TimeSpan
                                                         .FromMinutes(MessageTemplatesCacheDurationMinutes))
                                                 ?? Enumerable.Empty<MessageTemplate>())
            .ToList();

        if (goodbyeMessages.Count == 0)
        {
            await ctx.RespondAsync("No messages");
            return;
        }

        var pageCount = (int)Math.Ceiling(goodbyeMessages.Count / (double)MessagesPerPage);

        var pages = new List<Page>();

        for (var i = 0; i < pageCount; i++)
        {
            List<MessageTemplate> messagesForPage = goodbyeMessages
                .Skip(i * MessagesPerPage)
                .Take(MessagesPerPage)
                .ToList();

            var sb = new StringBuilder();

            for (var j = 0; j < messagesForPage.Count; j++)
            {
                MessageTemplate message = messagesForPage[j];
                string messageTemplateWithoutBoldedUserToken = message.Message.Replace($"**{UserToken}**", UserToken);

                DiscordMember? member = await _discordResolver.ResolveGuildMember(message.AuthorId);

                sb.AppendLine($"Id: {message.Id}, Author: {member?.DisplayName ?? "Unknown"}");
                sb.AppendLine($"```\r\n{messageTemplateWithoutBoldedUserToken}\r\n```");
                sb.AppendLine();
            }

            var pageContent = sb.ToString();
            DiscordEmbedBuilder pageEb = new DiscordEmbedBuilder()
                .WithTitle($"Goodbye messages. Page {i + 1}/{pageCount}")
                .WithColor(DiscordConstants.DefaultEmbedColor)
                .WithDescription(pageContent);

            pages.Add(new Page(string.Empty, pageEb));
        }

        await ctx.Channel.SendPaginatedMessageAsync(
            author,
            pages,
            PaginationBehaviour.WrapAround,
            ButtonPaginationBehavior.Disable,
            cancellationToken);
    }
}
