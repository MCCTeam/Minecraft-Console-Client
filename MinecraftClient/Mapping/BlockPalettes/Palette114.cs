using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    /// <summary>
    /// Defines mappings for Minecraft 1.14.
    /// Automatically generated using PaletteGenerator.cs
    /// </summary>
    public class Palette114 : PaletteMapping
    {
        private static Dictionary<int, Material> materials = new Dictionary<int, Material>();

        static Palette114()
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
            materials[70] = Material.IronOre;
            materials[71] = Material.CoalOre;
            for (int i = 72; i <= 74; i++)
                materials[i] = Material.OakLog;
            for (int i = 75; i <= 77; i++)
                materials[i] = Material.SpruceLog;
            for (int i = 78; i <= 80; i++)
                materials[i] = Material.BirchLog;
            for (int i = 81; i <= 83; i++)
                materials[i] = Material.JungleLog;
            for (int i = 84; i <= 86; i++)
                materials[i] = Material.AcaciaLog;
            for (int i = 87; i <= 89; i++)
                materials[i] = Material.DarkOakLog;
            for (int i = 90; i <= 92; i++)
                materials[i] = Material.StrippedSpruceLog;
            for (int i = 93; i <= 95; i++)
                materials[i] = Material.StrippedBirchLog;
            for (int i = 96; i <= 98; i++)
                materials[i] = Material.StrippedJungleLog;
            for (int i = 99; i <= 101; i++)
                materials[i] = Material.StrippedAcaciaLog;
            for (int i = 102; i <= 104; i++)
                materials[i] = Material.StrippedDarkOakLog;
            for (int i = 105; i <= 107; i++)
                materials[i] = Material.StrippedOakLog;
            for (int i = 108; i <= 110; i++)
                materials[i] = Material.OakWood;
            for (int i = 111; i <= 113; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 114; i <= 116; i++)
                materials[i] = Material.BirchWood;
            for (int i = 117; i <= 119; i++)
                materials[i] = Material.JungleWood;
            for (int i = 120; i <= 122; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 123; i <= 125; i++)
                materials[i] = Material.DarkOakWood;
            for (int i = 126; i <= 128; i++)
                materials[i] = Material.StrippedOakWood;
            for (int i = 129; i <= 131; i++)
                materials[i] = Material.StrippedSpruceWood;
            for (int i = 132; i <= 134; i++)
                materials[i] = Material.StrippedBirchWood;
            for (int i = 135; i <= 137; i++)
                materials[i] = Material.StrippedJungleWood;
            for (int i = 138; i <= 140; i++)
                materials[i] = Material.StrippedAcaciaWood;
            for (int i = 141; i <= 143; i++)
                materials[i] = Material.StrippedDarkOakWood;
            for (int i = 144; i <= 157; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 158; i <= 171; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 172; i <= 185; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 186; i <= 199; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 200; i <= 213; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 214; i <= 227; i++)
                materials[i] = Material.DarkOakLeaves;
            materials[228] = Material.Sponge;
            materials[229] = Material.WetSponge;
            materials[230] = Material.Glass;
            materials[231] = Material.LapisOre;
            materials[232] = Material.LapisBlock;
            for (int i = 233; i <= 244; i++)
                materials[i] = Material.Dispenser;
            materials[245] = Material.Sandstone;
            materials[246] = Material.ChiseledSandstone;
            materials[247] = Material.CutSandstone;
            for (int i = 248; i <= 1047; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 1048; i <= 1063; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 1064; i <= 1079; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 1080; i <= 1095; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 1096; i <= 1111; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 1112; i <= 1127; i++)
                materials[i] = Material.YellowBed;
            for (int i = 1128; i <= 1143; i++)
                materials[i] = Material.LimeBed;
            for (int i = 1144; i <= 1159; i++)
                materials[i] = Material.PinkBed;
            for (int i = 1160; i <= 1175; i++)
                materials[i] = Material.GrayBed;
            for (int i = 1176; i <= 1191; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 1192; i <= 1207; i++)
                materials[i] = Material.CyanBed;
            for (int i = 1208; i <= 1223; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 1224; i <= 1239; i++)
                materials[i] = Material.BlueBed;
            for (int i = 1240; i <= 1255; i++)
                materials[i] = Material.BrownBed;
            for (int i = 1256; i <= 1271; i++)
                materials[i] = Material.GreenBed;
            for (int i = 1272; i <= 1287; i++)
                materials[i] = Material.RedBed;
            for (int i = 1288; i <= 1303; i++)
                materials[i] = Material.BlackBed;
            for (int i = 1304; i <= 1315; i++)
                materials[i] = Material.PoweredRail;
            for (int i = 1316; i <= 1327; i++)
                materials[i] = Material.DetectorRail;
            for (int i = 1328; i <= 1339; i++)
                materials[i] = Material.StickyPiston;
            materials[1340] = Material.Cobweb;
            materials[1341] = Material.Grass;
            materials[1342] = Material.Fern;
            materials[1343] = Material.DeadBush;
            materials[1344] = Material.Seagrass;
            for (int i = 1345; i <= 1346; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 1347; i <= 1358; i++)
                materials[i] = Material.Piston;
            for (int i = 1359; i <= 1382; i++)
                materials[i] = Material.PistonHead;
            materials[1383] = Material.WhiteWool;
            materials[1384] = Material.OrangeWool;
            materials[1385] = Material.MagentaWool;
            materials[1386] = Material.LightBlueWool;
            materials[1387] = Material.YellowWool;
            materials[1388] = Material.LimeWool;
            materials[1389] = Material.PinkWool;
            materials[1390] = Material.GrayWool;
            materials[1391] = Material.LightGrayWool;
            materials[1392] = Material.CyanWool;
            materials[1393] = Material.PurpleWool;
            materials[1394] = Material.BlueWool;
            materials[1395] = Material.BrownWool;
            materials[1396] = Material.GreenWool;
            materials[1397] = Material.RedWool;
            materials[1398] = Material.BlackWool;
            for (int i = 1399; i <= 1410; i++)
                materials[i] = Material.MovingPiston;
            materials[1411] = Material.Dandelion;
            materials[1412] = Material.Poppy;
            materials[1413] = Material.BlueOrchid;
            materials[1414] = Material.Allium;
            materials[1415] = Material.AzureBluet;
            materials[1416] = Material.RedTulip;
            materials[1417] = Material.OrangeTulip;
            materials[1418] = Material.WhiteTulip;
            materials[1419] = Material.PinkTulip;
            materials[1420] = Material.OxeyeDaisy;
            materials[1421] = Material.Cornflower;
            materials[1422] = Material.WitherRose;
            materials[1423] = Material.LilyOfTheValley;
            materials[1424] = Material.BrownMushroom;
            materials[1425] = Material.RedMushroom;
            materials[1426] = Material.GoldBlock;
            materials[1427] = Material.IronBlock;
            materials[1428] = Material.Bricks;
            for (int i = 1429; i <= 1430; i++)
                materials[i] = Material.Tnt;
            materials[1431] = Material.Bookshelf;
            materials[1432] = Material.MossyCobblestone;
            materials[1433] = Material.Obsidian;
            materials[1434] = Material.Torch;
            for (int i = 1435; i <= 1438; i++)
                materials[i] = Material.WallTorch;
            for (int i = 1439; i <= 1950; i++)
                materials[i] = Material.Fire;
            materials[1951] = Material.Spawner;
            for (int i = 1952; i <= 2031; i++)
                materials[i] = Material.OakStairs;
            for (int i = 2032; i <= 2055; i++)
                materials[i] = Material.Chest;
            for (int i = 2056; i <= 3351; i++)
                materials[i] = Material.RedstoneWire;
            materials[3352] = Material.DiamondOre;
            materials[3353] = Material.DiamondBlock;
            materials[3354] = Material.CraftingTable;
            for (int i = 3355; i <= 3362; i++)
                materials[i] = Material.Wheat;
            for (int i = 3363; i <= 3370; i++)
                materials[i] = Material.Farmland;
            for (int i = 3371; i <= 3378; i++)
                materials[i] = Material.Furnace;
            for (int i = 3379; i <= 3410; i++)
                materials[i] = Material.OakSign;
            for (int i = 3411; i <= 3442; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 3443; i <= 3474; i++)
                materials[i] = Material.BirchSign;
            for (int i = 3475; i <= 3506; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 3507; i <= 3538; i++)
                materials[i] = Material.JungleSign;
            for (int i = 3539; i <= 3570; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 3571; i <= 3634; i++)
                materials[i] = Material.OakDoor;
            for (int i = 3635; i <= 3642; i++)
                materials[i] = Material.Ladder;
            for (int i = 3643; i <= 3652; i++)
                materials[i] = Material.Rail;
            for (int i = 3653; i <= 3732; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 3733; i <= 3740; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 3741; i <= 3748; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 3749; i <= 3756; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 3757; i <= 3764; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 3765; i <= 3772; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 3773; i <= 3780; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 3781; i <= 3804; i++)
                materials[i] = Material.Lever;
            for (int i = 3805; i <= 3806; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 3807; i <= 3870; i++)
                materials[i] = Material.IronDoor;
            for (int i = 3871; i <= 3872; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 3873; i <= 3874; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 3875; i <= 3876; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 3877; i <= 3878; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 3879; i <= 3880; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 3881; i <= 3882; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 3883; i <= 3884; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 3885; i <= 3886; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 3887; i <= 3894; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 3895; i <= 3918; i++)
                materials[i] = Material.StoneButton;
            for (int i = 3919; i <= 3926; i++)
                materials[i] = Material.Snow;
            materials[3927] = Material.Ice;
            materials[3928] = Material.SnowBlock;
            for (int i = 3929; i <= 3944; i++)
                materials[i] = Material.Cactus;
            materials[3945] = Material.Clay;
            for (int i = 3946; i <= 3961; i++)
                materials[i] = Material.SugarCane;
            for (int i = 3962; i <= 3963; i++)
                materials[i] = Material.Jukebox;
            for (int i = 3964; i <= 3995; i++)
                materials[i] = Material.OakFence;
            materials[3996] = Material.Pumpkin;
            materials[3997] = Material.Netherrack;
            materials[3998] = Material.SoulSand;
            materials[3999] = Material.Glowstone;
            for (int i = 4000; i <= 4001; i++)
                materials[i] = Material.NetherPortal;
            for (int i = 4002; i <= 4005; i++)
                materials[i] = Material.CarvedPumpkin;
            for (int i = 4006; i <= 4009; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 4010; i <= 4016; i++)
                materials[i] = Material.Cake;
            for (int i = 4017; i <= 4080; i++)
                materials[i] = Material.Repeater;
            materials[4081] = Material.WhiteStainedGlass;
            materials[4082] = Material.OrangeStainedGlass;
            materials[4083] = Material.MagentaStainedGlass;
            materials[4084] = Material.LightBlueStainedGlass;
            materials[4085] = Material.YellowStainedGlass;
            materials[4086] = Material.LimeStainedGlass;
            materials[4087] = Material.PinkStainedGlass;
            materials[4088] = Material.GrayStainedGlass;
            materials[4089] = Material.LightGrayStainedGlass;
            materials[4090] = Material.CyanStainedGlass;
            materials[4091] = Material.PurpleStainedGlass;
            materials[4092] = Material.BlueStainedGlass;
            materials[4093] = Material.BrownStainedGlass;
            materials[4094] = Material.GreenStainedGlass;
            materials[4095] = Material.RedStainedGlass;
            materials[4096] = Material.BlackStainedGlass;
            for (int i = 4097; i <= 4160; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 4161; i <= 4224; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 4225; i <= 4288; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 4289; i <= 4352; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 4353; i <= 4416; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 4417; i <= 4480; i++)
                materials[i] = Material.DarkOakTrapdoor;
            materials[4481] = Material.StoneBricks;
            materials[4482] = Material.MossyStoneBricks;
            materials[4483] = Material.CrackedStoneBricks;
            materials[4484] = Material.ChiseledStoneBricks;
            materials[4485] = Material.InfestedStone;
            materials[4486] = Material.InfestedCobblestone;
            materials[4487] = Material.InfestedStoneBricks;
            materials[4488] = Material.InfestedMossyStoneBricks;
            materials[4489] = Material.InfestedCrackedStoneBricks;
            materials[4490] = Material.InfestedChiseledStoneBricks;
            for (int i = 4491; i <= 4554; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 4555; i <= 4618; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 4619; i <= 4682; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 4683; i <= 4714; i++)
                materials[i] = Material.IronBars;
            for (int i = 4715; i <= 4746; i++)
                materials[i] = Material.GlassPane;
            materials[4747] = Material.Melon;
            for (int i = 4748; i <= 4751; i++)
                materials[i] = Material.AttachedPumpkinStem;
            for (int i = 4752; i <= 4755; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 4756; i <= 4763; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 4764; i <= 4771; i++)
                materials[i] = Material.MelonStem;
            for (int i = 4772; i <= 4803; i++)
                materials[i] = Material.Vine;
            for (int i = 4804; i <= 4835; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 4836; i <= 4915; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 4916; i <= 4995; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 4996; i <= 4997; i++)
                materials[i] = Material.Mycelium;
            materials[4998] = Material.LilyPad;
            materials[4999] = Material.NetherBricks;
            for (int i = 5000; i <= 5031; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 5032; i <= 5111; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 5112; i <= 5115; i++)
                materials[i] = Material.NetherWart;
            materials[5116] = Material.EnchantingTable;
            for (int i = 5117; i <= 5124; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 5125; i <= 5128; i++)
                materials[i] = Material.Cauldron;
            materials[5129] = Material.EndPortal;
            for (int i = 5130; i <= 5137; i++)
                materials[i] = Material.EndPortalFrame;
            materials[5138] = Material.EndStone;
            materials[5139] = Material.DragonEgg;
            for (int i = 5140; i <= 5141; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 5142; i <= 5153; i++)
                materials[i] = Material.Cocoa;
            for (int i = 5154; i <= 5233; i++)
                materials[i] = Material.SandstoneStairs;
            materials[5234] = Material.EmeraldOre;
            for (int i = 5235; i <= 5242; i++)
                materials[i] = Material.EnderChest;
            for (int i = 5243; i <= 5258; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 5259; i <= 5386; i++)
                materials[i] = Material.Tripwire;
            materials[5387] = Material.EmeraldBlock;
            for (int i = 5388; i <= 5467; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 5468; i <= 5547; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 5548; i <= 5627; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 5628; i <= 5639; i++)
                materials[i] = Material.CommandBlock;
            materials[5640] = Material.Beacon;
            for (int i = 5641; i <= 5704; i++)
                materials[i] = Material.CobblestoneWall;
            for (int i = 5705; i <= 5768; i++)
                materials[i] = Material.MossyCobblestoneWall;
            materials[5769] = Material.FlowerPot;
            materials[5770] = Material.PottedOakSapling;
            materials[5771] = Material.PottedSpruceSapling;
            materials[5772] = Material.PottedBirchSapling;
            materials[5773] = Material.PottedJungleSapling;
            materials[5774] = Material.PottedAcaciaSapling;
            materials[5775] = Material.PottedDarkOakSapling;
            materials[5776] = Material.PottedFern;
            materials[5777] = Material.PottedDandelion;
            materials[5778] = Material.PottedPoppy;
            materials[5779] = Material.PottedBlueOrchid;
            materials[5780] = Material.PottedAllium;
            materials[5781] = Material.PottedAzureBluet;
            materials[5782] = Material.PottedRedTulip;
            materials[5783] = Material.PottedOrangeTulip;
            materials[5784] = Material.PottedWhiteTulip;
            materials[5785] = Material.PottedPinkTulip;
            materials[5786] = Material.PottedOxeyeDaisy;
            materials[5787] = Material.PottedCornflower;
            materials[5788] = Material.PottedLilyOfTheValley;
            materials[5789] = Material.PottedWitherRose;
            materials[5790] = Material.PottedRedMushroom;
            materials[5791] = Material.PottedBrownMushroom;
            materials[5792] = Material.PottedDeadBush;
            materials[5793] = Material.PottedCactus;
            for (int i = 5794; i <= 5801; i++)
                materials[i] = Material.Carrots;
            for (int i = 5802; i <= 5809; i++)
                materials[i] = Material.Potatoes;
            for (int i = 5810; i <= 5833; i++)
                materials[i] = Material.OakButton;
            for (int i = 5834; i <= 5857; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 5858; i <= 5881; i++)
                materials[i] = Material.BirchButton;
            for (int i = 5882; i <= 5905; i++)
                materials[i] = Material.JungleButton;
            for (int i = 5906; i <= 5929; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 5930; i <= 5953; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 5954; i <= 5969; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 5970; i <= 5973; i++)
                materials[i] = Material.SkeletonWallSkull;
            for (int i = 5974; i <= 5989; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 5990; i <= 5993; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 5994; i <= 6009; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 6010; i <= 6013; i++)
                materials[i] = Material.ZombieWallHead;
            for (int i = 6014; i <= 6029; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 6030; i <= 6033; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 6034; i <= 6049; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 6050; i <= 6053; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 6054; i <= 6069; i++)
                materials[i] = Material.DragonHead;
            for (int i = 6070; i <= 6073; i++)
                materials[i] = Material.DragonWallHead;
            for (int i = 6074; i <= 6077; i++)
                materials[i] = Material.Anvil;
            for (int i = 6078; i <= 6081; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 6082; i <= 6085; i++)
                materials[i] = Material.DamagedAnvil;
            for (int i = 6086; i <= 6109; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 6110; i <= 6125; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 6126; i <= 6141; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            for (int i = 6142; i <= 6157; i++)
                materials[i] = Material.Comparator;
            for (int i = 6158; i <= 6189; i++)
                materials[i] = Material.DaylightDetector;
            materials[6190] = Material.RedstoneBlock;
            materials[6191] = Material.NetherQuartzOre;
            for (int i = 6192; i <= 6201; i++)
                materials[i] = Material.Hopper;
            materials[6202] = Material.QuartzBlock;
            materials[6203] = Material.ChiseledQuartzBlock;
            for (int i = 6204; i <= 6206; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 6207; i <= 6286; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 6287; i <= 6298; i++)
                materials[i] = Material.ActivatorRail;
            for (int i = 6299; i <= 6310; i++)
                materials[i] = Material.Dropper;
            materials[6311] = Material.WhiteTerracotta;
            materials[6312] = Material.OrangeTerracotta;
            materials[6313] = Material.MagentaTerracotta;
            materials[6314] = Material.LightBlueTerracotta;
            materials[6315] = Material.YellowTerracotta;
            materials[6316] = Material.LimeTerracotta;
            materials[6317] = Material.PinkTerracotta;
            materials[6318] = Material.GrayTerracotta;
            materials[6319] = Material.LightGrayTerracotta;
            materials[6320] = Material.CyanTerracotta;
            materials[6321] = Material.PurpleTerracotta;
            materials[6322] = Material.BlueTerracotta;
            materials[6323] = Material.BrownTerracotta;
            materials[6324] = Material.GreenTerracotta;
            materials[6325] = Material.RedTerracotta;
            materials[6326] = Material.BlackTerracotta;
            for (int i = 6327; i <= 6358; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            for (int i = 6359; i <= 6390; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            for (int i = 6391; i <= 6422; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            for (int i = 6423; i <= 6454; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            for (int i = 6455; i <= 6486; i++)
                materials[i] = Material.YellowStainedGlassPane;
            for (int i = 6487; i <= 6518; i++)
                materials[i] = Material.LimeStainedGlassPane;
            for (int i = 6519; i <= 6550; i++)
                materials[i] = Material.PinkStainedGlassPane;
            for (int i = 6551; i <= 6582; i++)
                materials[i] = Material.GrayStainedGlassPane;
            for (int i = 6583; i <= 6614; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            for (int i = 6615; i <= 6646; i++)
                materials[i] = Material.CyanStainedGlassPane;
            for (int i = 6647; i <= 6678; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            for (int i = 6679; i <= 6710; i++)
                materials[i] = Material.BlueStainedGlassPane;
            for (int i = 6711; i <= 6742; i++)
                materials[i] = Material.BrownStainedGlassPane;
            for (int i = 6743; i <= 6774; i++)
                materials[i] = Material.GreenStainedGlassPane;
            for (int i = 6775; i <= 6806; i++)
                materials[i] = Material.RedStainedGlassPane;
            for (int i = 6807; i <= 6838; i++)
                materials[i] = Material.BlackStainedGlassPane;
            for (int i = 6839; i <= 6918; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 6919; i <= 6998; i++)
                materials[i] = Material.DarkOakStairs;
            materials[6999] = Material.SlimeBlock;
            materials[7000] = Material.Barrier;
            for (int i = 7001; i <= 7064; i++)
                materials[i] = Material.IronTrapdoor;
            materials[7065] = Material.Prismarine;
            materials[7066] = Material.PrismarineBricks;
            materials[7067] = Material.DarkPrismarine;
            for (int i = 7068; i <= 7147; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 7148; i <= 7227; i++)
                materials[i] = Material.PrismarineBrickStairs;
            for (int i = 7228; i <= 7307; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 7308; i <= 7313; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 7314; i <= 7319; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 7320; i <= 7325; i++)
                materials[i] = Material.DarkPrismarineSlab;
            materials[7326] = Material.SeaLantern;
            for (int i = 7327; i <= 7329; i++)
                materials[i] = Material.HayBlock;
            materials[7330] = Material.WhiteCarpet;
            materials[7331] = Material.OrangeCarpet;
            materials[7332] = Material.MagentaCarpet;
            materials[7333] = Material.LightBlueCarpet;
            materials[7334] = Material.YellowCarpet;
            materials[7335] = Material.LimeCarpet;
            materials[7336] = Material.PinkCarpet;
            materials[7337] = Material.GrayCarpet;
            materials[7338] = Material.LightGrayCarpet;
            materials[7339] = Material.CyanCarpet;
            materials[7340] = Material.PurpleCarpet;
            materials[7341] = Material.BlueCarpet;
            materials[7342] = Material.BrownCarpet;
            materials[7343] = Material.GreenCarpet;
            materials[7344] = Material.RedCarpet;
            materials[7345] = Material.BlackCarpet;
            materials[7346] = Material.Terracotta;
            materials[7347] = Material.CoalBlock;
            materials[7348] = Material.PackedIce;
            for (int i = 7349; i <= 7350; i++)
                materials[i] = Material.Sunflower;
            for (int i = 7351; i <= 7352; i++)
                materials[i] = Material.Lilac;
            for (int i = 7353; i <= 7354; i++)
                materials[i] = Material.RoseBush;
            for (int i = 7355; i <= 7356; i++)
                materials[i] = Material.Peony;
            for (int i = 7357; i <= 7358; i++)
                materials[i] = Material.TallGrass;
            for (int i = 7359; i <= 7360; i++)
                materials[i] = Material.LargeFern;
            for (int i = 7361; i <= 7376; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 7377; i <= 7392; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 7393; i <= 7408; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 7409; i <= 7424; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 7425; i <= 7440; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 7441; i <= 7456; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 7457; i <= 7472; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 7473; i <= 7488; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 7489; i <= 7504; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 7505; i <= 7520; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 7521; i <= 7536; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 7537; i <= 7552; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 7553; i <= 7568; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 7569; i <= 7584; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 7585; i <= 7600; i++)
                materials[i] = Material.RedBanner;
            for (int i = 7601; i <= 7616; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 7617; i <= 7620; i++)
                materials[i] = Material.WhiteWallBanner;
            for (int i = 7621; i <= 7624; i++)
                materials[i] = Material.OrangeWallBanner;
            for (int i = 7625; i <= 7628; i++)
                materials[i] = Material.MagentaWallBanner;
            for (int i = 7629; i <= 7632; i++)
                materials[i] = Material.LightBlueWallBanner;
            for (int i = 7633; i <= 7636; i++)
                materials[i] = Material.YellowWallBanner;
            for (int i = 7637; i <= 7640; i++)
                materials[i] = Material.LimeWallBanner;
            for (int i = 7641; i <= 7644; i++)
                materials[i] = Material.PinkWallBanner;
            for (int i = 7645; i <= 7648; i++)
                materials[i] = Material.GrayWallBanner;
            for (int i = 7649; i <= 7652; i++)
                materials[i] = Material.LightGrayWallBanner;
            for (int i = 7653; i <= 7656; i++)
                materials[i] = Material.CyanWallBanner;
            for (int i = 7657; i <= 7660; i++)
                materials[i] = Material.PurpleWallBanner;
            for (int i = 7661; i <= 7664; i++)
                materials[i] = Material.BlueWallBanner;
            for (int i = 7665; i <= 7668; i++)
                materials[i] = Material.BrownWallBanner;
            for (int i = 7669; i <= 7672; i++)
                materials[i] = Material.GreenWallBanner;
            for (int i = 7673; i <= 7676; i++)
                materials[i] = Material.RedWallBanner;
            for (int i = 7677; i <= 7680; i++)
                materials[i] = Material.BlackWallBanner;
            materials[7681] = Material.RedSandstone;
            materials[7682] = Material.ChiseledRedSandstone;
            materials[7683] = Material.CutRedSandstone;
            for (int i = 7684; i <= 7763; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 7764; i <= 7769; i++)
                materials[i] = Material.OakSlab;
            for (int i = 7770; i <= 7775; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 7776; i <= 7781; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 7782; i <= 7787; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 7788; i <= 7793; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 7794; i <= 7799; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 7800; i <= 7805; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 7806; i <= 7811; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 7812; i <= 7817; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 7818; i <= 7823; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 7824; i <= 7829; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 7830; i <= 7835; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 7836; i <= 7841; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 7842; i <= 7847; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 7848; i <= 7853; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 7854; i <= 7859; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 7860; i <= 7865; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 7866; i <= 7871; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            for (int i = 7872; i <= 7877; i++)
                materials[i] = Material.PurpurSlab;
            materials[7878] = Material.SmoothStone;
            materials[7879] = Material.SmoothSandstone;
            materials[7880] = Material.SmoothQuartz;
            materials[7881] = Material.SmoothRedSandstone;
            for (int i = 7882; i <= 7913; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 7914; i <= 7945; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 7946; i <= 7977; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 7978; i <= 8009; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 8010; i <= 8041; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 8042; i <= 8073; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 8074; i <= 8105; i++)
                materials[i] = Material.BirchFence;
            for (int i = 8106; i <= 8137; i++)
                materials[i] = Material.JungleFence;
            for (int i = 8138; i <= 8169; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 8170; i <= 8201; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 8202; i <= 8265; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 8266; i <= 8329; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 8330; i <= 8393; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 8394; i <= 8457; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 8458; i <= 8521; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 8522; i <= 8527; i++)
                materials[i] = Material.EndRod;
            for (int i = 8528; i <= 8591; i++)
                materials[i] = Material.ChorusPlant;
            for (int i = 8592; i <= 8597; i++)
                materials[i] = Material.ChorusFlower;
            materials[8598] = Material.PurpurBlock;
            for (int i = 8599; i <= 8601; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 8602; i <= 8681; i++)
                materials[i] = Material.PurpurStairs;
            materials[8682] = Material.EndStoneBricks;
            for (int i = 8683; i <= 8686; i++)
                materials[i] = Material.Beetroots;
            materials[8687] = Material.GrassPath;
            materials[8688] = Material.EndGateway;
            for (int i = 8689; i <= 8700; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 8701; i <= 8712; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 8713; i <= 8716; i++)
                materials[i] = Material.FrostedIce;
            materials[8717] = Material.MagmaBlock;
            materials[8718] = Material.NetherWartBlock;
            materials[8719] = Material.RedNetherBricks;
            for (int i = 8720; i <= 8722; i++)
                materials[i] = Material.BoneBlock;
            materials[8723] = Material.StructureVoid;
            for (int i = 8724; i <= 8735; i++)
                materials[i] = Material.Observer;
            for (int i = 8736; i <= 8741; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 8742; i <= 8747; i++)
                materials[i] = Material.WhiteShulkerBox;
            for (int i = 8748; i <= 8753; i++)
                materials[i] = Material.OrangeShulkerBox;
            for (int i = 8754; i <= 8759; i++)
                materials[i] = Material.MagentaShulkerBox;
            for (int i = 8760; i <= 8765; i++)
                materials[i] = Material.LightBlueShulkerBox;
            for (int i = 8766; i <= 8771; i++)
                materials[i] = Material.YellowShulkerBox;
            for (int i = 8772; i <= 8777; i++)
                materials[i] = Material.LimeShulkerBox;
            for (int i = 8778; i <= 8783; i++)
                materials[i] = Material.PinkShulkerBox;
            for (int i = 8784; i <= 8789; i++)
                materials[i] = Material.GrayShulkerBox;
            for (int i = 8790; i <= 8795; i++)
                materials[i] = Material.LightGrayShulkerBox;
            for (int i = 8796; i <= 8801; i++)
                materials[i] = Material.CyanShulkerBox;
            for (int i = 8802; i <= 8807; i++)
                materials[i] = Material.PurpleShulkerBox;
            for (int i = 8808; i <= 8813; i++)
                materials[i] = Material.BlueShulkerBox;
            for (int i = 8814; i <= 8819; i++)
                materials[i] = Material.BrownShulkerBox;
            for (int i = 8820; i <= 8825; i++)
                materials[i] = Material.GreenShulkerBox;
            for (int i = 8826; i <= 8831; i++)
                materials[i] = Material.RedShulkerBox;
            for (int i = 8832; i <= 8837; i++)
                materials[i] = Material.BlackShulkerBox;
            for (int i = 8838; i <= 8841; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 8842; i <= 8845; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 8846; i <= 8849; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 8850; i <= 8853; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 8854; i <= 8857; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 8858; i <= 8861; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 8862; i <= 8865; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 8866; i <= 8869; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 8870; i <= 8873; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 8874; i <= 8877; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 8878; i <= 8881; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 8882; i <= 8885; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            for (int i = 8886; i <= 8889; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            for (int i = 8890; i <= 8893; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 8894; i <= 8897; i++)
                materials[i] = Material.RedGlazedTerracotta;
            for (int i = 8898; i <= 8901; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            materials[8902] = Material.WhiteConcrete;
            materials[8903] = Material.OrangeConcrete;
            materials[8904] = Material.MagentaConcrete;
            materials[8905] = Material.LightBlueConcrete;
            materials[8906] = Material.YellowConcrete;
            materials[8907] = Material.LimeConcrete;
            materials[8908] = Material.PinkConcrete;
            materials[8909] = Material.GrayConcrete;
            materials[8910] = Material.LightGrayConcrete;
            materials[8911] = Material.CyanConcrete;
            materials[8912] = Material.PurpleConcrete;
            materials[8913] = Material.BlueConcrete;
            materials[8914] = Material.BrownConcrete;
            materials[8915] = Material.GreenConcrete;
            materials[8916] = Material.RedConcrete;
            materials[8917] = Material.BlackConcrete;
            materials[8918] = Material.WhiteConcretePowder;
            materials[8919] = Material.OrangeConcretePowder;
            materials[8920] = Material.MagentaConcretePowder;
            materials[8921] = Material.LightBlueConcretePowder;
            materials[8922] = Material.YellowConcretePowder;
            materials[8923] = Material.LimeConcretePowder;
            materials[8924] = Material.PinkConcretePowder;
            materials[8925] = Material.GrayConcretePowder;
            materials[8926] = Material.LightGrayConcretePowder;
            materials[8927] = Material.CyanConcretePowder;
            materials[8928] = Material.PurpleConcretePowder;
            materials[8929] = Material.BlueConcretePowder;
            materials[8930] = Material.BrownConcretePowder;
            materials[8931] = Material.GreenConcretePowder;
            materials[8932] = Material.RedConcretePowder;
            materials[8933] = Material.BlackConcretePowder;
            for (int i = 8934; i <= 8959; i++)
                materials[i] = Material.Kelp;
            materials[8960] = Material.KelpPlant;
            materials[8961] = Material.DriedKelpBlock;
            for (int i = 8962; i <= 8973; i++)
                materials[i] = Material.TurtleEgg;
            materials[8974] = Material.DeadTubeCoralBlock;
            materials[8975] = Material.DeadBrainCoralBlock;
            materials[8976] = Material.DeadBubbleCoralBlock;
            materials[8977] = Material.DeadFireCoralBlock;
            materials[8978] = Material.DeadHornCoralBlock;
            materials[8979] = Material.TubeCoralBlock;
            materials[8980] = Material.BrainCoralBlock;
            materials[8981] = Material.BubbleCoralBlock;
            materials[8982] = Material.FireCoralBlock;
            materials[8983] = Material.HornCoralBlock;
            for (int i = 8984; i <= 8985; i++)
                materials[i] = Material.DeadTubeCoral;
            for (int i = 8986; i <= 8987; i++)
                materials[i] = Material.DeadBrainCoral;
            for (int i = 8988; i <= 8989; i++)
                materials[i] = Material.DeadBubbleCoral;
            for (int i = 8990; i <= 8991; i++)
                materials[i] = Material.DeadFireCoral;
            for (int i = 8992; i <= 8993; i++)
                materials[i] = Material.DeadHornCoral;
            for (int i = 8994; i <= 8995; i++)
                materials[i] = Material.TubeCoral;
            for (int i = 8996; i <= 8997; i++)
                materials[i] = Material.BrainCoral;
            for (int i = 8998; i <= 8999; i++)
                materials[i] = Material.BubbleCoral;
            for (int i = 9000; i <= 9001; i++)
                materials[i] = Material.FireCoral;
            for (int i = 9002; i <= 9003; i++)
                materials[i] = Material.HornCoral;
            for (int i = 9004; i <= 9005; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 9006; i <= 9007; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 9008; i <= 9009; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 9010; i <= 9011; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 9012; i <= 9013; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 9014; i <= 9015; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 9016; i <= 9017; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 9018; i <= 9019; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 9020; i <= 9021; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 9022; i <= 9023; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 9024; i <= 9031; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 9032; i <= 9039; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 9040; i <= 9047; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            for (int i = 9048; i <= 9055; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 9056; i <= 9063; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 9064; i <= 9071; i++)
                materials[i] = Material.TubeCoralWallFan;
            for (int i = 9072; i <= 9079; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 9080; i <= 9087; i++)
                materials[i] = Material.BubbleCoralWallFan;
            for (int i = 9088; i <= 9095; i++)
                materials[i] = Material.FireCoralWallFan;
            for (int i = 9096; i <= 9103; i++)
                materials[i] = Material.HornCoralWallFan;
            for (int i = 9104; i <= 9111; i++)
                materials[i] = Material.SeaPickle;
            materials[9112] = Material.BlueIce;
            for (int i = 9113; i <= 9114; i++)
                materials[i] = Material.Conduit;
            materials[9115] = Material.BambooSapling;
            for (int i = 9116; i <= 9127; i++)
                materials[i] = Material.Bamboo;
            materials[9128] = Material.PottedBamboo;
            materials[9129] = Material.VoidAir;
            materials[9130] = Material.CaveAir;
            for (int i = 9131; i <= 9132; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 9133; i <= 9212; i++)
                materials[i] = Material.PolishedGraniteStairs;
            for (int i = 9213; i <= 9292; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            for (int i = 9293; i <= 9372; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 9373; i <= 9452; i++)
                materials[i] = Material.PolishedDioriteStairs;
            for (int i = 9453; i <= 9532; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 9533; i <= 9612; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 9613; i <= 9692; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 9693; i <= 9772; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            for (int i = 9773; i <= 9852; i++)
                materials[i] = Material.SmoothQuartzStairs;
            for (int i = 9853; i <= 9932; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 9933; i <= 10012; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 10013; i <= 10092; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 10093; i <= 10172; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 10173; i <= 10252; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 10253; i <= 10258; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 10259; i <= 10264; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 10265; i <= 10270; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 10271; i <= 10276; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 10277; i <= 10282; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 10283; i <= 10288; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 10289; i <= 10294; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 10295; i <= 10300; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 10301; i <= 10306; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 10307; i <= 10312; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 10313; i <= 10318; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 10319; i <= 10324; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 10325; i <= 10330; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 10331; i <= 10394; i++)
                materials[i] = Material.BrickWall;
            for (int i = 10395; i <= 10458; i++)
                materials[i] = Material.PrismarineWall;
            for (int i = 10459; i <= 10522; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 10523; i <= 10586; i++)
                materials[i] = Material.MossyStoneBrickWall;
            for (int i = 10587; i <= 10650; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 10651; i <= 10714; i++)
                materials[i] = Material.StoneBrickWall;
            for (int i = 10715; i <= 10778; i++)
                materials[i] = Material.NetherBrickWall;
            for (int i = 10779; i <= 10842; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 10843; i <= 10906; i++)
                materials[i] = Material.RedNetherBrickWall;
            for (int i = 10907; i <= 10970; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 10971; i <= 11034; i++)
                materials[i] = Material.EndStoneBrickWall;
            for (int i = 11035; i <= 11098; i++)
                materials[i] = Material.DioriteWall;
            for (int i = 11099; i <= 11130; i++)
                materials[i] = Material.Scaffolding;
            for (int i = 11131; i <= 11134; i++)
                materials[i] = Material.Loom;
            for (int i = 11135; i <= 11146; i++)
                materials[i] = Material.Barrel;
            for (int i = 11147; i <= 11154; i++)
                materials[i] = Material.Smoker;
            for (int i = 11155; i <= 11162; i++)
                materials[i] = Material.BlastFurnace;
            materials[11163] = Material.CartographyTable;
            materials[11164] = Material.FletchingTable;
            for (int i = 11165; i <= 11176; i++)
                materials[i] = Material.Grindstone;
            for (int i = 11177; i <= 11192; i++)
                materials[i] = Material.Lectern;
            materials[11193] = Material.SmithingTable;
            for (int i = 11194; i <= 11197; i++)
                materials[i] = Material.Stonecutter;
            for (int i = 11198; i <= 11213; i++)
                materials[i] = Material.Bell;
            for (int i = 11214; i <= 11215; i++)
                materials[i] = Material.Lantern;
            for (int i = 11216; i <= 11247; i++)
                materials[i] = Material.Campfire;
            for (int i = 11248; i <= 11251; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 11252; i <= 11255; i++)
                materials[i] = Material.StructureBlock;
            for (int i = 11256; i <= 11261; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 11262; i <= 11270; i++)
                materials[i] = Material.Composter;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
