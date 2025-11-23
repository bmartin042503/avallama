// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Utilities;

namespace avallama.Tests;

public class SynchronousAvaloniaDispatcher : IAvaloniaDispatcher
{
    public void Post(Action action)
    {
        action();
    }

    public bool CheckAccess()
    {
        return true;
    }
}
