using Mcc.Cli.Presentation;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mcc.Cli.Diagnostics;

/// <summary>
/// A per-session diagnostics bundle: everything needed to work out what went wrong on someone else's machine, written under <c>logs/&lt;session&gt;/</c> and zipped when the session ends.
/// <para>
/// The point is that a tester can hand over ONE file and the person debugging can answer questions without a back-and-forth: which server, which protocol, which build, which plugins, what the console said, what actually went over the wire, and what the crash was.
/// Split across several small files rather than one big log, because the reader usually wants one of those questions and not the others.
/// </para>
/// <para>
/// <b>What is deliberately NOT in it.</b> This bundle is designed to be sent to someone else, so it must not carry anything that would let them log in as the tester.
/// The login phase is excluded from the packet capture entirely (it carries the shared secret and the session token), account logins are reduced to a display name, and nothing reads <c>accounts.toml</c>.
/// <see cref="WriteManifest"/> states that in the bundle itself, so the person handing it over can see what they are handing over.
/// </para>
/// </summary>
internal sealed partial class SessionDiagnostics : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _folder;
    private readonly DiagnosticsSettings _settings;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly object _gate = new();

    private StreamWriter? _console;
    private PacketCaptureWriter? _packets;
    private bool _disposed;

    private SessionDiagnostics(string folder, DiagnosticsSettings settings)
    {
        _folder = folder;
        _settings = settings;
    }

    /// <summary>The folder this session's files are being written to.</summary>
    public string Folder => _folder;

    /// <summary>Whether a packet capture is being written.</summary>
    public bool CapturingPackets => _packets is not null;

    /// <summary>
    /// Opens a bundle for this session, or returns null when diagnostics are off or the folder cannot be created.
    /// A diagnostics failure must never stop the client starting, so every path here is best-effort.
    /// </summary>
    /// <param name="logsRoot">The <c>logs/</c> directory.</param>
    /// <param name="settings">The resolved settings.</param>
    /// <param name="serverLabel">A short label for the server, used in the folder name.</param>
    public static SessionDiagnostics? TryOpen(string logsRoot, DiagnosticsSettings settings, string serverLabel)
    {
        ArgumentNullException.ThrowIfNull(logsRoot);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Enabled)
            return null;

        try
        {
            // The folder name is sortable-first so a directory listing is chronological, and carries the server so a tester with several bundles can tell them apart without opening any.
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string name = $"{stamp}-{Sanitize(serverLabel)}";
            string folder = Path.Combine(logsRoot, name);
            Directory.CreateDirectory(folder);

            var diagnostics = new SessionDiagnostics(folder, settings);
            diagnostics.Start();
            return diagnostics;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    private void Start()
    {
        _console = OpenText("console.log");

        if (_settings.CapturePackets)
        {
            _packets = PacketCaptureWriter.TryOpen(
                _folder,
                _settings.MaxCaptureBytes,
                DiagnosticsSettings.MaxCaptureDuration,
                _clock);
        }
    }

    /// <summary>Appends one safe, plain-text console line to the session transcript.</summary>
    /// <remarks>
    /// Reading the transcript beside the packet capture is how a report like "it froze after I typed /move" becomes a timestamp to look at.
    /// Terminal and Minecraft colour codes add no diagnostic value, so they are removed.
    /// Lines containing authentication commands are discarded entirely so a server password cannot enter a bundle through command echoing, chat, or a server response.
    /// </remarks>
    public void WriteConsole(string line)
    {
        string plain = LegacyFormatPattern().Replace(Ansi.Strip(line), string.Empty);
        if (IsAuthenticationCommandLine(plain))
            return;

        lock (_gate)
        {
            if (_console is null)
                return;

            try
            {
                _console.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"[{DateTime.Now:HH:mm:ss.fff} +{_clock.Elapsed:hh\\:mm\\:ss\\.fff}] {plain}"));
            }
            catch (IOException)
            {
                // A full disk must not take the session down with it.
            }
        }
    }

    /// <summary>Records one raw frame, if capture is on.</summary>
    public void WriteFrame(
        int protocol,
        Umpk.Protocol.Java.PacketFlow flow,
        Umpk.Protocol.Java.ProtocolPhase phase,
        int wireId,
        ReadOnlySpan<byte> payload)
        => _packets?.Write(protocol, flow, phase, wireId, payload);

    /// <summary>Writes one JSON file into the bundle.</summary>
    public void WriteJson(string fileName, object value)
    {
        try
        {
            File.WriteAllText(Path.Combine(_folder, fileName), JsonSerializer.Serialize(value, Json));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
        }
    }

    /// <summary>Writes one text file into the bundle.</summary>
    public void WriteText(string fileName, string content)
    {
        try
        {
            File.WriteAllText(Path.Combine(_folder, fileName), content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Records a crash: the exception, its whole chain, and the state the session was in.
    /// </summary>
    /// <remarks>
    /// Appended rather than overwritten, because a session can raise more than one and the SECOND one is often a consequence of the first.
    /// Losing the first would lose the cause.
    /// </remarks>
    public void WriteCrash(string context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var sb = new StringBuilder();
        sb.Append("=== ").Append(context).Append(" at ")
          .Append(DateTime.Now.ToString("O", CultureInfo.InvariantCulture)).AppendLine(" ===");
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Session uptime: {_clock.Elapsed}"));
        sb.AppendLine();

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            sb.Append(current.GetType().FullName).Append(": ").AppendLine(current.Message);
            sb.AppendLine(current.StackTrace ?? "  (no stack trace)");
            if (current.InnerException is not null)
                sb.AppendLine("--- caused by ---");
        }

        sb.AppendLine();

        try
        {
            File.AppendAllText(Path.Combine(_folder, "crash.txt"), sb.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Writes the index that tells a reader what each file is, and what the bundle deliberately omits.
    /// </summary>
    public void WriteManifest(string serverLabel, string clientVersion)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MCC session diagnostics").AppendLine();
        sb.Append("Server: ").AppendLine(serverLabel);
        sb.Append("Client: ").AppendLine(clientVersion);
        sb.Append("Started: ").AppendLine(DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
        sb.AppendLine();

        sb.AppendLine("## Files").AppendLine();
        sb.AppendLine("| File | What it holds |");
        sb.AppendLine("| --- | --- |");
        sb.AppendLine("| `system.json` | OS, CPU, memory, .NET runtime, locale, terminal |");
        sb.AppendLine("| `client.json` | MCC build, settings in force, plugins loaded, feature gates |");
        sb.AppendLine("| `server.json` | Everything the client learned about the server |");
        sb.AppendLine("| `console.log` | Plain-text session transcript with elapsed timestamps; authentication command lines are omitted |");
        sb.AppendLine("| `packets.NNNN.umpkcap` | Replayable UMPK packet captures, in frame order, capped at 20 minutes total |");
        sb.AppendLine("| `crash.txt` | Present only if something threw. Full exception chain and stack traces |");
        sb.AppendLine();

        sb.AppendLine("## Reading the capture").AppendLine();
        sb.AppendLine("Each frame carries a `timestampTicks` measured from the start of the session. Those are");
        sb.AppendLine("**Stopwatch ticks, not TimeSpan ticks**, and the rate is the recording machine's:");
        sb.AppendLine("divide by `Runtime.StopwatchTicksPerSecond` in `system.json` to get seconds. Add that to");
        sb.AppendLine("`Started` above for wall clock, which is what lines a frame up against `console.log`");
        sb.AppendLine("and against a server log.").AppendLine();
        sb.AppendLine("The chunks are UMPK `.umpkcap` v1 captures, so they load through `CorpusLoader` and can be");
        sb.AppendLine("replayed through the decoder rather than only read.").AppendLine();

        sb.AppendLine("## What this bundle does NOT contain").AppendLine();
        sb.AppendLine("Written down because this file is meant to be sent to someone else.").AppendLine();
        sb.AppendLine("- **No login traffic.** The login phase is excluded from the capture entirely: it");
        sb.AppendLine("  carries the encryption secret and the session token, and nothing in it is worth the");
        sb.AppendLine("  risk of handing over.");
        sb.AppendLine("- **No credentials.** `accounts.toml` is never read into the bundle. An account appears");
        sb.AppendLine("  as its display name and kind, never as an email address or a token.");
        sb.AppendLine("- **No `/login` or `/register` lines.** The console transcript discards any line containing");
        sb.AppendLine("  either command, regardless of whether it came from input echo, chat, or the server.");
        sb.AppendLine("- **No chat signing key.** The profile key never leaves the cache directory.").AppendLine();
        sb.AppendLine("Packet recording stops after 20 minutes even when the diagnostics session remains open.");
        sb.AppendLine();
        sb.AppendLine("It DOES contain what you did in-game: chat you sent and received, where you walked, and");
        sb.AppendLine("the address of the server you were on. Look through `console.log` before sharing it if");
        sb.AppendLine("that matters to you.");

        WriteText("MANIFEST.md", sb.ToString());
    }

    /// <summary>
    /// Closes the files and zips the folder, returning the archive path.
    /// </summary>
    /// <remarks>
    /// The folder is removed once the zip exists, so a tester has one file to send rather than a directory to compress themselves.
    /// If zipping fails the folder is kept: half a bundle is worth more than none.
    /// </remarks>
    public string? Finish()
    {
        lock (_gate)
        {
            if (_disposed)
                return null;

            _disposed = true;
            _console?.Flush();
            _console?.Dispose();
            _console = null;
            _packets?.Dispose();
            _packets = null;
        }

        try
        {
            string zipPath = _folder + ".zip";
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(_folder, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            Directory.Delete(_folder, recursive: true);
            Prune(Path.GetDirectoryName(_folder)!, _settings.KeepSessions);
            return zipPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes the oldest bundles beyond the keep count.
    /// A tester who leaves this on for a month should not find a gigabyte of captures they never asked for.
    /// </summary>
    public static void Prune(string logsRoot, int keep)
    {
        if (keep <= 0)
            return;

        try
        {
            var bundles = new DirectoryInfo(logsRoot)
                .GetFiles("*.zip")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Skip(keep)
                .ToList();

            foreach (FileInfo old in bundles)
                old.Delete();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
        }
    }

    /// <inheritdoc/>
    public void Dispose() => Finish();

    private static bool IsAuthenticationCommandLine(string line)
    {
        if (line.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || line.Contains("/register", StringComparison.OrdinalIgnoreCase))
            return true;

        // Vanilla command errors echo the submitted command without its leading slash, for example "login password<--[HERE]".
        // Recognize that first-token form as the same sensitive line.
        ReadOnlySpan<char> trimmed = line.AsSpan().TrimStart();
        return StartsWithCommand(trimmed, "login") || StartsWithCommand(trimmed, "register");
    }

    private static bool StartsWithCommand(ReadOnlySpan<char> line, ReadOnlySpan<char> command)
    {
        if (!line.StartsWith(command, StringComparison.OrdinalIgnoreCase))
            return false;

        return line.Length == command.Length
            || char.IsWhiteSpace(line[command.Length])
            || line[command.Length] == '<';
    }

    [GeneratedRegex("§(?:§[0-9a-fr]|#[0-9a-f]{6}|[0-9a-fk-or])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LegacyFormatPattern();

    private StreamWriter? OpenText(string fileName)
    {
        try
        {
            return new StreamWriter(Path.Combine(_folder, fileName), append: false, Encoding.UTF8)
            {
                AutoFlush = true,
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Makes a server label safe for a folder name on every platform.</summary>
    private static string Sanitize(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "session";

        var sb = new StringBuilder(label.Length);
        foreach (char ch in label)
            sb.Append(char.IsLetterOrDigit(ch) || ch is '-' or '.' ? ch : '_');

        string cleaned = sb.ToString().Trim('_', '.');
        return cleaned.Length == 0 ? "session" : cleaned[..Math.Min(cleaned.Length, 40)];
    }
}
