using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette1216 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette1216()
        {
            for (int i = 9492; i <= 9515; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 12973; i <= 13036; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 12589; i <= 12620; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 12301; i <= 12332; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 5130; i <= 5193; i++)
                materials[i] = Material.AcaciaHangingSign;
            for (int i = 364; i <= 391; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 148; i <= 150; i++)
                materials[i] = Material.AcaciaLog;
            materials[19] = Material.AcaciaPlanks;
            for (int i = 5900; i <= 5901; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 37; i <= 38; i++)
                materials[i] = Material.AcaciaSapling;
            for (int i = 4462; i <= 4493; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 12075; i <= 12080; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 10693; i <= 10772; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 6396; i <= 6459; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 5730; i <= 5737; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 4882; i <= 4889; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 213; i <= 215; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 10129; i <= 10152; i++)
                materials[i] = Material.ActivatorRail;
            materials[0] = Material.Air;
            materials[2125] = Material.Allium;
            materials[22091] = Material.AmethystBlock;
            for (int i = 22093; i <= 22104; i++)
                materials[i] = Material.AmethystCluster;
            materials[20508] = Material.AncientDebris;
            materials[6] = Material.Andesite;
            for (int i = 15191; i <= 15196; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 14817; i <= 14896; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 17807; i <= 18130; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 9916; i <= 9919; i++)
                materials[i] = Material.Anvil;
            for (int i = 7060; i <= 7063; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 7056; i <= 7059; i++)
                materials[i] = Material.AttachedPumpkinStem;
            materials[25884] = Material.Azalea;
            for (int i = 504; i <= 531; i++)
                materials[i] = Material.AzaleaLeaves;
            materials[2126] = Material.AzureBluet;
            for (int i = 14000; i <= 14011; i++)
                materials[i] = Material.Bamboo;
            for (int i = 168; i <= 170; i++)
                materials[i] = Material.BambooBlock;
            for (int i = 9612; i <= 9635; i++)
                materials[i] = Material.BambooButton;
            for (int i = 13293; i <= 13356; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 12749; i <= 12780; i++)
                materials[i] = Material.BambooFence;
            for (int i = 12461; i <= 12492; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 5642; i <= 5705; i++)
                materials[i] = Material.BambooHangingSign;
            materials[28] = Material.BambooMosaic;
            for (int i = 12111; i <= 12116; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 11173; i <= 11252; i++)
                materials[i] = Material.BambooMosaicStairs;
            materials[27] = Material.BambooPlanks;
            for (int i = 5910; i <= 5911; i++)
                materials[i] = Material.BambooPressurePlate;
            materials[13999] = Material.BambooSapling;
            for (int i = 4654; i <= 4685; i++)
                materials[i] = Material.BambooSign;
            for (int i = 12105; i <= 12110; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 11093; i <= 11172; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 6716; i <= 6779; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 5794; i <= 5801; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 4930; i <= 4937; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 19463; i <= 19474; i++)
                materials[i] = Material.Barrel;
            for (int i = 11254; i <= 11255; i++)
                materials[i] = Material.Barrier;
            for (int i = 6031; i <= 6033; i++)
                materials[i] = Material.Basalt;
            materials[8702] = Material.Beacon;
            materials[85] = Material.Bedrock;
            for (int i = 20457; i <= 20480; i++)
                materials[i] = Material.BeeNest;
            for (int i = 20481; i <= 20504; i++)
                materials[i] = Material.Beehive;
            for (int i = 13532; i <= 13535; i++)
                materials[i] = Material.Beetroots;
            for (int i = 19526; i <= 19557; i++)
                materials[i] = Material.Bell;
            for (int i = 25936; i <= 25967; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 25968; i <= 25975; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 9444; i <= 9467; i++)
                materials[i] = Material.BirchButton;
            for (int i = 12845; i <= 12908; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 12525; i <= 12556; i++)
                materials[i] = Material.BirchFence;
            for (int i = 12237; i <= 12268; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 5066; i <= 5129; i++)
                materials[i] = Material.BirchHangingSign;
            for (int i = 308; i <= 335; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 142; i <= 144; i++)
                materials[i] = Material.BirchLog;
            materials[17] = Material.BirchPlanks;
            for (int i = 5896; i <= 5897; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 33; i <= 34; i++)
                materials[i] = Material.BirchSapling;
            for (int i = 4430; i <= 4461; i++)
                materials[i] = Material.BirchSign;
            for (int i = 12063; i <= 12068; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 8530; i <= 8609; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 6268; i <= 6331; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 5722; i <= 5729; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 4874; i <= 4881; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 207; i <= 209; i++)
                materials[i] = Material.BirchWood;
            for (int i = 11888; i <= 11903; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 1971; i <= 1986; i++)
                materials[i] = Material.BlackBed;
            for (int i = 22041; i <= 22056; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 22089; i <= 22090; i++)
                materials[i] = Material.BlackCandleCake;
            materials[11632] = Material.BlackCarpet;
            materials[13766] = Material.BlackConcrete;
            materials[13782] = Material.BlackConcretePowder;
            for (int i = 13747; i <= 13750; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 13681; i <= 13686; i++)
                materials[i] = Material.BlackShulkerBox;
            materials[6139] = Material.BlackStainedGlass;
            for (int i = 10661; i <= 10692; i++)
                materials[i] = Material.BlackStainedGlassPane;
            materials[10180] = Material.BlackTerracotta;
            for (int i = 11964; i <= 11967; i++)
                materials[i] = Material.BlackWallBanner;
            materials[2108] = Material.BlackWool;
            materials[20520] = Material.Blackstone;
            for (int i = 20925; i <= 20930; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 20521; i <= 20600; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 20601; i <= 20924; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 19483; i <= 19490; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 11824; i <= 11839; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 1907; i <= 1922; i++)
                materials[i] = Material.BlueBed;
            for (int i = 21977; i <= 21992; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 22081; i <= 22082; i++)
                materials[i] = Material.BlueCandleCake;
            materials[11628] = Material.BlueCarpet;
            materials[13762] = Material.BlueConcrete;
            materials[13778] = Material.BlueConcretePowder;
            for (int i = 13731; i <= 13734; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            materials[13996] = Material.BlueIce;
            materials[2124] = Material.BlueOrchid;
            for (int i = 13657; i <= 13662; i++)
                materials[i] = Material.BlueShulkerBox;
            materials[6135] = Material.BlueStainedGlass;
            for (int i = 10533; i <= 10564; i++)
                materials[i] = Material.BlueStainedGlassPane;
            materials[10176] = Material.BlueTerracotta;
            for (int i = 11948; i <= 11951; i++)
                materials[i] = Material.BlueWallBanner;
            materials[2104] = Material.BlueWool;
            for (int i = 13569; i <= 13571; i++)
                materials[i] = Material.BoneBlock;
            materials[2142] = Material.Bookshelf;
            for (int i = 13880; i <= 13881; i++)
                materials[i] = Material.BrainCoral;
            materials[13864] = Material.BrainCoralBlock;
            for (int i = 13900; i <= 13901; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 13956; i <= 13963; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 8174; i <= 8181; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 12153; i <= 12158; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 7400; i <= 7479; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 15215; i <= 15538; i++)
                materials[i] = Material.BrickWall;
            materials[2139] = Material.Bricks;
            for (int i = 11840; i <= 11855; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 1923; i <= 1938; i++)
                materials[i] = Material.BrownBed;
            for (int i = 21993; i <= 22008; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 22083; i <= 22084; i++)
                materials[i] = Material.BrownCandleCake;
            materials[11629] = Material.BrownCarpet;
            materials[13763] = Material.BrownConcrete;
            materials[13779] = Material.BrownConcretePowder;
            for (int i = 13735; i <= 13738; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            materials[2135] = Material.BrownMushroom;
            for (int i = 6792; i <= 6855; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 13663; i <= 13668; i++)
                materials[i] = Material.BrownShulkerBox;
            materials[6136] = Material.BrownStainedGlass;
            for (int i = 10565; i <= 10596; i++)
                materials[i] = Material.BrownStainedGlassPane;
            materials[10177] = Material.BrownTerracotta;
            for (int i = 11952; i <= 11955; i++)
                materials[i] = Material.BrownWallBanner;
            materials[2105] = Material.BrownWool;
            for (int i = 14015; i <= 14016; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 13882; i <= 13883; i++)
                materials[i] = Material.BubbleCoral;
            materials[13865] = Material.BubbleCoralBlock;
            for (int i = 13902; i <= 13903; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 13964; i <= 13971; i++)
                materials[i] = Material.BubbleCoralWallFan;
            materials[22092] = Material.BuddingAmethyst;
            materials[2051] = Material.Bush;
            for (int i = 5960; i <= 5975; i++)
                materials[i] = Material.Cactus;
            materials[5976] = Material.CactusFlower;
            for (int i = 6053; i <= 6059; i++)
                materials[i] = Material.Cake;
            materials[23376] = Material.Calcite;
            for (int i = 23475; i <= 23858; i++)
                materials[i] = Material.CalibratedSculkSensor;
            for (int i = 19566; i <= 19597; i++)
                materials[i] = Material.Campfire;
            for (int i = 21785; i <= 21800; i++)
                materials[i] = Material.Candle;
            for (int i = 22057; i <= 22058; i++)
                materials[i] = Material.CandleCake;
            for (int i = 9380; i <= 9387; i++)
                materials[i] = Material.Carrots;
            materials[19491] = Material.CartographyTable;
            for (int i = 6045; i <= 6048; i++)
                materials[i] = Material.CarvedPumpkin;
            materials[8182] = Material.Cauldron;
            materials[14014] = Material.CaveAir;
            for (int i = 25829; i <= 25880; i++)
                materials[i] = Material.CaveVines;
            for (int i = 25881; i <= 25882; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 7016; i <= 7021; i++)
                materials[i] = Material.Chain;
            for (int i = 13550; i <= 13561; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 9516; i <= 9539; i++)
                materials[i] = Material.CherryButton;
            for (int i = 13037; i <= 13100; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 12621; i <= 12652; i++)
                materials[i] = Material.CherryFence;
            for (int i = 12333; i <= 12364; i++)
                materials[i] = Material.CherryFenceGate;
            for (int i = 5194; i <= 5257; i++)
                materials[i] = Material.CherryHangingSign;
            for (int i = 392; i <= 419; i++)
                materials[i] = Material.CherryLeaves;
            for (int i = 151; i <= 153; i++)
                materials[i] = Material.CherryLog;
            materials[20] = Material.CherryPlanks;
            for (int i = 5902; i <= 5903; i++)
                materials[i] = Material.CherryPressurePlate;
            for (int i = 39; i <= 40; i++)
                materials[i] = Material.CherrySapling;
            for (int i = 4494; i <= 4525; i++)
                materials[i] = Material.CherrySign;
            for (int i = 12081; i <= 12086; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 10773; i <= 10852; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 6460; i <= 6523; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 5738; i <= 5745; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 4890; i <= 4897; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 216; i <= 218; i++)
                materials[i] = Material.CherryWood;
            for (int i = 3018; i <= 3041; i++)
                materials[i] = Material.Chest;
            for (int i = 9920; i <= 9923; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 2143; i <= 2398; i++)
                materials[i] = Material.ChiseledBookshelf;
            materials[24011] = Material.ChiseledCopper;
            materials[27643] = Material.ChiseledDeepslate;
            materials[21782] = Material.ChiseledNetherBricks;
            materials[20934] = Material.ChiseledPolishedBlackstone;
            materials[10045] = Material.ChiseledQuartzBlock;
            materials[11969] = Material.ChiseledRedSandstone;
            materials[8055] = Material.ChiseledResinBricks;
            materials[579] = Material.ChiseledSandstone;
            materials[6783] = Material.ChiseledStoneBricks;
            materials[22963] = Material.ChiseledTuff;
            materials[23375] = Material.ChiseledTuffBricks;
            for (int i = 13427; i <= 13432; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 13363; i <= 13426; i++)
                materials[i] = Material.ChorusPlant;
            materials[5977] = Material.Clay;
            materials[27942] = Material.ClosedEyeblossom;
            materials[11634] = Material.CoalBlock;
            materials[133] = Material.CoalOre;
            materials[11] = Material.CoarseDirt;
            materials[25999] = Material.CobbledDeepslate;
            for (int i = 26080; i <= 26085; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 26000; i <= 26079; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 26086; i <= 26409; i++)
                materials[i] = Material.CobbledDeepslateWall;
            materials[14] = Material.Cobblestone;
            for (int i = 12147; i <= 12152; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 4778; i <= 4857; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 8703; i <= 9026; i++)
                materials[i] = Material.CobblestoneWall;
            materials[2047] = Material.Cobweb;
            for (int i = 8203; i <= 8214; i++)
                materials[i] = Material.Cocoa;
            for (int i = 8690; i <= 8701; i++)
                materials[i] = Material.CommandBlock;
            for (int i = 9984; i <= 9999; i++)
                materials[i] = Material.Comparator;
            for (int i = 20432; i <= 20440; i++)
                materials[i] = Material.Composter;
            for (int i = 13997; i <= 13998; i++)
                materials[i] = Material.Conduit;
            materials[23998] = Material.CopperBlock;
            for (int i = 25752; i <= 25755; i++)
                materials[i] = Material.CopperBulb;
            for (int i = 24712; i <= 24775; i++)
                materials[i] = Material.CopperDoor;
            for (int i = 25736; i <= 25737; i++)
                materials[i] = Material.CopperGrate;
            materials[24002] = Material.CopperOre;
            for (int i = 25224; i <= 25287; i++)
                materials[i] = Material.CopperTrapdoor;
            materials[2132] = Material.Cornflower;
            materials[27644] = Material.CrackedDeepslateBricks;
            materials[27645] = Material.CrackedDeepslateTiles;
            materials[21783] = Material.CrackedNetherBricks;
            materials[20933] = Material.CrackedPolishedBlackstoneBricks;
            materials[6782] = Material.CrackedStoneBricks;
            for (int i = 27682; i <= 27729; i++)
                materials[i] = Material.Crafter;
            materials[4341] = Material.CraftingTable;
            for (int i = 2920; i <= 2937; i++)
                materials[i] = Material.CreakingHeart;
            for (int i = 9796; i <= 9827; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 9828; i <= 9835; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 20155; i <= 20178; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 20203; i <= 20266; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 19739; i <= 19770; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 19931; i <= 19962; i++)
                materials[i] = Material.CrimsonFenceGate;
            materials[19664] = Material.CrimsonFungus;
            for (int i = 5450; i <= 5513; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 19657; i <= 19659; i++)
                materials[i] = Material.CrimsonHyphae;
            materials[19663] = Material.CrimsonNylium;
            materials[19721] = Material.CrimsonPlanks;
            for (int i = 19735; i <= 19736; i++)
                materials[i] = Material.CrimsonPressurePlate;
            materials[19720] = Material.CrimsonRoots;
            for (int i = 20331; i <= 20362; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 19723; i <= 19728; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 19995; i <= 20074; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 19651; i <= 19653; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 19803; i <= 19866; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 5778; i <= 5785; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 20395; i <= 20402; i++)
                materials[i] = Material.CrimsonWallSign;
            materials[20509] = Material.CryingObsidian;
            materials[24007] = Material.CutCopper;
            for (int i = 24354; i <= 24359; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 24256; i <= 24335; i++)
                materials[i] = Material.CutCopperStairs;
            materials[11970] = Material.CutRedSandstone;
            for (int i = 12189; i <= 12194; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            materials[580] = Material.CutSandstone;
            for (int i = 12135; i <= 12140; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 11792; i <= 11807; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 1875; i <= 1890; i++)
                materials[i] = Material.CyanBed;
            for (int i = 21945; i <= 21960; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 22077; i <= 22078; i++)
                materials[i] = Material.CyanCandleCake;
            materials[11626] = Material.CyanCarpet;
            materials[13760] = Material.CyanConcrete;
            materials[13776] = Material.CyanConcretePowder;
            for (int i = 13723; i <= 13726; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 13645; i <= 13650; i++)
                materials[i] = Material.CyanShulkerBox;
            materials[6133] = Material.CyanStainedGlass;
            for (int i = 10469; i <= 10500; i++)
                materials[i] = Material.CyanStainedGlassPane;
            materials[10174] = Material.CyanTerracotta;
            for (int i = 11940; i <= 11943; i++)
                materials[i] = Material.CyanWallBanner;
            materials[2102] = Material.CyanWool;
            for (int i = 9924; i <= 9927; i++)
                materials[i] = Material.DamagedAnvil;
            materials[2121] = Material.Dandelion;
            for (int i = 9540; i <= 9563; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 13101; i <= 13164; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 12653; i <= 12684; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 12365; i <= 12396; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 5322; i <= 5385; i++)
                materials[i] = Material.DarkOakHangingSign;
            for (int i = 420; i <= 447; i++)
                materials[i] = Material.DarkOakLeaves;
            for (int i = 154; i <= 156; i++)
                materials[i] = Material.DarkOakLog;
            materials[21] = Material.DarkOakPlanks;
            for (int i = 5904; i <= 5905; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 41; i <= 42; i++)
                materials[i] = Material.DarkOakSapling;
            for (int i = 4558; i <= 4589; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 12087; i <= 12092; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 10853; i <= 10932; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 6524; i <= 6587; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 5754; i <= 5761; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 4906; i <= 4913; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 219; i <= 221; i++)
                materials[i] = Material.DarkOakWood;
            materials[11354] = Material.DarkPrismarine;
            for (int i = 11607; i <= 11612; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 11515; i <= 11594; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 10000; i <= 10031; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 13870; i <= 13871; i++)
                materials[i] = Material.DeadBrainCoral;
            materials[13859] = Material.DeadBrainCoralBlock;
            for (int i = 13890; i <= 13891; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 13916; i <= 13923; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 13872; i <= 13873; i++)
                materials[i] = Material.DeadBubbleCoral;
            materials[13860] = Material.DeadBubbleCoralBlock;
            for (int i = 13892; i <= 13893; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 13924; i <= 13931; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            materials[2050] = Material.DeadBush;
            for (int i = 13874; i <= 13875; i++)
                materials[i] = Material.DeadFireCoral;
            materials[13861] = Material.DeadFireCoralBlock;
            for (int i = 13894; i <= 13895; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 13932; i <= 13939; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 13876; i <= 13877; i++)
                materials[i] = Material.DeadHornCoral;
            materials[13862] = Material.DeadHornCoralBlock;
            for (int i = 13896; i <= 13897; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 13940; i <= 13947; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 13868; i <= 13869; i++)
                materials[i] = Material.DeadTubeCoral;
            materials[13858] = Material.DeadTubeCoralBlock;
            for (int i = 13888; i <= 13889; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 13908; i <= 13915; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 27666; i <= 27681; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 25996; i <= 25998; i++)
                materials[i] = Material.Deepslate;
            for (int i = 27313; i <= 27318; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 27233; i <= 27312; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 27319; i <= 27642; i++)
                materials[i] = Material.DeepslateBrickWall;
            materials[27232] = Material.DeepslateBricks;
            materials[134] = Material.DeepslateCoalOre;
            materials[24003] = Material.DeepslateCopperOre;
            materials[4339] = Material.DeepslateDiamondOre;
            materials[8296] = Material.DeepslateEmeraldOre;
            materials[130] = Material.DeepslateGoldOre;
            materials[132] = Material.DeepslateIronOre;
            materials[564] = Material.DeepslateLapisOre;
            for (int i = 5914; i <= 5915; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 26902; i <= 26907; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 26822; i <= 26901; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 26908; i <= 27231; i++)
                materials[i] = Material.DeepslateTileWall;
            materials[26821] = Material.DeepslateTiles;
            for (int i = 2011; i <= 2034; i++)
                materials[i] = Material.DetectorRail;
            materials[4340] = Material.DiamondBlock;
            materials[4338] = Material.DiamondOre;
            materials[4] = Material.Diorite;
            for (int i = 15209; i <= 15214; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 15057; i <= 15136; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 19103; i <= 19426; i++)
                materials[i] = Material.DioriteWall;
            materials[10] = Material.Dirt;
            materials[13536] = Material.DirtPath;
            for (int i = 566; i <= 577; i++)
                materials[i] = Material.Dispenser;
            materials[8200] = Material.DragonEgg;
            for (int i = 9836; i <= 9867; i++)
                materials[i] = Material.DragonHead;
            for (int i = 9868; i <= 9875; i++)
                materials[i] = Material.DragonWallHead;
            for (int i = 13826; i <= 13857; i++)
                materials[i] = Material.DriedGhast;
            materials[13810] = Material.DriedKelpBlock;
            materials[25828] = Material.DripstoneBlock;
            for (int i = 10153; i <= 10164; i++)
                materials[i] = Material.Dropper;
            materials[8449] = Material.EmeraldBlock;
            materials[8295] = Material.EmeraldOre;
            materials[8173] = Material.EnchantingTable;
            materials[13537] = Material.EndGateway;
            materials[8190] = Material.EndPortal;
            for (int i = 8191; i <= 8198; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 13357; i <= 13362; i++)
                materials[i] = Material.EndRod;
            materials[8199] = Material.EndStone;
            for (int i = 15167; i <= 15172; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 14417; i <= 14496; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 18779; i <= 19102; i++)
                materials[i] = Material.EndStoneBrickWall;
            materials[13517] = Material.EndStoneBricks;
            for (int i = 8297; i <= 8304; i++)
                materials[i] = Material.EnderChest;
            materials[24010] = Material.ExposedChiseledCopper;
            materials[23999] = Material.ExposedCopper;
            for (int i = 25756; i <= 25759; i++)
                materials[i] = Material.ExposedCopperBulb;
            for (int i = 24776; i <= 24839; i++)
                materials[i] = Material.ExposedCopperDoor;
            for (int i = 25738; i <= 25739; i++)
                materials[i] = Material.ExposedCopperGrate;
            for (int i = 25288; i <= 25351; i++)
                materials[i] = Material.ExposedCopperTrapdoor;
            materials[24006] = Material.ExposedCutCopper;
            for (int i = 24348; i <= 24353; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 24176; i <= 24255; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 4350; i <= 4357; i++)
                materials[i] = Material.Farmland;
            materials[2049] = Material.Fern;
            for (int i = 2406; i <= 2917; i++)
                materials[i] = Material.Fire;
            for (int i = 13884; i <= 13885; i++)
                materials[i] = Material.FireCoral;
            materials[13866] = Material.FireCoralBlock;
            for (int i = 13904; i <= 13905; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 13972; i <= 13979; i++)
                materials[i] = Material.FireCoralWallFan;
            materials[27945] = Material.FireflyBush;
            materials[19492] = Material.FletchingTable;
            materials[9351] = Material.FlowerPot;
            materials[25885] = Material.FloweringAzalea;
            for (int i = 532; i <= 559; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            materials[27664] = Material.Frogspawn;
            for (int i = 13562; i <= 13565; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 4358; i <= 4365; i++)
                materials[i] = Material.Furnace;
            materials[21345] = Material.GildedBlackstone;
            materials[562] = Material.Glass;
            for (int i = 7022; i <= 7053; i++)
                materials[i] = Material.GlassPane;
            for (int i = 7112; i <= 7239; i++)
                materials[i] = Material.GlowLichen;
            materials[6042] = Material.Glowstone;
            materials[2137] = Material.GoldBlock;
            materials[129] = Material.GoldOre;
            materials[2] = Material.Granite;
            for (int i = 15185; i <= 15190; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 14737; i <= 14816; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 16511; i <= 16834; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            materials[124] = Material.Gravel;
            for (int i = 11760; i <= 11775; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 1843; i <= 1858; i++)
                materials[i] = Material.GrayBed;
            for (int i = 21913; i <= 21928; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 22073; i <= 22074; i++)
                materials[i] = Material.GrayCandleCake;
            materials[11624] = Material.GrayCarpet;
            materials[13758] = Material.GrayConcrete;
            materials[13774] = Material.GrayConcretePowder;
            for (int i = 13715; i <= 13718; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 13633; i <= 13638; i++)
                materials[i] = Material.GrayShulkerBox;
            materials[6131] = Material.GrayStainedGlass;
            for (int i = 10405; i <= 10436; i++)
                materials[i] = Material.GrayStainedGlassPane;
            materials[10172] = Material.GrayTerracotta;
            for (int i = 11932; i <= 11935; i++)
                materials[i] = Material.GrayWallBanner;
            materials[2100] = Material.GrayWool;
            for (int i = 11856; i <= 11871; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 1939; i <= 1954; i++)
                materials[i] = Material.GreenBed;
            for (int i = 22009; i <= 22024; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 22085; i <= 22086; i++)
                materials[i] = Material.GreenCandleCake;
            materials[11630] = Material.GreenCarpet;
            materials[13764] = Material.GreenConcrete;
            materials[13780] = Material.GreenConcretePowder;
            for (int i = 13739; i <= 13742; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 13669; i <= 13674; i++)
                materials[i] = Material.GreenShulkerBox;
            materials[6137] = Material.GreenStainedGlass;
            for (int i = 10597; i <= 10628; i++)
                materials[i] = Material.GreenStainedGlassPane;
            materials[10178] = Material.GreenTerracotta;
            for (int i = 11956; i <= 11959; i++)
                materials[i] = Material.GreenWallBanner;
            materials[2106] = Material.GreenWool;
            for (int i = 19493; i <= 19504; i++)
                materials[i] = Material.Grindstone;
            for (int i = 25992; i <= 25993; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 11614; i <= 11616; i++)
                materials[i] = Material.HayBlock;
            for (int i = 27774; i <= 27775; i++)
                materials[i] = Material.HeavyCore;
            for (int i = 9968; i <= 9983; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            materials[20505] = Material.HoneyBlock;
            materials[20506] = Material.HoneycombBlock;
            for (int i = 10034; i <= 10043; i++)
                materials[i] = Material.Hopper;
            for (int i = 13886; i <= 13887; i++)
                materials[i] = Material.HornCoral;
            materials[13867] = Material.HornCoralBlock;
            for (int i = 13906; i <= 13907; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 13980; i <= 13987; i++)
                materials[i] = Material.HornCoralWallFan;
            materials[5958] = Material.Ice;
            materials[6791] = Material.InfestedChiseledStoneBricks;
            materials[6787] = Material.InfestedCobblestone;
            materials[6790] = Material.InfestedCrackedStoneBricks;
            for (int i = 27646; i <= 27648; i++)
                materials[i] = Material.InfestedDeepslate;
            materials[6789] = Material.InfestedMossyStoneBricks;
            materials[6786] = Material.InfestedStone;
            materials[6788] = Material.InfestedStoneBricks;
            for (int i = 6984; i <= 7015; i++)
                materials[i] = Material.IronBars;
            materials[2138] = Material.IronBlock;
            for (int i = 5828; i <= 5891; i++)
                materials[i] = Material.IronDoor;
            materials[131] = Material.IronOre;
            for (int i = 11288; i <= 11351; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 6049; i <= 6052; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 20415; i <= 20426; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 5994; i <= 5995; i++)
                materials[i] = Material.Jukebox;
            for (int i = 9468; i <= 9491; i++)
                materials[i] = Material.JungleButton;
            for (int i = 12909; i <= 12972; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 12557; i <= 12588; i++)
                materials[i] = Material.JungleFence;
            for (int i = 12269; i <= 12300; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 5258; i <= 5321; i++)
                materials[i] = Material.JungleHangingSign;
            for (int i = 336; i <= 363; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 145; i <= 147; i++)
                materials[i] = Material.JungleLog;
            materials[18] = Material.JunglePlanks;
            for (int i = 5898; i <= 5899; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 35; i <= 36; i++)
                materials[i] = Material.JungleSapling;
            for (int i = 4526; i <= 4557; i++)
                materials[i] = Material.JungleSign;
            for (int i = 12069; i <= 12074; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 8610; i <= 8689; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 6332; i <= 6395; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 5746; i <= 5753; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 4898; i <= 4905; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 210; i <= 212; i++)
                materials[i] = Material.JungleWood;
            for (int i = 13783; i <= 13808; i++)
                materials[i] = Material.Kelp;
            materials[13809] = Material.KelpPlant;
            for (int i = 4750; i <= 4757; i++)
                materials[i] = Material.Ladder;
            for (int i = 19558; i <= 19561; i++)
                materials[i] = Material.Lantern;
            materials[565] = Material.LapisBlock;
            materials[563] = Material.LapisOre;
            for (int i = 22105; i <= 22116; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 11646; i <= 11647; i++)
                materials[i] = Material.LargeFern;
            for (int i = 102; i <= 117; i++)
                materials[i] = Material.Lava;
            materials[8186] = Material.LavaCauldron;
            for (int i = 25919; i <= 25934; i++)
                materials[i] = Material.LeafLitter;
            for (int i = 19505; i <= 19520; i++)
                materials[i] = Material.Lectern;
            for (int i = 5802; i <= 5825; i++)
                materials[i] = Material.Lever;
            for (int i = 11256; i <= 11287; i++)
                materials[i] = Material.Light;
            for (int i = 11696; i <= 11711; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 1779; i <= 1794; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 21849; i <= 21864; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 22065; i <= 22066; i++)
                materials[i] = Material.LightBlueCandleCake;
            materials[11620] = Material.LightBlueCarpet;
            materials[13754] = Material.LightBlueConcrete;
            materials[13770] = Material.LightBlueConcretePowder;
            for (int i = 13699; i <= 13702; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 13609; i <= 13614; i++)
                materials[i] = Material.LightBlueShulkerBox;
            materials[6127] = Material.LightBlueStainedGlass;
            for (int i = 10277; i <= 10308; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            materials[10168] = Material.LightBlueTerracotta;
            for (int i = 11916; i <= 11919; i++)
                materials[i] = Material.LightBlueWallBanner;
            materials[2096] = Material.LightBlueWool;
            for (int i = 11776; i <= 11791; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 1859; i <= 1874; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 21929; i <= 21944; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 22075; i <= 22076; i++)
                materials[i] = Material.LightGrayCandleCake;
            materials[11625] = Material.LightGrayCarpet;
            materials[13759] = Material.LightGrayConcrete;
            materials[13775] = Material.LightGrayConcretePowder;
            for (int i = 13719; i <= 13722; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 13639; i <= 13644; i++)
                materials[i] = Material.LightGrayShulkerBox;
            materials[6132] = Material.LightGrayStainedGlass;
            for (int i = 10437; i <= 10468; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            materials[10173] = Material.LightGrayTerracotta;
            for (int i = 11936; i <= 11939; i++)
                materials[i] = Material.LightGrayWallBanner;
            materials[2101] = Material.LightGrayWool;
            for (int i = 9952; i <= 9967; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 25784; i <= 25807; i++)
                materials[i] = Material.LightningRod;
            for (int i = 11638; i <= 11639; i++)
                materials[i] = Material.Lilac;
            materials[2134] = Material.LilyOfTheValley;
            materials[7642] = Material.LilyPad;
            for (int i = 11728; i <= 11743; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 1811; i <= 1826; i++)
                materials[i] = Material.LimeBed;
            for (int i = 21881; i <= 21896; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 22069; i <= 22070; i++)
                materials[i] = Material.LimeCandleCake;
            materials[11622] = Material.LimeCarpet;
            materials[13756] = Material.LimeConcrete;
            materials[13772] = Material.LimeConcretePowder;
            for (int i = 13707; i <= 13710; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 13621; i <= 13626; i++)
                materials[i] = Material.LimeShulkerBox;
            materials[6129] = Material.LimeStainedGlass;
            for (int i = 10341; i <= 10372; i++)
                materials[i] = Material.LimeStainedGlassPane;
            materials[10170] = Material.LimeTerracotta;
            for (int i = 11924; i <= 11927; i++)
                materials[i] = Material.LimeWallBanner;
            materials[2098] = Material.LimeWool;
            materials[20519] = Material.Lodestone;
            for (int i = 19459; i <= 19462; i++)
                materials[i] = Material.Loom;
            for (int i = 11680; i <= 11695; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 1763; i <= 1778; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 21833; i <= 21848; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 22063; i <= 22064; i++)
                materials[i] = Material.MagentaCandleCake;
            materials[11619] = Material.MagentaCarpet;
            materials[13753] = Material.MagentaConcrete;
            materials[13769] = Material.MagentaConcretePowder;
            for (int i = 13695; i <= 13698; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 13603; i <= 13608; i++)
                materials[i] = Material.MagentaShulkerBox;
            materials[6126] = Material.MagentaStainedGlass;
            for (int i = 10245; i <= 10276; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            materials[10167] = Material.MagentaTerracotta;
            for (int i = 11912; i <= 11915; i++)
                materials[i] = Material.MagentaWallBanner;
            materials[2095] = Material.MagentaWool;
            materials[13566] = Material.MagmaBlock;
            for (int i = 9588; i <= 9611; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 13229; i <= 13292; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 12717; i <= 12748; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 12429; i <= 12460; i++)
                materials[i] = Material.MangroveFenceGate;
            for (int i = 5578; i <= 5641; i++)
                materials[i] = Material.MangroveHangingSign;
            for (int i = 476; i <= 503; i++)
                materials[i] = Material.MangroveLeaves;
            for (int i = 160; i <= 162; i++)
                materials[i] = Material.MangroveLog;
            materials[26] = Material.MangrovePlanks;
            for (int i = 5908; i <= 5909; i++)
                materials[i] = Material.MangrovePressurePlate;
            for (int i = 45; i <= 84; i++)
                materials[i] = Material.MangrovePropagule;
            for (int i = 163; i <= 164; i++)
                materials[i] = Material.MangroveRoots;
            for (int i = 4622; i <= 4653; i++)
                materials[i] = Material.MangroveSign;
            for (int i = 12099; i <= 12104; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 11013; i <= 11092; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 6652; i <= 6715; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 5770; i <= 5777; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 4922; i <= 4929; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 222; i <= 224; i++)
                materials[i] = Material.MangroveWood;
            for (int i = 22117; i <= 22128; i++)
                materials[i] = Material.MediumAmethystBud;
            materials[7055] = Material.Melon;
            for (int i = 7072; i <= 7079; i++)
                materials[i] = Material.MelonStem;
            materials[25935] = Material.MossBlock;
            materials[25886] = Material.MossCarpet;
            materials[2399] = Material.MossyCobblestone;
            for (int i = 15161; i <= 15166; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 14337; i <= 14416; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 9027; i <= 9350; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 15149; i <= 15154; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 14177; i <= 14256; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 16187; i <= 16510; i++)
                materials[i] = Material.MossyStoneBrickWall;
            materials[6781] = Material.MossyStoneBricks;
            for (int i = 2109; i <= 2120; i++)
                materials[i] = Material.MovingPiston;
            materials[25995] = Material.Mud;
            for (int i = 12165; i <= 12170; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 7560; i <= 7639; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 17159; i <= 17482; i++)
                materials[i] = Material.MudBrickWall;
            materials[6785] = Material.MudBricks;
            for (int i = 165; i <= 167; i++)
                materials[i] = Material.MuddyMangroveRoots;
            for (int i = 6920; i <= 6983; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 7640; i <= 7641; i++)
                materials[i] = Material.Mycelium;
            for (int i = 8057; i <= 8088; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 12171; i <= 12176; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 8089; i <= 8168; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 17483; i <= 17806; i++)
                materials[i] = Material.NetherBrickWall;
            materials[8056] = Material.NetherBricks;
            materials[135] = Material.NetherGoldOre;
            for (int i = 6043; i <= 6044; i++)
                materials[i] = Material.NetherPortal;
            materials[10033] = Material.NetherQuartzOre;
            materials[19650] = Material.NetherSprouts;
            for (int i = 8169; i <= 8172; i++)
                materials[i] = Material.NetherWart;
            materials[13567] = Material.NetherWartBlock;
            materials[20507] = Material.NetheriteBlock;
            materials[6028] = Material.Netherrack;
            for (int i = 581; i <= 1730; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 9396; i <= 9419; i++)
                materials[i] = Material.OakButton;
            for (int i = 4686; i <= 4749; i++)
                materials[i] = Material.OakDoor;
            for (int i = 5996; i <= 6027; i++)
                materials[i] = Material.OakFence;
            for (int i = 7368; i <= 7399; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 4938; i <= 5001; i++)
                materials[i] = Material.OakHangingSign;
            for (int i = 252; i <= 279; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 136; i <= 138; i++)
                materials[i] = Material.OakLog;
            materials[15] = Material.OakPlanks;
            for (int i = 5892; i <= 5893; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 29; i <= 30; i++)
                materials[i] = Material.OakSapling;
            for (int i = 4366; i <= 4397; i++)
                materials[i] = Material.OakSign;
            for (int i = 12051; i <= 12056; i++)
                materials[i] = Material.OakSlab;
            for (int i = 2938; i <= 3017; i++)
                materials[i] = Material.OakStairs;
            for (int i = 6140; i <= 6203; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 5706; i <= 5713; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 4858; i <= 4865; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 201; i <= 203; i++)
                materials[i] = Material.OakWood;
            for (int i = 13573; i <= 13584; i++)
                materials[i] = Material.Observer;
            materials[2400] = Material.Obsidian;
            for (int i = 27655; i <= 27657; i++)
                materials[i] = Material.OchreFroglight;
            materials[27941] = Material.OpenEyeblossom;
            for (int i = 11664; i <= 11679; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 1747; i <= 1762; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 21817; i <= 21832; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 22061; i <= 22062; i++)
                materials[i] = Material.OrangeCandleCake;
            materials[11618] = Material.OrangeCarpet;
            materials[13752] = Material.OrangeConcrete;
            materials[13768] = Material.OrangeConcretePowder;
            for (int i = 13691; i <= 13694; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 13597; i <= 13602; i++)
                materials[i] = Material.OrangeShulkerBox;
            materials[6125] = Material.OrangeStainedGlass;
            for (int i = 10213; i <= 10244; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            materials[10166] = Material.OrangeTerracotta;
            materials[2128] = Material.OrangeTulip;
            for (int i = 11908; i <= 11911; i++)
                materials[i] = Material.OrangeWallBanner;
            materials[2094] = Material.OrangeWool;
            materials[2131] = Material.OxeyeDaisy;
            materials[24008] = Material.OxidizedChiseledCopper;
            materials[24001] = Material.OxidizedCopper;
            for (int i = 25764; i <= 25767; i++)
                materials[i] = Material.OxidizedCopperBulb;
            for (int i = 24840; i <= 24903; i++)
                materials[i] = Material.OxidizedCopperDoor;
            for (int i = 25742; i <= 25743; i++)
                materials[i] = Material.OxidizedCopperGrate;
            for (int i = 25352; i <= 25415; i++)
                materials[i] = Material.OxidizedCopperTrapdoor;
            materials[24004] = Material.OxidizedCutCopper;
            for (int i = 24336; i <= 24341; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 24016; i <= 24095; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            materials[11635] = Material.PackedIce;
            materials[6784] = Material.PackedMud;
            for (int i = 27939; i <= 27940; i++)
                materials[i] = Material.PaleHangingMoss;
            materials[27776] = Material.PaleMossBlock;
            for (int i = 27777; i <= 27938; i++)
                materials[i] = Material.PaleMossCarpet;
            for (int i = 9564; i <= 9587; i++)
                materials[i] = Material.PaleOakButton;
            for (int i = 13165; i <= 13228; i++)
                materials[i] = Material.PaleOakDoor;
            for (int i = 12685; i <= 12716; i++)
                materials[i] = Material.PaleOakFence;
            for (int i = 12397; i <= 12428; i++)
                materials[i] = Material.PaleOakFenceGate;
            for (int i = 5386; i <= 5449; i++)
                materials[i] = Material.PaleOakHangingSign;
            for (int i = 448; i <= 475; i++)
                materials[i] = Material.PaleOakLeaves;
            for (int i = 157; i <= 159; i++)
                materials[i] = Material.PaleOakLog;
            materials[25] = Material.PaleOakPlanks;
            for (int i = 5906; i <= 5907; i++)
                materials[i] = Material.PaleOakPressurePlate;
            for (int i = 43; i <= 44; i++)
                materials[i] = Material.PaleOakSapling;
            for (int i = 4590; i <= 4621; i++)
                materials[i] = Material.PaleOakSign;
            for (int i = 12093; i <= 12098; i++)
                materials[i] = Material.PaleOakSlab;
            for (int i = 10933; i <= 11012; i++)
                materials[i] = Material.PaleOakStairs;
            for (int i = 6588; i <= 6651; i++)
                materials[i] = Material.PaleOakTrapdoor;
            for (int i = 5762; i <= 5769; i++)
                materials[i] = Material.PaleOakWallHangingSign;
            for (int i = 4914; i <= 4921; i++)
                materials[i] = Material.PaleOakWallSign;
            for (int i = 22; i <= 24; i++)
                materials[i] = Material.PaleOakWood;
            for (int i = 27661; i <= 27663; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 11642; i <= 11643; i++)
                materials[i] = Material.Peony;
            for (int i = 12141; i <= 12146; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 9876; i <= 9907; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 9908; i <= 9915; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 11744; i <= 11759; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 1827; i <= 1842; i++)
                materials[i] = Material.PinkBed;
            for (int i = 21897; i <= 21912; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 22071; i <= 22072; i++)
                materials[i] = Material.PinkCandleCake;
            materials[11623] = Material.PinkCarpet;
            materials[13757] = Material.PinkConcrete;
            materials[13773] = Material.PinkConcretePowder;
            for (int i = 13711; i <= 13714; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 25887; i <= 25902; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 13627; i <= 13632; i++)
                materials[i] = Material.PinkShulkerBox;
            materials[6130] = Material.PinkStainedGlass;
            for (int i = 10373; i <= 10404; i++)
                materials[i] = Material.PinkStainedGlassPane;
            materials[10171] = Material.PinkTerracotta;
            materials[2130] = Material.PinkTulip;
            for (int i = 11928; i <= 11931; i++)
                materials[i] = Material.PinkWallBanner;
            materials[2099] = Material.PinkWool;
            for (int i = 2057; i <= 2068; i++)
                materials[i] = Material.Piston;
            for (int i = 2069; i <= 2092; i++)
                materials[i] = Material.PistonHead;
            for (int i = 13520; i <= 13529; i++)
                materials[i] = Material.PitcherCrop;
            for (int i = 13530; i <= 13531; i++)
                materials[i] = Material.PitcherPlant;
            for (int i = 9756; i <= 9787; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 9788; i <= 9795; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            for (int i = 25808; i <= 25827; i++)
                materials[i] = Material.PointedDripstone;
            materials[7] = Material.PolishedAndesite;
            for (int i = 15203; i <= 15208; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 14977; i <= 15056; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 6034; i <= 6036; i++)
                materials[i] = Material.PolishedBasalt;
            materials[20931] = Material.PolishedBlackstone;
            for (int i = 20935; i <= 20940; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 20941; i <= 21020; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 21021; i <= 21344; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            materials[20932] = Material.PolishedBlackstoneBricks;
            for (int i = 21434; i <= 21457; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 21432; i <= 21433; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 21426; i <= 21431; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 21346; i <= 21425; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 21458; i <= 21781; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            materials[26410] = Material.PolishedDeepslate;
            for (int i = 26491; i <= 26496; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 26411; i <= 26490; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 26497; i <= 26820; i++)
                materials[i] = Material.PolishedDeepslateWall;
            materials[5] = Material.PolishedDiorite;
            for (int i = 15155; i <= 15160; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 14257; i <= 14336; i++)
                materials[i] = Material.PolishedDioriteStairs;
            materials[3] = Material.PolishedGranite;
            for (int i = 15137; i <= 15142; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 14017; i <= 14096; i++)
                materials[i] = Material.PolishedGraniteStairs;
            materials[22552] = Material.PolishedTuff;
            for (int i = 22553; i <= 22558; i++)
                materials[i] = Material.PolishedTuffSlab;
            for (int i = 22559; i <= 22638; i++)
                materials[i] = Material.PolishedTuffStairs;
            for (int i = 22639; i <= 22962; i++)
                materials[i] = Material.PolishedTuffWall;
            materials[2123] = Material.Poppy;
            for (int i = 9388; i <= 9395; i++)
                materials[i] = Material.Potatoes;
            materials[9357] = Material.PottedAcaciaSapling;
            materials[9366] = Material.PottedAllium;
            materials[27653] = Material.PottedAzaleaBush;
            materials[9367] = Material.PottedAzureBluet;
            materials[14012] = Material.PottedBamboo;
            materials[9355] = Material.PottedBirchSapling;
            materials[9365] = Material.PottedBlueOrchid;
            materials[9377] = Material.PottedBrownMushroom;
            materials[9379] = Material.PottedCactus;
            materials[9358] = Material.PottedCherrySapling;
            materials[27944] = Material.PottedClosedEyeblossom;
            materials[9373] = Material.PottedCornflower;
            materials[20515] = Material.PottedCrimsonFungus;
            materials[20517] = Material.PottedCrimsonRoots;
            materials[9363] = Material.PottedDandelion;
            materials[9359] = Material.PottedDarkOakSapling;
            materials[9378] = Material.PottedDeadBush;
            materials[9362] = Material.PottedFern;
            materials[27654] = Material.PottedFloweringAzaleaBush;
            materials[9356] = Material.PottedJungleSapling;
            materials[9374] = Material.PottedLilyOfTheValley;
            materials[9361] = Material.PottedMangrovePropagule;
            materials[9353] = Material.PottedOakSapling;
            materials[27943] = Material.PottedOpenEyeblossom;
            materials[9369] = Material.PottedOrangeTulip;
            materials[9372] = Material.PottedOxeyeDaisy;
            materials[9360] = Material.PottedPaleOakSapling;
            materials[9371] = Material.PottedPinkTulip;
            materials[9364] = Material.PottedPoppy;
            materials[9376] = Material.PottedRedMushroom;
            materials[9368] = Material.PottedRedTulip;
            materials[9354] = Material.PottedSpruceSapling;
            materials[9352] = Material.PottedTorchflower;
            materials[20516] = Material.PottedWarpedFungus;
            materials[20518] = Material.PottedWarpedRoots;
            materials[9370] = Material.PottedWhiteTulip;
            materials[9375] = Material.PottedWitherRose;
            materials[23378] = Material.PowderSnow;
            for (int i = 8187; i <= 8189; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 1987; i <= 2010; i++)
                materials[i] = Material.PoweredRail;
            materials[11352] = Material.Prismarine;
            for (int i = 11601; i <= 11606; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 11435; i <= 11514; i++)
                materials[i] = Material.PrismarineBrickStairs;
            materials[11353] = Material.PrismarineBricks;
            for (int i = 11595; i <= 11600; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 11355; i <= 11434; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 15539; i <= 15862; i++)
                materials[i] = Material.PrismarineWall;
            materials[7054] = Material.Pumpkin;
            for (int i = 7064; i <= 7071; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 11808; i <= 11823; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 1891; i <= 1906; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 21961; i <= 21976; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 22079; i <= 22080; i++)
                materials[i] = Material.PurpleCandleCake;
            materials[11627] = Material.PurpleCarpet;
            materials[13761] = Material.PurpleConcrete;
            materials[13777] = Material.PurpleConcretePowder;
            for (int i = 13727; i <= 13730; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 13651; i <= 13656; i++)
                materials[i] = Material.PurpleShulkerBox;
            materials[6134] = Material.PurpleStainedGlass;
            for (int i = 10501; i <= 10532; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            materials[10175] = Material.PurpleTerracotta;
            for (int i = 11944; i <= 11947; i++)
                materials[i] = Material.PurpleWallBanner;
            materials[2103] = Material.PurpleWool;
            materials[13433] = Material.PurpurBlock;
            for (int i = 13434; i <= 13436; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 12195; i <= 12200; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 13437; i <= 13516; i++)
                materials[i] = Material.PurpurStairs;
            materials[10044] = Material.QuartzBlock;
            materials[21784] = Material.QuartzBricks;
            for (int i = 10046; i <= 10048; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 12177; i <= 12182; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 10049; i <= 10128; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 4758; i <= 4777; i++)
                materials[i] = Material.Rail;
            materials[27651] = Material.RawCopperBlock;
            materials[27652] = Material.RawGoldBlock;
            materials[27650] = Material.RawIronBlock;
            for (int i = 11872; i <= 11887; i++)
                materials[i] = Material.RedBanner;
            for (int i = 1955; i <= 1970; i++)
                materials[i] = Material.RedBed;
            for (int i = 22025; i <= 22040; i++)
                materials[i] = Material.RedCandle;
            for (int i = 22087; i <= 22088; i++)
                materials[i] = Material.RedCandleCake;
            materials[11631] = Material.RedCarpet;
            materials[13765] = Material.RedConcrete;
            materials[13781] = Material.RedConcretePowder;
            for (int i = 13743; i <= 13746; i++)
                materials[i] = Material.RedGlazedTerracotta;
            materials[2136] = Material.RedMushroom;
            for (int i = 6856; i <= 6919; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 15197; i <= 15202; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 14897; i <= 14976; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 18131; i <= 18454; i++)
                materials[i] = Material.RedNetherBrickWall;
            materials[13568] = Material.RedNetherBricks;
            materials[123] = Material.RedSand;
            materials[11968] = Material.RedSandstone;
            for (int i = 12183; i <= 12188; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 11971; i <= 12050; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 15863; i <= 16186; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 13675; i <= 13680; i++)
                materials[i] = Material.RedShulkerBox;
            materials[6138] = Material.RedStainedGlass;
            for (int i = 10629; i <= 10660; i++)
                materials[i] = Material.RedStainedGlassPane;
            materials[10179] = Material.RedTerracotta;
            materials[2127] = Material.RedTulip;
            for (int i = 11960; i <= 11963; i++)
                materials[i] = Material.RedWallBanner;
            materials[2107] = Material.RedWool;
            materials[10032] = Material.RedstoneBlock;
            for (int i = 8201; i <= 8202; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 5912; i <= 5913; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 5916; i <= 5917; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 5918; i <= 5925; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 3042; i <= 4337; i++)
                materials[i] = Material.RedstoneWire;
            materials[27665] = Material.ReinforcedDeepslate;
            for (int i = 6060; i <= 6123; i++)
                materials[i] = Material.Repeater;
            for (int i = 13538; i <= 13549; i++)
                materials[i] = Material.RepeatingCommandBlock;
            materials[7643] = Material.ResinBlock;
            for (int i = 7725; i <= 7730; i++)
                materials[i] = Material.ResinBrickSlab;
            for (int i = 7645; i <= 7724; i++)
                materials[i] = Material.ResinBrickStairs;
            for (int i = 7731; i <= 8054; i++)
                materials[i] = Material.ResinBrickWall;
            materials[7644] = Material.ResinBricks;
            for (int i = 7240; i <= 7367; i++)
                materials[i] = Material.ResinClump;
            for (int i = 20510; i <= 20514; i++)
                materials[i] = Material.RespawnAnchor;
            materials[25994] = Material.RootedDirt;
            for (int i = 11640; i <= 11641; i++)
                materials[i] = Material.RoseBush;
            materials[118] = Material.Sand;
            materials[578] = Material.Sandstone;
            for (int i = 12129; i <= 12134; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 8215; i <= 8294; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 18455; i <= 18778; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 19427; i <= 19458; i++)
                materials[i] = Material.Scaffolding;
            materials[23859] = Material.Sculk;
            for (int i = 23988; i <= 23989; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 23379; i <= 23474; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 23990; i <= 23997; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 23860; i <= 23987; i++)
                materials[i] = Material.SculkVein;
            materials[11613] = Material.SeaLantern;
            for (int i = 13988; i <= 13995; i++)
                materials[i] = Material.SeaPickle;
            materials[2054] = Material.Seagrass;
            materials[2052] = Material.ShortDryGrass;
            materials[2048] = Material.ShortGrass;
            materials[19665] = Material.Shroomlight;
            for (int i = 13585; i <= 13590; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 9636; i <= 9667; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 9668; i <= 9675; i++)
                materials[i] = Material.SkeletonWallSkull;
            materials[11253] = Material.SlimeBlock;
            for (int i = 22129; i <= 22140; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 25976; i <= 25991; i++)
                materials[i] = Material.SmallDripleaf;
            materials[19521] = Material.SmithingTable;
            for (int i = 19475; i <= 19482; i++)
                materials[i] = Material.Smoker;
            materials[27649] = Material.SmoothBasalt;
            materials[12203] = Material.SmoothQuartz;
            for (int i = 15179; i <= 15184; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 14657; i <= 14736; i++)
                materials[i] = Material.SmoothQuartzStairs;
            materials[12204] = Material.SmoothRedSandstone;
            for (int i = 15143; i <= 15148; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 14097; i <= 14176; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            materials[12202] = Material.SmoothSandstone;
            for (int i = 15173; i <= 15178; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 14577; i <= 14656; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            materials[12201] = Material.SmoothStone;
            for (int i = 12123; i <= 12128; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 13823; i <= 13825; i++)
                materials[i] = Material.SnifferEgg;
            for (int i = 5950; i <= 5957; i++)
                materials[i] = Material.Snow;
            materials[5959] = Material.SnowBlock;
            for (int i = 19598; i <= 19629; i++)
                materials[i] = Material.SoulCampfire;
            materials[2918] = Material.SoulFire;
            for (int i = 19562; i <= 19565; i++)
                materials[i] = Material.SoulLantern;
            materials[6029] = Material.SoulSand;
            materials[6030] = Material.SoulSoil;
            materials[6037] = Material.SoulTorch;
            for (int i = 6038; i <= 6041; i++)
                materials[i] = Material.SoulWallTorch;
            materials[2919] = Material.Spawner;
            materials[560] = Material.Sponge;
            materials[25883] = Material.SporeBlossom;
            for (int i = 9420; i <= 9443; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 12781; i <= 12844; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 12493; i <= 12524; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 12205; i <= 12236; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 5002; i <= 5065; i++)
                materials[i] = Material.SpruceHangingSign;
            for (int i = 280; i <= 307; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 139; i <= 141; i++)
                materials[i] = Material.SpruceLog;
            materials[16] = Material.SprucePlanks;
            for (int i = 5894; i <= 5895; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 31; i <= 32; i++)
                materials[i] = Material.SpruceSapling;
            for (int i = 4398; i <= 4429; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 12057; i <= 12062; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 8450; i <= 8529; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 6204; i <= 6267; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 5714; i <= 5721; i++)
                materials[i] = Material.SpruceWallHangingSign;
            for (int i = 4866; i <= 4873; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 204; i <= 206; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 2035; i <= 2046; i++)
                materials[i] = Material.StickyPiston;
            materials[1] = Material.Stone;
            for (int i = 12159; i <= 12164; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 7480; i <= 7559; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 16835; i <= 17158; i++)
                materials[i] = Material.StoneBrickWall;
            materials[6780] = Material.StoneBricks;
            for (int i = 5926; i <= 5949; i++)
                materials[i] = Material.StoneButton;
            for (int i = 5826; i <= 5827; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 12117; i <= 12122; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 14497; i <= 14576; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 19522; i <= 19525; i++)
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
            for (int i = 19660; i <= 19662; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 19654; i <= 19656; i++)
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
            for (int i = 19643; i <= 19645; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 19637; i <= 19639; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 20411; i <= 20414; i++)
                materials[i] = Material.StructureBlock;
            materials[13572] = Material.StructureVoid;
            for (int i = 5978; i <= 5993; i++)
                materials[i] = Material.SugarCane;
            for (int i = 11636; i <= 11637; i++)
                materials[i] = Material.Sunflower;
            for (int i = 125; i <= 128; i++)
                materials[i] = Material.SuspiciousGravel;
            for (int i = 119; i <= 122; i++)
                materials[i] = Material.SuspiciousSand;
            for (int i = 19630; i <= 19633; i++)
                materials[i] = Material.SweetBerryBush;
            materials[2053] = Material.TallDryGrass;
            for (int i = 11644; i <= 11645; i++)
                materials[i] = Material.TallGrass;
            for (int i = 2055; i <= 2056; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 20441; i <= 20456; i++)
                materials[i] = Material.Target;
            materials[11633] = Material.Terracotta;
            for (int i = 20427; i <= 20430; i++)
                materials[i] = Material.TestBlock;
            materials[20431] = Material.TestInstanceBlock;
            materials[23377] = Material.TintedGlass;
            for (int i = 2140; i <= 2141; i++)
                materials[i] = Material.Tnt;
            materials[2401] = Material.Torch;
            materials[2122] = Material.Torchflower;
            for (int i = 13518; i <= 13519; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 9928; i <= 9951; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 27730; i <= 27741; i++)
                materials[i] = Material.TrialSpawner;
            for (int i = 8321; i <= 8448; i++)
                materials[i] = Material.Tripwire;
            for (int i = 8305; i <= 8320; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 13878; i <= 13879; i++)
                materials[i] = Material.TubeCoral;
            materials[13863] = Material.TubeCoralBlock;
            for (int i = 13898; i <= 13899; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 13948; i <= 13955; i++)
                materials[i] = Material.TubeCoralWallFan;
            materials[22141] = Material.Tuff;
            for (int i = 22965; i <= 22970; i++)
                materials[i] = Material.TuffBrickSlab;
            for (int i = 22971; i <= 23050; i++)
                materials[i] = Material.TuffBrickStairs;
            for (int i = 23051; i <= 23374; i++)
                materials[i] = Material.TuffBrickWall;
            materials[22964] = Material.TuffBricks;
            for (int i = 22142; i <= 22147; i++)
                materials[i] = Material.TuffSlab;
            for (int i = 22148; i <= 22227; i++)
                materials[i] = Material.TuffStairs;
            for (int i = 22228; i <= 22551; i++)
                materials[i] = Material.TuffWall;
            for (int i = 13811; i <= 13822; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 19693; i <= 19718; i++)
                materials[i] = Material.TwistingVines;
            materials[19719] = Material.TwistingVinesPlant;
            for (int i = 27742; i <= 27773; i++)
                materials[i] = Material.Vault;
            for (int i = 27658; i <= 27660; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 7080; i <= 7111; i++)
                materials[i] = Material.Vine;
            materials[14013] = Material.VoidAir;
            for (int i = 2402; i <= 2405; i++)
                materials[i] = Material.WallTorch;
            for (int i = 20179; i <= 20202; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 20267; i <= 20330; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 19771; i <= 19802; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 19963; i <= 19994; i++)
                materials[i] = Material.WarpedFenceGate;
            materials[19647] = Material.WarpedFungus;
            for (int i = 5514; i <= 5577; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 19640; i <= 19642; i++)
                materials[i] = Material.WarpedHyphae;
            materials[19646] = Material.WarpedNylium;
            materials[19722] = Material.WarpedPlanks;
            for (int i = 19737; i <= 19738; i++)
                materials[i] = Material.WarpedPressurePlate;
            materials[19649] = Material.WarpedRoots;
            for (int i = 20363; i <= 20394; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 19729; i <= 19734; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 20075; i <= 20154; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 19634; i <= 19636; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 19867; i <= 19930; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 5786; i <= 5793; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 20403; i <= 20410; i++)
                materials[i] = Material.WarpedWallSign;
            materials[19648] = Material.WarpedWartBlock;
            for (int i = 86; i <= 101; i++)
                materials[i] = Material.Water;
            for (int i = 8183; i <= 8185; i++)
                materials[i] = Material.WaterCauldron;
            materials[24015] = Material.WaxedChiseledCopper;
            materials[24360] = Material.WaxedCopperBlock;
            for (int i = 25768; i <= 25771; i++)
                materials[i] = Material.WaxedCopperBulb;
            for (int i = 24968; i <= 25031; i++)
                materials[i] = Material.WaxedCopperDoor;
            for (int i = 25744; i <= 25745; i++)
                materials[i] = Material.WaxedCopperGrate;
            for (int i = 25480; i <= 25543; i++)
                materials[i] = Material.WaxedCopperTrapdoor;
            materials[24367] = Material.WaxedCutCopper;
            for (int i = 24706; i <= 24711; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 24608; i <= 24687; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            materials[24014] = Material.WaxedExposedChiseledCopper;
            materials[24362] = Material.WaxedExposedCopper;
            for (int i = 25772; i <= 25775; i++)
                materials[i] = Material.WaxedExposedCopperBulb;
            for (int i = 25032; i <= 25095; i++)
                materials[i] = Material.WaxedExposedCopperDoor;
            for (int i = 25746; i <= 25747; i++)
                materials[i] = Material.WaxedExposedCopperGrate;
            for (int i = 25544; i <= 25607; i++)
                materials[i] = Material.WaxedExposedCopperTrapdoor;
            materials[24366] = Material.WaxedExposedCutCopper;
            for (int i = 24700; i <= 24705; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 24528; i <= 24607; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            materials[24012] = Material.WaxedOxidizedChiseledCopper;
            materials[24363] = Material.WaxedOxidizedCopper;
            for (int i = 25780; i <= 25783; i++)
                materials[i] = Material.WaxedOxidizedCopperBulb;
            for (int i = 25096; i <= 25159; i++)
                materials[i] = Material.WaxedOxidizedCopperDoor;
            for (int i = 25750; i <= 25751; i++)
                materials[i] = Material.WaxedOxidizedCopperGrate;
            for (int i = 25608; i <= 25671; i++)
                materials[i] = Material.WaxedOxidizedCopperTrapdoor;
            materials[24364] = Material.WaxedOxidizedCutCopper;
            for (int i = 24688; i <= 24693; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 24368; i <= 24447; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            materials[24013] = Material.WaxedWeatheredChiseledCopper;
            materials[24361] = Material.WaxedWeatheredCopper;
            for (int i = 25776; i <= 25779; i++)
                materials[i] = Material.WaxedWeatheredCopperBulb;
            for (int i = 25160; i <= 25223; i++)
                materials[i] = Material.WaxedWeatheredCopperDoor;
            for (int i = 25748; i <= 25749; i++)
                materials[i] = Material.WaxedWeatheredCopperGrate;
            for (int i = 25672; i <= 25735; i++)
                materials[i] = Material.WaxedWeatheredCopperTrapdoor;
            materials[24365] = Material.WaxedWeatheredCutCopper;
            for (int i = 24694; i <= 24699; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 24448; i <= 24527; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            materials[24009] = Material.WeatheredChiseledCopper;
            materials[24000] = Material.WeatheredCopper;
            for (int i = 25760; i <= 25763; i++)
                materials[i] = Material.WeatheredCopperBulb;
            for (int i = 24904; i <= 24967; i++)
                materials[i] = Material.WeatheredCopperDoor;
            for (int i = 25740; i <= 25741; i++)
                materials[i] = Material.WeatheredCopperGrate;
            for (int i = 25416; i <= 25479; i++)
                materials[i] = Material.WeatheredCopperTrapdoor;
            materials[24005] = Material.WeatheredCutCopper;
            for (int i = 24342; i <= 24347; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 24096; i <= 24175; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 19666; i <= 19691; i++)
                materials[i] = Material.WeepingVines;
            materials[19692] = Material.WeepingVinesPlant;
            materials[561] = Material.WetSponge;
            for (int i = 4342; i <= 4349; i++)
                materials[i] = Material.Wheat;
            for (int i = 11648; i <= 11663; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 1731; i <= 1746; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 21801; i <= 21816; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 22059; i <= 22060; i++)
                materials[i] = Material.WhiteCandleCake;
            materials[11617] = Material.WhiteCarpet;
            materials[13751] = Material.WhiteConcrete;
            materials[13767] = Material.WhiteConcretePowder;
            for (int i = 13687; i <= 13690; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 13591; i <= 13596; i++)
                materials[i] = Material.WhiteShulkerBox;
            materials[6124] = Material.WhiteStainedGlass;
            for (int i = 10181; i <= 10212; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            materials[10165] = Material.WhiteTerracotta;
            materials[2129] = Material.WhiteTulip;
            for (int i = 11904; i <= 11907; i++)
                materials[i] = Material.WhiteWallBanner;
            materials[2093] = Material.WhiteWool;
            for (int i = 25903; i <= 25918; i++)
                materials[i] = Material.Wildflowers;
            materials[2133] = Material.WitherRose;
            for (int i = 9676; i <= 9707; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 9708; i <= 9715; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 11712; i <= 11727; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 1795; i <= 1810; i++)
                materials[i] = Material.YellowBed;
            for (int i = 21865; i <= 21880; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 22067; i <= 22068; i++)
                materials[i] = Material.YellowCandleCake;
            materials[11621] = Material.YellowCarpet;
            materials[13755] = Material.YellowConcrete;
            materials[13771] = Material.YellowConcretePowder;
            for (int i = 13703; i <= 13706; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 13615; i <= 13620; i++)
                materials[i] = Material.YellowShulkerBox;
            materials[6128] = Material.YellowStainedGlass;
            for (int i = 10309; i <= 10340; i++)
                materials[i] = Material.YellowStainedGlassPane;
            materials[10169] = Material.YellowTerracotta;
            for (int i = 11920; i <= 11923; i++)
                materials[i] = Material.YellowWallBanner;
            materials[2097] = Material.YellowWool;
            for (int i = 9716; i <= 9747; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 9748; i <= 9755; i++)
                materials[i] = Material.ZombieWallHead;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
