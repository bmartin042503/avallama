using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public enum ModelDownloadStatus
{
    NotEnoughSpaceForDownload,
    ReadyForDownload,
    Downloading,
    Downloaded
}

public enum ModelPerformanceStatus
{
    InsufficientVram,
    RunsOkay,
    RunsGreat
}

public partial class OllamaModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private double _parameterSize; // milliardban megadva (pl. 8.0B)
    [ObservableProperty] private string _quantizationLevel = string.Empty;
    [ObservableProperty] private bool _downloaded = false;
    [ObservableProperty] private long _size; // byteokban
    [ObservableProperty] private ModelDownloadStatus _downloadStatus;
    
    // csak ha lekérhetőek megfelelően az adatok, hogy futna-e a gépen
    // de szerintem erre biztos van valami C# könyvtár amivel le lehet kérni hogy mennyi VRAM van
    // és azt összehasonlítani a modellel, meg ilyesmi, de majd jobban megnézem én is
    // ha meg bonyolult lenne akkor max mást írhatnánk ide a modelről
    [ObservableProperty] private ModelPerformanceStatus _performanceStatus;
    
    [ObservableProperty] private double _downloadingProgress; // ha a status Downloading

    public OllamaModel(
        string name,
        double parameterSize,
        string quantizationLevel,
        bool downloaded,
        long size,
        ModelDownloadStatus downloadStatus,
        ModelPerformanceStatus performanceStatus
    )
    {
        Name = name;
        ParameterSize = parameterSize;
        QuantizationLevel = quantizationLevel;
        Downloaded = downloaded;
        Size = size;
        DownloadStatus = downloadStatus;
        PerformanceStatus = performanceStatus;
    }
}