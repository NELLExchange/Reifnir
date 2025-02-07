using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using Nellebot.DiscordModelMappers;
using Nellebot.Infrastructure;
using Nellebot.Services;

namespace Nellebot.CommandHandlers.MessageTemplates;

public record DeleteGoodbyeMessageCommand(CommandContext Ctx, string Id) : BotCommandCommand(Ctx);

public class DeleteGoodbyeMessageHandler : IRequestHandler<DeleteGoodbyeMessageCommand>
{
    private readonly AuthorizationService _authService;
    private readonly SharedCache _cache;
    private readonly MessageTemplateRepository _messageTemplateRepo;

    public DeleteGoodbyeMessageHandler(
        MessageTemplateRepository messageTemplateRepo,
        AuthorizationService authService,
        SharedCache cache)
    {
        _messageTemplateRepo = messageTemplateRepo;
        _authService = authService;
        _cache = cache;
    }

    public async Task Handle(DeleteGoodbyeMessageCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        string id = request.Id;
        DiscordMember author = ctx.Member ?? throw new Exception("Member was null");

        MessageTemplate? messageTemplate = await _messageTemplateRepo.GetMessageTemplate(id, cancellationToken);

        if (messageTemplate == null)
        {
            throw new ArgumentException(
                $"I couldn't find a goodbye message with id {id}, although I'm sure it's your fault.");
        }

        AppDiscordMember appMember = DiscordMemberMapper.Map(ctx.Member);

        bool isAuthorized = _authService.IsAdminOrMod(appMember);

        if (!isAuthorized && messageTemplate.AuthorId != author.Id)
        {
            throw new ArgumentException("Hey! Nacho cheese! Uh, I mean, nacho goodbye message.");
        }

        await _messageTemplateRepo.DeleteMessageTemplate(id, cancellationToken);

        _cache.FlushCache(SharedCacheKeys.GoodbyeMessages);

        await ctx.RespondAsync("Goodbye message deleted successfully.");
    }
}
