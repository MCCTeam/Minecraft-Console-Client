using System.Text.RegularExpressions;
using Umpk.Text;

namespace Mcc.Cli.Presentation;

/// <summary>Small helpers for ANSI (VT100) escape sequences the classic host emits.</summary>
internal static partial class Ansi
{
    /// <summary>
    /// Wraps <paramref name="text"/> in an SGR run for <paramref name="color"/>, shaped by the console's configured colour depth, and resets after it.
    /// Returns the text unchanged when colour is off, so a caller never has to branch on the setting itself.
    /// </summary>
    public static string Colorize(string text, TextColor color, ConsoleColorDepth depth)
    {
        ArgumentNullException.ThrowIfNull(text);
        return depth == ConsoleColorDepth.Disable
            ? text
            : $"\u001b[{AnsiColorMapping.Sgr(color, depth)}m{text}\u001b[0m";
    }

    /// <summary>Removes SGR color/decoration escape sequences from a string (used when color must be stripped).</summary>
    public static string Strip(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.IndexOf('\u001b') < 0 ? text : SgrPattern().Replace(text, string.Empty);
    }

    [GeneratedRegex("\u001b\\[[0-9;]*m")]
    private static partial Regex SgrPattern();
}
