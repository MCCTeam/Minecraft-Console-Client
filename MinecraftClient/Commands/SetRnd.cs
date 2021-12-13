using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace MinecraftClient.Commands
{
    class SetRnd : Command
    {
        public override string CmdName { get { return "setrnd"; } }
        public override string CmdUsage { get { return Translations.Get("cmd.setrnd.format"); } }
        public override string CmdDesc { get { return "cmd.setrnd.desc"; } }
        private static readonly Random rand = new Random();

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                string[] args = getArg(command).Split(' ');

                if (args[0] == "player")
                {
                    if (args.Length > 1)
                        return Translations.Get("cmd.setrndplayer.format");

                    var test = handler.GetUsername();
                    string[] playernames = handler.GetOnlinePlayers().Where(name => name != handler.GetUsername()).ToArray();
                    if (playernames.Length > 0)
                    {
                        Settings.SetVar("player", playernames[rand.Next(0, playernames.Length)]);
                        return string.Format("Set %{0}% to {1}.", "player", Settings.GetVar("player"));
                    }
                    else { return Translations.Get("cmd.setrndplayer.lonely"); }
                }
                else if (args.Length > 1)
                {
                    // detect "to" keyword in string
                    if (args.Length == 2 && args[1].Contains("to"))
                    {
                        int num1;
                        int num2;

                        // try to extract the two numbers from the string
                        try
                        {
                            num1 = Convert.ToInt32(args[1].Substring(0, args[1].IndexOf('t')));
                            num2 = Convert.ToInt32(args[1].Substring(args[1].IndexOf('o') + 1, args[1].Length - 1 - args[1].IndexOf('o')));
                        }
                        catch (Exception)
                        {
                            return Translations.Get("cmd.setrndnum.format");
                        }

                        // switch the values if they were entered in the wrong way
                        if (num2 < num1)
                        {
                            int temp = num1;
                            num1 = num2;
                            num2 = temp;
                        }

                        // create a variable or set it to num1 <= varlue < num2
                        if (Settings.SetVar(args[0], rand.Next(num1, num2)))
                        {
                            return string.Format("Set %{0}% to {1}.", args[0], Settings.GetVar(args[0])); //Success
                        }
                        else return Translations.Get("cmd.setrndnum.format");
                    }
                    else
                    {
                        // extract all arguments of the command
                        string argString = command.Substring(command.IndexOf(args[0]) + args[0].Length, command.Length - 8 - args[0].Length);

                        // process all arguments similar to regular terminals with quotes and escaping
                        List<string> values = parseCommandLine(argString);

                        // create a variable or set it to one of the values
                        if (values.Count > 0 && Settings.SetVar(args[0], values[rand.Next(0, values.Count)]))
                        {
                            return string.Format("Set %{0}% to {1}.", args[0], Settings.GetVar(args[0])); //Success
                        }
                        else return Translations.Get("cmd.setrndstr.format");
                    }
                }
                else return GetCmdDescTranslated();
            }
            else return GetCmdDescTranslated();
        }

        /// <summary>
        /// Extract arguments from a given string. Allows quotines and escaping them. 
        /// Similar to command line arguments in regular terminals.
        /// </summary>
        /// <param name="cmdLine">Provided arguments as a string</param>
        /// <returns>All extracted arguments in a string list</returns>
        private static List<string> parseCommandLine(string cmdLine)
        {
            var args = new List<string>();
            if (string.IsNullOrWhiteSpace(cmdLine)) return args;

            var currentArg = new StringBuilder();
            bool inQuotedArg = false;

            for (int i = 0; i < cmdLine.Length; i++)
            {
                if (cmdLine[i] == '"' && cmdLine[i-1] != '\\')
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
                        currentArg.Append("\"");
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
