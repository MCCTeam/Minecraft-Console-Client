using System.Globalization;

namespace Mcc.Cli.Presentation;

/// <summary>
/// A horizontal bar for a 0..1 value, drawn with block characters or with ASCII.
/// <para>
/// A number tells you what a value is; a bar tells you where it sits between empty and full without arithmetic.
/// Health and advancement progress both want that, so it is written once here rather than twice at the call sites, and both get the same widths and the same rounding.
/// </para>
/// </summary>
public static class Meter
{
    /// <summary>The colour band a filled fraction falls in, for callers that colour by level.</summary>
    public enum Band
    {
        /// <summary>Below 40%: bad.</summary>
        Low,

        /// <summary>Between 40% and 90%: middling.</summary>
        Medium,

        /// <summary>90% or above: good.</summary>
        High,
    }

    /// <summary>
    /// Renders a bar of <paramref name="width"/> cells for a fraction of 0..1.
    /// </summary>
    /// <param name="fraction">The filled fraction. Clamped, so a server sending 20.4/20 cannot overrun it.</param>
    /// <param name="width">The bar width in cells.</param>
    /// <param name="unicode">Block characters when true, ASCII when false. Follows the glyph setting.</param>
    /// <remarks>
    /// A non-zero fraction always fills at least one cell.
    /// Rounding 0.4% down to an empty bar reads as "no progress at all", which is a different statement from "barely any", and the second one is true.
    /// </remarks>
    public static string Render(double fraction, int width, bool unicode)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        double clamped = Math.Clamp(double.IsFinite(fraction) ? fraction : 0, 0, 1);
        int filled = (int)Math.Round(clamped * width, MidpointRounding.AwayFromZero);
        if (filled == 0 && clamped > 0)
            filled = 1;

        if (filled == width && clamped < 1)
            filled = width - 1;

        char full = unicode ? '▓' : '#';   // dark shade
        char empty = unicode ? '░' : '-';  // light shade
        return new string(full, filled) + new string(empty, width - filled);
    }

    /// <summary>Which band a fraction falls in.</summary>
    public static Band Of(double fraction) => fraction switch
    {
        >= 0.9 => Band.High,
        >= 0.4 => Band.Medium,
        _ => Band.Low,
    };

    /// <summary>The legacy colour code for a band: green, gold, red.</summary>
    public static char Colour(Band band) => band switch
    {
        Band.High => 'a',
        Band.Medium => 'e',
        _ => 'c',
    };

    /// <summary>A percentage, invariant, with one decimal only when it needs one.</summary>
    public static string Percent(double fraction)
    {
        double percent = Math.Clamp(double.IsFinite(fraction) ? fraction : 0, 0, 1) * 100;

        // A whole number reads better as "40%" than "40.0%", but rounding 0.13% to "0%" would say the opposite of what is true, so a small non-zero value keeps its decimal.
        string format = percent > 0 && percent < 10 ? "0.#" : "0.##";
        return percent.ToString(format, CultureInfo.InvariantCulture) + "%";
    }
}
