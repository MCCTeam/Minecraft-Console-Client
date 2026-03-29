using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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

            var root = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children = { _mapGrid, _infoRow, _legendPanel },
            };

            Content = root;

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

        private sealed class SampleResult
        {
            public Color[,] Pixels = null!;
            public (char Ch, Color Fg, Color Bg)?[,] CharOverlay = null!;
            public HashSet<MobCategory> VisibleCategories = [];
            public int[,] Heights = null!;
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
            };
            var world = client.GetWorld();
            var playerLoc = client.GetCurrentLocation();

            int playerBlockX = (int)Math.Floor(playerLoc.X);
            int playerBlockZ = (int)Math.Floor(playerLoc.Z);
            int playerBlockY = (int)Math.Floor(playerLoc.Y);

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
                        var (color, surfY) = SampleColumn(world, baseX, baseZ, scanTop, minY,
                            ref cachedColumn, ref cachedChunkX, ref cachedChunkZ);
                        result.Pixels[px, py] = color;
                        result.Heights[px, py] = surfY;
                    }
                    else
                    {
                        var (color, surfY) = SampleAreaDominant(world, baseX, baseZ, bpp, scanTop, minY,
                            ref cachedColumn, ref cachedChunkX, ref cachedChunkZ);
                        result.Pixels[px, py] = color;
                        result.Heights[px, py] = surfY;
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

        private static (Color color, int surfaceY) SampleColumn(World world, int x, int z,
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
                return (MinimapColorMap.VoidColor, minY);

            int waterDepth = 0;
            bool inIce = false;
            int surfaceY = minY;

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
                    if (waterDepth == 0) surfaceY = y;
                    waterDepth++;
                    continue;
                }

                if (MinimapColorMap.IsIce(mat) && !inIce)
                {
                    if (waterDepth == 0) surfaceY = y;
                    inIce = true;
                    continue;
                }

                if (waterDepth == 0 && !inIce) surfaceY = y;

                var baseColor = MinimapColorMap.GetBaseColor(mat);

                if (waterDepth > 0)
                    baseColor = MinimapColorMap.BlendWaterColor(baseColor, waterDepth);
                if (inIce)
                    baseColor = MinimapColorMap.BlendIceColor(baseColor);

                return (baseColor, surfaceY);
            }

            if (waterDepth > 0)
                return (MinimapColorMap.WaterColor, surfaceY);

            return (MinimapColorMap.VoidColor, minY);
        }

        private static (Color color, int surfaceY) SampleAreaDominant(World world, int baseX, int baseZ,
            int size, int scanTop, int minY,
            ref ChunkColumn? cachedColumn, ref int cachedChunkX, ref int cachedChunkZ)
        {
            var colorCounts = new Dictionary<Color, (int Count, int SumY)>();

            int step = Math.Max(1, size / 3);
            for (int dx = 0; dx < size; dx += step)
            {
                for (int dz = 0; dz < size; dz += step)
                {
                    var (c, surfY) = SampleColumn(world, baseX + dx, baseZ + dz, scanTop, minY,
                        ref cachedColumn, ref cachedChunkX, ref cachedChunkZ);

                    if (colorCounts.TryGetValue(c, out var existing))
                        colorCounts[c] = (existing.Count + 1, existing.SumY + surfY);
                    else
                        colorCounts[c] = (1, surfY);
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
            return (best, avgY);
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
