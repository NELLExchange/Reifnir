using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot;
using Nellebot.CommandHandlers;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using Nellebot.Services;
using Nellebot.Services.HtmlToImage;
using Nellebot.Services.Loggers;
using Nellebot.Services.Ordbok;
using Nellebot.Utils;
using Nellebot.Workers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
ConfigurationManager configuration = builder.Configuration;
IServiceCollection services = builder.Services;

services.Configure<BotOptions>(configuration.GetSection(BotOptions.OptionsKey));

IDataProtectionBuilder dataProtectionBuilder = services.AddDataProtection()
    .SetApplicationName(nameof(Nellebot))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(180));

if (configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
{
    bool allowUnprotectedKeyData = builder.Environment.IsDevelopment();

    try
    {
        string keyDataDir = configuration.GetValue<string>("Nellebot:ProtectorKeyDataDir") ?? string.Empty;
        string certPath = configuration.GetValue<string>("Nellebot:ProtectorCertificatePath") ?? string.Empty;
        string password = configuration.GetValue<string>("Nellebot:ProtectorCertificatePassword") ?? string.Empty;

        dataProtectionBuilder
            .PersistKeysToFileSystem(new DirectoryInfo(keyDataDir))
            .ProtectKeysWithCertificate(new X509Certificate2(certPath, password));
    }
    catch (Exception e) when (allowUnprotectedKeyData)
    {
        Console.WriteLine("Failed to load certificate: " + e.Message);
    }
}

services.AddHttpClient<OrdbokHttpClient>();

services.AddMediatR(
    cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CommandRequestPipelineBehaviour<,>));
    });
services.AddTransient<NotificationPublisher>();

services.AddSingleton<SharedCache>();
services.AddSingleton<ILocalizationService, LocalizationService>();
services.AddSingleton<PuppeteerFactory>();

services.AddSingleton<GoodbyeMessageBuffer>();

AddWorkers(services);

AddChannels(services);

AddInternalServices(services);

AddRepositories(services);

services.AddDbContext(configuration);

services.AddDiscordClient(configuration);

_ = services.AddJobScheduler(configuration);

IHost app = builder.Build();

app.Run();

return;

static void AddRepositories(IServiceCollection services)
{
    services.AddTransient<AwardMessageRepository>();
    services.AddTransient<BotSettingsRepository>();
    services.AddTransient<MessageRefRepository>();
    services.AddTransient<UserLogRepository>();
    services.AddTransient<ModmailTicketRepository>();
    services.AddTransient<OrdbokRepository>();
    services.AddTransient<MessageTemplateRepository>();
}

static void AddInternalServices(IServiceCollection services)
{
    services.AddTransient<AuthorizationService>();
    services.AddTransient<IDiscordErrorLogger, DiscordErrorLogger>();
    services.AddTransient<DiscordLogger>();
    services.AddTransient<AwardMessageService>();
    services.AddTransient<DiscordResolver>();
    services.AddTransient<ScribanTemplateLoader>();
    services.AddTransient<OrdbokModelMapper>();
    services.AddTransient<IOrdbokContentParser, OrdbokContentParser>();
    services.AddTransient<HtmlToImageService>();
    services.AddTransient<BotSettingsService>();
    services.AddTransient<MessageRefsService>();
    services.AddTransient<UserLogService>();
}

static void AddChannels(IServiceCollection services)
{
    const int channelSize = 1024;

    services.AddSingleton(new RequestQueueChannel(Channel.CreateBounded<IRequest>(channelSize)));
    services.AddSingleton(new CommandQueueChannel(Channel.CreateBounded<ICommand>(channelSize)));
    services.AddSingleton(new CommandParallelQueueChannel(Channel.CreateBounded<ICommand>(channelSize)));
    services.AddSingleton(new EventQueueChannel(Channel.CreateBounded<INotification>(channelSize)));
    services.AddSingleton(new DiscordLogChannel(Channel.CreateBounded<BaseDiscordLogItem>(channelSize)));
}

static void AddWorkers(IServiceCollection services)
{
    services.AddHostedService<BotWorker>();
    services.AddHostedService<RequestQueueWorker>();
    services.AddHostedService<CommandQueueWorker>();
    services.AddHostedService<CommandParallelQueueWorker>();
    services.AddHostedService<EventQueueWorker>();
    services.AddHostedService<DiscordLoggerWorker>();
}
