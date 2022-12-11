using System;
using System.Collections.Generic;
using System.Linq;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Inventory;
using MinecraftClient.Scripting;
using Tomlet.Attributes;
using static MinecraftClient.ChatBots.AutoDrop.Configs;

namespace MinecraftClient.ChatBots
{
    public class AutoDrop : ChatBot
    {
        public const string CommandName = "autodrop";

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
                    .Then(l => l.Literal("add")
                        .Executes(r => OnCommandHelp(r.Source, "add")))
                    .Then(l => l.Literal("remove")
                        .Executes(r => OnCommandHelp(r.Source, "remove")))
                    .Then(l => l.Literal("mode")
                        .Executes(r => OnCommandHelp(r.Source, "mode")))
                )
            );

            McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("on")
                    .Executes(r => OnCommandEnable(r.Source, true)))
                .Then(l => l.Literal("off")
                    .Executes(r => OnCommandEnable(r.Source, false)))
                .Then(l => l.Literal("add")
                    .Then(l => l.Argument("ItemType", MccArguments.ItemType())
                        .Executes(r => OnCommandAdd(r.Source, MccArguments.GetItemType(r, "ItemType")))))
                .Then(l => l.Literal("remove")
                    .Then(l => l.Argument("ItemType", MccArguments.ItemType())
                        .Executes(r => OnCommandRemove(r.Source, MccArguments.GetItemType(r, "ItemType")))))
                .Then(l => l.Literal("list")
                    .Executes(r => OnCommandList(r.Source)))
                .Then(l => l.Literal("mode")
                    .Then(l => l.Literal("include")
                        .Executes(r => OnCommandMode(r.Source, DropMode.include)))
                    .Then(l => l.Literal("exclude")
                        .Executes(r => OnCommandMode(r.Source, DropMode.exclude)))
                    .Then(l => l.Literal("everything")
                        .Executes(r => OnCommandMode(r.Source, DropMode.everything))))
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
                "add"       =>   Translations.cmd_inventory_help_usage + ": add <item name>",
                "remove"    =>   Translations.cmd_inventory_help_usage + ": remove <item name>",
                "mode"      =>   Translations.bot_autoDrop_unknown_mode,
                _           =>   string.Format(Translations.general_available_cmd, "on, off, add, remove, list, mode")
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandEnable(CmdResult r, bool enable)
        {
            if (enable)
            {
                Config.Enabled = true;
                inventoryUpdated = 0;
                OnUpdateFinish();
                return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autoDrop_on);
            }
            else
            {
                Config.Enabled = false;
                return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autoDrop_off);
            }
        }

        private int OnCommandAdd(CmdResult r, ItemType item)
        {
            Config.Items.Add(item);
            return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.bot_autoDrop_added, item.ToString()));
        }

        private int OnCommandRemove(CmdResult r, ItemType item)
        {
            if (Config.Items.Contains(item))
            {
                Config.Items.Remove(item);
                return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.bot_autoDrop_removed, item.ToString()));
            }
            else
            {
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.bot_autoDrop_not_in_list);
            }
        }

        private int OnCommandList(CmdResult r)
        {
            if (Config.Items.Count > 0)
                return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.bot_autoDrop_list, Config.Items.Count, string.Join("\n", Config.Items)));
            else
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.bot_autoDrop_no_item);
        }

        private int OnCommandMode(CmdResult r, DropMode mode)
        {
            Config.Mode = mode;
            inventoryUpdated = 0;
            OnUpdateFinish();
            return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.bot_autoDrop_switched, Config.Mode.ToString()));
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
