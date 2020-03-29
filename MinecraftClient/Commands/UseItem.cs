using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class UseItem : Command
    {
        public override string CMDName { get { return "useitem"; } }
        public override string CMDDesc { get { return "useitem: Use (left click) an item on the hand"; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                handler.UseItemOnHand();
                return "Used an item";
            }
            else return "Please enable inventoryhandling in config to use this command.";
        }
    }
}
