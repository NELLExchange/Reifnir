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

    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly ModmailTicketRepository _modmailTicketRepo;
    private readonly IMediator _mediator;
    private readonly BotOptions _options;

    public ModmailCleanupJob(
        IOptions<BotOptions> options,
        IDiscordErrorLogger discordErrorLogger,
        ModmailTicketRepository modmailTicketRepo,
        IMediator mediator)
    {
        _options = options.Value;
        _discordErrorLogger = discordErrorLogger;
        _modmailTicketRepo = modmailTicketRepo;
        _mediator = mediator;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            CancellationToken cancellationToken = context.CancellationToken;

            TimeSpan cleanupInterval = TimeSpan.FromHours(_options.ModmailTicketInactiveThresholdInHours);

            List<ModmailTicket> expiredTickets = await _modmailTicketRepo.GetOpenExpiredTickets(cleanupInterval);

            foreach (ModmailTicket ticket in expiredTickets)
            {
                await _mediator.Send(new CloseInactiveModmailTicketCommand(ticket), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, ex.Message);
            throw new JobExecutionException(ex);
        }
    }
}
