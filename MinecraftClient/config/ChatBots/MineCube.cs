//MCCScript 1.0

//using MinecraftClient.Inventory;
//using MinecraftClient.Mapping;
//using MinecraftClient.Scripting;
//using MinecraftClient;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Threading;
//using System;
//using Brigadier.NET.Builder;
//using MinecraftClient.CommandHandler.Patch;
//using Brigadier.NET;
//using MinecraftClient.CommandHandler;

MCC.LoadBot(new MineCube());

//MCCScript Extensions

//using System.Threading.Tasks;

class MineCube : ChatBot
{
    public const string CommandName = "mineup";

    private CancellationTokenSource cts;
    private Task currentMiningTask;
    private TimeSpan breakTimeout;
    private bool toolHandling;
    private int cacheSize;

    public override void Initialize()
    {
        if (!GetTerrainEnabled())
        {
            LogToConsole(Translations.extra_terrainandmovement_required);
            UnloadBot();
            return;
        }

        currentMiningTask = null;
        breakTimeout = TimeSpan.FromSeconds(15);
        cacheSize = 10;
        toolHandling = true;

        LogToConsole("Mining bot created by Daenges.");

        Handler.dispatcher.Register(l => l.Literal(CommandName)
            .Then(l => l.Argument("Commands", Arguments.GreedyString())
                .Executes(r => {
                    EvaluateMineCommand(CommandName + ' ' + Arguments.GetString(r, "Commands"), Arguments.GetString(r, "Commands").Split(' ', StringSplitOptions.TrimEntries));
                    return r.Source.SetAndReturn(CmdResult.Status.Done);
                }))
        );
    }

    public override void OnUnload()
    {
        Handler.dispatcher.Unregister(CommandName);
    }

    /// <summary>
    /// Walks in a 2 high area under an area of blocks and mines anything above its head.
    /// </summary>
    /// <param name="currentWorld">The current world</param>
    /// <param name="startBlock">The start corner of walking</param>
    /// <param name="stopBlock">The stop corner of walking</param>
    /// <param name="ct">CancellationToken to stop the task on cancel</param>
    public void MineUp(World currentWorld, Location startBlock, Location stopBlock, CancellationToken ct)
    {
        if (startBlock.Y != stopBlock.Y)
        {
            LogToConsole("Command FAILED. Both coordinates must be on the same y level.");
        }

        IEnumerable<int> xLocationRange = GetNumbersFromTo(Convert.ToInt32(Math.Round(startBlock.X)), Convert.ToInt32(Math.Round(stopBlock.X)));
        IEnumerable<int> zLocationRange = GetNumbersFromTo(Convert.ToInt32(Math.Round(startBlock.Z)), Convert.ToInt32(Math.Round(stopBlock.Z)));

        foreach (int currentXLoc in xLocationRange)
        {
            foreach (int currentZLoc in zLocationRange)
            {
                Location standLocation = new Location(currentXLoc, startBlock.Y, currentZLoc);

                // Walk to the new location.
                waitForMoveToLocation(standLocation, maxOffset: 1);

                for (int height = Convert.ToInt32(startBlock.Y) + 2; height < Convert.ToInt32(startBlock.Y) + 7; height++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        currentMiningTask = null;
                        LogToConsole("Cancellation requested. STOP MINING.");
                        return;
                    }

                    Location mineLocation = new Location(currentXLoc, height, currentZLoc);
                    Material mineLocationMaterial = currentWorld.GetBlock(mineLocation).Type;

                    // Stop mining process if breaking the next block could endager the bot
                    // through falling blocks or liquids.
                    if (!IsGravitySave(currentWorld, mineLocation) || IsSorroundedByLiquid(currentWorld, mineLocation)) { break; }
                    // Skip this block if it can not be mined.
                    if (Material2Tool.IsUnbreakable(mineLocationMaterial)) { continue; }

                    if (GetInventoryEnabled() && toolHandling)
                    {
                        // Search this tool in hotbar and select the correct slot
                        SelectCorrectSlotInHotbar(
                            // Returns the correct tool for this type
                            Material2Tool.GetCorrectToolForBlock(
                                // returns the type of the current block
                                mineLocationMaterial));
                    }

                    // If we are able to reach the block && break sucessfully sent
                    if (GetCurrentLocation().EyesLocation().DistanceSquared(mineLocation) <= 25 && DigBlock(mineLocation))
                    {
                        AutoTimeout.Perform(() =>
                        {
                            while (GetWorld().GetBlock(mineLocation).Type != Material.Air)
                            {
                                Thread.Sleep(100);

                                if (ct.IsCancellationRequested)
                                    break;
                            }
                        }, breakTimeout);
                    }
                    else
                    {
                        LogDebugToConsole("Unable to break this block: " + mineLocation.ToString());
                    }
                }
            }
        }
        LogToConsole("Finished mining up.");
    }

    /// <summary>
    /// Mine a cube of blocks from top to bottom between start and stop location
    /// </summary>
    /// <param name="currentWorld">The current world</param>
    /// <param name="startBlock">The upper corner of the cube to mine</param>
    /// <param name="stopBlock">The lower corner of the cube to mine</param>
    /// <param name="ct">CancellationToken to stop the task on cancel</param>
    public void Mine(World currentWorld, Location startBlock, Location stopBlock, CancellationToken ct)
    {
        // Turn the cube around, so the bot always starts from the top.
        if (stopBlock.Y > startBlock.Y)
        {
            Location temp = stopBlock;
            stopBlock = startBlock;
            startBlock = temp;
        }

        IEnumerable<int> xLocationRange = GetNumbersFromTo(Convert.ToInt32(Math.Round(startBlock.X)), Convert.ToInt32(Math.Round(stopBlock.X)));
        IEnumerable<int> yLocationRange = GetNumbersFromTo(Convert.ToInt32(Math.Round(startBlock.Y)), Convert.ToInt32(Math.Round(stopBlock.Y)));
        IEnumerable<int> zLocationRange = GetNumbersFromTo(Convert.ToInt32(Math.Round(startBlock.Z)), Convert.ToInt32(Math.Round(stopBlock.Z)));

        foreach (int currentYLoc in yLocationRange)
        {
            foreach (int currentXLoc in xLocationRange)
            {

                if (ct.IsCancellationRequested)
                {
                    currentMiningTask = null;
                    LogToConsole("Cancellation requested. STOP MINING.");
                    return;
                }

                List<Location> blocksToMine = null;

                // If the end of the new row is closer than the start, reverse the line and start here
                Location currentStandingLoc = GetCurrentLocation();
                Queue<int> currentZLocationRangeQueue = new Queue<int>(currentStandingLoc.DistanceSquared(new Location(currentXLoc, currentYLoc, zLocationRange.Last())) < currentStandingLoc.DistanceSquared(new Location(currentXLoc, currentYLoc, zLocationRange.First())) ?
                    zLocationRange.Reverse() :
                    zLocationRange);

                while (!ct.IsCancellationRequested && (currentZLocationRangeQueue.Count > 0 || blocksToMine.Count > 0))
                {
                    // Evaluate the next blocks to mine, while mining
                    Task<List<Location>> cacheEval = Task<List<Location>>.Factory.StartNew(() => // Get a new chunk of blocks that can be mined
                                                                EvaluateBlocks(currentWorld, currentXLoc, currentYLoc, currentZLocationRangeQueue, ct, cacheSize));

                    // On the first run, we need the task to finish, otherwise we would not have any results
                    if (blocksToMine != null)
                    {
                        // For all blocks in this block chunk
                        foreach (Location mineLocation in blocksToMine)
                        {
                            if (ct.IsCancellationRequested)
                                break;

                            Location currentLoc = GetCurrentLocation();
                            Location currentBlockUnderFeet = new Location(Math.Floor(currentLoc.X), Math.Floor(currentLoc.Y) - 1, Math.Floor(currentLoc.Z));

                            // If we are too far away from the mining location
                            if (currentLoc.EyesLocation().DistanceSquared(mineLocation) > 25)
                            {
                                // Walk to the new location
                                waitForMoveToLocation(mineLocation, maxOffset: 3);
                            }

                            // Prevent falling into danger
                            if (mineLocation == currentBlockUnderFeet && !Movement.IsSafe(currentWorld, currentBlockUnderFeet))
                                waitForMoveToLocation(mineLocation, maxOffset: 4, minOffset: 3);

                            // Is inventoryhandling activated?
                            if (GetInventoryEnabled() && toolHandling)
                            {
                                // Search this tool in hotbar and select the correct slot
                                SelectCorrectSlotInHotbar(
                                    // Returns the correct tool for this type
                                    Material2Tool.GetCorrectToolForBlock(
                                        // returns the type of the current block
                                        currentWorld.GetBlock(mineLocation).Type));
                            }

                            // If we are able to reach the block && break sucessfully sent
                            if (GetCurrentLocation().EyesLocation().DistanceSquared(mineLocation) <= 25 && DigBlock(mineLocation))
                            {
                                // Wait until the block is broken (== Air)
                                AutoTimeout.Perform(() =>
                                {
                                    while (GetWorld().GetBlock(mineLocation).Type != Material.Air)
                                    {
                                        Thread.Sleep(100);

                                        if (ct.IsCancellationRequested)
                                            break;
                                    }
                                }, breakTimeout);
                            }
                            else
                            {
                                LogDebugToConsole("Unable to break this block: " + mineLocation.ToString());
                            }

                        }
                    }

                    if (!ct.IsCancellationRequested)
                    {
                        // Wait for the block evaluation task to finish (if not already) and save the result
                        if (!cacheEval.IsCompleted)
                        {
                            cacheEval.Wait();
                        }
                        blocksToMine = cacheEval.Result;
                    }
                }
            }
        }
        currentMiningTask = null;
        LogToConsole("MINING FINISHED.");
    }

    /// <summary>
    /// This function selects a certain amount of minable blocks in a row
    /// </summary>
    /// <param name="currentWorld">The current world</param>
    /// <param name="xLoc">The current x location of the row</param>
    /// <param name="yLoc">The current y location of the row</param>
    /// <param name="zLocationQueue">All Z blocks that will be mined</param>
    /// <param name="ct">CancellationToken to stop the task on cancel</param>
    /// <param name="cacheSize">Maximum amount of blocks to return</param>
    /// <returns></returns>
    private List<Location> EvaluateBlocks(World currentWorld, int xLoc, int yLoc, Queue<int> zLocationQueue, CancellationToken ct, int cacheSize = 10)
    {
        List<Location> blockMiningCache = new List<Location>();
        int i = 0;
        while (zLocationQueue.Count > 0 && i < cacheSize && !ct.IsCancellationRequested)
        {
            // Get the block to mine, relative to the startblock of the row
            Location mineLocation = new Location(xLoc, yLoc, zLocationQueue.Dequeue());

            // Add the current location to the mining cache if it is safe to mine
            if (currentWorld.GetBlock(mineLocation).Type != Material.Air &&
                IsGravitySave(currentWorld, mineLocation) &&
                !IsSorroundedByLiquid(currentWorld, mineLocation) &&
                !Material2Tool.IsUnbreakable(currentWorld.GetBlock(mineLocation).Type))
            {
                blockMiningCache.Add(mineLocation);
                i++;
            }
        }
        return blockMiningCache;
    }

    /// <summary>
    /// Generates a sequence of numbers between a start and a stop number, including both
    /// </summary>
    /// <param name="start">Number to start from</param>
    /// <param name="stop">Number to end with</param>
    /// <returns>a sequence of numbers between a start and a stop number, including both</returns>
    private static IEnumerable<int> GetNumbersFromTo(int start, int stop)
    {
        return start <= stop ? Enumerable.Range(start, stop - start + 1) : Enumerable.Range(stop, start - stop + 1).Reverse();
    }

    /// <summary>
    /// Starts walk and waits until the client arrives
    /// </summary>
    /// <param name="location">Location to reach</param>
    /// <param name="allowUnsafe">Allow possible but unsafe locations thay may hurt the player: lava, cactus...</param>
    /// <param name="allowDirectTeleport">Allow non-vanilla direct teleport instead of computing path, but may cause invalid moves and/or trigger anti-cheat plugins</param>
    /// <param name="maxOffset">If no valid path can be found, also allow locations within specified distance of destination</param>
    /// <param name="minOffset">Do not get closer of destination than specified distance</param>
    /// <param name="timeout">How long to wait before stopping computation (default: 5 seconds)</param>
    private void waitForMoveToLocation(Location goal, bool allowUnsafe = false, bool allowDirectTeleport = false, int maxOffset = 0, int minOffset = 0, TimeSpan? timeout = null)
    {
        if (MoveToLocation(goal, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeout))
        {
            // Wait till the client stops moving
            while (ClientIsMoving())
            {
                Thread.Sleep(200);
            }
        }
        else
        {
            LogDebugToConsole("Unable to walk to: " + goal.ToString());
        }
    }

    /// <summary>
    /// Checks all slots of the hotbar for an Item and selects it if found
    /// </summary>
    /// <param name="tools">List of items that may be selected, from worst to best</param>
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

    /// <summary>
    /// Check if mining the current block would update others
    /// </summary>
    /// <param name="currentWorld">Current World</param>
    /// <param name="blockToMine">The block to be checked</param>
    /// <returns>true if mining the current block would not update others</returns>
    public bool IsGravitySave(World currentWorld, Location blockToMine)
    {
        Location currentLoc = GetCurrentLocation();
        Location block = new Location(Math.Round(blockToMine.X), Math.Round(blockToMine.Y), Math.Round(blockToMine.Z));
        List<Material> gravityBlockList = new List<Material>(new Material[] { Material.Gravel, Material.Sand, Material.RedSand, Material.Scaffolding, Material.Anvil, });
        Func<Location, bool> isGravityBlock = (Location blockToCheck) => gravityBlockList.Contains(currentWorld.GetBlock(blockToCheck).Type);
        Func<Location, bool> isBlockSolid = (Location blockToCheck) => currentWorld.GetBlock(blockToCheck).Type.IsSolid();

        return
            // Block can not fall down on player e.g. Sand, Gravel etc.
            !isGravityBlock(Movement.Move(block, Direction.Up)) &&
            (Movement.Move(currentLoc, Direction.Down) != blockToMine || currentWorld.GetBlock(Movement.Move(currentLoc, Direction.Down, 2)).Type.IsSolid()) &&
            // Prevent updating flying sand/gravel under player
            !isGravityBlock(Movement.Move(block, Direction.Down)) || isBlockSolid(Movement.Move(block, Direction.Down, 2));
    }

    /// <summary>
    /// Checks if the current block is sorrounded by liquids
    /// </summary>
    /// <param name="currentWorld">Current World</param>
    /// <param name="blockToMine">The block to be checked</param>
    /// <returns>true if mining the current block results in liquid flow change</returns>
    public bool IsSorroundedByLiquid(World currentWorld, Location blockToMine)
    {
        Location block = new Location(Math.Round(blockToMine.X), Math.Round(blockToMine.Y), Math.Round(blockToMine.Z));
        Func<Location, bool> isLiquid = (Location blockToCheck) => currentWorld.GetBlock(blockToCheck).Type.IsLiquid();

        return     // Liquid can not flow down the hole. Liquid is unable to flow diagonally.
            isLiquid(block) ||
            isLiquid(Movement.Move(block, Direction.Up)) ||
            isLiquid(Movement.Move(block, Direction.North)) ||
            isLiquid(Movement.Move(block, Direction.South)) ||
            isLiquid(Movement.Move(block, Direction.East)) ||
            isLiquid(Movement.Move(block, Direction.West));
    }

    /// <summary>
    /// The Help page for this command.
    /// </summary>
    /// <returns>a help page</returns>
    private string getHelpPage()
    {
        return
        "Usage of the mine bot:\n" +
        "/mine <x1> <y1> <z1> <x2> <y2> <z2> OR /mine <x> <y> <z>\n" +
        "to excavate a cube of blocks from top to bottom. (There must be a 2 high area of air above the cube you want to mine.)\n" +
        "/mineup <x1> <y1> <z1> <x2> <y1> <z2> OR /mineup <x> <y> <z>\n" +
        "to walk over a quadratic field of blocks and simultaniously mine everything above the head. \n" +
        "(Mines up to 5 Blocks, stops if gravel or lava would fall. There must be a 2 high area of air below the cube you want to mine.)\n" +
        "/mine OR /mineup cancel\n" +
        "to cancel the current mining process.\n" +
        "/mine OR /mineup cachesize\n" +
        "to set the current cache size\n" +
        "/mine OR /mineup breaktimeout\n" +
        "to set the time to wait until a block is broken."; ;

    }

    private string EvaluateMineCommand(string command, string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "breaktimeout":
                    int temp;
                    if (int.TryParse(args[i + 1], out temp))
                        breakTimeout = TimeSpan.FromMilliseconds(temp);
                    else return "Please enter a valid number.";
                    return string.Format("Set the break timout to {0} ms.", breakTimeout);

                case "cachesize":
                    return int.TryParse(args[i + 1], out cacheSize) ? string.Format("Set cache size to {0} blocks.", cacheSize) : "Please enter a valid number";

                case "cancel":
                    cts.Cancel();
                    currentMiningTask = null;
                    return "Cancelled current mining process.";

                case "toolHandling":
                    toolHandling = !toolHandling;
                    return string.Format("Tool handling was set to: {0}", toolHandling.ToString());
            }
        }

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

            if (currentMiningTask == null)
            {
                if (command.Contains("mineup"))
                {
                    cts = new CancellationTokenSource();

                    currentMiningTask = Task.Factory.StartNew(() => MineUp(GetWorld(), startBlock, stopBlock, cts.Token));
                    return "Start mining up.";
                }
                else if (command.Contains("mine"))
                {

                    cts = new CancellationTokenSource();

                    currentMiningTask = Task.Factory.StartNew(() => Mine(GetWorld(), startBlock, stopBlock, cts.Token));
                    return "Start mining cube.";
                }
            }
            else return "You are already mining. Cancel it with '/minecancel'";
        }

        return "Invalid command syntax.\n" + getHelpPage();
    }
}
