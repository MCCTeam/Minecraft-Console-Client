﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
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
                ConsoleIO.WriteLine("[AutoFishing] Entity Handling is not enabled in the config file!");
                ConsoleIO.WriteLine("[AutoFishing] This bot will be unloaded.");
                UnloadBot();
            }
            inventoryEnabled = GetInventoryEnabled();
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.TypeID == 102)
            {
                if (Entity.CalculateDistance(GetCurrentLocation(), entity.Location) < 2 && !isFishing)
                {
                    ConsoleIO.WriteLine("Threw a fishing rod");
                    fishingRod = entity;
                    LastPos = entity.Location;
                    isFishing = true;
                }
            }
        }
        public override void OnEntityDespawn(Entity entity)
        {
            if(entity.TypeID == 102)
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
            ConsoleIO.WriteLine(GetTimestamp()+": Caught a fish!");
            // retract fishing rod
            UseItemOnHand();
            if (inventoryEnabled)
            {
                if (!hasFishingRod())
                {
                    ConsoleIO.WriteLine(GetTimestamp() + ": No Fishing Rod on hand. Maybe broken?");
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

        public bool hasFishingRod()
        {
            if (!inventoryEnabled) return false;
            int start = 36;
            int end = 44;
            Inventory.Container container = GetPlayerInventory();
            foreach(KeyValuePair<int,Inventory.Item> a in container.Items)
            {
                if (a.Key < start || a.Key > end) continue;
                if (a.Value.ID == 622) return true;
            }
            return false;
        }
    }
}
