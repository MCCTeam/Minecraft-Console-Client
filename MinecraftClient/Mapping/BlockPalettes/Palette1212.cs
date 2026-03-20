using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette1212 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette1212()
        {
            for (int i = 8938; i <= 8961; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 12419; i <= 12482; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 12035; i <= 12066; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 11747; i <= 11778; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 5118; i <= 5181; i++)
                materials[i] = Material.AcaciaHangingSign;
            for (int i = 364; i <= 391; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 148; i <= 150; i++)
                materials[i] = Material.AcaciaLog;
            materials[19] = Material.AcaciaPlanks;
            for (int i = 5888; i <= 5889; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 37; i <= 38; i++)
                materials[i] = Material.AcaciaSapling;
            for (int i = 4450; i <= 4481; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 11521; i <= 11526; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 10139; i <= 10218; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 6383; i <= 6446; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 5718; i <= 5725; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 4870; i <= 4877; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 213; i <= 215; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 9575; i <= 9598; i++)
                materials[i] = Material.ActivatorRail;
            materials[0] = Material.Air;
            materials[2122] = Material.Allium;
            materials[21500] = Material.AmethystBlock;
            for (int i = 21502; i <= 21513; i++)
                materials[i] = Material.AmethystCluster;
            materials[19917] = Material.AncientDebris;
            materials[6] = Material.Andesite;
            for (int i = 14605; i <= 14610; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 14231; i <= 14310; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 17221; i <= 17544; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 9362; i <= 9365; i++)
                materials[i] = Material.Anvil;
            for (int i = 7047; i <= 7050; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 7043; i <= 7046; i++)
                materials[i] = Material.AttachedPumpkinStem;
            materials[25293] = Material.Azalea;
            for (int i = 504; i <= 531; i++)
                materials[i] = Material.AzaleaLeaves;
            materials[2123] = Material.AzureBluet;
            for (int i = 13414; i <= 13425; i++)
                materials[i] = Material.Bamboo;
            for (int i = 168; i <= 170; i++)
                materials[i] = Material.BambooBlock;
            for (int i = 9058; i <= 9081; i++)
                materials[i] = Material.BambooButton;
            for (int i = 12739; i <= 12802; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 12195; i <= 12226; i++)
                materials[i] = Material.BambooFence;
            for (int i = 11907; i <= 11938; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 5630; i <= 5693; i++)
                materials[i] = Material.BambooHangingSign;
            materials[28] = Material.BambooMosaic;
            for (int i = 11557; i <= 11562; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 10619; i <= 10698; i++)
                materials[i] = Material.BambooMosaicStairs;
            materials[27] = Material.BambooPlanks;
            for (int i = 5898; i <= 5899; i++)
                materials[i] = Material.BambooPressurePlate;
            materials[13413] = Material.BambooSapling;
            for (int i = 4642; i <= 4673; i++)
                materials[i] = Material.BambooSign;
            for (int i = 11551; i <= 11556; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 10539; i <= 10618; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 6703; i <= 6766; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 5782; i <= 5789; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 4918; i <= 4925; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 18877; i <= 18888; i++)
                materials[i] = Material.Barrel;
            for (int i = 10700; i <= 10701; i++)
                materials[i] = Material.Barrier;
            for (int i = 6018; i <= 6020; i++)
                materials[i] = Material.Basalt;
            materials[8148] = Material.Beacon;
            materials[85] = Material.Bedrock;
            for (int i = 19866; i <= 19889; i++)
                materials[i] = Material.BeeNest;
            for (int i = 19890; i <= 19913; i++)
                materials[i] = Material.Beehive;
            for (int i = 12978; i <= 12981; i++)
                materials[i] = Material.Beetroots;
            for (int i = 18940; i <= 18971; i++)
                materials[i] = Material.Bell;
            for (int i = 25313; i <= 25344; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 25345; i <= 25352; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 8890; i <= 8913; i++)
                materials[i] = Material.BirchButton;
            for (int i = 12291; i <= 12354; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 11971; i <= 12002; i++)
                materials[i] = Material.BirchFence;
            for (int i = 11683; i <= 11714; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 5054; i <= 5117; i++)
                materials[i] = Material.BirchHangingSign;
            for (int i = 308; i <= 335; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 142; i <= 144; i++)
                materials[i] = Material.BirchLog;
            materials[17] = Material.BirchPlanks;
            for (int i = 5884; i <= 5885; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 33; i <= 34; i++)
                materials[i] = Material.BirchSapling;
            for (int i = 4418; i <= 4449; i++)
                materials[i] = Material.BirchSign;
            for (int i = 11509; i <= 11514; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 7976; i <= 8055; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 6255; i <= 6318; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 5710; i <= 5717; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 4862; i <= 4869; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 207; i <= 209; i++)
                materials[i] = Material.BirchWood;
            for (int i = 11334; i <= 11349; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 1971; i <= 1986; i++)
                materials[i] = Material.BlackBed;
            for (int i = 21450; i <= 21465; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 21498; i <= 21499; i++)
                materials[i] = Material.BlackCandleCake;
            materials[11078] = Material.BlackCarpet;
            materials[13212] = Material.BlackConcrete;
            materials[13228] = Material.BlackConcretePowder;
            for (int i = 13193; i <= 13196; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 13127; i <= 13132; i++)
                materials[i] = Material.BlackShulkerBox;
            materials[6126] = Material.BlackStainedGlass;
            for (int i = 10107; i <= 10138; i++)
                materials[i] = Material.BlackStainedGlassPane;
            materials[9626] = Material.BlackTerracotta;
            for (int i = 11410; i <= 11413; i++)
                materials[i] = Material.BlackWallBanner;
            materials[2105] = Material.BlackWool;
            materials[19929] = Material.Blackstone;
            for (int i = 20334; i <= 20339; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 19930; i <= 20009; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 20010; i <= 20333; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 18897; i <= 18904; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 11270; i <= 11285; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 1907; i <= 1922; i++)
                materials[i] = Material.BlueBed;
            for (int i = 21386; i <= 21401; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 21490; i <= 21491; i++)
                materials[i] = Material.BlueCandleCake;
            materials[11074] = Material.BlueCarpet;
            materials[13208] = Material.BlueConcrete;
            materials[13224] = Material.BlueConcretePowder;
            for (int i = 13177; i <= 13180; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            materials[13410] = Material.BlueIce;
            materials[2121] = Material.BlueOrchid;
            for (int i = 13103; i <= 13108; i++)
                materials[i] = Material.BlueShulkerBox;
            materials[6122] = Material.BlueStainedGlass;
            for (int i = 9979; i <= 10010; i++)
                materials[i] = Material.BlueStainedGlassPane;
            materials[9622] = Material.BlueTerracotta;
            for (int i = 11394; i <= 11397; i++)
                materials[i] = Material.BlueWallBanner;
            materials[2101] = Material.BlueWool;
            for (int i = 13015; i <= 13017; i++)
                materials[i] = Material.BoneBlock;
            materials[2139] = Material.Bookshelf;
            for (int i = 13294; i <= 13295; i++)
                materials[i] = Material.BrainCoral;
            materials[13278] = Material.BrainCoralBlock;
            for (int i = 13314; i <= 13315; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 13370; i <= 13377; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 7620; i <= 7627; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 11599; i <= 11604; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 7259; i <= 7338; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 14629; i <= 14952; i++)
                materials[i] = Material.BrickWall;
            materials[2136] = Material.Bricks;
            for (int i = 11286; i <= 11301; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 1923; i <= 1938; i++)
                materials[i] = Material.BrownBed;
            for (int i = 21402; i <= 21417; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 21492; i <= 21493; i++)
                materials[i] = Material.BrownCandleCake;
            materials[11075] = Material.BrownCarpet;
            materials[13209] = Material.BrownConcrete;
            materials[13225] = Material.BrownConcretePowder;
            for (int i = 13181; i <= 13184; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            materials[2132] = Material.BrownMushroom;
            for (int i = 6779; i <= 6842; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 13109; i <= 13114; i++)
                materials[i] = Material.BrownShulkerBox;
            materials[6123] = Material.BrownStainedGlass;
            for (int i = 10011; i <= 10042; i++)
                materials[i] = Material.BrownStainedGlassPane;
            materials[9623] = Material.BrownTerracotta;
            for (int i = 11398; i <= 11401; i++)
                materials[i] = Material.BrownWallBanner;
            materials[2102] = Material.BrownWool;
            for (int i = 13429; i <= 13430; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 13296; i <= 13297; i++)
                materials[i] = Material.BubbleCoral;
            materials[13279] = Material.BubbleCoralBlock;
            for (int i = 13316; i <= 13317; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 13378; i <= 13385; i++)
                materials[i] = Material.BubbleCoralWallFan;
            materials[21501] = Material.BuddingAmethyst;
            for (int i = 5948; i <= 5963; i++)
                materials[i] = Material.Cactus;
            for (int i = 6040; i <= 6046; i++)
                materials[i] = Material.Cake;
            materials[22785] = Material.Calcite;
            for (int i = 22884; i <= 23267; i++)
                materials[i] = Material.CalibratedSculkSensor;
            for (int i = 18980; i <= 19011; i++)
                materials[i] = Material.Campfire;
            for (int i = 21194; i <= 21209; i++)
                materials[i] = Material.Candle;
            for (int i = 21466; i <= 21467; i++)
                materials[i] = Material.CandleCake;
            for (int i = 8826; i <= 8833; i++)
                materials[i] = Material.Carrots;
            materials[18905] = Material.CartographyTable;
            for (int i = 6032; i <= 6035; i++)
                materials[i] = Material.CarvedPumpkin;
            materials[7628] = Material.Cauldron;
            materials[13428] = Material.CaveAir;
            for (int i = 25238; i <= 25289; i++)
                materials[i] = Material.CaveVines;
            for (int i = 25290; i <= 25291; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 7003; i <= 7008; i++)
                materials[i] = Material.Chain;
            for (int i = 12996; i <= 13007; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 8962; i <= 8985; i++)
                materials[i] = Material.CherryButton;
            for (int i = 12483; i <= 12546; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 12067; i <= 12098; i++)
                materials[i] = Material.CherryFence;
            for (int i = 11779; i <= 11810; i++)
                materials[i] = Material.CherryFenceGate;
            for (int i = 5182; i <= 5245; i++)
                materials[i] = Material.CherryHangingSign;
            for (int i = 392; i <= 419; i++)
                materials[i] = Material.CherryLeaves;
            for (int i = 151; i <= 153; i++)
                materials[i] = Material.CherryLog;
            materials[20] = Material.CherryPlanks;
            for (int i = 5890; i <= 5891; i++)
                materials[i] = Material.CherryPressurePlate;
            for (int i = 39; i <= 40; i++)
                materials[i] = Material.CherrySapling;
            for (int i = 4482; i <= 4513; i++)
                materials[i] = Material.CherrySign;
            for (int i = 11527; i <= 11532; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 10219; i <= 10298; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 6447; i <= 6510; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 5726; i <= 5733; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 4878; i <= 4885; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 216; i <= 218; i++)
                materials[i] = Material.CherryWood;
            for (int i = 3006; i <= 3029; i++)
                materials[i] = Material.Chest;
            for (int i = 9366; i <= 9369; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 2140; i <= 2395; i++)
                materials[i] = Material.ChiseledBookshelf;
            materials[23420] = Material.ChiseledCopper;
            materials[27020] = Material.ChiseledDeepslate;
            materials[21191] = Material.ChiseledNetherBricks;
            materials[20343] = Material.ChiseledPolishedBlackstone;
            materials[9491] = Material.ChiseledQuartzBlock;
            materials[11415] = Material.ChiseledRedSandstone;
            materials[579] = Material.ChiseledSandstone;
            materials[6770] = Material.ChiseledStoneBricks;
            materials[22372] = Material.ChiseledTuff;
            materials[22784] = Material.ChiseledTuffBricks;
            for (int i = 12873; i <= 12878; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 12809; i <= 12872; i++)
                materials[i] = Material.ChorusPlant;
            materials[5964] = Material.Clay;
            materials[11080] = Material.CoalBlock;
            materials[133] = Material.CoalOre;
            materials[11] = Material.CoarseDirt;
            materials[25376] = Material.CobbledDeepslate;
            for (int i = 25457; i <= 25462; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 25377; i <= 25456; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 25463; i <= 25786; i++)
                materials[i] = Material.CobbledDeepslateWall;
            materials[14] = Material.Cobblestone;
            for (int i = 11593; i <= 11598; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 4766; i <= 4845; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 8149; i <= 8472; i++)
                materials[i] = Material.CobblestoneWall;
            materials[2047] = Material.Cobweb;
            for (int i = 7649; i <= 7660; i++)
                materials[i] = Material.Cocoa;
            for (int i = 8136; i <= 8147; i++)
                materials[i] = Material.CommandBlock;
            for (int i = 9430; i <= 9445; i++)
                materials[i] = Material.Comparator;
            for (int i = 19841; i <= 19849; i++)
                materials[i] = Material.Composter;
            for (int i = 13411; i <= 13412; i++)
                materials[i] = Material.Conduit;
            materials[23407] = Material.CopperBlock;
            for (int i = 25161; i <= 25164; i++)
                materials[i] = Material.CopperBulb;
            for (int i = 24121; i <= 24184; i++)
                materials[i] = Material.CopperDoor;
            for (int i = 25145; i <= 25146; i++)
                materials[i] = Material.CopperGrate;
            materials[23411] = Material.CopperOre;
            for (int i = 24633; i <= 24696; i++)
                materials[i] = Material.CopperTrapdoor;
            materials[2129] = Material.Cornflower;
            materials[27021] = Material.CrackedDeepslateBricks;
            materials[27022] = Material.CrackedDeepslateTiles;
            materials[21192] = Material.CrackedNetherBricks;
            materials[20342] = Material.CrackedPolishedBlackstoneBricks;
            materials[6769] = Material.CrackedStoneBricks;
            for (int i = 27059; i <= 27106; i++)
                materials[i] = Material.Crafter;
            materials[4329] = Material.CraftingTable;
            for (int i = 2917; i <= 2925; i++)
                materials[i] = Material.CreakingHeart;
            for (int i = 9242; i <= 9273; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 9274; i <= 9281; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 19569; i <= 19592; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 19617; i <= 19680; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 19153; i <= 19184; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 19345; i <= 19376; i++)
                materials[i] = Material.CrimsonFenceGate;
            materials[19078] = Material.CrimsonFungus;
            for (int i = 5438; i <= 5501; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 19071; i <= 19073; i++)
                materials[i] = Material.CrimsonHyphae;
            materials[19077] = Material.CrimsonNylium;
            materials[19135] = Material.CrimsonPlanks;
            for (int i = 19149; i <= 19150; i++)
                materials[i] = Material.CrimsonPressurePlate;
            materials[19134] = Material.CrimsonRoots;
            for (int i = 19745; i <= 19776; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 19137; i <= 19142; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 19409; i <= 19488; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 19065; i <= 19067; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 19217; i <= 19280; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 5766; i <= 5773; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 19809; i <= 19816; i++)
                materials[i] = Material.CrimsonWallSign;
            materials[19918] = Material.CryingObsidian;
            materials[23416] = Material.CutCopper;
            for (int i = 23763; i <= 23768; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 23665; i <= 23744; i++)
                materials[i] = Material.CutCopperStairs;
            materials[11416] = Material.CutRedSandstone;
            for (int i = 11635; i <= 11640; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            materials[580] = Material.CutSandstone;
            for (int i = 11581; i <= 11586; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 11238; i <= 11253; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 1875; i <= 1890; i++)
                materials[i] = Material.CyanBed;
            for (int i = 21354; i <= 21369; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 21486; i <= 21487; i++)
                materials[i] = Material.CyanCandleCake;
            materials[11072] = Material.CyanCarpet;
            materials[13206] = Material.CyanConcrete;
            materials[13222] = Material.CyanConcretePowder;
            for (int i = 13169; i <= 13172; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 13091; i <= 13096; i++)
                materials[i] = Material.CyanShulkerBox;
            materials[6120] = Material.CyanStainedGlass;
            for (int i = 9915; i <= 9946; i++)
                materials[i] = Material.CyanStainedGlassPane;
            materials[9620] = Material.CyanTerracotta;
            for (int i = 11386; i <= 11389; i++)
                materials[i] = Material.CyanWallBanner;
            materials[2099] = Material.CyanWool;
            for (int i = 9370; i <= 9373; i++)
                materials[i] = Material.DamagedAnvil;
            materials[2118] = Material.Dandelion;
            for (int i = 8986; i <= 9009; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 12547; i <= 12610; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 12099; i <= 12130; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 11811; i <= 11842; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 5310; i <= 5373; i++)
                materials[i] = Material.DarkOakHangingSign;
            for (int i = 420; i <= 447; i++)
                materials[i] = Material.DarkOakLeaves;
            for (int i = 154; i <= 156; i++)
                materials[i] = Material.DarkOakLog;
            materials[21] = Material.DarkOakPlanks;
            for (int i = 5892; i <= 5893; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 41; i <= 42; i++)
                materials[i] = Material.DarkOakSapling;
            for (int i = 4546; i <= 4577; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 11533; i <= 11538; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 10299; i <= 10378; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 6511; i <= 6574; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 5742; i <= 5749; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 4894; i <= 4901; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 219; i <= 221; i++)
                materials[i] = Material.DarkOakWood;
            materials[10800] = Material.DarkPrismarine;
            for (int i = 11053; i <= 11058; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 10961; i <= 11040; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 9446; i <= 9477; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 13284; i <= 13285; i++)
                materials[i] = Material.DeadBrainCoral;
            materials[13273] = Material.DeadBrainCoralBlock;
            for (int i = 13304; i <= 13305; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 13330; i <= 13337; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 13286; i <= 13287; i++)
                materials[i] = Material.DeadBubbleCoral;
            materials[13274] = Material.DeadBubbleCoralBlock;
            for (int i = 13306; i <= 13307; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 13338; i <= 13345; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            materials[2050] = Material.DeadBush;
            for (int i = 13288; i <= 13289; i++)
                materials[i] = Material.DeadFireCoral;
            materials[13275] = Material.DeadFireCoralBlock;
            for (int i = 13308; i <= 13309; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 13346; i <= 13353; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 13290; i <= 13291; i++)
                materials[i] = Material.DeadHornCoral;
            materials[13276] = Material.DeadHornCoralBlock;
            for (int i = 13310; i <= 13311; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 13354; i <= 13361; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 13282; i <= 13283; i++)
                materials[i] = Material.DeadTubeCoral;
            materials[13272] = Material.DeadTubeCoralBlock;
            for (int i = 13302; i <= 13303; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 13322; i <= 13329; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 27043; i <= 27058; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 25373; i <= 25375; i++)
                materials[i] = Material.Deepslate;
            for (int i = 26690; i <= 26695; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 26610; i <= 26689; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 26696; i <= 27019; i++)
                materials[i] = Material.DeepslateBrickWall;
            materials[26609] = Material.DeepslateBricks;
            materials[134] = Material.DeepslateCoalOre;
            materials[23412] = Material.DeepslateCopperOre;
            materials[4327] = Material.DeepslateDiamondOre;
            materials[7742] = Material.DeepslateEmeraldOre;
            materials[130] = Material.DeepslateGoldOre;
            materials[132] = Material.DeepslateIronOre;
            materials[564] = Material.DeepslateLapisOre;
            for (int i = 5902; i <= 5903; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 26279; i <= 26284; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 26199; i <= 26278; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 26285; i <= 26608; i++)
                materials[i] = Material.DeepslateTileWall;
            materials[26198] = Material.DeepslateTiles;
            for (int i = 2011; i <= 2034; i++)
                materials[i] = Material.DetectorRail;
            materials[4328] = Material.DiamondBlock;
            materials[4326] = Material.DiamondOre;
            materials[4] = Material.Diorite;
            for (int i = 14623; i <= 14628; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 14471; i <= 14550; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 18517; i <= 18840; i++)
                materials[i] = Material.DioriteWall;
            materials[10] = Material.Dirt;
            materials[12982] = Material.DirtPath;
            for (int i = 566; i <= 577; i++)
                materials[i] = Material.Dispenser;
            materials[7646] = Material.DragonEgg;
            for (int i = 9282; i <= 9313; i++)
                materials[i] = Material.DragonHead;
            for (int i = 9314; i <= 9321; i++)
                materials[i] = Material.DragonWallHead;
            materials[13256] = Material.DriedKelpBlock;
            materials[25237] = Material.DripstoneBlock;
            for (int i = 9599; i <= 9610; i++)
                materials[i] = Material.Dropper;
            materials[7895] = Material.EmeraldBlock;
            materials[7741] = Material.EmeraldOre;
            materials[7619] = Material.EnchantingTable;
            materials[12983] = Material.EndGateway;
            materials[7636] = Material.EndPortal;
            for (int i = 7637; i <= 7644; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 12803; i <= 12808; i++)
                materials[i] = Material.EndRod;
            materials[7645] = Material.EndStone;
            for (int i = 14581; i <= 14586; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 13831; i <= 13910; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 18193; i <= 18516; i++)
                materials[i] = Material.EndStoneBrickWall;
            materials[12963] = Material.EndStoneBricks;
            for (int i = 7743; i <= 7750; i++)
                materials[i] = Material.EnderChest;
            materials[23419] = Material.ExposedChiseledCopper;
            materials[23408] = Material.ExposedCopper;
            for (int i = 25165; i <= 25168; i++)
                materials[i] = Material.ExposedCopperBulb;
            for (int i = 24185; i <= 24248; i++)
                materials[i] = Material.ExposedCopperDoor;
            for (int i = 25147; i <= 25148; i++)
                materials[i] = Material.ExposedCopperGrate;
            for (int i = 24697; i <= 24760; i++)
                materials[i] = Material.ExposedCopperTrapdoor;
            materials[23415] = Material.ExposedCutCopper;
            for (int i = 23757; i <= 23762; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 23585; i <= 23664; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 4338; i <= 4345; i++)
                materials[i] = Material.Farmland;
            materials[2049] = Material.Fern;
            for (int i = 2403; i <= 2914; i++)
                materials[i] = Material.Fire;
            for (int i = 13298; i <= 13299; i++)
                materials[i] = Material.FireCoral;
            materials[13280] = Material.FireCoralBlock;
            for (int i = 13318; i <= 13319; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 13386; i <= 13393; i++)
                materials[i] = Material.FireCoralWallFan;
            materials[18906] = Material.FletchingTable;
            materials[8797] = Material.FlowerPot;
            materials[25294] = Material.FloweringAzalea;
            for (int i = 532; i <= 559; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            materials[27041] = Material.Frogspawn;
            for (int i = 13008; i <= 13011; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 4346; i <= 4353; i++)
                materials[i] = Material.Furnace;
            materials[20754] = Material.GildedBlackstone;
            materials[562] = Material.Glass;
            for (int i = 7009; i <= 7040; i++)
                materials[i] = Material.GlassPane;
            for (int i = 7099; i <= 7226; i++)
                materials[i] = Material.GlowLichen;
            materials[6029] = Material.Glowstone;
            materials[2134] = Material.GoldBlock;
            materials[129] = Material.GoldOre;
            materials[2] = Material.Granite;
            for (int i = 14599; i <= 14604; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 14151; i <= 14230; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 15925; i <= 16248; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            materials[124] = Material.Gravel;
            for (int i = 11206; i <= 11221; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 1843; i <= 1858; i++)
                materials[i] = Material.GrayBed;
            for (int i = 21322; i <= 21337; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 21482; i <= 21483; i++)
                materials[i] = Material.GrayCandleCake;
            materials[11070] = Material.GrayCarpet;
            materials[13204] = Material.GrayConcrete;
            materials[13220] = Material.GrayConcretePowder;
            for (int i = 13161; i <= 13164; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 13079; i <= 13084; i++)
                materials[i] = Material.GrayShulkerBox;
            materials[6118] = Material.GrayStainedGlass;
            for (int i = 9851; i <= 9882; i++)
                materials[i] = Material.GrayStainedGlassPane;
            materials[9618] = Material.GrayTerracotta;
            for (int i = 11378; i <= 11381; i++)
                materials[i] = Material.GrayWallBanner;
            materials[2097] = Material.GrayWool;
            for (int i = 11302; i <= 11317; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 1939; i <= 1954; i++)
                materials[i] = Material.GreenBed;
            for (int i = 21418; i <= 21433; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 21494; i <= 21495; i++)
                materials[i] = Material.GreenCandleCake;
            materials[11076] = Material.GreenCarpet;
            materials[13210] = Material.GreenConcrete;
            materials[13226] = Material.GreenConcretePowder;
            for (int i = 13185; i <= 13188; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 13115; i <= 13120; i++)
                materials[i] = Material.GreenShulkerBox;
            materials[6124] = Material.GreenStainedGlass;
            for (int i = 10043; i <= 10074; i++)
                materials[i] = Material.GreenStainedGlassPane;
            materials[9624] = Material.GreenTerracotta;
            for (int i = 11402; i <= 11405; i++)
                materials[i] = Material.GreenWallBanner;
            materials[2103] = Material.GreenWool;
            for (int i = 18907; i <= 18918; i++)
                materials[i] = Material.Grindstone;
            for (int i = 25369; i <= 25370; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 11060; i <= 11062; i++)
                materials[i] = Material.HayBlock;
            for (int i = 27151; i <= 27152; i++)
                materials[i] = Material.HeavyCore;
            for (int i = 9414; i <= 9429; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            materials[19914] = Material.HoneyBlock;
            materials[19915] = Material.HoneycombBlock;
            for (int i = 9480; i <= 9489; i++)
                materials[i] = Material.Hopper;
            for (int i = 13300; i <= 13301; i++)
                materials[i] = Material.HornCoral;
            materials[13281] = Material.HornCoralBlock;
            for (int i = 13320; i <= 13321; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 13394; i <= 13401; i++)
                materials[i] = Material.HornCoralWallFan;
            materials[5946] = Material.Ice;
            materials[6778] = Material.InfestedChiseledStoneBricks;
            materials[6774] = Material.InfestedCobblestone;
            materials[6777] = Material.InfestedCrackedStoneBricks;
            for (int i = 27023; i <= 27025; i++)
                materials[i] = Material.InfestedDeepslate;
            materials[6776] = Material.InfestedMossyStoneBricks;
            materials[6773] = Material.InfestedStone;
            materials[6775] = Material.InfestedStoneBricks;
            for (int i = 6971; i <= 7002; i++)
                materials[i] = Material.IronBars;
            materials[2135] = Material.IronBlock;
            for (int i = 5816; i <= 5879; i++)
                materials[i] = Material.IronDoor;
            materials[131] = Material.IronOre;
            for (int i = 10734; i <= 10797; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 6036; i <= 6039; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 19829; i <= 19840; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 5981; i <= 5982; i++)
                materials[i] = Material.Jukebox;
            for (int i = 8914; i <= 8937; i++)
                materials[i] = Material.JungleButton;
            for (int i = 12355; i <= 12418; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 12003; i <= 12034; i++)
                materials[i] = Material.JungleFence;
            for (int i = 11715; i <= 11746; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 5246; i <= 5309; i++)
                materials[i] = Material.JungleHangingSign;
            for (int i = 336; i <= 363; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 145; i <= 147; i++)
                materials[i] = Material.JungleLog;
            materials[18] = Material.JunglePlanks;
            for (int i = 5886; i <= 5887; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 35; i <= 36; i++)
                materials[i] = Material.JungleSapling;
            for (int i = 4514; i <= 4545; i++)
                materials[i] = Material.JungleSign;
            for (int i = 11515; i <= 11520; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 8056; i <= 8135; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 6319; i <= 6382; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 5734; i <= 5741; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 4886; i <= 4893; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 210; i <= 212; i++)
                materials[i] = Material.JungleWood;
            for (int i = 13229; i <= 13254; i++)
                materials[i] = Material.Kelp;
            materials[13255] = Material.KelpPlant;
            for (int i = 4738; i <= 4745; i++)
                materials[i] = Material.Ladder;
            for (int i = 18972; i <= 18975; i++)
                materials[i] = Material.Lantern;
            materials[565] = Material.LapisBlock;
            materials[563] = Material.LapisOre;
            for (int i = 21514; i <= 21525; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 11092; i <= 11093; i++)
                materials[i] = Material.LargeFern;
            for (int i = 102; i <= 117; i++)
                materials[i] = Material.Lava;
            materials[7632] = Material.LavaCauldron;
            for (int i = 18919; i <= 18934; i++)
                materials[i] = Material.Lectern;
            for (int i = 5790; i <= 5813; i++)
                materials[i] = Material.Lever;
            for (int i = 10702; i <= 10733; i++)
                materials[i] = Material.Light;
            for (int i = 11142; i <= 11157; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 1779; i <= 1794; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 21258; i <= 21273; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 21474; i <= 21475; i++)
                materials[i] = Material.LightBlueCandleCake;
            materials[11066] = Material.LightBlueCarpet;
            materials[13200] = Material.LightBlueConcrete;
            materials[13216] = Material.LightBlueConcretePowder;
            for (int i = 13145; i <= 13148; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 13055; i <= 13060; i++)
                materials[i] = Material.LightBlueShulkerBox;
            materials[6114] = Material.LightBlueStainedGlass;
            for (int i = 9723; i <= 9754; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            materials[9614] = Material.LightBlueTerracotta;
            for (int i = 11362; i <= 11365; i++)
                materials[i] = Material.LightBlueWallBanner;
            materials[2093] = Material.LightBlueWool;
            for (int i = 11222; i <= 11237; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 1859; i <= 1874; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 21338; i <= 21353; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 21484; i <= 21485; i++)
                materials[i] = Material.LightGrayCandleCake;
            materials[11071] = Material.LightGrayCarpet;
            materials[13205] = Material.LightGrayConcrete;
            materials[13221] = Material.LightGrayConcretePowder;
            for (int i = 13165; i <= 13168; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 13085; i <= 13090; i++)
                materials[i] = Material.LightGrayShulkerBox;
            materials[6119] = Material.LightGrayStainedGlass;
            for (int i = 9883; i <= 9914; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            materials[9619] = Material.LightGrayTerracotta;
            for (int i = 11382; i <= 11385; i++)
                materials[i] = Material.LightGrayWallBanner;
            materials[2098] = Material.LightGrayWool;
            for (int i = 9398; i <= 9413; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 25193; i <= 25216; i++)
                materials[i] = Material.LightningRod;
            for (int i = 11084; i <= 11085; i++)
                materials[i] = Material.Lilac;
            materials[2131] = Material.LilyOfTheValley;
            materials[7501] = Material.LilyPad;
            for (int i = 11174; i <= 11189; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 1811; i <= 1826; i++)
                materials[i] = Material.LimeBed;
            for (int i = 21290; i <= 21305; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 21478; i <= 21479; i++)
                materials[i] = Material.LimeCandleCake;
            materials[11068] = Material.LimeCarpet;
            materials[13202] = Material.LimeConcrete;
            materials[13218] = Material.LimeConcretePowder;
            for (int i = 13153; i <= 13156; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 13067; i <= 13072; i++)
                materials[i] = Material.LimeShulkerBox;
            materials[6116] = Material.LimeStainedGlass;
            for (int i = 9787; i <= 9818; i++)
                materials[i] = Material.LimeStainedGlassPane;
            materials[9616] = Material.LimeTerracotta;
            for (int i = 11370; i <= 11373; i++)
                materials[i] = Material.LimeWallBanner;
            materials[2095] = Material.LimeWool;
            materials[19928] = Material.Lodestone;
            for (int i = 18873; i <= 18876; i++)
                materials[i] = Material.Loom;
            for (int i = 11126; i <= 11141; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 1763; i <= 1778; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 21242; i <= 21257; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 21472; i <= 21473; i++)
                materials[i] = Material.MagentaCandleCake;
            materials[11065] = Material.MagentaCarpet;
            materials[13199] = Material.MagentaConcrete;
            materials[13215] = Material.MagentaConcretePowder;
            for (int i = 13141; i <= 13144; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 13049; i <= 13054; i++)
                materials[i] = Material.MagentaShulkerBox;
            materials[6113] = Material.MagentaStainedGlass;
            for (int i = 9691; i <= 9722; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            materials[9613] = Material.MagentaTerracotta;
            for (int i = 11358; i <= 11361; i++)
                materials[i] = Material.MagentaWallBanner;
            materials[2092] = Material.MagentaWool;
            materials[13012] = Material.MagmaBlock;
            for (int i = 9034; i <= 9057; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 12675; i <= 12738; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 12163; i <= 12194; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 11875; i <= 11906; i++)
                materials[i] = Material.MangroveFenceGate;
            for (int i = 5566; i <= 5629; i++)
                materials[i] = Material.MangroveHangingSign;
            for (int i = 476; i <= 503; i++)
                materials[i] = Material.MangroveLeaves;
            for (int i = 160; i <= 162; i++)
                materials[i] = Material.MangroveLog;
            materials[26] = Material.MangrovePlanks;
            for (int i = 5896; i <= 5897; i++)
                materials[i] = Material.MangrovePressurePlate;
            for (int i = 45; i <= 84; i++)
                materials[i] = Material.MangrovePropagule;
            for (int i = 163; i <= 164; i++)
                materials[i] = Material.MangroveRoots;
            for (int i = 4610; i <= 4641; i++)
                materials[i] = Material.MangroveSign;
            for (int i = 11545; i <= 11550; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 10459; i <= 10538; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 6639; i <= 6702; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 5758; i <= 5765; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 4910; i <= 4917; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 222; i <= 224; i++)
                materials[i] = Material.MangroveWood;
            for (int i = 21526; i <= 21537; i++)
                materials[i] = Material.MediumAmethystBud;
            materials[7042] = Material.Melon;
            for (int i = 7059; i <= 7066; i++)
                materials[i] = Material.MelonStem;
            materials[25312] = Material.MossBlock;
            materials[25295] = Material.MossCarpet;
            materials[2396] = Material.MossyCobblestone;
            for (int i = 14575; i <= 14580; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 13751; i <= 13830; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 8473; i <= 8796; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 14563; i <= 14568; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 13591; i <= 13670; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 15601; i <= 15924; i++)
                materials[i] = Material.MossyStoneBrickWall;
            materials[6768] = Material.MossyStoneBricks;
            for (int i = 2106; i <= 2117; i++)
                materials[i] = Material.MovingPiston;
            materials[25372] = Material.Mud;
            for (int i = 11611; i <= 11616; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 7419; i <= 7498; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 16573; i <= 16896; i++)
                materials[i] = Material.MudBrickWall;
            materials[6772] = Material.MudBricks;
            for (int i = 165; i <= 167; i++)
                materials[i] = Material.MuddyMangroveRoots;
            for (int i = 6907; i <= 6970; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 7499; i <= 7500; i++)
                materials[i] = Material.Mycelium;
            for (int i = 7503; i <= 7534; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 11617; i <= 11622; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 7535; i <= 7614; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 16897; i <= 17220; i++)
                materials[i] = Material.NetherBrickWall;
            materials[7502] = Material.NetherBricks;
            materials[135] = Material.NetherGoldOre;
            for (int i = 6030; i <= 6031; i++)
                materials[i] = Material.NetherPortal;
            materials[9479] = Material.NetherQuartzOre;
            materials[19064] = Material.NetherSprouts;
            for (int i = 7615; i <= 7618; i++)
                materials[i] = Material.NetherWart;
            materials[13013] = Material.NetherWartBlock;
            materials[19916] = Material.NetheriteBlock;
            materials[6015] = Material.Netherrack;
            for (int i = 581; i <= 1730; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 8842; i <= 8865; i++)
                materials[i] = Material.OakButton;
            for (int i = 4674; i <= 4737; i++)
                materials[i] = Material.OakDoor;
            for (int i = 5983; i <= 6014; i++)
                materials[i] = Material.OakFence;
            for (int i = 7227; i <= 7258; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 4926; i <= 4989; i++)
                materials[i] = Material.OakHangingSign;
            for (int i = 252; i <= 279; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 136; i <= 138; i++)
                materials[i] = Material.OakLog;
            materials[15] = Material.OakPlanks;
            for (int i = 5880; i <= 5881; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 29; i <= 30; i++)
                materials[i] = Material.OakSapling;
            for (int i = 4354; i <= 4385; i++)
                materials[i] = Material.OakSign;
            for (int i = 11497; i <= 11502; i++)
                materials[i] = Material.OakSlab;
            for (int i = 2926; i <= 3005; i++)
                materials[i] = Material.OakStairs;
            for (int i = 6127; i <= 6190; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 5694; i <= 5701; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 4846; i <= 4853; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 201; i <= 203; i++)
                materials[i] = Material.OakWood;
            for (int i = 13019; i <= 13030; i++)
                materials[i] = Material.Observer;
            materials[2397] = Material.Obsidian;
            for (int i = 27032; i <= 27034; i++)
                materials[i] = Material.OchreFroglight;
            for (int i = 11110; i <= 11125; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 1747; i <= 1762; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 21226; i <= 21241; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 21470; i <= 21471; i++)
                materials[i] = Material.OrangeCandleCake;
            materials[11064] = Material.OrangeCarpet;
            materials[13198] = Material.OrangeConcrete;
            materials[13214] = Material.OrangeConcretePowder;
            for (int i = 13137; i <= 13140; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 13043; i <= 13048; i++)
                materials[i] = Material.OrangeShulkerBox;
            materials[6112] = Material.OrangeStainedGlass;
            for (int i = 9659; i <= 9690; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            materials[9612] = Material.OrangeTerracotta;
            materials[2125] = Material.OrangeTulip;
            for (int i = 11354; i <= 11357; i++)
                materials[i] = Material.OrangeWallBanner;
            materials[2091] = Material.OrangeWool;
            materials[2128] = Material.OxeyeDaisy;
            materials[23417] = Material.OxidizedChiseledCopper;
            materials[23410] = Material.OxidizedCopper;
            for (int i = 25173; i <= 25176; i++)
                materials[i] = Material.OxidizedCopperBulb;
            for (int i = 24249; i <= 24312; i++)
                materials[i] = Material.OxidizedCopperDoor;
            for (int i = 25151; i <= 25152; i++)
                materials[i] = Material.OxidizedCopperGrate;
            for (int i = 24761; i <= 24824; i++)
                materials[i] = Material.OxidizedCopperTrapdoor;
            materials[23413] = Material.OxidizedCutCopper;
            for (int i = 23745; i <= 23750; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 23425; i <= 23504; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            materials[11081] = Material.PackedIce;
            materials[6771] = Material.PackedMud;
            for (int i = 27316; i <= 27317; i++)
                materials[i] = Material.PaleHangingMoss;
            materials[27153] = Material.PaleMossBlock;
            for (int i = 27154; i <= 27315; i++)
                materials[i] = Material.PaleMossCarpet;
            for (int i = 9010; i <= 9033; i++)
                materials[i] = Material.PaleOakButton;
            for (int i = 12611; i <= 12674; i++)
                materials[i] = Material.PaleOakDoor;
            for (int i = 12131; i <= 12162; i++)
                materials[i] = Material.PaleOakFence;
            for (int i = 11843; i <= 11874; i++)
                materials[i] = Material.PaleOakFenceGate;
            for (int i = 5374; i <= 5437; i++)
                materials[i] = Material.PaleOakHangingSign;
            for (int i = 448; i <= 475; i++)
                materials[i] = Material.PaleOakLeaves;
            for (int i = 157; i <= 159; i++)
                materials[i] = Material.PaleOakLog;
            materials[25] = Material.PaleOakPlanks;
            for (int i = 5894; i <= 5895; i++)
                materials[i] = Material.PaleOakPressurePlate;
            for (int i = 43; i <= 44; i++)
                materials[i] = Material.PaleOakSapling;
            for (int i = 4578; i <= 4609; i++)
                materials[i] = Material.PaleOakSign;
            for (int i = 11539; i <= 11544; i++)
                materials[i] = Material.PaleOakSlab;
            for (int i = 10379; i <= 10458; i++)
                materials[i] = Material.PaleOakStairs;
            for (int i = 6575; i <= 6638; i++)
                materials[i] = Material.PaleOakTrapdoor;
            for (int i = 5750; i <= 5757; i++)
                materials[i] = Material.PaleOakWallHangingSign;
            for (int i = 4902; i <= 4909; i++)
                materials[i] = Material.PaleOakWallSign;
            for (int i = 22; i <= 24; i++)
                materials[i] = Material.PaleOakWood;
            for (int i = 27038; i <= 27040; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 11088; i <= 11089; i++)
                materials[i] = Material.Peony;
            for (int i = 11587; i <= 11592; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 9322; i <= 9353; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 9354; i <= 9361; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 11190; i <= 11205; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 1827; i <= 1842; i++)
                materials[i] = Material.PinkBed;
            for (int i = 21306; i <= 21321; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 21480; i <= 21481; i++)
                materials[i] = Material.PinkCandleCake;
            materials[11069] = Material.PinkCarpet;
            materials[13203] = Material.PinkConcrete;
            materials[13219] = Material.PinkConcretePowder;
            for (int i = 13157; i <= 13160; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 25296; i <= 25311; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 13073; i <= 13078; i++)
                materials[i] = Material.PinkShulkerBox;
            materials[6117] = Material.PinkStainedGlass;
            for (int i = 9819; i <= 9850; i++)
                materials[i] = Material.PinkStainedGlassPane;
            materials[9617] = Material.PinkTerracotta;
            materials[2127] = Material.PinkTulip;
            for (int i = 11374; i <= 11377; i++)
                materials[i] = Material.PinkWallBanner;
            materials[2096] = Material.PinkWool;
            for (int i = 2054; i <= 2065; i++)
                materials[i] = Material.Piston;
            for (int i = 2066; i <= 2089; i++)
                materials[i] = Material.PistonHead;
            for (int i = 12966; i <= 12975; i++)
                materials[i] = Material.PitcherCrop;
            for (int i = 12976; i <= 12977; i++)
                materials[i] = Material.PitcherPlant;
            for (int i = 9202; i <= 9233; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 9234; i <= 9241; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            for (int i = 25217; i <= 25236; i++)
                materials[i] = Material.PointedDripstone;
            materials[7] = Material.PolishedAndesite;
            for (int i = 14617; i <= 14622; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 14391; i <= 14470; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 6021; i <= 6023; i++)
                materials[i] = Material.PolishedBasalt;
            materials[20340] = Material.PolishedBlackstone;
            for (int i = 20344; i <= 20349; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 20350; i <= 20429; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 20430; i <= 20753; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            materials[20341] = Material.PolishedBlackstoneBricks;
            for (int i = 20843; i <= 20866; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 20841; i <= 20842; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 20835; i <= 20840; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 20755; i <= 20834; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 20867; i <= 21190; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            materials[25787] = Material.PolishedDeepslate;
            for (int i = 25868; i <= 25873; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 25788; i <= 25867; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 25874; i <= 26197; i++)
                materials[i] = Material.PolishedDeepslateWall;
            materials[5] = Material.PolishedDiorite;
            for (int i = 14569; i <= 14574; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 13671; i <= 13750; i++)
                materials[i] = Material.PolishedDioriteStairs;
            materials[3] = Material.PolishedGranite;
            for (int i = 14551; i <= 14556; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 13431; i <= 13510; i++)
                materials[i] = Material.PolishedGraniteStairs;
            materials[21961] = Material.PolishedTuff;
            for (int i = 21962; i <= 21967; i++)
                materials[i] = Material.PolishedTuffSlab;
            for (int i = 21968; i <= 22047; i++)
                materials[i] = Material.PolishedTuffStairs;
            for (int i = 22048; i <= 22371; i++)
                materials[i] = Material.PolishedTuffWall;
            materials[2120] = Material.Poppy;
            for (int i = 8834; i <= 8841; i++)
                materials[i] = Material.Potatoes;
            materials[8803] = Material.PottedAcaciaSapling;
            materials[8812] = Material.PottedAllium;
            materials[27030] = Material.PottedAzaleaBush;
            materials[8813] = Material.PottedAzureBluet;
            materials[13426] = Material.PottedBamboo;
            materials[8801] = Material.PottedBirchSapling;
            materials[8811] = Material.PottedBlueOrchid;
            materials[8823] = Material.PottedBrownMushroom;
            materials[8825] = Material.PottedCactus;
            materials[8804] = Material.PottedCherrySapling;
            materials[8819] = Material.PottedCornflower;
            materials[19924] = Material.PottedCrimsonFungus;
            materials[19926] = Material.PottedCrimsonRoots;
            materials[8809] = Material.PottedDandelion;
            materials[8805] = Material.PottedDarkOakSapling;
            materials[8824] = Material.PottedDeadBush;
            materials[8808] = Material.PottedFern;
            materials[27031] = Material.PottedFloweringAzaleaBush;
            materials[8802] = Material.PottedJungleSapling;
            materials[8820] = Material.PottedLilyOfTheValley;
            materials[8807] = Material.PottedMangrovePropagule;
            materials[8799] = Material.PottedOakSapling;
            materials[8815] = Material.PottedOrangeTulip;
            materials[8818] = Material.PottedOxeyeDaisy;
            materials[8806] = Material.PottedPaleOakSapling;
            materials[8817] = Material.PottedPinkTulip;
            materials[8810] = Material.PottedPoppy;
            materials[8822] = Material.PottedRedMushroom;
            materials[8814] = Material.PottedRedTulip;
            materials[8800] = Material.PottedSpruceSapling;
            materials[8798] = Material.PottedTorchflower;
            materials[19925] = Material.PottedWarpedFungus;
            materials[19927] = Material.PottedWarpedRoots;
            materials[8816] = Material.PottedWhiteTulip;
            materials[8821] = Material.PottedWitherRose;
            materials[22787] = Material.PowderSnow;
            for (int i = 7633; i <= 7635; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 1987; i <= 2010; i++)
                materials[i] = Material.PoweredRail;
            materials[10798] = Material.Prismarine;
            for (int i = 11047; i <= 11052; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 10881; i <= 10960; i++)
                materials[i] = Material.PrismarineBrickStairs;
            materials[10799] = Material.PrismarineBricks;
            for (int i = 11041; i <= 11046; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 10801; i <= 10880; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 14953; i <= 15276; i++)
                materials[i] = Material.PrismarineWall;
            materials[7041] = Material.Pumpkin;
            for (int i = 7051; i <= 7058; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 11254; i <= 11269; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 1891; i <= 1906; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 21370; i <= 21385; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 21488; i <= 21489; i++)
                materials[i] = Material.PurpleCandleCake;
            materials[11073] = Material.PurpleCarpet;
            materials[13207] = Material.PurpleConcrete;
            materials[13223] = Material.PurpleConcretePowder;
            for (int i = 13173; i <= 13176; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 13097; i <= 13102; i++)
                materials[i] = Material.PurpleShulkerBox;
            materials[6121] = Material.PurpleStainedGlass;
            for (int i = 9947; i <= 9978; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            materials[9621] = Material.PurpleTerracotta;
            for (int i = 11390; i <= 11393; i++)
                materials[i] = Material.PurpleWallBanner;
            materials[2100] = Material.PurpleWool;
            materials[12879] = Material.PurpurBlock;
            for (int i = 12880; i <= 12882; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 11641; i <= 11646; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 12883; i <= 12962; i++)
                materials[i] = Material.PurpurStairs;
            materials[9490] = Material.QuartzBlock;
            materials[21193] = Material.QuartzBricks;
            for (int i = 9492; i <= 9494; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 11623; i <= 11628; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 9495; i <= 9574; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 4746; i <= 4765; i++)
                materials[i] = Material.Rail;
            materials[27028] = Material.RawCopperBlock;
            materials[27029] = Material.RawGoldBlock;
            materials[27027] = Material.RawIronBlock;
            for (int i = 11318; i <= 11333; i++)
                materials[i] = Material.RedBanner;
            for (int i = 1955; i <= 1970; i++)
                materials[i] = Material.RedBed;
            for (int i = 21434; i <= 21449; i++)
                materials[i] = Material.RedCandle;
            for (int i = 21496; i <= 21497; i++)
                materials[i] = Material.RedCandleCake;
            materials[11077] = Material.RedCarpet;
            materials[13211] = Material.RedConcrete;
            materials[13227] = Material.RedConcretePowder;
            for (int i = 13189; i <= 13192; i++)
                materials[i] = Material.RedGlazedTerracotta;
            materials[2133] = Material.RedMushroom;
            for (int i = 6843; i <= 6906; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 14611; i <= 14616; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 14311; i <= 14390; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 17545; i <= 17868; i++)
                materials[i] = Material.RedNetherBrickWall;
            materials[13014] = Material.RedNetherBricks;
            materials[123] = Material.RedSand;
            materials[11414] = Material.RedSandstone;
            for (int i = 11629; i <= 11634; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 11417; i <= 11496; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 15277; i <= 15600; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 13121; i <= 13126; i++)
                materials[i] = Material.RedShulkerBox;
            materials[6125] = Material.RedStainedGlass;
            for (int i = 10075; i <= 10106; i++)
                materials[i] = Material.RedStainedGlassPane;
            materials[9625] = Material.RedTerracotta;
            materials[2124] = Material.RedTulip;
            for (int i = 11406; i <= 11409; i++)
                materials[i] = Material.RedWallBanner;
            materials[2104] = Material.RedWool;
            materials[9478] = Material.RedstoneBlock;
            for (int i = 7647; i <= 7648; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 5900; i <= 5901; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 5904; i <= 5905; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 5906; i <= 5913; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 3030; i <= 4325; i++)
                materials[i] = Material.RedstoneWire;
            materials[27042] = Material.ReinforcedDeepslate;
            for (int i = 6047; i <= 6110; i++)
                materials[i] = Material.Repeater;
            for (int i = 12984; i <= 12995; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 19919; i <= 19923; i++)
                materials[i] = Material.RespawnAnchor;
            materials[25371] = Material.RootedDirt;
            for (int i = 11086; i <= 11087; i++)
                materials[i] = Material.RoseBush;
            materials[118] = Material.Sand;
            materials[578] = Material.Sandstone;
            for (int i = 11575; i <= 11580; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 7661; i <= 7740; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 17869; i <= 18192; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 18841; i <= 18872; i++)
                materials[i] = Material.Scaffolding;
            materials[23268] = Material.Sculk;
            for (int i = 23397; i <= 23398; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 22788; i <= 22883; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 23399; i <= 23406; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 23269; i <= 23396; i++)
                materials[i] = Material.SculkVein;
            materials[11059] = Material.SeaLantern;
            for (int i = 13402; i <= 13409; i++)
                materials[i] = Material.SeaPickle;
            materials[2051] = Material.Seagrass;
            materials[2048] = Material.ShortGrass;
            materials[19079] = Material.Shroomlight;
            for (int i = 13031; i <= 13036; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 9082; i <= 9113; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 9114; i <= 9121; i++)
                materials[i] = Material.SkeletonWallSkull;
            materials[10699] = Material.SlimeBlock;
            for (int i = 21538; i <= 21549; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 25353; i <= 25368; i++)
                materials[i] = Material.SmallDripleaf;
            materials[18935] = Material.SmithingTable;
            for (int i = 18889; i <= 18896; i++)
                materials[i] = Material.Smoker;
            materials[27026] = Material.SmoothBasalt;
            materials[11649] = Material.SmoothQuartz;
            for (int i = 14593; i <= 14598; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 14071; i <= 14150; i++)
                materials[i] = Material.SmoothQuartzStairs;
            materials[11650] = Material.SmoothRedSandstone;
            for (int i = 14557; i <= 14562; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 13511; i <= 13590; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            materials[11648] = Material.SmoothSandstone;
            for (int i = 14587; i <= 14592; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 13991; i <= 14070; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            materials[11647] = Material.SmoothStone;
            for (int i = 11569; i <= 11574; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 13269; i <= 13271; i++)
                materials[i] = Material.SnifferEgg;
            for (int i = 5938; i <= 5945; i++)
                materials[i] = Material.Snow;
            materials[5947] = Material.SnowBlock;
            for (int i = 19012; i <= 19043; i++)
                materials[i] = Material.SoulCampfire;
            materials[2915] = Material.SoulFire;
            for (int i = 18976; i <= 18979; i++)
                materials[i] = Material.SoulLantern;
            materials[6016] = Material.SoulSand;
            materials[6017] = Material.SoulSoil;
            materials[6024] = Material.SoulTorch;
            for (int i = 6025; i <= 6028; i++)
                materials[i] = Material.SoulWallTorch;
            materials[2916] = Material.Spawner;
            materials[560] = Material.Sponge;
            materials[25292] = Material.SporeBlossom;
            for (int i = 8866; i <= 8889; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 12227; i <= 12290; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 11939; i <= 11970; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 11651; i <= 11682; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 4990; i <= 5053; i++)
                materials[i] = Material.SpruceHangingSign;
            for (int i = 280; i <= 307; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 139; i <= 141; i++)
                materials[i] = Material.SpruceLog;
            materials[16] = Material.SprucePlanks;
            for (int i = 5882; i <= 5883; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 31; i <= 32; i++)
                materials[i] = Material.SpruceSapling;
            for (int i = 4386; i <= 4417; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 11503; i <= 11508; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 7896; i <= 7975; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 6191; i <= 6254; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 5702; i <= 5709; i++)
                materials[i] = Material.SpruceWallHangingSign;
            for (int i = 4854; i <= 4861; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 204; i <= 206; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 2035; i <= 2046; i++)
                materials[i] = Material.StickyPiston;
            materials[1] = Material.Stone;
            for (int i = 11605; i <= 11610; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 7339; i <= 7418; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 16249; i <= 16572; i++)
                materials[i] = Material.StoneBrickWall;
            materials[6767] = Material.StoneBricks;
            for (int i = 5914; i <= 5937; i++)
                materials[i] = Material.StoneButton;
            for (int i = 5814; i <= 5815; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 11563; i <= 11568; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 13911; i <= 13990; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 18936; i <= 18939; i++)
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
            for (int i = 19074; i <= 19076; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 19068; i <= 19070; i++)
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
            for (int i = 19057; i <= 19059; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 19051; i <= 19053; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 19825; i <= 19828; i++)
                materials[i] = Material.StructureBlock;
            materials[13018] = Material.StructureVoid;
            for (int i = 5965; i <= 5980; i++)
                materials[i] = Material.SugarCane;
            for (int i = 11082; i <= 11083; i++)
                materials[i] = Material.Sunflower;
            for (int i = 125; i <= 128; i++)
                materials[i] = Material.SuspiciousGravel;
            for (int i = 119; i <= 122; i++)
                materials[i] = Material.SuspiciousSand;
            for (int i = 19044; i <= 19047; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 11090; i <= 11091; i++)
                materials[i] = Material.TallGrass;
            for (int i = 2052; i <= 2053; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 19850; i <= 19865; i++)
                materials[i] = Material.Target;
            materials[11079] = Material.Terracotta;
            materials[22786] = Material.TintedGlass;
            for (int i = 2137; i <= 2138; i++)
                materials[i] = Material.Tnt;
            materials[2398] = Material.Torch;
            materials[2119] = Material.Torchflower;
            for (int i = 12964; i <= 12965; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 9374; i <= 9397; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 27107; i <= 27118; i++)
                materials[i] = Material.TrialSpawner;
            for (int i = 7767; i <= 7894; i++)
                materials[i] = Material.Tripwire;
            for (int i = 7751; i <= 7766; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 13292; i <= 13293; i++)
                materials[i] = Material.TubeCoral;
            materials[13277] = Material.TubeCoralBlock;
            for (int i = 13312; i <= 13313; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 13362; i <= 13369; i++)
                materials[i] = Material.TubeCoralWallFan;
            materials[21550] = Material.Tuff;
            for (int i = 22374; i <= 22379; i++)
                materials[i] = Material.TuffBrickSlab;
            for (int i = 22380; i <= 22459; i++)
                materials[i] = Material.TuffBrickStairs;
            for (int i = 22460; i <= 22783; i++)
                materials[i] = Material.TuffBrickWall;
            materials[22373] = Material.TuffBricks;
            for (int i = 21551; i <= 21556; i++)
                materials[i] = Material.TuffSlab;
            for (int i = 21557; i <= 21636; i++)
                materials[i] = Material.TuffStairs;
            for (int i = 21637; i <= 21960; i++)
                materials[i] = Material.TuffWall;
            for (int i = 13257; i <= 13268; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 19107; i <= 19132; i++)
                materials[i] = Material.TwistingVines;
            materials[19133] = Material.TwistingVinesPlant;
            for (int i = 27119; i <= 27150; i++)
                materials[i] = Material.Vault;
            for (int i = 27035; i <= 27037; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 7067; i <= 7098; i++)
                materials[i] = Material.Vine;
            materials[13427] = Material.VoidAir;
            for (int i = 2399; i <= 2402; i++)
                materials[i] = Material.WallTorch;
            for (int i = 19593; i <= 19616; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 19681; i <= 19744; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 19185; i <= 19216; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 19377; i <= 19408; i++)
                materials[i] = Material.WarpedFenceGate;
            materials[19061] = Material.WarpedFungus;
            for (int i = 5502; i <= 5565; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 19054; i <= 19056; i++)
                materials[i] = Material.WarpedHyphae;
            materials[19060] = Material.WarpedNylium;
            materials[19136] = Material.WarpedPlanks;
            for (int i = 19151; i <= 19152; i++)
                materials[i] = Material.WarpedPressurePlate;
            materials[19063] = Material.WarpedRoots;
            for (int i = 19777; i <= 19808; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 19143; i <= 19148; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 19489; i <= 19568; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 19048; i <= 19050; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 19281; i <= 19344; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 5774; i <= 5781; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 19817; i <= 19824; i++)
                materials[i] = Material.WarpedWallSign;
            materials[19062] = Material.WarpedWartBlock;
            for (int i = 86; i <= 101; i++)
                materials[i] = Material.Water;
            for (int i = 7629; i <= 7631; i++)
                materials[i] = Material.WaterCauldron;
            materials[23424] = Material.WaxedChiseledCopper;
            materials[23769] = Material.WaxedCopperBlock;
            for (int i = 25177; i <= 25180; i++)
                materials[i] = Material.WaxedCopperBulb;
            for (int i = 24377; i <= 24440; i++)
                materials[i] = Material.WaxedCopperDoor;
            for (int i = 25153; i <= 25154; i++)
                materials[i] = Material.WaxedCopperGrate;
            for (int i = 24889; i <= 24952; i++)
                materials[i] = Material.WaxedCopperTrapdoor;
            materials[23776] = Material.WaxedCutCopper;
            for (int i = 24115; i <= 24120; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 24017; i <= 24096; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            materials[23423] = Material.WaxedExposedChiseledCopper;
            materials[23771] = Material.WaxedExposedCopper;
            for (int i = 25181; i <= 25184; i++)
                materials[i] = Material.WaxedExposedCopperBulb;
            for (int i = 24441; i <= 24504; i++)
                materials[i] = Material.WaxedExposedCopperDoor;
            for (int i = 25155; i <= 25156; i++)
                materials[i] = Material.WaxedExposedCopperGrate;
            for (int i = 24953; i <= 25016; i++)
                materials[i] = Material.WaxedExposedCopperTrapdoor;
            materials[23775] = Material.WaxedExposedCutCopper;
            for (int i = 24109; i <= 24114; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 23937; i <= 24016; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            materials[23421] = Material.WaxedOxidizedChiseledCopper;
            materials[23772] = Material.WaxedOxidizedCopper;
            for (int i = 25189; i <= 25192; i++)
                materials[i] = Material.WaxedOxidizedCopperBulb;
            for (int i = 24505; i <= 24568; i++)
                materials[i] = Material.WaxedOxidizedCopperDoor;
            for (int i = 25159; i <= 25160; i++)
                materials[i] = Material.WaxedOxidizedCopperGrate;
            for (int i = 25017; i <= 25080; i++)
                materials[i] = Material.WaxedOxidizedCopperTrapdoor;
            materials[23773] = Material.WaxedOxidizedCutCopper;
            for (int i = 24097; i <= 24102; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 23777; i <= 23856; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            materials[23422] = Material.WaxedWeatheredChiseledCopper;
            materials[23770] = Material.WaxedWeatheredCopper;
            for (int i = 25185; i <= 25188; i++)
                materials[i] = Material.WaxedWeatheredCopperBulb;
            for (int i = 24569; i <= 24632; i++)
                materials[i] = Material.WaxedWeatheredCopperDoor;
            for (int i = 25157; i <= 25158; i++)
                materials[i] = Material.WaxedWeatheredCopperGrate;
            for (int i = 25081; i <= 25144; i++)
                materials[i] = Material.WaxedWeatheredCopperTrapdoor;
            materials[23774] = Material.WaxedWeatheredCutCopper;
            for (int i = 24103; i <= 24108; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 23857; i <= 23936; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            materials[23418] = Material.WeatheredChiseledCopper;
            materials[23409] = Material.WeatheredCopper;
            for (int i = 25169; i <= 25172; i++)
                materials[i] = Material.WeatheredCopperBulb;
            for (int i = 24313; i <= 24376; i++)
                materials[i] = Material.WeatheredCopperDoor;
            for (int i = 25149; i <= 25150; i++)
                materials[i] = Material.WeatheredCopperGrate;
            for (int i = 24825; i <= 24888; i++)
                materials[i] = Material.WeatheredCopperTrapdoor;
            materials[23414] = Material.WeatheredCutCopper;
            for (int i = 23751; i <= 23756; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 23505; i <= 23584; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 19080; i <= 19105; i++)
                materials[i] = Material.WeepingVines;
            materials[19106] = Material.WeepingVinesPlant;
            materials[561] = Material.WetSponge;
            for (int i = 4330; i <= 4337; i++)
                materials[i] = Material.Wheat;
            for (int i = 11094; i <= 11109; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 1731; i <= 1746; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 21210; i <= 21225; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 21468; i <= 21469; i++)
                materials[i] = Material.WhiteCandleCake;
            materials[11063] = Material.WhiteCarpet;
            materials[13197] = Material.WhiteConcrete;
            materials[13213] = Material.WhiteConcretePowder;
            for (int i = 13133; i <= 13136; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 13037; i <= 13042; i++)
                materials[i] = Material.WhiteShulkerBox;
            materials[6111] = Material.WhiteStainedGlass;
            for (int i = 9627; i <= 9658; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            materials[9611] = Material.WhiteTerracotta;
            materials[2126] = Material.WhiteTulip;
            for (int i = 11350; i <= 11353; i++)
                materials[i] = Material.WhiteWallBanner;
            materials[2090] = Material.WhiteWool;
            materials[2130] = Material.WitherRose;
            for (int i = 9122; i <= 9153; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 9154; i <= 9161; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 11158; i <= 11173; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 1795; i <= 1810; i++)
                materials[i] = Material.YellowBed;
            for (int i = 21274; i <= 21289; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 21476; i <= 21477; i++)
                materials[i] = Material.YellowCandleCake;
            materials[11067] = Material.YellowCarpet;
            materials[13201] = Material.YellowConcrete;
            materials[13217] = Material.YellowConcretePowder;
            for (int i = 13149; i <= 13152; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 13061; i <= 13066; i++)
                materials[i] = Material.YellowShulkerBox;
            materials[6115] = Material.YellowStainedGlass;
            for (int i = 9755; i <= 9786; i++)
                materials[i] = Material.YellowStainedGlassPane;
            materials[9615] = Material.YellowTerracotta;
            for (int i = 11366; i <= 11369; i++)
                materials[i] = Material.YellowWallBanner;
            materials[2094] = Material.YellowWool;
            for (int i = 9162; i <= 9193; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 9194; i <= 9201; i++)
                materials[i] = Material.ZombieWallHead;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
