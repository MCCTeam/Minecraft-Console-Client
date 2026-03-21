using System;
using System.Diagnostics;
using System.Threading;

namespace ConsoleInteractive {
    public static class ConsoleReader {
        public static bool DisplayUesrInput { get; set; } = true;

        /// <summary>
        /// Invoked when a message is received.
        /// </summary>
        public static event EventHandler<string>? MessageReceived;

        private static Buffer LastInputBuffer = new(string.Empty, 0);
        public static event EventHandler<Buffer>? OnInputChange;

        private static Thread? _readerThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static object ThreadLock = new();

        public static void SetInputVisible(bool visible) {
            InternalContext.SuppressInput = !visible;
        }

        public static Buffer GetBufferContent() {
            lock (InternalContext.UserInputBufferLock)
                return new Buffer(ConsoleBuffer.UserInputBuffer.ToString()!, ConsoleBuffer.BufferPosition);
        }

        public static void ClearBuffer() {
            ConsoleBuffer.FlushBuffer();
        }

        internal static void CheckInputBufferUpdate() {
            ConsoleSuggestion.OnInputUpdate();
            Buffer InputBuffer = GetBufferContent();
            if (InputBuffer != LastInputBuffer) {
                LastInputBuffer = InputBuffer;
                int cursorPos = InputBuffer.CursorPosition;
                for (int i = 0; i < InputBuffer.CursorPosition; i++)
                    if (InputBuffer.Text[i] == '\0')
                        --cursorPos;
                OnInputChange?.Invoke(null, new(InputBuffer.Text.Replace("\0", string.Empty), cursorPos));
            }
        }

        /// <summary>
        /// Starts a new Console Reader thread.
        /// </summary>
        /// <param name="cancellationToken">Exits from the reader thread when cancelled.</param>
        public static void BeginReadThread() {
            lock (ThreadLock) {
                if (_readerThread is { IsAlive: true }) {
                    throw new InvalidOperationException("Console Reader thread is already running.");
                }

                _cancellationTokenSource = new();
                _readerThread = new Thread(new ParameterizedThreadStart(KeyListener!)) {
                    Name = "ConsoleInteractive.ConsoleReader Reader Thread"
                };
                _readerThread.Start(_cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// Stops an existing Console Reader thread, if any.
        /// </summary>
        public static void StopReadThread() {
            lock (ThreadLock) {
                if (_readerThread is { IsAlive: false }) {
                    return;
                }

                _cancellationTokenSource?.Cancel();

                try { _readerThread!.Join(); }
                catch (Exception) { }

                InternalContext.BufferInitialized = false;
                ConsoleBuffer.ClearBackreadBuffer();
                ConsoleBuffer.FlushBuffer();
                ConsoleBuffer.ClearVisibleUserInput();
            }
        }

        public static string RequestImmediateInput() {
            lock (ThreadLock) {
                AutoResetEvent autoEvent = new(false);
                var bufferString = string.Empty;

                BeginReadThread();
                ConsoleBuffer.Init();
                MessageReceived += (sender, s) =>
                {
                    bufferString = s;
                    autoEvent.Set();
                };

                autoEvent.WaitOne();
                StopReadThread();
                InternalContext.BufferInitialized = false;
                return bufferString;
            }
        }

        /// <summary>
        /// Listens for keypresses and acts accordingly.
        /// </summary>
        /// <param name="cancellationToken">Exits from the key listener once cancelled.</param>
        private static void KeyListener(object cancellationToken) {
            CancellationToken token = (CancellationToken)cancellationToken!;
            ConsoleBuffer.Init();

            while (!token.IsCancellationRequested) {
                if (Console.IsInputRedirected) {
                    while (Console.In.Peek() == -1) {
                        if (token.IsCancellationRequested) return;
                        Thread.Sleep(8);
                    }

                    bool needCheckBufUpdate = false;

                    int charInt;
                    while ((charInt = Console.In.Read()) != -1) {
                        char c = (char)charInt;
                        if (c == '\n') {
                            OnEnter();
                            if (token.IsCancellationRequested) return;
                            needCheckBufUpdate = true;
                        } else if (c == '\t') {
                            ConsoleSuggestion.HandleTab();
                        } else {
                            if (ConsoleBuffer.Insert(c))
                                needCheckBufUpdate = true;
                        }

                        if (token.IsCancellationRequested) return;
                    }

                    if (needCheckBufUpdate)
                        CheckInputBufferUpdate();
                } else {
                    while (!Console.KeyAvailable) {
                        if (token.IsCancellationRequested) return;
                        Thread.Sleep(8);
                    }

                    bool needCheckBufUpdate = false;
                    while (Console.KeyAvailable) {
                        ConsoleKeyInfo k = Console.ReadKey(true);
                        if (token.IsCancellationRequested) return;

                        switch (k.Key) {
                            case ConsoleKey.Enter:
                                ConsoleSuggestion.HandleEnter();

                                OnEnter();

                                /*
                                 * The user can call cancellation after a command on enter.
                                 * This helps us safely exit the reader thread.
                                 */
                                if (token.IsCancellationRequested) return;
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.Backspace:
                                ConsoleBuffer.RemoveBackward(inWords: k.Modifiers == ConsoleModifiers.Control);
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.Delete:
                                ConsoleBuffer.RemoveForward(inWords: k.Modifiers == ConsoleModifiers.Control);
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.End:
                                ConsoleBuffer.MoveToEndBufferPosition();
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.Home:
                                ConsoleBuffer.MoveToStartBufferPosition();
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.LeftArrow:
                                ConsoleBuffer.MoveCursorBackward(inWords: k.Modifiers == ConsoleModifiers.Control);
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.RightArrow:
                                ConsoleBuffer.MoveCursorForward(inWords: k.Modifiers == ConsoleModifiers.Control);
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.UpArrow:
                                if (ConsoleSuggestion.HandleUpArrow()) break;
                                lock (InternalContext.BackreadBufferLock) {
                                    if (ConsoleBuffer.BackreadBuffer.Count == 0) break;

                                    var backread = ConsoleBuffer.GetBackreadBackwards();
                                    var backreadCopied = ConsoleBuffer.isCurrentBufferCopied;
                                    var backreadString = ConsoleBuffer.UserInputBufferCopy;
                                    ConsoleBuffer.SetBufferContent(backread);

                                    // SetBufferContent clears the backread, so we need to pass it again
                                    if (backreadCopied) {
                                        ConsoleBuffer.isCurrentBufferCopied = backreadCopied;
                                        ConsoleBuffer.UserInputBufferCopy = backreadString;

                                        Trace.Assert(ConsoleBuffer.isCurrentBufferCopied);
                                    }
                                }
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.DownArrow:
                                if (ConsoleSuggestion.HandleDownArrow()) break;
                                lock (InternalContext.BackreadBufferLock) {
                                    if (ConsoleBuffer.BackreadBuffer.Count == 0) break;

                                    var backread = ConsoleBuffer.GetBackreadForwards();
                                    var backreadCopied = ConsoleBuffer.isCurrentBufferCopied;
                                    var backreadString = ConsoleBuffer.UserInputBufferCopy;
                                    ConsoleBuffer.SetBufferContent(backread);


                                    // SetBufferContent clears the backread, so we need to pass it again
                                    if (backreadCopied) {
                                        ConsoleBuffer.isCurrentBufferCopied = backreadCopied;
                                        ConsoleBuffer.UserInputBufferCopy = backreadString;

                                        Trace.Assert(ConsoleBuffer.isCurrentBufferCopied);
                                    }
                                }
                                needCheckBufUpdate = true;

                                break;
                            case ConsoleKey.Tab:
                                ConsoleSuggestion.HandleTab();

                                break;
                            case ConsoleKey.Escape:
                                ConsoleSuggestion.HandleEscape();

                                break;
                            case ConsoleKey.P:
                                if (k.Modifiers == ConsoleModifiers.Control)
                                    ConsoleBuffer.PrintUserInput();
                                else
                                    goto default;

                                break;
                            default:
                                if (ConsoleBuffer.Insert(k.KeyChar))
                                    needCheckBufUpdate = true;

                                break;
                        }

                        if (token.IsCancellationRequested) return;
                    }

                    if (needCheckBufUpdate) {
                        ConsoleBuffer.RedrawInputArea();
                        CheckInputBufferUpdate();
                    }
                }
            }
            InternalContext.BufferInitialized = false;
        }

        private static void OnEnter() {
            var input = ConsoleBuffer.FlushBuffer();
            ConsoleBuffer.AddToBackreadBuffer(input);
            MessageReceived?.Invoke(null, input.Replace("\0", string.Empty));
        }

        public record Buffer {
            public string Text { get; init; }
            public int CursorPosition { get; init; }

            public Buffer(string text, int cursorPosition) {
                Text = text;
                CursorPosition = cursorPosition;
            }
        }
    }
}