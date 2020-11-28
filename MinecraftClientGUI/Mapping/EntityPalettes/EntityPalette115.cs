using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    /// <summary>
    /// Defines mappings for Minecraft 1.15.
    /// Automatically generated using EntityPaletteGenerator.cs
    /// </summary>
    public class EntityPalette115 : EntityPalette
    {
        private static Dictionary<int, EntityType> mappings = new Dictionary<int, EntityType>();

        static EntityPalette115()
        {
            mappings[0] = EntityType.AreaEffectCloud;
            mappings[1] = EntityType.ArmorStand;
            mappings[2] = EntityType.Arrow;
            mappings[3] = EntityType.Bat;
            mappings[4] = EntityType.Bee;
            mappings[5] = EntityType.Blaze;
            mappings[6] = EntityType.Boat;
            mappings[7] = EntityType.Cat;
            mappings[8] = EntityType.CaveSpider;
            mappings[9] = EntityType.Chicken;
            mappings[10] = EntityType.Cod;
            mappings[11] = EntityType.Cow;
            mappings[12] = EntityType.Creeper;
            mappings[13] = EntityType.Donkey;
            mappings[14] = EntityType.Dolphin;
            mappings[15] = EntityType.DragonFireball;
            mappings[16] = EntityType.Drowned;
            mappings[17] = EntityType.ElderGuardian;
            mappings[18] = EntityType.EndCrystal;
            mappings[19] = EntityType.EnderDragon;
            mappings[20] = EntityType.Enderman;
            mappings[21] = EntityType.Endermite;
            mappings[22] = EntityType.EvokerFangs;
            mappings[23] = EntityType.Evoker;
            mappings[24] = EntityType.ExperienceOrb;
            mappings[25] = EntityType.EyeOfEnder;
            mappings[26] = EntityType.FallingBlock;
            mappings[27] = EntityType.FireworkRocket;
            mappings[28] = EntityType.Fox;
            mappings[29] = EntityType.Ghast;
            mappings[30] = EntityType.Giant;
            mappings[31] = EntityType.Guardian;
            mappings[32] = EntityType.Horse;
            mappings[33] = EntityType.Husk;
            mappings[34] = EntityType.Illusioner;
            mappings[35] = EntityType.Item;
            mappings[36] = EntityType.ItemFrame;
            mappings[37] = EntityType.Fireball;
            mappings[38] = EntityType.LeashKnot;
            mappings[39] = EntityType.Llama;
            mappings[40] = EntityType.LlamaSpit;
            mappings[41] = EntityType.MagmaCube;
            mappings[42] = EntityType.Minecart;
            mappings[43] = EntityType.ChestMinecart;
            mappings[44] = EntityType.CommandBlockMinecart;
            mappings[45] = EntityType.FurnaceMinecart;
            mappings[46] = EntityType.HopperMinecart;
            mappings[47] = EntityType.SpawnerMinecart;
            mappings[48] = EntityType.TntMinecart;
            mappings[49] = EntityType.Mule;
            mappings[50] = EntityType.Mooshroom;
            mappings[51] = EntityType.Ocelot;
            mappings[52] = EntityType.Painting;
            mappings[53] = EntityType.Panda;
            mappings[54] = EntityType.Parrot;
            mappings[55] = EntityType.Pig;
            mappings[56] = EntityType.Pufferfish;
            mappings[57] = EntityType.ZombifiedPiglin;
            mappings[58] = EntityType.PolarBear;
            mappings[59] = EntityType.Tnt;
            mappings[60] = EntityType.Rabbit;
            mappings[61] = EntityType.Salmon;
            mappings[62] = EntityType.Sheep;
            mappings[63] = EntityType.Shulker;
            mappings[64] = EntityType.ShulkerBullet;
            mappings[65] = EntityType.Silverfish;
            mappings[66] = EntityType.Skeleton;
            mappings[67] = EntityType.SkeletonHorse;
            mappings[68] = EntityType.Slime;
            mappings[69] = EntityType.SmallFireball;
            mappings[70] = EntityType.SnowGolem;
            mappings[71] = EntityType.Snowball;
            mappings[72] = EntityType.SpectralArrow;
            mappings[73] = EntityType.Spider;
            mappings[74] = EntityType.Squid;
            mappings[75] = EntityType.Stray;
            mappings[76] = EntityType.TraderLlama;
            mappings[77] = EntityType.TropicalFish;
            mappings[78] = EntityType.Turtle;
            mappings[79] = EntityType.Egg;
            mappings[80] = EntityType.EnderPearl;
            mappings[81] = EntityType.ExperienceBottle;
            mappings[82] = EntityType.Potion;
            mappings[83] = EntityType.Trident;
            mappings[84] = EntityType.Vex;
            mappings[85] = EntityType.Villager;
            mappings[86] = EntityType.IronGolem;
            mappings[87] = EntityType.Vindicator;
            mappings[88] = EntityType.Pillager;
            mappings[89] = EntityType.WanderingTrader;
            mappings[90] = EntityType.Witch;
            mappings[91] = EntityType.Wither;
            mappings[92] = EntityType.WitherSkeleton;
            mappings[93] = EntityType.WitherSkull;
            mappings[94] = EntityType.Wolf;
            mappings[95] = EntityType.Zombie;
            mappings[96] = EntityType.ZombieHorse;
            mappings[97] = EntityType.ZombieVillager;
            mappings[98] = EntityType.Phantom;
            mappings[99] = EntityType.Ravager;
            mappings[100] = EntityType.LightningBolt;
            mappings[101] = EntityType.Player;
            mappings[102] = EntityType.FishingBobber;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
