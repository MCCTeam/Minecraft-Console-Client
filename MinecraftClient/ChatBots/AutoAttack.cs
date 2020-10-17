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
        private bool singleMode = true;
        private bool priorityDistance = true;

        public AutoAttack(string mode, string priority)
        {
            if (mode == "single")
                singleMode = true;
            else if (mode == "multi")
                singleMode = false;
            else LogToConsoleTranslated("bot.autoAttack.mode", mode);

            if (priority == "distance")
                priorityDistance = true;
            else if (priority == "health")
                priorityDistance = false;
            else LogToConsoleTranslated("bot.autoAttack.priority", priority);
        }

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                LogToConsoleTranslated("extra.entity_required");
                LogToConsoleTranslated("general.bot_unload");
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
                    if (singleMode)
                    {
                        int priorityEntity = 0;
                        if (priorityDistance) // closest distance priority
                        {
                            double distance = 5;
                            foreach (var entity in entitiesToAttack)
                            {
                                var tmp = GetCurrentLocation().Distance(entity.Value.Location);
                                if (tmp < distance)
                                {
                                    priorityEntity = entity.Key;
                                    distance = tmp;
                                }
                            }
                        }
                        else // low health priority
                        {
                            float entityHealth = int.MaxValue;
                            foreach (var entity in entitiesToAttack)
                            {
                                if (entity.Value.Health < entityHealth)
                                {
                                    priorityEntity = entity.Key;
                                    entityHealth = entity.Value.Health;
                                }
                            }
                        }
                        // check entity distance and health again
                        if (shouldAttackEntity(entitiesToAttack[priorityEntity]))
                        {
                            InteractEntity(priorityEntity, 1); // hit the entity!
                            SendAnimation(Inventory.Hand.MainHand); // Arm animation
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<int, Entity> entity in entitiesToAttack)
                        {
                            // check that we are in range once again.
                            if (shouldAttackEntity(entity.Value))
                            {
                                InteractEntity(entity.Key, 1); // hit the entity!
                            }
                        }
                        SendAnimation(Inventory.Hand.MainHand); // Arm animation
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
            shouldAttackEntity(entity);
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
            shouldAttackEntity(entity);
        }

        public override void OnHealthUpdate(float health, int food)
        {
            this.health = health;
        }

        public override void OnPlayerProperty(Dictionary<string, double> prop)
        {
            foreach (var attackSpeedKey in new[] { "generic.attackSpeed", "minecraft:generic.attack_speed" })
            {
                // adjust auto attack cooldown for maximum attack damage
                if (prop.ContainsKey(attackSpeedKey))
                {
                    if (attackSpeed != prop[attackSpeedKey])
                    {
                        serverTPS = GetServerTPS();
                        attackSpeed = prop[attackSpeedKey];
                        attackCooldownSecond = 1 / attackSpeed * (serverTPS / 20.0); // server tps will affect the cooldown
                        attackCooldown = Convert.ToInt32(Math.Truncate(attackCooldownSecond / 0.1) + 1);
                    }
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
        public bool shouldAttackEntity(Entity entity)
        {
            if (!entity.Type.IsHostile() || entity.Health <= 0)
                return false;

            bool isBeingAttacked = entitiesToAttack.ContainsKey(entity.ID);
            if (GetCurrentLocation().Distance(entity.Location) <= attackRange)
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
