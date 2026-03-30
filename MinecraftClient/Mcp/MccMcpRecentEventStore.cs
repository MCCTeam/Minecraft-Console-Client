using System;
using System.Collections.Generic;
using System.Linq;

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
    private static readonly object historyLock = new();
    private static readonly List<MccMcpRecentEventEntry> history = new();
    private const int MaxEntries = 500;
    private static long nextId = 1;

    public static long Add(string type, object? data = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(type);

        lock (historyLock)
        {
            long id = nextId++;
            history.Add(new MccMcpRecentEventEntry
            {
                Id = id,
                TimestampUtc = DateTimeOffset.UtcNow,
                Type = type,
                Data = data
            });

            if (history.Count > MaxEntries)
                history.RemoveRange(0, history.Count - MaxEntries);

            return id;
        }
    }

    public static long GetLatestId()
    {
        lock (historyLock)
        {
            return history.Count > 0 ? history[^1].Id : 0;
        }
    }

    public static MccMcpRecentEventEntry[] GetAfter(long afterId, int maxCount, string? typeFilter = null)
    {
        int count = Math.Clamp(maxCount, 1, MaxEntries);
        string? normalizedFilter = string.IsNullOrWhiteSpace(typeFilter) ? null : typeFilter.Trim();

        lock (historyLock)
        {
            return history
                .Where(entry => entry.Id > afterId)
                .Where(entry => normalizedFilter is null
                    || entry.Type.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase))
                .Take(count)
                .ToArray();
        }
    }

    public static void Clear()
    {
        lock (historyLock)
        {
            history.Clear();
        }
    }
}
