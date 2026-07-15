// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace avallama.DataTemplates;

public interface IStatefulTemplateItem
{
    string GetCurrentStateKey();
}

public class StatefulTemplateSelector : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new();

    public Control? Build(object? param)
    {
        if (param is not IStatefulTemplateItem item) return null;

        var key = item.GetCurrentStateKey();
        return AvailableTemplates.TryGetValue(key, out var template) ? template.Build(param) : null;
    }

    public bool Match(object? data)
    {
        if (data is not IStatefulTemplateItem item) return false;

        var key = item.GetCurrentStateKey();
        return !string.IsNullOrEmpty(key) && AvailableTemplates.ContainsKey(key);
    }
}
