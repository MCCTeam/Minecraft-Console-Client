using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MinecraftClient.Tui
{
    public class MainTuiView : UserControl
    {
        private const int MaxLogLines = 5000;

        private readonly ObservableCollection<string> _logLines = new();
        private readonly ItemsControl _logItemsControl;
        private readonly ScrollViewer _logScrollViewer;
        private readonly TextBox _commandInput;
        private bool _autoScroll = true;
        private readonly ObservableCollection<string> _commandHistory = new();
        private int _historyIndex = -1;

        private readonly Panel _rootPanel;
        private readonly DockPanel _mainContent;
        private Control? _overlayContent;
        private Action? _overlayCloseCallback;

        public MainTuiView()
        {
            Background = Brushes.Black;

            _logItemsControl = new ItemsControl
            {
                ItemsSource = _logLines,
                Focusable = false,
                ItemTemplate = new FuncDataTemplate<string>((s, _) =>
                    new TextBlock
                    {
                        Text = s,
                        Foreground = Brushes.White,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                        TextWrapping = TextWrapping.NoWrap,
                    }),
            };

            _logScrollViewer = new ScrollViewer
            {
                Content = _logItemsControl,
                Background = Brushes.Black,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(0),
                Focusable = false,
            };

            _logScrollViewer.ScrollChanged += OnLogScrollChanged;
            _logScrollViewer.PointerPressed += (_, _) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => _commandInput.Focus());
            };

            _commandInput = new TextBox
            {
                Watermark = "",
                Foreground = Brushes.White,
                Background = Brushes.Black,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                MinHeight = 1,
            };

            _commandInput.KeyDown += OnCommandKeyDown;
            _commandInput.TextChanged += OnCommandTextChanged;

            var promptLabel = new TextBlock
            {
                Text = "> ",
                Foreground = Brushes.Cyan,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.Bold,
            };

            var inputRow = new DockPanel
            {
                Background = Brushes.Black,
                Children =
                {
                    SetDock(promptLabel, Dock.Left),
                    _commandInput
                }
            };

            _mainContent = new DockPanel
            {
                Background = Brushes.Black,
                Children =
                {
                    SetDock(inputRow, Dock.Bottom),
                    _logScrollViewer
                }
            };

            _rootPanel = new Panel
            {
                Background = Brushes.Black,
                Children = { _mainContent }
            };

            Content = _rootPanel;
        }

        private static Control SetDock(Control control, Dock dock)
        {
            DockPanel.SetDock(control, dock);
            return control;
        }

        public void AppendLogLine(string text)
        {
            _logLines.Add(text);

            while (_logLines.Count > MaxLogLines)
                _logLines.RemoveAt(0);

            if (_autoScroll)
                ScrollToEnd();
        }

        private void ScrollToEnd()
        {
            _logScrollViewer.Offset = new Vector(0, _logScrollViewer.Extent.Height);
        }

        public string LatestLogLine => _logLines.Count > 0 ? _logLines[^1] : "";

        /// <summary>
        /// Returns the backing log collection so overlays can show live chat.
        /// </summary>
        public ObservableCollection<string> GetRecentLogLines(int _) => _logLines;

        public void ClearInput()
        {
            _commandInput.Text = string.Empty;
        }

        private void OnCommandKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    SubmitCommand();
                    e.Handled = true;
                    break;

                case Key.Up:
                    NavigateHistory(-1);
                    e.Handled = true;
                    break;

                case Key.Down:
                    NavigateHistory(1);
                    e.Handled = true;
                    break;

                case Key.PageUp:
                    ScrollLog(-10);
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    ScrollLog(10);
                    e.Handled = true;
                    break;
            }
        }

        private void OnCommandTextChanged(object? sender, TextChangedEventArgs e)
        {
            var backend = TuiConsoleBackend.Instance;
            if (backend == null) return;
            string text = _commandInput.Text ?? string.Empty;
            int cursor = _commandInput.CaretIndex;
            backend.OnInputChanged(text, cursor);
        }

        private void SubmitCommand()
        {
            string command = _commandInput.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(command))
                return;

            _commandHistory.Add(command);
            _historyIndex = _commandHistory.Count;
            _commandInput.Text = string.Empty;

            AppendLogLine($"> {command}");

            _autoScroll = true;

            TuiConsoleBackend.Instance?.OnCommandSubmitted(command);
        }

        private void NavigateHistory(int direction)
        {
            if (_commandHistory.Count == 0)
                return;

            _historyIndex += direction;
            if (_historyIndex < 0) _historyIndex = 0;
            if (_historyIndex >= _commandHistory.Count)
            {
                _historyIndex = _commandHistory.Count;
                _commandInput.Text = string.Empty;
                return;
            }

            _commandInput.Text = _commandHistory[_historyIndex];
            _commandInput.CaretIndex = _commandInput.Text?.Length ?? 0;
        }

        private void ScrollLog(int delta)
        {
            var sv = _logScrollViewer;
            var newY = sv.Offset.Y + delta;
            newY = Math.Max(0, Math.Min(newY, sv.Extent.Height - sv.Viewport.Height));
            sv.Offset = new Vector(0, newY);

            _autoScroll = newY >= sv.Extent.Height - sv.Viewport.Height - 2;
        }

        private void OnLogScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            var sv = _logScrollViewer;
            _autoScroll = sv.Offset.Y >= sv.Extent.Height - sv.Viewport.Height - 2;
        }

        public void ShowOverlay(Control content, Action? onClose = null)
        {
            if (_overlayContent != null)
                HideOverlay();

            _overlayContent = content;
            _overlayCloseCallback = onClose;
            _mainContent.IsVisible = false;

            _rootPanel.Children.Add(_overlayContent);
        }

        public void HideOverlay()
        {
            if (_overlayContent == null) return;

            _rootPanel.Children.Remove(_overlayContent);

            _overlayContent = null;
            _mainContent.IsVisible = true;

            var cb = _overlayCloseCallback;
            _overlayCloseCallback = null;
            cb?.Invoke();

            _commandInput.Focus();
        }

        public bool HasOverlay => _overlayContent != null;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _overlayContent != null)
            {
                HideOverlay();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _commandInput.Focus();
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }
}
