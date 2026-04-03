using System;
using MinecraftClient.Scripting;

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
    public static void SetTime(long newWorldAge, long newTimeOfDay)
    {
        MccObservedStateStore.SetTime(newWorldAge, newTimeOfDay);
    }

    public static void SetRainLevel(float level)
    {
        MccObservedStateStore.SetRainLevel(level);
    }

    public static void SetThunderLevel(float level)
    {
        MccObservedStateStore.SetThunderLevel(level);
    }

    public static MccMcpRuntimeStateSnapshot GetSnapshot()
    {
        MccRuntimeStateSnapshot snapshot = MccObservedStateStore.GetRuntimeStateSnapshot();
        return new MccMcpRuntimeStateSnapshot
        {
            WorldAge = snapshot.WorldAge,
            TimeOfDay = snapshot.TimeOfDay,
            RainLevel = snapshot.RainLevel,
            ThunderLevel = snapshot.ThunderLevel
        };
    }

    public static void Clear()
    {
        MccObservedStateStore.ClearRuntimeState();
    }
}
