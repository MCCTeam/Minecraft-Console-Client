using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    class AutoCarft : ChatBot
    {
        private bool waitingForResult = false;
        private int inventoryInUse;

        private enum ActionType
        {
            MoveTo,
            WaitForUpdate,
            Repeat
        }

        private class ActionStep
        {
            public ActionType Action;
            public int Slot;
            public int InventoryID;
        }

        public override void Initialize()
        {
            RegisterChatBotCommand("craft", "craft", CraftCommand);
            RegisterChatBotCommand("open", "open", Open);
            RegisterChatBotCommand("place", "place", Place);
        }

        public string Open(string command, string[] args)
        {
            double x = -258;
            double y = 64;
            double z = -187;
            Location l = new Location(x, y, z);
            SendPlaceBlock(l, Direction.Up);
            SendAnimation();
            return "Try to open";
        }

        public string Place(string command, string[] args)
        {
            double x = Convert.ToDouble(args[0]);
            double y = Convert.ToDouble(args[1]);
            double z = Convert.ToDouble(args[2]);
            SendPlaceBlock(new Location(x, y, z), Direction.Down);
            return "Try place";
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
                slotToTake = -2;
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
