//MCCScript 1.0
//using Brigadier.NET;
//using Brigadier.NET.Builder;
//using MinecraftClient;
//using MinecraftClient.CommandHandler;
//using MinecraftClient.CommandHandler.Patch;
//using MinecraftClient.Inventory;
//using MinecraftClient.Mapping;
//using MinecraftClient.Scripting;
//using System.Collections.Generic;
//using System.Linq;
//using static MinecraftClient.ChatBots.AutoCraft.Configs;
//using System.Text;

MCC.LoadBot(new AutoTree());

//MCCScript Extensions

public class AutoTree : ChatBot
{
    public const string CommandName = "autotree";

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

    public override void Initialize(CommandDispatcher<CmdResult> dispatcher)
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
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Then(l => l.Literal("set")
                        .Executes(r => OnCommandHelp(r.Source, "set")))
                    .Then(l => l.Literal("type")
                        .Executes(r => OnCommandHelp(r.Source, "type")))
                )
            );

            dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("toggle")
                    .Executes(r => { return r.Source.SetAndReturn(CmdResult.Status.Done, Toggle() ? "Now is running" : "Now is stopping"); }))
                .Then(l => l.Literal("set")
                    .Then(l => l.Argument("Location", MccArguments.Location())
                        .Executes(r => OnCommandSet(r.Source, MccArguments.GetLocation(r, "Location")))))
                .Then(l => l.Literal("type")
                    .Then(l => l.Argument("TreeType", Arguments.String())
                        .Executes(r => OnCommandType(r.Source, Arguments.GetString(r, "TreeType")))))
                .Then(l => l.Literal("_help")
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );

            LogToConsole("Loaded.");
        }
    }

    public override void OnUnload(CommandDispatcher<CmdResult> dispatcher)
    {
        dispatcher.Unregister(CommandName);
        dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
    }

    private int OnCommandHelp(CmdResult r, string? cmd)
    {
        return r.SetAndReturn(cmd switch
        {
#pragma warning disable format // @formatter:off
                "set"       =>   "Set the location for placing sapling. Usage: set <x> <y> <z>",
                "type"      =>   "Set the tree type. Usage: type <Acacia|Birch|Oak|DarkOak|Jungle|Spruce>",
                _           =>   "Available commands: toggle, set, type"
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
    }

    private int OnCommandSet(CmdResult r, Location location)
    {
        SetLocation(location.ToAbsolute(GetCurrentLocation()));
        return r.SetAndReturn(CmdResult.Status.Done, "Location set to " + location.ToString());
    }

    private int OnCommandType(CmdResult r, string treeType)
    {
        for (int i = 0; i < saplingItems.Length; i++)
        {
            if (saplingItems[i].ToString().ToLower().StartsWith(treeType))
            {
                treeTypeIndex = i;
                break;
            }
        }
        return r.SetAndReturn(CmdResult.Status.Done, "Tree sapling type set to " + saplingItems[treeTypeIndex].ToString());
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
}
