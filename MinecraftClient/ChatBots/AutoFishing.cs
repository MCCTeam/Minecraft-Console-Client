using System;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
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
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoFishing";

            public bool Enabled = false;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Antidespawn$")]
            public bool Antidespawn = false;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Mainhand$")]
            public bool Mainhand = true;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Auto_Start$")]
            public bool Auto_Start = true;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Cast_Delay$")]
            public double Cast_Delay = 0.4;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Fishing_Delay$")]
            public double Fishing_Delay = 3.0;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Fishing_Timeout$")]
            public double Fishing_Timeout = 300.0;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Durability_Limit$")]
            public double Durability_Limit = 2;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Auto_Rod_Switch$")]
            public bool Auto_Rod_Switch = true;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Stationary_Threshold$")]
            public double Stationary_Threshold = 0.001;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Hook_Threshold$")]
            public double Hook_Threshold = 0.2;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Log_Fish_Bobber$")]
            public bool Log_Fish_Bobber = false;

            [TomlInlineComment("$config.ChatBot.AutoFishing.Enable_Move$")]
            public bool Enable_Move = false;

            [TomlPrecedingComment("$config.ChatBot.AutoFishing.Movements$")]
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
                LogToConsoleTranslated("extra.entity_required");
                state = FishingState.WaitJoinGame;
            }
            inventoryEnabled = GetInventoryEnabled();
            if (!inventoryEnabled)
                LogToConsoleTranslated("bot.autoFish.no_inv_handle");
        }

        private void StartFishing()
        {
            isFishing = false;
            if (Config.Auto_Start)
            {
                double delay = Config.Fishing_Delay;
                LogToConsole(Translations.Get("bot.autoFish.start", delay));
                lock (stateLock)
                {
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
                state = FishingState.Stopping;
            }
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
                            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.cast_timeout", castTimeout / 10.0));

                            counter = Settings.DoubleToTick(Config.Cast_Delay);
                            state = FishingState.WaitingToCast;
                        }
                        break;
                    case FishingState.WaitingFishToBite:
                        if (++counter > Settings.DoubleToTick(Config.Fishing_Timeout))
                        {
                            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.fishing_timeout"));

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
                                    LogToConsole(Translations.Get("extra.terrainandmovement_required"));
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
                            LogToConsole(Translations.Get("bot.autoFish.update_lookat", nextYaw, nextPitch));

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
            if (entity.Type == EntityType.FishingBobber && entity.ObjectData == GetPlayerEntityID())
            {
                if (Config.Log_Fish_Bobber)
                    LogToConsole(string.Format("FishingBobber spawn at {0}, distance = {1:0.00}", entity.Location, GetCurrentLocation().Distance(entity.Location)));

                LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.throw"));
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
            if (entity != null && entity.Type == EntityType.FishingBobber && entity.ID == fishingBobber!.ID)
            {
                if (Config.Log_Fish_Bobber)
                    LogToConsole(string.Format("FishingBobber despawn at {0}", entity.Location));

                if (isFishing)
                {
                    isFishing = false;

                    if (Config.Antidespawn)
                    {
                        LogToConsoleTranslated("bot.autoFish.despawn");

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
            if (isFishing && entity != null && fishingBobber!.ID == entity.ID)
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
                LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.caught_at",
                    fishingBobber!.Location.X, fishingBobber!.Location.Y, fishingBobber!.Location.Z, fishCount));
            else
                LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.caught", fishCount));

            lock (stateLock)
            {
                UseFishRod();

                counter = 0;
                state = FishingState.StartMove;
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
                    LogToConsole(Translations.Get("cmd.move.chunk_not_loaded", goal.X, goal.Y, goal.Z));
                }
                else
                {
                    isMoveSuccessed = MoveToLocation(goal, allowUnsafe: false, allowDirectTeleport: false);
                }

                if (!isMoveSuccessed)
                {
                    (nextYaw, nextPitch) = (GetYaw(), GetPitch());
                    LogToConsole(Translations.Get("cmd.move.fail", goal));
                }
                else
                {
                    LogToConsole(Translations.Get("cmd.move.walk", goal, current));
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
                    LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.no_rod"));

                if (Config.Auto_Rod_Switch)
                {
                    foreach ((int slot, Item item) in container.Items)
                    {
                        if (item.Type == ItemType.FishingRod && (64 - item.Damage) >= Config.Durability_Limit)
                        {
                            WindowAction(0, slot, WindowActionType.LeftClick);
                            WindowAction(0, itemSolt, WindowActionType.LeftClick);
                            WindowAction(0, slot, WindowActionType.LeftClick);
                            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.switch", slot, (64 - item.Damage)));
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
