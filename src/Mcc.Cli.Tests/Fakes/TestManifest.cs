using DMCBK.PluginSdk;
namespace Mcc.Cli.Tests.Fakes;

// Explicitly upgrades copied legacy fixture inputs. Production parsing never applies these defaults.
internal static class TestManifest
{
    internal static string Upgrade(string text)
    {
        if (text.Contains("schema-version", StringComparison.Ordinal)) return text;
        string entry = System.Text.RegularExpressions.Regex.Match(text, "(?m)^\\s*entry\\s*=\\s*\"([^\"]+)\"").Groups[1].Value;
        string kind = entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? "compiled" : "source";
        string prefix = $"schema-version = 2\nkind = \"{kind}\"\ntarget = \"any\"\nframework = \"net10.0\"\n";
        if (!Has("version")) prefix += "version = \"1.0.0\"\n";
        if (!Has("api-version")) prefix += "api-version = \"1.0\"\n";
        if (!Has("dmcbk")) prefix += "dmcbk = \"*\"\n";
        if (!Has("umpk")) prefix += "umpk = \"*\"\n";
        return prefix + text;
        bool Has(string name) => System.Text.RegularExpressions.Regex.IsMatch(text, $@"(?m)^\s*{name}\s*=");
    }

    internal static bool TryParse(string text, out PluginManifest manifest, out string? error)
        => PluginManifest.TryParse(Upgrade(text), out manifest, out error);
}
