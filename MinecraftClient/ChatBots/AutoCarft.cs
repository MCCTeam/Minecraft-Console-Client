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
        private bool waitingForUpdate = false;
        private int inventoryInUse = -2;
        private int index = 0;
        private Recipe recipeInUse;

        private int updateDebounce = 0;

        private bool craftingFailed = false;

        private enum ActionType
        {
            LeftClick,
            ShiftClick,
            WaitForUpdate,
            ResetCraftArea,
            Repeat
        }

        private class ActionStep
        {
            public ActionType ActionType;
            public int Slot = -2;
            public int InventoryID = -2;
            public ItemType ItemType;

            public ActionStep(ActionType actionType)
            {
                ActionType = actionType;
            }
            public ActionStep(ActionType actionType, int inventoryID)
            {
                ActionType = actionType;
                InventoryID = inventoryID;
            }
            public ActionStep(ActionType actionType, int inventoryID, int slot)
            {
                ActionType = actionType;
                Slot = slot;
                InventoryID = inventoryID;
            }
            public ActionStep(ActionType actionType, int inventoryID, ItemType itemType)
            {
                ActionType = actionType;
                InventoryID = inventoryID;
                ItemType = itemType;
            }
        }

        private List<ActionStep> actionSteps = new List<ActionStep>();

        private class Recipe
        {
            public ItemType ResultItem;
            public ContainerType CraftingAreaType;
            public Dictionary<int, ItemType> Materials;

            public Recipe(Dictionary<int, ItemType> materials, ItemType resultItem, ContainerType type)
            {
                Materials = materials;
                ResultItem = resultItem;
                CraftingAreaType = type;
            }

            public static Recipe ConvertToCraftingTable(Recipe recipe)
            {
                if (recipe.CraftingAreaType == ContainerType.PlayerInventory)
                {
                    if (recipe.Materials.ContainsKey(4))
                    {
                        recipe.Materials[5] = recipe.Materials[4];
                        recipe.Materials.Remove(4);
                    }
                    if (recipe.Materials.ContainsKey(3))
                    {
                        recipe.Materials[4] = recipe.Materials[3];
                        recipe.Materials.Remove(3);
                    }
                }
                return recipe;
            }
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
            Dictionary<int, ItemType> materials = new Dictionary<int, ItemType>
            {
                { 1, ItemType.Coal },
                { 3, ItemType.Stick }
            };
            Recipe recipe = new Recipe(materials, ItemType.StoneButton, ContainerType.PlayerInventory);
            inventoryInUse = 0;

            recipeInUse = recipe;
            craftingFailed = false;
            waitingForUpdate = false;
            index = 0;

            var inventory = GetInventories()[inventoryInUse];
            foreach (KeyValuePair<int, ItemType> slot in recipe.Materials)
            {
                actionSteps.Add(new ActionStep(ActionType.LeftClick, inventoryInUse, slot.Value));
                actionSteps.Add(new ActionStep(ActionType.LeftClick, inventoryInUse, slot.Key));
            }
            if (actionSteps.Count > 0)
            {
                actionSteps.Add(new ActionStep(ActionType.WaitForUpdate, inventoryInUse, 0));
                actionSteps.Add(new ActionStep(ActionType.ShiftClick, inventoryInUse, 0));
                actionSteps.Add(new ActionStep(ActionType.WaitForUpdate, inventoryInUse));
                actionSteps.Add(new ActionStep(ActionType.Repeat));
                HandleNextStep();
                return "AutoCraft start!";
            }
            else return "AutoCraft cannot be started. Check your available materials";
        }

        public override void OnInventoryUpdate(int inventoryId)
        {
            if (waitingForUpdate && inventoryInUse == inventoryId)
            {
                updateDebounce = 2;
            }
        }

        public override void Update()
        {
            if (updateDebounce > 0)
            {
                updateDebounce--;
                if (updateDebounce <= 0)
                    InventoryUpdateFinished();
            }
        }

        private void InventoryUpdateFinished()
        {
            waitingForUpdate = false;
            HandleNextStep();
        }

        private void HandleNextStep()
        {
            while (actionSteps.Count > 0)
            {
                if (waitingForUpdate) break;
                ActionStep step = actionSteps[index];
                index++;
                switch (step.ActionType)
                {
                    case ActionType.LeftClick:
                        if (step.Slot != -2)
                        {
                            WindowAction(step.InventoryID, step.Slot, WindowActionType.LeftClick);
                        }
                        else
                        {
                            int[] slots = GetInventories()[step.InventoryID].SearchItem(step.ItemType);
                            if (slots.Count() > 0)
                            {
                                int ignoredSlot;
                                if (recipeInUse.CraftingAreaType == ContainerType.PlayerInventory)
                                    ignoredSlot = 9;
                                else
                                    ignoredSlot = 10;
                                slots = slots.Where(slot => slot >= ignoredSlot).ToArray();
                                if (slots.Count() > 0)
                                    WindowAction(step.InventoryID, slots[0], WindowActionType.LeftClick);
                                else
                                    craftingFailed = true;
                            }
                            else craftingFailed = true;
                        }
                        break;

                    case ActionType.ShiftClick:
                        if (step.Slot == 0)
                        {
                            WindowAction(step.InventoryID, step.Slot, WindowActionType.ShiftClick);
                        }
                        else craftingFailed = true;
                        break;

                    case ActionType.WaitForUpdate:
                        if (step.InventoryID != -2)
                        {
                            waitingForUpdate = true;
                        }
                        else craftingFailed = true;
                        break;

                    case ActionType.ResetCraftArea:
                        if (step.InventoryID != -2)
                            CloseInventory(step.InventoryID);
                        else
                            craftingFailed = true;
                        break;

                    case ActionType.Repeat:
                        index = 0;
                        break;
                }
                HandleError();
            }
            
        }

        private void HandleError()
        {
            if (craftingFailed)
            {
                actionSteps.Clear();
                CloseInventory(inventoryInUse);
                ConsoleIO.WriteLogLine("Crafting aborted! Check your available materials.");
            }
        }
    }
}
