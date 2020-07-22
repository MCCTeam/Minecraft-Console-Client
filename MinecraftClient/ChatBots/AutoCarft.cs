using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    class AutoCarft : ChatBot
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

        public override void Initialize()
        {
            if (!GetInventoryEnabled())
            {
                ConsoleIO.WriteLogLine("Inventory handling is disabled. AutoCraft will be unloaded");
                UnloadBot();
            }
            RegisterChatBotCommand("autocraft", "auto craft", CommandHandler);
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
                        return String.Format("Total {0} recipes loaded: {1}", recipes.Count, names);
                    case "reload":
                        recipes.Clear();
                        LoadConfig();
                        return "";
                    case "resetcfg":
                        WriteDefaultConfig();
                        return "Resetting your config to the default";
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
                            else return "Specified recipe name do not exist. Check your config file.";
                        }
                        else return "Please specify the recipe name you want to carft.";
                    case "stop":
                        StopCrafting();
                        return "AutoCraft stopped";
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
            return "Auto-crafting bot. Available commands: load, list, reload, resetcfg, start, stop, help. Use /autocraft help <cmd name> for more information";
        }

        private string GetCommandHelp(string cmd)
        {
            switch (cmd.ToLower())
            {
                case "load":
                    return "Load the config from default location.";
                case "list":
                    return "List loaded recipes name.";
                case "reload":
                    return "Reload the config from default location.";
                case "resetcfg":
                    return "Write the default example config to default location.";
                case "start":
                    return "Start the crafting. Usage: /autocraft start <recipe name>";
                case "stop":
                    return "Stop the current running crafting process";
                case "help":
                    return "Get the command description. Usage: /autocraft help <command name>";
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
                ConsoleIO.WriteLogLine("[AutoCraft] No config found. Writing a new one.");
            }
            try
            {
                ParseConfig();
                ConsoleIO.WriteLogLine("[AutoCraft] Successfully loaded");
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLogLine("[AutoCraft] Error while parsing config: \n" + e.Message);
            }
        }

        private void WriteDefaultConfig()
        {
            string[] content =
            {
                "[autocraft]",
                "# A vaild autocraft config must begin with [autocraft]",
                "",
                "tablelocation=0,65,0   # Location of the crafting table if you intended to use it. Terrain and movements must be enabled. Format: x,y,z",
                "onfailure=abort        # What to do on crafting failure, abort or wait",
                "updatedebounce=2       # DO NOT change this unless you know what you are doing. Value must be larger than 0, usually between 1-3",
                "",
                "# You can define multiple recipe in a single config file",
                "# This is an example of how to define a recipe",
                "[recipe]",
                "name=whatever          # name could be whatever you like. This must be in the first place",
                "type=player            # crafting table type: player or table",
                "result=StoneButton     # the resulting item",
                "",
                "# define slots with their deserved item",
                "slot1=Stone            # slot start with 1, count from left to right, top to bottom",
                "# For the naming of the items, please see",
                "# https://github.com/ORelio/Minecraft-Console-Client/blob/master/MinecraftClient/Inventory/ItemType.cs"
            };
            File.WriteAllLines(configPath, content);
        }

        private void ParseConfig()
        {
            string[] content = File.ReadAllLines(configPath);
            if (content[0] != "[autocraft]")
            {
                throw new Exception("Cannot parse this config");
            }

            // local variable for use in parsing config
            string session = "";
            Dictionary<string, Recipe> recipes = new Dictionary<string, Recipe>();
            string lastRecipe = "";

            foreach (string l in content)
            {
                // ignore comment start with #
                if (l.StartsWith("#")) continue;
                string line = l.Split('#')[0].Trim();
                if (line.Length <= 0) continue;

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    session = line.Substring(1, line.Length - 2).ToLower();
                    continue;
                }

                string key = line.Split('=')[0].ToLower();
                if (!(line.Length > (key.Length + 1))) continue;
                string value = line.Substring(key.Length + 1);
                switch (session)
                {
                    case "recipe": parseRecipe(key, value); break;
                    case "autocraft": parseMain(key, value); break;
                }
            }

            // check and save recipe
            foreach(var pair in recipes)
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
                    throw new Exception("Missing item in recipe");
                }
            }

            #region Local method for parsing different session of config

            void parseMain(string key, string value)
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
                        else throw new Exception("Invaild config format");
                        break;
                    case "onfailure":
                        abortOnFailure = value.ToLower() == "abort" ? true : false;
                        break;
                    case "updatedebounce":
                        updateDebounceValue = Convert.ToInt32(value);
                        break;
                }
            }

            void parseRecipe(string key, string value)
            {
                if (key.StartsWith("slot"))
                {
                    int slot = Convert.ToInt32(key[key.Length - 1].ToString());
                    if (slot > 0 && slot < 10)
                    {
                        if (recipes.ContainsKey(lastRecipe))
                        {
                            if (Enum.TryParse(value, out ItemType itemType))
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
                        }
                    }
                    throw new Exception("Invalid config format");
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
                                throw new Exception("Duplicate recipe name specified");
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
                                if (Enum.TryParse(value, out ItemType itemType))
                                {
                                    recipes[lastRecipe].ResultItem = itemType;
                                }
                            }
                            break;
                    }
                }
            }
            #endregion
        }

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
                    SetTimeout("table not found");
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
                // Put item back to inventory. (Using shift-click can take all item at once)
                actionSteps.Add(new ActionStep(ActionType.ShiftClick, inventoryInUse, 0));
                // We need to wait for server to update us after taking item from crafting result
                actionSteps.Add(new ActionStep(ActionType.WaitForUpdate, inventoryInUse));
                // Repeat the whole process again
                actionSteps.Add(new ActionStep(ActionType.Repeat));
                // Start crafting
                ConsoleIO.WriteLogLine("AutoCraft start!");
                HandleNextStep();
            }
            else ConsoleIO.WriteLogLine("AutoCraft cannot be started. Check your available materials");
        }

        /// <summary>
        /// Stop the crafting process by clearing crafting action steps and close the inventory
        /// </summary>
        private void StopCrafting()
        {
            actionSteps.Clear();
            // Closing inventory can make server to update our inventory
            // Useful when
            // - There are some items left in the crafting area
            // - Resynchronize player inventory if using crafting table
            if (GetInventories().ContainsKey(inventoryInUse))
            {
                CloseInventory(inventoryInUse);
                ConsoleIO.WriteLogLine("Inventory #" + inventoryInUse + " was closed by AutoCraft");
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

        /// <summary>
        /// Handle any crafting error after a step was processed
        /// </summary>
        private void HandleError()
        {
            if (craftingFailed)
            {
                if (abortOnFailure)
                {
                    StopCrafting();
                    ConsoleIO.WriteLogLine("Crafting aborted! Check your available materials.");
                }
                else
                {
                    waitingForMaterials = true;
                    // Even though crafting failed, action step index will still increase
                    // we want to do that failed step again so decrease index by 1
                    index--;
                    ConsoleIO.WriteLogLine("Crafting failed! Waiting for more materials");
                }
                if (actionSteps[index - 1].ActionType == ActionType.LeftClick && actionSteps[index - 1].ItemType != ItemType.Air)
                {
                    // Inform user the missing meterial name
                    ConsoleIO.WriteLogLine("Missing material: " + actionSteps[index - 1].ItemType.ToString());
                }
            }
        }

        private void HandleUpdateTimeout()
        {
            ConsoleIO.WriteLogLine("Action timeout! Reason: " + timeoutAction);
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
