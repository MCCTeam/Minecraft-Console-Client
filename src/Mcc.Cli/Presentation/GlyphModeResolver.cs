using System.Text;
using DMCBK.Core.Presentation;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Resolves <see cref="GlyphMode.Auto"/> against the terminal the process actually has.
/// <para>
/// This lives in the host, not in <c>DMCBK.Core</c>, for the same reason the setting moved to <c>console.toml</c>: answering it needs the output encoding and the terminal type, and the core is console-free.
/// The core is handed a resolved <see cref="GlyphSet"/> and never asks the question.
/// </para>
/// </summary>
internal static class GlyphModeResolver
{
    /// <summary>
    /// Picks the glyph vocabulary for a configured mode.
    /// <see cref="GlyphMode.Emoji"/> and <see cref="GlyphMode.Ascii"/> are honored exactly; <see cref="GlyphMode.Auto"/> is decided here.
    /// </summary>
    public static GlyphSet Resolve(GlyphMode mode)
        => mode switch
        {
            GlyphMode.Emoji => MccGlyphs.Emoji,
            GlyphMode.Ascii => GlyphSet.Ascii,
            _ => DetectSupportsEmoji() ? MccGlyphs.Emoji : GlyphSet.Ascii,
        };

    /// <summary>
    /// Whether this terminal can be expected to draw the emoji set.
    /// </summary>
    /// <remarks>
    /// Three tests, cheapest first, and every one of them can only turn emoji OFF.
    /// The default answer is yes, because it was legacy's default (<c>EnableEmoji = true</c>) and because the modal terminal in 2026 does render them; a false negative costs a user their emoji, while a false positive costs them a screen of replacement boxes.
    /// <list type="number">
    ///   <item><description>
    /// A non-Unicode output encoding cannot encode the glyphs at all, so the question is settled before the terminal is consulted.
    /// This is the Windows-codepage-437 case.
    ///   </description></item>
    ///   <item><description>
    /// Redirected output is not a terminal.
    /// Emoji are double-width, and a log file or a pipe into a parser has no business carrying them, so ASCII is the safer default there.
    /// It also keeps the file-input test harness deterministic across machines.
    ///   </description></item>
    ///   <item><description>
    /// <c>TERM</c> values that promise no such capability: <c>dumb</c>, and the bare <c>linux</c> console, whose framebuffer font has no emoji coverage.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public static bool DetectSupportsEmoji()
    {
        try
        {
            Encoding encoding = Console.OutputEncoding;
            if (encoding.CodePage is not (65001 or 1200 or 1201 or 12000 or 12001))
                return false;
        }
        catch (IOException)
        {
            // No console attached at all; fall through to the remaining tests.
        }

        if (Console.IsOutputRedirected)
            return false;

        string term = Environment.GetEnvironmentVariable("TERM") ?? string.Empty;
        if (term.Length == 0)
            // Windows Terminal and conhost set no TERM and both render emoji.
            return OperatingSystem.IsWindows();

        return !term.Equals("dumb", StringComparison.OrdinalIgnoreCase)
            && !term.Equals("linux", StringComparison.OrdinalIgnoreCase);
    }
}
