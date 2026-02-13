// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models.Ollama;

/// <summary>
/// Represents a specific Ollama model with observable properties for UI data binding.
/// </summary>
public partial class OllamaModel : ObservableObject
{
    /// <summary>
    /// The unique name or identifier of the model (e.g., 'qwen3:4b').
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// The number of parameters in the model.
    /// </summary>
    [ObservableProperty]
    private long _parameters;

    /// <summary>
    /// A dictionary containing additional metadata about the model (e.g., quantization, format, block count).
    /// </summary>
    [ObservableProperty]
    private IDictionary<string, string> _info = new Dictionary<string, string>();

    /// <summary>
    /// The family to which this model belongs.
    /// </summary>
    [ObservableProperty]
    private OllamaModelFamily? _family;

    /// <summary>
    /// The size of the model on disk in bytes.
    /// </summary>
    [ObservableProperty]
    private long _size;

    /// <summary>
    /// Indicates whether the model is expected to run slowly on the current hardware (e.g., due to VRAM limitations).
    /// </summary>
    [ObservableProperty]
    private bool _runsSlow;

    /// <summary>
    /// Indicates whether the model is fully downloaded and available for use.
    /// </summary>
    [ObservableProperty]
    private bool _isDownloaded;
}
