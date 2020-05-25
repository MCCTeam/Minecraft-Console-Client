using MinecraftClient.Protocol.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Animation : Command
    {
        public override string CMDName { get { return "animation"; } }
        public override string CMDDesc { get { return "animation  <<mainhand|offhand>|<0|1>>"; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                string[] args = getArgs(command);
                if (args.Length > 0)
                { 
                    string anim = args[0];
                    if (anim == "mainhand" || anim == "0")
                    {
                        handler.DoAnimation(0);
                    }
                    else if (anim == "offhand" || anim == "1")
                    {
                        handler.DoAnimation(1);
                    }
                    else
                    {
                        return CMDDesc;
                    }
                }
            }
            return CMDDesc;
        }
    }
}
