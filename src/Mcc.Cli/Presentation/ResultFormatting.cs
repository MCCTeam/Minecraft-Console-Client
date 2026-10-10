using Mcc.Cli.Tui.Hosting;
using DMCBK.Core.Presentation;

using DMCBK.Core.Commands;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Turns a finished <see cref="CmdResult"/> into the line a host prints: a status glyph, a colour, and the message.
/// <para>
/// Every host used to print <see cref="CmdResult.Message"/> with one unconditional write and no branch on <see cref="CmdResult.Status"/> (Program.cs, TuiHost), so <c>Selected hotbar slot 3.</c> and <c>No anvil is open.</c> arrived in the same colour with the same weight.
/// Six distinguishable outcomes looked like one.
/// This is the single place that decides how each one reads, so the classic host and the TUI cannot drift apart.
/// </para>
/// </summary>
public static class ResultFormatting
{
    /// <summary>
    /// Decorates a result message for display.
    /// Returns null when there is nothing to print.
    /// </summary>
    /// <param name="status">The status the command finished with.</param>
    /// <param name="message">The already-localized result message.</param>
    /// <param name="glyphs">The resolved glyph vocabulary.</param>
    public static string? Decorate(CmdStatus status, string? message, GlyphSet glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        if (string.IsNullOrEmpty(message))
            return message;

        // A message that already opens with a colour code is making its own decision about how it reads (the /debug state table, the chunk map legend, the dialog renderer).
        // Decorating it would fight that, so it is passed through untouched.
        if (message.StartsWith('§'))
            return message;

        (string glyph, char colour) = Style(status, glyphs);

        // Only the FIRST line carries the glyph.
        // A multi-line result is one outcome, not one per line, and a glyph gutter down the left of a block reads as a list of separate results.
        int firstBreak = message.IndexOf('\n');
        if (firstBreak < 0)
            return $"§{colour}{glyph} §r{message}";

        return $"§{colour}{glyph} §r{message[..firstBreak]}{message[firstBreak..]}";
    }

    /// <summary>The glyph and legacy colour code one status reads as.</summary>
    public static (string Glyph, char Colour) Style(CmdStatus status, GlyphSet glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        return status switch
        {
            CmdStatus.Done => (glyphs.Ok, 'a'),

            // The three feature gates are not failures of the command, they are a switched-off subsystem, and the user's next action is to edit a config key rather than to retype anything.
            // Gold, not red, and its own glyph.
            CmdStatus.FailNeedTerrain or CmdStatus.FailNeedInventory or CmdStatus.FailNeedEntity
                => (glyphs.Blocked, 'e'),

            // Not an error either: the answer is not knowable YET.
            CmdStatus.FailChunkNotLoad => (glyphs.Pending, '7'),

            CmdStatus.Fail => (glyphs.Fail, 'c'),

            // NotRun reaches a host only as a diagnostic (an unknown command in a non-slash prefix mode).
            _ => (glyphs.Info, 'b'),
        };
    }
}
