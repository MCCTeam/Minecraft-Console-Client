using System.Text.Json;

using DMCBK.Core.Beacon;
namespace Mcc.Cli.BeaconTooling;

/// <summary>
/// Parsed headless <c>run</c> arguments.
/// The headless <c>run</c> one-shot and any future in-client runner build this and run <see cref="BeaconRunCli.Execute"/> so behavior agrees.
/// </summary>
/// <param name="Files">Input files, in order.</param>
/// <param name="Seed">RNG seed (fixed default keeps runs deterministic).</param>
/// <param name="TickSeconds">Virtual seconds to advance after load, running due timers.</param>
/// <param name="Format"><c>text</c> (human view) or <c>json</c> (agent-scriptable document).</param>
/// <param name="UseStdin">Run piped source instead of files.</param>
/// <param name="StdinName">File name the piped source runs under (spans, JSON <c>file</c>).</param>
public sealed record BeaconRunCliArgs(
    IReadOnlyList<string> Files,
    int Seed,
    double TickSeconds,
    string Format,
    bool UseStdin,
    string? StdinName);

/// <summary>
/// The shared headless run runner: deterministic execution over <see cref="BeaconOfflineHost"/> (the fake server: reads empty, sends recorded-nowhere), a virtual clock, and a seeded RNG.
/// Console-free (no <c>System.Console</c>): callers render <c>stdout</c> and <c>stderr</c> themselves.
/// Exit codes mirror lint: 0 clean (warnings allowed), 1 script errors, 2 usage.
/// </summary>
public static class BeaconRunCli
{
    /// <summary>The default RNG seed (deterministic unless <c>--seed</c> overrides).</summary>
    public const int DefaultSeed = 1234;

    /// <summary>Parses headless <c>run</c> arguments; false means usage failure with <paramref name="error"/> set.</summary>
    public static bool TryParse(string[] args, out BeaconRunCliArgs? parsed, out string error)
    {
        ArgumentNullException.ThrowIfNull(args);
        parsed = null;
        error = string.Empty;

        int seed = DefaultSeed;
        double tickSeconds = 0;
        string format = "text";
        bool useStdin = false;
        string? stdinName = null;
        var files = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            BeaconCliParsing.SplitFlag(arg, out string flag, out string? attached);

            switch (flag)
            {
                case "--seed":
                    string? rawSeed = attached ?? (BeaconCliParsing.TryTakeNextValue(args, ref i, out string? seedNext) ? seedNext : null);
                    if (!int.TryParse(rawSeed, out int parsedSeed))
                    {
                        error = $"run: --seed needs an integer, not '{rawSeed ?? string.Empty}'. "
                            + "Usage: run <file...> [--seed N] [--tick S] [--format text|json] [--stdin] [--stdin-name <name>]";
                        return false;
                    }

                    seed = parsedSeed;
                    break;
                case "--tick":
                    string? rawTick = attached ?? (BeaconCliParsing.TryTakeNextValue(args, ref i, out string? tickNext) ? tickNext : null);
                    if (!double.TryParse(rawTick, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsedTick)
                        || double.IsNaN(parsedTick) || double.IsInfinity(parsedTick) || parsedTick < 0)
                    {
                        error = $"run: --tick needs a non-negative number of seconds, not '{rawTick ?? string.Empty}'.";
                        return false;
                    }

                    tickSeconds = parsedTick;
                    break;
                case "--format":
                    string? value = attached ?? (BeaconCliParsing.TryTakeNextValue(args, ref i, out string? formatNext) ? formatNext : null);
                    if (value is not ("text" or "json"))
                    {
                        error = $"run: --format must be 'text' or 'json', not '{value ?? string.Empty}'. "
                            + "Usage: run <file...> [--seed N] [--tick S] [--format text|json] [--stdin] [--stdin-name <name>]";
                        return false;
                    }

                    format = value;
                    break;
                case "--stdin":
                    useStdin = true;
                    break;
                case "--stdin-name":
                    string? name = attached ?? (BeaconCliParsing.TryTakeNextValue(args, ref i, out string? nameNext) ? nameNext : null);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        error = "run: --stdin-name needs a file name.";
                        return false;
                    }

                    stdinName = name;
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        error = $"run: unknown flag '{flag}'. "
                            + "Usage: run <file...> [--seed N] [--tick S] [--format text|json] [--stdin] [--stdin-name <name>]";
                        return false;
                    }

                    files.Add(arg);
                    break;
            }
        }

        parsed = new BeaconRunCliArgs(files, seed, tickSeconds, format, useStdin, stdinName);
        return true;
    }

    /// <summary>
    /// Runs files offline (or piped source with <c>UseStdin</c>).
    /// Returns the exit code (0 clean, 1 script errors, 2 usage); the run report goes to <paramref name="stdout"/>, usage failures to <paramref name="stderr"/>.
    /// </summary>
    public static int Execute(
        BeaconRunCliArgs args, string? stdinText, out string stdout, out string stderr)
    {
        ArgumentNullException.ThrowIfNull(args);
        stdout = string.Empty;
        stderr = string.Empty;

        if (args.UseStdin)
        {
            if (stdinText is null)
            {
                stderr = "run: --stdin needs piped source text.";
                return BeaconLint.ExitCodes.Usage;
            }

            string name = BeaconCliParsing.StdinDisplayName(args.StdinName);
            BeaconRunReport report = BeaconOfflineRunner.RunSourceAsync(name, stdinText, args.Seed, args.TickSeconds)
                .GetAwaiter().GetResult();
            stdout = Render([report], args.Format);
            return report.Ok ? BeaconLint.ExitCodes.Clean : BeaconLint.ExitCodes.Errors;
        }

        if (args.Files.Count == 0)
        {
            stderr = "run: no input files. Usage: run <file...> [--seed N] [--tick S] [--format text|json] [--stdin] [--stdin-name <name>]";
            return BeaconLint.ExitCodes.Usage;
        }

        var reports = new List<BeaconRunReport>(args.Files.Count);
        foreach (string path in args.Files)
        {
            string source;
            try
            {
                source = File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                stderr = $"run: cannot read '{path}': {ex.Message}";
                return BeaconLint.ExitCodes.Usage;
            }

            reports.Add(BeaconOfflineRunner.RunSourceAsync(path, source, args.Seed, args.TickSeconds).GetAwaiter().GetResult());
        }

        stdout = Render(reports, args.Format);
        return reports.All(r => r.Ok) ? BeaconLint.ExitCodes.Clean : BeaconLint.ExitCodes.Errors;
    }



    private static string Render(IReadOnlyList<BeaconRunReport> reports, string format)
    {
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            return ToJson(reports);

        var sb = new System.Text.StringBuilder();
        foreach (BeaconRunReport report in reports)
        {
            if (report.Ok && report.Output.Count == 0 && report.Diagnostics.Count == 0)
            {
                sb.Append(report.Path).Append(": ok\n");
                continue;
            }

            foreach (BeaconDiagnostic diagnostic in report.Diagnostics)
                sb.Append(BeaconErrorRenderer.Render(diagnostic, null, includeTrace: true)).Append('\n');

            foreach (string line in report.Output)
                sb.Append(line).Append('\n');

            if (report.Ok)
                sb.Append(report.Path).Append(": ok\n");
            else
                sb.Append(report.Path).Append(": failed\n");
        }

        return sb.ToString().TrimEnd('\n');
    }

    private static string ToJson(IReadOnlyList<BeaconRunReport> reports)
    {
        var files = new List<object>(reports.Count);
        var diagnostics = new List<object>();
        var output = new List<object>();
        int errors = 0;
        int warnings = 0;
        int notes = 0;
        foreach (BeaconRunReport report in reports)
        {
            files.Add(new { path = report.Path, ok = report.Ok });
            foreach (BeaconDiagnostic diagnostic in report.Diagnostics)
            {
                diagnostics.Add(BeaconReportJson.ToDiagnosticJson(diagnostic));
                BeaconReportJson.Tally(diagnostic.Severity, ref errors, ref warnings, ref notes);
            }

            foreach (string line in report.Output)
                output.Add(new { file = report.Path, line });
        }

        return JsonSerializer.Serialize(
            new { files, diagnostics, output, summary = new { errors, warnings, notes } },
            new JsonSerializerOptions { WriteIndented = true });
    }
}
