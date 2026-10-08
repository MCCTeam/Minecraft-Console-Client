using Mcc.Cli.Tui.Minimap;
using Tomlet.Attributes;

namespace Mcc.Cli.Configuration;

/// <summary>
/// The host-owned <c>console.toml</c> model (DMCBK.Core knows nothing about it).
/// The general blocks are wired (timestamps toggle, chat visibility, color on/off), and so are the Minimap and TabList blocks the TUI consumes.
/// Comment placeholders reuse the <c>$Console.*$</c> corpus keys carried in DMCBK.Core's embedded ConfigComments corpus.
/// </summary>
internal sealed class ConsoleTomlFile
{
    [TomlPrecedingComment("$Console.General$")]
    public GeneralTable General { get; set; } = new();

    [TomlPrecedingComment("$Console.CommandSuggestion$")]
    public CommandSuggestionTable CommandSuggestion { get; set; } = new();

    [TomlPrecedingComment("$Console.Minimap$")]
    public MinimapTable Minimap { get; set; } = new();

    [TomlPrecedingComment("$Console.TabList$")]
    public TabListTable TabList { get; set; } = new();

    internal sealed class GeneralTable
    {
        public bool ShowInventoryLayout { get; set; } = true;

        public bool ShowEffectNamesInTui { get; set; }

        [TomlPrecedingComment("$Console.General.ConsoleMode$")]
        public string ConsoleMode { get; set; } = "classic";

        [TomlPrecedingComment("$Console.General.ConsoleColorMode$")]
        public string ConsoleColorMode { get; set; } = "vt100_24bit";

        [TomlPrecedingComment("$Console.General.Display_Icon_Banner$")]
        public bool DisplayIconBanner { get; set; } = true;

        [TomlPrecedingComment("$Console.General.Display_Input$")]
        public bool DisplayInput { get; set; } = true;

        [TomlPrecedingComment("$Console.General.Display_Chat$")]
        public bool DisplayChat { get; set; } = true;

        // Whether a terminal can draw an emoji is a property of the CONSOLE, not of the world, which is why this lives here and not in client.toml [Gameplay] where legacy's EnableEmoji sat.
        [TomlPrecedingComment("$Console.General.Glyphs$")]
        public string Glyphs { get; set; } = "auto";

        [TomlPrecedingComment("$Console.General.Echo_Commands$")]
        public bool EchoCommands { get; set; } = true;

        // Prefix printed chat lines with a wall-clock time.
        // The TUI honors it too.
        public bool Timestamps { get; set; }

        [TomlPrecedingComment("$Console.General.History_Input_Records$")]
        public int HistoryInputRecords { get; set; } = 32;

        // Console window title (Windows only); supports %username%/%serverip% placeholders.
        // Undocumented in the generated file, matching legacy's Main.Advanced.ConsoleTitle (Settings.cs:792-793).
        public string ConsoleTitle { get; set; } = "%username%@%serverip% - Minecraft Console Client";

        [TomlPrecedingComment("$Console.General.TUI_Log_Scrollback$")]
        public int TuiLogScrollback { get; set; } = 3000;
    }

    internal sealed class CommandSuggestionTable
    {
        [TomlPrecedingComment("$Console.CommandSuggestion.Enable$")]
        public bool Enable { get; set; } = true;

        [TomlPrecedingComment("$Console.CommandSuggestion.Use_Basic_Arrow$")]
        public bool UseBasicArrow { get; set; }

        // Suggestion popup sizing and the 7 suggestion colors (hex "#rrggbb"), only visible with ConsoleColorMode = vt100_24bit (see the Console.CommandSuggestion corpus comment).
        // Undocumented in the generated file, matching legacy (Settings.cs:1260-1287), which leaves these uncommented too.
        public int MaxSuggestionWidth { get; set; } = 30;

        public int MaxDisplayedSuggestions { get; set; } = 10;

        public string TextColor { get; set; } = "#f8fafc";

        public string TextBackgroundColor { get; set; } = "#64748b";

        public string HighlightTextColor { get; set; } = "#334155";

        public string HighlightTextBackgroundColor { get; set; } = "#fde047";

        public string TooltipColor { get; set; } = "#7dd3fc";

        public string HighlightTooltipColor { get; set; } = "#3b82f6";

        public string ArrowSymbolColor { get; set; } = "#d1d5db";
    }

    // Storage-only in this model; the TUI consumes these.
    internal sealed class MinimapTable
    {
        [TomlPrecedingComment("$Console.Minimap.Enabled$")]
        public bool Enabled { get; set; }

        [TomlPrecedingComment("$Console.Minimap.Zoom$")]
        public int Zoom { get; set; } = 1;

        [TomlPrecedingComment("$Console.Minimap.Width$")]
        public int Width { get; set; } = 40;

        [TomlPrecedingComment("$Console.Minimap.Height$")]
        public int Height { get; set; } = 20;

        [TomlPrecedingComment("$Console.Minimap.Position$")]
        public string Position { get; set; } = "top_right";

        [TomlPrecedingComment("$Console.Minimap.ShowPlayerNames$")]
        public bool ShowPlayerNames { get; set; } = true;

        [TomlPrecedingComment("$Console.Minimap.ShowHostileNames$")]
        public bool ShowHostileNames { get; set; } = true;

        [TomlPrecedingComment("$Console.Minimap.ShowNeutralNames$")]
        public bool ShowNeutralNames { get; set; } = true;

        [TomlPrecedingComment("$Console.Minimap.ShowPassiveNames$")]
        public bool ShowPassiveNames { get; set; } = true;

        [TomlPrecedingComment("$Console.Minimap.RefreshInterval$")]
        public int RefreshInterval { get; set; } = MinimapControl.DefaultRefreshMs;

        [TomlPrecedingComment("$Console.Minimap.CaveMode$")]
        public bool CaveMode { get; set; }
    }

    internal sealed class TabListTable
    {
        [TomlPrecedingComment("$Console.TabList.ShowTeams$")]
        public bool ShowTeams { get; set; }
    }
}
