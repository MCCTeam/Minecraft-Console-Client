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
        private bool waitingForUpdate = false;
        private bool craftingFailed = false;
        private int inventoryInUse = -2;
        private int index = 0;
        private Recipe recipeInUse;
        private List<ActionStep> actionSteps = new List<ActionStep>();

        private int updateDebounce = 0;

        private string configPath = @"autocraft\config.ini";

        private Dictionary<string, Recipe> recipes = new Dictionary<string, Recipe>();

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
            RegisterChatBotCommand("craft", "craft", CraftCommand);
            RegisterChatBotCommand("open", "open", Open);
            RegisterChatBotCommand("autocraft", "auto craft", CommandHandler);
        }

        public string CommandHandler(string cmd, string[] args)
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
            }
            return "";
        }

        public string Open(string cmd, string[] args)
        {
            double x = Convert.ToDouble(args[0]);
            double y = Convert.ToDouble(args[1]);
            double z = Convert.ToDouble(args[2]);
            SendPlaceBlock(new Location(x, y, z), .5f, .5f, .5f, Direction.Up);
            return "ok";
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

        public string LoadConfigRunner(string command, string[] args)
        {
            LoadConfig();
            return "ok";
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
                ConsoleIO.WriteLogLine("No config found. Writing a new one.");
            }
            try
            {
                ParseConfig();
                ConsoleIO.WriteLogLine("Successfully loaded");
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLogLine("Error while parsing config: \n" + e.Message);
            }
        }

        private void WriteDefaultConfig()
        {
            string[] content =
            {
                "[autocraft]",
                "# A vaild autocraft config must begin with [autocraft]",
                "# You can define multiple recipe in the config file",
                "# This is a example of how to define a recipe",
                "[recipe]",
                "# name could be whatever you like. This must be in the first place",
                "name=whatever",
                "# crafting table type: player or table",
                "type=player",
                "# the resulting item",
                "result=StoneButton",
                "# define slots with their deserved item",
                "# slot start with 1, count from top to bottom, left to right",
                "slot1=Stone",
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
                    case "recipe":
                        parseRecipe(key, value);
                        break;
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

        #endregion
    }
}
