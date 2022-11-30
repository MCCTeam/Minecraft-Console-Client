using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;
using Tomlet.Attributes;
using static MinecraftClient.ChatBots.AutoDrop.Configs;

namespace MinecraftClient.ChatBots
{
    public class AutoDrop : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoDrop";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.AutoDrop.Mode$")]
            public DropMode Mode = DropMode.include;

            public List<ItemType> Items = new() { ItemType.Cobblestone, ItemType.Dirt };

            public void OnSettingUpdate() { }

            public enum DropMode
            {
                include,    // Items in list will be dropped
                exclude,    // Items in list will be kept
                everything  // Everything will be dropped
            }
        }

        private int updateDebounce = 0;
        private readonly int updateDebounceValue = 2;
        private int inventoryUpdated = -1;

        public string CommandHandler(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        Config.Enabled = true;
                        inventoryUpdated = 0;
                        OnUpdateFinish();
                        return Translations.bot_autoDrop_on;
                    case "off":
                        Config.Enabled = false;
                        return Translations.bot_autoDrop_off;
                    case "add":
                        if (args.Length >= 2)
                        {
                            if (Enum.TryParse(args[1], true, out ItemType item))
                            {
                                Config.Items.Add(item);
                                return string.Format(Translations.bot_autoDrop_added, item.ToString());
                            }
                            else
                            {
                                return string.Format(Translations.bot_autoDrop_incorrect_name, args[1]);
                            }
                        }
                        else
                        {
                            return Translations.cmd_inventory_help_usage + ": add <item name>";
                        }
                    case "remove":
                        if (args.Length >= 2)
                        {
                            if (Enum.TryParse(args[1], true, out ItemType item))
                            {
                                if (Config.Items.Contains(item))
                                {
                                    Config.Items.Remove(item);
                                    return string.Format(Translations.bot_autoDrop_removed, item.ToString());
                                }
                                else
                                {
                                    return Translations.bot_autoDrop_not_in_list;
                                }
                            }
                            else
                            {
                                return string.Format(Translations.bot_autoDrop_incorrect_name, args[1]);
                            }
                        }
                        else
                        {
                            return Translations.cmd_inventory_help_usage + ": remove <item name>";
                        }
                    case "list":
                        if (Config.Items.Count > 0)
                        {
                            return string.Format(Translations.bot_autoDrop_list, Config.Items.Count, string.Join("\n", Config.Items));
                        }
                        else
                        {
                            return Translations.bot_autoDrop_no_item;
                        }
                    case "mode":
                        if (args.Length >= 2)
                        {
                            switch (args[1].ToLower())
                            {
                                case "include":
                                    Config.Mode = DropMode.include;
                                    break;
                                case "exclude":
                                    Config.Mode = DropMode.exclude;
                                    break;
                                case "everything":
                                    Config.Mode = DropMode.everything;
                                    break;
                                default:
                                    return Translations.bot_autoDrop_unknown_mode; // Unknwon mode. Available modes: Include, Exclude, Everything
                            }
                            inventoryUpdated = 0;
                            OnUpdateFinish();
                            return string.Format(Translations.bot_autoDrop_switched, Config.Mode.ToString()); // Switched to {0} mode.
                        }
                        else
                        {
                            return Translations.bot_autoDrop_unknown_mode;
                        }
                    default:
                        return GetHelp();
                }
            }
            else
            {
                return GetHelp();
            }
        }

        private static string GetHelp()
        {
            return string.Format(Translations.general_available_cmd, "on, off, add, remove, list, mode");
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
            RegisterChatBotCommand("autodrop", Translations.bot_autoDrop_cmd, GetHelp(), CommandHandler);
            RegisterChatBotCommand("ad", Translations.bot_autoDrop_alias, GetHelp(), CommandHandler);
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
            if (Config.Enabled)
            {
                updateDebounce = updateDebounceValue;
                // Always interact container if available (larger ID) because they included player inventory (ID 0)
                if (inventoryId >= inventoryUpdated)
                    inventoryUpdated = inventoryId;
            }
        }

        private void OnUpdateFinish()
        {
            if (inventoryUpdated != -1)
            {
                if (!GetInventories().ContainsKey(inventoryUpdated))
                {
                    // Inventory updated but no inventory ?
                    LogDebugToConsole(string.Format(Translations.bot_autoDrop_no_inventory, inventoryUpdated));
                    return;
                }
                var inventory = GetInventories()[inventoryUpdated];
                var items = inventory.Items.ToDictionary(entry => entry.Key, entry => entry.Value);
                if (Config.Mode == DropMode.include)
                {
                    foreach (var item in items)
                    {
                        // Ingore crafting result slot
                        if (item.Key == 0)
                            continue;
                        if (Config.Items.Contains(item.Value.Type))
                        {
                            // Drop it !!
                            WindowAction(inventoryUpdated, item.Key, WindowActionType.DropItemStack);
                        }
                    }
                }
                else if (Config.Mode == DropMode.exclude)
                {
                    foreach (var item in items)
                    {
                        // Ingore crafting result slot
                        if (item.Key == 0)
                            continue;
                        if (!Config.Items.Contains(item.Value.Type))
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
                        // Ingore crafting result slot
                        if (item.Key == 0)
                            continue;
                        // Drop it !!
                        WindowAction(inventoryUpdated, item.Key, WindowActionType.DropItemStack);
                    }
                }
                inventoryUpdated = -1;
            }
        }
    }
}
