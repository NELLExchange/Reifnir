using System.Linq;
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

    public RunJobCommand(CommandContext ctx, string jobKeyName, bool dryRun)
        : base(ctx)
    {
        JobKeyName = jobKeyName;
        DryRun = dryRun;
    }

    public string JobKeyName { get; }

    public bool? DryRun { get; }
}

public class RunJobCommandHandler : IRequestHandler<RunJobCommand>
{
    private static readonly JobKey[] RunnableJobs =
    [
        RoleMaintenanceJob.Key,
        ModmailCleanupJob.Key,
        MigrateResourcesJob.Key,
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

        var jobDataMap = new JobDataMap();

        if (request.DryRun.HasValue)
        {
            jobDataMap["dryRun"] = request.DryRun.Value;
        }

        await scheduler.TriggerJob(jobKey, jobDataMap, cancellationToken);

        await ctx.RespondAsync($"Job triggered: {jobKey}");
    }
}
