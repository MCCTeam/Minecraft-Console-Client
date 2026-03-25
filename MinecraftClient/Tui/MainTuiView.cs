using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MinecraftClient.Tui
{
    public class MainTuiView : UserControl
    {
        private const int MaxLogLines = 5000;
        private const int CtrlCDoublePressMsec = 1500;

        private readonly ObservableCollection<string> _logLines = new();
        private readonly ObservableCollection<Control> _logControls = new();
        private readonly ItemsControl _logItemsControl;
        private readonly ScrollViewer _logScrollViewer;
        private readonly TextBox _commandInput;
        private bool _autoScroll = true;
        private bool _programmaticScroll;
        private readonly ObservableCollection<string> _commandHistory = new();
        private int _historyIndex = -1;

        private readonly Panel _rootPanel;
        private readonly DockPanel _mainContent;
        private Control? _overlayContent;
        private Action? _overlayCloseCallback;

        private readonly TextBlock _statusBar;
        private readonly Border _notificationBorder;
        private readonly TextBlock _notificationText;
        private long _lastCtrlCTicks;

        public MainTuiView()
        {
            Background = Brushes.Black;

            _statusBar = new TextBlock
            {
                Foreground = Brushes.Gray,
                Background = Brushes.Black,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                IsVisible = false,
            };

            _logItemsControl = new ItemsControl
            {
                ItemsSource = _logControls,
                Focusable = false,
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
                Dispatcher.UIThread.Post(() => _commandInput.Focus());
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

            _notificationText = new TextBlock
            {
                Foreground = Brushes.Yellow,
                Padding = new Thickness(1, 0),
            };
            _notificationBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(60, 50, 20)),
                BorderBrush = Brushes.Yellow,
                BorderThickness = new Thickness(1),
                Child = _notificationText,
                IsVisible = false,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            };

            _mainContent = new DockPanel
            {
                Background = Brushes.Black,
                Children =
                {
                    SetDock(_statusBar, Dock.Top),
                    SetDock(inputRow, Dock.Bottom),
                    _logScrollViewer
                }
            };

            _rootPanel = new Panel
            {
                Background = Brushes.Black,
                Children = { _mainContent, _notificationBorder }
            };

            Content = _rootPanel;

            StartStatusBarTimer();
        }

        private static Control SetDock(Control control, Dock dock)
        {
            DockPanel.SetDock(control, dock);
            return control;
        }

        #region Log output

        public void AppendLogLine(string text)
        {
            _logLines.Add(text);

            var tb = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
            };
            _logControls.Add(tb);

            TrimLog();

            if (_autoScroll)
                ScheduleScrollToEnd();
        }

        public void AppendFormattedLogLine(string text)
        {
            _logLines.Add(text);

            var tb = McColorParser.CreateColoredTextBlock(text, TextWrapping.Wrap);
            _logControls.Add(tb);

            TrimLog();

            if (_autoScroll)
                ScheduleScrollToEnd();
        }

        private void TrimLog()
        {
            while (_logLines.Count > MaxLogLines)
            {
                _logLines.RemoveAt(0);
                _logControls.RemoveAt(0);
            }
        }

        private void ScheduleScrollToEnd()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _programmaticScroll = true;
                var sv = _logScrollViewer;
                sv.Offset = new Vector(0, sv.Extent.Height);
                _programmaticScroll = false;
            }, DispatcherPriority.Background);
        }

        public string LatestLogLine => _logLines.Count > 0 ? _logLines[^1] : "";

        public ObservableCollection<string> GetRecentLogLines(int _) => _logLines;

        #endregion

        #region Input

        public void ClearInput()
        {
            _commandInput.Text = string.Empty;
        }

        private void OnCommandKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && (e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                HandleCtrlC();
                e.Handled = true;
                return;
            }

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

            _autoScroll = true;

            AppendLogLine($"> {command}");

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

        #endregion

        #region Ctrl+C

        private void HandleCtrlC()
        {
            long now = Environment.TickCount64;
            long elapsed = now - _lastCtrlCTicks;

            if (_lastCtrlCTicks > 0 && elapsed < CtrlCDoublePressMsec)
            {
                HideNotification();
                TuiConsoleBackend.Instance?.Shutdown();
                return;
            }

            _lastCtrlCTicks = now;

            string inputText = _commandInput.Text?.Trim() ?? "";
            if (inputText.Length > 0)
            {
                _commandInput.Text = string.Empty;
                ShowNotification("Input cleared. Press Ctrl+C again to quit.", CtrlCDoublePressMsec);
            }
            else
            {
                ShowNotification("Press Ctrl+C again to quit MCC.", CtrlCDoublePressMsec);
            }
        }

        private void ShowNotification(string message, int autoHideMs)
        {
            _notificationText.Text = message;
            _notificationBorder.IsVisible = true;

            var timer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(autoHideMs),
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                HideNotification();
            };
            timer.Start();
        }

        private void HideNotification()
        {
            _notificationBorder.IsVisible = false;
        }

        #endregion

        #region Scrolling

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
            if (_programmaticScroll) return;

            var sv = _logScrollViewer;
            _autoScroll = sv.Offset.Y >= sv.Extent.Height - sv.Viewport.Height - 2;
        }

        #endregion

        #region Status Bar (Health / Food)

        private void StartStatusBarTimer()
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            timer.Tick += (_, _) => UpdateStatusBar();
            timer.Start();
        }

        private void UpdateStatusBar()
        {
            if (McClient.Instance is not McClient client)
            {
                _statusBar.IsVisible = false;
                return;
            }

            int gamemode = client.GetGamemode();
            if (gamemode != 0 && gamemode != 2)
            {
                _statusBar.IsVisible = false;
                return;
            }

            float health = client.GetHealth();
            int food = client.GetSaturation();

            string hearts = RenderBar(health, 20f, "\u2764", "\u2661");
            string drumsticks = RenderBar(food, 20f, "\u2689", "\u25cb");

            _statusBar.Inlines?.Clear();
            _statusBar.Inlines ??= new Avalonia.Controls.Documents.InlineCollection();

            _statusBar.Inlines.Add(new Avalonia.Controls.Documents.Run(hearts + " ")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(255, 85, 85)),
            });
            _statusBar.Inlines.Add(new Avalonia.Controls.Documents.Run($"{health:F1}")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(255, 120, 120)),
            });
            _statusBar.Inlines.Add(new Avalonia.Controls.Documents.Run("  " + drumsticks + " ")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(200, 170, 80)),
            });
            _statusBar.Inlines.Add(new Avalonia.Controls.Documents.Run($"{food}")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(220, 190, 100)),
            });

            _statusBar.IsVisible = true;
        }

        private static string RenderBar(float value, float max, string filledChar, string emptyChar)
        {
            int total = 10;
            int filled = (int)Math.Ceiling(value / max * total);
            filled = Math.Clamp(filled, 0, total);
            return new string('x', filled).Replace("x", filledChar)
                 + new string('x', total - filled).Replace("x", emptyChar);
        }

        #endregion

        #region Overlay

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

        #endregion

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Dispatcher.UIThread.Post(() =>
            {
                _commandInput.Focus();
            }, DispatcherPriority.Loaded);
        }
    }
}
