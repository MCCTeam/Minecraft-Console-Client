using System;
using System.Threading;
using Avalonia;
using Avalonia.Threading;
using Consolonia;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui
{
    /// <summary>
    /// Manages the lifecycle of the inventory TUI.
    /// In TUI mode: opens as a Consolonia dialog window.
    /// In classic mode: launches standalone Consolonia on a dedicated thread.
    /// </summary>
    public static class InventoryTuiHost
    {
        private static volatile bool _isRunning;
        private static bool _classicEverLaunched;

        public static McClient? ActiveHandler { get; private set; }
        public static int ActiveWindowId { get; private set; }

        public static bool IsRunning => _isRunning;

        /// <summary>
        /// Whether the TUI can be launched (classic mode has a one-shot limit).
        /// </summary>
        public static bool CanLaunch
        {
            get
            {
                if (_isRunning) return false;
                if (ConsoleIO.Backend is TuiConsoleBackend) return true;
                return !_classicEverLaunched;
            }
        }

        /// <summary>
        /// Called before standalone TUI takes over the terminal (classic mode only).
        /// </summary>
        public static Action? OnSuspendConsole { get; set; }

        /// <summary>
        /// Called after standalone TUI releases the terminal (classic mode only).
        /// </summary>
        public static Action? OnResumeConsole { get; set; }

        public static bool Launch(McClient handler, int windowId)
        {
            if (_isRunning)
                return false;

            Container? container = handler.GetInventory(windowId);
            if (container == null)
                return false;

            _isRunning = true;
            ActiveHandler = handler;
            ActiveWindowId = windowId;

            if (ConsoleIO.Backend is TuiConsoleBackend)
            {
                LaunchAsDialog();
            }
            else
            {
                if (_classicEverLaunched)
                {
                    _isRunning = false;
                    ActiveHandler = null;
                    return false;
                }
                var tuiThread = new Thread(RunClassicTui) { Name = "InventoryTUI", IsBackground = false };
                tuiThread.Start();
            }

            return true;
        }

        /// <summary>
        /// Open inventory as an overlay panel within the main TUI view.
        /// </summary>
        private static void LaunchAsDialog()
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var view = TuiConsoleBackend.Instance?.GetView();
                    if (view != null)
                    {
                        var content = new InventoryMainView();
                        view.ShowOverlay(content, () =>
                        {
                            ActiveHandler = null;
                            _isRunning = false;
                        });
                    }
                    else
                    {
                        ActiveHandler = null;
                        _isRunning = false;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleIO.WriteLineFormatted($"§c[InventoryTUI] Error: {ex.Message}");
                    ConsoleIO.WriteLineFormatted($"§c[InventoryTUI] Stack: {ex.StackTrace}");
                    if (ex.InnerException != null)
                        ConsoleIO.WriteLineFormatted($"§c[InventoryTUI] Inner: {ex.InnerException.Message}");
                    ActiveHandler = null;
                    _isRunning = false;
                }
            });
        }

        /// <summary>
        /// Classic mode: run standalone Consolonia on a dedicated thread.
        /// </summary>
        private static void RunClassicTui()
        {
            try
            {
                OnSuspendConsole?.Invoke();
                _classicEverLaunched = true;

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
                System.Console.Write("\x1b[?1049l");
                System.Console.Write("\x1b[?25h");
                System.Console.Write("\x1b[?1000l");
                System.Console.Write("\x1b[?1002l");
                System.Console.Write("\x1b[?1003l");
                System.Console.Write("\x1b[?1006l");
                System.Console.Write("\x1b[?2004l");
                System.Console.Write("\x1b[0m");
                System.Console.Write("\x1b(B");
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
            catch { }
        }
    }
}
