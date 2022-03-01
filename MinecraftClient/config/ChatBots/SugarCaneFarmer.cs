//MCCScript 1.0

MCC.LoadBot(new SugarCaneFarmer());

//MCCScript Extensions

class SugarCaneFarmerBase : ChatBot
{
    /// <summary>
    /// MoveToLocation() + waiting until the bot is near the location.
    /// </summary>
    /// <param name="pos">Location to walk to</param>
    /// <param name="tolerance">Distance of blocks from the goal that is accepted as "arrived"</param>
    /// <returns>True if walking was successful</returns>
    public bool WaitForMoveToLocation(Location pos, float tolerance = 2f)
    {
        if (MoveToLocation(new Location(pos.X, pos.Y, pos.Z)))
        {
            while (GetCurrentLocation().Distance(pos) > tolerance)
            {
                Thread.Sleep(200);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// DigBlock + waiting until the block is broken
    /// </summary>
    /// <param name="block">Location of the block that should be broken</param>
    /// <param name="useCorrectTool">Switch to the correct tool, if it is in hotbar</param>
    /// <param name="digTimeout">Minimum time until the client gives up</param>
    /// <returns>True if mining was successful</returns>
    public bool WaitForDigBlock(Location block, bool useCorrectTool = false, int digTimeout = 1000)
    {
        if (Material2Tool.IsUnbreakable(GetWorld().GetBlock(block).Type))
            return false;

        if (useCorrectTool && GetInventoryEnabled())
        {
            // Search this tool in hotbar and select the correct slot
            SelectCorrectSlotInHotbar(
                // Returns the correct tool for this type
                Material2Tool.GetCorrectToolForBlock(
                    // returns the type of the current block
                    GetWorld().GetBlock(block).Type));
        }

        // Unable to check when breaking is over.
        if (DigBlock(block))
        {
            short i = 0; // Maximum wait time of 10 sec.
            while (GetWorld().GetBlock(block).Type != Material.Air && i <= digTimeout)
            {
                Thread.Sleep(100);
                i++;
            }
            return i <= digTimeout;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Selects the corect tool in the hotbar
    /// </summary>
    /// <param name="tools">List of tools that can be applied to mine a block</param>
    public void SelectCorrectSlotInHotbar(ItemType[] tools)
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
    /// Returns the head position from the current location
    /// </summary>
    public static Func<Location, Location> GetHeadLocation = locFeet => new Location(locFeet.X, locFeet.Y + 1, locFeet.Z);
}

class SugarCaneFarmer : SugarCaneFarmerBase
{
    public enum CoordinateType { X, Y, Z };

    /// <summary>
    /// Used to stop the farming process on demand.
    /// </summary>
    private bool farming = true;

    /// <summary>
    /// Returns all sugar canes that are above a solid block and another sugar cane
    /// </summary>
    /// <param name="radius">Radius of the search</param>
    /// <param name="coordinateOrder">Order of the returned sugar canes</param>
    /// <returns>list of sugar canes, sorted after given parameter</returns>
    private List<Location> collectSugarCaneBlocks(int radius, CoordinateType coordinateOrder = CoordinateType.X)
    {
        IEnumerable<Location> breakingCoordinates = GetWorld().FindBlock(
            new Location(Math.Floor(GetCurrentLocation().X), Math.Floor(GetCurrentLocation().Y), Math.Floor(GetCurrentLocation().Z)),
            Material.SugarCane, radius).Where(block =>
            GetWorld().GetBlock(new Location(block.X, block.Y - 1, block.Z)).Type == Material.SugarCane &&
            GetWorld().GetBlock(new Location(block.X, block.Y - 2, block.Z)).Type.IsSolid());

        switch (coordinateOrder)
        {
            case CoordinateType.Z:
                return breakingCoordinates.OrderBy(coordinate => coordinate.Z).ToList();
            case CoordinateType.Y:
                return breakingCoordinates.OrderBy(coordinate => coordinate.Y).ToList();
            default:
                return breakingCoordinates.OrderBy(coordinate => coordinate.X).ToList();
        }
    }

    public override void Initialize()
    {
        LogToConsole("Sugar Cane farming bot created by Daenges.");
        RegisterChatBotCommand("sugarcane", "Farm sugar cane automatically", "/sugarcane [range x|y|z]/[stop]", commandHandler);
    }

    /// <summary>
    /// Start the farming process
    /// </summary>
    /// <param name="range">Range of farming</param>
    /// <param name="sortOrder">In which order the plants should be farmed</param>
    private void startBot(int range, CoordinateType sortOrder)
    {
        List<Location> sugarCanePositions;

        // create a loop so the bot keeps farming if nothing else is stated
        // uses the same range as initially given
        do
        {
            sugarCanePositions = collectSugarCaneBlocks(range, sortOrder);

            LogToConsole(string.Format("Found: {0} Sugar Cane", sugarCanePositions.Count));

            if (sugarCanePositions.Count > 0)
            {
                foreach (Location loc in sugarCanePositions)
                {
                    if (!farming)
                        break;

                    if (GetHeadLocation(GetCurrentLocation()).Distance(loc) > 5)
                    {
                        if (!WaitForMoveToLocation(new Location(loc.X, loc.Y - 1, loc.Z)))
                        {
                            LogToConsole("Moving failed.");
                            continue;
                        }
                    }

                    if (!WaitForDigBlock(new Location(loc.X, loc.Y, loc.Z)))
                        LogDebugToConsole("Unable to break this block: " + loc.ToString());
                }
            }
            else { LogToConsole("Nothing found. Stop farming..."); }

            if (farming && sugarCanePositions.Count > 0)
                LogToConsole("Finished farming. Searching for further work... Use '/sugarcane stop' to stop farming.");

        } while (farming && sugarCanePositions.Count > 0);

        farming = true;
        LogToConsole("[FARMING STOPPED]");
    }

    private string commandHandler(string command, string[] args)
    {
        if (args.Length == 1)
        {
            if (args[0] == "stop")
            {
                farming = false;
                return "Stop farming...";
            }
            else
            {
                int range;

                try { range = Convert.ToInt32(args[0]); }
                catch (Exception) { return "Range must be a number"; }

                Thread workThread = new Thread(() => startBot(range, CoordinateType.X));
                workThread.Start();

                return "Start farming.";
            }
        }
        else if (args.Length == 2)
        {
            int range;

            try { range = Convert.ToInt32(args[0]); }
            catch (Exception) { return "Range must be a number"; }

            Thread workThread;

            if (args[1].ToLower() == "x")
                workThread = new Thread(() => startBot(range, CoordinateType.X));
            else if (args[1].ToLower() == "y")
                workThread = new Thread(() => startBot(range, CoordinateType.Y));
            else
                workThread = new Thread(() => startBot(range, CoordinateType.Z));

            workThread.Start();

            return "Start farming.";
        }
        else
            return "Usage: /sugarcane [range x|y|z]/[stop]";
    }
}
