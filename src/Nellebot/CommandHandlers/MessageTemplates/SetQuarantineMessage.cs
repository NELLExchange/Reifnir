using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using MediatR;
using Nellebot.Services;

namespace Nellebot.CommandHandlers.MessageTemplates;

public record SetQuarantineMessageCommand : BotCommandCommand
{
    public SetQuarantineMessageCommand(CommandContext ctx, string quarantineMessage)
        : base(ctx)
    {
        QuarantineMessage = quarantineMessage;
    }

    public string QuarantineMessage { get; }
}

public class SetQuarantineMessageHandler : IRequestHandler<SetQuarantineMessageCommand>
{
    private readonly BotSettingsService _botSettingsService;

    public SetQuarantineMessageHandler(BotSettingsService botSettingsService)
    {
        _botSettingsService = botSettingsService;
    }

    public async Task Handle(SetQuarantineMessageCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        string message = request.QuarantineMessage;

        await _botSettingsService.SetQuarantineMessage(message);

        string previewMemberMention = ctx.Member?.Mention ?? string.Empty;
        const string previewReason = "Sussy";

        string? messagePreview = await _botSettingsService.GetQuarantineMessage(previewMemberMention, previewReason);

        var sb = new StringBuilder("Quarantine message updated successfully. Here's a preview:");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine(messagePreview);

        await ctx.RespondAsync(sb.ToString());
    }
}
