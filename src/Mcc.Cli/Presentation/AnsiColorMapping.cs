using System.Globalization;
using Umpk.Text;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Maps a Minecraft <see cref="TextColor"/> to the SGR parameter fragment for a given <see cref="ConsoleColorDepth"/>: unconditional 24-bit truecolor, the xterm 256-color palette (8-bit), or the nearest of the 16 basic ANSI colors (4-bit; legacy_4bit and vt100_4bit share this path, see <see cref="ConsoleColorDepth"/>).
/// The nearest-color search, its perceptual RGB distance (a "redmean" approximation weighted by the mean red channel), and the 8-bit cube/grayscale palette are ported from legacy's <c>ColorHelper.GetColorEscapeCode</c> and <c>ColorRGBA.Distance</c> (MinecraftClient/ColorHelper.cs:81-203), foreground-only (Minecraft chat text carries no background color).
/// </summary>
internal static class AnsiColorMapping
{
    // The 16 basic ANSI colors, RGB-tagged so nearest-color search can pick among them, with their foreground SGR codes.
    // RGB anchors and codes ported verbatim from legacy's ColorHelper.ColorMap4 (which stores the background codes 40-47/100-107; foreground = background - 10, applied here directly).
    private static readonly (int R, int G, int B, int Sgr)[] Basic16 =
    [
        (0, 0, 0, 30),
        (128, 0, 0, 31),
        (0, 128, 0, 32),
        (151, 109, 77, 33),
        (45, 45, 180, 34),
        (128, 0, 128, 35),
        (138, 138, 220, 36),
        (174, 164, 115, 37),
        (96, 96, 96, 90),
        (255, 0, 0, 91),
        (127, 178, 56, 92),
        (213, 201, 140, 93),
        (64, 64, 255, 94),
        (255, 0, 255, 95),
        (0, 255, 255, 96),
        (255, 255, 255, 97),
    ];

    // xterm 256-color palette candidates: the 16 basic colors (indices 0-15, standard ANSI RGB anchors here, not the Basic16 table above) plus the 24-step grayscale ramp (232-255).
    // Ported verbatim from legacy's ColorHelper.ColorMap8.
    private static readonly (int R, int G, int B, int Index)[] Palette256 =
    [
        (0, 0, 0, 0), (128, 0, 0, 1), (0, 128, 0, 2), (128, 128, 0, 3),
        (0, 0, 128, 4), (128, 0, 128, 5), (0, 128, 128, 6), (192, 192, 192, 7),
        (128, 128, 128, 8), (255, 0, 0, 9), (0, 255, 0, 10), (255, 255, 0, 11),
        (0, 0, 255, 12), (255, 0, 255, 13), (0, 255, 255, 14), (255, 255, 255, 15),
        (8, 8, 8, 232), (18, 18, 18, 233), (28, 28, 28, 234), (38, 38, 38, 235),
        (48, 48, 48, 236), (58, 58, 58, 237), (68, 68, 68, 238), (78, 78, 78, 239),
        (88, 88, 88, 240), (98, 98, 98, 241), (108, 108, 108, 242), (118, 118, 118, 243),
        (128, 128, 128, 244), (138, 138, 138, 245), (148, 148, 148, 246), (158, 158, 158, 247),
        (168, 168, 168, 248), (178, 178, 178, 249), (188, 188, 188, 250), (198, 198, 198, 251),
        (208, 208, 208, 252), (218, 218, 218, 253), (228, 228, 228, 254), (238, 238, 238, 255),
    ];

    // The 6-step per-channel cube boundaries the xterm 216-color cube uses (0, 95, 135, 175, 215, 255).
    private static readonly int[] CubeStep = [0, 95, 135, 175, 215, 255];

    /// <summary>
    /// The SGR parameter fragment for <paramref name="color"/> at <paramref name="depth"/>: no ESC or brackets, e.g. <c>"38;2;255;85;85"</c>, <c>"38;5;196"</c>, or <c>"91"</c>.
    /// Empty for <see cref="ConsoleColorDepth.Disable"/>.
    /// </summary>
    public static string Sgr(TextColor color, ConsoleColorDepth depth)
    {
        int rgb = color.Rgb;
        int r = (rgb >> 16) & 0xFF;
        int g = (rgb >> 8) & 0xFF;
        int b = rgb & 0xFF;

        return depth switch
        {
            ConsoleColorDepth.Disable => string.Empty,
            ConsoleColorDepth.Legacy4Bit or ConsoleColorDepth.Vt1004Bit =>
                Nearest4Bit(r, g, b).ToString(CultureInfo.InvariantCulture),
            ConsoleColorDepth.Vt1008Bit => string.Create(CultureInfo.InvariantCulture, $"38;5;{Nearest8Bit(r, g, b)}"),
            _ => string.Create(CultureInfo.InvariantCulture, $"38;2;{r};{g};{b}"),
        };
    }

    private static int Nearest4Bit(int r, int g, int b)
    {
        int best = 0;
        double bestDistance = Distance(r, g, b, Basic16[0].R, Basic16[0].G, Basic16[0].B);
        for (int i = 1; i < Basic16.Length; i++)
        {
            double distance = Distance(r, g, b, Basic16[i].R, Basic16[i].G, Basic16[i].B);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return Basic16[best].Sgr;
    }

    private static int Nearest8Bit(int r, int g, int b)
    {
        // The cube-index estimate legacy computes before scanning the fixed palette, kept here as the fallback candidate for when nothing in the fixed 40-entry palette beats it (ColorHelper.cs:139-155).
        int rIdx = CubeIndex(r);
        int gIdx = CubeIndex(g);
        int bIdx = CubeIndex(b);
        double bestDistance = Distance(r, g, b, CubeStep[rIdx], CubeStep[gIdx], CubeStep[bIdx]);

        int best = -1;
        for (int i = 0; i < Palette256.Length; i++)
        {
            double distance = Distance(r, g, b, Palette256[i].R, Palette256[i].G, Palette256[i].B);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best == -1 ? 16 + (36 * rIdx) + (6 * gIdx) + bIdx : Palette256[best].Index;
    }

    private static int CubeIndex(int channel) => channel <= 95
        ? (int)Math.Round(channel / 95.0)
        : (int)(1 + Math.Round((channel - 95.0) / 40.0));

    // The "redmean" perceptual RGB distance legacy uses (ColorRGBA.Distance, ColorHelper.cs:196-203): weights green highest and red/blue by the mean red channel, approximating human color perception better than plain Euclidean RGB distance.
    private static double Distance(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        double meanR = (r1 + r2) / 2.0;
        double dr = r1 - r2;
        double dg = g1 - g2;
        double db = b1 - b2;
        return Math.Sqrt(((2.0 + (meanR / 256.0)) * dr * dr) + (4.0 * dg * dg) + ((2.0 + ((255 - meanR) / 256.0)) * db * db));
    }
}
