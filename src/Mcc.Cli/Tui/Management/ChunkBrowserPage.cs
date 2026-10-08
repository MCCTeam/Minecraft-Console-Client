using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;
using DMCBK.Core.Presentation;
using Umpk.Client.Snapshots;
using Umpk.Game.Players;

namespace Mcc.Cli.Tui.Management;

/// <summary>A fullscreen, terminal-sized view of the chunks currently held by the live session.</summary>
internal sealed class ChunkBrowserPage : Grid, IManagementWorkspacePage, IDisposable
{
    private const int MaximumColumns = 101;
    private const int MaximumRows = 61;

    private readonly ManagementWorkspace _workspace;
    private readonly Client _client;
    private readonly TextBlock _summary;
    private readonly StackPanel _map;
    private readonly TextBlock _legend;
    private bool _loading;
    private bool _pendingRefresh;
    private bool _disposed;
    private int _lastColumns;
    private int _lastRows;

    public ChunkBrowserPage(ManagementWorkspace workspace, Client client)
    {
        _workspace = workspace;
        _client = client;
        RowDefinitions = new RowDefinitions("Auto,*,Auto");
        Focusable = true;

        _summary = new TextBlock
        {
            Foreground = ManagementUi.Soft,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Button refresh = ManagementUi.Button(Strings.MgmtRefresh, Refresh);
        refresh.HorizontalAlignment = HorizontalAlignment.Right;
        var toolbar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        toolbar.Children.Add(_summary);
        Grid.SetColumn(refresh, 1);
        toolbar.Children.Add(refresh);
        Children.Add(toolbar);

        _map = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipToBounds = true,
        };
        Grid.SetRow(_map, 1);
        Children.Add(_map);

        _legend = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = ManagementUi.Soft,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0),
        };
        Grid.SetRow(_legend, 2);
        Children.Add(_legend);
        RenderLegend();
    }

    public bool UseWorkspaceScroll => false;

    public string? Hints => Strings.ChunkUiHints;

    public void Refresh()
    {
        if (_disposed)
            return;

        if (_loading)
        {
            _pendingRefresh = true;
            return;
        }

        _ = RefreshAsync();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Refresh();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        (int columns, int rows) = ResolveGridSize(e.NewSize, _client.Commands.Glyphs.IsEmoji);
        if (columns != _lastColumns || rows != _lastRows)
            Refresh();
    }

    internal static (int Columns, int Rows) ResolveGridSize(Size available, bool emoji)
    {
        int cellWidth = emoji ? 2 : 1;
        int width = double.IsFinite(available.Width) && available.Width > 0
            ? Math.Max(1, ((int)Math.Floor(available.Width) - 4) / cellWidth)
            : 25;
        int height = double.IsFinite(available.Height) && available.Height > 0
            ? Math.Max(1, (int)Math.Floor(available.Height) - 4)
            : 17;
        width = MakeOdd(Math.Clamp(width, 1, MaximumColumns));
        height = MakeOdd(Math.Clamp(height, 1, MaximumRows));
        return (width, height);
    }

    private static int MakeOdd(int value) => value % 2 == 0 ? Math.Max(1, value - 1) : value;

    private async Task RefreshAsync()
    {
        _loading = true;
        _pendingRefresh = false;
        (int columns, int rows) = ResolveGridSize(Bounds.Size, _client.Commands.Glyphs.IsEmoji);
        _lastColumns = columns;
        _lastRows = rows;

        try
        {
            PlayerPose pose = await _client.Game.Movement.GetPoseAsync(_workspace.Closed).ConfigureAwait(true);
            ChunkStatusGrid grid = await _client.Game.World
                .GetChunkStatusAsync(columns, rows, _workspace.Closed)
                .ConfigureAwait(true);
            if (_disposed)
                return;

            _summary.Text = Strings.ChunkUiSummary(
                pose.Position.X,
                pose.Position.Y,
                pose.Position.Z,
                grid.CenterChunkX,
                grid.CenterChunkZ,
                grid.LoadedCount,
                grid.Columns * grid.Rows);
            RenderMap(grid, _client.Commands.Glyphs);
            _workspace.SetStatus(null);
        }
        catch (OperationCanceledException) when (_workspace.Closed.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _map.Children.Clear();
            _map.Children.Add(ManagementUi.Body(Strings.ChunkUiNoData, ManagementUi.Muted));
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
        finally
        {
            _loading = false;
            if (_pendingRefresh && !_disposed)
                Refresh();
        }
    }

    private void RenderMap(ChunkStatusGrid grid, GlyphSet glyphs)
    {
        _map.Children.Clear();
        int playerRow = (grid.Rows - 1) / 2;
        int playerColumn = (grid.Columns - 1) / 2;
        for (int row = 0; row < grid.Rows; row++)
        {
            var line = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center };
            line.Inlines ??= [];
            for (int column = 0; column < grid.Columns; column++)
            {
                bool loaded = grid.IsLoadedAt(row, column);
                bool player = row == playerRow && column == playerColumn;
                line.Inlines.Add(new Run(loaded ? glyphs.ChunkLoaded : glyphs.ChunkUnloaded)
                {
                    Foreground = player
                        ? Brushes.Black
                        : loaded ? ManagementUi.Green : ManagementUi.Muted,
                    Background = player ? ManagementUi.Gold : ManagementUi.Canvas,
                });
            }
            _map.Children.Add(line);
        }
    }

    private void RenderLegend()
    {
        GlyphSet glyphs = _client.Commands.Glyphs;
        _legend.Inlines ??= [];
        _legend.Inlines.Clear();
        _legend.Inlines.Add(new Run($"{glyphs.ChunkLoaded} {Strings.ChunkUiLoaded}")
        {
            Foreground = ManagementUi.Green,
        });
        _legend.Inlines.Add(new Run("  "));
        _legend.Inlines.Add(new Run($"{glyphs.ChunkUnloaded} {Strings.ChunkUiUnloaded}")
        {
            Foreground = ManagementUi.Muted,
        });
        _legend.Inlines.Add(new Run("  "));
        _legend.Inlines.Add(new Run($"{glyphs.ChunkLoaded} {Strings.ChunkUiPlayer}")
        {
            Foreground = Brushes.Black,
            Background = ManagementUi.Gold,
        });
    }

    public void Dispose() => _disposed = true;
}
