using DMCBK.Core;
using Umpk.Text;

namespace Mcc.Cli.Tui.Presentation;

/// <summary>The 5-bar ping gauge tier a latency falls into, ported from legacy's <c>TabListFormatter.GetPingCell</c> thresholds.</summary>
internal enum PingTier
{
    /// <summary>No measurement yet (latency &lt; 0).</summary>
    Unknown,
    Excellent,
    Good,
    Fair,
    Poor,
    Bad,
}

/// <summary>
/// Pure tab-list presentation logic for the TUI overlay: the ping gauge, team-membership indexing, team-aware/gamemode-aware sort, and multi-column layout math.
/// Ported from legacy's <c>TabList/TabListFormatter.cs</c>.
/// Kept Avalonia-free (works in <see cref="Umpk.Text.TextColor"/> and plain data) so it is directly unit-testable, following the same pattern as <see cref="Minimap.MinimapColorMap"/>/<see cref="Minimap.MinimapEntityClassifier"/>.
/// </summary>
internal static class TabListFormatting
{
    /// <summary>The row count a column holds before a new column starts (legacy's <c>MaxRowsPerColumn</c>).</summary>
    public const int MaxRowsPerColumn = 20;

    /// <summary>The 5-bar ping tier for a latency in milliseconds; negative means "not yet measured".</summary>
    public static PingTier GetPingTier(int pingMs) => pingMs switch
    {
        < 0 => PingTier.Unknown,
        < 150 => PingTier.Excellent,
        < 300 => PingTier.Good,
        < 600 => PingTier.Fair,
        < 1000 => PingTier.Poor,
        _ => PingTier.Bad,
    };

    /// <summary>The number of filled bars (0-5) for a latency, matching <see cref="GetPingTier"/>'s thresholds.</summary>
    public static int GetFilledBars(int pingMs) => GetPingTier(pingMs) switch
    {
        PingTier.Unknown => 0,
        PingTier.Excellent => 5,
        PingTier.Good => 4,
        PingTier.Fair => 3,
        PingTier.Poor => 2,
        _ => 1,
    };

    /// <summary>The right-aligned "<c>NNNNms</c>" label for a latency, or "<c>???ms</c>" when unmeasured.</summary>
    public static string FormatPingMs(int pingMs) => pingMs >= 0 ? $"{Math.Min(pingMs, 9999)}ms" : "???ms";

    /// <summary>
    /// Whether <paramref name="gameMode"/> (a <see cref="TabListEntryInfo.GameMode"/> name) is spectator, sorted after everyone else and shown in italic/dim (matching legacy's gamemode==3 special-casing).
    /// </summary>
    public static bool IsSpectator(string gameMode) => string.Equals(gameMode, "Spectator", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indexes teams by member name (case-insensitive: player names on the wire are already case-normalized, but the tab-list <see cref="TabListEntryInfo.Name"/> and a team's <see cref="TeamInfo.Members"/> entries are matched loosely here to be forgiving of either).
    /// </summary>
    public static IReadOnlyDictionary<string, TeamInfo> IndexTeamsByMember(IReadOnlyList<TeamInfo> teams)
    {
        ArgumentNullException.ThrowIfNull(teams);
        var map = new Dictionary<string, TeamInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (TeamInfo team in teams)
        {
            foreach (string member in team.Members)
                map[member] = team;
        }

        return map;
    }

    /// <summary>
    /// A team's display label: the display name when it differs from the internal name, else the internal name unless it looks like an opaque generated id (a GUID, or a long hyphen-heavy string), in which case empty (the caller renders a placeholder).
    /// Ported from legacy's <c>GetTeamLabel</c>/<c>LooksLikeOpaqueTeamName</c>.
    /// </summary>
    public static string TeamLabel(TeamInfo? team)
    {
        if (team is null || string.IsNullOrWhiteSpace(team.Name))
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(team.DisplayName) && !string.Equals(team.DisplayName, team.Name, StringComparison.Ordinal))
            return team.DisplayName;

        return LooksLikeOpaqueTeamName(team.Name) ? string.Empty : team.Name;
    }

    /// <summary>True for a GUID or a 24+ character string with 3+ hyphens (an internal/generated team id, not a human label).</summary>
    public static bool LooksLikeOpaqueTeamName(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Guid.TryParse(text, out _))
            return true;

        int hyphenCount = text.Count(static ch => ch == '-');
        return text.Length >= 24 && hyphenCount >= 3;
    }

    /// <summary>
    /// Maps a vanilla team-color index (0-15 named, 21/other = none) to its <see cref="TextColor"/>, or null for "no color" (the reader falls back to whatever default the surrounding text uses).
    /// </summary>
    public static TextColor? TeamColor(int colorIndex) => colorIndex switch
    {
        0 => TextColor.Black,
        1 => TextColor.DarkBlue,
        2 => TextColor.DarkGreen,
        3 => TextColor.DarkAqua,
        4 => TextColor.DarkRed,
        5 => TextColor.DarkPurple,
        6 => TextColor.Gold,
        7 => TextColor.Gray,
        8 => TextColor.DarkGray,
        9 => TextColor.Blue,
        10 => TextColor.Green,
        11 => TextColor.Aqua,
        12 => TextColor.Red,
        13 => TextColor.LightPurple,
        14 => TextColor.Yellow,
        15 => TextColor.White,
        _ => null,
    };

    /// <summary>
    /// Sorts listed entries for display: primarily by <see cref="TabListEntryInfo.ListOrder"/> descending (matching UMPK's own <c>TabListSnapshot</c> ordering), then spectators last, then grouped by team name, then alphabetically by name.
    /// Ported from legacy's <c>TabListFormatter.Render</c> ordering chain (minus the vanilla list-order tiebreak legacy did not have either).
    /// Unlisted/blank-named entries are dropped.
    /// </summary>
    public static IReadOnlyList<TabListEntryInfo> Sort(
        IReadOnlyList<TabListEntryInfo> entries, IReadOnlyDictionary<string, TeamInfo> teamByMember)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(teamByMember);

        return entries
            .Where(static e => e.Listed && !string.IsNullOrWhiteSpace(e.Name))
            .OrderByDescending(e => e.ListOrder)
            .ThenBy(e => IsSpectator(e.GameMode) ? 1 : 0)
            .ThenBy(e => teamByMember.TryGetValue(e.Name, out TeamInfo? team) ? team.Name : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The column count for <paramref name="entryCount"/> listed entries, adding a column whenever the per-column row count would exceed <see cref="MaxRowsPerColumn"/>.
    /// Ported from legacy's column-growth loop in <c>BuildTableLines</c>.
    /// </summary>
    public static int ColumnCount(int entryCount)
    {
        if (entryCount <= 0)
            return 1;

        int columns = 1;
        int rows = entryCount;
        while (rows > MaxRowsPerColumn)
        {
            columns++;
            rows = (entryCount + columns - 1) / columns;
        }

        return columns;
    }

    /// <summary>The row count per column for <paramref name="entryCount"/> entries split across <paramref name="columns"/> columns.</summary>
    public static int RowsPerColumn(int entryCount, int columns)
    {
        if (columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns));

        return (entryCount + columns - 1) / columns;
    }
}
