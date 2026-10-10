using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Presentation;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Iciclecreek.Avalonia.WindowManager;
using DMCBK.Core;

namespace Mcc.Cli.Tui.Hosting;

/// <summary>
/// The primary TUI surface: a scrollback chat log, a command input line with a live suggestion popup, a server/vitals status bar, the startup banner, a managed-window host, and an overlay host for the container / book / dialog / tab views.
/// Ported in behavior from the legacy <c>MainTuiView</c> but fed exclusively by the new core (<see cref="GameApi"/>, Component chat) with no <c>McClient</c>/<c>ConsoleIO</c> coupling.
/// </summary>
public sealed class MainTuiView : UserControl
{
    private readonly int _maxLogLines;
    private readonly int _maxDisplayedSuggestions;
    private readonly bool _displayIconBanner;

    private readonly ObservableCollection<Control> _logControls = new();
    private readonly List<string> _history = new();
    private readonly Panel _rootPanel;
    private readonly DockPanel _mainContent;
    private readonly ScrollViewer _logScroll;
    private readonly TextBox _input;
    private readonly TextBlock _statusBar;
    private readonly Border _suggestionBorder;
    private readonly StackPanel _suggestionPanel;
    private readonly WindowsPanel _windowHost;

    private GameApi? _game;
    private Umpk.Text.ITranslationSource _translations = new DMCBK.Core.Localization.HostTranslations();
    private bool _showEffectNames; // legacy Advanced.ShowEffectNamesInTUI defaults OFF (Settings.cs:838)
    private DispatcherTimer? _statusTimer;
    private Control? _overlay;
    private Action? _overlayOnClose;
    private TuiTooltipService? _tooltipService;
    private int _historyIndex = -1;
    private IReadOnlyList<string> _suggestions = [];
    private int _suggestionIndex;
    private int _suggestReplaceStart;
    private int _suggestReplaceEnd;
    private bool _autoScroll = true;
    private DateTime _lastCtrlC = DateTime.MinValue;

    // The first visible row of the suggestion list.
    // The popup shows at most MaxVisibleSuggestions rows, so a selection that walks past either edge scrolls the window rather than being clamped out of view.
    // Mirrors MinecraftClient/Tui/MainTuiView.cs:741-766 and the submodule's own ConsoleSuggestion.cs:195-245.
    private int _suggestionViewTop;

    // Guards against the popup rebuilding itself out from under a programmatic edit.
    // Writing _input.Text raises TextChanged, which re-queries completions, whose UpdateSuggestions resets the selection to 0 - so without these, Tab always re-inserts the first entry and history navigation re-opens the popup.
    // Legacy carries the same pair (MinecraftClient/Tui/MainTuiView.cs:66, :531-532).
    private bool _acceptingSuggestion;
    private bool _tabCycling;

    /// <summary>Raised (on the UI thread) when the user submits a non-empty input line.</summary>
    public event Action<string>? LineEntered;

    /// <summary>Raised when the user asks to quit (double Ctrl+C or the exit control).</summary>
    public event Action? ShutdownRequested;

    /// <summary>Raised as the input text changes, so the host can compute completions.</summary>
    public event Action<string, int>? InputChanged;

    /// <summary>Raised when the top-bar control or Ctrl+M requests the management launcher.</summary>
    public event Action? MainMenuRequested;

    /// <summary>
    /// Builds the view.
    /// <paramref name="maxLogLines"/> is console.toml's <c>TuiLogScrollback</c> (default 3000, matching legacy's resolved default), <paramref name="maxDisplayedSuggestions"/> is console.toml's <c>CommandSuggestion.MaxDisplayedSuggestions</c> (default 10), and <paramref name="displayIconBanner"/> is console.toml's <c>Display_Icon_Banner</c>, gating the startup banner exactly like the classic host's own banner gate.
    /// </summary>
    public MainTuiView(int maxLogLines = 3000, int maxDisplayedSuggestions = 10, bool displayIconBanner = true)
    {
        _maxLogLines = Math.Max(1, maxLogLines);
        _maxDisplayedSuggestions = Math.Max(1, maxDisplayedSuggestions);
        _displayIconBanner = displayIconBanner;

        // Every surface below paints its own Background explicitly.
        // An unpainted Avalonia control is transparent, so it shows whatever ground the terminal supplies, and the greys chosen for a black TUI then sit on that ground at almost no contrast - which is the "text is nearly the background colour" defect.
        // Legacy paints Black at six points for the same reason (MinecraftClient/Tui/MainTuiView.cs:76, 81, 97, 111, 132, 199).
        Background = Brushes.Black;

        _statusBar = new TextBlock
        {
            Background = Brushes.Black,
            Foreground = Brushes.Gray,
            Padding = new Thickness(1, 0, 1, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        Button mainMenuButton = Management.ManagementUi.HeaderButton(
            Strings.TuiMainMenuButton,
            () => MainMenuRequested?.Invoke(),
            Management.ManagementButtonKind.Secondary);
        mainMenuButton.HorizontalAlignment = HorizontalAlignment.Right;

        var topBar = new Grid
        {
            Background = Brushes.Black,
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
        };
        topBar.Children.Add(_statusBar);
        Grid.SetColumn(mainMenuButton, 1);
        topBar.Children.Add(mainMenuButton);
        DockPanel.SetDock(topBar, Dock.Top);

        var logItems = new ItemsControl
        {
            ItemsSource = _logControls,
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel()),
        };
        _logScroll = new ScrollViewer
        {
            Content = logItems,
            Background = Brushes.Black,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        _logScroll.ScrollChanged += OnScrollChanged;

        _input = new TextBox
        {
            Watermark = Strings.TuiInputWatermark,
            AcceptsReturn = false,
            BorderThickness = new Thickness(0),
            Background = Brushes.Black,
            Foreground = Brushes.White,
        };
        // Tunnel, not bubble: Avalonia runs TextBox's class handler before instance handlers on the way up, and TextBox.OnKeyDown matches Ctrl+C against PlatformHotkeyConfiguration.Copy and sets Handled, which made the Ctrl+C branch below dead code.
        // Legacy registers Tunnel for the same reason (MinecraftClient/Tui/MainTuiView.cs:118).
        _input.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
        _input.TextChanged += OnInputTextChanged;

        var prompt = new TextBlock { Text = "> ", Foreground = Brushes.Cyan };
        DockPanel.SetDock(prompt, Dock.Left);
        var inputRow = new DockPanel { Background = Brushes.Black, Children = { prompt, _input } };
        DockPanel.SetDock(inputRow, Dock.Bottom);

        _mainContent = new DockPanel
        {
            Background = Brushes.Black,
            LastChildFill = true,
            Children = { topBar, inputRow, _logScroll },
        };

        _suggestionPanel = new StackPanel();
        _suggestionBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 45)),
            BorderBrush = Brushes.DarkCyan,
            BorderThickness = new Thickness(1),
            Child = _suggestionPanel,
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(2, 0, 0, 1),
        };

        _rootPanel = new Panel
        {
            Children = { _mainContent, _suggestionBorder },
        };

        // Consolonia's Gallery hosts ManagedWindow instances in a WindowsPanel.
        // Apply its current WindowsPanelWorkaround focus behavior locally because the matching 11.3 package predates that helper type: the host itself must not swallow focus or trap Tab navigation.
        _windowHost = new WindowsPanel { Content = _rootPanel, Focusable = false };
        KeyboardNavigation.SetTabNavigation(_windowHost, KeyboardNavigationMode.Continue);
        Content = _windowHost;
        AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);
        AddBanner();
    }

    private void AddBanner()
    {
        if (!_displayIconBanner)
            return;

        foreach (Control line in BannerBuilder.BuildLines())
            _logControls.Add(line);
    }

    /// <summary>Whether an overlay (container/book/dialog/tab/map) is currently shown.</summary>
    public bool HasOverlay => _overlay is not null;

    /// <summary>The currently shown overlay control, or null when none is open.</summary>
    public Control? CurrentOverlay => _overlay;

    /// <summary>
    /// The shared floating tooltip layer, lazily created above every other child in the root panel so it floats over the main content or an overlay.
    /// Callers (the minimap hover, and any future hover source) hold the same single instance for the life of the view.
    /// </summary>
    internal TuiTooltipService GetTooltipService()
        => _tooltipService ??= new TuiTooltipService(_rootPanel, _windowHost);

    /// <summary>Shows a non-modal managed tool window over the main TUI content.</summary>
    internal void ShowWindow(ManagedWindow window) => _windowHost.Show(window);

    /// <summary>The surface whose bounds constrain managed tool-window placement.</summary>
    internal Control WindowSurface => _windowHost;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Focus the input as soon as the view is shown so typing works before (and after) a session joins.
        if (_overlay is null)
            _input.Focus();
    }

    /// <summary>Binds the live game facade so the status bar and minimap can read snapshots.</summary>
    public void Bind(GameApi game, Umpk.Text.ITranslationSource? translations = null, bool showEffectNames = false)
    {
        _game = game;
        if (translations is not null)
            _translations = translations;

        _showEffectNames = showEffectNames;
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        // Task.Run, not a bare call.
        // A DispatcherTimer tick runs ON THE UI THREAD, and an async method runs synchronously up to its first await, so evaluating Player.GetStatusAsync() handed work to the session loop and waited for it there, stalling the UI once a second.
        // Measured: Escape took about a second to visibly close an overlay, matching this interval exactly.
        // The refresh needs nothing from the UI thread until its Post() at the end.
        _statusTimer.Tick += (_, _) => _ = Task.Run(RefreshStatusAsync);
        _statusTimer.Start();
        _ = Task.Run(RefreshStatusAsync);
        _input.Focus();
    }

    /// <summary>Appends a plain-text line to the chat log.</summary>
    public void AppendLogLine(string text)
        => AppendControlLine(new SelectableTextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            // Explicit, because an unset Foreground inherits whatever the host theme supplies, which on a dark terminal rendered grey-on-near-black.
            // Legacy paints log lines white (MinecraftClient/Tui/MainTuiView.cs:199, :210).
            Foreground = Brushes.White,
        });

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (!ShouldOpenMainMenu(
            e.Key,
            e.KeyModifiers,
            hasOverlay: _overlay is not null,
            inputIsEmpty: string.IsNullOrEmpty(_input.Text)))
            return;

        MainMenuRequested?.Invoke();
        e.Handled = true;
    }

    internal static bool IsMainMenuGesture(Key key, KeyModifiers modifiers)
        // Several terminal backends report Ctrl+M as Ctrl+Enter because both originate from carriage return.
        // Accept that normalized form as well as the native Avalonia Ctrl+M event.
        => modifiers.HasFlag(KeyModifiers.Control) && key is Key.M or Key.Enter;

    internal static bool ShouldOpenMainMenu(
        Key key,
        KeyModifiers modifiers,
        bool hasOverlay,
        bool inputIsEmpty)
    {
        // A real Ctrl+M stays global.
        // Traditional terminal input cannot distinguish Ctrl+M from Enter at all, so plain Enter is also accepted only at an empty main prompt, where Enter was a no-op.
        // The narrower scope leaves command submission and overlay-local Ctrl+Enter shortcuts unchanged.
        if (IsMainMenuGesture(key, modifiers))
            return key == Key.M || !hasOverlay;

        return !hasOverlay
            && key == Key.Enter
            && (modifiers.HasFlag(KeyModifiers.Control) || (modifiers == KeyModifiers.None && inputIsEmpty));
    }

    /// <summary>Appends a pre-built control (a Component-rendered line) to the chat log.</summary>
    public void AppendControlLine(Control control)
    {
        _logControls.Add(control);
        while (_logControls.Count > _maxLogLines)
            _logControls.RemoveAt(0);

        if (_autoScroll)
            Dispatcher.UIThread.Post(() => _logScroll.ScrollToEnd(), DispatcherPriority.Background);
    }

    /// <summary>
    /// Clears the chat log.
    /// The banner is NOT re-added: it is startup output that scrolls away like any other line, which is what the legacy client does (MinecraftClient/Tui/MainTuiView.cs:271-276, and the banner itself is written once from Program.cs:271).
    /// Re-adding it here is what made it look pinned.
    /// </summary>
    public void ClearLog() => _logControls.Clear();

    /// <summary>Shows an overlay control, hiding the main content until dismissed.</summary>
    public void ShowOverlay(Control overlay, Action? onClose = null)
    {
        HideOverlay();
        _overlay = overlay;
        _overlayOnClose = onClose;
        _mainContent.IsVisible = false;
        _suggestionBorder.IsVisible = false;
        _rootPanel.Children.Add(overlay);
        overlay.Focus();
    }

    /// <summary>Dismisses the current overlay and restores the main content.</summary>
    public void HideOverlay()
    {
        if (_overlay is null)
            return;

        _rootPanel.Children.Remove(_overlay);
        _overlay = null;
        _mainContent.IsVisible = true;
        Action? onClose = _overlayOnClose;
        _overlayOnClose = null;
        onClose?.Invoke();
        _input.Focus();
    }

    /// <summary>
    /// Closes the active browser and places text in the command input for review.
    /// The text is not executed.
    /// </summary>
    internal void InsertCommandText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        HideOverlay();
        SetCommandText(text);
        _input.Focus();
    }

    /// <summary>Replaces the suggestion popup contents (called by the host after a completion query).</summary>
    public void UpdateSuggestions(IReadOnlyList<string> suggestions, int replaceStart, int replaceEnd)
    {
        _suggestions = suggestions;
        _suggestionIndex = 0;
        _suggestionViewTop = 0;
        _suggestReplaceStart = replaceStart;
        _suggestReplaceEnd = replaceEnd;
        RenderSuggestions();
    }

    /// <summary>Clears the suggestion popup.</summary>
    public void ClearSuggestions()
    {
        _suggestions = [];
        _suggestionIndex = 0;
        _suggestionViewTop = 0;
        _tabCycling = false;
        _suggestionPanel.Children.Clear();
        _suggestionBorder.IsVisible = false;
    }

    /// <summary>
    /// Toggles password-masking on the input line: asterisks are echoed while typing instead of the plaintext.
    /// Uses Avalonia's own <see cref="TextBox.PasswordChar"/>, which Consolonia's rendering honors because it renders the standard Avalonia <c>TextBox</c>/<c>TextPresenter</c> visual tree rather than a reimplementation of it.
    /// </summary>
    public void SetPasswordMode(bool enabled) => _input.PasswordChar = enabled ? '*' : default;

    /// <summary>Handles a Ctrl+C: two within 1.5s quits, one clears input or hints.</summary>
    public void HandleCtrlC()
    {
        DateTime now = DateTime.UtcNow;
        if ((now - _lastCtrlC) < TimeSpan.FromMilliseconds(1500))
        {
            ShutdownRequested?.Invoke();
            return;
        }

        _lastCtrlC = now;
        if (!string.IsNullOrEmpty(_input.Text))
            _input.Text = string.Empty;
        else
            AppendLogLine(Strings.TuiQuitHint);
    }

    private void RenderSuggestions()
    {
        _suggestionPanel.Children.Clear();
        if (_suggestions.Count == 0)
        {
            _suggestionBorder.IsVisible = false;
            return;
        }

        int visible = Math.Min(_suggestions.Count, _maxDisplayedSuggestions);
        _suggestionViewTop = Math.Clamp(_suggestionViewTop, 0, Math.Max(0, _suggestions.Count - visible));

        for (int i = _suggestionViewTop; i < _suggestionViewTop + visible; i++)
        {
            bool selected = i == _suggestionIndex;
            _suggestionPanel.Children.Add(new TextBlock
            {
                Text = _suggestions[i],
                // Legacy's popup colours (MinecraftClient/Tui/MainTuiView.cs:676-685): white on a deep blue bar.
                // Black-on-cyan was unreadable against the panel and matched nothing in the old client.
                Foreground = selected ? Brushes.White : Brushes.Gainsboro,
                Background = selected ? new SolidColorBrush(Color.FromRgb(0, 90, 160)) : Brushes.Transparent,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
        }

        // The overflow hint, so a long list does not look like a short one (MinecraftClient/Tui/MainTuiView.cs:711-723).
        if (_suggestions.Count > visible)
        {
            _suggestionPanel.Children.Add(new TextBlock
            {
                Text = Strings.TuiSuggestionRange(_suggestionViewTop + 1, _suggestionViewTop + visible, _suggestions.Count),
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            });
        }

        _suggestionBorder.IsVisible = true;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        double distanceFromEnd = _logScroll.Extent.Height - (_logScroll.Offset.Y + _logScroll.Viewport.Height);
        _autoScroll = distanceFromEnd < 2.0;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        // Any key other than Tab ends a cycling run, so the next Tab starts from the current query again (MinecraftClient/Tui/MainTuiView.cs:339-340).
        if (_tabCycling && e.Key is not Key.Tab)
            _tabCycling = false;

        switch (e.Key)
        {
            case Key.Enter:
                Submit();
                e.Handled = true;
                break;

            case Key.Tab:
                CycleSuggestion();
                e.Handled = true;
                break;

            case Key.Escape:
                // Only swallowed when there is a popup to close; otherwise it must keep bubbling so an open overlay can still dismiss on Escape.
                if (SuggestionsVisible)
                {
                    ClearSuggestions();
                    e.Handled = true;
                }

                break;

            // The reported defect: these moved through input history even with the popup open.
            // The popup owns the arrows while it is up, exactly as legacy does (MinecraftClient/Tui/MainTuiView.cs:429-443) and as the classic backend's submodule does (ConsoleSuggestion.cs:195-245).
            case Key.Up:
                if (SuggestionsVisible)
                    MoveSuggestionSelection(-1);
                else
                    HistoryPrev();

                e.Handled = true;
                break;

            case Key.Down:
                if (SuggestionsVisible)
                    MoveSuggestionSelection(1);
                else
                    HistoryNext();

                e.Handled = true;
                break;

            default:
                if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    HandleCtrlC();
                    e.Handled = true;
                }

                break;
        }
    }

    private void Submit()
    {
        string text = _input.Text ?? string.Empty;
        if (text.Length == 0)
            return;

        _input.Text = string.Empty;
        ClearSuggestions();
        _history.Add(text);
        _historyIndex = _history.Count;
        LineEntered?.Invoke(text);
    }

    /// <summary>True while the popup is on screen with entries in it; the arrows belong to it then.</summary>
    private bool SuggestionsVisible => _suggestionBorder.IsVisible && _suggestions.Count > 0;

    /// <summary>
    /// Moves the popup selection, wrapping at both ends and scrolling the visible window when the selection leaves it.
    /// Deliberately does NOT touch the input text, so no completion re-query is triggered and the popup survives the keypress.
    /// </summary>
    private void MoveSuggestionSelection(int direction)
    {
        if (_suggestions.Count == 0)
            return;

        _suggestionIndex += direction;
        if (_suggestionIndex < 0)
            _suggestionIndex = _suggestions.Count - 1;
        else if (_suggestionIndex >= _suggestions.Count)
            _suggestionIndex = 0;

        int visible = Math.Min(_suggestions.Count, _maxDisplayedSuggestions);
        if (_suggestionIndex < _suggestionViewTop)
            _suggestionViewTop = _suggestionIndex;
        else if (_suggestionIndex >= _suggestionViewTop + visible)
            _suggestionViewTop = _suggestionIndex - visible + 1;

        _suggestionViewTop = Math.Clamp(_suggestionViewTop, 0, Math.Max(0, _suggestions.Count - visible));
        RenderSuggestions();
    }

    /// <summary>
    /// Tab: apply the current selection on the first press, then advance on each subsequent press, so a run of Tabs walks the list.
    /// Without <see cref="_tabCycling"/> the write below re-queries completions and the selection resets to 0, which is why Tab used to re-insert the same entry forever (MinecraftClient/Tui/MainTuiView.cs:400-414).
    /// </summary>
    private void CycleSuggestion()
    {
        if (_suggestions.Count == 0)
            return;

        if (_tabCycling)
            MoveSuggestionSelection(1);

        _tabCycling = true;
        ApplySuggestionInPlace(_suggestionIndex);
    }

    private void ApplySuggestionInPlace(int index)
    {
        if (index < 0 || index >= _suggestions.Count)
            return;

        string chosen = _suggestions[index];
        string text = _input.Text ?? string.Empty;
        int start = Math.Clamp(_suggestReplaceStart, 0, text.Length);
        int end = Math.Clamp(_suggestReplaceEnd, start, text.Length);

        _acceptingSuggestion = true;
        try
        {
            _input.Text = text[..start] + chosen + text[end..];
            _input.CaretIndex = start + chosen.Length;
        }
        finally
        {
            _acceptingSuggestion = false;
        }

        RenderSuggestions();
    }

    // The popup must not be rebuilt by our own writes; see the _acceptingSuggestion/_tabCycling fields.
    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_acceptingSuggestion || _tabCycling)
            return;

        InputChanged?.Invoke(_input.Text ?? string.Empty, _input.CaretIndex);
    }

    /// <summary>
    /// Replaces the input line without letting it re-open the popup.
    /// History navigation uses this: assigning _input.Text directly raises TextChanged, which re-queries completions, so recalling a command used to pop the suggestion list open over it.
    /// </summary>
    private void SetCommandText(string text)
    {
        _input.TextChanged -= OnInputTextChanged;
        try
        {
            _input.Text = text;
            _input.CaretIndex = text.Length;
        }
        finally
        {
            _input.TextChanged += OnInputTextChanged;
        }

        ClearSuggestions();
    }

    private void HistoryPrev()
    {
        if (_history.Count == 0)
            return;

        _historyIndex = Math.Max(0, _historyIndex - 1);
        SetCommandText(_history[_historyIndex]);
    }

    private void HistoryNext()
    {
        if (_history.Count == 0)
            return;

        _historyIndex = Math.Min(_history.Count, _historyIndex + 1);
        SetCommandText(_historyIndex >= _history.Count ? string.Empty : _history[_historyIndex]);
    }

    private async Task RefreshStatusAsync()
    {
        GameApi? game = _game;
        if (game is null)
            return;

        try
        {
            PlayerStatus status = await game.Player.GetStatusAsync().ConfigureAwait(true);

            // Effects are part of the bar (legacy MainTuiView.cs:967-1004), so a failure to read them must not cost the health and food halves; an empty list just draws the bar without them.
            IReadOnlyList<EffectSnapshot> effects;
            try
            {
                effects = await game.Player.GetEffectsAsync().ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
            {
                effects = [];
            }

            Dispatcher.UIThread.Post(() =>
            {
                _statusBar.Text = null;
                _statusBar.Inlines ??= [];
                StatusBarBuilder.Build(
                    _statusBar.Inlines,
                    status.Health,
                    status.Food,
                    status.ExperienceLevel,
                    status.ExperienceProgress,
                    status.TotalExperience,
                    effects,
                    _showEffectNames,
                    _translations);
            });
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _statusBar.Inlines?.Clear();
                _statusBar.Text = Strings.TuiStatusOffline;
            });
        }
    }
}
