﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
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
            [NonSerialized]
            private const string BotName = "Farmer";

            public bool Enabled = false;

            [TomlInlineComment("$config.ChatBot.Farmer.Delay_Between_Tasks$")]
            public double Delay_Between_Tasks = 1.0;

            public void OnSettingUpdate()
            {
                if (Delay_Between_Tasks < 1.0)
                    Delay_Between_Tasks = 1.0;
            }
        }

        public enum State
        {
            SearchingForCropsToBreak = 0,
            SearchingForFarmlandToPlant,
            PlantingCrops,
            BonemealingCrops
        }

        public enum CropType
        {
            Beetroot,
            Carrot,
            Melon,
            Netherwart,
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

        public int Delay_Between_Tasks_Millisecond => (int)Math.Round(Config.Delay_Between_Tasks * 1000);

        private const string commandDescription = "farmer <start <crop type> [radius:<radius = 30>] [unsafe:<true/false>] [teleport:<true/false>] [debug:<true/false>]|stop>";

        public override void Initialize(CommandDispatcher<CmdResult> dispatcher)
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

            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("stop")
                    .Executes(r => OnCommandStop(r.Source)))
                .Then(l => l.Literal("start")
                    .Then(l => l.Argument("CropType", MccArguments.FarmerCropType())
                        .Executes(r => OnCommandStart(r.Source, MccArguments.GetFarmerCropType(r, "CropType"), null))
                        .Then(l => l.Argument("OtherArgs", Arguments.GreedyString())
                            .Executes(r => OnCommandStart(r.Source, MccArguments.GetFarmerCropType(r, "CropType"), Arguments.GetString(r, "OtherArgs"))))))
                .Then(l => l.Literal("_help")
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );
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

            int radius = 30;

            state = State.SearchingForFarmlandToPlant;
            cropType = whatToFarm;
            allowUnsafe = false;
            allowTeleport = false;
            debugEnabled = false;

            if (!string.IsNullOrWhiteSpace(otherArgs))
            {
                string[] args = otherArgs.ToLower().Split(' ', StringSplitOptions.TrimEntries);
                foreach (string currentArg in args)
                {
                    if (!currentArg.Contains(':'))
                    {
                        LogToConsole("§x§1§0" + string.Format(Translations.bot_farmer_warining_invalid_parameter, currentArg));
                        continue;
                    }

                    string[] parts = currentArg.Split(":", StringSplitOptions.TrimEntries);

                    if (parts.Length != 2)
                    {
                        LogToConsole("§x§1§0" + string.Format(Translations.bot_farmer_warining_invalid_parameter, currentArg));
                        continue;
                    }

                    switch (parts[0])
                    {
                        case "r":
                        case "radius":
                            if (!int.TryParse(parts[1], NumberStyles.Any, CultureInfo.CurrentCulture, out radius))
                                LogToConsole("§x§1§0" + Translations.bot_farmer_invalid_radius);

                            if (radius <= 0)
                            {
                                LogToConsole("§x§1§0" + Translations.bot_farmer_invalid_radius);
                                radius = 30;
                            }

                            break;

                        case "f":
                        case "unsafe":
                            if (allowUnsafe)
                                break;

                            if (parts[1].Equals("true") || parts[1].Equals("1"))
                            {
                                LogToConsole("§x§1§0" + Translations.bot_farmer_warining_force_unsafe);
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
                                LogToConsole("§w§1§f" + Translations.bot_farmer_warining_allow_teleport);
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
            new Thread(() => MainPorcess()).Start();

            return r.SetAndReturn(CmdResult.Status.Done);
        }

        public override void AfterGameJoined()
        {
            running = false;
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            running = false;
            return true;
        }

        private void MainPorcess()
        {
            LogToConsole("§y§1§f" + Translations.bot_farmer_started);
            LogToConsole("§y§1§f " + Translations.bot_farmer_crop_type + ": " + cropType);
            LogToConsole("§y§1§f " + Translations.bot_farmer_radius + ": " + farmingRadius);

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

                        ItemType cropTypeToPlant = GetSeedItemTypeForCropType(cropType);

                        // If we don't have any seeds on our hotbar, skip this step and try collecting some
                        if (!SwitchToItem(cropTypeToPlant))
                        {
                            LogDebug("No seeds, trying to find some crops to break");
                            state = State.SearchingForCropsToBreak;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        List<Location> farmlandToPlantOn = findEmptyFarmland(farmingRadius);

                        if (farmlandToPlantOn.Count == 0)
                        {
                            LogDebug("Could not find any farmland, trying to find some crops to break");
                            state = State.SearchingForCropsToBreak;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        int i = 0;
                        foreach (Location location in farmlandToPlantOn)
                        {
                            if (!running) break;

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

                            double yValue = Math.Floor(location.Y) + 1;

                            // TODO: Figure out why this is not working.
                            // Why we need this: sometimes the server kicks the player for "invalid movement" packets.
                            /*if (cropType == CropType.Netherwart)
                                yValue = (double)(Math.Floor(location.Y) - 1.0) + (double)0.87500;*/

                            Location location2 = new Location(Math.Floor(location.X) + 0.5, yValue, Math.Floor(location.Z) + 0.5);

                            if (WaitForMoveToLocation(location2))
                            {
                                LogDebug("Moving to: " + location2);

                                // Stop if we do not have any more seeds left
                                if (!SwitchToItem(GetSeedItemTypeForCropType(cropType)))
                                {
                                    LogDebug("No seeds, trying to find some crops to break");
                                    break;
                                }

                                Location loc = new Location(Math.Floor(location.X), Math.Floor(location2.Y), Math.Floor(location.Z));
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

                        List<Location> cropsToCollect = findCrops(farmingRadius, cropType, true);

                        if (cropsToCollect.Count == 0)
                        {
                            LogToConsole("No crops to break, trying to bonemeal ungrown ones");
                            state = State.BonemealingCrops;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        // Switch to an axe for faster breaking if the bot has one in his inventory
                        if (cropType == CropType.Melon || cropType == CropType.Pumpkin)
                        {
                            // Start from Diamond axe, if not found, try a tier lower axe
                            bool switched = SwitchToItem(ItemType.DiamondAxe);

                            if (!switched)
                                switched = SwitchToItem(ItemType.IronAxe);

                            if (!switched)
                                switched = SwitchToItem(ItemType.GoldenAxe);

                            if (!switched)
                                SwitchToItem(ItemType.StoneAxe);
                        }

                        foreach (Location location in cropsToCollect)
                        {
                            if (!running) break;

                            // God damn C# rounding it to 0.94
                            // This will be needed when bot bonemeals carrots or potatoes which are at the first stage of growth,
                            // because sometimes the bot walks over crops and breaks them
                            // TODO: Figure out a fix
                            // new Location(Math.Floor(location.X) + 0.5, (double)((location.Y - 1) + (double)0.93750), Math.Floor(location.Z) + 0.5)

                            if (WaitForMoveToLocation(location))
                                WaitForDigBlock(location);

                            // Allow some time to pickup the item
                            Thread.Sleep(cropType == CropType.Melon || cropType == CropType.Pumpkin ? 400 : 200);
                        }

                        LogDebug("Finished breaking crops!");
                        state = State.BonemealingCrops;
                        break;

                    case State.BonemealingCrops:
                        // Can't be bonemealed
                        if (cropType == CropType.Netherwart)
                        {
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        // If we don't have any bonemeal on our hotbar, skip this step
                        if (!SwitchToItem(ItemType.BoneMeal))
                        {
                            LogDebug("No bonemeal, searching for some farmland to plant seeds on");
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        List<Location> cropsToBonemeal = findCrops(farmingRadius, cropType, false);

                        if (cropsToBonemeal.Count == 0)
                        {
                            LogDebug("No crops to bonemeal, searching for farmland to plant seeds on");
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Delay_Between_Tasks_Millisecond);
                            continue;
                        }

                        int i2 = 0;
                        foreach (Location location in cropsToBonemeal)
                        {
                            if (!running) break;

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
                                // Stop if we do not have any more bonemeal left
                                if (!SwitchToItem(ItemType.BoneMeal))
                                {
                                    LogDebug("No bonemeal, searching for some farmland to plant seeds on...");
                                    break;
                                }

                                Location location2 = new Location(Math.Floor(location.X) + 0.5, location.Y, Math.Floor(location.Z) + 0.5);
                                LogDebug("Trying to bonemeal: " + location2);

                                // Send like 4 bonemeal attempts, it should do the job with 2-3, but sometimes doesn't do
                                for (int boneMealTimes = 0; boneMealTimes < (cropType == CropType.Beetroot ? 6 : 5); boneMealTimes++)
                                {
                                    // TODO: Do a check if the carrot/potato is on the first growth stage
                                    // if so, use: new Location(location.X, (double)(location.Y - 1) + (double)0.93750, location.Z)
                                    SendPlaceBlock(location2, Direction.Down);
                                }

                                Thread.Sleep(100);
                            }

                            i2++;
                        }

                        LogDebug("Finished bonemealing crops!");
                        state = State.SearchingForFarmlandToPlant;
                        break;
                }

                LogDebug(string.Format("Waiting for {0:0.00} seconds for next cycle.", Config.Delay_Between_Tasks));
                Thread.Sleep(Delay_Between_Tasks_Millisecond);
            }

            LogToConsole(Translations.bot_farmer_stopped);
        }

        private Material GetMaterialForCropType(CropType type)
        {
            switch (type)
            {
                case CropType.Beetroot:
                    return Material.Beetroots;

                case CropType.Carrot:
                    return Material.Carrots;

                case CropType.Melon:
                    return Material.Melon;

                case CropType.Netherwart:
                    return Material.NetherWart;

                case CropType.Pumpkin:
                    return Material.Pumpkin;

                case CropType.Potato:
                    return Material.Potatoes;

                case CropType.Wheat:
                    return Material.Wheat;
            }

            throw new Exception("Material type for " + type.GetType().Name + " has not been mapped!");
        }

        private ItemType GetSeedItemTypeForCropType(CropType type)
        {
            switch (type)
            {
                case CropType.Beetroot:
                    return ItemType.BeetrootSeeds;

                case CropType.Carrot:
                    return ItemType.Carrot;

                case CropType.Melon:
                    return ItemType.MelonSeeds;

                case CropType.Netherwart:
                    return ItemType.NetherWart;

                case CropType.Pumpkin:
                    return ItemType.PumpkinSeeds;

                case CropType.Potato:
                    return ItemType.Potato;

                case CropType.Wheat:
                    return ItemType.WheatSeeds;
            }

            throw new Exception("Seed type for " + type.GetType().Name + " has not been mapped!");
        }

        private List<Location> findEmptyFarmland(int radius)
        {
            return GetWorld()
                 .FindBlock(GetCurrentLocation(), cropType == CropType.Netherwart ? Material.SoulSand : Material.Farmland, radius)
                 .Where(location => GetWorld().GetBlock(new Location(location.X, location.Y + 1, location.Z)).Type == Material.Air)
                 .ToList();
        }

        private List<Location> findCrops(int radius, CropType cropType, bool fullyGrown)
        {
            Material material = GetMaterialForCropType(cropType);

            // A bit of a hack to enable bonemealing melon and pumpkin stems
            if (!fullyGrown && (cropType == CropType.Melon || cropType == CropType.Pumpkin))
                material = cropType == CropType.Melon ? Material.MelonStem : Material.PumpkinStem;

            return GetWorld()
                .FindBlock(GetCurrentLocation(), material, radius)
                .Where(location =>
                {
                    if (fullyGrown && (material == Material.Melon || material == Material.Pumpkin))
                        return true;

                    bool isFullyGrown = IsCropFullyGrown(GetWorld().GetBlock(location), cropType);
                    return fullyGrown ? isFullyGrown : !isFullyGrown;
                })
                .ToList();
        }

        private bool IsCropFullyGrown(Block block, CropType cropType)
        {
            int protocolVersion = GetProtocolVersion();

            switch (cropType)
            {
                case CropType.Beetroot:
                    if (protocolVersion >= Protocol18Handler.MC_1_19_Version && protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
                    {
                        if (block.BlockId == 10103)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_17_Version && protocolVersion <= Protocol18Handler.MC_1_18_2_Version)
                    {
                        if (block.BlockId == 9472)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_16_Version && protocolVersion <= Protocol18Handler.MC_1_16_5_Version)
                    {
                        if (block.BlockId == 9226)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_14_Version && protocolVersion <= Protocol18Handler.MC_1_15_2_Version)
                    {
                        if (block.BlockId == 8686)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_13_Version && protocolVersion < Protocol18Handler.MC_1_14_Version)
                    {
                        if (block.BlockId == 8162)
                            return true;
                    }

                    break;

                case CropType.Carrot:
                    if (protocolVersion >= Protocol18Handler.MC_1_19_Version && protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
                    {
                        if (block.BlockId == 6930)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_17_Version && protocolVersion <= Protocol18Handler.MC_1_18_2_Version)
                    {
                        if (block.BlockId == 6543)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_16_Version && protocolVersion <= Protocol18Handler.MC_1_16_5_Version)
                    {
                        if (block.BlockId == 6341)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_14_Version && protocolVersion <= Protocol18Handler.MC_1_15_2_Version)
                    {
                        if (block.BlockId == 5801)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_13_Version && protocolVersion < Protocol18Handler.MC_1_14_Version)
                    {
                        if (block.BlockId == 5295)
                            return true;
                    }

                    break;

                // Checkin for stems and attached stems instead of Melons themselves
                case CropType.Melon:
                    if (protocolVersion >= Protocol18Handler.MC_1_19_Version && protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
                    {
                        if (block.BlockId == 5166 || block.BlockId == 5150)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_17_Version && protocolVersion <= Protocol18Handler.MC_1_18_2_Version)
                    {
                        if (block.BlockId == 4860 || block.BlockId == 4844)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_16_Version && protocolVersion <= Protocol18Handler.MC_1_16_5_Version)
                    {
                        if (block.BlockId == 4791 || block.BlockId == 4775)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_14_Version && protocolVersion <= Protocol18Handler.MC_1_15_2_Version)
                    {
                        if (block.BlockId == 4771 || block.BlockId == 4755)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_13_Version && protocolVersion < Protocol18Handler.MC_1_14_Version)
                    {
                        if (block.BlockId == 4268 || block.BlockId == 4252)
                            return true;
                    }
                    break;

                // Checkin for stems and attached stems instead of Melons themselves
                case CropType.Netherwart:
                    if (protocolVersion >= Protocol18Handler.MC_1_19_Version && protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
                    {
                        if (block.BlockId == 5718)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_17_Version && protocolVersion <= Protocol18Handler.MC_1_18_2_Version)
                    {
                        if (block.BlockId == 5332)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_16_Version && protocolVersion <= Protocol18Handler.MC_1_16_5_Version)
                    {
                        if (block.BlockId == 5135)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_14_Version && protocolVersion <= Protocol18Handler.MC_1_15_2_Version)
                    {
                        if (block.BlockId == 5115)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_13_Version && protocolVersion < Protocol18Handler.MC_1_14_Version)
                    {
                        if (block.BlockId == 4612)
                            return true;
                    }
                    break;

                // Checkin for stems and attached stems instead of Pumpkins themselves
                case CropType.Pumpkin:
                    if (protocolVersion >= Protocol18Handler.MC_1_19_Version && protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
                    {
                        if (block.BlockId == 5158 || block.BlockId == 5146)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_17_Version && protocolVersion <= Protocol18Handler.MC_1_18_2_Version)
                    {
                        if (block.BlockId == 4852 || block.BlockId == 4840)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_16_Version && protocolVersion <= Protocol18Handler.MC_1_16_5_Version)
                    {
                        if (block.BlockId == 4783 || block.BlockId == 4771)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_14_Version && protocolVersion <= Protocol18Handler.MC_1_15_2_Version)
                    {
                        if (block.BlockId == 4763 || block.BlockId == 4751)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_13_Version && protocolVersion < Protocol18Handler.MC_1_14_Version)
                    {
                        if (block.BlockId == 4260 || block.BlockId == 4248)
                            return true;
                    }
                    break;

                case CropType.Potato:
                    if (protocolVersion >= Protocol18Handler.MC_1_19_Version && protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
                    {
                        if (block.BlockId == 6938)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_17_Version && protocolVersion <= Protocol18Handler.MC_1_18_2_Version)
                    {
                        if (block.BlockId == 6551)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_16_Version && protocolVersion <= Protocol18Handler.MC_1_16_5_Version)
                    {
                        if (block.BlockId == 6349)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_14_Version && protocolVersion <= Protocol18Handler.MC_1_15_2_Version)
                    {
                        if (block.BlockId == 5809)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_13_Version && protocolVersion < Protocol18Handler.MC_1_14_Version)
                    {
                        if (block.BlockId == 5303)
                            return true;
                    }

                    break;

                case CropType.Wheat:
                    if (protocolVersion >= Protocol18Handler.MC_1_19_Version && protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
                    {
                        if (block.BlockId == 3619)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_17_Version && protocolVersion <= Protocol18Handler.MC_1_18_2_Version)
                    {
                        if (block.BlockId == 3421)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_16_Version && protocolVersion <= Protocol18Handler.MC_1_16_5_Version)
                    {
                        if (block.BlockId == 3364)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_14_Version && protocolVersion <= Protocol18Handler.MC_1_15_2_Version)
                    {
                        if (block.BlockId == 3362)
                            return true;
                    }
                    else if (protocolVersion >= Protocol18Handler.MC_1_13_Version && protocolVersion < Protocol18Handler.MC_1_14_Version)
                    {
                        if (block.BlockId == 3059)
                            return true;
                    }

                    break;
            }

            return false;
        }

        // Yoinked from ReinforceZwei's AutoTree and adapted to search the whole of inventory in additon to the hotbar
        private bool SwitchToItem(ItemType itemType)
        {
            Container playerInventory = GetPlayerInventory();

            if (playerInventory.Items.ContainsKey(GetCurrentSlot() - 36)
                && playerInventory.Items[GetCurrentSlot() - 36].Type == itemType)
                return true; // Already selected

            // Search the full inventory
            List<int> fullInventorySearch = new List<int>(playerInventory.SearchItem(itemType));

            // Search for the seed in the hotbar
            List<int> hotbarSerch = fullInventorySearch.Where(slot => slot >= 36 && slot <= 44).ToList();

            if (hotbarSerch.Count > 0)
            {
                ChangeSlot((short)(hotbarSerch[0] - 36));
                return true;
            }

            if (fullInventorySearch.Count == 0)
                return false;

            ItemMovingHelper movingHelper = new ItemMovingHelper(playerInventory, Handler);
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
            if (DigBlock(block.ToFloor()))
            {
                short i = 0; // Maximum wait time of 10 sec.
                while (GetWorld().GetBlock(block).Type != Material.Air && i <= digTimeout)
                {
                    Thread.Sleep(100);
                    i++;
                }

                return i <= digTimeout;
            }

            return false;
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
