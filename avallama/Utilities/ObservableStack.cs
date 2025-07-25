using System;
using System.Collections.ObjectModel;

namespace avallama.Utilities;

public class ObservableStack<T> : ObservableCollection<T>
{
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