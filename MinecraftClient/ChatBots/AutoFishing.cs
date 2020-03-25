using System;
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
        private Dictionary<int, Entity> fishingRod = new Dictionary<int, Entity>();
        private Double fishingHookThreshold = 0.2;
        private Location LastPos = new Location();
        private DateTime CaughtTime = DateTime.Now;

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                ConsoleIO.WriteLine("[AutoFishing] Entity Handling is not enabled in the config file!");
                ConsoleIO.WriteLine("[AutoFishing] This bot will be unloaded.");
                UnloadBot();
            }
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.TypeID == 102)
            {
                ConsoleIO.WriteLine("Threw a fishing rod");
                fishingRod.Add(entity.ID, entity);
                LastPos = entity.Location;
            }
        }
        public override void OnEntityMove(Entity entity)
        {
            if (fishingRod.ContainsKey(entity.ID))
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
                fishingRod[entity.ID] = entity;
            }
        }
        
        /// <summary>
        /// Called when detected a fish is caught
        /// </summary>
        public void OnCaughtFish()
        {
            ConsoleIO.WriteLine("Caught a fish!");
            // retract fishing rod
            UseItemOnHand();
            if (!hasFishingRod())
            {
                ConsoleIO.WriteLine("No Fishing Rod on hand. Maybe broken?");
                return;
            }
            // non-blocking delay
            Task.Factory.StartNew(delegate
            {
                // retract fishing rod need some time
                Thread.Sleep(500);
                // throw again
                UseItemOnHand();
            });
        }

        public bool hasFishingRod()
        {
            int start = 36;
            int end = 44;
            Inventory.Container container = GetPlayerInventory();
            foreach(KeyValuePair<int,Inventory.Item> a in container.Items)
            {
                if (a.Key < start || a.Key > end) continue;
                if (a.Value.ID == 626) return true;
            }
            return false;
        }
    }
}
