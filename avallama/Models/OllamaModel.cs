// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

// NoConnection would indicate that there is no internet connection and the model cannot be downloaded.
// This seems obvious, and it might even be unnecessary to set this state separately for each model block,
// but I thought it would remain this way in case later on models could be downloaded from multiple sources (not just Ollama)
// and if the server is not reachable for downloading then at least they can be chosen separately
// so what it can download it can download, what it can't, it can't (but this will be later)

public enum ModelDownloadStatus
{
    NotEnoughSpace, // not enough space to download
    NoConnection, // no connection to download
    Ready, // can be downloaded
    Downloading,
    Paused, // download is paused
    Downloaded
}

public enum ModelDownloadAction
{
    Start,
    Pause,
    Resume,
    Cancel,
    Delete
}

public static class ModelInfoKey
{
    public const string Format = "format";
    public const string QuantizationLevel = "quantization_level";
    public const string Parameters = "parameters";
    public const string Architecture = "architecture";
    public const string BlockCount = "block_count";
    public const string ContextLength = "context_length";
    public const string EmbeddingLength = "embedding_length";
    public const string PullCount = "pull_count";
    public const string LastUpdated = "last_updated";
    public const string License = "license";
}

public partial class OllamaModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private long _parameters;
    [ObservableProperty] private IDictionary<string, string> _info = new Dictionary<string, string>();
    [ObservableProperty] private OllamaModelFamily? _family;
    [ObservableProperty] private long _size; // in bytes
    [ObservableProperty] private bool _runsSlow;
    [ObservableProperty] private ModelDownloadStatus _downloadStatus;
    [ObservableProperty] private int _downloadPartCount;
}
