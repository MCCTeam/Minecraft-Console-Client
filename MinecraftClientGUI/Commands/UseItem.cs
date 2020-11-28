using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class UseItem : Command
    {
        public override string CmdName { get { return "useitem"; } }
        public override string CmdUsage { get { return "useitem"; } }
        public override string CmdDesc { get { return "cmd.useitem.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                handler.UseItemOnHand();
                return Translations.Get("cmd.useitem.use");
            }
            else return Translations.Get("extra.inventory_required");
        }
    }
}
