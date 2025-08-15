// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Windows.Input;
using avallama.Constants;
using avallama.Models;
using avallama.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Svg;
using Avalonia.Threading;
using ShimSkiaSharp;

namespace avallama.Controls;

// TODO:
// Paused svg renderelése
// felmérni hogy milyen a teljesítmény, különböző render metódusok mennyiszer hívódnak meg, mennyi ramot fogyaszt és allokál

public class ModelBlock : Control
{
    // StyledProperty - belekerül az Avalonia styles rendszerébe így például írhatunk rá stílusokat stb.
    // DirectProperty - nem kerül bele, megadott egyszerű értékeknek, jobb teljesítmény (kell getter, setter)
    
    // azért Title a property neve, mert a 'Name' már le van foglalva és szerintem összekavarodna
    // de ez a Model nevét tartalmazza amúgy
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelBlock, string?>(nameof(Title));

    public static readonly DirectProperty<ModelBlock, ModelDownloadStatus> DownloadStatusProperty =
        AvaloniaProperty.RegisterDirect<ModelBlock, ModelDownloadStatus>(
            nameof(DownloadStatus),
            o => o.DownloadStatus,
            (o, v) => o.DownloadStatus = v
        );

    public static readonly DirectProperty<ModelBlock, double?> DownloadProgressProperty =
        AvaloniaProperty.RegisterDirect<ModelBlock, double?>(
            nameof(DownloadProgress),
            o => o.DownloadProgress,
            (o, v) => o.DownloadProgress = v
        );

    public static readonly DirectProperty<ModelBlock, string?> SelectedNameProperty =
        AvaloniaProperty.RegisterDirect<ModelBlock, string?>(
            nameof(SelectedName),
            o => o.SelectedName,
            (o, v) => o.SelectedName = v,
            unsetValue: string.Empty
        );
    
    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelInfoBlock, ICommand>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ModelInfoBlock, object?>(nameof(CommandParameter));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private ModelDownloadStatus _downloadStatus;
    public ModelDownloadStatus DownloadStatus
    {
        get => _downloadStatus;
        set => SetAndRaise(DownloadStatusProperty, ref _downloadStatus, value);
    }

    private double? _downloadProgress;
    public double? DownloadProgress
    {
        get => _downloadProgress;
        set => SetAndRaise(DownloadProgressProperty, ref _downloadProgress, value);
    }

    private string? _selectedName;

    public string? SelectedName
    {
        get => _selectedName;
        set => SetAndRaise(SelectedNameProperty, ref _selectedName, value);
    }
    
    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private static readonly Thickness BasePadding = new(10,8);
    private static readonly CornerRadius BaseCornerRadius = new(6);
    private const double TitleFontSize = 14;
    private const double DownloadTextFontSize = 14;

    private TextLayout? _titleTextLayout;
    private TextLayout? _downloadTextLayout;
    
    // ebben lesz az svg
    private AvaloniaPicture? _svg;
    private SKPicture? _downloadedSkPicture;
    private SKPicture? _spinnerSkPicture;
    private SKPicture? _pauseSkPicture; // TODO: implementálni

    private readonly RotateTransform _rotateTransform = new();
    private double _currentRotationAngle;
    private DispatcherTimer? _rotationTimer;

    // forgatási animáció a spinnernek ha a model letöltés alatt van
    private void StartRotatingAnimation(double rotationPosition)
    {
        if (_rotationTimer != null)
            return;

        _rotationTimer = new DispatcherTimer
        {
            // ilyen időközönként fogja hozzáadni az új forgatási értéket
            Interval = TimeSpan.FromMilliseconds(20)
        };

        _rotationTimer.Tick += (_, _) =>
        {
            _currentRotationAngle += 7.5;
            if (_currentRotationAngle >= 360)
                _currentRotationAngle = 0;
            
            // beállítjuk az új értéket + azt is hogy a forgatás az svg középső pontjától menjen
            _rotateTransform.Angle = _currentRotationAngle;
            _rotateTransform.CenterY = rotationPosition;
            _rotateTransform.CenterX = rotationPosition;
            
            InvalidateVisual();
        };
        
        _rotationTimer.Start();
    }

    private void StopRotationAnimation()
    {
        _rotationTimer?.Stop();
        _rotationTimer = null;
    }

    public override void Render(DrawingContext context)
    {
        // Háttér renderelése
        RenderBackground(context);
        
        // Szöveg renderelése
        RenderText(context);
        
        // DownloadStatus renderelése
        RenderStatus(context);
    }

    private void RenderBackground(DrawingContext context)
    {
        var background = Title == SelectedName
            ? ColorProvider.GetColor(AppColor.Primary)
            : ColorProvider.GetColor(AppColor.SecondaryContainer);
        context.DrawRectangle(background, null,
            new RoundedRect(
                new Rect(Bounds.Size),
                BaseCornerRadius
            )
        );
    }

    private void RenderText(DrawingContext context)
    {
        _titleTextLayout?.Draw(
            context, 
            new Point(BasePadding.Left, BasePadding.Top)
        );
    }

    private void RenderStatus(DrawingContext context)
    {
        if (_titleTextLayout is null) return;
        
        if (DownloadStatus != ModelDownloadStatus.Downloading &&
            DownloadStatus != ModelDownloadStatus.Downloaded) return;

        var color = Title == SelectedName
            ? ColorProvider.GetColor(AppColor.OnPrimary)
            : ColorProvider.GetColor(AppColor.OnSecondaryContainer);
        
        switch (DownloadStatus)
        {
            case ModelDownloadStatus.Downloaded:
                _downloadedSkPicture ??= RenderHelper.LoadSvg(
                    RenderHelper.DownloadSvgPath,
                    strokeColor: color
                );
                
                // svg függőleges középre igazítás
                // annyival skálázzuk fel amennyiszer belefér a Title magasságába és így középen lesz
                // ezért elegendő az Y pozíción csak egy BasePadding.Top értékkel letolni
                var downloadSvgScale = _titleTextLayout.Height / _downloadedSkPicture!.CullRect.Height;
                // svg kezdő pozíciója
                var downloadSvgTranslate = Matrix.CreateTranslation(
                    Bounds.Width - BasePadding.Right - _downloadedSkPicture.CullRect.Width * downloadSvgScale,
                    BasePadding.Top
                );
                
                context.PushTransform(Matrix.CreateScale(downloadSvgScale, downloadSvgScale) * downloadSvgTranslate);
                
                _svg = AvaloniaPicture.Record(_downloadedSkPicture);
                _svg.Draw(context);
                break;
            case ModelDownloadStatus.Downloading:
                _spinnerSkPicture ??= RenderHelper.LoadSvg(
                    RenderHelper.SpinnerSvgPath,
                    fillColor: color
                );
                
                // hasonlóképp mint a downloadSvg-nél
                var spinnerSvgScale = _titleTextLayout.Height / _spinnerSkPicture!.CullRect.Height;
                var spinnerSvgTranslate = Matrix.CreateTranslation(
                    Bounds.Width - BasePadding.Right - _spinnerSkPicture.CullRect.Width * spinnerSvgScale,
                    BasePadding.Top
                );
                
                // svg és download text közti üres hely
                const double spacing = 14;
                
                _downloadTextLayout?.Draw(
                    context, 
                    new Point(
                        Bounds.Width - BasePadding.Right - _spinnerSkPicture.CullRect.Width - _downloadTextLayout.Width - spacing,
                        BasePadding.Top
                    )
                );
                
                context.PushTransform(Matrix.CreateScale(spinnerSvgScale, spinnerSvgScale) * spinnerSvgTranslate);
                
                // forgatás hozzáadása
                // ennek itt kell lennie, mert ha ez után hozzáadunk még egy transform-ot akkor világgá repül xD
                StartRotatingAnimation(_spinnerSkPicture.CullRect.Height / 2);
                context.PushTransform(_rotateTransform.Value);
                
                _svg = AvaloniaPicture.Record(_spinnerSkPicture);
                _svg.Draw(context);
                break;
        }
    }
    
    private TextLayout? CreateTextLayout()
    {
        if (string.IsNullOrEmpty(Title)) return null;
        
        var textColor = Title == SelectedName
            ? ColorProvider.GetColor(AppColor.OnPrimary)
            : ColorProvider.GetColor(AppColor.OnSecondaryContainer);
        
        return new TextLayout(
            Title,
            new Typeface(RenderHelper.ManropeFont),
            TitleFontSize,
            textColor,
            TextAlignment.Left,
            TextWrapping.NoWrap,
            TextTrimming.CharacterEllipsis,
            maxWidth: Bounds.Width * 0.5
        );
    }

    private TextLayout? CreateDownloadTextLayout()
    {
        if (DownloadStatus != ModelDownloadStatus.Downloading) return null;
        
        var textColor = Title == SelectedName
            ? ColorProvider.GetColor(AppColor.OnPrimary)
            : ColorProvider.GetColor(AppColor.OnSecondaryContainer);

        return new TextLayout(
            DownloadProgress?.ToString("P1") ?? "0%", // P2 = 0.00%-os megjelenítés
            new Typeface(RenderHelper.ManropeFont),
            DownloadTextFontSize,
            textColor,
            TextAlignment.Left,
            TextWrapping.Wrap
        );
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        // kurzor átállítása, nem kell ellenőrizni hogy hol a kurzor, mert a PointerEventArgs alapból a Controlon belül van
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // model kiválasztása, pontosabban ezzel meghívja a ModelManagerViewModelben lévő SelectModelt és átadja neki a kiválasztott model nevét
        if (Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }

    protected override void OnMeasureInvalidated()
    {
        _titleTextLayout?.Dispose();
        _titleTextLayout = null;
        _downloadTextLayout?.Dispose();
        _downloadTextLayout = null;
        _svg?.Dispose();
        _svg = null;
        _spinnerSkPicture = null;
        _downloadedSkPicture = null;
        
        base.OnMeasureInvalidated();
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        _titleTextLayout ??= CreateTextLayout();
        _downloadTextLayout ??= CreateDownloadTextLayout();

        return new Size(
            width: availableSize.Width,
            height: BasePadding.Top + BasePadding.Bottom + (_titleTextLayout?.Height ?? 0.0)
        );
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(DownloadProgress):
            case nameof(DownloadStatus):
                if (DownloadStatus != ModelDownloadStatus.Downloading) StopRotationAnimation();
                InvalidateVisual();
                break;
            case nameof(Title):
            case nameof(SelectedName):
            case nameof(Bounds):
                InvalidateVisual();
                InvalidateMeasure();
                break;
        }
    }
}