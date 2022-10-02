using System;
using System.Collections.Generic;
using System.IO;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// The AutoAttack bot will automatically attack any hostile mob close to the player
    /// </summary>
    class AutoAttack : ChatBot
    {
        private readonly Dictionary<int, Entity> entitiesToAttack = new(); // mobs within attack range
        private int attackCooldown = 6;
        private int attackCooldownCounter = 6;
        private Double attackSpeed = 4;
        private Double attackCooldownSeconds;
        private readonly bool overrideAttackSpeed = false;
        private readonly int attackRange = 4;
        private Double serverTPS;
        private float health = 100;
        private readonly bool singleMode = true;
        private readonly bool priorityDistance = true;
        private readonly InteractType interactMode;
        private readonly bool attackHostile = true;
        private readonly bool attackPassive = false;
        private readonly string listMode = "blacklist";
        private readonly List<EntityType> listedEntites = new();

        public AutoAttack(
            string mode, string priority, bool overrideAttackSpeed = false, double cooldownSeconds = 1, InteractType interaction = InteractType.Attack)
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

            interactMode = interaction;

            if (overrideAttackSpeed)
            {
                if (cooldownSeconds <= 0)
                {
                    LogToConsoleTranslated("bot.autoAttack.invalidcooldown");
                }
                else
                {
                    this.overrideAttackSpeed = overrideAttackSpeed;
                    attackCooldownSeconds = cooldownSeconds;
                    attackCooldown = Convert.ToInt32(Math.Truncate(attackCooldownSeconds / 0.1) + 1);
                }
            }

            attackHostile = Settings.AutoAttack_Attack_Hostile;
            attackPassive = Settings.AutoAttack_Attack_Passive;

            if (Settings.AutoAttack_ListMode.Length > 0)
            {
                listMode = Settings.AutoAttack_ListMode.ToLower();

                if (!(listMode.Equals("whitelist", StringComparison.OrdinalIgnoreCase) || listMode.Equals("blacklist", StringComparison.OrdinalIgnoreCase)))
                {
                    LogToConsole(Translations.TryGet("bot.autoAttack.invalidlist"));
                    listMode = "blacklist";
                }
            }
            else LogToConsole(Translations.TryGet("bot.autoAttack.invalidlist"));

            if (File.Exists(Settings.AutoAttack_ListFile))
            {
                string[] entityList = LoadDistinctEntriesFromFile(Settings.AutoAttack_ListFile);

                if (entityList.Length > 0)
                {
                    foreach (var item in entityList)
                    {
                        if (Enum.TryParse(item, true, out EntityType resultingType))
                            listedEntites.Add(resultingType);
                    }
                }
            }
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

                        if (entitiesToAttack.ContainsKey(priorityEntity))
                        {
                            // check entity distance and health again
                            if (ShouldAttackEntity(entitiesToAttack[priorityEntity]))
                            {
                                InteractEntity(priorityEntity, interactMode); // hit the entity!
                                SendAnimation(Inventory.Hand.MainHand); // Arm animation
                            }
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<int, Entity> entity in entitiesToAttack)
                        {
                            // check that we are in range once again.
                            if (ShouldAttackEntity(entity.Value))
                            {
                                InteractEntity(entity.Key, interactMode); // hit the entity!
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
            ShouldAttackEntity(entity);
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if (entitiesToAttack.ContainsKey(entity.ID))
            {
                entitiesToAttack.Remove(entity.ID);
            }
        }

        public override void OnEntityHealth(Entity entity, float health)
        {
            if (!IsAllowedToAttack(entity))
                return;

            if (entitiesToAttack.ContainsKey(entity.ID))
            {
                entitiesToAttack[entity.ID].Health = health;

                if (entitiesToAttack[entity.ID].Health <= 0)
                {
                    entitiesToAttack.Remove(entity.ID);
                }
            }
        }

        private bool IsAllowedToAttack(Entity entity)
        {
            bool result = false;

            if (attackHostile && entity.Type.IsHostile())
                result = true;

            if (attackPassive && entity.Type.IsPassive())
                result = true;

            if (listedEntites.Count > 0)
            {
                bool inList = listedEntites.Contains(entity.Type);
                result = listMode.Equals("blacklist") ? (!inList && result) : (inList);
            }

            return result;
        }

        public override void OnEntityMove(Entity entity)
        {
            ShouldAttackEntity(entity);
        }

        public override void OnHealthUpdate(float health, int food)
        {
            this.health = health;
        }

        public override void OnPlayerProperty(Dictionary<string, double> prop)
        {
            if (overrideAttackSpeed)
                return;
            foreach (var attackSpeedKey in new[] { "generic.attackSpeed", "minecraft:generic.attack_speed" })
            {
                // adjust auto attack cooldown for maximum attack damage
                if (prop.ContainsKey(attackSpeedKey))
                {
                    if (attackSpeed != prop[attackSpeedKey])
                    {
                        serverTPS = GetServerTPS();
                        attackSpeed = prop[attackSpeedKey];
                        attackCooldownSeconds = 1 / attackSpeed * (serverTPS / 20.0); // server tps will affect the cooldown
                        attackCooldown = Convert.ToInt32(Math.Truncate(attackCooldownSeconds / 0.1) + 1);
                    }
                }
            }
        }

        public override void OnServerTpsUpdate(double tps)
        {
            if (overrideAttackSpeed)
                return;

            serverTPS = tps;
            // re-calculate attack speed
            attackCooldownSeconds = 1 / attackSpeed * (serverTPS / 20.0); // server tps will affect the cooldown
            attackCooldown = Convert.ToInt32(Math.Truncate(attackCooldownSeconds / 0.1) + 1);
        }

        /// <summary>
        /// Checks to see if the entity should be attacked. If it should be attacked, it will add the entity
        /// To a list of entities to attack every few ticks.
        /// </summary>
        /// <param name="entity">The entity to handle</param>
        /// <returns>If the entity should be attacked</returns>
        public bool ShouldAttackEntity(Entity entity)
        {
            if (!IsAllowedToAttack(entity) || entity.Health <= 0)
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
