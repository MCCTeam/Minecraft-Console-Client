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

        private List<ActionStep> actionSteps = new List<ActionStep>();

        private enum ActionType
        {
            LeftClick,
            ShiftClick,
            WaitForUpdate,
            ResetCraftArea,
            Repeat
        }

        /// <summary>
        /// Represent a single action step of the whole crafting process
        /// </summary>
        private class ActionStep
        {
            /// <summary>
            /// The action type of this action step
            /// </summary>
            public ActionType ActionType;

            /// <summary>
            /// For storing data needed for processing
            /// </summary>
            /// <remarks>-2 mean not used</remarks>
            public int Slot = -2;

            /// <summary>
            /// For storing data needed for processing
            /// </summary>
            /// <remarks>-2 mean not used</remarks>
            public int InventoryID = -2;

            /// <summary>
            /// For storing data needed for processing
            /// </summary>
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

        /// <summary>
        /// Represent a crafting recipe
        /// </summary>
        private class Recipe
        {
            /// <summary>
            /// The results item of this recipe
            /// </summary>
            public ItemType ResultItem;

            /// <summary>
            /// Crafting table required for this recipe, playerInventory or Crafting
            /// </summary>
            public ContainerType CraftingAreaType;

            /// <summary>
            /// Materials needed and their position
            /// </summary>
            /// <remarks>position start with 1, from left to right, top to bottom</remarks>
            public Dictionary<int, ItemType> Materials;

            public Recipe(Dictionary<int, ItemType> materials, ItemType resultItem, ContainerType type)
            {
                Materials = materials;
                ResultItem = resultItem;
                CraftingAreaType = type;
            }

            /// <summary>
            /// Convert the position of a defined recipe from playerInventory to Crafting
            /// </summary>
            /// <param name="recipe"></param>
            /// <returns>Converted recipe</returns>
            /// <remarks>so that it can be used in crafting table</remarks>
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
        }

        public string CraftCommand(string command, string[] args)
        {
            /* Define crafting recipe */
            // TODO: make a dedicated config file for user to set their own recipe
            Dictionary<int, ItemType> materials = new Dictionary<int, ItemType>
            {
                { 1, ItemType.OakPlanks }, { 2, ItemType.OakPlanks }, { 3, ItemType.OakPlanks },
                { 4, ItemType.Cobblestone }, { 5, ItemType.IronIngot }, { 6, ItemType.Cobblestone },
                { 7, ItemType.Cobblestone }, { 8, ItemType.Redstone }, { 9, ItemType.Cobblestone }
            };
            Recipe recipe = new Recipe(materials, ItemType.StoneButton, ContainerType.Crafting);
            inventoryInUse = 1;

            recipeInUse = recipe;
            craftingFailed = false;
            waitingForUpdate = false;
            index = 0;
            
            foreach (KeyValuePair<int, ItemType> slot in recipe.Materials)
            {
                // Steps for moving items from inventory to crafting area
                actionSteps.Add(new ActionStep(ActionType.LeftClick, inventoryInUse, slot.Value));
                actionSteps.Add(new ActionStep(ActionType.LeftClick, inventoryInUse, slot.Key));
            }
            if (actionSteps.Count > 0)
            {
                // Wait for server to send us the crafting result
                actionSteps.Add(new ActionStep(ActionType.WaitForUpdate, inventoryInUse, 0));
                // Put item back to inventory. (Using shift-click can take all item at once)
                actionSteps.Add(new ActionStep(ActionType.ShiftClick, inventoryInUse, 0));
                // We need to wait for server to update us after taking item from crafting result
                actionSteps.Add(new ActionStep(ActionType.WaitForUpdate, inventoryInUse));
                // Repeat the whole process again
                actionSteps.Add(new ActionStep(ActionType.Repeat));
                // Start crafting
                HandleNextStep();
                return "AutoCraft start!";
            }
            else return "AutoCraft cannot be started. Check your available materials";
        }

        public override void OnInventoryUpdate(int inventoryId)
        {
            if (waitingForUpdate && inventoryInUse == inventoryId)
            {
                // Because server might send us a LOT of update at once, even there is only a single slot updated.
                // Using this to make sure we don't do things before inventory update finish
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
                // Closing inventory can make server to update our inventory
                // Useful when
                // - There are some items left in the crafting area
                // - Resynchronize player inventory if using crafting table
                CloseInventory(inventoryInUse);
                ConsoleIO.WriteLogLine("Crafting aborted! Check your available materials.");
            }
        }
    }
}
