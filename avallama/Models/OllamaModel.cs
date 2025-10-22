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

public enum ModelDownloadAction
{
    Start,
    Pause,
    Resume,
    Cancel,
    Delete
}

public partial class OllamaModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private long _parameters;
    [ObservableProperty] private IDictionary<string, string> _info = new Dictionary<string, string>();
    [ObservableProperty] private OllamaModelFamily? _family;
    [ObservableProperty] private long _size; // byteokban
    [ObservableProperty] private ModelDownloadStatus _downloadStatus;
    [ObservableProperty] private double _downloadProgress; // ha a status Downloading, 0.0 és 1.0 közötti érték
    [ObservableProperty] private bool _runsSlow;
}
