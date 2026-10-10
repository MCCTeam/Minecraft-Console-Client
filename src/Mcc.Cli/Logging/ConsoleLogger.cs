using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using DMCBK.Core.Configuration;
using Microsoft.Extensions.Logging;
using Umpk.Text;

namespace Mcc.Cli.Logging;

/// <summary>
/// The classic-host logger factory.
/// Two shapes: a bootstrap min-level factory (used before the logging config is known) and the full config-driven factory that honors the per-level toggles (<c>DebugMessages</c>/<c>InfoMessages</c>/<c>WarningMessages</c>/<c>ErrorMessages</c>), applies the debug filter (<c>DebugFilterRegex</c> + <c>FilterMode</c>) to debug-level lines, and tees every emitted line to the optional <see cref="LogFileSink"/>.
/// Both shapes also carry the color/timestamp settings in console.toml, so every emitted line gets the legacy bracketed level prefix (<c>"§8[DEBUG] "</c>, <c>"§6[WARN] "</c>, <c>"§c[ERROR] "</c> from ConsoleIO.LogPrefix / FilteredLogger; the <c>"[MCC] "</c> information prefix stays plain so it matches the stamp on every other host notice line) and, when enabled, the legacy <c>"HH:mm:ss "</c> timestamp.
/// The prefix color depth follows the <c>ConsoleColorMode</c> setting in console.toml, the same as <see cref="AnsiComponentRenderer"/>.
/// </summary>
internal sealed class ConsoleLoggerFactory : ILoggerFactory
{
    private readonly Func<LogLevel, bool> _enabled;
    private readonly LogFileSink? _fileSink;
    private readonly LogFilter? _debugFilter;
    private readonly ConsoleColorDepth _colorDepth;
    private readonly bool _timestamps;
    private readonly Action<string>? _tuiSink;

    /// <summary>Bootstrap factory: a simple minimum level, console only (no file, no filters).</summary>
    public ConsoleLoggerFactory(LogLevel minLevel, ConsoleColorDepth colorDepth = ConsoleColorDepth.Disable, bool timestamps = false)
        : this(level => level != LogLevel.None && level >= minLevel, null, null, colorDepth, timestamps)
    {
    }

    /// <summary>Full factory: config-driven level toggles, the debug filter, and the optional file sink.</summary>
    /// <param name="tuiSink">
    /// When set (TUI mode, after the backend is up), lines go there instead of <see cref="Console"/>: the TUI owns the terminal from that point, so a raw console write would corrupt its screen the same way it would the classic rich reader's.
    /// The prefix stays plain text (no SGR): Consolonia paints its own cells and would show escapes literally.
    /// </param>
    public ConsoleLoggerFactory(
        LoggingConfig config, LogFileSink? fileSink, ConsoleColorDepth colorDepth, bool timestamps,
        Action<string>? tuiSink = null)
        : this(BuildEnabled(config), fileSink, new LogFilter(config.DebugFilterRegex, config.FilterMode), colorDepth, timestamps, tuiSink)
    {
        ArgumentNullException.ThrowIfNull(config);
    }

    private ConsoleLoggerFactory(
        Func<LogLevel, bool> enabled, LogFileSink? fileSink, LogFilter? debugFilter, ConsoleColorDepth colorDepth, bool timestamps,
        Action<string>? tuiSink = null)
    {
        _enabled = enabled;
        _fileSink = fileSink;
        _debugFilter = debugFilter;
        _colorDepth = colorDepth;
        _timestamps = timestamps;
        _tuiSink = tuiSink;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
        => new ConsoleLogger(_enabled, _fileSink, _debugFilter, _colorDepth, _timestamps, _tuiSink);

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private static Func<LogLevel, bool> BuildEnabled(LoggingConfig config) => level => level switch
    {
        LogLevel.Trace or LogLevel.Debug => config.DebugMessages,
        LogLevel.Information => config.InfoMessages,
        LogLevel.Warning => config.WarningMessages,
        LogLevel.Error or LogLevel.Critical => config.ErrorMessages,
        _ => false,
    };
}

internal sealed class ConsoleLogger(
    Func<LogLevel, bool> enabled, LogFileSink? fileSink, LogFilter? debugFilter, ConsoleColorDepth colorDepth, bool timestamps,
    Action<string>? tuiSink = null)
    : ILogger
{
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => enabled(logLevel);

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        ArgumentNullException.ThrowIfNull(formatter);
        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception is null)
            return;

        // Debug-level lines pass through the debug filter (DebugFilterRegex + FilterMode) for both streams.
        if (logLevel is LogLevel.Trace or LogLevel.Debug && debugFilter is not null && !debugFilter.ShouldShow(message))
            return;

        // Legacy parity: the level-prefixed, possibly-colored body.
        // The file sink gets this same un-timestamped body (mirroring the ChatPresenter console/file split) and applies its own PrependTimestamp/SaveColorCodes policy instead of inheriting the console setting.
        // The TUI sink takes the same body with a PLAIN prefix: Consolonia paints its own cells, so SGR escapes would print literally (and the depth mapping exists only for the classic host).
        string body = tuiSink is null ? Prefix(logLevel, colorDepth) + message : PlainPrefix(logLevel) + message;
        string consoleLine = timestamps ? $"{DateTime.Now:HH:mm:ss} {body}" : body;

        // Through HostConsole, never straight to Console, whenever the rich reader owns the terminal.
        // ConsoleWriter.Write is the only path that feeds ConsoleSuggestion's 32-slot recent-message ring and redraws the input area afterwards (ConsoleWriter.cs:127-149).
        // A raw Console.WriteLine does neither, so every log line printed during a session both scrolled the live prompt away without repainting it AND shifted the ring's indices by one.
        // The suggestion popup reconstructs what it is covering from that ring rather than reading the screen, so after N bypassing log lines every popup restored the lines N rows off: closing a tab-completion popup painted the server-status panel over the chat history.
        // Measured at exactly 8 rows of drift after 8 [MCC]/[WARN] lines.
        //
        // The stream split is kept for the non-rich paths (redirected output, file input, exercises), where there is no ConsoleInteractive state to keep consistent and a caller may well be separating stdout from stderr.
        if (HostConsole.IsRich)
        {
            HostConsole.WriteLine(consoleLine);
            if (exception is not null)
                HostConsole.WriteLine(exception.ToString());
        }
        else if (tuiSink is not null)
        {
            tuiSink(consoleLine);
            if (exception is not null)
                tuiSink(exception.ToString());
        }
        else
        {
            TextWriter writer = logLevel >= LogLevel.Error ? Console.Error : Console.Out;
            writer.WriteLine(consoleLine);
            if (exception is not null)
                writer.WriteLine(exception);
        }

        if (fileSink is not null)
        {
            fileSink.Write(body);
            if (exception is not null)
                fileSink.Write(exception.ToString());
        }
    }

    // "[DEBUG] "/"[WARN] "/"[ERROR] ", colored the same as legacy's §8/§6/§c codes (ConsoleIO.LogPrefix, FilteredLogger.Debug/Warn/Error), as an SGR sequence shaped by console.toml's ConsoleColorMode (matching AnsiComponentRenderer's chat-line rendering; see AnsiColorMapping) when color is on, plain text otherwise.
    // "[MCC] " (Information) stays plain on purpose: it is the same stamp Strings.Host puts on every host notice line, and the stamp scans the same only uncolored.
    // A log prefix never carries translatable content, so this skips the LegacyText/AnsiComponentRenderer Component pipeline (built to resolve TranslatableContent against a live ITranslationSource) and goes straight from the canonical Umpk TextColor swatch to the SGR sequence.
    private static string Prefix(LogLevel level, ConsoleColorDepth depth)
    {
        (string word, TextColor? swatch) = level switch
        {
            LogLevel.Information => ("MCC", (TextColor?)null),
            LogLevel.Trace or LogLevel.Debug => ("DEBUG", TextColor.DarkGray),
            LogLevel.Warning => ("WARN", TextColor.Gold),
            LogLevel.Error or LogLevel.Critical => ("ERROR", TextColor.Red),
            _ => ("LOG", TextColor.Gray),
        };

        string bracket = $"[{word}] ";
        if (swatch is null || depth == ConsoleColorDepth.Disable)
            return bracket;

        string sgr = AnsiColorMapping.Sgr(swatch.Value, depth);
        return $"\u001b[{sgr}m{bracket}\u001b[0m";
    }

    /// <summary>The same words as <see cref="Prefix"/>, always plain: the TUI sink's only prefix shape.</summary>
    private static string PlainPrefix(LogLevel level) => level switch
    {
        LogLevel.Information => "[MCC] ",
        LogLevel.Trace or LogLevel.Debug => "[DEBUG] ",
        LogLevel.Warning => "[WARN] ",
        LogLevel.Error or LogLevel.Critical => "[ERROR] ",
        _ => "[LOG] ",
    };
}
