using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// The AutoAttack bot will automatically attack any hostile mob close to the player
    /// </summary>
    public class AutoAttack : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoAttack";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.AutoAttack.Mode$")]
            public AttackMode Mode = AttackMode.single;

            [TomlInlineComment("$ChatBot.AutoAttack.Priority$")]
            public PriorityType Priority = PriorityType.distance;

            [TomlInlineComment("$ChatBot.AutoAttack.Cooldown_Time$")]
            public CooldownConfig Cooldown_Time = new(false, 1.0);

            [TomlInlineComment("$ChatBot.AutoAttack.Interaction$")]
            public InteractType Interaction = InteractType.Attack;

            [TomlInlineComment("$ChatBot.AutoAttack.Attack_Range$")]
            public double Attack_Range = 4.0;

            [TomlInlineComment("$ChatBot.AutoAttack.Attack_Hostile$")]
            public bool Attack_Hostile = true;

            [TomlInlineComment("$ChatBot.AutoAttack.Attack_Passive$")]
            public bool Attack_Passive = false;

            [TomlInlineComment("$ChatBot.AutoAttack.List_Mode$")]
            public ListType List_Mode = ListType.whitelist;

            [TomlInlineComment("$ChatBot.AutoAttack.Entites_List$")]
            public List<EntityType> Entites_List = new() { EntityType.Zombie, EntityType.Cow };

            public void OnSettingUpdate()
            {
                if (Cooldown_Time.Custom && Cooldown_Time.value <= 0)
                {
                    LogToConsole(BotName, Translations.bot_autoAttack_invalidcooldown);
                    Cooldown_Time.value = 1.0;
                }

                if (Attack_Range < 1.0)
                    Attack_Range = 1.0;

                if (Attack_Range > 4.0)
                    Attack_Range = 4.0;
            }

            public enum AttackMode { single, multi };

            public enum PriorityType { distance, health };

            public enum ListType { blacklist, whitelist };

            public struct CooldownConfig
            {
                public bool Custom;
                public double value;

                public CooldownConfig()
                {
                    Custom = false;
                    value = 0;
                }

                public CooldownConfig(double value)
                {
                    Custom = true;
                    this.value = value;
                }

                public CooldownConfig(bool Override, double value)
                {
                    this.Custom = Override;
                    this.value = value;
                }
            }
        }

        private readonly Dictionary<int, Entity> entitiesToAttack = new(); // mobs within attack range
        private int attackCooldown = 6;
        private int attackCooldownCounter = 6;
        private Double attackSpeed = 4;
        private Double attackCooldownSeconds;
        private readonly bool overrideAttackSpeed = false;
        private readonly double attackRange = 4.0;
        private Double serverTPS;
        private float health = 100;
        private readonly bool attackHostile = true;
        private readonly bool attackPassive = false;

        public AutoAttack()
        {
            overrideAttackSpeed = Config.Cooldown_Time.Custom;
            if (Config.Cooldown_Time.Custom)
            {
                attackCooldownSeconds = Config.Cooldown_Time.value;
                attackCooldown = Convert.ToInt32(Math.Truncate(attackCooldownSeconds / 0.1) + 1);
            }

            attackHostile = Config.Attack_Hostile;
            attackPassive = Config.Attack_Passive;
            attackRange = Config.Attack_Range;
        }

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                LogToConsole(Translations.extra_entity_required);
                LogToConsole(Translations.general_bot_unload);
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
                    if (Config.Mode == Configs.AttackMode.single)
                    {
                        int priorityEntity = 0;
                        if (Config.Priority == Configs.PriorityType.distance) // closest distance priority
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
                                InteractEntity(priorityEntity, Config.Interaction); // hit the entity!
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
                                InteractEntity(entity.Key, Config.Interaction); // hit the entity!
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

            if (Config.Entites_List.Count > 0)
            {
                bool inList = Config.Entites_List.Contains(entity.Type);
                if (Config.List_Mode == Configs.ListType.blacklist)
                    result = !inList && result;
                else
                    result = inList;
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
