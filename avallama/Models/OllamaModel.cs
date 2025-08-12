// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Tmds.DBus.Protocol;

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

public partial class OllamaModel : ObservableObject
{
    // model neve
    [ObservableProperty] private string _name;
    [ObservableProperty] private int? _quantization;
    [ObservableProperty] private double? _parameters;

    // pl. GGUF, MLX stb.
    [ObservableProperty] private string? _format = string.Empty;

    // modell részletei szótárban, például
    // 'General architecture:' -> 'llama'
    // 'Context length:' -> '8192'
    // stb.
    [ObservableProperty] private IDictionary<string, string>? _details;

    [ObservableProperty] private long _size; // byteokban
    [ObservableProperty] private ModelDownloadStatus _downloadStatus;
    [ObservableProperty] private double _downloadProgress; // ha a status Downloading, 0.0 és 1.0 közötti érték

    // ha nincs elég vram vagy bármi hasonló ami miatt lassan futhat akkor ezt true-ra kell állítani
    [ObservableProperty] private bool _runsSlow;

    public OllamaModel(
        string name,
        int quantization,
        double parameters,
        string format,
        IDictionary<string, string> details,
        long size,
        ModelDownloadStatus downloadStatus,
        bool runsSlow
    )
    {
        Name = name;
        Quantization = quantization;
        Parameters = parameters;
        Format = format;
        Details = details;
        Size = size;
        DownloadStatus = downloadStatus;
        RunsSlow = runsSlow;
    }

    public OllamaModel()
    {
        Name = string.Empty;
        Quantization = null;
        Parameters = double.NaN;
        Format = string.Empty;
        Details = null;
    }
}