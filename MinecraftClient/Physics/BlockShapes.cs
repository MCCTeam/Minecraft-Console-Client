using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockPalettes;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// Registry of block collision shapes. Maps block state IDs to collision AABBs.
    /// Data sourced from PrismarineJS/minecraft-data blockCollisionShapes.json.
    /// </summary>
    public static class BlockShapes
    {
        private static readonly Aabb FullBlock = new(0, 0, 0, 1, 1, 1);
        private static readonly Aabb[] FullBlockArray = { FullBlock };
        private static readonly Aabb[] EmptyArray = Array.Empty<Aabb>();

        private static Dictionary<int, Aabb[]>? stateToShape;
        private static Dictionary<string, object>? prismarineBlocks;
        private static Dictionary<int, Aabb[]>? prismarineShapes;

        /// <summary>
        /// Initialize the shape registry from embedded data + current palette.
        /// Call once after the block palette is set.
        /// </summary>
        public static void Initialize()
        {
            LoadPrismarineData();
            BuildStateMap();
        }

        /// <summary>
        /// Get collision shapes for a block state ID.
        /// Returns empty array for air/passable blocks, single full-block for solid cubes, etc.
        /// </summary>
        public static Aabb[] GetShapes(int blockStateId)
        {
            if (stateToShape is not null && stateToShape.TryGetValue(blockStateId, out var shapes))
                return shapes;
            return FallbackShape(blockStateId);
        }

        /// <summary>
        /// Get collision shapes for a Block at a specific position (state-aware)
        /// </summary>
        public static Aabb[] GetShapes(Block block) => GetShapes(block.BlockId);

        /// <summary>
        /// Check if a block state is effectively empty (no collision)
        /// </summary>
        public static bool IsEmpty(int blockStateId)
        {
            var shapes = GetShapes(blockStateId);
            return shapes.Length == 0;
        }

        private static Aabb[] FallbackShape(int blockStateId)
        {
            Material mat = Block.Palette.FromId(blockStateId);
            if (mat == Material.Air) return EmptyArray;
            if (mat.IsLiquid()) return EmptyArray;
            if (mat.IsSolid()) return FullBlockArray;
            return EmptyArray;
        }

        private static void LoadPrismarineData()
        {
            prismarineBlocks = new Dictionary<string, object>();
            prismarineShapes = new Dictionary<int, Aabb[]>();

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("BlockShapeData.json");
                if (stream is null)
                {
                    ConsoleIO.WriteLineFormatted("§e[Physics] BlockShapeData.json not found as embedded resource");
                    return;
                }
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                // Parse shapes: shapeId -> list of AABB boxes
                if (root.TryGetProperty("shapes", out var shapesEl))
                {
                    foreach (var prop in shapesEl.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out int shapeId))
                        {
                            var boxes = new List<Aabb>();
                            foreach (var boxEl in prop.Value.EnumerateArray())
                            {
                                var coords = new double[6];
                                int idx = 0;
                                foreach (var c in boxEl.EnumerateArray())
                                {
                                    if (idx < 6) coords[idx++] = c.GetDouble();
                                }
                                if (idx == 6)
                                    boxes.Add(new Aabb(coords[0], coords[1], coords[2], coords[3], coords[4], coords[5]));
                            }
                            prismarineShapes[shapeId] = boxes.ToArray();
                        }
                    }
                }

                // Parse blocks: blockName -> shapeId (int) or list of shapeIds
                if (root.TryGetProperty("blocks", out var blocksEl))
                {
                    foreach (var prop in blocksEl.EnumerateObject())
                    {
                        string blockName = prop.Name;
                        if (prop.Value.ValueKind == JsonValueKind.Number)
                        {
                            prismarineBlocks[blockName] = prop.Value.GetInt32();
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            var ids = new List<int>();
                            foreach (var el in prop.Value.EnumerateArray())
                                ids.Add(el.GetInt32());
                            prismarineBlocks[blockName] = ids;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleIO.WriteLineFormatted($"§e[Physics] Failed to load BlockShapeData.json: {ex.Message}");
            }
        }

        private static void BuildStateMap()
        {
            stateToShape = new Dictionary<int, Aabb[]>();

            if (prismarineBlocks is null || prismarineShapes is null)
                return;

            var palette = Block.Palette;
            var dict = GetPaletteDict(palette);
            if (dict is null) return;

            // Group consecutive state IDs by Material to find state ranges per block
            var materialRanges = new Dictionary<Material, List<(int start, int end)>>();
            int? rangeStart = null;
            Material? currentMat = null;

            foreach (var kvp in dict.OrderBy(k => k.Key))
            {
                if (currentMat == kvp.Value && rangeStart.HasValue && kvp.Key == (materialRanges[currentMat.Value].Last().end + 1))
                {
                    var ranges = materialRanges[currentMat.Value];
                    ranges[ranges.Count - 1] = (ranges.Last().start, kvp.Key);
                }
                else
                {
                    currentMat = kvp.Value;
                    if (!materialRanges.ContainsKey(currentMat.Value))
                        materialRanges[currentMat.Value] = new List<(int, int)>();
                    materialRanges[currentMat.Value].Add((kvp.Key, kvp.Key));
                }
            }

            // Map each Material to PrismarineJS block name
            foreach (var kvp in materialRanges)
            {
                string snakeName = MaterialToSnakeCase(kvp.Key);
                if (!prismarineBlocks.TryGetValue(snakeName, out var blockShapeData))
                    continue;

                int globalStateOffset = 0;
                foreach (var (start, end) in kvp.Value)
                {
                    int stateCount = end - start + 1;

                    if (blockShapeData is int singleShapeId)
                    {
                        var shapes = prismarineShapes.GetValueOrDefault(singleShapeId, EmptyArray);
                        for (int sid = start; sid <= end; sid++)
                            stateToShape[sid] = shapes;
                    }
                    else if (blockShapeData is List<int> shapeIdList)
                    {
                        for (int i = 0; i < stateCount && (globalStateOffset + i) < shapeIdList.Count; i++)
                        {
                            int shapeId = shapeIdList[globalStateOffset + i];
                            stateToShape[start + i] = prismarineShapes.GetValueOrDefault(shapeId, EmptyArray);
                        }
                    }
                    globalStateOffset += stateCount;
                }
            }

        }

        /// <summary>
        /// Convert Material enum name (PascalCase) to snake_case block name
        /// </summary>
        private static string MaterialToSnakeCase(Material mat)
        {
            string name = mat.ToString();
            var sb = new System.Text.StringBuilder(name.Length + 5);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && i > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Access the internal dictionary of a palette via reflection (all palettes store it the same way)
        /// </summary>
        private static Dictionary<int, Material>? GetPaletteDict(BlockPalette palette)
        {
            try
            {
                var method = palette.GetType().GetMethod("GetDict",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                return method?.Invoke(palette, null) as Dictionary<int, Material>;
            }
            catch
            {
                return null;
            }
        }
    }
}
