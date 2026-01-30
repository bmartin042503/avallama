// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using avallama.Models.Download;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace avallama.DataTemplates;

public class ModelDownloadStatusTemplateSelector : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new();

    public Control? Build(object? param)
    {
        if (param is not ModelDownloadStatus status)
        {
            return null;
        }

        var key = status.DownloadState.ToString();

        return AvailableTemplates.TryGetValue(key, out var template) ? template.Build(param) : null;
    }

    public bool Match(object? data)
    {
        if (data is not ModelDownloadStatus status) return false;
        var key = status.DownloadState.ToString();
        return !string.IsNullOrEmpty(key) && AvailableTemplates.ContainsKey(key);

    }
}
