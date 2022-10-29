using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    public class Enchant : Command
    {
        public override string CmdName { get { return "enchant"; } }
        public override string CmdUsage { get { return "enchant <top|middle|bottom>"; } }
        public override string CmdDesc { get { return Translations.cmd_enchant_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (!handler.GetInventoryEnabled())
                return Translations.error_inventoryhandling_not_enabled;

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
                    return Translations.cmd_enchant_invalid_slot;

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
                    return Translations.cmd_enchant_enchanting_table_not_opened;

                int[] emptySlots = enchantingTable.GetEmpytSlots();

                if (emptySlots.Contains(0))
                    return Translations.cmd_enchant_enchanting_no_item;

                if (emptySlots.Contains(1))
                    return Translations.cmd_enchant_enchanting_no_lapis;

                Item lapisSlot = enchantingTable.Items[1];

                if (lapisSlot.Type != ItemType.LapisLazuli)
                    return Translations.cmd_enchant_enchanting_no_lapis;

                if (lapisSlot.Count < 3)
                    return Translations.cmd_enchant_enchanting_no_lapis;

                EnchantmentData? enchantment = handler.GetLastEnchantments();

                if (enchantment == null)
                    return Translations.cmd_enchant_no_enchantments;

                short requiredLevel = slotId switch
                {
                    0 => enchantment.TopEnchantmentLevelRequirement,
                    1 => enchantment.MiddleEnchantmentLevelRequirement,
                    2 => enchantment.BottomEnchantmentLevelRequirement,
                    _ => 9999
                };

                if (handler.GetLevel() < requiredLevel)
                    return string.Format(Translations.cmd_enchant_no_levels, handler.GetLevel(), requiredLevel);

                return handler.ClickContainerButton(enchantingTable.ID, slotId) ? Translations.cmd_enchant_clicked : Translations.cmd_enchant_not_clicked;
            }

            return GetCmdDescTranslated();
        }
    }
}
