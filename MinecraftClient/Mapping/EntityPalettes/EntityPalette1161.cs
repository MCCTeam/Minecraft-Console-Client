using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public class EntityPalette1161 : EntityPalette
    {
        private static Dictionary<int, EntityType> mappings = new Dictionary<int, EntityType>();

        static EntityPalette1161()
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
            mappings[13] = EntityType.Dolphin;
            mappings[14] = EntityType.Donkey;
            mappings[15] = EntityType.DragonFireball;
            mappings[16] = EntityType.Drowned;
            mappings[17] = EntityType.ElderGuardian;
            mappings[18] = EntityType.EndCrystal;
            mappings[19] = EntityType.EnderDragon;
            mappings[20] = EntityType.Enderman;
            mappings[21] = EntityType.Endermite;
            mappings[22] = EntityType.Evoker;
            mappings[23] = EntityType.EvokerFangs;
            mappings[24] = EntityType.ExperienceOrb;
            mappings[25] = EntityType.EyeOfEnder;
            mappings[26] = EntityType.FallingBlock;
            mappings[27] = EntityType.FireworkRocket;
            mappings[28] = EntityType.Fox;
            mappings[29] = EntityType.Ghast;
            mappings[30] = EntityType.Giant;
            mappings[31] = EntityType.Guardian;
            mappings[32] = EntityType.Hoglin;
            mappings[33] = EntityType.Horse;
            mappings[34] = EntityType.Husk;
            mappings[35] = EntityType.Illusioner;
            mappings[36] = EntityType.IronGolem;
            mappings[37] = EntityType.Item;
            mappings[38] = EntityType.ItemFrame;
            mappings[39] = EntityType.Fireball;
            mappings[40] = EntityType.LeashKnot;
            mappings[41] = EntityType.LightningBolt;
            mappings[42] = EntityType.Llama;
            mappings[43] = EntityType.LlamaSpit;
            mappings[44] = EntityType.MagmaCube;
            mappings[45] = EntityType.Minecart;
            mappings[46] = EntityType.ChestMinecart;
            mappings[47] = EntityType.CommandBlockMinecart;
            mappings[48] = EntityType.FurnaceMinecart;
            mappings[49] = EntityType.HopperMinecart;
            mappings[50] = EntityType.SpawnerMinecart;
            mappings[51] = EntityType.TntMinecart;
            mappings[52] = EntityType.Mule;
            mappings[53] = EntityType.Mooshroom;
            mappings[54] = EntityType.Ocelot;
            mappings[55] = EntityType.Painting;
            mappings[56] = EntityType.Panda;
            mappings[57] = EntityType.Parrot;
            mappings[58] = EntityType.Phantom;
            mappings[59] = EntityType.Pig;
            mappings[60] = EntityType.Piglin;
            mappings[61] = EntityType.Pillager;
            mappings[62] = EntityType.PolarBear;
            mappings[63] = EntityType.Tnt;
            mappings[64] = EntityType.Pufferfish;
            mappings[65] = EntityType.Rabbit;
            mappings[66] = EntityType.Ravager;
            mappings[67] = EntityType.Salmon;
            mappings[68] = EntityType.Sheep;
            mappings[69] = EntityType.Shulker;
            mappings[70] = EntityType.ShulkerBullet;
            mappings[71] = EntityType.Silverfish;
            mappings[72] = EntityType.Skeleton;
            mappings[73] = EntityType.SkeletonHorse;
            mappings[74] = EntityType.Slime;
            mappings[75] = EntityType.SmallFireball;
            mappings[76] = EntityType.SnowGolem;
            mappings[77] = EntityType.Snowball;
            mappings[78] = EntityType.SpectralArrow;
            mappings[79] = EntityType.Spider;
            mappings[80] = EntityType.Squid;
            mappings[81] = EntityType.Stray;
            mappings[82] = EntityType.Strider;
            mappings[83] = EntityType.Egg;
            mappings[84] = EntityType.EnderPearl;
            mappings[85] = EntityType.ExperienceBottle;
            mappings[86] = EntityType.Potion;
            mappings[87] = EntityType.Trident;
            mappings[88] = EntityType.TraderLlama;
            mappings[89] = EntityType.TropicalFish;
            mappings[90] = EntityType.Turtle;
            mappings[91] = EntityType.Vex;
            mappings[92] = EntityType.Villager;
            mappings[93] = EntityType.Vindicator;
            mappings[94] = EntityType.WanderingTrader;
            mappings[95] = EntityType.Witch;
            mappings[96] = EntityType.Wither;
            mappings[97] = EntityType.WitherSkeleton;
            mappings[98] = EntityType.WitherSkull;
            mappings[99] = EntityType.Wolf;
            mappings[100] = EntityType.Zoglin;
            mappings[101] = EntityType.Zombie;
            mappings[102] = EntityType.ZombieHorse;
            mappings[103] = EntityType.ZombieVillager;
            mappings[104] = EntityType.ZombifiedPiglin;
            mappings[105] = EntityType.Player;
            mappings[106] = EntityType.FishingBobber;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
