// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Services.Queue;

namespace avallama.Tests.Mocks;

public class TestQueueItem : QueueItem
{
    public string Id { get; } = Guid.NewGuid().ToString();
}

public class TestQueueService : QueueService<TestQueueItem>
{
    public ConcurrentDictionary<string, TaskCompletionSource> ProcessingBlockers { get; } = new();

    public ConcurrentBag<TestQueueItem> ProcessedItems { get; } = [];
    public ConcurrentBag<TestQueueItem> CanceledItems { get; } = [];
    public ConcurrentBag<(TestQueueItem Item, Exception Exception)> FailedItems { get; } = [];

    public bool ThrowExceptionOnProcess { get; set; }

    protected override async Task ProcessItemAsync(TestQueueItem item, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();
        ProcessingBlockers[item.Id] = tcs;

        if (ThrowExceptionOnProcess)
        {
            throw new InvalidOperationException("Simulated failure.");
        }

        await using (ct.Register(() => tcs.TrySetCanceled(ct)))
        {
            await tcs.Task;
        }

        ProcessedItems.Add(item);
    }

    protected override void OnItemCanceled(TestQueueItem item)
    {
        CanceledItems.Add(item);
        base.OnItemCanceled(item);
    }

    protected override void OnItemFailed(TestQueueItem item, Exception ex)
    {
        FailedItems.Add((item, ex));
        base.OnItemFailed(item, ex);
    }

    public void CompleteItem(TestQueueItem item)
    {
        if (ProcessingBlockers.TryGetValue(item.Id, out var tcs))
        {
            tcs.TrySetResult();
        }
    }
}
