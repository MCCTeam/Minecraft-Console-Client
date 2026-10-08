using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Presentation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DMCBK.Core;
using Umpk.Client.Snapshots;
using Umpk.Geometry;
using EntitySnapshot = DMCBK.Core.EntitySnapshot;

namespace Mcc.Cli.Tui.Minimap;

/// <summary>
/// The live top-down minimap control.
/// Each cell is an upper-half block (U+2580) so one cell shows two vertical map pixels (foreground = upper row color, background = lower row color), the same rendering idiom as the legacy <c>MinimapControl</c>.
/// It samples the world through the core surface-region feed (<see cref="WorldApi.GetSurfaceRegionAsync"/>) off the UI thread on a timer, colors columns via <see cref="MinimapColorMap"/> with a north-slope height shade and water/ice blending, overlays entities colored and depth-faded by <see cref="MinimapEntityClassifier"/>, bakes per-category name labels onto the map, shows a compass/zoom/cave info row with an auto-collapsing category legend, and feeds per-cell hover tooltips through <see cref="TooltipService"/>.
/// No legacy/UMPK coupling: every feed is the new core.
/// </summary>
internal sealed class MinimapControl : UserControl
{
    public const int MinZoom = 1;
    public const int MaxZoom = 16;

    /// <summary>The default minimap sample/refresh interval in milliseconds (console.toml Minimap.RefreshInterval).</summary>
    public const int DefaultRefreshMs = 1000;

    /// <summary>The lowest allowed refresh interval (console.toml Minimap.RefreshInterval clamp).</summary>
    public const int MinRefreshMs = 100;

    /// <summary>The highest allowed refresh interval (console.toml Minimap.RefreshInterval clamp).</summary>
    public const int MaxRefreshMs = 5000;

    private const int SampleBudget = 50000; // cap sampled columns/refresh so high zoom cannot stall the loop
    private const char HalfBlock = '▀'; // upper half block (U+2580)
    private const int MaxLegendItems = 4;
    private const int MaxTooltipEntities = 4;
    private const int MinMapWidth = 8;
    private const int MaxMapWidth = 240;
    private const int MinMapHeight = 8;
    private const int MaxMapHeight = 96;
    private const int FooterRows = 2;

    // The surface-region feed reports only the resolved surface block, not a true floor-relative water column depth (see WorldApi.GetSurfaceRegionAsync / SurfaceColumn), so water/ice blending here uses a nominal depth rather than a measured one; see MinimapColorMap.BlendWaterColor.
    private const int NominalWaterDepth = 3;

    private readonly GameApi _game;
    private int _width;
    private int _height;
    private TextBlock[,] _cells = new TextBlock[0, 0];
    private readonly StackPanel _grid;
    private readonly StackPanel _infoRow;
    private readonly StackPanel _legendPanel;
    private readonly DispatcherTimer _timer;
    private volatile bool _sampling;
    private bool _started;
    private SampleResult? _lastSample;
    private int _hoverCol = -1;
    private int _hoverRow = -1;

    public MinimapControl(GameApi game, int width, int height, int refreshMs = DefaultRefreshMs)
    {
        _game = game;
        _grid = new StackPanel();
        // Reserve both footer rows before the first sample arrives.
        // The managed window switches from SizeToContent to manual sizing immediately after opening; without this minimum the compass row did not exist during that measurement and was clipped once live data populated it.
        _infoRow = new StackPanel { Orientation = Orientation.Horizontal, MinHeight = 1 };
        _legendPanel = new StackPanel { Orientation = Orientation.Horizontal, MinHeight = 1 };
        var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto,Auto") };
        root.Children.Add(_grid);
        Grid.SetRow(_infoRow, 1);
        root.Children.Add(_infoRow);
        Grid.SetRow(_legendPanel, 2);
        root.Children.Add(_legendPanel);
        Content = root;

        ResizeMap(width, height);

        _grid.PointerMoved += OnGridPointerMoved;
        _grid.PointerExited += (_, _) => HideTooltip();

        int clampedRefreshMs = Math.Clamp(refreshMs, MinRefreshMs, MaxRefreshMs);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(clampedRefreshMs) };
        _timer.Tick += (_, _) => RequestSample();
    }

    /// <inheritdoc/>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        (int width, int height) = ResolveMapSize(e.NewSize, _width, _height);
        ResizeMap(width, height);
    }

    /// <summary>
    /// Converts the live content area to terminal map cells.
    /// Two rows stay pinned for coordinates and the legend, while the map consumes the remaining space.
    /// </summary>
    internal static (int Width, int Height) ResolveMapSize(Size available, int fallbackWidth, int fallbackHeight)
    {
        int width = double.IsFinite(available.Width) && available.Width > 0
            ? (int)Math.Floor(available.Width)
            : fallbackWidth;
        int height = double.IsFinite(available.Height) && available.Height > FooterRows
            ? (int)Math.Floor(available.Height) - FooterRows
            : fallbackHeight;
        return (
            Math.Clamp(width, MinMapWidth, MaxMapWidth),
            Math.Clamp(height, MinMapHeight, MaxMapHeight));
    }

    private void ResizeMap(int width, int height)
    {
        width = Math.Clamp(width, MinMapWidth, MaxMapWidth);
        height = Math.Clamp(height, MinMapHeight, MaxMapHeight);
        if (width == _width && height == _height)
            return;

        HideTooltip();
        _width = width;
        _height = height;
        _cells = new TextBlock[width, height];
        _grid.Children.Clear();

        for (int row = 0; row < height; row++)
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal };
            for (int col = 0; col < width; col++)
            {
                var cell = new TextBlock
                {
                    Text = HalfBlock.ToString(),
                    Foreground = Brushes.Black,
                    Background = Brushes.Black,
                };
                _cells[col, row] = cell;
                line.Children.Add(cell);
            }

            _grid.Children.Add(line);
        }

        _lastSample = null;
        if (_started)
            RequestSample();
    }

    public int Zoom { get; set; } = 1;

    /// <summary>
    /// Kept for the aggregate signal (any-names-on/off): mirrors whether any of the four per-category flags below is on.
    /// Setting it flips all four together (matching the <c>minimap names</c> command, which toggles them together); use <see cref="SetNameFlags"/> to set them independently.
    /// </summary>
    public bool ShowNames
    {
        get => ShowPlayerNames || ShowHostileNames || ShowNeutralNames || ShowPassiveNames;
        set => SetNameFlags(value, value, value, value);
    }

    /// <summary>Whether player name labels are baked onto the map.</summary>
    public bool ShowPlayerNames { get; private set; } = true;

    /// <summary>Whether hostile-mob name labels are baked onto the map.</summary>
    public bool ShowHostileNames { get; private set; } = true;

    /// <summary>Whether neutral-mob name labels are baked onto the map.</summary>
    public bool ShowNeutralNames { get; private set; } = true;

    /// <summary>Whether passive-mob name labels are baked onto the map.</summary>
    public bool ShowPassiveNames { get; private set; } = true;

    public bool CaveMode { get; set; }

    /// <summary>The floating tooltip layer to feed on hover; null disables tooltips (no session view yet).</summary>
    public TuiTooltipService? TooltipService { get; set; }

    public void Start()
    {
        _started = true;
        _timer.Start();
        RequestSample();
    }

    public void Stop()
    {
        _started = false;
        _timer.Stop();
    }

    public void SetZoom(int zoom) => Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);

    public void SetShowNames(bool on) => ShowNames = on;

    /// <summary>Sets the four per-category name-label flags independently.</summary>
    public void SetNameFlags(bool players, bool hostile, bool neutral, bool passive)
    {
        ShowPlayerNames = players;
        ShowHostileNames = hostile;
        ShowNeutralNames = neutral;
        ShowPassiveNames = passive;
    }

    public void SetCaveMode(bool on) => CaveMode = on;

    public static MinimapPosition ParsePosition(string value) => value?.ToLowerInvariant() switch
    {
        "top_left" => MinimapPosition.TopLeft,
        "bottom_left" => MinimapPosition.BottomLeft,
        "bottom_right" => MinimapPosition.BottomRight,
        "center" => MinimapPosition.Center,
        _ => MinimapPosition.TopRight,
    };

    private void RequestSample()
    {
        if (_sampling)
            return;

        _sampling = true;
        _ = SampleAsync();
    }

    private async Task SampleAsync()
    {
        try
        {
            PlayerPose pose = await _game.Movement.GetPoseAsync().ConfigureAwait(false);
            IReadOnlyList<EntitySnapshot> entities = await SafeEntitiesAsync().ConfigureAwait(false);

            int width = _width;
            int height = _height;
            int rows = height * 2;
            int zoom = ResolveZoom(width, rows);
            int spanX = width * zoom;
            int spanZ = rows * zoom;
            int originX = (int)Math.Floor(pose.Position.X) - (spanX / 2);
            int originZ = (int)Math.Floor(pose.Position.Z) - (spanZ / 2);
            int? ceiling = CaveMode ? (int)Math.Floor(pose.Position.Y) : null;

            SurfaceRegionSnapshot region = await _game.World
                .GetSurfaceRegionAsync(originX, originZ, spanX, spanZ, ceiling).ConfigureAwait(false);

            var result = new SampleResult
            {
                Pixels = new Color[width, rows],
                BlockIds = new string?[width, rows],
                EntityMap = new List<HoverEntity>?[width, rows],
                OriginX = originX,
                OriginZ = originZ,
                Zoom = zoom,
                Rows = rows,
                Width = width,
                Height = height,
            };

            BuildPixels(result, region);
            HashSet<MobCategory> visibleCategories = OverlayEntities(result, entities, pose.Position.Y, out List<NameLabel> nameLabels);

            Dispatcher.UIThread.Post(() => Apply(result, pose, visibleCategories, nameLabels));
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _infoRow.Children.Clear();
                _infoRow.Children.Add(new TextBlock { Text = Strings.TuiStatusOffline, Foreground = Brushes.Gray });
            });
        }
        finally
        {
            _sampling = false;
        }
    }

    private async Task<IReadOnlyList<EntitySnapshot>> SafeEntitiesAsync()
    {
        try
        {
            return await _game.Entities.AllAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            return [];
        }
    }

    private int ResolveZoom(int width, int rows)
    {
        int zoom = Math.Clamp(Zoom, MinZoom, MaxZoom);
        while (zoom > MinZoom && (long)width * zoom * rows * zoom > SampleBudget)
            zoom--;

        return zoom;
    }

    private void BuildPixels(SampleResult result, SurfaceRegionSnapshot region)
    {
        int half = result.Zoom / 2;
        for (int pz = 0; pz < result.Rows; pz++)
        {
            for (int px = 0; px < result.Width; px++)
            {
                int col = Math.Min((px * result.Zoom) + half, region.Width - 1);
                int row = Math.Min((pz * result.Zoom) + half, region.Length - 1);
                SurfaceColumn sample = region.Columns[(row * region.Width) + col];
                (result.Pixels[px, pz], result.BlockIds[px, pz]) = ColorFor(region, sample, col, row);
            }
        }
    }

    private static (Color Color, string? BlockId) ColorFor(SurfaceRegionSnapshot region, SurfaceColumn sample, int col, int row)
    {
        if (!sample.Loaded || !sample.Found)
            return (MinimapColorMap.VoidColor, null);

        string blockId = sample.State.Block.Id.ToString();
        bool isWater = sample.State.IsWaterlogged || MinimapColorMap.IsWater(blockId);
        bool isIce = !isWater && MinimapColorMap.IsIce(blockId);

        Color baseColor = isWater
            ? MinimapColorMap.BlendWaterColor(MinimapColorMap.UnknownColor, NominalWaterDepth)
            : isIce
                ? MinimapColorMap.BlendIceColor(MinimapColorMap.UnknownColor)
                : MinimapColorMap.GetBaseColor(blockId);

        // North-slope shade: compare this column's surface height to the neighbor to the north (row-1).
        int deltaY = 0;
        if (row > 0)
        {
            SurfaceColumn north = region.Columns[((row - 1) * region.Width) + col];
            if (north.Found)
                deltaY = sample.SurfaceY - north.SurfaceY;
        }

        return (MinimapColorMap.ApplyHeightShade(baseColor, deltaY), blockId);
    }

    /// <summary>
    /// Colors/fades/culls entities onto <paramref name="result"/>'s pixel grid, fills the per-pixel hover map and collects the name labels to bake.
    /// Returns the set of categories actually visible this frame (for the legend).
    /// </summary>
    private HashSet<MobCategory> OverlayEntities(
        SampleResult result, IReadOnlyList<EntitySnapshot> entities, double playerY, out List<NameLabel> nameLabels)
    {
        var priority = new int[result.Width, result.Rows];
        var visible = new HashSet<MobCategory>();
        nameLabels = [];
        int centerX = result.Width / 2;
        int centerZ = result.Rows / 2;

        foreach (EntitySnapshot entity in entities)
        {
            bool isPlayer = entity.PlayerName is not null;
            MobCategory category = MinimapEntityClassifier.Classify(entity.TypeId, isPlayer);
            if (category == MobCategory.NonLiving)
                continue;

            // Entities several floors below the player (through floors/caves) are dropped outright rather than just faded, matching legacy's ShouldDisplay (players are always shown).
            if (!MinimapEntityClassifier.ShouldDisplay(category, playerY, entity.Position.Y))
                continue;

            int px = (int)((entity.Position.X - result.OriginX) / result.Zoom);
            int pz = (int)((entity.Position.Z - result.OriginZ) / result.Zoom);
            if (px < 0 || px >= result.Width || pz < 0 || pz >= result.Rows)
                continue;

            // Same coordinate the player marker is drawn at below: an entity exactly on the player's own pixel (almost always the player's own entity, when self-tracking is on) would otherwise fight the marker for that cell.
            bool isSelfPixel = px == centerX && pz == centerZ;

            string name = entity.PlayerName ?? entity.CustomName ?? RegistryKey.ToPascal(entity.TypeId);
            var hover = new HoverEntity(name, category, entity.Position.X, entity.Position.Y, entity.Position.Z);
            (result.EntityMap[px, pz] ??= []).Add(hover);

            // Depth-based fade: entities below the player fade toward gray with depth; players never fade.
            Color baseColor = MinimapEntityClassifier.GetColor(category);
            if (!isPlayer)
                baseColor = MinimapEntityClassifier.ApplyDepthFade(baseColor, playerY, entity.Position.Y);

            int p = MinimapEntityClassifier.GetPriority(category) + 1;
            if (p > priority[px, pz])
            {
                priority[px, pz] = p;
                result.Pixels[px, pz] = baseColor;
            }

            visible.Add(category);

            if (ShouldShowName(category) && !isSelfPixel)
                nameLabels.Add(new NameLabel(name, baseColor, px, pz, MinimapEntityClassifier.GetPriority(category)));
        }

        return visible;
    }

    private bool ShouldShowName(MobCategory category) => category switch
    {
        MobCategory.Player => ShowPlayerNames,
        MobCategory.Hostile => ShowHostileNames,
        MobCategory.Neutral => ShowNeutralNames,
        MobCategory.Passive => ShowPassiveNames,
        _ => false,
    };

    /// <summary>
    /// Bakes name labels onto the map as a cell-level character overlay, ported from legacy's <c>BakeNameLabels</c>: labels are placed one cell row below their entity (or above, near the bottom edge), centered horizontally, highest-priority category first, and skipped when they would overlap an already-placed label.
    /// </summary>
    private (char Ch, Color Fg, Color Bg)?[,] BakeNameLabels(SampleResult result, List<NameLabel> labels)
    {
        var overlay = new (char, Color, Color)?[result.Width, result.Height];
        if (labels.Count == 0)
            return overlay;

        labels.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        var occupied = new HashSet<(int Col, int Row)>();

        foreach (NameLabel label in labels)
        {
            int cellRow = (label.PixelY / 2) + 1;
            if (cellRow >= result.Height)
                cellRow = (label.PixelY / 2) - 1;

            if (cellRow < 0 || cellRow >= result.Height)
                continue;

            int startCol = Math.Clamp(label.PixelX - (label.Name.Length / 2), 0, result.Width - 1);
            int endCol = Math.Min(startCol + label.Name.Length, result.Width);

            bool fits = true;
            for (int c = startCol; c < endCol; c++)
            {
                if (occupied.Contains((c, cellRow)))
                {
                    fits = false;
                    break;
                }
            }

            if (!fits)
                continue;

            for (int i = 0; i < label.Name.Length && startCol + i < result.Width; i++)
            {
                int col = startCol + i;
                occupied.Add((col, cellRow));

                Color topPixel = result.Pixels[col, cellRow * 2];
                Color botPixel = (cellRow * 2) + 1 < result.Rows ? result.Pixels[col, (cellRow * 2) + 1] : topPixel;
                var avgBg = Color.FromRgb(
                    (byte)((topPixel.R + botPixel.R) / 2), (byte)((topPixel.G + botPixel.G) / 2), (byte)((topPixel.B + botPixel.B) / 2));

                overlay[col, cellRow] = (label.Name[i], label.Color, avgBg);
            }
        }

        return overlay;
    }

    private void Apply(SampleResult result, PlayerPose pose, HashSet<MobCategory> visibleCategories, List<NameLabel> nameLabels)
    {
        if (result.Width != _width || result.Height != _height)
        {
            Dispatcher.UIThread.Post(RequestSample, DispatcherPriority.Background);
            return;
        }

        // Mark the player at the center pixel.
        int centerPx = result.Width / 2;
        int centerPz = result.Rows / 2;
        result.Pixels[centerPx, centerPz] = Colors.White;

        (char Ch, Color Fg, Color Bg)?[,] overlay = BakeNameLabels(result, nameLabels);

        for (int row = 0; row < result.Height; row++)
        {
            for (int col = 0; col < result.Width; col++)
            {
                TextBlock cell = _cells[col, row];
                if (overlay[col, row] is { } chr)
                {
                    cell.Text = chr.Ch.ToString();
                    cell.Foreground = new SolidColorBrush(chr.Fg);
                    cell.Background = new SolidColorBrush(chr.Bg);
                }
                else
                {
                    Color top = result.Pixels[col, row * 2];
                    Color bottom = result.Pixels[col, (row * 2) + 1];
                    cell.Text = HalfBlock.ToString();
                    cell.Foreground = new SolidColorBrush(top);
                    cell.Background = new SolidColorBrush(bottom);
                }
            }
        }

        _lastSample = result;
        UpdateInfoRowAndLegend(pose, result.Zoom, visibleCategories);

        if (_hoverCol >= 0 && _hoverRow >= 0)
            ShowTooltip(_hoverCol, _hoverRow);
    }

    /// <summary>
    /// Builds the compass/coords/zoom/cave info row and the category legend, ported from legacy's <c>UpdateInfoBarAndLegend</c>: the legend rides on the same line as the compass when it fits within the map's width, otherwise it collapses onto its own row underneath.
    /// </summary>
    private void UpdateInfoRowAndLegend(PlayerPose pose, int zoom, HashSet<MobCategory> visibleCategories)
    {
        string arrow = GetDirectionArrow(pose.Yaw);
        string compassText = Strings.MinimapInfoCompass(
            (int)Math.Floor(pose.Position.X), (int)Math.Floor(pose.Position.Y), (int)Math.Floor(pose.Position.Z),
            arrow, zoom, CaveMode);

        List<(string Label, Color Color)> legend = [];
        foreach (MobCategory category in visibleCategories
            .Where(static c => c != MobCategory.NonLiving)
            .OrderByDescending(MinimapEntityClassifier.GetPriority)
            .Take(MaxLegendItems))
            legend.Add((MinimapEntityClassifier.GetCategoryLabel(category), MinimapEntityClassifier.GetColor(category)));

        int legendLen = 0;
        for (int i = 0; i < legend.Count; i++)
            legendLen += 1 + legend[i].Label.Length + (i > 0 ? 1 : 0);

        bool fitsOnOneLine = legend.Count > 0 && compassText.Length + 2 + legendLen <= _width;

        _infoRow.Children.Clear();
        _infoRow.Children.Add(new TextBlock { Text = compassText, Foreground = Brushes.Gray });

        if (fitsOnOneLine)
        {
            AppendLegendItems(_infoRow, legend, leftMargin: 2);
            _legendPanel.Children.Clear();
        }
        else
        {
            _legendPanel.Children.Clear();
            AppendLegendItems(_legendPanel, legend, leftMargin: 0);
        }
    }

    private static void AppendLegendItems(StackPanel panel, List<(string Label, Color Color)> legend, int leftMargin)
    {
        for (int i = 0; i < legend.Count; i++)
        {
            int margin = i == 0 ? leftMargin : 1;
            panel.Children.Add(new TextBlock
            {
                Text = "●",
                Foreground = new SolidColorBrush(legend[i].Color),
                Margin = margin > 0 ? new Thickness(margin, 0, 0, 0) : default,
            });
            panel.Children.Add(new TextBlock { Text = legend[i].Label, Foreground = Brushes.Gray });
        }
    }

    private static string GetDirectionArrow(float yaw)
    {
        double normalized = ((yaw % 360) + 360) % 360;
        int index = (int)Math.Round(normalized / 45.0) % 8;
        return index switch
        {
            0 => "↓", // S
            1 => "↙", // SW
            2 => "←", // W
            3 => "↖", // NW
            4 => "↑", // N
            5 => "↗", // NE
            6 => "→", // E
            7 => "↘", // SE
            _ => "↓",
        };
    }

    #region Hover tooltip

    private void OnGridPointerMoved(object? sender, PointerEventArgs e)
    {
        Point pos = e.GetPosition(_grid);
        int col = (int)pos.X;
        int row = (int)pos.Y;
        if (col < 0 || col >= _width || row < 0 || row >= _height)
        {
            HideTooltip();
            return;
        }

        _hoverCol = col;
        _hoverRow = row;
        ShowTooltip(col, row);
    }

    private void HideTooltip()
    {
        _hoverCol = -1;
        _hoverRow = -1;
        TooltipService?.Hide();
    }

    private void ShowTooltip(int col, int row)
    {
        TuiTooltipService? tooltip = TooltipService;
        SampleResult? result = _lastSample;
        if (tooltip is null || result is null)
            return;

        int topPixelY = row * 2;
        int botPixelY = (row * 2) + 1;
        int half = result.Zoom / 2;
        int worldX = result.OriginX + (col * result.Zoom) + half;
        int worldZTop = result.OriginZ + (topPixelY * result.Zoom) + half;
        int worldZBot = botPixelY < result.Rows ? result.OriginZ + (botPixelY * result.Zoom) + half : worldZTop;

        var lines = new List<TuiTooltipLine>();
        string coordLine = worldZTop == worldZBot
            ? $"{worldX}, {worldZTop}"
            : $"{worldX}, {worldZTop}  /  {worldX}, {worldZBot}";
        lines.Add(new TuiTooltipLine(coordLine, Brushes.White));

        string? blockTop = result.BlockIds[col, topPixelY];
        string? blockBot = botPixelY < result.Rows ? result.BlockIds[col, botPixelY] : blockTop;
        if (blockTop is not null || blockBot is not null)
        {
            string blockLine = blockTop == blockBot
                ? FormatBlockId(blockTop)
                : $"{FormatBlockId(blockTop)} / {FormatBlockId(blockBot)}";
            lines.Add(new TuiTooltipLine(blockLine, Brushes.LightGray));
        }

        AppendEntityLines(result, col, topPixelY, botPixelY, lines);

        if (lines.Count == 0)
        {
            tooltip.Hide();
            return;
        }

        Point? global = this.VisualRoot is Visual root ? _grid.TranslatePoint(new Point(col, row), root) : null;
        double mx = global?.X ?? col;
        double my = global?.Y ?? row;

        // The map is a movable window, so its current screen half is a better tooltip direction than the last configured anchor preset.
        bool preferRight = this.VisualRoot is Visual r2 && mx < r2.Bounds.Width / 2;

        tooltip.Show(mx, my, lines, preferRight);
    }

    private static void AppendEntityLines(SampleResult result, int col, int topPy, int botPy, List<TuiTooltipLine> lines)
    {
        var combined = new List<HoverEntity>();
        if (result.EntityMap[col, topPy] is { } top)
            combined.AddRange(top);

        if (botPy < result.Rows && result.EntityMap[col, botPy] is { } bot)
            combined.AddRange(bot);

        if (combined.Count == 0)
            return;

        combined.Sort((a, b) => MinimapEntityClassifier.GetPriority(b.Category).CompareTo(MinimapEntityClassifier.GetPriority(a.Category)));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int shown = 0;
        foreach (HoverEntity entity in combined)
        {
            if (shown >= MaxTooltipEntities || !seen.Add(entity.Name))
                continue;

            // No per-entity health is available from the core entity snapshot (UMPK does not surface tracked entity health beyond the local player), so the tooltip shows name and coordinates only.
            lines.Add(new TuiTooltipLine(
                $"{entity.Name} ({entity.X:F0}, {entity.Y:F0}, {entity.Z:F0})",
                new SolidColorBrush(MinimapEntityClassifier.GetColor(entity.Category))));
            shown++;
        }
    }

    private static string FormatBlockId(string? blockId)
    {
        if (string.IsNullOrEmpty(blockId))
            return "?";

        return RegistryKey.ToPascal(blockId);
    }

    #endregion

    private sealed record NameLabel(string Name, Color Color, int PixelX, int PixelY, int Priority);

    private sealed record HoverEntity(string Name, MobCategory Category, double X, double Y, double Z);

    private sealed class SampleResult
    {
        public required Color[,] Pixels;
        public required string?[,] BlockIds;
        public required List<HoverEntity>?[,] EntityMap;
        public required int OriginX;
        public required int OriginZ;
        public required int Zoom;
        public required int Rows;
        public required int Width;
        public required int Height;
    }
}
