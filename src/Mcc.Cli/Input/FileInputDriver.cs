namespace Mcc.Cli.Input;

/// <summary>
/// Env-gated file-input drive replacing the legacy FileInputBot (harness contract).
/// When <c>MCC_FILE_INPUT=1</c>, tail the file at <c>MCC_INPUT_FILE</c> (default <c>mcc_input.txt</c>) and feed each new complete line as if typed on stdin.
/// <c>quit</c>/<c>exit</c> stops the drive.
/// Lines are only consumed once terminated by a newline, so a half-written appended line is never processed.
/// </summary>
internal static class FileInputDriver
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(150);

    /// <summary>True when the harness has requested file-input mode.</summary>
    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("MCC_FILE_INPUT"), "1", StringComparison.Ordinal);

    /// <summary>The tailed input file path.</summary>
    public static string FilePath =>
        Environment.GetEnvironmentVariable("MCC_INPUT_FILE") is { Length: > 0 } path ? path : "mcc_input.txt";

    /// <summary>
    /// Tails the input file, invoking <paramref name="onLine"/> for each complete non-quit line, until a quit/exit line, the cancellation token, or an unrecoverable read error.
    /// </summary>
    public static async Task RunAsync(Func<string, Task> onLine, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(onLine);
        string path = FilePath;
        int processed = 0;

        while (!ct.IsCancellationRequested)
        {
            string text;
            try
            {
                text = File.Exists(path) ? await ReadAllTextSharedAsync(path, ct).ConfigureAwait(false) : string.Empty;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                text = string.Empty;
            }

            string[] parts = text.Split('\n');
            // The element after the final newline is either empty (all lines complete) or a not-yet-terminated partial line; either way it is not ready, so complete-line count is parts.Length - 1.
            int completeCount = parts.Length - 1;

            for (; processed < completeCount; processed++)
            {
                string line = parts[processed].TrimEnd('\r').Trim();
                if (line.Length == 0)
                    continue;

                if (line.Equals("quit", StringComparison.OrdinalIgnoreCase)
                    || line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    return;

                await onLine(line).ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(PollInterval, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static async Task<string> ReadAllTextSharedAsync(string path, CancellationToken ct)
    {
        // Open with FileShare.ReadWrite so tailing never blocks the harness appending to the file.
        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }
}
