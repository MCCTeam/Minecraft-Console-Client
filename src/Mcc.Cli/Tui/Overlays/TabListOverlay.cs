using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Presentation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DMCBK.Core;
using Umpk.Text;

namespace Mcc.Cli.Tui.Overlays;

/// <summary>
/// The tab-list overlay behind <see cref="DMCBK.Core.Commands.IHostUi.TryShowTabOverlay"/>: a scrolling player list refreshed every 500ms from <see cref="PlayerApi.GetTabListAsync"/> (name, ping, gamemode, header, footer) and, when <paramref name="showTeams"/> is on, <see cref="PlayerApi.GetScoreboardAsync"/> for a team column with team-colored prefix/name/suffix.
/// Rows carry a 5-bar ping gauge, and lists over 20 entries split into side-by-side columns.
/// Escape closes.
/// Ported behavior from the legacy <c>TabListOverlay</c>/<c>TabList/TabListFormatter</c>, fed by the core snapshots and rendered through <see cref="ComponentInlineRenderer"/> so team/ping colors honor the host's resolved color capability the same way chat does.
/// </summary>
internal sealed class TabListOverlay : Border
{
    private const int PingCellWidth = 12; // 5 bar chars + 1 gap + up to "9999ms" (6 chars)
    private const int ColumnGap = 4;

    private readonly MainTuiView _view;
    private readonly GameApi _game;
    private readonly ComponentInlineRenderer? _renderer;
    private readonly bool _showTeams;
    private readonly StackPanel _list;
    private readonly DispatcherTimer _timer;

    private TabListOverlay(MainTuiView view, GameApi game, ComponentInlineRenderer? renderer, bool showTeams)
    {
        _view = view;
        _game = game;
        _renderer = renderer;
        _showTeams = showTeams;
        _list = new StackPanel();

        var scroll = new ScrollViewer
        {
            Content = _list,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        BorderBrush = Brushes.DarkCyan;
        BorderThickness = new Avalonia.Thickness(1);
        Background = new SolidColorBrush(Color.FromRgb(15, 15, 25));
        Padding = new Avalonia.Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Child = scroll;
        Focusable = true;

        KeyDown += OnKeyDown;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => _ = RefreshAsync();
    }

    public static void Open(MainTuiView view, GameApi game, TuiBackend backend, bool showTeams)
    {
        var overlay = new TabListOverlay(view, game, backend.Renderer, showTeams);
        view.ShowOverlay(overlay, overlay.OnClosed);
        overlay._timer.Start();
        _ = overlay.RefreshAsync();
    }

    private void OnClosed() => _timer.Stop();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape)
        {
            _view.HideOverlay();
            e.Handled = true;
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            TabListSnapshot list = await _game.Player.GetTabListAsync().ConfigureAwait(true);
            IReadOnlyDictionary<string, TeamInfo> teamByMember = TabListFormatting.IndexTeamsByMember([]);

            if (_showTeams)
            {
                try
                {
                    ScoreboardSnapshot board = await _game.Player.GetScoreboardAsync().ConfigureAwait(true);
                    teamByMember = TabListFormatting.IndexTeamsByMember(board.Teams);
                }
                catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
                {
                    // No scoreboard this session; render without a team column's data (still keep the column heading so the layout does not jump when a scoreboard shows up later).
                }
            }

            Dispatcher.UIThread.Post(() => Render(list, teamByMember));
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            Dispatcher.UIThread.Post(() => _view.HideOverlay());
        }
    }

    private void Render(TabListSnapshot snapshot, IReadOnlyDictionary<string, TeamInfo> teamByMember)
    {
        _list.Children.Clear();
        if (!string.IsNullOrWhiteSpace(snapshot.Header))
            _list.Children.Add(new TextBlock { Text = snapshot.Header, Foreground = Brushes.Gold });

        IReadOnlyList<TabListEntryInfo> entries = TabListFormatting.Sort(snapshot.Entries, teamByMember);
        _list.Children.Add(new TextBlock
        {
            Text = Strings.TuiTabHeader(entries.Count),
            Foreground = Brushes.Cyan,
            FontWeight = FontWeight.Bold,
        });

        if (entries.Count == 0)
            _list.Children.Add(new TextBlock { Text = Strings.TuiTabNoPlayers, Foreground = Brushes.Gray });
        else
            _list.Children.Add(BuildTable(entries, teamByMember));

        if (!string.IsNullOrWhiteSpace(snapshot.Footer))
            _list.Children.Add(new TextBlock { Text = snapshot.Footer, Foreground = Brushes.Gold });

        _list.Children.Add(new TextBlock { Text = Strings.TuiTabControls, Foreground = Brushes.DarkGray });
    }

    /// <summary>
    /// Builds the (possibly multi-column) table: over <see cref="TabListFormatting.MaxRowsPerColumn"/> rows splits into side-by-side columns, each repeating the header row, matching legacy's <c>BuildTableLines</c>.
    /// </summary>
    private Control BuildTable(IReadOnlyList<TabListEntryInfo> entries, IReadOnlyDictionary<string, TeamInfo> teamByMember)
    {
        int teamColumnWidth = 0;
        if (_showTeams)
        {
            teamColumnWidth = Strings.TuiTabColumnTeam.Length;
            foreach (TabListEntryInfo entry in entries)
            {
                teamByMember.TryGetValue(entry.Name, out TeamInfo? team);
                string label = TabListFormatting.TeamLabel(team) is { Length: > 0 } l ? l : "-";
                teamColumnWidth = Math.Max(teamColumnWidth, label.Length);
            }
        }

        int columns = TabListFormatting.ColumnCount(entries.Count);
        int rowsPerColumn = TabListFormatting.RowsPerColumn(entries.Count, columns);

        var columnsPanel = new StackPanel { Orientation = Orientation.Horizontal };
        for (int column = 0; column < columns; column++)
        {
            var columnStack = new StackPanel
            {
                Margin = column == 0 ? default : new Avalonia.Thickness(ColumnGap, 0, 0, 0),
            };
            columnStack.Children.Add(RenderRow(BuildHeaderRow(teamColumnWidth)));

            int start = column * rowsPerColumn;
            int end = Math.Min(start + rowsPerColumn, entries.Count);
            for (int i = start; i < end; i++)
            {
                TabListEntryInfo entry = entries[i];
                teamByMember.TryGetValue(entry.Name, out TeamInfo? team);
                columnStack.Children.Add(RenderRow(BuildEntryRow(entry, team, teamColumnWidth)));
            }

            columnsPanel.Children.Add(columnStack);
        }

        return columnsPanel;
    }

    private TextBlock RenderRow(Component row) => _renderer is { } renderer
        ? renderer.Render(row, Avalonia.Media.TextWrapping.NoWrap)
        : new TextBlock { Text = row.ToPlainText(), TextWrapping = Avalonia.Media.TextWrapping.NoWrap };

    private Component BuildHeaderRow(int teamColumnWidth)
    {
        List<Component> parts =
        [
            Styled(Strings.TuiTabColumnPing.PadRight(PingCellWidth), TextColor.DarkGray),
            Styled("  ", null),
        ];

        if (_showTeams)
        {
            parts.Add(Styled(Strings.TuiTabColumnTeam.PadRight(teamColumnWidth), TextColor.DarkGray));
            parts.Add(Styled("  ", null));
        }

        parts.Add(Styled(Strings.TuiTabColumnPlayer, TextColor.DarkGray));
        return Group(parts);
    }

    private Component BuildEntryRow(TabListEntryInfo entry, TeamInfo? team, int teamColumnWidth)
    {
        List<Component> parts = [BuildPingCell(entry.Latency), Styled("  ", null)];

        if (_showTeams)
        {
            parts.Add(BuildTeamCell(team, teamColumnWidth));
            parts.Add(Styled("  ", null));
        }

        parts.Add(BuildNameCell(entry, team));
        return Group(parts);
    }

    private static Component BuildPingCell(int pingMs)
    {
        int filled = TabListFormatting.GetFilledBars(pingMs);
        TextColor tierColor = TabListFormatting.GetPingTier(pingMs) switch
        {
            PingTier.Excellent => TextColor.Green,
            PingTier.Good => TextColor.Yellow,
            PingTier.Fair => TextColor.Gold,
            PingTier.Poor => TextColor.Red,
            PingTier.Bad => TextColor.DarkRed,
            _ => TextColor.DarkGray,
        };

        string label = " " + TabListFormatting.FormatPingMs(pingMs).PadLeft(6);
        return Group(
            Styled(new string('|', filled), tierColor),
            Styled(new string('.', 5 - filled), TextColor.DarkGray),
            Styled(label, TextColor.Gray));
    }

    private static Component BuildTeamCell(TeamInfo? team, int width)
    {
        string label = TabListFormatting.TeamLabel(team);
        bool placeholder = label.Length == 0;
        string text = (placeholder ? "-" : label).PadRight(width);
        TextColor? color = placeholder || team is null ? TextColor.DarkGray : TabListFormatting.TeamColor(team.Color);
        return Styled(text, color);
    }

    private static Component BuildNameCell(TabListEntryInfo entry, TeamInfo? team)
    {
        string label = string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.Name : entry.DisplayName;
        string text = (team?.Prefix ?? string.Empty) + label + (team?.Suffix ?? string.Empty);
        bool spectator = TabListFormatting.IsSpectator(entry.GameMode);
        TextColor? color = spectator ? TextColor.Gray : (team is not null ? TabListFormatting.TeamColor(team.Color) : null);
        return new Component(new TextContent(text), new Style { Color = color, Italic = spectator ? true : null });
    }

    private static Component Styled(string text, TextColor? color) => new(new TextContent(text), new Style { Color = color });

    private static Component Group(params Component[] parts) => Group((IReadOnlyList<Component>)parts);

    private static Component Group(IReadOnlyList<Component> parts) => new(new TextContent(string.Empty), Style.Empty, parts);
}
