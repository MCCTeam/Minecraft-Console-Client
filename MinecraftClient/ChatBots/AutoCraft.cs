using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;
using static MinecraftClient.ChatBots.AutoCraft.Configs;

namespace MinecraftClient.ChatBots
{
    public class AutoCraft : ChatBot
    {
        public const string CommandName = "autocraft";

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoCraft";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.AutoCraft.CraftingTable$")]
            public LocationConfig CraftingTable = new(123, 65, 456);

            [TomlInlineComment("$ChatBot.AutoCraft.OnFailure$")]
            public OnFailConfig OnFailure = OnFailConfig.abort;

            [TomlPrecedingComment("$ChatBot.AutoCraft.Recipes$")]
            public RecipeConfig[] Recipes = new RecipeConfig[]
            {
                new RecipeConfig(
                    Name: "Recipe-Name-1",
                    Type: CraftTypeConfig.player,
                    Result: ItemType.StoneBricks,
                    Slots: new ItemType[4] { ItemType.Stone, ItemType.Stone, ItemType.Stone, ItemType.Stone }
                ),
                new RecipeConfig(
                    Name: "Recipe-Name-2",
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
                _Table_Location = new Location(CraftingTable.X, CraftingTable.Y, CraftingTable.Z).ToFloor();

                List<string> nameList = new();
                foreach (RecipeConfig recipe in Recipes)
                {
                    if (string.IsNullOrWhiteSpace(recipe.Name))
                    {
                        recipe.Name = new Random().NextInt64().ToString();
                        LogToConsole(BotName, Translations.bot_autoCraft_exception_name_miss);
                    }
                    if (nameList.Contains(recipe.Name))
                    {
                        LogToConsole(BotName, string.Format(Translations.bot_autoCraft_exception_duplicate, recipe.Name));
                        do
                        {
                            recipe.Name = new Random().NextInt64().ToString();
                        } while (nameList.Contains(recipe.Name));
                    }
                    nameList.Add(recipe.Name);

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
                        LogToConsole(BotName, Translations.bot_autocraft_invaild_slots);
                    }

                    if (recipe.Result == ItemType.Air || recipe.Result == ItemType.Null)
                    {
                        LogToConsole(BotName, Translations.bot_autocraft_invaild_result);
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
                LogToConsole(Translations.extra_inventory_required);
                LogToConsole(Translations.general_bot_unload);
                UnloadBot();
                return;
            }

            McClient.dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Then(l => l.Literal("list")
                        .Executes(r => OnCommandHelp(r.Source, "list")))
                    .Then(l => l.Literal("start")
                        .Executes(r => OnCommandHelp(r.Source, "start")))
                    .Then(l => l.Literal("stop")
                        .Executes(r => OnCommandHelp(r.Source, "stop")))
                    .Then(l => l.Literal("help")
                        .Executes(r => OnCommandHelp(r.Source, "help")))
                )
            );

            McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("list")
                    .Executes(r => OnCommandList(r.Source)))
                .Then(l => l.Literal("start")
                    .Then(l => l.Argument("RecipeName", MccArguments.AutoCraftRecipeName())
                        .Executes(r => OnCommandStart(r.Source, Arguments.GetString(r, "RecipeName")))))
                .Then(l => l.Literal("stop")
                    .Executes(r => OnCommandStop(r.Source)))
                .Then(l => l.Literal("_help")
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );
        }

        public override void OnUnload()
        {
            McClient.dispatcher.Unregister(CommandName);
            McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
        }

        private int OnCommandHelp(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                "list"      =>   Translations.bot_autoCraft_help_list,
                "start"     =>   Translations.bot_autoCraft_help_start,
                "stop"      =>   Translations.bot_autoCraft_help_stop,
                "help"      =>   Translations.bot_autoCraft_help_help,
                _           =>   string.Format(Translations.bot_autoCraft_available_cmd, "load, list, reload, resetcfg, start, stop, help")
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandList(CmdResult r)
        {
            StringBuilder nameList = new();
            foreach (RecipeConfig recipe in Config.Recipes)
                nameList.Append(recipe.Name).Append(", ");
            return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.bot_autoCraft_cmd_list, Config.Recipes.Length, nameList.ToString()));
        }

        private int OnCommandStart(CmdResult r, string name)
        {
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
                return r.SetAndReturn(CmdResult.Status.Done);
            }
            else
            {
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.bot_autoCraft_recipe_not_exist);
            }
        }

        private int OnCommandStop(CmdResult r)
        {
            StopCrafting();
            return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autoCraft_stop);
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
                    materials[i + 1] = recipeConfig.Slots[i];

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
                    SetTimeout(Translations.bot_autoCraft_table_not_found);
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
                LogToConsole(string.Format(Translations.bot_autoCraft_start, recipe.ResultItem));
                HandleNextStep();
            }
            else LogToConsole(string.Format(Translations.bot_autoCraft_start_fail, recipe.ResultItem));
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
                LogToConsole(string.Format(Translations.bot_autoCraft_close_inventory, inventoryInUse));
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
                    LogToConsole(string.Format(Translations.bot_autoCraft_missing_material, actionSteps[index - 1].ItemType.ToString()));
                }
                if (Config.OnFailure == OnFailConfig.abort)
                {
                    StopCrafting();
                    LogToConsole(Translations.bot_autoCraft_aborted);
                }
                else
                {
                    waitingForMaterials = true;
                    // Even though crafting failed, action step index will still increase
                    // we want to do that failed step again so decrease index by 1
                    index--;
                    LogToConsole(Translations.bot_autoCraft_craft_fail);
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
            LogToConsole(string.Format(Translations.bot_autoCraft_timeout, timeoutAction));
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
