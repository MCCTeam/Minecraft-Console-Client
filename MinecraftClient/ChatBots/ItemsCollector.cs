using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots;

public class ItemsCollector : ChatBot
{
    public const string CommandName = "itemscollector";
    public static Configs Config = new();

    [TomlDoNotInlineObject]
    public class Configs
    {
        [NonSerialized] private const string BotName = "ItemsCollector";

        public bool Enabled = false;

        [TomlInlineComment("$ChatBot.ItemsCollector.Collect_All_Item_Types$")]
        public bool Collect_All_Item_Types = true;

        [TomlInlineComment("$ChatBot.ItemsCollector.Items_Whitelist$")]
        public List<ItemType> Items_Whitelist = new() { ItemType.Diamond, ItemType.NetheriteIngot };

        [TomlInlineComment("$ChatBot.ItemsCollector.Delay_Between_Tasks$")]
        public int Delay_Between_Tasks = 300;

        [TomlInlineComment("$ChatBot.ItemsCollector.Collection_Radius$")]
        public double Collection_Radius = 30.0;

        [TomlInlineComment("$ChatBot.ItemsCollector.Always_Return_To_Start$")]
        public bool Always_Return_To_Start = true;

        [TomlInlineComment("$ChatBot.ItemsCollector.Prioritize_Clusters$")]
        public bool Prioritize_Clusters = false;

        public void OnSettingUpdate()
        {
            if (Delay_Between_Tasks < 100)
                Delay_Between_Tasks = 100;
        }
    }

    private bool running = false;
    private Thread? mainProcessThread;

    public override void Initialize()
    {
        if (!GetEntityHandlingEnabled())
        {
            LogToConsole(Translations.extra_entity_required);
            LogToConsole(Translations.general_bot_unload);
            UnloadBot();
            return;
        }

        if (!GetTerrainEnabled())
        {
            LogToConsole(Translations.extra_terrainandmovement_required);
            LogToConsole(Translations.general_bot_unload);
            UnloadBot();
            return;
        }

        McClient.dispatcher.Register(l => l.Literal("help")
            .Then(l => l.Literal(CommandName)
                .Executes(r => OnCommandHelp(r.Source, string.Empty)))
        );

        McClient.dispatcher.Register(l => l.Literal(CommandName)
            .Then(l => l.Literal("start")
                .Executes(r => OnCommandStart(r.Source)))
            .Then(l => l.Literal("stop")
                .Executes(r => OnCommandStop(r.Source)))
            .Then(l => l.Literal("_help")
                .Executes(r => OnCommandHelp(r.Source, string.Empty))
                .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
        );
    }

    public override void OnUnload()
    {
        StopTheMainProcess();
        McClient.dispatcher.Unregister(CommandName);
        McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
    }

    private int OnCommandHelp(CmdResult r, string? cmd)
    {
        return r.SetAndReturn(cmd switch
        {
#pragma warning disable format // @formatter:off
                _           =>   Translations.cmd_items_collector_desc + ": " + Translations.cmd_items_collector_usage
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
        });
    }

    private int OnCommandStart(CmdResult r)
    {
        if (running)
            return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_items_collector_already_collecting);

        var movementLock = BotMovementLock.Instance;
        if (movementLock is { IsLocked: true })
            return r.SetAndReturn(CmdResult.Status.Fail,
                string.Format(Translations.bot_common_movement_lock_held, "Items Collector", movementLock.LockedBy));

        if (!movementLock!.Lock("Items Collector"))
        {
            LogToConsole($"§§6§1§0Items Collector bot failed to obtain the movement lock for some reason!");
            LogToConsole($"§§6§1§0Disable other bots who have movement mechanics, and try again!");
            return r.SetAndReturn(CmdResult.Status.Fail);
        }

        StartTheMainProcess();
        return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_items_collector_started);
    }

    private int OnCommandStop(CmdResult r)
    {
        if (!running)
            return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_items_collector_already_not_collecting);

        StopTheMainProcess();
        return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_items_collector_stopping);
    }

    private void StartTheMainProcess()
    {
        running = true;
        mainProcessThread = new Thread(MainProcess);
        mainProcessThread.Start();
    }

    private void StopTheMainProcess()
    {
        running = false;
        BotMovementLock.Instance?.UnLock("Items Collector");
    }

    private void MainProcess()
    {
        var startingLocation = GetCurrentLocation();

        while (running)
        {
            var currentLocation = GetCurrentLocation();
            var items = GetEntities()
                .Where(x =>
                    x.Value.Type == EntityType.Item &&
                    x.Value.Location.Distance(currentLocation) <= Config.Collection_Radius &&
                    (Config.Collect_All_Item_Types || Config.Items_Whitelist.Contains(x.Value.Item.Type)))
                .Select(x => x.Value)
                .ToList();

            if (Config.Prioritize_Clusters && items.Count > 1)
            {
                var centroid = new Location(
                    items.Average(x => x.Location.X),
                    items.Average(x => x.Location.Y),
                    items.Average(x => x.Location.Z)
                );

                items = items.OrderBy(x => x.Location.Distance(centroid) / items.Count).ToList();
            }
            else items = items.OrderBy(x => x.Location.Distance(currentLocation)).ToList();

            if (items.Any())
            {
                foreach (var entity in items.TakeWhile(entity => running))
                    WaitForMoveToLocation(entity.Location);
            }
            else
            {
                if (startingLocation.Distance(currentLocation) > 2f && Config.Always_Return_To_Start)
                    WaitForMoveToLocation(startingLocation);
            }

            if (!running)
                break;

            Thread.Sleep(Config.Delay_Between_Tasks);
        }

        LogToConsole(Translations.cmd_items_collector_stopped);
    }

    public override void AfterGameJoined()
    {
        StopTheMainProcess();
    }

    public override bool OnDisconnect(DisconnectReason reason, string message)
    {
        StopTheMainProcess();
        return true;
    }

    private bool WaitForMoveToLocation(Location location, float tolerance = 1f)
    {
        if (!MoveToLocation(location)) return false;
        while (GetCurrentLocation().Distance(location) > tolerance)
            Thread.Sleep(200);

        return true;
    }
}