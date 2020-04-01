using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
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

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                LogToConsole("Entity Handling is not enabled in the config file!");
                LogToConsole("This bot will be unloaded.");
                UnloadBot();
            }
            inventoryEnabled = GetInventoryEnabled();
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.TypeID == 102)
            {
                if (GetCurrentLocation().Distance(entity.Location) < 2 && !isFishing)
                {
                    LogToConsole("Threw a fishing rod");
                    fishingRod = entity;
                    LastPos = entity.Location;
                    isFishing = true;
                }
            }
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if(entity.TypeID == 102 && isFishing)
            {
                if(entity.ID == fishingRod.ID)
                {
                    isFishing = false;
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
        
        /// <summary>
        /// Called when detected a fish is caught
        /// </summary>
        public void OnCaughtFish()
        {
            LogToConsole(GetTimestamp() + ": Caught a fish!");
            // retract fishing rod
            UseItemOnHand();
            if (inventoryEnabled)
            {
                if (!hasFishingRod())
                {
                    LogToConsole(GetTimestamp() + ": No Fishing Rod on hand. Maybe broken?");
                    return;
                }
            }
            // non-blocking delay
            Task.Factory.StartNew(delegate
            {
                // retract fishing rod need some time
                Thread.Sleep(800);
                // throw again
                UseItemOnHand();
            });
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
