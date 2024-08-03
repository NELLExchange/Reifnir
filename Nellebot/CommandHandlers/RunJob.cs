﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using MediatR;
using Nellebot.Jobs;
using Quartz;

namespace Nellebot.CommandHandlers;

public record RunJobCommand : BotCommandCommand
{
    public RunJobCommand(CommandContext ctx, string jobKeyName)
        : base(ctx)
    {
        JobKeyName = jobKeyName;
    }

    public string JobKeyName { get; }
}

public class RunJobCommandHandler : IRequestHandler<RunJobCommand>
{
    private static readonly JobKey[] RunnableJobs =
    [
        RoleMaintenanceJob.Key,
        ModmailCleanupJob.Key,
    ];

    private readonly ISchedulerFactory _schedulerFactory;

    public RunJobCommandHandler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task Handle(RunJobCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;

        JobKey? jobKey = RunnableJobs.FirstOrDefault(k => k.Name == request.JobKeyName);

        if (jobKey is null)
        {
            await ctx.RespondAsync($"Unknown job name: {request.JobKeyName}");
            return;
        }

        IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        await scheduler.TriggerJob(jobKey, cancellationToken);

        await ctx.RespondAsync($"Job triggered: {jobKey}");
    }
}
