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
        private int attackCooldown = 6;
        private int attackCooldownCounter = 6;
        private Double attackSpeed = 4;
        private Double attackCooldownSecond;
        private int attackRange = 4;
        private Double serverTPS;
        private float health = 100;

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
            if (AutoEat.Eating | health == 0)
                return;

            if (attackCooldownCounter == 0)
            {
                attackCooldownCounter = attackCooldown;
                if (entitiesToAttack.Count > 0)
                {
                    foreach (KeyValuePair<int, Entity> entity in entitiesToAttack)
                    {
                        // check that we are in range once again.
                        bool shouldAttack = handleEntity(entity.Value);
                        if (shouldAttack)
                        {
                            // hit the entity!
                            InteractEntity(entity.Key, 1);
                        }
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
            handleEntity(entity);
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if (entitiesToAttack.ContainsKey(entity.ID))
            {
                entitiesToAttack.Remove(entity.ID);
            }
        }

        public override void OnEntityMove(Entity entity)
        {
            handleEntity(entity);
        }

        public override void OnHealthUpdate(float health, int food)
        {
            this.health = health;
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

        /// <summary>
        /// Checks to see if the entity should be attacked. If it should be attacked, it will add the entity
        /// To a list of entities to attack every few ticks.
        /// </summary>
        /// <param name="entity">The entity to handle</param>
        /// <returns>If the entity should be attacked</returns>
        public bool handleEntity(Entity entity)
        {
            if (!entity.Type.IsHostile())
                return false;

            bool isBeingAttacked = entitiesToAttack.ContainsKey(entity.ID);
            if (GetCurrentLocation().Distance(entity.Location) < attackRange)
            {
                // check to see if entity has not been marked as tracked, and if not, track it.
                if (!isBeingAttacked)
                {
                    entitiesToAttack.Add(entity.ID, entity);
                }

                return true;
            }
            else
            {
                // remove marker on entity to attack it, as it is now out of range
                if (isBeingAttacked)
                {
                    entitiesToAttack.Remove(entity.ID);
                }

                return false;
            }
        }
    }
}
