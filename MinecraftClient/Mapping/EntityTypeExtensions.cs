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
                case EntityType.Creeper:
                case EntityType.Drowned:
                case EntityType.Evoker:
                case EntityType.Ghast:
                case EntityType.Guardian:
                case EntityType.Husk:
                case EntityType.MagmaCube:
                case EntityType.Phantom:
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
                case EntityType.Zombie:
                case EntityType.ZombiePigman:
                case EntityType.ZombieVillager:
                    return true;
                default:
                    return false;
            }
        }
    }
}
