using System.Collections.Generic;
using System.Linq;
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

                Container? enchantingTable = null;

                foreach (var (id, container) in handler.GetInventories())
                {
                    if (container.Type == ContainerType.Enchantment)
                    {
                        enchantingTable = container;
                        break;
                    }
                }

                if (enchantingTable == null)
                    return Translations.TryGet("cmd.enchant.enchanting_table_not_opened");

                int[] emptySlots = enchantingTable.GetEmpytSlots();

                if (emptySlots.Contains(0))
                    return Translations.TryGet("cmd.enchant.enchanting_no_item");

                if (emptySlots.Contains(1))
                    return Translations.TryGet("cmd.enchant.enchanting_no_lapis");

                Item lapisSlot = enchantingTable.Items[1];

                if (lapisSlot.Type != ItemType.LapisLazuli)
                    return Translations.TryGet("cmd.enchant.enchanting_no_lapis");

                if (lapisSlot.Count < 3)
                    return Translations.TryGet("cmd.enchant.enchanting_no_lapis");

                return handler.ClickContainerButton(enchantingTable.ID, slotId) ? Translations.TryGet("cmd.enchant.clicked") : Translations.TryGet("cmd.enchant.not_clicked");
            }

            return GetCmdDescTranslated();
        }
    }
}
