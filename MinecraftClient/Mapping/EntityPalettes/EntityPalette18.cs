using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    /// <summary>
    /// Defines mappings of entitiy IDs for 1.8
    /// Manually typed out by Milutinke :(
    /// Data source: https://pokechu22.github.io/Burger/1.8.json and https://wiki.vg/index.php?title=Entity_metadata&oldid=7415
    /// </summary>
    public class EntityPalette18 : EntityPalette
    {
        private static Dictionary<int, EntityType> mappingsObjects = new Dictionary<int, EntityType>()
        {
            // https://wiki.vg/Entity_metadata#Objects
            { 1, EntityType.Boat },
            { 2, EntityType.Item },
            { 3, EntityType.AreaEffectCloud },
            { 10, EntityType.Minecart },
            { 50, EntityType.Tnt },
            { 51, EntityType.EndCrystal },
            { 60, EntityType.Arrow },
            { 61, EntityType.Snowball },
            { 62, EntityType.Egg },
            { 63, EntityType.Fireball },
            { 64, EntityType.SmallFireball },
            { 65, EntityType.EnderPearl },
            { 66, EntityType.WitherSkull },
            { 70, EntityType.FallingBlock },
            { 71, EntityType.ItemFrame },
            { 72, EntityType.EyeOfEnder },
            { 73, EntityType.Egg },
            { 75, EntityType.ExperienceBottle },
            { 76, EntityType.FireworkRocket },
            { 77, EntityType.LeashKnot },
            { 78, EntityType.ArmorStand },
            { 90, EntityType.FishingBobber },
            { 91, EntityType.SpectralArrow },
            { 93, EntityType.DragonFireball },
        };

        private static Dictionary<int, EntityType> mappingsMobs = new Dictionary<int, EntityType>() {
            { 1, EntityType.Item },
            { 2, EntityType.ExperienceOrb },
            { 8, EntityType.LeashKnot },
            { 9, EntityType.Painting },
            { 10, EntityType.Arrow },
            { 11, EntityType.Snowball },
            { 12, EntityType.Fireball },
            { 13, EntityType.SmallFireball },
            { 14, EntityType.EnderPearl },
            { 15, EntityType.EyeOfEnder },
            { 16, EntityType.Potion },
            { 17, EntityType.ExperienceBottle },
            { 18, EntityType.ItemFrame },
            { 19, EntityType.WitherSkull },
            { 20, EntityType.Tnt },
            { 21, EntityType.FallingBlock },
            { 22, EntityType.FireworkRocket },
            { 30, EntityType.ArmorStand },
            { 40, EntityType.CommandBlockMinecart },
            { 41, EntityType.Boat },
            { 42, EntityType.Minecart },
            { 43, EntityType.ChestMinecart },
            { 44, EntityType.FurnaceMinecart },
            { 45, EntityType.TntMinecart },
            { 46, EntityType.HopperMinecart },
            { 47, EntityType.SpawnerMinecart },
            { 50, EntityType.Creeper },
            { 51, EntityType.Skeleton },
            { 52, EntityType.Spider },
            { 53, EntityType.Giant },
            { 54, EntityType.Zombie },
            { 55, EntityType.Slime },
            { 56, EntityType.Ghast },
            { 57, EntityType.ZombifiedPiglin },
            { 58, EntityType.Enderman },
            { 59, EntityType.CaveSpider },
            { 60, EntityType.Silverfish },
            { 61, EntityType.Blaze },
            { 62, EntityType.MagmaCube },
            { 63, EntityType.EnderDragon },
            { 64, EntityType.Wither },
            { 65, EntityType.Bat },
            { 66, EntityType.Witch },
            { 67, EntityType.Endermite },
            { 68, EntityType.Guardian },
            { 90, EntityType.Pig },
            { 91, EntityType.Sheep },
            { 92, EntityType.Cow },
            { 93, EntityType.Chicken },
            { 94, EntityType.Squid },
            { 95, EntityType.Wolf },
            { 96, EntityType.Mooshroom },
            { 97, EntityType.SnowGolem },
            { 98, EntityType.Cat },
            { 99, EntityType.IronGolem },
            { 100, EntityType.Horse },
            { 101, EntityType.Rabbit },
            { 120, EntityType.Villager },
            { 200, EntityType.EndCrystal }
        };

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappingsMobs;
        }

        protected override Dictionary<int, EntityType> GetDictNonLiving()
        {
            return mappingsObjects;
        }
    }
}
