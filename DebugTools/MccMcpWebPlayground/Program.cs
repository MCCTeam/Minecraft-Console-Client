using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("openrouter", client =>
{
    client.Timeout = TimeSpan.FromMinutes(15);
});

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

const string AgentSystemPrompt = """
You are an agent controlling Minecraft Console Client (MCC) through MCP tools.
Use a plan-execute-verify loop.

Operating mode
- For simple social turns like "hello" or "thanks", do not waste tool calls. Finish directly unless MCC state is required.
- For MCC questions and actions, think in steps and use tools to gather evidence before you finish.
- Never output plain assistant text before calling agent_finish(answer).

Planning policy
- If the task is multi-step or physical, first decompose it into a short internal plan.
- Prefer the smallest plan that can succeed.
- For long or branchy tasks, keep a short checklist and update it as you go.
- Default sequence:
  1) inspect current state
  2) locate the target
  3) move into a valid position if needed
  4) perform the action
  5) verify with fresh tool calls
  6) call agent_finish(answer)
- If a step fails, revise the plan using the latest observation. Do not blindly repeat the same failing action.

Todo policy
- Use todo_write, todo_read, and todo_list for tasks with 4 or more steps, retries, or branching verification.
- Keep todos short, concrete, and action-oriented.
- Update todo status as facts change.
- Todo state is request-scoped for the current chat request only.
- Skip todo tools for simple one-step tasks.

Tool-use policy
- Use MCP tools for MCC/game-state questions and actions.
- Prefer the most direct high-signal tool first.
- Prefer structured inventory/container tools over raw window-click tools for chest or container management.
- If a tool result says success=false or includes an errorCode, treat that as a failed observation even if the transport call itself succeeded.
- Do not guess tool arguments repeatedly. If a tool returns invalid_args:
  - simplify to the minimum required arguments,
  - try at most one nearby variant,
  - or switch to a broader inspection tool.
- Avoid long speculative tool chains.

Verification policy
- Never claim success from intent alone.
- Never claim movement succeeded just because a move command was accepted. Check arrived or a fresh location result.
- Never claim an item was collected unless inventory or nearby entity state changed.
- Never claim blocks were removed unless block/world search results changed.
- If evidence is partial, say it is partial.
- If the request cannot be completed, say exactly what was verified and what remains unverified.

Action-specific guidance
- Move or approach:
  - locate the target,
  - choose a reachable nearby standing position when exact occupancy is risky,
  - move,
  - verify arrival before finishing.
- Dig or collect:
  - locate the blocks,
  - move next to them if needed,
  - dig in a sensible order,
  - re-check remaining blocks,
  - re-check inventory or nearby item entities before finishing.
- Container inventory:
  - locate the target container block,
  - open the container first,
  - inspect player and container inventory state,
  - use structured deposit or withdraw tools instead of raw window clicks,
  - verify both player and container counts changed before finishing.
- Search:
  - start with the most direct search tool,
  - use the user's requested radius when supported,
  - if a query fails, simplify it instead of trying many near-duplicates.

Good examples
1) User: "Pick up those logs."
   Good:
   - if the task looks long, write a short todo list
   - find the logs
   - move next to them
   - dig them
   - verify the logs are gone or reduced
   - verify inventory increased
   - then finish
2) User: "Is Zarko near you?"
   Good:
   - call a nearby-player tool
   - report the matched player and distance
   - then finish
3) User: "Hello"
   Good:
   - finish with a short greeting
   - no MCP tools
4) User: "Put 5 diamonds in the chest."
   Good:
   - open the chest
   - inspect inventory state
   - deposit exactly 5 diamonds
   - verify the chest count increased and player count decreased by 5
   - then finish

Wrong examples
1) Wrong:
   - inventory did not change
   - blocks may still exist
   - but you still say "I picked them up"
2) Wrong:
   - move returns pathFound=true but arrived=false
   - and you still say "I walked there"
3) Wrong:
   - a tool returns invalid_args several times
   - and you keep guessing similar argument combinations
4) Wrong:
   - you write assistant prose before agent_finish(answer)

Finish rules
- Complete only by calling agent_finish(answer).
- The final answer must be natural language for a human and include exactly:
  Reasoning:
  - brief bullets with the important verified observations
  Answer:
  - direct user-facing result with uncertainty stated when relevant
""";

const string BudgetReminderPrompt = """
Budget is nearly exhausted.
Use the strongest verified evidence you already have.
Do not start speculative new branches.
If the task is complete or partially complete, call agent_finish(answer) now and clearly distinguish verified facts from unverified assumptions.
Do not output plain assistant text before finishing.
""";

app.MapGet("/api/health", () => Results.Ok(new { ok = true }));
app.MapGet("/api/config", () =>
{
    return Results.Ok(new
    {
        model = GetModel(),
        openRouterBaseUrl = GetOpenRouterBaseUrl(),
        mcpEndpoint = GetMcpEndpoint(),
        hasApiKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"))
    });
});

app.MapPost("/api/chat/stream", async (ChatStreamRequest request, IHttpClientFactory httpClientFactory, HttpContext context, CancellationToken cancellationToken) =>
{
    context.Response.StatusCode = StatusCodes.Status200OK;
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers["X-Accel-Buffering"] = "no";

    try
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await WriteEvent(context.Response, "error", new { message = "OPENROUTER_API_KEY is not set." }, cancellationToken);
            return;
        }

        List<object> messages = BuildMessages(request.Messages);
        if (messages.Count == 0)
        {
            await WriteEvent(context.Response, "error", new { message = "No messages provided." }, cancellationToken);
            return;
        }

        string model = GetModel();
        int maxIterations = GetBoundedInt("MCC_WEB_MAX_ITERATIONS", 96, 4, 256);
        int maxToolCalls = GetBoundedInt("MCC_WEB_MAX_TOOL_CALLS", 320, 4, 1024);
        TimeSpan maxWallTime = TimeSpan.FromSeconds(GetBoundedInt("MCC_WEB_MAX_SECONDS", 900, 10, 3600));

        await using McpClient mcp = await CreateMcpClientAsync(cancellationToken);
        IList<McpClientTool> mcpTools = await mcp.ListToolsAsync(cancellationToken: cancellationToken);
        Dictionary<string, McpClientTool> mcpToolsByName = mcpTools
            .ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);

        object[] openRouterTools =
        [
            .. mcpTools.Select(ToOpenRouterTool),
            BuildTodoWriteToolSchema(),
            BuildTodoReadToolSchema(),
            BuildTodoListToolSchema(),
            BuildAgentFinishToolSchema()
        ];

        using HttpClient openRouter = httpClientFactory.CreateClient("openrouter");
        openRouter.BaseAddress = new Uri(GetOpenRouterBaseUrl().TrimEnd('/') + "/");
        openRouter.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        openRouter.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", "https://localhost/mcc-mcp-web-playground");
        openRouter.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", "MCC MCP Web Playground");

        Stopwatch wallClock = Stopwatch.StartNew();
        int toolCallCount = 0;
        bool reminderInjected = false;
        string? finalAnswer = null;
        List<string> observations = new();
        Dictionary<string, TodoEntry> todos = new(StringComparer.OrdinalIgnoreCase);
        int nextTodoOrder = 0;

        for (int iteration = 1; iteration <= maxIterations && !cancellationToken.IsCancellationRequested; iteration++)
        {
            if (!reminderInjected && ShouldInjectReminder(iteration, maxIterations, toolCallCount, maxToolCalls, wallClock.Elapsed, maxWallTime))
            {
                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "system",
                    ["content"] = BudgetReminderPrompt
                });
                reminderInjected = true;
            }

            if (wallClock.Elapsed >= maxWallTime || toolCallCount >= maxToolCalls)
                break;

            JsonElement choiceMessage = await RequestToolIterationAsync(openRouter, model, messages, openRouterTools, context.Response, cancellationToken);
            if (choiceMessage.ValueKind == JsonValueKind.Undefined)
                return;

            string assistantContent = choiceMessage.TryGetProperty("content", out JsonElement contentElement)
                ? contentElement.GetString() ?? string.Empty
                : string.Empty;

            if (choiceMessage.TryGetProperty("tool_calls", out JsonElement toolCallsElement)
                && toolCallsElement.ValueKind == JsonValueKind.Array
                && toolCallsElement.GetArrayLength() > 0)
            {
                List<object> toolCallsForHistory = new();
                List<object> toolMessages = new();
                bool stopLoop = false;

                foreach (JsonElement toolCall in toolCallsElement.EnumerateArray())
                {
                    if (!TryReadToolCall(toolCall, out string callId, out string toolName, out string argumentsRaw))
                        continue;

                    toolCallsForHistory.Add(new Dictionary<string, object?>
                    {
                        ["id"] = callId,
                        ["type"] = "function",
                        ["function"] = new Dictionary<string, object?>
                        {
                            ["name"] = toolName,
                            ["arguments"] = argumentsRaw
                        }
                    });

                    await WriteEvent(context.Response, "tool_call", new
                    {
                        id = callId,
                        name = toolName,
                        arguments = argumentsRaw
                    }, cancellationToken);

                    if (TryHandleLocalToolCall(toolName, argumentsRaw, todos, ref nextTodoOrder, out bool localIsError, out string localResultText, out string? completedAnswer))
                    {
                        await WriteEvent(context.Response, "tool_result", new
                        {
                            id = callId,
                            name = toolName,
                            isError = localIsError,
                            content = localResultText
                        }, cancellationToken);

                        toolMessages.Add(new Dictionary<string, object?>
                        {
                            ["role"] = "tool",
                            ["tool_call_id"] = callId,
                            ["content"] = localResultText
                        });

                        toolCallCount++;
                        observations.Add(SummarizeObservation(toolName, localResultText, localIsError));

                        if (completedAnswer is not null)
                        {
                            finalAnswer = EnsureFinalAnswerFormat(completedAnswer, observations);
                            stopLoop = true;
                            break;
                        }

                        continue;
                    }

                    if (!mcpToolsByName.ContainsKey(toolName))
                    {
                        string resultText = JsonSerializer.Serialize(new
                        {
                            success = false,
                            errorCode = "unknown_tool",
                            message = $"Unknown tool '{toolName}'."
                        });
                        await WriteEvent(context.Response, "tool_result", new
                        {
                            id = callId,
                            name = toolName,
                            isError = true,
                            content = resultText
                        }, cancellationToken);

                        observations.Add($"Tool {toolName} was rejected because it is unknown.");
                        toolMessages.Add(new Dictionary<string, object?>
                        {
                            ["role"] = "tool",
                            ["tool_call_id"] = callId,
                            ["content"] = resultText
                        });
                        continue;
                    }

                    if (toolCallCount >= maxToolCalls)
                    {
                        stopLoop = true;
                        break;
                    }

                    bool isError = false;
                    string toolResultText;
                    try
                    {
                        Dictionary<string, object?> arguments = ParseArguments(argumentsRaw);
                        CallToolResult toolResult = await mcp.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);
                        toolResultText = ReadToolResultText(toolResult);
                        isError = toolResult.IsError == true || InferStructuredToolError(toolResultText);
                    }
                    catch (Exception ex)
                    {
                        isError = true;
                        toolResultText = JsonSerializer.Serialize(new
                        {
                            success = false,
                            errorCode = "tool_call_failed",
                            message = ex.Message
                        });
                    }

                    toolCallCount++;
                    observations.Add(SummarizeObservation(toolName, toolResultText, isError));
                    await WriteEvent(context.Response, "tool_result", new
                    {
                        id = callId,
                        name = toolName,
                        isError,
                        content = toolResultText
                    }, cancellationToken);

                    toolMessages.Add(new Dictionary<string, object?>
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = callId,
                        ["content"] = toolResultText
                    });
                }

                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "assistant",
                    ["content"] = assistantContent,
                    ["tool_calls"] = toolCallsForHistory
                });
                foreach (object toolMessage in toolMessages)
                    messages.Add(toolMessage);

                if (finalAnswer is not null || stopLoop)
                    break;

                continue;
            }

            if (!string.IsNullOrWhiteSpace(assistantContent))
                observations.Add($"Model attempted direct text before finishing: {Truncate(assistantContent, 140)}");

            messages.Add(new Dictionary<string, object?>
            {
                ["role"] = "assistant",
                ["content"] = assistantContent
            });
            messages.Add(new Dictionary<string, object?>
            {
                ["role"] = "system",
                ["content"] = "Do not return assistant prose yet. Continue with tool calls and end only by calling agent_finish(answer)."
            });
        }

        finalAnswer ??= BuildForcedFinalAnswer(observations, toolCallCount, wallClock.Elapsed, maxIterations, maxToolCalls, maxWallTime);
        await StreamFinalAnswer(context.Response, finalAnswer, cancellationToken);
    }
    catch (OperationCanceledException)
    {
        await WriteEvent(context.Response, "error", new { message = "Request cancelled." }, CancellationToken.None);
    }
    catch (Exception ex)
    {
        await WriteEvent(context.Response, "error", new
        {
            message = "Unhandled server error.",
            detail = ex.Message
        }, CancellationToken.None);
    }
});

app.Run();

static string GetModel()
{
    return Environment.GetEnvironmentVariable("OPENROUTER_MODEL") ?? "minimax/minimax-m2.7";
}

static string GetOpenRouterBaseUrl()
{
    return Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL") ?? "https://openrouter.ai/api/v1";
}

static string GetMcpEndpoint()
{
    return Environment.GetEnvironmentVariable("MCC_MCP_ENDPOINT") ?? "http://127.0.0.1:33333/mcp";
}

static async Task<McpClient> CreateMcpClientAsync(CancellationToken cancellationToken)
{
    string endpoint = GetMcpEndpoint();
    string? token = Environment.GetEnvironmentVariable("MCC_MCP_AUTH_TOKEN");

    return await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri(endpoint),
        TransportMode = HttpTransportMode.AutoDetect,
        AdditionalHeaders = string.IsNullOrWhiteSpace(token)
            ? null
            : new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" }
    }), cancellationToken: cancellationToken);
}

List<object> BuildMessages(List<ChatMessage>? incoming)
{
    List<object> messages =
    [
        new Dictionary<string, object?>
        {
            ["role"] = "system",
            ["content"] = AgentSystemPrompt
        }
    ];

    if (incoming is null)
        return messages;

    foreach (ChatMessage message in incoming)
    {
        if (string.IsNullOrWhiteSpace(message.Role) || string.IsNullOrWhiteSpace(message.Content))
            continue;

        string role = message.Role.Trim().ToLowerInvariant();
        if (role is not ("system" or "user" or "assistant"))
            continue;

        messages.Add(new Dictionary<string, object?>
        {
            ["role"] = role,
            ["content"] = message.Content
        });
    }

    return messages;
}

static object ToOpenRouterTool(McpClientTool tool)
{
    JsonNode parameters = JsonNode.Parse(tool.JsonSchema.GetRawText()) ?? new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject()
    };

    return new Dictionary<string, object?>
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object?>
        {
            ["name"] = tool.Name,
            ["description"] = tool.Description,
            ["parameters"] = parameters
        }
    };
}

static object BuildAgentFinishToolSchema()
{
    return new Dictionary<string, object?>
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object?>
        {
            ["name"] = "agent_finish",
            ["description"] = "Finalize the response to the user after all required tool calls and verification are done.",
            ["parameters"] = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>
                {
                    ["answer"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                        ["description"] = "Final natural-language response for the user."
                    }
                },
                ["required"] = new[] { "answer" },
                ["additionalProperties"] = false
            }
        }
    };
}

static object BuildTodoWriteToolSchema()
{
    return new Dictionary<string, object?>
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object?>
        {
            ["name"] = "todo_write",
            ["description"] = "Create or update a short request-scoped todo item for complex task tracking.",
            ["parameters"] = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>
                {
                    ["id"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                        ["description"] = "Stable todo identifier, for example move_to_logs or verify_inventory."
                    },
                    ["content"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                        ["description"] = "Short actionable todo text. Required when creating a new item."
                    },
                    ["status"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                        ["description"] = "One of pending, in_progress, completed, blocked, cancelled."
                    },
                    ["notes"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                        ["description"] = "Optional brief note with the latest observation."
                    }
                },
                ["required"] = new[] { "id" },
                ["additionalProperties"] = false
            }
        }
    };
}

static object BuildTodoReadToolSchema()
{
    return new Dictionary<string, object?>
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object?>
        {
            ["name"] = "todo_read",
            ["description"] = "Read one request-scoped todo item by id.",
            ["parameters"] = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>
                {
                    ["id"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                        ["description"] = "Todo identifier."
                    }
                },
                ["required"] = new[] { "id" },
                ["additionalProperties"] = false
            }
        }
    };
}

static object BuildTodoListToolSchema()
{
    return new Dictionary<string, object?>
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object?>
        {
            ["name"] = "todo_list",
            ["description"] = "List all request-scoped todo items in creation order.",
            ["parameters"] = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>(),
                ["additionalProperties"] = false
            }
        }
    };
}

static bool TryHandleLocalToolCall(
    string toolName,
    string argumentsRaw,
    Dictionary<string, TodoEntry> todos,
    ref int nextTodoOrder,
    out bool isError,
    out string resultText,
    out string? completedAnswer)
{
    isError = false;
    resultText = string.Empty;
    completedAnswer = null;

    if (toolName.Equals("agent_finish", StringComparison.OrdinalIgnoreCase))
    {
        completedAnswer = ParseAgentFinishAnswer(argumentsRaw);
        resultText = JsonSerializer.Serialize(new
        {
            success = true,
            finished = true
        });
        return true;
    }

    if (toolName.Equals("todo_list", StringComparison.OrdinalIgnoreCase))
    {
        resultText = JsonSerializer.Serialize(new
        {
            success = true,
            data = new
            {
                count = todos.Count,
                items = todos.Values
                    .OrderBy(item => item.Order)
                    .Select(ToTodoDto)
                    .ToArray()
            }
        });
        return true;
    }

    Dictionary<string, object?> arguments = ParseArguments(argumentsRaw);
    if (toolName.Equals("todo_read", StringComparison.OrdinalIgnoreCase))
    {
        string? id = ReadOptionalStringArgument(arguments, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            isError = true;
            resultText = JsonSerializer.Serialize(new
            {
                success = false,
                errorCode = "invalid_args",
                message = "todo_read requires a non-empty id."
            });
            return true;
        }

        if (!todos.TryGetValue(id, out TodoEntry? item))
        {
            isError = true;
            resultText = JsonSerializer.Serialize(new
            {
                success = false,
                errorCode = "invalid_state",
                message = $"Todo '{id}' does not exist."
            });
            return true;
        }

        resultText = JsonSerializer.Serialize(new
        {
            success = true,
            data = new
            {
                item = ToTodoDto(item)
            }
        });
        return true;
    }

    if (!toolName.Equals("todo_write", StringComparison.OrdinalIgnoreCase))
        return false;

    string? todoId = ReadOptionalStringArgument(arguments, "id");
    if (string.IsNullOrWhiteSpace(todoId))
    {
        isError = true;
        resultText = JsonSerializer.Serialize(new
        {
            success = false,
            errorCode = "invalid_args",
            message = "todo_write requires a non-empty id."
        });
        return true;
    }

    todos.TryGetValue(todoId, out TodoEntry? existingItem);
    string? rawContent = ReadOptionalStringArgument(arguments, "content");
    string content = string.IsNullOrWhiteSpace(rawContent)
        ? existingItem?.Content ?? string.Empty
        : rawContent.Trim();
    if (content.Length == 0)
    {
        isError = true;
        resultText = JsonSerializer.Serialize(new
        {
            success = false,
            errorCode = "invalid_args",
            message = "todo_write requires content when creating a new item."
        });
        return true;
    }

    string requestedStatus = ReadOptionalStringArgument(arguments, "status") ?? existingItem?.Status ?? "pending";
    if (!TryNormalizeTodoStatus(requestedStatus, out string normalizedStatus))
    {
        isError = true;
        resultText = JsonSerializer.Serialize(new
        {
            success = false,
            errorCode = "invalid_args",
            message = "Invalid todo status.",
            data = new
            {
                status = requestedStatus,
                allowed = GetTodoStatusValues()
            }
        });
        return true;
    }

    string? notes = ReadOptionalStringArgument(arguments, "notes") ?? existingItem?.Notes;
    TodoEntry entry = existingItem ?? new TodoEntry
    {
        Id = todoId,
        Order = ++nextTodoOrder
    };
    entry.Content = content;
    entry.Status = normalizedStatus;
    entry.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    todos[todoId] = entry;

    resultText = JsonSerializer.Serialize(new
    {
        success = true,
        data = new
        {
            item = ToTodoDto(entry),
            totalCount = todos.Count
        }
    });
    return true;
}

static async Task<JsonElement> RequestToolIterationAsync(
    HttpClient openRouter,
    string model,
    List<object> messages,
    object[] tools,
    HttpResponse response,
    CancellationToken cancellationToken)
{
    var payload = new Dictionary<string, object?>
    {
        ["model"] = model,
        ["messages"] = messages,
        ["tools"] = tools,
        ["tool_choice"] = "auto"
    };

    using HttpResponseMessage completion = await openRouter.PostAsync(
        "chat/completions",
        new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        cancellationToken);

    string body = await completion.Content.ReadAsStringAsync(cancellationToken);
    if (!completion.IsSuccessStatusCode)
    {
        await WriteEvent(response, "error", new
        {
            message = "OpenRouter request failed.",
            statusCode = (int)completion.StatusCode,
            body
        }, cancellationToken);
        return default;
    }

    using JsonDocument doc = JsonDocument.Parse(body);
    if (!TryGetFirstChoiceMessage(doc.RootElement, out JsonElement message))
    {
        await WriteEvent(response, "error", new { message = "No completion choice returned by OpenRouter." }, cancellationToken);
        return default;
    }

    return message.Clone();
}

static bool TryReadToolCall(JsonElement toolCall, out string id, out string name, out string arguments)
{
    id = string.Empty;
    name = string.Empty;
    arguments = "{}";

    if (!toolCall.TryGetProperty("id", out JsonElement idElement)
        || !toolCall.TryGetProperty("function", out JsonElement functionElement)
        || !functionElement.TryGetProperty("name", out JsonElement nameElement))
    {
        return false;
    }

    id = idElement.GetString() ?? string.Empty;
    name = nameElement.GetString() ?? string.Empty;
    arguments = functionElement.TryGetProperty("arguments", out JsonElement argsElement)
        ? argsElement.GetString() ?? "{}"
        : "{}";
    return true;
}

static bool TryGetFirstChoiceMessage(JsonElement root, out JsonElement message)
{
    message = default;
    if (!root.TryGetProperty("choices", out JsonElement choices)
        || choices.ValueKind != JsonValueKind.Array
        || choices.GetArrayLength() == 0)
    {
        return false;
    }

    JsonElement first = choices[0];
    return first.TryGetProperty("message", out message);
}

static Dictionary<string, object?> ParseArguments(string raw)
{
    try
    {
        using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, object?>();

        Dictionary<string, object?> parsed = new();
        foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            parsed[property.Name] = ConvertJsonElement(property.Value);
        return parsed;
    }
    catch
    {
        return new Dictionary<string, object?>();
    }
}

static string? ReadOptionalStringArgument(Dictionary<string, object?> arguments, string key)
{
    if (!arguments.TryGetValue(key, out object? value) || value is null)
        return null;

    return value switch
    {
        string text => text.Trim(),
        _ => Convert.ToString(value)?.Trim()
    };
}

static object? ConvertJsonElement(JsonElement element)
{
    return element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => element.TryGetInt64(out long i64)
            ? i64
            : element.TryGetDouble(out double d) ? d : element.GetRawText(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(prop => prop.Name, prop => ConvertJsonElement(prop.Value)),
        _ => element.GetRawText()
    };
}

static string ReadToolResultText(CallToolResult result)
{
    if (result.Content is null)
        return result.IsError == true ? "{\"success\":false}" : "{\"success\":true}";

    StringBuilder sb = new();
    foreach (ContentBlock block in result.Content)
    {
        if (block is TextContentBlock text && !string.IsNullOrWhiteSpace(text.Text))
        {
            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(text.Text);
        }
    }

    if (sb.Length > 0)
        return sb.ToString();

    return JsonSerializer.Serialize(new { isError = result.IsError });
}

static bool InferStructuredToolError(string toolResultText)
{
    try
    {
        using JsonDocument doc = JsonDocument.Parse(toolResultText);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return false;

        if (doc.RootElement.TryGetProperty("success", out JsonElement successElement)
            && successElement.ValueKind == JsonValueKind.False)
        {
            return true;
        }

        return doc.RootElement.TryGetProperty("errorCode", out JsonElement errorCodeElement)
            && errorCodeElement.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(errorCodeElement.GetString());
    }
    catch
    {
        return false;
    }
}

static bool TryNormalizeTodoStatus(string rawStatus, out string normalizedStatus)
{
    normalizedStatus = rawStatus.Trim().ToLowerInvariant();
    return normalizedStatus is "pending" or "in_progress" or "completed" or "blocked" or "cancelled";
}

static string[] GetTodoStatusValues()
{
    return ["pending", "in_progress", "completed", "blocked", "cancelled"];
}

static object ToTodoDto(TodoEntry item)
{
    return new
    {
        id = item.Id,
        content = item.Content,
        status = item.Status,
        notes = item.Notes,
        order = item.Order
    };
}

static string ParseAgentFinishAnswer(string argumentsRaw)
{
    try
    {
        using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsRaw) ? "{}" : argumentsRaw);
        if (doc.RootElement.TryGetProperty("answer", out JsonElement answerElement)
            && answerElement.ValueKind == JsonValueKind.String)
        {
            string answer = answerElement.GetString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(answer))
                return answer.Trim();
        }
    }
    catch
    {
        // ignore and use fallback below
    }

    return """
Reasoning:
- The model requested completion without a textual payload.
- Returning a safe fallback response.

Answer:
I completed the requested tool workflow but did not receive a final textual answer payload.
""";
}

static string EnsureFinalAnswerFormat(string text, IReadOnlyList<string> observations)
{
    string trimmed = text.Trim();
    if (trimmed.Length == 0)
        trimmed = "I completed the tool workflow but produced no textual output.";

    bool hasReasoning = trimmed.Contains("Reasoning:", StringComparison.OrdinalIgnoreCase);
    bool hasAnswer = trimmed.Contains("Answer:", StringComparison.OrdinalIgnoreCase);
    if (hasReasoning && hasAnswer)
        return trimmed;

    string[] latestObservations = observations
        .TakeLast(3)
        .ToArray();
    if (latestObservations.Length == 0)
        latestObservations = ["Tool-assisted reasoning completed."];

    string observationBullets = string.Join('\n', latestObservations.Select(observation => $"- {observation}"));
    return $"""
Reasoning:
{observationBullets}
- Final response generated after tool execution and verification.

Answer:
{trimmed}
""";
}

static bool ShouldInjectReminder(int iteration, int maxIterations, int toolCallCount, int maxToolCalls, TimeSpan elapsed, TimeSpan maxWallTime)
{
    return iteration >= maxIterations - 6
        || toolCallCount >= maxToolCalls - 12
        || elapsed >= maxWallTime - TimeSpan.FromSeconds(45);
}

static string BuildForcedFinalAnswer(
    IReadOnlyList<string> observations,
    int toolCalls,
    TimeSpan elapsed,
    int maxIterations,
    int maxToolCalls,
    TimeSpan maxWallTime)
{
    string lastObservation = observations.Count > 0 ? observations[^1] : "No tool observation was captured.";
    return $"""
Reasoning:
- The agent loop reached its safety budget before `agent_finish` was called.
- Last observation: {lastObservation}
- Budget usage: toolCalls={toolCalls}/{maxToolCalls}, elapsed={elapsed.TotalSeconds:F1}s/{maxWallTime.TotalSeconds:F1}s, maxIterations={maxIterations}.

Answer:
I could not complete this request within the configured tool budget. Ask me to retry and I will continue with a fresh loop.
""";
}

static string SummarizeObservation(string toolName, string toolResultText, bool isError)
{
    string status = isError ? "error" : "ok";
    return $"{toolName} => {status}: {Truncate(toolResultText.Replace('\n', ' '), 180)}";
}

static string Truncate(string text, int maxLength)
{
    if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        return text;
    return text[..maxLength] + "...";
}

static async Task StreamFinalAnswer(HttpResponse response, string finalText, CancellationToken cancellationToken)
{
    string text = finalText.Trim();
    if (text.Length == 0)
        text = "I completed the request but no final text was generated.";

    MatchCollection tokens = Regex.Matches(text, @"\S+\s*", RegexOptions.CultureInvariant);
    if (tokens.Count == 0)
    {
        await WriteEvent(response, "token", new { text }, cancellationToken);
        await WriteEvent(response, "final", new { text }, cancellationToken);
        return;
    }

    const int wordsPerChunk = 10;
    StringBuilder chunk = new();
    int words = 0;

    foreach (Match token in tokens.Cast<Match>())
    {
        chunk.Append(token.Value);
        words++;
        if (words >= wordsPerChunk)
        {
            await WriteEvent(response, "token", new { text = chunk.ToString() }, cancellationToken);
            chunk.Clear();
            words = 0;
        }
    }

    if (chunk.Length > 0)
        await WriteEvent(response, "token", new { text = chunk.ToString() }, cancellationToken);

    await WriteEvent(response, "final", new { text }, cancellationToken);
}

static int GetBoundedInt(string envName, int fallback, int min, int max)
{
    string? raw = Environment.GetEnvironmentVariable(envName);
    if (!int.TryParse(raw, out int parsed))
        return fallback;
    return Math.Clamp(parsed, min, max);
}

static async Task WriteEvent(HttpResponse response, string eventName, object payload, CancellationToken cancellationToken)
{
    string json = JsonSerializer.Serialize(payload);
    await response.WriteAsync($"event: {eventName}\n", cancellationToken);
    await response.WriteAsync($"data: {json}\n\n", cancellationToken);
    await response.Body.FlushAsync(cancellationToken);
}

public sealed class ChatStreamRequest
{
    public List<ChatMessage>? Messages { get; set; }
}

public sealed class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class TodoEntry
{
    public required string Id { get; init; }
    public required int Order { get; init; }
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? Notes { get; set; }
}
