using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftClient
{
    /// <summary>
    /// Allows simultaneous console input and output without breaking user input
    /// (Without having this annoying behaviour : User inp[Some Console output]ut)
    /// Provide some fancy features such as formatted output, text pasting and tab-completion.
    /// By ORelio - (c) 2012-2018 - Available under the CDDL-1.0 license
    /// </summary>
    public static class ConsoleIO
    {
        private static IAutoComplete? autocomplete_engine;

        /// <summary>
        /// Reset the IO mechanism and clear all buffers
        /// </summary>
        public static void Reset()
        {
            ClearLineAndBuffer();
        }

        /// <summary>
        /// Set an auto-completion engine for TAB autocompletion.
        /// </summary>
        /// <param name="engine">Engine implementing the IAutoComplete interface</param>
        public static void SetAutoCompleteEngine(IAutoComplete engine)
        {
            autocomplete_engine = engine;
        }

        /// <summary>
        /// Determines whether to use interactive IO or basic IO.
        /// Set to true to disable interactive command prompt and use the default Console.Read|Write() methods.
        /// Color codes are printed as is when BasicIO is enabled.
        /// </summary>
        public static bool BasicIO = false;

        /// <summary>
        /// Determines whether not to print color codes in BasicIO mode.
        /// </summary>
        public static bool BasicIO_NoColor = false;

        /// <summary>
        /// Determine whether WriteLineFormatted() should prepend lines with timestamps by default.
        /// </summary>
        public static bool EnableTimestamps = false;

        /// <summary>
        /// Specify a generic log line prefix for WriteLogLine()
        /// </summary>
        public static string LogPrefix = "§8[Log] ";

        /// <summary>
        /// Read a password from the standard input
        /// </summary>
        public static string? ReadPassword()
        {
            if (BasicIO)
                return Console.ReadLine();
            else
            {
                ConsoleInteractive.ConsoleReader.SetInputVisible(false);
                var input = ConsoleInteractive.ConsoleReader.RequestImmediateInput();
                ConsoleInteractive.ConsoleReader.SetInputVisible(true);
                return input;
            }
        }

        /// <summary>
        /// Read a line from the standard input
        /// </summary>
        public static string ReadLine()
        {
            if (BasicIO)
                return Console.ReadLine() ?? String.Empty;
            else
                return ConsoleInteractive.ConsoleReader.RequestImmediateInput();
        }

        /// <summary>
        /// Debug routine: print all keys pressed in the console
        /// </summary>
        public static void DebugReadInput()
        {
            ConsoleKeyInfo k;
            while (true)
            {
                k = Console.ReadKey(true);
                Console.WriteLine("Key: {0}\tChar: {1}\tModifiers: {2}", k.Key, k.KeyChar, k.Modifiers);
            }
        }

        /// <summary>
        /// Write a string to the standard output with a trailing newline
        /// </summary>
        public static void WriteLine(string line)
        {
            if (BasicIO)
                Console.WriteLine(line);
            else
                ConsoleInteractive.ConsoleWriter.WriteLine(line);
        }

        /// <summary>
        /// Write a Minecraft-Like formatted string to the standard output, using §c color codes
        /// See minecraft.gamepedia.com/Classic_server_protocol#Color_Codes for more info
        /// </summary>
        /// <param name="str">String to write</param>
        /// <param name="acceptnewlines">If false, space are printed instead of newlines</param>
        /// <param name="displayTimestamp">
        /// If false, no timestamp is prepended.
        /// If true, "hh-mm-ss" timestamp will be prepended.
        /// If unspecified, value is retrieved from EnableTimestamps.
        /// </param>
        public static void WriteLineFormatted(string str, bool acceptnewlines = false, bool? displayTimestamp = null)
        {
            StringBuilder output = new();

            if (!String.IsNullOrEmpty(str))
            {
                displayTimestamp ??= EnableTimestamps;
                if (displayTimestamp.Value)
                {
                    int hour = DateTime.Now.Hour, minute = DateTime.Now.Minute, second = DateTime.Now.Second;
                    output.Append(String.Format("{0}:{1}:{2} ", hour.ToString("00"), minute.ToString("00"), second.ToString("00")));
                }
                if (!acceptnewlines)
                {
                    str = str.Replace('\n', ' ');
                }
                if (BasicIO)
                {
                    if (BasicIO_NoColor)
                    {
                        output.Append(ChatBot.GetVerbatim(str));
                    }
                    else
                    {
                        output.Append(str);
                    }
                    Console.WriteLine(output.ToString());
                    return;
                }
                output.Append(str);
                ConsoleInteractive.ConsoleWriter.WriteLineFormatted(output.ToString());
            }
        }

        /// <summary>
        /// Write a prefixed log line. Prefix is set in LogPrefix.
        /// </summary>
        /// <param name="text">Text of the log line</param>
        /// <param name="acceptnewlines">Allow line breaks</param>
        public static void WriteLogLine(string text, bool acceptnewlines = true)
        {
            if (!acceptnewlines)
                text = text.Replace('\n', ' ');
            WriteLineFormatted(LogPrefix + text, acceptnewlines);
        }

        #region Subfunctions

        /// <summary>
        /// Clear all text inside the input prompt
        /// </summary>
        private static void ClearLineAndBuffer()
        {
            if (BasicIO) return;
            ConsoleInteractive.ConsoleReader.ClearBuffer();
        }


        #endregion

        public static void AutocompleteHandler(object? sender, ConsoleKey e)
        {
            if (e != ConsoleKey.Tab) return;

            if (autocomplete_engine == null)
                return;

            var buffer = ConsoleInteractive.ConsoleReader.GetBufferContent();
            autocomplete_engine.AutoComplete(buffer.Text[..buffer.CursorPosition]);
        }
    }

    /// <summary>
    /// Interface for TAB autocompletion
    /// Allows to use any object which has an AutoComplete() method using the IAutocomplete interface
    /// </summary>
    public interface IAutoComplete
    {
        /// <summary>
        /// Provide a list of auto-complete strings based on the provided input behing the cursor
        /// </summary>
        /// <param name="BehindCursor">Text behind the cursor, e.g. "my input comm"</param>
        /// <returns>List of auto-complete words, e.g. ["command", "comment"]</returns>
        IEnumerable<string> AutoComplete(string BehindCursor);
    }
}
