using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Commands;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class Farmer : ChatBot
    {
        public const string CommandName = "farmer";

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized] private const string BotName = "Farmer";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.Farmer.Delay_Between_Tasks$")]
            public double Delay_Between_Tasks = 1.0;

            public void OnSettingUpdate()
            {
                if (Delay_Between_Tasks < 1.0)
                    Delay_Between_Tasks = 1.0;
            }
        }

        private enum State
        {
            SearchingForCropsToBreak = 0,
            SearchingForFarmlandToPlant,
            BoneMealingCrops,
            CollectingItems
        }

        public enum CropType
        {
            Beetroot,
            Carrot,
            Melon,
            NetherWart,
            Pumpkin,
            Potato,
            Wheat
        }

        private State state = State.SearchingForCropsToBreak;
        private CropType cropType = CropType.Wheat;
        private int farmingRadius = 30;
        private bool running = false;
        private bool allowUnsafe = false;
        private bool allowTeleport = false;
        private bool debugEnabled = false;

        private int Delay_Between_Tasks_Millisecond => (int)Math.Round(Config.Delay_Between_Tasks * 1000);

        private const string commandDescription =
            "farmer <start <crop type> [radius:<radius = 30>] [unsafe:<true/false>] [teleport:<true/false>] [debug:<true/false>]|stop>";

        public override void Initialize()
        {
            if (GetProtocolVersion() < Protocol18Handler.MC_1_13_Version)
            {
                LogToConsole(Translations.bot_farmer_not_implemented);
                return;
            }

            if (!GetTerrainEnabled())
            {
                LogToConsole(Translations.bot_farmer_needs_terrain);
                return;
            }

            if (!GetInventoryEnabled())
            {
                LogToConsole(Translations.bot_farmer_needs_inventory);
                return;
            }

            McClient.dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                )
            );

            McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("stop")
                    .Executes(r => OnCommandStop(r.Source)))
                .Then(l => l.Literal("start")
                    .Then(l => l.Argument("CropType", MccArguments.FarmerCropType())
                        .Executes(r => OnCommandStart(r.Source, MccArguments.GetFarmerCropType(r, "CropType"), null))
                        .Then(l => l.Argument("OtherArgs", Arguments.GreedyString())
                            .Executes(r => OnCommandStart(r.Source, MccArguments.GetFarmerCropType(r, "CropType"),
                                Arguments.GetString(r, "OtherArgs"))))))
                .Then(l => l.Literal("_help")
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );
        }

        public override void OnUnload()
        {
            running = false;
            BotMovementLock.Instance?.UnLock("Farmer");
            McClient.dispatcher.Unregister(CommandName);
            McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
        }

        private int OnCommandHelp(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                _           =>   Translations.bot_farmer_desc + ": " + commandDescription
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandStop(CmdResult r)
        {
            if (!running)
            {
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.bot_farmer_already_stopped);
            }
            else
            {
                running = false;
                return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_farmer_stopping);
            }
        }

        private int OnCommandStart(CmdResult r, CropType whatToFarm, string? otherArgs)
        {
            if (running)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.bot_farmer_already_running);

            var movementLock = BotMovementLock.Instance;
            if (movementLock is { IsLocked: true })
                return r.SetAndReturn(CmdResult.Status.Fail,
                    string.Format(Translations.bot_common_movement_lock_held, "Farmer", movementLock.LockedBy));

            var radius = 30;

            state = State.SearchingForFarmlandToPlant;
            cropType = whatToFarm;
            allowUnsafe = false;
            allowTeleport = false;
            debugEnabled = false;

            if (!string.IsNullOrWhiteSpace(otherArgs))
            {
                var args = otherArgs.ToLower().Split(' ', StringSplitOptions.TrimEntries);
                foreach (var currentArg in args)
                {
                    if (!currentArg.Contains(':'))
                    {
                        LogToConsole(
                            $"§§6§1§0{string.Format(Translations.bot_farmer_warining_invalid_parameter, currentArg)}");
                        continue;
                    }

                    var parts = currentArg.Split(":", StringSplitOptions.TrimEntries);

                    if (parts.Length != 2)
                    {
                        LogToConsole(
                            $"§§6§1§0{string.Format(Translations.bot_farmer_warining_invalid_parameter, currentArg)}");
                        continue;
                    }

                    switch (parts[0])
                    {
                        case "r":
                        case "radius":
                            if (!int.TryParse(parts[1], NumberStyles.Any, CultureInfo.CurrentCulture, out radius))
                                LogToConsole($"§§6§1§0{Translations.bot_farmer_invalid_radius}");

                            if (radius <= 0)
                            {
                                LogToConsole($"§§6§1§0{Translations.bot_farmer_invalid_radius}");
                                radius = 30;
                            }

                            break;

                        case "f":
                        case "unsafe":
                            if (allowUnsafe)
                                break;

                            if (parts[1].Equals("true") || parts[1].Equals("1"))
                            {
                                LogToConsole($"§§6§1§0{Translations.bot_farmer_warining_force_unsafe}");
                                allowUnsafe = true;
                            }
                            else allowUnsafe = false;

                            break;

                        case "t":
                        case "teleport":
                            if (allowTeleport)
                                break;

                            if (parts[1].Equals("true") || parts[1].Equals("1"))
                            {
                                LogToConsole($"§§4§1§f{Translations.bot_farmer_warining_allow_teleport}");
                                allowTeleport = true;
                            }
                            else allowTeleport = false;

                            break;

                        case "d":
                        case "debug":
                            if (debugEnabled)
                                break;

                            if (parts[1].Equals("true") || parts[1].Equals("1"))
                            {
                                LogToConsole("Debug enabled!");
                                debugEnabled = true;
                            }
                            else debugEnabled = false;

                            break;
                    }
                }
            }

            farmingRadius = radius;
            running = true;
            new Thread(() => MainProcess()).Start();

            return r.SetAndReturn(CmdResult.Status.Done);
        }

        public override void AfterGameJoined()
        {
            BotMovementLock.Instance?.UnLock("Farmer");
            running = false;
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            BotMovementLock.Instance?.UnLock("Farmer");
            running = false;
            return true;
        }

        private void MainProcess()
        {
            var movementLock = BotMovementLock.Instance;
            switch (movementLock)
            {
                case { IsLocked: false }:
                    if (!movementLock.Lock("Farmer"))
                    {
                        running = false;
                        LogToConsole($"§§6§1§0Farmer bot failed to obtain the movement lock for some reason!");
                        LogToConsole($"§§6§1§0Disable other bots who have movement mechanics, and try again!");
                        return;
                    }

                    LogDebug($"Locked the movement for other bots!");
                    break;
                case { IsLocked: true }:
                    running = false;
                    LogToConsole($"§§6§1§0Farmer bot failed to obtain the movement lock for some reason!");
                    LogToConsole($"§§6§1§0Disable other bots who have movement mechanics, and try again!");
                    return;
            }

            LogToConsole($"§§2§1§f{Translations.bot_farmer_started}");
            LogToConsole($"§§2§1§f {Translations.bot_farmer_crop_type}: {cropType}");
            LogToConsole($"§§2§1§f {Translations.bot_farmer_radius}: {farmingRadius}");

            var itemTypes = new List<ItemType>
            {
                GetSeedItemTypeForCropType(cropType),
                GetCropItemTypeForCropType(cropType)
            };
            itemTypes = itemTypes.Distinct().ToList();

            while (running)
            {
                // Don't do anything if the bot is currently eating, we wait for 1 second
                if (AutoEat.Eating)
                {
                    LogDebug("Eating...");
                    Thread.Sleep(Delay_Between_Tasks_Millisecond);
                    continue;
                }

                switch (state)
                {
                    case State.SearchingForFarmlandToPlant:
                        LogDebug("Looking for farmland...");

                        var cropTypeToPlant = GetSeedItemTypeForCropType(cropType);

                        // If we don't have any seeds on our hot bar, skip this step and try collecting some
                        if (!SwitchToItem(cropTypeToPlant))
                        {
                            LogDebug("No seeds, trying to find some crops to break");
                            state = State.SearchingForCropsToBreak;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        var farmlandToPlantOn = FindEmptyFarmland(farmingRadius);

                        if (farmlandToPlantOn.Count == 0)
                        {
                            LogDebug("Could not find any farmland, trying to find some crops to break");
                            state = State.SearchingForCropsToBreak;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        var i = 0;
                        foreach (var location in farmlandToPlantOn.TakeWhile(location => running))
                        {
                            // Check only every second iteration, minor optimization xD
                            if (i % 2 == 0)
                            {
                                if (!HasItemOfTypeInInventory(cropTypeToPlant))
                                {
                                    LogDebug("Ran out of seeds, looking for crops to break...");
                                    state = State.SearchingForCropsToBreak;
                                    Thread.Sleep(Delay_Between_Tasks_Millisecond);
                                    continue;
                                }
                            }

                            var yValue = Math.Floor(location.Y) + 1;

                            // TODO: Figure out why this is not working.
                            // Why we need this: sometimes the server kicks the player for "invalid movement" packets.
                            /*if (cropType == CropType.NetherWart)
                                yValue = (double)(Math.Floor(location.Y) - 1.0) + (double)0.87500;*/

                            var location2 = new Location(Math.Floor(location.X) + 0.5, yValue,
                                Math.Floor(location.Z) + 0.5);

                            if (WaitForMoveToLocation(location2))
                            {
                                LogDebug("Moving to: " + location2);

                                // Stop if we do not have any more seeds left
                                if (!SwitchToItem(GetSeedItemTypeForCropType(cropType)))
                                {
                                    LogDebug("No seeds, trying to find some crops to break");
                                    break;
                                }

                                var loc = new Location(Math.Floor(location.X), Math.Floor(location2.Y),
                                    Math.Floor(location.Z));
                                LogDebug("Sending placeblock to: " + loc);

                                SendPlaceBlock(loc, Direction.Up);
                                Thread.Sleep(300);
                            }
                            else LogDebug("Can't move to: " + location2);

                            i++;
                        }

                        LogDebug("Finished planting crops!");
                        state = State.SearchingForCropsToBreak;
                        break;

                    case State.SearchingForCropsToBreak:
                        LogDebug("Searching for crops to break...");

                        var cropsToCollect = findCrops(farmingRadius, cropType, true);

                        if (cropsToCollect.Count == 0)
                        {
                            LogToConsole("No crops to break, trying to bone meal un-grown ones");
                            state = State.BoneMealingCrops;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        // Switch to an axe for faster breaking if the bot has one in his inventory
                        if (cropType is CropType.Melon or CropType.Pumpkin)
                        {
                            // Start from Diamond axe, if not found, try a tier lower axe
                            var switched = SwitchToItem(ItemType.DiamondAxe);

                            if (!switched)
                                switched = SwitchToItem(ItemType.IronAxe);

                            if (!switched)
                                switched = SwitchToItem(ItemType.GoldenAxe);

                            if (!switched)
                                SwitchToItem(ItemType.StoneAxe);
                        }

                        foreach (var location in cropsToCollect.TakeWhile(location => running))
                        {
                            // God damn C# rounding it to 0.94
                            // This will be needed when bot bone meals carrots or potatoes which are at the first stage of growth,
                            // because sometimes the bot walks over crops and breaks them
                            // TODO: Figure out a fix
                            // new Location(Math.Floor(location.X) + 0.5, (double)((location.Y - 1) + (double)0.93750), Math.Floor(location.Z) + 0.5)

                            if (WaitForMoveToLocation(location))
                                WaitForDigBlock(location);

                            // Allow some time to pickup the item
                            Thread.Sleep(cropType is CropType.Melon or CropType.Pumpkin ? 400 : 200);
                        }

                        LogDebug("Finished breaking crops!");
                        state = State.BoneMealingCrops;
                        break;

                    case State.BoneMealingCrops:
                        // Can't be bone mealed
                        if (cropType == CropType.NetherWart)
                        {
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        // If we don't have any bone meal on our hot bar, skip this step
                        if (!SwitchToItem(ItemType.BoneMeal))
                        {
                            LogDebug("No bone meal, searching for some farmland to plant seeds on");
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        var cropsToBonemeal = findCrops(farmingRadius, cropType, false);

                        if (cropsToBonemeal.Count == 0)
                        {
                            LogDebug("No crops to bone meal, searching for farmland to plant seeds on");
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        var i2 = 0;
                        foreach (var location in cropsToBonemeal.TakeWhile(location => running))
                        {
                            // Check only every second iteration, minor optimization xD
                            if (i2 % 2 == 0)
                            {
                                if (!HasItemOfTypeInInventory(ItemType.BoneMeal))
                                {
                                    LogDebug("Ran out of Bone Meal, looking for farmland to plant on...");
                                    state = State.SearchingForFarmlandToPlant;
                                    Thread.Sleep(Delay_Between_Tasks_Millisecond);
                                    continue;
                                }
                            }

                            if (WaitForMoveToLocation(location))
                            {
                                // Stop if we do not have any more bone meal left
                                if (!SwitchToItem(ItemType.BoneMeal))
                                {
                                    LogDebug("No bone meal, searching for some farmland to plant seeds on...");
                                    break;
                                }

                                var location2 = new Location(Math.Floor(location.X) + 0.5, location.Y,
                                    Math.Floor(location.Z) + 0.5);
                                LogDebug("Trying to bone meal: " + location2);

                                // Send like 4 bone meal attempts, it should do the job with 2-3, but sometimes doesn't do
                                for (var boneMealTimes = 0;
                                     boneMealTimes < (cropType == CropType.Beetroot ? 6 : 5);
                                     boneMealTimes++)
                                {
                                    // TODO: Do a check if the carrot/potato is on the first growth stage
                                    // if so, use: new Location(location.X, (double)(location.Y - 1) + (double)0.93750, location.Z)
                                    SendPlaceBlock(location2, Direction.Down);
                                }

                                Thread.Sleep(100);
                            }

                            i2++;
                        }

                        LogDebug("Finished bone mealing crops!");
                        state = State.CollectingItems;
                        break;

                    case State.CollectingItems:
                        LogDebug("Searching for items to collect...");

                        var currentLocation = GetCurrentLocation();
                        var items = GetEntities()
                            .Where(x =>
                                x.Value.Type == EntityType.Item &&
                                x.Value.Location.Distance(currentLocation) <= farmingRadius &&
                                itemTypes.Contains(x.Value.Item.Type))
                            .Select(x => x.Value)
                            .ToList();
                        items = items.OrderBy(x => x.Location.Distance(currentLocation)).ToList();

                        if (items.Any())
                        {
                            LogDebug("Collecting items...");

                            foreach (var entity in items.TakeWhile(entity => running))
                                WaitForMoveToLocation(entity.Location);

                            LogDebug("Finished collecting items!");
                        }
                        else LogDebug("No items to collect!");

                        state = State.SearchingForFarmlandToPlant;
                        break;
                }

                LogDebug($"Waiting for {Config.Delay_Between_Tasks:0.00} seconds for next cycle.");
                Thread.Sleep(Delay_Between_Tasks_Millisecond);
            }

            movementLock?.UnLock("Farmer");
            LogDebug($"Unlocked the movement for other bots!");
            LogToConsole(Translations.bot_farmer_stopped);
        }

        private static Material GetMaterialForCropType(CropType type)
        {
            return type switch
            {
                CropType.Beetroot => Material.Beetroots,
                CropType.Carrot => Material.Carrots,
                CropType.Melon => Material.Melon,
                CropType.NetherWart => Material.NetherWart,
                CropType.Pumpkin => Material.Pumpkin,
                CropType.Potato => Material.Potatoes,
                CropType.Wheat => Material.Wheat,
                _ => throw new Exception("Material type for " + type.GetType().Name + " has not been mapped!")
            };
        }

        private static ItemType GetSeedItemTypeForCropType(CropType type)
        {
            return type switch
            {
                CropType.Beetroot => ItemType.BeetrootSeeds,
                CropType.Carrot => ItemType.Carrot,
                CropType.Melon => ItemType.MelonSeeds,
                CropType.NetherWart => ItemType.NetherWart,
                CropType.Pumpkin => ItemType.PumpkinSeeds,
                CropType.Potato => ItemType.Potato,
                CropType.Wheat => ItemType.WheatSeeds,
                _ => throw new Exception("Seed type for " + type.GetType().Name + " has not been mapped!")
            };
        }

        private static ItemType GetCropItemTypeForCropType(CropType type)
        {
            return type switch
            {
                CropType.Beetroot => ItemType.Beetroot,
                CropType.Carrot => ItemType.Carrot,
                CropType.Melon => ItemType.Melon,
                CropType.NetherWart => ItemType.NetherWart,
                CropType.Pumpkin => ItemType.Pumpkin,
                CropType.Potato => ItemType.Potato,
                CropType.Wheat => ItemType.Wheat,
                _ => throw new Exception("Item type for " + type.GetType().Name + " has not been mapped!")
            };
        }

        private List<Location> FindEmptyFarmland(int radius)
        {
            return GetWorld()
                .FindBlock(GetCurrentLocation(),
                    cropType == CropType.NetherWart ? Material.SoulSand : Material.Farmland, radius)
                .Where(location => GetWorld().GetBlock(new Location(location.X, location.Y + 1, location.Z)).Type ==
                                   Material.Air)
                .ToList();
        }

        private List<Location> findCrops(int radius, CropType cropType, bool fullyGrown)
        {
            var material = GetMaterialForCropType(cropType);

            // A bit of a hack to enable bone mealing melon and pumpkin stems
            if (!fullyGrown && cropType is CropType.Melon or CropType.Pumpkin)
                material = cropType == CropType.Melon ? Material.MelonStem : Material.PumpkinStem;

            return GetWorld()
                .FindBlock(GetCurrentLocation(), material, radius)
                .Where(location =>
                {
                    if (fullyGrown && material is Material.Melon or Material.Pumpkin)
                        return true;

                    var isFullyGrown = IsCropFullyGrown(GetWorld().GetBlock(location), cropType);
                    return fullyGrown ? isFullyGrown : !isFullyGrown;
                })
                .ToList();
        }

        private bool IsCropFullyGrown(Block block, CropType cropType)
        {
            var protocolVersion = GetProtocolVersion();

            switch (cropType)
            {
                case CropType.Beetroot:
                    switch (protocolVersion)
                    {
                        case Protocol18Handler.MC_1_20_Version when block.BlockId == 12371:
                        case Protocol18Handler.MC_1_19_4_Version when block.BlockId == 12356:
                        case Protocol18Handler.MC_1_19_3_Version when block.BlockId == 11887:
                        case >= Protocol18Handler.MC_1_19_Version and <= Protocol18Handler.MC_1_19_2_Version
                            when block.BlockId == 10103:
                        case >= Protocol18Handler.MC_1_17_Version and <= Protocol18Handler.MC_1_18_2_Version
                            when block.BlockId == 9472:
                        case >= Protocol18Handler.MC_1_16_Version and <= Protocol18Handler.MC_1_16_5_Version
                            when block.BlockId == 9226:
                        case >= Protocol18Handler.MC_1_14_Version and <= Protocol18Handler.MC_1_15_2_Version
                            when block.BlockId == 8686:
                        case >= Protocol18Handler.MC_1_13_Version and < Protocol18Handler.MC_1_14_Version
                            when block.BlockId == 8162:
                            return true;
                    }

                    break;

                case CropType.Carrot:
                    switch (protocolVersion)
                    {
                        case Protocol18Handler.MC_1_20_Version when block.BlockId == 8602:
                        case Protocol18Handler.MC_1_19_4_Version when block.BlockId == 8598:
                        case Protocol18Handler.MC_1_19_3_Version when block.BlockId == 8370:
                        case >= Protocol18Handler.MC_1_19_Version and <= Protocol18Handler.MC_1_19_2_Version
                            when block.BlockId == 6930:
                        case >= Protocol18Handler.MC_1_17_Version and <= Protocol18Handler.MC_1_18_2_Version
                            when block.BlockId == 6543:
                        case >= Protocol18Handler.MC_1_16_Version and <= Protocol18Handler.MC_1_16_5_Version
                            when block.BlockId == 6341:
                        case >= Protocol18Handler.MC_1_14_Version and <= Protocol18Handler.MC_1_15_2_Version
                            when block.BlockId == 5801:
                        case >= Protocol18Handler.MC_1_13_Version and < Protocol18Handler.MC_1_14_Version
                            when block.BlockId == 5295:
                            return true;
                    }

                    break;

                // Checkin for stems and attached stems instead of Melons themselves
                case CropType.Melon:
                    switch (protocolVersion)
                    {
                        case Protocol18Handler.MC_1_20_Version when block.BlockId is 6836 or 6820:
                        case Protocol18Handler.MC_1_19_4_Version when block.BlockId is 6808 or 6606:
                        case Protocol18Handler.MC_1_19_3_Version when block.BlockId is 6582 or 6832:
                        case >= Protocol18Handler.MC_1_19_Version and <= Protocol18Handler.MC_1_19_2_Version
                            when block.BlockId is 5166 or 5150:
                        case >= Protocol18Handler.MC_1_17_Version and <= Protocol18Handler.MC_1_18_2_Version
                            when block.BlockId is 4860 or 4844:
                        case >= Protocol18Handler.MC_1_16_Version and <= Protocol18Handler.MC_1_16_5_Version
                            when block.BlockId is 4791 or 4775:
                        case >= Protocol18Handler.MC_1_14_Version and <= Protocol18Handler.MC_1_15_2_Version
                            when block.BlockId is 4771 or 4755:
                        case >= Protocol18Handler.MC_1_13_Version and < Protocol18Handler.MC_1_14_Version
                            when block.BlockId is 4268 or 4252:
                            return true;
                    }

                    break;

                case CropType.NetherWart:
                    switch (protocolVersion)
                    {
                        case Protocol18Handler.MC_1_20_Version when block.BlockId == 7388:
                        case Protocol18Handler.MC_1_19_4_Version when block.BlockId == 7384:
                        case Protocol18Handler.MC_1_19_3_Version when block.BlockId == 7158:
                        case >= Protocol18Handler.MC_1_19_Version and <= Protocol18Handler.MC_1_19_2_Version
                            when block.BlockId == 5718:
                        case >= Protocol18Handler.MC_1_17_Version and <= Protocol18Handler.MC_1_18_2_Version
                            when block.BlockId == 5332:
                        case >= Protocol18Handler.MC_1_16_Version and <= Protocol18Handler.MC_1_16_5_Version
                            when block.BlockId == 5135:
                        case >= Protocol18Handler.MC_1_14_Version and <= Protocol18Handler.MC_1_15_2_Version
                            when block.BlockId == 5115:
                        case >= Protocol18Handler.MC_1_13_Version and < Protocol18Handler.MC_1_14_Version
                            when block.BlockId == 4612:
                            return true;
                    }

                    break;

                // Checkin for stems and attached stems instead of Pumpkins themselves
                case CropType.Pumpkin:
                    switch (protocolVersion)
                    {
                        case Protocol18Handler.MC_1_20_Version when block.BlockId is 5849 or 6816:
                        case Protocol18Handler.MC_1_19_4_Version when block.BlockId is 5845 or 6824:
                        case Protocol18Handler.MC_1_19_3_Version when block.BlockId is 5683 or 6598:
                        case >= Protocol18Handler.MC_1_19_Version and <= Protocol18Handler.MC_1_19_2_Version
                            when block.BlockId is 5158 or 5146:
                        case >= Protocol18Handler.MC_1_17_Version and <= Protocol18Handler.MC_1_18_2_Version
                            when block.BlockId is 4852 or 4840:
                        case >= Protocol18Handler.MC_1_16_Version and <= Protocol18Handler.MC_1_16_5_Version
                            when block.BlockId is 4783 or 4771:
                        case >= Protocol18Handler.MC_1_14_Version and <= Protocol18Handler.MC_1_15_2_Version
                            when block.BlockId is 4763 or 4751:
                        case >= Protocol18Handler.MC_1_13_Version and < Protocol18Handler.MC_1_14_Version
                            when block.BlockId is 4260 or 4248:
                            return true;
                    }

                    break;

                case CropType.Potato:
                    switch (protocolVersion)
                    {
                        case Protocol18Handler.MC_1_20_Version when block.BlockId == 8610:
                        case Protocol18Handler.MC_1_19_4_Version when block.BlockId == 8606:
                        case Protocol18Handler.MC_1_19_3_Version when block.BlockId == 8378:
                        case >= Protocol18Handler.MC_1_19_Version and <= Protocol18Handler.MC_1_19_2_Version
                            when block.BlockId == 6938:
                        case >= Protocol18Handler.MC_1_17_Version and <= Protocol18Handler.MC_1_18_2_Version
                            when block.BlockId == 6551:
                        case >= Protocol18Handler.MC_1_16_Version and <= Protocol18Handler.MC_1_16_5_Version
                            when block.BlockId == 6349:
                        case >= Protocol18Handler.MC_1_14_Version and <= Protocol18Handler.MC_1_15_2_Version
                            when block.BlockId == 5809:
                        case >= Protocol18Handler.MC_1_13_Version and < Protocol18Handler.MC_1_14_Version
                            when block.BlockId == 5303:
                            return true;
                    }

                    break;

                case CropType.Wheat:
                    switch (protocolVersion)
                    {
                        case Protocol18Handler.MC_1_20_Version when block.BlockId == 4285:
                        case Protocol18Handler.MC_1_19_4_Version when block.BlockId == 4281:
                        case Protocol18Handler.MC_1_19_3_Version when block.BlockId == 4233:
                        case >= Protocol18Handler.MC_1_19_Version and <= Protocol18Handler.MC_1_19_2_Version
                            when block.BlockId == 3619:
                        case >= Protocol18Handler.MC_1_17_Version and <= Protocol18Handler.MC_1_18_2_Version
                            when block.BlockId == 3421:
                        case >= Protocol18Handler.MC_1_16_Version and <= Protocol18Handler.MC_1_16_5_Version
                            when block.BlockId == 3364:
                        case >= Protocol18Handler.MC_1_14_Version and <= Protocol18Handler.MC_1_15_2_Version
                            when block.BlockId == 3362:
                        case >= Protocol18Handler.MC_1_13_Version and < Protocol18Handler.MC_1_14_Version
                            when block.BlockId == 3059:
                            return true;
                    }

                    break;
            }

            return false;
        }

        // Yoinked from ReinforceZwei's AutoTree and adapted to search the whole of inventory in additon to the hotbar
        private bool SwitchToItem(ItemType itemType)
        {
            var playerInventory = GetPlayerInventory();

            if (playerInventory.Items.TryGetValue(GetCurrentSlot() - 36, out var value) && value.Type == itemType)
                return true; // Already selected

            // Search the full inventory
            var fullInventorySearch = new List<int>(playerInventory.SearchItem(itemType));

            // Search for the seed in the hot bar
            var hotBarSearch = fullInventorySearch.Where(slot => slot is >= 36 and <= 44).ToList();

            if (hotBarSearch.Count > 0)
            {
                ChangeSlot((short)(hotBarSearch[0] - 36));
                return true;
            }

            if (fullInventorySearch.Count == 0)
                return false;

            var movingHelper = new ItemMovingHelper(playerInventory, Handler);
            movingHelper.Swap(fullInventorySearch[0], 36);
            ChangeSlot(0);

            return true;
        }

        // Yoinked from Daenges's Sugarcane Farmer
        private bool WaitForMoveToLocation(Location pos, float tolerance = 2f)
        {
            if (MoveToLocation(location: pos, allowUnsafe: allowUnsafe, allowDirectTeleport: allowTeleport))
            {
                LogDebug("Moving to: " + pos);

                while (GetCurrentLocation().Distance(pos) > tolerance)
                    Thread.Sleep(200);

                return true;
            }
            else LogDebug("Can't move to: " + pos);

            return false;
        }

        // Yoinked from Daenges's Sugarcane Farmer
        private bool WaitForDigBlock(Location block, int digTimeout = 1000)
        {
            if (!DigBlock(block.ToFloor())) return false;
            short i = 0; // Maximum wait time of 10 sec.
            while (GetWorld().GetBlock(block).Type != Material.Air && i <= digTimeout)
            {
                Thread.Sleep(100);
                i++;
            }

            return i <= digTimeout;
        }

        private bool HasItemOfTypeInInventory(ItemType itemType)
        {
            return GetPlayerInventory().SearchItem(itemType).Length > 0;
        }

        private void LogDebug(object text)
        {
            if (debugEnabled)
                LogToConsole(text);
            else LogDebugToConsole(text);
        }
    }
}