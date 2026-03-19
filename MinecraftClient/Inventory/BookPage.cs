using System.Collections.Generic;

namespace MinecraftClient.Inventory;

public record BookPage(
    string RawContent,
    bool HasFilteredContent,
    string? FilteredContent,
    Dictionary<string, object>? RawContentNbt = null,
    Dictionary<string, object>? FilteredContentNbt = null);