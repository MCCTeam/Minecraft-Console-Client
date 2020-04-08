using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// The AutoAttack bot will automatically attack any hostile mob close to the player
    /// </summary>
    class AutoAttack : ChatBot
    {
        private Dictionary<int, Entity> entitiesToAttack = new Dictionary<int, Entity>(); // mobs within attack range
        private Dictionary<int, Entity> entitiesToTrack = new Dictionary<int, Entity>(); // all mobs in view distance
        private int attackCooldown = 6;
        private int attackCooldownCounter = 6;
        private Double attackSpeed = 4;
        private Double attackCooldownSecond;
        private int attackRange = 4;
        private Double serverTPS;

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                LogToConsole("Entity Handling is not enabled in the config file!");
                LogToConsole("This bot will be unloaded.");
                UnloadBot();
            }
        }

        public override void Update()
        {
            if (!AutoEat.Eating)
            {
                if (attackCooldownCounter == 0)
                {
                    attackCooldownCounter = attackCooldown;
                    if (entitiesToAttack.Count > 0)
                    {
                        foreach (KeyValuePair<int, Entity> a in entitiesToAttack)
                        {
                            InteractEntity(a.Key, 1);
                        }
                    }
                }
                else
                {
                    attackCooldownCounter--;
                }
            }
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.IsHostile())
            {
                entitiesToTrack.Add(entity.ID, entity);
            }
        }
        public override void OnEntityDespawn(Entity entity)
        {
            if (entitiesToTrack.ContainsKey(entity.ID))
            {
                entitiesToTrack.Remove(entity.ID);
            }
        }
        public override void OnEntityMove(Entity entity)
        {
            if (entitiesToTrack.ContainsKey(entity.ID))
            {
                if (GetCurrentLocation().Distance(entity.Location) < attackRange)
                {
                    if (!entitiesToAttack.ContainsKey(entity.ID))
                        entitiesToAttack.Add(entity.ID, entity);
                }
            }
        }

        public override void OnPlayerProperty(Dictionary<string, double> prop)
        {
            // adjust auto attack cooldown for maximum attack damage
            if (prop.ContainsKey("generic.attackSpeed"))
            {
                if (attackSpeed != prop["generic.attackSpeed"])
                {
                    serverTPS = GetServerTPS();
                    attackSpeed = prop["generic.attackSpeed"];
                    attackCooldownSecond = 1 / attackSpeed * (serverTPS / 20.0); // server tps will affect the cooldown
                    attackCooldown = Convert.ToInt32(Math.Truncate(attackCooldownSecond / 0.1) + 1);
                }
            }
        }

        public override void OnServerTpsUpdate(double tps)
        {
            serverTPS = tps;
            // re-calculate attack speed
            attackCooldownSecond = 1 / attackSpeed * (serverTPS / 20.0); // server tps will affect the cooldown
            attackCooldown = Convert.ToInt32(Math.Truncate(attackCooldownSecond / 0.1) + 1);
        }
    }
}
