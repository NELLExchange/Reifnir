using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Common.Models.Modmail;
using Nellebot.Data.Repositories;
using Nellebot.Services.Loggers;
using Quartz;

namespace Nellebot.Jobs;

public class ModmailCleanupJob : IJob
{
    public static readonly JobKey Key = new("modmail-cleanup", "default");

    private readonly DiscordLogger _discordLogger;
    private readonly ModmailTicketRepository _modmailTicketRepo;
    private readonly IMediator _mediator;
    private readonly BotOptions _options;

    public ModmailCleanupJob(
        IOptions<BotOptions> options,
        DiscordLogger discordLogger,
        ModmailTicketRepository modmailTicketRepo,
        IMediator mediator)
    {
        _options = options.Value;
        _discordLogger = discordLogger;
        _modmailTicketRepo = modmailTicketRepo;
        _mediator = mediator;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _discordLogger.LogExtendedActivityMessage($"Job started: {Key}");

            CancellationToken cancellationToken = context.CancellationToken;

            TimeSpan cleanupInterval = TimeSpan.FromHours(_options.ModmailTicketInactiveThresholdInHours);

            List<ModmailTicket> expiredTickets = await _modmailTicketRepo.GetOpenExpiredTickets(cleanupInterval);

            _discordLogger.LogExtendedActivityMessage($"Found {expiredTickets.Count} expired modmail tickets.");

            foreach (ModmailTicket ticket in expiredTickets)
            {
                await _mediator.Send(new CloseInactiveModmailTicketCommand(ticket), cancellationToken);
            }

            _discordLogger.LogExtendedActivityMessage($"Job finished: {Key}");
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }
}
