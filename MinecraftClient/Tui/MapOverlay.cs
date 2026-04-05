using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.ChatBots;

namespace MinecraftClient.Tui
{
    /// <summary>
    /// Fullscreen overlay that renders a Minecraft map with interactive zoom and pan.
    /// Uses Unicode half-block characters where each terminal cell displays two vertical
    /// pixels (foreground = top, background = bottom). Supports mouse wheel zoom with
    /// center-anchoring, mouse drag panning, and keyboard navigation.
    /// At 100% one terminal column = one map pixel wide; at 200% two columns = one pixel.
    /// </summary>
    internal sealed class MapOverlay : Panel
    {
        private const double ZoomFactor = 1.25;
        private const double MaxScale = 2.0;
        private const double KeyPanStep = 4.0;
        private const double OffsetEpsilon = 0.5;

        private readonly TextBlock _mapBlock;
        private readonly TextBlock _headerBlock;
        private readonly TextBlock _controlsBlock;
        private readonly TextBlock _cornerTL;
        private readonly TextBlock _cornerTR;
        private readonly TextBlock _cornerBL;
        private readonly TextBlock _cornerBR;

        private McMap _map = null!;
        private double _scale = 1.0;
        private double _offsetX;
        private double _offsetY;
        private double _fitScale = 1.0;

        private bool _isDragging;
        private double _dragStartX;
        private double _dragStartY;
        private double _dragStartOffsetX;
        private double _dragStartOffsetY;
        private bool _initialLayoutDone;

        private static readonly IBrush IndicatorActive = Brushes.Yellow;
        private static readonly IBrush IndicatorDim = new SolidColorBrush(Color.FromRgb(60, 60, 60));

        public MapOverlay(McMap map)
        {
            ArgumentNullException.ThrowIfNull(map);

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Focusable = true;
            Background = Brushes.Black;

            _mapBlock = new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var border = new Border
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                Child = _mapBlock,
            };

            _headerBlock = new TextBlock
            {
                Foreground = Brushes.White,
                Background = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(1, 0),
            };

            _controlsBlock = new TextBlock
            {
                Text = Translations.bot_map_tui_controls,
                Foreground = Brushes.Gray,
                Background = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(1, 0),
            };

            _cornerTL = CreateCornerIndicator(HorizontalAlignment.Left, VerticalAlignment.Top, "\u25E4");
            _cornerTR = CreateCornerIndicator(HorizontalAlignment.Right, VerticalAlignment.Top, "\u25E5");
            _cornerBL = CreateCornerIndicator(HorizontalAlignment.Left, VerticalAlignment.Bottom, "\u25E3");
            _cornerBR = CreateCornerIndicator(HorizontalAlignment.Right, VerticalAlignment.Bottom, "\u25E2");

            Children.Add(border);
            Children.Add(_headerBlock);
            Children.Add(_controlsBlock);
            Children.Add(_cornerTL);
            Children.Add(_cornerTR);
            Children.Add(_cornerBL);
            Children.Add(_cornerBR);

            AttachedToVisualTree += (_, _) =>
            {
                AddHandler(KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel);
                AddHandler(TextInputEvent, OnTunnelTextInput, RoutingStrategies.Tunnel);
                Focus();
            };

            DetachedFromVisualTree += (_, _) =>
            {
                RemoveHandler(KeyDownEvent, OnTunnelKeyDown);
                RemoveHandler(TextInputEvent, OnTunnelTextInput);
            };

            _map = map;
            _scale = double.MinValue;
            UpdateHeaderText();
        }

        private static TextBlock CreateCornerIndicator(HorizontalAlignment hAlign, VerticalAlignment vAlign, string glyph)
        {
            return new TextBlock
            {
                Text = glyph,
                Background = Brushes.Black,
                Foreground = IndicatorDim,
                HorizontalAlignment = hAlign,
                VerticalAlignment = vAlign,
                Padding = new Thickness(0),
            };
        }

        public void UpdateMap(McMap map)
        {
            _map = map;

            if (map.Colors is null || map.Width == 0 || map.Height == 0)
                return;

            RecalculateFitScale();
            _scale = _fitScale;
            _offsetX = 0;
            _offsetY = 0;
            UpdateHeaderText();
            RenderViewport();
        }

        private void UpdateHeaderText()
        {
            int zoomPercent = _scale > 0 ? (int)Math.Round(_scale * 100) : 0;
            _headerBlock.Text = string.Format(Translations.bot_map_tui_header,
                _map.MapId, _map.Width, _map.Height, zoomPercent);
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);

            if (_map?.Colors is null || _map.Width == 0 || _map.Height == 0)
                return;

            RecalculateFitScale();

            if (!_initialLayoutDone)
            {
                _scale = _fitScale;
                _offsetX = 0;
                _offsetY = 0;
                _initialLayoutDone = true;
            }
            else
            {
                _scale = Math.Clamp(_scale, _fitScale, MaxScale);
            }

            ClampOffset();
            UpdateHeaderText();
            RenderViewport();
        }

        #region Scale / Offset

        private void GetViewportCells(out int viewW, out int viewH)
        {
            viewW = Math.Max(1, (int)Bounds.Width - 2);
            viewH = Math.Max(1, (int)Bounds.Height - 2);
        }

        private void RecalculateFitScale()
        {
            GetViewportCells(out int viewW, out int viewH);
            int viewPixelsH = viewH * 2;

            double scaleX = (double)viewW / _map.Width;
            double scaleY = (double)viewPixelsH / _map.Height;
            _fitScale = Math.Min(scaleX, scaleY);

            if (_fitScale > MaxScale)
                _fitScale = MaxScale;
        }

        private void ClampOffset()
        {
            GetViewportCells(out int viewW, out int viewH);
            int viewPixelsH = viewH * 2;

            double visibleMapW = viewW / _scale;
            double visibleMapH = viewPixelsH / _scale;

            double maxOffX = Math.Max(0, _map.Width - visibleMapW);
            double maxOffY = Math.Max(0, _map.Height - visibleMapH);

            _offsetX = Math.Clamp(_offsetX, 0, maxOffX);
            _offsetY = Math.Clamp(_offsetY, 0, maxOffY);
        }

        private void ZoomAtCenter(double factor)
        {
            if (_map?.Colors is null) return;

            GetViewportCells(out int viewW, out int viewH);
            int viewPixelsH = viewH * 2;

            double centerMapX = _offsetX + (viewW / 2.0) / _scale;
            double centerMapY = _offsetY + (viewPixelsH / 2.0) / _scale;

            double newScale = Math.Clamp(_scale * factor, _fitScale, MaxScale);

            if ((_scale < 1.0 - 1e-9 && newScale > 1.0 + 1e-9) ||
                (_scale > 1.0 + 1e-9 && newScale < 1.0 - 1e-9))
                newScale = 1.0;

            if (Math.Abs(newScale - _scale) < 1e-12)
                return;

            _scale = newScale;

            _offsetX = centerMapX - (viewW / 2.0) / _scale;
            _offsetY = centerMapY - (viewPixelsH / 2.0) / _scale;
            ClampOffset();
            UpdateHeaderText();
            RenderViewport();
        }

        #endregion

        #region Rendering

        private void RenderViewport()
        {
            if (_map?.Colors is null || _map.Width == 0 || _map.Height == 0)
                return;

            GetViewportCells(out int viewW, out int viewH);
            int viewPixelsH = viewH * 2;

            int mapW = _map.Width;
            int mapH = _map.Height;
            byte[] colors = _map.Colors;

            int renderCols = Math.Min(viewW, (int)Math.Ceiling(mapW * _scale));
            int renderPixelRows = Math.Min(viewPixelsH, (int)Math.Ceiling(mapH * _scale));
            int renderTextRows = (renderPixelRows + 1) / 2;

            _mapBlock.Inlines ??= [];
            _mapBlock.Inlines.Clear();

            double invScale = 1.0 / _scale;

            for (int r = 0; r < renderTextRows; r++)
            {
                if (r > 0)
                    _mapBlock.Inlines.Add(new LineBreak());

                IBrush? batchFg = null;
                IBrush? batchBg = null;
                int batchLen = 0;

                for (int c = 0; c < renderCols; c++)
                {
                    int srcX = Math.Clamp((int)(_offsetX + c * invScale), 0, mapW - 1);
                    int srcTopY = Math.Clamp((int)(_offsetY + (r * 2) * invScale), 0, mapH - 1);
                    int srcBotY = Math.Clamp((int)(_offsetY + (r * 2 + 1) * invScale), 0, mapH - 1);

                    ColorRGBA top = MapColors.ColorByteToRGBA(colors[srcX + srcTopY * mapW]);
                    ColorRGBA bot = MapColors.ColorByteToRGBA(colors[srcX + srcBotY * mapW]);

                    var fg = new SolidColorBrush(Color.FromRgb(top.R, top.G, top.B));
                    var bg = new SolidColorBrush(Color.FromRgb(bot.R, bot.G, bot.B));

                    if (batchLen > 0 && ColorsEqual(batchFg!, fg) && ColorsEqual(batchBg!, bg))
                    {
                        batchLen++;
                    }
                    else
                    {
                        if (batchLen > 0)
                            FlushBatch(batchFg!, batchBg!, batchLen);

                        batchFg = fg;
                        batchBg = bg;
                        batchLen = 1;
                    }
                }

                if (batchLen > 0)
                    FlushBatch(batchFg!, batchBg!, batchLen);
            }

            UpdateCornerIndicators();
        }

        private void FlushBatch(IBrush fg, IBrush bg, int count)
        {
            _mapBlock.Inlines!.Add(new Run(new string('\u2580', count))
            {
                Foreground = fg,
                Background = bg,
            });
        }

        private static bool ColorsEqual(IBrush a, IBrush b)
        {
            if (a is SolidColorBrush sa && b is SolidColorBrush sb)
                return sa.Color == sb.Color;
            return false;
        }

        private void UpdateCornerIndicators()
        {
            GetViewportCells(out int viewW, out int viewH);
            int viewPixelsH = viewH * 2;

            double visibleW = viewW / _scale;
            double visibleH = viewPixelsH / _scale;

            bool moreLeft = _offsetX > OffsetEpsilon;
            bool moreTop = _offsetY > OffsetEpsilon;
            bool moreRight = _offsetX + visibleW < _map.Width - OffsetEpsilon;
            bool moreBottom = _offsetY + visibleH < _map.Height - OffsetEpsilon;

            _cornerTL.Foreground = (moreLeft || moreTop) ? IndicatorActive : IndicatorDim;
            _cornerTR.Foreground = (moreRight || moreTop) ? IndicatorActive : IndicatorDim;
            _cornerBL.Foreground = (moreLeft || moreBottom) ? IndicatorActive : IndicatorDim;
            _cornerBR.Foreground = (moreRight || moreBottom) ? IndicatorActive : IndicatorDim;
        }

        #endregion

        #region Mouse interaction

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            double factor = e.Delta.Y > 0 ? ZoomFactor : 1.0 / ZoomFactor;
            ZoomAtCenter(factor);
            e.Handled = true;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed)
                return;

            _isDragging = true;
            var pos = e.GetPosition(this);
            _dragStartX = pos.X;
            _dragStartY = pos.Y;
            _dragStartOffsetX = _offsetX;
            _dragStartOffsetY = _offsetY;
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!_isDragging) return;

            var pos = e.GetPosition(this);
            double dxCells = pos.X - _dragStartX;
            double dyCells = pos.Y - _dragStartY;

            _offsetX = _dragStartOffsetX - dxCells / _scale;
            _offsetY = _dragStartOffsetY - dyCells * 2.0 / _scale;
            ClampOffset();
            RenderViewport();
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (!_isDragging) return;

            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        #endregion

        #region Keyboard interaction

        private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key is Key.Escape or Key.E)
            {
                TuiConsoleBackend.Instance?.DismissOverlay();
                e.Handled = true;
            }
        }

        private void OnTunnelTextInput(object? sender, TextInputEventArgs e)
        {
            if (e.Text is "+" or "=")
            {
                ZoomAtCenter(ZoomFactor);
                e.Handled = true;
            }
            else if (e.Text is "-")
            {
                ZoomAtCenter(1.0 / ZoomFactor);
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                case Key.E:
                    TuiConsoleBackend.Instance?.DismissOverlay();
                    e.Handled = true;
                    return;

                case Key.Add:
                    ZoomAtCenter(ZoomFactor);
                    e.Handled = true;
                    return;

                case Key.Subtract:
                    ZoomAtCenter(1.0 / ZoomFactor);
                    e.Handled = true;
                    return;

                case Key.Left:
                    _offsetX -= KeyPanStep / _scale;
                    ClampOffset();
                    RenderViewport();
                    e.Handled = true;
                    return;

                case Key.Right:
                    _offsetX += KeyPanStep / _scale;
                    ClampOffset();
                    RenderViewport();
                    e.Handled = true;
                    return;

                case Key.Up:
                    _offsetY -= KeyPanStep / _scale;
                    ClampOffset();
                    RenderViewport();
                    e.Handled = true;
                    return;

                case Key.Down:
                    _offsetY += KeyPanStep / _scale;
                    ClampOffset();
                    RenderViewport();
                    e.Handled = true;
                    return;
            }

            base.OnKeyDown(e);
        }

        #endregion
    }
}
