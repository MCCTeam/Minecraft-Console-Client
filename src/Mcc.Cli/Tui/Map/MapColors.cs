using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Map;

/// <summary>
/// The vanilla map-item color palette: turns one raw map pixel byte (a base-color index in the high six bits, a shade multiplier in the low two) into an RGB color.
/// Loaded from the <c>map_palette</c> section of the same embedded <c>MinimapBlockColors.json</c> resource the minimap block-color table (<see cref="Mcc.Cli.Tui.Minimap.MinimapColorMap"/>) already reads, ported from legacy's <c>ChatBots/Map.cs</c> <c>MapColors</c>.
/// Pure and unit-testable (no session dependency at rest).
/// </summary>
internal static class MapColors
{
    /// <summary>The four vanilla map shade multipliers (low two bits of the color byte), out of 255.</summary>
    private static readonly byte[] ShadeMultipliers = [180, 220, 255, 135];

    /// <summary>The color rendered for a base-color index the palette does not carry (a bright debug magenta).</summary>
    public static readonly Color UnknownColor = Color.FromRgb(248, 0, 248);

    private static readonly FrozenDictionary<byte, (byte R, byte G, byte B)> Palette;

    static MapColors()
    {
        var colors = new Dictionary<byte, (byte, byte, byte)>();
        try
        {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Mcc.Cli.Tui.Minimap.MinimapBlockColors.json");
            if (stream is not null)
            {
                using JsonDocument document = JsonDocument.Parse(stream);
                if (document.RootElement.TryGetProperty("map_palette", out JsonElement palette))
                {
                    foreach (JsonProperty property in palette.EnumerateObject())
                    {
                        if (!byte.TryParse(property.Name, out byte id) || property.Value.GetArrayLength() < 3)
                            continue;

                        JsonElement.ArrayEnumerator channels = property.Value.EnumerateArray();
                        channels.MoveNext();
                        byte r = (byte)Math.Clamp(channels.Current.GetInt32(), 0, 255);
                        channels.MoveNext();
                        byte g = (byte)Math.Clamp(channels.Current.GetInt32(), 0, 255);
                        channels.MoveNext();
                        byte b = (byte)Math.Clamp(channels.Current.GetInt32(), 0, 255);
                        colors[id] = (r, g, b);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Corrupt/absent resource: every pixel falls back to UnknownColor below.
        }

        Palette = colors.ToFrozenDictionary();
    }

    /// <summary>The number of base colors loaded (for diagnostics/tests).</summary>
    public static int PaletteCount => Palette.Count;

    /// <summary>
    /// Decodes one raw map pixel byte into its RGB color: the base-color index is the high six bits (<c>value &gt;&gt; 2</c>) looked up in the vanilla <c>MapColor</c> table, shaded by one of four brightness multipliers selected by the low two bits.
    /// An index the table does not carry (for example the reserved/unused entries) renders as <see cref="UnknownColor"/>, matching legacy's debug marker.
    /// </summary>
    public static Color ColorByteToColor(byte value)
    {
        byte baseIndex = (byte)(value >> 2);
        if (!Palette.TryGetValue(baseIndex, out (byte R, byte G, byte B) rgb))
            return UnknownColor;

        byte multiplier = ShadeMultipliers[value & 3];
        return Color.FromRgb(
            (byte)(rgb.R * multiplier / 255),
            (byte)(rgb.G * multiplier / 255),
            (byte)(rgb.B * multiplier / 255));
    }
}
