using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class AutoDig : ChatBot
    {
        public const string CommandName = "autodig";

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoDig";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.AutoDig.Auto_Tool_Switch$")]
            public bool Auto_Tool_Switch = false;

            [TomlInlineComment("$ChatBot.AutoDig.Durability_Limit$")]
            public int Durability_Limit = 2;

            [TomlInlineComment("$ChatBot.AutoDig.Drop_Low_Durability_Tools$")]
            public bool Drop_Low_Durability_Tools = false;

            [TomlInlineComment("$ChatBot.AutoDig.Mode$")]
            public ModeType Mode = ModeType.lookat;

            [TomlPrecedingComment("$ChatBot.AutoDig.Locations$")]
            public Coordination[] Locations = new Coordination[] { new(123.5, 64, 234.5), new(124.5, 63, 235.5) };

            [TomlInlineComment("$ChatBot.AutoDig.Location_Order$")]
            public OrderType Location_Order = OrderType.distance;

            [TomlInlineComment("$ChatBot.AutoDig.Auto_Start_Delay$")]
            public double Auto_Start_Delay = 3.0;

            [TomlInlineComment("$ChatBot.AutoDig.Dig_Timeout$")]
            public double Dig_Timeout = 60.0;

            [TomlInlineComment("$ChatBot.AutoDig.Log_Block_Dig$")]
            public bool Log_Block_Dig = true;

            [TomlInlineComment("$ChatBot.AutoDig.List_Type$")]
            public ListType List_Type = ListType.whitelist;

            public List<Material> Blocks = new() { Material.Cobblestone, Material.Stone };

            [NonSerialized]
            public Location[] _Locations = Array.Empty<Location>();

            public void OnSettingUpdate()
            {
                Durability_Limit = Math.Max(0, Durability_Limit);

                if (Auto_Start_Delay >= 0)
                    Auto_Start_Delay = Math.Max(0.1, Auto_Start_Delay);

                if (Dig_Timeout >= 0)
                    Dig_Timeout = Math.Max(0.1, Dig_Timeout);

                _Locations = new Location[Locations.Length];
                for (int i = 0; i < Locations.Length; ++i)
                    _Locations[i] = new(Locations[i].x, Locations[i].y, Locations[i].z);
            }

            public enum ModeType { lookat, fixedpos, both };

            public enum ListType { blacklist, whitelist };

            public enum OrderType { distance, index };

            public struct Coordination
            {
                public double x, y, z;

                public Coordination()
                {
                    x = 0;
                    y = 0;
                    z = 0;
                }

                public Coordination(double x, double y, double z)
                {
                    this.x = x; this.y = y; this.z = z;
                }
            }
        }

        private bool inventoryEnabled;

        private int counter = 0;
        private readonly Lock stateLock = new();
        private State state = State.WaitJoinGame;

        bool AlreadyWaitting = false;
        private Location currentDig = Location.Zero;

        private enum State
        {
            WaitJoinGame,
            WaitingStart,
            Digging,
            Stopping,
        }

        public override void Initialize()
        {
            if (!GetTerrainEnabled())
            {
                LogToConsole(Translations.extra_terrainandmovement_required);
                LogToConsole(Translations.general_bot_unload);
                UnloadBot();
                return;
            }

            inventoryEnabled = GetInventoryEnabled();
            if (!inventoryEnabled && Config.Auto_Tool_Switch)
                LogToConsole(Translations.bot_autodig_no_inv_handle);

            McClient.dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Then(l => l.Literal("start")
                        .Executes(r => OnCommandHelp(r.Source, "start")))
                    .Then(l => l.Literal("stop")
                        .Executes(r => OnCommandHelp(r.Source, "stop")))
                    .Then(l => l.Literal("help")
                        .Executes(r => OnCommandHelp(r.Source, "help")))
                )
            );

            var cmd = McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("start")
                    .Executes(r => OnCommandStart(r.Source)))
                .Then(l => l.Literal("stop")
                    .Executes(r => OnCommandStop(r.Source)))
                .Then(l => l.Literal("_help")
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );

            McClient.dispatcher.Register(l => l.Literal("digbot")
                .Redirect(cmd)
            );
        }

        public override void OnUnload()
        {
            McClient.dispatcher.Unregister("digbot");
            McClient.dispatcher.Unregister(CommandName);
            McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
        }

        private int OnCommandHelp(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                "start"     =>   Translations.bot_autodig_help_start,
                "stop"      =>   Translations.bot_autodig_help_stop,
                "help"      =>   Translations.bot_autodig_help_help,
                _           =>   string.Format(Translations.bot_autodig_available_cmd, "start, stop, help")
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandStart(CmdResult r)
        {
            lock (stateLock)
            {
                counter = 0;
                state = State.WaitingStart;
            }
            return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autodig_start);
        }

        private int OnCommandStop(CmdResult r)
        {
            StopDigging();
            return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autodig_stop);
        }

        private void StartDigging()
        {
            if (Config.Auto_Start_Delay > 0)
            {
                double delay = Config.Auto_Start_Delay;
                LogToConsole(string.Format(Translations.bot_autodig_start_delay, delay));
                lock (stateLock)
                {
                    counter = Settings.DoubleToTick(delay);
                    state = State.WaitingStart;
                }
            }
            else
            {
                lock (stateLock)
                {
                    state = State.WaitJoinGame;
                }
            }
        }

        private void StopDigging()
        {
            state = State.Stopping;
            lock (stateLock)
            {
                state = State.Stopping;
            }
        }

        private static int GetLegacyMaxDamage(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.WoodenPickaxe or ItemType.WoodenAxe or ItemType.WoodenShovel or ItemType.WoodenSword or ItemType.WoodenHoe => 59,
                ItemType.StonePickaxe or ItemType.StoneAxe or ItemType.StoneShovel or ItemType.StoneSword or ItemType.StoneHoe => 131,
                ItemType.IronPickaxe or ItemType.IronAxe or ItemType.IronShovel or ItemType.IronSword or ItemType.IronHoe => 250,
                ItemType.GoldenPickaxe or ItemType.GoldenAxe or ItemType.GoldenShovel or ItemType.GoldenSword or ItemType.GoldenHoe => 32,
                ItemType.DiamondPickaxe or ItemType.DiamondAxe or ItemType.DiamondShovel or ItemType.DiamondSword or ItemType.DiamondHoe => 1561,
                ItemType.NetheritePickaxe or ItemType.NetheriteAxe or ItemType.NetheriteShovel or ItemType.NetheriteSword or ItemType.NetheriteHoe => 2031,
                ItemType.Shears => 238,
                _ => 0
            };
        }

        private static int GetMaxDamage(Item item)
        {
            if (item.Components is not null)
            {
                var maxDamageComponent = item.Components.OfType<MaxDamageComponent>().FirstOrDefault();
                if (maxDamageComponent is not null)
                    return maxDamageComponent.MaxDamage;
            }

            return GetLegacyMaxDamage(item.Type);
        }

        private static int GetRemainingDurability(Item item)
        {
            int maxDamage = GetMaxDamage(item);
            return maxDamage > 0 ? maxDamage - item.Damage : int.MaxValue;
        }

        private bool HasEnoughDurability(Item item)
        {
            return Config.Durability_Limit <= 0 || GetRemainingDurability(item) >= Config.Durability_Limit;
        }

        private bool IsBelowDurabilityLimit(Item? item)
        {
            return item is not null && Config.Durability_Limit > 0 && GetRemainingDurability(item) < Config.Durability_Limit;
        }

        private static bool IsRecommendedTool(Item? item, ItemType[] recommendedTools)
        {
            return item is not null && recommendedTools.Contains(item.Type);
        }

        private bool SwapToolIntoHand(int sourceSlot, int handSlot)
        {
            return WindowAction(0, sourceSlot, WindowActionType.LeftClick)
                && WindowAction(0, handSlot, WindowActionType.LeftClick)
                && WindowAction(0, sourceSlot, WindowActionType.LeftClick);
        }

        private bool EnsureSuitableTool(Material blockType)
        {
            if (!inventoryEnabled || !Config.Auto_Tool_Switch)
                return true;

            ItemType[] recommendedTools = Material2Tool.GetCorrectToolForBlock(blockType);
            if (recommendedTools.Length == 0)
                return true;

            Container container = GetPlayerInventory();
            int handSlot = 36 + GetCurrentSlot();
            container.Items.TryGetValue(handSlot, out Item? currentTool);

            if (currentTool is not null && IsRecommendedTool(currentTool, recommendedTools) && HasEnoughDurability(currentTool))
                return true;

            foreach (ItemType recommendedTool in recommendedTools)
            {
                foreach ((int slot, Item item) in container.Items)
                {
                    if (slot == handSlot || item.Type != recommendedTool || !HasEnoughDurability(item))
                        continue;

                    if (!SwapToolIntoHand(slot, handSlot))
                        return false;

                    LogToConsole(GetTimestamp() + ": " + string.Format(Translations.bot_autodig_switch, item.GetTypeString(), slot));

                    if (Config.Drop_Low_Durability_Tools && IsBelowDurabilityLimit(currentTool) &&
                        WindowAction(0, slot, WindowActionType.DropItemStack))
                    {
                        LogToConsole(GetTimestamp() + ": " + string.Format(Translations.bot_autodig_drop_low_durability, currentTool!.GetTypeString(), slot));
                    }

                    return true;
                }
            }

            return !IsBelowDurabilityLimit(currentTool);
        }

        public override void Update()
        {
            lock (stateLock)
            {
                switch (state)
                {
                    case State.WaitJoinGame:
                        break;
                    case State.WaitingStart:
                        if (--counter < 0)
                        {
                            if (DoDigging())
                            {
                                AlreadyWaitting = false;
                                state = State.Digging;
                            }
                            else
                            {
                                counter = 0;
                                state = State.WaitingStart;
                            }
                        }
                        break;
                    case State.Digging:
                        if (++counter > Settings.DoubleToTick(Config.Dig_Timeout))
                        {
                            LogToConsole(GetTimestamp() + ": " + Translations.bot_autodig_dig_timeout);
                            state = State.WaitingStart;
                            counter = 0;
                        }
                        break;
                    case State.Stopping:
                        break;
                }
            }
        }

        private bool DoDigging()
        {
            if (Config.Mode == Configs.ModeType.lookat || Config.Mode == Configs.ModeType.both)
            {
                (bool hasBlock, Location blockLoc, Block block) = GetLookingBlock(4.5, false);
                if (!hasBlock)
                {
                    if (!AlreadyWaitting)
                    {
                        AlreadyWaitting = true;
                        if (Config.Log_Block_Dig)
                            LogToConsole(Translations.cmd_dig_too_far);
                    }
                    return false;
                }
                else if (block.Type == Material.Air)
                {
                    if (!AlreadyWaitting)
                    {
                        AlreadyWaitting = true;
                        if (Config.Log_Block_Dig)
                            LogToConsole(Translations.cmd_dig_no_block);
                    }
                    return false;
                }
                else if ((Config.List_Type == Configs.ListType.whitelist && Config.Blocks.Contains(block.Type)) ||
                        (Config.List_Type == Configs.ListType.blacklist && !Config.Blocks.Contains(block.Type)))
                {
                    if (Config.Mode == Configs.ModeType.lookat ||
                        (Config.Mode == Configs.ModeType.both && Config._Locations.Contains(blockLoc)))
                    {
                        if (!EnsureSuitableTool(block.Type))
                            return false;

                        if (DigBlock(blockLoc, Direction.Down, lookAtBlock: false))
                        {
                            currentDig = blockLoc;
                            if (Config.Log_Block_Dig)
                                LogToConsole(string.Format(Translations.cmd_dig_dig, blockLoc.X, blockLoc.Y, blockLoc.Z, block.GetTypeString()));
                            return true;
                        }
                        else
                        {
                            LogToConsole(Translations.cmd_dig_fail);
                            return false;
                        }
                    }
                    else
                    {
                        if (!AlreadyWaitting)
                        {
                            AlreadyWaitting = true;
                            if (Config.Log_Block_Dig)
                                LogToConsole(Translations.bot_autodig_not_allow);
                        }
                        return false;
                    }
                }
                else
                {
                    if (!AlreadyWaitting)
                    {
                        AlreadyWaitting = true;
                        if (Config.Log_Block_Dig)
                            LogToConsole(Translations.bot_autodig_not_allow);
                    }
                    return false;
                }
            }
            else if (Config.Mode == Configs.ModeType.fixedpos && Config.Location_Order == Configs.OrderType.distance)
            {
                Location current = GetCurrentLocation();

                double minDistance = double.MaxValue;
                Location target = Location.Zero;
                Block targetBlock = Block.Air;
                foreach (Location location in Config._Locations)
                {
                    Block block = GetWorld().GetBlock(location);
                    if (block.Type != Material.Air &&
                        ((Config.List_Type == Configs.ListType.whitelist && Config.Blocks.Contains(block.Type)) ||
                        (Config.List_Type == Configs.ListType.blacklist && !Config.Blocks.Contains(block.Type))))
                    {
                        double distance = current.Distance(location);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            target = location;
                            targetBlock = block;
                        }
                    }
                }

                if (minDistance <= 6.0)
                {
                    if (!EnsureSuitableTool(targetBlock.Type))
                        return false;

                    if (DigBlock(target, Direction.Down, lookAtBlock: true))
                    {
                        currentDig = target;
                        if (Config.Log_Block_Dig)
                            LogToConsole(string.Format(Translations.cmd_dig_dig, target.X, target.Y, target.Z, targetBlock.GetTypeString()));
                        return true;
                    }
                    else
                    {
                        LogToConsole(Translations.cmd_dig_fail);
                        return false;
                    }
                }
                else
                {
                    if (!AlreadyWaitting)
                    {
                        AlreadyWaitting = true;
                        if (Config.Log_Block_Dig)
                            LogToConsole(Translations.cmd_dig_no_block);
                    }
                    return false;
                }
            }
            else if (Config.Mode == Configs.ModeType.fixedpos && Config.Location_Order == Configs.OrderType.index)
            {
                for (int i = 0; i < Config._Locations.Length; ++i)
                {
                    Location blockLoc = Config._Locations[i];
                    Block block = GetWorld().GetBlock(blockLoc);
                    if (block.Type != Material.Air &&
                        ((Config.List_Type == Configs.ListType.whitelist && Config.Blocks.Contains(block.Type)) ||
                        (Config.List_Type == Configs.ListType.blacklist && !Config.Blocks.Contains(block.Type))))
                    {
                        if (!EnsureSuitableTool(block.Type))
                            return false;

                        if (DigBlock(blockLoc, Direction.Down, lookAtBlock: true))
                        {
                            currentDig = blockLoc;
                            if (Config.Log_Block_Dig)
                                LogToConsole(string.Format(Translations.cmd_dig_dig, blockLoc.X, blockLoc.Y, blockLoc.Z, block.GetTypeString()));
                            return true;
                        }
                        else
                        {
                            LogToConsole(Translations.cmd_dig_fail);
                            return false;
                        }
                    }
                }

                if (!AlreadyWaitting)
                {
                    AlreadyWaitting = true;
                    if (Config.Log_Block_Dig)
                        LogToConsole(Translations.cmd_dig_no_block);
                }
                return false;
            }
            return false;
        }

        public override void OnBlockChange(Location location, Block block)
        {
            if (location == currentDig)
            {
                lock (stateLock)
                {
                    if (state == State.Digging && location == currentDig)
                    {
                        currentDig = Location.Zero;
                        counter = 0;
                        state = State.WaitingStart;
                    }
                }
            }
        }

        public override void AfterGameJoined()
        {
            StartDigging();
        }

        public override void OnRespawn()
        {
            StartDigging();
        }

        public override void OnDeath()
        {
            StopDigging();
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            StopDigging();

            return base.OnDisconnect(reason, message);
        }
    }
}
