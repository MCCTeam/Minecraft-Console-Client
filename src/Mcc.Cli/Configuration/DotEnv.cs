// DotEnv - a minimal .env loader for the configurations folder.
// KEY=VALUE lines become process environment variables before anything reads them, so tokens live in one untracked file and settings link to them with env:NAME.
// A missing file is a no-op.
// Variables already present win, so an explicit export always beats the file.
// No interpolation, no multi-line values, no escapes: secrets do not need a language.

using System.Text.RegularExpressions;

namespace Mcc.Cli.Configuration;

/// <summary>Loads <c>.env</c> files from the configurations folder into the process environment.</summary>
public static partial class DotEnv
{
    /// <summary>The file name looked up inside the configurations folder.</summary>
    public const string FileName = ".env";

    /// <summary>
    /// Reads <c>.env</c> from <paramref name="configurationsFolder"/> and sets every variable the process does not already define.
    /// Returns how many were set.
    /// Never throws for file problems: a missing or unreadable file loads nothing.
    /// </summary>
    public static int Load(string configurationsFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationsFolder);

        string text;
        try
        {
            text = File.ReadAllText(Path.Combine(configurationsFolder, FileName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return 0;
        }

        int set = 0;
        foreach ((string key, string value) in Parse(text))
        {
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
                set++;
            }
        }

        return set;
    }

    /// <summary>Parses .env text into key/value pairs, skipping blanks, comments, and malformed lines.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> Parse(string text)
    {
        var pairs = new List<KeyValuePair<string, string>>();
        foreach (string raw in (text ?? string.Empty).Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line["export ".Length..].TrimStart();

            int separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            string key = line[..separator].Trim();
            if (!KeyPattern().IsMatch(key))
                continue;

            string value = line[(separator + 1)..].Trim();
            if (value.Length >= 2
                && value.StartsWith(value[0])
                && value.EndsWith(value[0])
                && (value[0] is '"' or '\''))
                value = value[1..^1];

            pairs.Add(new KeyValuePair<string, string>(key, value));
        }

        return pairs;
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex KeyPattern();
}
