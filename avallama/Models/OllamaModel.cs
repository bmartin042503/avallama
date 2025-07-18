using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public enum ModelDownloadStatus
{
    NotEnoughSpaceForDownload,
    ReadyForDownload, // ez jelöli azt ha nincs letöltve de letölthető
    Downloading,
    Downloaded
}

public enum ModelPerformanceStatus
{
    Unknown,
    InsufficientVram,
    RunsOkay,
    RunsGreat
}

public partial class OllamaModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private double _parameterSize; // milliardban megadva (pl. 8.0B)
    [ObservableProperty] private int _quantizationLevel; // 4-6-8 bites, vagy 0 ami jelentené hogy nem kvantált
    [ObservableProperty] private long _size; // byteokban
    
    [ObservableProperty] private ModelDownloadStatus _downloadStatus;
    [ObservableProperty] private double _downloadingProgress; // ha a status Downloading
    
    // csak ha lekérhetőek megfelelően az adatok, hogy futna-e a gépen
    // de szerintem erre biztos van valami C# könyvtár amivel le lehet kérni hogy mennyi VRAM van
    // és azt összehasonlítani a modellel, meg ilyesmi, de majd jobban megnézem én is
    // ha meg bonyolult lenne akkor max mást írhatnánk ide a modelről
    [ObservableProperty] private ModelPerformanceStatus _performanceStatus;
    [ObservableProperty] private double _generationSpeed; // a db-ből lekérve az átlagos generálási sebesség üzenetekre

    public OllamaModel(
        string name,
        double parameterSize,
        int quantizationLevel,
        long size,
        ModelDownloadStatus downloadStatus,
        ModelPerformanceStatus performanceStatus,
        double generationSpeed
    )
    {
        Name = name;
        ParameterSize = parameterSize;
        QuantizationLevel = quantizationLevel;
        Size = size;
        DownloadStatus = downloadStatus;
        PerformanceStatus = performanceStatus;
        GenerationSpeed = generationSpeed;
    }
}