// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using avallama.Models;
using avallama.Services;
using avallama.Utilities;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace avallama.Styles.TemplatedControls;

public class ModelItem : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelItem, string?>(nameof(Title));

    public static readonly StyledProperty<string?> InformationProperty =
        AvaloniaProperty.Register<ModelItem, string?>(nameof(Information));

    public static readonly DirectProperty<ModelItem, OllamaModelFamily?> FamilyProperty =
        AvaloniaProperty.RegisterDirect<ModelItem, OllamaModelFamily?>(
            nameof(Family),
            o => o.Family,
            (o, v) => o.Family = v
        );

    public static readonly DirectProperty<ModelItem, double?> ParametersProperty =
        AvaloniaProperty.RegisterDirect<ModelItem, double?>(
            nameof(Parameters),
            o => o.Parameters,
            (o, v) => o.Parameters = v
        );

    public static readonly DirectProperty<ModelItem, IDictionary<string, string>?> InformationSourceProperty =
        AvaloniaProperty.RegisterDirect<ModelItem, IDictionary<string, string>?>(
            nameof(InformationSource),
            o => o.InformationSource,
            (o, v) => o.InformationSource = v
        );

    public static readonly DirectProperty<ModelItem, long> SizeInBytesProperty =
        AvaloniaProperty.RegisterDirect<ModelItem, long>(
            nameof(SizeInBytes),
            o => o.SizeInBytes,
            (o, v) => o.SizeInBytes = v,
            unsetValue: 0
        );

    public static readonly DirectProperty<ModelItem, ModelDownloadStatus> DownloadStatusProperty =
        AvaloniaProperty.RegisterDirect<ModelItem, ModelDownloadStatus>(
            nameof(DownloadStatus),
            o => o.DownloadStatus,
            (o, v) => o.DownloadStatus = v
        );

    // Mbps
    public static readonly DirectProperty<ModelItem, double?> DownloadSpeedProperty =
        AvaloniaProperty.RegisterDirect<ModelItem, double?>(
            nameof(DownloadSpeed),
            o => o.DownloadSpeed,
            (o, v) => o.DownloadSpeed = v
        );

    public static readonly DirectProperty<ModelItem, long> DownloadedBytesProperty =
        AvaloniaProperty.RegisterDirect<ModelItem, long>(
            nameof(DownloadedBytes),
            o => o.DownloadedBytes,
            (o, v) => o.DownloadedBytes = v,
            unsetValue: 0
        );

    public static readonly DirectProperty<ModelItem, bool?> RunsSlowProperty =
        AvaloniaProperty.RegisterDirect<ModelItem, bool?>(
            nameof(RunsSlow),
            o => o.RunsSlow,
            (o, v) => o.RunsSlow = v
        );

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelItem, ICommand>(nameof(Command));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Information
    {
        get => GetValue(InformationProperty);
        set => SetValue(InformationProperty, value);
    }

    private double? _parameters;

    public double? Parameters
    {
        get => _parameters;
        set => SetAndRaise(ParametersProperty, ref _parameters, value);
    }

    private OllamaModelFamily? _family;

    public OllamaModelFamily? Family
    {
        get => _family;
        set => SetAndRaise(FamilyProperty, ref _family, value);
    }

    private IDictionary<string, string>? _informationSource;

    public IDictionary<string, string>? InformationSource
    {
        get => _informationSource;
        set => SetAndRaise(InformationSourceProperty, ref _informationSource, value);
    }

    private long _sizeInBytes;

    public long SizeInBytes
    {
        get => _sizeInBytes;
        set => SetAndRaise(SizeInBytesProperty, ref _sizeInBytes, value);
    }

    private ModelDownloadStatus _downloadStatus;

    public ModelDownloadStatus DownloadStatus
    {
        get => _downloadStatus;
        set => SetAndRaise(DownloadStatusProperty, ref _downloadStatus, value);
    }

    private double? _downloadSpeed;

    public double? DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetAndRaise(DownloadSpeedProperty, ref _downloadSpeed, value);
    }

    private long _downloadedBytes;

    public long DownloadedBytes
    {
        get => _downloadedBytes;
        set => SetAndRaise(DownloadedBytesProperty, ref _downloadedBytes, value);
    }

    private bool? _runsSlow;

    public bool? RunsSlow
    {
        get => _runsSlow;
        set => SetAndRaise(RunsSlowProperty, ref _runsSlow, value);
    }

    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == InformationSourceProperty)
        {
            if (InformationSource != null)
            {
                // Add pull count from family to model info
                InformationSource[LocalizationService.GetString("PULL_COUNT")] =
                    ConversionHelper.FormatToAbbreviatedNumber(Family?.PullCount ?? 0);

                // Add last updated date from family to model info
                InformationSource[LocalizationService.GetString("LAST_UPDATED")] =
                    Family?.LastUpdated.ToString("yyyy-MM-dd") ?? string.Empty;

                Information = string.Join('\n', InformationSource.Select(kv => $"{kv.Key}: {kv.Value}"));
            }
        }
    }
}

