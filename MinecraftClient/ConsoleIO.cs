using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

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
        private static IAutoComplete autocomplete_engine;
        private static LinkedList<string> autocomplete_words = new LinkedList<string>();
        private static LinkedList<string> previous = new LinkedList<string>();
        private static readonly object io_lock = new object();
        private static bool reading = false;
        private static string buffer = "";
        private static string buffer2 = "";

        /// <summary>
        /// Reset the IO mechanism and clear all buffers
        /// </summary>
        public static void Reset()
        {
            lock (io_lock)
            {
                if (reading)
                {
                    ClearLineAndBuffer();
                    reading = false;
                    Console.Write("\b \b");
                }
            }
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
        public static string ReadPassword()
        {
            StringBuilder password = new StringBuilder();

            ConsoleKeyInfo k;
            while ((k = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                switch (k.Key)
                {
                    case ConsoleKey.Backspace:
                        if (password.Length > 0)
                        {
                            Console.Write("\b \b");
                            password.Remove(password.Length - 1, 1);
                        }
                        break;

                    case ConsoleKey.Escape:
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.Home:
                    case ConsoleKey.End:
                    case ConsoleKey.Delete:
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.Tab:
                        break;

                    default:
                        if (k.KeyChar != 0)
                        {
                            Console.Write('*');
                            password.Append(k.KeyChar);
                        }
                        break;
                }
            }

            Console.WriteLine();
            return password.ToString();
        }

        /// <summary>
        /// Read a line from the standard input
        /// </summary>
        public static string ReadLine()
        {
            if (BasicIO)
            {
                return Console.ReadLine();
            }

            ConsoleKeyInfo k = new ConsoleKeyInfo();

            lock (io_lock)
            {
                Console.Write('>');
                reading = true;
                buffer = "";
                buffer2 = "";
            }

            while (k.Key != ConsoleKey.Enter)
            {
                k = Console.ReadKey(true);
                lock (io_lock)
                {
                    if (k.Key == ConsoleKey.V && k.Modifiers == ConsoleModifiers.Control)
                    {
                        string clip = ReadClipboard();
                        foreach (char c in clip)
                            AddChar(c);
                    }
                    else
                    {
                        switch (k.Key)
                        {
                            case ConsoleKey.Escape:
                                ClearLineAndBuffer();
                                break;
                            case ConsoleKey.Backspace:
                                RemoveOneChar();
                                break;
                            case ConsoleKey.Enter:
                                Console.Write('\n');
                                break;
                            case ConsoleKey.LeftArrow:
                                GoLeft();
                                break;
                            case ConsoleKey.RightArrow:
                                GoRight();
                                break;
                            case ConsoleKey.Home:
                                while (buffer.Length > 0) { GoLeft(); }
                                break;
                            case ConsoleKey.End:
                                while (buffer2.Length > 0) { GoRight(); }
                                break;
                            case ConsoleKey.Delete:
                                if (buffer2.Length > 0)
                                {
                                    GoRight();
                                    RemoveOneChar();
                                }
                                break;
                            case ConsoleKey.DownArrow:
                                if (previous.Count > 0)
                                {
                                    ClearLineAndBuffer();
                                    buffer = previous.First.Value;
                                    previous.AddLast(buffer);
                                    previous.RemoveFirst();
                                    Console.Write(buffer);
                                }
                                break;
                            case ConsoleKey.UpArrow:
                                if (previous.Count > 0)
                                {
                                    ClearLineAndBuffer();
                                    buffer = previous.Last.Value;
                                    previous.AddFirst(buffer);
                                    previous.RemoveLast();
                                    Console.Write(buffer);
                                }
                                break;
                            case ConsoleKey.Tab:
                                if (autocomplete_words.Count == 0 && autocomplete_engine != null && buffer.Length > 0)
                                    foreach (string result in autocomplete_engine.AutoComplete(buffer))
                                        autocomplete_words.AddLast(result);
                                string word_autocomplete = null;
                                if (autocomplete_words.Count > 0)
                                {
                                    word_autocomplete = autocomplete_words.First.Value;
                                    autocomplete_words.RemoveFirst();
                                    autocomplete_words.AddLast(word_autocomplete);
                                }
                                if (!String.IsNullOrEmpty(word_autocomplete) && word_autocomplete != buffer)
                                {
                                    while (buffer.Length > 0 && buffer[buffer.Length - 1] != ' ') { RemoveOneChar(); }
                                    foreach (char c in word_autocomplete) { AddChar(c); }
                                }
                                break;
                            default:
                                if (k.KeyChar != 0)
                                    AddChar(k.KeyChar);
                                break;
                        }
                    }
                    if (k.Key != ConsoleKey.Tab)
                        autocomplete_words.Clear();
                }
            }

            lock (io_lock)
            {
                reading = false;
                previous.AddLast(buffer + buffer2);
                return buffer + buffer2;
            }
        }

        /// <summary>
        /// Debug routine: print all keys pressed in the console
        /// </summary>
        public static void DebugReadInput()
        {
            ConsoleKeyInfo k = new ConsoleKeyInfo();
            while (true)
            {
                k = Console.ReadKey(true);
                Console.WriteLine("Key: {0}\tChar: {1}\tModifiers: {2}", k.Key, k.KeyChar, k.Modifiers);
            }
        }

        /// <summary>
        /// Write a string to the standard output, without newline character
        /// </summary>
        public static void Write(string text)
        {
            if (!BasicIO)
            {
                lock (io_lock)
                {
                    if (reading)
                    {
                        try
                        {
                            string buf = buffer;
                            string buf2 = buffer2;
                            ClearLineAndBuffer();
                            if (Console.CursorLeft == 0)
                            {
                                Console.CursorLeft = Console.BufferWidth - 1;
                                Console.CursorTop--;
                                Console.Write(' ');
                                Console.CursorLeft = Console.BufferWidth - 1;
                                Console.CursorTop--;
                            }
                            else Console.Write("\b \b");
                            Console.Write(text);
                            buffer = buf;
                            buffer2 = buf2;
                            Console.Write(">" + buffer);
                            if (buffer2.Length > 0)
                            {
                                Console.Write(buffer2 + " \b");
                                for (int i = 0; i < buffer2.Length; i++) { GoBack(); }
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //Console resized: Try again
                            Console.Write('\n');
                            Write(text);
                        }
                    }
                    else Console.Write(text);
                }
            }
            else Console.Write(text);
        }

        /// <summary>
        /// Write a string to the standard output with a trailing newline
        /// </summary>
        public static void WriteLine(string line)
        {
            Write(line + '\n');
        }

        /// <summary>
        /// Write a single character to the standard output
        /// </summary>
        public static void Write(char c)
        {
            Write("" + c);
        }

        /// <summary>
        /// Write a Minecraft-Like formatted string to the standard output, using §c color codes
        /// See minecraft.gamepedia.com/Classic_server_protocol#Color_Codes for more info
        /// </summary>
        /// <param name="str">String to write</param>
        /// <param name="acceptnewlines">If false, space are printed instead of newlines</param>
        /// <param name="displayTimestamps">
        /// If false, no timestamp is prepended.
        /// If true, "hh-mm-ss" timestamp will be prepended.
        /// If unspecified, value is retrieved from EnableTimestamps.
        /// </param>
        public static void WriteLineFormatted(string str, bool acceptnewlines = true, bool? displayTimestamp = null)
        {
            if (!String.IsNullOrEmpty(str))
            {
                if (!acceptnewlines)
                {
                    str = str.Replace('\n', ' ');
                }
                if (displayTimestamp == null)
                {
                    displayTimestamp = EnableTimestamps;
                }
                if (displayTimestamp.Value)
                {
                    int hour = DateTime.Now.Hour, minute = DateTime.Now.Minute, second = DateTime.Now.Second;
                    ConsoleIO.Write(String.Format("{0}:{1}:{2} ", hour.ToString("00"), minute.ToString("00"), second.ToString("00")));
                }
                if (BasicIO)
                {
                    Console.WriteLine(str);
                    return;
                }
                string[] parts = str.Split(new char[] { '§' });
                if (parts[0].Length > 0)
                {
                    ConsoleIO.Write(parts[0]);
                }
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].Length > 0)
                    {
                        switch (parts[i][0])
                        {
                            case '0': Console.ForegroundColor = ConsoleColor.Gray; break; //Should be Black but Black is non-readable on a black background
                            case '1': Console.ForegroundColor = ConsoleColor.DarkBlue; break;
                            case '2': Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                            case '3': Console.ForegroundColor = ConsoleColor.DarkCyan; break;
                            case '4': Console.ForegroundColor = ConsoleColor.DarkRed; break;
                            case '5': Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                            case '6': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                            case '7': Console.ForegroundColor = ConsoleColor.Gray; break;
                            case '8': Console.ForegroundColor = ConsoleColor.DarkGray; break;
                            case '9': Console.ForegroundColor = ConsoleColor.Blue; break;
                            case 'a': Console.ForegroundColor = ConsoleColor.Green; break;
                            case 'b': Console.ForegroundColor = ConsoleColor.Cyan; break;
                            case 'c': Console.ForegroundColor = ConsoleColor.Red; break;
                            case 'd': Console.ForegroundColor = ConsoleColor.Magenta; break;
                            case 'e': Console.ForegroundColor = ConsoleColor.Yellow; break;
                            case 'f': Console.ForegroundColor = ConsoleColor.White; break;
                            case 'r': Console.ForegroundColor = ConsoleColor.Gray; break;
                        }

                        if (parts[i].Length > 1)
                        {
                            ConsoleIO.Write(parts[i].Substring(1, parts[i].Length - 1));
                        }
                    }
                }
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            ConsoleIO.Write('\n');
        }

        /// <summary>
        /// Write a prefixed log line. Prefix is set in LogPrefix.
        /// </summary>
        /// <param name="text">Text of the log line</param>
        public static void WriteLogLine(string text)
        {
            WriteLineFormatted(LogPrefix + text);
        }

        #region Subfunctions

        /// <summary>
        /// Clear all text inside the input prompt
        /// </summary>
        private static void ClearLineAndBuffer()
        {
            while (buffer2.Length > 0)
            {
                GoRight();
            }
            while (buffer.Length > 0)
            {
                RemoveOneChar();
            }
        }

        /// <summary>
        /// Remove one character on the left of the cursor in input prompt
        /// </summary>
        private static void RemoveOneChar()
        {
            if (buffer.Length > 0)
            {
                try
                {
                    if (Console.CursorLeft == 0)
                    {
                        Console.CursorLeft = Console.BufferWidth - 1;
                        if (Console.CursorTop > 0)
                            Console.CursorTop--;
                        Console.Write(' ');
                        Console.CursorLeft = Console.BufferWidth - 1;
                        if (Console.CursorTop > 0)
                            Console.CursorTop--;
                    }
                    else Console.Write("\b \b");
                }
                catch (ArgumentOutOfRangeException) { /* Console was resized!? */ }
                buffer = buffer.Substring(0, buffer.Length - 1);

                if (buffer2.Length > 0)
                {
                    Console.Write(buffer2 + " \b");
                    for (int i = 0; i < buffer2.Length; i++)
                    {
                        GoBack();
                    }
                }
            }
        }

        /// <summary>
        /// Move the cursor one character to the left inside the console, regardless of input prompt state
        /// </summary>
        private static void GoBack()
        {
            try
            {
                if (Console.CursorLeft == 0)
                {
                    Console.CursorLeft = Console.BufferWidth - 1;
                    if (Console.CursorTop > 0)
                        Console.CursorTop--;
                }
                else Console.Write('\b');
            }
            catch (ArgumentOutOfRangeException) { /* Console was resized!? */ }
        }

        /// <summary>
        /// Move the cursor one character to the left in input prompt, adjusting buffers accordingly
        /// </summary>
        private static void GoLeft()
        {
            if (buffer.Length > 0)
            {
                buffer2 = "" + buffer[buffer.Length - 1] + buffer2;
                buffer = buffer.Substring(0, buffer.Length - 1);
                Console.Write('\b');
            }
        }

        /// <summary>
        /// Move the cursor one character to the right in input prompt, adjusting buffers accordingly
        /// </summary>
        private static void GoRight()
        {
            if (buffer2.Length > 0)
            {
                buffer = buffer + buffer2[0];
                Console.Write(buffer2[0]);
                buffer2 = buffer2.Substring(1);
            }
        }

        /// <summary>
        /// Insert a new character in the input prompt
        /// </summary>
        /// <param name="c">New character</param>
        private static void AddChar(char c)
        {
            Console.Write(c);
            buffer += c;
            Console.Write(buffer2);
            for (int i = 0; i < buffer2.Length; i++)
            {
                GoBack();
            }
        }

        #endregion

        #region Clipboard management

        /// <summary>
        /// Read a string from the Windows clipboard
        /// </summary>
        /// <returns>String from the Windows clipboard</returns>
        private static string ReadClipboard()
        {
            string clipdata = "";
            Thread staThread = new Thread(new ThreadStart(
                delegate
                {
                    try
                    {
                        clipdata = Clipboard.GetText();
                    }
                    catch { }
                }
            ));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
            return clipdata;
        }

        #endregion
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
