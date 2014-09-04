using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    public class Connect : Command
    {
        public override string CMDName { get { return "connect"; } }
        public override string CMDDesc { get { return "connect <server> [account]: connect to the specified server."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (hasArg(command))
            {
                string[] args = getArgs(command);
                if (args.Length > 1)
                {
                    if (!Settings.setAccount(args[1]))
                    {
                        return "Unknown account '" + args[1] + "'.";
                    }
                }

                if (Settings.setServerIP(args[0]))
                {
                    Program.Restart();
                    return "";
                }
                else return "Invalid server IP '" + args[0] + "'.";
            }
            else return CMDDesc;
        }
    }
}
