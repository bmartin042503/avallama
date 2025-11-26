// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

// NoConnection jelezné azt ha nincs internetkapcsolat és nem letölthető a model
// ez egyértelműnek tűnik, és talán felesleges is lenne külön minden modelblock részére beállítani ezt az állapotot
// de arra gondoltam hogy ez így maradna ha esetleg később több forrásból (nem csak ollama) lehetne modelt letölteni
// és ha nem elérhető a szerver a letöltéshez akkor így legalább külön választhatóak
// tehát amit le tud tölteni azt letöltheti, amit nem az nem (de ez majd később)

public enum ModelDownloadStatus
{
    NotEnoughSpace, // nincs elég hely a letöltéshez
    NoConnection, // nincs kapcsolat a letöltéshez
    Ready, // letölthető
    Downloading,
    Paused, // letöltés szünetelés alatt áll
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
    [ObservableProperty] private long _size; // byteokban
    [ObservableProperty] private ModelDownloadStatus _downloadStatus;
    [ObservableProperty] private bool _runsSlow;
}
