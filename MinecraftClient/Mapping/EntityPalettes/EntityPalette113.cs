using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    /// <summary>
    /// Defines mappings for Minecraft 1.13.
    /// 1.13 and lower has 2 set of ids: One for non-living objects and one for living mobs
    /// 1.14+ has only one set of ids for all types of entities
    /// </summary>
    public class EntityPalette113 : EntityPalette
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
            { 67, EntityType.ShulkerBullet },
            { 68, EntityType.LlamaSpit },
            { 70, EntityType.FallingBlock },
            { 71, EntityType.ItemFrame },
            { 72, EntityType.EyeOfEnder },
            { 73, EntityType.Potion },
            { 75, EntityType.ExperienceBottle },
            { 76, EntityType.FireworkRocket },
            { 77, EntityType.LeashKnot },
            { 78, EntityType.ArmorStand },
            { 79, EntityType.EvokerFangs },
            { 90, EntityType.FishingBobber },
            { 91, EntityType.SpectralArrow },
            { 93, EntityType.DragonFireball },
            { 94, EntityType.Trident },
        };

        private static Dictionary<int, EntityType> mappingsMobs = new Dictionary<int, EntityType>()
        {
            // https://wiki.vg/Entity_metadata#Mobs
            { 0, EntityType.AreaEffectCloud },
            { 1, EntityType.ArmorStand },
            { 2, EntityType.Arrow },
            { 3, EntityType.Bat },
            { 4, EntityType.Blaze },
            { 5, EntityType.Boat },
            { 6, EntityType.CaveSpider },
            { 7, EntityType.Chicken },
            { 8, EntityType.Cod },
            { 9, EntityType.Cow },
            { 10, EntityType.Creeper },
            { 11, EntityType.Donkey },
            { 12, EntityType.Dolphin },
            { 13, EntityType.DragonFireball },
            { 14, EntityType.Drowned },
            { 15, EntityType.ElderGuardian },
            { 16, EntityType.EndCrystal },
            { 17, EntityType.EnderDragon },
            { 18, EntityType.Enderman },
            { 19, EntityType.Endermite },
            { 20, EntityType.EvokerFangs },
            { 21, EntityType.Evoker },
            { 22, EntityType.ExperienceBottle },
            { 23, EntityType.EyeOfEnder },
            { 24, EntityType.FallingBlock },
            { 25, EntityType.FireworkRocket },
            { 26, EntityType.Ghast },
            { 27, EntityType.Giant },
            { 28, EntityType.Guardian },
            { 29, EntityType.Horse },
            { 30, EntityType.Husk },
            { 31, EntityType.Illusioner },
            { 32, EntityType.Item },
            { 33, EntityType.ItemFrame },
            { 34, EntityType.Fireball },
            { 35, EntityType.LeashKnot },
            { 36, EntityType.Llama },
            { 37, EntityType.LlamaSpit },
            { 38, EntityType.MagmaCube },
            { 39, EntityType.Minecart },
            { 40, EntityType.ChestMinecart },
            { 41, EntityType.CommandBlockMinecart },
            { 42, EntityType.FurnaceMinecart },
            { 43, EntityType.HopperMinecart },
            { 44, EntityType.SpawnerMinecart },
            { 45, EntityType.TntMinecart },
            { 46, EntityType.Mule },
            { 47, EntityType.Mooshroom },
            { 48, EntityType.Ocelot },
            { 49, EntityType.Painting },
            { 50, EntityType.Parrot },
            { 51, EntityType.Pig },
            { 52, EntityType.Pufferfish },
            { 53, EntityType.ZombifiedPiglin },
            { 54, EntityType.PolarBear },
            { 55, EntityType.Tnt },
            { 56, EntityType.Rabbit },
            { 57, EntityType.Salmon },
            { 58, EntityType.Sheep },
            { 59, EntityType.Shulker },
            { 60, EntityType.ShulkerBullet },
            { 61, EntityType.Silverfish },
            { 62, EntityType.Skeleton },
            { 63, EntityType.SkeletonHorse },
            { 64, EntityType.Slime },
            { 65, EntityType.SmallFireball },
            { 66, EntityType.SnowGolem },
            { 67, EntityType.Snowball },
            { 68, EntityType.SpectralArrow },
            { 69, EntityType.Spider },
            { 70, EntityType.Squid },
            { 71, EntityType.Stray },
            { 72, EntityType.TropicalFish },
            { 73, EntityType.Turtle },
            { 74, EntityType.Egg },
            { 75, EntityType.EnderPearl },
            { 76, EntityType.ExperienceBottle },
            { 77, EntityType.Potion },
            { 78, EntityType.Vex },
            { 79, EntityType.Villager },
            { 80, EntityType.IronGolem },
            { 81, EntityType.Vindicator },
            { 82, EntityType.Witch },
            { 83, EntityType.Wither },
            { 84, EntityType.WitherSkeleton },
            { 85, EntityType.WitherSkull },
            { 86, EntityType.Wolf },
            { 87, EntityType.Zombie },
            { 88, EntityType.ZombieHorse },
            { 89, EntityType.ZombieVillager },
            { 90, EntityType.Phantom },
            { 91, EntityType.LightningBolt },
            { 92, EntityType.Player },
            { 93, EntityType.FishingBobber },
            { 94, EntityType.Trident },
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
