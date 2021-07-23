﻿using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using System.Collections.Generic;

namespace MinecraftClient.ChatBots
{
    class Material2Tool
    {
        // Made with the following ressources: https://minecraft.fandom.com/wiki/Breaking
        // Sorted in alphabetical order.
        // Minable by Any Pickaxe.
        private readonly List<Material> pickaxe_tier0 = new List<Material>(new Material[]
        {
                Material.ActivatorRail,
                Material.Andesite,
                Material.AndesiteSlab,
                Material.AndesiteStairs,
                Material.AndesiteWall,
                Material.Anvil,
                Material.Basalt,
                Material.Bell,
                Material.BlackConcrete,
                Material.BlackGlazedTerracotta,
                Material.BlackShulkerBox,
                Material.BlackTerracotta,
                Material.Blackstone,
                Material.BlackstoneSlab,
                Material.BlackstoneStairs,
                Material.BlackstoneWall,
                Material.BlastFurnace,
                Material.BlueConcrete,
                Material.BlueGlazedTerracotta,
                Material.BlueIce,
                Material.BlueShulkerBox,
                Material.BlueTerracotta,
                Material.BoneBlock,
                Material.BrewingStand,
                Material.BrickSlab,
                Material.BrickStairs,
                Material.BrickWall,
                Material.Bricks,
                Material.BrownConcrete,
                Material.BrownGlazedTerracotta,
                Material.BrownShulkerBox,
                Material.BrownTerracotta,
                Material.Cauldron,
                Material.Chain,
                Material.ChippedAnvil,
                Material.ChiseledNetherBricks,
                Material.ChiseledPolishedBlackstone,
                Material.ChiseledQuartzBlock,
                Material.ChiseledRedSandstone,
                Material.ChiseledSandstone,
                Material.ChiseledStoneBricks,
                Material.CoalBlock,
                Material.CoalOre,
                Material.Cobblestone,
                Material.CobblestoneSlab,
                Material.CobblestoneStairs,
                Material.CobblestoneWall,
                Material.Conduit,
                Material.CrackedNetherBricks,
                Material.CrackedPolishedBlackstoneBricks,
                Material.CrackedStoneBricks,
                Material.CrimsonNylium,
                Material.CutRedSandstone,
                Material.CutRedSandstoneSlab,
                Material.CutSandstone,
                Material.CutSandstoneSlab,
                Material.CyanConcrete,
                Material.CyanGlazedTerracotta,
                Material.CyanShulkerBox,
                Material.CyanTerracotta,
                Material.DamagedAnvil,
                Material.DarkPrismarine,
                Material.DarkPrismarineSlab,
                Material.DarkPrismarineStairs,
                Material.DetectorRail,
                Material.Diorite,
                Material.DioriteSlab,
                Material.DioriteStairs,
                Material.DioriteWall,
                Material.Dispenser,
                Material.Dropper,
                Material.EnchantingTable,
                Material.EndRod,
                Material.EndStone,
                Material.EndStoneBrickSlab,
                Material.EndStoneBrickStairs,
                Material.EndStoneBrickWall,
                Material.EndStoneBricks,
                Material.EnderChest,
                Material.FrostedIce,
                Material.Furnace,
                Material.GildedBlackstone,
                Material.Glowstone,
                Material.Granite,
                Material.GraniteSlab,
                Material.GraniteStairs,
                Material.GraniteWall,
                Material.GrayConcrete,
                Material.GrayGlazedTerracotta,
                Material.GrayShulkerBox,
                Material.GrayTerracotta,
                Material.GreenConcrete,
                Material.GreenGlazedTerracotta,
                Material.GreenShulkerBox,
                Material.GreenTerracotta,
                Material.Grindstone,
                Material.HeavyWeightedPressurePlate,
                Material.Hopper,
                Material.Ice,
                Material.IronBars,
                Material.IronDoor,
                Material.IronTrapdoor,
                Material.Lantern,
                Material.LightBlueConcrete,
                Material.LightBlueGlazedTerracotta,
                Material.LightBlueShulkerBox,
                Material.LightBlueTerracotta,
                Material.LightGrayConcrete,
                Material.LightGrayGlazedTerracotta,
                Material.LightGrayShulkerBox,
                Material.LightGrayTerracotta,
                Material.LightWeightedPressurePlate,
                Material.LimeConcrete,
                Material.LimeGlazedTerracotta,
                Material.LimeShulkerBox,
                Material.LimeTerracotta,
                Material.Lodestone,
                Material.MagentaConcrete,
                Material.MagentaGlazedTerracotta,
                Material.MagentaShulkerBox,
                Material.MagentaTerracotta,
                Material.MagmaBlock,
                Material.MossyCobblestone,
                Material.MossyCobblestoneSlab,
                Material.MossyCobblestoneStairs,
                Material.MossyCobblestoneWall,
                Material.MossyStoneBrickSlab,
                Material.MossyStoneBrickStairs,
                Material.MossyStoneBrickWall,
                Material.MossyStoneBricks,
                Material.NetherBrickFence,
                Material.NetherBrickSlab,
                Material.NetherBrickStairs,
                Material.NetherBrickWall,
                Material.NetherBricks,
                Material.NetherGoldOre,
                Material.NetherQuartzOre,
                Material.Netherrack,
                Material.Observer,
                Material.OrangeConcrete,
                Material.OrangeGlazedTerracotta,
                Material.OrangeShulkerBox,
                Material.OrangeTerracotta,
                Material.PackedIce,
                Material.PetrifiedOakSlab,
                Material.PinkConcrete,
                Material.PinkGlazedTerracotta,
                Material.PinkShulkerBox,
                Material.PinkTerracotta,
                Material.Piston,
                Material.PolishedAndesite,
                Material.PolishedAndesiteSlab,
                Material.PolishedAndesiteStairs,
                Material.PolishedBasalt,
                Material.PolishedBlackstone,
                Material.PolishedBlackstoneBrickSlab,
                Material.PolishedBlackstoneBrickStairs,
                Material.PolishedBlackstoneBrickWall,
                Material.PolishedBlackstoneBricks,
                Material.PolishedBlackstoneButton,
                Material.PolishedBlackstonePressurePlate,
                Material.PolishedBlackstoneSlab,
                Material.PolishedBlackstoneStairs,
                Material.PolishedBlackstoneWall,
                Material.PolishedDiorite,
                Material.PolishedDioriteSlab,
                Material.PolishedDioriteStairs,
                Material.PolishedGranite,
                Material.PolishedGraniteSlab,
                Material.PolishedGraniteStairs,
                Material.PoweredRail,
                Material.Prismarine,
                Material.PrismarineBrickSlab,
                Material.PrismarineBrickStairs,
                Material.PrismarineBricks,
                Material.PrismarineSlab,
                Material.PrismarineStairs,
                Material.PrismarineWall,
                Material.PurpleConcrete,
                Material.PurpleGlazedTerracotta,
                Material.PurpleShulkerBox,
                Material.PurpleTerracotta,
                Material.PurpurBlock,
                Material.PurpurPillar,
                Material.PurpurSlab,
                Material.PurpurStairs,
                Material.QuartzBlock,
                Material.QuartzBricks,
                Material.QuartzPillar,
                Material.QuartzSlab,
                Material.QuartzStairs,
                Material.Rail,
                Material.RedConcrete,
                Material.RedGlazedTerracotta,
                Material.RedNetherBrickSlab,
                Material.RedNetherBrickStairs,
                Material.RedNetherBrickWall,
                Material.RedNetherBricks,
                Material.RedSandstone,
                Material.RedSandstoneSlab,
                Material.RedSandstoneStairs,
                Material.RedSandstoneWall,
                Material.RedShulkerBox,
                Material.RedTerracotta,
                Material.RedstoneBlock,
                Material.Sandstone,
                Material.SandstoneSlab,
                Material.SandstoneStairs,
                Material.SandstoneWall,
                Material.ShulkerBox,
                Material.Smoker,
                Material.SmoothQuartz,
                Material.SmoothQuartzSlab,
                Material.SmoothQuartzStairs,
                Material.SmoothRedSandstone,
                Material.SmoothRedSandstoneSlab,
                Material.SmoothRedSandstoneStairs,
                Material.SmoothSandstone,
                Material.SmoothSandstoneSlab,
                Material.SmoothSandstoneStairs,
                Material.SmoothStone,
                Material.SmoothStoneSlab,
                Material.Spawner,
                Material.StickyPiston,
                Material.Stone,
                Material.StoneBrickSlab,
                Material.StoneBrickStairs,
                Material.StoneBrickWall,
                Material.StoneBricks,
                Material.StoneButton,
                Material.StonePressurePlate,
                Material.StoneSlab,
                Material.StoneStairs,
                Material.Stonecutter,
                Material.Terracotta,
                Material.WarpedNylium,
                Material.WhiteConcrete,
                Material.WhiteGlazedTerracotta,
                Material.WhiteShulkerBox,
                Material.WhiteTerracotta,
                Material.YellowConcrete,
                Material.YellowGlazedTerracotta,
                Material.YellowShulkerBox,
                Material.YellowTerracotta
        });
        // Minable by Stone, iron, diamond, netherite.
        private readonly List<Material> pickaxe_tier1 = new List<Material>(new Material[]
        {
                Material.IronBlock,
                Material.IronOre,
                Material.LapisBlock,
                Material.LapisOre,
                Material.Terracotta,
        });
        // Minable by Iron, diamond, netherite.
        private readonly List<Material> pickaxe_tier2 = new List<Material>(new Material[]
        {
                Material.DiamondBlock,
                Material.DiamondOre,
                Material.EmeraldBlock,
                Material.EmeraldOre,
                Material.GoldBlock,
                Material.GoldOre,
                Material.RedstoneOre,
        });
        // Minable by Diamond, Netherite.
        private readonly List<Material> pickaxe_tier3 = new List<Material>(new Material[]
        {
                Material.AncientDebris,
                Material.CryingObsidian,
                Material.NetheriteBlock,
                Material.Obsidian,
                Material.RespawnAnchor
        });

        // Every shovel can mine every block (speed difference).
        private readonly List<Material> shovel = new List<Material>(new Material[]
        {
                Material.BlackConcretePowder,
                Material.BlueConcretePowder,
                Material.BrownConcretePowder,
                Material.Clay,
                Material.CoarseDirt,
                Material.CyanConcretePowder,
                Material.Dirt,
                Material.Farmland,
                Material.Grass,
                Material.GrassBlock,
                Material.GrassPath,
                Material.Gravel,
                Material.GrayConcretePowder,
                Material.GreenConcretePowder,
                Material.LightBlueConcretePowder,
                Material.LightGrayConcretePowder,
                Material.LimeConcretePowder,
                Material.MagentaConcretePowder,
                Material.Mycelium,
                Material.OrangeConcretePowder,
                Material.PinkConcretePowder,
                Material.Podzol,
                Material.PrismarineSlab,
                Material.PurpleConcretePowder,
                Material.RedConcretePowder,
                Material.RedSand,
                Material.Sand,
                Material.Snow,
                Material.SnowBlock,
                Material.SoulSand,
                Material.SoulSoil,
                Material.WhiteConcretePowder,
                Material.YellowConcretePowder
        });
        // Every axe can mine every block (speed difference).
        private readonly List<Material> axe = new List<Material>(new Material[]
        {
                Material.AcaciaButton,
                Material.AcaciaDoor,
                Material.AcaciaFence,
                Material.AcaciaFenceGate,
                Material.AcaciaLog,
                Material.AcaciaPlanks,
                Material.AcaciaPressurePlate,
                Material.AcaciaSign,
                Material.AcaciaSlab,
                Material.AcaciaStairs,
                Material.AcaciaTrapdoor,
                Material.AcaciaWallSign,
                Material.AcaciaWood,
                Material.Barrel,
                Material.BeeNest,
                Material.Beehive,
                Material.BirchButton,
                Material.BirchDoor,
                Material.BirchFence,
                Material.BirchFenceGate,
                Material.BirchLog,
                Material.BirchPlanks,
                Material.BirchPressurePlate,
                Material.BirchSign,
                Material.BirchSlab,
                Material.BirchStairs,
                Material.BirchTrapdoor,
                Material.BirchWallSign,
                Material.BirchWood,
                Material.BlackBanner,
                Material.BlackWallBanner,
                Material.BlueBanner,
                Material.BlueWallBanner,
                Material.Bookshelf,
                Material.BrownBanner,
                Material.BrownMushroomBlock,
                Material.BrownWallBanner,
                Material.Campfire,
                Material.CartographyTable,
                Material.Chest,
                Material.Cocoa,
                Material.Composter,
                Material.CraftingTable,
                Material.CrimsonButton,
                Material.CrimsonDoor,
                Material.CrimsonFence,
                Material.CrimsonFenceGate,
                Material.CrimsonHyphae,
                Material.CrimsonPlanks,
                Material.CrimsonPressurePlate,
                Material.CrimsonSign,
                Material.CrimsonSlab,
                Material.CrimsonStairs,
                Material.CrimsonStem,
                Material.CrimsonTrapdoor,
                Material.CrimsonWallSign,
                Material.CyanBanner,
                Material.CyanWallBanner,
                Material.DarkOakButton,
                Material.DarkOakDoor,
                Material.DarkOakFence,
                Material.DarkOakFenceGate,
                Material.DarkOakLog,
                Material.DarkOakPlanks,
                Material.DarkOakPressurePlate,
                Material.DarkOakSign,
                Material.DarkOakSlab,
                Material.DarkOakStairs,
                Material.DarkOakTrapdoor,
                Material.DarkOakWallSign,
                Material.DarkOakWood,
                Material.DaylightDetector,
                Material.FletchingTable,
                Material.GrayBanner,
                Material.GrayWallBanner,
                Material.GreenBanner,
                Material.GreenWallBanner,
                Material.JackOLantern,
                Material.Jukebox,
                Material.JungleButton,
                Material.JungleDoor,
                Material.JungleFence,
                Material.JungleFenceGate,
                Material.JungleLog,
                Material.JunglePlanks,
                Material.JunglePressurePlate,
                Material.JungleSign,
                Material.JungleSlab,
                Material.JungleStairs,
                Material.JungleTrapdoor,
                Material.JungleWallSign,
                Material.JungleWood,
                Material.Ladder,
                Material.Lectern,
                Material.LightBlueBanner,
                Material.LightBlueWallBanner,
                Material.LightGrayBanner,
                Material.LightGrayWallBanner,
                Material.LimeBanner,
                Material.LimeWallBanner,
                Material.Loom,
                Material.MagentaBanner,
                Material.MagentaWallBanner,
                Material.Melon,
                Material.MushroomStem,
                Material.NoteBlock,
                Material.OakButton,
                Material.OakDoor,
                Material.OakFence,
                Material.OakFenceGate,
                Material.OakLog,
                Material.OakPlanks,
                Material.OakPressurePlate,
                Material.OakSign,
                Material.OakSlab,
                Material.OakStairs,
                Material.OakTrapdoor,
                Material.OakWallSign,
                Material.OakWood,
                Material.OrangeBanner,
                Material.OrangeWallBanner,
                Material.PinkBanner,
                Material.PinkWallBanner,
                Material.Pumpkin,
                Material.PurpleBanner,
                Material.PurpleWallBanner,
                Material.RedBanner,
                Material.RedMushroomBlock,
                Material.RedWallBanner,
                Material.SmithingTable,
                Material.SoulCampfire,
                Material.SpruceButton,
                Material.SpruceDoor,
                Material.SpruceFence,
                Material.SpruceFenceGate,
                Material.SpruceLog,
                Material.SprucePlanks,
                Material.SprucePressurePlate,
                Material.SpruceSign,
                Material.SpruceSlab,
                Material.SpruceStairs,
                Material.SpruceTrapdoor,
                Material.SpruceWallSign,
                Material.SpruceWood,
                Material.StrippedAcaciaLog,
                Material.StrippedAcaciaWood,
                Material.StrippedBirchLog,
                Material.StrippedBirchWood,
                Material.StrippedCrimsonHyphae,
                Material.StrippedCrimsonStem,
                Material.StrippedDarkOakLog,
                Material.StrippedDarkOakWood,
                Material.StrippedDarkOakWood,
                Material.StrippedJungleLog,
                Material.StrippedJungleWood,
                Material.StrippedOakLog,
                Material.StrippedOakWood,
                Material.StrippedSpruceLog,
                Material.StrippedSpruceWood,
                Material.StrippedWarpedHyphae,
                Material.StrippedWarpedStem,
                Material.TrappedChest,
                Material.Vine,
                Material.WarpedButton,
                Material.WarpedDoor,
                Material.WarpedFence,
                Material.WarpedFenceGate,
                Material.WarpedHyphae,
                Material.WarpedPlanks,
                Material.WarpedPressurePlate,
                Material.WarpedSign,
                Material.WarpedSlab,
                Material.WarpedStairs,
                Material.WarpedStem,
                Material.WarpedTrapdoor,
                Material.WarpedWallSign,
                Material.WhiteBanner,
                Material.WhiteWallBanner,
                Material.YellowBanner,
                Material.YellowWallBanner
        });
        // Every block a shear can mine.
        private readonly List<Material> shears = new List<Material>(new Material[]
        {
                Material.AcaciaLeaves,
                Material.BirchLeaves,
                Material.BlackWool,
                Material.BlueWool,
                Material.BrownWool,
                Material.Cobweb,
                Material.CyanWool,
                Material.DarkOakLeaves,
                Material.GrayWool,
                Material.GreenWool,
                Material.JungleLeaves,
                Material.LightBlueWool,
                Material.LightGrayWool,
                Material.LimeWool,
                Material.MagentaWool,
                Material.OakLeaves,
                Material.OrangeWool,
                Material.PinkWool,
                Material.PurpleWool,
                Material.RedWool,
                Material.SpruceLeaves,
                Material.WhiteWool,
                Material.YellowWool,
        });
        // Every block that is mined with a sword.
        private List<Material> sword = new List<Material>(new Material[]
        {
                Material.Bamboo,
                Material.Cobweb,
                Material.InfestedChiseledStoneBricks,
                Material.InfestedCobblestone,
                Material.InfestedCrackedStoneBricks,
                Material.InfestedMossyStoneBricks,
                Material.InfestedStone,
                Material.InfestedStoneBricks,
        });
        // Every block that can be mined with a hoe.
        private readonly List<Material> hoe = new List<Material>(new Material[]
        {
                Material.AcaciaLeaves,
                Material.BirchLeaves,
                Material.DarkOakLeaves,
                Material.HayBlock,
                Material.JungleLeaves,
                Material.NetherWartBlock,
                Material.OakLeaves,
                Material.Shroomlight,
                Material.Sponge,
                Material.SpruceLeaves,
                Material.Target,
                Material.WarpedWartBlock,
                Material.WetSponge,
        });
        // Liquids
        private readonly List<Material> bucket = new List<Material>(new Material[] 
        {
            Material.Lava, 
            Material.Water
        });

        // Unbreakable Blocks
        private readonly List<Material> unbreakable = new List<Material>(new Material[]
        {
                Material.CommandBlock,
                Material.ChainCommandBlock,
                Material.RepeatingCommandBlock,
                Material.StructureBlock,
                Material.Jigsaw,
                Material.StructureVoid,
                Material.Barrier,
                Material.Bedrock,
                Material.EndGateway,
                Material.EndPortal,
                Material.EndPortalFrame,
                Material.NetherPortal,
                Material.Air,
                Material.BubbleColumn,
        });

        /// <summary>
        /// Evaluates the right tool for the job
        /// </summary>
        /// <param name="block">Enter the Material of a block</param>
        /// <returns>Returns a list of tools that can be used, best to worst</returns>
        public ItemType[] GetCorrectToolForBlock(Material block)
        {
            if (pickaxe_tier0.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.NetheritePickaxe,
                    ItemType.DiamondPickaxe,
                    ItemType.IronPickaxe,
                    ItemType.GoldenPickaxe,
                    ItemType.StonePickaxe,
                    ItemType.WoodenPickaxe,
                };
            }
            else if (pickaxe_tier1.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.NetheritePickaxe,
                    ItemType.DiamondPickaxe,
                    ItemType.IronPickaxe,
                    ItemType.GoldenPickaxe,
                    ItemType.StonePickaxe,
                };
            }
            else if (pickaxe_tier2.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.NetheritePickaxe,
                    ItemType.DiamondPickaxe,
                    ItemType.IronPickaxe,
                };
            }
            else if (pickaxe_tier3.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.NetheritePickaxe,
                    ItemType.DiamondPickaxe,
                };
            }
            else if (shovel.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.NetheriteShovel,
                    ItemType.DiamondShovel,
                    ItemType.IronShovel,
                    ItemType.GoldenShovel,
                    ItemType.StoneShovel,
                    ItemType.WoodenShovel,
                };
            }
            else if (axe.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.NetheriteAxe,
                    ItemType.DiamondAxe,
                    ItemType.IronAxe,
                    ItemType.GoldenAxe,
                    ItemType.StoneAxe,
                    ItemType.WoodenAxe,
                };
            }
            else if (shears.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.Shears,
                };
            }
            else if (sword.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.NetheriteSword,
                    ItemType.DiamondSword,
                    ItemType.IronSword,
                    ItemType.GoldenSword,
                    ItemType.StoneSword,
                    ItemType.WoodenSword,
                };
            }
            else if (hoe.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.NetheriteHoe,
                    ItemType.DiamondHoe,
                    ItemType.IronHoe,
                    ItemType.GoldenHoe,
                    ItemType.StoneHoe,
                    ItemType.WoodenHoe,
                };
            }
            else if (bucket.Contains(block))
            {
                return new ItemType[] 
                {
                    ItemType.Bucket,
                };
            }
            else { return new ItemType[0]; }
        }

        public bool IsUnbreakable(Material block) { return unbreakable.Contains(block); }
    }
}
