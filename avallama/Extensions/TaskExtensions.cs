// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace avallama.Extensions;

/// <summary>
/// Task típusra bővítmények
/// </summary>
public static class TaskExtensions
{
    // Task<T>-re és Task-ra használható bővítmény. Ha a megadott timeout időtartamot túllépi akkor TimeoutException kivételt dob.
    
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        // visszaadja azt a taskot amelyik hamarabb fejeződött be a kettő Task közül
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout));

        // ha a befejezett task az amelyiket adtunk neki akkor visszaadjuk, ellenkező esetben TimeoutException kivétel lesz dobva
        if (completedTask == task)
        {
            return await task;
        }
        
        throw new TimeoutException();
    }
    
    public static async Task WithTimeout(this Task task, TimeSpan timeout)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout));

        if (completedTask != task)
        {
            throw new TimeoutException();
        }
        
        await task;
    }
}

