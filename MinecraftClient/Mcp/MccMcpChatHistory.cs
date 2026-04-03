using System;
using System.Linq;
using MinecraftClient.Scripting;

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
    public static void Add(MccMcpChatHistoryEntry entry)
    {
        MccObservedStateStore.AddChatHistoryEntry(new MccChatHistoryEntry
        {
            TimestampUtc = entry.TimestampUtc,
            Kind = entry.Kind,
            Text = entry.Text,
            Sender = entry.Sender,
            Message = entry.Message,
            Json = entry.Json
        });
    }

    public static MccMcpChatHistoryEntry[] GetLatest(int maxCount)
    {
        return MccObservedStateStore.GetLatestChatHistory(maxCount)
            .Select(entry => new MccMcpChatHistoryEntry
            {
                TimestampUtc = entry.TimestampUtc,
                Kind = entry.Kind,
                Text = entry.Text,
                Sender = entry.Sender,
                Message = entry.Message,
                Json = entry.Json
            })
            .ToArray();
    }

    public static void Clear()
    {
        MccObservedStateStore.ClearChatHistory();
    }
}
