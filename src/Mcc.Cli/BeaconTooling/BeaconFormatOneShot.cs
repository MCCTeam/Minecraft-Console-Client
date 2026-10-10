using DMCBK.Core.Beacon;

namespace Mcc.Cli.BeaconTooling;

/// <summary>
/// <c>format &lt;file...&gt;</c>: format Beacon scripts headlessly, print the diff, exit.
/// It is handled before anything else in startup, ahead of config load and the rich writer that clears the terminal, so the report survives and no configuration folder is read or generated.
/// <para>
/// Fully offline: no session, no network, pure.
/// The engine lives in <c>DMCBK.Core</c> (console-free); this adapter only moves strings between <see cref="BeaconFormatCli"/> and <c>System.Console</c>.
/// Without <c>--check</c> changed files are rewritten in place; with it nothing is written and files that would change exit 1.
/// Exit codes mirror lint: 0 formatted or already clean, 1 script errors or <c>--check</c> would-change, 2 usage.
/// </para>
/// <para>
/// Dispatch is on exact <c>argv[0]</c>: only the bare token <c>format</c> triggers.
/// A configurations folder literally named <c>format</c> must be passed as <c>./format/</c>, which never matches.
/// </para>
/// </summary>
internal static class BeaconFormatOneShot
{
    internal const string Command = "format";

    /// <summary>
    /// Runs the formatter when <paramref name="args"/> asks for it, and reports whether it did.
    /// False means this is an ordinary run and startup carries on.
    /// </summary>
    internal static bool TryHandle(string[] args, out int exitCode)
    {
        exitCode = BeaconLint.ExitCodes.Clean;
        int at = Array.IndexOf(args, Command);
        if (at != 0)
            return false;

        string[] rest = args[1..];
        if (!BeaconFormatCli.TryParse(rest, out BeaconFormatCliArgs? parsed, out string error) || parsed is null)
        {
            Console.Error.WriteLine(error);
            exitCode = BeaconLint.ExitCodes.Usage;
            return true;
        }

        string? stdinText = BeaconOneShotTransport.ReadStdin(parsed.UseStdin);

        int exit = BeaconFormatCli.Execute(parsed, stdinText, out string stdout, out string stderr);
        BeaconOneShotTransport.WriteOutput(stdout, stderr);

        exitCode = exit;
        return true;
    }
}
