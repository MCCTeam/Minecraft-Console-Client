using System;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// The AutoFishing bot semi-automates fishing.
    /// The player needs to have a fishing rod in hand, then manually send it using the UseItem command.
    /// </summary>
    class AutoFishing : ChatBot
    {
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
            if (Settings.AutoFishing_AutoStart)
            {
                double delay = Settings.AutoFishing_FishingDelay;
                LogToConsole(Translations.Get("bot.autoFish.start", delay));
                lock (stateLock)
                {
                    counter = (int)(delay * 10);
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
            if (Settings.AutoFishing_Mainhand)
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
                            counter = (int)(Settings.AutoFishing_CastDelay * 10);
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

                            counter = (int)(Settings.AutoFishing_CastDelay * 10);
                            state = FishingState.WaitingToCast;
                        }
                        break;
                    case FishingState.WaitingFishToBite:
                        if (++counter > (int)(Settings.AutoFishing_FishingTimeout * 10))
                        {
                            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.fishing_timeout"));

                            counter = (int)(Settings.AutoFishing_CastDelay * 10);
                            state = FishingState.WaitingToCast;
                        }
                        break;
                    case FishingState.StartMove:
                        if (--counter < 0)
                        {
                            double[,]? locationList = Settings.AutoFishing_Location;
                            if (locationList != null)
                            {
                                if (GetTerrainEnabled())
                                {
                                    UpdateLocation(locationList);
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
                                counter = (int)(Settings.AutoFishing_CastDelay * 10);
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
                            counter = (int)(Settings.AutoFishing_CastDelay * 10);
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
                if (Settings.AutoFishing_LogFishingBobber)
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
                if (Settings.AutoFishing_LogFishingBobber)
                    LogToConsole(string.Format("FishingBobber despawn at {0}", entity.Location));

                if (isFishing)
                {
                    isFishing = false;

                    if (Settings.AutoFishing_Antidespawn)
                    {
                        LogToConsoleTranslated("bot.autoFish.despawn");

                        lock (stateLock)
                        {
                            counter = (int)(Settings.AutoFishing_CastDelay * 10);
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

                if (Settings.AutoFishing_LogFishingBobber)
                    LogToConsole(string.Format("FishingBobber {0}  Dx={1:0.000000} Dy={2:0.000000} Dz={3:0.000000}", Pos, Dx, Math.Abs(Dy), Dz));

                if (Math.Abs(Dx) < Math.Abs(Settings.AutoFishing_StationaryThreshold) &&
                    Math.Abs(Dz) < Math.Abs(Settings.AutoFishing_StationaryThreshold) &&
                    Math.Abs(Dy) > Math.Abs(Settings.AutoFishing_HookThreshold))
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
            if (Settings.AutoFishing_Location != null)
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

        private void UpdateLocation(double[,] locationList)
        {
            if (curLocationIdx >= locationList.GetLength(0))
            {
                curLocationIdx = Math.Max(0, locationList.GetLength(0) - 2);
                moveDir = -1;
            }
            else if (curLocationIdx < 0)
            {
                curLocationIdx = Math.Min(locationList.GetLength(0) - 1, 1);
                moveDir = 1;
            }

            int locationType = locationList.GetLength(1);

            if (locationType == 2)
            {
                nextYaw = (float)locationList[curLocationIdx, 0];
                nextPitch = (float)locationList[curLocationIdx, 1];
            }
            else if (locationType == 3)
            {
                nextYaw = GetYaw();
                nextPitch = GetPitch();
            }
            else if (locationType == 5)
            {
                nextYaw = (float)locationList[curLocationIdx, 3];
                nextPitch = (float)locationList[curLocationIdx, 4];
            }

            if (locationType == 3 || locationType == 5)
            {
                Location current = GetCurrentLocation();
                Location goal = new(locationList[curLocationIdx, 0], locationList[curLocationIdx, 1], locationList[curLocationIdx, 2]);

                bool isMoveSuccessed;
                if (!Movement.CheckChunkLoading(GetWorld(), current, goal))
                {
                    LogToConsole(Translations.Get("cmd.move.chunk_not_loaded", goal.X, goal.Y, goal.Z));
                    isMoveSuccessed = false;
                }
                else
                {
                    isMoveSuccessed = MoveToLocation(goal, allowUnsafe: false, allowDirectTeleport: false);
                }

                if (!isMoveSuccessed)
                {
                    nextYaw = GetYaw();
                    nextPitch = GetPitch();
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

            bool useMainHand = Settings.AutoFishing_Mainhand;
            Container container = GetPlayerInventory();

            int itemSolt = useMainHand ? GetCurrentSlot() + 36 : 45;

            if (container.Items.TryGetValue(itemSolt, out Item? handItem) &&
                handItem.Type == ItemType.FishingRod && (64 - handItem.Damage) >= Settings.AutoFishing_DurabilityLimit)
            {
                isWaitingRod = false;
                return true;
            }
            else
            {
                if (!isWaitingRod)
                    LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.no_rod"));

                if (Settings.AutoFishing_AutoRodSwitch)
                {
                    foreach ((int slot, Item item) in container.Items)
                    {
                        if (item.Type == ItemType.FishingRod && (64 - item.Damage) >= Settings.AutoFishing_DurabilityLimit)
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
