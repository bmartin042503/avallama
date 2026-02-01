// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace avallama.Utilities;

public class ObservableStack<T> : ObservableCollection<T>
{
    public ObservableStack() {}
    public ObservableStack(IEnumerable<T> collection) : base(collection) {}

    public void Push(T item)
    {
        Insert(0, item);
    }

    public T Pop()
    {
        if (Count == 0)
            throw new InvalidOperationException("Stack is empty");

        var item = this[0];
        RemoveAt(0);
        return item;
    }

    public T Peek()
    {
        if (Count == 0)
            throw new InvalidOperationException("Stack is empty");

        return this[0];
    }
}
