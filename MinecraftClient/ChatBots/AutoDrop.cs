using MinecraftClient.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    class AutoDrop : ChatBot
    {
        private enum Mode
        {
            Include,    // Items in list will be dropped
            Exclude,    // Items in list will be kept
            Everything  // Everything will be dropped
        }
        private Mode dropMode = Mode.Include;
        private bool enable = true;

        private int updateDebounce = 0;
        private int updateDebounceValue = 2;
        private int inventoryUpdated = -1;

        private List<ItemType> itemList = new List<ItemType>();

        public AutoDrop(string mode, string itemList)
        {
            if (!Enum.TryParse(mode, true, out dropMode))
            {
                LogToConsole("Cannot read drop mode from config. Using include mode.");
            }
            if (dropMode != Mode.Everything)
                this.itemList = ItemListParser(itemList).ToList();
        }

        /// <summary>
        /// Convert an item type string to item type array
        /// </summary>
        /// <param name="itemList">String to convert</param>
        /// <returns>Item type array</returns>
        private ItemType[] ItemListParser(string itemList)
        {
            string trimed = new string(itemList.Where(c => !char.IsWhiteSpace(c)).ToArray());
            string[] list = trimed.Split(',');
            List<ItemType> result = new List<ItemType>();
            foreach (string t in list)
            {
                ItemType item;
                if (Enum.TryParse(t, true, out item))
                {
                    result.Add(item);
                }
            }
            return result.ToArray();
        }

        public string CommandHandler(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        enable = true;
                        inventoryUpdated = 0;
                        OnUpdateFinish();
                        return "AutoDrop enabled";
                    case "off":
                        enable = false;
                        return "AutoDrop disabled";
                    case "add":
                        if (args.Length >= 2)
                        {
                            ItemType item;
                            if (Enum.TryParse(args[1], true, out item))
                            {
                                itemList.Add(item);
                                return "Added item " + item.ToString();
                            }
                            else
                            {
                                return "Incorrect item name " + args[1] + ". Please try again";
                            }
                        }
                        else
                        {
                            return "Usage: add <item name>";
                        }
                    case "remove":
                        if (args.Length >= 2)
                        {
                            ItemType item;
                            if (Enum.TryParse(args[1], true, out item))
                            {
                                if (itemList.Contains(item))
                                {
                                    itemList.Remove(item);
                                    return "Removed item " + item.ToString();
                                }
                                else
                                {
                                    return "Item not in the list";
                                }
                            }
                            else
                            {
                                return "Incorrect item name " + args[1] + ". Please try again";
                            }
                        }
                        else
                        {
                            return "Usage: remove <item name>";
                        }
                    case "list":
                        if (itemList.Count > 0)
                        {
                            return "Total " + itemList.Count + " in the list:\n" + string.Join("\n", itemList);
                        }
                        else
                        {
                            return "No item in the list";
                        }
                    default:
                        return GetHelp();
                }
            }
            else
            {
                return GetHelp();
            }
        }

        private string GetHelp()
        {
            return "AutoDrop ChatBot command. Available commands: on, off, add, remove, list";
        }

        public override void Initialize()
        {
            if (!GetInventoryEnabled())
            {
                LogToConsole("Inventory handling is disabled. Unloading...");
                UnloadBot();
                return;
            }
            RegisterChatBotCommand("autodrop", "AutoDrop ChatBot command", CommandHandler);
            RegisterChatBotCommand("ad", "AutoDrop ChatBot command alias", CommandHandler);
        }

        public override void Update()
        {
            if (updateDebounce > 0)
            {
                updateDebounce--;
                if (updateDebounce <= 0)
                {
                    OnUpdateFinish();
                }
            }
        }

        public override void OnInventoryUpdate(int inventoryId)
        {
            if (enable)
            {
                updateDebounce = updateDebounceValue;
                inventoryUpdated = inventoryId;
            }
        }

        private void OnUpdateFinish()
        {
            if (inventoryUpdated != -1)
            {
                var inventory = GetInventories()[inventoryUpdated];
                var items = inventory.Items.ToDictionary(entry => entry.Key, entry => entry.Value);
                if (dropMode == Mode.Include)
                {
                    foreach (var item in items)
                    {
                        if (itemList.Contains(item.Value.Type))
                        {
                            // Drop it !!
                            WindowAction(inventoryUpdated, item.Key, WindowActionType.DropItemStack);
                        }
                    }
                }
                else if (dropMode == Mode.Exclude)
                {
                    foreach (var item in items)
                    {
                        if (!itemList.Contains(item.Value.Type))
                        {
                            // Drop it !!
                            WindowAction(inventoryUpdated, item.Key, WindowActionType.DropItemStack);
                        }
                    }
                }
                else
                {
                    foreach (var item in items)
                    {
                        // Drop it !!
                        WindowAction(inventoryUpdated, item.Key, WindowActionType.DropItemStack);
                    }
                }
                inventoryUpdated = -1;
            }
        }
    }
}
