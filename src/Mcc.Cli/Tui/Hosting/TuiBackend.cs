using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using Mcc.Cli.Tui.Authentication;
using Mcc.Cli.Tui.Presentation;
using Mcc.Cli.Tui.Terminal;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using Consolonia;
using Umpk.Text;

namespace Mcc.Cli.Tui.Hosting;

/// <summary>
/// Owns the Consolonia app lifetime and bridges the async host to the UI thread.
/// The UI runs on a dedicated foreground thread (Avalonia's dispatcher binds to it); every view mutation marshals through <see cref="Post"/>.
/// The host feeds chat/log/status in and receives submitted input lines out.
/// This is the TUI analogue of the classic host's <see cref="RichConsole"/> reader, but it never references the legacy assembly nor the ConsoleInteractive submodule.
/// </summary>
internal sealed class TuiBackend
{
    private readonly ManualResetEventSlim _viewReady = new(false);
    private readonly TaskCompletionSource _uiExited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _lineGate = new();
    private ComponentInlineRenderer? _renderer;
    private MainTuiView? _view;
    private Thread? _uiThread;
    private TaskCompletionSource<string>? _pendingLine;
    private bool _color = true;
    private bool _timestamps;

    /// <summary>The single live backend, discovered by <see cref="MccTuiApp"/> and the host UI hooks.</summary>
    public static TuiBackend? Instance { get; private set; }

    /// <summary>The live main view once the UI is up (null before <see cref="Start"/> completes).</summary>
    public MainTuiView? View => _view;

    /// <summary>The Component renderer (set before <see cref="Start"/>).</summary>
    public ComponentInlineRenderer? Renderer => _renderer;

    /// <summary>
    /// The configured TUI log/chat scrollback (console.toml <c>TuiLogScrollback</c>), read by <see cref="MccTuiApp"/> when it constructs <see cref="MainTuiView"/>.
    /// </summary>
    public int LogScrollback { get; private set; } = 3000;

    /// <summary>
    /// The configured suggestion popup cap (console.toml <c>CommandSuggestion.MaxDisplayedSuggestions</c>), read by <see cref="MccTuiApp"/> when it constructs <see cref="MainTuiView"/>.
    /// </summary>
    public int MaxDisplayedSuggestions { get; private set; } = 10;

    /// <summary>
    /// Whether the startup icon banner is shown (console.toml <c>Display_Icon_Banner</c>), read by <see cref="MccTuiApp"/> when it constructs <see cref="MainTuiView"/>.
    /// Mirrors the classic host's same-named gate on its own startup banner.
    /// </summary>
    public bool DisplayIconBanner { get; private set; } = true;

    /// <summary>Completes when the Consolonia UI loop has exited and the terminal is restored.</summary>
    public Task Completion => _uiExited.Task;

    /// <summary>Raised on the UI thread when the user submits an input line that is not satisfying a pending prompt.</summary>
    public event Func<string, Task>? LineSubmitted;

    /// <summary>Wires the Component renderer used for chat/log rendering.</summary>
    /// <param name="translations">The translation source the renderer resolves translatable components with.</param>
    /// <param name="color">The host's resolved colour capability; false drops component colours.</param>
    /// <remarks>
    /// Read live on every render, so wiring it after <see cref="Start"/> (once the built client's translations exist) is fine; lines posted before that fall back to plaintext.
    /// </remarks>
    public void UseRenderer(ITranslationSource translations, bool color = true)
        => _renderer = new ComponentInlineRenderer(translations, color);

    /// <summary>Boots the Consolonia app on its own thread and blocks until the main view is ready.</summary>
    /// <param name="color">
    /// The host's resolved colour capability.
    /// False installs <see cref="MonochromeConsoleColorMode"/>, which is the only switch that reaches every painted cell (see that type for why "no colour" in a TUI means monochrome attributes rather than no ANSI at all).
    /// </param>
    /// <param name="timestamps">
    /// console.toml's <c>Timestamps</c> toggle: when true, every line entering the scrollback through <see cref="WriteLine"/>/<see cref="PostComponent"/> gets an <c>"[HH:mm:ss] "</c> prefix, the TUI counterpart of the classic host's <c>ChatPresenter</c> timestamp.
    /// Applied once here, presentation-side, rather than at every call site.
    /// </param>
    /// <param name="logScrollback">console.toml's <c>TuiLogScrollback</c>, read by <see cref="MccTuiApp"/> to size the view's scrollback.</param>
    /// <param name="maxDisplayedSuggestions">console.toml's <c>CommandSuggestion.MaxDisplayedSuggestions</c>, read by <see cref="MccTuiApp"/> to size the suggestion popup.</param>
    /// <param name="displayIconBanner">console.toml's <c>Display_Icon_Banner</c>, read by <see cref="MccTuiApp"/> to gate the startup banner.</param>
    public void Start(
        bool color = true, bool timestamps = false, int logScrollback = 3000, int maxDisplayedSuggestions = 10,
        bool displayIconBanner = true)
    {
        Instance = this;
        _color = color;
        _timestamps = timestamps;
        LogScrollback = logScrollback;
        MaxDisplayedSuggestions = maxDisplayedSuggestions;
        DisplayIconBanner = displayIconBanner;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        _uiThread = new Thread(RunUi) { Name = "MCC-TUI", IsBackground = false };
        _uiThread.Start();
        _viewReady.Wait();
    }

    private void OnProcessExit(object? sender, EventArgs e) => TerminalRestore.Restore();

    private void RunUi()
    {
        try
        {
            // Before the builder, not inside it: UseAutoDetectedConsole is what constructs the curses console, and ncurses latches ESCDELAY in initscr().
            // See CursesEscapeDelay.
            CursesEscapeDelay.Apply();

            AppBuilder builder = AppBuilder.Configure<MccTuiApp>()
                .UseConsolonia()
                .UseAutoDetectedConsole()
                .LogToException();

            if (!_color)
                builder = builder.UseConsoleColorMode(new MonochromeConsoleColorMode());

            builder.StartWithConsoleLifetime(Array.Empty<string>());
        }
        finally
        {
            TerminalRestore.Restore();
            _viewReady.Set(); // unblock Start() even if the app failed before creating the view
            _uiExited.TrySetResult();
        }
    }

    /// <summary>Called by <see cref="MccTuiApp"/> once the view exists.</summary>
    internal void SetView(MainTuiView view)
    {
        _view = view;
        view.LineEntered += OnLineEntered;
        view.ShutdownRequested += Shutdown;
        _viewReady.Set();
    }

    private void OnLineEntered(string text)
    {
        TaskCompletionSource<string>? pending;
        lock (_lineGate)
        {
            pending = _pendingLine;
            _pendingLine = null;
        }

        if (pending is not null)
        {
            pending.TrySetResult(text);
            return;
        }

        Func<string, Task>? handler = LineSubmitted;
        if (handler is not null)
            _ = RunHandlerAsync(handler, text);
    }

    private static async Task RunHandlerAsync(Func<string, Task> handler, string text)
    {
        try
        {
            await handler(text).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TuiBackend.Instance?.WriteLine(Strings.SendFailed(ex.Message));
        }
    }

    /// <summary>
    /// Toggles password-masking (asterisk echo instead of plaintext) on the input line, used while a Yggdrasil password prompt is pending (see <see cref="TuiAuthInteraction.GetYggdrasilCredentialsAsync"/>).
    /// </summary>
    public void SetPasswordMode(bool enabled) => Post(() => _view?.SetPasswordMode(enabled));

    /// <summary>Awaits a single submitted input line (used by interactive auth prompts in TUI mode).</summary>
    public Task<string> RequestLineAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lineGate)
            _pendingLine = tcs;

        ct.Register(() => tcs.TrySetCanceled(ct));
        return tcs.Task;
    }

    /// <summary>Runs an action on the UI thread (directly if already there).</summary>
    public void Post(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }

    /// <summary>Appends a plain-text line to the chat/log, timestamped when console.toml's Timestamps is on.</summary>
    /// <summary>
    /// Appends a plain (non-Component) line.
    /// Legacy section codes are rendered rather than printed: command output carries them (the chunk map's marker cells are background codes), and the TUI used to show them as literal characters.
    /// See <see cref="LegacyTextInlines"/>.
    /// </summary>
    public void WriteLine(string text) => Post(() =>
    {
        // Split first: the scrollback is a list of controls, one per line, and its cap counts entries, so an embedded newline used to enter it as a single item carrying two lines of text.
        // Server text is where that shows up (a two-line kick screen, for one), and it also has to be timestamped per line the way every other line is.
        foreach (string part in text.Split('\n'))
        {
            string line = TimestampPrefix() + part.TrimEnd('\r');
            if (LegacyTextInlines.HasCodes(line))
                _view?.AppendControlLine(LegacyTextInlines.Render(line));
            else
                _view?.AppendLogLine(line);
        }
    });

    /// <summary>
    /// Appends a Component-rendered line to the chat/log, optionally behind a coloured prefix run (the chat signature-standing marker) and, when console.toml's Timestamps is on, a leading <c>"[HH:mm:ss] "</c> inline.
    /// Both prefixes are separate inlines so neither changes the message text.
    /// </summary>
    public void PostComponent(Component component, string prefix = "", IBrush? prefixBrush = null)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(prefix);
        ComponentInlineRenderer? renderer = _renderer;
        if (renderer is null)
        {
            WriteLine(prefix.Length == 0 ? component.ToPlainText() : prefix + " " + component.ToPlainText());
            return;
        }

        Post(() =>
        {
            TextBlock block = renderer.Render(component);
            if (prefix.Length > 0)
                block.Inlines!.Insert(0, new Run(prefix + " ") { Foreground = prefixBrush });

            string timestamp = TimestampPrefix();
            if (timestamp.Length > 0)
                block.Inlines!.Insert(0, new Run(timestamp));

            _view?.AppendControlLine(block);
        });
    }

    private string TimestampPrefix() => _timestamps ? $"[{DateTime.Now:HH:mm:ss}] " : string.Empty;

    /// <summary>Replaces the input suggestion list; empty clears it.</summary>
    public void UpdateSuggestions(IReadOnlyList<string> suggestions, int replaceStart, int replaceEnd)
        => Post(() => _view?.UpdateSuggestions(suggestions, replaceStart, replaceEnd));

    /// <summary>Requests an orderly Consolonia shutdown (leaves the UI loop).</summary>
    public void Shutdown()
        => Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime life)
                life.Shutdown();
        });
}
