﻿namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents Minecraft Entity Types
    /// </summary>
    /// <remarks>
    /// Generated from registries.json using the --generator flag on the client
    /// Typical steps to handle new entity IDs for newer Minecraft versions:
    /// 1. Generate registries.json using data reporting on Vanilla Minecraft (https://wiki.vg/Data_Generators) or download it from: https://github.com/PixiGeko/Minecraft-generated-data
    /// 2. Generate temporary EntityTypeXXX.cs and EntityPaletteXXX.cs using the --generator flag on the client
    /// 3. Perform a diff with existing versions, add missing entries in EntityType.cs and EntityTypeExtensions.cs
    /// 4. If existing entity IDs were not randomized by Mojang, simply add missing entries to the latest existing EntityPaletteXXX.cs
    /// 5. If existing entity IDs were randomized, add a new palette as EntityPaletteXXX.cs into the codebase (worst case)
    /// </remarks>
    public enum EntityType
    {
        Allay,
        AreaEffectCloud,
        ArmorStand,
        Arrow,
        Axolotl,
        Bat,
        Bee,
        Blaze,
        Boat,
        Camel,
        Cat,
        CaveSpider,
        ChestBoat,
        ChestMinecart,
        Chicken,
        Cod,
        CommandBlockMinecart,
        Cow,
        Creeper,
        Dolphin,
        Donkey,
        DragonFireball,
        Drowned,
        Egg,
        ElderGuardian,
        EndCrystal,
        EnderDragon,
        EnderPearl,
        Enderman,
        Endermite,
        Evoker,
        EvokerFangs,
        ExperienceBottle,
        ExperienceOrb,
        EyeOfEnder,
        FallingBlock,
        Fireball,
        FireworkRocket,
        FishingBobber,
        Fox,
        Frog,
        FurnaceMinecart,
        Ghast,
        Giant,
        GlowItemFrame,
        GlowSquid,
        Goat,
        Guardian,
        Hoglin,
        HopperMinecart,
        Horse,
        Husk,
        Illusioner,
        IronGolem,
        Item,
        ItemFrame,
        LeashKnot,
        LightningBolt,
        Llama,
        LlamaSpit,
        MagmaCube,
        Marker,
        Minecart,
        Mooshroom,
        Mule,
        Ocelot,
        Painting,
        Panda,
        Parrot,
        Phantom,
        Pig,
        Piglin,
        PiglinBrute,
        Pillager,
        Player,
        PolarBear,
        Potion,
        Pufferfish,
        Rabbit,
        Ravager,
        Salmon,
        Sheep,
        Shulker,
        ShulkerBullet,
        Silverfish,
        Skeleton,
        SkeletonHorse,
        Slime,
        SmallFireball,
        SnowGolem,
        Snowball,
        SpawnerMinecart,
        SpectralArrow,
        Spider,
        Squid,
        Stray,
        Strider,
        Tadpole,
        Tnt,
        TntMinecart,
        TraderLlama,
        Trident,
        TropicalFish,
        Turtle,
        Vex,
        Villager,
        Vindicator,
        WanderingTrader,
        Warden,
        Witch,
        Wither,
        WitherSkeleton,
        WitherSkull,
        Wolf,
        Zoglin,
        Zombie,
        ZombieHorse,
        ZombieVillager,
        ZombifiedPiglin,
    }
}
