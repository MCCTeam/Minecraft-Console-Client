using System.Text;
using System.Text.Json;
using System.Reflection;
using DebugTools.MccMcpWebPlayground.Harness;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DebugTools.MccMcpWebPlayground.Infrastructure.Mcp;

public sealed class MccMcpSessionFactory
{
    private readonly MccWebHarnessOptions options;

    public MccMcpSessionFactory(IOptions<MccWebHarnessOptions> options)
    {
        this.options = options.Value;
    }

    public async Task<McpClient> CreateAsync(CancellationToken cancellationToken)
    {
        string endpoint = options.ResolveMcpEndpoint();
        string? token = options.ResolveMcpAuthToken();

        return await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(endpoint),
            TransportMode = HttpTransportMode.AutoDetect,
            AdditionalHeaders = string.IsNullOrWhiteSpace(token)
                ? null
                : new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {token}"
                }
        }), cancellationToken: cancellationToken);
    }
}

public static class MccMcpJson
{
    public static MccNormalizedToolResult Normalize(CallToolResult result)
    {
        JsonElement? structuredRoot = TryReadStructuredContent(result);
        string text = ReadToolResultText(result, structuredRoot);
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonElement parsedRoot = document.RootElement.Clone();
            JsonElement root = ShouldPreferStructuredRoot(parsedRoot, structuredRoot)
                ? structuredRoot!.Value
                : parsedRoot;
            JsonElement? data = root.TryGetProperty("data", out JsonElement dataElement)
                ? dataElement.Clone()
                : ShouldTreatRootAsData(root) ? root.Clone() : structuredRoot;
            bool success = root.TryGetProperty("success", out JsonElement successElement)
                ? successElement.ValueKind != JsonValueKind.False
                : result.IsError != true;
            string? errorCode = root.TryGetProperty("errorCode", out JsonElement errorCodeElement) && errorCodeElement.ValueKind == JsonValueKind.String
                ? errorCodeElement.GetString()
                : null;
            string? message = root.TryGetProperty("message", out JsonElement messageElement) && messageElement.ValueKind == JsonValueKind.String
                ? messageElement.GetString()
                : null;
            bool isError = result.IsError == true || !success || !string.IsNullOrWhiteSpace(errorCode);

            return new MccNormalizedToolResult(text, isError, success, errorCode, message, root, data);
        }
        catch
        {
            bool isError = result.IsError == true;
            return new MccNormalizedToolResult(text, isError, !isError, null, null, structuredRoot, structuredRoot);
        }
    }

    private static string ReadToolResultText(CallToolResult result, JsonElement? structuredRoot)
    {
        if (result.Content is null)
            return structuredRoot?.GetRawText() ?? (result.IsError == true ? "{\"success\":false}" : "{\"success\":true}");

        StringBuilder builder = new();
        foreach (ContentBlock block in result.Content)
        {
            if (block is TextContentBlock text && !string.IsNullOrWhiteSpace(text.Text))
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(text.Text);
            }
        }

        return builder.Length > 0
            ? builder.ToString()
            : structuredRoot?.GetRawText()
                ?? JsonSerializer.Serialize(new { success = result.IsError != true, isError = result.IsError });
    }

    private static JsonElement? TryReadStructuredContent(CallToolResult result)
    {
        PropertyInfo? property = typeof(CallToolResult).GetProperty("StructuredContent", BindingFlags.Instance | BindingFlags.Public);
        if (property?.GetValue(result) is not { } value)
            return null;

        return value switch
        {
            JsonElement json when json.ValueKind != JsonValueKind.Undefined && json.ValueKind != JsonValueKind.Null => json.Clone(),
            JsonDocument document => document.RootElement.Clone(),
            string text when !string.IsNullOrWhiteSpace(text) => TryParseJson(text),
            _ => TrySerializeToJson(value)
        };
    }

    private static JsonElement? TrySerializeToJson(object value)
    {
        try
        {
            return JsonSerializer.SerializeToElement(value);
        }
        catch
        {
            return null;
        }
    }

    private static JsonElement? TryParseJson(string text)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }

    private static bool ShouldPreferStructuredRoot(JsonElement parsedRoot, JsonElement? structuredRoot)
    {
        if (structuredRoot is null)
            return false;

        if (parsedRoot.ValueKind != JsonValueKind.Object)
            return true;

        return !parsedRoot.EnumerateObject().Any(property =>
            !property.NameEquals("success") &&
            !property.NameEquals("isError"));
    }

    private static bool ShouldTreatRootAsData(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        return root.EnumerateObject().Any(property =>
            !property.NameEquals("success") &&
            !property.NameEquals("isError") &&
            !property.NameEquals("errorCode") &&
            !property.NameEquals("message"));
    }
}

public static class MccJsonArguments
{
    public static Dictionary<string, object?> Parse(string rawJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, object?>();

            Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
                values[property.Name] = Convert(property.Value);
            return values;
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    private static object? Convert(JsonElement element)
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
            JsonValueKind.Array => element.EnumerateArray().Select(Convert).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => Convert(property.Value)),
            _ => element.GetRawText()
        };
    }
}
