// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Text.Json;

namespace avallama.Extensions;

public static class JsonElementExtensions
{
    public static bool TryGetString(this JsonElement element, out string? value)
    {
        try { value = element.GetString(); return true; }
        catch { value = null; return false; }
    }
}
