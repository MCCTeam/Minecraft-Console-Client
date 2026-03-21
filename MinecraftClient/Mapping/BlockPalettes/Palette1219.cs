using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette1219 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette1219()
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
            for (int i = 581; i <= 1730; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 1731; i <= 1746; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 1747; i <= 1762; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 1763; i <= 1778; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 1779; i <= 1794; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 1795; i <= 1810; i++)
                materials[i] = Material.YellowBed;
            for (int i = 1811; i <= 1826; i++)
                materials[i] = Material.LimeBed;
            for (int i = 1827; i <= 1842; i++)
                materials[i] = Material.PinkBed;
            for (int i = 1843; i <= 1858; i++)
                materials[i] = Material.GrayBed;
            for (int i = 1859; i <= 1874; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 1875; i <= 1890; i++)
                materials[i] = Material.CyanBed;
            for (int i = 1891; i <= 1906; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 1907; i <= 1922; i++)
                materials[i] = Material.BlueBed;
            for (int i = 1923; i <= 1938; i++)
                materials[i] = Material.BrownBed;
            for (int i = 1939; i <= 1954; i++)
                materials[i] = Material.GreenBed;
            for (int i = 1955; i <= 1970; i++)
                materials[i] = Material.RedBed;
            for (int i = 1971; i <= 1986; i++)
                materials[i] = Material.BlackBed;
            for (int i = 1987; i <= 2010; i++)
                materials[i] = Material.PoweredRail;
            for (int i = 2011; i <= 2034; i++)
                materials[i] = Material.DetectorRail;
            for (int i = 2035; i <= 2046; i++)
                materials[i] = Material.StickyPiston;
            for (int i = 2047; i <= 2047; i++)
                materials[i] = Material.Cobweb;
            for (int i = 2048; i <= 2048; i++)
                materials[i] = Material.ShortGrass;
            for (int i = 2049; i <= 2049; i++)
                materials[i] = Material.Fern;
            for (int i = 2050; i <= 2050; i++)
                materials[i] = Material.DeadBush;
            for (int i = 2051; i <= 2051; i++)
                materials[i] = Material.Bush;
            for (int i = 2052; i <= 2052; i++)
                materials[i] = Material.ShortDryGrass;
            for (int i = 2053; i <= 2053; i++)
                materials[i] = Material.TallDryGrass;
            for (int i = 2054; i <= 2054; i++)
                materials[i] = Material.Seagrass;
            for (int i = 2055; i <= 2056; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 2057; i <= 2068; i++)
                materials[i] = Material.Piston;
            for (int i = 2069; i <= 2092; i++)
                materials[i] = Material.PistonHead;
            for (int i = 2093; i <= 2093; i++)
                materials[i] = Material.WhiteWool;
            for (int i = 2094; i <= 2094; i++)
                materials[i] = Material.OrangeWool;
            for (int i = 2095; i <= 2095; i++)
                materials[i] = Material.MagentaWool;
            for (int i = 2096; i <= 2096; i++)
                materials[i] = Material.LightBlueWool;
            for (int i = 2097; i <= 2097; i++)
                materials[i] = Material.YellowWool;
            for (int i = 2098; i <= 2098; i++)
                materials[i] = Material.LimeWool;
            for (int i = 2099; i <= 2099; i++)
                materials[i] = Material.PinkWool;
            for (int i = 2100; i <= 2100; i++)
                materials[i] = Material.GrayWool;
            for (int i = 2101; i <= 2101; i++)
                materials[i] = Material.LightGrayWool;
            for (int i = 2102; i <= 2102; i++)
                materials[i] = Material.CyanWool;
            for (int i = 2103; i <= 2103; i++)
                materials[i] = Material.PurpleWool;
            for (int i = 2104; i <= 2104; i++)
                materials[i] = Material.BlueWool;
            for (int i = 2105; i <= 2105; i++)
                materials[i] = Material.BrownWool;
            for (int i = 2106; i <= 2106; i++)
                materials[i] = Material.GreenWool;
            for (int i = 2107; i <= 2107; i++)
                materials[i] = Material.RedWool;
            for (int i = 2108; i <= 2108; i++)
                materials[i] = Material.BlackWool;
            for (int i = 2109; i <= 2120; i++)
                materials[i] = Material.MovingPiston;
            for (int i = 2121; i <= 2121; i++)
                materials[i] = Material.Dandelion;
            for (int i = 2122; i <= 2122; i++)
                materials[i] = Material.Torchflower;
            for (int i = 2123; i <= 2123; i++)
                materials[i] = Material.Poppy;
            for (int i = 2124; i <= 2124; i++)
                materials[i] = Material.BlueOrchid;
            for (int i = 2125; i <= 2125; i++)
                materials[i] = Material.Allium;
            for (int i = 2126; i <= 2126; i++)
                materials[i] = Material.AzureBluet;
            for (int i = 2127; i <= 2127; i++)
                materials[i] = Material.RedTulip;
            for (int i = 2128; i <= 2128; i++)
                materials[i] = Material.OrangeTulip;
            for (int i = 2129; i <= 2129; i++)
                materials[i] = Material.WhiteTulip;
            for (int i = 2130; i <= 2130; i++)
                materials[i] = Material.PinkTulip;
            for (int i = 2131; i <= 2131; i++)
                materials[i] = Material.OxeyeDaisy;
            for (int i = 2132; i <= 2132; i++)
                materials[i] = Material.Cornflower;
            for (int i = 2133; i <= 2133; i++)
                materials[i] = Material.WitherRose;
            for (int i = 2134; i <= 2134; i++)
                materials[i] = Material.LilyOfTheValley;
            for (int i = 2135; i <= 2135; i++)
                materials[i] = Material.BrownMushroom;
            for (int i = 2136; i <= 2136; i++)
                materials[i] = Material.RedMushroom;
            for (int i = 2137; i <= 2137; i++)
                materials[i] = Material.GoldBlock;
            for (int i = 2138; i <= 2138; i++)
                materials[i] = Material.IronBlock;
            for (int i = 2139; i <= 2139; i++)
                materials[i] = Material.Bricks;
            for (int i = 2140; i <= 2141; i++)
                materials[i] = Material.Tnt;
            for (int i = 2142; i <= 2142; i++)
                materials[i] = Material.Bookshelf;
            for (int i = 2143; i <= 2398; i++)
                materials[i] = Material.ChiseledBookshelf;
            for (int i = 2399; i <= 2462; i++)
                materials[i] = Material.AcaciaShelf;
            for (int i = 2463; i <= 2526; i++)
                materials[i] = Material.BambooShelf;
            for (int i = 2527; i <= 2590; i++)
                materials[i] = Material.BirchShelf;
            for (int i = 2591; i <= 2654; i++)
                materials[i] = Material.CherryShelf;
            for (int i = 2655; i <= 2718; i++)
                materials[i] = Material.CrimsonShelf;
            for (int i = 2719; i <= 2782; i++)
                materials[i] = Material.DarkOakShelf;
            for (int i = 2783; i <= 2846; i++)
                materials[i] = Material.JungleShelf;
            for (int i = 2847; i <= 2910; i++)
                materials[i] = Material.MangroveShelf;
            for (int i = 2911; i <= 2974; i++)
                materials[i] = Material.OakShelf;
            for (int i = 2975; i <= 3038; i++)
                materials[i] = Material.PaleOakShelf;
            for (int i = 3039; i <= 3102; i++)
                materials[i] = Material.SpruceShelf;
            for (int i = 3103; i <= 3166; i++)
                materials[i] = Material.WarpedShelf;
            for (int i = 3167; i <= 3167; i++)
                materials[i] = Material.MossyCobblestone;
            for (int i = 3168; i <= 3168; i++)
                materials[i] = Material.Obsidian;
            for (int i = 3169; i <= 3169; i++)
                materials[i] = Material.Torch;
            for (int i = 3170; i <= 3173; i++)
                materials[i] = Material.WallTorch;
            for (int i = 3174; i <= 3685; i++)
                materials[i] = Material.Fire;
            for (int i = 3686; i <= 3686; i++)
                materials[i] = Material.SoulFire;
            for (int i = 3687; i <= 3687; i++)
                materials[i] = Material.Spawner;
            for (int i = 3688; i <= 3705; i++)
                materials[i] = Material.CreakingHeart;
            for (int i = 3706; i <= 3785; i++)
                materials[i] = Material.OakStairs;
            for (int i = 3786; i <= 3809; i++)
                materials[i] = Material.Chest;
            for (int i = 3810; i <= 5105; i++)
                materials[i] = Material.RedstoneWire;
            for (int i = 5106; i <= 5106; i++)
                materials[i] = Material.DiamondOre;
            for (int i = 5107; i <= 5107; i++)
                materials[i] = Material.DeepslateDiamondOre;
            for (int i = 5108; i <= 5108; i++)
                materials[i] = Material.DiamondBlock;
            for (int i = 5109; i <= 5109; i++)
                materials[i] = Material.CraftingTable;
            for (int i = 5110; i <= 5117; i++)
                materials[i] = Material.Wheat;
            for (int i = 5118; i <= 5125; i++)
                materials[i] = Material.Farmland;
            for (int i = 5126; i <= 5133; i++)
                materials[i] = Material.Furnace;
            for (int i = 5134; i <= 5165; i++)
                materials[i] = Material.OakSign;
            for (int i = 5166; i <= 5197; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 5198; i <= 5229; i++)
                materials[i] = Material.BirchSign;
            for (int i = 5230; i <= 5261; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 5262; i <= 5293; i++)
                materials[i] = Material.CherrySign;
            for (int i = 5294; i <= 5325; i++)
                materials[i] = Material.JungleSign;
            for (int i = 5326; i <= 5357; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 5358; i <= 5389; i++)
                materials[i] = Material.PaleOakSign;
            for (int i = 5390; i <= 5421; i++)
                materials[i] = Material.MangroveSign;
            for (int i = 5422; i <= 5453; i++)
                materials[i] = Material.BambooSign;
            for (int i = 5454; i <= 5517; i++)
                materials[i] = Material.OakDoor;
            for (int i = 5518; i <= 5525; i++)
                materials[i] = Material.Ladder;
            for (int i = 5526; i <= 5545; i++)
                materials[i] = Material.Rail;
            for (int i = 5546; i <= 5625; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 5626; i <= 5633; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 5634; i <= 5641; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 5642; i <= 5649; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 5650; i <= 5657; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 5658; i <= 5665; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 5666; i <= 5673; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 5674; i <= 5681; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 5682; i <= 5689; i++)
                materials[i] = Material.PaleOakWallSign;
            for (int i = 5690; i <= 5697; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 5698; i <= 5705; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 5706; i <= 5769; i++)
                materials[i] = Material.OakHangingSign;
            for (int i = 5770; i <= 5833; i++)
                materials[i] = Material.SpruceHangingSign;
            for (int i = 5834; i <= 5897; i++)
                materials[i] = Material.BirchHangingSign;
            for (int i = 5898; i <= 5961; i++)
                materials[i] = Material.AcaciaHangingSign;
            for (int i = 5962; i <= 6025; i++)
                materials[i] = Material.CherryHangingSign;
            for (int i = 6026; i <= 6089; i++)
                materials[i] = Material.JungleHangingSign;
            for (int i = 6090; i <= 6153; i++)
                materials[i] = Material.DarkOakHangingSign;
            for (int i = 6154; i <= 6217; i++)
                materials[i] = Material.PaleOakHangingSign;
            for (int i = 6218; i <= 6281; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 6282; i <= 6345; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 6346; i <= 6409; i++)
                materials[i] = Material.MangroveHangingSign;
            for (int i = 6410; i <= 6473; i++)
                materials[i] = Material.BambooHangingSign;
            for (int i = 6474; i <= 6481; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 6482; i <= 6489; i++)
                materials[i] = Material.SpruceWallHangingSign;
            for (int i = 6490; i <= 6497; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 6498; i <= 6505; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 6506; i <= 6513; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 6514; i <= 6521; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 6522; i <= 6529; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 6530; i <= 6537; i++)
                materials[i] = Material.PaleOakWallHangingSign;
            for (int i = 6538; i <= 6545; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 6546; i <= 6553; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 6554; i <= 6561; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 6562; i <= 6569; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 6570; i <= 6593; i++)
                materials[i] = Material.Lever;
            for (int i = 6594; i <= 6595; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 6596; i <= 6659; i++)
                materials[i] = Material.IronDoor;
            for (int i = 6660; i <= 6661; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 6662; i <= 6663; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 6664; i <= 6665; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 6666; i <= 6667; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 6668; i <= 6669; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 6670; i <= 6671; i++)
                materials[i] = Material.CherryPressurePlate;
            for (int i = 6672; i <= 6673; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 6674; i <= 6675; i++)
                materials[i] = Material.PaleOakPressurePlate;
            for (int i = 6676; i <= 6677; i++)
                materials[i] = Material.MangrovePressurePlate;
            for (int i = 6678; i <= 6679; i++)
                materials[i] = Material.BambooPressurePlate;
            for (int i = 6680; i <= 6681; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 6682; i <= 6683; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 6684; i <= 6685; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 6686; i <= 6693; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 6694; i <= 6717; i++)
                materials[i] = Material.StoneButton;
            for (int i = 6718; i <= 6725; i++)
                materials[i] = Material.Snow;
            for (int i = 6726; i <= 6726; i++)
                materials[i] = Material.Ice;
            for (int i = 6727; i <= 6727; i++)
                materials[i] = Material.SnowBlock;
            for (int i = 6728; i <= 6743; i++)
                materials[i] = Material.Cactus;
            for (int i = 6744; i <= 6744; i++)
                materials[i] = Material.CactusFlower;
            for (int i = 6745; i <= 6745; i++)
                materials[i] = Material.Clay;
            for (int i = 6746; i <= 6761; i++)
                materials[i] = Material.SugarCane;
            for (int i = 6762; i <= 6763; i++)
                materials[i] = Material.Jukebox;
            for (int i = 6764; i <= 6795; i++)
                materials[i] = Material.OakFence;
            for (int i = 6796; i <= 6796; i++)
                materials[i] = Material.Netherrack;
            for (int i = 6797; i <= 6797; i++)
                materials[i] = Material.SoulSand;
            for (int i = 6798; i <= 6798; i++)
                materials[i] = Material.SoulSoil;
            for (int i = 6799; i <= 6801; i++)
                materials[i] = Material.Basalt;
            for (int i = 6802; i <= 6804; i++)
                materials[i] = Material.PolishedBasalt;
            for (int i = 6805; i <= 6805; i++)
                materials[i] = Material.SoulTorch;
            for (int i = 6806; i <= 6809; i++)
                materials[i] = Material.SoulWallTorch;
            for (int i = 6810; i <= 6810; i++)
                materials[i] = Material.CopperTorch;
            for (int i = 6811; i <= 6814; i++)
                materials[i] = Material.CopperWallTorch;
            for (int i = 6815; i <= 6815; i++)
                materials[i] = Material.Glowstone;
            for (int i = 6816; i <= 6817; i++)
                materials[i] = Material.NetherPortal;
            for (int i = 6818; i <= 6821; i++)
                materials[i] = Material.CarvedPumpkin;
            for (int i = 6822; i <= 6825; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 6826; i <= 6832; i++)
                materials[i] = Material.Cake;
            for (int i = 6833; i <= 6896; i++)
                materials[i] = Material.Repeater;
            for (int i = 6897; i <= 6897; i++)
                materials[i] = Material.WhiteStainedGlass;
            for (int i = 6898; i <= 6898; i++)
                materials[i] = Material.OrangeStainedGlass;
            for (int i = 6899; i <= 6899; i++)
                materials[i] = Material.MagentaStainedGlass;
            for (int i = 6900; i <= 6900; i++)
                materials[i] = Material.LightBlueStainedGlass;
            for (int i = 6901; i <= 6901; i++)
                materials[i] = Material.YellowStainedGlass;
            for (int i = 6902; i <= 6902; i++)
                materials[i] = Material.LimeStainedGlass;
            for (int i = 6903; i <= 6903; i++)
                materials[i] = Material.PinkStainedGlass;
            for (int i = 6904; i <= 6904; i++)
                materials[i] = Material.GrayStainedGlass;
            for (int i = 6905; i <= 6905; i++)
                materials[i] = Material.LightGrayStainedGlass;
            for (int i = 6906; i <= 6906; i++)
                materials[i] = Material.CyanStainedGlass;
            for (int i = 6907; i <= 6907; i++)
                materials[i] = Material.PurpleStainedGlass;
            for (int i = 6908; i <= 6908; i++)
                materials[i] = Material.BlueStainedGlass;
            for (int i = 6909; i <= 6909; i++)
                materials[i] = Material.BrownStainedGlass;
            for (int i = 6910; i <= 6910; i++)
                materials[i] = Material.GreenStainedGlass;
            for (int i = 6911; i <= 6911; i++)
                materials[i] = Material.RedStainedGlass;
            for (int i = 6912; i <= 6912; i++)
                materials[i] = Material.BlackStainedGlass;
            for (int i = 6913; i <= 6976; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 6977; i <= 7040; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 7041; i <= 7104; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 7105; i <= 7168; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 7169; i <= 7232; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 7233; i <= 7296; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 7297; i <= 7360; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 7361; i <= 7424; i++)
                materials[i] = Material.PaleOakTrapdoor;
            for (int i = 7425; i <= 7488; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 7489; i <= 7552; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 7553; i <= 7553; i++)
                materials[i] = Material.StoneBricks;
            for (int i = 7554; i <= 7554; i++)
                materials[i] = Material.MossyStoneBricks;
            for (int i = 7555; i <= 7555; i++)
                materials[i] = Material.CrackedStoneBricks;
            for (int i = 7556; i <= 7556; i++)
                materials[i] = Material.ChiseledStoneBricks;
            for (int i = 7557; i <= 7557; i++)
                materials[i] = Material.PackedMud;
            for (int i = 7558; i <= 7558; i++)
                materials[i] = Material.MudBricks;
            for (int i = 7559; i <= 7559; i++)
                materials[i] = Material.InfestedStone;
            for (int i = 7560; i <= 7560; i++)
                materials[i] = Material.InfestedCobblestone;
            for (int i = 7561; i <= 7561; i++)
                materials[i] = Material.InfestedStoneBricks;
            for (int i = 7562; i <= 7562; i++)
                materials[i] = Material.InfestedMossyStoneBricks;
            for (int i = 7563; i <= 7563; i++)
                materials[i] = Material.InfestedCrackedStoneBricks;
            for (int i = 7564; i <= 7564; i++)
                materials[i] = Material.InfestedChiseledStoneBricks;
            for (int i = 7565; i <= 7628; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 7629; i <= 7692; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 7693; i <= 7756; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 7757; i <= 7788; i++)
                materials[i] = Material.IronBars;
            for (int i = 7789; i <= 7820; i++)
                materials[i] = Material.CopperBars;
            for (int i = 7821; i <= 7852; i++)
                materials[i] = Material.ExposedCopperBars;
            for (int i = 7853; i <= 7884; i++)
                materials[i] = Material.WeatheredCopperBars;
            for (int i = 7885; i <= 7916; i++)
                materials[i] = Material.OxidizedCopperBars;
            for (int i = 7917; i <= 7948; i++)
                materials[i] = Material.WaxedCopperBars;
            for (int i = 7949; i <= 7980; i++)
                materials[i] = Material.WaxedExposedCopperBars;
            for (int i = 7981; i <= 8012; i++)
                materials[i] = Material.WaxedWeatheredCopperBars;
            for (int i = 8013; i <= 8044; i++)
                materials[i] = Material.WaxedOxidizedCopperBars;
            for (int i = 8045; i <= 8050; i++)
                materials[i] = Material.IronChain;
            for (int i = 8051; i <= 8056; i++)
                materials[i] = Material.CopperChain;
            for (int i = 8057; i <= 8062; i++)
                materials[i] = Material.ExposedCopperChain;
            for (int i = 8063; i <= 8068; i++)
                materials[i] = Material.WeatheredCopperChain;
            for (int i = 8069; i <= 8074; i++)
                materials[i] = Material.OxidizedCopperChain;
            for (int i = 8075; i <= 8080; i++)
                materials[i] = Material.WaxedCopperChain;
            for (int i = 8081; i <= 8086; i++)
                materials[i] = Material.WaxedExposedCopperChain;
            for (int i = 8087; i <= 8092; i++)
                materials[i] = Material.WaxedWeatheredCopperChain;
            for (int i = 8093; i <= 8098; i++)
                materials[i] = Material.WaxedOxidizedCopperChain;
            for (int i = 8099; i <= 8130; i++)
                materials[i] = Material.GlassPane;
            for (int i = 8131; i <= 8131; i++)
                materials[i] = Material.Pumpkin;
            for (int i = 8132; i <= 8132; i++)
                materials[i] = Material.Melon;
            for (int i = 8133; i <= 8136; i++)
                materials[i] = Material.AttachedPumpkinStem;
            for (int i = 8137; i <= 8140; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 8141; i <= 8148; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 8149; i <= 8156; i++)
                materials[i] = Material.MelonStem;
            for (int i = 8157; i <= 8188; i++)
                materials[i] = Material.Vine;
            for (int i = 8189; i <= 8316; i++)
                materials[i] = Material.GlowLichen;
            for (int i = 8317; i <= 8444; i++)
                materials[i] = Material.ResinClump;
            for (int i = 8445; i <= 8476; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 8477; i <= 8556; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 8557; i <= 8636; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 8637; i <= 8716; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 8717; i <= 8718; i++)
                materials[i] = Material.Mycelium;
            for (int i = 8719; i <= 8719; i++)
                materials[i] = Material.LilyPad;
            for (int i = 8720; i <= 8720; i++)
                materials[i] = Material.ResinBlock;
            for (int i = 8721; i <= 8721; i++)
                materials[i] = Material.ResinBricks;
            for (int i = 8722; i <= 8801; i++)
                materials[i] = Material.ResinBrickStairs;
            for (int i = 8802; i <= 8807; i++)
                materials[i] = Material.ResinBrickSlab;
            for (int i = 8808; i <= 9131; i++)
                materials[i] = Material.ResinBrickWall;
            for (int i = 9132; i <= 9132; i++)
                materials[i] = Material.ChiseledResinBricks;
            for (int i = 9133; i <= 9133; i++)
                materials[i] = Material.NetherBricks;
            for (int i = 9134; i <= 9165; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 9166; i <= 9245; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 9246; i <= 9249; i++)
                materials[i] = Material.NetherWart;
            for (int i = 9250; i <= 9250; i++)
                materials[i] = Material.EnchantingTable;
            for (int i = 9251; i <= 9258; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 9259; i <= 9259; i++)
                materials[i] = Material.Cauldron;
            for (int i = 9260; i <= 9262; i++)
                materials[i] = Material.WaterCauldron;
            for (int i = 9263; i <= 9263; i++)
                materials[i] = Material.LavaCauldron;
            for (int i = 9264; i <= 9266; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 9267; i <= 9267; i++)
                materials[i] = Material.EndPortal;
            for (int i = 9268; i <= 9275; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 9276; i <= 9276; i++)
                materials[i] = Material.EndStone;
            for (int i = 9277; i <= 9277; i++)
                materials[i] = Material.DragonEgg;
            for (int i = 9278; i <= 9279; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 9280; i <= 9291; i++)
                materials[i] = Material.Cocoa;
            for (int i = 9292; i <= 9371; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 9372; i <= 9372; i++)
                materials[i] = Material.EmeraldOre;
            for (int i = 9373; i <= 9373; i++)
                materials[i] = Material.DeepslateEmeraldOre;
            for (int i = 9374; i <= 9381; i++)
                materials[i] = Material.EnderChest;
            for (int i = 9382; i <= 9397; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 9398; i <= 9525; i++)
                materials[i] = Material.Tripwire;
            for (int i = 9526; i <= 9526; i++)
                materials[i] = Material.EmeraldBlock;
            for (int i = 9527; i <= 9606; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 9607; i <= 9686; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 9687; i <= 9766; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 9767; i <= 9778; i++)
                materials[i] = Material.CommandBlock;
            for (int i = 9779; i <= 9779; i++)
                materials[i] = Material.Beacon;
            for (int i = 9780; i <= 10103; i++)
                materials[i] = Material.CobblestoneWall;
            for (int i = 10104; i <= 10427; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 10428; i <= 10428; i++)
                materials[i] = Material.FlowerPot;
            for (int i = 10429; i <= 10429; i++)
                materials[i] = Material.PottedTorchflower;
            for (int i = 10430; i <= 10430; i++)
                materials[i] = Material.PottedOakSapling;
            for (int i = 10431; i <= 10431; i++)
                materials[i] = Material.PottedSpruceSapling;
            for (int i = 10432; i <= 10432; i++)
                materials[i] = Material.PottedBirchSapling;
            for (int i = 10433; i <= 10433; i++)
                materials[i] = Material.PottedJungleSapling;
            for (int i = 10434; i <= 10434; i++)
                materials[i] = Material.PottedAcaciaSapling;
            for (int i = 10435; i <= 10435; i++)
                materials[i] = Material.PottedCherrySapling;
            for (int i = 10436; i <= 10436; i++)
                materials[i] = Material.PottedDarkOakSapling;
            for (int i = 10437; i <= 10437; i++)
                materials[i] = Material.PottedPaleOakSapling;
            for (int i = 10438; i <= 10438; i++)
                materials[i] = Material.PottedMangrovePropagule;
            for (int i = 10439; i <= 10439; i++)
                materials[i] = Material.PottedFern;
            for (int i = 10440; i <= 10440; i++)
                materials[i] = Material.PottedDandelion;
            for (int i = 10441; i <= 10441; i++)
                materials[i] = Material.PottedPoppy;
            for (int i = 10442; i <= 10442; i++)
                materials[i] = Material.PottedBlueOrchid;
            for (int i = 10443; i <= 10443; i++)
                materials[i] = Material.PottedAllium;
            for (int i = 10444; i <= 10444; i++)
                materials[i] = Material.PottedAzureBluet;
            for (int i = 10445; i <= 10445; i++)
                materials[i] = Material.PottedRedTulip;
            for (int i = 10446; i <= 10446; i++)
                materials[i] = Material.PottedOrangeTulip;
            for (int i = 10447; i <= 10447; i++)
                materials[i] = Material.PottedWhiteTulip;
            for (int i = 10448; i <= 10448; i++)
                materials[i] = Material.PottedPinkTulip;
            for (int i = 10449; i <= 10449; i++)
                materials[i] = Material.PottedOxeyeDaisy;
            for (int i = 10450; i <= 10450; i++)
                materials[i] = Material.PottedCornflower;
            for (int i = 10451; i <= 10451; i++)
                materials[i] = Material.PottedLilyOfTheValley;
            for (int i = 10452; i <= 10452; i++)
                materials[i] = Material.PottedWitherRose;
            for (int i = 10453; i <= 10453; i++)
                materials[i] = Material.PottedRedMushroom;
            for (int i = 10454; i <= 10454; i++)
                materials[i] = Material.PottedBrownMushroom;
            for (int i = 10455; i <= 10455; i++)
                materials[i] = Material.PottedDeadBush;
            for (int i = 10456; i <= 10456; i++)
                materials[i] = Material.PottedCactus;
            for (int i = 10457; i <= 10464; i++)
                materials[i] = Material.Carrots;
            for (int i = 10465; i <= 10472; i++)
                materials[i] = Material.Potatoes;
            for (int i = 10473; i <= 10496; i++)
                materials[i] = Material.OakButton;
            for (int i = 10497; i <= 10520; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 10521; i <= 10544; i++)
                materials[i] = Material.BirchButton;
            for (int i = 10545; i <= 10568; i++)
                materials[i] = Material.JungleButton;
            for (int i = 10569; i <= 10592; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 10593; i <= 10616; i++)
                materials[i] = Material.CherryButton;
            for (int i = 10617; i <= 10640; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 10641; i <= 10664; i++)
                materials[i] = Material.PaleOakButton;
            for (int i = 10665; i <= 10688; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 10689; i <= 10712; i++)
                materials[i] = Material.BambooButton;
            for (int i = 10713; i <= 10744; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 10745; i <= 10752; i++)
                materials[i] = Material.SkeletonWallSkull;
            for (int i = 10753; i <= 10784; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 10785; i <= 10792; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 10793; i <= 10824; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 10825; i <= 10832; i++)
                materials[i] = Material.ZombieWallHead;
            for (int i = 10833; i <= 10864; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 10865; i <= 10872; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 10873; i <= 10904; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 10905; i <= 10912; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 10913; i <= 10944; i++)
                materials[i] = Material.DragonHead;
            for (int i = 10945; i <= 10952; i++)
                materials[i] = Material.DragonWallHead;
            for (int i = 10953; i <= 10984; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 10985; i <= 10992; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 10993; i <= 10996; i++)
                materials[i] = Material.Anvil;
            for (int i = 10997; i <= 11000; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 11001; i <= 11004; i++)
                materials[i] = Material.DamagedAnvil;
            for (int i = 11005; i <= 11028; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 11029; i <= 11044; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 11045; i <= 11060; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            for (int i = 11061; i <= 11076; i++)
                materials[i] = Material.Comparator;
            for (int i = 11077; i <= 11108; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 11109; i <= 11109; i++)
                materials[i] = Material.RedstoneBlock;
            for (int i = 11110; i <= 11110; i++)
                materials[i] = Material.NetherQuartzOre;
            for (int i = 11111; i <= 11120; i++)
                materials[i] = Material.Hopper;
            for (int i = 11121; i <= 11121; i++)
                materials[i] = Material.QuartzBlock;
            for (int i = 11122; i <= 11122; i++)
                materials[i] = Material.ChiseledQuartzBlock;
            for (int i = 11123; i <= 11125; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 11126; i <= 11205; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 11206; i <= 11229; i++)
                materials[i] = Material.ActivatorRail;
            for (int i = 11230; i <= 11241; i++)
                materials[i] = Material.Dropper;
            for (int i = 11242; i <= 11242; i++)
                materials[i] = Material.WhiteTerracotta;
            for (int i = 11243; i <= 11243; i++)
                materials[i] = Material.OrangeTerracotta;
            for (int i = 11244; i <= 11244; i++)
                materials[i] = Material.MagentaTerracotta;
            for (int i = 11245; i <= 11245; i++)
                materials[i] = Material.LightBlueTerracotta;
            for (int i = 11246; i <= 11246; i++)
                materials[i] = Material.YellowTerracotta;
            for (int i = 11247; i <= 11247; i++)
                materials[i] = Material.LimeTerracotta;
            for (int i = 11248; i <= 11248; i++)
                materials[i] = Material.PinkTerracotta;
            for (int i = 11249; i <= 11249; i++)
                materials[i] = Material.GrayTerracotta;
            for (int i = 11250; i <= 11250; i++)
                materials[i] = Material.LightGrayTerracotta;
            for (int i = 11251; i <= 11251; i++)
                materials[i] = Material.CyanTerracotta;
            for (int i = 11252; i <= 11252; i++)
                materials[i] = Material.PurpleTerracotta;
            for (int i = 11253; i <= 11253; i++)
                materials[i] = Material.BlueTerracotta;
            for (int i = 11254; i <= 11254; i++)
                materials[i] = Material.BrownTerracotta;
            for (int i = 11255; i <= 11255; i++)
                materials[i] = Material.GreenTerracotta;
            for (int i = 11256; i <= 11256; i++)
                materials[i] = Material.RedTerracotta;
            for (int i = 11257; i <= 11257; i++)
                materials[i] = Material.BlackTerracotta;
            for (int i = 11258; i <= 11289; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            for (int i = 11290; i <= 11321; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            for (int i = 11322; i <= 11353; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            for (int i = 11354; i <= 11385; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            for (int i = 11386; i <= 11417; i++)
                materials[i] = Material.YellowStainedGlassPane;
            for (int i = 11418; i <= 11449; i++)
                materials[i] = Material.LimeStainedGlassPane;
            for (int i = 11450; i <= 11481; i++)
                materials[i] = Material.PinkStainedGlassPane;
            for (int i = 11482; i <= 11513; i++)
                materials[i] = Material.GrayStainedGlassPane;
            for (int i = 11514; i <= 11545; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            for (int i = 11546; i <= 11577; i++)
                materials[i] = Material.CyanStainedGlassPane;
            for (int i = 11578; i <= 11609; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            for (int i = 11610; i <= 11641; i++)
                materials[i] = Material.BlueStainedGlassPane;
            for (int i = 11642; i <= 11673; i++)
                materials[i] = Material.BrownStainedGlassPane;
            for (int i = 11674; i <= 11705; i++)
                materials[i] = Material.GreenStainedGlassPane;
            for (int i = 11706; i <= 11737; i++)
                materials[i] = Material.RedStainedGlassPane;
            for (int i = 11738; i <= 11769; i++)
                materials[i] = Material.BlackStainedGlassPane;
            for (int i = 11770; i <= 11849; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 11850; i <= 11929; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 11930; i <= 12009; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 12010; i <= 12089; i++)
                materials[i] = Material.PaleOakStairs;
            for (int i = 12090; i <= 12169; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 12170; i <= 12249; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 12250; i <= 12329; i++)
                materials[i] = Material.BambooMosaicStairs;
            for (int i = 12330; i <= 12330; i++)
                materials[i] = Material.SlimeBlock;
            for (int i = 12331; i <= 12332; i++)
                materials[i] = Material.Barrier;
            for (int i = 12333; i <= 12364; i++)
                materials[i] = Material.Light;
            for (int i = 12365; i <= 12428; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 12429; i <= 12429; i++)
                materials[i] = Material.Prismarine;
            for (int i = 12430; i <= 12430; i++)
                materials[i] = Material.PrismarineBricks;
            for (int i = 12431; i <= 12431; i++)
                materials[i] = Material.DarkPrismarine;
            for (int i = 12432; i <= 12511; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 12512; i <= 12591; i++)
                materials[i] = Material.PrismarineBrickStairs;
            for (int i = 12592; i <= 12671; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 12672; i <= 12677; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 12678; i <= 12683; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 12684; i <= 12689; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 12690; i <= 12690; i++)
                materials[i] = Material.SeaLantern;
            for (int i = 12691; i <= 12693; i++)
                materials[i] = Material.HayBlock;
            for (int i = 12694; i <= 12694; i++)
                materials[i] = Material.WhiteCarpet;
            for (int i = 12695; i <= 12695; i++)
                materials[i] = Material.OrangeCarpet;
            for (int i = 12696; i <= 12696; i++)
                materials[i] = Material.MagentaCarpet;
            for (int i = 12697; i <= 12697; i++)
                materials[i] = Material.LightBlueCarpet;
            for (int i = 12698; i <= 12698; i++)
                materials[i] = Material.YellowCarpet;
            for (int i = 12699; i <= 12699; i++)
                materials[i] = Material.LimeCarpet;
            for (int i = 12700; i <= 12700; i++)
                materials[i] = Material.PinkCarpet;
            for (int i = 12701; i <= 12701; i++)
                materials[i] = Material.GrayCarpet;
            for (int i = 12702; i <= 12702; i++)
                materials[i] = Material.LightGrayCarpet;
            for (int i = 12703; i <= 12703; i++)
                materials[i] = Material.CyanCarpet;
            for (int i = 12704; i <= 12704; i++)
                materials[i] = Material.PurpleCarpet;
            for (int i = 12705; i <= 12705; i++)
                materials[i] = Material.BlueCarpet;
            for (int i = 12706; i <= 12706; i++)
                materials[i] = Material.BrownCarpet;
            for (int i = 12707; i <= 12707; i++)
                materials[i] = Material.GreenCarpet;
            for (int i = 12708; i <= 12708; i++)
                materials[i] = Material.RedCarpet;
            for (int i = 12709; i <= 12709; i++)
                materials[i] = Material.BlackCarpet;
            for (int i = 12710; i <= 12710; i++)
                materials[i] = Material.Terracotta;
            for (int i = 12711; i <= 12711; i++)
                materials[i] = Material.CoalBlock;
            for (int i = 12712; i <= 12712; i++)
                materials[i] = Material.PackedIce;
            for (int i = 12713; i <= 12714; i++)
                materials[i] = Material.Sunflower;
            for (int i = 12715; i <= 12716; i++)
                materials[i] = Material.Lilac;
            for (int i = 12717; i <= 12718; i++)
                materials[i] = Material.RoseBush;
            for (int i = 12719; i <= 12720; i++)
                materials[i] = Material.Peony;
            for (int i = 12721; i <= 12722; i++)
                materials[i] = Material.TallGrass;
            for (int i = 12723; i <= 12724; i++)
                materials[i] = Material.LargeFern;
            for (int i = 12725; i <= 12740; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 12741; i <= 12756; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 12757; i <= 12772; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 12773; i <= 12788; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 12789; i <= 12804; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 12805; i <= 12820; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 12821; i <= 12836; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 12837; i <= 12852; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 12853; i <= 12868; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 12869; i <= 12884; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 12885; i <= 12900; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 12901; i <= 12916; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 12917; i <= 12932; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 12933; i <= 12948; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 12949; i <= 12964; i++)
                materials[i] = Material.RedBanner;
            for (int i = 12965; i <= 12980; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 12981; i <= 12984; i++)
                materials[i] = Material.WhiteWallBanner;
            for (int i = 12985; i <= 12988; i++)
                materials[i] = Material.OrangeWallBanner;
            for (int i = 12989; i <= 12992; i++)
                materials[i] = Material.MagentaWallBanner;
            for (int i = 12993; i <= 12996; i++)
                materials[i] = Material.LightBlueWallBanner;
            for (int i = 12997; i <= 13000; i++)
                materials[i] = Material.YellowWallBanner;
            for (int i = 13001; i <= 13004; i++)
                materials[i] = Material.LimeWallBanner;
            for (int i = 13005; i <= 13008; i++)
                materials[i] = Material.PinkWallBanner;
            for (int i = 13009; i <= 13012; i++)
                materials[i] = Material.GrayWallBanner;
            for (int i = 13013; i <= 13016; i++)
                materials[i] = Material.LightGrayWallBanner;
            for (int i = 13017; i <= 13020; i++)
                materials[i] = Material.CyanWallBanner;
            for (int i = 13021; i <= 13024; i++)
                materials[i] = Material.PurpleWallBanner;
            for (int i = 13025; i <= 13028; i++)
                materials[i] = Material.BlueWallBanner;
            for (int i = 13029; i <= 13032; i++)
                materials[i] = Material.BrownWallBanner;
            for (int i = 13033; i <= 13036; i++)
                materials[i] = Material.GreenWallBanner;
            for (int i = 13037; i <= 13040; i++)
                materials[i] = Material.RedWallBanner;
            for (int i = 13041; i <= 13044; i++)
                materials[i] = Material.BlackWallBanner;
            for (int i = 13045; i <= 13045; i++)
                materials[i] = Material.RedSandstone;
            for (int i = 13046; i <= 13046; i++)
                materials[i] = Material.ChiseledRedSandstone;
            for (int i = 13047; i <= 13047; i++)
                materials[i] = Material.CutRedSandstone;
            for (int i = 13048; i <= 13127; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 13128; i <= 13133; i++)
                materials[i] = Material.OakSlab;
            for (int i = 13134; i <= 13139; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 13140; i <= 13145; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 13146; i <= 13151; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 13152; i <= 13157; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 13158; i <= 13163; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 13164; i <= 13169; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 13170; i <= 13175; i++)
                materials[i] = Material.PaleOakSlab;
            for (int i = 13176; i <= 13181; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 13182; i <= 13187; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 13188; i <= 13193; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 13194; i <= 13199; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 13200; i <= 13205; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 13206; i <= 13211; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 13212; i <= 13217; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 13218; i <= 13223; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 13224; i <= 13229; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 13230; i <= 13235; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 13236; i <= 13241; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 13242; i <= 13247; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 13248; i <= 13253; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 13254; i <= 13259; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 13260; i <= 13265; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 13266; i <= 13271; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            for (int i = 13272; i <= 13277; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 13278; i <= 13278; i++)
                materials[i] = Material.SmoothStone;
            for (int i = 13279; i <= 13279; i++)
                materials[i] = Material.SmoothSandstone;
            for (int i = 13280; i <= 13280; i++)
                materials[i] = Material.SmoothQuartz;
            for (int i = 13281; i <= 13281; i++)
                materials[i] = Material.SmoothRedSandstone;
            for (int i = 13282; i <= 13313; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 13314; i <= 13345; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 13346; i <= 13377; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 13378; i <= 13409; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 13410; i <= 13441; i++)
                materials[i] = Material.CherryFenceGate;
            for (int i = 13442; i <= 13473; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 13474; i <= 13505; i++)
                materials[i] = Material.PaleOakFenceGate;
            for (int i = 13506; i <= 13537; i++)
                materials[i] = Material.MangroveFenceGate;
            for (int i = 13538; i <= 13569; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 13570; i <= 13601; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 13602; i <= 13633; i++)
                materials[i] = Material.BirchFence;
            for (int i = 13634; i <= 13665; i++)
                materials[i] = Material.JungleFence;
            for (int i = 13666; i <= 13697; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 13698; i <= 13729; i++)
                materials[i] = Material.CherryFence;
            for (int i = 13730; i <= 13761; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 13762; i <= 13793; i++)
                materials[i] = Material.PaleOakFence;
            for (int i = 13794; i <= 13825; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 13826; i <= 13857; i++)
                materials[i] = Material.BambooFence;
            for (int i = 13858; i <= 13921; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 13922; i <= 13985; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 13986; i <= 14049; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 14050; i <= 14113; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 14114; i <= 14177; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 14178; i <= 14241; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 14242; i <= 14305; i++)
                materials[i] = Material.PaleOakDoor;
            for (int i = 14306; i <= 14369; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 14370; i <= 14433; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 14434; i <= 14439; i++)
                materials[i] = Material.EndRod;
            for (int i = 14440; i <= 14503; i++)
                materials[i] = Material.ChorusPlant;
            for (int i = 14504; i <= 14509; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 14510; i <= 14510; i++)
                materials[i] = Material.PurpurBlock;
            for (int i = 14511; i <= 14513; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 14514; i <= 14593; i++)
                materials[i] = Material.PurpurStairs;
            for (int i = 14594; i <= 14594; i++)
                materials[i] = Material.EndStoneBricks;
            for (int i = 14595; i <= 14596; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 14597; i <= 14606; i++)
                materials[i] = Material.PitcherCrop;
            for (int i = 14607; i <= 14608; i++)
                materials[i] = Material.PitcherPlant;
            for (int i = 14609; i <= 14612; i++)
                materials[i] = Material.Beetroots;
            for (int i = 14613; i <= 14613; i++)
                materials[i] = Material.DirtPath;
            for (int i = 14614; i <= 14614; i++)
                materials[i] = Material.EndGateway;
            for (int i = 14615; i <= 14626; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 14627; i <= 14638; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 14639; i <= 14642; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 14643; i <= 14643; i++)
                materials[i] = Material.MagmaBlock;
            for (int i = 14644; i <= 14644; i++)
                materials[i] = Material.NetherWartBlock;
            for (int i = 14645; i <= 14645; i++)
                materials[i] = Material.RedNetherBricks;
            for (int i = 14646; i <= 14648; i++)
                materials[i] = Material.BoneBlock;
            for (int i = 14649; i <= 14649; i++)
                materials[i] = Material.StructureVoid;
            for (int i = 14650; i <= 14661; i++)
                materials[i] = Material.Observer;
            for (int i = 14662; i <= 14667; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 14668; i <= 14673; i++)
                materials[i] = Material.WhiteShulkerBox;
            for (int i = 14674; i <= 14679; i++)
                materials[i] = Material.OrangeShulkerBox;
            for (int i = 14680; i <= 14685; i++)
                materials[i] = Material.MagentaShulkerBox;
            for (int i = 14686; i <= 14691; i++)
                materials[i] = Material.LightBlueShulkerBox;
            for (int i = 14692; i <= 14697; i++)
                materials[i] = Material.YellowShulkerBox;
            for (int i = 14698; i <= 14703; i++)
                materials[i] = Material.LimeShulkerBox;
            for (int i = 14704; i <= 14709; i++)
                materials[i] = Material.PinkShulkerBox;
            for (int i = 14710; i <= 14715; i++)
                materials[i] = Material.GrayShulkerBox;
            for (int i = 14716; i <= 14721; i++)
                materials[i] = Material.LightGrayShulkerBox;
            for (int i = 14722; i <= 14727; i++)
                materials[i] = Material.CyanShulkerBox;
            for (int i = 14728; i <= 14733; i++)
                materials[i] = Material.PurpleShulkerBox;
            for (int i = 14734; i <= 14739; i++)
                materials[i] = Material.BlueShulkerBox;
            for (int i = 14740; i <= 14745; i++)
                materials[i] = Material.BrownShulkerBox;
            for (int i = 14746; i <= 14751; i++)
                materials[i] = Material.GreenShulkerBox;
            for (int i = 14752; i <= 14757; i++)
                materials[i] = Material.RedShulkerBox;
            for (int i = 14758; i <= 14763; i++)
                materials[i] = Material.BlackShulkerBox;
            for (int i = 14764; i <= 14767; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 14768; i <= 14771; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 14772; i <= 14775; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 14776; i <= 14779; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 14780; i <= 14783; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 14784; i <= 14787; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 14788; i <= 14791; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 14792; i <= 14795; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 14796; i <= 14799; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 14800; i <= 14803; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 14804; i <= 14807; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 14808; i <= 14811; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            for (int i = 14812; i <= 14815; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            for (int i = 14816; i <= 14819; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 14820; i <= 14823; i++)
                materials[i] = Material.RedGlazedTerracotta;
            for (int i = 14824; i <= 14827; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 14828; i <= 14828; i++)
                materials[i] = Material.WhiteConcrete;
            for (int i = 14829; i <= 14829; i++)
                materials[i] = Material.OrangeConcrete;
            for (int i = 14830; i <= 14830; i++)
                materials[i] = Material.MagentaConcrete;
            for (int i = 14831; i <= 14831; i++)
                materials[i] = Material.LightBlueConcrete;
            for (int i = 14832; i <= 14832; i++)
                materials[i] = Material.YellowConcrete;
            for (int i = 14833; i <= 14833; i++)
                materials[i] = Material.LimeConcrete;
            for (int i = 14834; i <= 14834; i++)
                materials[i] = Material.PinkConcrete;
            for (int i = 14835; i <= 14835; i++)
                materials[i] = Material.GrayConcrete;
            for (int i = 14836; i <= 14836; i++)
                materials[i] = Material.LightGrayConcrete;
            for (int i = 14837; i <= 14837; i++)
                materials[i] = Material.CyanConcrete;
            for (int i = 14838; i <= 14838; i++)
                materials[i] = Material.PurpleConcrete;
            for (int i = 14839; i <= 14839; i++)
                materials[i] = Material.BlueConcrete;
            for (int i = 14840; i <= 14840; i++)
                materials[i] = Material.BrownConcrete;
            for (int i = 14841; i <= 14841; i++)
                materials[i] = Material.GreenConcrete;
            for (int i = 14842; i <= 14842; i++)
                materials[i] = Material.RedConcrete;
            for (int i = 14843; i <= 14843; i++)
                materials[i] = Material.BlackConcrete;
            for (int i = 14844; i <= 14844; i++)
                materials[i] = Material.WhiteConcretePowder;
            for (int i = 14845; i <= 14845; i++)
                materials[i] = Material.OrangeConcretePowder;
            for (int i = 14846; i <= 14846; i++)
                materials[i] = Material.MagentaConcretePowder;
            for (int i = 14847; i <= 14847; i++)
                materials[i] = Material.LightBlueConcretePowder;
            for (int i = 14848; i <= 14848; i++)
                materials[i] = Material.YellowConcretePowder;
            for (int i = 14849; i <= 14849; i++)
                materials[i] = Material.LimeConcretePowder;
            for (int i = 14850; i <= 14850; i++)
                materials[i] = Material.PinkConcretePowder;
            for (int i = 14851; i <= 14851; i++)
                materials[i] = Material.GrayConcretePowder;
            for (int i = 14852; i <= 14852; i++)
                materials[i] = Material.LightGrayConcretePowder;
            for (int i = 14853; i <= 14853; i++)
                materials[i] = Material.CyanConcretePowder;
            for (int i = 14854; i <= 14854; i++)
                materials[i] = Material.PurpleConcretePowder;
            for (int i = 14855; i <= 14855; i++)
                materials[i] = Material.BlueConcretePowder;
            for (int i = 14856; i <= 14856; i++)
                materials[i] = Material.BrownConcretePowder;
            for (int i = 14857; i <= 14857; i++)
                materials[i] = Material.GreenConcretePowder;
            for (int i = 14858; i <= 14858; i++)
                materials[i] = Material.RedConcretePowder;
            for (int i = 14859; i <= 14859; i++)
                materials[i] = Material.BlackConcretePowder;
            for (int i = 14860; i <= 14885; i++)
                materials[i] = Material.Kelp;
            for (int i = 14886; i <= 14886; i++)
                materials[i] = Material.KelpPlant;
            for (int i = 14887; i <= 14887; i++)
                materials[i] = Material.DriedKelpBlock;
            for (int i = 14888; i <= 14899; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 14900; i <= 14902; i++)
                materials[i] = Material.SnifferEgg;
            for (int i = 14903; i <= 14934; i++)
                materials[i] = Material.DriedGhast;
            for (int i = 14935; i <= 14935; i++)
                materials[i] = Material.DeadTubeCoralBlock;
            for (int i = 14936; i <= 14936; i++)
                materials[i] = Material.DeadBrainCoralBlock;
            for (int i = 14937; i <= 14937; i++)
                materials[i] = Material.DeadBubbleCoralBlock;
            for (int i = 14938; i <= 14938; i++)
                materials[i] = Material.DeadFireCoralBlock;
            for (int i = 14939; i <= 14939; i++)
                materials[i] = Material.DeadHornCoralBlock;
            for (int i = 14940; i <= 14940; i++)
                materials[i] = Material.TubeCoralBlock;
            for (int i = 14941; i <= 14941; i++)
                materials[i] = Material.BrainCoralBlock;
            for (int i = 14942; i <= 14942; i++)
                materials[i] = Material.BubbleCoralBlock;
            for (int i = 14943; i <= 14943; i++)
                materials[i] = Material.FireCoralBlock;
            for (int i = 14944; i <= 14944; i++)
                materials[i] = Material.HornCoralBlock;
            for (int i = 14945; i <= 14946; i++)
                materials[i] = Material.DeadTubeCoral;
            for (int i = 14947; i <= 14948; i++)
                materials[i] = Material.DeadBrainCoral;
            for (int i = 14949; i <= 14950; i++)
                materials[i] = Material.DeadBubbleCoral;
            for (int i = 14951; i <= 14952; i++)
                materials[i] = Material.DeadFireCoral;
            for (int i = 14953; i <= 14954; i++)
                materials[i] = Material.DeadHornCoral;
            for (int i = 14955; i <= 14956; i++)
                materials[i] = Material.TubeCoral;
            for (int i = 14957; i <= 14958; i++)
                materials[i] = Material.BrainCoral;
            for (int i = 14959; i <= 14960; i++)
                materials[i] = Material.BubbleCoral;
            for (int i = 14961; i <= 14962; i++)
                materials[i] = Material.FireCoral;
            for (int i = 14963; i <= 14964; i++)
                materials[i] = Material.HornCoral;
            for (int i = 14965; i <= 14966; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 14967; i <= 14968; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 14969; i <= 14970; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 14971; i <= 14972; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 14973; i <= 14974; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 14975; i <= 14976; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 14977; i <= 14978; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 14979; i <= 14980; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 14981; i <= 14982; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 14983; i <= 14984; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 14985; i <= 14992; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 14993; i <= 15000; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 15001; i <= 15008; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            for (int i = 15009; i <= 15016; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 15017; i <= 15024; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 15025; i <= 15032; i++)
                materials[i] = Material.TubeCoralWallFan;
            for (int i = 15033; i <= 15040; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 15041; i <= 15048; i++)
                materials[i] = Material.BubbleCoralWallFan;
            for (int i = 15049; i <= 15056; i++)
                materials[i] = Material.FireCoralWallFan;
            for (int i = 15057; i <= 15064; i++)
                materials[i] = Material.HornCoralWallFan;
            for (int i = 15065; i <= 15072; i++)
                materials[i] = Material.SeaPickle;
            for (int i = 15073; i <= 15073; i++)
                materials[i] = Material.BlueIce;
            for (int i = 15074; i <= 15075; i++)
                materials[i] = Material.Conduit;
            for (int i = 15076; i <= 15076; i++)
                materials[i] = Material.BambooSapling;
            for (int i = 15077; i <= 15088; i++)
                materials[i] = Material.Bamboo;
            for (int i = 15089; i <= 15089; i++)
                materials[i] = Material.PottedBamboo;
            for (int i = 15090; i <= 15090; i++)
                materials[i] = Material.VoidAir;
            for (int i = 15091; i <= 15091; i++)
                materials[i] = Material.CaveAir;
            for (int i = 15092; i <= 15093; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 15094; i <= 15173; i++)
                materials[i] = Material.PolishedGraniteStairs;
            for (int i = 15174; i <= 15253; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            for (int i = 15254; i <= 15333; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 15334; i <= 15413; i++)
                materials[i] = Material.PolishedDioriteStairs;
            for (int i = 15414; i <= 15493; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 15494; i <= 15573; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 15574; i <= 15653; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 15654; i <= 15733; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            for (int i = 15734; i <= 15813; i++)
                materials[i] = Material.SmoothQuartzStairs;
            for (int i = 15814; i <= 15893; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 15894; i <= 15973; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 15974; i <= 16053; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 16054; i <= 16133; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 16134; i <= 16213; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 16214; i <= 16219; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 16220; i <= 16225; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 16226; i <= 16231; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 16232; i <= 16237; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 16238; i <= 16243; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 16244; i <= 16249; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 16250; i <= 16255; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 16256; i <= 16261; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 16262; i <= 16267; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 16268; i <= 16273; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 16274; i <= 16279; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 16280; i <= 16285; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 16286; i <= 16291; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 16292; i <= 16615; i++)
                materials[i] = Material.BrickWall;
            for (int i = 16616; i <= 16939; i++)
                materials[i] = Material.PrismarineWall;
            for (int i = 16940; i <= 17263; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 17264; i <= 17587; i++)
                materials[i] = Material.MossyStoneBrickWall;
            for (int i = 17588; i <= 17911; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 17912; i <= 18235; i++)
                materials[i] = Material.StoneBrickWall;
            for (int i = 18236; i <= 18559; i++)
                materials[i] = Material.MudBrickWall;
            for (int i = 18560; i <= 18883; i++)
                materials[i] = Material.NetherBrickWall;
            for (int i = 18884; i <= 19207; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 19208; i <= 19531; i++)
                materials[i] = Material.RedNetherBrickWall;
            for (int i = 19532; i <= 19855; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 19856; i <= 20179; i++)
                materials[i] = Material.EndStoneBrickWall;
            for (int i = 20180; i <= 20503; i++)
                materials[i] = Material.DioriteWall;
            for (int i = 20504; i <= 20535; i++)
                materials[i] = Material.Scaffolding;
            for (int i = 20536; i <= 20539; i++)
                materials[i] = Material.Loom;
            for (int i = 20540; i <= 20551; i++)
                materials[i] = Material.Barrel;
            for (int i = 20552; i <= 20559; i++)
                materials[i] = Material.Smoker;
            for (int i = 20560; i <= 20567; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 20568; i <= 20568; i++)
                materials[i] = Material.CartographyTable;
            for (int i = 20569; i <= 20569; i++)
                materials[i] = Material.FletchingTable;
            for (int i = 20570; i <= 20581; i++)
                materials[i] = Material.Grindstone;
            for (int i = 20582; i <= 20597; i++)
                materials[i] = Material.Lectern;
            for (int i = 20598; i <= 20598; i++)
                materials[i] = Material.SmithingTable;
            for (int i = 20599; i <= 20602; i++)
                materials[i] = Material.Stonecutter;
            for (int i = 20603; i <= 20634; i++)
                materials[i] = Material.Bell;
            for (int i = 20635; i <= 20638; i++)
                materials[i] = Material.Lantern;
            for (int i = 20639; i <= 20642; i++)
                materials[i] = Material.SoulLantern;
            for (int i = 20643; i <= 20646; i++)
                materials[i] = Material.CopperLantern;
            for (int i = 20647; i <= 20650; i++)
                materials[i] = Material.ExposedCopperLantern;
            for (int i = 20651; i <= 20654; i++)
                materials[i] = Material.WeatheredCopperLantern;
            for (int i = 20655; i <= 20658; i++)
                materials[i] = Material.OxidizedCopperLantern;
            for (int i = 20659; i <= 20662; i++)
                materials[i] = Material.WaxedCopperLantern;
            for (int i = 20663; i <= 20666; i++)
                materials[i] = Material.WaxedExposedCopperLantern;
            for (int i = 20667; i <= 20670; i++)
                materials[i] = Material.WaxedWeatheredCopperLantern;
            for (int i = 20671; i <= 20674; i++)
                materials[i] = Material.WaxedOxidizedCopperLantern;
            for (int i = 20675; i <= 20706; i++)
                materials[i] = Material.Campfire;
            for (int i = 20707; i <= 20738; i++)
                materials[i] = Material.SoulCampfire;
            for (int i = 20739; i <= 20742; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 20743; i <= 20745; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 20746; i <= 20748; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 20749; i <= 20751; i++)
                materials[i] = Material.WarpedHyphae;
            for (int i = 20752; i <= 20754; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 20755; i <= 20755; i++)
                materials[i] = Material.WarpedNylium;
            for (int i = 20756; i <= 20756; i++)
                materials[i] = Material.WarpedFungus;
            for (int i = 20757; i <= 20757; i++)
                materials[i] = Material.WarpedWartBlock;
            for (int i = 20758; i <= 20758; i++)
                materials[i] = Material.WarpedRoots;
            for (int i = 20759; i <= 20759; i++)
                materials[i] = Material.NetherSprouts;
            for (int i = 20760; i <= 20762; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 20763; i <= 20765; i++)
                materials[i] = Material.StrippedCrimsonStem;
            for (int i = 20766; i <= 20768; i++)
                materials[i] = Material.CrimsonHyphae;
            for (int i = 20769; i <= 20771; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 20772; i <= 20772; i++)
                materials[i] = Material.CrimsonNylium;
            for (int i = 20773; i <= 20773; i++)
                materials[i] = Material.CrimsonFungus;
            for (int i = 20774; i <= 20774; i++)
                materials[i] = Material.Shroomlight;
            for (int i = 20775; i <= 20800; i++)
                materials[i] = Material.WeepingVines;
            for (int i = 20801; i <= 20801; i++)
                materials[i] = Material.WeepingVinesPlant;
            for (int i = 20802; i <= 20827; i++)
                materials[i] = Material.TwistingVines;
            for (int i = 20828; i <= 20828; i++)
                materials[i] = Material.TwistingVinesPlant;
            for (int i = 20829; i <= 20829; i++)
                materials[i] = Material.CrimsonRoots;
            for (int i = 20830; i <= 20830; i++)
                materials[i] = Material.CrimsonPlanks;
            for (int i = 20831; i <= 20831; i++)
                materials[i] = Material.WarpedPlanks;
            for (int i = 20832; i <= 20837; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 20838; i <= 20843; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 20844; i <= 20845; i++)
                materials[i] = Material.CrimsonPressurePlate;
            for (int i = 20846; i <= 20847; i++)
                materials[i] = Material.WarpedPressurePlate;
            for (int i = 20848; i <= 20879; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 20880; i <= 20911; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 20912; i <= 20975; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 20976; i <= 21039; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 21040; i <= 21071; i++)
                materials[i] = Material.CrimsonFenceGate;
            for (int i = 21072; i <= 21103; i++)
                materials[i] = Material.WarpedFenceGate;
            for (int i = 21104; i <= 21183; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 21184; i <= 21263; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 21264; i <= 21287; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 21288; i <= 21311; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 21312; i <= 21375; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 21376; i <= 21439; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 21440; i <= 21471; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 21472; i <= 21503; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 21504; i <= 21511; i++)
                materials[i] = Material.CrimsonWallSign;
            for (int i = 21512; i <= 21519; i++)
                materials[i] = Material.WarpedWallSign;
            for (int i = 21520; i <= 21523; i++)
                materials[i] = Material.StructureBlock;
            for (int i = 21524; i <= 21535; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 21536; i <= 21539; i++)
                materials[i] = Material.TestBlock;
            for (int i = 21540; i <= 21540; i++)
                materials[i] = Material.TestInstanceBlock;
            for (int i = 21541; i <= 21549; i++)
                materials[i] = Material.Composter;
            for (int i = 21550; i <= 21565; i++)
                materials[i] = Material.Target;
            for (int i = 21566; i <= 21589; i++)
                materials[i] = Material.BeeNest;
            for (int i = 21590; i <= 21613; i++)
                materials[i] = Material.Beehive;
            for (int i = 21614; i <= 21614; i++)
                materials[i] = Material.HoneyBlock;
            for (int i = 21615; i <= 21615; i++)
                materials[i] = Material.HoneycombBlock;
            for (int i = 21616; i <= 21616; i++)
                materials[i] = Material.NetheriteBlock;
            for (int i = 21617; i <= 21617; i++)
                materials[i] = Material.AncientDebris;
            for (int i = 21618; i <= 21618; i++)
                materials[i] = Material.CryingObsidian;
            for (int i = 21619; i <= 21623; i++)
                materials[i] = Material.RespawnAnchor;
            for (int i = 21624; i <= 21624; i++)
                materials[i] = Material.PottedCrimsonFungus;
            for (int i = 21625; i <= 21625; i++)
                materials[i] = Material.PottedWarpedFungus;
            for (int i = 21626; i <= 21626; i++)
                materials[i] = Material.PottedCrimsonRoots;
            for (int i = 21627; i <= 21627; i++)
                materials[i] = Material.PottedWarpedRoots;
            for (int i = 21628; i <= 21628; i++)
                materials[i] = Material.Lodestone;
            for (int i = 21629; i <= 21629; i++)
                materials[i] = Material.Blackstone;
            for (int i = 21630; i <= 21709; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 21710; i <= 22033; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 22034; i <= 22039; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 22040; i <= 22040; i++)
                materials[i] = Material.PolishedBlackstone;
            for (int i = 22041; i <= 22041; i++)
                materials[i] = Material.PolishedBlackstoneBricks;
            for (int i = 22042; i <= 22042; i++)
                materials[i] = Material.CrackedPolishedBlackstoneBricks;
            for (int i = 22043; i <= 22043; i++)
                materials[i] = Material.ChiseledPolishedBlackstone;
            for (int i = 22044; i <= 22049; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 22050; i <= 22129; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 22130; i <= 22453; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            for (int i = 22454; i <= 22454; i++)
                materials[i] = Material.GildedBlackstone;
            for (int i = 22455; i <= 22534; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 22535; i <= 22540; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 22541; i <= 22542; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 22543; i <= 22566; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 22567; i <= 22890; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            for (int i = 22891; i <= 22891; i++)
                materials[i] = Material.ChiseledNetherBricks;
            for (int i = 22892; i <= 22892; i++)
                materials[i] = Material.CrackedNetherBricks;
            for (int i = 22893; i <= 22893; i++)
                materials[i] = Material.QuartzBricks;
            for (int i = 22894; i <= 22909; i++)
                materials[i] = Material.Candle;
            for (int i = 22910; i <= 22925; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 22926; i <= 22941; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 22942; i <= 22957; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 22958; i <= 22973; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 22974; i <= 22989; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 22990; i <= 23005; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 23006; i <= 23021; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 23022; i <= 23037; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 23038; i <= 23053; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 23054; i <= 23069; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 23070; i <= 23085; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 23086; i <= 23101; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 23102; i <= 23117; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 23118; i <= 23133; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 23134; i <= 23149; i++)
                materials[i] = Material.RedCandle;
            for (int i = 23150; i <= 23165; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 23166; i <= 23167; i++)
                materials[i] = Material.CandleCake;
            for (int i = 23168; i <= 23169; i++)
                materials[i] = Material.WhiteCandleCake;
            for (int i = 23170; i <= 23171; i++)
                materials[i] = Material.OrangeCandleCake;
            for (int i = 23172; i <= 23173; i++)
                materials[i] = Material.MagentaCandleCake;
            for (int i = 23174; i <= 23175; i++)
                materials[i] = Material.LightBlueCandleCake;
            for (int i = 23176; i <= 23177; i++)
                materials[i] = Material.YellowCandleCake;
            for (int i = 23178; i <= 23179; i++)
                materials[i] = Material.LimeCandleCake;
            for (int i = 23180; i <= 23181; i++)
                materials[i] = Material.PinkCandleCake;
            for (int i = 23182; i <= 23183; i++)
                materials[i] = Material.GrayCandleCake;
            for (int i = 23184; i <= 23185; i++)
                materials[i] = Material.LightGrayCandleCake;
            for (int i = 23186; i <= 23187; i++)
                materials[i] = Material.CyanCandleCake;
            for (int i = 23188; i <= 23189; i++)
                materials[i] = Material.PurpleCandleCake;
            for (int i = 23190; i <= 23191; i++)
                materials[i] = Material.BlueCandleCake;
            for (int i = 23192; i <= 23193; i++)
                materials[i] = Material.BrownCandleCake;
            for (int i = 23194; i <= 23195; i++)
                materials[i] = Material.GreenCandleCake;
            for (int i = 23196; i <= 23197; i++)
                materials[i] = Material.RedCandleCake;
            for (int i = 23198; i <= 23199; i++)
                materials[i] = Material.BlackCandleCake;
            for (int i = 23200; i <= 23200; i++)
                materials[i] = Material.AmethystBlock;
            for (int i = 23201; i <= 23201; i++)
                materials[i] = Material.BuddingAmethyst;
            for (int i = 23202; i <= 23213; i++)
                materials[i] = Material.AmethystCluster;
            for (int i = 23214; i <= 23225; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 23226; i <= 23237; i++)
                materials[i] = Material.MediumAmethystBud;
            for (int i = 23238; i <= 23249; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 23250; i <= 23250; i++)
                materials[i] = Material.Tuff;
            for (int i = 23251; i <= 23256; i++)
                materials[i] = Material.TuffSlab;
            for (int i = 23257; i <= 23336; i++)
                materials[i] = Material.TuffStairs;
            for (int i = 23337; i <= 23660; i++)
                materials[i] = Material.TuffWall;
            for (int i = 23661; i <= 23661; i++)
                materials[i] = Material.PolishedTuff;
            for (int i = 23662; i <= 23667; i++)
                materials[i] = Material.PolishedTuffSlab;
            for (int i = 23668; i <= 23747; i++)
                materials[i] = Material.PolishedTuffStairs;
            for (int i = 23748; i <= 24071; i++)
                materials[i] = Material.PolishedTuffWall;
            for (int i = 24072; i <= 24072; i++)
                materials[i] = Material.ChiseledTuff;
            for (int i = 24073; i <= 24073; i++)
                materials[i] = Material.TuffBricks;
            for (int i = 24074; i <= 24079; i++)
                materials[i] = Material.TuffBrickSlab;
            for (int i = 24080; i <= 24159; i++)
                materials[i] = Material.TuffBrickStairs;
            for (int i = 24160; i <= 24483; i++)
                materials[i] = Material.TuffBrickWall;
            for (int i = 24484; i <= 24484; i++)
                materials[i] = Material.ChiseledTuffBricks;
            for (int i = 24485; i <= 24485; i++)
                materials[i] = Material.Calcite;
            for (int i = 24486; i <= 24486; i++)
                materials[i] = Material.TintedGlass;
            for (int i = 24487; i <= 24487; i++)
                materials[i] = Material.PowderSnow;
            for (int i = 24488; i <= 24583; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 24584; i <= 24967; i++)
                materials[i] = Material.CalibratedSculkSensor;
            for (int i = 24968; i <= 24968; i++)
                materials[i] = Material.Sculk;
            for (int i = 24969; i <= 25096; i++)
                materials[i] = Material.SculkVein;
            for (int i = 25097; i <= 25098; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 25099; i <= 25106; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 25107; i <= 25107; i++)
                materials[i] = Material.CopperBlock;
            for (int i = 25108; i <= 25108; i++)
                materials[i] = Material.ExposedCopper;
            for (int i = 25109; i <= 25109; i++)
                materials[i] = Material.WeatheredCopper;
            for (int i = 25110; i <= 25110; i++)
                materials[i] = Material.OxidizedCopper;
            for (int i = 25111; i <= 25111; i++)
                materials[i] = Material.CopperOre;
            for (int i = 25112; i <= 25112; i++)
                materials[i] = Material.DeepslateCopperOre;
            for (int i = 25113; i <= 25113; i++)
                materials[i] = Material.OxidizedCutCopper;
            for (int i = 25114; i <= 25114; i++)
                materials[i] = Material.WeatheredCutCopper;
            for (int i = 25115; i <= 25115; i++)
                materials[i] = Material.ExposedCutCopper;
            for (int i = 25116; i <= 25116; i++)
                materials[i] = Material.CutCopper;
            for (int i = 25117; i <= 25117; i++)
                materials[i] = Material.OxidizedChiseledCopper;
            for (int i = 25118; i <= 25118; i++)
                materials[i] = Material.WeatheredChiseledCopper;
            for (int i = 25119; i <= 25119; i++)
                materials[i] = Material.ExposedChiseledCopper;
            for (int i = 25120; i <= 25120; i++)
                materials[i] = Material.ChiseledCopper;
            for (int i = 25121; i <= 25121; i++)
                materials[i] = Material.WaxedOxidizedChiseledCopper;
            for (int i = 25122; i <= 25122; i++)
                materials[i] = Material.WaxedWeatheredChiseledCopper;
            for (int i = 25123; i <= 25123; i++)
                materials[i] = Material.WaxedExposedChiseledCopper;
            for (int i = 25124; i <= 25124; i++)
                materials[i] = Material.WaxedChiseledCopper;
            for (int i = 25125; i <= 25204; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            for (int i = 25205; i <= 25284; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 25285; i <= 25364; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 25365; i <= 25444; i++)
                materials[i] = Material.CutCopperStairs;
            for (int i = 25445; i <= 25450; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 25451; i <= 25456; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 25457; i <= 25462; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 25463; i <= 25468; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 25469; i <= 25469; i++)
                materials[i] = Material.WaxedCopperBlock;
            for (int i = 25470; i <= 25470; i++)
                materials[i] = Material.WaxedWeatheredCopper;
            for (int i = 25471; i <= 25471; i++)
                materials[i] = Material.WaxedExposedCopper;
            for (int i = 25472; i <= 25472; i++)
                materials[i] = Material.WaxedOxidizedCopper;
            for (int i = 25473; i <= 25473; i++)
                materials[i] = Material.WaxedOxidizedCutCopper;
            for (int i = 25474; i <= 25474; i++)
                materials[i] = Material.WaxedWeatheredCutCopper;
            for (int i = 25475; i <= 25475; i++)
                materials[i] = Material.WaxedExposedCutCopper;
            for (int i = 25476; i <= 25476; i++)
                materials[i] = Material.WaxedCutCopper;
            for (int i = 25477; i <= 25556; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            for (int i = 25557; i <= 25636; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            for (int i = 25637; i <= 25716; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            for (int i = 25717; i <= 25796; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            for (int i = 25797; i <= 25802; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 25803; i <= 25808; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 25809; i <= 25814; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 25815; i <= 25820; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 25821; i <= 25884; i++)
                materials[i] = Material.CopperDoor;
            for (int i = 25885; i <= 25948; i++)
                materials[i] = Material.ExposedCopperDoor;
            for (int i = 25949; i <= 26012; i++)
                materials[i] = Material.OxidizedCopperDoor;
            for (int i = 26013; i <= 26076; i++)
                materials[i] = Material.WeatheredCopperDoor;
            for (int i = 26077; i <= 26140; i++)
                materials[i] = Material.WaxedCopperDoor;
            for (int i = 26141; i <= 26204; i++)
                materials[i] = Material.WaxedExposedCopperDoor;
            for (int i = 26205; i <= 26268; i++)
                materials[i] = Material.WaxedOxidizedCopperDoor;
            for (int i = 26269; i <= 26332; i++)
                materials[i] = Material.WaxedWeatheredCopperDoor;
            for (int i = 26333; i <= 26396; i++)
                materials[i] = Material.CopperTrapdoor;
            for (int i = 26397; i <= 26460; i++)
                materials[i] = Material.ExposedCopperTrapdoor;
            for (int i = 26461; i <= 26524; i++)
                materials[i] = Material.OxidizedCopperTrapdoor;
            for (int i = 26525; i <= 26588; i++)
                materials[i] = Material.WeatheredCopperTrapdoor;
            for (int i = 26589; i <= 26652; i++)
                materials[i] = Material.WaxedCopperTrapdoor;
            for (int i = 26653; i <= 26716; i++)
                materials[i] = Material.WaxedExposedCopperTrapdoor;
            for (int i = 26717; i <= 26780; i++)
                materials[i] = Material.WaxedOxidizedCopperTrapdoor;
            for (int i = 26781; i <= 26844; i++)
                materials[i] = Material.WaxedWeatheredCopperTrapdoor;
            for (int i = 26845; i <= 26846; i++)
                materials[i] = Material.CopperGrate;
            for (int i = 26847; i <= 26848; i++)
                materials[i] = Material.ExposedCopperGrate;
            for (int i = 26849; i <= 26850; i++)
                materials[i] = Material.WeatheredCopperGrate;
            for (int i = 26851; i <= 26852; i++)
                materials[i] = Material.OxidizedCopperGrate;
            for (int i = 26853; i <= 26854; i++)
                materials[i] = Material.WaxedCopperGrate;
            for (int i = 26855; i <= 26856; i++)
                materials[i] = Material.WaxedExposedCopperGrate;
            for (int i = 26857; i <= 26858; i++)
                materials[i] = Material.WaxedWeatheredCopperGrate;
            for (int i = 26859; i <= 26860; i++)
                materials[i] = Material.WaxedOxidizedCopperGrate;
            for (int i = 26861; i <= 26864; i++)
                materials[i] = Material.CopperBulb;
            for (int i = 26865; i <= 26868; i++)
                materials[i] = Material.ExposedCopperBulb;
            for (int i = 26869; i <= 26872; i++)
                materials[i] = Material.WeatheredCopperBulb;
            for (int i = 26873; i <= 26876; i++)
                materials[i] = Material.OxidizedCopperBulb;
            for (int i = 26877; i <= 26880; i++)
                materials[i] = Material.WaxedCopperBulb;
            for (int i = 26881; i <= 26884; i++)
                materials[i] = Material.WaxedExposedCopperBulb;
            for (int i = 26885; i <= 26888; i++)
                materials[i] = Material.WaxedWeatheredCopperBulb;
            for (int i = 26889; i <= 26892; i++)
                materials[i] = Material.WaxedOxidizedCopperBulb;
            for (int i = 26893; i <= 26916; i++)
                materials[i] = Material.CopperChest;
            for (int i = 26917; i <= 26940; i++)
                materials[i] = Material.ExposedCopperChest;
            for (int i = 26941; i <= 26964; i++)
                materials[i] = Material.WeatheredCopperChest;
            for (int i = 26965; i <= 26988; i++)
                materials[i] = Material.OxidizedCopperChest;
            for (int i = 26989; i <= 27012; i++)
                materials[i] = Material.WaxedCopperChest;
            for (int i = 27013; i <= 27036; i++)
                materials[i] = Material.WaxedExposedCopperChest;
            for (int i = 27037; i <= 27060; i++)
                materials[i] = Material.WaxedWeatheredCopperChest;
            for (int i = 27061; i <= 27084; i++)
                materials[i] = Material.WaxedOxidizedCopperChest;
            for (int i = 27085; i <= 27116; i++)
                materials[i] = Material.CopperGolemStatue;
            for (int i = 27117; i <= 27148; i++)
                materials[i] = Material.ExposedCopperGolemStatue;
            for (int i = 27149; i <= 27180; i++)
                materials[i] = Material.WeatheredCopperGolemStatue;
            for (int i = 27181; i <= 27212; i++)
                materials[i] = Material.OxidizedCopperGolemStatue;
            for (int i = 27213; i <= 27244; i++)
                materials[i] = Material.WaxedCopperGolemStatue;
            for (int i = 27245; i <= 27276; i++)
                materials[i] = Material.WaxedExposedCopperGolemStatue;
            for (int i = 27277; i <= 27308; i++)
                materials[i] = Material.WaxedWeatheredCopperGolemStatue;
            for (int i = 27309; i <= 27340; i++)
                materials[i] = Material.WaxedOxidizedCopperGolemStatue;
            for (int i = 27341; i <= 27364; i++)
                materials[i] = Material.LightningRod;
            for (int i = 27365; i <= 27388; i++)
                materials[i] = Material.ExposedLightningRod;
            for (int i = 27389; i <= 27412; i++)
                materials[i] = Material.WeatheredLightningRod;
            for (int i = 27413; i <= 27436; i++)
                materials[i] = Material.OxidizedLightningRod;
            for (int i = 27437; i <= 27460; i++)
                materials[i] = Material.WaxedLightningRod;
            for (int i = 27461; i <= 27484; i++)
                materials[i] = Material.WaxedExposedLightningRod;
            for (int i = 27485; i <= 27508; i++)
                materials[i] = Material.WaxedWeatheredLightningRod;
            for (int i = 27509; i <= 27532; i++)
                materials[i] = Material.WaxedOxidizedLightningRod;
            for (int i = 27533; i <= 27552; i++)
                materials[i] = Material.PointedDripstone;
            for (int i = 27553; i <= 27553; i++)
                materials[i] = Material.DripstoneBlock;
            for (int i = 27554; i <= 27605; i++)
                materials[i] = Material.CaveVines;
            for (int i = 27606; i <= 27607; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 27608; i <= 27608; i++)
                materials[i] = Material.SporeBlossom;
            for (int i = 27609; i <= 27609; i++)
                materials[i] = Material.Azalea;
            for (int i = 27610; i <= 27610; i++)
                materials[i] = Material.FloweringAzalea;
            for (int i = 27611; i <= 27611; i++)
                materials[i] = Material.MossCarpet;
            for (int i = 27612; i <= 27627; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 27628; i <= 27643; i++)
                materials[i] = Material.Wildflowers;
            for (int i = 27644; i <= 27659; i++)
                materials[i] = Material.LeafLitter;
            for (int i = 27660; i <= 27660; i++)
                materials[i] = Material.MossBlock;
            for (int i = 27661; i <= 27692; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 27693; i <= 27700; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 27701; i <= 27716; i++)
                materials[i] = Material.SmallDripleaf;
            for (int i = 27717; i <= 27718; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 27719; i <= 27719; i++)
                materials[i] = Material.RootedDirt;
            for (int i = 27720; i <= 27720; i++)
                materials[i] = Material.Mud;
            for (int i = 27721; i <= 27723; i++)
                materials[i] = Material.Deepslate;
            for (int i = 27724; i <= 27724; i++)
                materials[i] = Material.CobbledDeepslate;
            for (int i = 27725; i <= 27804; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 27805; i <= 27810; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 27811; i <= 28134; i++)
                materials[i] = Material.CobbledDeepslateWall;
            for (int i = 28135; i <= 28135; i++)
                materials[i] = Material.PolishedDeepslate;
            for (int i = 28136; i <= 28215; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 28216; i <= 28221; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 28222; i <= 28545; i++)
                materials[i] = Material.PolishedDeepslateWall;
            for (int i = 28546; i <= 28546; i++)
                materials[i] = Material.DeepslateTiles;
            for (int i = 28547; i <= 28626; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 28627; i <= 28632; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 28633; i <= 28956; i++)
                materials[i] = Material.DeepslateTileWall;
            for (int i = 28957; i <= 28957; i++)
                materials[i] = Material.DeepslateBricks;
            for (int i = 28958; i <= 29037; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 29038; i <= 29043; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 29044; i <= 29367; i++)
                materials[i] = Material.DeepslateBrickWall;
            for (int i = 29368; i <= 29368; i++)
                materials[i] = Material.ChiseledDeepslate;
            for (int i = 29369; i <= 29369; i++)
                materials[i] = Material.CrackedDeepslateBricks;
            for (int i = 29370; i <= 29370; i++)
                materials[i] = Material.CrackedDeepslateTiles;
            for (int i = 29371; i <= 29373; i++)
                materials[i] = Material.InfestedDeepslate;
            for (int i = 29374; i <= 29374; i++)
                materials[i] = Material.SmoothBasalt;
            for (int i = 29375; i <= 29375; i++)
                materials[i] = Material.RawIronBlock;
            for (int i = 29376; i <= 29376; i++)
                materials[i] = Material.RawCopperBlock;
            for (int i = 29377; i <= 29377; i++)
                materials[i] = Material.RawGoldBlock;
            for (int i = 29378; i <= 29378; i++)
                materials[i] = Material.PottedAzaleaBush;
            for (int i = 29379; i <= 29379; i++)
                materials[i] = Material.PottedFloweringAzaleaBush;
            for (int i = 29380; i <= 29382; i++)
                materials[i] = Material.OchreFroglight;
            for (int i = 29383; i <= 29385; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 29386; i <= 29388; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 29389; i <= 29389; i++)
                materials[i] = Material.Frogspawn;
            for (int i = 29390; i <= 29390; i++)
                materials[i] = Material.ReinforcedDeepslate;
            for (int i = 29391; i <= 29406; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 29407; i <= 29454; i++)
                materials[i] = Material.Crafter;
            for (int i = 29455; i <= 29466; i++)
                materials[i] = Material.TrialSpawner;
            for (int i = 29467; i <= 29498; i++)
                materials[i] = Material.Vault;
            for (int i = 29499; i <= 29500; i++)
                materials[i] = Material.HeavyCore;
            for (int i = 29501; i <= 29501; i++)
                materials[i] = Material.PaleMossBlock;
            for (int i = 29502; i <= 29663; i++)
                materials[i] = Material.PaleMossCarpet;
            for (int i = 29664; i <= 29665; i++)
                materials[i] = Material.PaleHangingMoss;
            for (int i = 29666; i <= 29666; i++)
                materials[i] = Material.OpenEyeblossom;
            for (int i = 29667; i <= 29667; i++)
                materials[i] = Material.ClosedEyeblossom;
            for (int i = 29668; i <= 29668; i++)
                materials[i] = Material.PottedOpenEyeblossom;
            for (int i = 29669; i <= 29669; i++)
                materials[i] = Material.PottedClosedEyeblossom;
            for (int i = 29670; i <= 29670; i++)
                materials[i] = Material.FireflyBush;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
