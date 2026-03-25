using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Threading;
using Consolonia;

namespace MinecraftClient.Tui
{
    /// <summary>
    /// Console backend that uses Avalonia/Consolonia for a full-screen TUI.
    /// Avalonia Dispatcher runs on the main thread; MCC logic runs on background threads.
    /// </summary>
    public class TuiConsoleBackend : IConsoleBackend
    {
        public event EventHandler<string>? MessageReceived;
        public event EventHandler<ConsoleInputBuffer>? OnInputChange;

        private MainTuiView? _view;
        private volatile bool _readThreadActive;

        public bool DisplayUserInput { get; set; } = true;

        internal static TuiConsoleBackend? Instance { get; private set; }

        /// <summary>
        /// Initializes the Avalonia app and starts the main UI loop.
        /// This blocks the calling thread until the TUI exits.
        /// Before blocking, it starts MCC's remaining initialization on a background thread.
        /// </summary>
        public void RunTuiMainLoop(string[] args)
        {
            Instance = this;

            AppDomain.CurrentDomain.ProcessExit += (_, _) => RestoreTerminalState();

            System.Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                var view = _view;
                if (view != null)
                    Dispatcher.UIThread.Post(() => view.HandleCtrlC());
            };

            new Thread(() =>
            {
                Thread.Sleep(500);
                ContinueMccStartup(args);
            })
            { Name = "MCC-Main", IsBackground = true }.Start();

            AppBuilder builder = AppBuilder.Configure<MccTuiApp>()
                .UseConsolonia()
                .UseAutoDetectedConsole()
                .LogToException();

            try
            {
                builder.StartWithConsoleLifetime(Array.Empty<string>());
            }
            finally
            {
                RestoreTerminalState();
            }
        }

        private static volatile bool _terminalRestored;

        private static void RestoreTerminalState()
        {
            if (_terminalRestored) return;
            _terminalRestored = true;

            try
            {
                System.Console.Write("\x1b[?1000l"); // disable X11 mouse
                System.Console.Write("\x1b[?1001l"); // disable highlight mouse
                System.Console.Write("\x1b[?1002l"); // disable button-event mouse
                System.Console.Write("\x1b[?1003l"); // disable any-event mouse
                System.Console.Write("\x1b[?1004l"); // disable focus events
                System.Console.Write("\x1b[?1005l"); // disable UTF-8 mouse encoding
                System.Console.Write("\x1b[?1006l"); // disable SGR mouse encoding
                System.Console.Write("\x1b[?1015l"); // disable urxvt mouse encoding
                System.Console.Write("\x1b[?1049l"); // leave alternate screen
                System.Console.Write("\x1b[?25h");   // show cursor
                System.Console.Write("\x1b[?7h");    // re-enable line wrap
                System.Console.Write("\x1b[0m");     // reset attributes
                System.Console.Write("\x1b[2J");     // clear entire screen
                System.Console.Write("\x1b[H");      // cursor to home
                System.Console.Out.Flush();
            }
            catch { }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using var proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = "stty",
                        Arguments = "sane",
                        UseShellExecute = false,
                    });
                    proc?.WaitForExit(500);
                }
                catch { }
            }
        }

        private static void ContinueMccStartup(string[] args)
        {
            try
            {
                Program.ContinueAfterTuiInit(args);
            }
            catch (Exception ex)
            {
                ConsoleIO.WriteLineFormatted($"§c[MCC] Fatal: {ex.Message}");
            }
        }

        internal void SetView(MainTuiView view)
        {
            _view = view;
        }

        internal MainTuiView? GetView() => _view;

        public void Init()
        {
        }

        public void WriteLine(string text)
        {
            var view = _view;
            if (view == null)
            {
                System.Console.WriteLine(text);
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
                view.AppendLogLine(text);
            else
                Dispatcher.UIThread.Post(() => view.AppendLogLine(text));
        }

        public void WriteLineFormatted(string text)
        {
            var view = _view;
            if (view == null)
            {
                System.Console.WriteLine(Scripting.ChatBot.GetVerbatim(text));
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
                view.AppendFormattedLogLine(text);
            else
                Dispatcher.UIThread.Post(() => view.AppendFormattedLogLine(text));
        }

        public void BeginReadThread()
        {
            _readThreadActive = true;
        }

        public void StopReadThread()
        {
            _readThreadActive = false;
            DismissOverlay();
        }

        /// <summary>
        /// Close any open overlay (e.g. inventory) so the user can interact
        /// with the main console again. Safe to call from any thread.
        /// </summary>
        internal void DismissOverlay()
        {
            var view = _view;
            if (view == null) return;

            if (Dispatcher.UIThread.CheckAccess())
            {
                view.HideOverlay();
            }
            else
            {
                Dispatcher.UIThread.Post(() => view.HideOverlay());
            }
        }

        public string RequestImmediateInput()
        {
            if (_shutdownRequested)
            {
                Thread.Sleep(Timeout.Infinite);
                return string.Empty;
            }

            var mre = new ManualResetEventSlim(false);
            string? result = null;

            void Handler(object? sender, string e)
            {
                result = e;
                mre.Set();
            }

            MessageReceived += Handler;
            mre.Wait();
            MessageReceived -= Handler;

            return result ?? string.Empty;
        }

        public string? ReadPassword()
        {
            return RequestImmediateInput();
        }

        public void ClearInputBuffer()
        {
            if (_view == null) return;
            if (Dispatcher.UIThread.CheckAccess())
                _view.ClearInput();
            else
                Dispatcher.UIThread.Post(() => _view?.ClearInput());
        }

        public void SetInputVisible(bool visible)
        {
        }

        public void SetBackreadBufferLimit(int limit)
        {
        }

        public void Shutdown()
        {
            _shutdownRequested = true;
            RestoreTerminalState();

            var lifetime = Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IControlledApplicationLifetime;

            if (lifetime != null)
            {
                if (Dispatcher.UIThread.CheckAccess())
                    lifetime.Shutdown();
                else
                    Dispatcher.UIThread.Post(() => lifetime.Shutdown());
            }

            new Thread(() =>
            {
                Thread.Sleep(500);
                Environment.Exit(0);
            }) { Name = "TUI-Exit-Guard", IsBackground = true }.Start();
        }

        private volatile bool _shutdownRequested;

        /// <summary>
        /// Called from the TUI view when user presses Enter in the command input.
        /// Always fires MessageReceived so that both the normal read-thread path
        /// and RequestImmediateInput (used by offline prompt) receive the input.
        /// </summary>
        internal void OnCommandSubmitted(string command)
        {
            OnInputChange?.Invoke(this, new ConsoleInputBuffer(command, command.Length));
            MessageReceived?.Invoke(this, command);
        }

        /// <summary>
        /// Called from the TUI view when user types in the command input.
        /// </summary>
        internal void OnInputChanged(string text, int cursorPos)
        {
            OnInputChange?.Invoke(this, new ConsoleInputBuffer(text, cursorPos));
        }
    }
}
