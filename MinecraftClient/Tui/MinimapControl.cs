using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MinecraftClient.Mapping;

namespace MinecraftClient.Tui
{
    /// <summary>
    /// TUI minimap control rendered as a grid of TextBlocks using half-block characters.
    /// Zoom is expressed as blocks-per-pixel (1 = 1:1, 16 = 16 blocks per pixel).
    /// Entity names are drawn directly on the map below their icon.
    /// </summary>
    public class MinimapControl : UserControl
    {
        public const int MinZoom = 1;
        public const int MaxZoom = 16;
        public const int DefaultZoom = 2;
        public const int DefaultWidth = 40;
        public const int DefaultHeight = 40;
        public const int DefaultRefreshMs = 1000;
        public const int MinRefreshMs = 100;
        public const int MaxRefreshMs = 5000;

        private int _mapWidth;
        private int _mapHeight;
        private int _cellRows;

        private int _blocksPerPixel = DefaultZoom;
        private volatile bool _sampling;
        private CancellationTokenSource? _cts;

        private readonly NameDisplayConfig _nameConfig = new();

        private TextBlock[,] _cells;
        private readonly StackPanel _infoRow;
        private readonly StackPanel _legendPanel;
        private readonly Grid _mapGrid;
        private readonly DispatcherTimer _timer;

        private readonly Canvas _tooltipCanvas;
        private readonly Border _tooltipBorder;
        private readonly StackPanel _tooltipContent;
        private SampleResult? _lastResult;
        private int _hoverCol = -1;
        private int _hoverRow = -1;

        public int BlocksPerPixel
        {
            get => _blocksPerPixel;
            set => _blocksPerPixel = Math.Clamp(value, MinZoom, MaxZoom);
        }

        public NameDisplayConfig NameConfig => _nameConfig;

        public int MapPixelWidth => _mapWidth;
        public int MapPixelHeight => _mapHeight;

        public int RefreshIntervalMs
        {
            get => (int)_timer.Interval.TotalMilliseconds;
            set => _timer.Interval = TimeSpan.FromMilliseconds(Math.Clamp(value, MinRefreshMs, MaxRefreshMs));
        }

        public MinimapControl() : this(DefaultWidth, DefaultHeight) { }

        public MinimapControl(int width, int height)
        {
            _mapWidth = Math.Max(10, width);
            _mapHeight = Math.Max(4, height % 2 == 0 ? height : height + 1);
            _cellRows = _mapHeight / 2;

            _mapGrid = new Grid();
            _cells = BuildGrid(_mapGrid, _cellRows, _mapWidth);

            _infoRow = new StackPanel { Orientation = Orientation.Horizontal };
            _legendPanel = new StackPanel { Orientation = Orientation.Horizontal };

            _tooltipContent = new StackPanel { Orientation = Orientation.Vertical };
            _tooltipBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1),
                Child = _tooltipContent,
                IsVisible = false,
            };

            _tooltipCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Children = { _tooltipBorder },
            };

            var mapLayer = new Panel
            {
                ClipToBounds = true,
                Children = { _mapGrid, _tooltipCanvas },
            };

            var root = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children = { mapLayer, _infoRow, _legendPanel },
            };

            Content = root;

            _mapGrid.PointerMoved += OnMapPointerMoved;
            _mapGrid.PointerExited += OnMapPointerExited;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DefaultRefreshMs),
            };
            _timer.Tick += (_, _) => RequestSample();
        }

        public void Resize(int width, int height)
        {
            _mapWidth = Math.Max(10, width);
            _mapHeight = Math.Max(4, height % 2 == 0 ? height : height + 1);
            _cellRows = _mapHeight / 2;

            _mapGrid.Children.Clear();
            _mapGrid.RowDefinitions.Clear();
            _mapGrid.ColumnDefinitions.Clear();
            _cells = BuildGrid(_mapGrid, _cellRows, _mapWidth);
        }

        private static TextBlock[,] BuildGrid(Grid grid, int rows, int cols)
        {
            var cells = new TextBlock[rows, cols];
            for (int r = 0; r < rows; r++)
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int c = 0; c < cols; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var tb = new TextBlock
                    {
                        Text = "\u2580",
                        Foreground = Brushes.Black,
                        Background = Brushes.Black,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                        FontSize = 1,
                    };
                    Grid.SetRow(tb, r);
                    Grid.SetColumn(tb, c);
                    grid.Children.Add(tb);
                    cells[r, c] = tb;
                }
            }
            return cells;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _timer.Start();
            RequestSample();
        }

        public void Stop()
        {
            _timer.Stop();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void RequestSample()
        {
            if (_sampling) return;
            if (McClient.Instance is not McClient client) return;
            if (!client.GetTerrainEnabled()) return;

            _sampling = true;
            var ct = _cts?.Token ?? CancellationToken.None;
            int bpp = _blocksPerPixel;
            int w = _mapWidth;
            int h = _mapHeight;

            bool showPlayers = _nameConfig.Players;
            bool showHostile = _nameConfig.Hostile;
            bool showNeutral = _nameConfig.Neutral;
            bool showPassive = _nameConfig.Passive;

            Task.Run(() =>
            {
                try
                {
                    var result = SampleTerrain(client, bpp, w, h,
                        showPlayers, showHostile, showNeutral, showPassive, ct);
                    if (ct.IsCancellationRequested) return;

                    Dispatcher.UIThread.Post(() =>
                    {
                        ApplyPixelBuffer(result, w, h);
                        UpdateInfoBarAndLegend(client, bpp, result.VisibleCategories, w);
                    });
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    ConsoleIO.WriteLineFormatted($"\u00a7e[Minimap] Sample error: {ex.Message}");
                }
                finally
                {
                    _sampling = false;
                }
            }, ct);
        }

        internal sealed class EntityLabel
        {
            public string Name = "";
            public Color LabelColor;
            public int PixelX;
            public int PixelY;
        }

        internal sealed class PixelEntityInfo
        {
            public string Name = "";
            public MobCategory Category;
            public float Health;
            public float MaxHealth;
            public int Priority;
        }

        private sealed class SampleResult
        {
            public Color[,] Pixels = null!;
            public (char Ch, Color Fg, Color Bg)?[,] CharOverlay = null!;
            public HashSet<MobCategory> VisibleCategories = [];
            public int[,] Heights = null!;
            public Material[,]? BlockTypes;
            public List<(Material Mat, int Count)>?[,]? BlockSummary;
            public List<PixelEntityInfo>?[,]? EntityMap;
            public int PlayerBlockX;
            public int PlayerBlockZ;
            public int CenterX;
            public int CenterY;
            public int Bpp;
        }

        private static bool ShouldShowNameLocal(MobCategory cat,
            bool showPlayers, bool showHostile, bool showNeutral, bool showPassive)
        {
            return cat switch
            {
                MobCategory.Player => showPlayers,
                MobCategory.Hostile => showHostile,
                MobCategory.Neutral => showNeutral,
                MobCategory.Passive => showPassive,
                _ => false,
            };
        }

        private static SampleResult SampleTerrain(McClient client, int bpp, int mapW, int mapH,
            bool showPlayers, bool showHostile, bool showNeutral, bool showPassive,
            CancellationToken ct)
        {
            var result = new SampleResult
            {
                Pixels = new Color[mapW, mapH],
                CharOverlay = new (char, Color, Color)?[mapW, mapH / 2],
                Heights = new int[mapW, mapH],
                EntityMap = new List<PixelEntityInfo>?[mapW, mapH],
                BlockTypes = bpp == 1 ? new Material[mapW, mapH] : null,
                BlockSummary = bpp > 1 ? new List<(Material, int)>?[mapW, mapH] : null,
                Bpp = bpp,
            };
            var world = client.GetWorld();
            var playerLoc = client.GetCurrentLocation();

            int playerBlockX = (int)Math.Floor(playerLoc.X);
            int playerBlockZ = (int)Math.Floor(playerLoc.Z);
            int playerBlockY = (int)Math.Floor(playerLoc.Y);

            result.PlayerBlockX = playerBlockX;
            result.PlayerBlockZ = playerBlockZ;
            result.CenterX = mapW / 2;
            result.CenterY = mapH / 2;

            var dim = World.GetDimension();
            int minY = dim.minY;
            int scanTop = Math.Min(playerBlockY + 32, dim.maxY - 1);

            var entities = client.GetEntityHandlingEnabled()
                ? client.GetEntities()
                : null;

            var entityPixels = new Dictionary<(int, int), (Color Color, int Priority)>();
            int centerX = mapW / 2;
            int centerY = mapH / 2;

            var nameLabels = new List<EntityLabel>();
            var uuidNameMap = client.GetOnlinePlayersWithUUID();

            if (entities is not null)
            {
                int playerEntityId = client.GetPlayerEntityID();
                foreach (var kvp in entities)
                {
                    if (ct.IsCancellationRequested) return result;
                    var entity = kvp.Value;
                    var cat = MinimapEntityClassifier.Classify(entity.Type);
                    if (cat == MobCategory.NonLiving) continue;
                    if (kvp.Key == playerEntityId) continue;

                    if (!MinimapEntityClassifier.ShouldDisplay(cat, playerLoc.Y, entity.Location.Y))
                        continue;

                    double relX = (entity.Location.X - playerLoc.X) / bpp;
                    double relZ = (entity.Location.Z - playerLoc.Z) / bpp;
                    int px = (int)Math.Floor(relX) + centerX;
                    int py = (int)Math.Floor(relZ) + centerY;

                    if (px < 0 || px >= mapW || py < 0 || py >= mapH) continue;

                    var baseColor = MinimapEntityClassifier.GetBaseColor(cat);
                    Color color;
                    if (cat == MobCategory.Player)
                        color = baseColor;
                    else
                        color = MinimapEntityClassifier.ApplyDepthFade(baseColor, playerLoc.Y, entity.Location.Y);
                    int priority = MinimapEntityClassifier.GetPriority(cat);

                    var key = (px, py);
                    if (!entityPixels.TryGetValue(key, out var existing) || priority > existing.Priority)
                        entityPixels[key] = (color, priority);

                    result.VisibleCategories.Add(cat);

                    string eName = ResolveEntityName(client, entity, cat, uuidNameMap);
                    var pixelList = result.EntityMap![px, py] ??= [];
                    pixelList.Add(new PixelEntityInfo
                    {
                        Name = eName,
                        Category = cat,
                        Health = entity.Health,
                        MaxHealth = -1,
                        Priority = priority,
                    });

                    if (ShouldShowNameLocal(cat, showPlayers, showHostile, showNeutral, showPassive))
                    {
                        string name = ResolveEntityName(client, entity, cat, uuidNameMap);
                        nameLabels.Add(new EntityLabel
                        {
                            Name = name,
                            LabelColor = color,
                            PixelX = px,
                            PixelY = py,
                        });
                    }
                }
            }

            entityPixels[(centerX, centerY)] = (MinimapEntityClassifier.PlayerColor, 5);
            result.VisibleCategories.Add(MobCategory.Player);

            var selfList = result.EntityMap![centerX, centerY] ??= [];
            selfList.Add(new PixelEntityInfo
            {
                Name = client.GetUsername(),
                Category = MobCategory.Player,
                Health = client.GetHealth(),
                MaxHealth = 20f,
                Priority = 5,
            });

            ChunkColumn? cachedColumn = null;
            int cachedChunkX = int.MinValue, cachedChunkZ = int.MinValue;

            for (int px = 0; px < mapW; px++)
            {
                for (int py = 0; py < mapH; py++)
                {
                    if (ct.IsCancellationRequested) return result;

                    int baseX = playerBlockX + (px - centerX) * bpp;
                    int baseZ = playerBlockZ + (py - centerY) * bpp;

                    if (bpp == 1)
                    {
                        var (color, surfY, surfMat) = SampleColumn(world, baseX, baseZ, scanTop, minY,
                            ref cachedColumn, ref cachedChunkX, ref cachedChunkZ);
                        result.Pixels[px, py] = color;
                        result.Heights[px, py] = surfY;
                        result.BlockTypes![px, py] = surfMat;
                    }
                    else
                    {
                        var (color, surfY, matSum) = SampleAreaDominant(world, baseX, baseZ, bpp,
                            scanTop, minY, result.BlockSummary is not null,
                            ref cachedColumn, ref cachedChunkX, ref cachedChunkZ);
                        result.Pixels[px, py] = color;
                        result.Heights[px, py] = surfY;
                        if (result.BlockSummary is not null)
                            result.BlockSummary[px, py] = matSum;
                    }
                }
            }

            for (int px = 0; px < mapW; px++)
            {
                for (int py = 0; py < mapH; py++)
                {
                    if (entityPixels.ContainsKey((px, py))) continue;

                    int northHeight = py > 0 ? result.Heights[px, py - 1] : result.Heights[px, py];
                    int delta = result.Heights[px, py] - northHeight;
                    result.Pixels[px, py] = MinimapColorMap.ApplyHeightShade(result.Pixels[px, py], delta);
                }
            }

            foreach (var (key, info) in entityPixels)
            {
                var (px, py) = key;
                if (px >= 0 && px < mapW && py >= 0 && py < mapH)
                    result.Pixels[px, py] = info.Color;
            }

            BakeNameLabels(result, nameLabels, mapW, mapH);

            return result;
        }

        private static string ResolveEntityName(McClient client, Entity entity,
            MobCategory cat, Dictionary<string, string>? uuidNameMap)
        {
            if (cat == MobCategory.Player)
            {
                if (!string.IsNullOrWhiteSpace(entity.Name))
                    return entity.Name;

                if (entity.UUID != System.Guid.Empty)
                {
                    var playerInfo = client.GetPlayerInfo(entity.UUID);
                    if (!string.IsNullOrWhiteSpace(playerInfo?.Name))
                        return playerInfo.Name;

                    if (uuidNameMap is not null &&
                        uuidNameMap.TryGetValue(entity.UUID.ToString(), out string? mapped) &&
                        !string.IsNullOrWhiteSpace(mapped))
                        return mapped;
                }

                return "Player";
            }

            if (!string.IsNullOrWhiteSpace(entity.Name))
                return entity.Name;

            return entity.Type.ToString();
        }

        private static void BakeNameLabels(SampleResult result, List<EntityLabel> labels,
            int mapW, int mapH)
        {
            if (labels.Count == 0) return;
            int cellRows = mapH / 2;

            var occupied = new HashSet<(int col, int row)>();

            labels.Sort((a, b) =>
            {
                int pa = MinimapEntityClassifier.GetPriority(
                    a.LabelColor == MinimapEntityClassifier.PlayerColor ? MobCategory.Player :
                    a.LabelColor == MinimapEntityClassifier.HostileColor ? MobCategory.Hostile :
                    a.LabelColor == MinimapEntityClassifier.NeutralColor ? MobCategory.Neutral : MobCategory.Passive);
                int pb = MinimapEntityClassifier.GetPriority(
                    b.LabelColor == MinimapEntityClassifier.PlayerColor ? MobCategory.Player :
                    b.LabelColor == MinimapEntityClassifier.HostileColor ? MobCategory.Hostile :
                    b.LabelColor == MinimapEntityClassifier.NeutralColor ? MobCategory.Neutral : MobCategory.Passive);
                return pb.CompareTo(pa);
            });

            foreach (var lbl in labels)
            {
                int cellRow = (lbl.PixelY / 2) + 1;
                if (cellRow >= cellRows) cellRow = lbl.PixelY / 2 - 1;
                if (cellRow < 0 || cellRow >= cellRows) continue;

                int startCol = lbl.PixelX - lbl.Name.Length / 2;
                startCol = Math.Clamp(startCol, 0, mapW - 1);

                bool fits = true;
                int endCol = Math.Min(startCol + lbl.Name.Length, mapW);
                for (int c = startCol; c < endCol; c++)
                {
                    if (occupied.Contains((c, cellRow)))
                    {
                        fits = false;
                        break;
                    }
                }
                if (!fits) continue;

                for (int i = 0; i < lbl.Name.Length && startCol + i < mapW; i++)
                {
                    int col = startCol + i;
                    occupied.Add((col, cellRow));

                    var bgTop = result.Pixels[col, cellRow * 2];
                    var bgBot = (cellRow * 2 + 1 < mapH)
                        ? result.Pixels[col, cellRow * 2 + 1]
                        : bgTop;

                    var avgBg = Color.FromRgb(
                        (byte)((bgTop.R + bgBot.R) / 2),
                        (byte)((bgTop.G + bgBot.G) / 2),
                        (byte)((bgTop.B + bgBot.B) / 2));

                    result.CharOverlay[col, cellRow] = (lbl.Name[i], lbl.LabelColor, avgBg);
                }
            }
        }

        private static (Color color, int surfaceY, Material surfaceMat) SampleColumn(World world, int x, int z,
            int scanTop, int minY,
            ref ChunkColumn? cachedColumn, ref int cachedChunkX, ref int cachedChunkZ)
        {
            int chunkX = x >> 4;
            int chunkZ = z >> 4;
            if (chunkX != cachedChunkX || chunkZ != cachedChunkZ)
            {
                cachedColumn = world[chunkX, chunkZ];
                cachedChunkX = chunkX;
                cachedChunkZ = chunkZ;
            }

            if (cachedColumn is null)
                return (MinimapColorMap.VoidColor, minY, Material.Air);

            int waterDepth = 0;
            bool inIce = false;
            int surfaceY = minY;
            Material topMat = Material.Air;

            for (int y = scanTop; y >= minY; y--)
            {
                var loc = new Mapping.Location(x, y, z);
                var chunk = cachedColumn.GetChunk(loc);
                if (chunk is null) continue;

                var block = chunk.GetBlock(loc);
                var mat = block.Type;

                if (MinimapColorMap.IsFullyTransparent(mat))
                    continue;

                if (MinimapColorMap.IsWater(mat))
                {
                    if (waterDepth == 0) { surfaceY = y; topMat = mat; }
                    waterDepth++;
                    continue;
                }

                if (MinimapColorMap.IsIce(mat) && !inIce)
                {
                    if (waterDepth == 0) { surfaceY = y; topMat = mat; }
                    inIce = true;
                    continue;
                }

                if (waterDepth == 0 && !inIce) { surfaceY = y; topMat = mat; }

                var baseColor = MinimapColorMap.GetBaseColor(mat);

                if (waterDepth > 0)
                    baseColor = MinimapColorMap.BlendWaterColor(baseColor, waterDepth);
                if (inIce)
                    baseColor = MinimapColorMap.BlendIceColor(baseColor);

                return (baseColor, surfaceY, topMat);
            }

            if (waterDepth > 0)
                return (MinimapColorMap.WaterColor, surfaceY, topMat);

            return (MinimapColorMap.VoidColor, minY, Material.Air);
        }

        private static (Color color, int surfaceY, List<(Material Mat, int Count)>? matSummary)
            SampleAreaDominant(World world, int baseX, int baseZ,
            int size, int scanTop, int minY, bool collectMats,
            ref ChunkColumn? cachedColumn, ref int cachedChunkX, ref int cachedChunkZ)
        {
            var colorCounts = new Dictionary<Color, (int Count, int SumY)>();
            Dictionary<Material, int>? matCounts = collectMats ? [] : null;

            int step = Math.Max(1, size / 3);
            for (int dx = 0; dx < size; dx += step)
            {
                for (int dz = 0; dz < size; dz += step)
                {
                    var (c, surfY, surfMat) = SampleColumn(world, baseX + dx, baseZ + dz, scanTop, minY,
                        ref cachedColumn, ref cachedChunkX, ref cachedChunkZ);

                    if (colorCounts.TryGetValue(c, out var existing))
                        colorCounts[c] = (existing.Count + 1, existing.SumY + surfY);
                    else
                        colorCounts[c] = (1, surfY);

                    if (matCounts is not null)
                    {
                        if (matCounts.TryGetValue(surfMat, out int mc))
                            matCounts[surfMat] = mc + 1;
                        else
                            matCounts[surfMat] = 1;
                    }
                }
            }

            Color best = MinimapColorMap.VoidColor;
            int bestCount = 0;
            int avgY = minY;
            foreach (var kvp in colorCounts)
            {
                if (kvp.Value.Count > bestCount)
                {
                    bestCount = kvp.Value.Count;
                    best = kvp.Key;
                    avgY = kvp.Value.SumY / kvp.Value.Count;
                }
            }

            List<(Material, int)>? summary = null;
            if (matCounts is not null && matCounts.Count > 0)
            {
                summary = matCounts
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv => (kv.Key, kv.Value))
                    .ToList();
            }

            return (best, avgY, summary);
        }

        private void ApplyPixelBuffer(SampleResult result, int w, int h)
        {
            int rows = h / 2;
            for (int row = 0; row < rows && row < _cellRows; row++)
            {
                for (int col = 0; col < w && col < _mapWidth; col++)
                {
                    var overlay = result.CharOverlay[col, row];
                    if (overlay is not null)
                    {
                        var (ch, fg, bg) = overlay.Value;
                        _cells[row, col].Text = ch.ToString();
                        _cells[row, col].Foreground = new SolidColorBrush(fg);
                        _cells[row, col].Background = new SolidColorBrush(bg);
                    }
                    else
                    {
                        var topColor = result.Pixels[col, row * 2];
                        var bottomColor = result.Pixels[col, row * 2 + 1];

                        _cells[row, col].Text = "\u2580";
                        _cells[row, col].Foreground = new SolidColorBrush(topColor);
                        _cells[row, col].Background = new SolidColorBrush(bottomColor);
                    }
                }
            }

            _lastResult = result;

            if (_hoverCol >= 0 && _hoverRow >= 0)
                UpdateTooltip(_hoverCol, _hoverRow);
        }

        private void OnMapPointerMoved(object? sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(_mapGrid);
            int col = (int)pos.X;
            int row = (int)pos.Y;

            if (col < 0 || col >= _mapWidth || row < 0 || row >= _cellRows)
            {
                HideTooltip();
                return;
            }

            _hoverCol = col;
            _hoverRow = row;
            UpdateTooltip(col, row);
        }

        private void OnMapPointerExited(object? sender, PointerEventArgs e)
        {
            HideTooltip();
        }

        private void HideTooltip()
        {
            _hoverCol = -1;
            _hoverRow = -1;
            _tooltipBorder.IsVisible = false;
        }

        private void UpdateTooltip(int col, int row)
        {
            var result = _lastResult;
            if (result is null) { _tooltipBorder.IsVisible = false; return; }

            int bpp = result.Bpp;
            int centerX = result.CenterX;
            int centerY = result.CenterY;

            int topPixelY = row * 2;
            int botPixelY = row * 2 + 1;

            int baseX = result.PlayerBlockX + (col - centerX) * bpp;
            int baseZ_top = result.PlayerBlockZ + (topPixelY - centerY) * bpp;
            int baseZ_bot = result.PlayerBlockZ + (botPixelY - centerY) * bpp;

            _tooltipContent.Children.Clear();

            if (bpp == 1)
            {
                int surfY_top = (topPixelY < result.Heights.GetLength(1)) ? result.Heights[col, topPixelY] : 0;
                int surfY_bot = (botPixelY < result.Heights.GetLength(1)) ? result.Heights[col, botPixelY] : 0;

                string coordLine = baseZ_top == baseZ_bot
                    ? $"{baseX}, {surfY_top}, {baseZ_top}"
                    : $"{baseX}, {surfY_top}, {baseZ_top}  /  {baseX}, {surfY_bot}, {baseZ_bot}";
                _tooltipContent.Children.Add(MakeTooltipText(coordLine, Brushes.White));

                if (result.BlockTypes is not null)
                {
                    var mat_top = result.BlockTypes[col, topPixelY];
                    var mat_bot = (botPixelY < result.BlockTypes.GetLength(1))
                        ? result.BlockTypes[col, botPixelY] : mat_top;
                    string blockLine = mat_top == mat_bot
                        ? FormatMaterialName(mat_top)
                        : $"{FormatMaterialName(mat_top)} / {FormatMaterialName(mat_bot)}";
                    _tooltipContent.Children.Add(MakeTooltipText(blockLine, Brushes.LightGray));
                }
            }
            else
            {
                int endX = baseX + bpp - 1;
                int endZ_bot = baseZ_bot + bpp - 1;
                string coordLine = $"X {baseX}~{endX}  Z {baseZ_top}~{endZ_bot}";
                _tooltipContent.Children.Add(MakeTooltipText(coordLine, Brushes.White));

                AppendBlockSummary(result, col, topPixelY, botPixelY);
            }

            AppendEntityInfo(result, col, topPixelY, botPixelY);

            if (_tooltipContent.Children.Count == 0)
            {
                _tooltipBorder.IsVisible = false;
                return;
            }

            int maxTipW = Math.Max(10, _mapWidth / 2 - 2);
            _tooltipBorder.MaxWidth = maxTipW;
            _tooltipBorder.MaxHeight = _cellRows;

            bool showRight = col < _mapWidth / 2;
            int tipX = showRight ? col + 2 : Math.Max(0, col - maxTipW - 1);
            int tipY = Math.Clamp(row, 0, _cellRows - 1);

            Canvas.SetLeft(_tooltipBorder, tipX);
            Canvas.SetTop(_tooltipBorder, tipY);
            _tooltipBorder.IsVisible = true;
        }

        private void AppendBlockSummary(SampleResult result, int col, int topPy, int botPy)
        {
            if (result.BlockSummary is null) return;

            var merged = new Dictionary<Material, int>();
            MergeBlockCounts(result.BlockSummary, col, topPy, merged);
            if (botPy < result.BlockSummary.GetLength(1))
                MergeBlockCounts(result.BlockSummary, col, botPy, merged);

            if (merged.Count == 0) return;

            var sorted = merged.OrderByDescending(kv => kv.Value).Take(4);
            int totalSamples = 0;
            foreach (var kv in merged) totalSamples += kv.Value;

            var parts = new List<string>();
            foreach (var kv in sorted)
            {
                if (kv.Key == Material.Air && merged.Count > 1) continue;
                parts.Add(kv.Value > 1
                    ? $"{FormatMaterialName(kv.Key)} x{kv.Value}"
                    : FormatMaterialName(kv.Key));
            }

            if (parts.Count == 0) return;

            string line = string.Join(", ", parts);
            _tooltipContent.Children.Add(MakeTooltipText(line, Brushes.LightGray));
        }

        private static void MergeBlockCounts(List<(Material Mat, int Count)>?[,] summary,
            int px, int py, Dictionary<Material, int> target)
        {
            var list = summary[px, py];
            if (list is null) return;
            foreach (var (mat, count) in list)
            {
                if (target.TryGetValue(mat, out int c))
                    target[mat] = c + count;
                else
                    target[mat] = count;
            }
        }

        private void AppendEntityInfo(SampleResult result, int col, int topPy, int botPy)
        {
            var entityMap = result.EntityMap;
            if (entityMap is null) return;

            var combined = new List<PixelEntityInfo>();
            AddEntitiesFromPixel(entityMap, col, topPy, combined);
            if (botPy < entityMap.GetLength(1))
                AddEntitiesFromPixel(entityMap, col, botPy, combined);

            if (combined.Count == 0) return;

            combined.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            int shown = 0;
            var seen = new HashSet<string>();
            foreach (var ent in combined)
            {
                if (shown >= 4) break;
                string key = $"{ent.Name}_{ent.Health:F0}";
                if (!seen.Add(key)) continue;

                var catColor = MinimapEntityClassifier.GetBaseColor(ent.Category);
                string hpStr;
                if (ent.Health > 0)
                {
                    hpStr = ent.MaxHealth > 0
                        ? $"  HP:{ent.Health:F0}/{ent.MaxHealth:F0}"
                        : $"  HP:{ent.Health:F0}";
                }
                else
                    hpStr = "";

                _tooltipContent.Children.Add(MakeTooltipText(
                    $"{ent.Name}{hpStr}",
                    new SolidColorBrush(catColor)));
                shown++;
            }
        }

        private static void AddEntitiesFromPixel(List<PixelEntityInfo>?[,] map,
            int px, int py, List<PixelEntityInfo> target)
        {
            if (px >= 0 && px < map.GetLength(0) && py >= 0 && py < map.GetLength(1))
            {
                var list = map[px, py];
                if (list is not null)
                    target.AddRange(list);
            }
        }

        private static TextBlock MakeTooltipText(string text, IBrush foreground)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = foreground,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                FontSize = 1,
            };
        }

        private static string FormatMaterialName(Material mat)
        {
            if (mat == Material.Air) return "Air";
            string raw = mat.ToString();
            return raw.Replace('_', ' ');
        }

        private void UpdateInfoBarAndLegend(McClient client, int bpp,
            HashSet<MobCategory> categories, int mapW)
        {
            var loc = client.GetCurrentLocation();
            float yaw = client.GetYaw();
            string arrow = GetDirectionArrow(yaw);

            int x = (int)Math.Floor(loc.X);
            int y = (int)Math.Floor(loc.Y);
            int z = (int)Math.Floor(loc.Z);

            string coordPart = $"{x}, {y}, {z}  {arrow}  {bpp}:1";

            var legendParts = new List<string>();
            var legendColors = new List<Color>();

            var sorted = categories
                .Where(c => c != MobCategory.NonLiving)
                .OrderByDescending(MinimapEntityClassifier.GetPriority);

            int catCount = 0;
            foreach (var cat in sorted)
            {
                if (catCount >= 4) break;
                legendParts.Add(MinimapEntityClassifier.GetCategoryLabel(cat));
                legendColors.Add(MinimapEntityClassifier.GetBaseColor(cat));
                catCount++;
            }

            int legendLen = 0;
            for (int i = 0; i < legendParts.Count; i++)
                legendLen += 1 + legendParts[i].Length + (i > 0 ? 1 : 0);

            bool fitsOnOneLine = legendParts.Count > 0
                && coordPart.Length + 2 + legendLen <= mapW;

            _infoRow.Children.Clear();
            _infoRow.Children.Add(new TextBlock
            {
                Text = coordPart,
                Foreground = Brushes.Gray,
                Padding = new Thickness(0),
            });

            if (fitsOnOneLine)
            {
                AppendLegendItems(_infoRow, legendParts, legendColors, leftMargin: 2);
                _legendPanel.Children.Clear();
                _legendPanel.IsVisible = false;
            }
            else
            {
                _legendPanel.IsVisible = legendParts.Count > 0;
                _legendPanel.Children.Clear();
                AppendLegendItems(_legendPanel, legendParts, legendColors, leftMargin: 0);
            }
        }

        private static void AppendLegendItems(StackPanel panel,
            List<string> parts, List<Color> colors, int leftMargin)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                int ml = i == 0 ? leftMargin : 1;
                panel.Children.Add(new TextBlock
                {
                    Text = "\u25cf",
                    Foreground = new SolidColorBrush(colors[i]),
                    Padding = new Thickness(0),
                    Margin = ml > 0 ? new Thickness(ml, 0, 0, 0) : new Thickness(0),
                });
                panel.Children.Add(new TextBlock
                {
                    Text = parts[i],
                    Foreground = Brushes.Gray,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                });
            }
        }

        private static string GetDirectionArrow(float yaw)
        {
            double normalized = ((yaw % 360) + 360) % 360;
            int index = (int)Math.Round(normalized / 45.0) % 8;
            return index switch
            {
                0 => "\u2193", // S
                1 => "\u2199", // SW
                2 => "\u2190", // W
                3 => "\u2196", // NW
                4 => "\u2191", // N
                5 => "\u2197", // NE
                6 => "\u2192", // E
                7 => "\u2198", // SE
                _ => "\u2193",
            };
        }
    }
}
