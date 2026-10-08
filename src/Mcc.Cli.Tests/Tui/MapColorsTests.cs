using Avalonia.Media;
using Mcc.Cli.Tui.Map;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

/// <summary>
/// Unit tests for the pure vanilla map-color palette decoder: base-color lookup and shade multiplier, ported from legacy's <c>ChatBots/Map.cs</c> <c>MapColors</c>.
/// No session dependency.
/// </summary>
public sealed class MapColorsTests
{
    [Fact]
    public void PaletteCount_LoadsEmbeddedTable()
        => Assert.True(MapColors.PaletteCount > 50, $"expected a populated map palette, got {MapColors.PaletteCount}");

    [Fact]
    public void ColorByteToColor_FullBrightnessMatchesBasePaletteEntry()
    {
        // Base color index 1 (grass) is [127, 178, 56] in MinimapBlockColors.json's map_palette; shade index 2 (the low two bits) is the "full brightness" (255/255) multiplier, so the decoded color is exact.
        byte value = (1 << 2) | 2;
        Color color = MapColors.ColorByteToColor(value);
        Assert.Equal(Color.FromRgb(127, 178, 56), color);
    }

    [Fact]
    public void ColorByteToColor_LowShadeIsDarkerThanHighShade()
    {
        byte darkest = (1 << 2) | 3; // shade index 3 = 135/255
        byte brightest = (1 << 2) | 2; // shade index 2 = 255/255
        Color dark = MapColors.ColorByteToColor(darkest);
        Color bright = MapColors.ColorByteToColor(brightest);
        Assert.True(dark.R < bright.R);
        Assert.True(dark.G < bright.G);
        Assert.True(dark.B < bright.B);
    }

    [Fact]
    public void ColorByteToColor_UnknownBaseIndexFallsBackToUnknownColor()
    {
        // Base index 63 (value >> 2 = 63, i.e. value 252) is past every generated palette entry.
        Color color = MapColors.ColorByteToColor(252);
        Assert.Equal(MapColors.UnknownColor, color);
    }
}
