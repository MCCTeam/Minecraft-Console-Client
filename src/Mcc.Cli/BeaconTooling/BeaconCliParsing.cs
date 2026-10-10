namespace Mcc.Cli.BeaconTooling;

/// <summary>
/// Shared headless CLI argument mechanics: <c>--flag=value</c> splitting, next-value consumption, and piped-source display names.
/// Consolidates the three TryParse copies (lint, run, format) so flag handling can never drift; per-command error text stays with each caller.
/// </summary>
internal static class BeaconCliParsing
{
    /// <summary>Splits <c>--flag=value</c> into flag plus attached value.</summary>
    internal static void SplitFlag(string arg, out string flag, out string? attached)
    {
        ArgumentNullException.ThrowIfNull(arg);
        int equals = arg.IndexOf('=');
        if (arg.StartsWith("--", StringComparison.Ordinal) && equals > 2)
        {
            flag = arg[..equals];
            attached = arg[(equals + 1)..];
        }
        else
        {
            flag = arg;
            attached = null;
        }
    }

    /// <summary>Consumes the next array element as a separated flag value.</summary>
    internal static bool TryTakeNextValue(string[] args, ref int i, out string? value)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (i + 1 < args.Length)
        {
            i++;
            value = args[i];
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Resolves the display name for piped source.</summary>
    internal static string StdinDisplayName(string? stdinName)
        => string.IsNullOrWhiteSpace(stdinName) ? "stdin.bcn" : stdinName!;
}
