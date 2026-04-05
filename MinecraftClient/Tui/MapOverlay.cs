using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MinecraftClient.ChatBots;

namespace MinecraftClient.Tui
{
    /// <summary>
    /// Fullscreen overlay that renders a Minecraft map using Unicode half-block
    /// characters with Avalonia rich-text inlines. Each terminal cell displays two
    /// vertical pixels via the upper-half-block glyph: foreground = top pixel,
    /// background = bottom pixel. Title and controls text are embedded into the
    /// border lines themselves to maximize the map display area.
    /// </summary>
    internal sealed class MapOverlay : Panel
    {
        private readonly ScrollViewer _scrollViewer;
        private readonly TextBlock _mapBlock;
        private readonly TextBlock _headerBlock;
        private readonly TextBlock _controlsBlock;

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
            };

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _mapBlock,
            };

            var border = new Border
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                Child = _scrollViewer,
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

            Children.Add(border);
            Children.Add(_headerBlock);
            Children.Add(_controlsBlock);

            AttachedToVisualTree += (_, _) =>
            {
                AddHandler(KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel);
                Focus();
            };

            DetachedFromVisualTree += (_, _) =>
            {
                RemoveHandler(KeyDownEvent, OnTunnelKeyDown);
            };

            UpdateMap(map);
        }

        public void UpdateMap(McMap map)
        {
            _headerBlock.Text = string.Format(Translations.bot_map_tui_header, map.MapId, map.Width, map.Height);

            if (map.Colors is null || map.Width == 0 || map.Height == 0)
                return;

            RenderMapInlines(map);
        }

        /// <summary>
        /// Renders the map pixel data as Avalonia <see cref="Run"/> inlines using
        /// the upper-half-block character. Each terminal row represents two pixel
        /// rows: foreground color = upper pixel, background color = lower pixel.
        /// Adjacent cells with identical colors are batched into a single Run.
        /// </summary>
        private void RenderMapInlines(McMap map)
        {
            int w = map.Width;
            int h = map.Height;
            byte[] colors = map.Colors!;

            _mapBlock.Inlines ??= [];
            _mapBlock.Inlines.Clear();

            for (int py = 0; py < h; py += 2)
            {
                if (py > 0)
                    _mapBlock.Inlines.Add(new LineBreak());

                IBrush? batchFg = null;
                IBrush? batchBg = null;
                int batchLen = 0;

                for (int px = 0; px < w; px++)
                {
                    ColorRGBA top = MapColors.ColorByteToRGBA(colors[px + py * w]);
                    ColorRGBA bot = (py + 1 < h)
                        ? MapColors.ColorByteToRGBA(colors[px + (py + 1) * w])
                        : top;

                    var fg = new SolidColorBrush(Color.FromRgb(top.R, top.G, top.B));
                    var bg = new SolidColorBrush(Color.FromRgb(bot.R, bot.G, bot.B));

                    if (batchLen > 0 && ColorsEqual(batchFg!, fg) && ColorsEqual(batchBg!, bg))
                    {
                        batchLen++;
                    }
                    else
                    {
                        if (batchLen > 0)
                            FlushBatch(_mapBlock, batchFg!, batchBg!, batchLen);

                        batchFg = fg;
                        batchBg = bg;
                        batchLen = 1;
                    }
                }

                if (batchLen > 0)
                    FlushBatch(_mapBlock, batchFg!, batchBg!, batchLen);
            }
        }

        private static void FlushBatch(TextBlock tb, IBrush fg, IBrush bg, int count)
        {
            tb.Inlines!.Add(new Run(new string('\u2580', count))
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

        private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key is Key.Escape or Key.E)
            {
                TuiConsoleBackend.Instance?.DismissOverlay();
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key is Key.Escape or Key.E)
            {
                TuiConsoleBackend.Instance?.DismissOverlay();
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }
    }
}
