using System;
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
    /// Returns a <see cref="JsonValue"/> wrapping the raw string when the input
    /// is not valid JSON (e.g. a plain-text Minecraft MOTD or chat message).
    /// </summary>
    public static JsonNode? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        ReadOnlySpan<char> text = json.AsSpan().TrimStart();
        if (!LooksLikeJson(text))
            return JsonValue.Create(json);

        try { return JsonNode.Parse(json); }
        catch (JsonException) { return JsonValue.Create(json); }
    }

    private static bool LooksLikeJson(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
            return false;

        return text[0] switch
        {
            '{' or '"' => true,
            '[' => LooksLikeJsonArray(text[1..]),
            '-' => text.Length > 1 && char.IsAsciiDigit(text[1]),
            >= '0' and <= '9' => true,
            't' or 'f' or 'n' => true,
            _ => false
        };
    }

    private static bool LooksLikeJsonArray(ReadOnlySpan<char> text)
    {
        text = text.TrimStart();
        if (text.IsEmpty)
            return false;

        return text[0] switch
        {
            ']' or '{' or '[' or '"' => true,
            '-' => text.Length > 1 && char.IsAsciiDigit(text[1]),
            >= '0' and <= '9' => true,
            't' or 'f' or 'n' => true,
            _ => false
        };
    }

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
