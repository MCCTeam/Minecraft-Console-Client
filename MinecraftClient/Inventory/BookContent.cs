using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Inventory;

public enum BookHand
{
    Main = 0,
    Off = 1
}

public sealed record BookLimits(int MaxPages, int MaxPageLength, int MaxTitleLength)
{
    public static BookLimits ForProtocol(int protocolVersion)
    {
        int maxPageLength = protocolVersion switch
        {
            >= Protocol18Handler.MC_1_21_2_Version => 1024,
            >= Protocol18Handler.MC_1_17_Version => 8192,
            _ => 32767
        };

        int maxTitleLength = protocolVersion switch
        {
            >= Protocol18Handler.MC_1_21_2_Version => 32,
            >= Protocol18Handler.MC_1_17_Version => 128,
            _ => 16
        };

        return new BookLimits(100, maxPageLength, maxTitleLength);
    }
}

public sealed record BookContent(
    IReadOnlyList<string> Pages,
    string? Title,
    string? Author,
    int Generation,
    bool IsSigned)
{
    public static BookContent EmptyWritable { get; } = new([string.Empty], null, null, 0, false);
}

public static class BookContentHelper
{
    public static bool IsBook(Item? item) => item?.Type is ItemType.WritableBook or ItemType.WrittenBook;

    public static bool IsWritableBook(Item? item) => item?.Type == ItemType.WritableBook;

    public static bool TryRead(Item? item, out BookContent content)
    {
        content = BookContent.EmptyWritable;

        if (item is null || item.IsEmpty)
            return false;

        return item.Type switch
        {
            ItemType.WritableBook => TryReadWritable(item, out content),
            ItemType.WrittenBook => TryReadWritten(item, out content),
            _ => false
        };
    }

    public static Item CreateWritablePayload(Item currentBook, IReadOnlyList<string> pages)
    {
        return new Item(ItemType.WritableBook, 1, currentBook.Data, new Dictionary<string, object>
        {
            ["pages"] = pages.Cast<object>().ToArray()
        });
    }

    public static Item CreateWrittenPayload(Item currentBook, IReadOnlyList<string> pages, string title, string author, bool encodePagesAsJson)
    {
        object[] encodedPages = pages
            .Select(page => encodePagesAsJson ? ToJsonTextComponent(page) : page)
            .Cast<object>()
            .ToArray();

        return new Item(ItemType.WrittenBook, 1, currentBook.Data, new Dictionary<string, object>
        {
            ["author"] = author,
            ["title"] = title,
            ["pages"] = encodedPages
        });
    }

    public static IReadOnlyList<string> NormalizePages(IEnumerable<string> pages)
    {
        string[] normalized = pages.Select(page => page ?? string.Empty).ToArray();
        return normalized.Length == 0 ? [string.Empty] : normalized;
    }

    private static bool TryReadWritable(Item item, out BookContent content)
    {
        if (item.Components is not null)
        {
            var component = item.Components.OfType<WritableBlookContentComponent>().FirstOrDefault();
            if (component is not null)
            {
                content = new BookContent(
                    NormalizePages(component.Pages.Select(page => page.RawContent)),
                    null,
                    null,
                    0,
                    IsSigned: false);
                return true;
            }
        }

        content = new BookContent(ReadStringList(item.NBT, "pages", parseJson: false), null, null, 0, IsSigned: false);
        return true;
    }

    private static bool TryReadWritten(Item item, out BookContent content)
    {
        if (item.Components is not null)
        {
            var component = item.Components.OfType<WrittenBlookContentComponent>().FirstOrDefault();
            if (component is not null)
            {
                content = new BookContent(
                    NormalizePages(component.Pages.Select(page => page.RawContent)),
                    component.RawTitle,
                    component.Author,
                    component.Generation,
                    IsSigned: true);
                return true;
            }
        }

        string? title = ReadString(item.NBT, "title");
        string? author = ReadString(item.NBT, "author");
        int generation = ReadInt(item.NBT, "generation");
        content = new BookContent(ReadStringList(item.NBT, "pages", parseJson: true), title, author, generation, IsSigned: true);
        return true;
    }

    private static IReadOnlyList<string> ReadStringList(Dictionary<string, object>? nbt, string key, bool parseJson)
    {
        if (nbt is null || !nbt.TryGetValue(key, out object? value) || value is not object[] values)
            return [string.Empty];

        string[] pages = values
            .Select(value => value?.ToString() ?? string.Empty)
            .Select(value => parseJson ? ChatParser.ParseText(value) : value)
            .ToArray();

        return pages.Length == 0 ? [string.Empty] : pages;
    }

    private static string? ReadString(Dictionary<string, object>? nbt, string key)
    {
        return nbt is not null && nbt.TryGetValue(key, out object? value)
            ? value?.ToString()
            : null;
    }

    private static int ReadInt(Dictionary<string, object>? nbt, string key)
    {
        if (nbt is null || !nbt.TryGetValue(key, out object? value) || value is null)
            return 0;

        return value switch
        {
            int i => i,
            short s => s,
            byte b => b,
            _ when int.TryParse(value.ToString(), out int parsed) => parsed,
            _ => 0
        };
    }

    private static string ToJsonTextComponent(string text)
    {
        return JsonSerializer.Serialize(new Dictionary<string, string> { ["text"] = text });
    }
}
