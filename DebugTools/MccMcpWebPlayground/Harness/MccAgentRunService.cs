using System.Globalization;
using System.Runtime.CompilerServices;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using DebugTools.MccMcpWebPlayground.Contracts;
using DebugTools.MccMcpWebPlayground.Infrastructure.Mcp;
using DebugTools.MccMcpWebPlayground.Infrastructure.OpenRouter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DebugTools.MccMcpWebPlayground.Harness;

public interface IMccAgentRunService
{
    IAsyncEnumerable<SseItem<MccStreamEnvelope>> StreamAsync(ChatStreamRequest request, HttpContext httpContext, CancellationToken cancellationToken);
}

public sealed class MccAgentRunService : IMccAgentRunService
{
    private readonly MccMcpSessionFactory sessionFactory;
    private readonly MccGuidanceSource guidanceSource;
    private readonly MccPromptComposer promptComposer;
    private readonly MccContextCompressor contextCompressor;
    private readonly MccFinalizer finalizer;
    private readonly OpenRouterChatClient openRouterChatClient;
    private readonly MccWebHarnessOptions options;

    public MccAgentRunService(
        MccMcpSessionFactory sessionFactory,
        MccGuidanceSource guidanceSource,
        MccPromptComposer promptComposer,
        MccContextCompressor contextCompressor,
        MccFinalizer finalizer,
        OpenRouterChatClient openRouterChatClient,
        IOptions<MccWebHarnessOptions> options)
    {
        this.sessionFactory = sessionFactory;
        this.guidanceSource = guidanceSource;
        this.promptComposer = promptComposer;
        this.contextCompressor = contextCompressor;
        this.finalizer = finalizer;
        this.openRouterChatClient = openRouterChatClient;
        this.options = options.Value;
    }

    public async IAsyncEnumerable<SseItem<MccStreamEnvelope>> StreamAsync(
        ChatStreamRequest request,
        HttpContext httpContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, httpContext.RequestAborted);
        CancellationToken linkedToken = linkedCts.Token;

        string runId = Guid.NewGuid().ToString("n");
        long sequence = 0;

        string? model = options.ResolveModel();
        if (string.IsNullOrWhiteSpace(model))
        {
            yield return CreateEvent(runId, ref sequence, "error", new MccErrorData("configuration_error", "OPENROUTER_MODEL or MccWebHarness:Model must be configured."));
            yield break;
        }

        if (!options.HasApiKeyConfigured())
        {
            yield return CreateEvent(runId, ref sequence, "error", new MccErrorData("configuration_error", "OPENROUTER_API_KEY is not set."));
            yield break;
        }

        List<object> baseConversationMessages = NormalizeConversation(request.Messages);
        string userRequest = ExtractUserRequest(request.Messages);
        if (string.IsNullOrWhiteSpace(userRequest))
        {
            yield return CreateEvent(runId, ref sequence, "error", new MccErrorData("invalid_request", "No user message was provided."));
            yield break;
        }

        await using McpClient client = await sessionFactory.CreateAsync(linkedToken);
        MccGuidanceBundle guidance = await guidanceSource.LoadAsync(client, linkedToken);

        MccRunState runState = new()
        {
            RunId = runId,
            UserRequest = userRequest,
            BaseConversationMessages = baseConversationMessages,
            ConfiguredModel = model,
            Guidance = guidance
        };

        yield return CreateEvent(runId, ref sequence, "run_started", new MccRunStartedData(model, options.ResolveMcpEndpoint(), runState.StartedAtUtc));
        yield return CreateEvent(runId, ref sequence, "guidance_loaded", new MccGuidanceLoadedData(
            guidance.SourceToolName,
            guidance.CanonicalPromptName,
            guidance.GuidanceVersion,
            guidance.CapabilityStatus));

        IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: linkedToken);
        MccToolCatalog catalog = MccToolPolicy.BuildCatalog(tools, options, finalizer.BuildSubmitToolSchema());

        while (!linkedToken.IsCancellationRequested)
        {
            runState.TurnCount++;
            contextCompressor.CompactIfNeeded(runState);
            yield return CreateEvent(runId, ref sequence, "state_summary", BuildStateSummary(runState, options));

            if (runState.IsSoftFinish(options, DateTimeOffset.UtcNow))
            {
                yield return CreateEvent(runId, ref sequence, "budget", BuildBudgetData(runState));
            }

            if (runState.IsHardStop(options, DateTimeOffset.UtcNow))
                break;

            MccModelTurn? turn = null;
            Exception? providerException = null;
            try
            {
                turn = await openRouterChatClient.CreateTurnAsync(
                    promptComposer.Compose(runState),
                    catalog.ModelVisibleTools,
                    options,
                    linkedToken);
            }
            catch (Exception ex)
            {
                providerException = ex;
            }

            if (providerException is not null || turn is null)
            {
                yield return CreateEvent(runId, ref sequence, "error", new MccErrorData("provider_error", "OpenRouter request failed.", providerException?.Message));
                yield return CreateEvent(runId, ref sequence, "final", finalizer.BuildHardStopResult(runState, options));
                yield break;
            }

            runState.RoutedModel = turn.ModelId;
            runState.RoutedProvider = turn.RoutedProvider;

            if (turn.ToolCalls.Count == 0)
            {
                runState.DirectAnswerAttempts++;
                string content = string.IsNullOrWhiteSpace(turn.AssistantContent) ? "(empty assistant turn)" : turn.AssistantContent.Trim();
                runState.ToolConversationMessages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "assistant",
                    ["content"] = content
                });

                if (runState.DirectAnswerAttempts >= 4)
                {
                    yield return CreateEvent(runId, ref sequence, "error", new MccErrorData(
                        "model_protocol_error",
                        "The model kept returning plain assistant text instead of using tools or mcc_submit_final.",
                        content));
                    yield return CreateEvent(runId, ref sequence, "final", finalizer.BuildHardStopResult(runState, options));
                    yield break;
                }

                runState.ToolConversationMessages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = "The previous plain assistant text was not accepted by this harness. On your next turn, you must either call the relevant MCC tools or call mcc_submit_final. Do not answer with plain assistant text again."
                });
                continue;
            }

            Dictionary<string, object?> assistantMessage = new()
            {
                ["role"] = "assistant",
                ["content"] = turn.AssistantContent,
                ["tool_calls"] = turn.ToolCalls.Select(call => new Dictionary<string, object?>
                {
                    ["id"] = call.CallId,
                    ["type"] = "function",
                    ["function"] = new Dictionary<string, object?>
                    {
                        ["name"] = call.Name,
                        ["arguments"] = call.ArgumentsJson
                    }
                }).ToArray()
            };
            runState.ToolConversationMessages.Add(assistantMessage);

            foreach (MccModelToolCall toolCall in turn.ToolCalls)
            {
                MccToolProfile profile = MccToolPolicy.GetProfile(toolCall.Name);
                yield return CreateEvent(runId, ref sequence, "tool_called", new MccToolCalledData(
                    toolCall.CallId,
                    toolCall.Name,
                    toolCall.ArgumentsJson,
                    profile.Risk == MccToolRisk.EscapeHatch,
                    profile.Risk == MccToolRisk.Sensitive));

                if (toolCall.Name.Equals("mcc_submit_final", StringComparison.OrdinalIgnoreCase))
                {
                    MccFinalizationValidation validation = finalizer.Validate(runState, toolCall.ArgumentsJson);
                    if (validation.Accepted)
                    {
                        yield return CreateEvent(runId, ref sequence, "final", validation.Payload!);
                        yield break;
                    }

                    string localResultText = JsonSerializer.Serialize(new
                    {
                        success = false,
                        errorCode = "invalid_final_submission",
                        message = validation.ErrorText
                    });
                    runState.ToolConversationMessages.Add(BuildToolMessage(toolCall.CallId, localResultText));
                    yield return CreateEvent(runId, ref sequence, "tool_result", new MccToolResultData(
                        toolCall.CallId,
                        toolCall.Name,
                        IsError: true,
                        Success: false,
                        ErrorCode: "invalid_final_submission",
                        Summary: validation.ErrorText ?? "Invalid final submission.",
                        RawText: localResultText,
                        EvidenceId: string.Empty));
                    continue;
                }

                if (MccToolPolicy.RequiresExplicitUserIntent(toolCall.Name) && !MccToolPolicy.HasExplicitUserIntent(runState.UserRequest, toolCall.Name))
                {
                    string localResultText = JsonSerializer.Serialize(new
                    {
                        success = false,
                        errorCode = "explicit_user_intent_required",
                        message = $"Tool '{toolCall.Name}' requires explicit user intent."
                    });
                    runState.ToolConversationMessages.Add(BuildToolMessage(toolCall.CallId, localResultText));
                    yield return CreateEvent(runId, ref sequence, "tool_result", new MccToolResultData(
                        toolCall.CallId,
                        toolCall.Name,
                        IsError: true,
                        Success: false,
                        ErrorCode: "explicit_user_intent_required",
                        Summary: $"Tool '{toolCall.Name}' requires explicit user intent.",
                        RawText: localResultText,
                        EvidenceId: string.Empty));
                    continue;
                }

                if (!catalog.ToolsByName.TryGetValue(toolCall.Name, out MccToolCatalogEntry? entry))
                {
                    string unknownToolText = JsonSerializer.Serialize(new
                    {
                        success = false,
                        errorCode = "unknown_tool",
                        message = $"Unknown tool '{toolCall.Name}'."
                    });
                    runState.ToolConversationMessages.Add(BuildToolMessage(toolCall.CallId, unknownToolText));
                    yield return CreateEvent(runId, ref sequence, "tool_result", new MccToolResultData(
                        toolCall.CallId,
                        toolCall.Name,
                        IsError: true,
                        Success: false,
                        ErrorCode: "unknown_tool",
                        Summary: $"Unknown tool '{toolCall.Name}'.",
                        RawText: unknownToolText,
                        EvidenceId: string.Empty));
                    continue;
                }

                CallToolResult? result = null;
                Exception? toolException = null;
                try
                {
                    Dictionary<string, object?> arguments = MccJsonArguments.Parse(toolCall.ArgumentsJson);
                    result = await client.CallToolAsync(toolCall.Name, arguments, cancellationToken: linkedToken);
                }
                catch (Exception ex)
                {
                    toolException = ex;
                }

                if (toolException is not null || result is null)
                {
                    string failedText = JsonSerializer.Serialize(new
                    {
                        success = false,
                        errorCode = "tool_call_failed",
                        message = toolException?.Message
                    });
                    runState.ToolConversationMessages.Add(BuildToolMessage(toolCall.CallId, failedText));
                    yield return CreateEvent(runId, ref sequence, "tool_result", new MccToolResultData(
                        toolCall.CallId,
                        toolCall.Name,
                        IsError: true,
                        Success: false,
                        ErrorCode: "tool_call_failed",
                        Summary: toolException?.Message ?? "Tool call failed.",
                        RawText: failedText,
                        EvidenceId: string.Empty));
                    continue;
                }

                runState.ToolCallCount++;
                MccNormalizedToolResult normalized = MccMcpJson.Normalize(result);
                MccEvidenceRecord evidence = CreateEvidence(runState, toolCall.Name, normalized);
                runState.Evidence.Add(evidence);
                runState.ToolExecutions.Add(new MccToolExecutionRecord
                {
                    CallId = toolCall.CallId,
                    ToolName = toolCall.Name,
                    ArgumentsJson = toolCall.ArgumentsJson,
                    Evidence = evidence
                });

                runState.ToolConversationMessages.Add(BuildToolMessage(toolCall.CallId, normalized.Text));

                foreach (MccVerificationObligation obligation in CreateObligations(runState, evidence, toolCall.ArgumentsJson))
                {
                    runState.VerificationObligations.Add(obligation);
                    yield return CreateEvent(runId, ref sequence, "verification_required", new MccVerificationEventData(
                        obligation.Id,
                        obligation.ToolName,
                        obligation.Kind,
                        obligation.Description));

                    if (obligation.Cleared)
                    {
                        yield return CreateEvent(runId, ref sequence, "verification_cleared", new MccVerificationEventData(
                            obligation.Id,
                            obligation.ToolName,
                            obligation.Kind,
                            obligation.Description));
                    }
                }

                foreach (MccVerificationObligation cleared in TryClearObligationsFromEvidence(runState, evidence))
                {
                    yield return CreateEvent(runId, ref sequence, "verification_cleared", new MccVerificationEventData(
                        cleared.Id,
                        cleared.ToolName,
                        cleared.Kind,
                        cleared.Description));
                }

                yield return CreateEvent(runId, ref sequence, "tool_result", new MccToolResultData(
                    toolCall.CallId,
                    toolCall.Name,
                    evidence.IsError,
                    evidence.Success,
                    evidence.ErrorCode,
                    evidence.Summary,
                    evidence.RawText,
                    evidence.Id));
            }
        }

        yield return CreateEvent(runId, ref sequence, "final", finalizer.BuildHardStopResult(runState, options));
    }

    private static List<object> NormalizeConversation(List<ChatMessage>? incoming)
    {
        List<object> messages = [];
        if (incoming is null)
            return messages;

        foreach (ChatMessage message in incoming)
        {
            if (string.IsNullOrWhiteSpace(message.Role) || string.IsNullOrWhiteSpace(message.Content))
                continue;

            string role = message.Role.Trim().ToLowerInvariant();
            if (role is not ("user" or "assistant" or "system"))
                continue;

            messages.Add(new Dictionary<string, object?>
            {
                ["role"] = role,
                ["content"] = message.Content.Trim()
            });
        }

        return messages;
    }

    private static string ExtractUserRequest(List<ChatMessage>? incoming)
    {
        return incoming?
            .LastOrDefault(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(message.Content))
            ?.Content
            ?.Trim()
            ?? string.Empty;
    }

    private static Dictionary<string, object?> BuildToolMessage(string callId, string content)
    {
        return new Dictionary<string, object?>
        {
            ["role"] = "tool",
            ["tool_call_id"] = callId,
            ["content"] = content
        };
    }

    private static MccStateSummaryData BuildStateSummary(MccRunState runState, MccWebHarnessOptions options)
    {
        return new MccStateSummaryData(
            TurnCount: runState.TurnCount,
            ToolCallCount: runState.ToolCallCount,
            SoftFinish: runState.IsSoftFinish(options, DateTimeOffset.UtcNow),
            DirectAnswerAttempts: runState.DirectAnswerAttempts,
            OpenVerification: runState.OpenObligations
                .Select(obligation => new MccVerificationObligationView(obligation.Id, obligation.ToolName, obligation.Kind, obligation.Description))
                .ToArray(),
            RecentEvidence: runState.Evidence
                .TakeLast(6)
                .Select(evidence => new MccEvidenceView(evidence.Id, evidence.ToolName, evidence.Summary, evidence.IsError))
                .ToArray(),
            CompactionSummary: runState.CompactionSummary);
    }

    private MccBudgetData BuildBudgetData(MccRunState runState)
    {
        return new MccBudgetData(
            TurnCount: runState.TurnCount,
            MaxTurns: options.MaxTurns,
            ToolCallCount: runState.ToolCallCount,
            MaxToolCalls: options.MaxToolCalls,
            ElapsedSeconds: (DateTimeOffset.UtcNow - runState.StartedAtUtc).TotalSeconds,
            MaxWallClockSeconds: options.MaxWallClockSeconds);
    }

    private static MccEvidenceRecord CreateEvidence(MccRunState runState, string toolName, MccNormalizedToolResult result)
    {
        string summary = SummarizeEvidence(toolName, result);
        return new MccEvidenceRecord
        {
            Id = runState.NextEvidenceId(),
            ToolName = toolName,
            Summary = summary,
            RawText = result.Text,
            IsError = result.IsError,
            Success = result.Success,
            ErrorCode = result.ErrorCode,
            Root = result.Root,
            Data = result.Data
        };
    }

    private static string SummarizeEvidence(string toolName, MccNormalizedToolResult result)
    {
        if (result.Data is JsonElement data)
        {
            if ((toolName.Equals("mcc_move_to", StringComparison.OrdinalIgnoreCase) || toolName.Equals("mcc_move_to_player", StringComparison.OrdinalIgnoreCase))
                && TryReadBool(data, "arrived", out bool arrived))
            {
                return arrived
                    ? $"movement verified; arrived={arrived}"
                    : $"movement not yet verified; arrived={arrived}";
            }

            if (toolName.Equals("mcc_dig_block", StringComparison.OrdinalIgnoreCase))
            {
                bool destroyed = TryReadBool(data, "destroyed", out bool destroyedValue) && destroyedValue;
                bool changed = TryReadBool(data, "changed", out bool changedValue) && changedValue;
                return $"dig result changed={changed} destroyed={destroyed}";
            }

            if (toolName.Equals("mcc_items_pickup", StringComparison.OrdinalIgnoreCase))
            {
                int successful = TryReadInt(data, "successfulPickups", out int successfulValue) ? successfulValue : 0;
                int collected = TryReadInt(data, "collectedCount", out int collectedValue) ? collectedValue : 0;
                return $"pickup result successfulPickups={successful} collectedCount={collected}";
            }

            if (toolName.Equals("mcc_container_open_at", StringComparison.OrdinalIgnoreCase)
                && TryReadBool(data, "opened", out bool opened))
            {
                return $"container open result opened={opened}";
            }

            if (toolName is "mcc_container_deposit_item" or "mcc_container_withdraw_item" or "mcc_inventory_drop_item")
            {
                int moved = TryReadInt(data, "movedCount", out int movedValue)
                    ? movedValue
                    : TryReadInt(data, "droppedCount", out int droppedValue) ? droppedValue : 0;
                return $"{toolName} movedCount={moved}";
            }
        }

        string prefix = result.IsError ? "error" : "ok";
        return $"{prefix}: {Truncate(result.Text.Replace('\n', ' '), 180)}";
    }

    private List<MccVerificationObligation> CreateObligations(MccRunState runState, MccEvidenceRecord evidence, string argumentsJson)
    {
        List<MccVerificationObligation> obligations = [];
        JsonElement metadata = ParseArgumentsToJson(argumentsJson);

        if (evidence.ToolName.Equals("mcc_move_to", StringComparison.OrdinalIgnoreCase))
        {
            MccVerificationObligation obligation = new()
            {
                Id = runState.NextObligationId(),
                ToolName = evidence.ToolName,
                Kind = "movement",
                Description = "Verify final player location for the requested move target.",
                SourceEvidenceId = evidence.Id,
                Metadata = BuildMoveMetadata(evidence, metadata),
                Cleared = IsMovementVerified(evidence)
            };
            obligations.Add(obligation);
            return obligations;
        }

        if (evidence.ToolName.Equals("mcc_move_to_player", StringComparison.OrdinalIgnoreCase))
        {
            MccVerificationObligation obligation = new()
            {
                Id = runState.NextObligationId(),
                ToolName = evidence.ToolName,
                Kind = "movement",
                Description = "Verify final proximity to the requested player target.",
                SourceEvidenceId = evidence.Id,
                Metadata = BuildMoveToPlayerMetadata(evidence, metadata),
                Cleared = IsMovementVerified(evidence)
            };
            obligations.Add(obligation);
            return obligations;
        }

        if (evidence.ToolName.Equals("mcc_container_open_at", StringComparison.OrdinalIgnoreCase))
        {
            obligations.Add(new MccVerificationObligation
            {
                Id = runState.NextObligationId(),
                ToolName = evidence.ToolName,
                Kind = "container",
                Description = "Verify that the target container is open and active.",
                SourceEvidenceId = evidence.Id,
                Metadata = null,
                Cleared = IsContainerOpenVerified(evidence)
            });
            return obligations;
        }

        if (evidence.ToolName is "mcc_container_deposit_item" or "mcc_container_withdraw_item" or "mcc_inventory_drop_item")
        {
            obligations.Add(new MccVerificationObligation
            {
                Id = runState.NextObligationId(),
                ToolName = evidence.ToolName,
                Kind = "inventory",
                Description = "Verify the requested inventory delta.",
                SourceEvidenceId = evidence.Id,
                Metadata = evidence.Data,
                Cleared = IsInventoryVerified(evidence)
            });
            return obligations;
        }

        if (evidence.ToolName.Equals("mcc_items_pickup", StringComparison.OrdinalIgnoreCase))
        {
            obligations.Add(new MccVerificationObligation
            {
                Id = runState.NextObligationId(),
                ToolName = evidence.ToolName,
                Kind = "pickup",
                Description = "Verify that the requested dropped items were picked up.",
                SourceEvidenceId = evidence.Id,
                Metadata = evidence.Data,
                Cleared = IsPickupVerified(evidence)
            });
            return obligations;
        }

        if (evidence.ToolName.Equals("mcc_dig_block", StringComparison.OrdinalIgnoreCase))
        {
            obligations.Add(new MccVerificationObligation
            {
                Id = runState.NextObligationId(),
                ToolName = evidence.ToolName,
                Kind = "block_change",
                Description = "Verify that the target block changed state after digging.",
                SourceEvidenceId = evidence.Id,
                Metadata = evidence.Data,
                Cleared = IsDigVerified(evidence)
            });
        }

        return obligations;
    }

    private List<MccVerificationObligation> TryClearObligationsFromEvidence(MccRunState runState, MccEvidenceRecord evidence)
    {
        List<MccVerificationObligation> cleared = [];
        foreach (MccVerificationObligation obligation in runState.OpenObligations)
        {
            if (obligation.Cleared)
                continue;

            if (obligation.Kind == "movement" && TryClearMovementObligation(obligation, evidence))
            {
                obligation.Cleared = true;
                obligation.ClearedByEvidenceId = evidence.Id;
                cleared.Add(obligation);
                continue;
            }

            if (obligation.Kind == "block_change" && TryClearDigObligation(obligation, evidence))
            {
                obligation.Cleared = true;
                obligation.ClearedByEvidenceId = evidence.Id;
                cleared.Add(obligation);
            }
        }

        return cleared;
    }

    private static bool TryClearMovementObligation(MccVerificationObligation obligation, MccEvidenceRecord evidence)
    {
        if (evidence.ToolName.Equals("mcc_player_state", StringComparison.OrdinalIgnoreCase)
            && evidence.Data is JsonElement data
            && data.TryGetProperty("location", out JsonElement location)
            && obligation.Metadata is JsonElement metadata)
        {
            if (obligation.ToolName.Equals("mcc_move_to", StringComparison.OrdinalIgnoreCase)
                && metadata.TryGetProperty("x", out JsonElement targetX)
                && metadata.TryGetProperty("y", out JsonElement targetY)
                && metadata.TryGetProperty("z", out JsonElement targetZ))
            {
                double tolerance = metadata.TryGetProperty("tolerance", out JsonElement toleranceElement) && toleranceElement.TryGetDouble(out double tol) ? tol : 1.5;
                return TryReadDouble(location, "x", out double x)
                    && TryReadDouble(location, "y", out double y)
                    && TryReadDouble(location, "z", out double z)
                    && Distance(x, y, z, targetX.GetDouble(), targetY.GetDouble(), targetZ.GetDouble()) <= tolerance;
            }
        }

        if (evidence.ToolName.Equals("mcc_player_locate", StringComparison.OrdinalIgnoreCase)
            && obligation.ToolName.Equals("mcc_move_to_player", StringComparison.OrdinalIgnoreCase)
            && evidence.Data is JsonElement playerData
            && obligation.Metadata is JsonElement playerMetadata)
        {
            string? expectedName = playerMetadata.TryGetProperty("playerName", out JsonElement nameElement) ? nameElement.GetString() : null;
            string? matchedName = playerData.TryGetProperty("matchedName", out JsonElement matchedNameElement) ? matchedNameElement.GetString() : null;
            if (!string.IsNullOrWhiteSpace(expectedName) && !string.Equals(expectedName, matchedName, StringComparison.OrdinalIgnoreCase))
                return false;

            if (TryReadDouble(playerData, "distance", out double distance))
            {
                double tolerance = playerMetadata.TryGetProperty("tolerance", out JsonElement toleranceElement) && toleranceElement.TryGetDouble(out double tol) ? tol : 2.0;
                return distance <= tolerance;
            }
        }

        return false;
    }

    private static bool TryClearDigObligation(MccVerificationObligation obligation, MccEvidenceRecord evidence)
    {
        if (!evidence.ToolName.Equals("mcc_world_block_at", StringComparison.OrdinalIgnoreCase)
            || evidence.Data is not JsonElement data
            || obligation.Metadata is not JsonElement metadata)
        {
            return false;
        }

        if (!metadata.TryGetProperty("target", out JsonElement target)
            || !TryReadDouble(target, "x", out double x)
            || !TryReadDouble(target, "y", out double y)
            || !TryReadDouble(target, "z", out double z))
        {
            return false;
        }

        return TryReadInt(data, "x", out int blockX)
            && TryReadInt(data, "y", out int blockY)
            && TryReadInt(data, "z", out int blockZ)
            && Math.Abs(blockX - x) < 0.5
            && Math.Abs(blockY - y) < 0.5
            && Math.Abs(blockZ - z) < 0.5
            && data.TryGetProperty("block", out JsonElement block)
            && block.TryGetProperty("material", out JsonElement material)
            && !string.Equals(material.GetString(), "Air", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMovementVerified(MccEvidenceRecord evidence)
    {
        if (evidence.Data is not JsonElement data)
            return false;

        if (TryReadBool(data, "arrived", out bool arrived) && arrived)
            return true;

        if (TryReadDouble(data, "finalDistance", out double finalDistance))
        {
            double tolerance = TryReadDouble(data, "tolerance", out double tol) ? tol : 1.5;
            return finalDistance <= tolerance;
        }

        return false;
    }

    private static bool IsContainerOpenVerified(MccEvidenceRecord evidence)
    {
        return evidence.Data is JsonElement data
            && TryReadBool(data, "opened", out bool opened)
            && opened;
    }

    private static bool IsInventoryVerified(MccEvidenceRecord evidence)
    {
        if (evidence.Data is not JsonElement data)
            return false;

        if (TryReadInt(data, "requestedCount", out int requestedCount)
            && TryReadInt(data, "movedCount", out int movedCount))
        {
            return movedCount == requestedCount;
        }

        if (TryReadInt(data, "requestedCount", out requestedCount)
            && TryReadInt(data, "droppedCount", out int droppedCount))
        {
            return droppedCount == requestedCount;
        }

        return evidence.Success;
    }

    private static bool IsPickupVerified(MccEvidenceRecord evidence)
    {
        if (evidence.Data is not JsonElement data)
            return false;

        return (TryReadInt(data, "successfulPickups", out int successfulPickups) && successfulPickups > 0)
            || (TryReadInt(data, "collectedCount", out int collectedCount) && collectedCount > 0);
    }

    private static bool IsDigVerified(MccEvidenceRecord evidence)
    {
        if (evidence.Data is not JsonElement data)
            return false;

        return (TryReadBool(data, "destroyed", out bool destroyed) && destroyed)
            || (TryReadBool(data, "changed", out bool changed) && changed);
    }

    private static JsonElement? BuildMoveMetadata(MccEvidenceRecord evidence, JsonElement arguments)
    {
        if (evidence.Data is not JsonElement data)
            return null;

        double x = TryReadDoubleFromArguments(arguments, "x", out double targetX)
            ? targetX
            : data.TryGetProperty("target", out JsonElement target) && TryReadDouble(target, "x", out double fromDataX) ? fromDataX : 0;
        double y = TryReadDoubleFromArguments(arguments, "y", out double targetY)
            ? targetY
            : data.TryGetProperty("target", out target) && TryReadDouble(target, "y", out double fromDataY) ? fromDataY : 0;
        double z = TryReadDoubleFromArguments(arguments, "z", out double targetZ)
            ? targetZ
            : data.TryGetProperty("target", out target) && TryReadDouble(target, "z", out double fromDataZ) ? fromDataZ : 0;
        double tolerance = TryReadDouble(data, "tolerance", out double tol) ? tol : 1.5;

        return JsonSerializer.SerializeToElement(new
        {
            x,
            y,
            z,
            tolerance
        });
    }

    private static JsonElement? BuildMoveToPlayerMetadata(MccEvidenceRecord evidence, JsonElement arguments)
    {
        string? playerName = arguments.TryGetProperty("playerName", out JsonElement property) ? property.GetString() : null;
        double tolerance = evidence.Data is JsonElement data && TryReadDouble(data, "tolerance", out double tol) ? tol : 2.0;
        return JsonSerializer.SerializeToElement(new
        {
            playerName,
            tolerance
        });
    }

    private static JsonElement ParseArgumentsToJson(string argumentsJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            return document.RootElement.Clone();
        }
        catch
        {
            using JsonDocument document = JsonDocument.Parse("{}");
            return document.RootElement.Clone();
        }
    }

    private static bool TryReadBool(JsonElement element, string propertyName, out bool value)
    {
        value = false;
        return element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            && ((value = property.GetBoolean()) || !value || true);
    }

    private static bool TryReadInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out JsonElement property) && property.TryGetInt32(out value);
    }

    private static bool TryReadDouble(JsonElement element, string propertyName, out double value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out JsonElement property) && property.TryGetDouble(out value);
    }

    private static bool TryReadDoubleFromArguments(JsonElement element, string propertyName, out double value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out JsonElement property))
            return false;

        return property.ValueKind == JsonValueKind.Number
            ? property.TryGetDouble(out value)
            : property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), out value);
    }

    private static double Distance(double x1, double y1, double z1, double x2, double y2, double z2)
    {
        double dx = x1 - x2;
        double dy = y1 - y2;
        double dz = z1 - z2;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static string Truncate(string text, int maxLength)
    {
        return string.IsNullOrEmpty(text) || text.Length <= maxLength ? text : text[..maxLength] + "...";
    }

    private static SseItem<MccStreamEnvelope> CreateEvent<T>(string runId, ref long sequence, string kind, T data)
    {
        sequence++;
        return new SseItem<MccStreamEnvelope>(
            new MccStreamEnvelope(runId, sequence, kind, data!),
            kind)
        {
            EventId = sequence.ToString(CultureInfo.InvariantCulture)
        };
    }
}
