using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using MediatR;
using Nellebot.Services;

namespace Nellebot.CommandHandlers;

public record SetGreetingMessageCommand : BotCommandCommand
{
    public SetGreetingMessageCommand(CommandContext ctx, string greetingMessage)
        : base(ctx)
    {
        GreetingMessage = greetingMessage;
    }

    public string GreetingMessage { get; set; }
}

public class SetGreetingMessageHandler : IRequestHandler<SetGreetingMessageCommand>
{
    private readonly BotSettingsService _botSettingsService;

    public SetGreetingMessageHandler(BotSettingsService botSettingsService)
    {
        _botSettingsService = botSettingsService;
    }

    public async Task Handle(SetGreetingMessageCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        string message = request.GreetingMessage;

        await _botSettingsService.SetGreetingMessage(message);

        string previewMemberMention = ctx.Member?.Mention ?? string.Empty;

        string? messagePreview = await _botSettingsService.GetGreetingsMessage(previewMemberMention);

        var sb = new StringBuilder("Greeting message updated successfully. Here's a preview:");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine(messagePreview);

        await ctx.RespondAsync(sb.ToString());
    }
}
