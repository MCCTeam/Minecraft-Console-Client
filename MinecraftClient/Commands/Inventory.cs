using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Inventory;

namespace MinecraftClient.Commands
{
    class Inventory : Command
    {
        public override string CMDName { get { return "inventory"; } }
        public override string CMDDesc { get { return GetCommandDesc(); } }

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
                                            return "Requested " + itemType + " x" + count + " in slot #" + slot;
                                        else return "Failed to request Creative Give";
                                    }
                                    else return "You need Gamemode Creative";
                                }
                                else
                                {
                                    return CMDDesc;
                                }
                            }
                            else return CMDDesc;
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
                            if (availableIds.Count == 1)
                                inventoryId = availableIds[0]; // one container, use it
                            else return "Cannot find container, please retry with explicit ID";
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
                                    return "Closing Inventoy #" + inventoryId;
                                else return "Failed to close Inventory #" + inventoryId;
                            case "list":
                                Container inventory = handler.GetInventory(inventoryId);
                                if(inventory==null)
                                    return "Inventory #" + inventoryId + " do not exist";
                                List<string> response = new List<string>();
                                response.Add("Inventory #" + inventoryId + " - " + inventory.Title + "§8");
                                foreach (KeyValuePair<int, Item> item in inventory.Items)
                                {
                                    string displayName = item.Value.DisplayName;
                                    if (String.IsNullOrEmpty(displayName))
                                        response.Add(String.Format(" #{0}: {1} x{2}", item.Key, item.Value.Type, item.Value.Count));
                                    else response.Add(String.Format(" #{0}: {1} x{2} - {3}§8", item.Key, item.Value.Type, item.Value.Count, displayName));
                                }
                                if (inventoryId == 0) response.Add("Your selected hotbar is " + (handler.GetCurrentSlot() + 1));
                                return String.Join("\n", response.ToArray());
                            case "click":
                                if (args.Length >= 3)
                                {
                                    int slot = int.Parse(args[2]);
                                    WindowActionType actionType = WindowActionType.LeftClick;
                                    string keyName = "Left";
                                    if (args.Length >= 4)
                                    {
                                        string b = args[3];
                                        if (b.ToLower()[0] == 'r')
                                        {
                                            actionType = WindowActionType.RightClick;
                                            keyName = "Right";
                                        }
                                        if (b.ToLower()[0] == 'm')
                                        {
                                            actionType = WindowActionType.MiddleClick;
                                            keyName = "Middle";
                                        }
                                    }
                                    handler.DoWindowAction(inventoryId, slot, actionType);
                                    return keyName + " clicking slot " + slot + " in window #" + inventoryId;
                                }
                                else return CMDDesc;
                            case "drop":
                                if (args.Length >= 3)
                                {
                                    int slot = int.Parse(args[2]);
                                    // check item exist
                                    if (!handler.GetInventory(inventoryId).Items.ContainsKey(slot))
                                        return "No item in slot #" + slot;
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
                                            return "Dropped whole item stack from slot #" + slot;
                                        else return "Dropped 1 item from slot #" + slot;
                                    }
                                    else
                                    {
                                        return "Failed";
                                    }
                                }
                                else return CMDDesc;
                            default:
                                return CMDDesc;
                        }
                    }
                    catch (FormatException) { return CMDDesc; }
                }
                else
                {
                    Dictionary<int, Container> inventories = handler.GetInventories();
                    List<string> response = new List<string>();
                    response.Add("Inventories:");
                    foreach (var inventory in inventories)
                        response.Add(String.Format(" #{0}: {1}", inventory.Key, inventory.Value.Title + "§8"));
                    response.Add(CMDDesc);
                    return String.Join("\n", response);
                }
            }
            else return "Please enable inventoryhandling in config to use this command.";
        }

        #region Methods for commands help
        private string GetCommandDesc()
        {
            return GetBasicUsage() + " Type \"/inventory help\" for more help";
        }

        private string GetAvailableActions()
        {
            return "Available actions: list, close, click, drop.";
        }

        private string GetBasicUsage()
        {
            return "Basic usage: /inventory <player|container|<id>> <action>.";
        }

        private string GetHelp()
        {
            return GetBasicUsage()
                + "\n " + GetAvailableActions() + " Use \"/inventory help <action>\" for action help."
                + "\n Creative mode give: " + GetCreativeGiveHelp()
                + "\n \"player\" and \"container\" can be simplified to \"p\" and \"c\"."
                + "\n Note that parameters started with \"?\" are optional.";
        }

        private string GetCreativeGiveHelp()
        {
            return "Usage: /inventory creativegive <slot> <itemtype> <count>";
        }

        private string GetSubCommandHelp(string cmd)
        {
            switch (cmd)
            {
                case "list":
                    return "List your inventory. Usage: /inventory <player|container|<id>> list";
                case "close":
                    return "Close an opened container. Usage: /inventory <player|container|<id>> close";
                case "click":
                    return "Click on an item. Usage: /inventory <player|container|<id>> click <slot> <?left|right|middle>. \nDefault is left click";
                case "drop":
                    return "Drop an item from inventory. Usage: /inventory <player|container|<id>> drop <slot> <?all>. \nAll means drop full stack";
                case "help":
                    return GetHelp();
                default:
                    return "Unknown action. " + GetAvailableActions();
            }
        }
        #endregion
    }
}
