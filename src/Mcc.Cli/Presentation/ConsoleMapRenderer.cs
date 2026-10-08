using System.Globalization;
using System.Text;
using DMCBK.Core.Commands;
namespace Mcc.Cli.Presentation;

/// <summary>
/// The half-block console renderer.
/// Each terminal cell shows two stacked pixels through U+2580: the foreground paints the top pixel, the background the bottom one.
/// Colour escapes are emitted only when they change.
/// </summary>
public static class ConsoleMapRenderer
{
    /// <summary>U+2580 UPPER HALF BLOCK, written as an escape so the source stays pure ASCII.</summary>
    private const char UpperHalfBlock = '\u2580';

    /// <summary>Computes the integer downscale factor for a given terminal box.</summary>
    public static int ComputeScale(int width, int height, int consoleWidth, int consoleHeight)
    {
        int columns = Math.Max(1, consoleWidth) / 2;
        int rows = Math.Max(1, consoleHeight);
        int scaleX = (width + columns - 1) / Math.Max(1, columns);
        int scaleY = (height + rows - 1) / Math.Max(1, rows);
        return Math.Max(1, Math.Max(scaleX, scaleY));
    }

    /// <summary>
    /// Renders the map into an ANSI truecolour block.
    /// Returns the lines; the caller decides where they go.
    /// </summary>
    public static IReadOnlyList<string> Render(RgbImageFrame image, int scale)
    {
        ArgumentNullException.ThrowIfNull(image);
        scale = Math.Max(1, scale);

        var lines = new List<string>();
        for (int baseY = 0; baseY < image.Height; baseY += scale)
        {
            var line = new StringBuilder();
            string lastForeground = string.Empty;
            string lastBackground = string.Empty;

            for (int baseX = 0; baseX < image.Width; baseX += scale)
            {
                (RgbPixel upperLeft, RgbPixel upperRight, RgbPixel lowerLeft, RgbPixel lowerRight) =
                    SampleQuadrants(image, baseX, baseY, scale);

                AppendCell(line, upperLeft, lowerLeft, ref lastForeground, ref lastBackground);
                AppendCell(line, upperRight, lowerRight, ref lastForeground, ref lastBackground);
            }

            line.Append(AnsiColors.Reset);
            lines.Add(line.ToString());
        }

        return lines;
    }

    private static void AppendCell(
        StringBuilder line, RgbPixel top, RgbPixel bottom, ref string lastForeground, ref string lastBackground)
    {
        string foreground = AnsiColors.Foreground(top);
        if (!string.Equals(lastForeground, foreground, StringComparison.Ordinal))
        {
            line.Append(foreground);
            lastForeground = foreground;
        }

        string background = AnsiColors.Background(bottom);
        if (!string.Equals(lastBackground, background, StringComparison.Ordinal))
        {
            line.Append(background);
            lastBackground = background;
        }

        line.Append(UpperHalfBlock);
    }

    /// <summary>Averages the four quadrants of one scale x scale source block.</summary>
    private static (RgbPixel UpperLeft, RgbPixel UpperRight, RgbPixel LowerLeft, RgbPixel LowerRight)
        SampleQuadrants(RgbImageFrame image, int baseX, int baseY, int scale)
    {
        int upperLeftR = 0, upperLeftG = 0, upperLeftB = 0;
        int upperRightR = 0, upperRightG = 0, upperRightB = 0;
        int lowerLeftR = 0, lowerLeftG = 0, lowerLeftB = 0;
        int lowerRightR = 0, lowerRightG = 0, lowerRightB = 0;
        double mid = (scale - 1) / 2.0;

        for (int dy = 0; dy < scale; dy++)
        {
            for (int dx = 0; dx < scale; dx++)
            {
                int x = Math.Min(baseX + dx, image.Width - 1);
                int y = Math.Min(baseY + dy, image.Height - 1);
                RgbPixel color = image.GetPixel(x, y);

                if (dx <= mid)
                {
                    if (dy <= mid)
                    {
                        upperLeftR += color.R; upperLeftG += color.G; upperLeftB += color.B;
                    }

                    if (dy >= mid)
                    {
                        lowerLeftR += color.R; lowerLeftG += color.G; lowerLeftB += color.B;
                    }
                }

                if (dx >= mid)
                {
                    if (dy <= mid)
                    {
                        upperRightR += color.R; upperRightG += color.G; upperRightB += color.B;
                    }

                    if (dy >= mid)
                    {
                        lowerRightR += color.R; lowerRightG += color.G; lowerRightB += color.B;
                    }
                }
            }
        }

        int count = ((scale + 1) / 2) * ((scale + 1) / 2);
        return (
            Average(upperLeftR, upperLeftG, upperLeftB, count),
            Average(upperRightR, upperRightG, upperRightB, count),
            Average(lowerLeftR, lowerLeftG, lowerLeftB, count),
            Average(lowerRightR, lowerRightG, lowerRightB, count));
    }

    private static RgbPixel Average(int r, int g, int b, int count) => new(
        (byte)Math.Round((double)r / count),
        (byte)Math.Round((double)g / count),
        (byte)Math.Round((double)b / count));
}

/// <summary>The three truecolour escape sequences the renderer emits.</summary>
internal static class AnsiColors
{
    private const string Escape = "\u001b[";

    internal static string Reset => Escape + "0m";

    internal static string Foreground(RgbPixel color) => string.Create(
        CultureInfo.InvariantCulture, $"{Escape}38;2;{color.R};{color.G};{color.B}m");

    internal static string Background(RgbPixel color) => string.Create(
        CultureInfo.InvariantCulture, $"{Escape}48;2;{color.R};{color.G};{color.B}m");
}
