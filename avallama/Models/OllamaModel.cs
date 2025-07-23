// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

// NoConnectionForDownload jelezné azt ha nincs internetkapcsolat és nem letölthető a model
// ez egyértelműnek tűnik, és talán felesleges is lenne külön minden modelblock részére beállítani ezt az állapotot
// de arra gondoltam hogy ez így maradna ha esetleg később több forrásból (nem csak ollama) lehetne modelt letölteni
// és ha nem elérhető a szerver a letöltéshez akkor így legalább külön választhatóak
// tehát amit le tud tölteni azt letöltheti, amit nem az nem (de ez majd később)
public enum ModelDownloadStatus
{
    NotEnoughSpaceForDownload,
    NoConnectionForDownload,
    ReadyForDownload, // ez jelöli azt ha nincs letöltve de letölthető
    Downloading,
    Downloaded
}

public enum ModelLabelHighlight
{
    Default,
    Strong
}

public class ModelLabel()
{
    public ModelLabelHighlight Highlight { get; set; }
    public string Name { get; set; } = string.Empty;

    public ModelLabel(string name, ModelLabelHighlight highlight = ModelLabelHighlight.Default) : this()
    {
        Name = name;
        Highlight = highlight;
    }
}

public partial class OllamaModel : ObservableObject
{
    // model neve
    [ObservableProperty] private string _name = string.Empty;
    
    // A detailshez és a labelshez lehetne hozzáadni azokat az adatokat a modelhez amik elérhetőek
    // pl. parameters, quantization stb. dinamikusan
    
    // a modelblock részletei, pl. 'Parameters' '3.25B' stb.
    // azért string hogy lehessen majd előtte lokalizált formában hozzáadni
    [ObservableProperty] private IDictionary<string, string> _details;
    
    // a modelblock címkéi, Label típusokban
    // ez azt jelentené hogy a modelhez kapcsolódó kisebb infókat pl. gyorsaság, futtathatóság kisebb címkékben jelenítené meg
    // LabelHighlight, vagyis különböző kiemeléssel, (pl. insufficientvram kapna egy erősebb 'Strong' címkét)
    [ObservableProperty] private IEnumerable<ModelLabel> _labels;
    
    [ObservableProperty] private long _size; // byteokban
    [ObservableProperty] private ModelDownloadStatus _downloadStatus;
    [ObservableProperty] private double _downloadProgress; // ha a status Downloading

    public OllamaModel(
        string name,
        IDictionary<string, string> details,
        IEnumerable<ModelLabel> labels,
        long size,
        ModelDownloadStatus downloadStatus
    )
    {
        Name = name;
        Details = details;
        Labels = labels;
        Size = size;
        DownloadStatus = downloadStatus;
    }

}