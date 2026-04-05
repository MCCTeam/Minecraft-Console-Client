using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

        private Program.StartupState? _pendingStartupState;
        private readonly ManualResetEventSlim _viewReady = new(false);

        /// <summary>
        /// Initializes the Avalonia app and starts the main UI loop.
        /// This blocks the calling thread until the TUI exits.
        /// Before blocking, it starts MCC's remaining initialization on a background thread.
        /// </summary>
        internal void RunTuiMainLoop(string[] args, Program.StartupState startupState)
        {
            Instance = this;
            _pendingStartupState = startupState;

            AppDomain.CurrentDomain.ProcessExit += (_, _) => RestoreTerminalState();

            System.Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                var view = _view;
                if (view != null)
                    Dispatcher.UIThread.Post(() => view.HandleCtrlC());
            };

            _ = Task.Run(() =>
            {
                _viewReady.Wait();
                ContinueMccStartup(args);
            });

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
                var instance = Instance;
                if (instance?._pendingStartupState is { } state)
                {
                    instance._pendingStartupState = null;
                    if (!Program.ProcessStartupState(state))
                        return;
                }

                Program.RunStartupSequence(args);
            }
            catch (Exception ex)
            {
                ConsoleIO.WriteLineFormatted($"§c[MCC] Fatal: {ex.Message}");
            }
        }

        internal void SetView(MainTuiView view)
        {
            _view = view;
            _viewReady.Set();
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
            return RequestImmediateInputAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task<string> RequestImmediateInputAsync(CancellationToken cancellationToken)
        {
            if (_shutdownRequested)
            {
                return Task.FromCanceled<string>(cancellationToken.CanBeCanceled
                    ? cancellationToken
                    : new CancellationToken(canceled: true));
            }

            TaskCompletionSource<string> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? sender, string e)
            {
                MessageReceived -= Handler;
                completion.TrySetResult(e);
            }

            MessageReceived += Handler;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    MessageReceived -= Handler;
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            return completion.Task;
        }

        public string? ReadPassword()
        {
            return ReadPasswordAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<string?> ReadPasswordAsync(CancellationToken cancellationToken)
        {
            return await RequestImmediateInputAsync(cancellationToken);
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

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                Environment.Exit(0);
            });
        }

        private volatile bool _shutdownRequested;

        /// <summary>
        /// Called from the TUI view when user presses Enter in the command input.
        /// Always fires MessageReceived so that both the normal read-thread path
        /// and RequestImmediateInput (used by offline prompt) receive the input.
        /// </summary>
        internal void OnCommandSubmitted(string command)
        {
            MessageReceived?.Invoke(this, command);
        }

        /// <summary>
        /// Called from the TUI view when user types in the command input.
        /// </summary>
        internal void OnInputChanged(string text, int cursorPos)
        {
            OnInputChange?.Invoke(this, new ConsoleInputBuffer(text, cursorPos));
        }

        internal void UpdateSuggestions(CommandSuggestion[] suggestions, (int Start, int End) range)
        {
            var view = _view;
            if (view == null) return;

            if (Dispatcher.UIThread.CheckAccess())
                view.UpdateSuggestions(suggestions, range);
            else
                Dispatcher.UIThread.Post(() => view.UpdateSuggestions(suggestions, range));
        }

        internal void ClearSuggestions()
        {
            var view = _view;
            if (view == null) return;

            if (Dispatcher.UIThread.CheckAccess())
                view.ClearSuggestions();
            else
                Dispatcher.UIThread.Post(() => view.ClearSuggestions());
        }
    }
}
