using System;
using System.Collections.Generic;
using System.Linq;

namespace MinecraftClient.Mcp;

public sealed class MccMcpChatHistoryEntry
{
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Kind { get; init; }
    public required string Text { get; init; }
    public string? Sender { get; init; }
    public string? Message { get; init; }
    public string? Json { get; init; }
}

public static class MccMcpChatHistoryStore
{
    private static readonly object historyLock = new();
    private static readonly List<MccMcpChatHistoryEntry> history = new();
    private const int MaxEntries = 500;

    public static void Add(MccMcpChatHistoryEntry entry)
    {
        lock (historyLock)
        {
            history.Add(entry);
            if (history.Count > MaxEntries)
                history.RemoveRange(0, history.Count - MaxEntries);
        }
    }

    public static MccMcpChatHistoryEntry[] GetLatest(int maxCount)
    {
        int count = Math.Clamp(maxCount, 1, MaxEntries);
        lock (historyLock)
        {
            return history.TakeLast(count).ToArray();
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
