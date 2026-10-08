namespace Mcc.Cli.BeaconTooling;

/// <summary>
/// Shared <c>System.Console</c> transport for the Beacon headless one-shots.
/// Reads piped source only when the parsed arguments request it, and prints nonempty stdout before nonempty stderr, each with <c>WriteLine</c> newline behavior.
/// Empty streams are left unwritten.
/// Argument matching, parsing, execution, and exit codes stay in each adapter.
/// </summary>
internal static class BeaconOneShotTransport
{
    /// <summary>
    /// Reads piped source when <paramref name="useStdin"/> is set, else null.
    /// Call only after successful parsing so failed parses never consume input.
    /// </summary>
    internal static string? ReadStdin(bool useStdin)
        => useStdin ? Console.In.ReadToEnd() : null;

    /// <summary>
    /// Prints nonempty stdout first, then nonempty stderr.
    /// </summary>
    internal static void WriteOutput(string stdout, string stderr)
    {
        if (stdout.Length > 0)
            Console.Out.WriteLine(stdout);

        if (stderr.Length > 0)
            Console.Error.WriteLine(stderr);
    }
}
