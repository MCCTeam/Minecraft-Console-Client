using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;

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

        private bool isFishing = false;
        private Entity? fishingBobber;
        private Location LastPos = Location.Zero;
        private DateTime CaughtTime = DateTime.Now;

        private int counter = 0;
        private readonly object stateLock = new();
        private FishingState state = FishingState.WaitJoinGame;

        private int castTimeout = 12;

        private enum FishingState
        {
            WaitJoinGame,
            WaitingToCast,
            CastingRod,
            WaitingFishingBobber,
            WaitingFishBite,
            Preparing,
            Stopping,
        }

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                LogToConsoleTranslated("extra.entity_required");
                LogToConsoleTranslated("general.bot_unload");
                UnloadBot();
            }
            inventoryEnabled = GetInventoryEnabled();
        }

        public override void AfterGameJoined()
        {
            double delay = Settings.AutoFishing_FishingDelay;
            LogToConsole(Translations.Get("bot.autoFish.start", delay));
            lock (stateLock)
            {
                counter = (int)(delay * 10);
                state = FishingState.WaitingToCast;
            }
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
                        if (--counter < 0)
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
                                castTimeout *= 2;
                            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.cast_timeout", castTimeout / 10.0));

                            counter = (int)(Settings.AutoFishing_FishingCastDelay * 10);
                            state = FishingState.WaitingToCast;
                        }
                        break;
                    case FishingState.WaitingFishBite:
                        if (++counter > (int)(Settings.AutoFishing_FishingTimeout * 10))
                        {
                            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.fishing_timeout"));

                            counter = (int)(Settings.AutoFishing_FishingCastDelay * 10);
                            state = FishingState.WaitingToCast;
                        }
                        break;
                    case FishingState.Preparing:
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
                LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.throw"));
                lock (stateLock)
                {
                    fishingBobber = entity;
                    LastPos = entity.Location;
                    isFishing = true;

                    castTimeout = 24;
                    counter = 0;
                    state = FishingState.WaitingFishBite;
                }
            }
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if (isFishing && entity.Type == EntityType.FishingBobber && entity.ID == fishingBobber!.ID)
            {
                isFishing = false;

                if (Settings.AutoFishing_Antidespawn)
                {
                    LogToConsoleTranslated("bot.autoFish.despawn");

                    lock (stateLock)
                    {
                        counter = (int)(Settings.AutoFishing_FishingCastDelay * 10);
                        state = FishingState.WaitingToCast;
                    }
                }
            }
        }

        public override void OnEntityMove(Entity entity)
        {
            if (isFishing && fishingBobber!.ID == entity.ID)
            {
                Location Pos = entity.Location;
                double Dx = LastPos.X - Pos.X;
                double Dy = LastPos.Y - Pos.Y;
                double Dz = LastPos.Z - Pos.Z;
                LastPos = Pos;

                // check if fishing hook is stationary
                if (Dx == 0 && Dz == 0)
                {
                    if (Math.Abs(Dy) > Settings.AutoFishing_FishingHookThreshold)
                    {
                        // caught
                        // prevent triggering multiple time
                        if ((DateTime.Now - CaughtTime).TotalSeconds > 1)
                        {
                            CaughtTime = DateTime.Now;
                            OnCaughtFish();
                        }
                    }
                }
            }
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            lock (stateLock)
            {
                isFishing = false;

                counter = 0;
                state = FishingState.Stopping;
            }

            fishingBobber = null;
            LastPos = Location.Zero;
            CaughtTime = DateTime.Now;

            return base.OnDisconnect(reason, message);
        }

        private void UseFishRod()
        {
            UseItemInHand();
        }

        /// <summary>
        /// Called when detected a fish is caught
        /// </summary>
        public void OnCaughtFish()
        {
            lock (stateLock)
            {
                state = FishingState.Preparing;
            }

            UseFishRod();

            ++fishCount;
            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.caught", fishCount));

            if (inventoryEnabled)
            {
                if (!HasFishingRod())
                {
                    LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.no_rod"));
                    return;
                }
            }

            lock (stateLock)
            {
                counter = (int)(Settings.AutoFishing_FishingCastDelay * 10);
                state = FishingState.WaitingToCast;
            }
        }

        /// <summary>
        /// Check whether the player has a fishing rod in inventory
        /// </summary>
        /// <returns>TRUE if the player has a fishing rod</returns>
        public bool HasFishingRod()
        {
            if (!inventoryEnabled)
                return false;
            int start = 36;
            int end = 44;
            Container container = GetPlayerInventory();

            foreach (KeyValuePair<int, Item> a in container.Items)
            {
                if (a.Key < start || a.Key > end)
                    continue;

                if (a.Value.Type == ItemType.FishingRod)
                    return true;
            }

            return false;
        }
    }
}
