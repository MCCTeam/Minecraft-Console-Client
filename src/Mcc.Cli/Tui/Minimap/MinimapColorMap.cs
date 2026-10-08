using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Minimap;

/// <summary>
/// The block-color table for the minimap, loaded from the embedded <c>MinimapBlockColors.json</c> (ported from the legacy tree).
/// Keyed by the PascalCase block key (<see cref="RegistryKey.ToPascal"/>) since the generated data uses MCC <c>Material</c> enum names while the core exposes namespaced block ids.
/// Provides base colors, transparency/water/ice classification, height shading and simple blending, mirroring the legacy <c>MinimapColorMap</c>.
/// Pure and unit-testable (no Consolonia/session dependency at rest).
/// </summary>
internal static class MinimapColorMap
{
    public static readonly Color WaterColor = Color.FromRgb(50, 80, 180);
    public static readonly Color IceColor = Color.FromRgb(140, 170, 220);
    public static readonly Color VoidColor = Color.FromRgb(0, 0, 0);
    public static readonly Color UnknownColor = Color.FromRgb(120, 120, 120);

    private static readonly FrozenDictionary<string, Color> Colors;
    private static readonly FrozenSet<string> Transparent;
    private static readonly FrozenSet<string> Water;
    private static readonly FrozenSet<string> Ice;

    static MinimapColorMap()
    {
        var colors = new Dictionary<string, Color>(StringComparer.Ordinal);
        var transparent = new HashSet<string>(StringComparer.Ordinal) { "Air", "CaveAir", "VoidAir" };
        var water = new HashSet<string>(StringComparer.Ordinal) { "Water" };
        var ice = new HashSet<string>(StringComparer.Ordinal) { "Ice", "PackedIce", "BlueIce", "FrostedIce" };

        try
        {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Mcc.Cli.Tui.Minimap.MinimapBlockColors.json");
            if (stream is not null)
            {
                using JsonDocument document = JsonDocument.Parse(stream);
                JsonElement root = document.RootElement;
                if (root.TryGetProperty("colors", out JsonElement colorMap))
                {
                    foreach (JsonProperty property in colorMap.EnumerateObject())
                    {
                        if (TryReadColor(property.Value, out Color color))
                            colors[property.Name] = color;
                    }
                }

                ReadSet(root, "transparent", transparent);
                ReadSet(root, "water", water);
                ReadSet(root, "ice", ice);
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Corrupt/absent resource: fall back to the hardcoded transparent/water/ice defaults above.
        }

        Colors = colors.ToFrozenDictionary(StringComparer.Ordinal);
        Transparent = transparent.ToFrozenSet(StringComparer.Ordinal);
        Water = water.ToFrozenSet(StringComparer.Ordinal);
        Ice = ice.ToFrozenSet(StringComparer.Ordinal);
    }

    /// <summary>The number of colors loaded (for diagnostics/tests).</summary>
    public static int ColorCount => Colors.Count;

    /// <summary>True when the block id names a fully transparent block (air, glass, barrier, ...).</summary>
    public static bool IsTransparent(string blockId) => Transparent.Contains(RegistryKey.ToPascal(blockId));

    /// <summary>True when the block id names water.</summary>
    public static bool IsWater(string blockId) => Water.Contains(RegistryKey.ToPascal(blockId));

    /// <summary>True when the block id names an ice variant.</summary>
    public static bool IsIce(string blockId) => Ice.Contains(RegistryKey.ToPascal(blockId));

    /// <summary>The base color for a block id, or a neutral fallback when unmapped.</summary>
    public static Color GetBaseColor(string blockId)
    {
        if (IsWater(blockId))
            return WaterColor;

        if (IsIce(blockId))
            return IceColor;

        return Colors.TryGetValue(RegistryKey.ToPascal(blockId), out Color color) ? color : UnknownColor;
    }

    /// <summary>Applies a north-slope height shade (brighter uphill, darker downhill) to a color.</summary>
    public static Color ApplyHeightShade(Color color, int deltaY)
    {
        double factor = deltaY switch
        {
            > 0 => 1.0,
            0 => 0.86,
            _ => 0.70,
        };

        return Scale(color, factor);
    }

    /// <summary>
    /// Blends <see cref="WaterColor"/> over <paramref name="floorColor"/>, ported from legacy's <c>BlendWaterColor</c>: deeper water is more opaque blue (alpha rises with depth, capped at 0.85).
    /// The surface-region feed (<see cref="Umpk.Client.Snapshots.SurfaceColumn"/>) reports only the single resolved surface block, not a true floor-relative water column depth, so callers pass a nominal depth (the minimap uses a constant shallow depth) rather than a measured one; this is a data-availability limitation of the surface-region API, not of this blend function, which is exact given a depth.
    /// </summary>
    public static Color BlendWaterColor(Color floorColor, int waterDepth)
    {
        double alpha = Math.Min(0.85, 0.35 + (waterDepth * 0.08));
        return Blend(WaterColor, floorColor, alpha);
    }

    /// <summary>Blends <see cref="IceColor"/> over <paramref name="floorColor"/> at a fixed translucency, ported from legacy's <c>BlendIceColor</c>.</summary>
    public static Color BlendIceColor(Color floorColor) => Blend(IceColor, floorColor, 0.35);

    /// <summary>Darkens a color to simulate underground lighting (cave floors), ported from legacy's <c>ApplyCaveDarkening</c>.</summary>
    public static Color ApplyCaveDarkening(Color color, double factor = 0.55) => Scale(color, factor);

    private static Color Blend(Color top, Color bottom, double topAlpha)
        => Color.FromRgb(
            Clamp((top.R * topAlpha) + (bottom.R * (1.0 - topAlpha))),
            Clamp((top.G * topAlpha) + (bottom.G * (1.0 - topAlpha))),
            Clamp((top.B * topAlpha) + (bottom.B * (1.0 - topAlpha))));

    private static Color Scale(Color color, double factor)
        => Color.FromRgb(Clamp(color.R * factor), Clamp(color.G * factor), Clamp(color.B * factor));

    private static byte Clamp(double value) => (byte)Math.Clamp(value, 0, 255);

    private static void ReadSet(JsonElement root, string name, HashSet<string> into)
    {
        if (root.TryGetProperty(name, out JsonElement array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    into.Add(item.GetString()!);
            }
        }
    }

    private static bool TryReadColor(JsonElement element, out Color color)
    {
        color = default;
        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() < 3)
            return false;

        int i = 0;
        Span<byte> rgb = stackalloc byte[3];
        foreach (JsonElement channel in element.EnumerateArray())
        {
            if (i >= 3)
                break;

            rgb[i++] = (byte)Math.Clamp(channel.GetInt32(), 0, 255);
        }

        color = Color.FromRgb(rgb[0], rgb[1], rgb[2]);
        return true;
    }
}
