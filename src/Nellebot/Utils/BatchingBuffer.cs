using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.Utils;

public class BatchingBuffer<T>
{
    private readonly Func<IEnumerable<T>, Task> _callback;
    private readonly int _delayMillis;
    private readonly object _lockObject;
    private readonly ConcurrentQueue<T> _messageQueue;
    private readonly Timer _timer;

    public BatchingBuffer(int delayMillis, Func<IEnumerable<T>, Task> callback)
    {
        _messageQueue = new ConcurrentQueue<T>();
        _delayMillis = delayMillis;
        _callback = callback;
        _lockObject = new object();
        _timer = new Timer(InvokeCallback, state: null, Timeout.Infinite, Timeout.Infinite);
    }

    public void AddMessage(T message)
    {
        _messageQueue.Enqueue(message);
        _timer.Change(_delayMillis, Timeout.Infinite);
    }

    private void InvokeCallback(object? state)
    {
        lock (_lockObject)
        {
            var allMessages = new List<T>();

            while (_messageQueue.TryDequeue(out T? message))
            {
                allMessages.Add(message);
            }

            _ = InvokeCallbackAsync(allMessages);
        }
    }

    private async Task InvokeCallbackAsync(IEnumerable<T> messages)
    {
        await _callback.Invoke(messages).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}
