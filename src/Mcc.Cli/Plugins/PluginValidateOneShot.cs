using Mcc.Cli.Hosting;
using Mcc.Cli.Localization;
using DMCBK.PluginSdk;

namespace Mcc.Cli.Plugins;

/// <summary>
/// <c>--validate-plugin &lt;folder&gt;</c>: check one plugin folder, print what is wrong, exit.
/// It is handled before anything else in startup, ahead of the rich writer that clears the terminal, so the report survives and no configuration folder is read or generated.
/// <para>
/// This exists for CI.
/// A catalogue's own validation can check that an entry points at a folder; only the client can check that the folder's language table answers the keys its code reads.
/// </para>
/// </summary>
internal static class PluginValidateOneShot
{
    internal const string Flag = "--validate-plugin";

    /// <summary>
    /// Runs the check when <paramref name="args"/> asks for it, and reports whether it did.
    /// False means this is an ordinary run and startup carries on.
    /// </summary>
    internal static bool TryHandle(string[] args, out int exitCode)
    {
        exitCode = HostExit.Clean;
        int at = Array.IndexOf(args, Flag);
        if (at < 0)
            return false;

        if (at + 1 >= args.Length || args[at + 1].Length == 0)
        {
            Console.Error.WriteLine(Strings.ValidatePluginUsage);
            exitCode = HostExit.Usage;
            return true;
        }

        PluginValidationReport report = PluginValidator.Validate(args[at + 1]);
        foreach (PluginValidationProblem problem in report.Problems)
        {
            Console.Out.WriteLine(Strings.ValidatePluginProblem(
                problem.Severity == PluginValidationSeverity.Error,
                Where(problem),
                problem.Message));
        }

        Console.Out.WriteLine(report.IsValid
            ? Strings.ValidatePluginOk(report.Id ?? report.Folder, report.WarningCount)
            : Strings.ValidatePluginFailed(report.Id ?? report.Folder, report.ErrorCount));

        exitCode = report.IsValid ? HostExit.Clean : HostExit.Usage;
        return true;
    }

    private static string Where(PluginValidationProblem problem)
    {
        if (problem.File.Length == 0)
            return string.Empty;

        return problem.Line > 0 ? $"{problem.File}:{problem.Line}" : problem.File;
    }
}
