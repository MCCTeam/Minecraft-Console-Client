using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MinecraftClient.Scripting;

/// <summary>
/// Represents a recent high-signal runtime event observed by MCC.
/// </summary>
public sealed class MccRecentEventEntry
{
    public required long Id { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Type { get; init; }
    public object? Data { get; init; }
}

/// <summary>
/// Represents a recent chat or system line observed by MCC.
/// </summary>
public sealed class MccChatHistoryEntry
{
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Kind { get; init; }
    public required string Text { get; init; }
    public string? Sender { get; init; }
    public string? Message { get; init; }
    public string? Json { get; init; }
}

/// <summary>
/// Represents the latest observed world time and weather values.
/// </summary>
public sealed class MccRuntimeStateSnapshot
{
    public long? WorldAge { get; init; }
    public long? TimeOfDay { get; init; }
    public float? RainLevel { get; init; }
    public float? ThunderLevel { get; init; }
}

/// <summary>
/// Shared observed-state store populated from ChatBot callbacks and consumed by MCP and bots/scripts.
/// </summary>
public static class MccObservedStateStore
{
    private const int MaxEntries = 500;

    private static readonly Lock s_recentEventsLock = new();
    private static readonly Lock s_chatHistoryLock = new();
    private static readonly Lock s_runtimeStateLock = new();

    private static readonly List<MccRecentEventEntry> s_recentEvents = [];
    private static readonly List<MccChatHistoryEntry> s_chatHistory = [];

    private static long s_nextRecentEventId = 1;
    private static long? s_worldAge;
    private static long? s_timeOfDay;
    private static float? s_rainLevel;
    private static float? s_thunderLevel;

    public static long AddRecentEvent(string type, object? data = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(type);

        lock (s_recentEventsLock)
        {
            long id = s_nextRecentEventId++;
            s_recentEvents.Add(new MccRecentEventEntry
            {
                Id = id,
                TimestampUtc = DateTimeOffset.UtcNow,
                Type = type,
                Data = data
            });

            TrimToMaxEntries(s_recentEvents);
            return id;
        }
    }

    public static long GetLatestRecentEventId()
    {
        lock (s_recentEventsLock)
        {
            return s_recentEvents.Count > 0 ? s_recentEvents[^1].Id : 0;
        }
    }

    public static MccRecentEventEntry[] GetRecentEventsAfter(long afterId, int maxCount, string? typeFilter = null)
    {
        int count = Math.Clamp(maxCount, 1, MaxEntries);
        string? normalizedFilter = string.IsNullOrWhiteSpace(typeFilter) ? null : typeFilter.Trim();

        lock (s_recentEventsLock)
        {
            return s_recentEvents
                .Where(entry => entry.Id > afterId)
                .Where(entry => normalizedFilter is null
                    || entry.Type.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase))
                .Take(count)
                .ToArray();
        }
    }

    public static void AddChatHistoryEntry(MccChatHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (s_chatHistoryLock)
        {
            s_chatHistory.Add(entry);
            TrimToMaxEntries(s_chatHistory);
        }
    }

    public static MccChatHistoryEntry[] GetLatestChatHistory(int maxCount)
    {
        int count = Math.Clamp(maxCount, 1, MaxEntries);
        lock (s_chatHistoryLock)
        {
            return s_chatHistory.TakeLast(count).ToArray();
        }
    }

    public static void SetTime(long worldAge, long timeOfDay)
    {
        lock (s_runtimeStateLock)
        {
            s_worldAge = worldAge;
            s_timeOfDay = timeOfDay;
        }
    }

    public static void SetRainLevel(float level)
    {
        lock (s_runtimeStateLock)
        {
            s_rainLevel = level;
        }
    }

    public static void SetThunderLevel(float level)
    {
        lock (s_runtimeStateLock)
        {
            s_thunderLevel = level;
        }
    }

    public static MccRuntimeStateSnapshot GetRuntimeStateSnapshot()
    {
        lock (s_runtimeStateLock)
        {
            return new MccRuntimeStateSnapshot
            {
                WorldAge = s_worldAge,
                TimeOfDay = s_timeOfDay,
                RainLevel = s_rainLevel,
                ThunderLevel = s_thunderLevel
            };
        }
    }

    public static void ClearRecentEvents()
    {
        lock (s_recentEventsLock)
        {
            s_recentEvents.Clear();
        }
    }

    public static void ClearChatHistory()
    {
        lock (s_chatHistoryLock)
        {
            s_chatHistory.Clear();
        }
    }

    public static void ClearRuntimeState()
    {
        lock (s_runtimeStateLock)
        {
            s_worldAge = null;
            s_timeOfDay = null;
            s_rainLevel = null;
            s_thunderLevel = null;
        }
    }

    public static void ClearAll()
    {
        ClearChatHistory();
        ClearRuntimeState();
        ClearRecentEvents();
    }

    private static void TrimToMaxEntries<T>(List<T> entries)
    {
        if (entries.Count > MaxEntries)
            entries.RemoveRange(0, entries.Count - MaxEntries);
    }
}
