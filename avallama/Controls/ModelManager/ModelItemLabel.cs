// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Utilities.Render;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace avallama.Controls.ModelManager
{
    public class ModelItemLabel : Control
    {
        #region Avalonia Properties

        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<ModelItemLabel, string?>(nameof(Text));

        public static readonly StyledProperty<IBrush?> TextBrushProperty =
            AvaloniaProperty.Register<ModelItemLabel, IBrush?>(nameof(TextBrush));

        public static readonly StyledProperty<double?> TextFontSizeProperty =
            AvaloniaProperty.Register<ModelItemLabel, double?>(nameof(TextFontSize));

        public static readonly StyledProperty<Thickness?> PaddingProperty =
            AvaloniaProperty.Register<ModelItemLabel, Thickness?>(nameof(Padding));

        public static readonly StyledProperty<CornerRadius?> CornerRadiusProperty =
            AvaloniaProperty.Register<ModelItemLabel, CornerRadius?>(nameof(CornerRadius));

        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            AvaloniaProperty.Register<ModelItemLabel, IBrush?>(nameof(Background));

        public static readonly StyledProperty<IBrush?> BorderBrushProperty =
            AvaloniaProperty.Register<ModelItemLabel, IBrush?>(nameof(BorderBrush));

        public static readonly StyledProperty<double?> BorderThicknessProperty =
            AvaloniaProperty.Register<ModelItemLabel, double?>(nameof(BorderThickness));

        #endregion

        #region Public Properties

        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public IBrush? TextBrush
        {
            get => GetValue(TextBrushProperty);
            set => SetValue(TextBrushProperty, value);
        }

        public double? TextFontSize
        {
            get => GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
        }

        public Thickness? Padding
        {
            get => GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public CornerRadius? CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public IBrush? BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public double? BorderThickness
        {
            get => GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        #endregion

        #region Private Fields

        private TextLayout? _textLayout;

        #endregion

        #region Rendering

        public override void Render(DrawingContext context)
        {
            if (string.IsNullOrEmpty(Text) || _textLayout is null) return;

            context.DrawRectangle(
                Background,
                new Pen(
                    BorderBrush,
                    BorderThickness ?? 0
                ),
                new RoundedRect(
                    new Rect(
                        new Point(0, 0),
                        new Size(
                            Padding?.Left + _textLayout.Width + Padding?.Right + BorderThickness * 2 ?? 0,
                            Padding?.Top + _textLayout.Height + Padding?.Bottom + BorderThickness * 2 ?? 0
                        )
                    ),
                    CornerRadius ?? new CornerRadius(0)
                )
            );
        }

        #endregion

        #region Text Layout Creation

        private void CreateTextLayout()
        {
            if (string.IsNullOrEmpty(Text))
                return;

            var typeface = new Typeface(RenderHelper.ManropeFont);

            _textLayout = new TextLayout(
                Text,
                typeface,
                null,
                TextFontSize ?? 8.0,
                TextBrush,
                TextAlignment.Center,
                TextWrapping.Wrap,
                flowDirection: FlowDirection.LeftToRight);
        }

        private void InvalidateTextLayout()
        {
            _textLayout?.Dispose();
            _textLayout = null;
        }

        #endregion

        #region Layout Overrides

        protected override void OnMeasureInvalidated()
        {
            InvalidateTextLayout();
            base.OnMeasureInvalidated();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_textLayout is null) return new Size(0, 0);

            return new Size(
                Padding?.Left + _textLayout.Width + Padding?.Right + BorderThickness * 2 ?? 0,
                Padding?.Top + _textLayout.Height + Padding?.Bottom + BorderThickness * 2 ?? 0
            );
        }

        #endregion

        #region Property Changes

        /// <summary>
        /// Handles property change events to update visual state and selection brush when necessary.
        /// </summary>
        /// <param name="change">The property change event arguments.</param>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                // Properties that affect size and layout
                case nameof(Text):
                case nameof(TextFontSize):
                case nameof(Padding):
                case nameof(CornerRadius):
                case nameof(BorderThickness):
                    InvalidateMeasure();
                    break;

                // Visual properties
                case nameof(TextBrush):
                case nameof(Background):
                case nameof(BorderBrush):
                    InvalidateTextLayout();
                    CreateTextLayout();
                    InvalidateVisual();
                    break;
            }
        }

        #endregion
    }
}
