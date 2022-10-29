using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Brigadier.NET;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class Inventory : Command
    {
        public override string CmdName { get { return "inventory"; } }
        public override string CmdUsage { get { return GetBasicUsage(); } }
        public override string CmdDesc { get { return Translations.cmd_inventory_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CommandSource> dispatcher)
        {
        }

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
                            if (!int.TryParse(args[1], NumberStyles.Any, CultureInfo.CurrentCulture, out int slot))
                                return GetCmdDescTranslated();

                            if (Enum.TryParse(args[2], true, out ItemType itemType))
                            {
                                if (handler.GetGamemode() == 1)
                                {
                                    if (!int.TryParse(args[3], NumberStyles.Any, CultureInfo.CurrentCulture, out int count))
                                        return GetCmdDescTranslated();

                                    if (handler.DoCreativeGive(slot, itemType, count, null))
                                        return string.Format(Translations.cmd_inventory_creative_done, itemType, count, slot);
                                    else
                                        return Translations.cmd_inventory_creative_fail;
                                }
                                else
                                    return Translations.cmd_inventory_need_creative;
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
                            if (!int.TryParse(args[1], NumberStyles.Any, CultureInfo.CurrentCulture, out int slot))
                                return GetCmdDescTranslated();

                            if (handler.GetGamemode() == 1)
                            {
                                if (handler.DoCreativeGive(slot, ItemType.Null, 0, null))
                                    return string.Format(Translations.cmd_inventory_creative_delete, slot);
                                else
                                    return Translations.cmd_inventory_creative_fail;
                            }
                            else
                                return Translations.cmd_inventory_need_creative;
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
                            return Translations.cmd_inventory_container_not_found;
                    }
                    else if (args[0].ToLower().StartsWith("inventories") || args[0].ToLower().StartsWith("i"))
                    {
                        Dictionary<int, Container> inventories = handler.GetInventories();
                        List<int> availableIds = inventories.Keys.ToList();
                        StringBuilder response = new();
                        response.AppendLine(Translations.cmd_inventory_inventories_available);

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

                        if (shouldUseItemCount && !int.TryParse(args[2], NumberStyles.Any, CultureInfo.CurrentCulture, out itemCount))
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
                            return Translations.cmd_inventory_no_found_items;

                        StringBuilder response = new();

                        response.AppendLine(Translations.cmd_inventory_found_items + ":");

                        foreach ((int invId, List<Item> itemsList) in new SortedDictionary<int, List<Item>>(foundItems))
                        {
                            if (itemsList.Count > 0)
                            {
                                response.AppendLine(String.Format("{0} (#{1}):", inventories[invId].Title, invId));

                                foreach (Item item in itemsList)
                                    response.AppendLine(String.Format("\t- {0}", item.ToFullString()));

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
                    else if (!int.TryParse(args[0], NumberStyles.Any, CultureInfo.CurrentCulture, out inventoryId))
                        return GetCmdDescTranslated();

                    Container? inventory = handler.GetInventory(inventoryId);
                    if (inventory == null)
                        return string.Format(Translations.cmd_inventory_not_exist, inventoryId);

                    string action = args.Length > 1 ? args[1].ToLower() : "list";
                    if (action == "close")
                    {
                        if (handler.CloseInventory(inventoryId))
                            return string.Format(Translations.cmd_inventory_close, inventoryId);
                        else
                            return string.Format(Translations.cmd_inventory_close_fail, inventoryId);
                    }
                    else if (action == "list")
                    {
                        StringBuilder response = new();
                        response.Append(Translations.cmd_inventory_inventory);
                        response.AppendLine(String.Format(" #{0} - {1}§8", inventoryId, inventory.Title));

                        string? asciiArt = inventory.Type.GetAsciiArt();
                        if (asciiArt != null && Settings.Config.Main.Advanced.ShowInventoryLayout)
                            response.AppendLine(asciiArt);

                        int selectedHotbar = handler.GetCurrentSlot() + 1;
                        foreach ((int itemId, Item item) in new SortedDictionary<int, Item>(inventory.Items))
                        {
                            bool isHotbar = inventory.IsHotbar(itemId, out int hotbar);
                            string hotbarString = isHotbar ? (hotbar + 1).ToString() : " ";
                            if ((hotbar + 1) == selectedHotbar)
                                hotbarString = ">" + hotbarString;
                            response.AppendLine(String.Format("{0,2} | #{1,-2}: {2}", hotbarString, itemId, item.ToFullString()));
                        }

                        if (inventoryId == 0)
                            response.AppendLine(string.Format(Translations.cmd_inventory_hotbar, (handler.GetCurrentSlot() + 1)));

                        response.Remove(response.Length - 1, 1); // Remove last '\n'
                        return response.ToString();
                    }
                    else if (action == "click" && args.Length >= 3)
                    {
                        if (!int.TryParse(args[2], NumberStyles.Any, CultureInfo.CurrentCulture, out int slot))
                            return GetCmdDescTranslated();

                        WindowActionType actionType = WindowActionType.LeftClick;
                        string keyName = Translations.cmd_inventory_left;
                        if (args.Length >= 4)
                        {
                            string b = args[3];
                            if (b.ToLower()[0] == 'r')
                                (actionType, keyName) = (WindowActionType.RightClick, Translations.cmd_inventory_right);
                            else if (b.ToLower()[0] == 'm')
                                (actionType, keyName) = (WindowActionType.MiddleClick, Translations.cmd_inventory_middle);
                        }

                        handler.DoWindowAction(inventoryId, slot, actionType);
                        return string.Format(Translations.cmd_inventory_clicking, keyName, slot, inventoryId);
                    }
                    else if (action == "shiftclick" && args.Length >= 3)
                    {
                        if (!int.TryParse(args[2], NumberStyles.Any, CultureInfo.CurrentCulture, out int slot))
                            return GetCmdDescTranslated();

                        if (!handler.DoWindowAction(inventoryId, slot, WindowActionType.ShiftClick))
                            return Translations.cmd_inventory_shiftclick_fail;

                        return string.Format(Translations.cmd_inventory_shiftclick, slot, inventoryId);
                    }
                    else if (action == "drop" && args.Length >= 3)
                    {
                        if (!int.TryParse(args[2], NumberStyles.Any, CultureInfo.CurrentCulture, out int slot))
                            return GetCmdDescTranslated();

                        // check item exist
                        if (!inventory.Items.ContainsKey(slot))
                            return string.Format(Translations.cmd_inventory_no_item, slot);

                        WindowActionType actionType = WindowActionType.DropItem;
                        if (args.Length >= 4 && args[3].ToLower() == "all")
                            actionType = WindowActionType.DropItemStack;

                        if (handler.DoWindowAction(inventoryId, slot, actionType))
                        {
                            if (actionType == WindowActionType.DropItemStack)
                                return string.Format(Translations.cmd_inventory_drop_stack, slot);
                            else
                                return string.Format(Translations.cmd_inventory_drop, slot);
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
                    response.Append(Translations.cmd_inventory_inventories).Append(":\n");
                    foreach ((int invId, Container inv) in handler.GetInventories())
                        response.AppendLine(String.Format(" #{0}: {1}§8", invId, inv.Title));
                    response.Append(CmdUsage);
                    return response.ToString();
                }
            }
            else
                return Translations.extra_inventory_required;
        }

        #region Methods for commands help

        private static string GetAvailableActions()
        {
            return Translations.cmd_inventory_help_available + ": list, close, click, drop, creativegive, creativedelete.";
        }

        private static string GetBasicUsage()
        {
            return Translations.cmd_inventory_help_basic + ": /inventory <player|container|<id>> <action>.";
        }

        private static string GetHelp()
        {
            return string.Format(Translations.cmd_inventory_help_help, GetAvailableActions());
        }

        private static string GetSubCommandHelp(string cmd)
        {
            string usageStr = ' ' + Translations.cmd_inventory_help_usage + ": ";
            return cmd switch
            {
#pragma warning disable format // @formatter:off
                "list"           => Translations.cmd_inventory_help_list           + usageStr + "/inventory <player|container|<id>> list",
                "close"          => Translations.cmd_inventory_help_close          + usageStr + "/inventory <player|container|<id>> close",
                "click"          => Translations.cmd_inventory_help_click          + usageStr + "/inventory <player|container|<id>> click <slot> [left|right|middle]\nDefault is left click",
                "shiftclick"     => Translations.cmd_inventory_help_shiftclick     + usageStr + "/inventory <player|container|<id>> shiftclick <slot>",
                "drop"           => Translations.cmd_inventory_help_drop           + usageStr + "/inventory <player|container|<id>> drop <slot> [all]\nAll means drop full stack",
                "creativegive"   => Translations.cmd_inventory_help_creativegive   + usageStr + "/inventory creativegive <slot> <itemtype> <amount>",
                "creativedelete" => Translations.cmd_inventory_help_creativedelete + usageStr + "/inventory creativedelete <slot>",
                "inventories"    => Translations.cmd_inventory_help_inventories    + usageStr + "/inventory inventories",
                "search"         => Translations.cmd_inventory_help_search         + usageStr + "/inventory search <item type> [count]",
                "help"           => GetHelp(),
                _                => Translations.cmd_inventory_help_unknown        + GetAvailableActions(),
#pragma warning restore format // @formatter:on
            };
        }
        #endregion
    }
}
