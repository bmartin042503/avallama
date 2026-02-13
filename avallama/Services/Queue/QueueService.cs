// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Models;

namespace avallama.Services.Queue;

public interface IQueueService<T> : IDisposable where T : QueueItem
{
    void Enqueue(T item);
    IReadOnlyList<T> GetQueuedItems();
    void SetParallelism(int newCount);
    void CancelAll();
}

internal class RunningTaskContext<T>
{
    public required T Item { get; init; }
    public required Task Task { get; init; }
    public required CancellationTokenSource Cts { get; init; }
}

public abstract class QueueService<T> : IQueueService<T>
    where T : QueueItem
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly List<RunningTaskContext<T>> _runningTaskContexts = [];
    private CancellationTokenSource _serviceCts = new();
    private TaskCompletionSource _parallelismChangeTcs = new();
    private int _maxParallelism = 1;
    private readonly Lock _lock = new();

    public void SetParallelism(int newCount)
    {
        if (newCount < 1) return;
        lock (_lock)
        {
            if (newCount < _maxParallelism)
            {
                var currentRunningCount = _runningTaskContexts.Count;
                var excessCount = currentRunningCount - newCount;

                if (excessCount <= 0) return;
                var tasksToCancel = _runningTaskContexts.TakeLast(excessCount).ToList();

                foreach (var context in tasksToCancel.Where(context => !context.Cts.IsCancellationRequested))
                {
                    context.Item.QueueItemCancellationReason = QueueItemCancellationReason.SystemScaling;
                    context.Cts.Cancel();
                }
            }
            _maxParallelism = newCount;

            // "notify" the inner process of the queue that parallelism settings have been changed
            _parallelismChangeTcs.TrySetResult();
        }
        _ = ProcessQueueAsync();
    }

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        _ = ProcessQueueAsync();
    }

    public IReadOnlyList<T> GetQueuedItems() => _queue.ToList();

    private async Task ProcessQueueAsync()
    {
        // ensures that one while loop runs only
        if (!await _semaphore.WaitAsync(0)) return;

        try
        {
            while (!_serviceCts.Token.IsCancellationRequested)
            {
                lock (_lock)
                {
                    // remove tasks from the queue that are completed
                    var completed = _runningTaskContexts.Where(c => c.Task.IsCompleted).ToList();
                    foreach (var ctx in completed)
                    {
                        ctx.Cts.Dispose();
                        _runningTaskContexts.Remove(ctx);
                    }

                    if (_parallelismChangeTcs.Task.IsCompleted)
                    {
                        _parallelismChangeTcs = new TaskCompletionSource();
                    }
                }

                int currentCount;
                int limit;
                Task parallelismChangeTask;

                lock (_lock)
                {
                    // count of the tasks in the queue at the moment
                    currentCount = _runningTaskContexts.Count;

                    // limit of the tasks in the queue at the moment
                    limit = _maxParallelism;

                    parallelismChangeTask = _parallelismChangeTcs.Task;
                }

                if (currentCount >= limit)
                {
                    if (currentCount > 0)
                    {
                        Task[] tasksToWait;
                        lock (_lock)
                        {
                            tasksToWait = _runningTaskContexts.Select(c => c.Task).ToArray();
                        }

                        if (tasksToWait.Length > 0)
                        {
                            // include the task which completes when parallelism settings have been changed
                            // so when it completes we'll continue the loop and operate with the new limit
                            var allTasksToWait = tasksToWait.Append(parallelismChangeTask);
                            await Task.WhenAny(allTasksToWait);
                        }
                        continue;
                    }
                }

                // take an item from the queue, otherwise we break from the loop if the queue is empty
                if (!_queue.TryDequeue(out var item))
                {
                    break;
                }

                var internalCts = CancellationTokenSource.CreateLinkedTokenSource(_serviceCts.Token, item.Token);

                var processingTask = TryProcessItemAsync(item, internalCts);

                lock (_lock)
                {
                    // create a new queue task associated with the data, task and cancellationtoken so we can keep track of it
                    _runningTaskContexts.Add(new RunningTaskContext<T>
                    {
                        Item = item,
                        Task = processingTask,
                        Cts = internalCts
                    });
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task TryProcessItemAsync(T item, CancellationTokenSource linkedCts)
    {
        try
        {
            await ProcessItemAsync(item, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            OnItemCancelled(item);
        }
        catch (Exception ex)
        {
            OnItemFailed(item, ex);
        }
    }

    protected abstract Task ProcessItemAsync(T item, CancellationToken ct);
    protected virtual void OnItemCancelled(T item) {}
    protected virtual void OnItemFailed(T item, Exception ex) {}

    public void CancelAll()
    {
        _serviceCts.Cancel();
        _serviceCts.Dispose();
        _serviceCts = new CancellationTokenSource();
        _queue.Clear();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
