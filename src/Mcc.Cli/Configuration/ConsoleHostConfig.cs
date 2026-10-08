using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using Mcc.Cli.Tui.Minimap;
using DMCBK.Core.Configuration;
using DMCBK.Core.Presentation;
using Microsoft.Extensions.Logging;
using Tomlet;
using Tomlet.Models;

namespace Mcc.Cli.Configuration;

/// <summary>
/// The validated, host-owned console settings loaded from <c>console.toml</c>.
/// </summary>
internal sealed record ConsoleHostConfig
{
    /// <summary>Draw the host's inventory layout above item listings.</summary>
    public bool ShowInventoryLayout { get; init; } = true;

    /// <summary>Show effect names instead of icons in the TUI status bar.</summary>
    public bool ShowEffectNamesInTui { get; init; }

    /// <summary>Prefix printed chat lines with a timestamp.</summary>
    public bool Timestamps { get; init; }

    /// <summary>Print incoming chat to the console.</summary>
    public bool DisplayChat { get; init; } = true;

    /// <summary>Whether ANSI color output is enabled (color mode other than <c>disable</c>).</summary>
    public bool ColorEnabled { get; init; } = true;

    /// <summary>The configured ANSI color depth (console.toml <c>ConsoleColorMode</c>).</summary>
    public ConsoleColorDepth ColorDepth { get; init; } = ConsoleColorDepth.Vt10024Bit;

    /// <summary>The raw console mode (classic/tui); set it to tui to run the TUI host.</summary>
    public string ConsoleMode { get; init; } = "classic";

    /// <summary>
    /// How status glyphs are drawn (console.toml <c>[General] Glyphs</c>).
    /// Successor to legacy's <c>Main.Advanced.EnableEmoji</c>, moved here because it is a console property.
    /// Still the raw mode: <see cref="GlyphMode.Auto"/> is resolved against the live terminal by <see cref="GlyphModeResolver"/> during startup.
    /// </summary>
    public GlyphMode GlyphMode { get; init; } = GlyphMode.Auto;

    /// <summary>
    /// Echo an executed command into the console log (console.toml <c>[General] EchoCommands</c>).
    /// The line editor clears the input line on Enter, so without this the scrollback holds output with nothing saying what produced it.
    /// </summary>
    public bool EchoCommands { get; init; } = true;

    /// <summary>
    /// Whether the classic startup banner is printed.
    /// Classic mode has no icon, so this repurposes the legacy Display_Icon_Banner toggle to gate the text banner instead.
    /// </summary>
    public bool DisplayIconBanner { get; init; } = true;

    /// <summary>Whether typed input is echoed back to the terminal (ConsoleReader.DisplayUesrInput).</summary>
    public bool DisplayInput { get; init; } = true;

    /// <summary>Input history backread buffer size (ConsoleBuffer.SetBackreadBufferLimit).</summary>
    public int HistoryInputRecords { get; init; } = 32;

    /// <summary>Whether the command-suggestion popup is computed at all.</summary>
    public bool SuggestionsEnabled { get; init; } = true;

    /// <summary>Whether the suggestion popup arrows fall back to plain ASCII ('^'/'v') glyphs.</summary>
    public bool SuggestionUseBasicArrow { get; init; }

    /// <summary>Maximum suggestion entry width in characters (ConsoleSuggestion.SetMaxSuggestionLength).</summary>
    public int SuggestionMaxWidth { get; init; } = 30;

    /// <summary>Maximum number of suggestions shown at once (ConsoleSuggestion.SetMaxSuggestionCount).</summary>
    public int SuggestionMaxDisplayed { get; init; } = 10;

    /// <summary>The suggestion popup's normal text color ("#rrggbb"; only visible at vt100_24bit).</summary>
    public string SuggestionTextColor { get; init; } = "#f8fafc";

    /// <summary>The suggestion popup's normal background color.</summary>
    public string SuggestionTextBackgroundColor { get; init; } = "#64748b";

    /// <summary>The suggestion popup's highlighted (selected) entry text color.</summary>
    public string SuggestionHighlightTextColor { get; init; } = "#334155";

    /// <summary>The suggestion popup's highlighted (selected) entry background color.</summary>
    public string SuggestionHighlightTextBackgroundColor { get; init; } = "#fde047";

    /// <summary>The suggestion popup's tooltip text color.</summary>
    public string SuggestionTooltipColor { get; init; } = "#7dd3fc";

    /// <summary>The suggestion popup's highlighted tooltip text color.</summary>
    public string SuggestionHighlightTooltipColor { get; init; } = "#3b82f6";

    /// <summary>The suggestion popup's up/down arrow glyph color.</summary>
    public string SuggestionArrowSymbolColor { get; init; } = "#d1d5db";

    /// <summary>
    /// Console window title template (Windows only).
    /// Supports %username%/%serverip% placeholder expansion.
    /// </summary>
    public string ConsoleTitle { get; init; } = "%username%@%serverip% - Minecraft Console Client";

    /// <summary>Maximum log/chat lines kept in the TUI scrollback (0 = automatic, resolves to 3000).</summary>
    public int TuiLogScrollback { get; init; } = 3000;

    /// <summary>Whether the TUI minimap starts enabled.</summary>
    public bool MinimapEnabled { get; init; }

    /// <summary>The TUI minimap blocks-per-pixel zoom (1..16).</summary>
    public int MinimapZoom { get; init; } = 1;

    /// <summary>The TUI minimap width in cells.</summary>
    public int MinimapWidth { get; init; } = 40;

    /// <summary>The TUI minimap height in cells.</summary>
    public int MinimapHeight { get; init; } = 20;

    /// <summary>The TUI minimap anchor position (top_left/top_right/bottom_left/bottom_right/center).</summary>
    public string MinimapPosition { get; init; } = "top_right";

    /// <summary>Whether the minimap shows player name labels.</summary>
    public bool MinimapShowPlayerNames { get; init; } = true;

    /// <summary>Whether the minimap shows hostile mob name labels.</summary>
    public bool MinimapShowHostileNames { get; init; } = true;

    /// <summary>Whether the minimap shows neutral mob name labels.</summary>
    public bool MinimapShowNeutralNames { get; init; } = true;

    /// <summary>Whether the minimap shows passive mob name labels.</summary>
    public bool MinimapShowPassiveNames { get; init; } = true;

    /// <summary>The minimap's sample/refresh interval in milliseconds (clamped 100-5000).</summary>
    public int MinimapRefreshIntervalMs { get; init; } = Tui.Minimap.MinimapControl.DefaultRefreshMs;

    /// <summary>Whether the minimap starts in cave mode.</summary>
    public bool MinimapCaveMode { get; init; }

    /// <summary>Whether the TUI tab-list overlay shows a team column with team-colored prefix/name/suffix.</summary>
    public bool TabListShowTeams { get; init; }

    /// <summary>
    /// Loads console.toml from a configurations folder, generating a commented default on first run.
    /// A malformed file falls back to defaults, same as before, but now also logs a visible warning through <paramref name="logger"/> (the CLI host's bootstrap logger) so a silently-ignored parse failure is not silent anymore.
    /// <paramref name="overrides"/> carries <c>--console.&lt;table&gt;.&lt;key&gt;=value</c> dotted CLI overrides, applied after the file parses and before validation/derivation, so they win over the on-disk file the same way client.toml's dotted overrides do.
    /// </summary>
    public static ConsoleHostConfig Load(
        string folder, ILogger? logger = null, IReadOnlyList<KeyValuePair<string, string>>? overrides = null)
    {
        string path = Path.Combine(folder, ConfigurationPaths.ConsoleFileName);
        var writer = new DefaultConfigWriter(new ConfigCommentSource());

        if (!File.Exists(path))
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(path, writer.Serialize(new ConsoleTomlFile()));
        }

        ConsoleTomlFile file;
        try
        {
            TomlDocument document = new TomlParser().Parse(File.ReadAllText(path));
            file = TomletMain.To<ConsoleTomlFile>(document);
        }
        catch (Exception ex)
        {
            logger?.LogWarning("{Message}", Strings.ConsoleConfigParseFailed(path, ex.Message));
            file = new ConsoleTomlFile();
        }

        if (overrides is { Count: > 0 })
        {
            foreach (KeyValuePair<string, string> kv in overrides)
                ConsoleTomlOverrideBinder.Apply(file, kv.Key, kv.Value, message => logger?.LogWarning("{Message}", message));
        }

        ConsoleColorDepth colorDepth = TerminalCapability.ParseColorDepth(file.General.ConsoleColorMode);

        return new ConsoleHostConfig
        {
            ShowInventoryLayout = file.General.ShowInventoryLayout,
            ShowEffectNamesInTui = file.General.ShowEffectNamesInTui,
            Timestamps = file.General.Timestamps,
            DisplayChat = file.General.DisplayChat,
            ColorEnabled = colorDepth != ConsoleColorDepth.Disable,
            ColorDepth = colorDepth,
            ConsoleMode = file.General.ConsoleMode,
            GlyphMode = ParseGlyphMode(file.General.Glyphs),
            EchoCommands = file.General.EchoCommands,
            DisplayIconBanner = file.General.DisplayIconBanner,
            DisplayInput = file.General.DisplayInput,
            HistoryInputRecords = file.General.HistoryInputRecords,
            SuggestionsEnabled = file.CommandSuggestion.Enable,
            SuggestionUseBasicArrow = file.CommandSuggestion.UseBasicArrow,
            SuggestionMaxWidth = file.CommandSuggestion.MaxSuggestionWidth,
            SuggestionMaxDisplayed = file.CommandSuggestion.MaxDisplayedSuggestions,
            SuggestionTextColor = file.CommandSuggestion.TextColor,
            SuggestionTextBackgroundColor = file.CommandSuggestion.TextBackgroundColor,
            SuggestionHighlightTextColor = file.CommandSuggestion.HighlightTextColor,
            SuggestionHighlightTextBackgroundColor = file.CommandSuggestion.HighlightTextBackgroundColor,
            SuggestionTooltipColor = file.CommandSuggestion.TooltipColor,
            SuggestionHighlightTooltipColor = file.CommandSuggestion.HighlightTooltipColor,
            SuggestionArrowSymbolColor = file.CommandSuggestion.ArrowSymbolColor,
            ConsoleTitle = file.General.ConsoleTitle,
            TuiLogScrollback = file.General.TuiLogScrollback > 0 ? file.General.TuiLogScrollback : 3000,
            MinimapEnabled = file.Minimap.Enabled,
            MinimapZoom = file.Minimap.Zoom,
            MinimapWidth = file.Minimap.Width,
            MinimapHeight = file.Minimap.Height,
            MinimapPosition = file.Minimap.Position,
            MinimapShowPlayerNames = file.Minimap.ShowPlayerNames,
            MinimapShowHostileNames = file.Minimap.ShowHostileNames,
            MinimapShowNeutralNames = file.Minimap.ShowNeutralNames,
            MinimapShowPassiveNames = file.Minimap.ShowPassiveNames,
            MinimapRefreshIntervalMs = Math.Clamp(
                file.Minimap.RefreshInterval, Tui.Minimap.MinimapControl.MinRefreshMs, Tui.Minimap.MinimapControl.MaxRefreshMs),
            MinimapCaveMode = file.Minimap.CaveMode,
            TabListShowTeams = file.TabList.ShowTeams,
        };
    }

    /// <summary>
    /// Parses <c>[General] Glyphs</c>.
    /// An unrecognised value falls back to <see cref="GlyphMode.Auto"/> rather than failing the load: a typo in a cosmetic key must not stop the client from starting.
    /// </summary>
    private static GlyphMode ParseGlyphMode(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "emoji" or "on" or "true" => GlyphMode.Emoji,
        "ascii" or "off" or "false" or "none" => GlyphMode.Ascii,
        _ => GlyphMode.Auto,
    };
}
