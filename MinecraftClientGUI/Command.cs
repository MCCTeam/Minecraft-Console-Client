using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            string s = string.IsNullOrEmpty(CmdUsage) || string.IsNullOrEmpty(CmdDesc) ? "" : ": "; // If either one is empty, no colon :
            return CmdUsage + s + Translations.TryGet(CmdDesc);
        }

        /// <summary>
        /// Usage message, eg: 'name [args]'
        /// </summary>
        public abstract string CmdUsage { get; }

        /// <summary>
        /// Perform the command
        /// </summary>
        /// <param name="command">The full command, eg: 'mycommand arg1 arg2'</param>
        /// <param name="localVars">Local variables passed along with the command (may be null)</param>
        /// <returns>A confirmation/error message, or "" if no message</returns>
        public abstract string Run(McClient handler, string command, Dictionary<string, object> localVars);

        /// <summary>
        /// Return a list of aliases for this command.
        /// Override this method if you wish to put aliases to the command
        /// </summary>
        public virtual IEnumerable<string> getCMDAliases() { return new string[0]; }

        /// <summary>
        /// Check if at least one argument has been passed to the command
        /// </summary>
        public static bool hasArg(string command)
        {
            int first_space = command.IndexOf(' ');
            return (first_space > 0 && first_space < command.Length - 1);
        }

        /// <summary>
        /// Extract the argument string from the command
        /// </summary>
        /// <returns>Argument or "" if no argument</returns>
        public static string getArg(string command)
        {
            if (hasArg(command))
            {
                return command.Substring(command.IndexOf(' ') + 1);
            }
            else return "";
        }

        /// <summary>
        /// Extract the arguments as a string array from the command
        /// </summary>
        /// <returns>Argument array or empty array if no arguments</returns>
        public static string[] getArgs(string command)
        {
            string[] args = getArg(command).Split(' ');
            if (args.Length == 1 && args[0] == "")
            {
                return new string[] { };
            }
            else
            {
                return args;
            }
        }
    }
}
