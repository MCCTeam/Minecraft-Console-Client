using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public class EntityPalette1204 : EntityPalette
    {
        private static readonly Dictionary<int, EntityType> mappings = new();

        static EntityPalette1204()
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
            mappings[10] = EntityType.Breeze;
            mappings[11] = EntityType.Camel;
            mappings[12] = EntityType.Cat;
            mappings[13] = EntityType.CaveSpider;
            mappings[14] = EntityType.ChestBoat;
            mappings[15] = EntityType.ChestMinecart;
            mappings[16] = EntityType.Chicken;
            mappings[17] = EntityType.Cod;
            mappings[18] = EntityType.CommandBlockMinecart;
            mappings[19] = EntityType.Cow;
            mappings[20] = EntityType.Creeper;
            mappings[21] = EntityType.Dolphin;
            mappings[22] = EntityType.Donkey;
            mappings[23] = EntityType.DragonFireball;
            mappings[24] = EntityType.Drowned;
            mappings[25] = EntityType.Egg;
            mappings[26] = EntityType.ElderGuardian;
            mappings[27] = EntityType.EndCrystal;
            mappings[28] = EntityType.EnderDragon;
            mappings[29] = EntityType.EnderPearl;
            mappings[30] = EntityType.Enderman;
            mappings[31] = EntityType.Endermite;
            mappings[32] = EntityType.Evoker;
            mappings[33] = EntityType.EvokerFangs;
            mappings[34] = EntityType.ExperienceBottle;
            mappings[35] = EntityType.ExperienceOrb;
            mappings[36] = EntityType.EyeOfEnder;
            mappings[37] = EntityType.FallingBlock;
            mappings[58] = EntityType.Fireball;
            mappings[38] = EntityType.FireworkRocket;
            mappings[125] = EntityType.FishingBobber;
            mappings[39] = EntityType.Fox;
            mappings[40] = EntityType.Frog;
            mappings[41] = EntityType.FurnaceMinecart;
            mappings[42] = EntityType.Ghast;
            mappings[43] = EntityType.Giant;
            mappings[44] = EntityType.GlowItemFrame;
            mappings[45] = EntityType.GlowSquid;
            mappings[46] = EntityType.Goat;
            mappings[47] = EntityType.Guardian;
            mappings[48] = EntityType.Hoglin;
            mappings[49] = EntityType.HopperMinecart;
            mappings[50] = EntityType.Horse;
            mappings[51] = EntityType.Husk;
            mappings[52] = EntityType.Illusioner;
            mappings[53] = EntityType.Interaction;
            mappings[54] = EntityType.IronGolem;
            mappings[55] = EntityType.Item;
            mappings[56] = EntityType.ItemDisplay;
            mappings[57] = EntityType.ItemFrame;
            mappings[59] = EntityType.LeashKnot;
            mappings[60] = EntityType.LightningBolt;
            mappings[61] = EntityType.Llama;
            mappings[62] = EntityType.LlamaSpit;
            mappings[63] = EntityType.MagmaCube;
            mappings[64] = EntityType.Marker;
            mappings[65] = EntityType.Minecart;
            mappings[66] = EntityType.Mooshroom;
            mappings[67] = EntityType.Mule;
            mappings[68] = EntityType.Ocelot;
            mappings[69] = EntityType.Painting;
            mappings[70] = EntityType.Panda;
            mappings[71] = EntityType.Parrot;
            mappings[72] = EntityType.Phantom;
            mappings[73] = EntityType.Pig;
            mappings[74] = EntityType.Piglin;
            mappings[75] = EntityType.PiglinBrute;
            mappings[76] = EntityType.Pillager;
            mappings[124] = EntityType.Player;
            mappings[77] = EntityType.PolarBear;
            mappings[78] = EntityType.Potion;
            mappings[79] = EntityType.Pufferfish;
            mappings[80] = EntityType.Rabbit;
            mappings[81] = EntityType.Ravager;
            mappings[82] = EntityType.Salmon;
            mappings[83] = EntityType.Sheep;
            mappings[84] = EntityType.Shulker;
            mappings[85] = EntityType.ShulkerBullet;
            mappings[86] = EntityType.Silverfish;
            mappings[87] = EntityType.Skeleton;
            mappings[88] = EntityType.SkeletonHorse;
            mappings[89] = EntityType.Slime;
            mappings[90] = EntityType.SmallFireball;
            mappings[91] = EntityType.Sniffer;
            mappings[92] = EntityType.SnowGolem;
            mappings[93] = EntityType.Snowball;
            mappings[94] = EntityType.SpawnerMinecart;
            mappings[95] = EntityType.SpectralArrow;
            mappings[96] = EntityType.Spider;
            mappings[97] = EntityType.Squid;
            mappings[98] = EntityType.Stray;
            mappings[99] = EntityType.Strider;
            mappings[100] = EntityType.Tadpole;
            mappings[101] = EntityType.TextDisplay;
            mappings[102] = EntityType.Tnt;
            mappings[103] = EntityType.TntMinecart;
            mappings[104] = EntityType.TraderLlama;
            mappings[105] = EntityType.Trident;
            mappings[106] = EntityType.TropicalFish;
            mappings[107] = EntityType.Turtle;
            mappings[108] = EntityType.Vex;
            mappings[109] = EntityType.Villager;
            mappings[110] = EntityType.Vindicator;
            mappings[111] = EntityType.WanderingTrader;
            mappings[112] = EntityType.Warden;
            mappings[113] = EntityType.WindCharge;
            mappings[114] = EntityType.Witch;
            mappings[115] = EntityType.Wither;
            mappings[116] = EntityType.WitherSkeleton;
            mappings[117] = EntityType.WitherSkull;
            mappings[118] = EntityType.Wolf;
            mappings[119] = EntityType.Zoglin;
            mappings[120] = EntityType.Zombie;
            mappings[121] = EntityType.ZombieHorse;
            mappings[122] = EntityType.ZombieVillager;
            mappings[123] = EntityType.ZombifiedPiglin;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
