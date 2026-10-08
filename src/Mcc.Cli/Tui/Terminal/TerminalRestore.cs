using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Mcc.Cli.Tui.Terminal;

/// <summary>
/// Idempotently restores the terminal after the Consolonia TUI exits: disables every mouse-tracking mode, leaves the alternate screen, shows the cursor, re-enables wrap and resets attributes, then runs <c>stty sane</c> on POSIX.
/// Ported behavior from the legacy TUI backend (never referencing it).
/// Registered on process exit and run in the UI-loop finally so a crash still leaves a usable terminal.
/// </summary>
internal static class TerminalRestore
{
    private static volatile bool _restored;

    public static void Restore()
    {
        if (_restored)
            return;

        _restored = true;

        try
        {
            // Disable mouse tracking (X11, highlight, button-event, any-event, focus, UTF-8/SGR/urxvt encodings).
            Console.Write("\x1b[?1000l\x1b[?1001l\x1b[?1002l\x1b[?1003l\x1b[?1004l\x1b[?1005l\x1b[?1006l\x1b[?1015l");
            Console.Write("\x1b[?1049l"); // leave alternate screen
            Console.Write("\x1b[?25h");   // show cursor
            Console.Write("\x1b[?7h");    // re-enable line wrap
            Console.Write("\x1b[0m");     // reset attributes
        }
        catch (IOException)
        {
            // Output redirected; nothing to restore.
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo("stty", "sane")
                {
                    RedirectStandardInput = false,
                    UseShellExecute = false,
                });
                process?.WaitForExit(1000);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                // stty unavailable; the ANSI resets above are the fallback.
            }
        }
    }
}
