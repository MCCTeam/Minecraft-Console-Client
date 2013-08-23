using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EcoCityCraftClient
{
    /// <summary>
    /// Allows simultaneous console input and output without breaking user input
    /// (Without having this annoying behaviour : User inp[Some Console output]ut)
    /// </summary>

    public static class ConsoleIO
    {
        public static void Reset() { if (reading) { reading = false; Console.Write("\b \b"); } }
        public static void SetAutoCompleteEngine(IAutoComplete engine) { autocomplete_engine = engine; }
        private static IAutoComplete autocomplete_engine;
        private static LinkedList<string> previous = new LinkedList<string>();
        private static string buffer = "";
        private static string buffer2 = "";
        private static bool reading = false;
        private static bool reading_lock = false;
        private static bool writing_lock = false;

        #region Read User Input
        public static string ReadLine()
        {
            ConsoleKeyInfo k = new ConsoleKeyInfo();
            Console.Write('>');
            reading = true;
            buffer = "";
            buffer2 = "";

            while (k.Key != ConsoleKey.Enter)
            {
                k = Console.ReadKey(true);
                while (writing_lock) { }
                reading_lock = true;
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
                    case ConsoleKey.Oem6:
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
                        if (autocomplete_engine != null && buffer.Length > 0)
                        {
                            string[] tmp = buffer.Split(' ');
                            if (tmp.Length > 0)
                            {
                                string word_tocomplete = tmp[tmp.Length - 1];
                                string word_autocomplete = autocomplete_engine.AutoComplete(word_tocomplete);
                                if (!String.IsNullOrEmpty(word_autocomplete) && word_autocomplete != word_tocomplete)
                                {
                                    while (buffer.Length > 0 && buffer[buffer.Length - 1] != ' ') { RemoveOneChar(); }
                                    foreach (char c in word_autocomplete) { AddChar(c); }
                                }
                            }
                        }
                        break;
                    default:
                        AddChar(k.KeyChar);
                        break;
                }
                reading_lock = false;
            }
            while (writing_lock) { }
            reading = false;
            previous.AddLast(buffer + buffer2);
            return buffer + buffer2;
        }
        #endregion

        #region Console Output
        public static void Write(string text)
        {
            while (reading_lock) { }
            writing_lock = true;
            if (reading)
            {
                ConsoleColor fore = Console.ForegroundColor;
                ConsoleColor back = Console.BackgroundColor;
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
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.BackgroundColor = ConsoleColor.Black;
                buffer = buf;
                buffer2 = buf2;
                Console.Write(">" + buffer);
                if (buffer2.Length > 0)
                {
                    Console.Write(buffer2 + " \b");
                    for (int i = 0; i < buffer2.Length; i++) { GoBack(); }
                }
                Console.ForegroundColor = fore;
                Console.BackgroundColor = back;
            }
            else Console.Write(text);
            writing_lock = false;
        }

        public static void WriteLine(string line)
        {
            Write(line + '\n');
        }

        public static void Write(char c)
        {
            Write("" + c);
        }
        #endregion

        #region subfunctions
        private static void ClearLineAndBuffer()
        {
            while (buffer2.Length > 0) { GoRight(); }
            while (buffer.Length > 0) { RemoveOneChar(); }
        }
        private static void RemoveOneChar()
        {
            if (buffer.Length > 0)
            {
                if (Console.CursorLeft == 0)
                {
                    Console.CursorLeft = Console.BufferWidth - 1;
                    Console.CursorTop--;
                    Console.Write(' ');
                    Console.CursorLeft = Console.BufferWidth - 1;
                    Console.CursorTop--;
                }
                else Console.Write("\b \b");
                buffer = buffer.Substring(0, buffer.Length - 1);

                if (buffer2.Length > 0)
                {
                    Console.Write(buffer2 + " \b");
                    for (int i = 0; i < buffer2.Length; i++) { GoBack(); }
                }
            }
        }
        private static void GoBack()
        {
            if (Console.CursorLeft == 0)
            {
                Console.CursorLeft = Console.BufferWidth - 1;
                Console.CursorTop--;
            }
            else Console.Write('\b');
        }
        private static void GoLeft()
        {
            if (buffer.Length > 0)
            {
                buffer2 = "" + buffer[buffer.Length - 1] + buffer2;
                buffer = buffer.Substring(0, buffer.Length - 1);
                Console.Write('\b');
            }
        }
        private static void GoRight()
        {
            if (buffer2.Length > 0)
            {
                buffer = buffer + buffer2[0];
                Console.Write(buffer2[0]);
                buffer2 = buffer2.Substring(1);
            }
        }
        private static void AddChar(char c)
        {
            Console.Write(c);
            buffer += c;
            Console.Write(buffer2);
            for (int i = 0; i < buffer2.Length; i++) { GoBack(); }
        }
        #endregion
    }

    /// <summary>
    /// Interface for TAB autocompletion
    /// Allows to use any object which has an AutoComplete() method using the IAutocomplete interface
    /// </summary>

    public interface IAutoComplete
    {
        string AutoComplete(string BehindCursor);
    }
}
