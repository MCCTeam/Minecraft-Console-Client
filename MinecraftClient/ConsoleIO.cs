using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Brigadier.NET;
using FuzzySharp;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;
using static MinecraftClient.Settings;

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
        /// <see href="https://minecraft.wiki/w/Classic_server_protocol#Color_codes"/> for more info
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

        internal static bool AutoCompleteDone = false;
        internal static string[] AutoCompleteResult = Array.Empty<string>();

        private static HashSet<string> Commands = new();
        private static string[] CommandsFromAutoComplete = Array.Empty<string>();
        private static string[] CommandsFromDeclareCommands = Array.Empty<string>();

        private static Task _latestTask = Task.CompletedTask;
        private static CancellationTokenSource? _cancellationTokenSource;

        private static void MccAutocompleteHandler(ConsoleInteractive.ConsoleReader.Buffer buffer)
        {
            string fullCommand = buffer.Text;
            if (string.IsNullOrEmpty(fullCommand))
            {
                ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
                return;
            }

            var InternalCmdChar = Config.Main.Advanced.InternalCmdChar;
            if (InternalCmdChar == MainConfigHealper.MainConfig.AdvancedConfig.InternalCmdCharType.none || fullCommand[0] == InternalCmdChar.ToChar())
            {
                int offset = InternalCmdChar == MainConfigHealper.MainConfig.AdvancedConfig.InternalCmdCharType.none ? 0 : 1;
                if (buffer.CursorPosition - offset < 0)
                {
                    ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
                    return;
                }
                _cancellationTokenSource?.Cancel();
                using var cts = new CancellationTokenSource();
                _cancellationTokenSource = cts;
                var previousTask = _latestTask;
                var newTask = new Task(async () =>
                {
                    string command = fullCommand[offset..];
                    if (command.Length == 0)
                    {
                        List<ConsoleInteractive.ConsoleSuggestion.Suggestion> sugList = new();

                        sugList.Add(new("/"));

                        var childs = McClient.dispatcher.GetRoot().Children;
                        if (childs != null)
                            foreach (var child in childs)
                                sugList.Add(new(child.Name));

                        foreach (var cmd in Commands)
                            sugList.Add(new(cmd));

                        ConsoleInteractive.ConsoleSuggestion.UpdateSuggestions(sugList.ToArray(), new(offset, offset));
                    }
                    else if (command.Length > 0 && command[0] == '/' && !command.Contains(' '))
                    {
                        var sorted = Process.ExtractSorted(command[1..], Commands);
                        var sugList = new ConsoleInteractive.ConsoleSuggestion.Suggestion[sorted.Count()];

                        int index = 0;
                        foreach (var sug in sorted)
                            sugList[index++] = new(sug.Value);
                        ConsoleInteractive.ConsoleSuggestion.UpdateSuggestions(sugList, new(offset, offset + command.Length));
                    }
                    else
                    {
                        CommandDispatcher<CmdResult>? dispatcher = McClient.dispatcher;
                        if (dispatcher == null)
                            return;

                        ParseResults<CmdResult> parse = dispatcher.Parse(command, CmdResult.Empty);

                        Brigadier.NET.Suggestion.Suggestions suggestions = await dispatcher.GetCompletionSuggestions(parse, buffer.CursorPosition - offset);

                        int sugLen = suggestions.List.Count;
                        if (sugLen == 0)
                        {
                            ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
                            return;
                        }

                        Dictionary<string, string?> dictionary = new();
                        foreach (var sug in suggestions.List)
                            dictionary.Add(sug.Text, sug.Tooltip?.String);

                        var sugList = new ConsoleInteractive.ConsoleSuggestion.Suggestion[sugLen];
                        if (cts.IsCancellationRequested)
                            return;

                        Tuple<int, int> range = new(suggestions.Range.Start + offset, suggestions.Range.End + offset);
                        var sorted = Process.ExtractSorted(fullCommand[range.Item1..range.Item2], dictionary.Keys);
                        if (cts.IsCancellationRequested)
                            return;

                        int index = 0;
                        foreach (var sug in sorted)
                            sugList[index++] = new(sug.Value, dictionary[sug.Value] ?? string.Empty);

                        ConsoleInteractive.ConsoleSuggestion.UpdateSuggestions(sugList, range);
                    }
                }, cts.Token);
                _latestTask = newTask;
                try { newTask.Start(); } catch { }
                if (_cancellationTokenSource == cts) _cancellationTokenSource = null;
            }
            else
            {
                ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
                return;
            }
        }

        public static void AutocompleteHandler(object? sender, ConsoleInteractive.ConsoleReader.Buffer buffer)
        {
            if (Settings.Config.Console.CommandSuggestion.Enable)
                MccAutocompleteHandler(buffer);
        }

        public static void CancelAutocomplete()
        {
            _cancellationTokenSource?.Cancel();
            _latestTask = Task.CompletedTask;
            ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();

            AutoCompleteDone = false;
            AutoCompleteResult = Array.Empty<string>();
            CommandsFromAutoComplete = Array.Empty<string>();
            CommandsFromDeclareCommands = Array.Empty<string>();
        }

        private static void MergeCommands()
        {
            Commands.Clear();
            foreach (string cmd in CommandsFromAutoComplete)
                Commands.Add('/' + cmd);
            foreach (string cmd in CommandsFromDeclareCommands)
                Commands.Add('/' + cmd);
        }

        public static void OnAutoCompleteDone(int transactionId, string[] result)
        {
            AutoCompleteResult = result;
            if (transactionId == 0)
            {
                CommandsFromAutoComplete = result;
                MergeCommands();
            }
            AutoCompleteDone = true;
        }

        public static void OnDeclareMinecraftCommand(string[] rootCommands)
        {
            CommandsFromDeclareCommands = rootCommands;
            MergeCommands();
        }

        public static void InitCommandList(CommandDispatcher<CmdResult> dispatcher)
        {
            autocomplete_engine!.AutoComplete("/");
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
        int AutoComplete(string BehindCursor);
    }
}
