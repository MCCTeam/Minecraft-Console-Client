using System;
using System.Threading;
using Avalonia;
using Consolonia;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    /// <summary>
    /// Manages the lifecycle of the Consolonia-based inventory TUI.
    /// Runs on a dedicated thread to avoid blocking MCC's network thread.
    /// </summary>
    public static class InventoryTuiHost
    {
        private static volatile bool _isRunning;
        private static bool _everLaunched;

        public static McClient? ActiveHandler { get; private set; }
        public static int ActiveWindowId { get; private set; }

        public static bool IsRunning => _isRunning;
        public static bool CanLaunch => !_isRunning && !_everLaunched;

        /// <summary>
        /// Called before TUI takes over the terminal.
        /// Should stop ConsoleInteractive's read thread and detach events.
        /// </summary>
        public static Action? OnSuspendConsole { get; set; }

        /// <summary>
        /// Called after TUI releases the terminal.
        /// Should re-initialize ConsoleInteractive and reattach events.
        /// </summary>
        public static Action? OnResumeConsole { get; set; }

        public static bool Launch(McClient handler, int windowId)
        {
            if (_isRunning)
                return false;

            if (_everLaunched)
                return false; // Avalonia doesn't support re-initialization

            Container? container = handler.GetInventory(windowId);
            if (container == null)
                return false;

            _isRunning = true;
            ActiveHandler = handler;
            ActiveWindowId = windowId;

            var tuiThread = new Thread(RunTui) { Name = "InventoryTUI", IsBackground = false };
            tuiThread.Start();

            return true;
        }

        private static void RunTui()
        {
            try
            {
                OnSuspendConsole?.Invoke();

                _everLaunched = true;

                AppBuilder builder = AppBuilder.Configure<InventoryApp>()
                    .UseConsolonia()
                    .UseAutoDetectedConsole()
                    .LogToException();

                builder.StartWithConsoleLifetime(Array.Empty<string>());
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"[InventoryTUI] Error: {ex.Message}");
                System.Console.Error.WriteLine($"[InventoryTUI] Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                    System.Console.Error.WriteLine($"[InventoryTUI] Inner: {ex.InnerException}");
            }
            finally
            {
                RestoreTerminalState();
                OnResumeConsole?.Invoke();

                ActiveHandler = null;
                _isRunning = false;
            }
        }

        private static void RestoreTerminalState()
        {
            try
            {
                System.Console.Write("\x1b[?1049l"); // leave alternate screen
                System.Console.Write("\x1b[?25h");   // show cursor
                System.Console.Write("\x1b[?1000l"); // disable mouse click tracking
                System.Console.Write("\x1b[?1002l"); // disable mouse button tracking
                System.Console.Write("\x1b[?1003l"); // disable mouse any-event tracking
                System.Console.Write("\x1b[?1006l"); // disable SGR mouse mode
                System.Console.Write("\x1b[?2004l"); // disable bracketed paste
                System.Console.Write("\x1b[0m");     // reset attributes
                System.Console.Write("\x1b(B");      // reset character set
                System.Console.Out.Flush();

                try
                {
                    using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "stty",
                        Arguments = "sane",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    });
                    proc?.WaitForExit(2000);
                }
                catch { }
            }
            catch
            {
            }
        }
    }
}
