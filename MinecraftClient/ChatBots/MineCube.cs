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
        public int addRow(Row givenRow = null)
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
        public int addLayer(Layer givenLayer = null)
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
                UnLoadBot(this);
            }
            RegisterChatBotCommand("mine", "Mine a cube from a to b", "/mine x y z OR /mine x1 y1 z1 x2 y2 z2", evaluateCommand);
        }

        public void Mine(Cube cubeToMine)
        {
            foreach (Layer lay in cubeToMine.LayersToMine)
            {
                foreach (Row r in lay.RowsToMine)
                {
                    foreach (Location loc in r.BlocksToMine)
                    {
                        if (getHeadLocation(GetCurrentLocation()).Distance(loc) > 5)
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

            // Initialize cube to mine
            Cube cubeToMine = new Cube();

            // Get the distance between start and finish as Vector
            Location vectorToStopPosition = stopBlock - startBlock;

            // Initialize Iteration process
            int[] iterateX = getNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.X))).ToArray();
            int[] iterateY = getNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.Y))).ToArray();
            int[] iterateZ = getNumbersFromTo(0, Convert.ToInt32(Math.Round(vectorToStopPosition.Z))).ToArray();

            LogDebugToConsole("Iterate on X: 0-" + (iterateX.Length - 1).ToString() + " Y: 0-" + (iterateY.Length - 1).ToString() + " Z: 0-" + (iterateZ.Length - 1).ToString());

            // Iterate through all coordinates relative to the start block
            foreach (int y in iterateY)
            {
                Layer tempLayer = new Layer();
                foreach (int x in iterateX)
                {
                    Row tempRow = new Row();
                    foreach (int z in iterateZ)
                    {
                        Location tempLocation = new Location(Math.Round(startBlock.X + x), Math.Round(startBlock.Y + y), Math.Round(startBlock.Z + z));
                        if (isMinable(GetWorld().GetBlock(tempLocation).Type))
                        {
                            tempRow.BlocksToMine.Add(tempLocation);
                        }
                    }
                    if (tempRow.BlocksToMine.Count > 0)
                    {
                        tempLayer.addRow(tempRow);
                    }
                }
                if (tempLayer.RowsToMine.Count > 0)
                {
                    cubeToMine.addLayer(tempLayer);
                }
            }

            // Remove later ;D
            printCubeToConsole(cubeToMine);

            if (Settings.DebugMessages)
            {
                printCubeToConsole(cubeToMine);
            }

            return cubeToMine;
        }

        /// <summary>
        /// Get all numbers between from and to.
        /// </summary>
        /// <param name="start">Number to start</param>
        /// <param name="end">Number to stop</param>
        /// <returns>All numbers between the first, including the stop number</returns>
        public List<int> getNumbersFromTo(int start, int stop)
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

        public Func<Location, Location> getHeadLocation = locFeet => new Location(locFeet.X, locFeet.Y + 1, locFeet.Z);

        /// <summary>
        /// Checks whether a material is minable
        /// </summary>
        /// <param name="block">Block that should be checked</param>
        /// <returns>Is block minable</returns>
        private bool isMinable(Material block)
        {
            return (
                block != Material.Air &&
                block != Material.Bedrock &&
                !block.IsLiquid()
                    );
        }

        private void selectCorrectSlotInHotbar(ItemType[] tools)
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
        private ItemType[] getCorrectToolForBlock(Material block)
        {
            List<Material> pickaxe = new List<Material>(new Material[]
            {
                Material.Ice,
                Material.PackedIce,
                Material.BlueIce,
                Material.FrostedIce,
                Material.Anvil,
                Material.Bell,
                Material.RedstoneBlock,
                Material.BrewingStand,
                Material.Cauldron,
                Material.Chain,
                Material.Hopper,
                Material.IronBars,
                Material.IronDoor,
                Material.IronTrapdoor,
                Material.Lantern,
                Material.HeavyWeightedPressurePlate,
                Material.LightWeightedPressurePlate,
                Material.IronBlock,
                Material.LapisBlock,
                Material.DiamondBlock,
                Material.EmeraldBlock,
                Material.GoldBlock,
                Material.NetheriteBlock,
                Material.Piston,
                Material.StickyPiston,
                Material.Conduit,
                Material.ShulkerBox,
                Material.ActivatorRail,
                Material.DetectorRail,
                Material.PoweredRail,
                Material.Rail,
                Material.Andesite,
                Material.Basalt,
                Material.Blackstone,
                Material.BlastFurnace,
                Material.CoalOre,
                Material.Cobblestone,
                Material.CobblestoneWall,
                Material.DarkPrismarine,
                Material.Diorite,
                Material.Dispenser,
                Material.Dropper,
                Material.EnchantingTable,
                Material.EndStone,
                Material.EnderChest,
                Material.Furnace,
                Material.Granite,
                Material.Grindstone,
                Material.Lodestone,
                Material.MossyCobblestone,
                Material.NetherBricks,
                Material.NetherBrickFence,
                Material.NetherGoldOre,
                Material.NetherQuartzOre,
                Material.Netherrack,
                Material.Observer,
                Material.Prismarine,
                Material.PrismarineBricks,
                Material.PolishedAndesite,
                Material.PolishedBlackstone,
                Material.PolishedBlackstoneBricks,
                Material.PolishedDiorite,
                Material.PolishedGranite,
                Material.RedSandstone,
                Material.RedSandstoneSlab,
                Material.Sandstone,
                Material.Smoker,
                Material.Spawner,
                Material.Stonecutter,
                Material.SmoothStone,
                Material.Stone,
                Material.StoneBricks,
                Material.StoneButton,
                Material.StonePressurePlate,
                Material.Terracotta,
                Material.IronOre,
                Material.LapisOre,
                Material.DiamondOre,
                Material.EmeraldOre,
                Material.GoldOre,
                Material.RedstoneOre,
                Material.AncientDebris,
                Material.CryingObsidian,
                Material.Obsidian,
                Material.RespawnAnchor

            });
            List<Material> shovel = new List<Material>(new Material[] 
            {
                Material.Clay,
                Material.CoarseDirt,
                Material.Dirt,
                Material.Farmland,
                Material.Grass,
                Material.GrassPath,
                Material.Gravel,
                Material.Mycelium,
                Material.Podzol,
                Material.RedSand,
                Material.Sand,
                Material.SoulSand,
                Material.SoulSoil
            });
            List<Material> axe = new List<Material>(new Material[] 
            {
                Material.Cocoa,
                Material.JackOLantern,
                Material.Pumpkin,
                Material.Vine,
                Material.Melon,
                Material.BeeNest,
                Material.MushroomStem,
                Material.BrownMushroomBlock,
                Material.RedMushroomBlock,
                Material.Barrel,
                Material.Bookshelf,
                Material.Chest,
                Material.Beehive,
                Material.Campfire,
                Material.CartographyTable,
                Material.Composter,
                Material.CraftingTable,
                Material.DaylightDetector,
                Material.FletchingTable,
                Material.Jukebox,
                Material.Ladder,
                Material.Lectern,
                Material.Loom,
                Material.NoteBlock,
                Material.SmithingTable,
                Material.TrappedChest,
            });

            // Search for keywords instead of every color
            if (pickaxe.Contains(block) || block.ToString().Contains("Concrete") || block.ToString().Contains("GlazedTerracotta") ||
                        (block.ToString().Contains("slab") &&
                        !block.ToString().Contains("Oak") &&
                        !block.ToString().Contains("Spruce") &&
                        !block.ToString().Contains("Birch") &&
                        !block.ToString().Contains("Jungle") &&
                        !block.ToString().Contains("Arcacia") &&
                        !block.ToString().Contains("Crimson") &&
                        !block.ToString().Contains("Warped")))
            {
                return new ItemType[] { ItemType.NetheritePickaxe, ItemType.DiamondPickaxe, ItemType.IronPickaxe, ItemType.GoldenPickaxe, ItemType.StonePickaxe, ItemType.WoodenPickaxe};
            }
            else if (axe.Contains(block) ||
                        //block.ToString().Contains("Fence") ||
                        block.ToString().Contains("Banner") ||
                        //block.ToString().Contains("Plank") ||
                        //block.ToString().Contains("Sign") ||
                        //block.ToString().Contains("Log") ||
                        block.ToString().Contains("Hyphae") ||
                        //block.ToString().Contains("Button") && block != Material.StoneButton ||
                        //block.ToString().Contains("Door") && block != Material.IronDoor ||
                        //block.ToString().Contains("PressurePlate") && block != Material.StonePressurePlate ||
                        block.ToString().Contains("Oak") ||
                        block.ToString().Contains("Spruce") ||
                        block.ToString().Contains("Birch") ||
                        block.ToString().Contains("Jungle") ||
                        block.ToString().Contains("Arcacia") ||
                        block.ToString().Contains("Crimson") ||
                        block.ToString().Contains("Warped"))
                 {
                     return new ItemType[] { ItemType.NetheriteAxe, ItemType.DiamondAxe, ItemType.IronAxe, ItemType.GoldenAxe, ItemType.StoneAxe, ItemType.WoodenAxe };
                 }
            else if (shovel.Contains(block) || block.ToString().Contains("ConcretePowder"))
            {
                return new ItemType[] { ItemType.NetheriteShovel, ItemType.DiamondShovel, ItemType.IronShovel, ItemType.GoldenShovel, ItemType.StoneShovel, ItemType.WoodenShovel};
            }
          
            return null;
        }

        /// <summary>
        /// Prints a whole cube to the console. Separated in layers and rows.
        /// </summary>
        /// <param name="cubeToPrint">Some cube</param>
        private void printCubeToConsole(Cube cubeToPrint)
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

        private string evaluateCommand(string command, string[] args)
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
