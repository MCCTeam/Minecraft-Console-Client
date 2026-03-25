using System;
using System.Collections.Generic;
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
        private long _lastLogClickTicks;
        private const int DoubleClickMsec = 500;

        private readonly Border _suggestionBorder;
        private readonly StackPanel _suggestionPanel;
        private CommandSuggestion[] _suggestions = Array.Empty<CommandSuggestion>();
        private (int Start, int End) _suggestionRange;
        private int _selectedSuggestionIndex = -1;
        private int _suggestionViewTop;
        private bool _acceptingSuggestion;
        private bool _tabCycling;

        private int MaxVisibleSuggestions =>
            Math.Max(1, Settings.Config.Console.CommandSuggestion.Max_Displayed_Suggestions);

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
            _logScrollViewer.PointerPressed += OnLogAreaPointerPressed;

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

            _commandInput.AddHandler(KeyDownEvent, OnCommandKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
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

            _suggestionPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Vertical,
            };
            _suggestionPanel.PointerWheelChanged += OnSuggestionWheelChanged;
            _suggestionBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(1),
                Child = _suggestionPanel,
                IsVisible = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 1),
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
                Children = { _mainContent, _notificationBorder, _suggestionBorder }
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

        private void OnLogAreaPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(null).Properties;
            if (!props.IsLeftButtonPressed)
            {
                Dispatcher.UIThread.Post(() => _commandInput.Focus());
                return;
            }

            bool shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
            if (shift)
                return;

            long now = Environment.TickCount64;
            long elapsed = now - _lastLogClickTicks;
            _lastLogClickTicks = now;

            if (elapsed < DoubleClickMsec)
            {
                ShowNotification("Hold Shift + Left-click to select and copy text.", 3000);
                _lastLogClickTicks = 0;
            }

            Dispatcher.UIThread.Post(() => _commandInput.Focus());
        }

        #endregion

        #region Input

        public void ClearInput()
        {
            _commandInput.Text = string.Empty;
        }

        private void OnCommandKeyDown(object? sender, KeyEventArgs e)
        {
            if (_tabCycling && e.Key is not (Key.Tab or Key.Up or Key.Down or Key.Escape))
                _tabCycling = false;

            bool ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;

            if (e.Key == Key.C && ctrl)
            {
                HandleCtrlC();
                e.Handled = true;
                return;
            }

            if ((e.Key == Key.Back || e.Key == Key.W) && ctrl)
            {
                DeleteWordBackward();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left && ctrl)
            {
                MoveCaretWordLeft();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Right && ctrl)
            {
                MoveCaretWordRight();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.A && ctrl)
            {
                _commandInput.CaretIndex = 0;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.E && ctrl)
            {
                _commandInput.CaretIndex = _commandInput.Text?.Length ?? 0;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.U && ctrl)
            {
                _commandInput.Text = string.Empty;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && SuggestionsVisible)
            {
                ClearSuggestions();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab && SuggestionsVisible)
            {
                if (_tabCycling)
                {
                    MoveSuggestionSelection(1);
                    ApplySuggestionInPlace(_selectedSuggestionIndex);
                }
                else
                {
                    ApplySuggestionInPlace(_selectedSuggestionIndex);
                    _tabCycling = true;
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab)
            {
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
                    if (SuggestionsVisible)
                        MoveSuggestionSelection(-1);
                    else
                        NavigateHistory(-1);
                    e.Handled = true;
                    break;

                case Key.Down:
                    if (SuggestionsVisible)
                        MoveSuggestionSelection(1);
                    else
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

        private void DeleteWordBackward()
        {
            string text = _commandInput.Text ?? "";
            int caret = _commandInput.CaretIndex;
            if (caret == 0 || text.Length == 0) return;

            int pos = caret - 1;
            while (pos > 0 && text[pos - 1] == ' ') pos--;
            while (pos > 0 && text[pos - 1] != ' ') pos--;

            _commandInput.Text = text[..pos] + text[caret..];
            _commandInput.CaretIndex = pos;
        }

        private void MoveCaretWordLeft()
        {
            string text = _commandInput.Text ?? "";
            int pos = _commandInput.CaretIndex;
            if (pos == 0) return;

            pos--;
            while (pos > 0 && text[pos - 1] == ' ') pos--;
            while (pos > 0 && text[pos - 1] != ' ') pos--;

            _commandInput.CaretIndex = pos;
        }

        private void MoveCaretWordRight()
        {
            string text = _commandInput.Text ?? "";
            int pos = _commandInput.CaretIndex;
            if (pos >= text.Length) return;

            while (pos < text.Length && text[pos] != ' ') pos++;
            while (pos < text.Length && text[pos] == ' ') pos++;

            _commandInput.CaretIndex = pos;
        }

        private void OnCommandTextChanged(object? sender, TextChangedEventArgs e)
        {
            string text = _commandInput.Text ?? string.Empty;

            if (text.Contains('\n') || text.Contains('\r'))
            {
                string cleaned = text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
                _commandInput.Text = cleaned;
                _commandInput.CaretIndex = cleaned.Length;
                return;
            }

            if (_acceptingSuggestion || _tabCycling)
                return;

            if (string.IsNullOrEmpty(text))
            {
                ClearSuggestions();
                return;
            }

            var backend = TuiConsoleBackend.Instance;
            if (backend == null) return;
            int cursor = _commandInput.CaretIndex;
            backend.OnInputChanged(text, cursor);
        }

        private void SubmitCommand()
        {
            string command = _commandInput.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(command))
                return;

            ClearSuggestions();
            _tabCycling = false;

            _commandHistory.Add(command);
            _historyIndex = _commandHistory.Count;

            _acceptingSuggestion = true;
            try { _commandInput.Text = string.Empty; }
            finally { _acceptingSuggestion = false; }

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

            string historyText = _commandHistory[_historyIndex];
            _commandInput.Text = historyText;
            _commandInput.CaretIndex = historyText.Length;
            Dispatcher.UIThread.Post(() => _commandInput.CaretIndex = historyText.Length,
                DispatcherPriority.Input);
        }

        #endregion

        #region Suggestions

        private const int PromptWidth = 2; // "> "
        private const int BorderAndPadding = 2; // 1 border + 1 padding on each side

        internal void UpdateSuggestions(CommandSuggestion[] suggestions, (int Start, int End) range)
        {
            if (suggestions.Length == 0)
            {
                ClearSuggestions();
                return;
            }

            _suggestions = suggestions;
            _suggestionRange = range;
            _selectedSuggestionIndex = 0;
            _suggestionViewTop = 0;

            int leftOffset = PromptWidth + range.Start - BorderAndPadding;
            double screenWidth = Bounds.Width;
            if (screenWidth < 1)
                screenWidth = 80;

            if (leftOffset < 0)
                leftOffset = 0;

            _suggestionBorder.Margin = new Thickness(leftOffset, 0, 0, 1);
            _suggestionBorder.MaxWidth = Math.Max(10, screenWidth - leftOffset);

            RebuildSuggestionItems();
            _suggestionBorder.IsVisible = true;
        }

        internal void ClearSuggestions()
        {
            if (!_suggestionBorder.IsVisible && _suggestions.Length == 0)
                return;

            _suggestions = Array.Empty<CommandSuggestion>();
            _selectedSuggestionIndex = -1;
            _suggestionBorder.IsVisible = false;
            _suggestionPanel.Children.Clear();
        }

        private bool SuggestionsVisible => _suggestionBorder.IsVisible && _suggestions.Length > 0;

        private void RebuildSuggestionItems()
        {
            _suggestionPanel.Children.Clear();

            int visibleCount = Math.Min(_suggestions.Length, MaxVisibleSuggestions);
            int viewBottom = _suggestionViewTop + visibleCount;

            for (int i = _suggestionViewTop; i < viewBottom && i < _suggestions.Length; i++)
            {
                var sug = _suggestions[i];
                int index = i;

                string label = sug.Text;
                if (!string.IsNullOrEmpty(sug.Tooltip))
                    label += "  " + sug.Tooltip;

                var tb = new TextBlock
                {
                    Text = label,
                    Padding = new Thickness(1, 0),
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Background = i == _selectedSuggestionIndex
                        ? new SolidColorBrush(Color.FromRgb(0, 90, 160))
                        : Brushes.Transparent,
                };

                var row = new Border
                {
                    Child = tb,
                    Background = Brushes.Transparent,
                };

                row.PointerPressed += (_, _) =>
                {
                    _selectedSuggestionIndex = index;
                    ApplySuggestionInPlace(index);
                    _tabCycling = true;
                };
                row.PointerEntered += (_, _) =>
                {
                    if (_selectedSuggestionIndex != index)
                    {
                        _selectedSuggestionIndex = index;
                        UpdateSuggestionHighlight();
                    }
                };

                _suggestionPanel.Children.Add(row);
            }

            if (_suggestions.Length > MaxVisibleSuggestions)
            {
                string scrollHint = $"[{_suggestionViewTop + 1}-{viewBottom}/{_suggestions.Length}]";
                var hintTb = new TextBlock
                {
                    Text = scrollHint,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    Padding = new Thickness(1, 0),
                    TextAlignment = TextAlignment.Right,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                _suggestionPanel.Children.Add(hintTb);
            }
        }

        private void UpdateSuggestionHighlight()
        {
            int visibleCount = Math.Min(_suggestions.Length, MaxVisibleSuggestions);
            for (int i = 0; i < visibleCount && i < _suggestionPanel.Children.Count; i++)
            {
                if (_suggestionPanel.Children[i] is Border border && border.Child is TextBlock tb)
                {
                    int dataIndex = _suggestionViewTop + i;
                    tb.Background = dataIndex == _selectedSuggestionIndex
                        ? new SolidColorBrush(Color.FromRgb(0, 90, 160))
                        : Brushes.Transparent;
                }
            }
        }

        private void MoveSuggestionSelection(int direction)
        {
            if (_suggestions.Length == 0) return;

            _selectedSuggestionIndex += direction;
            if (_selectedSuggestionIndex < 0)
                _selectedSuggestionIndex = _suggestions.Length - 1;
            else if (_selectedSuggestionIndex >= _suggestions.Length)
                _selectedSuggestionIndex = 0;

            int visibleCount = Math.Min(_suggestions.Length, MaxVisibleSuggestions);
            if (_selectedSuggestionIndex < _suggestionViewTop)
            {
                _suggestionViewTop = _selectedSuggestionIndex;
                RebuildSuggestionItems();
            }
            else if (_selectedSuggestionIndex >= _suggestionViewTop + visibleCount)
            {
                _suggestionViewTop = _selectedSuggestionIndex - visibleCount + 1;
                RebuildSuggestionItems();
            }
            else
            {
                UpdateSuggestionHighlight();
            }
        }

        private void OnSuggestionWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (!SuggestionsVisible) return;

            int direction = e.Delta.Y > 0 ? -1 : 1;
            ScrollSuggestionViewport(direction);
            e.Handled = true;
        }

        private void ScrollSuggestionViewport(int direction)
        {
            if (_suggestions.Length <= MaxVisibleSuggestions) return;

            int newTop = _suggestionViewTop + direction;
            int maxTop = _suggestions.Length - MaxVisibleSuggestions;
            newTop = Math.Clamp(newTop, 0, maxTop);

            if (newTop == _suggestionViewTop) return;
            _suggestionViewTop = newTop;

            int viewBottom = _suggestionViewTop + MaxVisibleSuggestions;
            if (_selectedSuggestionIndex < _suggestionViewTop)
                _selectedSuggestionIndex = _suggestionViewTop;
            else if (_selectedSuggestionIndex >= viewBottom)
                _selectedSuggestionIndex = viewBottom - 1;

            RebuildSuggestionItems();
        }

        private void ApplySuggestionText(int index)
        {
            if (index < 0 || index >= _suggestions.Length) return;

            string text = _commandInput.Text ?? "";
            string selected = _suggestions[index].Text;

            int start = Math.Min(_suggestionRange.Start, text.Length);
            int end = Math.Min(_suggestionRange.End, text.Length);

            string before = text[..start];
            string after = text[end..];
            string newText = before + selected + after;

            _commandInput.Text = newText;
            _commandInput.CaretIndex = before.Length + selected.Length;

            _suggestionRange = (start, start + selected.Length);
        }

        private void ApplySuggestionInPlace(int index)
        {
            if (index < 0 || index >= _suggestions.Length) return;

            _acceptingSuggestion = true;
            try
            {
                ApplySuggestionText(index);
            }
            finally
            {
                _acceptingSuggestion = false;
            }
            UpdateSuggestionHighlight();
        }

        #endregion

        #region Ctrl+C

        internal void HandleCtrlC()
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

            int heartsFilled = (int)Math.Ceiling(health / 20f * 10);
            heartsFilled = Math.Clamp(heartsFilled, 0, 10);
            int foodFilled = (int)Math.Ceiling(food / 20f * 10);
            foodFilled = Math.Clamp(foodFilled, 0, 10);

            _statusBar.Inlines?.Clear();
            _statusBar.Inlines ??= new Avalonia.Controls.Documents.InlineCollection();

            var healthText = BuildBarText(heartsFilled, 10, "\u2764\ufe0f", " \u2661 ");
            var foodText = BuildBarText(foodFilled, 10, "\ud83c\udf56", " \u25cb ");

            _statusBar.Inlines.Add(new Avalonia.Controls.Documents.Run(healthText)
            {
                Foreground = new SolidColorBrush(Color.FromRgb(255, 85, 85)),
            });
            _statusBar.Inlines.Add(new Avalonia.Controls.Documents.Run($" {health:F1}  ")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(255, 150, 150)),
            });
            _statusBar.Inlines.Add(new Avalonia.Controls.Documents.Run(foodText)
            {
                Foreground = new SolidColorBrush(Color.FromRgb(200, 160, 80)),
            });
            _statusBar.Inlines.Add(new Avalonia.Controls.Documents.Run($" {food}")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(220, 190, 100)),
            });

            _statusBar.IsVisible = true;
        }

        private static string BuildBarText(int filled, int total, string filledChar, string emptyChar)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < filled; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(filledChar);
            }
            for (int i = filled; i < total; i++)
            {
                sb.Append(emptyChar);
            }
            return sb.ToString();
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
