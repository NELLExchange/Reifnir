using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.Data;
using Nellebot.NotificationHandlers;
using Nellebot.Services.Loggers;
using Nellebot.Workers;

namespace Nellebot.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddDiscordClient(this IServiceCollection services, IConfiguration configuration)
    {
        string defaultLogLevel = configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Warning";
        string botToken = configuration.GetValue<string>("Nellebot:BotToken")
                          ?? throw new Exception("Bot token not found");

        var logLevel = Enum.Parse<LogLevel>(defaultLogLevel);

        var clientBuilder = DiscordClientBuilder.CreateDefault(botToken, DiscordIntents.All, services);

        clientBuilder.SetLogLevel(logLevel);

        clientBuilder.RegisterCommands(configuration);

        clientBuilder.RegisterEventHandlers();

        clientBuilder.UseInteractivity(
            new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
            });

        // This replacement has to happen after the DiscordClientBuilder.CreateDefault call
        // and before the DiscordClient is built.
        services.Replace<IGatewayController, NoWayGateway>();

        // Calling build registers the DiscordClient as a singleton in the service collection
        clientBuilder.Build();
    }

    public static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BotDbContext>(
            builder =>
            {
                var dbConnString = configuration.GetValue<string>("Nellebot:ConnectionString");
                var logLevel = configuration.GetValue<string>("Logging:LogLevel:Default");

                builder.EnableSensitiveDataLogging(logLevel == "Debug");

                builder.UseNpgsql(dbConnString);
            },
            ServiceLifetime.Transient,
            ServiceLifetime.Singleton);
    }

    private static void RegisterCommands(this DiscordClientBuilder builder, IConfiguration configuration)
    {
        var guildId = configuration.GetValue<ulong>("Nellebot:GuildId");
        string commandPrefix = configuration.GetValue<string>("Nellebot:CommandPrefix")
                               ?? throw new Exception("Command prefix not found");

        var config = new CommandsConfiguration
        {
            UseDefaultCommandErrorHandler = false,
        };

        builder.UseCommands(
            (_, commands) =>
            {
                commands.AddCommands(typeof(Program).Assembly, guildId);

                var textCommandProcessor = new TextCommandProcessor(
                    new TextCommandConfiguration()
                    {
                        IgnoreBots = true,
                        PrefixResolver = new DefaultPrefixResolver(false, commandPrefix).ResolvePrefixAsync,
                    });

                commands.AddProcessor(textCommandProcessor);

                commands.AddChecks(typeof(Program).Assembly);

                commands.CommandExecuted += OnCommandExecuted;
                commands.CommandErrored += OnCommandErrored;
            },
            config);
    }

    private static void RegisterEventHandlers(this DiscordClientBuilder builder)
    {
        builder.ConfigureEventHandlers(
            cfg =>
            {
                cfg.HandleSocketOpened(
                    (client, args) =>
                        client.WriteNotification(new ClientConnected(args)));
                cfg.HandleSocketClosed(
                    (client, args) =>
                        client.WriteNotification(new ClientDisconnected(args)));
                cfg.HandleSessionCreated(
                    (client, _) =>
                        client.WriteNotification(new SessionCreatedNotification()));
                cfg.HandleSessionResumed(
                    (client, _) =>
                        client.WriteNotification(new SessionResumedOrDownloadCompletedNotification("SessionResumed")));
                cfg.HandleGuildDownloadCompleted(
                    (client, _) =>
                        client.WriteNotification(
                            new SessionResumedOrDownloadCompletedNotification("GuildDownloadCompleted")));

                cfg.HandleMessageCreated(
                    (client, args) =>
                        client.WriteNotification(new MessageCreatedNotification(args)));
                cfg.HandleMessageUpdated(
                    (client, args) =>
                        client.WriteNotification(new MessageUpdatedNotification(args)));
                cfg.HandleMessageDeleted(
                    (client, args) =>
                        client.WriteNotification(new MessageDeletedNotification(args)));
                cfg.HandleMessagesBulkDeleted(
                    (client, args) =>
                        client.WriteNotification(new MessageBulkDeletedNotification(args)));
                cfg.HandleMessageReactionAdded(
                    (client, args) =>
                        client.WriteNotification(new MessageReactionAddedNotification(args)));
                cfg.HandleMessageReactionRemoved(
                    (client, args) =>
                        client.WriteNotification(new MessageReactionRemovedNotification(args)));

                cfg.HandleGuildMemberAdded(
                    (client, args) =>
                        client.WriteNotification(new GuildMemberAddedNotification(args)));
                cfg.HandleGuildMemberRemoved(
                    (client, args) =>
                        client.WriteNotification(new GuildMemberRemovedNotification(args)));
                cfg.HandleGuildMemberUpdated(
                    (client, args) =>
                        client.WriteNotification(new GuildMemberUpdatedNotification(args)));
                cfg.HandleGuildBanAdded(
                    (client, args) =>
                        client.WriteNotification(new GuildBanAddedNotification(args)));
                cfg.HandleGuildBanRemoved(
                    (client, args) =>
                        client.WriteNotification(new GuildBanRemovedNotification(args)));
            });
    }

    private static async Task WriteNotification<T>(this DiscordClient client, T notification)
        where T : EventNotification
    {
        var eventQueue = client.ServiceProvider.GetRequiredService<EventQueueChannel>();
        await eventQueue.Writer.WriteAsync(notification);
    }

    private static Task OnCommandExecuted(CommandsExtension sender, CommandExecutedEventArgs e)
    {
        IServiceProvider services = sender.Client.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<CommandsExtension>>();

        CommandContext ctx = e.Context;
        string user = ctx.Member?.DisplayName ?? ctx.User.Username;

        switch (ctx)
        {
            case TextCommandContext textCtx:
                DiscordMessage message = textCtx.Message;

                string messageContent = message.Content;

                logger.LogDebug("Text command: {user} -> {messageContent}", user, messageContent);
                break;

            case SlashCommandContext slashCtx:
                // TODO implement a ToString method for Command that includes the command and its arguments
                string commandName = slashCtx.Command.FullName;

                logger.LogDebug("Slash command: {user} -> {commandName}", user, commandName);
                break;

            default:
                logger.LogCritical("Unknown command context type");
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Try to find a suitable error message to return to the user.
    ///     Log error to discord logger.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private static async Task OnCommandErrored(CommandsExtension sender, CommandErroredEventArgs e)
    {
        CommandContext ctx = e.Context;
        Exception exception = e.Exception;

        IServiceProvider services = sender.Client.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<CommandsExtension>>();
        var discordErrorLogger = services.GetRequiredService<IDiscordErrorLogger>();
        BotOptions options = services.GetRequiredService<IOptions<BotOptions>>().Value;

        var errorMessage = string.Empty;
        var appendHelpText = false;

        string commandPrefix = options.CommandPrefix;
        var commandHelpText = $"Type \"{commandPrefix}help\" to get some help.";

        string message = ctx is TextCommandContext textCtx ? textCtx.Message.Content : string.Empty;

        bool isChecksFailedException = exception is ChecksFailedException;
        bool isUnknownCommandException = exception is CommandNotFoundException;

        // TODO: If this isn't enough, create a custom exception class for validation errors
        bool isPossiblyValidationException = exception is ArgumentException;

        if (isUnknownCommandException)
        {
            errorMessage = "I do not recognize your command.";
            appendHelpText = true;
        }
        else if (isChecksFailedException)
        {
            var checksFailedException = (ChecksFailedException)exception;

            ContextCheckFailedData? failedCheck = checksFailedException.Errors.FirstOrDefault();

            if (failedCheck is null)
            {
                errorMessage = "An unknown check failed.";
            }
            else
            {
                ContextCheckAttribute contextCheckAttribute = failedCheck.ContextCheckAttribute;

                if (contextCheckAttribute is BaseCommandCheckAttribute)
                {
                    errorMessage = "I do not care for DM commands.";
                }
                else if (contextCheckAttribute is RequirePermissionsAttribute or RequireTrustedMemberAttribute)
                {
                    errorMessage = "You do not have permission to do that.";
                }
                else
                {
                    errorMessage = "Preexecution check failed.";
                }
            }
        }
        else if (isPossiblyValidationException)
        {
            errorMessage = $"{exception.Message}.";
            appendHelpText = true;
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
            errorMessage = "Something went wrong.";

        if (appendHelpText)
            errorMessage += $" {commandHelpText}";

        await ctx.RespondAsync(errorMessage);

        discordErrorLogger.LogCommandError(ctx, exception.ToString());

        logger.LogWarning("Message: {message}\r\nCommand failed: {exception})", message, exception);
    }
}
