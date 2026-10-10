using DMCBK.Core.Beacon;

namespace Mcc.Cli.BeaconTooling;

/// <summary>
/// <c>run &lt;file...&gt;</c>: execute Beacon scripts headlessly, print the report, exit.
/// Handled before anything else in startup, ahead of config load and the rich writer that clears the terminal, so the report survives and no configuration folder is read or generated.
/// <para>
/// Fully offline on the inert host (reads empty, sends nowhere) with a virtual clock and a seeded RNG, so the same file plus the same flags replay bit-for-bit.
/// The engine lives in <c>DMCBK.Core</c> (console-free); this adapter only moves strings between <see cref="BeaconRunCli"/> and <c>System.Console</c>.
/// Exit codes mirror lint: 0 clean (warnings allowed), 1 script errors, 2 usage.
/// </para>
/// <para>
/// Dispatch is on exact <c>argv[0]</c>: only the bare token <c>run</c> triggers.
/// The configurations folder is flag-only (<c>--configurations</c>), so no folder name can collide with this token.
/// </para>
/// </summary>
internal static class BeaconRunOneShot
{
    internal const string Command = "run";

    /// <summary>
    /// Runs scripts headlessly when <paramref name="args"/> asks for it, and reports whether it did.
    /// False means this is an ordinary run and startup carries on.
    /// </summary>
    internal static bool TryHandle(string[] args, out int exitCode)
    {
        exitCode = BeaconLint.ExitCodes.Clean;
        int at = Array.IndexOf(args, Command);
        if (at != 0)
            return false;

        string[] rest = args[1..];
        if (!BeaconRunCli.TryParse(rest, out BeaconRunCliArgs? parsed, out string error) || parsed is null)
        {
            Console.Error.WriteLine(error);
            exitCode = BeaconLint.ExitCodes.Usage;
            return true;
        }

        string? stdinText = BeaconOneShotTransport.ReadStdin(parsed.UseStdin);

        int exit = BeaconRunCli.Execute(parsed, stdinText, out string stdout, out string stderr);
        BeaconOneShotTransport.WriteOutput(stdout, stderr);

        exitCode = exit;
        return true;
    }
}
