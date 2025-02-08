using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.Utils;

public class MessageBuffer
{
    private readonly Func<IEnumerable<string>, Task> _callback;
    private readonly int _delayMillis;
    private readonly object _lockObject;
    private readonly ConcurrentQueue<string> _messageQueue;
    private readonly Timer _timer;

    public MessageBuffer(int delayMillis, Func<IEnumerable<string>, Task> callback)
    {
        _messageQueue = new ConcurrentQueue<string>();
        _delayMillis = delayMillis;
        _callback = callback;
        _lockObject = new object();
        _timer = new Timer(InvokeCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void AddMessage(string message)
    {
        _messageQueue.Enqueue(message);
        _timer.Change(_delayMillis, Timeout.Infinite);
    }

    private void InvokeCallback(object? state)
    {
        lock (_lockObject)
        {
            var allMessages = new List<string>();

            while (_messageQueue.TryDequeue(out string? message))
            {
                allMessages.Add(message);
            }

            _ = InvokeCallbackAsync(allMessages);
        }
    }

    private async Task InvokeCallbackAsync(IEnumerable<string> messages)
    {
        await _callback.Invoke(messages).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}
