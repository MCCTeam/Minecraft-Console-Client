using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public class EntityPalette1162 : EntityPalette
    {
        private static Dictionary<int, EntityType> mappings = new Dictionary<int, EntityType>();

        static EntityPalette1162()
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
            mappings[61] = EntityType.PiglinBrute;
            mappings[62] = EntityType.Pillager;
            mappings[63] = EntityType.PolarBear;
            mappings[64] = EntityType.Tnt;
            mappings[65] = EntityType.Pufferfish;
            mappings[66] = EntityType.Rabbit;
            mappings[67] = EntityType.Ravager;
            mappings[68] = EntityType.Salmon;
            mappings[69] = EntityType.Sheep;
            mappings[70] = EntityType.Shulker;
            mappings[71] = EntityType.ShulkerBullet;
            mappings[72] = EntityType.Silverfish;
            mappings[73] = EntityType.Skeleton;
            mappings[74] = EntityType.SkeletonHorse;
            mappings[75] = EntityType.Slime;
            mappings[76] = EntityType.SmallFireball;
            mappings[77] = EntityType.SnowGolem;
            mappings[78] = EntityType.Snowball;
            mappings[79] = EntityType.SpectralArrow;
            mappings[80] = EntityType.Spider;
            mappings[81] = EntityType.Squid;
            mappings[82] = EntityType.Stray;
            mappings[83] = EntityType.Strider;
            mappings[84] = EntityType.Egg;
            mappings[85] = EntityType.EnderPearl;
            mappings[86] = EntityType.ExperienceBottle;
            mappings[87] = EntityType.Potion;
            mappings[88] = EntityType.Trident;
            mappings[89] = EntityType.TraderLlama;
            mappings[90] = EntityType.TropicalFish;
            mappings[91] = EntityType.Turtle;
            mappings[92] = EntityType.Vex;
            mappings[93] = EntityType.Villager;
            mappings[94] = EntityType.Vindicator;
            mappings[95] = EntityType.WanderingTrader;
            mappings[96] = EntityType.Witch;
            mappings[97] = EntityType.Wither;
            mappings[98] = EntityType.WitherSkeleton;
            mappings[99] = EntityType.WitherSkull;
            mappings[100] = EntityType.Wolf;
            mappings[101] = EntityType.Zoglin;
            mappings[102] = EntityType.Zombie;
            mappings[103] = EntityType.ZombieHorse;
            mappings[104] = EntityType.ZombieVillager;
            mappings[105] = EntityType.ZombifiedPiglin;
            mappings[106] = EntityType.Player;
            mappings[107] = EntityType.FishingBobber;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
