namespace Mcc.Cli.Configuration;

/// <summary>
/// Applies <c>--console.&lt;table&gt;.&lt;key&gt;=value</c> dotted CLI overrides onto a mutable <see cref="ConsoleTomlFile"/> before it is folded into a <see cref="ConsoleHostConfig"/>, the console.toml analogue of DMCBK.Core's <c>ClientTomlOverrideBinder</c>.
/// Path matching is case-insensitive.
/// Only the commonly toggled keys are bound (the rest of console.toml is edited on disk); an unbound path is warn-and-ignored, same tolerance policy as the client.toml binder.
/// </summary>
internal static class ConsoleTomlOverrideBinder
{
    private static readonly IReadOnlyDictionary<string, Action<ConsoleTomlFile, string>> Setters = Build();

    /// <summary>The documented bindable paths (lowercased), for help text and tests.</summary>
    public static IReadOnlyCollection<string> BindablePaths => (IReadOnlyCollection<string>)Setters.Keys;

    /// <summary>
    /// Applies one <c>table.key=value</c> override (the path with its leading <c>console.</c> already stripped by the caller).
    /// Unknown paths report through <paramref name="warn"/> and are otherwise ignored.
    /// </summary>
    public static void Apply(ConsoleTomlFile model, string path, string value, Action<string> warn)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(warn);

        string key = path.Trim().ToLowerInvariant();
        if (Setters.TryGetValue(key, out Action<ConsoleTomlFile, string>? setter))
            setter(model, value);
        else
            warn($"Unknown override '--console.{path}=...'; ignored.");
    }

    private static Dictionary<string, Action<ConsoleTomlFile, string>> Build() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["general.showinventorylayout"] = (m, v) => m.General.ShowInventoryLayout = ParseBool(v, m.General.ShowInventoryLayout),
        ["general.showeffectnamesintui"] = (m, v) => m.General.ShowEffectNamesInTui = ParseBool(v, m.General.ShowEffectNamesInTui),
        ["general.consolemode"] = (m, v) => m.General.ConsoleMode = v,
        ["general.timestamps"] = (m, v) => m.General.Timestamps = ParseBool(v, m.General.Timestamps),
        ["general.consolecolormode"] = (m, v) => m.General.ConsoleColorMode = v,
        ["general.glyphs"] = (m, v) => m.General.Glyphs = v,
        ["general.echocommands"] = (m, v) => m.General.EchoCommands = ParseBool(v, m.General.EchoCommands),
        ["commandsuggestion.enable"] = (m, v) => m.CommandSuggestion.Enable = ParseBool(v, m.CommandSuggestion.Enable),
        ["minimap.enabled"] = (m, v) => m.Minimap.Enabled = ParseBool(v, m.Minimap.Enabled),
    };

    private static bool ParseBool(string value, bool fallback) => value.Trim().ToLowerInvariant() switch
    {
        "true" or "1" or "yes" or "on" => true,
        "false" or "0" or "no" or "off" => false,
        _ => fallback,
    };
}
