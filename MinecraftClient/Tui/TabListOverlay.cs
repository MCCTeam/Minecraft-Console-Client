using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MinecraftClient.Tui
{
    internal sealed class TabListOverlay : Border
    {
        private readonly McClient _handler;
        private readonly ScrollViewer _scrollViewer;
        private readonly DispatcherTimer _refreshTimer;

        public TabListOverlay(McClient handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            _handler = handler;

            BorderBrush = Brushes.White;
            BorderThickness = new Thickness(1);
            Background = Brushes.Black;
            Padding = new Thickness(1);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Focusable = true;

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Focusable = true,
            };

            Child = _scrollViewer;

            _refreshTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, static (_, _) => { })
            {
                IsEnabled = false
            };
            _refreshTimer.Tick += (_, _) => Refresh();

            AttachedToVisualTree += (_, _) =>
            {
                AddHandler(KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel);
                Refresh();
                _refreshTimer.Start();
                Focus();
                Dispatcher.UIThread.Post(() => _scrollViewer.Focus(), DispatcherPriority.Input);
            };

            DetachedFromVisualTree += (_, _) =>
            {
                RemoveHandler(KeyDownEvent, OnTunnelKeyDown);
                _refreshTimer.Stop();
            };
        }

        private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            TuiConsoleBackend.Instance?.DismissOverlay();
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                TuiConsoleBackend.Instance?.DismissOverlay();
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        private void Refresh()
        {
            string text = TabListFormatter.Render(_handler.GetTabListSnapshot(), includeOverlayHint: true);
            var block = McColorParser.CreateColoredTextBlock(text, TextWrapping.NoWrap);
            block.Margin = new Thickness(0);
            _scrollViewer.Content = block;
        }
    }
}
