// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using avallama.Models;

namespace avallama.Services.Queue;

public interface IQueueService<T> : IDisposable where T : QueueItem
{
    void Enqueue(T item);
    IReadOnlyList<T> GetQueuedItems();
    void SetParallelism(int newCount);
    void CancelAll();
}

public abstract class QueueService<T> : IQueueService<T>
    where T : QueueItem
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly List<Task> _runningTasks = [];
    private CancellationTokenSource _serviceCts = new();
    private int _maxParallelism = 1;

    public void SetParallelism(int newCount)
    {
        if (newCount < 1) return;
        _maxParallelism = newCount;
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
        if (!await _semaphore.WaitAsync(0)) return;

        try
        {
            while (!_serviceCts.Token.IsCancellationRequested)
            {
                _runningTasks.RemoveAll(t => t.IsCompleted);

                var currentLimit = _maxParallelism;

                if (_runningTasks.Count >= currentLimit && _runningTasks.Count > 0)
                {
                    await Task.WhenAny(_runningTasks);
                    continue;
                }

                if (!_queue.TryDequeue(out var item))
                {
                    break;
                }

                var processingTask = TryProcessItemAsync(item, _serviceCts.Token);
                _runningTasks.Add(processingTask);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task TryProcessItemAsync(T item, CancellationToken serviceToken)
    {
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serviceToken, item.Token);
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
