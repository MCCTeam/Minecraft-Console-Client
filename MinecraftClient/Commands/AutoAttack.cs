using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class AutoAttack : Command
    {
        public override string CMDName { get { return "autoattack"; } }
        public override string CMDDesc { get { return "autoattack <on|off>: Auto attack mobs around you."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (hasArg(command))
            {
                string arg = getArg(command);
                if (arg == "on")
                {
                    if (handler.GetEntityHandlingEnabled())
                    {
                        // TODO: check if the bot already loaded
                        // I don't know how :C
                        handler.BotLoad(new ChatBots.AutoAttack());
                        return "Auto Attack now enabled.";
                    }
                    else
                    {
                        return "Please enable EntityHandling in the config before using this.";
                    }
                }
                else if(arg == "off")
                {
                    // TODO: unload auto attack bot
                    // I don't know how to unload a single bot :C
                    return "Currently not implemented :/";
                }
            }
            return CMDDesc;
        }
    }
}
