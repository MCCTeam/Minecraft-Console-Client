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
        private Entity fishingRod;
        private Double fishingHookThreshold = 0.2;
        private Location LastPos = new Location();
        private DateTime CaughtTime = DateTime.Now;
        private bool inventoryEnabled;
        private bool isFishing = false;
        private int useItemCounter = 0;

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

        public override void Update()
        {
            if (useItemCounter > 0)
            {
                useItemCounter--;
                if (useItemCounter <= 0)
                {
                    UseItemInHand();
                }
            }
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.Type == EntityType.FishingBobber)
            {
                if (GetCurrentLocation().Distance(entity.Location) < 2 && !isFishing)
                {
                    LogToConsoleTranslated("bot.autoFish.throw");
                    fishingRod = entity;
                    LastPos = entity.Location;
                    isFishing = true;
                }
            }
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if (entity.Type == EntityType.FishingBobber)
            {
                if(entity.ID == fishingRod.ID)
                {
                    isFishing = false;
                    if (Settings.AutoFishing_Antidespawn)
                    {
                        useItemCounter = 5; // 500ms
                    }
                }
            }
        }

        public override void OnEntityMove(Entity entity)
        {
            if (isFishing)
            {
                if (fishingRod.ID == entity.ID)
                {
                    Location Pos = entity.Location;
                    Double Dx = LastPos.X - Pos.X;
                    Double Dy = LastPos.Y - Pos.Y;
                    Double Dz = LastPos.Z - Pos.Z;
                    LastPos = Pos;
                    // check if fishing hook is stationary
                    if (Dx == 0 && Dz == 0)
                    {
                        if (Math.Abs(Dy) > fishingHookThreshold)
                        {
                            // caught
                            // prevent triggering multiple time
                            if ((DateTime.Now - CaughtTime).TotalSeconds > 1)
                            {
                                OnCaughtFish();
                                CaughtTime = DateTime.Now;
                            }
                        }
                    }
                    fishingRod = entity;
                }
            }
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            fishingRod = null;
            LastPos = new Location();
            CaughtTime = DateTime.Now;
            isFishing = false;
            useItemCounter = 0;
            return base.OnDisconnect(reason, message);
        }

        /// <summary>
        /// Called when detected a fish is caught
        /// </summary>
        public void OnCaughtFish()
        {
            LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.caught"));
            // retract fishing rod
            UseItemInHand();
            if (inventoryEnabled)
            {
                if (!hasFishingRod())
                {
                    LogToConsole(GetTimestamp() + ": " + Translations.Get("bot.autoFish.no_rod"));
                    return;
                }
            }
            // thread-safe
            useItemCounter = 8; // 800ms
        }

        /// <summary>
        /// Check whether the player has a fishing rod in inventory
        /// </summary>
        /// <returns>TRUE if the player has a fishing rod</returns>
        public bool hasFishingRod()
        {
            if (!inventoryEnabled)
                return false;
            int start = 36;
            int end = 44;
            Inventory.Container container = GetPlayerInventory();

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
