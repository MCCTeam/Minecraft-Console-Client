using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class ChangeSlot : Command
    {
        public override string CMDName { get { return "changeslot"; } }
        public override string CMDDesc { get { return "changeslot <1-9>: Change hotbar"; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (!handler.GetInventoryEnabled()) return "Please enable InventoryHandling in the config file first.";
            if (hasArg(command))
            {
                short slot;
                try
                {
                    slot = Convert.ToInt16(getArg(command));
                }
                catch (FormatException)
                {
                    return "Could not change slot: Not a Number";
                }
                if (slot >= 1 && slot <= 9)
                {
                    if (handler.ChangeSlot(slot-=1))
                    {
                        return "Changed to slot " + (slot+=1);
                    }
                    else
                    {
                        return "Could not change slot";
                    }
                }
            }
            return CMDDesc;
        }
    }
}
