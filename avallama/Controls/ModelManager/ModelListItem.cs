// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Windows.Input;
using avallama.Constants;
using avallama.Models;
using avallama.Utilities.Render;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Svg;
using Avalonia.Threading;
using ShimSkiaSharp;

namespace avallama.Controls.ModelManager;

// TODO:
// felmérni hogy milyen a teljesítmény, különböző render metódusok mennyiszer hívódnak meg, mennyi ramot fogyaszt és allokál
// code cleaning + proper documentation

public class ModelListItem : Control
{
    // StyledProperty - belekerül az Avalonia styles rendszerébe így például írhatunk rá stílusokat stb.
    // DirectProperty - nem kerül bele, megadott egyszerű értékeknek, jobb teljesítmény (kell getter, setter)

    // azért Title a property neve, mert a 'Name' már le van foglalva és szerintem összekavarodna
    // de ez a Model nevét tartalmazza amúgy
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelListItem, string?>(nameof(Title));

    public static readonly DirectProperty<ModelListItem, ModelDownloadStatus> DownloadStatusProperty =
        AvaloniaProperty.RegisterDirect<ModelListItem, ModelDownloadStatus>(
            nameof(DownloadStatus),
            o => o.DownloadStatus,
            (o, v) => o.DownloadStatus = v
        );

    public static readonly DirectProperty<ModelListItem, long> DownloadedBytesProperty =
        AvaloniaProperty.RegisterDirect<ModelListItem, long>(
            nameof(DownloadedBytes),
            o => o.DownloadedBytes,
            (o, v) => o.DownloadedBytes = v,
            unsetValue: 0
        );

    public static readonly DirectProperty<ModelListItem, long> SizeInBytesProperty =
        AvaloniaProperty.RegisterDirect<ModelListItem, long>(
            nameof(SizeInBytes),
            o => o.SizeInBytes,
            (o, v) => o.SizeInBytes = v,
            unsetValue: 0
        );

    public static readonly DirectProperty<ModelListItem, string?> SelectedNameProperty =
        AvaloniaProperty.RegisterDirect<ModelListItem, string?>(
            nameof(SelectedName),
            o => o.SelectedName,
            (o, v) => o.SelectedName = v,
            unsetValue: string.Empty
        );

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelListItem, ICommand>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ModelListItem, object?>(nameof(CommandParameter));

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

    private long _downloadedBytes;

    public long DownloadedBytes
    {
        get => _downloadedBytes;
        set => SetAndRaise(DownloadedBytesProperty, ref _downloadedBytes, value);
    }

    private long _sizeInBytes;

    public long SizeInBytes
    {
        get => _sizeInBytes;
        set => SetAndRaise(SizeInBytesProperty, ref _sizeInBytes, value);
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

    // TODO: kiszervezni stílusokba
    private static readonly Thickness BasePadding = new(10, 8);
    private static readonly CornerRadius BaseCornerRadius = new(6);
    private const double TitleFontSize = 14;
    private const double DownloadTextFontSize = 14;
    private const double SvgSpacing = 8;

    private TextLayout? _titleTextLayout;
    private TextLayout? _downloadTextLayout;

    // ebben lesz az svg
    private AvaloniaPicture? _svg;
    private SKPicture? _downloadedSkPicture;
    private SKPicture? _spinnerSkPicture;
    private SKPicture? _pauseSkPicture;

    private readonly RotateTransform _rotateTransform = new();
    private double _currentRotationAngle;
    private DispatcherTimer? _rotationTimer;

    // rajta van-e a kurzor a Controlon
    private bool _isPointerOver;

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
        IBrush background;

        if (Title == SelectedName)
        {
            background = ColorProvider.GetColor(AppColor.Primary);
        }
        else
        {
            background = _isPointerOver
                ? ColorProvider.GetColor(AppColor.SecondaryContainer)
                : ColorProvider.GetColor(AppColor.SurfaceContainerHighest);
        }

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
            DownloadStatus != ModelDownloadStatus.Downloaded && DownloadStatus != ModelDownloadStatus.Paused) return;

        IBrush color;

        if (Title == SelectedName)
        {
            color = ColorProvider.GetColor(AppColor.OnPrimary);
        }
        else
        {
            color = _isPointerOver
                ? ColorProvider.GetColor(AppColor.OnSecondaryContainer)
                : ColorProvider.GetColor(AppColor.OnSurface);
        }

        switch (DownloadStatus)
        {
            case ModelDownloadStatus.Downloaded:
                _downloadedSkPicture ??= RenderHelper.LoadSvg(
                    RenderHelper.DownloadSvgPath,
                    strokeColor: color
                );

                // annyival skálázzuk fel amennyiszer belefér a Title magasságába
                var downloadSvgScale = _titleTextLayout.Height / _downloadedSkPicture!.CullRect.Height;
                var downloadSvgAspectRatio = _downloadedSkPicture.CullRect.Width / _downloadedSkPicture.CullRect.Height;
                var downloadSvgWidth = _downloadedSkPicture.CullRect.Height * downloadSvgScale * downloadSvgAspectRatio;

                // svg függőleges középre igazítás
                var centeredDownloadSvgPosY = (BasePadding.Top + _titleTextLayout.Height + BasePadding.Bottom
                                               - _downloadedSkPicture.CullRect.Height * downloadSvgScale) / 2;

                // svg kezdő pozíciója
                var downloadSvgTranslate = Matrix.CreateTranslation(
                    Bounds.Width - BasePadding.Right - downloadSvgWidth,
                    centeredDownloadSvgPosY
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

                var spinnerSvgScale = _titleTextLayout.Height / _spinnerSkPicture!.CullRect.Height;
                var spinnerSvgAspectRatio = _spinnerSkPicture.CullRect.Width / _spinnerSkPicture.CullRect.Height;
                var spinnerSvgWidth = _spinnerSkPicture.CullRect.Height * spinnerSvgScale * spinnerSvgAspectRatio;

                var centeredSpinnerSvgPosY = (BasePadding.Top + _titleTextLayout.Height + BasePadding.Bottom
                                              - _spinnerSkPicture.CullRect.Height * spinnerSvgScale) / 2;

                var spinnerSvgTranslate = Matrix.CreateTranslation(
                    Bounds.Width - BasePadding.Right - spinnerSvgWidth,
                    centeredSpinnerSvgPosY
                );

                _downloadTextLayout?.Draw(
                    context,
                    new Point(
                        Bounds.Width - BasePadding.Right - spinnerSvgWidth - _downloadTextLayout.Width - SvgSpacing,
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
            case ModelDownloadStatus.Paused:
                _pauseSkPicture ??= RenderHelper.LoadSvg(
                    RenderHelper.PauseSvgPath,
                    fillColor: color
                );

                // minimum 1.15-ös skálázás ugyanis a pause svg elég nagy és hiába egységes az összes svg property, rosszul néz ki
                var pauseSvgScale = Math.Min(_titleTextLayout.Height / _pauseSkPicture!.CullRect.Height, 1.15);
                var pauseSvgAspectRatio = _pauseSkPicture.CullRect.Width / _pauseSkPicture.CullRect.Height;
                var pauseSvgWidth = _pauseSkPicture.CullRect.Height * pauseSvgScale * pauseSvgAspectRatio;

                var centeredPauseSvgPosY = (BasePadding.Top + _titleTextLayout.Height + BasePadding.Bottom
                                            - _pauseSkPicture.CullRect.Height * pauseSvgScale) / 2;

                var pauseSvgTranslate = Matrix.CreateTranslation(
                    Bounds.Width - BasePadding.Right - pauseSvgWidth,
                    centeredPauseSvgPosY
                );

                _downloadTextLayout?.Draw(
                    context,
                    new Point(
                        Bounds.Width - BasePadding.Right - pauseSvgWidth - _downloadTextLayout.Width - SvgSpacing,
                        BasePadding.Top
                    )
                );

                context.PushTransform(Matrix.CreateScale(pauseSvgScale, pauseSvgScale) * pauseSvgTranslate);

                _svg = AvaloniaPicture.Record(_pauseSkPicture);
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

    private TextLayout CreateDownloadTextLayout()
    {
        var downloadProgress = (double)DownloadedBytes / SizeInBytes;

        var textColor = Title == SelectedName
            ? ColorProvider.GetColor(AppColor.OnPrimary)
            : ColorProvider.GetColor(AppColor.OnSecondaryContainer);

        return new TextLayout(
            downloadProgress.ToString("P1"), // P1 = 0.0%-os megjelenítés
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
        _isPointerOver = true;
        InvalidateControl();
        CreateTextLayouts();
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _isPointerOver = false;
        InvalidateControl();
        CreateTextLayouts();
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // model kiválasztása, pontosabban ezzel meghívja a ModelManagerViewModelben lévő SelectModelt és átadja neki a kiválasztott model nevét
        if (Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }

    // invalidálja a Control összes elemét
    private void InvalidateControl()
    {
        _titleTextLayout?.Dispose();
        _titleTextLayout = null;
        _downloadTextLayout?.Dispose();
        _downloadTextLayout = null;
        _svg?.Dispose();
        _svg = null;
        _spinnerSkPicture = null;
        _downloadedSkPicture = null;
        _pauseSkPicture = null;
    }

    private void CreateTextLayouts()
    {
        _titleTextLayout ??= CreateTextLayout();
        _downloadTextLayout ??= CreateDownloadTextLayout();
    }

    protected override void OnMeasureInvalidated()
    {
        InvalidateControl();
        base.OnMeasureInvalidated();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        CreateTextLayouts();

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
            case nameof(Title):
            case nameof(SelectedName):
            case nameof(DownloadStatus):
            case nameof(DownloadedBytes):
            case nameof(SizeInBytes):
            case nameof(Bounds):
                if (DownloadStatus != ModelDownloadStatus.Downloading) StopRotationAnimation();
                InvalidateVisual();
                InvalidateMeasure();
                break;
        }
    }
}
