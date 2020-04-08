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
        public override string CMDDesc { get { return "inventory <id> <list|close|click <slot>>: Interact with inventories"; } }

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                string[] args = getArgs(command);
                if (args.Length >= 1)
                {
                    try
                    {
                        int inventoryId = int.Parse(args[0]);
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
                                if (args.Length == 3)
                                {
                                    int slot = int.Parse(args[2]);
                                    handler.ClickWindowSlot(inventoryId, slot);
                                    return "Clicking slot " + slot + " in window #" + inventoryId;
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
