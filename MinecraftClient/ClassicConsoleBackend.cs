using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace MinecraftClient
{
    /// <summary>
    /// Console backend wrapping the ConsoleInteractive library (existing behavior).
    /// </summary>
    public partial class ClassicConsoleBackend : IConsoleBackend
    {
        private static readonly (byte R, byte G, byte B, char Code)[] McStandardColors =
        [
            (0, 0, 0, '0'),       // black
            (0, 0, 170, '1'),     // dark_blue
            (0, 170, 0, '2'),     // dark_green
            (0, 170, 170, '3'),   // dark_aqua
            (170, 0, 0, '4'),     // dark_red
            (170, 0, 170, '5'),   // dark_purple
            (255, 170, 0, '6'),   // gold
            (170, 170, 170, '7'), // gray
            (85, 85, 85, '8'),    // dark_gray
            (85, 85, 255, '9'),   // blue
            (85, 255, 85, 'a'),   // green
            (85, 255, 255, 'b'),  // aqua
            (255, 85, 85, 'c'),   // red
            (255, 85, 255, 'd'),  // light_purple
            (255, 255, 85, 'e'),  // yellow
            (255, 255, 255, 'f'), // white
        ];

        [GeneratedRegex("§#([0-9a-fA-F]{6})")]
        private static partial Regex HexColorRegex();

        private static char NearestMcColor(byte r, byte g, byte b)
        {
            int bestIdx = 0;
            long bestDist = long.MaxValue;

            for (int i = 0; i < McStandardColors.Length; i++)
            {
                var (sr, sg, sb, _) = McStandardColors[i];
                long dr = r - sr;
                long dg = g - sg;
                long db = b - sb;
                long dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return McStandardColors[bestIdx].Code;
        }

        private static string ResolveHexColors(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains("§#", StringComparison.Ordinal))
                return text;

            return HexColorRegex().Replace(text, match =>
            {
                ReadOnlySpan<char> hex = match.Groups[1].ValueSpan;
                byte r = (byte)((HexVal(hex[0]) << 4) | HexVal(hex[1]));
                byte g = (byte)((HexVal(hex[2]) << 4) | HexVal(hex[3]));
                byte b = (byte)((HexVal(hex[4]) << 4) | HexVal(hex[5]));
                return ColorHelper.GetColorEscapeCode(r, g, b, foreground: true);
            });
        }

        private static int HexVal(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => 0
        };

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

        public void WriteLineFormatted(string text)
        {
            ConsoleInteractive.ConsoleWriter.WriteLineFormatted(ResolveHexColors(text));
        }

        public void BeginReadThread()
        {
            ConsoleInteractive.ConsoleReader.MessageReceived += ForwardMessage;
            ConsoleInteractive.ConsoleReader.OnInputChange += ForwardInputChange;
            ConsoleInteractive.ConsoleReader.BeginReadThread();
        }

        public void StopReadThread()
        {
            Thread stopThread = new(() =>
            {
                try
                {
                    ConsoleInteractive.ConsoleReader.StopReadThread();
                }
                catch
                {
                    // Best-effort shutdown; do not block the caller thread during reconnect/restart.
                }

                try
                {
                    ConsoleInteractive.ConsoleReader.MessageReceived -= ForwardMessage;
                    ConsoleInteractive.ConsoleReader.OnInputChange -= ForwardInputChange;
                }
                catch
                {
                    // Ignore detach failures.
                }
            })
            {
                IsBackground = true,
                Name = "ClassicConsoleBackend.StopReadThread"
            };
            stopThread.Start();
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

        public void ClearScreen()
        {
            Console.Clear();
            ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
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
