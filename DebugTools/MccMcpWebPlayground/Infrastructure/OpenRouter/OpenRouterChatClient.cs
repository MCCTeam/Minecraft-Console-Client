using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DebugTools.MccMcpWebPlayground.Harness;

namespace DebugTools.MccMcpWebPlayground.Infrastructure.OpenRouter;

public sealed class OpenRouterChatClient
{
    private readonly IHttpClientFactory httpClientFactory;

    public OpenRouterChatClient(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<MccModelTurn> CreateTurnAsync(
        List<object> messages,
        IReadOnlyList<object> tools,
        MccWebHarnessOptions options,
        CancellationToken cancellationToken)
    {
        string apiKey = options.ResolveApiKey() ?? throw new InvalidOperationException("OPENROUTER_API_KEY is not configured.");
        string model = options.ResolveModel() ?? throw new InvalidOperationException("Model is not configured.");

        using HttpClient client = httpClientFactory.CreateClient("openrouter");
        client.BaseAddress = new Uri(options.ResolveOpenRouterBaseUrl().TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", "https://localhost/mcc-mcp-web-playground");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", "MCC MCP Web Playground");

        Dictionary<string, object?> payload = new()
        {
            ["model"] = model,
            ["messages"] = messages,
            ["tools"] = tools,
            ["tool_choice"] = "auto",
            ["provider"] = new Dictionary<string, object?>
            {
                ["allow_fallbacks"] = options.AllowFallbacks,
                ["require_parameters"] = options.RequireProviderParameters
            }
        };

        if (ShouldSendParallelToolCallsParameter(model))
            payload["parallel_tool_calls"] = !options.DisableParallelToolCalls;

        using HttpResponseMessage response = await client.PostAsync(
            "chat/completions",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenRouter returned HTTP {(int)response.StatusCode}: {body}");

        using JsonDocument document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("choices", out JsonElement choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("OpenRouter did not return any choices.");
        }

        JsonElement message = choices[0].GetProperty("message");
        string assistantContent = message.TryGetProperty("content", out JsonElement contentElement)
            ? contentElement.GetString() ?? string.Empty
            : string.Empty;

        List<MccModelToolCall> toolCalls = [];
        if (message.TryGetProperty("tool_calls", out JsonElement toolCallsElement) && toolCallsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement toolCall in toolCallsElement.EnumerateArray())
            {
                if (!toolCall.TryGetProperty("id", out JsonElement idElement)
                    || !toolCall.TryGetProperty("function", out JsonElement functionElement)
                    || !functionElement.TryGetProperty("name", out JsonElement nameElement))
                {
                    continue;
                }

                toolCalls.Add(new MccModelToolCall(
                    CallId: idElement.GetString() ?? Guid.NewGuid().ToString("n"),
                    Name: nameElement.GetString() ?? string.Empty,
                    ArgumentsJson: functionElement.TryGetProperty("arguments", out JsonElement argumentsElement)
                        ? argumentsElement.GetString() ?? "{}"
                        : "{}"));
            }
        }

        string modelId = document.RootElement.TryGetProperty("model", out JsonElement modelElement)
            ? modelElement.GetString() ?? model
            : model;

        string? routedProvider = response.Headers.TryGetValues("x-openrouter-provider", out IEnumerable<string>? providerValues)
            ? providerValues.FirstOrDefault()
            : null;

        return new MccModelTurn(modelId, routedProvider, assistantContent, toolCalls);
    }

    private static bool ShouldSendParallelToolCallsParameter(string model)
    {
        // Some OpenRouter model families reject tool-enabled requests when the parallel_tool_calls
        // parameter is present at all, even if it is explicitly set to false. The harness still
        // executes all returned tool calls sequentially, so omitting the transport hint for those
        // families preserves the intended runtime behavior while keeping the stricter flag for
        // compatible models.
        return !model.StartsWith("minimax/", StringComparison.OrdinalIgnoreCase)
            && !model.StartsWith("google/gemini-", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record MccModelTurn(
    string ModelId,
    string? RoutedProvider,
    string AssistantContent,
    IReadOnlyList<MccModelToolCall> ToolCalls);

public sealed record MccModelToolCall(string CallId, string Name, string ArgumentsJson);
