using System;

namespace MinecraftClient.Mcp;

public sealed class MccMcpRuntimeStateSnapshot
{
    public long? WorldAge { get; init; }
    public long? TimeOfDay { get; init; }
    public float? RainLevel { get; init; }
    public float? ThunderLevel { get; init; }
}

public static class MccMcpRuntimeStateStore
{
    private static readonly object stateLock = new();
    private static long? worldAge;
    private static long? timeOfDay;
    private static float? rainLevel;
    private static float? thunderLevel;

    public static void SetTime(long newWorldAge, long newTimeOfDay)
    {
        lock (stateLock)
        {
            worldAge = newWorldAge;
            timeOfDay = newTimeOfDay;
        }
    }

    public static void SetRainLevel(float level)
    {
        lock (stateLock)
        {
            rainLevel = level;
        }
    }

    public static void SetThunderLevel(float level)
    {
        lock (stateLock)
        {
            thunderLevel = level;
        }
    }

    public static MccMcpRuntimeStateSnapshot GetSnapshot()
    {
        lock (stateLock)
        {
            return new MccMcpRuntimeStateSnapshot
            {
                WorldAge = worldAge,
                TimeOfDay = timeOfDay,
                RainLevel = rainLevel,
                ThunderLevel = thunderLevel
            };
        }
    }

    public static void Clear()
    {
        lock (stateLock)
        {
            worldAge = null;
            timeOfDay = null;
            rainLevel = null;
            thunderLevel = null;
        }
    }
}
