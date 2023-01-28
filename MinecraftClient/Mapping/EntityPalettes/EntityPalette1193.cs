using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public class EntityPalette1193 : EntityPalette
    {
        private static readonly Dictionary<int, EntityType> mappings = new();

        static EntityPalette1193()
        {
            mappings[0] = EntityType.Allay;
            mappings[1] = EntityType.AreaEffectCloud;
            mappings[2] = EntityType.ArmorStand;
            mappings[3] = EntityType.Arrow;
            mappings[4] = EntityType.Axolotl;
            mappings[5] = EntityType.Bat;
            mappings[6] = EntityType.Bee;
            mappings[7] = EntityType.Blaze;
            mappings[8] = EntityType.Boat;
            mappings[11] = EntityType.Camel;
            mappings[10] = EntityType.Cat;
            mappings[12] = EntityType.CaveSpider;
            mappings[9] = EntityType.ChestBoat;
            mappings[55] = EntityType.ChestMinecart;
            mappings[13] = EntityType.Chicken;
            mappings[14] = EntityType.Cod;
            mappings[56] = EntityType.CommandBlockMinecart;
            mappings[15] = EntityType.Cow;
            mappings[16] = EntityType.Creeper;
            mappings[17] = EntityType.Dolphin;
            mappings[18] = EntityType.Donkey;
            mappings[19] = EntityType.DragonFireball;
            mappings[20] = EntityType.Drowned;
            mappings[94] = EntityType.Egg;
            mappings[21] = EntityType.ElderGuardian;
            mappings[22] = EntityType.EndCrystal;
            mappings[23] = EntityType.EnderDragon;
            mappings[95] = EntityType.EnderPearl;
            mappings[24] = EntityType.Enderman;
            mappings[25] = EntityType.Endermite;
            mappings[26] = EntityType.Evoker;
            mappings[27] = EntityType.EvokerFangs;
            mappings[96] = EntityType.ExperienceBottle;
            mappings[28] = EntityType.ExperienceOrb;
            mappings[29] = EntityType.EyeOfEnder;
            mappings[30] = EntityType.FallingBlock;
            mappings[47] = EntityType.Fireball;
            mappings[31] = EntityType.FireworkRocket;
            mappings[118] = EntityType.FishingBobber;
            mappings[32] = EntityType.Fox;
            mappings[33] = EntityType.Frog;
            mappings[57] = EntityType.FurnaceMinecart;
            mappings[34] = EntityType.Ghast;
            mappings[35] = EntityType.Giant;
            mappings[36] = EntityType.GlowItemFrame;
            mappings[37] = EntityType.GlowSquid;
            mappings[38] = EntityType.Goat;
            mappings[39] = EntityType.Guardian;
            mappings[40] = EntityType.Hoglin;
            mappings[58] = EntityType.HopperMinecart;
            mappings[41] = EntityType.Horse;
            mappings[42] = EntityType.Husk;
            mappings[43] = EntityType.Illusioner;
            mappings[44] = EntityType.IronGolem;
            mappings[45] = EntityType.Item;
            mappings[46] = EntityType.ItemFrame;
            mappings[48] = EntityType.LeashKnot;
            mappings[49] = EntityType.LightningBolt;
            mappings[50] = EntityType.Llama;
            mappings[51] = EntityType.LlamaSpit;
            mappings[52] = EntityType.MagmaCube;
            mappings[53] = EntityType.Marker;
            mappings[54] = EntityType.Minecart;
            mappings[62] = EntityType.Mooshroom;
            mappings[61] = EntityType.Mule;
            mappings[63] = EntityType.Ocelot;
            mappings[64] = EntityType.Painting;
            mappings[65] = EntityType.Panda;
            mappings[66] = EntityType.Parrot;
            mappings[67] = EntityType.Phantom;
            mappings[68] = EntityType.Pig;
            mappings[69] = EntityType.Piglin;
            mappings[70] = EntityType.PiglinBrute;
            mappings[71] = EntityType.Pillager;
            mappings[117] = EntityType.Player;
            mappings[72] = EntityType.PolarBear;
            mappings[97] = EntityType.Potion;
            mappings[74] = EntityType.Pufferfish;
            mappings[75] = EntityType.Rabbit;
            mappings[76] = EntityType.Ravager;
            mappings[77] = EntityType.Salmon;
            mappings[78] = EntityType.Sheep;
            mappings[79] = EntityType.Shulker;
            mappings[80] = EntityType.ShulkerBullet;
            mappings[81] = EntityType.Silverfish;
            mappings[82] = EntityType.Skeleton;
            mappings[83] = EntityType.SkeletonHorse;
            mappings[84] = EntityType.Slime;
            mappings[85] = EntityType.SmallFireball;
            mappings[86] = EntityType.SnowGolem;
            mappings[87] = EntityType.Snowball;
            mappings[59] = EntityType.SpawnerMinecart;
            mappings[88] = EntityType.SpectralArrow;
            mappings[89] = EntityType.Spider;
            mappings[90] = EntityType.Squid;
            mappings[91] = EntityType.Stray;
            mappings[92] = EntityType.Strider;
            mappings[93] = EntityType.Tadpole;
            mappings[73] = EntityType.Tnt;
            mappings[60] = EntityType.TntMinecart;
            mappings[99] = EntityType.TraderLlama;
            mappings[98] = EntityType.Trident;
            mappings[100] = EntityType.TropicalFish;
            mappings[101] = EntityType.Turtle;
            mappings[102] = EntityType.Vex;
            mappings[103] = EntityType.Villager;
            mappings[104] = EntityType.Vindicator;
            mappings[105] = EntityType.WanderingTrader;
            mappings[106] = EntityType.Warden;
            mappings[107] = EntityType.Witch;
            mappings[108] = EntityType.Wither;
            mappings[109] = EntityType.WitherSkeleton;
            mappings[110] = EntityType.WitherSkull;
            mappings[111] = EntityType.Wolf;
            mappings[112] = EntityType.Zoglin;
            mappings[113] = EntityType.Zombie;
            mappings[114] = EntityType.ZombieHorse;
            mappings[115] = EntityType.ZombieVillager;
            mappings[116] = EntityType.ZombifiedPiglin;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
