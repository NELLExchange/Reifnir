using System;
using DSharpPlus.Commands;
using Nellebot.CommandHandlers;

namespace Nellebot.Services.Loggers;

public interface IDiscordErrorLogger
{
    void LogCommandError(CommandContext ctx, string errorMessage);

    void LogEventError(EventContext ctx, string errorMessage);

    void LogError(string error, string errorMessage);

    void LogError(string errorMessage);

    void LogWarning(string warning, string warningMessage);

    void LogError(Exception ex, string message);
}
