﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.Workers
{
    public class MessageAwardQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 0;

        private readonly ILogger<MessageAwardQueueWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly MessageAwardQueue _awardQueue;

        public MessageAwardQueueWorker(
                ILogger<MessageAwardQueueWorker> logger,
                IServiceProvider serviceProvider,
                MessageAwardQueue awardQueue
            )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _awardQueue = awardQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_awardQueue.Count == 0 || !_awardQueue.TryDequeue(out var awardMessageQueueItem))
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _logger.LogTrace($"Dequeued message. {_awardQueue.Count} left in queue");

                    using var scope = _serviceProvider.CreateScope();

                    var awardMessageService = scope.ServiceProvider.GetRequiredService<AwardMessageService>();

                    switch (awardMessageQueueItem.Action)
                    {
                        case MessageAwardQueueAction.ReactionChanged:
                            await awardMessageService.HandleAwardChange(awardMessageQueueItem);
                            break;
                        case MessageAwardQueueAction.MessageUpdated:
                            await awardMessageService.HandleAwardMessageUpdated(awardMessageQueueItem);
                            break;
                        case MessageAwardQueueAction.MessageDeleted:
                            await awardMessageService.HandleAwardMessageDeleted(awardMessageQueueItem);
                            break;
                        case MessageAwardQueueAction.AwardDeleted:
                            await awardMessageService.HandleAwardedMessageDeleted(awardMessageQueueItem);
                            break;
                    }

                    nextDelay = BusyDelay;
                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, nameof(MessageAwardQueueWorker));
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }


    }
}
