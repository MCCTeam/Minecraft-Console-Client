using System.Text.Json;
using DebugTools.MccMcpWebPlayground.Contracts;

namespace DebugTools.MccMcpWebPlayground.Harness;

public sealed class MccRunState
{
    private int evidenceCounter;
    private int obligationCounter;

    public required string RunId { get; init; }
    public required string UserRequest { get; init; }
    public required List<object> BaseConversationMessages { get; init; }
    public required string ConfiguredModel { get; init; }
    public required MccGuidanceBundle Guidance { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public List<object> ToolConversationMessages { get; } = [];
    public List<MccEvidenceRecord> Evidence { get; } = [];
    public List<MccToolExecutionRecord> ToolExecutions { get; } = [];
    public List<MccVerificationObligation> VerificationObligations { get; } = [];
    public string? CompactionSummary { get; set; }
    public string? RoutedModel { get; set; }
    public string? RoutedProvider { get; set; }
    public int TurnCount { get; set; }
    public int ToolCallCount { get; set; }
    public int DirectAnswerAttempts { get; set; }

    public string NextEvidenceId() => $"e{++evidenceCounter:0000}";

    public string NextObligationId() => $"v{++obligationCounter:0000}";

    public bool IsSoftFinish(MccWebHarnessOptions options, DateTimeOffset nowUtc)
    {
        TimeSpan elapsed = nowUtc - StartedAtUtc;
        return (options.MaxTurns - TurnCount) <= options.SoftFinishRemainingTurns
            || (options.MaxToolCalls - ToolCallCount) <= options.SoftFinishRemainingToolCalls
            || (options.MaxWallClockSeconds - (int)elapsed.TotalSeconds) <= options.SoftFinishRemainingSeconds;
    }

    public bool IsHardStop(MccWebHarnessOptions options, DateTimeOffset nowUtc)
    {
        TimeSpan elapsed = nowUtc - StartedAtUtc;
        return TurnCount >= options.MaxTurns
            || ToolCallCount >= options.MaxToolCalls
            || elapsed.TotalSeconds >= options.MaxWallClockSeconds;
    }

    public IReadOnlyList<MccVerificationObligation> OpenObligations =>
        VerificationObligations.Where(obligation => !obligation.Cleared).ToArray();
}

public sealed record MccGuidanceBundle(
    string SourceToolName,
    string CanonicalPromptName,
    string SkillName,
    string GuidanceVersion,
    string SystemPrompt,
    string[] BestPractices,
    string[] ExampleScenarioTitles,
    MccCapabilityStatus CapabilityStatus);

public sealed class MccEvidenceRecord
{
    public required string Id { get; init; }
    public required string ToolName { get; init; }
    public required string Summary { get; init; }
    public required string RawText { get; init; }
    public required bool IsError { get; init; }
    public required bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public JsonElement? Root { get; init; }
    public JsonElement? Data { get; init; }
}

public sealed class MccToolExecutionRecord
{
    public required string CallId { get; init; }
    public required string ToolName { get; init; }
    public required string ArgumentsJson { get; init; }
    public required MccEvidenceRecord Evidence { get; init; }
}

public sealed class MccVerificationObligation
{
    public required string Id { get; init; }
    public required string ToolName { get; init; }
    public required string Kind { get; init; }
    public required string Description { get; init; }
    public required string SourceEvidenceId { get; init; }
    public JsonElement? Metadata { get; init; }
    public bool Cleared { get; set; }
    public string? ClearedByEvidenceId { get; set; }
}

public sealed record MccNormalizedToolResult(
    string Text,
    bool IsError,
    bool Success,
    string? ErrorCode,
    string? Message,
    JsonElement? Root,
    JsonElement? Data);
