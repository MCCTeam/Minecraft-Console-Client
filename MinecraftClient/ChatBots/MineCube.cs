﻿using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;
using System.Threading;

namespace MinecraftClient.ChatBots
{
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
            RegisterChatBotCommand("mine", "Mine a cube from a to b", "/mine x y z OR /mine x1 y1 z1 x2 y2 z2", EvaluateMineCommand);
            RegisterChatBotCommand("mineup", "Walk over a flat cubic platform of blocks and mine everything above you", "/mine x1 y1 z1 x2 y2 z2 (y1 = y2)", EvaluateMineUpCommand);
        }

        /// <summary>
        /// Dig out a 2 Block high cube and let the bot walk through it
        /// mining all blocks above it that it can reach.
        /// </summary>
        /// <param name="walkingArea">Area that the bot should walk through. (The lower Y coordinate of the 2 high cube.)</param>
        public void MineUp(Cube walkingArea)
        {
            Material2Tool m2t = new Material2Tool();
            foreach (Layer lay in walkingArea.LayersToMine)
            {
                foreach (Row r in lay.RowsToMine)
                {
                    foreach (Location loc in r.BlocksToMine)
                    {
                        Location currentLoc = GetCurrentLocation();

                        if (MoveToLocation(new Location(loc.X, loc.Y, loc.Z)))
                        {
                            while (Math.Round(GetCurrentLocation().Distance(loc)) > 1)
                            {
                                Thread.Sleep(200);
                            }
                        }
                        else
                        {
                            // This block is not reachable for some reason.
                            // Keep on going with the next collumn.
                            LogToConsole("Unable to walk to: " + loc.X.ToString() + " " + (loc.Y).ToString() + " " + loc.Z.ToString());
                            continue;
                        }

                        for (int height = Convert.ToInt32(Math.Round(currentLoc.Y)) + 2; height < Convert.ToInt32(Math.Round(currentLoc.Y)) + 7; height++)
                        {
                            Location mineLocation = new Location(loc.X, height, loc.Z);
                            Material mineLocationMaterial = GetWorld().GetBlock(mineLocation).Type;

                            // Stop mining process if breaking the next block could endager the bot
                            // through falling blocks or liquids.
                            if (IsSorroundedByGravityBlocks(mineLocation)) { break; }
                            // Skip this block if it can not be mined.
                            if (m2t.IsUnbreakable(mineLocationMaterial)) { continue; }

                            //DateTime start = DateTime.Now;
                            // Search this tool in hotbar and select the correct slot
                            SelectCorrectSlotInHotbar(
                                // Returns the correct tool for this type
                                m2t.GetCorrectToolForBlock(
                                    // returns the type of the current block
                                    mineLocationMaterial));

                            // Unable to check when breaking is over.
                            if (DigBlock(mineLocation))
                            {
                                short i = 0; // Maximum wait time of 10 sec.
                                while (GetWorld().GetBlock(mineLocation).Type != Material.Air && i <= 100)
                                {
                                    Thread.Sleep(100);
                                    i++;
                                }
                            }
                            else
                            {
                                LogDebugToConsole("Unable to break this block: " + mineLocation.ToString());
                            }
                        }
                    }
                }
            }
            LogToConsole("Finished mining up.");
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
                        Material locMaterial = GetWorld().GetBlock(loc).Type;
                        if (!m2t.IsUnbreakable(locMaterial))
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
                                    LogToConsole("Unable to walk to: " + loc.X.ToString() + " " + (loc.Y + 1).ToString() + " " + loc.Z.ToString());
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
            }
            LogToConsole("Mining finished.");
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

            List<Material> gravityBlockList = new List<Material>(new Material[] {Material.Gravel, Material.Sand, Material.RedSand, Material.Scaffolding, Material.Anvil, });
            List<Material> liquidBlockList = new List<Material>(new Material[] { Material.Water, Material.Lava, });

            var temptype = world.GetBlock(new Location(blockX, blockY + 1, blockZ)).Type;
            var tempLoc = gravityBlockList.Contains(world.GetBlock(new Location(blockX, blockY + 1, blockZ)).Type);

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

        private string EvaluateMineUpCommand(string command, string[] args)
        {
            Location startBlock = new Location(
                        double.Parse(args[0]),
                        double.Parse(args[1]),
                        double.Parse(args[2])
                        );

            Location stopBlock = new Location(
                    double.Parse(args[3]),
                    double.Parse(args[4]),
                    double.Parse(args[5])
                    );

            if (Math.Round(startBlock.Y) != Math.Round(stopBlock.Y))
            {
                return "Both blocks must have the same Y value!";
            }

            CubeFromWorld CFW = new CubeFromWorld();
            List<Material> materialWhitelist = new List<Material>(new Material[] { Material.Air });
            Thread tempThread = new Thread(() => MineUp(CFW.GetBlocksAsCube(GetWorld(), startBlock, stopBlock, materialWhitelist, isBlacklist:false)));
            tempThread.Start();

            return "Start mining up.";
        }

        private string EvaluateMineCommand(string command, string[] args)
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

                CubeFromWorld CFW = new CubeFromWorld();
                List<Material> blacklistedMaterials = new List<Material>(new Material[] { Material.Air, Material.Water, Material.Lava });
                Thread tempThread = new Thread(() => Mine(CFW.GetBlocksAsCube(GetWorld(), startBlock, stopBlock, blacklistedMaterials)));
                tempThread.Start();

                return "Start mining cube.";
            }
                
            
            return "Invalid command syntax";
        }
    }
}
