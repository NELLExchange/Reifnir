using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using MediatR;
using Nellebot.Data.Repositories;

namespace Nellebot.NotificationHandlers;

public class UntitledHandler : INotificationHandler<MessageCreatedNotification>,
    INotificationHandler<MessageUpdatedNotification>
{
    private readonly MessageRefRepository _messageRefRepo;

    public UntitledHandler(MessageRefRepository messageRefRepo)
    {
        _messageRefRepo = messageRefRepo;
    }

    public async Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        MessageUpdatedEventArgs args = notification.EventArgs;

        const ulong seventeenThreadId = 1151798901750378607;

        if (args.Channel.Id == seventeenThreadId)
        {
            string messageContent = args.Message.Content;

            if (Seventeen.IsMatch(messageContent))
            {
                await args.Message.DeleteAsync();
            }
        }

        // await _messageRefRepo.CreateMessageRef(args.Message.Id, args.Channel.Id, args.Author.Id);
    }

    public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        MessageCreatedEventArgs args = notification.EventArgs;

        const ulong seventeenThreadId = 1151798901750378607;

        if (args.Channel.Id == seventeenThreadId)
        {
            string messageContent = args.Message.Content;

            if (Seventeen.IsMatch(messageContent))
            {
                await args.Message.DeleteAsync();
            }
        }

        await _messageRefRepo.CreateMessageRef(args.Message.Id, args.Channel.Id, args.Author.Id);
    }
}
