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
using avallama.Models.Download;

namespace avallama.Services.Queue;

/// <summary>
/// Defines a contract for a service that manages a queue of items to be processed concurrently.
/// </summary>
/// <typeparam name="T">The type of items in the queue, which must inherit from <see cref="QueueItem"/>.</typeparam>
public interface IQueueService<T> : IDisposable where T : QueueItem
{
    /// <summary>
    /// Adds an item to the end of the queue and triggers processing.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    void Enqueue(T item);

    /// <summary>
    /// Retrieves a snapshot of the items currently waiting in the queue.
    /// </summary>
    /// <returns>A read-only list of queued items.</returns>
    IReadOnlyList<T> GetQueuedItems();

    /// <summary>
    /// Adjusts the maximum number of items that can be processed simultaneously.
    /// </summary>
    /// <param name="newCount">The new maximum concurrency level. Must be at least 1.</param>
    void SetParallelism(int newCount);

    /// <summary>
    /// Cancels all currently processing items and clears the queue.
    /// </summary>
    void CancelAll();
}

/// <summary>
/// Provides a base implementation for an asynchronous, parallel queue processing service.
/// </summary>
/// <typeparam name="T">The type of items in the queue, which must inherit from <see cref="QueueItem"/>.</typeparam>
public abstract class QueueService<T> : IQueueService<T>
    where T : QueueItem
{
    /// <summary>
    /// Represents the context of a currently executing task within the queue service.
    /// </summary>
    private class RunningTaskContext
    {
        /// <summary>
        /// Gets the queue item being processed.
        /// </summary>
        public required T Item { get; init; }

        /// <summary>
        /// Gets the task representing the asynchronous processing operation.
        /// </summary>
        public required Task Task { get; init; }

        /// <summary>
        /// Gets the linked cancellation token source for this specific execution context.
        /// </summary>
        public required CancellationTokenSource Cts { get; init; }
    }

    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly List<RunningTaskContext> _runningTaskContexts = [];
    private CancellationTokenSource _serviceCts = new();
    private TaskCompletionSource _parallelismChangeTcs = new();
    private int _maxParallelism = 1;
    private readonly Lock _lock = new();

    /// <inheritdoc />
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

                // cancel excess tasks
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

    /// <inheritdoc />
    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        _ = ProcessQueueAsync();
    }

    /// <inheritdoc />
    public IReadOnlyList<T> GetQueuedItems() => _queue.ToList();

    /// <summary>
    /// The core loop that continuously monitors the queue and dispatches items to be processed
    /// while respecting the configured concurrency limits.
    /// </summary>
    /// <returns>A task representing the asynchronous queue processing loop.</returns>
    private async Task ProcessQueueAsync()
    {
        // ensures that only one instance of the processing loop is running at a time
        if (!await _semaphore.WaitAsync(0)) return;

        try
        {
            while (IsServiceActive())
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
                Task[] tasksToWait;

                lock (_lock)
                {
                    currentCount = _runningTaskContexts.Count;
                    limit = _maxParallelism;
                    parallelismChangeTask = _parallelismChangeTcs.Task;
                    tasksToWait = _runningTaskContexts.Select(c => c.Task).ToArray();
                }

                if (currentCount >= limit && currentCount > 0)
                {
                    // if we reached the limit and there are running tasks, wait for them or wait for parallelism to change
                    if (tasksToWait.Length > 0)
                    {
                        // include the task which completes when parallelism settings have been changed
                        // so when it completes we'll continue the loop and operate with the new limit
                        var allTasksToWait = tasksToWait.Append(parallelismChangeTask);
                        await Task.WhenAny(allTasksToWait);
                    }
                    continue;
                }

                // try to take an item from the queue, if empty we break from the loop
                if (!_queue.TryDequeue(out var item))
                {
                    break;
                }

                var internalCts = CancellationTokenSource.CreateLinkedTokenSource(_serviceCts.Token, item.Token);

                // do not pass the cancellation token since we continue with a cleaning code which has to run always
                // this is necessary in case the cleaning is not executed if we breaked the loop already
                var processingTask = TryProcessItemAsync(item, internalCts).ContinueWith(_ =>
                {
                    lock (_lock)
                    {
                        var ctx = _runningTaskContexts.FirstOrDefault(c => c.Item == item);
                        if (ctx == null) return;
                        ctx.Cts.Dispose();
                        _runningTaskContexts.Remove(ctx);
                    }
                });

                lock (_lock)
                {
                    // creates a new queue task associated with the data, task and cancellation token so we can keep track of it
                    _runningTaskContexts.Add(new RunningTaskContext
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

    /// <summary>
    /// Safely determines whether the overall queue service is still active and has not been canceled.
    /// Utilizes a lock to ensure the cancellation token state is read in a thread-safe manner.
    /// </summary>
    /// <returns><c>True</c> if the service is active and running; otherwise, <c>False</c>.</returns>
    private bool IsServiceActive()
    {
        lock (_lock)
        {
            return !_serviceCts.IsCancellationRequested;
        }
    }

    /// <summary>
    /// Safely attempts to process a queue item, handling standard cancellation and execution exceptions.
    /// </summary>
    /// <param name="item">The item to process.</param>
    /// <param name="linkedCts">The linked cancellation token source to monitor for cancellation.</param>
    /// <returns>A task representing the processing operation.</returns>
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

    /// <summary>
    /// When overridden in a derived class, contains the actual processing logic for a single queue item.
    /// </summary>
    /// <param name="item">The item to process.</param>
    /// <param name="ct">The cancellation token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task ProcessItemAsync(T item, CancellationToken ct);

    /// <summary>
    /// Invoked when an item's processing is canceled via an <see cref="OperationCanceledException"/>.
    /// </summary>
    /// <param name="item">The item that was canceled.</param>
    protected virtual void OnItemCancelled(T item) {}

    /// <summary>
    /// Invoked when an item's processing throws an unhandled exception.
    /// </summary>
    /// <param name="item">The item that failed.</param>
    /// <param name="ex">The exception that occurred during processing.</param>
    protected virtual void OnItemFailed(T item, Exception ex) {}

    /// <inheritdoc />
    public void CancelAll()
    {
        lock (_lock)
        {
            _serviceCts.Cancel();
            _serviceCts.Dispose();
            _serviceCts = new CancellationTokenSource();
        }
        _queue.Clear();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="QueueService{T}"/>.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _serviceCts.Cancel();
            _serviceCts.Dispose();
            _queue.Clear();
        }

        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
