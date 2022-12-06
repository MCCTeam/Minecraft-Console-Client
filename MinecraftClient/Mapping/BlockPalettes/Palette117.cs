using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette117 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette117()
        {
            materials[0] = Material.Air;
            materials[1] = Material.Stone;
            materials[2] = Material.Granite;
            materials[3] = Material.PolishedGranite;
            materials[4] = Material.Diorite;
            materials[5] = Material.PolishedDiorite;
            materials[6] = Material.Andesite;
            materials[7] = Material.PolishedAndesite;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            materials[10] = Material.Dirt;
            materials[11] = Material.CoarseDirt;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            materials[14] = Material.Cobblestone;
            materials[15] = Material.OakPlanks;
            materials[16] = Material.SprucePlanks;
            materials[17] = Material.BirchPlanks;
            materials[18] = Material.JunglePlanks;
            materials[19] = Material.AcaciaPlanks;
            materials[20] = Material.DarkOakPlanks;
            for (int i = 21; i <= 22; i++)
                materials[i] = Material.OakSapling;
            for (int i = 23; i <= 24; i++)
                materials[i] = Material.SpruceSapling;
            for (int i = 25; i <= 26; i++)
                materials[i] = Material.BirchSapling;
            for (int i = 27; i <= 28; i++)
                materials[i] = Material.JungleSapling;
            for (int i = 29; i <= 30; i++)
                materials[i] = Material.AcaciaSapling;
            for (int i = 31; i <= 32; i++)
                materials[i] = Material.DarkOakSapling;
            materials[33] = Material.Bedrock;
            for (int i = 34; i <= 49; i++)
                materials[i] = Material.Water;
            for (int i = 50; i <= 65; i++)
                materials[i] = Material.Lava;
            materials[66] = Material.Sand;
            materials[67] = Material.RedSand;
            materials[68] = Material.Gravel;
            materials[69] = Material.GoldOre;
            materials[70] = Material.DeepslateGoldOre;
            materials[71] = Material.IronOre;
            materials[72] = Material.DeepslateIronOre;
            materials[73] = Material.CoalOre;
            materials[74] = Material.DeepslateCoalOre;
            materials[75] = Material.NetherGoldOre;
            for (int i = 76; i <= 78; i++)
                materials[i] = Material.OakLog;
            for (int i = 79; i <= 81; i++)
                materials[i] = Material.SpruceLog;
            for (int i = 82; i <= 84; i++)
                materials[i] = Material.BirchLog;
            for (int i = 85; i <= 87; i++)
                materials[i] = Material.JungleLog;
            for (int i = 88; i <= 90; i++)
                materials[i] = Material.AcaciaLog;
            for (int i = 91; i <= 93; i++)
                materials[i] = Material.DarkOakLog;
            for (int i = 94; i <= 96; i++)
                materials[i] = Material.StrippedSpruceLog;
            for (int i = 97; i <= 99; i++)
                materials[i] = Material.StrippedBirchLog;
            for (int i = 100; i <= 102; i++)
                materials[i] = Material.StrippedJungleLog;
            for (int i = 103; i <= 105; i++)
                materials[i] = Material.StrippedAcaciaLog;
            for (int i = 106; i <= 108; i++)
                materials[i] = Material.StrippedDarkOakLog;
            for (int i = 109; i <= 111; i++)
                materials[i] = Material.StrippedOakLog;
            for (int i = 112; i <= 114; i++)
                materials[i] = Material.OakWood;
            for (int i = 115; i <= 117; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 118; i <= 120; i++)
                materials[i] = Material.BirchWood;
            for (int i = 121; i <= 123; i++)
                materials[i] = Material.JungleWood;
            for (int i = 124; i <= 126; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 127; i <= 129; i++)
                materials[i] = Material.DarkOakWood;
            for (int i = 130; i <= 132; i++)
                materials[i] = Material.StrippedOakWood;
            for (int i = 133; i <= 135; i++)
                materials[i] = Material.StrippedSpruceWood;
            for (int i = 136; i <= 138; i++)
                materials[i] = Material.StrippedBirchWood;
            for (int i = 139; i <= 141; i++)
                materials[i] = Material.StrippedJungleWood;
            for (int i = 142; i <= 144; i++)
                materials[i] = Material.StrippedAcaciaWood;
            for (int i = 145; i <= 147; i++)
                materials[i] = Material.StrippedDarkOakWood;
            for (int i = 148; i <= 161; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 162; i <= 175; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 176; i <= 189; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 190; i <= 203; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 204; i <= 217; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 218; i <= 231; i++)
                materials[i] = Material.DarkOakLeaves;
            for (int i = 232; i <= 245; i++)
                materials[i] = Material.AzaleaLeaves;
            for (int i = 246; i <= 259; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            materials[260] = Material.Sponge;
            materials[261] = Material.WetSponge;
            materials[262] = Material.Glass;
            materials[263] = Material.LapisOre;
            materials[264] = Material.DeepslateLapisOre;
            materials[265] = Material.LapisBlock;
            for (int i = 266; i <= 277; i++)
                materials[i] = Material.Dispenser;
            materials[278] = Material.Sandstone;
            materials[279] = Material.ChiseledSandstone;
            materials[280] = Material.CutSandstone;
            for (int i = 281; i <= 1080; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 1081; i <= 1096; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 1097; i <= 1112; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 1113; i <= 1128; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 1129; i <= 1144; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 1145; i <= 1160; i++)
                materials[i] = Material.YellowBed;
            for (int i = 1161; i <= 1176; i++)
                materials[i] = Material.LimeBed;
            for (int i = 1177; i <= 1192; i++)
                materials[i] = Material.PinkBed;
            for (int i = 1193; i <= 1208; i++)
                materials[i] = Material.GrayBed;
            for (int i = 1209; i <= 1224; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 1225; i <= 1240; i++)
                materials[i] = Material.CyanBed;
            for (int i = 1241; i <= 1256; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 1257; i <= 1272; i++)
                materials[i] = Material.BlueBed;
            for (int i = 1273; i <= 1288; i++)
                materials[i] = Material.BrownBed;
            for (int i = 1289; i <= 1304; i++)
                materials[i] = Material.GreenBed;
            for (int i = 1305; i <= 1320; i++)
                materials[i] = Material.RedBed;
            for (int i = 1321; i <= 1336; i++)
                materials[i] = Material.BlackBed;
            for (int i = 1337; i <= 1360; i++)
                materials[i] = Material.PoweredRail;
            for (int i = 1361; i <= 1384; i++)
                materials[i] = Material.DetectorRail;
            for (int i = 1385; i <= 1396; i++)
                materials[i] = Material.StickyPiston;
            materials[1397] = Material.Cobweb;
            materials[1398] = Material.Grass;
            materials[1399] = Material.Fern;
            materials[1400] = Material.DeadBush;
            materials[1401] = Material.Seagrass;
            for (int i = 1402; i <= 1403; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 1404; i <= 1415; i++)
                materials[i] = Material.Piston;
            for (int i = 1416; i <= 1439; i++)
                materials[i] = Material.PistonHead;
            materials[1440] = Material.WhiteWool;
            materials[1441] = Material.OrangeWool;
            materials[1442] = Material.MagentaWool;
            materials[1443] = Material.LightBlueWool;
            materials[1444] = Material.YellowWool;
            materials[1445] = Material.LimeWool;
            materials[1446] = Material.PinkWool;
            materials[1447] = Material.GrayWool;
            materials[1448] = Material.LightGrayWool;
            materials[1449] = Material.CyanWool;
            materials[1450] = Material.PurpleWool;
            materials[1451] = Material.BlueWool;
            materials[1452] = Material.BrownWool;
            materials[1453] = Material.GreenWool;
            materials[1454] = Material.RedWool;
            materials[1455] = Material.BlackWool;
            for (int i = 1456; i <= 1467; i++)
                materials[i] = Material.MovingPiston;
            materials[1468] = Material.Dandelion;
            materials[1469] = Material.Poppy;
            materials[1470] = Material.BlueOrchid;
            materials[1471] = Material.Allium;
            materials[1472] = Material.AzureBluet;
            materials[1473] = Material.RedTulip;
            materials[1474] = Material.OrangeTulip;
            materials[1475] = Material.WhiteTulip;
            materials[1476] = Material.PinkTulip;
            materials[1477] = Material.OxeyeDaisy;
            materials[1478] = Material.Cornflower;
            materials[1479] = Material.WitherRose;
            materials[1480] = Material.LilyOfTheValley;
            materials[1481] = Material.BrownMushroom;
            materials[1482] = Material.RedMushroom;
            materials[1483] = Material.GoldBlock;
            materials[1484] = Material.IronBlock;
            materials[1485] = Material.Bricks;
            for (int i = 1486; i <= 1487; i++)
                materials[i] = Material.Tnt;
            materials[1488] = Material.Bookshelf;
            materials[1489] = Material.MossyCobblestone;
            materials[1490] = Material.Obsidian;
            materials[1491] = Material.Torch;
            for (int i = 1492; i <= 1495; i++)
                materials[i] = Material.WallTorch;
            for (int i = 1496; i <= 2007; i++)
                materials[i] = Material.Fire;
            materials[2008] = Material.SoulFire;
            materials[2009] = Material.Spawner;
            for (int i = 2010; i <= 2089; i++)
                materials[i] = Material.OakStairs;
            for (int i = 2090; i <= 2113; i++)
                materials[i] = Material.Chest;
            for (int i = 2114; i <= 3409; i++)
                materials[i] = Material.RedstoneWire;
            materials[3410] = Material.DiamondOre;
            materials[3411] = Material.DeepslateDiamondOre;
            materials[3412] = Material.DiamondBlock;
            materials[3413] = Material.CraftingTable;
            for (int i = 3414; i <= 3421; i++)
                materials[i] = Material.Wheat;
            for (int i = 3422; i <= 3429; i++)
                materials[i] = Material.Farmland;
            for (int i = 3430; i <= 3437; i++)
                materials[i] = Material.Furnace;
            for (int i = 3438; i <= 3469; i++)
                materials[i] = Material.OakSign;
            for (int i = 3470; i <= 3501; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 3502; i <= 3533; i++)
                materials[i] = Material.BirchSign;
            for (int i = 3534; i <= 3565; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 3566; i <= 3597; i++)
                materials[i] = Material.JungleSign;
            for (int i = 3598; i <= 3629; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 3630; i <= 3693; i++)
                materials[i] = Material.OakDoor;
            for (int i = 3694; i <= 3701; i++)
                materials[i] = Material.Ladder;
            for (int i = 3702; i <= 3721; i++)
                materials[i] = Material.Rail;
            for (int i = 3722; i <= 3801; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 3802; i <= 3809; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 3810; i <= 3817; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 3818; i <= 3825; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 3826; i <= 3833; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 3834; i <= 3841; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 3842; i <= 3849; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 3850; i <= 3873; i++)
                materials[i] = Material.Lever;
            for (int i = 3874; i <= 3875; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 3876; i <= 3939; i++)
                materials[i] = Material.IronDoor;
            for (int i = 3940; i <= 3941; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 3942; i <= 3943; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 3944; i <= 3945; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 3946; i <= 3947; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 3948; i <= 3949; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 3950; i <= 3951; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 3952; i <= 3953; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 3954; i <= 3955; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 3956; i <= 3957; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 3958; i <= 3965; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 3966; i <= 3989; i++)
                materials[i] = Material.StoneButton;
            for (int i = 3990; i <= 3997; i++)
                materials[i] = Material.Snow;
            materials[3998] = Material.Ice;
            materials[3999] = Material.SnowBlock;
            for (int i = 4000; i <= 4015; i++)
                materials[i] = Material.Cactus;
            materials[4016] = Material.Clay;
            for (int i = 4017; i <= 4032; i++)
                materials[i] = Material.SugarCane;
            for (int i = 4033; i <= 4034; i++)
                materials[i] = Material.Jukebox;
            for (int i = 4035; i <= 4066; i++)
                materials[i] = Material.OakFence;
            materials[4067] = Material.Pumpkin;
            materials[4068] = Material.Netherrack;
            materials[4069] = Material.SoulSand;
            materials[4070] = Material.SoulSoil;
            for (int i = 4071; i <= 4073; i++)
                materials[i] = Material.Basalt;
            for (int i = 4074; i <= 4076; i++)
                materials[i] = Material.PolishedBasalt;
            materials[4077] = Material.SoulTorch;
            for (int i = 4078; i <= 4081; i++)
                materials[i] = Material.SoulWallTorch;
            materials[4082] = Material.Glowstone;
            for (int i = 4083; i <= 4084; i++)
                materials[i] = Material.NetherPortal;
            for (int i = 4085; i <= 4088; i++)
                materials[i] = Material.CarvedPumpkin;
            for (int i = 4089; i <= 4092; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 4093; i <= 4099; i++)
                materials[i] = Material.Cake;
            for (int i = 4100; i <= 4163; i++)
                materials[i] = Material.Repeater;
            materials[4164] = Material.WhiteStainedGlass;
            materials[4165] = Material.OrangeStainedGlass;
            materials[4166] = Material.MagentaStainedGlass;
            materials[4167] = Material.LightBlueStainedGlass;
            materials[4168] = Material.YellowStainedGlass;
            materials[4169] = Material.LimeStainedGlass;
            materials[4170] = Material.PinkStainedGlass;
            materials[4171] = Material.GrayStainedGlass;
            materials[4172] = Material.LightGrayStainedGlass;
            materials[4173] = Material.CyanStainedGlass;
            materials[4174] = Material.PurpleStainedGlass;
            materials[4175] = Material.BlueStainedGlass;
            materials[4176] = Material.BrownStainedGlass;
            materials[4177] = Material.GreenStainedGlass;
            materials[4178] = Material.RedStainedGlass;
            materials[4179] = Material.BlackStainedGlass;
            for (int i = 4180; i <= 4243; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 4244; i <= 4307; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 4308; i <= 4371; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 4372; i <= 4435; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 4436; i <= 4499; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 4500; i <= 4563; i++)
                materials[i] = Material.DarkOakTrapdoor;
            materials[4564] = Material.StoneBricks;
            materials[4565] = Material.MossyStoneBricks;
            materials[4566] = Material.CrackedStoneBricks;
            materials[4567] = Material.ChiseledStoneBricks;
            materials[4568] = Material.InfestedStone;
            materials[4569] = Material.InfestedCobblestone;
            materials[4570] = Material.InfestedStoneBricks;
            materials[4571] = Material.InfestedMossyStoneBricks;
            materials[4572] = Material.InfestedCrackedStoneBricks;
            materials[4573] = Material.InfestedChiseledStoneBricks;
            for (int i = 4574; i <= 4637; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 4638; i <= 4701; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 4702; i <= 4765; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 4766; i <= 4797; i++)
                materials[i] = Material.IronBars;
            for (int i = 4798; i <= 4803; i++)
                materials[i] = Material.Chain;
            for (int i = 4804; i <= 4835; i++)
                materials[i] = Material.GlassPane;
            materials[4836] = Material.Melon;
            for (int i = 4837; i <= 4840; i++)
                materials[i] = Material.AttachedPumpkinStem;
            for (int i = 4841; i <= 4844; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 4845; i <= 4852; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 4853; i <= 4860; i++)
                materials[i] = Material.MelonStem;
            for (int i = 4861; i <= 4892; i++)
                materials[i] = Material.Vine;
            for (int i = 4893; i <= 5020; i++)
                materials[i] = Material.GlowLichen;
            for (int i = 5021; i <= 5052; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 5053; i <= 5132; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 5133; i <= 5212; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 5213; i <= 5214; i++)
                materials[i] = Material.Mycelium;
            materials[5215] = Material.LilyPad;
            materials[5216] = Material.NetherBricks;
            for (int i = 5217; i <= 5248; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 5249; i <= 5328; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 5329; i <= 5332; i++)
                materials[i] = Material.NetherWart;
            materials[5333] = Material.EnchantingTable;
            for (int i = 5334; i <= 5341; i++)
                materials[i] = Material.BrewingStand;
            materials[5342] = Material.Cauldron;
            for (int i = 5343; i <= 5345; i++)
                materials[i] = Material.WaterCauldron;
            materials[5346] = Material.LavaCauldron;
            for (int i = 5347; i <= 5349; i++)
                materials[i] = Material.PowderSnowCauldron;
            materials[5350] = Material.EndPortal;
            for (int i = 5351; i <= 5358; i++)
                materials[i] = Material.EndPortalFrame;
            materials[5359] = Material.EndStone;
            materials[5360] = Material.DragonEgg;
            for (int i = 5361; i <= 5362; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 5363; i <= 5374; i++)
                materials[i] = Material.Cocoa;
            for (int i = 5375; i <= 5454; i++)
                materials[i] = Material.SandstoneStairs;
            materials[5455] = Material.EmeraldOre;
            materials[5456] = Material.DeepslateEmeraldOre;
            for (int i = 5457; i <= 5464; i++)
                materials[i] = Material.EnderChest;
            for (int i = 5465; i <= 5480; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 5481; i <= 5608; i++)
                materials[i] = Material.Tripwire;
            materials[5609] = Material.EmeraldBlock;
            for (int i = 5610; i <= 5689; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 5690; i <= 5769; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 5770; i <= 5849; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 5850; i <= 5861; i++)
                materials[i] = Material.CommandBlock;
            materials[5862] = Material.Beacon;
            for (int i = 5863; i <= 6186; i++)
                materials[i] = Material.CobblestoneWall;
            for (int i = 6187; i <= 6510; i++)
                materials[i] = Material.MossyCobblestoneWall;
            materials[6511] = Material.FlowerPot;
            materials[6512] = Material.PottedOakSapling;
            materials[6513] = Material.PottedSpruceSapling;
            materials[6514] = Material.PottedBirchSapling;
            materials[6515] = Material.PottedJungleSapling;
            materials[6516] = Material.PottedAcaciaSapling;
            materials[6517] = Material.PottedDarkOakSapling;
            materials[6518] = Material.PottedFern;
            materials[6519] = Material.PottedDandelion;
            materials[6520] = Material.PottedPoppy;
            materials[6521] = Material.PottedBlueOrchid;
            materials[6522] = Material.PottedAllium;
            materials[6523] = Material.PottedAzureBluet;
            materials[6524] = Material.PottedRedTulip;
            materials[6525] = Material.PottedOrangeTulip;
            materials[6526] = Material.PottedWhiteTulip;
            materials[6527] = Material.PottedPinkTulip;
            materials[6528] = Material.PottedOxeyeDaisy;
            materials[6529] = Material.PottedCornflower;
            materials[6530] = Material.PottedLilyOfTheValley;
            materials[6531] = Material.PottedWitherRose;
            materials[6532] = Material.PottedRedMushroom;
            materials[6533] = Material.PottedBrownMushroom;
            materials[6534] = Material.PottedDeadBush;
            materials[6535] = Material.PottedCactus;
            for (int i = 6536; i <= 6543; i++)
                materials[i] = Material.Carrots;
            for (int i = 6544; i <= 6551; i++)
                materials[i] = Material.Potatoes;
            for (int i = 6552; i <= 6575; i++)
                materials[i] = Material.OakButton;
            for (int i = 6576; i <= 6599; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 6600; i <= 6623; i++)
                materials[i] = Material.BirchButton;
            for (int i = 6624; i <= 6647; i++)
                materials[i] = Material.JungleButton;
            for (int i = 6648; i <= 6671; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 6672; i <= 6695; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 6696; i <= 6711; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 6712; i <= 6715; i++)
                materials[i] = Material.SkeletonWallSkull;
            for (int i = 6716; i <= 6731; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 6732; i <= 6735; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 6736; i <= 6751; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 6752; i <= 6755; i++)
                materials[i] = Material.ZombieWallHead;
            for (int i = 6756; i <= 6771; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 6772; i <= 6775; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 6776; i <= 6791; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 6792; i <= 6795; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 6796; i <= 6811; i++)
                materials[i] = Material.DragonHead;
            for (int i = 6812; i <= 6815; i++)
                materials[i] = Material.DragonWallHead;
            for (int i = 6816; i <= 6819; i++)
                materials[i] = Material.Anvil;
            for (int i = 6820; i <= 6823; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 6824; i <= 6827; i++)
                materials[i] = Material.DamagedAnvil;
            for (int i = 6828; i <= 6851; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 6852; i <= 6867; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 6868; i <= 6883; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            for (int i = 6884; i <= 6899; i++)
                materials[i] = Material.Comparator;
            for (int i = 6900; i <= 6931; i++)
                materials[i] = Material.DaylightDetector;
            materials[6932] = Material.RedstoneBlock;
            materials[6933] = Material.NetherQuartzOre;
            for (int i = 6934; i <= 6943; i++)
                materials[i] = Material.Hopper;
            materials[6944] = Material.QuartzBlock;
            materials[6945] = Material.ChiseledQuartzBlock;
            for (int i = 6946; i <= 6948; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 6949; i <= 7028; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 7029; i <= 7052; i++)
                materials[i] = Material.ActivatorRail;
            for (int i = 7053; i <= 7064; i++)
                materials[i] = Material.Dropper;
            materials[7065] = Material.WhiteTerracotta;
            materials[7066] = Material.OrangeTerracotta;
            materials[7067] = Material.MagentaTerracotta;
            materials[7068] = Material.LightBlueTerracotta;
            materials[7069] = Material.YellowTerracotta;
            materials[7070] = Material.LimeTerracotta;
            materials[7071] = Material.PinkTerracotta;
            materials[7072] = Material.GrayTerracotta;
            materials[7073] = Material.LightGrayTerracotta;
            materials[7074] = Material.CyanTerracotta;
            materials[7075] = Material.PurpleTerracotta;
            materials[7076] = Material.BlueTerracotta;
            materials[7077] = Material.BrownTerracotta;
            materials[7078] = Material.GreenTerracotta;
            materials[7079] = Material.RedTerracotta;
            materials[7080] = Material.BlackTerracotta;
            for (int i = 7081; i <= 7112; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            for (int i = 7113; i <= 7144; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            for (int i = 7145; i <= 7176; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            for (int i = 7177; i <= 7208; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            for (int i = 7209; i <= 7240; i++)
                materials[i] = Material.YellowStainedGlassPane;
            for (int i = 7241; i <= 7272; i++)
                materials[i] = Material.LimeStainedGlassPane;
            for (int i = 7273; i <= 7304; i++)
                materials[i] = Material.PinkStainedGlassPane;
            for (int i = 7305; i <= 7336; i++)
                materials[i] = Material.GrayStainedGlassPane;
            for (int i = 7337; i <= 7368; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            for (int i = 7369; i <= 7400; i++)
                materials[i] = Material.CyanStainedGlassPane;
            for (int i = 7401; i <= 7432; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            for (int i = 7433; i <= 7464; i++)
                materials[i] = Material.BlueStainedGlassPane;
            for (int i = 7465; i <= 7496; i++)
                materials[i] = Material.BrownStainedGlassPane;
            for (int i = 7497; i <= 7528; i++)
                materials[i] = Material.GreenStainedGlassPane;
            for (int i = 7529; i <= 7560; i++)
                materials[i] = Material.RedStainedGlassPane;
            for (int i = 7561; i <= 7592; i++)
                materials[i] = Material.BlackStainedGlassPane;
            for (int i = 7593; i <= 7672; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 7673; i <= 7752; i++)
                materials[i] = Material.DarkOakStairs;
            materials[7753] = Material.SlimeBlock;
            materials[7754] = Material.Barrier;
            for (int i = 7755; i <= 7786; i++)
                materials[i] = Material.Light;
            for (int i = 7787; i <= 7850; i++)
                materials[i] = Material.IronTrapdoor;
            materials[7851] = Material.Prismarine;
            materials[7852] = Material.PrismarineBricks;
            materials[7853] = Material.DarkPrismarine;
            for (int i = 7854; i <= 7933; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 7934; i <= 8013; i++)
                materials[i] = Material.PrismarineBrickStairs;
            for (int i = 8014; i <= 8093; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 8094; i <= 8099; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 8100; i <= 8105; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 8106; i <= 8111; i++)
                materials[i] = Material.DarkPrismarineSlab;
            materials[8112] = Material.SeaLantern;
            for (int i = 8113; i <= 8115; i++)
                materials[i] = Material.HayBlock;
            materials[8116] = Material.WhiteCarpet;
            materials[8117] = Material.OrangeCarpet;
            materials[8118] = Material.MagentaCarpet;
            materials[8119] = Material.LightBlueCarpet;
            materials[8120] = Material.YellowCarpet;
            materials[8121] = Material.LimeCarpet;
            materials[8122] = Material.PinkCarpet;
            materials[8123] = Material.GrayCarpet;
            materials[8124] = Material.LightGrayCarpet;
            materials[8125] = Material.CyanCarpet;
            materials[8126] = Material.PurpleCarpet;
            materials[8127] = Material.BlueCarpet;
            materials[8128] = Material.BrownCarpet;
            materials[8129] = Material.GreenCarpet;
            materials[8130] = Material.RedCarpet;
            materials[8131] = Material.BlackCarpet;
            materials[8132] = Material.Terracotta;
            materials[8133] = Material.CoalBlock;
            materials[8134] = Material.PackedIce;
            for (int i = 8135; i <= 8136; i++)
                materials[i] = Material.Sunflower;
            for (int i = 8137; i <= 8138; i++)
                materials[i] = Material.Lilac;
            for (int i = 8139; i <= 8140; i++)
                materials[i] = Material.RoseBush;
            for (int i = 8141; i <= 8142; i++)
                materials[i] = Material.Peony;
            for (int i = 8143; i <= 8144; i++)
                materials[i] = Material.TallGrass;
            for (int i = 8145; i <= 8146; i++)
                materials[i] = Material.LargeFern;
            for (int i = 8147; i <= 8162; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 8163; i <= 8178; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 8179; i <= 8194; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 8195; i <= 8210; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 8211; i <= 8226; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 8227; i <= 8242; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 8243; i <= 8258; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 8259; i <= 8274; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 8275; i <= 8290; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 8291; i <= 8306; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 8307; i <= 8322; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 8323; i <= 8338; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 8339; i <= 8354; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 8355; i <= 8370; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 8371; i <= 8386; i++)
                materials[i] = Material.RedBanner;
            for (int i = 8387; i <= 8402; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 8403; i <= 8406; i++)
                materials[i] = Material.WhiteWallBanner;
            for (int i = 8407; i <= 8410; i++)
                materials[i] = Material.OrangeWallBanner;
            for (int i = 8411; i <= 8414; i++)
                materials[i] = Material.MagentaWallBanner;
            for (int i = 8415; i <= 8418; i++)
                materials[i] = Material.LightBlueWallBanner;
            for (int i = 8419; i <= 8422; i++)
                materials[i] = Material.YellowWallBanner;
            for (int i = 8423; i <= 8426; i++)
                materials[i] = Material.LimeWallBanner;
            for (int i = 8427; i <= 8430; i++)
                materials[i] = Material.PinkWallBanner;
            for (int i = 8431; i <= 8434; i++)
                materials[i] = Material.GrayWallBanner;
            for (int i = 8435; i <= 8438; i++)
                materials[i] = Material.LightGrayWallBanner;
            for (int i = 8439; i <= 8442; i++)
                materials[i] = Material.CyanWallBanner;
            for (int i = 8443; i <= 8446; i++)
                materials[i] = Material.PurpleWallBanner;
            for (int i = 8447; i <= 8450; i++)
                materials[i] = Material.BlueWallBanner;
            for (int i = 8451; i <= 8454; i++)
                materials[i] = Material.BrownWallBanner;
            for (int i = 8455; i <= 8458; i++)
                materials[i] = Material.GreenWallBanner;
            for (int i = 8459; i <= 8462; i++)
                materials[i] = Material.RedWallBanner;
            for (int i = 8463; i <= 8466; i++)
                materials[i] = Material.BlackWallBanner;
            materials[8467] = Material.RedSandstone;
            materials[8468] = Material.ChiseledRedSandstone;
            materials[8469] = Material.CutRedSandstone;
            for (int i = 8470; i <= 8549; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 8550; i <= 8555; i++)
                materials[i] = Material.OakSlab;
            for (int i = 8556; i <= 8561; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 8562; i <= 8567; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 8568; i <= 8573; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 8574; i <= 8579; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 8580; i <= 8585; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 8586; i <= 8591; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 8592; i <= 8597; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 8598; i <= 8603; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 8604; i <= 8609; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 8610; i <= 8615; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 8616; i <= 8621; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 8622; i <= 8627; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 8628; i <= 8633; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 8634; i <= 8639; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 8640; i <= 8645; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 8646; i <= 8651; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 8652; i <= 8657; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            for (int i = 8658; i <= 8663; i++)
                materials[i] = Material.PurpurSlab;
            materials[8664] = Material.SmoothStone;
            materials[8665] = Material.SmoothSandstone;
            materials[8666] = Material.SmoothQuartz;
            materials[8667] = Material.SmoothRedSandstone;
            for (int i = 8668; i <= 8699; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 8700; i <= 8731; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 8732; i <= 8763; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 8764; i <= 8795; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 8796; i <= 8827; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 8828; i <= 8859; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 8860; i <= 8891; i++)
                materials[i] = Material.BirchFence;
            for (int i = 8892; i <= 8923; i++)
                materials[i] = Material.JungleFence;
            for (int i = 8924; i <= 8955; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 8956; i <= 8987; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 8988; i <= 9051; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 9052; i <= 9115; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 9116; i <= 9179; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 9180; i <= 9243; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 9244; i <= 9307; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 9308; i <= 9313; i++)
                materials[i] = Material.EndRod;
            for (int i = 9314; i <= 9377; i++)
                materials[i] = Material.ChorusPlant;
            for (int i = 9378; i <= 9383; i++)
                materials[i] = Material.ChorusFlower;
            materials[9384] = Material.PurpurBlock;
            for (int i = 9385; i <= 9387; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 9388; i <= 9467; i++)
                materials[i] = Material.PurpurStairs;
            materials[9468] = Material.EndStoneBricks;
            for (int i = 9469; i <= 9472; i++)
                materials[i] = Material.Beetroots;
            materials[9473] = Material.DirtPath;
            materials[9474] = Material.EndGateway;
            for (int i = 9475; i <= 9486; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 9487; i <= 9498; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 9499; i <= 9502; i++)
                materials[i] = Material.FrostedIce;
            materials[9503] = Material.MagmaBlock;
            materials[9504] = Material.NetherWartBlock;
            materials[9505] = Material.RedNetherBricks;
            for (int i = 9506; i <= 9508; i++)
                materials[i] = Material.BoneBlock;
            materials[9509] = Material.StructureVoid;
            for (int i = 9510; i <= 9521; i++)
                materials[i] = Material.Observer;
            for (int i = 9522; i <= 9527; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 9528; i <= 9533; i++)
                materials[i] = Material.WhiteShulkerBox;
            for (int i = 9534; i <= 9539; i++)
                materials[i] = Material.OrangeShulkerBox;
            for (int i = 9540; i <= 9545; i++)
                materials[i] = Material.MagentaShulkerBox;
            for (int i = 9546; i <= 9551; i++)
                materials[i] = Material.LightBlueShulkerBox;
            for (int i = 9552; i <= 9557; i++)
                materials[i] = Material.YellowShulkerBox;
            for (int i = 9558; i <= 9563; i++)
                materials[i] = Material.LimeShulkerBox;
            for (int i = 9564; i <= 9569; i++)
                materials[i] = Material.PinkShulkerBox;
            for (int i = 9570; i <= 9575; i++)
                materials[i] = Material.GrayShulkerBox;
            for (int i = 9576; i <= 9581; i++)
                materials[i] = Material.LightGrayShulkerBox;
            for (int i = 9582; i <= 9587; i++)
                materials[i] = Material.CyanShulkerBox;
            for (int i = 9588; i <= 9593; i++)
                materials[i] = Material.PurpleShulkerBox;
            for (int i = 9594; i <= 9599; i++)
                materials[i] = Material.BlueShulkerBox;
            for (int i = 9600; i <= 9605; i++)
                materials[i] = Material.BrownShulkerBox;
            for (int i = 9606; i <= 9611; i++)
                materials[i] = Material.GreenShulkerBox;
            for (int i = 9612; i <= 9617; i++)
                materials[i] = Material.RedShulkerBox;
            for (int i = 9618; i <= 9623; i++)
                materials[i] = Material.BlackShulkerBox;
            for (int i = 9624; i <= 9627; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 9628; i <= 9631; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 9632; i <= 9635; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 9636; i <= 9639; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 9640; i <= 9643; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 9644; i <= 9647; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 9648; i <= 9651; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 9652; i <= 9655; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 9656; i <= 9659; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 9660; i <= 9663; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 9664; i <= 9667; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 9668; i <= 9671; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            for (int i = 9672; i <= 9675; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            for (int i = 9676; i <= 9679; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 9680; i <= 9683; i++)
                materials[i] = Material.RedGlazedTerracotta;
            for (int i = 9684; i <= 9687; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            materials[9688] = Material.WhiteConcrete;
            materials[9689] = Material.OrangeConcrete;
            materials[9690] = Material.MagentaConcrete;
            materials[9691] = Material.LightBlueConcrete;
            materials[9692] = Material.YellowConcrete;
            materials[9693] = Material.LimeConcrete;
            materials[9694] = Material.PinkConcrete;
            materials[9695] = Material.GrayConcrete;
            materials[9696] = Material.LightGrayConcrete;
            materials[9697] = Material.CyanConcrete;
            materials[9698] = Material.PurpleConcrete;
            materials[9699] = Material.BlueConcrete;
            materials[9700] = Material.BrownConcrete;
            materials[9701] = Material.GreenConcrete;
            materials[9702] = Material.RedConcrete;
            materials[9703] = Material.BlackConcrete;
            materials[9704] = Material.WhiteConcretePowder;
            materials[9705] = Material.OrangeConcretePowder;
            materials[9706] = Material.MagentaConcretePowder;
            materials[9707] = Material.LightBlueConcretePowder;
            materials[9708] = Material.YellowConcretePowder;
            materials[9709] = Material.LimeConcretePowder;
            materials[9710] = Material.PinkConcretePowder;
            materials[9711] = Material.GrayConcretePowder;
            materials[9712] = Material.LightGrayConcretePowder;
            materials[9713] = Material.CyanConcretePowder;
            materials[9714] = Material.PurpleConcretePowder;
            materials[9715] = Material.BlueConcretePowder;
            materials[9716] = Material.BrownConcretePowder;
            materials[9717] = Material.GreenConcretePowder;
            materials[9718] = Material.RedConcretePowder;
            materials[9719] = Material.BlackConcretePowder;
            for (int i = 9720; i <= 9745; i++)
                materials[i] = Material.Kelp;
            materials[9746] = Material.KelpPlant;
            materials[9747] = Material.DriedKelpBlock;
            for (int i = 9748; i <= 9759; i++)
                materials[i] = Material.TurtleEgg;
            materials[9760] = Material.DeadTubeCoralBlock;
            materials[9761] = Material.DeadBrainCoralBlock;
            materials[9762] = Material.DeadBubbleCoralBlock;
            materials[9763] = Material.DeadFireCoralBlock;
            materials[9764] = Material.DeadHornCoralBlock;
            materials[9765] = Material.TubeCoralBlock;
            materials[9766] = Material.BrainCoralBlock;
            materials[9767] = Material.BubbleCoralBlock;
            materials[9768] = Material.FireCoralBlock;
            materials[9769] = Material.HornCoralBlock;
            for (int i = 9770; i <= 9771; i++)
                materials[i] = Material.DeadTubeCoral;
            for (int i = 9772; i <= 9773; i++)
                materials[i] = Material.DeadBrainCoral;
            for (int i = 9774; i <= 9775; i++)
                materials[i] = Material.DeadBubbleCoral;
            for (int i = 9776; i <= 9777; i++)
                materials[i] = Material.DeadFireCoral;
            for (int i = 9778; i <= 9779; i++)
                materials[i] = Material.DeadHornCoral;
            for (int i = 9780; i <= 9781; i++)
                materials[i] = Material.TubeCoral;
            for (int i = 9782; i <= 9783; i++)
                materials[i] = Material.BrainCoral;
            for (int i = 9784; i <= 9785; i++)
                materials[i] = Material.BubbleCoral;
            for (int i = 9786; i <= 9787; i++)
                materials[i] = Material.FireCoral;
            for (int i = 9788; i <= 9789; i++)
                materials[i] = Material.HornCoral;
            for (int i = 9790; i <= 9791; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 9792; i <= 9793; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 9794; i <= 9795; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 9796; i <= 9797; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 9798; i <= 9799; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 9800; i <= 9801; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 9802; i <= 9803; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 9804; i <= 9805; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 9806; i <= 9807; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 9808; i <= 9809; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 9810; i <= 9817; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 9818; i <= 9825; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 9826; i <= 9833; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            for (int i = 9834; i <= 9841; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 9842; i <= 9849; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 9850; i <= 9857; i++)
                materials[i] = Material.TubeCoralWallFan;
            for (int i = 9858; i <= 9865; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 9866; i <= 9873; i++)
                materials[i] = Material.BubbleCoralWallFan;
            for (int i = 9874; i <= 9881; i++)
                materials[i] = Material.FireCoralWallFan;
            for (int i = 9882; i <= 9889; i++)
                materials[i] = Material.HornCoralWallFan;
            for (int i = 9890; i <= 9897; i++)
                materials[i] = Material.SeaPickle;
            materials[9898] = Material.BlueIce;
            for (int i = 9899; i <= 9900; i++)
                materials[i] = Material.Conduit;
            materials[9901] = Material.BambooSapling;
            for (int i = 9902; i <= 9913; i++)
                materials[i] = Material.Bamboo;
            materials[9914] = Material.PottedBamboo;
            materials[9915] = Material.VoidAir;
            materials[9916] = Material.CaveAir;
            for (int i = 9917; i <= 9918; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 9919; i <= 9998; i++)
                materials[i] = Material.PolishedGraniteStairs;
            for (int i = 9999; i <= 10078; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            for (int i = 10079; i <= 10158; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 10159; i <= 10238; i++)
                materials[i] = Material.PolishedDioriteStairs;
            for (int i = 10239; i <= 10318; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 10319; i <= 10398; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 10399; i <= 10478; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 10479; i <= 10558; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            for (int i = 10559; i <= 10638; i++)
                materials[i] = Material.SmoothQuartzStairs;
            for (int i = 10639; i <= 10718; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 10719; i <= 10798; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 10799; i <= 10878; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 10879; i <= 10958; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 10959; i <= 11038; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 11039; i <= 11044; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 11045; i <= 11050; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 11051; i <= 11056; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 11057; i <= 11062; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 11063; i <= 11068; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 11069; i <= 11074; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 11075; i <= 11080; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 11081; i <= 11086; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 11087; i <= 11092; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 11093; i <= 11098; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 11099; i <= 11104; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 11105; i <= 11110; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 11111; i <= 11116; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 11117; i <= 11440; i++)
                materials[i] = Material.BrickWall;
            for (int i = 11441; i <= 11764; i++)
                materials[i] = Material.PrismarineWall;
            for (int i = 11765; i <= 12088; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 12089; i <= 12412; i++)
                materials[i] = Material.MossyStoneBrickWall;
            for (int i = 12413; i <= 12736; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 12737; i <= 13060; i++)
                materials[i] = Material.StoneBrickWall;
            for (int i = 13061; i <= 13384; i++)
                materials[i] = Material.NetherBrickWall;
            for (int i = 13385; i <= 13708; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 13709; i <= 14032; i++)
                materials[i] = Material.RedNetherBrickWall;
            for (int i = 14033; i <= 14356; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 14357; i <= 14680; i++)
                materials[i] = Material.EndStoneBrickWall;
            for (int i = 14681; i <= 15004; i++)
                materials[i] = Material.DioriteWall;
            for (int i = 15005; i <= 15036; i++)
                materials[i] = Material.Scaffolding;
            for (int i = 15037; i <= 15040; i++)
                materials[i] = Material.Loom;
            for (int i = 15041; i <= 15052; i++)
                materials[i] = Material.Barrel;
            for (int i = 15053; i <= 15060; i++)
                materials[i] = Material.Smoker;
            for (int i = 15061; i <= 15068; i++)
                materials[i] = Material.BlastFurnace;
            materials[15069] = Material.CartographyTable;
            materials[15070] = Material.FletchingTable;
            for (int i = 15071; i <= 15082; i++)
                materials[i] = Material.Grindstone;
            for (int i = 15083; i <= 15098; i++)
                materials[i] = Material.Lectern;
            materials[15099] = Material.SmithingTable;
            for (int i = 15100; i <= 15103; i++)
                materials[i] = Material.Stonecutter;
            for (int i = 15104; i <= 15135; i++)
                materials[i] = Material.Bell;
            for (int i = 15136; i <= 15139; i++)
                materials[i] = Material.Lantern;
            for (int i = 15140; i <= 15143; i++)
                materials[i] = Material.SoulLantern;
            for (int i = 15144; i <= 15175; i++)
                materials[i] = Material.Campfire;
            for (int i = 15176; i <= 15207; i++)
                materials[i] = Material.SoulCampfire;
            for (int i = 15208; i <= 15211; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 15212; i <= 15214; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 15215; i <= 15217; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 15218; i <= 15220; i++)
                materials[i] = Material.WarpedHyphae;
            for (int i = 15221; i <= 15223; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            materials[15224] = Material.WarpedNylium;
            materials[15225] = Material.WarpedFungus;
            materials[15226] = Material.WarpedWartBlock;
            materials[15227] = Material.WarpedRoots;
            materials[15228] = Material.NetherSprouts;
            for (int i = 15229; i <= 15231; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 15232; i <= 15234; i++)
                materials[i] = Material.StrippedCrimsonStem;
            for (int i = 15235; i <= 15237; i++)
                materials[i] = Material.CrimsonHyphae;
            for (int i = 15238; i <= 15240; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            materials[15241] = Material.CrimsonNylium;
            materials[15242] = Material.CrimsonFungus;
            materials[15243] = Material.Shroomlight;
            for (int i = 15244; i <= 15269; i++)
                materials[i] = Material.WeepingVines;
            materials[15270] = Material.WeepingVinesPlant;
            for (int i = 15271; i <= 15296; i++)
                materials[i] = Material.TwistingVines;
            materials[15297] = Material.TwistingVinesPlant;
            materials[15298] = Material.CrimsonRoots;
            materials[15299] = Material.CrimsonPlanks;
            materials[15300] = Material.WarpedPlanks;
            for (int i = 15301; i <= 15306; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 15307; i <= 15312; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 15313; i <= 15314; i++)
                materials[i] = Material.CrimsonPressurePlate;
            for (int i = 15315; i <= 15316; i++)
                materials[i] = Material.WarpedPressurePlate;
            for (int i = 15317; i <= 15348; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 15349; i <= 15380; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 15381; i <= 15444; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 15445; i <= 15508; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 15509; i <= 15540; i++)
                materials[i] = Material.CrimsonFenceGate;
            for (int i = 15541; i <= 15572; i++)
                materials[i] = Material.WarpedFenceGate;
            for (int i = 15573; i <= 15652; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 15653; i <= 15732; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 15733; i <= 15756; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 15757; i <= 15780; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 15781; i <= 15844; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 15845; i <= 15908; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 15909; i <= 15940; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 15941; i <= 15972; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 15973; i <= 15980; i++)
                materials[i] = Material.CrimsonWallSign;
            for (int i = 15981; i <= 15988; i++)
                materials[i] = Material.WarpedWallSign;
            for (int i = 15989; i <= 15992; i++)
                materials[i] = Material.StructureBlock;
            for (int i = 15993; i <= 16004; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 16005; i <= 16013; i++)
                materials[i] = Material.Composter;
            for (int i = 16014; i <= 16029; i++)
                materials[i] = Material.Target;
            for (int i = 16030; i <= 16053; i++)
                materials[i] = Material.BeeNest;
            for (int i = 16054; i <= 16077; i++)
                materials[i] = Material.Beehive;
            materials[16078] = Material.HoneyBlock;
            materials[16079] = Material.HoneycombBlock;
            materials[16080] = Material.NetheriteBlock;
            materials[16081] = Material.AncientDebris;
            materials[16082] = Material.CryingObsidian;
            for (int i = 16083; i <= 16087; i++)
                materials[i] = Material.RespawnAnchor;
            materials[16088] = Material.PottedCrimsonFungus;
            materials[16089] = Material.PottedWarpedFungus;
            materials[16090] = Material.PottedCrimsonRoots;
            materials[16091] = Material.PottedWarpedRoots;
            materials[16092] = Material.Lodestone;
            materials[16093] = Material.Blackstone;
            for (int i = 16094; i <= 16173; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 16174; i <= 16497; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 16498; i <= 16503; i++)
                materials[i] = Material.BlackstoneSlab;
            materials[16504] = Material.PolishedBlackstone;
            materials[16505] = Material.PolishedBlackstoneBricks;
            materials[16506] = Material.CrackedPolishedBlackstoneBricks;
            materials[16507] = Material.ChiseledPolishedBlackstone;
            for (int i = 16508; i <= 16513; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 16514; i <= 16593; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 16594; i <= 16917; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            materials[16918] = Material.GildedBlackstone;
            for (int i = 16919; i <= 16998; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 16999; i <= 17004; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 17005; i <= 17006; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 17007; i <= 17030; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 17031; i <= 17354; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            materials[17355] = Material.ChiseledNetherBricks;
            materials[17356] = Material.CrackedNetherBricks;
            materials[17357] = Material.QuartzBricks;
            for (int i = 17358; i <= 17373; i++)
                materials[i] = Material.Candle;
            for (int i = 17374; i <= 17389; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 17390; i <= 17405; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 17406; i <= 17421; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 17422; i <= 17437; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 17438; i <= 17453; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 17454; i <= 17469; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 17470; i <= 17485; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 17486; i <= 17501; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 17502; i <= 17517; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 17518; i <= 17533; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 17534; i <= 17549; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 17550; i <= 17565; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 17566; i <= 17581; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 17582; i <= 17597; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 17598; i <= 17613; i++)
                materials[i] = Material.RedCandle;
            for (int i = 17614; i <= 17629; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 17630; i <= 17631; i++)
                materials[i] = Material.CandleCake;
            for (int i = 17632; i <= 17633; i++)
                materials[i] = Material.WhiteCandleCake;
            for (int i = 17634; i <= 17635; i++)
                materials[i] = Material.OrangeCandleCake;
            for (int i = 17636; i <= 17637; i++)
                materials[i] = Material.MagentaCandleCake;
            for (int i = 17638; i <= 17639; i++)
                materials[i] = Material.LightBlueCandleCake;
            for (int i = 17640; i <= 17641; i++)
                materials[i] = Material.YellowCandleCake;
            for (int i = 17642; i <= 17643; i++)
                materials[i] = Material.LimeCandleCake;
            for (int i = 17644; i <= 17645; i++)
                materials[i] = Material.PinkCandleCake;
            for (int i = 17646; i <= 17647; i++)
                materials[i] = Material.GrayCandleCake;
            for (int i = 17648; i <= 17649; i++)
                materials[i] = Material.LightGrayCandleCake;
            for (int i = 17650; i <= 17651; i++)
                materials[i] = Material.CyanCandleCake;
            for (int i = 17652; i <= 17653; i++)
                materials[i] = Material.PurpleCandleCake;
            for (int i = 17654; i <= 17655; i++)
                materials[i] = Material.BlueCandleCake;
            for (int i = 17656; i <= 17657; i++)
                materials[i] = Material.BrownCandleCake;
            for (int i = 17658; i <= 17659; i++)
                materials[i] = Material.GreenCandleCake;
            for (int i = 17660; i <= 17661; i++)
                materials[i] = Material.RedCandleCake;
            for (int i = 17662; i <= 17663; i++)
                materials[i] = Material.BlackCandleCake;
            materials[17664] = Material.AmethystBlock;
            materials[17665] = Material.BuddingAmethyst;
            for (int i = 17666; i <= 17677; i++)
                materials[i] = Material.AmethystCluster;
            for (int i = 17678; i <= 17689; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 17690; i <= 17701; i++)
                materials[i] = Material.MediumAmethystBud;
            for (int i = 17702; i <= 17713; i++)
                materials[i] = Material.SmallAmethystBud;
            materials[17714] = Material.Tuff;
            materials[17715] = Material.Calcite;
            materials[17716] = Material.TintedGlass;
            materials[17717] = Material.PowderSnow;
            for (int i = 17718; i <= 17813; i++)
                materials[i] = Material.SculkSensor;
            materials[17814] = Material.OxidizedCopper;
            materials[17815] = Material.WeatheredCopper;
            materials[17816] = Material.ExposedCopper;
            materials[17817] = Material.CopperBlock;
            materials[17818] = Material.CopperOre;
            materials[17819] = Material.DeepslateCopperOre;
            materials[17820] = Material.OxidizedCutCopper;
            materials[17821] = Material.WeatheredCutCopper;
            materials[17822] = Material.ExposedCutCopper;
            materials[17823] = Material.CutCopper;
            for (int i = 17824; i <= 17903; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            for (int i = 17904; i <= 17983; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 17984; i <= 18063; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 18064; i <= 18143; i++)
                materials[i] = Material.CutCopperStairs;
            for (int i = 18144; i <= 18149; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 18150; i <= 18155; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 18156; i <= 18161; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 18162; i <= 18167; i++)
                materials[i] = Material.CutCopperSlab;
            materials[18168] = Material.WaxedCopperBlock;
            materials[18169] = Material.WaxedWeatheredCopper;
            materials[18170] = Material.WaxedExposedCopper;
            materials[18171] = Material.WaxedOxidizedCopper;
            materials[18172] = Material.WaxedOxidizedCutCopper;
            materials[18173] = Material.WaxedWeatheredCutCopper;
            materials[18174] = Material.WaxedExposedCutCopper;
            materials[18175] = Material.WaxedCutCopper;
            for (int i = 18176; i <= 18255; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            for (int i = 18256; i <= 18335; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            for (int i = 18336; i <= 18415; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            for (int i = 18416; i <= 18495; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            for (int i = 18496; i <= 18501; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 18502; i <= 18507; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 18508; i <= 18513; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 18514; i <= 18519; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 18520; i <= 18543; i++)
                materials[i] = Material.LightningRod;
            for (int i = 18544; i <= 18563; i++)
                materials[i] = Material.PointedDripstone;
            materials[18564] = Material.DripstoneBlock;
            for (int i = 18565; i <= 18616; i++)
                materials[i] = Material.CaveVines;
            for (int i = 18617; i <= 18618; i++)
                materials[i] = Material.CaveVinesPlant;
            materials[18619] = Material.SporeBlossom;
            materials[18620] = Material.Azalea;
            materials[18621] = Material.FloweringAzalea;
            materials[18622] = Material.MossCarpet;
            materials[18623] = Material.MossBlock;
            for (int i = 18624; i <= 18655; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 18656; i <= 18663; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 18664; i <= 18679; i++)
                materials[i] = Material.SmallDripleaf;
            for (int i = 18680; i <= 18681; i++)
                materials[i] = Material.HangingRoots;
            materials[18682] = Material.RootedDirt;
            for (int i = 18683; i <= 18685; i++)
                materials[i] = Material.Deepslate;
            materials[18686] = Material.CobbledDeepslate;
            for (int i = 18687; i <= 18766; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 18767; i <= 18772; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 18773; i <= 19096; i++)
                materials[i] = Material.CobbledDeepslateWall;
            materials[19097] = Material.PolishedDeepslate;
            for (int i = 19098; i <= 19177; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 19178; i <= 19183; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 19184; i <= 19507; i++)
                materials[i] = Material.PolishedDeepslateWall;
            materials[19508] = Material.DeepslateTiles;
            for (int i = 19509; i <= 19588; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 19589; i <= 19594; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 19595; i <= 19918; i++)
                materials[i] = Material.DeepslateTileWall;
            materials[19919] = Material.DeepslateBricks;
            for (int i = 19920; i <= 19999; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 20000; i <= 20005; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 20006; i <= 20329; i++)
                materials[i] = Material.DeepslateBrickWall;
            materials[20330] = Material.ChiseledDeepslate;
            materials[20331] = Material.CrackedDeepslateBricks;
            materials[20332] = Material.CrackedDeepslateTiles;
            for (int i = 20333; i <= 20335; i++)
                materials[i] = Material.InfestedDeepslate;
            materials[20336] = Material.SmoothBasalt;
            materials[20337] = Material.RawIronBlock;
            materials[20338] = Material.RawCopperBlock;
            materials[20339] = Material.RawGoldBlock;
            materials[20340] = Material.PottedAzaleaBush;
            materials[20341] = Material.PottedFloweringAzaleaBush;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
