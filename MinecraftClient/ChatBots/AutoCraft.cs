using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    class AutoCraft : ChatBot
    {
        private bool waitingForMaterials = false;
        private bool waitingForUpdate = false;
        private bool waitingForTable = false;
        private bool craftingFailed = false;
        private int inventoryInUse = -2;
        private int index = 0;
        private Recipe recipeInUse;
        private List<ActionStep> actionSteps = new List<ActionStep>();

        private Location tableLocation = new Location();
        private bool abortOnFailure = true;
        private int updateDebounceValue = 2;
        private int updateDebounce = 0;
        private int updateTimeoutValue = 10;
        private int updateTimeout = 0;
        private string timeoutAction = "unspecified";

        private string configPath = @"autocraft\config.ini";
        private string lastRecipe = ""; // Used in parsing recipe config

        private Dictionary<string, Recipe> recipes = new Dictionary<string, Recipe>();

        private void resetVar()
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
            public Dictionary<int, ItemType> Materials;

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

        public AutoCraft(string configPath = @"autocraft\config.ini")
        {
            this.configPath = configPath;
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
            LoadConfig();
        }

        public string CommandHandler(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "load":
                        LoadConfig();
                        return "";
                    case "list":
                        string names = string.Join(", ", recipes.Keys.ToList());
                        return Translations.Get("bot.autoCraft.cmd.list", recipes.Count, names);
                    case "reload":
                        recipes.Clear();
                        LoadConfig();
                        return "";
                    case "resetcfg":
                        WriteDefaultConfig();
                        return Translations.Get("bot.autoCraft.cmd.resetcfg");
                    case "start":
                        if (args.Length >= 2)
                        {
                            string name = args[1];
                            if (recipes.ContainsKey(name))
                            {
                                resetVar();
                                PrepareCrafting(recipes[name]);
                                return "";
                            }
                            else return Translations.Get("bot.autoCraft.recipe_not_exist");
                        }
                        else return Translations.Get("bot.autoCraft.no_recipe_name");
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

        private string GetHelp()
        {
            return Translations.Get("bot.autoCraft.available_cmd", "load, list, reload, resetcfg, start, stop, help");
        }

        private string GetCommandHelp(string cmd)
        {
            switch (cmd.ToLower())
            {
                case "load":
                    return Translations.Get("bot.autocraft.help.load");
                case "list":
                    return Translations.Get("bot.autocraft.help.list");
                case "reload":
                    return Translations.Get("bot.autocraft.help.reload");
                case "resetcfg":
                    return Translations.Get("bot.autocraft.help.resetcfg");
                case "start":
                    return Translations.Get("bot.autocraft.help.start");
                case "stop":
                    return Translations.Get("bot.autocraft.help.stop");
                case "help":
                    return Translations.Get("bot.autocraft.help.help");
                default:
                    return GetHelp();
            }
        }

        #region Config handling

        public void LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                if (!Directory.Exists(configPath))
                {
                    Directory.CreateDirectory(@"autocraft");
                }
                WriteDefaultConfig();
                LogDebugToConsoleTranslated("bot.autoCraft.debug.no_config");
            }
            try
            {
                ParseConfig();
                LogToConsoleTranslated("bot.autoCraft.loaded");
            }
            catch (Exception e)
            {
                LogToConsoleTranslated("bot.autoCraft.error.config", "\n" + e.Message);
            }
        }

        private void WriteDefaultConfig()
        {
            string[] content =
            {
                "[AutoCraft]",
                "# A valid autocraft config must begin with [AutoCraft]",
                "",
                "tablelocation=0,65,0   # Location of the crafting table if you intended to use it. Terrain and movements must be enabled. Format: x,y,z",
                "onfailure=abort        # What to do on crafting failure, abort or wait",
                "",
                "# You can define multiple recipes in a single config file",
                "# This is an example of how to define a recipe",
                "[Recipe]",
                "name=whatever          # name could be whatever you like. This field must be defined first",
                "type=player            # crafting table type: player or table",
                "result=StoneButton     # the resulting item",
                "",
                "# define slots with their deserved item",
                "slot1=Stone            # slot start with 1, count from left to right, top to bottom",
                "# For the naming of the items, please see",
                "# https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Inventory/ItemType.cs"
            };
            File.WriteAllLines(configPath, content);
        }

        private void ParseConfig()
        {
            string[] content = File.ReadAllLines(configPath);
            if (content.Length <= 0)
            {
                throw new Exception(Translations.Get("bot.autoCraft.exception.empty", configPath));
            }
            if (content[0].ToLower() != "[autocraft]")
            {
                throw new Exception(Translations.Get("bot.autoCraft.exception.invalid", configPath));
            }

            // local variable for use in parsing config
            string section = "";
            Dictionary<string, Recipe> recipes = new Dictionary<string, Recipe>();

            foreach (string l in content)
            {
                // ignore comment start with #
                if (l.StartsWith("#"))
                    continue;
                string line = l.Split('#')[0].Trim();
                if (line.Length <= 0)
                    continue;

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    section = line.Substring(1, line.Length - 2).ToLower();
                    continue;
                }

                string key = line.Split('=')[0].ToLower();
                if (!(line.Length > (key.Length + 1)))
                    continue;
                string value = line.Substring(key.Length + 1);
                switch (section)
                {
                    case "recipe": parseRecipe(key, value); break;
                    case "autocraft": parseMain(key, value); break;
                }
            }

            // check and save recipe
            foreach (var pair in recipes)
            {
                if ((pair.Value.CraftingAreaType == ContainerType.PlayerInventory
                    || pair.Value.CraftingAreaType == ContainerType.Crafting)
                    && (pair.Value.Materials != null
                    && pair.Value.Materials.Count > 0)
                    && pair.Value.ResultItem != ItemType.Air)
                {
                    // checking pass
                    this.recipes.Add(pair.Key, pair.Value);
                }
                else
                {
                    throw new Exception(Translations.Get("bot.autoCraft.exception.item_miss", pair.Key));
                }
            }

            
        }

        #region Method for parsing different section of config

        private void parseMain(string key, string value)
        {
            switch (key)
            {
                case "tablelocation":
                    string[] values = value.Split(',');
                    if (values.Length == 3)
                    {
                        tableLocation.X = Convert.ToInt32(values[0]);
                        tableLocation.Y = Convert.ToInt32(values[1]);
                        tableLocation.Z = Convert.ToInt32(values[2]);
                    }
                    else throw new Exception(Translations.Get("bot.autoCraft.exception.invalid_table", key));
                    break;
                case "onfailure":
                    abortOnFailure = value.ToLower() == "abort" ? true : false;
                    break;
                case "updatedebounce":
                    updateDebounceValue = Convert.ToInt32(value);
                    break;
            }
        }

        private void parseRecipe(string key, string value)
        {
            if (key.StartsWith("slot"))
            {
                int slot = Convert.ToInt32(key[key.Length - 1].ToString());
                if (slot > 0 && slot < 10)
                {
                    if (recipes.ContainsKey(lastRecipe))
                    {
                        ItemType itemType;
                        if (Enum.TryParse(value, true, out itemType))
                        {
                            if (recipes[lastRecipe].Materials != null && recipes[lastRecipe].Materials.Count > 0)
                            {
                                recipes[lastRecipe].Materials.Add(slot, itemType);
                            }
                            else
                            {
                                recipes[lastRecipe].Materials = new Dictionary<int, ItemType>()
                                    {
                                        { slot, itemType }
                                    };
                            }
                            return;
                        }
                        else
                        {
                            throw new Exception(Translations.Get("bot.autoCraft.exception.item_name", lastRecipe, key));
                        }
                    }
                    else
                    {
                        throw new Exception(Translations.Get("bot.autoCraft.exception.name_miss"));
                    }
                }
                else
                {
                    throw new Exception(Translations.Get("bot.autoCraft.exception.slot", key));
                }
            }
            else
            {
                switch (key)
                {
                    case "name":
                        if (!recipes.ContainsKey(value))
                        {
                            recipes.Add(value, new Recipe());
                            lastRecipe = value;
                        }
                        else
                        {
                            throw new Exception(Translations.Get("bot.autoCraft.exception.duplicate", value));
                        }
                        break;
                    case "type":
                        if (recipes.ContainsKey(lastRecipe))
                        {
                            recipes[lastRecipe].CraftingAreaType = value.ToLower() == "player" ? ContainerType.PlayerInventory : ContainerType.Crafting;
                        }
                        break;
                    case "result":
                        if (recipes.ContainsKey(lastRecipe))
                        {
                            ItemType itemType;
                            if (Enum.TryParse(value, true, out itemType))
                            {
                                recipes[lastRecipe].ResultItem = itemType;
                            }
                        }
                        break;
                }
            }
        }
        #endregion

        #endregion

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
                    PrepareCrafting(recipeInUse);
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
        private void PrepareCrafting(string name)
        {
            PrepareCrafting(recipes[name]);
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
                    OpenTable(tableLocation);
                    waitingForTable = true;
                    SetTimeout(Translations.Get("bot.autoCraft.table_not_found"));
                    return;
                }
            }

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
                if (abortOnFailure)
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
