//MCCScript 1.0

MCC.LoadBot(new AutoTree());

//MCCScript Extensions

public class AutoTree : ChatBot
{
    // Auto sapling placer - made for auto tree machine
    // Put your bot in designed position for placing sapling
    // Set the tree type by "/autotree type <Acacia|Birch|Oak|DarkOak|Jungle|Spruce>"
    // Toggle on and off by "/autotree toggle"

    // Hard-code the location of the sapling (dirt coordinate with y-axis plus 1)
    // Or use the in-game command "/autotree set x y z"
    Location sapling = new Location(-6811, 79, -9935);

    Material[] saplingBlocks =
    {
        Material.AcaciaSapling,
        Material.BirchSapling,
        Material.DarkOakSapling,
        Material.JungleSapling,
        Material.OakSapling,
        Material.SpruceSapling
    };

    ItemType[] saplingItems =
    {
        ItemType.AcaciaSapling,
        ItemType.BirchSapling,
        ItemType.DarkOakSapling,
        ItemType.JungleSapling,
        ItemType.OakSapling,
        ItemType.SpruceSapling
    };

    bool running = false;
    int treeTypeIndex = 1; // Default birch tree

    public override void Update()
    {
        if (running)
        {
            Material blockType = GetWorld().GetBlock(sapling).Type;
            if (blockType == saplingBlocks[treeTypeIndex]) // Tree not yet grown
                return;
            switch (blockType)
            {
                case Material.Air:
                    // No tree, plant something
                    if (!SwitchToSapling())
                    {
                        LogToConsole("No sapling in hotbar. Refill and start again.");
                        Toggle();
                        break;
                    }
                    SendPlaceBlock(sapling, Direction.Up);
                    break;
            }
        }
    }
    public override void Initialize()
    {
        if (!GetTerrainEnabled())
        {
            LogToConsoleTranslated("extra.terrainandmovement_required");
            UnloadBot();
        }
        else if (!GetInventoryEnabled())
        {
            LogToConsoleTranslated("extra.inventory_required");
            UnloadBot();
        }
        else
        {
            RegisterChatBotCommand("autotree", "AutoTree ChatBot command", "Available commands: toggle, set, type", CommandHandler);
            LogToConsole("Loaded.");
        }
    }

    public bool SetTreeType(int index)
    {
        if (index >= 0 && index < saplingItems.Length)
        {
            treeTypeIndex = index;
            return true;
        }
        else return false;
    }

    public void SetLocation(Location l)
    {
        sapling = l;
    }

    public bool Toggle()
    {
        running = !running;
        return running;
    }

    public bool SwitchToSapling()
    {
        Container p = GetPlayerInventory();
        if (p.Items.ContainsKey(GetCurrentSlot() - 36) 
            && p.Items[GetCurrentSlot() - 36].Type == saplingItems[treeTypeIndex])
        {
            // Already selected
            return true;
        }
        // Search sapling in hotbar
        List<int> result = new List<int>(p.SearchItem(saplingItems[treeTypeIndex]))
            .Where(slot => slot >= 36 && slot <= 44)
            .ToList();
        if (result.Count <= 0)
        {
            return false;
        }
        else
        {
            ChangeSlot((short)(result[0] - 36));
            return true;
        }
    }

    public string CommandHandler(string cmd, string[] args)
    {
        if (args.Length <= 0)
        {
            return "Available commands: toggle, set, type";
        }
        string subCommand = args[0].ToLower();
        switch (subCommand)
        {
            case "toggle":
                {
                    return Toggle() ? "Now is running" : "Now is stopping";
                }
            case "set":
                {
                    if (args.Length < 4)
                    {
                        return "Set the location for placing sapling. Usage: set <x> <y> <z>";
                    }
                    try
                    {
                        int x = int.Parse(args[1]);
                        int y = int.Parse(args[2]);
                        int z = int.Parse(args[3]);
                        var l = new Location(x, y, z);
                        SetLocation(l);
                        return "Location set to " + l.ToString();
                    }
                    catch
                    {
                        return "Please input numbers. Usage: set <x> <y> <z>";
                    }
                }
            case "type":
                {
                    if (args.Length < 2)
                    {
                        return "Set the tree type. Usage: type <Acacia|Birch|Oak|DarkOak|Jungle|Spruce>";
                    }
                    string typeString = args[1].ToLower();
                    for (int i = 0; i < saplingItems.Length; i++)
                    {
                        if (saplingItems[i].ToString().ToLower().StartsWith(typeString))
                        {
                            treeTypeIndex = i;
                            break;
                        }
                    }
                    return "Tree sapling type set to " + saplingItems[treeTypeIndex].ToString();
                }
            default: return "Available commands: toggle, set, type";
        }
    }
}