using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class Inventory : Command
    {
        public override string CmdName { get { return "inventory"; } }
        public override string CmdUsage { get { return GetBasicUsage(); } }
        public override string CmdDesc { get { return "cmd.inventory.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                string[] args = getArgs(command);
                if (args.Length >= 1)
                {
                    try
                    {
                        int inventoryId;
                        if (args[0].ToLower() == "creativegive")
                        {
                            if (args.Length >= 4)
                            {
                                int slot = int.Parse(args[1]);
                                ItemType itemType = ItemType.Stone;
                                if (Enum.TryParse(args[2], true, out itemType))
                                {
                                    if (handler.GetGamemode() == 1)
                                    {
                                        int count = int.Parse(args[3]);
                                        if (handler.DoCreativeGive(slot, itemType, count, null))
                                            return Translations.Get("cmd.inventory.creative_done", itemType, count, slot);
                                        else return Translations.Get("cmd.inventory.creative_fail");
                                    }
                                    else return Translations.Get("cmd.inventory.need_creative");
                                }
                                else
                                {
                                    return GetCmdDescTranslated();
                                }
                            }
                            else return GetCmdDescTranslated();
                        }
                        else if (args[0].ToLower() == "creativedelete")
                        {
                            if (args.Length >= 2)
                            {
                                int slot = int.Parse(args[1]);
                                if (handler.GetGamemode() == 1)
                                {
                                    if (handler.DoCreativeGive(slot, ItemType.Null, 0, null))
                                        return Translations.Get("cmd.inventory.creative_delete", slot);
                                    else return Translations.Get("cmd.inventory.creative_fail");
                                }
                                else return Translations.Get("cmd.inventory.need_creative");
                            }
                            else return GetCmdDescTranslated();
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
                            else return Translations.Get("cmd.inventory.container_not_found");
                        }
                        else if (args[0].ToLower() == "help")
                        {
                            if (args.Length >= 2)
                            {
                                return GetSubCommandHelp(args[1]);
                            }
                            else return GetHelp();
                        }
                        else inventoryId = int.Parse(args[0]);
                        string action = args.Length > 1
                            ? args[1].ToLower()
                            : "list";
                        switch (action)
                        {
                            case "close":
                                if (handler.CloseInventory(inventoryId))
                                    return Translations.Get("cmd.inventory.close", inventoryId);
                                else return Translations.Get("cmd.inventory.close_fail", inventoryId);
                            case "list":
                                Container inventory = handler.GetInventory(inventoryId);
                                if (inventory == null)
                                    return Translations.Get("cmd.inventory.not_exist", inventoryId);
                                SortedDictionary<int, Item> itemsSorted = new SortedDictionary<int, Item>(inventory.Items);
                                List<string> response = new List<string>();
                                response.Add(Translations.Get("cmd.inventory.inventory") + " #" + inventoryId + " - " + inventory.Title + "§8");
                                string asciiArt = inventory.Type.GetAsciiArt();
                                if (asciiArt != null && Settings.DisplayInventoryLayout)
                                    response.Add(asciiArt);
                                int selectedHotbar = handler.GetCurrentSlot() + 1;
                                foreach (KeyValuePair<int, Item> item in itemsSorted)
                                {
                                    int hotbar;
                                    bool isHotbar = inventory.IsHotbar(item.Key, out hotbar);
                                    string hotbarString = isHotbar ? (hotbar + 1).ToString() : " ";
                                    if ((hotbar + 1) == selectedHotbar)
                                        hotbarString = ">" + hotbarString;
                                    response.Add(String.Format("{0,2} | #{1,-2}: {2}", hotbarString, item.Key, item.Value.ToString()));
                                }
                                if (inventoryId == 0) 
                                    response.Add(Translations.Get("cmd.inventory.hotbar", (handler.GetCurrentSlot() + 1)));
                                return String.Join("\n", response.ToArray());
                            case "click":
                                if (args.Length >= 3)
                                {
                                    int slot = int.Parse(args[2]);
                                    WindowActionType actionType = WindowActionType.LeftClick;
                                    string keyName = "cmd.inventory.left";
                                    if (args.Length >= 4)
                                    {
                                        string b = args[3];
                                        if (b.ToLower()[0] == 'r')
                                        {
                                            actionType = WindowActionType.RightClick;
                                            keyName = "cmd.inventory.right";
                                        }
                                        if (b.ToLower()[0] == 'm')
                                        {
                                            actionType = WindowActionType.MiddleClick;
                                            keyName = "cmd.inventory.middle";
                                        }
                                    }
                                    handler.DoWindowAction(inventoryId, slot, actionType);
                                    return Translations.Get("cmd.inventory.clicking", Translations.Get(keyName), slot, inventoryId);
                                }
                                else return CmdUsage;
                            case "drop":
                                if (args.Length >= 3)
                                {
                                    int slot = int.Parse(args[2]);
                                    // check item exist
                                    if (!handler.GetInventory(inventoryId).Items.ContainsKey(slot))
                                        return Translations.Get("cmd.inventory.no_item", slot);
                                    WindowActionType actionType = WindowActionType.DropItem;
                                    if (args.Length >= 4)
                                    {
                                        if (args[3].ToLower() == "all")
                                        {
                                            actionType = WindowActionType.DropItemStack;
                                        }
                                    }
                                    if (handler.DoWindowAction(inventoryId, slot, actionType))
                                    {
                                        if (actionType == WindowActionType.DropItemStack)
                                            return Translations.Get("cmd.inventory.drop_stack", slot);
                                        else return Translations.Get("cmd.inventory.drop", slot);
                                    }
                                    else
                                    {
                                        return "Failed";
                                    }
                                }
                                else return GetCmdDescTranslated();
                            default:
                                return GetCmdDescTranslated();
                        }
                    }
                    catch (FormatException) { return GetCmdDescTranslated(); }
                }
                else
                {
                    Dictionary<int, Container> inventories = handler.GetInventories();
                    List<string> response = new List<string>();
                    response.Add(Translations.Get("cmd.inventory.inventories") + ":");
                    foreach (KeyValuePair<int, Container> inventory in inventories)
                    {
                        response.Add(String.Format(" #{0}: {1}", inventory.Key, inventory.Value.Title + "§8"));
                    }
                    response.Add(CmdUsage);
                    return String.Join("\n", response);
                }
            }
            else return Translations.Get("extra.inventory_required");
        }

        #region Methods for commands help
        private string GetCommandDesc()
        {
            return GetBasicUsage() + " Type \"/inventory help\" for more help";
        }

        private string GetAvailableActions()
        {
            return Translations.Get("cmd.inventory.help.available") + ": list, close, click, drop, creativegive, creativedelete.";
        }

        private string GetBasicUsage()
        {
            return Translations.Get("cmd.inventory.help.basic") + ": /inventory <player|container|<id>> <action>.";
        }

        private string GetHelp()
        {
            return Translations.Get("cmd.inventory.help.help", GetAvailableActions());
        }

        private string GetSubCommandHelp(string cmd)
        {
            switch (cmd)
            {
                case "list":
                    return Translations.Get("cmd.inventory.help.list") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> list";
                case "close":
                    return Translations.Get("cmd.inventory.help.close") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> close";
                case "click":
                    return Translations.Get("cmd.inventory.help.click") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> click <slot> [left|right|middle]. \nDefault is left click";
                case "drop":
                    return Translations.Get("cmd.inventory.help.drop") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> drop <slot> [all]. \nAll means drop full stack";
                case "creativegive":
                    return Translations.Get("cmd.inventory.help.creativegive") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory creativegive <slot> <itemtype> <amount>";
                case "creativedelete":
                    return Translations.Get("cmd.inventory.help.creativedelete") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory creativedelete <slot>";
                case "help":
                    return GetHelp();
                default:
                    return Translations.Get("cmd.inventory.help.unknown") + GetAvailableActions();
            }
        }
        #endregion
    }
}
