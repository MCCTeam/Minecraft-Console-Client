using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    public static class EntityTypeExtensions
    {
        /// <summary>
        /// Return TRUE if the Entity is an hostile mob
        /// </summary>
        /// <remarks>New mobs added in newer Minecraft versions might be absent from the list</remarks>
        /// <returns>TRUE if hostile</returns>
        public static bool IsHostile(this EntityType e)
        {
            switch (e)
            {
                case EntityType.Blaze:
                case EntityType.CaveSpider:
                case EntityType.Creeper:
                case EntityType.Drowned:
                case EntityType.Enderman:
                case EntityType.Endermite:
                case EntityType.Evoker:
                case EntityType.Ghast:
                case EntityType.Guardian:
                case EntityType.Hoglin:
                case EntityType.Husk:
                case EntityType.Illusioner:
                case EntityType.MagmaCube:
                case EntityType.Phantom:
                case EntityType.Piglin:
                case EntityType.PiglinBrute:
                case EntityType.Pillager:
                case EntityType.Ravager:
                case EntityType.Shulker:
                case EntityType.Silverfish:
                case EntityType.Skeleton:
                case EntityType.Slime:
                case EntityType.Spider:
                case EntityType.Stray:
                case EntityType.Vex:
                case EntityType.Vindicator:
                case EntityType.Witch:
                case EntityType.WitherSkeleton:
                case EntityType.Zoglin:
                case EntityType.Zombie:
                case EntityType.ZombieVillager:
                case EntityType.ZombifiedPiglin:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates whether the entity type contains an inner item
        /// </summary>
        /// <returns>TRUE if item holder (Item Entity, ItemFrame...)</returns>
        public static bool ContainsItem(this EntityType e)
        {
            switch (e)
            {
                case EntityType.Item:
                case EntityType.ItemFrame:
                case EntityType.EyeOfEnder:
                case EntityType.Egg:
                case EntityType.EnderPearl:
                case EntityType.Potion:
                case EntityType.Fireball:
                case EntityType.FireworkRocket:
                    return true;
                default:
                    return false;
            };
        }
    }
}
