using System.Collections.Generic;
using Brigadier.NET;

namespace MinecraftClient.Commands
{
    class UseItem : Command
    {
        public override string CmdName { get { return "useitem"; } }
        public override string CmdUsage { get { return "useitem"; } }
        public override string CmdDesc { get { return Translations.cmd_useitem_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                handler.UseItemOnHand();
                return Translations.cmd_useitem_use;
            }
            else return Translations.extra_inventory_required;
        }
    }
}
