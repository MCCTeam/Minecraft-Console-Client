using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class Fishing : Command
    {
        public override string CMDName { get { return "fishing"; } }
        public override string CMDDesc { get { return "fishing <on|off>: Auto fishing"; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (hasArg(command))
            {
                string state = getArg(command);
                if (state.ToLower() == "on")
                {
                    if (!handler.AutoFishing)
                    {
                        handler.AutoFishing = true;
                        handler.useItemOnHand();
                        return "Auto fishing turned on.";
                    }
                    else
                    {
                        return "Auto fishing is on.";
                    }
                }
                else if (state.ToLower() == "off")
                {
                    if (handler.AutoFishing)
                    {
                        handler.AutoFishing = false;
                        handler.useItemOnHand();
                        return "Auto fishing turned off.";
                    }
                    else
                    {
                        return "Auto fishing is off.";
                    }
                }
            }
            return CMDDesc;
        }
    }
}
