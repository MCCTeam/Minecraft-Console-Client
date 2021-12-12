using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace MinecraftClient.Commands
{
    class SetRnd : Command
    {
        public override string CmdName { get { return "setrnd"; } }
        public override string CmdUsage { get { return "setrnd varname -7to10 OR string1,string2,string3"; } }
        public override string CmdDesc { get { return "cmd.set.desc"; } }
        private static readonly Random rand = new Random();

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                string[] args = getArg(command).Split(' ');
                if (args.Length > 1)
                {
                    if (args.Length == 2 && args[1].Contains("to"))
                    {
                        int num1;
                        int num2;

                        try
                        {
                            num1 = Convert.ToInt32(args[1].Substring(0, args[1].IndexOf('t')));
                            num2 = Convert.ToInt32(args[1].Substring(args[1].IndexOf('o') + 1, args[1].Length - 1 - args[1].IndexOf('o')));
                        }
                        catch (Exception)
                        {
                            return "Unknown syntax";
                        }

                        if (num2 < num1)
                        {
                            int temp = num1;
                            num1 = num2;
                            num2 = temp;
                        }

                        if (Settings.SetVar(args[0], rand.Next(num1, num2)))
                        {
                            return string.Format("Set %{0}% to {1}.", args[0], Settings.GetVar(args[0])); //Success
                        }
                        else return Translations.Get("cmd.set.format");
                    }
                    else
                    {
                        var test = command.IndexOf(args[0]) + args[0].Length;
                        var test1 = command.Length - 8 - args[0].Length;
                        string argString = command.Substring(test, test1);
                        List<string> values = parseCommandLine(argString);

                        if (Settings.SetVar(args[0], values[rand.Next(0, values.Count)]))
                        {
                            return string.Format("Set %{0}% to {1}.", args[0], Settings.GetVar(args[0])); //Success
                        }
                        else return Translations.Get("cmd.set.format");
                    }
                }
                else return GetCmdDescTranslated();
            }
            else return GetCmdDescTranslated();
        }

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
