using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using avallama.Utilities;
using Xunit;

namespace avallama.Tests.Utilities;

[Collection("Database Tests")]
public class DatabaseLockTests
{
    [Fact]
    public async Task DatabaseLock_Allows_MultipleReaders()
    {
        var lockInstance = DatabaseLock.Instance;
        var concurrent = 0;
        var maxConcurrent = 0;
        var allStarted = new CountdownEvent(10);

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            using (lockInstance.AcquireReadLock())
            {
                var cur = Interlocked.Increment(ref concurrent);
                allStarted.Signal();

                SpinWait.SpinUntil(() =>
                {
                    var prev = Volatile.Read(ref maxConcurrent);
                    return cur <= prev || Interlocked.CompareExchange(ref maxConcurrent, cur, prev) == prev;
                });

                allStarted.Wait(TimeSpan.FromSeconds(5));
                Thread.Sleep(50);

                Interlocked.Decrement(ref concurrent);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.True(maxConcurrent >= 2, $"Expected at least 2 concurrent readers, got {maxConcurrent}");
    }

    [Fact]
    public async Task DatabaseLock_WriteLock_IsExclusive()
    {
        var lockInstance = DatabaseLock.Instance;
        var writerStarted = new TaskCompletionSource();
        var writerRelease = new TaskCompletionSource();

        var writer = Task.Run(() =>
        {
            using (lockInstance.AcquireWriteLock())
            {
                writerStarted.SetResult();
                writerRelease.Task.Wait();
            }
        });

        await writerStarted.Task;

        var readerAcquired = false;
        var reader = Task.Run(() =>
        {
            using (lockInstance.AcquireReadLock())
            {
                readerAcquired = true;
            }
        });

        await Task.Delay(100);
        Assert.False(readerAcquired, "Reader should not acquire lock while writer holds it.");

        writerRelease.SetResult();
        await Task.WhenAll(writer, reader);
        Assert.True(readerAcquired, "Reader should acquire lock after writer releases it.");
    }

    [Fact]
    public void DatabaseLock_IsSingleton()
    {
        var instance1 = DatabaseLock.Instance;
        var instance2 = DatabaseLock.Instance;

        Assert.Same(instance1, instance2);
    }
}
