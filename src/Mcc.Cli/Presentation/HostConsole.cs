using Mcc.Cli.Hosting.Classic;
using Mcc.Cli.Logging;
using Umpk.Text.Serialization;

namespace Mcc.Cli.Presentation;

/// <summary>
/// The classic host's single console sink.
/// <para>
/// Every user-facing line the classic host emits after startup MUST go through here.
/// That is not a style preference, it is a correctness requirement of the ConsoleInteractive submodule:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
/// <c>ConsoleWriter.Write</c> (ConsoleWriter.cs:127-149) is the only path that brackets a write with <c>ConsoleSuggestion.BeforeWrite</c> / <c>AfterWrite</c> and follows it with <c>ConsoleBuffer.RedrawInputArea(RedrawAll: true)</c>.
/// A raw <see cref="Console.WriteLine(string)"/> does none of that, so it scrolls the screen out from under the live input line without repainting it.
///     </description>
///   </item>
///   <item>
///     <description>
/// <c>BeforeWrite</c> is also what feeds <c>DrawHelper.RecentMessageHandler</c>, a 32-slot ring of the lines most recently written (ConsoleSuggestion.cs:705-717).
/// The suggestion popup does NOT read the screen to find out what it is covering: it reconstructs that from the ring (ConsoleSuggestion.cs:362, <c>GetBgMessageBuffer(RecentMessageHandler.GetRecentMessage(...))</c>) and replays it on close (<c>ClearSingleSuggestionPopup</c>, :581-600).
/// So a line written outside <c>ConsoleWriter</c> is invisible to the ring, the ring's indices drift by exactly that many lines, and every later popup restores the WRONG lines into its rectangle.
/// That is the "previously cleared content reappears where the popup was" defect, reproduced live against a 1.21.11 server and absent from the legacy client, which routed every line through ConsoleIO -> ConsoleWriter.
///     </description>
///   </item>
/// </list>
/// <para>
/// When the rich reader is not in use (file input, an exercise run, redirected output) there is no ConsoleInteractive state to keep consistent and the plain <see cref="Console"/> is correct, so the sink degrades to it and the harness contract is untouched.
/// </para>
/// </summary>
internal static class HostConsole
{
    private static volatile bool _rich;

    /// <summary>True once the rich ConsoleInteractive writer owns the terminal.</summary>
    public static bool IsRich => _rich;

    /// <summary>
    /// Marks the rich writer as active.
    /// Called immediately after <see cref="RichConsole.InitWriter"/>, so nothing written before the submodule is initialized is routed into it.
    /// </summary>
    public static void UseRichWriter() => _rich = true;

    /// <summary>Resets the sink to the plain console (used when the rich reader is torn down).</summary>
    public static void UsePlainWriter() => _rich = false;

    /// <summary>Writes one line through whichever writer currently owns the terminal.</summary>
    public static void WriteLine(string line)
    {
        // Tee'd before the write, so a line that crashes the writer is still in the transcript.
        // The transcript is the record of what the user SAW, and the most interesting line in a crash report is usually the last one.
        Transcript?.Invoke(line);

        if (_rich)
            RichConsole.WriteLine(line);
        else
            Console.WriteLine(line);
    }

    /// <summary>
    /// A sink that receives every line this console writes, for the session diagnostics transcript.
    /// Null when diagnostics are off, which is the only state where the client writes nothing to disk.
    /// </summary>
    public static Action<string>? Transcript { get; set; }

    /// <summary>The sink as a delegate, for the presenter and the exercise runner.</summary>
    public static Action<string> Sink { get; } = WriteLine;

    /// <summary>
    /// Writes one error line: through the rich writer when it owns the terminal, else to stderr.
    /// <para>
    /// The split is deliberate.
    /// In rich mode correctness beats stream separation: a raw <see cref="Console.Error"/> write skips <c>ConsoleWriter</c>'s input-area handling and its recent-message ring, so the live <c>"> "</c> prompt leaks onto the line and every later suggestion popup restores the wrong lines.
    /// <see cref="ConsoleLogger"/> already makes the same trade for log lines.
    /// When the rich writer is not in use there is no ring to keep consistent and the harness may be separating the streams, so stderr is preserved.
    /// </para>
    /// </summary>
    public static void WriteErrorLine(string line)
    {
        Transcript?.Invoke(line);

        if (_rich)
            RichConsole.WriteLine(line);
        else
            Console.Error.WriteLine(line);
    }

    private static AnsiComponentRenderer? _renderer;

    /// <summary>
    /// Installs the renderer used by <see cref="WriteFormatted"/>.
    /// Classic host only; with none installed the formatted write degrades to a plain one, which is what the TUI and the redirected paths want.
    /// </summary>
    public static void UseRenderer(AnsiComponentRenderer renderer, ConsoleColorDepth depth)
    {
        _renderer = renderer;
        _depth = depth;
    }

    private static ConsoleColorDepth _depth = ConsoleColorDepth.Vt10024Bit;

    /// <summary>
    /// Writes a line that may carry legacy section-sign colour codes, rendering them.
    /// <para>
    /// Command output arrives on two separate paths and BOTH carry codes: the body, written through <see cref="ConsoleCommandOutput"/> as the command runs, and the single result message the dispatcher returns, written by the caller afterwards.
    /// Only the first was rendering, so a command whose whole output is its result (tps, for one, whose number legacy colours by threshold) printed a literal "§a20" while a command that wrote its own lines came out right.
    /// </para>
    /// </summary>
    public static void WriteFormatted(string text)
    {
        if (_renderer is null)
        {
            WriteLine(text);
            return;
        }

        foreach (string line in text.Split('\n'))
            // Through LegacyBackground rather than straight to the renderer: it forwards a line with no background marker unchanged, and handles MCC's own §§X background extension around the renderer for the lines that carry one (the chunk map and its legend).
            WriteLine(LegacyBackground.Render(line, _renderer, _depth));
    }

    /// <summary>
    /// Clears the visible console.
    /// Delegates to <see cref="RichConsole.ClearScreen"/> while the rich writer owns the terminal, because a bare <see cref="Console.Clear"/> leaves ConsoleInteractive's input-area diff cache and the popup's background ring describing a screen that no longer exists.
    /// </summary>
    /// <param name="confirmation">
    /// A line written after the wipe.
    /// It is not decoration: <c>ConsoleBuffer.RedrawInputArea</c> only repaints the parts of the input line that differ from what it last drew (ConsoleBuffer.cs:406-411), and after a wipe the screen is blank while that cache still says "&gt; ", so nothing is repainted and the prompt stays invisible until enough characters are typed to force a difference.
    /// A write through <c>ConsoleWriter</c> is the only public trigger for <c>RedrawInputArea(RedrawAll: true)</c>, which is what puts the prompt back.
    /// </param>
    public static void ClearScreen(string? confirmation = null)
    {
        if (_rich)
        {
            RichConsole.ClearScreen(confirmation);
            return;
        }

        RichConsole.TryRawClear();
        if (!string.IsNullOrEmpty(confirmation))
            Console.WriteLine(confirmation);
    }
}
