using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public class EntityPalette119 : EntityPalette
    {
        private static readonly Dictionary<int, EntityType> mappings = new();

        static EntityPalette119()
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
            mappings[10] = EntityType.Cat;
            mappings[11] = EntityType.CaveSpider;
            mappings[9] = EntityType.ChestBoat;
            mappings[54] = EntityType.ChestMinecart;
            mappings[12] = EntityType.Chicken;
            mappings[13] = EntityType.Cod;
            mappings[55] = EntityType.CommandBlockMinecart;
            mappings[14] = EntityType.Cow;
            mappings[15] = EntityType.Creeper;
            mappings[16] = EntityType.Dolphin;
            mappings[17] = EntityType.Donkey;
            mappings[18] = EntityType.DragonFireball;
            mappings[19] = EntityType.Drowned;
            mappings[93] = EntityType.Egg;
            mappings[20] = EntityType.ElderGuardian;
            mappings[21] = EntityType.EndCrystal;
            mappings[22] = EntityType.EnderDragon;
            mappings[94] = EntityType.EnderPearl;
            mappings[23] = EntityType.Enderman;
            mappings[24] = EntityType.Endermite;
            mappings[25] = EntityType.Evoker;
            mappings[26] = EntityType.EvokerFangs;
            mappings[95] = EntityType.ExperienceBottle;
            mappings[27] = EntityType.ExperienceOrb;
            mappings[28] = EntityType.EyeOfEnder;
            mappings[29] = EntityType.FallingBlock;
            mappings[46] = EntityType.Fireball;
            mappings[30] = EntityType.FireworkRocket;
            mappings[117] = EntityType.FishingBobber;
            mappings[31] = EntityType.Fox;
            mappings[32] = EntityType.Frog;
            mappings[56] = EntityType.FurnaceMinecart;
            mappings[33] = EntityType.Ghast;
            mappings[34] = EntityType.Giant;
            mappings[35] = EntityType.GlowItemFrame;
            mappings[36] = EntityType.GlowSquid;
            mappings[37] = EntityType.Goat;
            mappings[38] = EntityType.Guardian;
            mappings[39] = EntityType.Hoglin;
            mappings[57] = EntityType.HopperMinecart;
            mappings[40] = EntityType.Horse;
            mappings[41] = EntityType.Husk;
            mappings[42] = EntityType.Illusioner;
            mappings[43] = EntityType.IronGolem;
            mappings[44] = EntityType.Item;
            mappings[45] = EntityType.ItemFrame;
            mappings[47] = EntityType.LeashKnot;
            mappings[48] = EntityType.LightningBolt;
            mappings[49] = EntityType.Llama;
            mappings[50] = EntityType.LlamaSpit;
            mappings[51] = EntityType.MagmaCube;
            mappings[52] = EntityType.Marker;
            mappings[53] = EntityType.Minecart;
            mappings[61] = EntityType.Mooshroom;
            mappings[60] = EntityType.Mule;
            mappings[62] = EntityType.Ocelot;
            mappings[63] = EntityType.Painting;
            mappings[64] = EntityType.Panda;
            mappings[65] = EntityType.Parrot;
            mappings[66] = EntityType.Phantom;
            mappings[67] = EntityType.Pig;
            mappings[68] = EntityType.Piglin;
            mappings[69] = EntityType.PiglinBrute;
            mappings[70] = EntityType.Pillager;
            mappings[116] = EntityType.Player;
            mappings[71] = EntityType.PolarBear;
            mappings[96] = EntityType.Potion;
            mappings[73] = EntityType.Pufferfish;
            mappings[74] = EntityType.Rabbit;
            mappings[75] = EntityType.Ravager;
            mappings[76] = EntityType.Salmon;
            mappings[77] = EntityType.Sheep;
            mappings[78] = EntityType.Shulker;
            mappings[79] = EntityType.ShulkerBullet;
            mappings[80] = EntityType.Silverfish;
            mappings[81] = EntityType.Skeleton;
            mappings[82] = EntityType.SkeletonHorse;
            mappings[83] = EntityType.Slime;
            mappings[84] = EntityType.SmallFireball;
            mappings[85] = EntityType.SnowGolem;
            mappings[86] = EntityType.Snowball;
            mappings[58] = EntityType.SpawnerMinecart;
            mappings[87] = EntityType.SpectralArrow;
            mappings[88] = EntityType.Spider;
            mappings[89] = EntityType.Squid;
            mappings[90] = EntityType.Stray;
            mappings[91] = EntityType.Strider;
            mappings[92] = EntityType.Tadpole;
            mappings[72] = EntityType.Tnt;
            mappings[59] = EntityType.TntMinecart;
            mappings[98] = EntityType.TraderLlama;
            mappings[97] = EntityType.Trident;
            mappings[99] = EntityType.TropicalFish;
            mappings[100] = EntityType.Turtle;
            mappings[101] = EntityType.Vex;
            mappings[102] = EntityType.Villager;
            mappings[103] = EntityType.Vindicator;
            mappings[104] = EntityType.WanderingTrader;
            mappings[105] = EntityType.Warden;
            mappings[106] = EntityType.Witch;
            mappings[107] = EntityType.Wither;
            mappings[108] = EntityType.WitherSkeleton;
            mappings[109] = EntityType.WitherSkull;
            mappings[110] = EntityType.Wolf;
            mappings[111] = EntityType.Zoglin;
            mappings[112] = EntityType.Zombie;
            mappings[113] = EntityType.ZombieHorse;
            mappings[114] = EntityType.ZombieVillager;
            mappings[115] = EntityType.ZombifiedPiglin;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
