using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette1219 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette1219()
        {
            for (int i = 10569; i <= 10592; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 14050; i <= 14113; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 13666; i <= 13697; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 13378; i <= 13409; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 5898; i <= 5961; i++)
                materials[i] = Material.AcaciaHangingSign;
            for (int i = 364; i <= 391; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 148; i <= 150; i++)
                materials[i] = Material.AcaciaLog;
            materials[19] = Material.AcaciaPlanks;
            for (int i = 6668; i <= 6669; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 37; i <= 38; i++)
                materials[i] = Material.AcaciaSapling;
            for (int i = 2399; i <= 2462; i++)
                materials[i] = Material.AcaciaShelf;
            for (int i = 5230; i <= 5261; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 13152; i <= 13157; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 11770; i <= 11849; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 7169; i <= 7232; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 6498; i <= 6505; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 5650; i <= 5657; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 213; i <= 215; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 11206; i <= 11229; i++)
                materials[i] = Material.ActivatorRail;
            materials[0] = Material.Air;
            materials[2125] = Material.Allium;
            materials[23200] = Material.AmethystBlock;
            for (int i = 23202; i <= 23213; i++)
                materials[i] = Material.AmethystCluster;
            materials[21617] = Material.AncientDebris;
            materials[6] = Material.Andesite;
            for (int i = 16268; i <= 16273; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 15894; i <= 15973; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 18884; i <= 19207; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 10993; i <= 10996; i++)
                materials[i] = Material.Anvil;
            for (int i = 8137; i <= 8140; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 8133; i <= 8136; i++)
                materials[i] = Material.AttachedPumpkinStem;
            materials[27609] = Material.Azalea;
            for (int i = 504; i <= 531; i++)
                materials[i] = Material.AzaleaLeaves;
            materials[2126] = Material.AzureBluet;
            for (int i = 15077; i <= 15088; i++)
                materials[i] = Material.Bamboo;
            for (int i = 168; i <= 170; i++)
                materials[i] = Material.BambooBlock;
            for (int i = 10689; i <= 10712; i++)
                materials[i] = Material.BambooButton;
            for (int i = 14370; i <= 14433; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 13826; i <= 13857; i++)
                materials[i] = Material.BambooFence;
            for (int i = 13538; i <= 13569; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 6410; i <= 6473; i++)
                materials[i] = Material.BambooHangingSign;
            materials[28] = Material.BambooMosaic;
            for (int i = 13188; i <= 13193; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 12250; i <= 12329; i++)
                materials[i] = Material.BambooMosaicStairs;
            materials[27] = Material.BambooPlanks;
            for (int i = 6678; i <= 6679; i++)
                materials[i] = Material.BambooPressurePlate;
            materials[15076] = Material.BambooSapling;
            for (int i = 2463; i <= 2526; i++)
                materials[i] = Material.BambooShelf;
            for (int i = 5422; i <= 5453; i++)
                materials[i] = Material.BambooSign;
            for (int i = 13182; i <= 13187; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 12170; i <= 12249; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 7489; i <= 7552; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 6562; i <= 6569; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 5698; i <= 5705; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 20540; i <= 20551; i++)
                materials[i] = Material.Barrel;
            for (int i = 12331; i <= 12332; i++)
                materials[i] = Material.Barrier;
            for (int i = 6799; i <= 6801; i++)
                materials[i] = Material.Basalt;
            materials[9779] = Material.Beacon;
            materials[85] = Material.Bedrock;
            for (int i = 21566; i <= 21589; i++)
                materials[i] = Material.BeeNest;
            for (int i = 21590; i <= 21613; i++)
                materials[i] = Material.Beehive;
            for (int i = 14609; i <= 14612; i++)
                materials[i] = Material.Beetroots;
            for (int i = 20603; i <= 20634; i++)
                materials[i] = Material.Bell;
            for (int i = 27661; i <= 27692; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 27693; i <= 27700; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 10521; i <= 10544; i++)
                materials[i] = Material.BirchButton;
            for (int i = 13922; i <= 13985; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 13602; i <= 13633; i++)
                materials[i] = Material.BirchFence;
            for (int i = 13314; i <= 13345; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 5834; i <= 5897; i++)
                materials[i] = Material.BirchHangingSign;
            for (int i = 308; i <= 335; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 142; i <= 144; i++)
                materials[i] = Material.BirchLog;
            materials[17] = Material.BirchPlanks;
            for (int i = 6664; i <= 6665; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 33; i <= 34; i++)
                materials[i] = Material.BirchSapling;
            for (int i = 2527; i <= 2590; i++)
                materials[i] = Material.BirchShelf;
            for (int i = 5198; i <= 5229; i++)
                materials[i] = Material.BirchSign;
            for (int i = 13140; i <= 13145; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 9607; i <= 9686; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 7041; i <= 7104; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 6490; i <= 6497; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 5642; i <= 5649; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 207; i <= 209; i++)
                materials[i] = Material.BirchWood;
            for (int i = 12965; i <= 12980; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 1971; i <= 1986; i++)
                materials[i] = Material.BlackBed;
            for (int i = 23150; i <= 23165; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 23198; i <= 23199; i++)
                materials[i] = Material.BlackCandleCake;
            materials[12709] = Material.BlackCarpet;
            materials[14843] = Material.BlackConcrete;
            materials[14859] = Material.BlackConcretePowder;
            for (int i = 14824; i <= 14827; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 14758; i <= 14763; i++)
                materials[i] = Material.BlackShulkerBox;
            materials[6912] = Material.BlackStainedGlass;
            for (int i = 11738; i <= 11769; i++)
                materials[i] = Material.BlackStainedGlassPane;
            materials[11257] = Material.BlackTerracotta;
            for (int i = 13041; i <= 13044; i++)
                materials[i] = Material.BlackWallBanner;
            materials[2108] = Material.BlackWool;
            materials[21629] = Material.Blackstone;
            for (int i = 22034; i <= 22039; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 21630; i <= 21709; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 21710; i <= 22033; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 20560; i <= 20567; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 12901; i <= 12916; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 1907; i <= 1922; i++)
                materials[i] = Material.BlueBed;
            for (int i = 23086; i <= 23101; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 23190; i <= 23191; i++)
                materials[i] = Material.BlueCandleCake;
            materials[12705] = Material.BlueCarpet;
            materials[14839] = Material.BlueConcrete;
            materials[14855] = Material.BlueConcretePowder;
            for (int i = 14808; i <= 14811; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            materials[15073] = Material.BlueIce;
            materials[2124] = Material.BlueOrchid;
            for (int i = 14734; i <= 14739; i++)
                materials[i] = Material.BlueShulkerBox;
            materials[6908] = Material.BlueStainedGlass;
            for (int i = 11610; i <= 11641; i++)
                materials[i] = Material.BlueStainedGlassPane;
            materials[11253] = Material.BlueTerracotta;
            for (int i = 13025; i <= 13028; i++)
                materials[i] = Material.BlueWallBanner;
            materials[2104] = Material.BlueWool;
            for (int i = 14646; i <= 14648; i++)
                materials[i] = Material.BoneBlock;
            materials[2142] = Material.Bookshelf;
            for (int i = 14957; i <= 14958; i++)
                materials[i] = Material.BrainCoral;
            materials[14941] = Material.BrainCoralBlock;
            for (int i = 14977; i <= 14978; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 15033; i <= 15040; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 9251; i <= 9258; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 13230; i <= 13235; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 8477; i <= 8556; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 16292; i <= 16615; i++)
                materials[i] = Material.BrickWall;
            materials[2139] = Material.Bricks;
            for (int i = 12917; i <= 12932; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 1923; i <= 1938; i++)
                materials[i] = Material.BrownBed;
            for (int i = 23102; i <= 23117; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 23192; i <= 23193; i++)
                materials[i] = Material.BrownCandleCake;
            materials[12706] = Material.BrownCarpet;
            materials[14840] = Material.BrownConcrete;
            materials[14856] = Material.BrownConcretePowder;
            for (int i = 14812; i <= 14815; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            materials[2135] = Material.BrownMushroom;
            for (int i = 7565; i <= 7628; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 14740; i <= 14745; i++)
                materials[i] = Material.BrownShulkerBox;
            materials[6909] = Material.BrownStainedGlass;
            for (int i = 11642; i <= 11673; i++)
                materials[i] = Material.BrownStainedGlassPane;
            materials[11254] = Material.BrownTerracotta;
            for (int i = 13029; i <= 13032; i++)
                materials[i] = Material.BrownWallBanner;
            materials[2105] = Material.BrownWool;
            for (int i = 15092; i <= 15093; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 14959; i <= 14960; i++)
                materials[i] = Material.BubbleCoral;
            materials[14942] = Material.BubbleCoralBlock;
            for (int i = 14979; i <= 14980; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 15041; i <= 15048; i++)
                materials[i] = Material.BubbleCoralWallFan;
            materials[23201] = Material.BuddingAmethyst;
            materials[2051] = Material.Bush;
            for (int i = 6728; i <= 6743; i++)
                materials[i] = Material.Cactus;
            materials[6744] = Material.CactusFlower;
            for (int i = 6826; i <= 6832; i++)
                materials[i] = Material.Cake;
            materials[24485] = Material.Calcite;
            for (int i = 24584; i <= 24967; i++)
                materials[i] = Material.CalibratedSculkSensor;
            for (int i = 20675; i <= 20706; i++)
                materials[i] = Material.Campfire;
            for (int i = 22894; i <= 22909; i++)
                materials[i] = Material.Candle;
            for (int i = 23166; i <= 23167; i++)
                materials[i] = Material.CandleCake;
            for (int i = 10457; i <= 10464; i++)
                materials[i] = Material.Carrots;
            materials[20568] = Material.CartographyTable;
            for (int i = 6818; i <= 6821; i++)
                materials[i] = Material.CarvedPumpkin;
            materials[9259] = Material.Cauldron;
            materials[15091] = Material.CaveAir;
            for (int i = 27554; i <= 27605; i++)
                materials[i] = Material.CaveVines;
            for (int i = 27606; i <= 27607; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 14627; i <= 14638; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 10593; i <= 10616; i++)
                materials[i] = Material.CherryButton;
            for (int i = 14114; i <= 14177; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 13698; i <= 13729; i++)
                materials[i] = Material.CherryFence;
            for (int i = 13410; i <= 13441; i++)
                materials[i] = Material.CherryFenceGate;
            for (int i = 5962; i <= 6025; i++)
                materials[i] = Material.CherryHangingSign;
            for (int i = 392; i <= 419; i++)
                materials[i] = Material.CherryLeaves;
            for (int i = 151; i <= 153; i++)
                materials[i] = Material.CherryLog;
            materials[20] = Material.CherryPlanks;
            for (int i = 6670; i <= 6671; i++)
                materials[i] = Material.CherryPressurePlate;
            for (int i = 39; i <= 40; i++)
                materials[i] = Material.CherrySapling;
            for (int i = 2591; i <= 2654; i++)
                materials[i] = Material.CherryShelf;
            for (int i = 5262; i <= 5293; i++)
                materials[i] = Material.CherrySign;
            for (int i = 13158; i <= 13163; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 11850; i <= 11929; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 7233; i <= 7296; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 6506; i <= 6513; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 5658; i <= 5665; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 216; i <= 218; i++)
                materials[i] = Material.CherryWood;
            for (int i = 3786; i <= 3809; i++)
                materials[i] = Material.Chest;
            for (int i = 10997; i <= 11000; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 2143; i <= 2398; i++)
                materials[i] = Material.ChiseledBookshelf;
            materials[25120] = Material.ChiseledCopper;
            materials[29368] = Material.ChiseledDeepslate;
            materials[22891] = Material.ChiseledNetherBricks;
            materials[22043] = Material.ChiseledPolishedBlackstone;
            materials[11122] = Material.ChiseledQuartzBlock;
            materials[13046] = Material.ChiseledRedSandstone;
            materials[9132] = Material.ChiseledResinBricks;
            materials[579] = Material.ChiseledSandstone;
            materials[7556] = Material.ChiseledStoneBricks;
            materials[24072] = Material.ChiseledTuff;
            materials[24484] = Material.ChiseledTuffBricks;
            for (int i = 14504; i <= 14509; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 14440; i <= 14503; i++)
                materials[i] = Material.ChorusPlant;
            materials[6745] = Material.Clay;
            materials[29667] = Material.ClosedEyeblossom;
            materials[12711] = Material.CoalBlock;
            materials[133] = Material.CoalOre;
            materials[11] = Material.CoarseDirt;
            materials[27724] = Material.CobbledDeepslate;
            for (int i = 27805; i <= 27810; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 27725; i <= 27804; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 27811; i <= 28134; i++)
                materials[i] = Material.CobbledDeepslateWall;
            materials[14] = Material.Cobblestone;
            for (int i = 13224; i <= 13229; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 5546; i <= 5625; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 9780; i <= 10103; i++)
                materials[i] = Material.CobblestoneWall;
            materials[2047] = Material.Cobweb;
            for (int i = 9280; i <= 9291; i++)
                materials[i] = Material.Cocoa;
            for (int i = 9767; i <= 9778; i++)
                materials[i] = Material.CommandBlock;
            for (int i = 11061; i <= 11076; i++)
                materials[i] = Material.Comparator;
            for (int i = 21541; i <= 21549; i++)
                materials[i] = Material.Composter;
            for (int i = 15074; i <= 15075; i++)
                materials[i] = Material.Conduit;
            for (int i = 7789; i <= 7820; i++)
                materials[i] = Material.CopperBars;
            materials[25107] = Material.CopperBlock;
            for (int i = 26861; i <= 26864; i++)
                materials[i] = Material.CopperBulb;
            for (int i = 8051; i <= 8056; i++)
                materials[i] = Material.CopperChain;
            for (int i = 26893; i <= 26916; i++)
                materials[i] = Material.CopperChest;
            for (int i = 25821; i <= 25884; i++)
                materials[i] = Material.CopperDoor;
            for (int i = 27085; i <= 27116; i++)
                materials[i] = Material.CopperGolemStatue;
            for (int i = 26845; i <= 26846; i++)
                materials[i] = Material.CopperGrate;
            for (int i = 20643; i <= 20646; i++)
                materials[i] = Material.CopperLantern;
            materials[25111] = Material.CopperOre;
            materials[6810] = Material.CopperTorch;
            for (int i = 26333; i <= 26396; i++)
                materials[i] = Material.CopperTrapdoor;
            for (int i = 6811; i <= 6814; i++)
                materials[i] = Material.CopperWallTorch;
            materials[2132] = Material.Cornflower;
            materials[29369] = Material.CrackedDeepslateBricks;
            materials[29370] = Material.CrackedDeepslateTiles;
            materials[22892] = Material.CrackedNetherBricks;
            materials[22042] = Material.CrackedPolishedBlackstoneBricks;
            materials[7555] = Material.CrackedStoneBricks;
            for (int i = 29407; i <= 29454; i++)
                materials[i] = Material.Crafter;
            materials[5109] = Material.CraftingTable;
            for (int i = 3688; i <= 3705; i++)
                materials[i] = Material.CreakingHeart;
            for (int i = 10873; i <= 10904; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 10905; i <= 10912; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 21264; i <= 21287; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 21312; i <= 21375; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 20848; i <= 20879; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 21040; i <= 21071; i++)
                materials[i] = Material.CrimsonFenceGate;
            materials[20773] = Material.CrimsonFungus;
            for (int i = 6218; i <= 6281; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 20766; i <= 20768; i++)
                materials[i] = Material.CrimsonHyphae;
            materials[20772] = Material.CrimsonNylium;
            materials[20830] = Material.CrimsonPlanks;
            for (int i = 20844; i <= 20845; i++)
                materials[i] = Material.CrimsonPressurePlate;
            materials[20829] = Material.CrimsonRoots;
            for (int i = 2655; i <= 2718; i++)
                materials[i] = Material.CrimsonShelf;
            for (int i = 21440; i <= 21471; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 20832; i <= 20837; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 21104; i <= 21183; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 20760; i <= 20762; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 20912; i <= 20975; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 6546; i <= 6553; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 21504; i <= 21511; i++)
                materials[i] = Material.CrimsonWallSign;
            materials[21618] = Material.CryingObsidian;
            materials[25116] = Material.CutCopper;
            for (int i = 25463; i <= 25468; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 25365; i <= 25444; i++)
                materials[i] = Material.CutCopperStairs;
            materials[13047] = Material.CutRedSandstone;
            for (int i = 13266; i <= 13271; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            materials[580] = Material.CutSandstone;
            for (int i = 13212; i <= 13217; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 12869; i <= 12884; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 1875; i <= 1890; i++)
                materials[i] = Material.CyanBed;
            for (int i = 23054; i <= 23069; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 23186; i <= 23187; i++)
                materials[i] = Material.CyanCandleCake;
            materials[12703] = Material.CyanCarpet;
            materials[14837] = Material.CyanConcrete;
            materials[14853] = Material.CyanConcretePowder;
            for (int i = 14800; i <= 14803; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 14722; i <= 14727; i++)
                materials[i] = Material.CyanShulkerBox;
            materials[6906] = Material.CyanStainedGlass;
            for (int i = 11546; i <= 11577; i++)
                materials[i] = Material.CyanStainedGlassPane;
            materials[11251] = Material.CyanTerracotta;
            for (int i = 13017; i <= 13020; i++)
                materials[i] = Material.CyanWallBanner;
            materials[2102] = Material.CyanWool;
            for (int i = 11001; i <= 11004; i++)
                materials[i] = Material.DamagedAnvil;
            materials[2121] = Material.Dandelion;
            for (int i = 10617; i <= 10640; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 14178; i <= 14241; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 13730; i <= 13761; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 13442; i <= 13473; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 6090; i <= 6153; i++)
                materials[i] = Material.DarkOakHangingSign;
            for (int i = 420; i <= 447; i++)
                materials[i] = Material.DarkOakLeaves;
            for (int i = 154; i <= 156; i++)
                materials[i] = Material.DarkOakLog;
            materials[21] = Material.DarkOakPlanks;
            for (int i = 6672; i <= 6673; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 41; i <= 42; i++)
                materials[i] = Material.DarkOakSapling;
            for (int i = 2719; i <= 2782; i++)
                materials[i] = Material.DarkOakShelf;
            for (int i = 5326; i <= 5357; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 13164; i <= 13169; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 11930; i <= 12009; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 7297; i <= 7360; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 6522; i <= 6529; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 5674; i <= 5681; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 219; i <= 221; i++)
                materials[i] = Material.DarkOakWood;
            materials[12431] = Material.DarkPrismarine;
            for (int i = 12684; i <= 12689; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 12592; i <= 12671; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 11077; i <= 11108; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 14947; i <= 14948; i++)
                materials[i] = Material.DeadBrainCoral;
            materials[14936] = Material.DeadBrainCoralBlock;
            for (int i = 14967; i <= 14968; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 14993; i <= 15000; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 14949; i <= 14950; i++)
                materials[i] = Material.DeadBubbleCoral;
            materials[14937] = Material.DeadBubbleCoralBlock;
            for (int i = 14969; i <= 14970; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 15001; i <= 15008; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            materials[2050] = Material.DeadBush;
            for (int i = 14951; i <= 14952; i++)
                materials[i] = Material.DeadFireCoral;
            materials[14938] = Material.DeadFireCoralBlock;
            for (int i = 14971; i <= 14972; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 15009; i <= 15016; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 14953; i <= 14954; i++)
                materials[i] = Material.DeadHornCoral;
            materials[14939] = Material.DeadHornCoralBlock;
            for (int i = 14973; i <= 14974; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 15017; i <= 15024; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 14945; i <= 14946; i++)
                materials[i] = Material.DeadTubeCoral;
            materials[14935] = Material.DeadTubeCoralBlock;
            for (int i = 14965; i <= 14966; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 14985; i <= 14992; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 29391; i <= 29406; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 27721; i <= 27723; i++)
                materials[i] = Material.Deepslate;
            for (int i = 29038; i <= 29043; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 28958; i <= 29037; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 29044; i <= 29367; i++)
                materials[i] = Material.DeepslateBrickWall;
            materials[28957] = Material.DeepslateBricks;
            materials[134] = Material.DeepslateCoalOre;
            materials[25112] = Material.DeepslateCopperOre;
            materials[5107] = Material.DeepslateDiamondOre;
            materials[9373] = Material.DeepslateEmeraldOre;
            materials[130] = Material.DeepslateGoldOre;
            materials[132] = Material.DeepslateIronOre;
            materials[564] = Material.DeepslateLapisOre;
            for (int i = 6682; i <= 6683; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 28627; i <= 28632; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 28547; i <= 28626; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 28633; i <= 28956; i++)
                materials[i] = Material.DeepslateTileWall;
            materials[28546] = Material.DeepslateTiles;
            for (int i = 2011; i <= 2034; i++)
                materials[i] = Material.DetectorRail;
            materials[5108] = Material.DiamondBlock;
            materials[5106] = Material.DiamondOre;
            materials[4] = Material.Diorite;
            for (int i = 16286; i <= 16291; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 16134; i <= 16213; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 20180; i <= 20503; i++)
                materials[i] = Material.DioriteWall;
            materials[10] = Material.Dirt;
            materials[14613] = Material.DirtPath;
            for (int i = 566; i <= 577; i++)
                materials[i] = Material.Dispenser;
            materials[9277] = Material.DragonEgg;
            for (int i = 10913; i <= 10944; i++)
                materials[i] = Material.DragonHead;
            for (int i = 10945; i <= 10952; i++)
                materials[i] = Material.DragonWallHead;
            for (int i = 14903; i <= 14934; i++)
                materials[i] = Material.DriedGhast;
            materials[14887] = Material.DriedKelpBlock;
            materials[27553] = Material.DripstoneBlock;
            for (int i = 11230; i <= 11241; i++)
                materials[i] = Material.Dropper;
            materials[9526] = Material.EmeraldBlock;
            materials[9372] = Material.EmeraldOre;
            materials[9250] = Material.EnchantingTable;
            materials[14614] = Material.EndGateway;
            materials[9267] = Material.EndPortal;
            for (int i = 9268; i <= 9275; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 14434; i <= 14439; i++)
                materials[i] = Material.EndRod;
            materials[9276] = Material.EndStone;
            for (int i = 16244; i <= 16249; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 15494; i <= 15573; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 19856; i <= 20179; i++)
                materials[i] = Material.EndStoneBrickWall;
            materials[14594] = Material.EndStoneBricks;
            for (int i = 9374; i <= 9381; i++)
                materials[i] = Material.EnderChest;
            materials[25119] = Material.ExposedChiseledCopper;
            materials[25108] = Material.ExposedCopper;
            for (int i = 7821; i <= 7852; i++)
                materials[i] = Material.ExposedCopperBars;
            for (int i = 26865; i <= 26868; i++)
                materials[i] = Material.ExposedCopperBulb;
            for (int i = 8057; i <= 8062; i++)
                materials[i] = Material.ExposedCopperChain;
            for (int i = 26917; i <= 26940; i++)
                materials[i] = Material.ExposedCopperChest;
            for (int i = 25885; i <= 25948; i++)
                materials[i] = Material.ExposedCopperDoor;
            for (int i = 27117; i <= 27148; i++)
                materials[i] = Material.ExposedCopperGolemStatue;
            for (int i = 26847; i <= 26848; i++)
                materials[i] = Material.ExposedCopperGrate;
            for (int i = 20647; i <= 20650; i++)
                materials[i] = Material.ExposedCopperLantern;
            for (int i = 26397; i <= 26460; i++)
                materials[i] = Material.ExposedCopperTrapdoor;
            materials[25115] = Material.ExposedCutCopper;
            for (int i = 25457; i <= 25462; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 25285; i <= 25364; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 27365; i <= 27388; i++)
                materials[i] = Material.ExposedLightningRod;
            for (int i = 5118; i <= 5125; i++)
                materials[i] = Material.Farmland;
            materials[2049] = Material.Fern;
            for (int i = 3174; i <= 3685; i++)
                materials[i] = Material.Fire;
            for (int i = 14961; i <= 14962; i++)
                materials[i] = Material.FireCoral;
            materials[14943] = Material.FireCoralBlock;
            for (int i = 14981; i <= 14982; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 15049; i <= 15056; i++)
                materials[i] = Material.FireCoralWallFan;
            materials[29670] = Material.FireflyBush;
            materials[20569] = Material.FletchingTable;
            materials[10428] = Material.FlowerPot;
            materials[27610] = Material.FloweringAzalea;
            for (int i = 532; i <= 559; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            materials[29389] = Material.Frogspawn;
            for (int i = 14639; i <= 14642; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 5126; i <= 5133; i++)
                materials[i] = Material.Furnace;
            materials[22454] = Material.GildedBlackstone;
            materials[562] = Material.Glass;
            for (int i = 8099; i <= 8130; i++)
                materials[i] = Material.GlassPane;
            for (int i = 8189; i <= 8316; i++)
                materials[i] = Material.GlowLichen;
            materials[6815] = Material.Glowstone;
            materials[2137] = Material.GoldBlock;
            materials[129] = Material.GoldOre;
            materials[2] = Material.Granite;
            for (int i = 16262; i <= 16267; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 15814; i <= 15893; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 17588; i <= 17911; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            materials[124] = Material.Gravel;
            for (int i = 12837; i <= 12852; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 1843; i <= 1858; i++)
                materials[i] = Material.GrayBed;
            for (int i = 23022; i <= 23037; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 23182; i <= 23183; i++)
                materials[i] = Material.GrayCandleCake;
            materials[12701] = Material.GrayCarpet;
            materials[14835] = Material.GrayConcrete;
            materials[14851] = Material.GrayConcretePowder;
            for (int i = 14792; i <= 14795; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 14710; i <= 14715; i++)
                materials[i] = Material.GrayShulkerBox;
            materials[6904] = Material.GrayStainedGlass;
            for (int i = 11482; i <= 11513; i++)
                materials[i] = Material.GrayStainedGlassPane;
            materials[11249] = Material.GrayTerracotta;
            for (int i = 13009; i <= 13012; i++)
                materials[i] = Material.GrayWallBanner;
            materials[2100] = Material.GrayWool;
            for (int i = 12933; i <= 12948; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 1939; i <= 1954; i++)
                materials[i] = Material.GreenBed;
            for (int i = 23118; i <= 23133; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 23194; i <= 23195; i++)
                materials[i] = Material.GreenCandleCake;
            materials[12707] = Material.GreenCarpet;
            materials[14841] = Material.GreenConcrete;
            materials[14857] = Material.GreenConcretePowder;
            for (int i = 14816; i <= 14819; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 14746; i <= 14751; i++)
                materials[i] = Material.GreenShulkerBox;
            materials[6910] = Material.GreenStainedGlass;
            for (int i = 11674; i <= 11705; i++)
                materials[i] = Material.GreenStainedGlassPane;
            materials[11255] = Material.GreenTerracotta;
            for (int i = 13033; i <= 13036; i++)
                materials[i] = Material.GreenWallBanner;
            materials[2106] = Material.GreenWool;
            for (int i = 20570; i <= 20581; i++)
                materials[i] = Material.Grindstone;
            for (int i = 27717; i <= 27718; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 12691; i <= 12693; i++)
                materials[i] = Material.HayBlock;
            for (int i = 29499; i <= 29500; i++)
                materials[i] = Material.HeavyCore;
            for (int i = 11045; i <= 11060; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            materials[21614] = Material.HoneyBlock;
            materials[21615] = Material.HoneycombBlock;
            for (int i = 11111; i <= 11120; i++)
                materials[i] = Material.Hopper;
            for (int i = 14963; i <= 14964; i++)
                materials[i] = Material.HornCoral;
            materials[14944] = Material.HornCoralBlock;
            for (int i = 14983; i <= 14984; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 15057; i <= 15064; i++)
                materials[i] = Material.HornCoralWallFan;
            materials[6726] = Material.Ice;
            materials[7564] = Material.InfestedChiseledStoneBricks;
            materials[7560] = Material.InfestedCobblestone;
            materials[7563] = Material.InfestedCrackedStoneBricks;
            for (int i = 29371; i <= 29373; i++)
                materials[i] = Material.InfestedDeepslate;
            materials[7562] = Material.InfestedMossyStoneBricks;
            materials[7559] = Material.InfestedStone;
            materials[7561] = Material.InfestedStoneBricks;
            for (int i = 7757; i <= 7788; i++)
                materials[i] = Material.IronBars;
            materials[2138] = Material.IronBlock;
            for (int i = 8045; i <= 8050; i++)
                materials[i] = Material.IronChain;
            for (int i = 6596; i <= 6659; i++)
                materials[i] = Material.IronDoor;
            materials[131] = Material.IronOre;
            for (int i = 12365; i <= 12428; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 6822; i <= 6825; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 21524; i <= 21535; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 6762; i <= 6763; i++)
                materials[i] = Material.Jukebox;
            for (int i = 10545; i <= 10568; i++)
                materials[i] = Material.JungleButton;
            for (int i = 13986; i <= 14049; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 13634; i <= 13665; i++)
                materials[i] = Material.JungleFence;
            for (int i = 13346; i <= 13377; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 6026; i <= 6089; i++)
                materials[i] = Material.JungleHangingSign;
            for (int i = 336; i <= 363; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 145; i <= 147; i++)
                materials[i] = Material.JungleLog;
            materials[18] = Material.JunglePlanks;
            for (int i = 6666; i <= 6667; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 35; i <= 36; i++)
                materials[i] = Material.JungleSapling;
            for (int i = 2783; i <= 2846; i++)
                materials[i] = Material.JungleShelf;
            for (int i = 5294; i <= 5325; i++)
                materials[i] = Material.JungleSign;
            for (int i = 13146; i <= 13151; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 9687; i <= 9766; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 7105; i <= 7168; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 6514; i <= 6521; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 5666; i <= 5673; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 210; i <= 212; i++)
                materials[i] = Material.JungleWood;
            for (int i = 14860; i <= 14885; i++)
                materials[i] = Material.Kelp;
            materials[14886] = Material.KelpPlant;
            for (int i = 5518; i <= 5525; i++)
                materials[i] = Material.Ladder;
            for (int i = 20635; i <= 20638; i++)
                materials[i] = Material.Lantern;
            materials[565] = Material.LapisBlock;
            materials[563] = Material.LapisOre;
            for (int i = 23214; i <= 23225; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 12723; i <= 12724; i++)
                materials[i] = Material.LargeFern;
            for (int i = 102; i <= 117; i++)
                materials[i] = Material.Lava;
            materials[9263] = Material.LavaCauldron;
            for (int i = 27644; i <= 27659; i++)
                materials[i] = Material.LeafLitter;
            for (int i = 20582; i <= 20597; i++)
                materials[i] = Material.Lectern;
            for (int i = 6570; i <= 6593; i++)
                materials[i] = Material.Lever;
            for (int i = 12333; i <= 12364; i++)
                materials[i] = Material.Light;
            for (int i = 12773; i <= 12788; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 1779; i <= 1794; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 22958; i <= 22973; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 23174; i <= 23175; i++)
                materials[i] = Material.LightBlueCandleCake;
            materials[12697] = Material.LightBlueCarpet;
            materials[14831] = Material.LightBlueConcrete;
            materials[14847] = Material.LightBlueConcretePowder;
            for (int i = 14776; i <= 14779; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 14686; i <= 14691; i++)
                materials[i] = Material.LightBlueShulkerBox;
            materials[6900] = Material.LightBlueStainedGlass;
            for (int i = 11354; i <= 11385; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            materials[11245] = Material.LightBlueTerracotta;
            for (int i = 12993; i <= 12996; i++)
                materials[i] = Material.LightBlueWallBanner;
            materials[2096] = Material.LightBlueWool;
            for (int i = 12853; i <= 12868; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 1859; i <= 1874; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 23038; i <= 23053; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 23184; i <= 23185; i++)
                materials[i] = Material.LightGrayCandleCake;
            materials[12702] = Material.LightGrayCarpet;
            materials[14836] = Material.LightGrayConcrete;
            materials[14852] = Material.LightGrayConcretePowder;
            for (int i = 14796; i <= 14799; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 14716; i <= 14721; i++)
                materials[i] = Material.LightGrayShulkerBox;
            materials[6905] = Material.LightGrayStainedGlass;
            for (int i = 11514; i <= 11545; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            materials[11250] = Material.LightGrayTerracotta;
            for (int i = 13013; i <= 13016; i++)
                materials[i] = Material.LightGrayWallBanner;
            materials[2101] = Material.LightGrayWool;
            for (int i = 11029; i <= 11044; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 27341; i <= 27364; i++)
                materials[i] = Material.LightningRod;
            for (int i = 12715; i <= 12716; i++)
                materials[i] = Material.Lilac;
            materials[2134] = Material.LilyOfTheValley;
            materials[8719] = Material.LilyPad;
            for (int i = 12805; i <= 12820; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 1811; i <= 1826; i++)
                materials[i] = Material.LimeBed;
            for (int i = 22990; i <= 23005; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 23178; i <= 23179; i++)
                materials[i] = Material.LimeCandleCake;
            materials[12699] = Material.LimeCarpet;
            materials[14833] = Material.LimeConcrete;
            materials[14849] = Material.LimeConcretePowder;
            for (int i = 14784; i <= 14787; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 14698; i <= 14703; i++)
                materials[i] = Material.LimeShulkerBox;
            materials[6902] = Material.LimeStainedGlass;
            for (int i = 11418; i <= 11449; i++)
                materials[i] = Material.LimeStainedGlassPane;
            materials[11247] = Material.LimeTerracotta;
            for (int i = 13001; i <= 13004; i++)
                materials[i] = Material.LimeWallBanner;
            materials[2098] = Material.LimeWool;
            materials[21628] = Material.Lodestone;
            for (int i = 20536; i <= 20539; i++)
                materials[i] = Material.Loom;
            for (int i = 12757; i <= 12772; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 1763; i <= 1778; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 22942; i <= 22957; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 23172; i <= 23173; i++)
                materials[i] = Material.MagentaCandleCake;
            materials[12696] = Material.MagentaCarpet;
            materials[14830] = Material.MagentaConcrete;
            materials[14846] = Material.MagentaConcretePowder;
            for (int i = 14772; i <= 14775; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 14680; i <= 14685; i++)
                materials[i] = Material.MagentaShulkerBox;
            materials[6899] = Material.MagentaStainedGlass;
            for (int i = 11322; i <= 11353; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            materials[11244] = Material.MagentaTerracotta;
            for (int i = 12989; i <= 12992; i++)
                materials[i] = Material.MagentaWallBanner;
            materials[2095] = Material.MagentaWool;
            materials[14643] = Material.MagmaBlock;
            for (int i = 10665; i <= 10688; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 14306; i <= 14369; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 13794; i <= 13825; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 13506; i <= 13537; i++)
                materials[i] = Material.MangroveFenceGate;
            for (int i = 6346; i <= 6409; i++)
                materials[i] = Material.MangroveHangingSign;
            for (int i = 476; i <= 503; i++)
                materials[i] = Material.MangroveLeaves;
            for (int i = 160; i <= 162; i++)
                materials[i] = Material.MangroveLog;
            materials[26] = Material.MangrovePlanks;
            for (int i = 6676; i <= 6677; i++)
                materials[i] = Material.MangrovePressurePlate;
            for (int i = 45; i <= 84; i++)
                materials[i] = Material.MangrovePropagule;
            for (int i = 163; i <= 164; i++)
                materials[i] = Material.MangroveRoots;
            for (int i = 2847; i <= 2910; i++)
                materials[i] = Material.MangroveShelf;
            for (int i = 5390; i <= 5421; i++)
                materials[i] = Material.MangroveSign;
            for (int i = 13176; i <= 13181; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 12090; i <= 12169; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 7425; i <= 7488; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 6538; i <= 6545; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 5690; i <= 5697; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 222; i <= 224; i++)
                materials[i] = Material.MangroveWood;
            for (int i = 23226; i <= 23237; i++)
                materials[i] = Material.MediumAmethystBud;
            materials[8132] = Material.Melon;
            for (int i = 8149; i <= 8156; i++)
                materials[i] = Material.MelonStem;
            materials[27660] = Material.MossBlock;
            materials[27611] = Material.MossCarpet;
            materials[3167] = Material.MossyCobblestone;
            for (int i = 16238; i <= 16243; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 15414; i <= 15493; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 10104; i <= 10427; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 16226; i <= 16231; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 15254; i <= 15333; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 17264; i <= 17587; i++)
                materials[i] = Material.MossyStoneBrickWall;
            materials[7554] = Material.MossyStoneBricks;
            for (int i = 2109; i <= 2120; i++)
                materials[i] = Material.MovingPiston;
            materials[27720] = Material.Mud;
            for (int i = 13242; i <= 13247; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 8637; i <= 8716; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 18236; i <= 18559; i++)
                materials[i] = Material.MudBrickWall;
            materials[7558] = Material.MudBricks;
            for (int i = 165; i <= 167; i++)
                materials[i] = Material.MuddyMangroveRoots;
            for (int i = 7693; i <= 7756; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 8717; i <= 8718; i++)
                materials[i] = Material.Mycelium;
            for (int i = 9134; i <= 9165; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 13248; i <= 13253; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 9166; i <= 9245; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 18560; i <= 18883; i++)
                materials[i] = Material.NetherBrickWall;
            materials[9133] = Material.NetherBricks;
            materials[135] = Material.NetherGoldOre;
            for (int i = 6816; i <= 6817; i++)
                materials[i] = Material.NetherPortal;
            materials[11110] = Material.NetherQuartzOre;
            materials[20759] = Material.NetherSprouts;
            for (int i = 9246; i <= 9249; i++)
                materials[i] = Material.NetherWart;
            materials[14644] = Material.NetherWartBlock;
            materials[21616] = Material.NetheriteBlock;
            materials[6796] = Material.Netherrack;
            for (int i = 581; i <= 1730; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 10473; i <= 10496; i++)
                materials[i] = Material.OakButton;
            for (int i = 5454; i <= 5517; i++)
                materials[i] = Material.OakDoor;
            for (int i = 6764; i <= 6795; i++)
                materials[i] = Material.OakFence;
            for (int i = 8445; i <= 8476; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 5706; i <= 5769; i++)
                materials[i] = Material.OakHangingSign;
            for (int i = 252; i <= 279; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 136; i <= 138; i++)
                materials[i] = Material.OakLog;
            materials[15] = Material.OakPlanks;
            for (int i = 6660; i <= 6661; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 29; i <= 30; i++)
                materials[i] = Material.OakSapling;
            for (int i = 2911; i <= 2974; i++)
                materials[i] = Material.OakShelf;
            for (int i = 5134; i <= 5165; i++)
                materials[i] = Material.OakSign;
            for (int i = 13128; i <= 13133; i++)
                materials[i] = Material.OakSlab;
            for (int i = 3706; i <= 3785; i++)
                materials[i] = Material.OakStairs;
            for (int i = 6913; i <= 6976; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 6474; i <= 6481; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 5626; i <= 5633; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 201; i <= 203; i++)
                materials[i] = Material.OakWood;
            for (int i = 14650; i <= 14661; i++)
                materials[i] = Material.Observer;
            materials[3168] = Material.Obsidian;
            for (int i = 29380; i <= 29382; i++)
                materials[i] = Material.OchreFroglight;
            materials[29666] = Material.OpenEyeblossom;
            for (int i = 12741; i <= 12756; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 1747; i <= 1762; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 22926; i <= 22941; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 23170; i <= 23171; i++)
                materials[i] = Material.OrangeCandleCake;
            materials[12695] = Material.OrangeCarpet;
            materials[14829] = Material.OrangeConcrete;
            materials[14845] = Material.OrangeConcretePowder;
            for (int i = 14768; i <= 14771; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 14674; i <= 14679; i++)
                materials[i] = Material.OrangeShulkerBox;
            materials[6898] = Material.OrangeStainedGlass;
            for (int i = 11290; i <= 11321; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            materials[11243] = Material.OrangeTerracotta;
            materials[2128] = Material.OrangeTulip;
            for (int i = 12985; i <= 12988; i++)
                materials[i] = Material.OrangeWallBanner;
            materials[2094] = Material.OrangeWool;
            materials[2131] = Material.OxeyeDaisy;
            materials[25117] = Material.OxidizedChiseledCopper;
            materials[25110] = Material.OxidizedCopper;
            for (int i = 7885; i <= 7916; i++)
                materials[i] = Material.OxidizedCopperBars;
            for (int i = 26873; i <= 26876; i++)
                materials[i] = Material.OxidizedCopperBulb;
            for (int i = 8069; i <= 8074; i++)
                materials[i] = Material.OxidizedCopperChain;
            for (int i = 26965; i <= 26988; i++)
                materials[i] = Material.OxidizedCopperChest;
            for (int i = 25949; i <= 26012; i++)
                materials[i] = Material.OxidizedCopperDoor;
            for (int i = 27181; i <= 27212; i++)
                materials[i] = Material.OxidizedCopperGolemStatue;
            for (int i = 26851; i <= 26852; i++)
                materials[i] = Material.OxidizedCopperGrate;
            for (int i = 20655; i <= 20658; i++)
                materials[i] = Material.OxidizedCopperLantern;
            for (int i = 26461; i <= 26524; i++)
                materials[i] = Material.OxidizedCopperTrapdoor;
            materials[25113] = Material.OxidizedCutCopper;
            for (int i = 25445; i <= 25450; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 25125; i <= 25204; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            for (int i = 27413; i <= 27436; i++)
                materials[i] = Material.OxidizedLightningRod;
            materials[12712] = Material.PackedIce;
            materials[7557] = Material.PackedMud;
            for (int i = 29664; i <= 29665; i++)
                materials[i] = Material.PaleHangingMoss;
            materials[29501] = Material.PaleMossBlock;
            for (int i = 29502; i <= 29663; i++)
                materials[i] = Material.PaleMossCarpet;
            for (int i = 10641; i <= 10664; i++)
                materials[i] = Material.PaleOakButton;
            for (int i = 14242; i <= 14305; i++)
                materials[i] = Material.PaleOakDoor;
            for (int i = 13762; i <= 13793; i++)
                materials[i] = Material.PaleOakFence;
            for (int i = 13474; i <= 13505; i++)
                materials[i] = Material.PaleOakFenceGate;
            for (int i = 6154; i <= 6217; i++)
                materials[i] = Material.PaleOakHangingSign;
            for (int i = 448; i <= 475; i++)
                materials[i] = Material.PaleOakLeaves;
            for (int i = 157; i <= 159; i++)
                materials[i] = Material.PaleOakLog;
            materials[25] = Material.PaleOakPlanks;
            for (int i = 6674; i <= 6675; i++)
                materials[i] = Material.PaleOakPressurePlate;
            for (int i = 43; i <= 44; i++)
                materials[i] = Material.PaleOakSapling;
            for (int i = 2975; i <= 3038; i++)
                materials[i] = Material.PaleOakShelf;
            for (int i = 5358; i <= 5389; i++)
                materials[i] = Material.PaleOakSign;
            for (int i = 13170; i <= 13175; i++)
                materials[i] = Material.PaleOakSlab;
            for (int i = 12010; i <= 12089; i++)
                materials[i] = Material.PaleOakStairs;
            for (int i = 7361; i <= 7424; i++)
                materials[i] = Material.PaleOakTrapdoor;
            for (int i = 6530; i <= 6537; i++)
                materials[i] = Material.PaleOakWallHangingSign;
            for (int i = 5682; i <= 5689; i++)
                materials[i] = Material.PaleOakWallSign;
            for (int i = 22; i <= 24; i++)
                materials[i] = Material.PaleOakWood;
            for (int i = 29386; i <= 29388; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 12719; i <= 12720; i++)
                materials[i] = Material.Peony;
            for (int i = 13218; i <= 13223; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 10953; i <= 10984; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 10985; i <= 10992; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 12821; i <= 12836; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 1827; i <= 1842; i++)
                materials[i] = Material.PinkBed;
            for (int i = 23006; i <= 23021; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 23180; i <= 23181; i++)
                materials[i] = Material.PinkCandleCake;
            materials[12700] = Material.PinkCarpet;
            materials[14834] = Material.PinkConcrete;
            materials[14850] = Material.PinkConcretePowder;
            for (int i = 14788; i <= 14791; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 27612; i <= 27627; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 14704; i <= 14709; i++)
                materials[i] = Material.PinkShulkerBox;
            materials[6903] = Material.PinkStainedGlass;
            for (int i = 11450; i <= 11481; i++)
                materials[i] = Material.PinkStainedGlassPane;
            materials[11248] = Material.PinkTerracotta;
            materials[2130] = Material.PinkTulip;
            for (int i = 13005; i <= 13008; i++)
                materials[i] = Material.PinkWallBanner;
            materials[2099] = Material.PinkWool;
            for (int i = 2057; i <= 2068; i++)
                materials[i] = Material.Piston;
            for (int i = 2069; i <= 2092; i++)
                materials[i] = Material.PistonHead;
            for (int i = 14597; i <= 14606; i++)
                materials[i] = Material.PitcherCrop;
            for (int i = 14607; i <= 14608; i++)
                materials[i] = Material.PitcherPlant;
            for (int i = 10833; i <= 10864; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 10865; i <= 10872; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            for (int i = 27533; i <= 27552; i++)
                materials[i] = Material.PointedDripstone;
            materials[7] = Material.PolishedAndesite;
            for (int i = 16280; i <= 16285; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 16054; i <= 16133; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 6802; i <= 6804; i++)
                materials[i] = Material.PolishedBasalt;
            materials[22040] = Material.PolishedBlackstone;
            for (int i = 22044; i <= 22049; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 22050; i <= 22129; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 22130; i <= 22453; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            materials[22041] = Material.PolishedBlackstoneBricks;
            for (int i = 22543; i <= 22566; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 22541; i <= 22542; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 22535; i <= 22540; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 22455; i <= 22534; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 22567; i <= 22890; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            materials[28135] = Material.PolishedDeepslate;
            for (int i = 28216; i <= 28221; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 28136; i <= 28215; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 28222; i <= 28545; i++)
                materials[i] = Material.PolishedDeepslateWall;
            materials[5] = Material.PolishedDiorite;
            for (int i = 16232; i <= 16237; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 15334; i <= 15413; i++)
                materials[i] = Material.PolishedDioriteStairs;
            materials[3] = Material.PolishedGranite;
            for (int i = 16214; i <= 16219; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 15094; i <= 15173; i++)
                materials[i] = Material.PolishedGraniteStairs;
            materials[23661] = Material.PolishedTuff;
            for (int i = 23662; i <= 23667; i++)
                materials[i] = Material.PolishedTuffSlab;
            for (int i = 23668; i <= 23747; i++)
                materials[i] = Material.PolishedTuffStairs;
            for (int i = 23748; i <= 24071; i++)
                materials[i] = Material.PolishedTuffWall;
            materials[2123] = Material.Poppy;
            for (int i = 10465; i <= 10472; i++)
                materials[i] = Material.Potatoes;
            materials[10434] = Material.PottedAcaciaSapling;
            materials[10443] = Material.PottedAllium;
            materials[29378] = Material.PottedAzaleaBush;
            materials[10444] = Material.PottedAzureBluet;
            materials[15089] = Material.PottedBamboo;
            materials[10432] = Material.PottedBirchSapling;
            materials[10442] = Material.PottedBlueOrchid;
            materials[10454] = Material.PottedBrownMushroom;
            materials[10456] = Material.PottedCactus;
            materials[10435] = Material.PottedCherrySapling;
            materials[29669] = Material.PottedClosedEyeblossom;
            materials[10450] = Material.PottedCornflower;
            materials[21624] = Material.PottedCrimsonFungus;
            materials[21626] = Material.PottedCrimsonRoots;
            materials[10440] = Material.PottedDandelion;
            materials[10436] = Material.PottedDarkOakSapling;
            materials[10455] = Material.PottedDeadBush;
            materials[10439] = Material.PottedFern;
            materials[29379] = Material.PottedFloweringAzaleaBush;
            materials[10433] = Material.PottedJungleSapling;
            materials[10451] = Material.PottedLilyOfTheValley;
            materials[10438] = Material.PottedMangrovePropagule;
            materials[10430] = Material.PottedOakSapling;
            materials[29668] = Material.PottedOpenEyeblossom;
            materials[10446] = Material.PottedOrangeTulip;
            materials[10449] = Material.PottedOxeyeDaisy;
            materials[10437] = Material.PottedPaleOakSapling;
            materials[10448] = Material.PottedPinkTulip;
            materials[10441] = Material.PottedPoppy;
            materials[10453] = Material.PottedRedMushroom;
            materials[10445] = Material.PottedRedTulip;
            materials[10431] = Material.PottedSpruceSapling;
            materials[10429] = Material.PottedTorchflower;
            materials[21625] = Material.PottedWarpedFungus;
            materials[21627] = Material.PottedWarpedRoots;
            materials[10447] = Material.PottedWhiteTulip;
            materials[10452] = Material.PottedWitherRose;
            materials[24487] = Material.PowderSnow;
            for (int i = 9264; i <= 9266; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 1987; i <= 2010; i++)
                materials[i] = Material.PoweredRail;
            materials[12429] = Material.Prismarine;
            for (int i = 12678; i <= 12683; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 12512; i <= 12591; i++)
                materials[i] = Material.PrismarineBrickStairs;
            materials[12430] = Material.PrismarineBricks;
            for (int i = 12672; i <= 12677; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 12432; i <= 12511; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 16616; i <= 16939; i++)
                materials[i] = Material.PrismarineWall;
            materials[8131] = Material.Pumpkin;
            for (int i = 8141; i <= 8148; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 12885; i <= 12900; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 1891; i <= 1906; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 23070; i <= 23085; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 23188; i <= 23189; i++)
                materials[i] = Material.PurpleCandleCake;
            materials[12704] = Material.PurpleCarpet;
            materials[14838] = Material.PurpleConcrete;
            materials[14854] = Material.PurpleConcretePowder;
            for (int i = 14804; i <= 14807; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 14728; i <= 14733; i++)
                materials[i] = Material.PurpleShulkerBox;
            materials[6907] = Material.PurpleStainedGlass;
            for (int i = 11578; i <= 11609; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            materials[11252] = Material.PurpleTerracotta;
            for (int i = 13021; i <= 13024; i++)
                materials[i] = Material.PurpleWallBanner;
            materials[2103] = Material.PurpleWool;
            materials[14510] = Material.PurpurBlock;
            for (int i = 14511; i <= 14513; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 13272; i <= 13277; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 14514; i <= 14593; i++)
                materials[i] = Material.PurpurStairs;
            materials[11121] = Material.QuartzBlock;
            materials[22893] = Material.QuartzBricks;
            for (int i = 11123; i <= 11125; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 13254; i <= 13259; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 11126; i <= 11205; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 5526; i <= 5545; i++)
                materials[i] = Material.Rail;
            materials[29376] = Material.RawCopperBlock;
            materials[29377] = Material.RawGoldBlock;
            materials[29375] = Material.RawIronBlock;
            for (int i = 12949; i <= 12964; i++)
                materials[i] = Material.RedBanner;
            for (int i = 1955; i <= 1970; i++)
                materials[i] = Material.RedBed;
            for (int i = 23134; i <= 23149; i++)
                materials[i] = Material.RedCandle;
            for (int i = 23196; i <= 23197; i++)
                materials[i] = Material.RedCandleCake;
            materials[12708] = Material.RedCarpet;
            materials[14842] = Material.RedConcrete;
            materials[14858] = Material.RedConcretePowder;
            for (int i = 14820; i <= 14823; i++)
                materials[i] = Material.RedGlazedTerracotta;
            materials[2136] = Material.RedMushroom;
            for (int i = 7629; i <= 7692; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 16274; i <= 16279; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 15974; i <= 16053; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 19208; i <= 19531; i++)
                materials[i] = Material.RedNetherBrickWall;
            materials[14645] = Material.RedNetherBricks;
            materials[123] = Material.RedSand;
            materials[13045] = Material.RedSandstone;
            for (int i = 13260; i <= 13265; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 13048; i <= 13127; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 16940; i <= 17263; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 14752; i <= 14757; i++)
                materials[i] = Material.RedShulkerBox;
            materials[6911] = Material.RedStainedGlass;
            for (int i = 11706; i <= 11737; i++)
                materials[i] = Material.RedStainedGlassPane;
            materials[11256] = Material.RedTerracotta;
            materials[2127] = Material.RedTulip;
            for (int i = 13037; i <= 13040; i++)
                materials[i] = Material.RedWallBanner;
            materials[2107] = Material.RedWool;
            materials[11109] = Material.RedstoneBlock;
            for (int i = 9278; i <= 9279; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 6680; i <= 6681; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 6684; i <= 6685; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 6686; i <= 6693; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 3810; i <= 5105; i++)
                materials[i] = Material.RedstoneWire;
            materials[29390] = Material.ReinforcedDeepslate;
            for (int i = 6833; i <= 6896; i++)
                materials[i] = Material.Repeater;
            for (int i = 14615; i <= 14626; i++)
                materials[i] = Material.RepeatingCommandBlock;
            materials[8720] = Material.ResinBlock;
            for (int i = 8802; i <= 8807; i++)
                materials[i] = Material.ResinBrickSlab;
            for (int i = 8722; i <= 8801; i++)
                materials[i] = Material.ResinBrickStairs;
            for (int i = 8808; i <= 9131; i++)
                materials[i] = Material.ResinBrickWall;
            materials[8721] = Material.ResinBricks;
            for (int i = 8317; i <= 8444; i++)
                materials[i] = Material.ResinClump;
            for (int i = 21619; i <= 21623; i++)
                materials[i] = Material.RespawnAnchor;
            materials[27719] = Material.RootedDirt;
            for (int i = 12717; i <= 12718; i++)
                materials[i] = Material.RoseBush;
            materials[118] = Material.Sand;
            materials[578] = Material.Sandstone;
            for (int i = 13206; i <= 13211; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 9292; i <= 9371; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 19532; i <= 19855; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 20504; i <= 20535; i++)
                materials[i] = Material.Scaffolding;
            materials[24968] = Material.Sculk;
            for (int i = 25097; i <= 25098; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 24488; i <= 24583; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 25099; i <= 25106; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 24969; i <= 25096; i++)
                materials[i] = Material.SculkVein;
            materials[12690] = Material.SeaLantern;
            for (int i = 15065; i <= 15072; i++)
                materials[i] = Material.SeaPickle;
            materials[2054] = Material.Seagrass;
            materials[2052] = Material.ShortDryGrass;
            materials[2048] = Material.ShortGrass;
            materials[20774] = Material.Shroomlight;
            for (int i = 14662; i <= 14667; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 10713; i <= 10744; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 10745; i <= 10752; i++)
                materials[i] = Material.SkeletonWallSkull;
            materials[12330] = Material.SlimeBlock;
            for (int i = 23238; i <= 23249; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 27701; i <= 27716; i++)
                materials[i] = Material.SmallDripleaf;
            materials[20598] = Material.SmithingTable;
            for (int i = 20552; i <= 20559; i++)
                materials[i] = Material.Smoker;
            materials[29374] = Material.SmoothBasalt;
            materials[13280] = Material.SmoothQuartz;
            for (int i = 16256; i <= 16261; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 15734; i <= 15813; i++)
                materials[i] = Material.SmoothQuartzStairs;
            materials[13281] = Material.SmoothRedSandstone;
            for (int i = 16220; i <= 16225; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 15174; i <= 15253; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            materials[13279] = Material.SmoothSandstone;
            for (int i = 16250; i <= 16255; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 15654; i <= 15733; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            materials[13278] = Material.SmoothStone;
            for (int i = 13200; i <= 13205; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 14900; i <= 14902; i++)
                materials[i] = Material.SnifferEgg;
            for (int i = 6718; i <= 6725; i++)
                materials[i] = Material.Snow;
            materials[6727] = Material.SnowBlock;
            for (int i = 20707; i <= 20738; i++)
                materials[i] = Material.SoulCampfire;
            materials[3686] = Material.SoulFire;
            for (int i = 20639; i <= 20642; i++)
                materials[i] = Material.SoulLantern;
            materials[6797] = Material.SoulSand;
            materials[6798] = Material.SoulSoil;
            materials[6805] = Material.SoulTorch;
            for (int i = 6806; i <= 6809; i++)
                materials[i] = Material.SoulWallTorch;
            materials[3687] = Material.Spawner;
            materials[560] = Material.Sponge;
            materials[27608] = Material.SporeBlossom;
            for (int i = 10497; i <= 10520; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 13858; i <= 13921; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 13570; i <= 13601; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 13282; i <= 13313; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 5770; i <= 5833; i++)
                materials[i] = Material.SpruceHangingSign;
            for (int i = 280; i <= 307; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 139; i <= 141; i++)
                materials[i] = Material.SpruceLog;
            materials[16] = Material.SprucePlanks;
            for (int i = 6662; i <= 6663; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 31; i <= 32; i++)
                materials[i] = Material.SpruceSapling;
            for (int i = 3039; i <= 3102; i++)
                materials[i] = Material.SpruceShelf;
            for (int i = 5166; i <= 5197; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 13134; i <= 13139; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 9527; i <= 9606; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 6977; i <= 7040; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 6482; i <= 6489; i++)
                materials[i] = Material.SpruceWallHangingSign;
            for (int i = 5634; i <= 5641; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 204; i <= 206; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 2035; i <= 2046; i++)
                materials[i] = Material.StickyPiston;
            materials[1] = Material.Stone;
            for (int i = 13236; i <= 13241; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 8557; i <= 8636; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 17912; i <= 18235; i++)
                materials[i] = Material.StoneBrickWall;
            materials[7553] = Material.StoneBricks;
            for (int i = 6694; i <= 6717; i++)
                materials[i] = Material.StoneButton;
            for (int i = 6594; i <= 6595; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 13194; i <= 13199; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 15574; i <= 15653; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 20599; i <= 20602; i++)
                materials[i] = Material.Stonecutter;
            for (int i = 180; i <= 182; i++)
                materials[i] = Material.StrippedAcaciaLog;
            for (int i = 237; i <= 239; i++)
                materials[i] = Material.StrippedAcaciaWood;
            for (int i = 198; i <= 200; i++)
                materials[i] = Material.StrippedBambooBlock;
            for (int i = 174; i <= 176; i++)
                materials[i] = Material.StrippedBirchLog;
            for (int i = 231; i <= 233; i++)
                materials[i] = Material.StrippedBirchWood;
            for (int i = 183; i <= 185; i++)
                materials[i] = Material.StrippedCherryLog;
            for (int i = 240; i <= 242; i++)
                materials[i] = Material.StrippedCherryWood;
            for (int i = 20769; i <= 20771; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 20763; i <= 20765; i++)
                materials[i] = Material.StrippedCrimsonStem;
            for (int i = 186; i <= 188; i++)
                materials[i] = Material.StrippedDarkOakLog;
            for (int i = 243; i <= 245; i++)
                materials[i] = Material.StrippedDarkOakWood;
            for (int i = 177; i <= 179; i++)
                materials[i] = Material.StrippedJungleLog;
            for (int i = 234; i <= 236; i++)
                materials[i] = Material.StrippedJungleWood;
            for (int i = 195; i <= 197; i++)
                materials[i] = Material.StrippedMangroveLog;
            for (int i = 249; i <= 251; i++)
                materials[i] = Material.StrippedMangroveWood;
            for (int i = 192; i <= 194; i++)
                materials[i] = Material.StrippedOakLog;
            for (int i = 225; i <= 227; i++)
                materials[i] = Material.StrippedOakWood;
            for (int i = 189; i <= 191; i++)
                materials[i] = Material.StrippedPaleOakLog;
            for (int i = 246; i <= 248; i++)
                materials[i] = Material.StrippedPaleOakWood;
            for (int i = 171; i <= 173; i++)
                materials[i] = Material.StrippedSpruceLog;
            for (int i = 228; i <= 230; i++)
                materials[i] = Material.StrippedSpruceWood;
            for (int i = 20752; i <= 20754; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 20746; i <= 20748; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 21520; i <= 21523; i++)
                materials[i] = Material.StructureBlock;
            materials[14649] = Material.StructureVoid;
            for (int i = 6746; i <= 6761; i++)
                materials[i] = Material.SugarCane;
            for (int i = 12713; i <= 12714; i++)
                materials[i] = Material.Sunflower;
            for (int i = 125; i <= 128; i++)
                materials[i] = Material.SuspiciousGravel;
            for (int i = 119; i <= 122; i++)
                materials[i] = Material.SuspiciousSand;
            for (int i = 20739; i <= 20742; i++)
                materials[i] = Material.SweetBerryBush;
            materials[2053] = Material.TallDryGrass;
            for (int i = 12721; i <= 12722; i++)
                materials[i] = Material.TallGrass;
            for (int i = 2055; i <= 2056; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 21550; i <= 21565; i++)
                materials[i] = Material.Target;
            materials[12710] = Material.Terracotta;
            for (int i = 21536; i <= 21539; i++)
                materials[i] = Material.TestBlock;
            materials[21540] = Material.TestInstanceBlock;
            materials[24486] = Material.TintedGlass;
            for (int i = 2140; i <= 2141; i++)
                materials[i] = Material.Tnt;
            materials[3169] = Material.Torch;
            materials[2122] = Material.Torchflower;
            for (int i = 14595; i <= 14596; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 11005; i <= 11028; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 29455; i <= 29466; i++)
                materials[i] = Material.TrialSpawner;
            for (int i = 9398; i <= 9525; i++)
                materials[i] = Material.Tripwire;
            for (int i = 9382; i <= 9397; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 14955; i <= 14956; i++)
                materials[i] = Material.TubeCoral;
            materials[14940] = Material.TubeCoralBlock;
            for (int i = 14975; i <= 14976; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 15025; i <= 15032; i++)
                materials[i] = Material.TubeCoralWallFan;
            materials[23250] = Material.Tuff;
            for (int i = 24074; i <= 24079; i++)
                materials[i] = Material.TuffBrickSlab;
            for (int i = 24080; i <= 24159; i++)
                materials[i] = Material.TuffBrickStairs;
            for (int i = 24160; i <= 24483; i++)
                materials[i] = Material.TuffBrickWall;
            materials[24073] = Material.TuffBricks;
            for (int i = 23251; i <= 23256; i++)
                materials[i] = Material.TuffSlab;
            for (int i = 23257; i <= 23336; i++)
                materials[i] = Material.TuffStairs;
            for (int i = 23337; i <= 23660; i++)
                materials[i] = Material.TuffWall;
            for (int i = 14888; i <= 14899; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 20802; i <= 20827; i++)
                materials[i] = Material.TwistingVines;
            materials[20828] = Material.TwistingVinesPlant;
            for (int i = 29467; i <= 29498; i++)
                materials[i] = Material.Vault;
            for (int i = 29383; i <= 29385; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 8157; i <= 8188; i++)
                materials[i] = Material.Vine;
            materials[15090] = Material.VoidAir;
            for (int i = 3170; i <= 3173; i++)
                materials[i] = Material.WallTorch;
            for (int i = 21288; i <= 21311; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 21376; i <= 21439; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 20880; i <= 20911; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 21072; i <= 21103; i++)
                materials[i] = Material.WarpedFenceGate;
            materials[20756] = Material.WarpedFungus;
            for (int i = 6282; i <= 6345; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 20749; i <= 20751; i++)
                materials[i] = Material.WarpedHyphae;
            materials[20755] = Material.WarpedNylium;
            materials[20831] = Material.WarpedPlanks;
            for (int i = 20846; i <= 20847; i++)
                materials[i] = Material.WarpedPressurePlate;
            materials[20758] = Material.WarpedRoots;
            for (int i = 3103; i <= 3166; i++)
                materials[i] = Material.WarpedShelf;
            for (int i = 21472; i <= 21503; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 20838; i <= 20843; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 21184; i <= 21263; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 20743; i <= 20745; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 20976; i <= 21039; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 6554; i <= 6561; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 21512; i <= 21519; i++)
                materials[i] = Material.WarpedWallSign;
            materials[20757] = Material.WarpedWartBlock;
            for (int i = 86; i <= 101; i++)
                materials[i] = Material.Water;
            for (int i = 9260; i <= 9262; i++)
                materials[i] = Material.WaterCauldron;
            materials[25124] = Material.WaxedChiseledCopper;
            for (int i = 7917; i <= 7948; i++)
                materials[i] = Material.WaxedCopperBars;
            materials[25469] = Material.WaxedCopperBlock;
            for (int i = 26877; i <= 26880; i++)
                materials[i] = Material.WaxedCopperBulb;
            for (int i = 8075; i <= 8080; i++)
                materials[i] = Material.WaxedCopperChain;
            for (int i = 26989; i <= 27012; i++)
                materials[i] = Material.WaxedCopperChest;
            for (int i = 26077; i <= 26140; i++)
                materials[i] = Material.WaxedCopperDoor;
            for (int i = 27213; i <= 27244; i++)
                materials[i] = Material.WaxedCopperGolemStatue;
            for (int i = 26853; i <= 26854; i++)
                materials[i] = Material.WaxedCopperGrate;
            for (int i = 20659; i <= 20662; i++)
                materials[i] = Material.WaxedCopperLantern;
            for (int i = 26589; i <= 26652; i++)
                materials[i] = Material.WaxedCopperTrapdoor;
            materials[25476] = Material.WaxedCutCopper;
            for (int i = 25815; i <= 25820; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 25717; i <= 25796; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            materials[25123] = Material.WaxedExposedChiseledCopper;
            materials[25471] = Material.WaxedExposedCopper;
            for (int i = 7949; i <= 7980; i++)
                materials[i] = Material.WaxedExposedCopperBars;
            for (int i = 26881; i <= 26884; i++)
                materials[i] = Material.WaxedExposedCopperBulb;
            for (int i = 8081; i <= 8086; i++)
                materials[i] = Material.WaxedExposedCopperChain;
            for (int i = 27013; i <= 27036; i++)
                materials[i] = Material.WaxedExposedCopperChest;
            for (int i = 26141; i <= 26204; i++)
                materials[i] = Material.WaxedExposedCopperDoor;
            for (int i = 27245; i <= 27276; i++)
                materials[i] = Material.WaxedExposedCopperGolemStatue;
            for (int i = 26855; i <= 26856; i++)
                materials[i] = Material.WaxedExposedCopperGrate;
            for (int i = 20663; i <= 20666; i++)
                materials[i] = Material.WaxedExposedCopperLantern;
            for (int i = 26653; i <= 26716; i++)
                materials[i] = Material.WaxedExposedCopperTrapdoor;
            materials[25475] = Material.WaxedExposedCutCopper;
            for (int i = 25809; i <= 25814; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 25637; i <= 25716; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            for (int i = 27461; i <= 27484; i++)
                materials[i] = Material.WaxedExposedLightningRod;
            for (int i = 27437; i <= 27460; i++)
                materials[i] = Material.WaxedLightningRod;
            materials[25121] = Material.WaxedOxidizedChiseledCopper;
            materials[25472] = Material.WaxedOxidizedCopper;
            for (int i = 8013; i <= 8044; i++)
                materials[i] = Material.WaxedOxidizedCopperBars;
            for (int i = 26889; i <= 26892; i++)
                materials[i] = Material.WaxedOxidizedCopperBulb;
            for (int i = 8093; i <= 8098; i++)
                materials[i] = Material.WaxedOxidizedCopperChain;
            for (int i = 27061; i <= 27084; i++)
                materials[i] = Material.WaxedOxidizedCopperChest;
            for (int i = 26205; i <= 26268; i++)
                materials[i] = Material.WaxedOxidizedCopperDoor;
            for (int i = 27309; i <= 27340; i++)
                materials[i] = Material.WaxedOxidizedCopperGolemStatue;
            for (int i = 26859; i <= 26860; i++)
                materials[i] = Material.WaxedOxidizedCopperGrate;
            for (int i = 20671; i <= 20674; i++)
                materials[i] = Material.WaxedOxidizedCopperLantern;
            for (int i = 26717; i <= 26780; i++)
                materials[i] = Material.WaxedOxidizedCopperTrapdoor;
            materials[25473] = Material.WaxedOxidizedCutCopper;
            for (int i = 25797; i <= 25802; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 25477; i <= 25556; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            for (int i = 27509; i <= 27532; i++)
                materials[i] = Material.WaxedOxidizedLightningRod;
            materials[25122] = Material.WaxedWeatheredChiseledCopper;
            materials[25470] = Material.WaxedWeatheredCopper;
            for (int i = 7981; i <= 8012; i++)
                materials[i] = Material.WaxedWeatheredCopperBars;
            for (int i = 26885; i <= 26888; i++)
                materials[i] = Material.WaxedWeatheredCopperBulb;
            for (int i = 8087; i <= 8092; i++)
                materials[i] = Material.WaxedWeatheredCopperChain;
            for (int i = 27037; i <= 27060; i++)
                materials[i] = Material.WaxedWeatheredCopperChest;
            for (int i = 26269; i <= 26332; i++)
                materials[i] = Material.WaxedWeatheredCopperDoor;
            for (int i = 27277; i <= 27308; i++)
                materials[i] = Material.WaxedWeatheredCopperGolemStatue;
            for (int i = 26857; i <= 26858; i++)
                materials[i] = Material.WaxedWeatheredCopperGrate;
            for (int i = 20667; i <= 20670; i++)
                materials[i] = Material.WaxedWeatheredCopperLantern;
            for (int i = 26781; i <= 26844; i++)
                materials[i] = Material.WaxedWeatheredCopperTrapdoor;
            materials[25474] = Material.WaxedWeatheredCutCopper;
            for (int i = 25803; i <= 25808; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 25557; i <= 25636; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            for (int i = 27485; i <= 27508; i++)
                materials[i] = Material.WaxedWeatheredLightningRod;
            materials[25118] = Material.WeatheredChiseledCopper;
            materials[25109] = Material.WeatheredCopper;
            for (int i = 7853; i <= 7884; i++)
                materials[i] = Material.WeatheredCopperBars;
            for (int i = 26869; i <= 26872; i++)
                materials[i] = Material.WeatheredCopperBulb;
            for (int i = 8063; i <= 8068; i++)
                materials[i] = Material.WeatheredCopperChain;
            for (int i = 26941; i <= 26964; i++)
                materials[i] = Material.WeatheredCopperChest;
            for (int i = 26013; i <= 26076; i++)
                materials[i] = Material.WeatheredCopperDoor;
            for (int i = 27149; i <= 27180; i++)
                materials[i] = Material.WeatheredCopperGolemStatue;
            for (int i = 26849; i <= 26850; i++)
                materials[i] = Material.WeatheredCopperGrate;
            for (int i = 20651; i <= 20654; i++)
                materials[i] = Material.WeatheredCopperLantern;
            for (int i = 26525; i <= 26588; i++)
                materials[i] = Material.WeatheredCopperTrapdoor;
            materials[25114] = Material.WeatheredCutCopper;
            for (int i = 25451; i <= 25456; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 25205; i <= 25284; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 27389; i <= 27412; i++)
                materials[i] = Material.WeatheredLightningRod;
            for (int i = 20775; i <= 20800; i++)
                materials[i] = Material.WeepingVines;
            materials[20801] = Material.WeepingVinesPlant;
            materials[561] = Material.WetSponge;
            for (int i = 5110; i <= 5117; i++)
                materials[i] = Material.Wheat;
            for (int i = 12725; i <= 12740; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 1731; i <= 1746; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 22910; i <= 22925; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 23168; i <= 23169; i++)
                materials[i] = Material.WhiteCandleCake;
            materials[12694] = Material.WhiteCarpet;
            materials[14828] = Material.WhiteConcrete;
            materials[14844] = Material.WhiteConcretePowder;
            for (int i = 14764; i <= 14767; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 14668; i <= 14673; i++)
                materials[i] = Material.WhiteShulkerBox;
            materials[6897] = Material.WhiteStainedGlass;
            for (int i = 11258; i <= 11289; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            materials[11242] = Material.WhiteTerracotta;
            materials[2129] = Material.WhiteTulip;
            for (int i = 12981; i <= 12984; i++)
                materials[i] = Material.WhiteWallBanner;
            materials[2093] = Material.WhiteWool;
            for (int i = 27628; i <= 27643; i++)
                materials[i] = Material.Wildflowers;
            materials[2133] = Material.WitherRose;
            for (int i = 10753; i <= 10784; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 10785; i <= 10792; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 12789; i <= 12804; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 1795; i <= 1810; i++)
                materials[i] = Material.YellowBed;
            for (int i = 22974; i <= 22989; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 23176; i <= 23177; i++)
                materials[i] = Material.YellowCandleCake;
            materials[12698] = Material.YellowCarpet;
            materials[14832] = Material.YellowConcrete;
            materials[14848] = Material.YellowConcretePowder;
            for (int i = 14780; i <= 14783; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 14692; i <= 14697; i++)
                materials[i] = Material.YellowShulkerBox;
            materials[6901] = Material.YellowStainedGlass;
            for (int i = 11386; i <= 11417; i++)
                materials[i] = Material.YellowStainedGlassPane;
            materials[11246] = Material.YellowTerracotta;
            for (int i = 12997; i <= 13000; i++)
                materials[i] = Material.YellowWallBanner;
            materials[2097] = Material.YellowWool;
            for (int i = 10793; i <= 10824; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 10825; i <= 10832; i++)
                materials[i] = Material.ZombieWallHead;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
