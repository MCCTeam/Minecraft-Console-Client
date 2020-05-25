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
        public override string CMDDesc { get { return "inventory <<id>|player|container> <list|close|drop <slot> <1|all>|click <slot> <left|right|middle>|creativegive <slot> <itemtype> <count>>: Interact with inventories"; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                string[] args = getArgs(command);
                if (args.Length >= 1)
                {
                    try
                    {
                        int inventoryId;
                        if (args[0].ToLower() == "player")
                        {
                            // player inventory is always ID 0
                            inventoryId = 0;
                        }
                        else if (args[0].ToLower() == "container")
                        {
                            List<int> availableIds = handler.GetInventories().Keys.ToList();
                            availableIds.Remove(0); // remove player inventory ID from list
                            if (availableIds.Count == 1)
                                inventoryId = availableIds[0]; // one container, use it
                            else return "Cannot find container, please retry with explicit ID";
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
                            case "creativegive":
                                if (args.Length >= 3)
                                {
                                    int slot = int.Parse(args[2]);
                                    ItemType ItemType = ItemType.Stone;
                                    if (Enum.TryParse(args[3], out ItemType))
                                    {
                                        int count = int.Parse(args[4]);
                                        Dictionary<string, object> NBT = null;
                                        Item item = new Item((int)ItemType, count, NBT);

                                        if (handler.DoCreativeInventoryAction(slot, item))
                                        {
                                            return "You have received " + ItemType + " x" + count + " in the slot #" + slot;
                                        }
                                        else
                                        {
                                            return "Failed";
                                        }
                                    }
                                    else;
                                    {
                                        return CMDDesc;
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
    }
}
