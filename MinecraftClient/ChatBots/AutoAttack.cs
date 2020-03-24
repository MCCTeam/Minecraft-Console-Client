using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
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
                ConsoleIO.WriteLine("[AutoAttack] Entity Handling is not enabled in the config file!");
                ConsoleIO.WriteLine("[AutoAttack] This bot will be unloaded.");
                UnloadBot();
            }
        }

        public override void Update()
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

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.GetMobName() != "")
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
                Double distance = Entity.CalculateDistance(GetCurrentLocation(), entity.Location);
                if(distance < attackRange)
                {
                    if(!entitiesToAttack.ContainsKey(entity.ID))
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
                    GetServerTPS();
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
