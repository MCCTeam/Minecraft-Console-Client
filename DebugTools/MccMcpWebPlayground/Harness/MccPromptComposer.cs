namespace DebugTools.MccMcpWebPlayground.Harness;

public sealed class MccPromptComposer
{
    private const string HarnessContract = """
You are operating Minecraft Console Client through MCC MCP tools.

Rules:
- Use tool results and the run-state summary as the source of truth.
- Execute tools sequentially.
- End the run only with mcc_submit_final.
- status=completed is valid only when no required verification obligations remain open.
- If the task is blocked or partial, say exactly what is verified and what remains unverified.
- Do not repeat the same failing stateful action with the same arguments.
- mcc_quit_client requires explicit user intent.
- Prefer structured high-level tools. Avoid escape hatches unless they are explicitly exposed and necessary.
""";

    public List<object> Compose(MccRunState runState)
    {
        List<object> messages =
        [
            BuildSystemMessage(HarnessContract),
            BuildSystemMessage(runState.Guidance.SystemPrompt),
            BuildSystemMessage(BuildStateSummary(runState)),
            .. runState.BaseConversationMessages
        ];

        if (!string.IsNullOrWhiteSpace(runState.CompactionSummary))
        {
            messages.Add(BuildSystemMessage($"""
Older verified evidence summary
{runState.CompactionSummary}
"""));
        }

        foreach (object message in runState.ToolConversationMessages.TakeLast(12))
            messages.Add(message);

        return messages;
    }

    private static Dictionary<string, object?> BuildSystemMessage(string text)
    {
        return new Dictionary<string, object?>
        {
            ["role"] = "system",
            ["content"] = text
        };
    }

    private static string BuildStateSummary(MccRunState runState)
    {
        string evidence = runState.Evidence.Count == 0
            ? "- none yet"
            : string.Join('\n', runState.Evidence.TakeLast(6).Select(record =>
                $"- {record.Id} {record.ToolName}: {record.Summary}"));

        string obligations = runState.OpenObligations.Count == 0
            ? "- none"
            : string.Join('\n', runState.OpenObligations.Select(obligation =>
                $"- {obligation.Id} {obligation.ToolName}/{obligation.Kind}: {obligation.Description}"));

        string bestPractices = runState.Guidance.BestPractices.Length == 0
            ? "- use verified MCC state before claiming success"
            : string.Join('\n', runState.Guidance.BestPractices.Take(4).Select(item => $"- {item}"));

        return $"""
Current run state
- turnCount: {runState.TurnCount}
- toolCallCount: {runState.ToolCallCount}
- directAnswerAttempts: {runState.DirectAnswerAttempts}
- routedModel: {runState.RoutedModel ?? runState.ConfiguredModel}
- routedProvider: {runState.RoutedProvider ?? "unknown"}

Outstanding verification
{obligations}

Recent evidence
{evidence}

Guidance highlights
{bestPractices}
""";
    }
}
