using System;
using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    class SetRnd : Command
    {
        public override string CmdName { get { return "setrnd"; } }
        public override string CmdUsage { get { return "setrnd varname -7to10 OR string1,string2,string3"; } }
        public override string CmdDesc { get { return "cmd.set.desc"; } }
        private static Random rand = new Random();

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                string[] args = getArg(command).Split(' ');
                if (args.Length > 1)
                {
                    if (args[1].Contains("to"))
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
                    else if (args[1].Contains(","))
                    {
                        string[] values = args[1].Split(',');

                        if (Settings.SetVar(args[0], values[rand.Next(0, values.Length)]))
                        {
                            return string.Format("Set %{0}% to {1}.", args[0], Settings.GetVar(args[0])); //Success
                        }
                        else return Translations.Get("cmd.set.format");
                    }
                    else 
                    {
                        return "Unknown syntax";
                    }
                }
                else return GetCmdDescTranslated();
            }
            else return GetCmdDescTranslated();
        }
    }
}
