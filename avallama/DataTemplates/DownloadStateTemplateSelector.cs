// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using avallama.Constants;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace avallama.DataTemplates;

public class DownloadStateTemplateSelector : IDataTemplate
{

    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new();

    public Control? Build(object? param)
    {
        var key = param?.ToString();
        return key is null ? throw new ArgumentNullException(nameof(param)) : AvailableTemplates[key].Build(param);
    }

    public bool Match(object? data)
    {
        // Our Keys in the dictionary are strings, so we call .ToString() to get the key to look up
        var key = data?.ToString();

        return data is DownloadState
               && !string.IsNullOrEmpty(key)
               && AvailableTemplates.ContainsKey(key);
    }
}
