using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nellebot.Jobs;
using Quartz;

namespace Nellebot.Infrastructure;

public static class JobSchedulerProvider
{
    public static IServiceCollection AddJobScheduler(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(
            config =>
            {
                config.SchedulerName = "DefaultScheduler";

                config.ScheduleJob<RoleMaintenanceJob>(
                    t =>
                    {
                        t.ForJob(RoleMaintenanceJob.Key)
                            .WithCronSchedule("0 0 0/6 ? * * *")
                            .WithDescription("Run every 6 hours starting at midnight");
                    },
                    j =>
                    {
                        j.WithIdentity(RoleMaintenanceJob.Key)
                            .WithDescription("Perform role maintenance tasks");
                    });

                config.ScheduleJob<ModmailCleanupJob>(
                    t =>
                    {
                        t.ForJob(ModmailCleanupJob.Key)
                            .WithSimpleSchedule(s => s.WithIntervalInMinutes(10).RepeatForever())
                            .WithDescription("Run every 10 minutes");
                    },
                    j =>
                    {
                        j.WithIdentity(ModmailCleanupJob.Key)
                            .WithDescription("Close inactive modmail tickets");
                    });

                config.AddJob<MigrateResourcesJob>(
                    j =>
                    {
                        j.WithIdentity(MigrateResourcesJob.Key)
                            .WithDescription("Migrate resources to channel")
                            .StoreDurably();
                    });
            });

        services.AddQuartzHostedService(
            opts =>
            {
                opts.AwaitApplicationStarted = true;
                opts.WaitForJobsToComplete = true;

                // Give the bot some time to start up before running the jobs
#if DEBUG
                opts.StartDelay = TimeSpan.FromSeconds(5);
#else
                opts.StartDelay = TimeSpan.FromSeconds(30);
#endif
            });

        return services;
    }
}
