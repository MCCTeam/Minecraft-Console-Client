using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    /// <summary>
    /// Defines mappings for pre-1.13 block IDs to post-1.13 Materials
    /// Some block Ids could map to different blocks depending on BlockMeta, here we assumed BlockMeta = 0
    /// Some blocks previously had different IDs depending on state, they have been merged here
    /// Comments correspond to changed material names since previous MCC versions
    /// </summary>
    public class Palette112 : PaletteMapping
    {
        private static Dictionary<int, Material> materials = new Dictionary<int, Material>()
        {
            { 0, Material.Air },
            { 1, Material.Stone },
            { 2, Material.GrassBlock },                      // Grass
            { 3, Material.Dirt },
            { 4, Material.Cobblestone },
            { 5, Material.OakPlanks },                       // Wood:0
            { 6, Material.OakSapling },                      // Sapling:0
            { 7, Material.Bedrock },
            { 8, Material.Water },                           // FlowingWater
            { 9, Material.Water },                           // StationaryWater
            { 10, Material.Lava },                           // FlowingLava
            { 11, Material.Lava },                           // StationaryLava
            { 12, Material.Sand },
            { 13, Material.Gravel },
            { 14, Material.GoldOre },
            { 15, Material.IronOre },
            { 16, Material.CoalOre },
            { 17, Material.OakLog },                         // Log:0
            { 18, Material.OakLeaves },                      // Leaves:0
            { 19, Material.Sponge },
            { 20, Material.Glass },
            { 21, Material.LapisOre },
            { 22, Material.LapisBlock },
            { 23, Material.Dispenser },
            { 24, Material.Sandstone },
            { 25, Material.NoteBlock },
            { 26, Material.RedBed },                         // Bed:0
            { 27, Material.PoweredRail },
            { 28, Material.DetectorRail },
            { 29, Material.StickyPiston },                   // PistonStickyBase
            { 30, Material.Cobweb },                         // Web
            { 31, Material.Grass },                          // LongGrass
            { 32, Material.DeadBush },
            { 33, Material.Piston },                         // PistonBase
            { 34, Material.PistonHead },                     // PistonExtension
            { 35, Material.WhiteWool },                      // Wool:0
            { 36, Material.MovingPiston },                   // PistonMovingPiece
            { 37, Material.Dandelion },                      // YellowFlower
            { 38, Material.Poppy },                          // RedRose
            { 39, Material.BrownMushroom },
            { 40, Material.RedMushroom },
            { 41, Material.GoldBlock },
            { 42, Material.IronBlock },
            { 43, Material.StoneSlab },                      // DoubleStep
            { 44, Material.StoneSlab },                      // Step
            { 45, Material.Bricks },                         // Brick
            { 46, Material.Tnt },
            { 47, Material.Bookshelf },
            { 48, Material.MossyCobblestone },
            { 49, Material.Obsidian },
            { 50, Material.Torch },
            { 51, Material.Fire },
            { 52, Material.Spawner },                        // MobSpawner
            { 53, Material.OakStairs },                      // WoodStairs:0
            { 54, Material.Chest },
            { 55, Material.RedstoneWire },
            { 56, Material.DiamondOre },
            { 57, Material.DiamondBlock },
            { 58, Material.CraftingTable },                  // Workbench
            { 59, Material.Wheat },                          // Crops
            { 60, Material.Farmland },                       // Soil
            { 61, Material.Furnace },                        // Furnace
            { 62, Material.Furnace },                        // BurningFurnace
            { 63, Material.OakWallSign },                    // SignPost
            { 64, Material.OakDoor },                        // WoodenDoor:0
            { 65, Material.Ladder },
            { 66, Material.Rail },                           // Rails
            { 67, Material.CobblestoneStairs },
            { 68, Material.OakWallSign },                    // WallSign
            { 69, Material.Lever },
            { 70, Material.StonePressurePlate },             // StonePlate
            { 71, Material.IronDoor },                       // IronDoorBlock
            { 72, Material.OakPressurePlate },               // WoodPlate:0
            { 73, Material.RedstoneOre },                    // RedstoneOre
            { 74, Material.RedstoneOre },                    // GlowingRedstoneOre
            { 75, Material.RedstoneTorch },                  // RedstoneTorchOff
            { 76, Material.RedstoneTorch },                  // RedstoneTorchOn 
            { 77, Material.StoneButton },
            { 78, Material.Snow },
            { 79, Material.Ice },
            { 80, Material.SnowBlock },
            { 81, Material.Cactus },
            { 82, Material.Clay },
            { 83, Material.SugarCane },                      // SugarCaneBlock
            { 84, Material.Jukebox },
            { 85, Material.OakFence },                       // Fence:0
            { 86, Material.Pumpkin },
            { 87, Material.Netherrack },
            { 88, Material.SoulSand },
            { 89, Material.Glowstone },
            { 90, Material.NetherPortal },                   // Portal
            { 91, Material.JackOLantern },
            { 92, Material.Cake },                           // CakeBlock
            { 93, Material.Repeater },                       // DiodeBlockOff
            { 94, Material.Repeater },                       // DiodeBlockOn
            { 95, Material.WhiteStainedGlass },              // StainedGlass:0
            { 96, Material.OakTrapdoor },                    // TrapDoor
            { 97, Material.InfestedStone },                  // MonsterEggs:0
            { 98, Material.StoneBricks },                    // SmoothBrick
            { 99, Material.BrownMushroomBlock },             // HugeMushroom1
            { 100, Material.BrownMushroomBlock },            // HugeMushroom2
            { 101, Material.IronBars },                      // IronFence
            { 102, Material.GlassPane },                     // ThinGlass
            { 103, Material.Melon },                         // MelonBlock
            { 104, Material.PumpkinStem },
            { 105, Material.MelonStem },
            { 106, Material.Vine },
            { 107, Material.OakFenceGate },                  // FenceGate:0
            { 108, Material.BrickStairs },
            { 109, Material.StoneBrickStairs },              // SmoothStairs
            { 110, Material.Mycelium },                      // Mycel
            { 111, Material.LilyPad },                       // WaterLily
            { 112, Material.NetherBricks},                   // NetherBrick
            { 113, Material.NetherBrickFence },              // NetherFence
            { 114, Material.NetherBrickStairs },
            { 115, Material.NetherWart },                    // NetherWarts
            { 116, Material.EnchantingTable },               // EnchantmentTable
            { 117, Material.BrewingStand },
            { 118, Material.Cauldron },
            { 119, Material.EndPortal },                     // EnderPortal
            { 120, Material.EndPortalFrame },                // EnderPortalFrame
            { 121, Material.EndStone },                      // EnderStone
            { 122, Material.DragonEgg },
            { 123, Material.RedstoneLamp },                  // RedstoneLampOff
            { 124, Material.RedstoneLamp },                  // RedstoneLampOn
            { 125, Material.OakSlab },                       // WoodDoubleStep:0
            { 126, Material.OakSlab },                       // WoodStep
            { 127, Material.Cocoa },
            { 128, Material.SandstoneStairs },
            { 129, Material.EmeraldOre },
            { 130, Material.EnderChest },
            { 131, Material.TripwireHook },
            { 132, Material.Tripwire },
            { 133, Material.EmeraldBlock },
            { 134, Material.SpruceStairs },                  // SpruceWoodStairs
            { 135, Material.BirchStairs },                   // BirchWoodStairs
            { 136, Material.JungleStairs },                  // JungleWoodStairs
            { 137, Material.CommandBlock },                  // Command
            { 138, Material.Beacon },
            { 139, Material.CobblestoneWall },               // CobbleWall
            { 140, Material.FlowerPot },
            { 141, Material.Carrots },                       // Carrot
            { 142, Material.Potatoes },                      // Potato
            { 143, Material.OakButton },                     // WoodButton
            { 144, Material.SkeletonSkull },                 // Skull:0
            { 145, Material.Anvil },
            { 146, Material.TrappedChest },
            { 147, Material.LightWeightedPressurePlate },    // GoldPlate
            { 148, Material.HeavyWeightedPressurePlate },    // IronPlate
            { 149, Material.Comparator },                    // RedstoneComparatorOff
            { 150, Material.Comparator },                    // RedstoneComparatorOn
            { 151, Material.DaylightDetector },
            { 152, Material.RedstoneBlock },
            { 153, Material.QuartzBlock },                   // QuartzOre
            { 154, Material.Hopper },
            { 155, Material.QuartzBlock },
            { 156, Material.QuartzStairs },
            { 157, Material.ActivatorRail },
            { 158, Material.Dropper },
            { 159, Material.WhiteConcrete },                 // StainedClay:0
            { 160, Material.WhiteStainedGlassPane },         // StainedGlassPane:0
            { 161, Material.OakLeaves },                     // Leaves2:0
            { 162, Material.OakLog },                        // Log2:0
            { 163, Material.AcaciaStairs },
            { 164, Material.DarkOakStairs },
            { 170, Material.HayBlock },
            { 171, Material.WhiteCarpet },                   // Carpet:0
            { 172, Material.WhiteConcrete },                 // HardClay
            { 173, Material.CoalBlock },
            { 174, Material.PackedIce },
            { 175, Material.TallGrass },                     // DoublePlant
        };

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }

        public override bool IdHasMetadata
        {
            get
            {
                return true;
            }
        }
    }
}
