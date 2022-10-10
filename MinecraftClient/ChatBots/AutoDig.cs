using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Mapping;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class AutoDig : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoDig";

            public bool Enabled = false;

            [NonSerialized]
            [TomlInlineComment("$config.ChatBot.AutoDig.Auto_Tool_Switch$")]
            public bool Auto_Tool_Switch = false;

            [NonSerialized]
            [TomlInlineComment("$config.ChatBot.AutoDig.Durability_Limit$")]
            public int Durability_Limit = 2;

            [NonSerialized]
            [TomlInlineComment("$config.ChatBot.AutoDig.Drop_Low_Durability_Tools$")]
            public bool Drop_Low_Durability_Tools = false;

            [TomlInlineComment("$config.ChatBot.AutoDig.Mode$")]
            public ModeType Mode = ModeType.lookat;

            [TomlInlineComment("$config.ChatBot.AutoDig.Locations$")]
            public Coordination[] Locations = new Coordination[] { new(123.5, 64, 234.5), new(124.5, 63, 235.5) };

            [TomlInlineComment("$config.ChatBot.AutoDig.Location_Order$")]
            public OrderType Location_Order = OrderType.distance;

            [TomlInlineComment("$config.ChatBot.AutoDig.Auto_Start_Delay$")]
            public double Auto_Start_Delay = 3.0;

            [TomlInlineComment("$config.ChatBot.AutoDig.Dig_Timeout$")]
            public double Dig_Timeout = 60.0;

            [TomlInlineComment("$config.ChatBot.AutoDig.Log_Block_Dig$")]
            public bool Log_Block_Dig = true;

            [TomlInlineComment("$config.ChatBot.AutoDig.List_Type$")]
            public ListType List_Type = ListType.whitelist;

            public List<Material> Blocks = new() { Material.Cobblestone, Material.Stone };

            [NonSerialized]
            public Location[] _Locations = Array.Empty<Location>();

            public void OnSettingUpdate()
            {
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

                public Coordination(double x, double y, double z)
                {
                    this.x = x; this.y = y; this.z = z;
                }
            }
        }

        private bool inventoryEnabled;

        private int counter = 0;
        private readonly object stateLock = new();
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
                LogToConsoleTranslated("extra.terrainandmovement_required");
                LogToConsoleTranslated("general.bot_unload");
                UnloadBot();
                return;
            }

            inventoryEnabled = GetInventoryEnabled();
            if (!inventoryEnabled && Config.Auto_Tool_Switch)
                LogToConsoleTranslated("bot.autodig.no_inv_handle");

            RegisterChatBotCommand("digbot", Translations.Get("bot.digbot.cmd"), GetHelp(), CommandHandler);
        }

        public string CommandHandler(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "start":
                        lock (stateLock)
                        {
                            counter = 0;
                            state = State.WaitingStart;
                        }
                        return Translations.Get("bot.autodig.start");
                    case "stop":
                        StopDigging();
                        return Translations.Get("bot.autodig.stop");
                    case "help":
                        return GetCommandHelp(args.Length >= 2 ? args[1] : "");
                    default:
                        return GetHelp();
                }
            }
            else return GetHelp();
        }

        private void StartDigging()
        {
            if (Config.Auto_Start_Delay > 0)
            {
                double delay = Config.Auto_Start_Delay;
                LogToConsole(Translations.Get("bot.autodig.start_delay", delay));
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
                            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autodig.dig_timeout"));
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
                            LogToConsole(Translations.Get("cmd.dig.too_far"));
                    }
                    return false;
                }
                else if (block.Type == Material.Air)
                {
                    if (!AlreadyWaitting)
                    {
                        AlreadyWaitting = true;
                        if (Config.Log_Block_Dig)
                            LogToConsole(Translations.Get("cmd.dig.no_block"));
                    }
                    return false;
                }
                else if ((Config.List_Type == Configs.ListType.whitelist && Config.Blocks.Contains(block.Type)) ||
                        (Config.List_Type == Configs.ListType.blacklist && !Config.Blocks.Contains(block.Type)))
                {
                    if (Config.Mode == Configs.ModeType.lookat || 
                        (Config.Mode == Configs.ModeType.both && Config._Locations.Contains(blockLoc)))
                    {
                        if (DigBlock(blockLoc, lookAtBlock: false))
                        {
                            currentDig = blockLoc;
                            if (Config.Log_Block_Dig)
                                LogToConsole(Translations.Get("cmd.dig.dig", blockLoc.X, blockLoc.Y, blockLoc.Z, block.Type));
                            return true;
                        }
                        else
                        {
                            LogToConsole(Translations.Get("cmd.dig.fail"));
                            return false;
                        }
                    }
                    else
                    {
                        if (!AlreadyWaitting)
                        {
                            AlreadyWaitting = true;
                            if (Config.Log_Block_Dig)
                                LogToConsole(Translations.Get("bot.autodig.not_allow"));
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
                            LogToConsole(Translations.Get("bot.autodig.not_allow"));
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
                    if (DigBlock(target, lookAtBlock: true))
                    {
                        currentDig = target;
                        if (Config.Log_Block_Dig)
                            LogToConsole(Translations.Get("cmd.dig.dig", target.X, target.Y, target.Z, targetBlock.Type));
                        return true;
                    }
                    else
                    {
                        LogToConsole(Translations.Get("cmd.dig.fail"));
                        return false;
                    }
                }
                else
                {
                    if (!AlreadyWaitting)
                    {
                        AlreadyWaitting = true;
                        if (Config.Log_Block_Dig)
                            LogToConsole(Translations.Get("cmd.dig.no_block"));
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
                        if (DigBlock(blockLoc, lookAtBlock: true))
                        {
                            currentDig = blockLoc;
                            if (Config.Log_Block_Dig)
                                LogToConsole(Translations.Get("cmd.dig.dig", blockLoc.X, blockLoc.Y, blockLoc.Z, block.Type));
                            return true;
                        }
                        else
                        {
                            LogToConsole(Translations.Get("cmd.dig.fail"));
                            return false;
                        }
                    }
                }

                if (!AlreadyWaitting)
                {
                    AlreadyWaitting = true;
                    if (Config.Log_Block_Dig)
                        LogToConsole(Translations.Get("cmd.dig.no_block"));
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

        private static string GetHelp()
        {
            return Translations.Get("bot.autodig.available_cmd", "start, stop, help");
        }

        private string GetCommandHelp(string cmd)
        {
            return cmd.ToLower() switch
            {
#pragma warning disable format // @formatter:off
                "start"     =>   Translations.Get("bot.autodig.help.start"),
                "stop"      =>   Translations.Get("bot.autodig.help.stop"),
                "help"      =>   Translations.Get("bot.autodig.help.help"),
                _           =>    GetHelp(),
#pragma warning restore format // @formatter:on
            };
        }
    }
}
