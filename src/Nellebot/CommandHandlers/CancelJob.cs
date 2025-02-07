using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using MediatR;
using Nellebot.Jobs;
using Quartz;

namespace Nellebot.CommandHandlers;

public record CancelJobCommand : BotCommandCommand
{
    public CancelJobCommand(CommandContext ctx, string jobKeyName)
        : base(ctx)
    {
        JobKeyName = jobKeyName;
    }

    public string JobKeyName { get; }
}

public class CancelJobCommandHandler : IRequestHandler<CancelJobCommand>
{
    private readonly ISchedulerFactory _schedulerFactory;

    public CancelJobCommandHandler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task Handle(CancelJobCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;

        IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        IReadOnlyCollection<IJobExecutionContext> runningJobs =
            await scheduler.GetCurrentlyExecutingJobs(cancellationToken);

        IJobExecutionContext? jobToCancel = runningJobs.FirstOrDefault(j => j.JobDetail.Key.Name == request.JobKeyName);

        if (jobToCancel is null)
        {
            await ctx.RespondAsync($"No job running with name: {request.JobKeyName}");
            return;
        }

        await scheduler.Interrupt(jobToCancel.FireInstanceId, cancellationToken);

        await ctx.RespondAsync($"Canceled job: {request.JobKeyName}");
    }
}
