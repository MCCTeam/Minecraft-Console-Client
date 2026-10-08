using Mcc.Cli.Presentation;
namespace Mcc.Cli.Logging;

/// <summary>
/// The optional file log sink (re-homing the legacy <c>FileLogLogger</c>): appends host log and chat lines to <c>LoggingConfig.LogFile</c>, honoring <c>PrependTimestamp</c> (a wall-clock prefix) and <c>SaveColorCodes</c> (keep the ANSI escapes when true, strip them when false).
/// Thread-safe: the console reader thread and session callbacks both write.
/// Filtering (FilterMode) is applied by the caller before the line reaches the sink, so the sink only formats and persists.
/// </summary>
internal sealed class LogFileSink : IDisposable
{
    private readonly object _gate = new();
    private readonly bool _prependTimestamp;
    private readonly bool _saveColorCodes;
    private StreamWriter? _writer;

    /// <summary>Opens (or creates, appending) the log file. Throws on an unusable path so the host can report it.</summary>
    public LogFileSink(string path, bool prependTimestamp, bool saveColorCodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _prependTimestamp = prependTimestamp;
        _saveColorCodes = saveColorCodes;

        string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true,
        };
    }

    /// <summary>Writes one line, stripping ANSI when color codes are not saved and prepending a timestamp when configured.</summary>
    public void Write(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        string text = _saveColorCodes ? line : Ansi.Strip(line);
        if (_prependTimestamp)
            text = $"[{DateTime.Now:HH:mm:ss}] {text}";

        lock (_gate)
            _writer?.WriteLine(text);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
