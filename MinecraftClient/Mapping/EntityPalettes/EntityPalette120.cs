using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public class EntityPalette120 : EntityPalette
    {
        private static readonly Dictionary<int, EntityType> mappings = new();

        static EntityPalette120()
        {
            mappings[0] = EntityType.Allay;
            mappings[1] = EntityType.AreaEffectCloud;
            mappings[2] = EntityType.ArmorStand;
            mappings[3] = EntityType.Arrow;
            mappings[4] = EntityType.Axolotl;
            mappings[5] = EntityType.Bat;
            mappings[6] = EntityType.Bee;
            mappings[7] = EntityType.Blaze;
            mappings[8] = EntityType.BlockDisplay;
            mappings[9] = EntityType.Boat;
            mappings[10] = EntityType.Camel;
            mappings[11] = EntityType.Cat;
            mappings[12] = EntityType.CaveSpider;
            mappings[13] = EntityType.ChestBoat;
            mappings[14] = EntityType.ChestMinecart;
            mappings[15] = EntityType.Chicken;
            mappings[16] = EntityType.Cod;
            mappings[17] = EntityType.CommandBlockMinecart;
            mappings[18] = EntityType.Cow;
            mappings[19] = EntityType.Creeper;
            mappings[20] = EntityType.Dolphin;
            mappings[21] = EntityType.Donkey;
            mappings[22] = EntityType.DragonFireball;
            mappings[23] = EntityType.Drowned;
            mappings[24] = EntityType.Egg;
            mappings[25] = EntityType.ElderGuardian;
            mappings[26] = EntityType.EndCrystal;
            mappings[27] = EntityType.EnderDragon;
            mappings[28] = EntityType.EnderPearl;
            mappings[29] = EntityType.Enderman;
            mappings[30] = EntityType.Endermite;
            mappings[31] = EntityType.Evoker;
            mappings[32] = EntityType.EvokerFangs;
            mappings[33] = EntityType.ExperienceBottle;
            mappings[34] = EntityType.ExperienceOrb;
            mappings[35] = EntityType.EyeOfEnder;
            mappings[36] = EntityType.FallingBlock;
            mappings[57] = EntityType.Fireball;
            mappings[37] = EntityType.FireworkRocket;
            mappings[123] = EntityType.FishingBobber;
            mappings[38] = EntityType.Fox;
            mappings[39] = EntityType.Frog;
            mappings[40] = EntityType.FurnaceMinecart;
            mappings[41] = EntityType.Ghast;
            mappings[42] = EntityType.Giant;
            mappings[43] = EntityType.GlowItemFrame;
            mappings[44] = EntityType.GlowSquid;
            mappings[45] = EntityType.Goat;
            mappings[46] = EntityType.Guardian;
            mappings[47] = EntityType.Hoglin;
            mappings[48] = EntityType.HopperMinecart;
            mappings[49] = EntityType.Horse;
            mappings[50] = EntityType.Husk;
            mappings[51] = EntityType.Illusioner;
            mappings[52] = EntityType.Interaction;
            mappings[53] = EntityType.IronGolem;
            mappings[54] = EntityType.Item;
            mappings[55] = EntityType.ItemDisplay;
            mappings[56] = EntityType.ItemFrame;
            mappings[58] = EntityType.LeashKnot;
            mappings[59] = EntityType.LightningBolt;
            mappings[60] = EntityType.Llama;
            mappings[61] = EntityType.LlamaSpit;
            mappings[62] = EntityType.MagmaCube;
            mappings[63] = EntityType.Marker;
            mappings[64] = EntityType.Minecart;
            mappings[65] = EntityType.Mooshroom;
            mappings[66] = EntityType.Mule;
            mappings[67] = EntityType.Ocelot;
            mappings[68] = EntityType.Painting;
            mappings[69] = EntityType.Panda;
            mappings[70] = EntityType.Parrot;
            mappings[71] = EntityType.Phantom;
            mappings[72] = EntityType.Pig;
            mappings[73] = EntityType.Piglin;
            mappings[74] = EntityType.PiglinBrute;
            mappings[75] = EntityType.Pillager;
            mappings[122] = EntityType.Player;
            mappings[76] = EntityType.PolarBear;
            mappings[77] = EntityType.Potion;
            mappings[78] = EntityType.Pufferfish;
            mappings[79] = EntityType.Rabbit;
            mappings[80] = EntityType.Ravager;
            mappings[81] = EntityType.Salmon;
            mappings[82] = EntityType.Sheep;
            mappings[83] = EntityType.Shulker;
            mappings[84] = EntityType.ShulkerBullet;
            mappings[85] = EntityType.Silverfish;
            mappings[86] = EntityType.Skeleton;
            mappings[87] = EntityType.SkeletonHorse;
            mappings[88] = EntityType.Slime;
            mappings[89] = EntityType.SmallFireball;
            mappings[90] = EntityType.Sniffer;
            mappings[91] = EntityType.SnowGolem;
            mappings[92] = EntityType.Snowball;
            mappings[93] = EntityType.SpawnerMinecart;
            mappings[94] = EntityType.SpectralArrow;
            mappings[95] = EntityType.Spider;
            mappings[96] = EntityType.Squid;
            mappings[97] = EntityType.Stray;
            mappings[98] = EntityType.Strider;
            mappings[99] = EntityType.Tadpole;
            mappings[100] = EntityType.TextDisplay;
            mappings[101] = EntityType.Tnt;
            mappings[102] = EntityType.TntMinecart;
            mappings[103] = EntityType.TraderLlama;
            mappings[104] = EntityType.Trident;
            mappings[105] = EntityType.TropicalFish;
            mappings[106] = EntityType.Turtle;
            mappings[107] = EntityType.Vex;
            mappings[108] = EntityType.Villager;
            mappings[109] = EntityType.Vindicator;
            mappings[110] = EntityType.WanderingTrader;
            mappings[111] = EntityType.Warden;
            mappings[112] = EntityType.Witch;
            mappings[113] = EntityType.Wither;
            mappings[114] = EntityType.WitherSkeleton;
            mappings[115] = EntityType.WitherSkull;
            mappings[116] = EntityType.Wolf;
            mappings[117] = EntityType.Zoglin;
            mappings[118] = EntityType.Zombie;
            mappings[119] = EntityType.ZombieHorse;
            mappings[120] = EntityType.ZombieVillager;
            mappings[121] = EntityType.ZombifiedPiglin;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}