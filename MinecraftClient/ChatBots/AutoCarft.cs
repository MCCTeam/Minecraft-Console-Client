using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Inventory;

namespace MinecraftClient.ChatBots
{
    class AutoCarft : ChatBot
    {
        private bool waitingForResult = false;
        private int inventoryInUse;

        public override void Initialize()
        {
            RegisterChatBotCommand("craft", "craft", CraftCommand);
        }

        public string CraftCommand(string command, string[] args)
        {
            Dictionary<int, ItemType> recipe = new Dictionary<int, ItemType>
            {
                { 1, ItemType.Stone }
            };
            var inventory = GetInventories()[0];
            int slotToPut = -2;
            int slotToTake = -2;
            inventoryInUse = 0;
            foreach (KeyValuePair<int, ItemType> slot in recipe)
            {
                slotToPut = slot.Key + 1;
                // Find material in our inventory
                foreach (KeyValuePair<int, Item> item in inventory.Items)
                {
                    if (slot.Value == item.Value.Type)
                    {
                        slotToTake = item.Key;
                        break;
                    }
                }
                if (slotToTake != -2)
                {
                    // move found material to correct crafting slot
                    WindowAction(0, slotToTake, WindowActionType.LeftClick);
                    WindowAction(0, slotToPut, WindowActionType.LeftClick);
                }
            }
            if (slotToPut != -2 && slotToTake != -2)
            {
                waitingForResult = true;
                // Now wait for server to update the slot 0, craft result
                return "Waiting for result";
            }
            else return "Failed before waiting for result";
        }

        public override void OnInventoryUpdate(int inventoryId)
        {
            ConsoleIO.WriteLine("Inventory " + inventoryId + " is being updated");
            if (waitingForResult && inventoryInUse == inventoryId)
            {
                var inventory = GetInventories()[inventoryId];
                if (inventory.Items.ContainsKey(0))
                {
                    // slot 0 have item, click on it
                    WindowAction(0, 0, WindowActionType.LeftClick);
                    // Now wait for server to update our inventory
                    ConsoleIO.WriteLine("Crafting success");
                }
                else if (inventory.Items.ContainsKey(-1))
                {
                    // Server have updated our cursor to the item we want to take out from craft result
                    // Now put the item back to our inventory
                    WindowAction(0, 37, WindowActionType.LeftClick);
                    ConsoleIO.WriteLine("Moved crafted item to inventory");
                    waitingForResult = false;
                }
            }
        }
    }
}
