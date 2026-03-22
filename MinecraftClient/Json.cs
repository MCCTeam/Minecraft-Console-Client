using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MinecraftClient;

/// <summary>
/// JSON utilities backed by System.Text.Json.
/// </summary>
public static class Json
{
    private static readonly JsonSerializerOptions s_escapeOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Parse a JSON string into a mutable <see cref="JsonNode"/> DOM.
    /// Returns null for null, empty, or whitespace-only input.
    /// </summary>
    public static JsonNode? ParseJson(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json);

    /// <summary>
    /// Escape a string for embedding inside a JSON string literal.
    /// Uses System.Text.Json serialization and strips the surrounding quotes.
    /// </summary>
    public static string EscapeString(string src) =>
        JsonSerializer.Serialize(src, s_escapeOptions)[1..^1];
}

/// <summary>
/// Extension helpers for <see cref="JsonNode"/> that replicate the access patterns
/// of the former <c>JSONData.StringValue</c> property.
/// </summary>
public static class JsonNodeExtensions
{
    /// <summary>
    /// Return the string representation of any JSON value.
    /// Strings are returned without quotes; numbers, booleans, and null
    /// are returned as their text representation.
    /// </summary>
    public static string GetStringValue(this JsonNode? node) => node switch
    {
        null => "null",
        JsonValue val when val.TryGetValue<string>(out var s) => s,
        _ => node.ToJsonString()
    };
}