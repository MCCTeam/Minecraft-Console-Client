using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public class EntityPalette117 : EntityPalette
    {
        private static readonly Dictionary<int, EntityType> mappings = new();

        static EntityPalette117()
        {
            mappings[0] = EntityType.AreaEffectCloud;
            mappings[1] = EntityType.ArmorStand;
            mappings[2] = EntityType.Arrow;
            mappings[3] = EntityType.Axolotl;
            mappings[4] = EntityType.Bat;
            mappings[5] = EntityType.Bee;
            mappings[6] = EntityType.Blaze;
            mappings[7] = EntityType.Boat;
            mappings[8] = EntityType.Cat;
            mappings[9] = EntityType.CaveSpider;
            mappings[10] = EntityType.Chicken;
            mappings[11] = EntityType.Cod;
            mappings[12] = EntityType.Cow;
            mappings[13] = EntityType.Creeper;
            mappings[14] = EntityType.Dolphin;
            mappings[15] = EntityType.Donkey;
            mappings[16] = EntityType.DragonFireball;
            mappings[17] = EntityType.Drowned;
            mappings[18] = EntityType.ElderGuardian;
            mappings[19] = EntityType.EndCrystal;
            mappings[20] = EntityType.EnderDragon;
            mappings[21] = EntityType.Enderman;
            mappings[22] = EntityType.Endermite;
            mappings[23] = EntityType.Evoker;
            mappings[24] = EntityType.EvokerFangs;
            mappings[25] = EntityType.ExperienceOrb;
            mappings[26] = EntityType.EyeOfEnder;
            mappings[27] = EntityType.FallingBlock;
            mappings[28] = EntityType.FireworkRocket;
            mappings[29] = EntityType.Fox;
            mappings[30] = EntityType.Ghast;
            mappings[31] = EntityType.Giant;
            mappings[32] = EntityType.GlowItemFrame;
            mappings[33] = EntityType.GlowSquid;
            mappings[34] = EntityType.Goat;
            mappings[35] = EntityType.Guardian;
            mappings[36] = EntityType.Hoglin;
            mappings[37] = EntityType.Horse;
            mappings[38] = EntityType.Husk;
            mappings[39] = EntityType.Illusioner;
            mappings[40] = EntityType.IronGolem;
            mappings[41] = EntityType.Item;
            mappings[42] = EntityType.ItemFrame;
            mappings[43] = EntityType.Fireball;
            mappings[44] = EntityType.LeashKnot;
            mappings[45] = EntityType.LightningBolt;
            mappings[46] = EntityType.Llama;
            mappings[47] = EntityType.LlamaSpit;
            mappings[48] = EntityType.MagmaCube;
            mappings[49] = EntityType.Marker;
            mappings[50] = EntityType.Minecart;
            mappings[51] = EntityType.ChestMinecart;
            mappings[52] = EntityType.CommandBlockMinecart;
            mappings[53] = EntityType.FurnaceMinecart;
            mappings[54] = EntityType.HopperMinecart;
            mappings[55] = EntityType.SpawnerMinecart;
            mappings[56] = EntityType.TntMinecart;
            mappings[57] = EntityType.Mule;
            mappings[58] = EntityType.Mooshroom;
            mappings[59] = EntityType.Ocelot;
            mappings[60] = EntityType.Painting;
            mappings[61] = EntityType.Panda;
            mappings[62] = EntityType.Parrot;
            mappings[63] = EntityType.Phantom;
            mappings[64] = EntityType.Pig;
            mappings[65] = EntityType.Piglin;
            mappings[66] = EntityType.PiglinBrute;
            mappings[67] = EntityType.Pillager;
            mappings[68] = EntityType.PolarBear;
            mappings[69] = EntityType.Tnt;
            mappings[70] = EntityType.Pufferfish;
            mappings[71] = EntityType.Rabbit;
            mappings[72] = EntityType.Ravager;
            mappings[73] = EntityType.Salmon;
            mappings[74] = EntityType.Sheep;
            mappings[75] = EntityType.Shulker;
            mappings[76] = EntityType.ShulkerBullet;
            mappings[77] = EntityType.Silverfish;
            mappings[78] = EntityType.Skeleton;
            mappings[79] = EntityType.SkeletonHorse;
            mappings[80] = EntityType.Slime;
            mappings[81] = EntityType.SmallFireball;
            mappings[82] = EntityType.SnowGolem;
            mappings[83] = EntityType.Snowball;
            mappings[84] = EntityType.SpectralArrow;
            mappings[85] = EntityType.Spider;
            mappings[86] = EntityType.Squid;
            mappings[87] = EntityType.Stray;
            mappings[88] = EntityType.Strider;
            mappings[89] = EntityType.Egg;
            mappings[90] = EntityType.EnderPearl;
            mappings[91] = EntityType.ExperienceBottle;
            mappings[92] = EntityType.Potion;
            mappings[93] = EntityType.Trident;
            mappings[94] = EntityType.TraderLlama;
            mappings[95] = EntityType.TropicalFish;
            mappings[96] = EntityType.Turtle;
            mappings[97] = EntityType.Vex;
            mappings[98] = EntityType.Villager;
            mappings[99] = EntityType.Vindicator;
            mappings[100] = EntityType.WanderingTrader;
            mappings[101] = EntityType.Witch;
            mappings[102] = EntityType.Wither;
            mappings[103] = EntityType.WitherSkeleton;
            mappings[104] = EntityType.WitherSkull;
            mappings[105] = EntityType.Wolf;
            mappings[106] = EntityType.Zoglin;
            mappings[107] = EntityType.Zombie;
            mappings[108] = EntityType.ZombieHorse;
            mappings[109] = EntityType.ZombieVillager;
            mappings[110] = EntityType.ZombifiedPiglin;
            mappings[111] = EntityType.Player;
            mappings[112] = EntityType.FishingBobber;
        }

        protected override Dictionary<int, EntityType> GetDict()
        {
            return mappings;
        }
    }
}
