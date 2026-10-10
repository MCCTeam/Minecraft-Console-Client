using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;

namespace Mcc.Cli.Tui.Map;

/// <summary>
/// The <c>/map</c> fullscreen overlay behind <see cref="MapController"/>: renders one filled map's 128x128 color-index grid (<see cref="MapSnapshotInfo.Colors"/>) as Unicode half-block pixel art, where each terminal cell shows two vertical map pixels (foreground = top, background = bottom).
/// Supports keyboard zoom (+/-) and pan (arrow keys) plus mouse wheel zoom and drag panning, ported in behavior from the legacy <c>Tui/MapOverlay.cs</c>.
/// Escape or E closes; the footer shows the map id and scale.
/// </summary>
internal sealed class MapOverlay : Panel
{
    private const double MaxScale = 2.0;
    private const double ZoomStep = 0.125;
    private const double KeyPanStep = 4.0;

    private readonly MainTuiView _view;
    private readonly TextBlock _mapBlock;
    private readonly TextBlock _footerBlock;

    private MapSnapshotInfo _map;
    private double _scale = 1.0;
    private double _offsetX;
    private double _offsetY;
    private double _fitScale = 1.0;
    private bool _initialLayoutDone;

    private bool _isDragging;
    private double _dragStartX;
    private double _dragStartY;
    private double _dragStartOffsetX;
    private double _dragStartOffsetY;

    private MapOverlay(MainTuiView view, MapSnapshotInfo map)
    {
        _view = view;
        _map = map;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Focusable = true;
        Background = Brushes.Black;

        _mapBlock = new TextBlock
        {
            TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var border = new Border
        {
            BorderBrush = Brushes.White,
            BorderThickness = new Avalonia.Thickness(1),
            Child = _mapBlock,
        };

        _footerBlock = new TextBlock
        {
            Foreground = Brushes.Gray,
            Background = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Avalonia.Thickness(1, 0),
        };

        Children.Add(border);
        Children.Add(_footerBlock);

        SizeChanged += OnSizeChanged;
        KeyDown += OnKeyDown;
        TextInput += OnTextInput;

        UpdateFooter();
    }

    /// <summary>Opens the overlay showing <paramref name="map"/>.</summary>
    public static void Open(MainTuiView view, MapSnapshotInfo map)
    {
        var overlay = new MapOverlay(view, map);
        view.ShowOverlay(overlay);
    }

    /// <summary>Replaces the displayed map data (called when the same map id refreshes while the overlay is open).</summary>
    public void UpdateMap(MapSnapshotInfo map)
    {
        _map = map;
        UpdateFooter();
        RenderViewport();
    }

    /// <summary>The map id currently displayed (so the caller can match live updates to the open overlay).</summary>
    public int MapId => _map.MapId;

    private void UpdateFooter()
        => _footerBlock.Text = Strings.TuiMapFooter(_map.MapId, _map.Scale, (_scale * 100).ToString("F0"));

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        RecalculateFitScale();
        if (!_initialLayoutDone)
        {
            _scale = _fitScale;
            _offsetX = 0;
            _offsetY = 0;
            _initialLayoutDone = true;
        }
        else
            _scale = Math.Clamp(_scale, _fitScale, MaxScale);

        ClampOffset();
        UpdateFooter();
        RenderViewport();
    }

    #region Scale / offset

    private void GetViewportCells(out int viewW, out int viewH)
    {
        viewW = Math.Max(1, (int)Bounds.Width - 2);
        viewH = Math.Max(1, (int)Bounds.Height - 2);
    }

    private void RecalculateFitScale()
    {
        GetViewportCells(out int viewW, out int viewH);
        int viewPixelsH = viewH * 2;
        double scaleX = (double)viewW / Umpk.Game.Players.MapData.Size;
        double scaleY = (double)viewPixelsH / Umpk.Game.Players.MapData.Size;
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
        double maxOffX = Math.Max(0, Umpk.Game.Players.MapData.Size - visibleMapW);
        double maxOffY = Math.Max(0, Umpk.Game.Players.MapData.Size - visibleMapH);
        _offsetX = Math.Clamp(_offsetX, 0, maxOffX);
        _offsetY = Math.Clamp(_offsetY, 0, maxOffY);
    }

    private double NextStepScale(bool zoomIn)
    {
        if (zoomIn)
        {
            double next = Math.Floor((_scale / ZoomStep) + 1.0 - 1e-9) * ZoomStep;
            if (next <= _scale + 1e-9)
                next += ZoomStep;

            return Math.Clamp(next, _fitScale, MaxScale);
        }

        double prev = Math.Ceiling((_scale / ZoomStep) - 1.0 + 1e-9) * ZoomStep;
        if (prev >= _scale - 1e-9)
            prev -= ZoomStep;

        return Math.Clamp(prev, _fitScale, MaxScale);
    }

    private void ZoomAtCenter(bool zoomIn)
    {
        GetViewportCells(out int viewW, out int viewH);
        int viewPixelsH = viewH * 2;
        double centerMapX = _offsetX + ((viewW / 2.0) / _scale);
        double centerMapY = _offsetY + ((viewPixelsH / 2.0) / _scale);

        double newScale = NextStepScale(zoomIn);
        if (Math.Abs(newScale - _scale) < 1e-12)
            return;

        _scale = newScale;
        _offsetX = centerMapX - ((viewW / 2.0) / _scale);
        _offsetY = centerMapY - ((viewPixelsH / 2.0) / _scale);
        ClampOffset();
        UpdateFooter();
        RenderViewport();
    }

    #endregion

    #region Rendering

    private void RenderViewport()
    {
        GetViewportCells(out int viewW, out int viewH);
        int viewPixelsH = viewH * 2;
        const int mapSize = Umpk.Game.Players.MapData.Size;
        byte[] colors = _map.Colors;

        int renderCols = Math.Min(viewW, (int)Math.Ceiling(mapSize * _scale));
        int renderPixelRows = Math.Min(viewPixelsH, (int)Math.Ceiling(mapSize * _scale));
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
                int srcX = Math.Clamp((int)(_offsetX + (c * invScale)), 0, mapSize - 1);
                int srcTopY = Math.Clamp((int)(_offsetY + ((r * 2) * invScale)), 0, mapSize - 1);
                int srcBotY = Math.Clamp((int)(_offsetY + (((r * 2) + 1) * invScale)), 0, mapSize - 1);

                Color top = MapColors.ColorByteToColor(colors[srcX + (srcTopY * mapSize)]);
                Color bot = MapColors.ColorByteToColor(colors[srcX + (srcBotY * mapSize)]);

                var fg = new SolidColorBrush(top);
                var bg = new SolidColorBrush(bot);

                if (batchLen > 0 && ColorsEqual(batchFg!, fg) && ColorsEqual(batchBg!, bg))
                    batchLen++;
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
    }

    private void FlushBatch(IBrush fg, IBrush bg, int count)
        => _mapBlock.Inlines!.Add(new Run(new string('▀', count)) { Foreground = fg, Background = bg });

    private static bool ColorsEqual(IBrush a, IBrush b)
        => a is SolidColorBrush sa && b is SolidColorBrush sb && sa.Color == sb.Color;

    #endregion

    #region Input

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        ZoomAtCenter(e.Delta.Y > 0);
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        Avalonia.Point pos = e.GetPosition(this);
        _dragStartX = pos.X;
        _dragStartY = pos.Y;
        _dragStartOffsetX = _offsetX;
        _dragStartOffsetY = _offsetY;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isDragging)
            return;

        Avalonia.Point pos = e.GetPosition(this);
        double dxCells = pos.X - _dragStartX;
        double dyCells = pos.Y - _dragStartY;
        _offsetX = _dragStartOffsetX - (dxCells / _scale);
        _offsetY = _dragStartOffsetY - ((dyCells * 2.0) / _scale);
        ClampOffset();
        RenderViewport();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (e.Text is "+" or "=")
        {
            ZoomAtCenter(true);
            e.Handled = true;
        }
        else if (e.Text is "-")
        {
            ZoomAtCenter(false);
            e.Handled = true;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
            case Key.E:
                _view.HideOverlay();
                e.Handled = true;
                return;

            case Key.Add:
                ZoomAtCenter(true);
                e.Handled = true;
                return;

            case Key.Subtract:
                ZoomAtCenter(false);
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
    }

    #endregion
}
