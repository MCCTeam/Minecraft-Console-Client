using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MinecraftClient.Tui
{
    public sealed class TuiTooltipLine
    {
        public string Text { get; init; } = "";
        public IBrush Foreground { get; init; } = Brushes.White;
    }

    /// <summary>
    /// Global tooltip that floats above all TUI content.
    /// Owned by MainTuiView, used by minimap / chat / other components.
    /// </summary>
    public sealed class TuiTooltipService
    {
        private readonly Panel _rootPanel;
        private readonly Canvas _canvas;
        private readonly Border _border;
        private readonly StackPanel _content;

        internal TuiTooltipService(Panel rootPanel)
        {
            _content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical };
            _border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1),
                Child = _content,
                IsVisible = false,
            };

            _canvas = new Canvas
            {
                IsHitTestVisible = false,
                Children = { _border },
            };

            _rootPanel = rootPanel;
            rootPanel.Children.Add(_canvas);
        }

        /// <param name="mouseX">Global X of the mouse cursor.</param>
        /// <param name="mouseY">Global Y of the mouse cursor.</param>
        /// <param name="preferRight">
        /// If true, try placing tooltip to the right of mouseX;
        /// if false, try placing to the left.
        /// The service auto-flips when the tooltip would overflow the screen.
        /// </param>
        public void Show(double mouseX, double mouseY, IReadOnlyList<TuiTooltipLine> lines,
            bool preferRight = true)
        {
            _content.Children.Clear();

            if (lines.Count == 0)
            {
                _border.IsVisible = false;
                return;
            }

            int maxChars = 0;
            foreach (var line in lines)
            {
                _content.Children.Add(new TextBlock
                {
                    Text = line.Text,
                    Foreground = line.Foreground,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    FontSize = 1,
                });
                if (line.Text.Length > maxChars)
                    maxChars = line.Text.Length;
            }

            double tipW = maxChars + 4;
            double screenW = _rootPanel.Bounds.Width;

            const double gap = 1;
            double gx;
            if (preferRight)
            {
                gx = mouseX + gap;
                if (gx + tipW > screenW)
                    gx = mouseX - tipW - gap;
            }
            else
            {
                gx = mouseX - tipW - gap;
                if (gx < 0)
                    gx = mouseX + gap;
            }

            Canvas.SetLeft(_border, Math.Max(0, gx));
            Canvas.SetTop(_border, Math.Max(0, mouseY));
            _border.IsVisible = true;
        }

        public void Hide()
        {
            _border.IsVisible = false;
            _content.Children.Clear();
        }

        public bool IsVisible => _border.IsVisible;
    }
}
