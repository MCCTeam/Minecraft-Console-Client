using DMCBK.Core.Beacon;
namespace Mcc.Cli.BeaconTooling;

/// <summary>
/// Parsed headless <c>format</c> arguments.
/// Both frontends (in-client <c>/scripts format</c> and the headless <c>format</c> one-shot) build this and run <see cref="BeaconFormatCli.Execute"/> so the file rewriting and the exit codes can never disagree.
/// </summary>
/// <param name="Files">Input files, in order.</param>
/// <param name="Check">Print diffs without writing; exit 1 when any file would change.</param>
/// <param name="UseStdin">Format piped source instead of files.</param>
/// <param name="StdinName">File name the piped source formats under (spans, diff headers).</param>
public sealed record BeaconFormatCliArgs(
    IReadOnlyList<string> Files,
    bool Check,
    bool UseStdin,
    string? StdinName);

/// <summary>
/// The shared headless format runner: pure argument parsing plus <see cref="BeaconFormat"/> execution over files (or piped source with <c>UseStdin</c>).
/// Console-free (no <c>System.Console</c>): callers render <c>stdout</c> and <c>stderr</c> themselves, which is what keeps the in-client and headless frontends structurally identical.
/// Exit codes: 0 formatted (or already clean), 1 script errors or <c>--check</c> would-change, 2 usage.
/// </summary>
public static class BeaconFormatCli
{
    /// <summary>Parses headless <c>format</c> arguments; false means usage failure with <paramref name="error"/> set.</summary>
    public static bool TryParse(string[] args, out BeaconFormatCliArgs? parsed, out string error)
    {
        ArgumentNullException.ThrowIfNull(args);
        parsed = null;
        error = string.Empty;

        bool check = false;
        bool useStdin = false;
        string? stdinName = null;
        var files = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            BeaconCliParsing.SplitFlag(arg, out string flag, out string? attached);

            switch (flag)
            {
                case "--check":
                    check = true;
                    break;
                case "--stdin":
                    useStdin = true;
                    break;
                case "--stdin-name":
                    string? name = attached ?? (BeaconCliParsing.TryTakeNextValue(args, ref i, out string? nameNext) ? nameNext : null);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        error = "format: --stdin-name needs a file name.";
                        return false;
                    }

                    stdinName = name;
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        error = $"format: unknown flag '{flag}'. "
                            + "Usage: format <file...> [--check] [--stdin] [--stdin-name <name>]";
                        return false;
                    }

                    files.Add(arg);
                    break;
            }
        }

        parsed = new BeaconFormatCliArgs(files, check, useStdin, stdinName);
        return true;
    }

    /// <summary>
    /// Runs the formatter over files (or piped source with <c>UseStdin</c>).
    /// Returns the exit
    /// code (0 formatted or already clean, 1 errors or <c>--check</c> would-change, 2 usage);
    /// diffs and status lines go to <paramref name="stdout"/>, problems to <paramref name="stderr"/>.
    /// Without <c>--check</c> changed files are rewritten in place; with it nothing is written.
    /// </summary>
    public static int Execute(
        BeaconFormatCliArgs args, string? stdinText, out string stdout, out string stderr)
    {
        ArgumentNullException.ThrowIfNull(args);
        stdout = string.Empty;
        stderr = string.Empty;

        var outText = new System.Text.StringBuilder();
        var errText = new System.Text.StringBuilder();

        if (args.UseStdin)
        {
            if (stdinText is null)
            {
                stderr = "format: --stdin needs piped source text.";
                return BeaconLint.ExitCodes.Usage;
            }

            string name = BeaconCliParsing.StdinDisplayName(args.StdinName);
            BeaconFormatResult result = BeaconFormat.FormatSource(name, stdinText);
            stdout = result.Formatted;
            if (result.HadErrors)
            {
                errText.Append($"format: '{name}' still has lint errors after formatting.");
                stderr = errText.ToString();
                return BeaconLint.ExitCodes.Errors;
            }

            return BeaconLint.ExitCodes.Clean;
        }

        if (args.Files.Count == 0)
        {
            stderr = "format: no input files. Usage: format <file...> [--check] [--stdin] [--stdin-name <name>]";
            return BeaconLint.ExitCodes.Usage;
        }

        bool anyChanged = false;
        bool anyErrors = false;
        foreach (string path in args.Files)
        {
            string source;
            try
            {
                source = File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errText.Append($"format: cannot read '{path}': {ex.Message}\n");
                stdout = outText.ToString().TrimEnd('\n');
                stderr = errText.ToString().TrimEnd('\n');
                return BeaconLint.ExitCodes.Usage;
            }

            BeaconFormatResult result = BeaconFormat.FormatSource(path, source);
            anyChanged |= result.Changed;
            anyErrors |= result.HadErrors;

            if (!result.Changed)
            {
                outText.Append($"'{path}' is already formatted\n");
                continue;
            }

            outText.Append(result.Diff).Append('\n');
            if (args.Check)
            {
                outText.Append($"'{path}' would change\n");
                continue;
            }

            try
            {
                File.WriteAllText(path, result.Formatted);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errText.Append($"format: cannot write formatted '{path}': {ex.Message}\n");
                stdout = outText.ToString().TrimEnd('\n');
                stderr = errText.ToString().TrimEnd('\n');
                return BeaconLint.ExitCodes.Usage;
            }

            outText.Append($"formatted '{path}'\n");
        }

        stdout = outText.ToString().TrimEnd('\n');
        stderr = errText.ToString().TrimEnd('\n');
        if (args.Check && anyChanged)
            return BeaconLint.ExitCodes.Errors;

        return anyErrors ? BeaconLint.ExitCodes.Errors : BeaconLint.ExitCodes.Clean;
    }
}
