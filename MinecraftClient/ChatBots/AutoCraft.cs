﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using Tomlet.Attributes;
using static MinecraftClient.ChatBots.AutoCraft.Configs;

namespace MinecraftClient.ChatBots
{
    public class AutoCraft : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoCraft";

            public bool Enabled = false;

            [TomlInlineComment("$config.ChatBot.AutoCraft.Table_Location$")]
            public LocationConfig Table_Location = new(123, 65, 456);

            [TomlInlineComment("$config.ChatBot.AutoCraft.On_Failure$")]
            public OnFailConfig On_Failure = OnFailConfig.abort;

            [TomlPrecedingComment("$config.ChatBot.AutoCraft.Recipes$")]
            public RecipeConfig[] Recipes = new RecipeConfig[]
            {
                new RecipeConfig(
                    Name: "Recipe Name 1",
                    Type: CraftTypeConfig.player,
                    Result: ItemType.StoneBricks,
                    Slots: new ItemType[4] { ItemType.Stone, ItemType.Stone, ItemType.Stone, ItemType.Stone }
                ),
                new RecipeConfig(
                    Name: "Recipe Name 2",
                    Type: CraftTypeConfig.table,
                    Result: ItemType.StoneBricks,
                    Slots: new ItemType[9] { 
                        ItemType.Stone, ItemType.Stone, ItemType.Null,
                        ItemType.Stone, ItemType.Stone, ItemType.Null,
                        ItemType.Null, ItemType.Null, ItemType.Null,
                    }
                ),
            };

            [NonSerialized]
            public Location _Table_Location = Location.Zero;

            public void OnSettingUpdate()
            {
                _Table_Location = new Location(Table_Location.X, Table_Location.Y, Table_Location.Z).ToFloor();
                foreach (RecipeConfig recipe in Recipes)
                {
                    recipe.Name ??= string.Empty;

                    int fixLength = -1;
                    if (recipe.Type == CraftTypeConfig.player && recipe.Slots.Length != 4)
                        fixLength = 4;
                    else if (recipe.Type == CraftTypeConfig.table && recipe.Slots.Length != 9)
                        fixLength = 9;

                    if (fixLength > 0)
                    {
                        ItemType[] Slots = new ItemType[fixLength];
                        for (int i = 0; i < fixLength; ++i)
                            Slots[i] = (i < recipe.Slots.Length) ? recipe.Slots[i] : ItemType.Null;
                        recipe.Slots = Slots;
                        LogToConsole(BotName, Translations.TryGet("bot.autocraft.invaild_slots"));
                    }

                    if (recipe.Result == ItemType.Air || recipe.Result == ItemType.Null)
                    {
                        LogToConsole(BotName, Translations.TryGet("bot.autocraft.invaild_result"));
                    }
                }
            }

            public struct LocationConfig
            {
                public double X, Y, Z;

                public LocationConfig(double X, double Y, double Z)
                {
                    this.X = X;
                    this.Y = Y;
                    this.Z = Z;
                }
            }

            public enum OnFailConfig { abort, wait }

            public class RecipeConfig
            {
                public string Name = "Recipe Name";

                public CraftTypeConfig Type = CraftTypeConfig.player;

                public ItemType Result = ItemType.Air;

                public ItemType[] Slots = new ItemType[9] { 
                    ItemType.Null, ItemType.Null, ItemType.Null,
                    ItemType.Null, ItemType.Null, ItemType.Null,
                    ItemType.Null, ItemType.Null, ItemType.Null,
                };

                public RecipeConfig() { }

                public RecipeConfig(string Name, CraftTypeConfig Type, ItemType Result, ItemType[] Slots)
                {
                    this.Name = Name;
                    this.Type = Type;
                    this.Result = Result;
                    this.Slots = Slots;
                }
            }

            public enum CraftTypeConfig { player, table }
        }

        private bool waitingForMaterials = false;
        private bool waitingForUpdate = false;
        private bool waitingForTable = false;
        private bool craftingFailed = false;
        private int inventoryInUse = -2;
        private int index = 0;
        private Recipe? recipeInUse;
        private readonly List<ActionStep> actionSteps = new();

        private int updateDebounceValue = 2;
        private int updateDebounce = 0;
        private readonly int updateTimeoutValue = 10;
        private int updateTimeout = 0;
        private string timeoutAction = "unspecified";

        private void ResetVar()
        {
            craftingFailed = false;
            waitingForTable = false;
            waitingForUpdate = false;
            waitingForMaterials = false;
            inventoryInUse = -2;
            index = 0;
            recipeInUse = null;
            actionSteps.Clear();
        }

        private enum ActionType
        {
            LeftClick,
            ShiftClick,
            WaitForUpdate,
            ResetCraftArea,
            Repeat,
            CheckResult
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
            public Dictionary<int, ItemType>? Materials;

            public Recipe() { }

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
                if (recipe.CraftingAreaType == ContainerType.PlayerInventory && recipe.Materials != null)
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
            if (!GetInventoryEnabled())
            {
                LogToConsoleTranslated("extra.inventory_required");
                LogToConsoleTranslated("general.bot_unload");
                UnloadBot();
                return;
            }
            RegisterChatBotCommand("autocraft", Translations.Get("bot.autoCraft.cmd"), GetHelp(), CommandHandler);
            RegisterChatBotCommand("ac", Translations.Get("bot.autoCraft.alias"), GetHelp(), CommandHandler);
        }

        public string CommandHandler(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "list":
                        string names = string.Join(", ", Config.Recipes.ToList());
                        return Translations.Get("bot.autoCraft.cmd.list", Config.Recipes.Length, names);
                    case "start":
                        if (args.Length >= 2)
                        {
                            string name = args[1];

                            bool hasRecipe = false;
                            RecipeConfig? recipe = null;
                            foreach (RecipeConfig recipeConfig in Config.Recipes)
                            {
                                if (recipeConfig.Name == name)
                                {
                                    hasRecipe = true;
                                    recipe = recipeConfig;
                                    break;
                                }
                            }

                            if (hasRecipe)
                            {
                                ResetVar();
                                PrepareCrafting(recipe!);
                                return "";
                            }
                            else
                                return Translations.Get("bot.autoCraft.recipe_not_exist");
                        }
                        else
                            return Translations.Get("bot.autoCraft.no_recipe_name");
                    case "stop":
                        StopCrafting();
                        return Translations.Get("bot.autoCraft.stop");
                    case "help":
                        return GetCommandHelp(args.Length >= 2 ? args[1] : "");
                    default:
                        return GetHelp();
                }
            }
            else return GetHelp();
        }

        private static string GetHelp()
        {
            return Translations.Get("bot.autoCraft.available_cmd", "load, list, reload, resetcfg, start, stop, help");
        }

        private string GetCommandHelp(string cmd)
        {
            return cmd.ToLower() switch
            {
#pragma warning disable format // @formatter:off
                "load"      =>   Translations.Get("bot.autocraft.help.load"),
                "list"      =>   Translations.Get("bot.autocraft.help.list"),
                "reload"    =>   Translations.Get("bot.autocraft.help.reload"),
                "resetcfg"  =>   Translations.Get("bot.autocraft.help.resetcfg"),
                "start"     =>   Translations.Get("bot.autocraft.help.start"),
                "stop"      =>   Translations.Get("bot.autocraft.help.stop"),
                "help"      =>   Translations.Get("bot.autocraft.help.help"),
                _           =>    GetHelp(),
#pragma warning restore format // @formatter:on
            };
        }

        #region Core part of auto-crafting

        public override void OnInventoryUpdate(int inventoryId)
        {
            if ((waitingForUpdate && inventoryInUse == inventoryId) || (waitingForMaterials && inventoryInUse == inventoryId))
            {
                // Because server might send us a LOT of update at once, even there is only a single slot updated.
                // Using this to make sure we don't do things before inventory update finish
                updateDebounce = updateDebounceValue;
            }
        }

        public override void OnInventoryOpen(int inventoryId)
        {
            if (waitingForTable)
            {
                if (GetInventories()[inventoryId].Type == ContainerType.Crafting)
                {
                    waitingForTable = false;
                    ClearTimeout();
                    // After table opened, we need to wait for server to update table inventory items
                    waitingForUpdate = true;
                    inventoryInUse = inventoryId;
                    PrepareCrafting(recipeInUse!);
                }
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
            if (updateTimeout > 0)
            {
                updateTimeout--;
                if (updateTimeout <= 0)
                    HandleUpdateTimeout();
            }
        }

        private void InventoryUpdateFinished()
        {
            if (waitingForUpdate || waitingForMaterials)
            {
                if (waitingForUpdate)
                    waitingForUpdate = false;
                if (waitingForMaterials)
                {
                    waitingForMaterials = false;
                    craftingFailed = false;
                }
                HandleNextStep();
            }
        }

        private void OpenTable(Location location)
        {
            SendPlaceBlock(location, Direction.Up);
        }

        /// <summary>
        /// Prepare the crafting action steps by the given recipe name and start crafting
        /// </summary>
        /// <param name="recipe">Name of the recipe to craft</param>
        private void PrepareCrafting(RecipeConfig recipeConfig)
        {
            Dictionary<int, ItemType> materials = new();
            for (int i = 0; i < recipeConfig.Slots.Length; ++i)
                if (recipeConfig.Slots[i] != ItemType.Null)
                    materials[i] = recipeConfig.Slots[i];

            ItemType ResultItem = recipeConfig.Result;

            ContainerType CraftingAreaType = 
                (recipeConfig.Type == CraftTypeConfig.player) ? ContainerType.PlayerInventory : ContainerType.Crafting;

            PrepareCrafting(new Recipe(materials, ResultItem, CraftingAreaType));
        }

        /// <summary>
        /// Prepare the crafting action steps by the given recipe and start crafting
        /// </summary>
        /// <param name="recipe">Recipe to craft</param>
        private void PrepareCrafting(Recipe recipe)
        {
            recipeInUse = recipe;
            if (recipeInUse.CraftingAreaType == ContainerType.PlayerInventory)
                inventoryInUse = 0;
            else
            {
                var inventories = GetInventories();
                foreach (var inventory in inventories)
                    if (inventory.Value.Type == ContainerType.Crafting)
                        inventoryInUse = inventory.Key;
                if (inventoryInUse == -2)
                {
                    // table required but not found. Try to open one
                    OpenTable(Config._Table_Location);
                    waitingForTable = true;
                    SetTimeout(Translations.Get("bot.autoCraft.table_not_found"));
                    return;
                }
            }

            if (recipe.Materials != null)
            {
                foreach (KeyValuePair<int, ItemType> slot in recipe.Materials)
                {
                    // Steps for moving items from inventory to crafting area
                    actionSteps.Add(new ActionStep(ActionType.LeftClick, inventoryInUse, slot.Value));
                    actionSteps.Add(new ActionStep(ActionType.LeftClick, inventoryInUse, slot.Key));
                }
            }
            if (actionSteps.Count > 0)
            {
                // Wait for server to send us the crafting result
                actionSteps.Add(new ActionStep(ActionType.WaitForUpdate, inventoryInUse, 0));
                // Check the crafting result is the item we want
                actionSteps.Add(new ActionStep(ActionType.CheckResult, inventoryInUse, recipe.ResultItem));
                // Put item back to inventory. (Using shift-click can take all item at once)
                actionSteps.Add(new ActionStep(ActionType.ShiftClick, inventoryInUse, 0));
                // We need to wait for server to update us after taking item from crafting result
                actionSteps.Add(new ActionStep(ActionType.WaitForUpdate, inventoryInUse));
                // Repeat the whole process again
                actionSteps.Add(new ActionStep(ActionType.Repeat));
                // Start crafting
                LogToConsoleTranslated("bot.autoCraft.start", recipe.ResultItem);
                HandleNextStep();
            }
            else LogToConsoleTranslated("bot.autoCraft.start_fail", recipe.ResultItem);
        }

        /// <summary>
        /// Stop the crafting process by clearing crafting action steps and close the inventory
        /// </summary>
        private void StopCrafting()
        {
            actionSteps.Clear();
            // Put item back to inventory or they will be dropped
            ClearCraftingArea(inventoryInUse);
            // Closing inventory can make server to update our inventory
            // Useful when
            // - There are some items left in the crafting area
            // - Resynchronize player inventory if using crafting table
            if (GetInventories().ContainsKey(inventoryInUse))
            {
                CloseInventory(inventoryInUse);
                LogToConsoleTranslated("bot.autoCraft.close_inventory", inventoryInUse);
            }
        }

        /// <summary>
        /// Handle next crafting action step
        /// </summary>
        private void HandleNextStep()
        {
            while (actionSteps.Count > 0)
            {
                if (waitingForUpdate || waitingForMaterials || craftingFailed) break;
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
                            if (slots.Length > 0)
                            {
                                int ignoredSlot;
                                if (recipeInUse!.CraftingAreaType == ContainerType.PlayerInventory)
                                    ignoredSlot = 9;
                                else
                                    ignoredSlot = 10;
                                slots = slots.Where(slot => slot >= ignoredSlot).ToArray();
                                if (slots.Length > 0)
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

                    // Compare the crafting result with the recipe result
                    case ActionType.CheckResult:
                        if (GetInventories()[step.InventoryID].Items.ContainsKey(0)
                            && GetInventories()[step.InventoryID].Items[0].Type == step.ItemType)
                        {
                            // OK
                            break;
                        }
                        else
                        {
                            // Bad, reset everything
                            ClearCraftingArea(step.InventoryID);
                            index = 0;
                        }
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
                            ClearCraftingArea(step.InventoryID);
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

        /// <summary>
        /// Handle any crafting error after a step was processed
        /// </summary>
        private void HandleError()
        {
            if (craftingFailed)
            {
                if (actionSteps[index - 1].ActionType == ActionType.LeftClick && actionSteps[index - 1].ItemType != ItemType.Air)
                {
                    // Inform user the missing meterial name
                    LogToConsoleTranslated("bot.autoCraft.missing_material", actionSteps[index - 1].ItemType.ToString());
                }
                if (Config.On_Failure == OnFailConfig.abort)
                {
                    StopCrafting();
                    LogToConsoleTranslated("bot.autoCraft.aborted");
                }
                else
                {
                    waitingForMaterials = true;
                    // Even though crafting failed, action step index will still increase
                    // we want to do that failed step again so decrease index by 1
                    index--;
                    LogToConsoleTranslated("bot.autoCraft.craft_fail");
                }
            }
        }

        /// <summary>
        /// Put any item left in the crafting area back to inventory
        /// </summary>
        /// <param name="inventoryId">Inventory ID to operate with</param>
        private void ClearCraftingArea(int inventoryId)
        {
            if (GetInventories().ContainsKey(inventoryId))
            {
                var inventory = GetInventories()[inventoryId];
                int areaStart = 1;
                int areaEnd = 4;
                if (inventory.Type == ContainerType.Crafting)
                {
                    areaEnd = 9;
                }
                List<int> emptySlots = inventory.GetEmpytSlots().Where(s => s > 9).ToList();
                for (int i = areaStart; i <= areaEnd; i++)
                {
                    if (inventory.Items.ContainsKey(i))
                    {
                        if (emptySlots.Count != 0)
                        {
                            WindowAction(inventoryId, i, WindowActionType.LeftClick);
                            WindowAction(inventoryId, emptySlots[0], WindowActionType.LeftClick);
                            emptySlots.RemoveAt(0);
                        }
                        else
                        {
                            WindowAction(inventoryId, i, WindowActionType.DropItemStack);
                        }
                    }
                }
            }
        }

        private void HandleUpdateTimeout()
        {
            LogToConsoleTranslated("bot.autoCraft.timeout", timeoutAction);
        }

        /// <summary>
        /// Set the timeout. Used to detect the failure of open crafting table
        /// </summary>
        /// <param name="reason">The reason to display if timeout</param>
        private void SetTimeout(string reason = "unspecified")
        {
            updateTimeout = updateTimeoutValue;
            timeoutAction = reason;
        }

        /// <summary>
        /// Clear the timeout
        /// </summary>
        private void ClearTimeout()
        {
            updateTimeout = 0;
        }

        #endregion
    }
}
