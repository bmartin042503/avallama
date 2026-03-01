// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Tests.Mocks;
using Xunit;

namespace avallama.Tests.Services.Queue;

public class QueueServiceTests : IDisposable
{
    private readonly TestQueueService _service = new();

    public void Dispose()
    {
        _service.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Enqueue_ShouldProcessItemSuccessfully()
    {
        var item = new TestQueueItem();

        _service.Enqueue(item);
        _service.CompleteItem(item);

        Assert.Empty(_service.GetQueuedItems());
        Assert.True(_service.ProcessingBlockers.ContainsKey(item.Id));
    }

    [Fact]
    public void Enqueue_ShouldRespectMaxParallelism()
    {
        _service.SetParallelism(1);
        var item1 = new TestQueueItem();
        var item2 = new TestQueueItem();

        _service.Enqueue(item1);
        _service.Enqueue(item2);

        var queued = _service.GetQueuedItems();
        Assert.Single(queued);
        Assert.Equal(item2, queued[0]);
        Assert.False(_service.ProcessingBlockers.ContainsKey(item2.Id));
    }

    [Fact]
    public void SetParallelism_Increase_ShouldStartWaitingItems()
    {
        _service.SetParallelism(1);
        var item1 = new TestQueueItem();
        var item2 = new TestQueueItem();

        _service.Enqueue(item1);
        _service.Enqueue(item2);

        _service.SetParallelism(2);

        Assert.Empty(_service.GetQueuedItems());

        Assert.True(_service.ProcessingBlockers.ContainsKey(item1.Id));
        Assert.True(_service.ProcessingBlockers.ContainsKey(item2.Id));
    }

    [Fact]
    public void SetParallelism_Decrease_ShouldCancelExcessItemsWithSystemScalingReason()
    {
        _service.SetParallelism(2);
        var item1 = new TestQueueItem();
        var item2 = new TestQueueItem();

        _service.Enqueue(item1);
        _service.Enqueue(item2);

        _service.SetParallelism(1);

        var canceledItem = _service.CanceledItems.First();

        Assert.Equal(item2, canceledItem);
        Assert.Equal(QueueItemCancellationReason.SystemScaling, canceledItem.QueueItemCancellationReason);
    }

    [Fact]
    public void QueueItem_Cancel_ShouldCancelOnlySpecificItem()
    {
        _service.SetParallelism(2);
        var item1 = new TestQueueItem();
        var item2 = new TestQueueItem();

        _service.Enqueue(item1);
        _service.Enqueue(item2);

        item1.QueueItemCancellationReason = QueueItemCancellationReason.UserCancelRequest;
        item1.Cancel();

        Assert.Contains(item1, _service.CanceledItems);
        Assert.DoesNotContain(item2, _service.CanceledItems);

        _service.CompleteItem(item2);
        Assert.Contains(item2, _service.ProcessedItems);
    }

    [Fact]
    public void CancelAll_ShouldClearQueueAndCancelAllRunningItems()
    {
        _service.SetParallelism(1);
        var item1 = new TestQueueItem();
        var item2 = new TestQueueItem();

        _service.Enqueue(item1);
        _service.Enqueue(item2);

        _service.CancelAll();

        Assert.Empty(_service.GetQueuedItems());
        Assert.Contains(item1, _service.CanceledItems);
        Assert.Contains(item2, _service.CanceledItems);
    }

    [Fact]
    public void ProcessItem_OnException_ShouldTriggerOnItemFailed()
    {
        _service.ThrowExceptionOnProcess = true;
        var item = new TestQueueItem();

        _service.Enqueue(item);

        var failedItem = _service.FailedItems.First(f => f.Item == item);
        Assert.IsType<InvalidOperationException>(failedItem.Exception);
    }

    [Fact]
    public async Task CompleteItem_ShouldAutomaticallyStartNextQueuedItem()
    {
        _service.SetParallelism(1);
        var item1 = new TestQueueItem();
        var item2 = new TestQueueItem();

        _service.Enqueue(item1);
        _service.Enqueue(item2);

        Assert.DoesNotContain(item1, _service.GetQueuedItems());
        Assert.Contains(item2, _service.GetQueuedItems());

        _service.CompleteItem(item1);

        // TODO: remove after queue service is more testable
        await Task.Delay(100);

        Assert.DoesNotContain(item1, _service.GetQueuedItems());
        Assert.DoesNotContain(item2, _service.GetQueuedItems());

        Assert.Contains(item1, _service.ProcessedItems);

        _service.CompleteItem(item2);

        Assert.Contains(item2, _service.ProcessedItems);
    }

    [Fact]
    public void SetParallelism_LessThanOne_ShouldBeIgnored()
    {
        _service.SetParallelism(2);
        var item1 = new TestQueueItem();
        var item2 = new TestQueueItem();
        var item3 = new TestQueueItem();

        _service.SetParallelism(0);
        _service.SetParallelism(-5);

        _service.Enqueue(item1);
        _service.Enqueue(item2);
        _service.Enqueue(item3);

        var queued = _service.GetQueuedItems();
        Assert.Single(queued);
        Assert.Equal(item3, queued[0]);
    }

    [Fact]
    public async Task ResetToken_ShouldAllowItemToBeRequeuedAfterCancellation()
    {
        var item = new TestQueueItem();
        _service.Enqueue(item);

        item.Cancel();
        item.ResetToken();

        _service.Enqueue(item);

        // TODO: remove after queue service is more testable
        await Task.Delay(100);

        _service.CompleteItem(item);

        Assert.Contains(item, _service.ProcessedItems);
    }
}
