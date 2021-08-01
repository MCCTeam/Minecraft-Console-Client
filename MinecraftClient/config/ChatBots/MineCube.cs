//MCCScript 1.0

MCC.LoadBot(new MineCube());

//MCCScript Extensions

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
        RegisterChatBotCommand("mineup", "Walk over a flat cubic platform of blocks and mine everything above you", "/mine x1 y1 z1 x2 y2 z2 (y1 = y2)", EvaluateMineCommand);
        LogToConsole("Mining bot created by Daenges.");
    }

    /// <summary>
    /// Dig out a 2 Block high cube and let the bot walk through it
    /// mining all blocks above it that it can reach.
    /// </summary>
    /// <param name="walkingArea">Area that the bot should walk through. (The lower Y coordinate of the 2 high cube.)</param>
    public void MineUp(Cube walkingArea)
    {
        foreach (Layer lay in walkingArea.LayersInCube)
        {
            foreach (Row r in lay.RowsInLayer)
            {
                foreach (Location loc in r.BlocksInRow)
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
                        LogDebugToConsole("Unable to walk to: " + loc.X.ToString() + " " + (loc.Y).ToString() + " " + loc.Z.ToString());
                        continue;
                    }

                    for (int height = Convert.ToInt32(Math.Round(currentLoc.Y)) + 2; height < Convert.ToInt32(Math.Round(currentLoc.Y)) + 7; height++)
                    {
                        Location mineLocation = new Location(loc.X, height, loc.Z);
                        Material mineLocationMaterial = GetWorld().GetBlock(mineLocation).Type;

                        // Stop mining process if breaking the next block could endager the bot
                        // through falling blocks or liquids.
                        if (IsGravityBlockAbove(mineLocation) || IsSorroundedByLiquid(mineLocation)) { break; }
                        // Skip this block if it can not be mined.
                        if (Material2Tool.IsUnbreakable(mineLocationMaterial)) { continue; }

                        //DateTime start = DateTime.Now;
                        // Search this tool in hotbar and select the correct slot
                        SelectCorrectSlotInHotbar(
                            // Returns the correct tool for this type
                            Material2Tool.GetCorrectToolForBlock(
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
        foreach (Layer lay in cubeToMine.LayersInCube)
        {
            foreach (Row r in lay.RowsInLayer)
            {
                foreach (Location loc in r.BlocksInRow)
                {
                    Material locMaterial = GetWorld().GetBlock(loc).Type;
                    if (!Material2Tool.IsUnbreakable(locMaterial) && !IsSorroundedByLiquid(loc))
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
                            else
                            {
                                LogDebugToConsole("Unable to walk to: " + loc.X.ToString() + " " + (loc.Y + 1).ToString() + " " + loc.Z.ToString());
                            }
                        }

                        //DateTime start = DateTime.Now;
                        // Search this tool in hotbar and select the correct slot
                        SelectCorrectSlotInHotbar(
                            // Returns the correct tool for this type
                            Material2Tool.GetCorrectToolForBlock(
                                // returns the type of the current block
                                GetWorld().GetBlock(loc).Type));

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
                if (tempArray.Length > 0 && tempArray[0] > 35)
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

    public bool IsGravityBlockAbove(Location block)
    {
        World world = GetWorld();
        double blockX = Math.Round(block.X);
        double blockY = Math.Round(block.Y);
        double blockZ = Math.Round(block.Z);

        List<Material> gravityBlockList = new List<Material>(new Material[] { Material.Gravel, Material.Sand, Material.RedSand, Material.Scaffolding, Material.Anvil, });


        var temptype = world.GetBlock(new Location(blockX, blockY + 1, blockZ)).Type;
        var tempLoc = gravityBlockList.Contains(world.GetBlock(new Location(blockX, blockY + 1, blockZ)).Type);

        return
            // Block can not fall down on player e.g. Sand, Gravel etc.
            gravityBlockList.Contains(world.GetBlock(new Location(blockX, blockY + 1, blockZ)).Type);
    }

    public bool IsSorroundedByLiquid(Location block)
    {
        World world = GetWorld();
        double blockX = Math.Round(block.X);
        double blockY = Math.Round(block.Y);
        double blockZ = Math.Round(block.Z);

        List<Material> liquidBlockList = new List<Material>(new Material[] { Material.Water, Material.Lava, });

        return     // Liquid can not flow down the hole. Liquid is unable to flow diagonally.
            liquidBlockList.Contains(world.GetBlock(new Location(blockX, blockY + 1, blockZ)).Type) ||
            liquidBlockList.Contains(world.GetBlock(new Location(blockX - 1, blockY, blockZ)).Type) ||
            liquidBlockList.Contains(world.GetBlock(new Location(blockX + 1, blockY, blockZ)).Type) ||
            liquidBlockList.Contains(world.GetBlock(new Location(blockX, blockY, blockZ - 1)).Type) ||
            liquidBlockList.Contains(world.GetBlock(new Location(blockX, blockY, blockZ + 1)).Type);
    }

    ///
    private string getHelpPage()
    {
        return
        "Usage of the mine bot:\n" +
        "/mine <x1> <y1> <z1> <x2> <y2> <z2> OR /mine <x> <y> <z>\n" +
        "to excavate a cube of blocks from top to bottom. (2 high area above the cube must be dug free by hand.)\n" +
        "/mineup <x1> <y1> <z1> <x2> <y1> <z2> OR /mineup <x> <y> <z>\n" +
        "to walk over a quadratic field of blocks and simultaniously mine everything above the head. \n" +
        "(Mines up to 5 Blocks, stops if gravel or lava would fall. 2 High area below this must be dug fee by hand.)\n";
    }

    /// <summary>
    /// Evaluates the given command
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    private string EvaluateMineCommand(string command, string[] args)
    {
        if (args.Length > 2)
        {
            Location startBlock;
            Location stopBlock;

            if (args.Length > 5)
            {
                try
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
                catch (Exception e)
                {
                    LogDebugToConsole(e.ToString());
                    return "Please enter correct coordinates as numbers.\n" + getHelpPage();
                }
            }
            else
            {
                Location tempLoc = GetCurrentLocation();
                startBlock = new Location(Math.Round(tempLoc.X),
                    Math.Round(tempLoc.Y),
                    Math.Round(tempLoc.Z));

                try
                {
                    stopBlock = new Location(
                    double.Parse(args[0]),
                    double.Parse(args[1]),
                    double.Parse(args[2])
                    );
                }
                catch (Exception e)
                {
                    LogDebugToConsole(e.ToString());
                    return "Please enter correct coordinates as numbers.\n" + getHelpPage();
                }
            }

            if (command.Contains("mineup"))
            {
                if (Math.Round(startBlock.Y) != Math.Round(stopBlock.Y))
                {
                    return "Both blocks must have the same Y value!\n" + getHelpPage();
                }

                List<Material> materialWhitelist = new List<Material>() { Material.Air };
                Thread tempThread = new Thread(() => MineUp(CubeFromWorld.GetBlocksAsCube(GetWorld(), startBlock, stopBlock, materialWhitelist, isBlacklist: false)));
                tempThread.Start();
                return "Start mining up.";
            }
            else
            {
                // Turn the cube around, so the bot always starts from the top.
                if (stopBlock.Y > startBlock.Y)
                {
                    Location temp = stopBlock;
                    stopBlock = startBlock;
                    startBlock = temp;
                }

                List<Material> blacklistedMaterials = new List<Material>() { Material.Air, Material.Water, Material.Lava };
                Thread tempThread = new Thread(() => Mine(CubeFromWorld.GetBlocksAsCube(GetWorld(), startBlock, stopBlock, blacklistedMaterials)));
                tempThread.Start();

                return "Start mining cube.";
            }
        }

        return "Invalid command syntax.\n" + getHelpPage();
    }
}