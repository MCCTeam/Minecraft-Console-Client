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

        /// <summary>
        /// Dig out a 2 Block high cube and let the bot walk through it
        /// mining all blocks above it that it can reach.
        /// </summary>
        /// <param name="walkingArea">Area that the bot should walk through. (The lower Y coordinate of the 2 high cube.)</param>
        public void MineCubeUp(Cube walkingArea)
        {
            Material2Tool m2t = new Material2Tool();
            foreach (Layer lay in walkingArea.LayersToMine)
            {
                foreach (Row r in lay.RowsToMine)
                {
                    foreach (Location loc in r.BlocksToMine)
                    {
                        Location currentLoc = GetCurrentLocation();

                        if (MoveToLocation(new Location(loc.X, loc.Y + 1, loc.Z)))
                        {
                            while (GetCurrentLocation().Distance(loc) > 1)
                            {
                                Thread.Sleep(200);
                            }
                        }
                        else
                        {
                            LogToConsole("Unable to walk to: " + loc.X.ToString() + " " + (loc.Y + 1).ToString() + " " + loc.Z.ToString());
                        }

                        for (int height = Convert.ToInt32(Math.Round(currentLoc.Y)); height < Convert.ToInt32(Math.Round(currentLoc.Y)) + 7; height++)
                        {
                            Location mineLocation = new Location(currentLoc.X, height, currentLoc.Y);

                            // Stop mining process if breaking the next block could endager the bot
                            // through falling blocks or liquids.
                            if (IsSorroundedByGravityBlocks(mineLocation)) { break; }

                            //DateTime start = DateTime.Now;
                            // Search this tool in hotbar and select the correct slot
                            SelectCorrectSlotInHotbar(
                                // Returns the correct tool for this type
                                m2t.GetCorrectToolForBlock(
                                    // returns the type of the current block
                                    GetWorld().GetBlock(mineLocation).Type));

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
            }
        }

        /// <summary>
        /// Mines out a cube of blocks from top to bottom.
        /// </summary>
        /// <param name="cubeToMine">The cube that should be mined.</param>
        public void Mine(Cube cubeToMine)
        {
            Material2Tool m2t = new Material2Tool();
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
                                while (GetCurrentLocation().Distance(loc) > 4)
                                {
                                    Thread.Sleep(200);
                                }
                            }
                            // Some blocks might not be reachable, although approximation would be enough
                            // but the client either denies walking or walks to the goal block.
                            else
                            {
                                LogToConsole("Unable to walk to: " + loc.X.ToString()+ " " + (loc.Y + 1).ToString() + " " + loc.Z.ToString());
                            }
                        }

                        //DateTime start = DateTime.Now;
                        // Search this tool in hotbar and select the correct slot
                        SelectCorrectSlotInHotbar(
                            // Returns the correct tool for this type
                            m2t.GetCorrectToolForBlock(
                                // returns the type of the current block
                                GetWorld().GetBlock(loc).Type));
                        //LogToConsole("It took " + (DateTime.Now-start).TotalSeconds.ToString() + " seconds to find the correct tool.");

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

        /// <summary>
        /// Creates a cube of blocks out of two coordinates.
        /// </summary>
        /// <param name="startBlock">Start Location</param>
        /// <param name="stopBlock">Stop Location</param>
        /// <returns>A cube of blocks consisting of Layers, Rows and single blocks</returns>
        public Cube GetMinableBlocksAsCube(Location startBlock, Location stopBlock)
        {
            Material2Tool m2t = new Material2Tool();
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
                        Material tempLocationType = GetWorld().GetBlock(tempLocation).Type;
                        if (!m2t.IsUnbreakable(tempLocationType) && tempLocationType != Material.Water && tempLocationType != Material.Lava)
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

        private void SelectCorrectSlotInHotbar(ItemType[] tools)
        {
            if (GetInventoryEnabled())
            {
                foreach (ItemType tool in tools)
                {
                    int[] tempArray = GetPlayerInventory().SearchItem(tool);
                    // Check whether an item could be found and make sure that it is in
                    // a hotbar slot (36-44).
                    if(tempArray.Length > 0 && tempArray[0] > 35)
                    {
                        // Changeslot takes numbers from 0-8
                        ChangeSlot(Convert.ToInt16(tempArray[0] - 36));
                        break;
                    }
                }
            }
            else
            {
                LogToConsole("Activate Inventory Handling.");
            }
        }

        public bool IsSorroundedByGravityBlocks(Location block)
        {
            World world = GetWorld();
            double blockX = Math.Round(block.X);
            double blockY = Math.Round(block.Y);
            double blockZ = Math.Round(block.Z);

            List<Material> gravityBlockList = new List<Material>(new Material[] {Material.Gravel, Material.Sand, Material.Scaffolding, Material.Anvil, });
            List<Material> liquidBlockList = new List<Material>(new Material[] { Material.Water, Material.Lava, });

            return
                // Block can not fall down on player e.g. Sand, Gravel etc.
                gravityBlockList.Contains(world.GetBlock(new Location(blockX, blockY + 1, blockZ)).Type) ||
                
                // Liquid can not flow down the hole. Liquid is unable to flow diagonally.
                liquidBlockList.Contains(world.GetBlock(new Location(blockX, blockY + 1, blockZ)).Type) ||
                liquidBlockList.Contains(world.GetBlock(new Location(blockX - 1, blockY, blockZ)).Type) ||
                liquidBlockList.Contains(world.GetBlock(new Location(blockX + 1, blockY, blockZ)).Type) ||
                liquidBlockList.Contains(world.GetBlock(new Location(blockX, blockY, blockZ - 1)).Type) ||
                liquidBlockList.Contains(world.GetBlock(new Location(blockX, blockY, blockZ + 1)).Type);
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

                // Turn the cube around, so the bot always starts from the top.
                if (stopBlock.Y > startBlock.Y) 
                {
                    Location temp = stopBlock;
                    stopBlock = startBlock;
                    startBlock = temp;
                }

                Thread tempThread = new Thread(() => Mine(GetMinableBlocksAsCube(startBlock, stopBlock)));
                tempThread.Start();

                return "Start mining.";
            }
            return "Invalid command syntax";
        }
    }
}
