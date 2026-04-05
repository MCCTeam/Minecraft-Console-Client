using System;
using System.Text.RegularExpressions;
using static MinecraftClient.Settings.ConsoleConfigHealper.ConsoleConfig;

namespace MinecraftClient
{
    /// <summary>
    /// Console backend wrapping the ConsoleInteractive library (existing behavior).
    /// </summary>
    public class ClassicConsoleBackend : IConsoleBackend
    {
        public event EventHandler<string>? MessageReceived;
        public event EventHandler<ConsoleInputBuffer>? OnInputChange;

        public bool DisplayUserInput
        {
            get => ConsoleInteractive.ConsoleReader.DisplayUesrInput;
            set => ConsoleInteractive.ConsoleReader.DisplayUesrInput = value;
        }

        public void Init()
        {
            ConsoleInteractive.ConsoleWriter.Init();
        }

        public void WriteLine(string text)
        {
            ConsoleInteractive.ConsoleWriter.WriteLine(text);
        }

        private static readonly Regex HexColorRegex = new(@"§#([0-9a-fA-F]{6})", RegexOptions.Compiled);

        public void WriteLineFormatted(string text)
        {
            bool hasHex = text.Contains("§#");
            if (hasHex)
                text = ResolveHexColors(text);
            ConsoleInteractive.ConsoleWriter.WriteLineFormatted(text);
        }

        private static string ResolveHexColors(string text)
        {
            var mode = Settings.Config.Console.General.ConsoleColorMode;
            return HexColorRegex.Replace(text, match =>
            {
                ReadOnlySpan<char> hex = match.Groups[1].ValueSpan;
                byte r = Convert.ToByte(hex[..2].ToString(), 16);
                byte g = Convert.ToByte(hex[2..4].ToString(), 16);
                byte b = Convert.ToByte(hex[4..6].ToString(), 16);
                return ColorHelper.GetColorEscapeCode(r, g, b, foreground: true, mode);
            });
        }

        public void BeginReadThread()
        {
            ConsoleInteractive.ConsoleReader.MessageReceived += ForwardMessage;
            ConsoleInteractive.ConsoleReader.OnInputChange += ForwardInputChange;
            ConsoleInteractive.ConsoleReader.BeginReadThread();
        }

        public void StopReadThread()
        {
            ConsoleInteractive.ConsoleReader.StopReadThread();
            ConsoleInteractive.ConsoleReader.MessageReceived -= ForwardMessage;
            ConsoleInteractive.ConsoleReader.OnInputChange -= ForwardInputChange;
        }

        public string RequestImmediateInput()
        {
            return ConsoleInteractive.ConsoleReader.RequestImmediateInput();
        }

        public string? ReadPassword()
        {
            ConsoleInteractive.ConsoleReader.SetInputVisible(false);
            var input = ConsoleInteractive.ConsoleReader.RequestImmediateInput();
            ConsoleInteractive.ConsoleReader.SetInputVisible(true);
            return input;
        }

        public void ClearInputBuffer()
        {
            ConsoleInteractive.ConsoleReader.ClearBuffer();
        }

        public void SetInputVisible(bool visible)
        {
            ConsoleInteractive.ConsoleReader.SetInputVisible(visible);
        }

        public void SetBackreadBufferLimit(int limit)
        {
            ConsoleInteractive.ConsoleBuffer.SetBackreadBufferLimit(limit);
        }

        public void Shutdown()
        {
            ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
        }

        #region Suggestion forwarding for classic mode

        public void UpdateSuggestions(
            ConsoleInteractive.ConsoleSuggestion.Suggestion[] suggestions,
            Tuple<int, int> range)
        {
            ConsoleInteractive.ConsoleSuggestion.UpdateSuggestions(suggestions, range);
        }

        public void ClearSuggestions()
        {
            ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
        }

        public void SetSuggestionColors(
            string textColor, string textBgColor,
            string hlTextColor, string hlTextBgColor,
            string tooltipColor, string hlTooltipColor,
            string arrowColor)
        {
            ConsoleInteractive.ConsoleSuggestion.SetColors(
                textColor, textBgColor,
                hlTextColor, hlTextBgColor,
                tooltipColor, hlTooltipColor,
                arrowColor);
        }

        public bool EnableSuggestionColor
        {
            get => ConsoleInteractive.ConsoleSuggestion.EnableColor;
            set => ConsoleInteractive.ConsoleSuggestion.EnableColor = value;
        }

        public bool Enable24bitColor
        {
            get => ConsoleInteractive.ConsoleSuggestion.Enable24bitColor;
            set => ConsoleInteractive.ConsoleSuggestion.Enable24bitColor = value;
        }

        public bool UseBasicArrow
        {
            get => ConsoleInteractive.ConsoleSuggestion.UseBasicArrow;
            set => ConsoleInteractive.ConsoleSuggestion.UseBasicArrow = value;
        }

        public int SetMaxSuggestionLength(int length)
        {
            return ConsoleInteractive.ConsoleSuggestion.SetMaxSuggestionLength(length);
        }

        public int SetMaxSuggestionCount(int count)
        {
            return ConsoleInteractive.ConsoleSuggestion.SetMaxSuggestionCount(count);
        }

        public bool EnableWriterColor
        {
            get => ConsoleInteractive.ConsoleWriter.EnableColor;
            set => ConsoleInteractive.ConsoleWriter.EnableColor = value;
        }

        public bool UseVT100ColorCode
        {
            get => ConsoleInteractive.ConsoleWriter.UseVT100ColorCode;
            set => ConsoleInteractive.ConsoleWriter.UseVT100ColorCode = value;
        }

        #endregion

        private void ForwardMessage(object? sender, string e)
        {
            MessageReceived?.Invoke(sender, e);
        }

        private void ForwardInputChange(object? sender, ConsoleInteractive.ConsoleReader.Buffer buffer)
        {
            OnInputChange?.Invoke(sender, new ConsoleInputBuffer(buffer.Text, buffer.CursorPosition));
        }
    }
}
