namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents Minecraft Materials
    /// </summary>
    /// <remarks>
    /// Mostly ported from CraftBukkit's Material class
    /// </remarks>
    /// <see href="https://github.com/Bukkit/Bukkit/blob/master/src/main/java/org/bukkit/Material.java"/>
    public enum Material
    {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Dirt = 3,
        Cobblestone = 4,
        Wood = 5,
        Sapling = 6,
        Bedrock = 7,
        Water = 8,
        StationaryWater = 9,
        Lava = 10,
        StationaryLava = 11,
        Sand = 12,
        Gravel = 13,
        GoldOre = 14,
        IronOre = 15,
        CoalOre = 16,
        Log = 17,
        Leaves = 18,
        Sponge = 19,
        Glass = 20,
        LapisOre = 21,
        LapisBlock = 22,
        Dispenser = 23,
        Sandstone = 24,
        NoteBlock = 25,
        BedBlock = 26,
        PoweredRail = 27,
        DetectorRail = 28,
        PistonStickyBase = 29,
        Web = 30,
        LongGrass = 31,
        DeadBush = 32,
        PistonBase = 33,
        PistonExtension = 34,
        Wool = 35,
        PistonMovingPiece = 36,
        YellowFlower = 37,
        RedRose = 38,
        BrownMushroom = 39,
        RedMushroom = 40,
        GoldBlock = 41,
        IronBlock = 42,
        DoubleStep = 43,
        Step = 44,
        Brick = 45,
        Tnt = 46,
        Bookshelf = 47,
        MossyCobblestone = 48,
        Obsidian = 49,
        Torch = 50,
        Fire = 51,
        MobSpawner = 52,
        WoodStairs = 53,
        Chest = 54,
        RedstoneWire = 55,
        DiamondOre = 56,
        DiamondBlock = 57,
        Workbench = 58,
        Crops = 59,
        Soil = 60,
        Furnace = 61,
        BurningFurnace = 62,
        SignPost = 63,
        WoodenDoor = 64,
        Ladder = 65,
        Rails = 66,
        CobblestoneStairs = 67,
        WallSign = 68,
        Lever = 69,
        StonePlate = 70,
        IronDoorBlock = 71,
        WoodPlate = 72,
        RedstoneOre = 73,
        GlowingRedstoneOre = 74,
        RedstoneTorchOff = 75,
        RedstoneTorchOn = 76,
        StoneButton = 77,
        Snow = 78,
        Ice = 79,
        SnowBlock = 80,
        Cactus = 81,
        Clay = 82,
        SugarCaneBlock = 83,
        Jukebox = 84,
        Fence = 85,
        Pumpkin = 86,
        Netherrack = 87,
        SoulSand = 88,
        Glowstone = 89,
        Portal = 90,
        JackOLantern = 91,
        CakeBlock = 92,
        DiodeBlockOff = 93,
        DiodeBlockOn = 94,
        StainedGlass = 95,
        TrapDoor = 96,
        MonsterEggs = 97,
        SmoothBrick = 98,
        HugeMushroom1 = 99,
        HugeMushroom2 = 100,
        IronFence = 101,
        ThinGlass = 102,
        MelonBlock = 103,
        PumpkinStem = 104,
        MelonStem = 105,
        Vine = 106,
        FenceGate = 107,
        BrickStairs = 108,
        SmoothStairs = 109,
        Mycel = 110,
        WaterLily = 111,
        NetherBrick = 112,
        NetherFence = 113,
        NetherBrickStairs = 114,
        NetherWarts = 115,
        EnchantmentTable = 116,
        BrewingStand = 117,
        Cauldron = 118,
        EnderPortal = 119,
        EnderPortalFrame = 120,
        EnderStone = 121,
        DragonEgg = 122,
        RedstoneLampOff = 123,
        RedstoneLampOn = 124,
        WoodDoubleStep = 125,
        WoodStep = 126,
        Cocoa = 127,
        SandstoneStairs = 128,
        EmeraldOre = 129,
        EnderChest = 130,
        TripwireHook = 131,
        Tripwire = 132,
        EmeraldBlock = 133,
        SpruceWoodStairs = 134,
        BirchWoodStairs = 135,
        JungleWoodStairs = 136,
        Command = 137,
        Beacon = 138,
        CobbleWall = 139,
        FlowerPot = 140,
        Carrot = 141,
        Potato = 142,
        WoodButton = 143,
        Skull = 144,
        Anvil = 145,
        TrappedChest = 146,
        GoldPlate = 147,
        IronPlate = 148,
        RedstoneComparatorOff = 149,
        RedstoneComparatorOn = 150,
        DaylightDetector = 151,
        RedstoneBlock = 152,
        QuartzOre = 153,
        Hopper = 154,
        QuartzBlock = 155,
        QuartzStairs = 156,
        ActivatorRail = 157,
        Dropper = 158,
        StainedClay = 159,
        StainedGlassPane = 160,
        Leaves2 = 161,
        Log2 = 162,
        AcaciaStairs = 163,
        DarkOakStairs = 164,
        HayBlock = 170,
        Carpet = 171,
        HardClay = 172,
        CoalBlock = 173,
        PackedIce = 174,
        DoublePlant = 175
    }

    /// <summary>
    /// Defines extension methods for the Material enumeration
    /// </summary>
    public static class MaterialExtensions
    {
        /// <summary>
        /// Check if the player cannot pass through the specified material
        /// </summary>
        /// <param name="m">Material to test</param>
        /// <returns>True if the material is harmful</returns>
        public static bool IsSolid(this Material m)
        {
            switch (m)
            {
                case Material.Stone:
                case Material.Grass:
                case Material.Dirt:
                case Material.Cobblestone:
                case Material.Wood:
                case Material.Bedrock:
                case Material.Sand:
                case Material.Gravel:
                case Material.GoldOre:
                case Material.IronOre:
                case Material.CoalOre:
                case Material.Log:
                case Material.Leaves:
                case Material.Sponge:
                case Material.Glass:
                case Material.LapisOre:
                case Material.LapisBlock:
                case Material.Dispenser:
                case Material.Sandstone:
                case Material.NoteBlock:
                case Material.BedBlock:
                case Material.PistonStickyBase:
                case Material.PistonBase:
                case Material.PistonExtension:
                case Material.Wool:
                case Material.PistonMovingPiece:
                case Material.GoldBlock:
                case Material.IronBlock:
                case Material.DoubleStep:
                case Material.Step:
                case Material.Brick:
                case Material.Tnt:
                case Material.Bookshelf:
                case Material.MossyCobblestone:
                case Material.Obsidian:
                case Material.MobSpawner:
                case Material.WoodStairs:
                case Material.Chest:
                case Material.DiamondOre:
                case Material.DiamondBlock:
                case Material.Workbench:
                case Material.Soil:
                case Material.Furnace:
                case Material.BurningFurnace:
                case Material.SignPost:
                case Material.WoodenDoor:
                case Material.CobblestoneStairs:
                case Material.WallSign:
                case Material.StonePlate:
                case Material.IronDoorBlock:
                case Material.WoodPlate:
                case Material.RedstoneOre:
                case Material.GlowingRedstoneOre:
                case Material.Ice:
                case Material.SnowBlock:
                case Material.Cactus:
                case Material.Clay:
                case Material.Jukebox:
                case Material.Fence:
                case Material.Pumpkin:
                case Material.Netherrack:
                case Material.SoulSand:
                case Material.Glowstone:
                case Material.JackOLantern:
                case Material.CakeBlock:
                case Material.StainedGlass:
                case Material.TrapDoor:
                case Material.MonsterEggs:
                case Material.SmoothBrick:
                case Material.HugeMushroom1:
                case Material.HugeMushroom2:
                case Material.IronFence:
                case Material.ThinGlass:
                case Material.MelonBlock:
                case Material.FenceGate:
                case Material.BrickStairs:
                case Material.SmoothStairs:
                case Material.Mycel:
                case Material.NetherBrick:
                case Material.NetherFence:
                case Material.NetherBrickStairs:
                case Material.EnchantmentTable:
                case Material.BrewingStand:
                case Material.Cauldron:
                case Material.EnderPortalFrame:
                case Material.EnderStone:
                case Material.DragonEgg:
                case Material.RedstoneLampOff:
                case Material.RedstoneLampOn:
                case Material.WoodDoubleStep:
                case Material.WoodStep:
                case Material.SandstoneStairs:
                case Material.EmeraldOre:
                case Material.EnderChest:
                case Material.EmeraldBlock:
                case Material.SpruceWoodStairs:
                case Material.BirchWoodStairs:
                case Material.JungleWoodStairs:
                case Material.Command:
                case Material.Beacon:
                case Material.CobbleWall:
                case Material.Anvil:
                case Material.TrappedChest:
                case Material.GoldPlate:
                case Material.IronPlate:
                case Material.DaylightDetector:
                case Material.RedstoneBlock:
                case Material.QuartzOre:
                case Material.Hopper:
                case Material.QuartzBlock:
                case Material.QuartzStairs:
                case Material.Dropper:
                case Material.StainedClay:
                case Material.HayBlock:
                case Material.HardClay:
                case Material.CoalBlock:
                case Material.StainedGlassPane:
                case Material.Leaves2:
                case Material.Log2:
                case Material.AcaciaStairs:
                case Material.DarkOakStairs:
                case Material.PackedIce:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if contact with the provided material can harm players
        /// </summary>
        /// <param name="m">Material to test</param>
        /// <returns>True if the material is harmful</returns>
        public static bool CanHarmPlayers(this Material m)
        {
            switch (m)
            {
                case Material.Fire:
                case Material.Cactus:
                case Material.Lava:
                case Material.StationaryLava:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if the provided material is a liquid a player can swim into
        /// </summary>
        /// <param name="m">Material to test</param>
        /// <returns>True if the material is a liquid</returns>
        public static bool IsLiquid(this Material m)
        {
            switch (m)
            {
                case Material.Water:
                case Material.StationaryWater:
                case Material.Lava:
                case Material.StationaryLava:
                    return true;
                default:
                    return false;
            }
        }
    }
}
