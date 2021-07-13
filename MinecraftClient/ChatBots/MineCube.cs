using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;
using System.Threading;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// A row of blocks that will be mined
    /// </summary>
    public class Row
    {
        private List<Location> blocksInRow;

        public List<Location> BlocksToMine
        {
            get { return blocksInRow; }
        }

        /// <summary>
        /// Initialize a row of blocks
        /// </summary>
        /// <param name="bIL"> Enter a list of blocks </param>
        public Row(List<Location> bIL = null)
        {
            blocksInRow = bIL ?? new List<Location>();
        }
    }

    /// <summary>
    /// Several rows are summarized in a layer
    /// </summary>
    public class Layer
    {
        private List<Row> rowsToMine;

        public List<Row> RowsToMine
        {
            get { return rowsToMine; }
        }

        /// <summary>
        /// Add a new row to this layer
        /// </summary>
        /// <param name="givenRow"> enter a row that should be added </param>
        /// <returns> Index of the last row </returns>
        public int AddRow(Row givenRow = null)
        {
            rowsToMine.Add(givenRow ?? new Row());
            return rowsToMine.Count - 1;
        }

        /// <summary>
        /// Initialize a layer
        /// </summary>
        /// <param name="rTM"> Enter a list of rows </param>
        public Layer(List<Row> rTM = null)
        {
            rowsToMine = rTM ?? new List<Row>();
        }
    }

    /// <summary>
    /// Several layers result in a cube
    /// </summary>
    public class Cube
    {
        private List<Layer> layersToMine;

        public List<Layer> LayersToMine
        {
            get { return layersToMine; }
        }

        /// <summary>
        /// Add a new layer to the cube
        /// </summary>
        /// <param name="givenLayer"> Enter a layer that should be added </param>
        /// <returns> Index of the last layer </returns>
        public int AddLayer(Layer givenLayer = null)
        {
            layersToMine.Add(givenLayer ?? new Layer());
            return layersToMine.Count - 1;
        }

        /// <summary>
        /// Initialize a cube
        /// </summary>
        /// <param name="lTM"> Enter a list of layers </param>
        public Cube(List<Layer> lTM = null)
        {
            layersToMine = lTM ?? new List<Layer>();
        }
    }

    class MineCube : ChatBot
    {
        public override void Initialize()
        {
            if (!GetTerrainEnabled())
            {
                LogToConsole(Translations.Get("extra.terrainandmovement_required"));
                UnloadBot();
                return;
            }
            RegisterChatBotCommand("mine", "Mine a cube from a to b", "/mine x y z OR /mine x1 y1 z1 x2 y2 z2", EvaluateCommand);
        }

        public void Mine(Cube cubeToMine)
        {
            foreach (Layer lay in cubeToMine.LayersToMine)
            {
                foreach (Row r in lay.RowsToMine)
                {
                    foreach (Location loc in r.BlocksToMine)
                    {
                        if (GetHeadLocation(GetCurrentLocation()).Distance(loc) > 5)
                        {
                            // Unable to detect when walking is over and goal is reached.
                            if (MoveToLocation(new Location(loc.X, loc.Y + 1, loc.Z)))
                            {
                                while (GetCurrentLocation().Distance(loc) > 2)
                                {
                                    Thread.Sleep(200);
                                }
                            }
                            // Some blocks might not be reachable, although approximation would be enough
                            // but the client either denies walking or walks to the goal block.
                            else
                            {
                                LogToConsole("Unable to walk to: " + loc.ToString());
                            }
                        }
                        // Unable to check when breaking is over.
                        if (DigBlock(loc))
                        {
                            short i = 0; // Maximum wait time of 10 sec.
                            while (GetWorld().GetBlock(loc).Type != Material.Air && i <= 100)
                            {
                                Thread.Sleep(100);
                                i++;
                            }
                        }
                        else
                        {
                            LogDebugToConsole("Unable to break this block: " + loc.ToString());
                        }
                    }
                }
            }
            LogToConsole("Mining finished.");
        }

        public Cube GetMinableBlocksAsCube(Location startBlock, Location stopBlock)
        {
            LogToConsole("StartPos: " + startBlock.ToString() + " EndPos: " + stopBlock.ToString());

            // Initialize cube to mine.
            Cube cubeToMine = new Cube();

            // Get the distance between start and finish as Vector.
            Location vectorToStopPosition = stopBlock - startBlock;

            // Initialize Iteration process
            int[] iterateX = GetNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.X))).ToArray();
            int[] iterateY = GetNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.Y))).ToArray();
            int[] iterateZ = GetNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.Z))).ToArray();

            LogDebugToConsole("Iterate on X: 0-" + (iterateX.Length - 1).ToString() + " Y: 0-" + (iterateY.Length - 1).ToString() + " Z: 0-" + (iterateZ.Length - 1).ToString());

            // Iterate through all coordinates relative to the start block.
            foreach (int y in iterateY)
            {
                Layer tempLayer = new Layer();
                foreach (int x in iterateX)
                {
                    Row tempRow = new Row();
                    foreach (int z in iterateZ)
                    {
                        Location tempLocation = new Location(Math.Round(startBlock.X + x), Math.Round(startBlock.Y + y), Math.Round(startBlock.Z + z));
                        if (IsMinable(GetWorld().GetBlock(tempLocation).Type))
                        {
                            tempRow.BlocksToMine.Add(tempLocation);
                        }
                    }
                    if (tempRow.BlocksToMine.Count > 0)
                    {
                        tempLayer.AddRow(tempRow);
                    }
                }
                if (tempLayer.RowsToMine.Count > 0)
                {
                    cubeToMine.AddLayer(tempLayer);
                }
            }

            // Remove later ;D
            PrintCubeToConsole(cubeToMine);

            if (Settings.DebugMessages)
            {
                PrintCubeToConsole(cubeToMine);
            }

            return cubeToMine;
        }

        /// <summary>
        /// Get all numbers between from and to.
        /// </summary>
        /// <param name="start">Number to start</param>
        /// <param name="end">Number to stop</param>
        /// <returns>All numbers between the first, including the stop number</returns>
        public List<int> GetNumbersFromTo(int start, int stop)
        {
            List<int> tempList = new List<int>();
            if (start <= stop)
            {
                for (int i = start; i <= stop; i++)
                {
                    tempList.Add(i);
                }
            }
            else
            {
                for (int i = start; i >= stop; i--)
                {
                    tempList.Add(i);
                }
            }
            return tempList;
        }

        public Func<Location, Location> GetHeadLocation = locFeet => new Location(locFeet.X, locFeet.Y + 1, locFeet.Z);

        /// <summary>
        /// Checks whether a material is minable
        /// </summary>
        /// <param name="block">Block that should be checked</param>
        /// <returns>Is block minable</returns>
        private bool IsMinable(Material block)
        {
            return (
                block != Material.Air &&
                block != Material.Bedrock &&
                !block.IsLiquid()
                    );
        }

        private void SelectCorrectSlotInHotbar(ItemType[] tools)
        {
            if (GetInventoryEnabled())
            {
                foreach (ItemType tool in tools)
                {
                    int[] tempArray = GetPlayerInventory().SearchItem(tool);
                    if(tempArray.Length > 0 && tempArray[0] < 10)
                    {
                        ChangeSlot(Convert.ToInt16(tempArray[0]));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Evaluates the right tool for the job
        /// </summary>
        /// <param name="block">Enter the Material of a block</param>
        /// <returns>The right tool cathegory as string</returns>
        public ItemType[] GetCorrectToolForBlock(Material block)
        {
            // Made with the following ressources: https://minecraft.fandom.com/wiki/Breaking
            // Sorted in alphabetical order.

            // Minable by Any Pickaxe.
            List<Material> pickaxe_class0 = new List<Material>(new Material[]
            {
                Material.ActivatorRail,
                Material.Andesite,
                Material.AndesiteSlab,
                Material.AndesiteStairs,
                Material.Anvil,
                Material.Basalt,
                Material.Bell,
                Material.BlackConcrete,
                Material.BlackConcrete,
                Material.BlackGlazedTerracotta,
                Material.BlackShulkerBox,
                Material.BlackShulkerBox,
                Material.BlackShulkerBox,
                Material.BlackTerracotta,
                Material.Blackstone,
                Material.BlackstoneSlab,
                Material.BlackstoneStairs,
                Material.BlastFurnace,
                Material.BlueConcrete,
                Material.BlueConcrete,
                Material.BlueGlazedTerracotta,
                Material.BlueIce,
                Material.BlueShulkerBox,
                Material.BlueShulkerBox,
                Material.BlueShulkerBox,
                Material.BlueTerracotta,
                Material.BrewingStand,
                Material.BrickSlab,
                Material.BrickStairs,
                Material.Bricks,
                Material.BrownConcrete,
                Material.BrownConcrete,
                Material.BrownGlazedTerracotta,
                Material.BrownShulkerBox,
                Material.BrownShulkerBox,
                Material.BrownShulkerBox,
                Material.BrownTerracotta,
                Material.Cauldron,
                Material.Chain,
                Material.ChippedAnvil,
                Material.ChiseledQuartzBlock,
                Material.CoalBlock,
                Material.CoalOre,
                Material.Cobblestone,
                Material.CobblestoneSlab,
                Material.CobblestoneWall,
                Material.Conduit,
                Material.CutRedSandstoneSlab,
                Material.CutSandstoneSlab,
                Material.CyanConcrete,
                Material.CyanConcrete,
                Material.CyanGlazedTerracotta,
                Material.CyanShulkerBox,
                Material.CyanShulkerBox,
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
                Material.Dispenser,
                Material.Dropper,
                Material.EnchantingTable,
                Material.EndStone,
                Material.EndStoneBrickSlab,
                Material.EndStoneBrickStairs,
                Material.EnderChest,
                Material.FrostedIce,
                Material.Furnace,
                Material.Granite,
                Material.GraniteSlab,
                Material.GraniteStairs,
                Material.GrayConcrete,
                Material.GrayConcrete,
                Material.GrayGlazedTerracotta,
                Material.GrayShulkerBox,
                Material.GrayShulkerBox,
                Material.GrayShulkerBox,
                Material.GrayTerracotta,
                Material.GreenConcrete,
                Material.GreenConcrete,
                Material.GreenGlazedTerracotta,
                Material.GreenShulkerBox,
                Material.GreenShulkerBox,
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
                Material.LightBlueConcrete,
                Material.LightBlueGlazedTerracotta,
                Material.LightBlueShulkerBox,
                Material.LightBlueShulkerBox,
                Material.LightBlueShulkerBox,
                Material.LightBlueTerracotta,
                Material.LightGrayConcrete,
                Material.LightGrayConcrete,
                Material.LightGrayGlazedTerracotta,
                Material.LightGrayShulkerBox,
                Material.LightGrayShulkerBox,
                Material.LightGrayShulkerBox,
                Material.LightGrayTerracotta,
                Material.LightWeightedPressurePlate,
                Material.LimeConcrete,
                Material.LimeConcrete,
                Material.LimeGlazedTerracotta,
                Material.LimeShulkerBox,
                Material.LimeShulkerBox,
                Material.LimeShulkerBox,
                Material.LimeTerracotta,
                Material.Lodestone,
                Material.MagentaConcrete,
                Material.MagentaConcrete,
                Material.MagentaGlazedTerracotta,
                Material.MagentaShulkerBox,
                Material.MagentaShulkerBox,
                Material.MagentaShulkerBox,
                Material.MagentaTerracotta,
                Material.MossyCobblestone,
                Material.MossyCobblestoneSlab,
                Material.MossyCobblestoneStairs,
                Material.MossyStoneBrickSlab,
                Material.MossyStoneBrickStairs,
                Material.NetherBrickFence,
                Material.NetherBrickSlab,
                Material.NetherBrickStairs,
                Material.NetherBricks,
                Material.NetherGoldOre,
                Material.NetherQuartzOre,
                Material.Netherrack,
                Material.Observer,
                Material.OrangeConcrete,
                Material.OrangeConcrete,
                Material.OrangeGlazedTerracotta,
                Material.OrangeShulkerBox,
                Material.OrangeShulkerBox,
                Material.OrangeShulkerBox,
                Material.OrangeTerracotta,
                Material.PackedIce,
                Material.PetrifiedOakSlab,
                Material.PinkConcrete,
                Material.PinkConcrete,
                Material.PinkGlazedTerracotta,
                Material.PinkShulkerBox,
                Material.PinkShulkerBox,
                Material.PinkShulkerBox,
                Material.PinkTerracotta,
                Material.Piston,
                Material.PolishedAndesite,
                Material.PolishedAndesiteSlab,
                Material.PolishedAndesiteStairs,
                Material.PolishedBlackstone,
                Material.PolishedBlackstoneBrickSlab,
                Material.PolishedBlackstoneBrickStairs,
                Material.PolishedBlackstoneBricks,
                Material.PolishedBlackstoneSlab,
                Material.PolishedBlackstoneStairs,
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
                Material.PurpleConcrete,
                Material.PurpleConcrete,
                Material.PurpleGlazedTerracotta,
                Material.PurpleShulkerBox,
                Material.PurpleShulkerBox,
                Material.PurpleShulkerBox,
                Material.PurpleTerracotta,
                Material.PurpurSlab,
                Material.PurpurStairs,
                Material.QuartzBlock,
                Material.QuartzBricks,
                Material.QuartzPillar,
                Material.QuartzSlab,
                Material.QuartzStairs,
                Material.Rail,
                Material.RedConcrete,
                Material.RedConcrete,
                Material.RedGlazedTerracotta,
                Material.RedNetherBrickSlab,
                Material.RedNetherBrickStairs,
                Material.RedSandstone,
                Material.RedSandstoneSlab,
                Material.RedSandstoneSlab,
                Material.RedSandstoneStairs,
                Material.RedShulkerBox,
                Material.RedShulkerBox,
                Material.RedShulkerBox,
                Material.RedTerracotta,
                Material.RedstoneBlock,
                Material.Sandstone,
                Material.SandstoneSlab,
                Material.SandstoneStairs,
                Material.ShulkerBox,
                Material.ShulkerBox,
                Material.ShulkerBox,
                Material.Smoker,
                Material.SmoothQuartz,
                Material.SmoothQuartzSlab,
                Material.SmoothQuartzStairs,
                Material.SmoothRedSandstoneSlab,
                Material.SmoothRedSandstoneStairs,
                Material.SmoothSandstoneSlab,
                Material.SmoothSandstoneStairs,
                Material.SmoothStone,
                Material.SmoothStoneSlab,
                Material.Spawner,
                Material.StickyPiston,
                Material.Stone,
                Material.StoneBrickSlab,
                Material.StoneBrickStairs,
                Material.StoneBricks,
                Material.StoneButton,
                Material.StonePressurePlate,
                Material.StoneSlab,
                Material.StoneStairs,
                Material.Stonecutter,
                Material.Terracotta,
                Material.WhiteConcrete,
                Material.WhiteConcrete,
                Material.WhiteGlazedTerracotta,
                Material.WhiteShulkerBox,
                Material.WhiteShulkerBox,
                Material.WhiteShulkerBox,
                Material.WhiteTerracotta,
                Material.YellowConcrete,
                Material.YellowConcrete,
                Material.YellowGlazedTerracotta,
                Material.YellowShulkerBox,
                Material.YellowShulkerBox,
                Material.YellowShulkerBox,
                Material.YellowTerracotta


            });
            // Minable by Stone, iron, diamond, netherite.
            List<Material> pickaxe_class1 = new List<Material>(new Material[]
            {
                Material.IronBlock,
                Material.IronOre,
                Material.LapisBlock,
                Material.LapisOre,
                Material.Terracotta,
            });
            // Minable by Iron, diamond, netherite.
            List<Material> pickaxe_class2 = new List<Material>(new Material[]
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
            List<Material> pickaxe_class3 = new List<Material>(new Material[]
            {
                Material.AncientDebris,
                Material.CryingObsidian,
                Material.NetheriteBlock,
                Material.Obsidian,
                Material.RespawnAnchor
            });

            // Every shovel can mine every block (speed difference).
            List<Material> shovel = new List<Material>(new Material[]
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
            List<Material> axe = new List<Material>(new Material[]
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
                Material.DaylightDetector,
                Material.FletchingTable,
                Material.GrayBanner,
                Material.GrayWallBanner,
                Material.GreenBanner,
                Material.GreenWallBanner,
                Material.IronDoor,
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
                Material.OakFence,
                Material.OakFenceGate,
                Material.OakLog,
                Material.OakPlanks,
                Material.OakPressurePlate,
                Material.OakSign,
                Material.OakSlab,
                Material.OakTrapdoor,
                Material.OakWallSign,
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
                Material.StrippedAcaciaLog,
                Material.StrippedBirchLog,
                Material.StrippedDarkOakLog,
                Material.StrippedJungleLog,
                Material.StrippedOakLog,
                Material.StrippedSpruceLog,
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
                Material.WarpedTrapdoor,
                Material.WarpedWallSign,
                Material.WhiteBanner,
                Material.WhiteWallBanner,
                Material.YellowBanner,
                Material.YellowWallBanner

            });
            // Every block a shear can mine.
            List<Material> shears = new List<Material>(new Material[] 
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
            List<Material> sword = new List<Material>(new Material[] 
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
            List<Material> hoe = new List<Material>(new Material[] 
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

            // Search for keywords instead of every color.
            if (pickaxe_class0.Contains(block))
            {
                return new ItemType[] { ItemType.WoodenPickaxe, ItemType.StonePickaxe, ItemType.GoldenPickaxe, ItemType.IronPickaxe, ItemType.DiamondPickaxe, ItemType.NetheritePickaxe };
            }
            else if (pickaxe_class1.Contains(block))
            {
                return new ItemType[] { ItemType.StonePickaxe, ItemType.GoldenPickaxe, ItemType.IronPickaxe, ItemType.DiamondPickaxe, ItemType.NetheritePickaxe };
            }
            else if (pickaxe_class2.Contains(block))
            {
                return new ItemType[] { ItemType.IronPickaxe, ItemType.DiamondPickaxe, ItemType.NetheritePickaxe };
            }
            else if (pickaxe_class3.Contains(block))
            {
                return new ItemType[] { ItemType.DiamondPickaxe, ItemType.NetheritePickaxe };
            }
            else if (shovel.Contains(block))
            {
                return new ItemType[] { ItemType.WoodenShovel, ItemType.StoneShovel, ItemType.GoldenShovel, ItemType.IronShovel, ItemType.DiamondShovel, ItemType.NetheriteShovel };
            }
            else if (axe.Contains(block))
            {
                return new ItemType[] { ItemType.WoodenAxe, ItemType.StoneAxe, ItemType.GoldenAxe, ItemType.IronAxe, ItemType.DiamondAxe, ItemType.NetheriteAxe };
            }
            else if (shears.Contains(block))
            {
                return new ItemType[] { ItemType.Shears, };
            }
            else if (sword.Contains(block))
            {
                return new ItemType[] { ItemType.WoodenSword, ItemType.StoneSword, ItemType.GoldenSword, ItemType.IronSword, ItemType.DiamondSword, ItemType.NetheriteSword };
            }
            else if (hoe.Contains(block))
            {
                return new ItemType[] { ItemType.WoodenHoe, ItemType.IronHoe, ItemType.GoldenHoe, ItemType.IronHoe, ItemType.DiamondHoe, ItemType.NetheriteHoe };
            }
            else { return new ItemType[0]; }
        }

        /// <summary>
        /// Prints a whole cube to the console. Separated in layers and rows.
        /// </summary>
        /// <param name="cubeToPrint">Some cube</param>
        private void PrintCubeToConsole(Cube cubeToPrint)
        {
            LogToConsole("Cube generated:");
            foreach (Layer lay in cubeToPrint.LayersToMine)
            {
                LogToConsole("Layer:");
                foreach (Row r in lay.RowsToMine)
                {
                    string generatedRow = "Row: ";
                    foreach (Location loc in r.BlocksToMine)
                    {
                        generatedRow += loc.ToString() + "; ";
                    }
                    LogToConsole(generatedRow);
                }
            }
            LogToConsole("End of cube.");
        }

        private string EvaluateCommand(string command, string[] args)
        {

            if (args.Length > 2)
            {
                Location startBlock;
                Location stopBlock;

                if (args.Length > 5)
                {
                    startBlock = new Location(
                    double.Parse(args[0]),
                    double.Parse(args[1]),
                    double.Parse(args[2])
                    );

                    stopBlock = new Location(
                    double.Parse(args[3]),
                    double.Parse(args[4]),
                    double.Parse(args[5])
                    );
                }
                else
                {
                    // Sometimes GetCurrentLocation() function returns false coordinates. (Maybe a bug.)
                    var temp = GetCurrentLocation();
                    startBlock.X = Math.Round(temp.X);
                    startBlock.Y = Math.Round(temp.Y);
                    startBlock.Z = Math.Round(temp.Z);

                    stopBlock = new Location(
                    double.Parse(args[0]),
                    double.Parse(args[1]),
                    double.Parse(args[2])
                    );
                }

                Thread tempThread = new Thread(() => Mine(GetMinableBlocksAsCube(startBlock, stopBlock)));
                tempThread.Start();

                return "Start mining.";
            }
            return "Invalid command syntax";
        }
    }
}
