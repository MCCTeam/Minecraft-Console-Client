using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette261 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette261()
        {
            for (int i = 0; i <= 0; i++)
                materials[i] = Material.Air;
            for (int i = 1; i <= 1; i++)
                materials[i] = Material.Stone;
            for (int i = 2; i <= 2; i++)
                materials[i] = Material.Granite;
            for (int i = 3; i <= 3; i++)
                materials[i] = Material.PolishedGranite;
            for (int i = 4; i <= 4; i++)
                materials[i] = Material.Diorite;
            for (int i = 5; i <= 5; i++)
                materials[i] = Material.PolishedDiorite;
            for (int i = 6; i <= 6; i++)
                materials[i] = Material.Andesite;
            for (int i = 7; i <= 7; i++)
                materials[i] = Material.PolishedAndesite;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            for (int i = 10; i <= 10; i++)
                materials[i] = Material.Dirt;
            for (int i = 11; i <= 11; i++)
                materials[i] = Material.CoarseDirt;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            for (int i = 14; i <= 14; i++)
                materials[i] = Material.Cobblestone;
            for (int i = 15; i <= 15; i++)
                materials[i] = Material.OakPlanks;
            for (int i = 16; i <= 16; i++)
                materials[i] = Material.SprucePlanks;
            for (int i = 17; i <= 17; i++)
                materials[i] = Material.BirchPlanks;
            for (int i = 18; i <= 18; i++)
                materials[i] = Material.JunglePlanks;
            for (int i = 19; i <= 19; i++)
                materials[i] = Material.AcaciaPlanks;
            for (int i = 20; i <= 20; i++)
                materials[i] = Material.CherryPlanks;
            for (int i = 21; i <= 21; i++)
                materials[i] = Material.DarkOakPlanks;
            for (int i = 22; i <= 24; i++)
                materials[i] = Material.PaleOakWood;
            for (int i = 25; i <= 25; i++)
                materials[i] = Material.PaleOakPlanks;
            for (int i = 26; i <= 26; i++)
                materials[i] = Material.MangrovePlanks;
            for (int i = 27; i <= 27; i++)
                materials[i] = Material.BambooPlanks;
            for (int i = 28; i <= 28; i++)
                materials[i] = Material.BambooMosaic;
            for (int i = 29; i <= 30; i++)
                materials[i] = Material.OakSapling;
            for (int i = 31; i <= 32; i++)
                materials[i] = Material.SpruceSapling;
            for (int i = 33; i <= 34; i++)
                materials[i] = Material.BirchSapling;
            for (int i = 35; i <= 36; i++)
                materials[i] = Material.JungleSapling;
            for (int i = 37; i <= 38; i++)
                materials[i] = Material.AcaciaSapling;
            for (int i = 39; i <= 40; i++)
                materials[i] = Material.CherrySapling;
            for (int i = 41; i <= 42; i++)
                materials[i] = Material.DarkOakSapling;
            for (int i = 43; i <= 44; i++)
                materials[i] = Material.PaleOakSapling;
            for (int i = 45; i <= 84; i++)
                materials[i] = Material.MangrovePropagule;
            for (int i = 85; i <= 85; i++)
                materials[i] = Material.Bedrock;
            for (int i = 86; i <= 101; i++)
                materials[i] = Material.Water;
            for (int i = 102; i <= 117; i++)
                materials[i] = Material.Lava;
            for (int i = 118; i <= 118; i++)
                materials[i] = Material.Sand;
            for (int i = 119; i <= 122; i++)
                materials[i] = Material.SuspiciousSand;
            for (int i = 123; i <= 123; i++)
                materials[i] = Material.RedSand;
            for (int i = 124; i <= 124; i++)
                materials[i] = Material.Gravel;
            for (int i = 125; i <= 128; i++)
                materials[i] = Material.SuspiciousGravel;
            for (int i = 129; i <= 129; i++)
                materials[i] = Material.GoldOre;
            for (int i = 130; i <= 130; i++)
                materials[i] = Material.DeepslateGoldOre;
            for (int i = 131; i <= 131; i++)
                materials[i] = Material.IronOre;
            for (int i = 132; i <= 132; i++)
                materials[i] = Material.DeepslateIronOre;
            for (int i = 133; i <= 133; i++)
                materials[i] = Material.CoalOre;
            for (int i = 134; i <= 134; i++)
                materials[i] = Material.DeepslateCoalOre;
            for (int i = 135; i <= 135; i++)
                materials[i] = Material.NetherGoldOre;
            for (int i = 136; i <= 138; i++)
                materials[i] = Material.OakLog;
            for (int i = 139; i <= 141; i++)
                materials[i] = Material.SpruceLog;
            for (int i = 142; i <= 144; i++)
                materials[i] = Material.BirchLog;
            for (int i = 145; i <= 147; i++)
                materials[i] = Material.JungleLog;
            for (int i = 148; i <= 150; i++)
                materials[i] = Material.AcaciaLog;
            for (int i = 151; i <= 153; i++)
                materials[i] = Material.CherryLog;
            for (int i = 154; i <= 156; i++)
                materials[i] = Material.DarkOakLog;
            for (int i = 157; i <= 159; i++)
                materials[i] = Material.PaleOakLog;
            for (int i = 160; i <= 162; i++)
                materials[i] = Material.MangroveLog;
            for (int i = 163; i <= 164; i++)
                materials[i] = Material.MangroveRoots;
            for (int i = 165; i <= 167; i++)
                materials[i] = Material.MuddyMangroveRoots;
            for (int i = 168; i <= 170; i++)
                materials[i] = Material.BambooBlock;
            for (int i = 171; i <= 173; i++)
                materials[i] = Material.StrippedSpruceLog;
            for (int i = 174; i <= 176; i++)
                materials[i] = Material.StrippedBirchLog;
            for (int i = 177; i <= 179; i++)
                materials[i] = Material.StrippedJungleLog;
            for (int i = 180; i <= 182; i++)
                materials[i] = Material.StrippedAcaciaLog;
            for (int i = 183; i <= 185; i++)
                materials[i] = Material.StrippedCherryLog;
            for (int i = 186; i <= 188; i++)
                materials[i] = Material.StrippedDarkOakLog;
            for (int i = 189; i <= 191; i++)
                materials[i] = Material.StrippedPaleOakLog;
            for (int i = 192; i <= 194; i++)
                materials[i] = Material.StrippedOakLog;
            for (int i = 195; i <= 197; i++)
                materials[i] = Material.StrippedMangroveLog;
            for (int i = 198; i <= 200; i++)
                materials[i] = Material.StrippedBambooBlock;
            for (int i = 201; i <= 203; i++)
                materials[i] = Material.OakWood;
            for (int i = 204; i <= 206; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 207; i <= 209; i++)
                materials[i] = Material.BirchWood;
            for (int i = 210; i <= 212; i++)
                materials[i] = Material.JungleWood;
            for (int i = 213; i <= 215; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 216; i <= 218; i++)
                materials[i] = Material.CherryWood;
            for (int i = 219; i <= 221; i++)
                materials[i] = Material.DarkOakWood;
            for (int i = 222; i <= 224; i++)
                materials[i] = Material.MangroveWood;
            for (int i = 225; i <= 227; i++)
                materials[i] = Material.StrippedOakWood;
            for (int i = 228; i <= 230; i++)
                materials[i] = Material.StrippedSpruceWood;
            for (int i = 231; i <= 233; i++)
                materials[i] = Material.StrippedBirchWood;
            for (int i = 234; i <= 236; i++)
                materials[i] = Material.StrippedJungleWood;
            for (int i = 237; i <= 239; i++)
                materials[i] = Material.StrippedAcaciaWood;
            for (int i = 240; i <= 242; i++)
                materials[i] = Material.StrippedCherryWood;
            for (int i = 243; i <= 245; i++)
                materials[i] = Material.StrippedDarkOakWood;
            for (int i = 246; i <= 248; i++)
                materials[i] = Material.StrippedPaleOakWood;
            for (int i = 249; i <= 251; i++)
                materials[i] = Material.StrippedMangroveWood;
            for (int i = 252; i <= 279; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 280; i <= 307; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 308; i <= 335; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 336; i <= 363; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 364; i <= 391; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 392; i <= 419; i++)
                materials[i] = Material.CherryLeaves;
            for (int i = 420; i <= 447; i++)
                materials[i] = Material.DarkOakLeaves;
            for (int i = 448; i <= 475; i++)
                materials[i] = Material.PaleOakLeaves;
            for (int i = 476; i <= 503; i++)
                materials[i] = Material.MangroveLeaves;
            for (int i = 504; i <= 531; i++)
                materials[i] = Material.AzaleaLeaves;
            for (int i = 532; i <= 559; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            for (int i = 560; i <= 560; i++)
                materials[i] = Material.Sponge;
            for (int i = 561; i <= 561; i++)
                materials[i] = Material.WetSponge;
            for (int i = 562; i <= 562; i++)
                materials[i] = Material.Glass;
            for (int i = 563; i <= 563; i++)
                materials[i] = Material.LapisOre;
            for (int i = 564; i <= 564; i++)
                materials[i] = Material.DeepslateLapisOre;
            for (int i = 565; i <= 565; i++)
                materials[i] = Material.LapisBlock;
            for (int i = 566; i <= 577; i++)
                materials[i] = Material.Dispenser;
            for (int i = 578; i <= 578; i++)
                materials[i] = Material.Sandstone;
            for (int i = 579; i <= 579; i++)
                materials[i] = Material.ChiseledSandstone;
            for (int i = 580; i <= 580; i++)
                materials[i] = Material.CutSandstone;
            for (int i = 581; i <= 1930; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 1931; i <= 1946; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 1947; i <= 1962; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 1963; i <= 1978; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 1979; i <= 1994; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 1995; i <= 2010; i++)
                materials[i] = Material.YellowBed;
            for (int i = 2011; i <= 2026; i++)
                materials[i] = Material.LimeBed;
            for (int i = 2027; i <= 2042; i++)
                materials[i] = Material.PinkBed;
            for (int i = 2043; i <= 2058; i++)
                materials[i] = Material.GrayBed;
            for (int i = 2059; i <= 2074; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 2075; i <= 2090; i++)
                materials[i] = Material.CyanBed;
            for (int i = 2091; i <= 2106; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 2107; i <= 2122; i++)
                materials[i] = Material.BlueBed;
            for (int i = 2123; i <= 2138; i++)
                materials[i] = Material.BrownBed;
            for (int i = 2139; i <= 2154; i++)
                materials[i] = Material.GreenBed;
            for (int i = 2155; i <= 2170; i++)
                materials[i] = Material.RedBed;
            for (int i = 2171; i <= 2186; i++)
                materials[i] = Material.BlackBed;
            for (int i = 2187; i <= 2210; i++)
                materials[i] = Material.PoweredRail;
            for (int i = 2211; i <= 2234; i++)
                materials[i] = Material.DetectorRail;
            for (int i = 2235; i <= 2246; i++)
                materials[i] = Material.StickyPiston;
            for (int i = 2247; i <= 2247; i++)
                materials[i] = Material.Cobweb;
            for (int i = 2248; i <= 2248; i++)
                materials[i] = Material.ShortGrass;
            for (int i = 2249; i <= 2249; i++)
                materials[i] = Material.Fern;
            for (int i = 2250; i <= 2250; i++)
                materials[i] = Material.DeadBush;
            for (int i = 2251; i <= 2251; i++)
                materials[i] = Material.Bush;
            for (int i = 2252; i <= 2252; i++)
                materials[i] = Material.ShortDryGrass;
            for (int i = 2253; i <= 2253; i++)
                materials[i] = Material.TallDryGrass;
            for (int i = 2254; i <= 2254; i++)
                materials[i] = Material.Seagrass;
            for (int i = 2255; i <= 2256; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 2257; i <= 2268; i++)
                materials[i] = Material.Piston;
            for (int i = 2269; i <= 2292; i++)
                materials[i] = Material.PistonHead;
            for (int i = 2293; i <= 2293; i++)
                materials[i] = Material.WhiteWool;
            for (int i = 2294; i <= 2294; i++)
                materials[i] = Material.OrangeWool;
            for (int i = 2295; i <= 2295; i++)
                materials[i] = Material.MagentaWool;
            for (int i = 2296; i <= 2296; i++)
                materials[i] = Material.LightBlueWool;
            for (int i = 2297; i <= 2297; i++)
                materials[i] = Material.YellowWool;
            for (int i = 2298; i <= 2298; i++)
                materials[i] = Material.LimeWool;
            for (int i = 2299; i <= 2299; i++)
                materials[i] = Material.PinkWool;
            for (int i = 2300; i <= 2300; i++)
                materials[i] = Material.GrayWool;
            for (int i = 2301; i <= 2301; i++)
                materials[i] = Material.LightGrayWool;
            for (int i = 2302; i <= 2302; i++)
                materials[i] = Material.CyanWool;
            for (int i = 2303; i <= 2303; i++)
                materials[i] = Material.PurpleWool;
            for (int i = 2304; i <= 2304; i++)
                materials[i] = Material.BlueWool;
            for (int i = 2305; i <= 2305; i++)
                materials[i] = Material.BrownWool;
            for (int i = 2306; i <= 2306; i++)
                materials[i] = Material.GreenWool;
            for (int i = 2307; i <= 2307; i++)
                materials[i] = Material.RedWool;
            for (int i = 2308; i <= 2308; i++)
                materials[i] = Material.BlackWool;
            for (int i = 2309; i <= 2320; i++)
                materials[i] = Material.MovingPiston;
            for (int i = 2321; i <= 2321; i++)
                materials[i] = Material.Dandelion;
            for (int i = 2322; i <= 2322; i++)
                materials[i] = Material.GoldenDandelion;
            for (int i = 2323; i <= 2323; i++)
                materials[i] = Material.Torchflower;
            for (int i = 2324; i <= 2324; i++)
                materials[i] = Material.Poppy;
            for (int i = 2325; i <= 2325; i++)
                materials[i] = Material.BlueOrchid;
            for (int i = 2326; i <= 2326; i++)
                materials[i] = Material.Allium;
            for (int i = 2327; i <= 2327; i++)
                materials[i] = Material.AzureBluet;
            for (int i = 2328; i <= 2328; i++)
                materials[i] = Material.RedTulip;
            for (int i = 2329; i <= 2329; i++)
                materials[i] = Material.OrangeTulip;
            for (int i = 2330; i <= 2330; i++)
                materials[i] = Material.WhiteTulip;
            for (int i = 2331; i <= 2331; i++)
                materials[i] = Material.PinkTulip;
            for (int i = 2332; i <= 2332; i++)
                materials[i] = Material.OxeyeDaisy;
            for (int i = 2333; i <= 2333; i++)
                materials[i] = Material.Cornflower;
            for (int i = 2334; i <= 2334; i++)
                materials[i] = Material.WitherRose;
            for (int i = 2335; i <= 2335; i++)
                materials[i] = Material.LilyOfTheValley;
            for (int i = 2336; i <= 2336; i++)
                materials[i] = Material.BrownMushroom;
            for (int i = 2337; i <= 2337; i++)
                materials[i] = Material.RedMushroom;
            for (int i = 2338; i <= 2338; i++)
                materials[i] = Material.GoldBlock;
            for (int i = 2339; i <= 2339; i++)
                materials[i] = Material.IronBlock;
            for (int i = 2340; i <= 2340; i++)
                materials[i] = Material.Bricks;
            for (int i = 2341; i <= 2342; i++)
                materials[i] = Material.Tnt;
            for (int i = 2343; i <= 2343; i++)
                materials[i] = Material.Bookshelf;
            for (int i = 2344; i <= 2599; i++)
                materials[i] = Material.ChiseledBookshelf;
            for (int i = 2600; i <= 2663; i++)
                materials[i] = Material.AcaciaShelf;
            for (int i = 2664; i <= 2727; i++)
                materials[i] = Material.BambooShelf;
            for (int i = 2728; i <= 2791; i++)
                materials[i] = Material.BirchShelf;
            for (int i = 2792; i <= 2855; i++)
                materials[i] = Material.CherryShelf;
            for (int i = 2856; i <= 2919; i++)
                materials[i] = Material.CrimsonShelf;
            for (int i = 2920; i <= 2983; i++)
                materials[i] = Material.DarkOakShelf;
            for (int i = 2984; i <= 3047; i++)
                materials[i] = Material.JungleShelf;
            for (int i = 3048; i <= 3111; i++)
                materials[i] = Material.MangroveShelf;
            for (int i = 3112; i <= 3175; i++)
                materials[i] = Material.OakShelf;
            for (int i = 3176; i <= 3239; i++)
                materials[i] = Material.PaleOakShelf;
            for (int i = 3240; i <= 3303; i++)
                materials[i] = Material.SpruceShelf;
            for (int i = 3304; i <= 3367; i++)
                materials[i] = Material.WarpedShelf;
            for (int i = 3368; i <= 3368; i++)
                materials[i] = Material.MossyCobblestone;
            for (int i = 3369; i <= 3369; i++)
                materials[i] = Material.Obsidian;
            for (int i = 3370; i <= 3370; i++)
                materials[i] = Material.Torch;
            for (int i = 3371; i <= 3374; i++)
                materials[i] = Material.WallTorch;
            for (int i = 3375; i <= 3886; i++)
                materials[i] = Material.Fire;
            for (int i = 3887; i <= 3887; i++)
                materials[i] = Material.SoulFire;
            for (int i = 3888; i <= 3888; i++)
                materials[i] = Material.Spawner;
            for (int i = 3889; i <= 3906; i++)
                materials[i] = Material.CreakingHeart;
            for (int i = 3907; i <= 3986; i++)
                materials[i] = Material.OakStairs;
            for (int i = 3987; i <= 4010; i++)
                materials[i] = Material.Chest;
            for (int i = 4011; i <= 5306; i++)
                materials[i] = Material.RedstoneWire;
            for (int i = 5307; i <= 5307; i++)
                materials[i] = Material.DiamondOre;
            for (int i = 5308; i <= 5308; i++)
                materials[i] = Material.DeepslateDiamondOre;
            for (int i = 5309; i <= 5309; i++)
                materials[i] = Material.DiamondBlock;
            for (int i = 5310; i <= 5310; i++)
                materials[i] = Material.CraftingTable;
            for (int i = 5311; i <= 5318; i++)
                materials[i] = Material.Wheat;
            for (int i = 5319; i <= 5326; i++)
                materials[i] = Material.Farmland;
            for (int i = 5327; i <= 5334; i++)
                materials[i] = Material.Furnace;
            for (int i = 5335; i <= 5366; i++)
                materials[i] = Material.OakSign;
            for (int i = 5367; i <= 5398; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 5399; i <= 5430; i++)
                materials[i] = Material.BirchSign;
            for (int i = 5431; i <= 5462; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 5463; i <= 5494; i++)
                materials[i] = Material.CherrySign;
            for (int i = 5495; i <= 5526; i++)
                materials[i] = Material.JungleSign;
            for (int i = 5527; i <= 5558; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 5559; i <= 5590; i++)
                materials[i] = Material.PaleOakSign;
            for (int i = 5591; i <= 5622; i++)
                materials[i] = Material.MangroveSign;
            for (int i = 5623; i <= 5654; i++)
                materials[i] = Material.BambooSign;
            for (int i = 5655; i <= 5718; i++)
                materials[i] = Material.OakDoor;
            for (int i = 5719; i <= 5726; i++)
                materials[i] = Material.Ladder;
            for (int i = 5727; i <= 5746; i++)
                materials[i] = Material.Rail;
            for (int i = 5747; i <= 5826; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 5827; i <= 5834; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 5835; i <= 5842; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 5843; i <= 5850; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 5851; i <= 5858; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 5859; i <= 5866; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 5867; i <= 5874; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 5875; i <= 5882; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 5883; i <= 5890; i++)
                materials[i] = Material.PaleOakWallSign;
            for (int i = 5891; i <= 5898; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 5899; i <= 5906; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 5907; i <= 5970; i++)
                materials[i] = Material.OakHangingSign;
            for (int i = 5971; i <= 6034; i++)
                materials[i] = Material.SpruceHangingSign;
            for (int i = 6035; i <= 6098; i++)
                materials[i] = Material.BirchHangingSign;
            for (int i = 6099; i <= 6162; i++)
                materials[i] = Material.AcaciaHangingSign;
            for (int i = 6163; i <= 6226; i++)
                materials[i] = Material.CherryHangingSign;
            for (int i = 6227; i <= 6290; i++)
                materials[i] = Material.JungleHangingSign;
            for (int i = 6291; i <= 6354; i++)
                materials[i] = Material.DarkOakHangingSign;
            for (int i = 6355; i <= 6418; i++)
                materials[i] = Material.PaleOakHangingSign;
            for (int i = 6419; i <= 6482; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 6483; i <= 6546; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 6547; i <= 6610; i++)
                materials[i] = Material.MangroveHangingSign;
            for (int i = 6611; i <= 6674; i++)
                materials[i] = Material.BambooHangingSign;
            for (int i = 6675; i <= 6682; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 6683; i <= 6690; i++)
                materials[i] = Material.SpruceWallHangingSign;
            for (int i = 6691; i <= 6698; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 6699; i <= 6706; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 6707; i <= 6714; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 6715; i <= 6722; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 6723; i <= 6730; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 6731; i <= 6738; i++)
                materials[i] = Material.PaleOakWallHangingSign;
            for (int i = 6739; i <= 6746; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 6747; i <= 6754; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 6755; i <= 6762; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 6763; i <= 6770; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 6771; i <= 6794; i++)
                materials[i] = Material.Lever;
            for (int i = 6795; i <= 6796; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 6797; i <= 6860; i++)
                materials[i] = Material.IronDoor;
            for (int i = 6861; i <= 6862; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 6863; i <= 6864; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 6865; i <= 6866; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 6867; i <= 6868; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 6869; i <= 6870; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 6871; i <= 6872; i++)
                materials[i] = Material.CherryPressurePlate;
            for (int i = 6873; i <= 6874; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 6875; i <= 6876; i++)
                materials[i] = Material.PaleOakPressurePlate;
            for (int i = 6877; i <= 6878; i++)
                materials[i] = Material.MangrovePressurePlate;
            for (int i = 6879; i <= 6880; i++)
                materials[i] = Material.BambooPressurePlate;
            for (int i = 6881; i <= 6882; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 6883; i <= 6884; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 6885; i <= 6886; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 6887; i <= 6894; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 6895; i <= 6918; i++)
                materials[i] = Material.StoneButton;
            for (int i = 6919; i <= 6926; i++)
                materials[i] = Material.Snow;
            for (int i = 6927; i <= 6927; i++)
                materials[i] = Material.Ice;
            for (int i = 6928; i <= 6928; i++)
                materials[i] = Material.SnowBlock;
            for (int i = 6929; i <= 6944; i++)
                materials[i] = Material.Cactus;
            for (int i = 6945; i <= 6945; i++)
                materials[i] = Material.CactusFlower;
            for (int i = 6946; i <= 6946; i++)
                materials[i] = Material.Clay;
            for (int i = 6947; i <= 6962; i++)
                materials[i] = Material.SugarCane;
            for (int i = 6963; i <= 6964; i++)
                materials[i] = Material.Jukebox;
            for (int i = 6965; i <= 6996; i++)
                materials[i] = Material.OakFence;
            for (int i = 6997; i <= 6997; i++)
                materials[i] = Material.Netherrack;
            for (int i = 6998; i <= 6998; i++)
                materials[i] = Material.SoulSand;
            for (int i = 6999; i <= 6999; i++)
                materials[i] = Material.SoulSoil;
            for (int i = 7000; i <= 7002; i++)
                materials[i] = Material.Basalt;
            for (int i = 7003; i <= 7005; i++)
                materials[i] = Material.PolishedBasalt;
            for (int i = 7006; i <= 7006; i++)
                materials[i] = Material.SoulTorch;
            for (int i = 7007; i <= 7010; i++)
                materials[i] = Material.SoulWallTorch;
            for (int i = 7011; i <= 7011; i++)
                materials[i] = Material.CopperTorch;
            for (int i = 7012; i <= 7015; i++)
                materials[i] = Material.CopperWallTorch;
            for (int i = 7016; i <= 7016; i++)
                materials[i] = Material.Glowstone;
            for (int i = 7017; i <= 7018; i++)
                materials[i] = Material.NetherPortal;
            for (int i = 7019; i <= 7022; i++)
                materials[i] = Material.CarvedPumpkin;
            for (int i = 7023; i <= 7026; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 7027; i <= 7033; i++)
                materials[i] = Material.Cake;
            for (int i = 7034; i <= 7097; i++)
                materials[i] = Material.Repeater;
            for (int i = 7098; i <= 7098; i++)
                materials[i] = Material.WhiteStainedGlass;
            for (int i = 7099; i <= 7099; i++)
                materials[i] = Material.OrangeStainedGlass;
            for (int i = 7100; i <= 7100; i++)
                materials[i] = Material.MagentaStainedGlass;
            for (int i = 7101; i <= 7101; i++)
                materials[i] = Material.LightBlueStainedGlass;
            for (int i = 7102; i <= 7102; i++)
                materials[i] = Material.YellowStainedGlass;
            for (int i = 7103; i <= 7103; i++)
                materials[i] = Material.LimeStainedGlass;
            for (int i = 7104; i <= 7104; i++)
                materials[i] = Material.PinkStainedGlass;
            for (int i = 7105; i <= 7105; i++)
                materials[i] = Material.GrayStainedGlass;
            for (int i = 7106; i <= 7106; i++)
                materials[i] = Material.LightGrayStainedGlass;
            for (int i = 7107; i <= 7107; i++)
                materials[i] = Material.CyanStainedGlass;
            for (int i = 7108; i <= 7108; i++)
                materials[i] = Material.PurpleStainedGlass;
            for (int i = 7109; i <= 7109; i++)
                materials[i] = Material.BlueStainedGlass;
            for (int i = 7110; i <= 7110; i++)
                materials[i] = Material.BrownStainedGlass;
            for (int i = 7111; i <= 7111; i++)
                materials[i] = Material.GreenStainedGlass;
            for (int i = 7112; i <= 7112; i++)
                materials[i] = Material.RedStainedGlass;
            for (int i = 7113; i <= 7113; i++)
                materials[i] = Material.BlackStainedGlass;
            for (int i = 7114; i <= 7177; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 7178; i <= 7241; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 7242; i <= 7305; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 7306; i <= 7369; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 7370; i <= 7433; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 7434; i <= 7497; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 7498; i <= 7561; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 7562; i <= 7625; i++)
                materials[i] = Material.PaleOakTrapdoor;
            for (int i = 7626; i <= 7689; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 7690; i <= 7753; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 7754; i <= 7754; i++)
                materials[i] = Material.StoneBricks;
            for (int i = 7755; i <= 7755; i++)
                materials[i] = Material.MossyStoneBricks;
            for (int i = 7756; i <= 7756; i++)
                materials[i] = Material.CrackedStoneBricks;
            for (int i = 7757; i <= 7757; i++)
                materials[i] = Material.ChiseledStoneBricks;
            for (int i = 7758; i <= 7758; i++)
                materials[i] = Material.PackedMud;
            for (int i = 7759; i <= 7759; i++)
                materials[i] = Material.MudBricks;
            for (int i = 7760; i <= 7760; i++)
                materials[i] = Material.InfestedStone;
            for (int i = 7761; i <= 7761; i++)
                materials[i] = Material.InfestedCobblestone;
            for (int i = 7762; i <= 7762; i++)
                materials[i] = Material.InfestedStoneBricks;
            for (int i = 7763; i <= 7763; i++)
                materials[i] = Material.InfestedMossyStoneBricks;
            for (int i = 7764; i <= 7764; i++)
                materials[i] = Material.InfestedCrackedStoneBricks;
            for (int i = 7765; i <= 7765; i++)
                materials[i] = Material.InfestedChiseledStoneBricks;
            for (int i = 7766; i <= 7829; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 7830; i <= 7893; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 7894; i <= 7957; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 7958; i <= 7989; i++)
                materials[i] = Material.IronBars;
            for (int i = 7990; i <= 8021; i++)
                materials[i] = Material.CopperBars;
            for (int i = 8022; i <= 8053; i++)
                materials[i] = Material.ExposedCopperBars;
            for (int i = 8054; i <= 8085; i++)
                materials[i] = Material.WeatheredCopperBars;
            for (int i = 8086; i <= 8117; i++)
                materials[i] = Material.OxidizedCopperBars;
            for (int i = 8118; i <= 8149; i++)
                materials[i] = Material.WaxedCopperBars;
            for (int i = 8150; i <= 8181; i++)
                materials[i] = Material.WaxedExposedCopperBars;
            for (int i = 8182; i <= 8213; i++)
                materials[i] = Material.WaxedWeatheredCopperBars;
            for (int i = 8214; i <= 8245; i++)
                materials[i] = Material.WaxedOxidizedCopperBars;
            for (int i = 8246; i <= 8251; i++)
                materials[i] = Material.IronChain;
            for (int i = 8252; i <= 8257; i++)
                materials[i] = Material.CopperChain;
            for (int i = 8258; i <= 8263; i++)
                materials[i] = Material.ExposedCopperChain;
            for (int i = 8264; i <= 8269; i++)
                materials[i] = Material.WeatheredCopperChain;
            for (int i = 8270; i <= 8275; i++)
                materials[i] = Material.OxidizedCopperChain;
            for (int i = 8276; i <= 8281; i++)
                materials[i] = Material.WaxedCopperChain;
            for (int i = 8282; i <= 8287; i++)
                materials[i] = Material.WaxedExposedCopperChain;
            for (int i = 8288; i <= 8293; i++)
                materials[i] = Material.WaxedWeatheredCopperChain;
            for (int i = 8294; i <= 8299; i++)
                materials[i] = Material.WaxedOxidizedCopperChain;
            for (int i = 8300; i <= 8331; i++)
                materials[i] = Material.GlassPane;
            for (int i = 8332; i <= 8332; i++)
                materials[i] = Material.Pumpkin;
            for (int i = 8333; i <= 8333; i++)
                materials[i] = Material.Melon;
            for (int i = 8334; i <= 8337; i++)
                materials[i] = Material.AttachedPumpkinStem;
            for (int i = 8338; i <= 8341; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 8342; i <= 8349; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 8350; i <= 8357; i++)
                materials[i] = Material.MelonStem;
            for (int i = 8358; i <= 8389; i++)
                materials[i] = Material.Vine;
            for (int i = 8390; i <= 8517; i++)
                materials[i] = Material.GlowLichen;
            for (int i = 8518; i <= 8645; i++)
                materials[i] = Material.ResinClump;
            for (int i = 8646; i <= 8677; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 8678; i <= 8757; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 8758; i <= 8837; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 8838; i <= 8917; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 8918; i <= 8919; i++)
                materials[i] = Material.Mycelium;
            for (int i = 8920; i <= 8920; i++)
                materials[i] = Material.LilyPad;
            for (int i = 8921; i <= 8921; i++)
                materials[i] = Material.ResinBlock;
            for (int i = 8922; i <= 8922; i++)
                materials[i] = Material.ResinBricks;
            for (int i = 8923; i <= 9002; i++)
                materials[i] = Material.ResinBrickStairs;
            for (int i = 9003; i <= 9008; i++)
                materials[i] = Material.ResinBrickSlab;
            for (int i = 9009; i <= 9332; i++)
                materials[i] = Material.ResinBrickWall;
            for (int i = 9333; i <= 9333; i++)
                materials[i] = Material.ChiseledResinBricks;
            for (int i = 9334; i <= 9334; i++)
                materials[i] = Material.NetherBricks;
            for (int i = 9335; i <= 9366; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 9367; i <= 9446; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 9447; i <= 9450; i++)
                materials[i] = Material.NetherWart;
            for (int i = 9451; i <= 9451; i++)
                materials[i] = Material.EnchantingTable;
            for (int i = 9452; i <= 9459; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 9460; i <= 9460; i++)
                materials[i] = Material.Cauldron;
            for (int i = 9461; i <= 9463; i++)
                materials[i] = Material.WaterCauldron;
            for (int i = 9464; i <= 9464; i++)
                materials[i] = Material.LavaCauldron;
            for (int i = 9465; i <= 9467; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 9468; i <= 9468; i++)
                materials[i] = Material.EndPortal;
            for (int i = 9469; i <= 9476; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 9477; i <= 9477; i++)
                materials[i] = Material.EndStone;
            for (int i = 9478; i <= 9478; i++)
                materials[i] = Material.DragonEgg;
            for (int i = 9479; i <= 9480; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 9481; i <= 9492; i++)
                materials[i] = Material.Cocoa;
            for (int i = 9493; i <= 9572; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 9573; i <= 9573; i++)
                materials[i] = Material.EmeraldOre;
            for (int i = 9574; i <= 9574; i++)
                materials[i] = Material.DeepslateEmeraldOre;
            for (int i = 9575; i <= 9582; i++)
                materials[i] = Material.EnderChest;
            for (int i = 9583; i <= 9598; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 9599; i <= 9726; i++)
                materials[i] = Material.Tripwire;
            for (int i = 9727; i <= 9727; i++)
                materials[i] = Material.EmeraldBlock;
            for (int i = 9728; i <= 9807; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 9808; i <= 9887; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 9888; i <= 9967; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 9968; i <= 9979; i++)
                materials[i] = Material.CommandBlock;
            for (int i = 9980; i <= 9980; i++)
                materials[i] = Material.Beacon;
            for (int i = 9981; i <= 10304; i++)
                materials[i] = Material.CobblestoneWall;
            for (int i = 10305; i <= 10628; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 10629; i <= 10629; i++)
                materials[i] = Material.FlowerPot;
            for (int i = 10630; i <= 10630; i++)
                materials[i] = Material.PottedTorchflower;
            for (int i = 10631; i <= 10631; i++)
                materials[i] = Material.PottedOakSapling;
            for (int i = 10632; i <= 10632; i++)
                materials[i] = Material.PottedSpruceSapling;
            for (int i = 10633; i <= 10633; i++)
                materials[i] = Material.PottedBirchSapling;
            for (int i = 10634; i <= 10634; i++)
                materials[i] = Material.PottedJungleSapling;
            for (int i = 10635; i <= 10635; i++)
                materials[i] = Material.PottedAcaciaSapling;
            for (int i = 10636; i <= 10636; i++)
                materials[i] = Material.PottedCherrySapling;
            for (int i = 10637; i <= 10637; i++)
                materials[i] = Material.PottedDarkOakSapling;
            for (int i = 10638; i <= 10638; i++)
                materials[i] = Material.PottedPaleOakSapling;
            for (int i = 10639; i <= 10639; i++)
                materials[i] = Material.PottedMangrovePropagule;
            for (int i = 10640; i <= 10640; i++)
                materials[i] = Material.PottedFern;
            for (int i = 10641; i <= 10641; i++)
                materials[i] = Material.PottedDandelion;
            for (int i = 10642; i <= 10642; i++)
                materials[i] = Material.PottedGoldenDandelion;
            for (int i = 10643; i <= 10643; i++)
                materials[i] = Material.PottedPoppy;
            for (int i = 10644; i <= 10644; i++)
                materials[i] = Material.PottedBlueOrchid;
            for (int i = 10645; i <= 10645; i++)
                materials[i] = Material.PottedAllium;
            for (int i = 10646; i <= 10646; i++)
                materials[i] = Material.PottedAzureBluet;
            for (int i = 10647; i <= 10647; i++)
                materials[i] = Material.PottedRedTulip;
            for (int i = 10648; i <= 10648; i++)
                materials[i] = Material.PottedOrangeTulip;
            for (int i = 10649; i <= 10649; i++)
                materials[i] = Material.PottedWhiteTulip;
            for (int i = 10650; i <= 10650; i++)
                materials[i] = Material.PottedPinkTulip;
            for (int i = 10651; i <= 10651; i++)
                materials[i] = Material.PottedOxeyeDaisy;
            for (int i = 10652; i <= 10652; i++)
                materials[i] = Material.PottedCornflower;
            for (int i = 10653; i <= 10653; i++)
                materials[i] = Material.PottedLilyOfTheValley;
            for (int i = 10654; i <= 10654; i++)
                materials[i] = Material.PottedWitherRose;
            for (int i = 10655; i <= 10655; i++)
                materials[i] = Material.PottedRedMushroom;
            for (int i = 10656; i <= 10656; i++)
                materials[i] = Material.PottedBrownMushroom;
            for (int i = 10657; i <= 10657; i++)
                materials[i] = Material.PottedDeadBush;
            for (int i = 10658; i <= 10658; i++)
                materials[i] = Material.PottedCactus;
            for (int i = 10659; i <= 10666; i++)
                materials[i] = Material.Carrots;
            for (int i = 10667; i <= 10674; i++)
                materials[i] = Material.Potatoes;
            for (int i = 10675; i <= 10698; i++)
                materials[i] = Material.OakButton;
            for (int i = 10699; i <= 10722; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 10723; i <= 10746; i++)
                materials[i] = Material.BirchButton;
            for (int i = 10747; i <= 10770; i++)
                materials[i] = Material.JungleButton;
            for (int i = 10771; i <= 10794; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 10795; i <= 10818; i++)
                materials[i] = Material.CherryButton;
            for (int i = 10819; i <= 10842; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 10843; i <= 10866; i++)
                materials[i] = Material.PaleOakButton;
            for (int i = 10867; i <= 10890; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 10891; i <= 10914; i++)
                materials[i] = Material.BambooButton;
            for (int i = 10915; i <= 10946; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 10947; i <= 10954; i++)
                materials[i] = Material.SkeletonWallSkull;
            for (int i = 10955; i <= 10986; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 10987; i <= 10994; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 10995; i <= 11026; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 11027; i <= 11034; i++)
                materials[i] = Material.ZombieWallHead;
            for (int i = 11035; i <= 11066; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 11067; i <= 11074; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 11075; i <= 11106; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 11107; i <= 11114; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 11115; i <= 11146; i++)
                materials[i] = Material.DragonHead;
            for (int i = 11147; i <= 11154; i++)
                materials[i] = Material.DragonWallHead;
            for (int i = 11155; i <= 11186; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 11187; i <= 11194; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 11195; i <= 11198; i++)
                materials[i] = Material.Anvil;
            for (int i = 11199; i <= 11202; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 11203; i <= 11206; i++)
                materials[i] = Material.DamagedAnvil;
            for (int i = 11207; i <= 11230; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 11231; i <= 11246; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 11247; i <= 11262; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            for (int i = 11263; i <= 11278; i++)
                materials[i] = Material.Comparator;
            for (int i = 11279; i <= 11310; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 11311; i <= 11311; i++)
                materials[i] = Material.RedstoneBlock;
            for (int i = 11312; i <= 11312; i++)
                materials[i] = Material.NetherQuartzOre;
            for (int i = 11313; i <= 11322; i++)
                materials[i] = Material.Hopper;
            for (int i = 11323; i <= 11323; i++)
                materials[i] = Material.QuartzBlock;
            for (int i = 11324; i <= 11324; i++)
                materials[i] = Material.ChiseledQuartzBlock;
            for (int i = 11325; i <= 11327; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 11328; i <= 11407; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 11408; i <= 11431; i++)
                materials[i] = Material.ActivatorRail;
            for (int i = 11432; i <= 11443; i++)
                materials[i] = Material.Dropper;
            for (int i = 11444; i <= 11444; i++)
                materials[i] = Material.WhiteTerracotta;
            for (int i = 11445; i <= 11445; i++)
                materials[i] = Material.OrangeTerracotta;
            for (int i = 11446; i <= 11446; i++)
                materials[i] = Material.MagentaTerracotta;
            for (int i = 11447; i <= 11447; i++)
                materials[i] = Material.LightBlueTerracotta;
            for (int i = 11448; i <= 11448; i++)
                materials[i] = Material.YellowTerracotta;
            for (int i = 11449; i <= 11449; i++)
                materials[i] = Material.LimeTerracotta;
            for (int i = 11450; i <= 11450; i++)
                materials[i] = Material.PinkTerracotta;
            for (int i = 11451; i <= 11451; i++)
                materials[i] = Material.GrayTerracotta;
            for (int i = 11452; i <= 11452; i++)
                materials[i] = Material.LightGrayTerracotta;
            for (int i = 11453; i <= 11453; i++)
                materials[i] = Material.CyanTerracotta;
            for (int i = 11454; i <= 11454; i++)
                materials[i] = Material.PurpleTerracotta;
            for (int i = 11455; i <= 11455; i++)
                materials[i] = Material.BlueTerracotta;
            for (int i = 11456; i <= 11456; i++)
                materials[i] = Material.BrownTerracotta;
            for (int i = 11457; i <= 11457; i++)
                materials[i] = Material.GreenTerracotta;
            for (int i = 11458; i <= 11458; i++)
                materials[i] = Material.RedTerracotta;
            for (int i = 11459; i <= 11459; i++)
                materials[i] = Material.BlackTerracotta;
            for (int i = 11460; i <= 11491; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            for (int i = 11492; i <= 11523; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            for (int i = 11524; i <= 11555; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            for (int i = 11556; i <= 11587; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            for (int i = 11588; i <= 11619; i++)
                materials[i] = Material.YellowStainedGlassPane;
            for (int i = 11620; i <= 11651; i++)
                materials[i] = Material.LimeStainedGlassPane;
            for (int i = 11652; i <= 11683; i++)
                materials[i] = Material.PinkStainedGlassPane;
            for (int i = 11684; i <= 11715; i++)
                materials[i] = Material.GrayStainedGlassPane;
            for (int i = 11716; i <= 11747; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            for (int i = 11748; i <= 11779; i++)
                materials[i] = Material.CyanStainedGlassPane;
            for (int i = 11780; i <= 11811; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            for (int i = 11812; i <= 11843; i++)
                materials[i] = Material.BlueStainedGlassPane;
            for (int i = 11844; i <= 11875; i++)
                materials[i] = Material.BrownStainedGlassPane;
            for (int i = 11876; i <= 11907; i++)
                materials[i] = Material.GreenStainedGlassPane;
            for (int i = 11908; i <= 11939; i++)
                materials[i] = Material.RedStainedGlassPane;
            for (int i = 11940; i <= 11971; i++)
                materials[i] = Material.BlackStainedGlassPane;
            for (int i = 11972; i <= 12051; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 12052; i <= 12131; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 12132; i <= 12211; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 12212; i <= 12291; i++)
                materials[i] = Material.PaleOakStairs;
            for (int i = 12292; i <= 12371; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 12372; i <= 12451; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 12452; i <= 12531; i++)
                materials[i] = Material.BambooMosaicStairs;
            for (int i = 12532; i <= 12532; i++)
                materials[i] = Material.SlimeBlock;
            for (int i = 12533; i <= 12534; i++)
                materials[i] = Material.Barrier;
            for (int i = 12535; i <= 12566; i++)
                materials[i] = Material.Light;
            for (int i = 12567; i <= 12630; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 12631; i <= 12631; i++)
                materials[i] = Material.Prismarine;
            for (int i = 12632; i <= 12632; i++)
                materials[i] = Material.PrismarineBricks;
            for (int i = 12633; i <= 12633; i++)
                materials[i] = Material.DarkPrismarine;
            for (int i = 12634; i <= 12713; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 12714; i <= 12793; i++)
                materials[i] = Material.PrismarineBrickStairs;
            for (int i = 12794; i <= 12873; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 12874; i <= 12879; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 12880; i <= 12885; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 12886; i <= 12891; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 12892; i <= 12892; i++)
                materials[i] = Material.SeaLantern;
            for (int i = 12893; i <= 12895; i++)
                materials[i] = Material.HayBlock;
            for (int i = 12896; i <= 12896; i++)
                materials[i] = Material.WhiteCarpet;
            for (int i = 12897; i <= 12897; i++)
                materials[i] = Material.OrangeCarpet;
            for (int i = 12898; i <= 12898; i++)
                materials[i] = Material.MagentaCarpet;
            for (int i = 12899; i <= 12899; i++)
                materials[i] = Material.LightBlueCarpet;
            for (int i = 12900; i <= 12900; i++)
                materials[i] = Material.YellowCarpet;
            for (int i = 12901; i <= 12901; i++)
                materials[i] = Material.LimeCarpet;
            for (int i = 12902; i <= 12902; i++)
                materials[i] = Material.PinkCarpet;
            for (int i = 12903; i <= 12903; i++)
                materials[i] = Material.GrayCarpet;
            for (int i = 12904; i <= 12904; i++)
                materials[i] = Material.LightGrayCarpet;
            for (int i = 12905; i <= 12905; i++)
                materials[i] = Material.CyanCarpet;
            for (int i = 12906; i <= 12906; i++)
                materials[i] = Material.PurpleCarpet;
            for (int i = 12907; i <= 12907; i++)
                materials[i] = Material.BlueCarpet;
            for (int i = 12908; i <= 12908; i++)
                materials[i] = Material.BrownCarpet;
            for (int i = 12909; i <= 12909; i++)
                materials[i] = Material.GreenCarpet;
            for (int i = 12910; i <= 12910; i++)
                materials[i] = Material.RedCarpet;
            for (int i = 12911; i <= 12911; i++)
                materials[i] = Material.BlackCarpet;
            for (int i = 12912; i <= 12912; i++)
                materials[i] = Material.Terracotta;
            for (int i = 12913; i <= 12913; i++)
                materials[i] = Material.CoalBlock;
            for (int i = 12914; i <= 12914; i++)
                materials[i] = Material.PackedIce;
            for (int i = 12915; i <= 12916; i++)
                materials[i] = Material.Sunflower;
            for (int i = 12917; i <= 12918; i++)
                materials[i] = Material.Lilac;
            for (int i = 12919; i <= 12920; i++)
                materials[i] = Material.RoseBush;
            for (int i = 12921; i <= 12922; i++)
                materials[i] = Material.Peony;
            for (int i = 12923; i <= 12924; i++)
                materials[i] = Material.TallGrass;
            for (int i = 12925; i <= 12926; i++)
                materials[i] = Material.LargeFern;
            for (int i = 12927; i <= 12942; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 12943; i <= 12958; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 12959; i <= 12974; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 12975; i <= 12990; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 12991; i <= 13006; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 13007; i <= 13022; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 13023; i <= 13038; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 13039; i <= 13054; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 13055; i <= 13070; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 13071; i <= 13086; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 13087; i <= 13102; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 13103; i <= 13118; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 13119; i <= 13134; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 13135; i <= 13150; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 13151; i <= 13166; i++)
                materials[i] = Material.RedBanner;
            for (int i = 13167; i <= 13182; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 13183; i <= 13186; i++)
                materials[i] = Material.WhiteWallBanner;
            for (int i = 13187; i <= 13190; i++)
                materials[i] = Material.OrangeWallBanner;
            for (int i = 13191; i <= 13194; i++)
                materials[i] = Material.MagentaWallBanner;
            for (int i = 13195; i <= 13198; i++)
                materials[i] = Material.LightBlueWallBanner;
            for (int i = 13199; i <= 13202; i++)
                materials[i] = Material.YellowWallBanner;
            for (int i = 13203; i <= 13206; i++)
                materials[i] = Material.LimeWallBanner;
            for (int i = 13207; i <= 13210; i++)
                materials[i] = Material.PinkWallBanner;
            for (int i = 13211; i <= 13214; i++)
                materials[i] = Material.GrayWallBanner;
            for (int i = 13215; i <= 13218; i++)
                materials[i] = Material.LightGrayWallBanner;
            for (int i = 13219; i <= 13222; i++)
                materials[i] = Material.CyanWallBanner;
            for (int i = 13223; i <= 13226; i++)
                materials[i] = Material.PurpleWallBanner;
            for (int i = 13227; i <= 13230; i++)
                materials[i] = Material.BlueWallBanner;
            for (int i = 13231; i <= 13234; i++)
                materials[i] = Material.BrownWallBanner;
            for (int i = 13235; i <= 13238; i++)
                materials[i] = Material.GreenWallBanner;
            for (int i = 13239; i <= 13242; i++)
                materials[i] = Material.RedWallBanner;
            for (int i = 13243; i <= 13246; i++)
                materials[i] = Material.BlackWallBanner;
            for (int i = 13247; i <= 13247; i++)
                materials[i] = Material.RedSandstone;
            for (int i = 13248; i <= 13248; i++)
                materials[i] = Material.ChiseledRedSandstone;
            for (int i = 13249; i <= 13249; i++)
                materials[i] = Material.CutRedSandstone;
            for (int i = 13250; i <= 13329; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 13330; i <= 13335; i++)
                materials[i] = Material.OakSlab;
            for (int i = 13336; i <= 13341; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 13342; i <= 13347; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 13348; i <= 13353; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 13354; i <= 13359; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 13360; i <= 13365; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 13366; i <= 13371; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 13372; i <= 13377; i++)
                materials[i] = Material.PaleOakSlab;
            for (int i = 13378; i <= 13383; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 13384; i <= 13389; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 13390; i <= 13395; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 13396; i <= 13401; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 13402; i <= 13407; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 13408; i <= 13413; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 13414; i <= 13419; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 13420; i <= 13425; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 13426; i <= 13431; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 13432; i <= 13437; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 13438; i <= 13443; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 13444; i <= 13449; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 13450; i <= 13455; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 13456; i <= 13461; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 13462; i <= 13467; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 13468; i <= 13473; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            for (int i = 13474; i <= 13479; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 13480; i <= 13480; i++)
                materials[i] = Material.SmoothStone;
            for (int i = 13481; i <= 13481; i++)
                materials[i] = Material.SmoothSandstone;
            for (int i = 13482; i <= 13482; i++)
                materials[i] = Material.SmoothQuartz;
            for (int i = 13483; i <= 13483; i++)
                materials[i] = Material.SmoothRedSandstone;
            for (int i = 13484; i <= 13515; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 13516; i <= 13547; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 13548; i <= 13579; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 13580; i <= 13611; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 13612; i <= 13643; i++)
                materials[i] = Material.CherryFenceGate;
            for (int i = 13644; i <= 13675; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 13676; i <= 13707; i++)
                materials[i] = Material.PaleOakFenceGate;
            for (int i = 13708; i <= 13739; i++)
                materials[i] = Material.MangroveFenceGate;
            for (int i = 13740; i <= 13771; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 13772; i <= 13803; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 13804; i <= 13835; i++)
                materials[i] = Material.BirchFence;
            for (int i = 13836; i <= 13867; i++)
                materials[i] = Material.JungleFence;
            for (int i = 13868; i <= 13899; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 13900; i <= 13931; i++)
                materials[i] = Material.CherryFence;
            for (int i = 13932; i <= 13963; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 13964; i <= 13995; i++)
                materials[i] = Material.PaleOakFence;
            for (int i = 13996; i <= 14027; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 14028; i <= 14059; i++)
                materials[i] = Material.BambooFence;
            for (int i = 14060; i <= 14123; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 14124; i <= 14187; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 14188; i <= 14251; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 14252; i <= 14315; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 14316; i <= 14379; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 14380; i <= 14443; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 14444; i <= 14507; i++)
                materials[i] = Material.PaleOakDoor;
            for (int i = 14508; i <= 14571; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 14572; i <= 14635; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 14636; i <= 14641; i++)
                materials[i] = Material.EndRod;
            for (int i = 14642; i <= 14705; i++)
                materials[i] = Material.ChorusPlant;
            for (int i = 14706; i <= 14711; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 14712; i <= 14712; i++)
                materials[i] = Material.PurpurBlock;
            for (int i = 14713; i <= 14715; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 14716; i <= 14795; i++)
                materials[i] = Material.PurpurStairs;
            for (int i = 14796; i <= 14796; i++)
                materials[i] = Material.EndStoneBricks;
            for (int i = 14797; i <= 14798; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 14799; i <= 14808; i++)
                materials[i] = Material.PitcherCrop;
            for (int i = 14809; i <= 14810; i++)
                materials[i] = Material.PitcherPlant;
            for (int i = 14811; i <= 14814; i++)
                materials[i] = Material.Beetroots;
            for (int i = 14815; i <= 14815; i++)
                materials[i] = Material.DirtPath;
            for (int i = 14816; i <= 14816; i++)
                materials[i] = Material.EndGateway;
            for (int i = 14817; i <= 14828; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 14829; i <= 14840; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 14841; i <= 14844; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 14845; i <= 14845; i++)
                materials[i] = Material.MagmaBlock;
            for (int i = 14846; i <= 14846; i++)
                materials[i] = Material.NetherWartBlock;
            for (int i = 14847; i <= 14847; i++)
                materials[i] = Material.RedNetherBricks;
            for (int i = 14848; i <= 14850; i++)
                materials[i] = Material.BoneBlock;
            for (int i = 14851; i <= 14851; i++)
                materials[i] = Material.StructureVoid;
            for (int i = 14852; i <= 14863; i++)
                materials[i] = Material.Observer;
            for (int i = 14864; i <= 14869; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 14870; i <= 14875; i++)
                materials[i] = Material.WhiteShulkerBox;
            for (int i = 14876; i <= 14881; i++)
                materials[i] = Material.OrangeShulkerBox;
            for (int i = 14882; i <= 14887; i++)
                materials[i] = Material.MagentaShulkerBox;
            for (int i = 14888; i <= 14893; i++)
                materials[i] = Material.LightBlueShulkerBox;
            for (int i = 14894; i <= 14899; i++)
                materials[i] = Material.YellowShulkerBox;
            for (int i = 14900; i <= 14905; i++)
                materials[i] = Material.LimeShulkerBox;
            for (int i = 14906; i <= 14911; i++)
                materials[i] = Material.PinkShulkerBox;
            for (int i = 14912; i <= 14917; i++)
                materials[i] = Material.GrayShulkerBox;
            for (int i = 14918; i <= 14923; i++)
                materials[i] = Material.LightGrayShulkerBox;
            for (int i = 14924; i <= 14929; i++)
                materials[i] = Material.CyanShulkerBox;
            for (int i = 14930; i <= 14935; i++)
                materials[i] = Material.PurpleShulkerBox;
            for (int i = 14936; i <= 14941; i++)
                materials[i] = Material.BlueShulkerBox;
            for (int i = 14942; i <= 14947; i++)
                materials[i] = Material.BrownShulkerBox;
            for (int i = 14948; i <= 14953; i++)
                materials[i] = Material.GreenShulkerBox;
            for (int i = 14954; i <= 14959; i++)
                materials[i] = Material.RedShulkerBox;
            for (int i = 14960; i <= 14965; i++)
                materials[i] = Material.BlackShulkerBox;
            for (int i = 14966; i <= 14969; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 14970; i <= 14973; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 14974; i <= 14977; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 14978; i <= 14981; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 14982; i <= 14985; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 14986; i <= 14989; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 14990; i <= 14993; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 14994; i <= 14997; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 14998; i <= 15001; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 15002; i <= 15005; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 15006; i <= 15009; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 15010; i <= 15013; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            for (int i = 15014; i <= 15017; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            for (int i = 15018; i <= 15021; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 15022; i <= 15025; i++)
                materials[i] = Material.RedGlazedTerracotta;
            for (int i = 15026; i <= 15029; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 15030; i <= 15030; i++)
                materials[i] = Material.WhiteConcrete;
            for (int i = 15031; i <= 15031; i++)
                materials[i] = Material.OrangeConcrete;
            for (int i = 15032; i <= 15032; i++)
                materials[i] = Material.MagentaConcrete;
            for (int i = 15033; i <= 15033; i++)
                materials[i] = Material.LightBlueConcrete;
            for (int i = 15034; i <= 15034; i++)
                materials[i] = Material.YellowConcrete;
            for (int i = 15035; i <= 15035; i++)
                materials[i] = Material.LimeConcrete;
            for (int i = 15036; i <= 15036; i++)
                materials[i] = Material.PinkConcrete;
            for (int i = 15037; i <= 15037; i++)
                materials[i] = Material.GrayConcrete;
            for (int i = 15038; i <= 15038; i++)
                materials[i] = Material.LightGrayConcrete;
            for (int i = 15039; i <= 15039; i++)
                materials[i] = Material.CyanConcrete;
            for (int i = 15040; i <= 15040; i++)
                materials[i] = Material.PurpleConcrete;
            for (int i = 15041; i <= 15041; i++)
                materials[i] = Material.BlueConcrete;
            for (int i = 15042; i <= 15042; i++)
                materials[i] = Material.BrownConcrete;
            for (int i = 15043; i <= 15043; i++)
                materials[i] = Material.GreenConcrete;
            for (int i = 15044; i <= 15044; i++)
                materials[i] = Material.RedConcrete;
            for (int i = 15045; i <= 15045; i++)
                materials[i] = Material.BlackConcrete;
            for (int i = 15046; i <= 15046; i++)
                materials[i] = Material.WhiteConcretePowder;
            for (int i = 15047; i <= 15047; i++)
                materials[i] = Material.OrangeConcretePowder;
            for (int i = 15048; i <= 15048; i++)
                materials[i] = Material.MagentaConcretePowder;
            for (int i = 15049; i <= 15049; i++)
                materials[i] = Material.LightBlueConcretePowder;
            for (int i = 15050; i <= 15050; i++)
                materials[i] = Material.YellowConcretePowder;
            for (int i = 15051; i <= 15051; i++)
                materials[i] = Material.LimeConcretePowder;
            for (int i = 15052; i <= 15052; i++)
                materials[i] = Material.PinkConcretePowder;
            for (int i = 15053; i <= 15053; i++)
                materials[i] = Material.GrayConcretePowder;
            for (int i = 15054; i <= 15054; i++)
                materials[i] = Material.LightGrayConcretePowder;
            for (int i = 15055; i <= 15055; i++)
                materials[i] = Material.CyanConcretePowder;
            for (int i = 15056; i <= 15056; i++)
                materials[i] = Material.PurpleConcretePowder;
            for (int i = 15057; i <= 15057; i++)
                materials[i] = Material.BlueConcretePowder;
            for (int i = 15058; i <= 15058; i++)
                materials[i] = Material.BrownConcretePowder;
            for (int i = 15059; i <= 15059; i++)
                materials[i] = Material.GreenConcretePowder;
            for (int i = 15060; i <= 15060; i++)
                materials[i] = Material.RedConcretePowder;
            for (int i = 15061; i <= 15061; i++)
                materials[i] = Material.BlackConcretePowder;
            for (int i = 15062; i <= 15087; i++)
                materials[i] = Material.Kelp;
            for (int i = 15088; i <= 15088; i++)
                materials[i] = Material.KelpPlant;
            for (int i = 15089; i <= 15089; i++)
                materials[i] = Material.DriedKelpBlock;
            for (int i = 15090; i <= 15101; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 15102; i <= 15104; i++)
                materials[i] = Material.SnifferEgg;
            for (int i = 15105; i <= 15136; i++)
                materials[i] = Material.DriedGhast;
            for (int i = 15137; i <= 15137; i++)
                materials[i] = Material.DeadTubeCoralBlock;
            for (int i = 15138; i <= 15138; i++)
                materials[i] = Material.DeadBrainCoralBlock;
            for (int i = 15139; i <= 15139; i++)
                materials[i] = Material.DeadBubbleCoralBlock;
            for (int i = 15140; i <= 15140; i++)
                materials[i] = Material.DeadFireCoralBlock;
            for (int i = 15141; i <= 15141; i++)
                materials[i] = Material.DeadHornCoralBlock;
            for (int i = 15142; i <= 15142; i++)
                materials[i] = Material.TubeCoralBlock;
            for (int i = 15143; i <= 15143; i++)
                materials[i] = Material.BrainCoralBlock;
            for (int i = 15144; i <= 15144; i++)
                materials[i] = Material.BubbleCoralBlock;
            for (int i = 15145; i <= 15145; i++)
                materials[i] = Material.FireCoralBlock;
            for (int i = 15146; i <= 15146; i++)
                materials[i] = Material.HornCoralBlock;
            for (int i = 15147; i <= 15148; i++)
                materials[i] = Material.DeadTubeCoral;
            for (int i = 15149; i <= 15150; i++)
                materials[i] = Material.DeadBrainCoral;
            for (int i = 15151; i <= 15152; i++)
                materials[i] = Material.DeadBubbleCoral;
            for (int i = 15153; i <= 15154; i++)
                materials[i] = Material.DeadFireCoral;
            for (int i = 15155; i <= 15156; i++)
                materials[i] = Material.DeadHornCoral;
            for (int i = 15157; i <= 15158; i++)
                materials[i] = Material.TubeCoral;
            for (int i = 15159; i <= 15160; i++)
                materials[i] = Material.BrainCoral;
            for (int i = 15161; i <= 15162; i++)
                materials[i] = Material.BubbleCoral;
            for (int i = 15163; i <= 15164; i++)
                materials[i] = Material.FireCoral;
            for (int i = 15165; i <= 15166; i++)
                materials[i] = Material.HornCoral;
            for (int i = 15167; i <= 15168; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 15169; i <= 15170; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 15171; i <= 15172; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 15173; i <= 15174; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 15175; i <= 15176; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 15177; i <= 15178; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 15179; i <= 15180; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 15181; i <= 15182; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 15183; i <= 15184; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 15185; i <= 15186; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 15187; i <= 15194; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 15195; i <= 15202; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 15203; i <= 15210; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            for (int i = 15211; i <= 15218; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 15219; i <= 15226; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 15227; i <= 15234; i++)
                materials[i] = Material.TubeCoralWallFan;
            for (int i = 15235; i <= 15242; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 15243; i <= 15250; i++)
                materials[i] = Material.BubbleCoralWallFan;
            for (int i = 15251; i <= 15258; i++)
                materials[i] = Material.FireCoralWallFan;
            for (int i = 15259; i <= 15266; i++)
                materials[i] = Material.HornCoralWallFan;
            for (int i = 15267; i <= 15274; i++)
                materials[i] = Material.SeaPickle;
            for (int i = 15275; i <= 15275; i++)
                materials[i] = Material.BlueIce;
            for (int i = 15276; i <= 15277; i++)
                materials[i] = Material.Conduit;
            for (int i = 15278; i <= 15278; i++)
                materials[i] = Material.BambooSapling;
            for (int i = 15279; i <= 15290; i++)
                materials[i] = Material.Bamboo;
            for (int i = 15291; i <= 15291; i++)
                materials[i] = Material.PottedBamboo;
            for (int i = 15292; i <= 15292; i++)
                materials[i] = Material.VoidAir;
            for (int i = 15293; i <= 15293; i++)
                materials[i] = Material.CaveAir;
            for (int i = 15294; i <= 15295; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 15296; i <= 15375; i++)
                materials[i] = Material.PolishedGraniteStairs;
            for (int i = 15376; i <= 15455; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            for (int i = 15456; i <= 15535; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 15536; i <= 15615; i++)
                materials[i] = Material.PolishedDioriteStairs;
            for (int i = 15616; i <= 15695; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 15696; i <= 15775; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 15776; i <= 15855; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 15856; i <= 15935; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            for (int i = 15936; i <= 16015; i++)
                materials[i] = Material.SmoothQuartzStairs;
            for (int i = 16016; i <= 16095; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 16096; i <= 16175; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 16176; i <= 16255; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 16256; i <= 16335; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 16336; i <= 16415; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 16416; i <= 16421; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 16422; i <= 16427; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 16428; i <= 16433; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 16434; i <= 16439; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 16440; i <= 16445; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 16446; i <= 16451; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 16452; i <= 16457; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 16458; i <= 16463; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 16464; i <= 16469; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 16470; i <= 16475; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 16476; i <= 16481; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 16482; i <= 16487; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 16488; i <= 16493; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 16494; i <= 16817; i++)
                materials[i] = Material.BrickWall;
            for (int i = 16818; i <= 17141; i++)
                materials[i] = Material.PrismarineWall;
            for (int i = 17142; i <= 17465; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 17466; i <= 17789; i++)
                materials[i] = Material.MossyStoneBrickWall;
            for (int i = 17790; i <= 18113; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 18114; i <= 18437; i++)
                materials[i] = Material.StoneBrickWall;
            for (int i = 18438; i <= 18761; i++)
                materials[i] = Material.MudBrickWall;
            for (int i = 18762; i <= 19085; i++)
                materials[i] = Material.NetherBrickWall;
            for (int i = 19086; i <= 19409; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 19410; i <= 19733; i++)
                materials[i] = Material.RedNetherBrickWall;
            for (int i = 19734; i <= 20057; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 20058; i <= 20381; i++)
                materials[i] = Material.EndStoneBrickWall;
            for (int i = 20382; i <= 20705; i++)
                materials[i] = Material.DioriteWall;
            for (int i = 20706; i <= 20737; i++)
                materials[i] = Material.Scaffolding;
            for (int i = 20738; i <= 20741; i++)
                materials[i] = Material.Loom;
            for (int i = 20742; i <= 20753; i++)
                materials[i] = Material.Barrel;
            for (int i = 20754; i <= 20761; i++)
                materials[i] = Material.Smoker;
            for (int i = 20762; i <= 20769; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 20770; i <= 20770; i++)
                materials[i] = Material.CartographyTable;
            for (int i = 20771; i <= 20771; i++)
                materials[i] = Material.FletchingTable;
            for (int i = 20772; i <= 20783; i++)
                materials[i] = Material.Grindstone;
            for (int i = 20784; i <= 20799; i++)
                materials[i] = Material.Lectern;
            for (int i = 20800; i <= 20800; i++)
                materials[i] = Material.SmithingTable;
            for (int i = 20801; i <= 20804; i++)
                materials[i] = Material.Stonecutter;
            for (int i = 20805; i <= 20836; i++)
                materials[i] = Material.Bell;
            for (int i = 20837; i <= 20840; i++)
                materials[i] = Material.Lantern;
            for (int i = 20841; i <= 20844; i++)
                materials[i] = Material.SoulLantern;
            for (int i = 20845; i <= 20848; i++)
                materials[i] = Material.CopperLantern;
            for (int i = 20849; i <= 20852; i++)
                materials[i] = Material.ExposedCopperLantern;
            for (int i = 20853; i <= 20856; i++)
                materials[i] = Material.WeatheredCopperLantern;
            for (int i = 20857; i <= 20860; i++)
                materials[i] = Material.OxidizedCopperLantern;
            for (int i = 20861; i <= 20864; i++)
                materials[i] = Material.WaxedCopperLantern;
            for (int i = 20865; i <= 20868; i++)
                materials[i] = Material.WaxedExposedCopperLantern;
            for (int i = 20869; i <= 20872; i++)
                materials[i] = Material.WaxedWeatheredCopperLantern;
            for (int i = 20873; i <= 20876; i++)
                materials[i] = Material.WaxedOxidizedCopperLantern;
            for (int i = 20877; i <= 20908; i++)
                materials[i] = Material.Campfire;
            for (int i = 20909; i <= 20940; i++)
                materials[i] = Material.SoulCampfire;
            for (int i = 20941; i <= 20944; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 20945; i <= 20947; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 20948; i <= 20950; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 20951; i <= 20953; i++)
                materials[i] = Material.WarpedHyphae;
            for (int i = 20954; i <= 20956; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 20957; i <= 20957; i++)
                materials[i] = Material.WarpedNylium;
            for (int i = 20958; i <= 20958; i++)
                materials[i] = Material.WarpedFungus;
            for (int i = 20959; i <= 20959; i++)
                materials[i] = Material.WarpedWartBlock;
            for (int i = 20960; i <= 20960; i++)
                materials[i] = Material.WarpedRoots;
            for (int i = 20961; i <= 20961; i++)
                materials[i] = Material.NetherSprouts;
            for (int i = 20962; i <= 20964; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 20965; i <= 20967; i++)
                materials[i] = Material.StrippedCrimsonStem;
            for (int i = 20968; i <= 20970; i++)
                materials[i] = Material.CrimsonHyphae;
            for (int i = 20971; i <= 20973; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 20974; i <= 20974; i++)
                materials[i] = Material.CrimsonNylium;
            for (int i = 20975; i <= 20975; i++)
                materials[i] = Material.CrimsonFungus;
            for (int i = 20976; i <= 20976; i++)
                materials[i] = Material.Shroomlight;
            for (int i = 20977; i <= 21002; i++)
                materials[i] = Material.WeepingVines;
            for (int i = 21003; i <= 21003; i++)
                materials[i] = Material.WeepingVinesPlant;
            for (int i = 21004; i <= 21029; i++)
                materials[i] = Material.TwistingVines;
            for (int i = 21030; i <= 21030; i++)
                materials[i] = Material.TwistingVinesPlant;
            for (int i = 21031; i <= 21031; i++)
                materials[i] = Material.CrimsonRoots;
            for (int i = 21032; i <= 21032; i++)
                materials[i] = Material.CrimsonPlanks;
            for (int i = 21033; i <= 21033; i++)
                materials[i] = Material.WarpedPlanks;
            for (int i = 21034; i <= 21039; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 21040; i <= 21045; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 21046; i <= 21047; i++)
                materials[i] = Material.CrimsonPressurePlate;
            for (int i = 21048; i <= 21049; i++)
                materials[i] = Material.WarpedPressurePlate;
            for (int i = 21050; i <= 21081; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 21082; i <= 21113; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 21114; i <= 21177; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 21178; i <= 21241; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 21242; i <= 21273; i++)
                materials[i] = Material.CrimsonFenceGate;
            for (int i = 21274; i <= 21305; i++)
                materials[i] = Material.WarpedFenceGate;
            for (int i = 21306; i <= 21385; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 21386; i <= 21465; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 21466; i <= 21489; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 21490; i <= 21513; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 21514; i <= 21577; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 21578; i <= 21641; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 21642; i <= 21673; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 21674; i <= 21705; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 21706; i <= 21713; i++)
                materials[i] = Material.CrimsonWallSign;
            for (int i = 21714; i <= 21721; i++)
                materials[i] = Material.WarpedWallSign;
            for (int i = 21722; i <= 21725; i++)
                materials[i] = Material.StructureBlock;
            for (int i = 21726; i <= 21737; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 21738; i <= 21741; i++)
                materials[i] = Material.TestBlock;
            for (int i = 21742; i <= 21742; i++)
                materials[i] = Material.TestInstanceBlock;
            for (int i = 21743; i <= 21751; i++)
                materials[i] = Material.Composter;
            for (int i = 21752; i <= 21767; i++)
                materials[i] = Material.Target;
            for (int i = 21768; i <= 21791; i++)
                materials[i] = Material.BeeNest;
            for (int i = 21792; i <= 21815; i++)
                materials[i] = Material.Beehive;
            for (int i = 21816; i <= 21816; i++)
                materials[i] = Material.HoneyBlock;
            for (int i = 21817; i <= 21817; i++)
                materials[i] = Material.HoneycombBlock;
            for (int i = 21818; i <= 21818; i++)
                materials[i] = Material.NetheriteBlock;
            for (int i = 21819; i <= 21819; i++)
                materials[i] = Material.AncientDebris;
            for (int i = 21820; i <= 21820; i++)
                materials[i] = Material.CryingObsidian;
            for (int i = 21821; i <= 21825; i++)
                materials[i] = Material.RespawnAnchor;
            for (int i = 21826; i <= 21826; i++)
                materials[i] = Material.PottedCrimsonFungus;
            for (int i = 21827; i <= 21827; i++)
                materials[i] = Material.PottedWarpedFungus;
            for (int i = 21828; i <= 21828; i++)
                materials[i] = Material.PottedCrimsonRoots;
            for (int i = 21829; i <= 21829; i++)
                materials[i] = Material.PottedWarpedRoots;
            for (int i = 21830; i <= 21830; i++)
                materials[i] = Material.Lodestone;
            for (int i = 21831; i <= 21831; i++)
                materials[i] = Material.Blackstone;
            for (int i = 21832; i <= 21911; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 21912; i <= 22235; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 22236; i <= 22241; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 22242; i <= 22242; i++)
                materials[i] = Material.PolishedBlackstone;
            for (int i = 22243; i <= 22243; i++)
                materials[i] = Material.PolishedBlackstoneBricks;
            for (int i = 22244; i <= 22244; i++)
                materials[i] = Material.CrackedPolishedBlackstoneBricks;
            for (int i = 22245; i <= 22245; i++)
                materials[i] = Material.ChiseledPolishedBlackstone;
            for (int i = 22246; i <= 22251; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 22252; i <= 22331; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 22332; i <= 22655; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            for (int i = 22656; i <= 22656; i++)
                materials[i] = Material.GildedBlackstone;
            for (int i = 22657; i <= 22736; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 22737; i <= 22742; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 22743; i <= 22744; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 22745; i <= 22768; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 22769; i <= 23092; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            for (int i = 23093; i <= 23093; i++)
                materials[i] = Material.ChiseledNetherBricks;
            for (int i = 23094; i <= 23094; i++)
                materials[i] = Material.CrackedNetherBricks;
            for (int i = 23095; i <= 23095; i++)
                materials[i] = Material.QuartzBricks;
            for (int i = 23096; i <= 23111; i++)
                materials[i] = Material.Candle;
            for (int i = 23112; i <= 23127; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 23128; i <= 23143; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 23144; i <= 23159; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 23160; i <= 23175; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 23176; i <= 23191; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 23192; i <= 23207; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 23208; i <= 23223; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 23224; i <= 23239; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 23240; i <= 23255; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 23256; i <= 23271; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 23272; i <= 23287; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 23288; i <= 23303; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 23304; i <= 23319; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 23320; i <= 23335; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 23336; i <= 23351; i++)
                materials[i] = Material.RedCandle;
            for (int i = 23352; i <= 23367; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 23368; i <= 23369; i++)
                materials[i] = Material.CandleCake;
            for (int i = 23370; i <= 23371; i++)
                materials[i] = Material.WhiteCandleCake;
            for (int i = 23372; i <= 23373; i++)
                materials[i] = Material.OrangeCandleCake;
            for (int i = 23374; i <= 23375; i++)
                materials[i] = Material.MagentaCandleCake;
            for (int i = 23376; i <= 23377; i++)
                materials[i] = Material.LightBlueCandleCake;
            for (int i = 23378; i <= 23379; i++)
                materials[i] = Material.YellowCandleCake;
            for (int i = 23380; i <= 23381; i++)
                materials[i] = Material.LimeCandleCake;
            for (int i = 23382; i <= 23383; i++)
                materials[i] = Material.PinkCandleCake;
            for (int i = 23384; i <= 23385; i++)
                materials[i] = Material.GrayCandleCake;
            for (int i = 23386; i <= 23387; i++)
                materials[i] = Material.LightGrayCandleCake;
            for (int i = 23388; i <= 23389; i++)
                materials[i] = Material.CyanCandleCake;
            for (int i = 23390; i <= 23391; i++)
                materials[i] = Material.PurpleCandleCake;
            for (int i = 23392; i <= 23393; i++)
                materials[i] = Material.BlueCandleCake;
            for (int i = 23394; i <= 23395; i++)
                materials[i] = Material.BrownCandleCake;
            for (int i = 23396; i <= 23397; i++)
                materials[i] = Material.GreenCandleCake;
            for (int i = 23398; i <= 23399; i++)
                materials[i] = Material.RedCandleCake;
            for (int i = 23400; i <= 23401; i++)
                materials[i] = Material.BlackCandleCake;
            for (int i = 23402; i <= 23402; i++)
                materials[i] = Material.AmethystBlock;
            for (int i = 23403; i <= 23403; i++)
                materials[i] = Material.BuddingAmethyst;
            for (int i = 23404; i <= 23415; i++)
                materials[i] = Material.AmethystCluster;
            for (int i = 23416; i <= 23427; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 23428; i <= 23439; i++)
                materials[i] = Material.MediumAmethystBud;
            for (int i = 23440; i <= 23451; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 23452; i <= 23452; i++)
                materials[i] = Material.Tuff;
            for (int i = 23453; i <= 23458; i++)
                materials[i] = Material.TuffSlab;
            for (int i = 23459; i <= 23538; i++)
                materials[i] = Material.TuffStairs;
            for (int i = 23539; i <= 23862; i++)
                materials[i] = Material.TuffWall;
            for (int i = 23863; i <= 23863; i++)
                materials[i] = Material.PolishedTuff;
            for (int i = 23864; i <= 23869; i++)
                materials[i] = Material.PolishedTuffSlab;
            for (int i = 23870; i <= 23949; i++)
                materials[i] = Material.PolishedTuffStairs;
            for (int i = 23950; i <= 24273; i++)
                materials[i] = Material.PolishedTuffWall;
            for (int i = 24274; i <= 24274; i++)
                materials[i] = Material.ChiseledTuff;
            for (int i = 24275; i <= 24275; i++)
                materials[i] = Material.TuffBricks;
            for (int i = 24276; i <= 24281; i++)
                materials[i] = Material.TuffBrickSlab;
            for (int i = 24282; i <= 24361; i++)
                materials[i] = Material.TuffBrickStairs;
            for (int i = 24362; i <= 24685; i++)
                materials[i] = Material.TuffBrickWall;
            for (int i = 24686; i <= 24686; i++)
                materials[i] = Material.ChiseledTuffBricks;
            for (int i = 24687; i <= 24687; i++)
                materials[i] = Material.Calcite;
            for (int i = 24688; i <= 24688; i++)
                materials[i] = Material.TintedGlass;
            for (int i = 24689; i <= 24689; i++)
                materials[i] = Material.PowderSnow;
            for (int i = 24690; i <= 24785; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 24786; i <= 25169; i++)
                materials[i] = Material.CalibratedSculkSensor;
            for (int i = 25170; i <= 25170; i++)
                materials[i] = Material.Sculk;
            for (int i = 25171; i <= 25298; i++)
                materials[i] = Material.SculkVein;
            for (int i = 25299; i <= 25300; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 25301; i <= 25308; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 25309; i <= 25309; i++)
                materials[i] = Material.CopperBlock;
            for (int i = 25310; i <= 25310; i++)
                materials[i] = Material.ExposedCopper;
            for (int i = 25311; i <= 25311; i++)
                materials[i] = Material.WeatheredCopper;
            for (int i = 25312; i <= 25312; i++)
                materials[i] = Material.OxidizedCopper;
            for (int i = 25313; i <= 25313; i++)
                materials[i] = Material.CopperOre;
            for (int i = 25314; i <= 25314; i++)
                materials[i] = Material.DeepslateCopperOre;
            for (int i = 25315; i <= 25315; i++)
                materials[i] = Material.OxidizedCutCopper;
            for (int i = 25316; i <= 25316; i++)
                materials[i] = Material.WeatheredCutCopper;
            for (int i = 25317; i <= 25317; i++)
                materials[i] = Material.ExposedCutCopper;
            for (int i = 25318; i <= 25318; i++)
                materials[i] = Material.CutCopper;
            for (int i = 25319; i <= 25319; i++)
                materials[i] = Material.OxidizedChiseledCopper;
            for (int i = 25320; i <= 25320; i++)
                materials[i] = Material.WeatheredChiseledCopper;
            for (int i = 25321; i <= 25321; i++)
                materials[i] = Material.ExposedChiseledCopper;
            for (int i = 25322; i <= 25322; i++)
                materials[i] = Material.ChiseledCopper;
            for (int i = 25323; i <= 25323; i++)
                materials[i] = Material.WaxedOxidizedChiseledCopper;
            for (int i = 25324; i <= 25324; i++)
                materials[i] = Material.WaxedWeatheredChiseledCopper;
            for (int i = 25325; i <= 25325; i++)
                materials[i] = Material.WaxedExposedChiseledCopper;
            for (int i = 25326; i <= 25326; i++)
                materials[i] = Material.WaxedChiseledCopper;
            for (int i = 25327; i <= 25406; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            for (int i = 25407; i <= 25486; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 25487; i <= 25566; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 25567; i <= 25646; i++)
                materials[i] = Material.CutCopperStairs;
            for (int i = 25647; i <= 25652; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 25653; i <= 25658; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 25659; i <= 25664; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 25665; i <= 25670; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 25671; i <= 25671; i++)
                materials[i] = Material.WaxedCopperBlock;
            for (int i = 25672; i <= 25672; i++)
                materials[i] = Material.WaxedWeatheredCopper;
            for (int i = 25673; i <= 25673; i++)
                materials[i] = Material.WaxedExposedCopper;
            for (int i = 25674; i <= 25674; i++)
                materials[i] = Material.WaxedOxidizedCopper;
            for (int i = 25675; i <= 25675; i++)
                materials[i] = Material.WaxedOxidizedCutCopper;
            for (int i = 25676; i <= 25676; i++)
                materials[i] = Material.WaxedWeatheredCutCopper;
            for (int i = 25677; i <= 25677; i++)
                materials[i] = Material.WaxedExposedCutCopper;
            for (int i = 25678; i <= 25678; i++)
                materials[i] = Material.WaxedCutCopper;
            for (int i = 25679; i <= 25758; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            for (int i = 25759; i <= 25838; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            for (int i = 25839; i <= 25918; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            for (int i = 25919; i <= 25998; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            for (int i = 25999; i <= 26004; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 26005; i <= 26010; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 26011; i <= 26016; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 26017; i <= 26022; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 26023; i <= 26086; i++)
                materials[i] = Material.CopperDoor;
            for (int i = 26087; i <= 26150; i++)
                materials[i] = Material.ExposedCopperDoor;
            for (int i = 26151; i <= 26214; i++)
                materials[i] = Material.OxidizedCopperDoor;
            for (int i = 26215; i <= 26278; i++)
                materials[i] = Material.WeatheredCopperDoor;
            for (int i = 26279; i <= 26342; i++)
                materials[i] = Material.WaxedCopperDoor;
            for (int i = 26343; i <= 26406; i++)
                materials[i] = Material.WaxedExposedCopperDoor;
            for (int i = 26407; i <= 26470; i++)
                materials[i] = Material.WaxedOxidizedCopperDoor;
            for (int i = 26471; i <= 26534; i++)
                materials[i] = Material.WaxedWeatheredCopperDoor;
            for (int i = 26535; i <= 26598; i++)
                materials[i] = Material.CopperTrapdoor;
            for (int i = 26599; i <= 26662; i++)
                materials[i] = Material.ExposedCopperTrapdoor;
            for (int i = 26663; i <= 26726; i++)
                materials[i] = Material.OxidizedCopperTrapdoor;
            for (int i = 26727; i <= 26790; i++)
                materials[i] = Material.WeatheredCopperTrapdoor;
            for (int i = 26791; i <= 26854; i++)
                materials[i] = Material.WaxedCopperTrapdoor;
            for (int i = 26855; i <= 26918; i++)
                materials[i] = Material.WaxedExposedCopperTrapdoor;
            for (int i = 26919; i <= 26982; i++)
                materials[i] = Material.WaxedOxidizedCopperTrapdoor;
            for (int i = 26983; i <= 27046; i++)
                materials[i] = Material.WaxedWeatheredCopperTrapdoor;
            for (int i = 27047; i <= 27048; i++)
                materials[i] = Material.CopperGrate;
            for (int i = 27049; i <= 27050; i++)
                materials[i] = Material.ExposedCopperGrate;
            for (int i = 27051; i <= 27052; i++)
                materials[i] = Material.WeatheredCopperGrate;
            for (int i = 27053; i <= 27054; i++)
                materials[i] = Material.OxidizedCopperGrate;
            for (int i = 27055; i <= 27056; i++)
                materials[i] = Material.WaxedCopperGrate;
            for (int i = 27057; i <= 27058; i++)
                materials[i] = Material.WaxedExposedCopperGrate;
            for (int i = 27059; i <= 27060; i++)
                materials[i] = Material.WaxedWeatheredCopperGrate;
            for (int i = 27061; i <= 27062; i++)
                materials[i] = Material.WaxedOxidizedCopperGrate;
            for (int i = 27063; i <= 27066; i++)
                materials[i] = Material.CopperBulb;
            for (int i = 27067; i <= 27070; i++)
                materials[i] = Material.ExposedCopperBulb;
            for (int i = 27071; i <= 27074; i++)
                materials[i] = Material.WeatheredCopperBulb;
            for (int i = 27075; i <= 27078; i++)
                materials[i] = Material.OxidizedCopperBulb;
            for (int i = 27079; i <= 27082; i++)
                materials[i] = Material.WaxedCopperBulb;
            for (int i = 27083; i <= 27086; i++)
                materials[i] = Material.WaxedExposedCopperBulb;
            for (int i = 27087; i <= 27090; i++)
                materials[i] = Material.WaxedWeatheredCopperBulb;
            for (int i = 27091; i <= 27094; i++)
                materials[i] = Material.WaxedOxidizedCopperBulb;
            for (int i = 27095; i <= 27118; i++)
                materials[i] = Material.CopperChest;
            for (int i = 27119; i <= 27142; i++)
                materials[i] = Material.ExposedCopperChest;
            for (int i = 27143; i <= 27166; i++)
                materials[i] = Material.WeatheredCopperChest;
            for (int i = 27167; i <= 27190; i++)
                materials[i] = Material.OxidizedCopperChest;
            for (int i = 27191; i <= 27214; i++)
                materials[i] = Material.WaxedCopperChest;
            for (int i = 27215; i <= 27238; i++)
                materials[i] = Material.WaxedExposedCopperChest;
            for (int i = 27239; i <= 27262; i++)
                materials[i] = Material.WaxedWeatheredCopperChest;
            for (int i = 27263; i <= 27286; i++)
                materials[i] = Material.WaxedOxidizedCopperChest;
            for (int i = 27287; i <= 27318; i++)
                materials[i] = Material.CopperGolemStatue;
            for (int i = 27319; i <= 27350; i++)
                materials[i] = Material.ExposedCopperGolemStatue;
            for (int i = 27351; i <= 27382; i++)
                materials[i] = Material.WeatheredCopperGolemStatue;
            for (int i = 27383; i <= 27414; i++)
                materials[i] = Material.OxidizedCopperGolemStatue;
            for (int i = 27415; i <= 27446; i++)
                materials[i] = Material.WaxedCopperGolemStatue;
            for (int i = 27447; i <= 27478; i++)
                materials[i] = Material.WaxedExposedCopperGolemStatue;
            for (int i = 27479; i <= 27510; i++)
                materials[i] = Material.WaxedWeatheredCopperGolemStatue;
            for (int i = 27511; i <= 27542; i++)
                materials[i] = Material.WaxedOxidizedCopperGolemStatue;
            for (int i = 27543; i <= 27566; i++)
                materials[i] = Material.LightningRod;
            for (int i = 27567; i <= 27590; i++)
                materials[i] = Material.ExposedLightningRod;
            for (int i = 27591; i <= 27614; i++)
                materials[i] = Material.WeatheredLightningRod;
            for (int i = 27615; i <= 27638; i++)
                materials[i] = Material.OxidizedLightningRod;
            for (int i = 27639; i <= 27662; i++)
                materials[i] = Material.WaxedLightningRod;
            for (int i = 27663; i <= 27686; i++)
                materials[i] = Material.WaxedExposedLightningRod;
            for (int i = 27687; i <= 27710; i++)
                materials[i] = Material.WaxedWeatheredLightningRod;
            for (int i = 27711; i <= 27734; i++)
                materials[i] = Material.WaxedOxidizedLightningRod;
            for (int i = 27735; i <= 27754; i++)
                materials[i] = Material.PointedDripstone;
            for (int i = 27755; i <= 27755; i++)
                materials[i] = Material.DripstoneBlock;
            for (int i = 27756; i <= 27807; i++)
                materials[i] = Material.CaveVines;
            for (int i = 27808; i <= 27809; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 27810; i <= 27810; i++)
                materials[i] = Material.SporeBlossom;
            for (int i = 27811; i <= 27811; i++)
                materials[i] = Material.Azalea;
            for (int i = 27812; i <= 27812; i++)
                materials[i] = Material.FloweringAzalea;
            for (int i = 27813; i <= 27813; i++)
                materials[i] = Material.MossCarpet;
            for (int i = 27814; i <= 27829; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 27830; i <= 27845; i++)
                materials[i] = Material.Wildflowers;
            for (int i = 27846; i <= 27861; i++)
                materials[i] = Material.LeafLitter;
            for (int i = 27862; i <= 27862; i++)
                materials[i] = Material.MossBlock;
            for (int i = 27863; i <= 27894; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 27895; i <= 27902; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 27903; i <= 27918; i++)
                materials[i] = Material.SmallDripleaf;
            for (int i = 27919; i <= 27920; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 27921; i <= 27921; i++)
                materials[i] = Material.RootedDirt;
            for (int i = 27922; i <= 27922; i++)
                materials[i] = Material.Mud;
            for (int i = 27923; i <= 27925; i++)
                materials[i] = Material.Deepslate;
            for (int i = 27926; i <= 27926; i++)
                materials[i] = Material.CobbledDeepslate;
            for (int i = 27927; i <= 28006; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 28007; i <= 28012; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 28013; i <= 28336; i++)
                materials[i] = Material.CobbledDeepslateWall;
            for (int i = 28337; i <= 28337; i++)
                materials[i] = Material.PolishedDeepslate;
            for (int i = 28338; i <= 28417; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 28418; i <= 28423; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 28424; i <= 28747; i++)
                materials[i] = Material.PolishedDeepslateWall;
            for (int i = 28748; i <= 28748; i++)
                materials[i] = Material.DeepslateTiles;
            for (int i = 28749; i <= 28828; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 28829; i <= 28834; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 28835; i <= 29158; i++)
                materials[i] = Material.DeepslateTileWall;
            for (int i = 29159; i <= 29159; i++)
                materials[i] = Material.DeepslateBricks;
            for (int i = 29160; i <= 29239; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 29240; i <= 29245; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 29246; i <= 29569; i++)
                materials[i] = Material.DeepslateBrickWall;
            for (int i = 29570; i <= 29570; i++)
                materials[i] = Material.ChiseledDeepslate;
            for (int i = 29571; i <= 29571; i++)
                materials[i] = Material.CrackedDeepslateBricks;
            for (int i = 29572; i <= 29572; i++)
                materials[i] = Material.CrackedDeepslateTiles;
            for (int i = 29573; i <= 29575; i++)
                materials[i] = Material.InfestedDeepslate;
            for (int i = 29576; i <= 29576; i++)
                materials[i] = Material.SmoothBasalt;
            for (int i = 29577; i <= 29577; i++)
                materials[i] = Material.RawIronBlock;
            for (int i = 29578; i <= 29578; i++)
                materials[i] = Material.RawCopperBlock;
            for (int i = 29579; i <= 29579; i++)
                materials[i] = Material.RawGoldBlock;
            for (int i = 29580; i <= 29580; i++)
                materials[i] = Material.PottedAzaleaBush;
            for (int i = 29581; i <= 29581; i++)
                materials[i] = Material.PottedFloweringAzaleaBush;
            for (int i = 29582; i <= 29584; i++)
                materials[i] = Material.OchreFroglight;
            for (int i = 29585; i <= 29587; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 29588; i <= 29590; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 29591; i <= 29591; i++)
                materials[i] = Material.Frogspawn;
            for (int i = 29592; i <= 29592; i++)
                materials[i] = Material.ReinforcedDeepslate;
            for (int i = 29593; i <= 29608; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 29609; i <= 29656; i++)
                materials[i] = Material.Crafter;
            for (int i = 29657; i <= 29668; i++)
                materials[i] = Material.TrialSpawner;
            for (int i = 29669; i <= 29700; i++)
                materials[i] = Material.Vault;
            for (int i = 29701; i <= 29702; i++)
                materials[i] = Material.HeavyCore;
            for (int i = 29703; i <= 29703; i++)
                materials[i] = Material.PaleMossBlock;
            for (int i = 29704; i <= 29865; i++)
                materials[i] = Material.PaleMossCarpet;
            for (int i = 29866; i <= 29867; i++)
                materials[i] = Material.PaleHangingMoss;
            for (int i = 29868; i <= 29868; i++)
                materials[i] = Material.OpenEyeblossom;
            for (int i = 29869; i <= 29869; i++)
                materials[i] = Material.ClosedEyeblossom;
            for (int i = 29870; i <= 29870; i++)
                materials[i] = Material.PottedOpenEyeblossom;
            for (int i = 29871; i <= 29871; i++)
                materials[i] = Material.PottedClosedEyeblossom;
            for (int i = 29872; i <= 29872; i++)
                materials[i] = Material.FireflyBush;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
