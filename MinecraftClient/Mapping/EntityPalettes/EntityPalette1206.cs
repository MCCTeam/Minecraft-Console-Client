using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public class EntityPalette1206 : EntityPalette
    {
        private static readonly Dictionary<int, EntityType> mappings = new();

        static EntityPalette1206()
        {
            mappings[0] = EntityType.Allay;
            mappings[1] = EntityType.AreaEffectCloud;
            mappings[2] = EntityType.Armadillo;
            mappings[3] = EntityType.ArmorStand;
            mappings[4] = EntityType.Arrow;
            mappings[5] = EntityType.Axolotl;
            mappings[6] = EntityType.Bat;
            mappings[7] = EntityType.Bee;
            mappings[8] = EntityType.Blaze;
            mappings[9] = EntityType.BlockDisplay;
            mappings[10] = EntityType.Boat;
            mappings[11] = EntityType.Bogged;
            mappings[12] = EntityType.Breeze;
            mappings[13] = EntityType.BreezeWindCharge;
            mappings[14] = EntityType.Camel;
            mappings[15] = EntityType.Cat;
            mappings[16] = EntityType.CaveSpider;
            mappings[17] = EntityType.ChestBoat;
            mappings[18] = EntityType.ChestMinecart;
            mappings[19] = EntityType.Chicken;
            mappings[20] = EntityType.Cod;
            mappings[21] = EntityType.CommandBlockMinecart;
            mappings[22] = EntityType.Cow;
            mappings[23] = EntityType.Creeper;
            mappings[24] = EntityType.Dolphin;
            mappings[25] = EntityType.Donkey;
            mappings[26] = EntityType.DragonFireball;
            mappings[27] = EntityType.Drowned;
            mappings[28] = EntityType.Egg;
            mappings[29] = EntityType.ElderGuardian;
            mappings[30] = EntityType.EndCrystal;
            mappings[31] = EntityType.EnderDragon;
            mappings[32] = EntityType.EnderPearl;
            mappings[33] = EntityType.Enderman;
            mappings[34] = EntityType.Endermite;
            mappings[35] = EntityType.Evoker;
            mappings[36] = EntityType.EvokerFangs;
            mappings[37] = EntityType.ExperienceBottle;
            mappings[38] = EntityType.ExperienceOrb;
            mappings[39] = EntityType.EyeOfEnder;
            mappings[40] = EntityType.FallingBlock;
            mappings[62] = EntityType.Fireball;
            mappings[41] = EntityType.FireworkRocket;
            mappings[129] = EntityType.FishingBobber;
            mappings[42] = EntityType.Fox;
            mappings[43] = EntityType.Frog;
            mappings[44] = EntityType.FurnaceMinecart;
            mappings[45] = EntityType.Ghast;
            mappings[46] = EntityType.Giant;
            mappings[47] = EntityType.GlowItemFrame;
            mappings[48] = EntityType.GlowSquid;
            mappings[49] = EntityType.Goat;
            mappings[50] = EntityType.Guardian;
            mappings[51] = EntityType.Hoglin;
            mappings[52] = EntityType.HopperMinecart;
            mappings[53] = EntityType.Horse;
            mappings[54] = EntityType.Husk;
            mappings[55] = EntityType.Illusioner;
            mappings[56] = EntityType.Interaction;
            mappings[57] = EntityType.IronGolem;
            mappings[58] = EntityType.Item;
            mappings[59] = EntityType.ItemDisplay;
            mappings[60] = EntityType.ItemFrame;
            mappings[63] = EntityType.LeashKnot;
            mappings[64] = EntityType.LightningBolt;
            mappings[65] = EntityType.Llama;
            mappings[66] = EntityType.LlamaSpit;
            mappings[67] = EntityType.MagmaCube;
            mappings[68] = EntityType.Marker;
            mappings[69] = EntityType.Minecart;
            mappings[70] = EntityType.Mooshroom;
            mappings[71] = EntityType.Mule;
            mappings[72] = EntityType.Ocelot;
            mappings[61] = EntityType.OminousItemSpawner;
            mappings[73] = EntityType.Painting;
            mappings[74] = EntityType.Panda;
            mappings[75] = EntityType.Parrot;
            mappings[76] = EntityType.Phantom;
            mappings[77] = EntityType.Pig;
            mappings[78] = EntityType.Piglin;
            mappings[79] = EntityType.PiglinBrute;
            mappings[80] = EntityType.Pillager;
            mappings[128] = EntityType.Player;
            mappings[81] = EntityType.PolarBear;
            mappings[82] = EntityType.Potion;
            mappings[83] = EntityType.Pufferfish;
            mappings[84] = EntityType.Rabbit;
            mappings[85] = EntityType.Ravager;
            mappings[86] = EntityType.Salmon;
            mappings[87] = EntityType.Sheep;
            mappings[88] = EntityType.Shulker;
            mappings[89] = EntityType.ShulkerBullet;
            mappings[90] = EntityType.Silverfish;
            mappings[91] = EntityType.Skeleton;
            mappings[92] = EntityType.SkeletonHorse;
            mappings[93] = EntityType.Slime;
            mappings[94] = EntityType.SmallFireball;
            mappings[95] = EntityType.Sniffer;
            mappings[96] = EntityType.SnowGolem;
            mappings[97] = EntityType.Snowball;
            mappings[98] = EntityType.SpawnerMinecart;
            mappings[99] = EntityType.SpectralArrow;
            mappings[100] = EntityType.Spider;
            mappings[101] = EntityType.Squid;
            mappings[102] = EntityType.Stray;
            mappings[103] = EntityType.Strider;
            mappings[104] = EntityType.Tadpole;
            mappings[105] = EntityType.TextDisplay;
            mappings[106] = EntityType.Tnt;
            mappings[107] = EntityType.TntMinecart;
            mappings[108] = EntityType.TraderLlama;
            mappings[109] = EntityType.Trident;
            mappings[110] = EntityType.TropicalFish;
            mappings[111] = EntityType.Turtle;
            mappings[112] = EntityType.Vex;
            mappings[113] = EntityType.Villager;
            mappings[114] = EntityType.Vindicator;
            mappings[115] = EntityType.WanderingTrader;
            mappings[116] = EntityType.Warden;
            mappings[117] = EntityType.WindCharge;
            mappings[118] = EntityType.Witch;
            mappings[119] = EntityType.Wither;
            mappings[120] = EntityType.WitherSkeleton;
            mappings[121] = EntityType.WitherSkull;
            mappings[122] = EntityType.Wolf;
            mappings[123] = EntityType.Zoglin;
            mappings[124] = EntityType.Zombie;
            mappings[125] = EntityType.ZombieHorse;
            mappings[126] = EntityType.ZombieVillager;
            mappings[127] = EntityType.ZombifiedPiglin;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
