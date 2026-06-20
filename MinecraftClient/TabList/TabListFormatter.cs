using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;

namespace MinecraftClient
{
    internal sealed record TabListSnapshot(string Header, string Footer, IReadOnlyList<TabListEntry> Entries);

    internal sealed record TabListEntry(
        Guid Uuid,
        string Name,
        string DisplayName,
        string TeamName,
        string TeamDisplayName,
        int Gamemode,
        int Ping,
        int TabListOrder,
        bool Listed);

    internal static class TabListFormatter
    {
        private const int MaxRowsPerColumn = 20;
        private const int PingColumnWidth = 11;
        private const int ColumnGapWidth = 4;

        public static string FormatTeamMemberName(string playerName, PlayerTeam? team)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(playerName);

            if (team is null)
                return playerName;

            var sb = new StringBuilder();
            sb.Append(team.Prefix);

            string colorCode = TeamColorToTag(team.Color);
            if (!string.IsNullOrEmpty(colorCode))
                sb.Append(colorCode);

            sb.Append(playerName);
            sb.Append(team.Suffix);
            return sb.ToString();
        }

        public static string Render(TabListSnapshot snapshot, bool includeOverlayHint = false)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            bool showTeams = Settings.Config.Console.TabList.ShowTeams;

            var lines = new List<string>
            {
                $"§e{string.Format(Translations.cmd_tab_title, snapshot.Entries.Count)}§r"
            };

            AppendSection(lines, snapshot.Header);

            var listedEntries = snapshot.Entries
                .Where(static entry => entry.Listed && !string.IsNullOrWhiteSpace(entry.Name))
                .OrderBy(static entry => entry.TabListOrder)
                .ThenBy(static entry => entry.Gamemode == 3 ? 1 : 0)
                .ThenBy(static entry => entry.TeamName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (listedEntries.Count == 0)
            {
                lines.Add($"§7{Translations.cmd_tab_no_players}§r");
            }
            else
            {
                lines.AddRange(BuildTableLines(listedEntries, showTeams));
            }

            AppendSection(lines, snapshot.Footer);

            if (includeOverlayHint)
            {
                lines.Add(string.Empty);
                lines.Add($"§8{Translations.tui_tab_hint}§r");
            }

            return string.Join('\n', lines);
        }

        private static void AppendSection(List<string> lines, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            foreach (string line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }
        }

        private static List<string> BuildTableLines(IReadOnlyList<TabListEntry> entries, bool showTeams)
        {
            int columns = 1;
            int rows = entries.Count;
            while (rows > MaxRowsPerColumn)
            {
                columns++;
                rows = (entries.Count + columns - 1) / columns;
            }

            int teamColumnWidth = showTeams
                ? Math.Max(
                    GetVisibleLength(Translations.cmd_tab_column_team),
                    entries.Max(static entry => GetVisibleLength(GetTeamLabel(entry))))
                : 0;

            string headerRow = BuildRow(
                $"§8{Translations.cmd_tab_column_ping}§r",
                showTeams ? $"§8{Translations.cmd_tab_column_team}§r" : null,
                $"§8{Translations.cmd_tab_column_player}§r",
                teamColumnWidth);

            List<string> renderedRows = entries
                .Select(entry => BuildRow(
                    GetPingCell(entry.Ping),
                    showTeams ? GetTeamLabel(entry) : null,
                    GetPlayerLabel(entry),
                    teamColumnWidth))
                .ToList();

            int[] columnWidths = new int[columns];
            for (int column = 0; column < columns; column++)
            {
                int width = GetVisibleLength(headerRow);
                for (int row = 0; row < rows; row++)
                {
                    int index = row + (column * rows);
                    if (index >= renderedRows.Count)
                        break;

                    width = Math.Max(width, GetVisibleLength(renderedRows[index]));
                }
                columnWidths[column] = width;
            }

            var lines = new List<string> { CombineColumns(headerRow, rows, columns, columnWidths) };
            for (int row = 0; row < rows; row++)
                lines.Add(CombineColumns(renderedRows, row, rows, columns, columnWidths));

            return lines;
        }

        private static string CombineColumns(string headerRow, int rows, int columns, IReadOnlyList<int> columnWidths)
        {
            var perColumnValues = new string[columns];
            for (int column = 0; column < columns; column++)
                perColumnValues[column] = headerRow;

            return CombineColumns(perColumnValues, columnWidths);
        }

        private static string CombineColumns(IReadOnlyList<string> renderedRows, int row, int rows, int columns, IReadOnlyList<int> columnWidths)
        {
            var perColumnValues = new string[columns];
            for (int column = 0; column < columns; column++)
            {
                int index = row + (column * rows);
                perColumnValues[column] = index < renderedRows.Count ? renderedRows[index] : string.Empty;
            }

            return CombineColumns(perColumnValues, columnWidths);
        }

        private static string CombineColumns(IReadOnlyList<string> parts, IReadOnlyList<int> widths)
        {
            var sb = new StringBuilder();
            for (int index = 0; index < parts.Count; index++)
            {
                if (index > 0)
                    sb.Append(' ', ColumnGapWidth);

                sb.Append(PadFormattedRight(parts[index], widths[index]));
            }
            return sb.ToString().TrimEnd();
        }

        private static string BuildRow(string pingCell, string? teamCell, string playerCell, int teamColumnWidth)
        {
            var sb = new StringBuilder();
            sb.Append(PadFormattedRight(pingCell, PingColumnWidth));
            sb.Append("  ");
            if (!string.IsNullOrEmpty(teamCell))
            {
                sb.Append(PadFormattedRight(teamCell, teamColumnWidth));
                sb.Append("  ");
            }
            sb.Append(playerCell);
            return sb.ToString();
        }

        private static string GetPingCell(int ping)
        {
            string barColor;
            int filledBars;

            if (ping < 0)
            {
                barColor = "§8";
                filledBars = 0;
            }
            else if (ping < 150)
            {
                barColor = "§a";
                filledBars = 5;
            }
            else if (ping < 300)
            {
                barColor = "§e";
                filledBars = 4;
            }
            else if (ping < 600)
            {
                barColor = "§6";
                filledBars = 3;
            }
            else if (ping < 1000)
            {
                barColor = "§c";
                filledBars = 2;
            }
            else
            {
                barColor = "§4";
                filledBars = 1;
            }

            string numericPing = ping >= 0 ? $"{Math.Min(ping, 9999),4}ms" : " ???ms";
            return $"{barColor}{new string('|', filledBars)}§8{new string('.', 5 - filledBars)}§r {numericPing}";
        }

        private static string GetTeamLabel(TabListEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.TeamName))
                return "§8-§r";

            if (!string.IsNullOrWhiteSpace(entry.TeamDisplayName))
                return entry.TeamDisplayName;

            if (LooksLikeOpaqueTeamName(entry.TeamName))
                return "§8-§r";

            return entry.TeamName;
        }

        private static string GetPlayerLabel(TabListEntry entry)
        {
            string label = string.IsNullOrWhiteSpace(entry.DisplayName)
                ? entry.Name
                : entry.DisplayName;

            if (entry.Gamemode == 3)
                return $"§7§o{label}§r";

            return label;
        }

        private static string PadFormattedRight(string text, int totalWidth)
        {
            int visibleLength = GetVisibleLength(text);
            if (visibleLength >= totalWidth)
                return text;

            return text + new string(' ', totalWidth - visibleLength);
        }

        private static int GetVisibleLength(string text) => ChatBot.GetVerbatim(text).Length;

        private static bool LooksLikeOpaqueTeamName(string text)
        {
            if (Guid.TryParse(text, out _))
                return true;

            int hyphenCount = text.Count(static ch => ch == '-');
            if (text.Length >= 24 && hyphenCount >= 3)
                return true;

            return false;
        }

        private static string TeamColorToTag(int color) => color switch
        {
            0 => "§0",
            1 => "§1",
            2 => "§2",
            3 => "§3",
            4 => "§4",
            5 => "§5",
            6 => "§6",
            7 => "§7",
            8 => "§8",
            9 => "§9",
            10 => "§a",
            11 => "§b",
            12 => "§c",
            13 => "§d",
            14 => "§e",
            15 => "§f",
            _ => string.Empty
        };
    }
}
