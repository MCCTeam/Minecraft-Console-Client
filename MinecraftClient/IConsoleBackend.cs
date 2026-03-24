using System;

namespace MinecraftClient
{
    /// <summary>
    /// Input buffer state passed with OnInputChange events.
    /// </summary>
    public readonly struct ConsoleInputBuffer
    {
        public string Text { get; }
        public int CursorPosition { get; }

        public ConsoleInputBuffer(string text, int cursorPosition)
        {
            Text = text;
            CursorPosition = cursorPosition;
        }
    }

    /// <summary>
    /// Abstraction over the console I/O backend.
    /// Implementations: ClassicConsoleBackend (ConsoleInteractive), TuiConsoleBackend (Avalonia/Consolonia), BasicConsoleBackend (stdio).
    /// </summary>
    public interface IConsoleBackend
    {
        void Init();

        void WriteLine(string text);

        void WriteLineFormatted(string text);

        void BeginReadThread();

        void StopReadThread();

        event EventHandler<string>? MessageReceived;

        event EventHandler<ConsoleInputBuffer>? OnInputChange;

        string RequestImmediateInput();

        string? ReadPassword();

        void ClearInputBuffer();

        bool DisplayUserInput { get; set; }

        void SetInputVisible(bool visible);

        void SetBackreadBufferLimit(int limit);

        void Shutdown();
    }
}
