using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class AutoAttack : Command
    {
        public override string CMDName { get { return "autoattack"; } }
        public override string CMDDesc { get { return "autoattack <on|off>: Enable/disable auto attack mobs."; } }

        public override string Run(McTcpClient handler, string command)
        {
            if (hasArg(command))
            {
                string state = getArg(command);
                if (state.ToLower() == "on")
                {
                    if (!handler.AutoAttack)
                    {
                        handler.AutoAttack = true;
                        return "Auto attack turned on.";
                    }
                    else
                    {
                        return "Auto attack is on.";
                    }
                }else if (state.ToLower() == "off")
                {
                    if (handler.AutoAttack)
                    {
                        handler.AutoAttack = false;
                        return "Auto attack turned off.";
                    }
                    else
                    {
                        return "Auto attack is off.";
                    }
                }
                return "";
            }
            else return CMDDesc;
        }
    }
}
