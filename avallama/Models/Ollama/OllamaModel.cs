// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models.Ollama;

// NoConnection would indicate that there is no internet connection and the model cannot be downloaded.
// This seems obvious, and it might even be unnecessary to set this state separately for each model block,
// but I thought it would remain this way in case later on models could be downloaded from multiple sources (not just Ollama)
// and if the server is not reachable for downloading then at least they can be chosen separately
// so what it can download it can download, what it can't, it can't (but this will be later)

public partial class OllamaModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private long _parameters;
    [ObservableProperty] private IDictionary<string, string> _info = new Dictionary<string, string>();
    [ObservableProperty] private OllamaModelFamily? _family;
    [ObservableProperty] private long _size; // in bytes
    [ObservableProperty] private bool _runsSlow;
    [ObservableProperty] private bool _isDownloaded;
}
