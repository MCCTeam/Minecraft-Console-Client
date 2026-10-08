using Mcc.Cli.Hosting;
using System.Threading.Channels;
using ConsoleInteractive;
using Mcc.Cli.Configuration;
using DMCBK.Core;
using DMCBK.Core.Commands;

namespace Mcc.Cli.Presentation;

/// <summary>
/// The classic host's rich line editor, backed by the ConsoleInteractive submodule (the only place it may be referenced).
/// It drives input through <see cref="ConsoleReader"/> (history, cursor editing) and feeds the <see cref="ConsoleSuggestion"/> popup from BOTH the local internal-command tree (<see cref="CommandService.CompleteAsync(string, CancellationToken)"/>) and server command completion (<see cref="ChatApi.CompleteAsync"/>).
/// It is used only for a genuine interactive terminal; the file-input / BasicIO / redirected paths keep the plain reader so the harness contract is untouched.
/// </summary>
internal static class RichConsole
{
    private static int _suggestionGeneration;

    /// <summary>
    /// The live REPL's line channel, published while <see cref="RunAsync"/> is running so a command that has to ask a question can take the next line the user types.
    /// <para>
    /// It has to be this channel and not a second reader.
    /// <see cref="ConsoleReader"/> owns the keyboard for as long as the REPL lives, and a <see cref="Console.ReadLine"/> beside it would race the read thread for keystrokes.
    /// While a command runs the REPL is inside <c>await processLine(...)</c>, so the channel is buffering and nobody else is reading from it.
    /// </para>
    /// </summary>
    private static ChannelReader<string>? _lines;

    /// <summary>True while the rich REPL is running and can hand over the next typed line.</summary>
    public static bool CanRequestLine => Volatile.Read(ref _lines) is not null;

    /// <summary>
    /// Reads the next line the user types, or an empty string when the REPL is not running.
    /// The caller has already printed whatever it is asking.
    /// </summary>
    public static async Task<string> RequestLineAsync(CancellationToken ct)
    {
        ChannelReader<string>? lines = Volatile.Read(ref _lines);
        if (lines is null)
            return string.Empty;

        try
        {
            return await lines.ReadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or ChannelClosedException)
        {
            return string.Empty;
        }
    }

    /// <summary>True when stdin and stdout are both a real terminal (so the key-listener reader is safe to use).</summary>
    public static bool IsSupported
    {
        get
        {
            try
            {
                return !Console.IsInputRedirected && !Console.IsOutputRedirected;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Initializes the rich writer (Windows ANSI enable, VT100 passthrough for pre-rendered ANSI).
    /// Wires the two console.toml settings the submodule exposes directly: Display_Input (the ConsoleReader.DisplayUesrInput echo toggle) and History_Input_Records (the ConsoleBuffer backread limit), and the suggestion popup config: color on/off and depth, Use_Basic_Arrow, the max width/count, and the 7 suggestion colors applied to ConsoleSuggestion.
    /// <paramref name="colorDepth"/> is the already-resolved depth (NO_COLOR/redirect/MCC_FORCE_COLOR folded in), matching what <see cref="AnsiComponentRenderer"/> uses for the chat/log text this popup sits next to.
    /// </summary>
    public static void InitWriter(ConsoleHostConfig console, ConsoleColorDepth colorDepth)
    {
        ArgumentNullException.ThrowIfNull(console);

        ConsoleWriter.Init();
        // We hand ConsoleWriter fully-rendered ANSI strings; keep its own color pipeline out of the way.
        ConsoleWriter.UseVT100ColorCode = true;

        ConsoleReader.DisplayUesrInput = console.DisplayInput;
        ConsoleBuffer.SetBackreadBufferLimit(console.HistoryInputRecords);

        // Legacy forces Enable_Color off when the color mode is "disable"; folded into colorDepth here since Disable is one of the depths ResolveColorDepth can already produce.
        bool suggestionColor = console.SuggestionsEnabled && colorDepth != ConsoleColorDepth.Disable;
        ConsoleSuggestion.EnableColor = suggestionColor;
        ConsoleSuggestion.Enable24bitColor = colorDepth == ConsoleColorDepth.Vt10024Bit;
        ConsoleSuggestion.UseBasicArrow = console.SuggestionUseBasicArrow;
        ConsoleSuggestion.SetMaxSuggestionLength(console.SuggestionMaxWidth);
        ConsoleSuggestion.SetMaxSuggestionCount(console.SuggestionMaxDisplayed);
        ConsoleSuggestion.SetColors(
            console.SuggestionTextColor,
            console.SuggestionTextBackgroundColor,
            console.SuggestionHighlightTextColor,
            console.SuggestionHighlightTextBackgroundColor,
            console.SuggestionTooltipColor,
            console.SuggestionHighlightTooltipColor,
            console.SuggestionArrowSymbolColor);
    }

    /// <summary>The console line sink for the rich path (interleaves cleanly with the live input line).</summary>
    public static void WriteLine(string line) => ConsoleWriter.WriteLine(line);

    /// <summary>
    /// Clears the screen in the one order ConsoleInteractive survives.
    /// A bare <see cref="Console.Clear"/>, which is what the clear command used to do, leaves two pieces of submodule state describing a screen that no longer exists, and both are user-visible:
    /// <list type="number">
    ///   <item><description>
    /// The popup's background ring (<c>DrawHelper.RecentMessageHandler</c>, ConsoleSuggestion.cs:705-717) still holds pre-clear lines.
    /// The popup reconstructs what it is covering from that ring rather than from the screen (:362) and replays it on close (:581-600), so the next popup paints cleared text back into its own rectangle.
    ///   </description></item>
    ///   <item><description>
    /// <c>ConsoleBuffer.RedrawInputArea</c> only writes the characters that differ from the line it last drew (ConsoleBuffer.cs:406-411).
    /// After a wipe the screen is blank but that cache still reads "&gt; ", so the comparison finds no difference, the branch at :413 moves the caret without writing, and the prompt never comes back.
    /// Note the legacy client has this same defect; matching it exactly would reproduce the bug, so this deliberately goes one better.
    ///   </description></item>
    /// </list>
    /// </summary>
    /// <param name="confirmation">Line written after the wipe; see <see cref="HostConsole.ClearScreen"/>.</param>
    public static void ClearScreen(string? confirmation = null)
    {
        if (Console.IsOutputRedirected)
        {
            TryRawClear();
            return;
        }

        // 1. Tear the popup down while the screen it captured is still intact, so its replay writes real
        // content and InUse goes false.
        // Legacy clears after the wipe (ClassicConsoleBackend.cs:137-138), which only avoids a ghost row because a homed cursor drives every cursorTop negative and ClearSingleSuggestionPopup bails at ConsoleSuggestion.cs:582.
        // Ordering it first does not depend on that accident.
        ConsoleSuggestion.ClearSuggestions();

        // 2. Evict the background ring. The submodule exposes no flush and is read-only (AGENTS.md), so
        // writing MaxValueOfMaxSuggestionCount blank lines through ConsoleWriter is the only lever: it is exactly the number of slots, so no pre-clear line can survive to be replayed.
        // Before the wipe, so the blanks scroll away with everything else instead of landing on a cleared screen.
        for (int i = 0; i < ConsoleSuggestion.MaxValueOfMaxSuggestionCount; i++)
            ConsoleWriter.WriteLine(string.Empty);

        TryRawClear();

        // 3. One write through ConsoleWriter, the only public trigger for RedrawInputArea(RedrawAll: true)
        // (ConsoleWriter.cs:144-145), which is what repaints "> " and the caret.
        ConsoleWriter.WriteLine(confirmation ?? string.Empty);
    }

    /// <summary>The raw wipe, with the two failures a non-interactive or exotic terminal can raise.</summary>
    internal static void TryRawClear()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Output is redirected (file-input/tests); nothing to clear.
        }
        catch (PlatformNotSupportedException)
        {
            // Legacy swallows this too (ConsoleIO.cs:203-215); the command only caught IOException.
        }
    }

    /// <summary>
    /// Runs the interactive read loop: each entered line is handed to <paramref name="processLine"/>; input changes recompute the suggestion popup, unless <paramref name="suggestionsEnabled"/> is false (the CommandSuggestion.Enable setting in console.toml), in which case the popup is never subscribed and so never renders.
    /// Stops on exit request or cancellation.
    /// </summary>
    public static async Task RunAsync(
        Client client, HostControl control, Func<string, Task> processLine, bool suggestionsEnabled, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(processLine);

        var lines = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        void OnMessage(object? sender, string line) => lines.Writer.TryWrite(line);
        void OnInputChange(object? sender, ConsoleReader.Buffer buffer) => RefreshSuggestions(client, buffer);

        ConsoleReader.MessageReceived += OnMessage;
        if (suggestionsEnabled)
            ConsoleReader.OnInputChange += OnInputChange;

        ConsoleReader.BeginReadThread();
        Volatile.Write(ref _lines, lines.Reader);

        try
        {
            while (!control.ExitRequested && !ct.IsCancellationRequested)
            {
                string line;
                try
                {
                    line = await lines.Reader.ReadAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (line.Length == 0)
                    continue;

                await processLine(line).ConfigureAwait(false);
            }
        }
        finally
        {
            Volatile.Write(ref _lines, null);
            ConsoleReader.StopReadThread();
            ConsoleReader.MessageReceived -= OnMessage;
            ConsoleReader.OnInputChange -= OnInputChange;
            ConsoleSuggestion.ClearSuggestions();
        }
    }

    // Recompute suggestions off the reader thread; a generation guard drops stale async results so a fast typist never sees an out-of-date popup.
    // Which lines are completable, and against what, is decided by CommandService.CompleteInputAsync (the one copy of the prefix rules), not by a leading-slash test here: the internal prefix is configurable, and a '/'-only test both missed the backslash/none prefixes and mistook the "//" server-command escape for an internal command.
    private static void RefreshSuggestions(Client client, ConsoleReader.Buffer buffer)
    {
        string text = buffer.Text;
        int cursor = Math.Clamp(buffer.CursorPosition, 0, text.Length);
        int generation = Interlocked.Increment(ref _suggestionGeneration);

        if (text.Length == 0)
        {
            ConsoleSuggestion.ClearSuggestions();
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                (ConsoleSuggestion.Suggestion[] suggestions, Tuple<int, int> range) =
                    await ComputeAsync(client, text, cursor).ConfigureAwait(false);

                if (generation != Volatile.Read(ref _suggestionGeneration))
                    return;

                if (suggestions.Length == 0)
                    ConsoleSuggestion.ClearSuggestions();
                else
                    ConsoleSuggestion.UpdateSuggestions(suggestions, range);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ConsoleSuggestion.ClearSuggestions();
            }
        });
    }

    private static async Task<(ConsoleSuggestion.Suggestion[] Suggestions, Tuple<int, int> Range)> ComputeAsync(
        Client client, string text, int cursor)
    {
        // One merged query: the internal-command tree plus the server, with the prefix rules applied once and the replacement span already expressed in this buffer's own indices.
        InputCompletion completion = await client.Commands.CompleteInputAsync(text, cursor).ConfigureAwait(false);

        var suggestions = new ConsoleSuggestion.Suggestion[completion.Suggestions.Count];
        for (int i = 0; i < suggestions.Length; i++)
        {
            ChatCompletion candidate = completion.Suggestions[i];
            suggestions[i] = new ConsoleSuggestion.Suggestion(candidate.Text, candidate.Tooltip);
        }

        return (suggestions, Tuple.Create(completion.Start, completion.End));
    }
}
