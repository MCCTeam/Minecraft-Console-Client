using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class Inventory : Command
    {
        public override string CmdName { get { return "inventory"; } }
        public override string CmdUsage { get { return GetBasicUsage(); } }
        public override string CmdDesc { get { return "cmd.inventory.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                string[] args = GetArgs(command);
                if (args.Length >= 1)
                {
                    int inventoryId;
                    if (args[0].ToLower() == "creativegive")
                    {
                        if (args.Length >= 4)
                        {
                            if (!int.TryParse(args[1], out int slot))
                                return GetCmdDescTranslated();

                            if (Enum.TryParse(args[2], true, out ItemType itemType))
                            {
                                if (handler.GetGamemode() == 1)
                                {
                                    if (!int.TryParse(args[3], out int count))
                                        return GetCmdDescTranslated();

                                    if (handler.DoCreativeGive(slot, itemType, count, null))
                                        return Translations.Get("cmd.inventory.creative_done", itemType, count, slot);
                                    else
                                        return Translations.Get("cmd.inventory.creative_fail");
                                }
                                else
                                    return Translations.Get("cmd.inventory.need_creative");
                            }
                            else
                                return GetCmdDescTranslated();
                        }
                        else
                            return GetCmdDescTranslated();
                    }
                    else if (args[0].ToLower() == "creativedelete")
                    {
                        if (args.Length >= 2)
                        {
                            if (!int.TryParse(args[1], out int slot))
                                return GetCmdDescTranslated();

                            if (handler.GetGamemode() == 1)
                            {
                                if (handler.DoCreativeGive(slot, ItemType.Null, 0, null))
                                    return Translations.Get("cmd.inventory.creative_delete", slot);
                                else
                                    return Translations.Get("cmd.inventory.creative_fail");
                            }
                            else
                                return Translations.Get("cmd.inventory.need_creative");
                        }
                        else
                            return GetCmdDescTranslated();
                    }
                    else if (args[0].ToLower().StartsWith("p"))
                    {
                        // player inventory is always ID 0
                        inventoryId = 0;
                    }
                    else if (args[0].ToLower().StartsWith("c"))
                    {
                        List<int> availableIds = handler.GetInventories().Keys.ToList();
                        availableIds.Remove(0); // remove player inventory ID from list
                        if (availableIds.Count > 0)
                            inventoryId = availableIds.Max(); // use foreground container
                        else
                            return Translations.Get("cmd.inventory.container_not_found");
                    }
                    else if (args[0].ToLower().StartsWith("inventories") || args[0].ToLower().StartsWith("i"))
                    {
                        Dictionary<int, Container> inventories = handler.GetInventories();
                        List<int> availableIds = inventories.Keys.ToList();
                        StringBuilder response = new();
                        response.AppendLine(Translations.Get("cmd.inventory.inventories_available"));

                        foreach (int id in availableIds)
                            response.AppendLine(String.Format(" #{0} - {1}§8", id, inventories[id].Title));

                        return response.ToString();
                    }
                    else if (args[0].ToLower().StartsWith("search") || args[0].ToLower().StartsWith("s"))
                    {
                        if (args.Length < 2)
                            return GetCmdDescTranslated();

                        if (!Enum.TryParse(args[1], true, out ItemType parsedItemType))
                            return GetCmdDescTranslated();

                        bool shouldUseItemCount = args.Length >= 3;
                        int itemCount = 0;

                        if (shouldUseItemCount && !int.TryParse(args[2], out itemCount))
                            return GetCmdDescTranslated();

                        Dictionary<int, Container> inventories = handler.GetInventories();
                        Dictionary<int, List<Item>> foundItems = new();

                        List<Container> availableInventories = inventories.Values.ToList();

                        availableInventories.ForEach(inventory =>
                        {
                            inventory.Items.Values
                                .ToList()
                                .FindAll(item => item.Type == parsedItemType && (!shouldUseItemCount || item.Count == itemCount))
                                .ForEach(item =>
                                {
                                    if (!foundItems.ContainsKey(inventory.ID))
                                    {
                                        foundItems.Add(inventory.ID, new List<Item>() { item });
                                        return;
                                    }

                                    List<Item> invItems = foundItems[inventory.ID];
                                    invItems.Add(item);
                                    foundItems.Remove(inventory.ID);
                                    foundItems.Add(inventory.ID, invItems);
                                });
                        });

                        if (foundItems.Count == 0)
                            return Translations.Get("cmd.inventory.no_found_items");

                        StringBuilder response = new();

                        response.AppendLine(Translations.Get("cmd.inventory.found_items") + ":");

                        foreach ((int invId, List<Item> itemsList) in new SortedDictionary<int, List<Item>>(foundItems))
                        {
                            if (itemsList.Count > 0)
                            {
                                response.AppendLine(String.Format("{0} (#{1}):", inventories[invId].Title, invId));

                                foreach (Item item in itemsList)
                                    response.AppendLine(String.Format("\t- {0}", item.ToString()));

                                response.AppendLine(" ");
                            }
                        }

                        return response.ToString();
                    }
                    else if (args[0].ToLower() == "help")
                    {
                        if (args.Length >= 2)
                            return GetSubCommandHelp(args[1]);
                        else
                            return GetHelp();
                    }
                    else if (!int.TryParse(args[0], out inventoryId))
                        return GetCmdDescTranslated();

                    Container? inventory = handler.GetInventory(inventoryId);
                    if (inventory == null)
                        return Translations.Get("cmd.inventory.not_exist", inventoryId);

                    string action = args.Length > 1 ? args[1].ToLower() : "list";
                    if (action == "close")
                    {
                        if (handler.CloseInventory(inventoryId))
                            return Translations.Get("cmd.inventory.close", inventoryId);
                        else
                            return Translations.Get("cmd.inventory.close_fail", inventoryId);
                    }
                    else if (action == "list")
                    {
                        StringBuilder response = new();
                        response.Append(Translations.Get("cmd.inventory.inventory"));
                        response.AppendLine(String.Format(" #{0} - {1}§8", inventoryId, inventory.Title));

                        string? asciiArt = inventory.Type.GetAsciiArt();
                        if (asciiArt != null && Settings.DisplayInventoryLayout)
                            response.AppendLine(asciiArt);

                        int selectedHotbar = handler.GetCurrentSlot() + 1;
                        foreach ((int itemId, Item item) in new SortedDictionary<int, Item>(inventory.Items))
                        {
                            bool isHotbar = inventory.IsHotbar(itemId, out int hotbar);
                            string hotbarString = isHotbar ? (hotbar + 1).ToString() : " ";
                            if ((hotbar + 1) == selectedHotbar)
                                hotbarString = ">" + hotbarString;
                            response.AppendLine(String.Format("{0,2} | #{1,-2}: {2}", hotbarString, itemId, item.ToString()));
                        }

                        if (inventoryId == 0)
                            response.AppendLine(Translations.Get("cmd.inventory.hotbar", (handler.GetCurrentSlot() + 1)));

                        response.Remove(response.Length - 1, 1); // Remove last '\n'
                        return response.ToString();
                    }
                    else if (action == "click" && args.Length >= 3)
                    {
                        if (!int.TryParse(args[2], out int slot))
                            return GetCmdDescTranslated();

                        WindowActionType actionType = WindowActionType.LeftClick;
                        string keyName = "cmd.inventory.left";
                        if (args.Length >= 4)
                        {
                            string b = args[3];
                            if (b.ToLower()[0] == 'r')
                                (actionType, keyName) = (WindowActionType.RightClick, "cmd.inventory.right");
                            else if (b.ToLower()[0] == 'm')
                                (actionType, keyName) = (WindowActionType.MiddleClick, "cmd.inventory.middle");
                        }

                        handler.DoWindowAction(inventoryId, slot, actionType);
                        return Translations.Get("cmd.inventory.clicking", Translations.Get(keyName), slot, inventoryId);
                    }
                    else if (action == "shiftclick" && args.Length >= 3)
                    {
                        if (!int.TryParse(args[2], out int slot))
                            return GetCmdDescTranslated();

                        if (!handler.DoWindowAction(inventoryId, slot, WindowActionType.ShiftClick))
                            return Translations.Get("cmd.inventory.shiftclick_fail");

                        return Translations.Get("cmd.inventory.shiftclick", slot, inventoryId);
                    }
                    else if (action == "drop" && args.Length >= 3)
                    {
                        if (!int.TryParse(args[2], out int slot))
                            return GetCmdDescTranslated();

                        // check item exist
                        if (!inventory.Items.ContainsKey(slot))
                            return Translations.Get("cmd.inventory.no_item", slot);

                        WindowActionType actionType = WindowActionType.DropItem;
                        if (args.Length >= 4 && args[3].ToLower() == "all")
                            actionType = WindowActionType.DropItemStack;

                        if (handler.DoWindowAction(inventoryId, slot, actionType))
                        {
                            if (actionType == WindowActionType.DropItemStack)
                                return Translations.Get("cmd.inventory.drop_stack", slot);
                            else
                                return Translations.Get("cmd.inventory.drop", slot);
                        }
                        else
                            return "Failed";
                    }
                    else
                        return GetCmdDescTranslated();
                }
                else
                {
                    StringBuilder response = new();
                    response.Append(Translations.Get("cmd.inventory.inventories")).Append(":\n");
                    foreach ((int invId, Container inv) in handler.GetInventories())
                        response.AppendLine(String.Format(" #{0}: {1}§8", invId, inv.Title));
                    response.Append(CmdUsage);
                    return response.ToString();
                }
            }
            else
                return Translations.Get("extra.inventory_required");
        }

        #region Methods for commands help

        private static string GetAvailableActions()
        {
            return Translations.Get("cmd.inventory.help.available") + ": list, close, click, drop, creativegive, creativedelete.";
        }

        private static string GetBasicUsage()
        {
            return Translations.Get("cmd.inventory.help.basic") + ": /inventory <player|container|<id>> <action>.";
        }

        private static string GetHelp()
        {
            return Translations.Get("cmd.inventory.help.help", GetAvailableActions());
        }

        private static string GetSubCommandHelp(string cmd)
        {
            string usageStr = ' ' + Translations.Get("cmd.inventory.help.usage") + ": ";
            return cmd switch
            {
                "list" => Translations.Get("cmd.inventory.help.list") + usageStr + "/inventory <player|container|<id>> list",
                "close" => Translations.Get("cmd.inventory.help.close") + usageStr + "/inventory <player|container|<id>> close",
                "click" => Translations.Get("cmd.inventory.help.click") + usageStr + "/inventory <player|container|<id>> click <slot> [left|right|middle]\nDefault is left click",
                "shiftclick" => Translations.Get("cmd.inventory.help.shiftclick") + usageStr + "/inventory <player|container|<id>> shiftclick <slot>",
                "drop" => Translations.Get("cmd.inventory.help.drop") + usageStr + "/inventory <player|container|<id>> drop <slot> [all]\nAll means drop full stack",
                "creativegive" => Translations.Get("cmd.inventory.help.creativegive") + usageStr + "/inventory creativegive <slot> <itemtype> <amount>",
                "creativedelete" => Translations.Get("cmd.inventory.help.creativedelete") + usageStr + "/inventory creativedelete <slot>",
                "inventories" => Translations.Get("cmd.inventory.help.inventories") + usageStr + "/inventory inventories",
                "search" => Translations.Get("cmd.inventory.help.search") + usageStr + "/inventory search <item type> [count]",
                "help" => GetHelp(),
                _ => Translations.Get("cmd.inventory.help.unknown") + GetAvailableActions(),
            };
        }
        #endregion
    }
}
