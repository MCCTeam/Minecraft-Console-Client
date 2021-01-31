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
                LogToConsoleTranslated("bot.autoDrop.no_mode");
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
                        return Translations.Get("bot.autoDrop.on");
                    case "off":
                        enable = false;
                        return Translations.Get("bot.autoDrop.off");
                    case "add":
                        if (args.Length >= 2)
                        {
                            ItemType item;
                            if (Enum.TryParse(args[1], true, out item))
                            {
                                itemList.Add(item);
                                return Translations.Get("bot.autoDrop.added", item.ToString());
                            }
                            else
                            {
                                return Translations.Get("bot.autoDrop.incorrect_name", args[1]);
                            }
                        }
                        else
                        {
                            return Translations.Get("cmd.inventory.help.usage") + ": add <item name>";
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
                                    return Translations.Get("bot.autoDrop.removed", item.ToString());
                                }
                                else
                                {
                                    return Translations.Get("bot.autoDrop.not_in_list");
                                }
                            }
                            else
                            {
                                return Translations.Get("bot.autoDrop.incorrect_name", args[1]);
                            }
                        }
                        else
                        {
                            return Translations.Get("cmd.inventory.help.usage") +  ": remove <item name>";
                        }
                    case "list":
                        if (itemList.Count > 0)
                        {
                            return Translations.Get("bot.autoDrop.list", itemList.Count, string.Join("\n", itemList));
                        }
                        else
                        {
                            return Translations.Get("bot.autoDrop.no_item");
                        }
                    case "mode":
                        if (args.Length >= 2)
                        {
                            switch (args[1].ToLower())
                            {
                                case "include":
                                    dropMode = Mode.Include;
                                    break;
                                case "exclude":
                                    dropMode = Mode.Exclude;
                                    break;
                                case "everything":
                                    dropMode = Mode.Everything;
                                    break;
                                default:
                                    return Translations.Get("bot.autoDrop.unknown_mode"); // Unknwon mode. Available modes: Include, Exclude, Everything
                            }
                            inventoryUpdated = 0;
                            OnUpdateFinish();
                            return Translations.Get("bot.autoDrop.switched", dropMode.ToString()); // Switched to {0} mode.
                        }
                        else
                        {
                            return Translations.Get("bot.autoDrop.unknown_mode");
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
            return Translations.Get("general.available_cmd", "on, off, add, remove, list, mode");
        }

        public override void Initialize()
        {
            if (!GetInventoryEnabled())
            {
                LogToConsoleTranslated("extra.inventory_required");
                LogToConsoleTranslated("general.bot_unload");
                UnloadBot();
                return;
            }
            RegisterChatBotCommand("autodrop", Translations.Get("bot.autoDrop.cmd"), GetHelp(), CommandHandler);
            RegisterChatBotCommand("ad", Translations.Get("bot.autoDrop.alias"), GetHelp(), CommandHandler);
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
                // Always interact container if available (larger ID) because they included player inventory (ID 0)
                if (inventoryId >= inventoryUpdated)
                    inventoryUpdated = inventoryId;
            }
        }

        private void OnUpdateFinish()
        {
            if (inventoryUpdated != -1)
            {
                if (!GetInventories().ContainsKey(inventoryUpdated))
                {
                    // Inventory updated but no inventory ?
                    LogDebugToConsoleTranslated("bot.autoDrop.no_inventory", inventoryUpdated);
                    return;
                }
                var inventory = GetInventories()[inventoryUpdated];
                var items = inventory.Items.ToDictionary(entry => entry.Key, entry => entry.Value);
                if (dropMode == Mode.Include)
                {
                    foreach (var item in items)
                    {
                        // Ingore crafting result slot
                        if (item.Key == 0)
                            continue;
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
                        // Ingore crafting result slot
                        if (item.Key == 0)
                            continue;
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
                        // Ingore crafting result slot
                        if (item.Key == 0)
                            continue;
                        // Drop it !!
                        WindowAction(inventoryUpdated, item.Key, WindowActionType.DropItemStack);
                    }
                }
                inventoryUpdated = -1;
            }
        }
    }
}
