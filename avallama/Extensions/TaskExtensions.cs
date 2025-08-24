using System;
using System.Threading;
using System.Threading.Tasks;

namespace avallama.Extensions;

/// <summary>
/// Task típusra bővítmények
/// </summary>
public static class TaskExtensions
{
    // Task<T>-re használható bővítmény. Ha a megadott timeout időtartamot túllépi akkor TimeoutException kivételt dob.
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        // cancellation token létrehozása, aminek megadjuk a timeout időtartamot
        // ez timeout időtartam elteltével visszavonja azt a Taskot amihez használva van a token
        using var cts = new CancellationTokenSource(timeout);
        var cancellationToken = cts.Token;
        
        // visszaadja azt a taskot amelyik hamarabb fejeződött be a kettő Task közül
        // a "task" érték itt az, amelyikre meghívtuk ezt a metódust
        // a másik Task pedig Timeout.Infinite értékig fut, de ez mindenkép lefog állni timeout időtartam után
        // a leállításról a cancellationToken gondoskodik
        var completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cancellationToken));

        // ha a befejezett task az amelyiket adtunk neki akkor visszaadjuk, ellenkező esetben TimeoutException kivétel lesz dobva
        if (completedTask == task)
        {
            return await task;
        }
        
        throw new TimeoutException();
    }
    
    // Task-re használható bővítmény. Ha a megadott timeout időtartamot túllépi akkor TimeoutException kivételt dob.
    public static async Task WithTimeout(this Task task, TimeSpan timeout)
    {
        // ugyanúgy működik mint az előző WithTimeout, csak annyi hogy ez nem generikus
        using var cts = new CancellationTokenSource(timeout);
        
        var completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));

        if (completedTask != task)
        {
            throw new TimeoutException();
        }
        
        await task;
    }
}

