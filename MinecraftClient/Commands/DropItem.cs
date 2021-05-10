using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class DropItem : Command
    {
        public override string CmdName { get { return "dropitem"; } }

        public override string CmdDesc { get { return "cmd.dropItem.desc"; } }

        public override string CmdUsage { get { return "/dropitem <itemtype>"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (!handler.GetInventoryEnabled())
            {
                return Translations.Get("extra.inventory_required");
            }
            if (hasArg(command))
            {
                string arg = getArg(command);
                ItemType itemType;
                if (Enum.TryParse(arg, true, out itemType))
                {
                    int inventoryId;
                    var inventories = handler.GetInventories();
                    List<int> availableIds = inventories.Keys.ToList();
                    availableIds.Remove(0); // remove player inventory ID from list
                    if (availableIds.Count == 1)
                        inventoryId = availableIds[0]; // one container, use it
                    else
                        inventoryId = 0;
                    var p = inventories[inventoryId];
                    int[] targetItems = p.SearchItem(itemType);
                    foreach (int slot in targetItems)
                    {
                        handler.DoWindowAction(inventoryId, slot, WindowActionType.DropItemStack);
                    }
                    return Translations.Get("cmd.dropItem.dropped", itemType.ToString(), inventoryId);
                }
                else
                {
                    return Translations.Get("cmd.dropItem.unknown_item", arg);
                }
            }
            else
            {
                return CmdUsage;
            }
        }
    }
}
