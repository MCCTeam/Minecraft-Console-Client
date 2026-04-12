using System;
using System.Threading;
using MinecraftClient.Mapping;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class FlatWorldTestBuilder
{
    private static readonly Lock InitLock = new();
    private static bool _defaultsLoaded;

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
                world.SetBlock(new Location(x, floorY, z), new Block(1));
            }
        }

        return world;
    }

    private static void EnsureDefaultDimensionsLoaded()
    {
        lock (InitLock)
        {
            if (_defaultsLoaded)
                return;

            World.LoadDefaultDimensions1206Plus();
            _defaultsLoaded = true;
        }
    }
}
