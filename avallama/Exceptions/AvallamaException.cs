// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace avallama.Exceptions;

public abstract class AvallamaException : Exception
{
    protected AvallamaException(string message) : base(message) {}
    protected AvallamaException(string message, Exception? innerException = null) : base(message, innerException) {}
}
