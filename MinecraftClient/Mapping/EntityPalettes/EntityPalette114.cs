using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    /// <summary>
    /// Defines mappings for Minecraft 1.14.
    /// Automatically generated using EntityPaletteGenerator.cs
    /// </summary>
    public class EntityPalette114 : EntityPalette
    {
        private static Dictionary<int, EntityType> mappings = new Dictionary<int, EntityType>();

        static EntityPalette114()
        {
            mappings[0] = EntityType.AreaEffectCloud;
            mappings[1] = EntityType.ArmorStand;
            mappings[2] = EntityType.Arrow;
            mappings[3] = EntityType.Bat;
            mappings[4] = EntityType.Blaze;
            mappings[5] = EntityType.Boat;
            mappings[6] = EntityType.Cat;
            mappings[7] = EntityType.CaveSpider;
            mappings[8] = EntityType.Chicken;
            mappings[9] = EntityType.Cod;
            mappings[10] = EntityType.Cow;
            mappings[11] = EntityType.Creeper;
            mappings[12] = EntityType.Donkey;
            mappings[13] = EntityType.Dolphin;
            mappings[14] = EntityType.DragonFireball;
            mappings[15] = EntityType.Drowned;
            mappings[16] = EntityType.ElderGuardian;
            mappings[17] = EntityType.EndCrystal;
            mappings[18] = EntityType.EnderDragon;
            mappings[19] = EntityType.Enderman;
            mappings[20] = EntityType.Endermite;
            mappings[21] = EntityType.EvokerFangs;
            mappings[22] = EntityType.Evoker;
            mappings[23] = EntityType.ExperienceOrb;
            mappings[24] = EntityType.EyeOfEnder;
            mappings[25] = EntityType.FallingBlock;
            mappings[26] = EntityType.FireworkRocket;
            mappings[27] = EntityType.Fox;
            mappings[28] = EntityType.Ghast;
            mappings[29] = EntityType.Giant;
            mappings[30] = EntityType.Guardian;
            mappings[31] = EntityType.Horse;
            mappings[32] = EntityType.Husk;
            mappings[33] = EntityType.Illusioner;
            mappings[34] = EntityType.Item;
            mappings[35] = EntityType.ItemFrame;
            mappings[36] = EntityType.Fireball;
            mappings[37] = EntityType.LeashKnot;
            mappings[38] = EntityType.Llama;
            mappings[39] = EntityType.LlamaSpit;
            mappings[40] = EntityType.MagmaCube;
            mappings[41] = EntityType.Minecart;
            mappings[42] = EntityType.ChestMinecart;
            mappings[43] = EntityType.CommandBlockMinecart;
            mappings[44] = EntityType.FurnaceMinecart;
            mappings[45] = EntityType.HopperMinecart;
            mappings[46] = EntityType.SpawnerMinecart;
            mappings[47] = EntityType.TntMinecart;
            mappings[48] = EntityType.Mule;
            mappings[49] = EntityType.Mooshroom;
            mappings[50] = EntityType.Ocelot;
            mappings[51] = EntityType.Painting;
            mappings[52] = EntityType.Panda;
            mappings[53] = EntityType.Parrot;
            mappings[54] = EntityType.Pig;
            mappings[55] = EntityType.Pufferfish;
            mappings[56] = EntityType.ZombifiedPiglin;
            mappings[57] = EntityType.PolarBear;
            mappings[58] = EntityType.Tnt;
            mappings[59] = EntityType.Rabbit;
            mappings[60] = EntityType.Salmon;
            mappings[61] = EntityType.Sheep;
            mappings[62] = EntityType.Shulker;
            mappings[63] = EntityType.ShulkerBullet;
            mappings[64] = EntityType.Silverfish;
            mappings[65] = EntityType.Skeleton;
            mappings[66] = EntityType.SkeletonHorse;
            mappings[67] = EntityType.Slime;
            mappings[68] = EntityType.SmallFireball;
            mappings[69] = EntityType.SnowGolem;
            mappings[70] = EntityType.Snowball;
            mappings[71] = EntityType.SpectralArrow;
            mappings[72] = EntityType.Spider;
            mappings[73] = EntityType.Squid;
            mappings[74] = EntityType.Stray;
            mappings[75] = EntityType.TraderLlama;
            mappings[76] = EntityType.TropicalFish;
            mappings[77] = EntityType.Turtle;
            mappings[78] = EntityType.Egg;
            mappings[79] = EntityType.EnderPearl;
            mappings[80] = EntityType.ExperienceBottle;
            mappings[81] = EntityType.Potion;
            mappings[82] = EntityType.Trident;
            mappings[83] = EntityType.Vex;
            mappings[84] = EntityType.Villager;
            mappings[85] = EntityType.IronGolem;
            mappings[86] = EntityType.Vindicator;
            mappings[87] = EntityType.Pillager;
            mappings[88] = EntityType.WanderingTrader;
            mappings[89] = EntityType.Witch;
            mappings[90] = EntityType.Wither;
            mappings[91] = EntityType.WitherSkeleton;
            mappings[92] = EntityType.WitherSkull;
            mappings[93] = EntityType.Wolf;
            mappings[94] = EntityType.Zombie;
            mappings[95] = EntityType.ZombieHorse;
            mappings[96] = EntityType.ZombieVillager;
            mappings[97] = EntityType.Phantom;
            mappings[98] = EntityType.Ravager;
            mappings[99] = EntityType.LightningBolt;
            mappings[100] = EntityType.Player;
            mappings[101] = EntityType.FishingBobber;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
