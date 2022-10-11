using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    enum State
    {
        SearchingForCropsToBreak = 0,
        SearchingForFarmlandToPlant,
        PlantingCrops,
        BonemealingCrops
    }

    enum CropType
    {
        Beetroot,
        Carrot,
        Melon,
        Netherwart,
        Pumpkin,
        Potato,
        Wheat
    }

    public class Farmer : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "Farmer";

            public bool Enabled = false;

            public int Delay_Between_Tasks = 1;

            public void OnSettingUpdate()
            {
                if (Delay_Between_Tasks <= 0)
                    Delay_Between_Tasks = 1;
            }
        }

        private State state = State.SearchingForCropsToBreak;
        private CropType cropType = CropType.Wheat;
        private int farmingRadius = 30;
        private bool running = false;

        public override void Initialize()
        {
            if (GetProtocolVersion() < Protocol18Handler.MC_1_13_Version)
            {
                LogToConsole("Not implemented bellow 1.13!");
                return;
            }

            if (!GetTerrainEnabled())
            {
                LogToConsole("Terrain handling needed!");
                return;
            }

            if (!GetInventoryEnabled())
            {
                LogToConsole("Inventory handling needed!");
                return;
            }

            RegisterChatBotCommand("farmer", "cmd.farmer.desc", "farmer <start <crop type> <radius>|stop>", OnFarmCommand);
        }

        private string OnFarmCommand(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    if (!running)
                        return Translations.TryGet("cmd.farmer.already_stopped");

                    running = false;
                    return Translations.TryGet("cmd.farmer.stopping");
                }

                if (args[0].Equals("start", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length == 3)
                    {
                        if (running)
                            return Translations.TryGet("cmd.farmer.already_running");

                        if (!Enum.TryParse(args[1], true, out CropType whatToFarm))
                            return Translations.TryGet("cmd.farmer.invalid_crop_type");

                        if (!int.TryParse(args[2], NumberStyles.Any, CultureInfo.CurrentCulture, out int radius))
                            return Translations.TryGet("cmd.farmer.invalid_radius");

                        state = State.SearchingForFarmlandToPlant;
                        cropType = whatToFarm;
                        farmingRadius = radius <= 0 ? 30 : radius;

                        running = true;
                        new Thread(() => MainPorcess()).Start();

                        return Translations.TryGet("cmd.farmer.staring", args[1], farmingRadius);
                    }
                }
            }

            return Translations.TryGet("cmd.farmer.desc") + ": " + Translations.TryGet("cmd.farmer.usage");
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
            LogToConsole("Started");

            while (running)
            {
                // Don't do anything if the bot is currently eating, we wait for 1 second
                if (AutoEat.Eating)
                {
                    LogToConsole("Eating.");
                    Thread.Sleep(Config.Delay_Between_Tasks * 1000);
                    continue;
                }

                switch (state)
                {
                    case State.SearchingForFarmlandToPlant:
                        LogToConsole("Looking for farmland");

                        // If we don't have any seeds on our hotbar, skip this step and try collecting some
                        if (!SwitchToItem(GetSeedItemTypeForCropType(cropType)))
                        {
                            LogToConsole("No seeds, trying to find some crops to break");
                            state = State.SearchingForCropsToBreak;
                            Thread.Sleep(Config.Delay_Between_Tasks * 1000);
                            continue;
                        }

                        List<Location> farmlandToPlantOn = findEmptyFarmland(farmingRadius);

                        if (farmlandToPlantOn.Count == 0)
                        {
                            LogToConsole("Could not find any farmland, trying to find some crops to break");
                            state = State.SearchingForCropsToBreak;
                            Thread.Sleep(Config.Delay_Between_Tasks * 1000);
                            continue;
                        }

                        foreach (Location location in farmlandToPlantOn)
                        {
                            if (!running) break;

                            double yValue = Math.Floor(location.Y) + 1;

                            if (cropType == CropType.Netherwart)
                                yValue = (double)(Math.Floor(location.Y) - 1.0) + (double)0.87500;

                            Location location2 = new Location(Math.Floor(location.X), yValue, Math.Floor(location.Z));

                            if (WaitForMoveToLocation(location2))
                            {
                                LogToConsole("Moving to: " + location2);

                                // Stop if we do not have any more seeds left
                                if (!SwitchToItem(GetSeedItemTypeForCropType(cropType)))
                                {
                                    LogToConsole("No seeds, trying to find some crops to break");
                                    break;
                                }

                                Location loc = new Location(Math.Floor(location.X), Math.Floor(location2.Y), Math.Floor(location.Z));
                                LogToConsole("Sending placeblock to: " + loc);

                                SendPlaceBlock(loc, Direction.Up);
                                Thread.Sleep(300);
                            }
                            else
                            {
                                LogToConsole("Can't move to: " + location2);
                            }
                        }

                        LogToConsole("Finished planting");
                        state = State.SearchingForCropsToBreak;
                        break;

                    case State.SearchingForCropsToBreak:
                        LogToConsole("Searching for crops to break");

                        List<Location> cropsToCollect = findCrops(farmingRadius, cropType, true);

                        if (cropsToCollect.Count == 0)
                        {
                            LogToConsole("No crops to break, trying to bonemeal ungrown ones");
                            state = State.BonemealingCrops;
                            Thread.Sleep(Config.Delay_Between_Tasks * 1000);
                            continue;
                        }

                        foreach (Location location in cropsToCollect)
                        {
                            if (!running) break;

                            // God damn C# rounding Y to (Y - 1) + 0.94
                            // TODO: Figure out a fix
                            // new Location(Math.Floor(location.X) + 0.5, (double)((double)(location.Y - 1) + (double)0.93750), Math.Floor(location.Z) + 0.5)

                            if (WaitForMoveToLocation(location))
                                WaitForDigBlock(location);

                            // Allow some time to pickup the item
                            Thread.Sleep(cropType == CropType.Melon || cropType == CropType.Pumpkin ? 400 : 200);
                        }

                        LogToConsole("Finished breaking crops");
                        state = State.BonemealingCrops;
                        break;

                    case State.BonemealingCrops:
                        // Can't be bonemealed
                        if (cropType == CropType.Netherwart)
                        {
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Config.Delay_Between_Tasks * 1000);
                            continue;
                        }

                        // If we don't have any bonemeal on our hotbar, skip this step
                        if (!SwitchToItem(ItemType.BoneMeal))
                        {
                            LogToConsole("No bonemeal, searching for some farmland to plant seeds on");
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Config.Delay_Between_Tasks * 1000);
                            continue;
                        }

                        List<Location> cropsToBonemeal = findCrops(farmingRadius, cropType, false);

                        if (cropsToBonemeal.Count == 0)
                        {
                            LogToConsole("No crops to bonemeal, searching for farmland to plant seeds on");
                            state = State.SearchingForFarmlandToPlant;
                            Thread.Sleep(Config.Delay_Between_Tasks * 1000);
                            continue;
                        }

                        foreach (Location location in cropsToBonemeal)
                        {
                            if (WaitForMoveToLocation(location))
                            {
                                if (!running) break;

                                // Stop if we do not have any more bonemeal left
                                if (!SwitchToItem(ItemType.BoneMeal))
                                {
                                    LogToConsole("No bonemeal, searching for some farmland to plant seeds on");
                                    break;
                                }

                                // Send like 5 bonemeal attempts, it should do the job with 2-3, but sometimes doesn't do
                                for (int boneMealTimes = 0; boneMealTimes < 4; boneMealTimes++)
                                {
                                    // TODO: Do a check if the carrot/potato is on the first growth stage
                                    // if so, use: new Location(location.X, (double)(location.Y - 1) + (double)0.93750, location.Z)
                                    SendPlaceBlock(location, Direction.Down);
                                }

                                Thread.Sleep(100);
                            }
                        }

                        LogToConsole("Finished bonemealing crops");
                        state = State.SearchingForFarmlandToPlant;
                        break;
                }

                LogToConsole("Waiting for " + Config.Delay_Between_Tasks + " seconds for next cycle.");
                Thread.Sleep(Config.Delay_Between_Tasks * 1000);
            }

            LogToConsole("Stopped farming!");
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

        // Yoinked from ReinforceZwei's AutoTree
        // TODO: Rewrite to support the whole inventory and moving to the hotbar
        public bool SwitchToItem(ItemType itemType)
        {
            Container playerInventory = GetPlayerInventory();

            if (playerInventory.Items.ContainsKey(GetCurrentSlot() - 36)
                && playerInventory.Items[GetCurrentSlot() - 36].Type == itemType)
                return true; // Already selected

            // Search for the seed in the hotbar
            List<int> result = new List<int>(playerInventory.SearchItem(itemType))
                .Where(slot => slot >= 36 && slot <= 44)
                .ToList();

            if (result.Count <= 0)
                return false;

            ChangeSlot((short)(result[0] - 36));
            return true;
        }

        // Yoinked from Daenges's Sugarcane Farmer
        private bool WaitForMoveToLocation(Location pos, float tolerance = 2f)
        {
            if (MoveToLocation(pos))
            {
                LogToConsole("Moving to: " + pos);

                while (GetCurrentLocation().Distance(pos) > tolerance)
                    Thread.Sleep(200);

                return true;
            }
            else LogToConsole("Can't move to: " + pos);

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
    }
}
