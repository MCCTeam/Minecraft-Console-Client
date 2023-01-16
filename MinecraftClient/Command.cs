using System;
using System.Collections.Generic;
using System.Text;
using Brigadier.NET;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;

namespace MinecraftClient
{
    /// <summary>
    /// Represents an internal MCC command: Command name, source code and usage message
    /// To add a new command, inherit from this class while adding the command class to the folder "Commands".
    /// If inheriting from the 'Command' class and placed in the 'Commands' namespace, the command will be
    /// automatically loaded and available in main chat prompt, scripts, remote control and command help.
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// The command name
        /// </summary>
        public abstract string CmdName { get; }

        /// <summary>
        /// Command description with translation support. Please add your message in Translations.cs file and return mapping key in this property
        /// </summary>
        public abstract string CmdDesc { get; }

        /// <summary>
        /// Get the translated version of command description.
        /// </summary>
        /// <returns>Translated command description</returns>
        public string GetCmdDescTranslated()
        {
            char cmdChar = Settings.Config.Main.Advanced.InternalCmdChar.ToChar();

            StringBuilder sb = new();
            string s = (string.IsNullOrEmpty(CmdUsage) || string.IsNullOrEmpty(CmdDesc)) ? string.Empty : ": "; // If either one is empty, no colon :
            sb.Append("§e").Append(cmdChar).Append(CmdUsage).Append("§r").Append(s).AppendLine(CmdDesc);
            sb.Append(McClient.dispatcher.GetAllUsageString(CmdName, false));
            return sb.ToString();
        }

        /// <summary>
        /// Usage message, eg: 'name [args]'
        /// </summary>
        public abstract string CmdUsage { get; }

        /// <summary>
        /// Register the command.
        /// </summary>
        public abstract void RegisterCommand(CommandDispatcher<CmdResult> dispatcher);

        /// <summary>
        /// Check if at least one argument has been passed to the command
        /// </summary>
        public static bool HasArg(string command)
        {
            int first_space = command.IndexOf(' ');
            return (first_space > 0 && first_space < command.Length - 1);
        }

        /// <summary>
        /// Extract the argument string from the command
        /// </summary>
        /// <returns>Argument or "" if no argument</returns>
        public static string GetArg(string command)
        {
            if (HasArg(command))
                return command[(command.IndexOf(' ') + 1)..];
            else
                return string.Empty;
        }

        /// <summary>
        /// Extract the arguments as a string array from the command
        /// </summary>
        /// <returns>Argument array or empty array if no arguments</returns>
        public static string[] GetArgs(string command)
        {
            string[] args = GetArg(command).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 1 && args[0] == string.Empty)
                return Array.Empty<string>();
            else
                return args;
        }

        /// <summary>
        /// Extract arguments from a given string. Allows quotines and escaping them. 
        /// Similar to command line arguments in regular terminals.
        /// </summary>
        /// <param name="cmdLine">Provided arguments as a string</param>
        /// <returns>All extracted arguments in a string list</returns>
        public static List<string> ParseCommandLine(string cmdLine)
        {
            var args = new List<string>();
            if (string.IsNullOrWhiteSpace(cmdLine)) return args;

            var currentArg = new StringBuilder();
            bool inQuotedArg = false;

            for (int i = 0; i < cmdLine.Length; i++)
            {
                if ((cmdLine[i] == '"' && i > 0 && cmdLine[i - 1] != '\\') || (cmdLine[i] == '"' && i == 0))
                {
                    if (inQuotedArg)
                    {
                        args.Add(currentArg.ToString());
                        currentArg = new StringBuilder();
                        inQuotedArg = false;
                    }
                    else
                    {
                        inQuotedArg = true;
                    }
                }
                else if (cmdLine[i] == ' ')
                {
                    if (inQuotedArg)
                    {
                        currentArg.Append(cmdLine[i]);
                    }
                    else if (currentArg.Length > 0)
                    {
                        args.Add(currentArg.ToString());
                        currentArg = new StringBuilder();
                    }
                }
                else
                {
                    if (cmdLine[i] == '\\' && cmdLine[i + 1] == '\"')
                    {
                        currentArg.Append('"');
                        i += 1;
                    }
                    else
                    {
                        currentArg.Append(cmdLine[i]);
                    }
                }
            }

            if (currentArg.Length > 0) args.Add(currentArg.ToString());

            return args;
        }
    }
}
