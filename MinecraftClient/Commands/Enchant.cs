using System.Collections.Generic;
using System.Globalization;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    public class Enchant : Command
    {
        public override string CmdName { get { return "enchant"; } }
        public override string CmdUsage { get { return "enchant <top|middle|bottom>"; } }
        public override string CmdDesc { get { return "cmd.enchant.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string slot = GetArg(command).ToLower().Trim();

                int slotId = slot switch
                {
                    "top" => 0,
                    "middle" => 1,
                    "bottom" => 2,
                    _ => -1
                };

                if (slotId == -1)
                    return Translations.TryGet("cmd.enchant.invalid_slot");

                int containerId = -1;

                foreach (var (id, container) in handler.GetInventories())
                {
                    if (container.Type == ContainerType.Enchantment)
                    {
                        containerId = id;
                        break;
                    }
                }

                if (containerId == -1)
                    return Translations.TryGet("cmd.enchant.enchanting_table_not_opened");

                return handler.ClickContainerButton(containerId, slotId) ? Translations.TryGet("cmd.enchant.clicked") : Translations.TryGet("cmd.enchant.not_clicked");
            }

            return GetCmdDescTranslated();
        }
    }
}
