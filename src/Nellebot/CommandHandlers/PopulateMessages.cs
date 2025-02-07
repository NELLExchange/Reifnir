using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Services;

namespace Nellebot.CommandHandlers;

public record PopulateMessagesCommand : BotCommandCommand
{
    public PopulateMessagesCommand(CommandContext ctx)
        : base(ctx)
    { }
}

public class PopulateMessagesHandler : IRequestHandler<PopulateMessagesCommand>
{
    private readonly MessageRefsService _messageRefsService;

    public PopulateMessagesHandler(MessageRefsService messageRefsService)
    {
        _messageRefsService = messageRefsService;
    }

    public async Task Handle(PopulateMessagesCommand request, CancellationToken cancellationToken)
    {
        DiscordGuild guild = request.Ctx.Guild;
        DiscordChannel channel = request.Ctx.Channel;

        IList<DiscordMessage> createdMessages = await _messageRefsService.PopulateMessageRefsInit(guild);

        if (createdMessages.Count == 0)
        {
            await channel.SendMessageAsync("No messages to populate");
            return;
        }

        List<IGrouping<string, DiscordMessage>> messagesInChannel =
            createdMessages.GroupBy(x => x.Channel.Name).ToList();

        foreach (IGrouping<string, DiscordMessage> messageGroup in messagesInChannel)
        {
            await channel.SendMessageAsync($"Populated {messageGroup.Count()} message refs in {messageGroup.Key}");
        }
    }
}
