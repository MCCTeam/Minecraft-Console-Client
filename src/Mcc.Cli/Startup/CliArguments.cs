using Mcc.Cli.Localization;
using DMCBK.Core.Configuration;

namespace Mcc.Cli.Startup;

/// <summary>
/// Parses the positional contract <c>&lt;username&gt; &lt;password|-&gt; &lt;host:port&gt;</c> plus the flag forms <c>-v</c>, <c>--auth</c>, <c>--auth-server</c>, <c>--configurations</c> and dotted <c>--section.setting=value</c> overrides.
/// The configurations folder is flag-only: it used to be the first positional, which meant <c>Mcc.Cli Steve - play.example.com</c> silently treated <c>Steve</c> as a folder instead of a login.
/// Every positional is optional and overrides config; dotted flags win over positionals.
/// A dotted flag whose first path segment is <c>console</c> (case-insensitive) is a console.toml override and is collected into <see cref="ConsoleOverrides"/> instead of <see cref="ConfigurationOverrides.Dotted"/>, since console.toml is a separate, host-owned file with its own setter map (<see cref="Configuration.ConsoleTomlOverrideBinder"/>).
/// </summary>
internal sealed record CliArguments(
    string? ConfigArg,
    ConfigurationOverrides Overrides,
    string? Exercise = null,
    IReadOnlyList<KeyValuePair<string, string>>? ConsoleOverrides = null)
{
    private const string ConsolePrefix = "console.";

    public static bool TryParse(string[] args, out CliArguments parsed, out string? error)
    {
        parsed = new CliArguments(null, new ConfigurationOverrides());
        error = null;

        var positionals = new List<string>();
        var dotted = new List<KeyValuePair<string, string>>();
        var consoleDotted = new List<KeyValuePair<string, string>>();
        string? configArg = null;
        string? version = null;
        string? authMode = null;
        string? authServer = null;
        string? exercise = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "-v")
            {
                if (i + 1 >= args.Length)
                {
                    error = Strings.MissingFlagValue("-v");
                    return false;
                }

                version = args[++i];
                continue;
            }

            if (arg == "--configurations")
            {
                if (i + 1 >= args.Length)
                {
                    error = Strings.MissingFlagValue("--configurations");
                    return false;
                }

                configArg = args[++i];
                continue;
            }

            if (arg == "--auth")
            {
                if (i + 1 >= args.Length)
                {
                    error = Strings.MissingFlagValue("--auth");
                    return false;
                }

                authMode = args[++i];
                continue;
            }

            if (arg == "--auth-server")
            {
                if (i + 1 >= args.Length)
                {
                    error = Strings.MissingFlagValue("--auth-server");
                    return false;
                }

                authServer = args[++i];
                continue;
            }

            if (arg == "--exercise")
            {
                if (i + 1 >= args.Length)
                {
                    error = Strings.MissingFlagValue("--exercise");
                    return false;
                }

                exercise = args[++i];
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                int eq = arg.IndexOf('=', StringComparison.Ordinal);
                if (eq < 3)
                {
                    error = Strings.BadFlag(arg);
                    return false;
                }

                string key = arg[2..eq];
                string value = arg[(eq + 1)..];
                switch (key.ToLowerInvariant())
                {
                    case "auth":
                        authMode = value;
                        break;
                    case "auth-server":
                        authServer = value;
                        break;
                    case "configurations":
                        configArg = value;
                        break;
                    case "exercise":
                        exercise = value;
                        break;
                    default:
                        if (key.StartsWith(ConsolePrefix, StringComparison.OrdinalIgnoreCase))
                            consoleDotted.Add(new KeyValuePair<string, string>(key[ConsolePrefix.Length..], value));
                        else
                            dotted.Add(new KeyValuePair<string, string>(key, value));

                        break;
                }

                continue;
            }

            // A lone "-" is the offline password sentinel (a positional), not a flag.
            if (arg.StartsWith('-') && arg.Length > 1)
            {
                error = Strings.BadFlag(arg);
                return false;
            }

            positionals.Add(arg);
        }

        // At most three words: username, password, host.
        // A fourth is almost certainly the old config-first order, which would otherwise misparse silently (the folder becoming the username and everything shifting one slot over).
        if (positionals.Count > 3)
        {
            error = Strings.UnexpectedArgument(positionals[3]);
            return false;
        }

        // A username can never look like a path, so one that does is the old config-first order.
        // Point at the flag instead of letting a folder become a login.
        if (positionals.Count > 0 && LooksLikeFolder(positionals[0]))
        {
            error = Strings.UnexpectedArgument(positionals[0]);
            return false;
        }

        var overrides = new ConfigurationOverrides
        {
            Username = positionals.Count > 0 ? positionals[0] : null,
            Password = positionals.Count > 1 ? positionals[1] : null,
            Address = positionals.Count > 2 && positionals[2].Length > 0 ? positionals[2] : null,
            Version = version,
            AuthMode = authMode,
            AuthServer = authServer,
            Dotted = dotted,
        };

        parsed = new CliArguments(configArg, overrides, exercise, consoleDotted);
        return true;
    }

    private static bool LooksLikeFolder(string value)
        => value.Contains('/') || value.Contains('\\')
            || value.EndsWith(".toml", StringComparison.OrdinalIgnoreCase);
}
