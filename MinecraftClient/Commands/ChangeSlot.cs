using System;
using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    class ChangeSlot : Command
    {
        public override string CmdName { get { return "changeslot"; } }
        public override string CmdUsage { get { return "changeslot <1-9>"; } }
        public override string CmdDesc { get { return Translations.cmd_changeSlot_desc; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (!handler.GetInventoryEnabled())
                return Translations.extra_inventory_required;

            if (HasArg(command))
            {
                short slot;
                try
                {
                    slot = Convert.ToInt16(GetArg(command));
                }
                catch (FormatException)
                {
                    return Translations.cmd_changeSlot_nan;
                }
                if (slot >= 1 && slot <= 9)
                {
                    if (handler.ChangeSlot(slot -= 1))
                    {
                        return string.Format(Translations.cmd_changeSlot_changed, (slot += 1));
                    }
                    else
                    {
                        return Translations.cmd_changeSlot_fail;
                    }
                }
            }
            return GetCmdDescTranslated();
        }
    }
}
