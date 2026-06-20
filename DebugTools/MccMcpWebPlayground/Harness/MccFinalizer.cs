using System.Text.Json;
using System.Text.Json.Nodes;
using DebugTools.MccMcpWebPlayground.Contracts;

namespace DebugTools.MccMcpWebPlayground.Harness;

public sealed class MccFinalizer
{
    private static readonly string[] AllowedStatuses = ["completed", "partial", "blocked", "clarification_needed", "failed"];

    public object BuildSubmitToolSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object?>
            {
                ["name"] = "mcc_submit_final",
                ["description"] = "Submit the final result for this MCC run. Use completed only when no required verification obligations remain open.",
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["status"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray(AllowedStatuses.Select(status => JsonValue.Create(status)).ToArray())
                        },
                        ["headline"] = new JsonObject { ["type"] = "string" },
                        ["answerMarkdown"] = new JsonObject { ["type"] = "string" },
                        ["verifiedFacts"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["items"] = new JsonObject { ["type"] = "string" }
                        },
                        ["openIssues"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["items"] = new JsonObject { ["type"] = "string" }
                        },
                        ["evidenceIds"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["items"] = new JsonObject { ["type"] = "string" }
                        },
                        ["nextAction"] = new JsonObject
                        {
                            ["type"] = new JsonArray("string", "null")
                        }
                    },
                    ["required"] = new JsonArray("status", "headline", "answerMarkdown", "verifiedFacts", "openIssues", "evidenceIds", "nextAction")
                }
            }
        };
    }

    public MccFinalizationValidation Validate(MccRunState runState, string argumentsJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            JsonElement root = document.RootElement;
            MccSubmitFinalArgs submission = new(
                Status: ReadRequiredString(root, "status"),
                Headline: ReadRequiredString(root, "headline"),
                AnswerMarkdown: ReadRequiredString(root, "answerMarkdown"),
                VerifiedFacts: ReadStringArray(root, "verifiedFacts"),
                OpenIssues: ReadStringArray(root, "openIssues"),
                EvidenceIds: ReadStringArray(root, "evidenceIds"),
                NextAction: ReadNullableString(root, "nextAction"));

            string normalizedStatus = submission.Status.Trim().ToLowerInvariant();
            if (!AllowedStatuses.Contains(normalizedStatus, StringComparer.Ordinal))
                return MccFinalizationValidation.Reject("Invalid final status.");

            if (string.IsNullOrWhiteSpace(submission.Headline) || string.IsNullOrWhiteSpace(submission.AnswerMarkdown))
                return MccFinalizationValidation.Reject("headline and answerMarkdown are required.");

            Dictionary<string, MccEvidenceRecord> evidenceById = runState.Evidence.ToDictionary(record => record.Id, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> evidenceAliasByCallId = new(StringComparer.OrdinalIgnoreCase);
            foreach (MccToolExecutionRecord execution in runState.ToolExecutions)
            {
                evidenceAliasByCallId[execution.CallId] = execution.Evidence.Id;

                int suffixSeparator = execution.CallId.LastIndexOf('_');
                if (suffixSeparator >= 0 && suffixSeparator < execution.CallId.Length - 1)
                    evidenceAliasByCallId[execution.CallId[(suffixSeparator + 1)..]] = execution.Evidence.Id;
            }

            List<string> normalizedEvidenceIds = [];
            foreach (string evidenceId in submission.EvidenceIds)
            {
                string normalizedEvidenceId = evidenceAliasByCallId.TryGetValue(evidenceId, out string? mappedEvidenceId)
                    ? mappedEvidenceId
                    : evidenceId;

                if (!evidenceById.ContainsKey(normalizedEvidenceId))
                    return MccFinalizationValidation.Reject($"Unknown evidence id '{evidenceId}'.");

                if (!normalizedEvidenceIds.Contains(normalizedEvidenceId, StringComparer.OrdinalIgnoreCase))
                    normalizedEvidenceIds.Add(normalizedEvidenceId);
            }

            if (normalizedStatus == "completed" && runState.OpenObligations.Count > 0)
                return MccFinalizationValidation.Reject("completed is invalid while verification obligations remain open.");

            if (!AreVerifiedFactsGrounded(submission.VerifiedFacts, normalizedEvidenceIds, evidenceById))
                return MccFinalizationValidation.Reject("verifiedFacts must be grounded in the referenced evidence.");

            return MccFinalizationValidation.Accept(new MccFinalPayload(
                normalizedStatus,
                submission.Headline.Trim(),
                submission.AnswerMarkdown.Trim(),
                submission.VerifiedFacts,
                submission.OpenIssues,
                normalizedEvidenceIds,
                string.IsNullOrWhiteSpace(submission.NextAction) ? null : submission.NextAction.Trim()));
        }
        catch (Exception ex)
        {
            return MccFinalizationValidation.Reject($"Invalid mcc_submit_final payload: {ex.Message}");
        }
    }

    public MccFinalPayload BuildHardStopResult(MccRunState runState, MccWebHarnessOptions options)
    {
        IReadOnlyList<string> openIssues = runState.OpenObligations.Count > 0
            ? runState.OpenObligations.Select(obligation => obligation.Description).ToArray()
            : ["The harness reached its execution budget before the run was explicitly finalized."];

        IReadOnlyList<string> evidenceIds = runState.Evidence.TakeLast(4).Select(record => record.Id).ToArray();
        IReadOnlyList<string> verifiedFacts = runState.Evidence
            .TakeLast(4)
            .Where(record => record.Success)
            .Select(record => record.Summary)
            .ToArray();

        return new MccFinalPayload(
            Status: runState.OpenObligations.Count > 0 ? "partial" : "blocked",
            Headline: "Run stopped before explicit completion",
            AnswerMarkdown: "I could not finish the request within the current harness budget. I am returning the strongest verified state captured so far.",
            VerifiedFacts: verifiedFacts,
            OpenIssues: openIssues,
            EvidenceIds: evidenceIds,
            NextAction: "Retry with a fresh run if you want me to continue from the latest verified state.");
    }

    private static bool AreVerifiedFactsGrounded(
        IReadOnlyList<string> verifiedFacts,
        IReadOnlyList<string> evidenceIds,
        IReadOnlyDictionary<string, MccEvidenceRecord> evidenceById)
    {
        if (verifiedFacts.Count == 0)
            return true;

        if (evidenceIds.Count == 0)
            return false;

        string evidenceCorpus = string.Join(' ', evidenceIds
            .Where(evidenceById.ContainsKey)
            .Select(id => evidenceById[id].Summary))
            .ToLowerInvariant();

        foreach (string fact in verifiedFacts)
        {
            HashSet<string> factTokens = fact.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(token => token.Trim().Trim(',', '.', ':', ';', '!', '?', '"', '\''))
                .Where(token => token.Length >= 4)
                .Select(token => token.ToLowerInvariant())
                .ToHashSet(StringComparer.Ordinal);

            if (factTokens.Count == 0)
                continue;

            int matches = factTokens.Count(token => evidenceCorpus.Contains(token, StringComparison.Ordinal));
            if (matches < Math.Min(2, factTokens.Count))
                return false;
        }

        return true;
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        string? value = ReadNullableString(root, propertyName);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{propertyName} is required.");

        return value.Trim();
    }

    private static string? ReadNullableString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property))
            return null;

        return property.ValueKind == JsonValueKind.Null ? null : property.GetString();
    }

    private static string[] ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Array)
            return [];

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToArray();
    }
}

public sealed record MccFinalizationValidation(bool Accepted, string? ErrorText, MccFinalPayload? Payload)
{
    public static MccFinalizationValidation Accept(MccFinalPayload payload) => new(true, null, payload);

    public static MccFinalizationValidation Reject(string errorText) => new(false, errorText, null);
}
