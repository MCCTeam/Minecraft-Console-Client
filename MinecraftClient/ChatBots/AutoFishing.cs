using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;
using static MinecraftClient.ChatBots.AutoFishing.Configs;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// The AutoFishing bot semi-automates fishing.
    /// The player needs to have a fishing rod in hand, then manually send it using the UseItem command.
    /// </summary>
    public class AutoFishing : ChatBot
    {
        public const string CommandName = "autofishing";

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoFishing";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.AutoFishing.Antidespawn$")]
            public bool Antidespawn = false;

            [TomlInlineComment("$ChatBot.AutoFishing.Mainhand$")]
            public bool Mainhand = true;

            [TomlInlineComment("$ChatBot.AutoFishing.Auto_Start$")]
            public bool Auto_Start = true;

            [TomlInlineComment("$ChatBot.AutoFishing.Cast_Delay$")]
            public double Cast_Delay = 0.4;

            [TomlInlineComment("$ChatBot.AutoFishing.Fishing_Delay$")]
            public double Fishing_Delay = 3.0;

            [TomlInlineComment("$ChatBot.AutoFishing.Fishing_Timeout$")]
            public double Fishing_Timeout = 300.0;

            [TomlInlineComment("$ChatBot.AutoFishing.Durability_Limit$")]
            public double Durability_Limit = 2;

            [TomlInlineComment("$ChatBot.AutoFishing.Auto_Rod_Switch$")]
            public bool Auto_Rod_Switch = true;

            [TomlInlineComment("$ChatBot.AutoFishing.Stationary_Threshold$")]
            public double Stationary_Threshold = 0.001;

            [TomlInlineComment("$ChatBot.AutoFishing.Hook_Threshold$")]
            public double Hook_Threshold = 0.2;

            [TomlInlineComment("$ChatBot.AutoFishing.Log_Fish_Bobber$")]
            public bool Log_Fish_Bobber = false;

            [TomlInlineComment("$ChatBot.AutoFishing.Enable_Move$")]
            public bool Enable_Move = false;

            [TomlPrecedingComment("$ChatBot.AutoFishing.Movements$")]
            public LocationConfig[] Movements = new LocationConfig[]
            {
                new LocationConfig(12.34, -23.45),
                new LocationConfig(123.45, 64, -654.32, -25.14, 36.25),
                new LocationConfig(-1245.63, 63.5, 1.2),
            };

            public void OnSettingUpdate()
            {
                if (Cast_Delay < 0)
                    Cast_Delay = 0;

                if (Fishing_Delay < 0)
                    Fishing_Delay = 0;

                if (Fishing_Timeout < 0)
                    Fishing_Timeout = 0;

                if (Durability_Limit < 0)
                    Durability_Limit = 0;
                else if (Durability_Limit > 64)
                    Durability_Limit = 64;

                if (Stationary_Threshold < 0)
                    Stationary_Threshold = -Stationary_Threshold;

                if (Hook_Threshold < 0)
                    Hook_Threshold = -Hook_Threshold;
            }

            public struct LocationConfig
            {
                public Coordination? XYZ;
                public Facing? facing;

                public LocationConfig(double yaw, double pitch)
                {
                    this.XYZ = null;
                    this.facing = new(yaw, pitch);
                }

                public LocationConfig(double x, double y, double z)
                {
                    this.XYZ = new(x, y, z);
                    this.facing = null;
                }

                public LocationConfig(double x, double y, double z, double yaw, double pitch)
                {
                    this.XYZ = new(x, y, z);
                    this.facing = new(yaw, pitch);
                }

                public struct Coordination
                {
                    public double x, y, z;

                    public Coordination(double x, double y, double z)
                    {
                        this.x = x; this.y = y; this.z = z;
                    }
                }

                public struct Facing
                {
                    public double yaw, pitch;

                    public Facing(double yaw, double pitch)
                    {
                        this.yaw = yaw; this.pitch = pitch;
                    }
                }
            }
        }

        private int fishCount = 0;
        private bool inventoryEnabled;
        private int castTimeout = 12;

        private bool isFishing = false, isWaitingRod = false;
        private Entity? fishingBobber;
        private Location LastPos = Location.Zero;
        private DateTime CaughtTime = DateTime.Now;
        private int fishItemCounter = 15;
        private Dictionary<ItemType, uint> fishItemCnt = new();
        private Entity fishItem = new(-1, EntityType.Item, Location.Zero);

        private int counter = 0;
        private readonly object stateLock = new();
        private FishingState state = FishingState.WaitJoinGame;

        private int curLocationIdx = 0, moveDir = 1;
        float nextYaw = 0, nextPitch = 0;

        private enum FishingState
        {
            WaitJoinGame,
            WaitingToCast,
            CastingRod,
            WaitingFishingBobber,
            WaitingFishToBite,
            StartMove,
            WaitingMovement,
            DurabilityCheck,
            Stopping,
        }

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                LogToConsole(Translations.extra_entity_required);
                state = FishingState.WaitJoinGame;
            }

            inventoryEnabled = GetInventoryEnabled();
            if (!inventoryEnabled)
                LogToConsole(Translations.bot_autoFish_no_inv_handle);

            McClient.dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Then(l => l.Literal("start")
                        .Executes(r => OnCommandHelp(r.Source, "start")))
                    .Then(l => l.Literal("stop")
                        .Executes(r => OnCommandHelp(r.Source, "stop")))
                    .Then(l => l.Literal("status")
                        .Executes(r => OnCommandHelp(r.Source, "status")))
                    .Then(l => l.Literal("help")
                        .Executes(r => OnCommandHelp(r.Source, "help")))
                )
            );

            McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("start")
                    .Executes(r => OnCommandStart(r.Source)))
                .Then(l => l.Literal("stop")
                    .Executes(r => OnCommandStop(r.Source)))
                .Then(l => l.Literal("status")
                    .Executes(r => OnCommandStatus(r.Source))
                    .Then(l => l.Literal("clear")
                        .Executes(r => OnCommandStatusClear(r.Source))))
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
                "start"     =>   Translations.bot_autoFish_help_start,
                "stop"      =>   Translations.bot_autoFish_help_stop,
                "status"    =>   Translations.bot_autoFish_help_status,
                "help"      =>   Translations.bot_autoFish_help_help,
                _           =>   string.Format(Translations.bot_autoFish_available_cmd, "start, stop, status, help")
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandStart(CmdResult r)
        {
            isFishing = false;
            lock (stateLock)
            {
                isFishing = false;
                counter = 0;
                state = FishingState.StartMove;
            }
            return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autoFish_start);
        }

        private int OnCommandStop(CmdResult r)
        {
            isFishing = false;
            lock (stateLock)
            {
                isFishing = false;
                if (state == FishingState.WaitingFishToBite)
                    UseFishRod();
                state = FishingState.Stopping;
            }
            StopFishing();
            return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autoFish_stop);
        }

        private int OnCommandStatus(CmdResult r)
        {
            if (fishItemCnt.Count == 0)
                return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autoFish_status_info);

            List<KeyValuePair<ItemType, uint>> orderedList = fishItemCnt.OrderBy(x => x.Value).ToList();
            int maxLen = orderedList[^1].Value.ToString().Length;
            StringBuilder sb = new();
            sb.Append(Translations.bot_autoFish_status_info);
            foreach ((ItemType type, uint cnt) in orderedList)
            {
                sb.Append(Environment.NewLine);

                string cntStr = cnt.ToString();
                sb.Append(' ', maxLen - cntStr.Length).Append(cntStr);
                sb.Append(" x ");
                sb.Append(Item.GetTypeString(type));
            }
            LogToConsole(sb.ToString());
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int OnCommandStatusClear(CmdResult r)
        {
            fishItemCnt = new();
            return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_autoFish_status_clear);
        }

        private void StartFishing()
        {
            isFishing = false;
            if (Config.Auto_Start)
            {
                double delay = Config.Fishing_Delay;
                LogToConsole(string.Format(Translations.bot_autoFish_start_at, delay));
                lock (stateLock)
                {
                    isFishing = false;
                    counter = Settings.DoubleToTick(delay);
                    state = FishingState.StartMove;
                }
            }
            else
            {
                lock (stateLock)
                {
                    state = FishingState.WaitJoinGame;
                }
            }
        }

        private void StopFishing()
        {
            isFishing = false;
            lock (stateLock)
            {
                isFishing = false;
                state = FishingState.Stopping;
            }
            fishItemCounter = 15;
        }

        private void UseFishRod()
        {
            if (Config.Mainhand)
                UseItemInHand();
            else
                UseItemInLeftHand();
        }

        public override void Update()
        {
            if (fishItemCounter < 15)
                ++fishItemCounter;

            lock (stateLock)
            {
                switch (state)
                {
                    case FishingState.WaitJoinGame:
                        break;
                    case FishingState.WaitingToCast:
                        if (AutoEat.Eating)
                            counter = Settings.DoubleToTick(Config.Cast_Delay);
                        else if (--counter < 0)
                            state = FishingState.CastingRod;
                        break;
                    case FishingState.CastingRod:
                        UseFishRod();
                        counter = 0;
                        state = FishingState.WaitingFishingBobber;
                        break;
                    case FishingState.WaitingFishingBobber:
                        if (++counter > castTimeout)
                        {
                            if (castTimeout < 6000)
                                castTimeout *= 2; // Exponential backoff
                            LogToConsole(GetShortTimestamp() + ": " + string.Format(Translations.bot_autoFish_cast_timeout, castTimeout / 10.0));

                            counter = Settings.DoubleToTick(Config.Cast_Delay);
                            state = FishingState.WaitingToCast;
                        }
                        break;
                    case FishingState.WaitingFishToBite:
                        if (++counter > Settings.DoubleToTick(Config.Fishing_Timeout))
                        {
                            LogToConsole(GetShortTimestamp() + ": " + Translations.bot_autoFish_fishing_timeout);

                            counter = Settings.DoubleToTick(Config.Cast_Delay);
                            state = FishingState.WaitingToCast;
                        }
                        break;
                    case FishingState.StartMove:
                        if (--counter < 0)
                        {
                            if (Config.Enable_Move && Config.Movements.Length > 0)
                            {
                                if (GetTerrainEnabled())
                                {
                                    UpdateLocation(Config.Movements);
                                    state = FishingState.WaitingMovement;
                                }
                                else
                                {
                                    LogToConsole(Translations.extra_terrainandmovement_required);
                                    state = FishingState.WaitJoinGame;
                                }
                            }
                            else
                            {
                                counter = Settings.DoubleToTick(Config.Cast_Delay);
                                state = FishingState.DurabilityCheck;
                                goto case FishingState.DurabilityCheck;
                            }
                        }
                        break;
                    case FishingState.WaitingMovement:
                        if (!ClientIsMoving())
                        {
                            LookAtLocation(nextYaw, nextPitch);
                            LogToConsole(string.Format(Translations.bot_autoFish_update_lookat, nextYaw, nextPitch));

                            state = FishingState.DurabilityCheck;
                            goto case FishingState.DurabilityCheck;
                        }
                        break;
                    case FishingState.DurabilityCheck:
                        if (DurabilityCheck())
                        {
                            counter = Settings.DoubleToTick(Config.Cast_Delay);
                            state = FishingState.WaitingToCast;
                        }
                        break;
                    case FishingState.Stopping:
                        break;
                }
            }
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (fishItemCounter < 15 && entity.Type == EntityType.Item && Math.Abs(entity.Location.Y - LastPos.Y) < 2.2 &&
                Math.Abs(entity.Location.X - LastPos.X) < 0.12 && Math.Abs(entity.Location.Z - LastPos.Z) < 0.12)
            {
                if (Config.Log_Fish_Bobber)
                    LogToConsole(string.Format("Item ({0}) spawn at {1}, distance = {2:0.00}", entity.ID, entity.Location, entity.Location.Distance(LastPos)));
                fishItem = entity;
            }
            else if (entity.Type == EntityType.FishingBobber && entity.ObjectData == GetPlayerEntityID())
            {
                if (Config.Log_Fish_Bobber)
                    LogToConsole(string.Format("FishingBobber spawn at {0}, distance = {1:0.00}", entity.Location, GetCurrentLocation().Distance(entity.Location)));

                fishItemCounter = 15;

                LogToConsole(GetShortTimestamp() + ": " + Translations.bot_autoFish_throw);
                lock (stateLock)
                {
                    fishingBobber = entity;
                    LastPos = entity.Location;
                    isFishing = true;

                    castTimeout = 24;
                    counter = 0;
                    state = FishingState.WaitingFishToBite;
                }
            }
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if (entity != null && fishingBobber != null && entity.Type == EntityType.FishingBobber && entity.ID == fishingBobber!.ID)
            {
                if (Config.Log_Fish_Bobber)
                    LogToConsole(string.Format("FishingBobber despawn at {0}", entity.Location));

                if (isFishing)
                {
                    isFishing = false;

                    if (Config.Antidespawn)
                    {
                        LogToConsole(Translations.bot_autoFish_despawn);

                        lock (stateLock)
                        {
                            counter = Settings.DoubleToTick(Config.Cast_Delay);
                            state = FishingState.WaitingToCast;
                        }
                    }
                }
            }
        }

        public override void OnEntityMove(Entity entity)
        {
            if (isFishing && entity != null && fishingBobber!.ID == entity.ID &&
                (state == FishingState.WaitingFishToBite || state == FishingState.WaitingFishingBobber))
            {
                Location Pos = entity.Location;
                double Dx = LastPos.X - Pos.X;
                double Dy = LastPos.Y - Pos.Y;
                double Dz = LastPos.Z - Pos.Z;
                LastPos = Pos;

                if (Config.Log_Fish_Bobber)
                    LogToConsole(string.Format("FishingBobber {0}  Dx={1:0.000000} Dy={2:0.000000} Dz={3:0.000000}", Pos, Dx, Math.Abs(Dy), Dz));

                if (Math.Abs(Dx) < Math.Abs(Config.Stationary_Threshold) &&
                    Math.Abs(Dz) < Math.Abs(Config.Stationary_Threshold) &&
                    Math.Abs(Dy) > Math.Abs(Config.Hook_Threshold))
                {
                    // prevent triggering multiple time
                    if ((DateTime.Now - CaughtTime).TotalSeconds > 1)
                    {
                        isFishing = false;
                        CaughtTime = DateTime.Now;
                        OnCaughtFish();
                    }
                }
            }
        }

        public override void OnEntityMetadata(Entity entity, Dictionary<int, object?> metadata)
        {
            if (fishItemCounter < 15 && entity.ID == fishItem.ID && metadata.TryGetValue(8, out object? itemObj))
            {
                fishItemCounter = 15;
                Item item = (Item)itemObj!;
                LogToConsole(string.Format(Translations.bot_autoFish_got, item.ToFullString()));
                if (fishItemCnt.ContainsKey(item.Type))
                    fishItemCnt[item.Type] += (uint)item.Count;
                else
                    fishItemCnt.Add(item.Type, (uint)item.Count);
            }
        }

        public override void AfterGameJoined()
        {
            StartFishing();
        }

        public override void OnRespawn()
        {
            StartFishing();
        }

        public override void OnDeath()
        {
            StopFishing();
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            StopFishing();

            fishingBobber = null;
            LastPos = Location.Zero;
            CaughtTime = DateTime.Now;

            return base.OnDisconnect(reason, message);
        }

        /// <summary>
        /// Called when detected a fish is caught
        /// </summary>
        public void OnCaughtFish()
        {
            ++fishCount;
            if (Config.Enable_Move && Config.Movements.Length > 0)
                LogToConsole(GetShortTimestamp() + ": " + string.Format(Translations.bot_autoFish_caught_at,
                    fishingBobber!.Location.X, fishingBobber!.Location.Y, fishingBobber!.Location.Z, fishCount));
            else
                LogToConsole(GetShortTimestamp() + ": " + string.Format(Translations.bot_autoFish_caught, fishCount));

            lock (stateLock)
            {
                counter = 0;
                state = FishingState.StartMove;

                fishItemCounter = 0;
                UseFishRod();
            }
        }

        private void UpdateLocation(LocationConfig[] locationList)
        {
            if (curLocationIdx >= locationList.Length)
            {
                curLocationIdx = Math.Max(0, locationList.Length - 2);
                moveDir = -1;
            }
            else if (curLocationIdx < 0)
            {
                curLocationIdx = Math.Min(locationList.Length - 1, 1);
                moveDir = 1;
            }

            LocationConfig curConfig = locationList[curLocationIdx];

            if (curConfig.facing != null)
                (nextYaw, nextPitch) = ((float)curConfig.facing.Value.yaw, (float)curConfig.facing.Value.pitch);
            else
                (nextYaw, nextPitch) = (GetYaw(), GetPitch());

            if (curConfig.XYZ != null)
            {
                Location current = GetCurrentLocation();
                Location goal = new(curConfig.XYZ.Value.x, curConfig.XYZ.Value.y, curConfig.XYZ.Value.z);

                bool isMoveSuccessed;
                if (!Movement.CheckChunkLoading(GetWorld(), current, goal))
                {
                    isMoveSuccessed = false;
                    LogToConsole(string.Format(Translations.cmd_move_chunk_not_loaded, goal.X, goal.Y, goal.Z));
                }
                else
                {
                    isMoveSuccessed = MoveToLocation(goal, allowUnsafe: false, allowDirectTeleport: false);
                }

                if (!isMoveSuccessed)
                {
                    (nextYaw, nextPitch) = (GetYaw(), GetPitch());
                    LogToConsole(string.Format(Translations.cmd_move_fail, goal));
                }
                else
                {
                    LogToConsole(string.Format(Translations.cmd_move_walk, goal, current));
                }
            }

            curLocationIdx += moveDir;
        }

        private bool DurabilityCheck()
        {
            if (!inventoryEnabled)
                return true;

            bool useMainHand = Config.Mainhand;
            Container container = GetPlayerInventory();

            int itemSolt = useMainHand ? GetCurrentSlot() + 36 : 45;

            if (container.Items.TryGetValue(itemSolt, out Item? handItem) &&
                handItem.Type == ItemType.FishingRod && (64 - handItem.Damage) >= Config.Durability_Limit)
            {
                isWaitingRod = false;
                return true;
            }
            else
            {
                if (!isWaitingRod)
                    LogToConsole(GetTimestamp() + ": " + Translations.bot_autoFish_no_rod);

                if (Config.Auto_Rod_Switch)
                {
                    foreach ((int slot, Item item) in container.Items)
                    {
                        if (item.Type == ItemType.FishingRod && (64 - item.Damage) >= Config.Durability_Limit)
                        {
                            WindowAction(0, slot, WindowActionType.LeftClick);
                            WindowAction(0, itemSolt, WindowActionType.LeftClick);
                            WindowAction(0, slot, WindowActionType.LeftClick);
                            LogToConsole(GetTimestamp() + ": " + string.Format(Translations.bot_autoFish_switch, slot, (64 - item.Damage)));
                            isWaitingRod = false;
                            return true;
                        }
                    }
                }

                isWaitingRod = true;
                return false;
            }
        }
    }
}
