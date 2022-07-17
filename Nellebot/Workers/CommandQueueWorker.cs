﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;

namespace Nellebot.Workers
{
    public class CommandQueue : ConcurrentQueue<CommandRequest>
    {

    }

    public class CommandQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 0;

        private readonly ILogger<CommandQueueWorker> _logger;
        private readonly CommandQueue _commandQueue;
        private readonly IMediator _mediator;

        public CommandQueueWorker(
                ILogger<CommandQueueWorker> logger,
                CommandQueue commandQueue,
                IMediator mediator
            )
        {
            _logger = logger;
            _commandQueue = commandQueue;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_commandQueue.Count == 0 || !_commandQueue.TryDequeue(out var command))
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _logger.LogDebug($"Dequeued command. {_commandQueue.Count} left in queue");

                    await _mediator.Send(command, stoppingToken);

                    nextDelay = BusyDelay;

                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, typeof(CommandQueueWorker).Name);
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
