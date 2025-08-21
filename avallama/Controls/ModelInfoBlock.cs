// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using avallama.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Svg;
using ShimSkiaSharp;

namespace avallama.Controls;

// TODO - Hibák:
// - Legkisebb lehetséges modelinformation container esetén a "learn more" szöveg nagyon közel kerül a details elemekhez
// és ha a kurzort rávisszük a learn more szövegre majd le róla akkor a modelinformation container elkezd lefele menni

// TODO - Javítás/Implementálás:
// - felmérni hogy milyen a teljesítmény, különböző render metódusok mennyiszer hívódnak meg, mennyi ramot fogyaszt és allokál
// - code cleaning + proper documentation
// - scroll funkcionalitás modelinformationre
// - szövegkijelölés, vmi új felhasználható típusban

public class ModelInfoBlock : Control
{
    // StyledProperty - belekerül az Avalonia styles rendszerébe így például írhatunk rá stílusokat stb.
    // DirectProperty - nem kerül bele, megadott egyszerű értékeknek, jobb teljesítmény (kell getter, setter)

    // interaktálható elemek
    private enum HoverTarget
    {
        None,
        LinkText,
        DownloadButton,
        DeleteButton,
        ResumeDownloadButton,
        PauseDownloadButton,
        CancelDownloadButton
    }

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelInfoBlock, string?>(nameof(Title));

    public static readonly DirectProperty<ModelInfoBlock, int?> QuantizationProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, int?>(
            nameof(Quantization),
            o => o.Quantization,
            (o, v) => o.Quantization = v
        );

    public static readonly DirectProperty<ModelInfoBlock, double?> ParametersProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, double?>(
            nameof(Parameters),
            o => o.Parameters,
            (o, v) => o.Parameters = v
        );

    public static readonly DirectProperty<ModelInfoBlock, string?> FormatProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, string?>(
            nameof(Format),
            o => o.Format,
            (o, v) => o.Format = v
        );

    public static readonly DirectProperty<ModelInfoBlock, IDictionary<string, string>?> DetailItemsSourceProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, IDictionary<string, string>?>(
            nameof(DetailItemsSource),
            o => o.DetailItemsSource,
            (o, v) => o.DetailItemsSource = v
        );

    public static readonly DirectProperty<ModelInfoBlock, long> SizeInBytesProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, long>(
            nameof(SizeInBytes),
            o => o.SizeInBytes,
            (o, v) => o.SizeInBytes = v,
            unsetValue: 0
        );

    public static readonly DirectProperty<ModelInfoBlock, ModelDownloadStatus> DownloadStatusProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, ModelDownloadStatus>(
            nameof(DownloadStatus),
            o => o.DownloadStatus,
            (o, v) => o.DownloadStatus = v
        );

    // Mbps
    public static readonly DirectProperty<ModelInfoBlock, double?> DownloadSpeedProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, double?>(
            nameof(DownloadSpeed),
            o => o.DownloadSpeed,
            (o, v) => o.DownloadSpeed = v
        );

    public static readonly DirectProperty<ModelInfoBlock, long> DownloadedBytesProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, long>(
            nameof(DownloadedBytes),
            o => o.DownloadedBytes,
            (o, v) => o.DownloadedBytes = v,
            unsetValue: 0
        );

    public static readonly DirectProperty<ModelInfoBlock, bool?> RunsSlowProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, bool?>(
            nameof(RunsSlow),
            o => o.RunsSlow,
            (o, v) => o.RunsSlow = v
        );

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelInfoBlock, ICommand>(nameof(Command));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private int? _quantization;

    public int? Quantization
    {
        get => _quantization;
        set => SetAndRaise(QuantizationProperty, ref _quantization, value);
    }

    private double? _parameters;

    public double? Parameters
    {
        get => _parameters;
        set => SetAndRaise(ParametersProperty, ref _parameters, value);
    }

    private string? _format;

    public string? Format
    {
        get => _format;
        set => SetAndRaise(FormatProperty, ref _format, value);
    }

    private IDictionary<string, string>? _detailItemsSource;

    public IDictionary<string, string>? DetailItemsSource
    {
        get => _detailItemsSource;
        set => SetAndRaise(DetailItemsSourceProperty, ref _detailItemsSource, value);
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

    private const double TitleFontSize = 26;
    private const double WarningFontSize = 14;
    private const double LabelFontSize = 12;
    private const double DetailsFontSize = 14;
    private const double ButtonFontSize = 12;
    private const double DetailsLineHeight = 28;
    private const double BorderThickness = 0.25;
    private const double LabelSpacing = 8.0;
    private const double SvgHeight = 12.0;
    private const double SvgSpacing = 8.0;
    private const double DownloadingInfoSpacing = 16.0;

    private static readonly Thickness BasePadding = new(16);
    private static readonly Thickness LabelPadding = new(10, 5);
    private static readonly Thickness ContainerPadding = new(12);
    private static readonly Thickness ButtonPadding = new(20, 8);
    private static readonly Thickness SvgButtonPadding = new(10, 8);
    private static readonly CornerRadius BaseCornerRadius = new(10);
    private static readonly CornerRadius ContainerCornerRadius = new(8);
    private static readonly CornerRadius LabelCornerRadius = new(4);
    private static readonly CornerRadius ButtonCornerRadius = new(8);
    private static readonly CornerRadius SvgButtonCornerRadius = new(6);

    private TextLayout? _titleTextLayout;
    private TextLayout? _warningTextLayout;
    private TextLayout? _quantizationTextLayout;
    private TextLayout? _parametersTextLayout;
    private TextLayout? _formatTextLayout;
    private TextLayout? _sizeTextLayout;
    private TextLayout? _detailsTextLayout;
    private TextLayout? _modelInfoTextLayout;
    private TextLayout? _linkTextLayout;
    private TextLayout? _statusTextLayout;

    private AvaloniaPicture? _leftActionSvg;
    private AvaloniaPicture? _rightActionSvg;

    private SKPicture? _pauseSkPicture;
    private SKPicture? _resumeSkPicture;
    private SKPicture? _cancelSkPicture;

    private double _renderPosX;
    private double _renderPosY;

    private double _labelsHeight;

    private Point _linkTextLayoutStartPoint = new(0, 0);
    private Point _leftActionSvgStartPoint = new(0, 0);
    private Point _rightActionSvgStartPoint = new(0, 0);
    private Point _actionButtonStartPoint = new(0, 0);

    // az az elem amin a kurzor rajta van és interaktálható (ez vagy a learn more szöveg vagy a letöltést kezelő gombok)
    private HoverTarget _activeHoverTarget = HoverTarget.None;

    private const string OllamaLibraryUrl = @"https://ollama.com/library/";

    public override void Render(DrawingContext context)
    {
        // Háttér renderelése
        RenderBackground(context);

        // Model cím (+warning szöveg ha van) renderelése
        RenderTitle(context);

        // alap címkék renderelése (kvantálás, paraméterek, format) + méret label
        RenderLabels(context);

        // model információinak renderelése
        RenderModelInformation(context);

        // model (letöltési) állapotának renderelése
        RenderStatus(context);
    }

    private void RenderBackground(DrawingContext context)
    {
        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.SurfaceContainer),
            new Pen(ColorProvider.GetColor(AppColor.OnSurface), BorderThickness),
            new RoundedRect(
                new Rect(Bounds.Size),
                BaseCornerRadius
            )
        );
    }

    private void RenderTitle(DrawingContext context)
    {
        _titleTextLayout?.Draw(
            context,
            new Point(BasePadding.Left, BasePadding.Top)
        );

        const double spacing = 6.0;

        _warningTextLayout?.Draw(
            context,
            new Point(
                Bounds.Width - _warningTextLayout.Width - BasePadding.Right,
                BasePadding.Top + spacing
            )
        );

        _renderPosY += BasePadding.Top + (_titleTextLayout?.Height ?? 0) + BasePadding.Bottom;
        _renderPosX = BasePadding.Left;
    }

    // kirajzol egy alap labelt, egy hátteret és egy benne lévő szöveget, _renderPosX és _renderPosY alapján
    // a három alap infónak van ami kiemelten jelenik meg
    private void DrawBaseLabel(
        DrawingContext context,
        double width,
        double height,
        TextLayout? textLayout
    )
    {
        if (textLayout == null) return;

        double posXChange;
        var labelHeight = LabelPadding.Top + textLayout.Height + LabelPadding.Bottom + LabelSpacing;

        // belefér a sorba a label
        if (Bounds.Width - BasePadding.Left - BasePadding.Right - _renderPosX >= textLayout.Width)
        {
            posXChange = LabelPadding.Left + textLayout.Width + LabelPadding.Right + LabelSpacing;
            _labelsHeight = Math.Max(_labelsHeight, labelHeight);
        }
        else
        {
            // ha új sorba kezdődik akkor reseteljük a renderPosX-et és hozzáadjuk a label magasságot a renderPosY-hoz
            posXChange = 0.0;
            _renderPosX = BasePadding.Left;
            _renderPosY += LabelPadding.Top + textLayout.Height + LabelPadding.Bottom + LabelSpacing;
            _labelsHeight += labelHeight;
        }

        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.Secondary), null,
            new RoundedRect(
                new Rect(
                    new Point(
                        _renderPosX,
                        _renderPosY
                    ),
                    new Size(
                        width,
                        height
                    )
                ),
                LabelCornerRadius
            )
        );

        textLayout.Draw(
            context,
            new Point(_renderPosX + LabelPadding.Left, _renderPosY + LabelPadding.Top)
        );

        // a render pozíció megkapja az X változást (ha pl. nem került új sorba az adott label akkor hozzáadjuk annak a hosszát
        // egyébként meg nullát, ha új sorba került és akkor megint BasePadding.Left-ről kezdi
        _renderPosX += posXChange;
    }

    private void RenderLabels(DrawingContext context)
    {
        if (_quantizationTextLayout != null && Quantization.HasValue)
        {
            DrawBaseLabel(context,
                LabelPadding.Left + _quantizationTextLayout.Width + LabelPadding.Right,
                LabelPadding.Top + _quantizationTextLayout.Height + LabelPadding.Bottom,
                _quantizationTextLayout
            );
        }

        if (_parametersTextLayout != null && Parameters.HasValue)
        {
            DrawBaseLabel(context,
                LabelPadding.Left + _parametersTextLayout.Width + LabelPadding.Right,
                LabelPadding.Top + _parametersTextLayout.Height + LabelPadding.Bottom,
                _parametersTextLayout
            );
        }

        if (_formatTextLayout != null && !string.IsNullOrEmpty(Format))
        {
            DrawBaseLabel(context,
                LabelPadding.Left + _formatTextLayout.Width + LabelPadding.Right,
                LabelPadding.Top + _formatTextLayout.Height + LabelPadding.Bottom,
                _formatTextLayout
            );
        }

        // renderPosY újra számítása, ugyanis itt nem lehetne szimplán hozzáadni a labelsHeightot mert a renderelés közben már
        // hozzá lett adva, nem az egész, de így könnyebb ha újraszámoljuk hogy hol kell lennie a render pozíciónak
        _renderPosY = BasePadding.Top + (_titleTextLayout?.Height ?? 0.0) + BasePadding.Bottom + _labelsHeight -
            LabelSpacing + BasePadding.Bottom;

        // SizeLabel (ehhez nem kell renderPosX és renderPosY mert fixen mindig a bal alsó sarokban lesz)
        if (_sizeTextLayout == null) return;
        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.SurfaceContainerHighest),
            new Pen(ColorProvider.GetColor(AppColor.OnSurface), BorderThickness),
            new RoundedRect(
                new Rect(
                    new Point(
                        BasePadding.Left,
                        Bounds.Height - BasePadding.Bottom - LabelPadding.Bottom - _sizeTextLayout.Height -
                        LabelPadding.Top
                    ),
                    new Size(
                        width: LabelPadding.Left + _sizeTextLayout.Width + LabelPadding.Right,
                        height: LabelPadding.Top + _sizeTextLayout.Height + LabelPadding.Bottom
                    )
                ),
                LabelCornerRadius
            )
        );

        _sizeTextLayout.Draw(
            context,
            new Point(
                BasePadding.Left + LabelPadding.Left,
                Bounds.Height - BasePadding.Bottom - LabelPadding.Bottom - _sizeTextLayout.Height
            )
        );
    }

    private void RenderModelInformation(DrawingContext context)
    {
        // "model information" szöveg és a details text közötti térköz
        const double spacing = 14.0;

        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.SurfaceContainerHighest),
            new Pen(ColorProvider.GetColor(AppColor.OnSurface), BorderThickness),
            new RoundedRect(
                new Rect(
                    new Point(
                        BasePadding.Left,
                        _renderPosY
                    ),
                    new Size(
                        width: Bounds.Width - BasePadding.Left - BasePadding.Right,
                        height: Bounds.Height - _renderPosY - BasePadding.Bottom * 3 - LabelPadding.Top -
                                (_sizeTextLayout?.Height ?? 0.0) - LabelPadding.Bottom
                    )
                ),
                ContainerCornerRadius
            )
        );

        _renderPosY += ContainerPadding.Top;
        _renderPosX = BasePadding.Left + ContainerPadding.Left;

        _modelInfoTextLayout?.Draw(
            context,
            new Point(
                _renderPosX,
                _renderPosY
            )
        );

        _renderPosY += (_modelInfoTextLayout?.Height ?? 0) + spacing;

        _detailsTextLayout?.Draw(
            context,
            new Point(
                _renderPosX,
                _renderPosY
            )
        );

        _linkTextLayoutStartPoint = new Point(
            BasePadding.Left + ContainerPadding.Left,
            Bounds.Height - BasePadding.Bottom * 3 - LabelPadding.Bottom - (_sizeTextLayout?.Height ?? 0.0) -
            LabelPadding.Top - ContainerPadding.Bottom - (_linkTextLayout?.Height ?? 0.0)
        );

        _linkTextLayout?.Draw(
            context,
            _linkTextLayoutStartPoint
        );
    }

    // NoConnection/NotEnoughSpace: szöveg

    // Downloading: szöveg, szünet, letöltés bezárása
    // Paused: szöveg, folytatás, letöltés bezárása

    // Downloaded: törlés gomb
    // Ready: letöltés gomb

    private void RenderStatus(DrawingContext context)
    {
        switch (DownloadStatus)
        {
            case ModelDownloadStatus.NoConnection or ModelDownloadStatus.NotEnoughSpace:
                RenderWarningState(context);
                break;
            case ModelDownloadStatus.Downloading:
                RenderDownloadingState(context);
                break;
            case ModelDownloadStatus.Paused:
                RenderPausedState(context);
                break;
            case ModelDownloadStatus.Downloaded:
                RenderDownloadedState(context);
                break;
            case ModelDownloadStatus.Ready:
                RenderReadyState(context);
                break;
        }
    }

    private void RenderWarningState(DrawingContext context)
    {
        // a méret szöveghez igazítva
        var warningTextPosX = Bounds.Width - BasePadding.Right - (_statusTextLayout?.Width ?? 0.0) - LabelPadding.Right;
        var warningTextPosY =
            Bounds.Height - BasePadding.Bottom - (_sizeTextLayout?.Height ?? 0.0) - LabelPadding.Bottom;

        _statusTextLayout?.Draw(context, new Point(warningTextPosX, warningTextPosY));
    }

    private void RenderToggleDownloadState(
        DrawingContext context,
        SKPicture rightSkPicture,
        SKPicture leftSkPicture
    )
    {
        // ez itt lehet, mivel a jobb oldali svg mindig a cancelbutton lesz de ha más is lenne akkor ezt ki kell szervezni
        var rightSvgRectBackground = _activeHoverTarget == HoverTarget.CancelDownloadButton
            ? AppColor.PrimaryContainer
            : AppColor.Primary;

        var leftSvgRectBackground =
            _activeHoverTarget is HoverTarget.PauseDownloadButton or HoverTarget.ResumeDownloadButton
                ? AppColor.PrimaryContainer
                : AppColor.Primary;

        // svg méretei, arányai, kiszámított skálázás úgy, hogy egy méretben legyenek a megadott SvgHeight szerint
        var rightSvgAspectRatio = _cancelSkPicture?.CullRect.Width / _cancelSkPicture?.CullRect.Height ?? 1.0;
        var leftSvgAspectRatio = leftSkPicture.CullRect.Width / leftSkPicture.CullRect.Height;

        var rightSvgWidth = SvgHeight * rightSvgAspectRatio;
        var leftSvgWidth = SvgHeight * leftSvgAspectRatio;

        var rightSvgScale = SvgHeight / rightSkPicture.CullRect.Height;
        var leftSvgScale = SvgHeight / leftSkPicture.CullRect.Height;

        // pozíciót tároló változók amiket módosítunk annyiban hogy hozzáadjuk a már renderelt elemek méreteit + paddingot, spacinget
        var posX = Bounds.Width - BasePadding.Right - SvgButtonPadding.Right - rightSvgWidth;
        var posY = Bounds.Height - BasePadding.Bottom - SvgButtonPadding.Bottom - SvgHeight;

        var rightSvgTranslate = Matrix.CreateTranslation(posX, posY);

        var rightRectPosX = posX - SvgButtonPadding.Left;
        var rightRectPosY = posY - SvgButtonPadding.Top;

        _rightActionSvgStartPoint = new Point(rightRectPosX, rightRectPosY);

        // rightSvg hátterének renderelése
        context.DrawRectangle(
            ColorProvider.GetColor(rightSvgRectBackground),
            null,
            new RoundedRect(
                new Rect(
                    new Point(rightRectPosX, rightRectPosY),
                    new Size(
                        width: SvgButtonPadding.Left + rightSvgWidth + SvgButtonPadding.Right,
                        height: SvgButtonPadding.Top + SvgHeight + SvgButtonPadding.Bottom
                    )
                ),
                SvgButtonCornerRadius
            )
        );

        posX -= SvgButtonPadding.Left + SvgButtonPadding.Right + SvgSpacing + leftSvgWidth;

        var leftRectPosX = posX - SvgButtonPadding.Left;
        var leftRectPosY = posY - SvgButtonPadding.Top;

        _leftActionSvgStartPoint = new Point(leftRectPosX, leftRectPosY);

        // pauseSvg hátterének renderelése
        context.DrawRectangle(
            ColorProvider.GetColor(leftSvgRectBackground),
            null,
            new RoundedRect(
                new Rect(
                    new Point(leftRectPosX, leftRectPosY),
                    new Size(
                        width: SvgButtonPadding.Left + leftSvgWidth + SvgButtonPadding.Right,
                        height: SvgButtonPadding.Top + SvgHeight + SvgButtonPadding.Bottom
                    )
                ),
                SvgButtonCornerRadius
            )
        );

        var leftSvgTranslate = Matrix.CreateTranslation(posX, posY);

        if (_cancelSkPicture != null) _rightActionSvg = AvaloniaPicture.Record(rightSkPicture);
        if (_pauseSkPicture != null || _resumeSkPicture != null) _leftActionSvg = AvaloniaPicture.Record(leftSkPicture);

        // DrawingContexten megadjuk PushTransform-al hogy hol legyenek az svg-k, de miután kirajzoltuk az egyiket
        // disposeoljuk a pushedStatet, mert különben a következő svg kb. duplán alkalmazná az első eltolást és nem jelenne meg
        var pushedState =
            context.PushTransform(Matrix.CreateScale(rightSvgScale, rightSvgScale) * rightSvgTranslate);
        _rightActionSvg?.Draw(context);
        pushedState.Dispose();

        pushedState = context.PushTransform(Matrix.CreateScale(leftSvgScale, leftSvgScale) * leftSvgTranslate);
        _leftActionSvg?.Draw(context);
        pushedState.Dispose();

        posX -= SvgButtonPadding.Left + DownloadingInfoSpacing + (_statusTextLayout?.Width ?? 0.0);

        _statusTextLayout?.Draw(
            context,
            new Point(
                posX,
                Bounds.Height - BasePadding.Bottom - (SvgHeight + SvgButtonPadding.Top + SvgButtonPadding.Bottom
                                                      - (_statusTextLayout?.Height ?? 0.0) / 2)
            )
        );
    }

    private void RenderDownloadingState(DrawingContext context)
    {
        var cancelSvgFill = _activeHoverTarget == HoverTarget.CancelDownloadButton
            ? AppColor.OnErrorContainer
            : AppColor.OnError;

        var pauseSvgFill = _activeHoverTarget == HoverTarget.PauseDownloadButton
            ? AppColor.OnPrimaryContainer
            : AppColor.OnPrimary;

        // svg-k betöltése
        _cancelSkPicture ??= RenderHelper.LoadSvg(
            RenderHelper.CloseSvgPath,
            strokeColor: ColorProvider.GetColor(cancelSvgFill),
            strokeWidth: 5.0
        );

        _pauseSkPicture ??= RenderHelper.LoadSvg(
            RenderHelper.PauseSvgPath,
            ColorProvider.GetColor(pauseSvgFill)
        );

        if (_cancelSkPicture == null || _pauseSkPicture == null) return;
        RenderToggleDownloadState(
            context,
            rightSkPicture: _cancelSkPicture,
            leftSkPicture: _pauseSkPicture
        );
    }

    private void RenderPausedState(DrawingContext context)
    {
        var cancelSvgFill = _activeHoverTarget == HoverTarget.CancelDownloadButton
            ? AppColor.OnErrorContainer
            : AppColor.OnError;

        var resumeSvgFill = _activeHoverTarget == HoverTarget.ResumeDownloadButton
            ? AppColor.OnPrimaryContainer
            : AppColor.OnPrimary;

        // svg-k betöltése
        _cancelSkPicture ??= RenderHelper.LoadSvg(
            RenderHelper.CloseSvgPath,
            strokeColor: ColorProvider.GetColor(cancelSvgFill),
            strokeWidth: 5.0
        );

        _resumeSkPicture ??= RenderHelper.LoadSvg(
            RenderHelper.ResumeSvgPath,
            fillColor: ColorProvider.GetColor(resumeSvgFill),
            strokeColor: ColorProvider.GetColor(resumeSvgFill),
            strokeWidth: 1.75
        );

        if (_cancelSkPicture == null || _resumeSkPicture == null) return;
        RenderToggleDownloadState(
            context,
            rightSkPicture: _cancelSkPicture,
            leftSkPicture: _resumeSkPicture
        );
    }

    private void RenderActionButton(
        DrawingContext context,
        AppColor rectBackground
    )
    {
        var rectPosX = Bounds.Width - BasePadding.Right - ButtonPadding.Right - (_statusTextLayout?.Width ?? 0.0)
                       - ButtonPadding.Left;
        var rectPosY = Bounds.Height - BasePadding.Bottom - ButtonPadding.Bottom - (_statusTextLayout?.Height ?? 0.0)
                       - ButtonPadding.Top;

        _actionButtonStartPoint = new Point(rectPosX, rectPosY);

        context.DrawRectangle(
            ColorProvider.GetColor(rectBackground),
            null,
            new RoundedRect(
                new Rect(
                    new Point(rectPosX, rectPosY),
                    new Size(
                        width: ButtonPadding.Left + (_statusTextLayout?.Width ?? 0.0) + ButtonPadding.Right,
                        height: ButtonPadding.Top + (_statusTextLayout?.Height ?? 0.0) + ButtonPadding.Bottom
                    )
                ),
                ButtonCornerRadius
            )
        );

        _statusTextLayout?.Draw(context, new Point(rectPosX + ButtonPadding.Right, rectPosY + ButtonPadding.Top));
    }

    private void RenderDownloadedState(DrawingContext context)
    {
        var rectBackground = _activeHoverTarget == HoverTarget.DeleteButton
            ? AppColor.ErrorContainer
            : AppColor.Error;

        RenderActionButton(context, rectBackground);
    }

    private void RenderReadyState(DrawingContext context)
    {
        var rectBackground = _activeHoverTarget == HoverTarget.DownloadButton
            ? AppColor.PrimaryContainer
            : AppColor.Primary;

        RenderActionButton(context, rectBackground);
    }

    // Model címéhez tartozó textlayout létrehozása
    private TextLayout? CreateTitleTextLayout()
    {
        if (string.IsNullOrEmpty(Title)) return null;

        const double spacing = 26.0;

        return new TextLayout(
            Title,
            new Typeface(RenderHelper.ManropeFont),
            null,
            TitleFontSize,
            ColorProvider.GetColor(AppColor.OnSurface),
            maxWidth: Bounds.Width - BasePadding.Left - BasePadding.Right - (_warningTextLayout?.Width + spacing ?? 0),
            textWrapping: TextWrapping.Wrap,
            maxLines: 2,
            textTrimming: TextTrimming.CharacterEllipsis
        );
    }

    // Warning textlayout létrehozása (ha szükség van rá, pl. ki kell írni hogy lassan futhat a model)
    private TextLayout? CreateWarningTextLayout()
    {
        if (!RunsSlow.HasValue || !RunsSlow.Value) return null;
        return new TextLayout(
            LocalizationService.GetString("MIGHT_RUN_SLOW"),
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Regular),
            null,
            WarningFontSize,
            ColorProvider.GetColor(AppColor.Error)
        );
    }

    // quantizationnek, parametersnek és a formatnak
    private static TextLayout CreateLabelTextLayout(string text)
    {
        return new TextLayout(
            text,
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Bold),
            null,
            LabelFontSize,
            ColorProvider.GetColor(AppColor.OnSecondary)
        );
    }

    // Model részleteihez tartozó textlayout létrehozása
    private TextLayout? CreateDetailsTextLayout()
    {
        if (DetailItemsSource == null || DetailItemsSource.Count == 0) return null;

        // details elemek egy stringbe fűzése
        var mergedDetailsText = string.Join('\n', DetailItemsSource.Select(kv => $"{kv.Key}: {kv.Value}"));

        return new TextLayout(
            mergedDetailsText,
            new Typeface(RenderHelper.ManropeFont),
            null,
            DetailsFontSize,
            ColorProvider.GetColor(AppColor.OnSurface),
            lineHeight: DetailsLineHeight
        );
    }

    private TextLayout? CreateSizeTextLayout()
    {
        if (SizeInBytes <= 0) return null;

        var formattedSize = string.Format(LocalizationService.GetString("MODEL_SIZE"),
            RenderHelper.GetSizeInGb(SizeInBytes));

        return new TextLayout(
            formattedSize,
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Regular),
            null,
            LabelFontSize,
            ColorProvider.GetColor(AppColor.OnSurface)
        );
    }

    private static TextLayout CreateModelInfoTextLayout()
    {
        return new TextLayout(
            LocalizationService.GetString("MODEL_INFORMATION"),
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Bold),
            null,
            DetailsFontSize,
            ColorProvider.GetColor(AppColor.OnSurface)
        );
    }

    private TextLayout CreateLinkTextLayout()
    {
        var textColor = _activeHoverTarget == HoverTarget.LinkText ? AppColor.Primary : AppColor.OnSurface;

        return new TextLayout(
            LocalizationService.GetString("LEARN_MORE"),
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Bold),
            fontFeatures: null,
            textDecorations:
            [
                new TextDecoration
                {
                    Location = TextDecorationLocation.Underline,
                    Stroke = ColorProvider.GetColor(textColor),
                    StrokeThickness = 1,
                    StrokeOffset = 0.75,
                    StrokeThicknessUnit = TextDecorationUnit.Pixel,
                    StrokeOffsetUnit = TextDecorationUnit.Pixel
                }
            ],
            fontSize: DetailsFontSize,
            foreground: ColorProvider.GetColor(textColor)
        );
    }

    private string GetResponsiveDownloadInfo()
    {
        if (DownloadStatus is ModelDownloadStatus.Downloading or ModelDownloadStatus.Paused)
        {
            var downloadProgress = (double)DownloadedBytes / SizeInBytes;
            if (downloadProgress <= 0) return string.Format(LocalizationService.GetString("DOWNLOADING"), "0%");

            var leftSkPicture = DownloadStatus == ModelDownloadStatus.Downloading
                ? _pauseSkPicture
                : _resumeSkPicture;

            var leftSvgAspectRatio = leftSkPicture?.CullRect.Width / leftSkPicture?.CullRect.Height ?? 1.0;
            var leftSvgWidth = SvgHeight * leftSvgAspectRatio;

            var cancelSvgAspectRatio = _cancelSkPicture?.CullRect.Width / _cancelSkPicture?.CullRect.Height ?? 1.0;
            var cancelSvgWidth = SvgHeight * cancelSvgAspectRatio;

            var availableWidth = Bounds.Width - BasePadding.Left - LabelPadding.Left - (_sizeTextLayout?.Width ?? 0) -
                                 LabelPadding.Right - BasePadding.Right - SvgButtonPadding.Right * 2 -
                                 SvgButtonPadding.Left * 2 - SvgSpacing -
                                 DownloadingInfoSpacing * 2 - leftSvgWidth - cancelSvgWidth;

            // a statusTextLayoutban lévő egy karakter hossza
            // ezt sajna így kell beégetetten megadni, mert ugye ez a metódus adja vissza a statusTextLayoutnak a szöveget
            // ez azért kell hogy a szöveget reszponzívan le tudja vágni és ki legyen használva a teljes hely
            const double characterVisualLength = 6.024;

            var downloadInfoText = DownloadStatus == ModelDownloadStatus.Downloading
                ? LocalizationService.GetString("DOWNLOADING")
                : LocalizationService.GetString("PAUSED");

            downloadInfoText +=
                $".. {downloadProgress:P1} ({RenderHelper.GetSizeInGb(DownloadedBytes)}/{RenderHelper.GetSizeInGb(SizeInBytes)}) - {DownloadSpeed} Mbps";

            // visszaadjuk a downloadInfoTextet és csak annyit belőle amennyire elegendő hely van
            return downloadInfoText.Length * characterVisualLength > availableWidth
                ? downloadInfoText[..Math.Clamp((int)(availableWidth / characterVisualLength), 0, downloadInfoText.Length - 1)]
                : downloadInfoText;
        }

        return string.Empty;
    }

    private TextLayout CreateStatusTextLayout()
    {
        var statusText = DownloadStatus switch
        {
            ModelDownloadStatus.NotEnoughSpace => LocalizationService.GetString("NOT_ENOUGH_SPACE_DOWNLOAD"),
            ModelDownloadStatus.NoConnection => LocalizationService.GetString("NO_CONNECTION_DOWNLOAD"),
            ModelDownloadStatus.Ready => LocalizationService.GetString("DOWNLOAD"),
            ModelDownloadStatus.Downloading or ModelDownloadStatus.Paused => GetResponsiveDownloadInfo(),
            ModelDownloadStatus.Downloaded => LocalizationService.GetString("DELETE"),
            _ => string.Empty
        };

        var statusColor = DownloadStatus switch
        {
            ModelDownloadStatus.NotEnoughSpace or
                ModelDownloadStatus.NoConnection or
                ModelDownloadStatus.Downloading or
                ModelDownloadStatus.Paused => AppColor.OnSurface,

            ModelDownloadStatus.Ready
                =>
                _activeHoverTarget == HoverTarget.DownloadButton
                    ? AppColor.OnPrimaryContainer
                    : AppColor.OnPrimary,

            ModelDownloadStatus.Downloaded =>
                _activeHoverTarget == HoverTarget.DeleteButton
                    ? AppColor.OnErrorContainer
                    : AppColor.OnError,

            _ => AppColor.OnSurface
        };

        return new TextLayout(
            statusText,
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Bold),
            null,
            ButtonFontSize,
            new SolidColorBrush(
                ColorProvider.GetColor(statusColor).Color,
                DownloadStatus is ModelDownloadStatus.NotEnoughSpace or ModelDownloadStatus.NoConnection ? 0.6 : 1.0
            )
        );
    }

    // controlhoz tartozó összes elem felszabadítása
    private void InvalidateControl()
    {
        _warningTextLayout?.Dispose();
        _warningTextLayout = null;
        _titleTextLayout?.Dispose();
        _titleTextLayout = null;
        _detailsTextLayout?.Dispose();
        _detailsTextLayout = null;
        _quantizationTextLayout?.Dispose();
        _quantizationTextLayout = null;
        _parametersTextLayout?.Dispose();
        _parametersTextLayout = null;
        _formatTextLayout?.Dispose();
        _formatTextLayout = null;
        _sizeTextLayout?.Dispose();
        _sizeTextLayout = null;
        _modelInfoTextLayout?.Dispose();
        _modelInfoTextLayout = null;
        _linkTextLayout?.Dispose();
        _linkTextLayout = null;
        _statusTextLayout?.Dispose();
        _statusTextLayout = null;

        _leftActionSvg?.Dispose();
        _leftActionSvg = null;
        _rightActionSvg?.Dispose();
        _rightActionSvg = null;

        _pauseSkPicture = null;
        _resumeSkPicture = null;
        _cancelSkPicture = null;

        _renderPosX = 0;
        _renderPosY = 0;

        _labelsHeight = 0;
    }

    private void CreateTextLayouts()
    {
        // ennek itt kell lennie legelöl, hogy a titleTextLayout tudja mekkora helyet foglal el a warning szöveg
        // és annak alapján fogja ő is kitölteni a helyet
        _warningTextLayout ??= CreateWarningTextLayout();

        _titleTextLayout ??= CreateTitleTextLayout();
        _detailsTextLayout ??= CreateDetailsTextLayout();
        _sizeTextLayout ??= CreateSizeTextLayout();
        _modelInfoTextLayout ??= CreateModelInfoTextLayout();
        _linkTextLayout ??= CreateLinkTextLayout();
        _statusTextLayout ??= CreateStatusTextLayout();

        if (Quantization is > 0)
        {
            _quantizationTextLayout ??= CreateLabelTextLayout(
                string.Format(LocalizationService.GetString("MODEL_QUANTIZATION"), Quantization)
            );
        }

        if (Parameters is > 0)
        {
            _parametersTextLayout ??= CreateLabelTextLayout(
                string.Format(LocalizationService.GetString("MODEL_PARAMETER_SIZE"), Parameters)
            );
        }

        if (!string.IsNullOrEmpty(Format))
        {
            _formatTextLayout ??= CreateLabelTextLayout(Format);
        }
    }

    protected override void OnMeasureInvalidated()
    {
        InvalidateControl();
        base.OnMeasureInvalidated();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        CreateTextLayouts();

        // visszaadjuk a teljes elérhető szélességet, magasságot, hiszen ez a control kitölti a teljes rendelkezésre álló helyet
        return new Size(
            width: availableSize.Width,
            height: availableSize.Height
        );
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Title):
            case nameof(Quantization):
            case nameof(Parameters):
            case nameof(Format):
            case nameof(DetailItemsSource):
            case nameof(DownloadedBytes):
            case nameof(SizeInBytes):
            case nameof(DownloadStatus):
            case nameof(RunsSlow):
                InvalidateControl();
                InvalidateVisual();
                CreateTextLayouts();
                break;
            case nameof(Bounds):
                InvalidateMeasure();
                break;
        }
    }

    private void InvalidateHoveredItems()
    {
        _linkTextLayout?.Dispose();
        _linkTextLayout = null;
        _linkTextLayout = CreateLinkTextLayout();
        _statusTextLayout?.Dispose();
        _statusTextLayout = null;
        _statusTextLayout = CreateStatusTextLayout();

        _leftActionSvg?.Dispose();
        _leftActionSvg = null;
        _rightActionSvg?.Dispose();
        _rightActionSvg = null;

        _pauseSkPicture = null;
        _resumeSkPicture = null;
        _cancelSkPicture = null;

        _renderPosX = 0;
        _renderPosY = 0;

        _labelsHeight = 0;

        InvalidateVisual();
    }

    private bool IsPointerOverLinkText(Point position)
    {
        return position.X >= _linkTextLayoutStartPoint.X &&
               position.X <= _linkTextLayoutStartPoint.X + (_linkTextLayout?.Width ?? 0.0) &&
               position.Y >= _linkTextLayoutStartPoint.Y &&
               position.Y <= _linkTextLayoutStartPoint.Y + (_linkTextLayout?.Height ?? 0.0);
    }

    private bool IsPointerOverActionButton(Point position)
    {
        return position.X >= _actionButtonStartPoint.X &&
               position.X <= _actionButtonStartPoint.X + (_statusTextLayout?.Width ?? 0.0) + ButtonPadding.Left +
               ButtonPadding.Right &&
               position.Y >= _actionButtonStartPoint.Y &&
               position.Y <= _actionButtonStartPoint.Y + (_statusTextLayout?.Height ?? 0.0) + ButtonPadding.Top +
               ButtonPadding.Bottom;
    }

    private bool IsPointerOverLeftActionButton(Point position)
    {
        var activeSkPicture = DownloadStatus == ModelDownloadStatus.Downloading
            ? _pauseSkPicture
            : _resumeSkPicture;

        var svgAspectRatio = activeSkPicture?.CullRect.Width / activeSkPicture?.CullRect.Height ?? 1.0;
        var svgWidth = SvgHeight * svgAspectRatio;

        return position.X >= _leftActionSvgStartPoint.X &&
               position.X <= _leftActionSvgStartPoint.X + svgWidth + SvgButtonPadding.Left +
               SvgButtonPadding.Right &&
               position.Y >= _leftActionSvgStartPoint.Y &&
               position.Y <= _leftActionSvgStartPoint.Y + SvgHeight + SvgButtonPadding.Top +
               SvgButtonPadding.Bottom;
    }

    private bool IsPointerOverRightActionButton(Point position)
    {
        var svgAspectRatio = _cancelSkPicture?.CullRect.Width / _cancelSkPicture?.CullRect.Height ?? 1.0;
        var svgWidth = SvgHeight * svgAspectRatio;

        return position.X >= _rightActionSvgStartPoint.X &&
               position.X <= _rightActionSvgStartPoint.X + svgWidth +
               SvgButtonPadding.Left +
               SvgButtonPadding.Right &&
               position.Y >= _rightActionSvgStartPoint.Y &&
               position.Y <= _rightActionSvgStartPoint.Y + SvgHeight +
               SvgButtonPadding.Top +
               SvgButtonPadding.Bottom;
    }

    private void SetHoveredItem(HoverTarget newItem)
    {
        if (_activeHoverTarget == newItem) return;
        _activeHoverTarget = newItem;
        Cursor = new Cursor(newItem == HoverTarget.None
            ? StandardCursorType.Arrow
            : StandardCursorType.Hand);
        InvalidateHoveredItems();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);

        if (_linkTextLayout != null && _linkTextLayoutStartPoint != default &&
            IsPointerOverLinkText(pos))
        {
            SetHoveredItem(HoverTarget.LinkText);
            return;
        }

        if (_statusTextLayout != null && _actionButtonStartPoint != default &&
            DownloadStatus is ModelDownloadStatus.Ready or ModelDownloadStatus.Downloaded &&
            IsPointerOverActionButton(pos))
        {
            if (DownloadStatus is ModelDownloadStatus.Ready)
            {
                SetHoveredItem(HoverTarget.DownloadButton);
            }
            else if (DownloadStatus is ModelDownloadStatus.Downloaded)
            {
                SetHoveredItem(HoverTarget.DeleteButton);
            }

            return;
        }

        if (_leftActionSvgStartPoint != default && IsPointerOverLeftActionButton(pos))
        {
            if (DownloadStatus is ModelDownloadStatus.Downloading)
            {
                SetHoveredItem(HoverTarget.PauseDownloadButton);
            }
            else if (DownloadStatus is ModelDownloadStatus.Paused)
            {
                SetHoveredItem(HoverTarget.ResumeDownloadButton);
            }

            return;
        }

        if (_rightActionSvgStartPoint != default &&
            DownloadStatus is ModelDownloadStatus.Downloading or ModelDownloadStatus.Paused &&
            IsPointerOverRightActionButton(pos))
        {
            SetHoveredItem(HoverTarget.CancelDownloadButton);
            return;
        }

        SetHoveredItem(HoverTarget.None);
    }

    // ha a learn more szövegre vagy az action buttonra (letöltési műveletek) kattintott akkor azt itt kezeli le
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        switch (_activeHoverTarget)
        {
            case HoverTarget.LinkText:
                Process.Start(new ProcessStartInfo
                {
                    FileName = OllamaLibraryUrl + Title,
                    UseShellExecute = true
                });
                break;
            case HoverTarget.DownloadButton:
                if (Command.CanExecute(ModelDownloadAction.Start)) Command.Execute(ModelDownloadAction.Start);
                break;
            case HoverTarget.PauseDownloadButton:
                if (Command.CanExecute(ModelDownloadAction.Pause)) Command.Execute(ModelDownloadAction.Pause);
                SetHoveredItem(HoverTarget.ResumeDownloadButton);
                break;
            case HoverTarget.ResumeDownloadButton:
                if (Command.CanExecute(ModelDownloadAction.Resume)) Command.Execute(ModelDownloadAction.Resume);
                SetHoveredItem(HoverTarget.PauseDownloadButton);
                break;
            case HoverTarget.CancelDownloadButton:
                if (Command.CanExecute(ModelDownloadAction.Cancel)) Command.Execute(ModelDownloadAction.Cancel);
                break;
            case HoverTarget.DeleteButton:
                if (Command.CanExecute(ModelDownloadAction.Delete)) Command.Execute(ModelDownloadAction.Delete);
                SetHoveredItem(HoverTarget.DownloadButton);
                break;
        }
    }
}