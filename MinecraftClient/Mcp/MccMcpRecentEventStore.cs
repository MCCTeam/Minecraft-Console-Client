using System;
using System.Linq;
using MinecraftClient.Scripting;

namespace MinecraftClient.Mcp;

public sealed class MccMcpRecentEventEntry
{
    public required long Id { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Type { get; init; }
    public object? Data { get; init; }
}

public static class MccMcpRecentEventStore
{
    public static long Add(string type, object? data = null)
    {
        return MccObservedStateStore.AddRecentEvent(type, data);
    }

    public static long GetLatestId()
    {
        return MccObservedStateStore.GetLatestRecentEventId();
    }

    public static MccMcpRecentEventEntry[] GetAfter(long afterId, int maxCount, string? typeFilter = null)
    {
        return MccObservedStateStore.GetRecentEventsAfter(afterId, maxCount, typeFilter)
            .Select(entry => new MccMcpRecentEventEntry
            {
                Id = entry.Id,
                TimestampUtc = entry.TimestampUtc,
                Type = entry.Type,
                Data = entry.Data
            })
            .ToArray();
    }

    public static void Clear()
    {
        MccObservedStateStore.ClearRecentEvents();
    }
}
