using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.Scripting;

/// <summary>
/// Represents a world coordinate rounded for tool and script consumption.
/// </summary>
public sealed class MccCoordinate
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
}

/// <summary>
/// Represents a block state snapshot.
/// </summary>
public sealed class MccBlockStateSnapshot
{
    public required string Material { get; init; }
    public required string TypeLabel { get; init; }
    public required int BlockId { get; init; }
    public required int BlockMeta { get; init; }
}

/// <summary>
/// Represents a simple item stack snapshot.
/// </summary>
public sealed class MccItemStackSnapshot
{
    public required string Type { get; init; }
    public required int Count { get; init; }
}

/// <summary>
/// Shared normalization and formatting helpers for gameplay operations.
/// </summary>
public static class MccGameCommon
{
    private const int CoordinateRoundingPrecision = 2;

    public static bool TryParseItemType(string rawItemType, out ItemType itemType)
    {
        if (Enum.TryParse(rawItemType, true, out itemType) && itemType is not (ItemType.Unknown or ItemType.Null))
            return true;

        string normalized = NormalizeToken(rawItemType);
        if (normalized.Length == 0)
        {
            itemType = ItemType.Unknown;
            return false;
        }

        foreach (ItemType candidate in Enum.GetValues<ItemType>())
        {
            if (candidate is ItemType.Unknown or ItemType.Null)
                continue;

            if (NormalizeToken(candidate.ToString()) == normalized)
            {
                itemType = candidate;
                return true;
            }
        }

        itemType = ItemType.Unknown;
        return false;
    }

    public static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        char[] buffer = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(buffer);
    }

    public static bool TextEqualsFilter(string text, string filter)
    {
        return text.Equals(filter, StringComparison.OrdinalIgnoreCase)
            || NormalizeToken(text) == NormalizeToken(filter);
    }

    public static bool TextMatchesFilter(string text, string filter)
    {
        if (text.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;

        string normalizedFilter = NormalizeToken(filter);
        if (normalizedFilter.Length == 0)
            return false;

        return NormalizeToken(text).Contains(normalizedFilter, StringComparison.Ordinal);
    }

    public static double GetDistance(Location from, Location to)
    {
        double dx = from.X - to.X;
        double dy = from.Y - to.Y;
        double dz = from.Z - to.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static double RoundCoordinate(double value)
    {
        return Math.Round(value, CoordinateRoundingPrecision, MidpointRounding.AwayFromZero);
    }

    public static MccCoordinate ToCoordinate(Location location)
    {
        return ToCoordinate(location.X, location.Y, location.Z);
    }

    public static MccCoordinate ToCoordinate(double x, double y, double z)
    {
        return new MccCoordinate
        {
            X = RoundCoordinate(x),
            Y = RoundCoordinate(y),
            Z = RoundCoordinate(z)
        };
    }

    public static Location ToBlockLocation(double x, double y, double z)
    {
        return new Location(Math.Floor(x), Math.Floor(y), Math.Floor(z));
    }

    public static MccBlockStateSnapshot ToBlockState(Block block)
    {
        return new MccBlockStateSnapshot
        {
            Material = block.Type.ToString(),
            TypeLabel = block.GetTypeString(),
            BlockId = block.BlockId,
            BlockMeta = block.BlockMeta
        };
    }

    public static object? DescribeMetadataValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            bool b => b,
            byte b => b,
            sbyte b => b,
            short s => s,
            ushort s => s,
            int i => i,
            uint i => i,
            long l => l,
            ulong l => l,
            float f => f,
            double d => d,
            decimal d => d,
            Enum e => e.ToString(),
            Location location => ToCoordinate(location),
            Item item => ToItemStack(item),
            byte[] data => new { bytes = data.Length },
            _ => value.ToString()
        };
    }

    public static MccItemStackSnapshot ToItemStack(Item item)
    {
        return new MccItemStackSnapshot
        {
            Type = item.Type.ToString(),
            Count = item.Count
        };
    }

    public static bool TryParseBlockQuery(string? query, out int? blockId, out int? blockMeta)
    {
        blockId = null;
        blockMeta = null;
        if (string.IsNullOrWhiteSpace(query))
            return false;

        string trimmed = query.Trim();
        int separator = trimmed.IndexOf(':');
        if (separator >= 0)
        {
            string idPart = trimmed[..separator].Trim();
            string metaPart = trimmed[(separator + 1)..].Trim();
            if (int.TryParse(idPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedId))
            {
                blockId = parsedId;
                if (int.TryParse(metaPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMeta))
                    blockMeta = parsedMeta;
                return true;
            }

            return false;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int blockStateId))
        {
            blockId = blockStateId;
            return true;
        }

        return false;
    }
}
