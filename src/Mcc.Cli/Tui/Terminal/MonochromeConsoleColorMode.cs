using Mcc.Cli.Tui.Map;
using Avalonia.Media;
using Consolonia.Core.Drawing.PixelBufferImplementation;

namespace Mcc.Cli.Tui.Terminal;

/// <summary>
/// The TUI's answer to <c>NO_COLOR</c> and <c>ConsoleColorMode = "disable"</c>: every cell the terminal receives is drawn with a zero-saturation attribute.
/// <para>
/// A full-screen TUI cannot honour <c>NO_COLOR</c> the way a line-oriented program does.
/// The classic host simply stops emitting SGR sequences and the bytes on the wire become plain text; a Consolonia application paints a grid of cells and always writes an attribute per cell, so "no colour" here can only mean MONOCHROME CELL ATTRIBUTES, not absent ANSI.
/// This is that, and it is applied at Consolonia's own <see cref="IConsoleColorMode"/> seam rather than at MCC's brushes, which is what makes it complete: the theme's chrome, the views' own brushes, the minimap and the server's chat colours all pass through <see cref="MapColors"/> on their way to the console, so one switch covers surfaces MCC does not own.
/// </para>
/// <para>
/// Colours are flattened to their luminance rather than to a fixed pair, so every contrast relationship the layout depends on survives: the selected suggestion stays inverted against the unselected ones, a dark panel stays darker than the text on it, and nothing becomes invisible.
/// Rec.
/// 601 luma is used because it is the standard perceptual weighting; any monotonic mapping would preserve ordering, but this one also preserves how different two colours LOOK.
/// Transparency is passed through untouched: a transparent cell is the terminal's own background showing through, which is the least coloured thing available.
/// </para>
/// </summary>
internal sealed class MonochromeConsoleColorMode : IConsoleColorMode
{
    private readonly RgbConsoleColorMode _inner = new();

    /// <inheritdoc/>
    public Color Blend(Color background, Color foreground, bool isUnderline)
        => Flatten(_inner.Blend(background, foreground, isUnderline));

    /// <inheritdoc/>
    public (object, object) MapColors(Color foreground, Color background, FontWeight? weight)
        => _inner.MapColors(Flatten(foreground), Flatten(background), weight);

    /// <summary>
    /// Replaces a colour with the grey of the same perceived brightness (Rec.
    /// 601 luma), preserving alpha.
    /// A colour that is already grey is returned unchanged, so a monochrome design is not disturbed.
    /// </summary>
    internal static Color Flatten(Color color)
    {
        if (color.A == 0)
            return color;

        byte luma = (byte)Math.Clamp(
            Math.Round((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)), 0, 255);
        return Color.FromArgb(color.A, luma, luma, luma);
    }
}
