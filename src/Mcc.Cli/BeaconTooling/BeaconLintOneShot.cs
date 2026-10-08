using DMCBK.Core.Beacon;

namespace Mcc.Cli.BeaconTooling;

/// <summary>
/// <c>lint &lt;file...&gt;</c>: lint Beacon scripts headlessly, print the report, exit.
/// It is handled before anything else in startup, ahead of config load and the rich writer that clears the terminal, so the report survives and no configuration folder is read or generated.
/// <para>
/// Fully offline: no session, no network, pure. The engine lives in <c>DMCBK.Core</c> (console-free);
/// this adapter only moves strings between <see cref="BeaconLintCli"/> and <c>System.Console</c>.
/// The JSON document goes to stdout, human chatter to stderr, and the exit code is the contract agents script against: 0 clean (warnings allowed), 1 errors, 2 usage.
/// </para>
/// <para>
/// Dispatch is on exact <c>argv[0]</c>: only the bare token <c>lint</c> triggers.
/// A configurations folder literally named <c>lint</c> must be passed as <c>./lint/</c>, which never matches.
/// </para>
/// </summary>
internal static class BeaconLintOneShot
{
    internal const string Command = "lint";

    /// <summary>
    /// Runs the lint when <paramref name="args"/> asks for it, and reports whether it did.
    /// False means this is an ordinary run and startup carries on.
    /// </summary>
    internal static bool TryHandle(string[] args, out int exitCode)
    {
        exitCode = BeaconLint.ExitCodes.Clean;
        int at = Array.IndexOf(args, Command);
        if (at != 0)
            return false;

        string[] rest = args[1..];
        if (!BeaconLintCli.TryParse(rest, out BeaconLintRequest? parsed, out string error) || parsed is null)
        {
            Console.Error.WriteLine(error);
            exitCode = BeaconLint.ExitCodes.Usage;
            return true;
        }

        string? stdinText = BeaconOneShotTransport.ReadStdin(parsed.UseStdin);

        int exit = BeaconLintCli.Execute(parsed, stdinText, out string stdout, out string stderr);
        BeaconOneShotTransport.WriteOutput(stdout, stderr);

        exitCode = exit;
        return true;
    }
}
