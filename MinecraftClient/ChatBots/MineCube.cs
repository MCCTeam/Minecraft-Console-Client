﻿using System;
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
