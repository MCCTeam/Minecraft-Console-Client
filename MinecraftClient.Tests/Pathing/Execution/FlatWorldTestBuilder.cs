using System;
using System.Collections.Generic;
using System.Threading;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Physics;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class FlatWorldTestBuilder
{
    private static readonly Lock InitLock = new();
    private static bool _defaultsLoaded;
    private static readonly Dictionary<Material, ushort> MaterialIds = new();

    public static World CreateStoneFloor(int floorY = 79, int min = -32, int max = 32)
    {
        EnsureDefaultDimensionsLoaded();
        World.SetDimension("minecraft:overworld");

        var world = new World();
        int minChunk = (int)Math.Floor(min / 16.0);
        int maxChunk = (int)Math.Floor(max / 16.0);

        for (int chunkX = minChunk; chunkX <= maxChunk; chunkX++)
        {
            for (int chunkZ = minChunk; chunkZ <= maxChunk; chunkZ++)
            {
                world[chunkX, chunkZ] = new ChunkColumn(24) { FullyLoaded = true };
            }
        }

        for (int x = min; x <= max; x++)
        {
            for (int z = min; z <= max; z++)
            {
                SetSolid(world, x, floorY, z);
            }
        }

        return world;
    }

    public static void SetSolid(World world, int x, int y, int z)
    {
        SetMaterial(world, x, y, z, Material.Stone);
    }

    public static void FillSolid(World world, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        {
            for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            {
                for (int z = Math.Min(z1, z2); z <= Math.Max(z1, z2); z++)
                {
                    SetSolid(world, x, y, z);
                }
            }
        }
    }

    public static void ClearBox(World world, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        {
            for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            {
                for (int z = Math.Min(z1, z2); z <= Math.Max(z1, z2); z++)
                {
                    world.SetBlock(new Location(x, y, z), Block.Air);
                }
            }
        }
    }

    public static void SetMaterial(World world, int x, int y, int z, Material material)
    {
        EnsureChunkColumn(world, x, z);
        world.SetBlock(new Location(x, y, z), new Block(ResolveMaterialId(material)));
    }

    public static void SetClimbable(World world, int x, int y, int z)
    {
        SetMaterial(world, x, y, z, Material.Ladder);
    }

    private static void EnsureDefaultDimensionsLoaded()
    {
        lock (InitLock)
        {
            if (_defaultsLoaded)
                return;

            Block.Palette = new Palette1219();
            World.LoadDefaultDimensions1206Plus();
            BlockShapes.Initialize();
            _defaultsLoaded = true;
        }
    }

    private static void EnsureChunkColumn(World world, int x, int z)
    {
        int chunkX = (int)Math.Floor(x / 16.0);
        int chunkZ = (int)Math.Floor(z / 16.0);
        if (world[chunkX, chunkZ] is null)
            world[chunkX, chunkZ] = new ChunkColumn(24) { FullyLoaded = true };
    }

    private static ushort ResolveMaterialId(Material material)
    {
        lock (InitLock)
        {
            if (MaterialIds.TryGetValue(material, out ushort id))
                return id;

            for (int candidate = 0; candidate <= ushort.MaxValue; candidate++)
            {
                if (Block.Palette.FromId(candidate) == material)
                {
                    ushort resolved = (ushort)candidate;
                    MaterialIds[material] = resolved;
                    return resolved;
                }
            }

            throw new InvalidOperationException($"Could not resolve a block id for material {material}");
        }
    }
}
