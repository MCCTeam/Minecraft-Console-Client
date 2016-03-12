using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Set : Command
    {
        public override string CMDName { get { return "set"; } }
        public override string CMDDesc { get { return "set varname=value: set a custom %variable%."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (hasArg(command))
            {
                string[] temp = getArg(command).Split('=');
                if (temp.Length > 1)
                {
                    if (Settings.SetVar(temp[0], getArg(command).Substring(temp[0].Length + 1)))
                    {
                        return ""; //Success
                    }
                    else return "variable name must be A-Za-z0-9.";
                }
                else return CMDDesc;
            }
            else return CMDDesc;
        }
    }
}
