using DMCBK.Core.Beacon;
namespace Mcc.Cli.BeaconTooling;

internal static class BeaconLintCli
{
    public static bool TryParse(string[] args, out BeaconLintRequest? parsed, out string error)
    {
        ArgumentNullException.ThrowIfNull(args);
        parsed = null;
        error = string.Empty;

        string format = "text";
        int? targetLib = null;
        bool strict = false;
        bool fix = false;
        bool useStdin = false;
        string? stdinName = null;
        var files = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            BeaconCliParsing.SplitFlag(arg, out string flag, out string? attached);

            switch (flag)
            {
                case "--format":
                    string? value = attached ?? (BeaconCliParsing.TryTakeNextValue(args, ref i, out string? next) ? next : null);
                    if (value is not ("text" or "json"))
                    {
                        error = $"lint: --format must be 'text' or 'json', not '{value ?? string.Empty}'. "
                            + "Usage: lint <file...> [--format text|json] [--target-lib N] [--strict] [--fix] [--stdin] [--stdin-name <name>]";
                        return false;
                    }

                    format = value;
                    break;
                case "--target-lib":
                    string? raw = attached ?? (BeaconCliParsing.TryTakeNextValue(args, ref i, out string? libNext) ? libNext : null);
                    if (!int.TryParse(raw, out int lib) || lib < 1)
                    {
                        error = $"lint: --target-lib needs a positive integer, not '{raw ?? string.Empty}'.";
                        return false;
                    }

                    targetLib = lib;
                    break;
                case "--strict":
                    strict = true;
                    break;
                case "--fix":
                    fix = true;
                    break;
                case "--stdin":
                    useStdin = true;
                    break;
                case "--stdin-name":
                    string? name = attached ?? (BeaconCliParsing.TryTakeNextValue(args, ref i, out string? nameNext) ? nameNext : null);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        error = "lint: --stdin-name needs a file name.";
                        return false;
                    }

                    stdinName = name;
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        error = $"lint: unknown flag '{flag}'. "
                            + "Usage: lint <file...> [--format text|json] [--target-lib N] [--strict] [--fix] [--stdin] [--stdin-name <name>]";
                        return false;
                    }

                    files.Add(arg);
                    break;
            }
        }

        parsed = new BeaconLintRequest(files, format, targetLib, strict, fix, useStdin, stdinName);
        return true;
    }
    public static int Execute(BeaconLintRequest args, string? stdinText, out string stdout, out string stderr)
        => BeaconLintBatch.Execute(args, stdinText, out stdout, out stderr);
}
