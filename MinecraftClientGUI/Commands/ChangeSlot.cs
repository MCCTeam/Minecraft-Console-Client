using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Commands
{
    class ChangeSlot : Command
    {
        public override string CmdName { get { return "changeslot"; } }
        public override string CmdUsage { get { return "changeslot <1-9>"; } }
        public override string CmdDesc { get { return "cmd.changeSlot.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (!handler.GetInventoryEnabled())
                return Translations.Get("extra.inventory_required");

            if (hasArg(command))
            {
                short slot;
                try
                {
                    slot = Convert.ToInt16(getArg(command));
                }
                catch (FormatException)
                {
                    return Translations.Get("cmd.changeSlot.nan");
                }
                if (slot >= 1 && slot <= 9)
                {
                    if (handler.ChangeSlot(slot-=1))
                    {
                        return Translations.Get("cmd.changeSlot.changed", (slot+=1));
                    }
                    else
                    {
                        return Translations.Get("cmd.changeSlot.fail");
                    }
                }
            }
            return GetCmdDescTranslated();
        }
    }
}
