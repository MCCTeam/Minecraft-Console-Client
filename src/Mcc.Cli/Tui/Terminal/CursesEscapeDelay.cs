using Mcc.Cli.Hosting;
using System.Runtime.InteropServices;

namespace Mcc.Cli.Tui.Terminal;

/// <summary>
/// Lowers ncurses' <c>ESCDELAY</c> before the TUI's console is created, so pressing Escape does something within a few tens of milliseconds instead of a full second.
/// </summary>
/// <remarks>
/// <para>
/// On Linux and macOS the TUI runs on <c>Consolonia.PlatformSupport.CursesConsole</c>, which is real ncurses: its <c>PrepareConsole</c> calls <c>keypad(true)</c>, turning on ncurses' escape-sequence disambiguation.
/// Every function key a terminal sends is <c>ESC</c> followed by more bytes (Up is <c>ESC [ A</c>), so on seeing a lone <c>ESC</c> ncurses cannot yet know whether a key sequence has begun, and waits <c>ESCDELAY</c> milliseconds for the rest.
/// Nothing follows a real Escape keypress, so the whole delay is spent, inside a single <c>get_wch()</c> on Consolonia's reader thread, BEFORE the key is dispatched to anything.
/// </para>
/// <para>
/// ncurses' compiled-in default is 1000 ms, a holdover from serial terminals.
/// Measured end to end against a live server: dismissing an open container overlay took 1020/1025/1021 ms with Escape and 22/17/20 ms with E, which share one line of one handler (<see cref="Container.ContainerOverlay"/>: <c>if (e.Key is Key.Escape or Key.E)</c>).
/// E is a plain character and never enters the disambiguation path; that gap IS ESCDELAY and nothing else.
/// With this applied the same measurement is 72/72/71 ms.
/// </para>
/// <para>
/// It has to be the native environment, not the managed one. ncurses reads <c>getenv("ESCDELAY")</c> during <c>initscr()</c>, and on Unix .NET's <see cref="Environment.SetEnvironmentVariable(string, string)"/> writes only to the runtime's own managed copy: a probe in this repo's scratchpad set the variable that way and native <c>getenv</c> still returned null, while <c>setenv</c> was visible to both.
/// So the obvious one-liner would have compiled, run, and done nothing.
/// Consolonia binds neither <c>set_escdelay</c> nor <c>get_escdelay</c> (checked in 11.3.9, 11.3.10, 11.3.12.3 and 11.3.12.6), so there is no managed API to call instead.
/// </para>
/// <para>
/// The trade is real but small, and is why the value is not zero.
/// A terminal splitting a genuine escape sequence across a gap longer than the delay would have its Up/Down/Home/F-key read as a bare Escape followed by stray letters.
/// In practice a terminal writes the whole sequence at once and it arrives in one read; 50 ms leaves headroom for a slow SSH link while staying below the threshold where a person notices.
/// Verified after the change that Up still recalls from history, which is precisely the <c>ESC [ A</c> case at risk.
/// </para>
/// <para>
/// Anyone who needs a different value already has the standard knob: this does not overwrite an <c>ESCDELAY</c> the user (or their terminal multiplexer) has already exported.
/// </para>
/// </remarks>
internal static class CursesEscapeDelay
{
    private const string Variable = "ESCDELAY";

    /// <summary>Milliseconds ncurses may wait for the rest of an escape sequence. See the remarks.</summary>
    private const string DefaultMilliseconds = "50";

    /// <summary><c>setenv</c>'s overwrite flag: 0 leaves an existing value alone.</summary>
    private const int DoNotOverwrite = 0;

    // DllImport rather than LibraryImport: the source generator requires AllowUnsafeBlocks for the whole project, which is a large thing to switch on for one three-argument call.
    // ProcessCloseGuard already declares its kernel32 imports this way.
    [DllImport("libc", EntryPoint = "setenv", CharSet = CharSet.Ansi)]
    private static extern int SetEnvironmentVariable(string name, string value, int overwrite);

    /// <summary>
    /// Applies the default, unless the environment already carries one.
    /// Call before the Consolonia app builder runs: ncurses latches the value in <c>initscr()</c> and ignores later changes.
    /// </summary>
    public static void Apply()
    {
        // Windows has no ncurses and no libc to call; Consolonia uses its Win32 console there.
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            SetEnvironmentVariable(Variable, DefaultMilliseconds, DoNotOverwrite);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // A platform without this libc entry point keeps ncurses' own default.
            // A slow Escape is worth strictly less than the TUI failing to start.
        }
    }
}
