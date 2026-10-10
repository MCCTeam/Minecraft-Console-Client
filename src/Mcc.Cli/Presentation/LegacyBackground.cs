using System.Globalization;
using System.Text;
using Umpk.Text;
using Umpk.Text.Serialization;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Renders MCC's own <c>§§X</c> BACKGROUND extension for the classic host, around the ordinary <see cref="AnsiComponentRenderer"/> that handles everything else.
/// <para>
/// <c>§X</c> is vanilla's foreground code and UMPK's <see cref="LegacyText"/> owns it.
/// <c>§§X</c> is not vanilla at all: it is MCC's own extension for a background colour, used by the chunk map to mark the player's chunk (<c>§§7</c>, grey) and the marked chunk (<c>§§4</c>, red), and by the legend that explains them.
/// UMPK's parser has never heard of it, and by its rules an unrecognised code keeps the first character and re-reads from the second, so <c>§§7</c> parsed as a literal <c>§</c> followed by the REAL code <c>§7</c>: the lone section sign was then swallowed by the flattener's end-of-string rule and the cell came out with a dark grey FOREGROUND instead of a grey background.
/// On the legend line, whose cells are two spaces, that is invisible ink, which is exactly how it was reported: the player and marked-chunk markers do not render.
/// </para>
/// <para>
/// The TUI has had its own reader for this the whole time (<c>Tui.LegacyTextInlines</c>, whose format comment names the same two cells); this is the classic host's missing half.
/// It lives here rather than in UMPK because the extension is MCC's, not Minecraft's, and rather than inside <see cref="AnsiComponentRenderer"/> because that renderer's input is a <see cref="Component"/>, and a component has no background: vanilla's style model does not carry one, so a background cannot survive the trip through it and has to be written around it.
/// </para>
/// </summary>
internal static class LegacyBackground
{
    /// <summary>The section sign both the vanilla codes and the MCC extension are introduced by.</summary>
    private const char Prefix = '§';

    /// <summary>
    /// The full reset, used for <c>§§r</c>.
    /// Chosen over the narrower <c>ESC[49m</c> (default background) because the suggestion popup's restore replays the escapes from the last FULL reset: a line whose only reset is <c>49</c> gives that replay no anchor, so it would start from the beginning of the line every time.
    /// A full reset is also what the map wants, since nothing else is styled around it.
    /// </summary>
    private const string Reset = "\u001b[0m";

    /// <summary>True when <paramref name="text"/> carries at least one <c>§§</c> background marker.</summary>
    public static bool HasBackground(string? text)
        => text is not null && text.Contains("§§", StringComparison.Ordinal);

    /// <summary>
    /// Renders one line: the <c>§§X</c> markers become background SGR sequences, and everything between them goes through <paramref name="renderer"/> exactly as it would without them.
    /// </summary>
    public static string Render(string line, AnsiComponentRenderer renderer, ConsoleColorDepth depth)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(renderer);

        if (!HasBackground(line))
            return renderer.Render(LegacyText.Parse(line));

        var output = new StringBuilder(line.Length + 32);
        var segment = new StringBuilder(line.Length);

        void FlushSegment()
        {
            if (segment.Length == 0)
                return;

            output.Append(renderer.Render(LegacyText.Parse(segment.ToString())));
            segment.Clear();
        }

        int i = 0;
        while (i < line.Length)
        {
            // A background marker is THREE characters and needs all three; a §§ at the very end of the line is not one, and is left where it is rather than eating the rest of the buffer.
            if (line[i] != Prefix || i + 2 >= line.Length || line[i + 1] != Prefix)
            {
                segment.Append(line[i]);
                i++;
                continue;
            }

            if (BackgroundSgr(line[i + 2], depth) is not { } sgr)
            {
                // §§ followed by something that is not a colour or a reset: not ours, so it stays text.
                segment.Append(line[i]);
                i++;
                continue;
            }

            FlushSegment();
            output.Append(sgr);
            i += 3;
        }

        FlushSegment();
        return output.ToString();
    }

    /// <summary>
    /// The escape sequence for one background code, or null when the character is not one.
    /// Colour depth is honoured the same way the foreground path honours it, including <see cref="ConsoleColorDepth.Disable"/>, where a background is simply not drawn rather than drawn in some fallback.
    /// </summary>
    private static string? BackgroundSgr(char code, ConsoleColorDepth depth)
    {
        if (code is 'r' or 'R')
            return depth == ConsoleColorDepth.Disable ? string.Empty : Reset;

        if (LegacyColorName(code) is not { } name || TextColor.FromName(name) is not { } color)
            return null;

        if (depth == ConsoleColorDepth.Disable)
            return string.Empty;

        // The foreground mapping, shifted onto the background.
        // The 4-bit and 8-bit forms differ in HOW they shift: a basic foreground code (30-37, 90-97) becomes a background by adding 10, while the 8-bit and 24-bit forms swap their 38 selector for 48.
        string foreground = AnsiColorMapping.Sgr(color, depth);
        if (foreground.Length == 0)
            return string.Empty;

        string background = foreground.StartsWith("38;", StringComparison.Ordinal)
            ? "48;" + foreground[3..]
            : (int.Parse(foreground, CultureInfo.InvariantCulture) + 10).ToString(CultureInfo.InvariantCulture);

        return $"\u001b[{background}m";
    }

    /// <summary>
    /// The vanilla colour a legacy code names.
    /// The order is <c>ChatFormatting</c>'s own (<c>0</c>..<c>f</c>), which is the table <see cref="LegacyText"/> parses the foreground form with, so the two cannot disagree about what <c>§4</c> means.
    /// </summary>
    private static string? LegacyColorName(char code) => char.ToLowerInvariant(code) switch
    {
        '0' => "black",
        '1' => "dark_blue",
        '2' => "dark_green",
        '3' => "dark_aqua",
        '4' => "dark_red",
        '5' => "dark_purple",
        '6' => "gold",
        '7' => "gray",
        '8' => "dark_gray",
        '9' => "blue",
        'a' => "green",
        'b' => "aqua",
        'c' => "red",
        'd' => "light_purple",
        'e' => "yellow",
        'f' => "white",
        _ => null,
    };
}
