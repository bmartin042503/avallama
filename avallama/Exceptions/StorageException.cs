// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace avallama.Exceptions;

public abstract class StorageException(string message, Exception? inner = null) : AvallamaException(message, inner);

public class InsufficientDiskSpaceException(long requiredBytes, long availableBytes)
    : StorageException($"Not enough disk space. Required: {requiredBytes}, available: {availableBytes}.")
{
    public long RequiredBytes { get; } = requiredBytes;
    public long AvailableBytes { get; } = availableBytes;
}
public class DiskFullException(string message, Exception? inner = null) : StorageException(message, inner);
