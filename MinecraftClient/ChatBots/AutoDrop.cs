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

        public override void Initialize()
        {
            if (!GetInventoryEnabled())
            {
                LogToConsole("Inventory handling is disabled. Unloading...");
                UnloadBot();
            }

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
            updateDebounce = updateDebounceValue;
            inventoryUpdated = inventoryId;
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
            }
        }
    }
}
