// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;

namespace avallama.Utilities;

public sealed class DatabaseLock
{
    // Singleton instance
    private static readonly Lazy<DatabaseLock> _instance = new(() => new DatabaseLock());
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    public static DatabaseLock Instance => _instance.Value;

    public IDisposable AcquireReadLock()
    {
        _lock.EnterReadLock();
        return new DisposableAction(() => _lock.ExitReadLock());
    }

    public IDisposable AcquireWriteLock()
    {
        _lock.EnterWriteLock();
        return new DisposableAction(() => _lock.ExitWriteLock());
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));
        public void Dispose() => _action();
    }
}
