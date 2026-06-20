using System.Text.Json.Serialization;

namespace DebugTools.MccMcpWebPlayground.Contracts;

public sealed class ChatStreamRequest
{
    public List<ChatMessage>? Messages { get; set; }
}

public sealed class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed record MccConfigResponse(
    string? Model,
    string OpenRouterBaseUrl,
    string McpEndpoint,
    bool HasApiKey,
    bool ExposeInventoryWindowAction,
    bool ExposeInternalCommandTool);

public sealed record MccStreamEnvelope(string RunId, long Sequence, string Kind, object Data);

public sealed record MccRunStartedData(string Model, string McpEndpoint, DateTimeOffset StartedAtUtc);

public sealed record MccGuidanceLoadedData(
    string SourceTool,
    string CanonicalPromptName,
    string GuidanceVersion,
    MccCapabilityStatus CapabilityStatus);

public sealed record MccStateSummaryData(
    int TurnCount,
    int ToolCallCount,
    bool SoftFinish,
    int DirectAnswerAttempts,
    IReadOnlyList<MccVerificationObligationView> OpenVerification,
    IReadOnlyList<MccEvidenceView> RecentEvidence,
    string? CompactionSummary);

public sealed record MccToolCalledData(string CallId, string Name, string ArgumentsJson, bool Advanced, bool Sensitive);

public sealed record MccToolResultData(
    string CallId,
    string Name,
    bool IsError,
    bool Success,
    string? ErrorCode,
    string Summary,
    string RawText,
    string EvidenceId);

public sealed record MccVerificationEventData(string ObligationId, string ToolName, string Kind, string Description);

public sealed record MccBudgetData(
    int TurnCount,
    int MaxTurns,
    int ToolCallCount,
    int MaxToolCalls,
    double ElapsedSeconds,
    int MaxWallClockSeconds);

public sealed record MccErrorData(string Code, string Message, string? Detail = null);

public sealed record MccFinalPayload(
    string Status,
    string Headline,
    string AnswerMarkdown,
    IReadOnlyList<string> VerifiedFacts,
    IReadOnlyList<string> OpenIssues,
    IReadOnlyList<string> EvidenceIds,
    string? NextAction);

public sealed record MccSubmitFinalArgs(
    string Status,
    string Headline,
    string AnswerMarkdown,
    IReadOnlyList<string> VerifiedFacts,
    IReadOnlyList<string> OpenIssues,
    IReadOnlyList<string> EvidenceIds,
    string? NextAction);

public sealed record MccCapabilityStatus(
    [property: JsonPropertyName("sessionStatus")] bool SessionStatus,
    [property: JsonPropertyName("chatAndCommands")] bool ChatAndCommands,
    [property: JsonPropertyName("movement")] bool Movement,
    [property: JsonPropertyName("inventory")] bool Inventory,
    [property: JsonPropertyName("entityWorld")] bool EntityWorld);

public sealed record MccEvidenceView(string Id, string ToolName, string Summary, bool IsError);

public sealed record MccVerificationObligationView(string Id, string ToolName, string Kind, string Description);
