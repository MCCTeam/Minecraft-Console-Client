using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette1204 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette1204()
        {
            for (int i = 8707; i <= 8730; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 12014; i <= 12077; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 11662; i <= 11693; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 11406; i <= 11437; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 5026; i <= 5089; i++)
                materials[i] = Material.AcaciaHangingSign;
            for (int i = 349; i <= 376; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 142; i <= 144; i++)
                materials[i] = Material.AcaciaLog;
            materials[19] = Material.AcaciaPlanks;
            for (int i = 5724; i <= 5725; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 33; i <= 34; i++)
                materials[i] = Material.AcaciaSapling;
            for (int i = 4398; i <= 4429; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 11186; i <= 11191; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 9884; i <= 9963; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 6217; i <= 6280; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 5562; i <= 5569; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 4786; i <= 4793; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 201; i <= 203; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 9320; i <= 9343; i++)
                materials[i] = Material.ActivatorRail;
            materials[0] = Material.Air;
            materials[2079] = Material.Allium;
            materials[21031] = Material.AmethystBlock;
            for (int i = 21033; i <= 21044; i++)
                materials[i] = Material.AmethystCluster;
            materials[19448] = Material.AncientDebris;
            materials[6] = Material.Andesite;
            for (int i = 14136; i <= 14141; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 13762; i <= 13841; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 16752; i <= 17075; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 9107; i <= 9110; i++)
                materials[i] = Material.Anvil;
            for (int i = 6817; i <= 6820; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 6813; i <= 6816; i++)
                materials[i] = Material.AttachedPumpkinStem;
            materials[24824] = Material.Azalea;
            for (int i = 461; i <= 488; i++)
                materials[i] = Material.AzaleaLeaves;
            materials[2080] = Material.AzureBluet;
            for (int i = 12945; i <= 12956; i++)
                materials[i] = Material.Bamboo;
            for (int i = 159; i <= 161; i++)
                materials[i] = Material.BambooBlock;
            for (int i = 8803; i <= 8826; i++)
                materials[i] = Material.BambooButton;
            for (int i = 12270; i <= 12333; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 11790; i <= 11821; i++)
                materials[i] = Material.BambooFence;
            for (int i = 11534; i <= 11565; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 5474; i <= 5537; i++)
                materials[i] = Material.BambooHangingSign;
            materials[24] = Material.BambooMosaic;
            for (int i = 11216; i <= 11221; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 10284; i <= 10363; i++)
                materials[i] = Material.BambooMosaicStairs;
            materials[23] = Material.BambooPlanks;
            for (int i = 5732; i <= 5733; i++)
                materials[i] = Material.BambooPressurePlate;
            materials[12944] = Material.BambooSapling;
            for (int i = 4558; i <= 4589; i++)
                materials[i] = Material.BambooSign;
            for (int i = 11210; i <= 11215; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 10204; i <= 10283; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 6473; i <= 6536; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 5618; i <= 5625; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 4826; i <= 4833; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 18408; i <= 18419; i++)
                materials[i] = Material.Barrel;
            for (int i = 10365; i <= 10366; i++)
                materials[i] = Material.Barrier;
            for (int i = 5852; i <= 5854; i++)
                materials[i] = Material.Basalt;
            materials[7918] = Material.Beacon;
            materials[79] = Material.Bedrock;
            for (int i = 19397; i <= 19420; i++)
                materials[i] = Material.BeeNest;
            for (int i = 19421; i <= 19444; i++)
                materials[i] = Material.Beehive;
            for (int i = 12509; i <= 12512; i++)
                materials[i] = Material.Beetroots;
            for (int i = 18471; i <= 18502; i++)
                materials[i] = Material.Bell;
            for (int i = 24844; i <= 24875; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 24876; i <= 24883; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 8659; i <= 8682; i++)
                materials[i] = Material.BirchButton;
            for (int i = 11886; i <= 11949; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 11598; i <= 11629; i++)
                materials[i] = Material.BirchFence;
            for (int i = 11342; i <= 11373; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 4962; i <= 5025; i++)
                materials[i] = Material.BirchHangingSign;
            for (int i = 293; i <= 320; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 136; i <= 138; i++)
                materials[i] = Material.BirchLog;
            materials[17] = Material.BirchPlanks;
            for (int i = 5720; i <= 5721; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 29; i <= 30; i++)
                materials[i] = Material.BirchSapling;
            for (int i = 4366; i <= 4397; i++)
                materials[i] = Material.BirchSign;
            for (int i = 11174; i <= 11179; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 7746; i <= 7825; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 6089; i <= 6152; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 5554; i <= 5561; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 4778; i <= 4785; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 195; i <= 197; i++)
                materials[i] = Material.BirchWood;
            for (int i = 10999; i <= 11014; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 1928; i <= 1943; i++)
                materials[i] = Material.BlackBed;
            for (int i = 20981; i <= 20996; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 21029; i <= 21030; i++)
                materials[i] = Material.BlackCandleCake;
            materials[10743] = Material.BlackCarpet;
            materials[12743] = Material.BlackConcrete;
            materials[12759] = Material.BlackConcretePowder;
            for (int i = 12724; i <= 12727; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 12658; i <= 12663; i++)
                materials[i] = Material.BlackShulkerBox;
            materials[5960] = Material.BlackStainedGlass;
            for (int i = 9852; i <= 9883; i++)
                materials[i] = Material.BlackStainedGlassPane;
            materials[9371] = Material.BlackTerracotta;
            for (int i = 11075; i <= 11078; i++)
                materials[i] = Material.BlackWallBanner;
            materials[2062] = Material.BlackWool;
            materials[19460] = Material.Blackstone;
            for (int i = 19865; i <= 19870; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 19461; i <= 19540; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 19541; i <= 19864; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 18428; i <= 18435; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 10935; i <= 10950; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 1864; i <= 1879; i++)
                materials[i] = Material.BlueBed;
            for (int i = 20917; i <= 20932; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 21021; i <= 21022; i++)
                materials[i] = Material.BlueCandleCake;
            materials[10739] = Material.BlueCarpet;
            materials[12739] = Material.BlueConcrete;
            materials[12755] = Material.BlueConcretePowder;
            for (int i = 12708; i <= 12711; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            materials[12941] = Material.BlueIce;
            materials[2078] = Material.BlueOrchid;
            for (int i = 12634; i <= 12639; i++)
                materials[i] = Material.BlueShulkerBox;
            materials[5956] = Material.BlueStainedGlass;
            for (int i = 9724; i <= 9755; i++)
                materials[i] = Material.BlueStainedGlassPane;
            materials[9367] = Material.BlueTerracotta;
            for (int i = 11059; i <= 11062; i++)
                materials[i] = Material.BlueWallBanner;
            materials[2058] = Material.BlueWool;
            for (int i = 12546; i <= 12548; i++)
                materials[i] = Material.BoneBlock;
            materials[2096] = Material.Bookshelf;
            for (int i = 12825; i <= 12826; i++)
                materials[i] = Material.BrainCoral;
            materials[12809] = Material.BrainCoralBlock;
            for (int i = 12845; i <= 12846; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 12901; i <= 12908; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 7390; i <= 7397; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 11258; i <= 11263; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 7029; i <= 7108; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 14160; i <= 14483; i++)
                materials[i] = Material.BrickWall;
            materials[2093] = Material.Bricks;
            for (int i = 10951; i <= 10966; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 1880; i <= 1895; i++)
                materials[i] = Material.BrownBed;
            for (int i = 20933; i <= 20948; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 21023; i <= 21024; i++)
                materials[i] = Material.BrownCandleCake;
            materials[10740] = Material.BrownCarpet;
            materials[12740] = Material.BrownConcrete;
            materials[12756] = Material.BrownConcretePowder;
            for (int i = 12712; i <= 12715; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            materials[2089] = Material.BrownMushroom;
            for (int i = 6549; i <= 6612; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 12640; i <= 12645; i++)
                materials[i] = Material.BrownShulkerBox;
            materials[5957] = Material.BrownStainedGlass;
            for (int i = 9756; i <= 9787; i++)
                materials[i] = Material.BrownStainedGlassPane;
            materials[9368] = Material.BrownTerracotta;
            for (int i = 11063; i <= 11066; i++)
                materials[i] = Material.BrownWallBanner;
            materials[2059] = Material.BrownWool;
            for (int i = 12960; i <= 12961; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 12827; i <= 12828; i++)
                materials[i] = Material.BubbleCoral;
            materials[12810] = Material.BubbleCoralBlock;
            for (int i = 12847; i <= 12848; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 12909; i <= 12916; i++)
                materials[i] = Material.BubbleCoralWallFan;
            materials[21032] = Material.BuddingAmethyst;
            for (int i = 5782; i <= 5797; i++)
                materials[i] = Material.Cactus;
            for (int i = 5874; i <= 5880; i++)
                materials[i] = Material.Cake;
            materials[22316] = Material.Calcite;
            for (int i = 22415; i <= 22798; i++)
                materials[i] = Material.CalibratedSculkSensor;
            for (int i = 18511; i <= 18542; i++)
                materials[i] = Material.Campfire;
            for (int i = 20725; i <= 20740; i++)
                materials[i] = Material.Candle;
            for (int i = 20997; i <= 20998; i++)
                materials[i] = Material.CandleCake;
            for (int i = 8595; i <= 8602; i++)
                materials[i] = Material.Carrots;
            materials[18436] = Material.CartographyTable;
            for (int i = 5866; i <= 5869; i++)
                materials[i] = Material.CarvedPumpkin;
            materials[7398] = Material.Cauldron;
            materials[12959] = Material.CaveAir;
            for (int i = 24769; i <= 24820; i++)
                materials[i] = Material.CaveVines;
            for (int i = 24821; i <= 24822; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 6773; i <= 6778; i++)
                materials[i] = Material.Chain;
            for (int i = 12527; i <= 12538; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 8731; i <= 8754; i++)
                materials[i] = Material.CherryButton;
            for (int i = 12078; i <= 12141; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 11694; i <= 11725; i++)
                materials[i] = Material.CherryFence;
            for (int i = 11438; i <= 11469; i++)
                materials[i] = Material.CherryFenceGate;
            for (int i = 5090; i <= 5153; i++)
                materials[i] = Material.CherryHangingSign;
            for (int i = 377; i <= 404; i++)
                materials[i] = Material.CherryLeaves;
            for (int i = 145; i <= 147; i++)
                materials[i] = Material.CherryLog;
            materials[20] = Material.CherryPlanks;
            for (int i = 5726; i <= 5727; i++)
                materials[i] = Material.CherryPressurePlate;
            for (int i = 35; i <= 36; i++)
                materials[i] = Material.CherrySapling;
            for (int i = 4430; i <= 4461; i++)
                materials[i] = Material.CherrySign;
            for (int i = 11192; i <= 11197; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 9964; i <= 10043; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 6281; i <= 6344; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 5570; i <= 5577; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 4794; i <= 4801; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 204; i <= 206; i++)
                materials[i] = Material.CherryWood;
            for (int i = 2954; i <= 2977; i++)
                materials[i] = Material.Chest;
            for (int i = 9111; i <= 9114; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 2097; i <= 2352; i++)
                materials[i] = Material.ChiseledBookshelf;
            materials[22951] = Material.ChiseledCopper;
            materials[26551] = Material.ChiseledDeepslate;
            materials[20722] = Material.ChiseledNetherBricks;
            materials[19874] = Material.ChiseledPolishedBlackstone;
            materials[9236] = Material.ChiseledQuartzBlock;
            materials[11080] = Material.ChiseledRedSandstone;
            materials[536] = Material.ChiseledSandstone;
            materials[6540] = Material.ChiseledStoneBricks;
            materials[21903] = Material.ChiseledTuff;
            materials[22315] = Material.ChiseledTuffBricks;
            for (int i = 12404; i <= 12409; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 12340; i <= 12403; i++)
                materials[i] = Material.ChorusPlant;
            materials[5798] = Material.Clay;
            materials[10745] = Material.CoalBlock;
            materials[127] = Material.CoalOre;
            materials[11] = Material.CoarseDirt;
            materials[24907] = Material.CobbledDeepslate;
            for (int i = 24988; i <= 24993; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 24908; i <= 24987; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 24994; i <= 25317; i++)
                materials[i] = Material.CobbledDeepslateWall;
            materials[14] = Material.Cobblestone;
            for (int i = 11252; i <= 11257; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 4682; i <= 4761; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 7919; i <= 8242; i++)
                materials[i] = Material.CobblestoneWall;
            materials[2004] = Material.Cobweb;
            for (int i = 7419; i <= 7430; i++)
                materials[i] = Material.Cocoa;
            for (int i = 7906; i <= 7917; i++)
                materials[i] = Material.CommandBlock;
            for (int i = 9175; i <= 9190; i++)
                materials[i] = Material.Comparator;
            for (int i = 19372; i <= 19380; i++)
                materials[i] = Material.Composter;
            for (int i = 12942; i <= 12943; i++)
                materials[i] = Material.Conduit;
            materials[22938] = Material.CopperBlock;
            for (int i = 24692; i <= 24695; i++)
                materials[i] = Material.CopperBulb;
            for (int i = 23652; i <= 23715; i++)
                materials[i] = Material.CopperDoor;
            for (int i = 24676; i <= 24677; i++)
                materials[i] = Material.CopperGrate;
            materials[22942] = Material.CopperOre;
            for (int i = 24164; i <= 24227; i++)
                materials[i] = Material.CopperTrapdoor;
            materials[2086] = Material.Cornflower;
            materials[26552] = Material.CrackedDeepslateBricks;
            materials[26553] = Material.CrackedDeepslateTiles;
            materials[20723] = Material.CrackedNetherBricks;
            materials[19873] = Material.CrackedPolishedBlackstoneBricks;
            materials[6539] = Material.CrackedStoneBricks;
            for (int i = 26590; i <= 26637; i++)
                materials[i] = Material.Crafter;
            materials[4277] = Material.CraftingTable;
            for (int i = 8987; i <= 9018; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 9019; i <= 9026; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 19100; i <= 19123; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 19148; i <= 19211; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 18684; i <= 18715; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 18876; i <= 18907; i++)
                materials[i] = Material.CrimsonFenceGate;
            materials[18609] = Material.CrimsonFungus;
            for (int i = 5282; i <= 5345; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 18602; i <= 18604; i++)
                materials[i] = Material.CrimsonHyphae;
            materials[18608] = Material.CrimsonNylium;
            materials[18666] = Material.CrimsonPlanks;
            for (int i = 18680; i <= 18681; i++)
                materials[i] = Material.CrimsonPressurePlate;
            materials[18665] = Material.CrimsonRoots;
            for (int i = 19276; i <= 19307; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 18668; i <= 18673; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 18940; i <= 19019; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 18596; i <= 18598; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 18748; i <= 18811; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 5602; i <= 5609; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 19340; i <= 19347; i++)
                materials[i] = Material.CrimsonWallSign;
            materials[19449] = Material.CryingObsidian;
            materials[22947] = Material.CutCopper;
            for (int i = 23294; i <= 23299; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 23196; i <= 23275; i++)
                materials[i] = Material.CutCopperStairs;
            materials[11081] = Material.CutRedSandstone;
            for (int i = 11294; i <= 11299; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            materials[537] = Material.CutSandstone;
            for (int i = 11240; i <= 11245; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 10903; i <= 10918; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 1832; i <= 1847; i++)
                materials[i] = Material.CyanBed;
            for (int i = 20885; i <= 20900; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 21017; i <= 21018; i++)
                materials[i] = Material.CyanCandleCake;
            materials[10737] = Material.CyanCarpet;
            materials[12737] = Material.CyanConcrete;
            materials[12753] = Material.CyanConcretePowder;
            for (int i = 12700; i <= 12703; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 12622; i <= 12627; i++)
                materials[i] = Material.CyanShulkerBox;
            materials[5954] = Material.CyanStainedGlass;
            for (int i = 9660; i <= 9691; i++)
                materials[i] = Material.CyanStainedGlassPane;
            materials[9365] = Material.CyanTerracotta;
            for (int i = 11051; i <= 11054; i++)
                materials[i] = Material.CyanWallBanner;
            materials[2056] = Material.CyanWool;
            for (int i = 9115; i <= 9118; i++)
                materials[i] = Material.DamagedAnvil;
            materials[2075] = Material.Dandelion;
            for (int i = 8755; i <= 8778; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 12142; i <= 12205; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 11726; i <= 11757; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 11470; i <= 11501; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 5218; i <= 5281; i++)
                materials[i] = Material.DarkOakHangingSign;
            for (int i = 405; i <= 432; i++)
                materials[i] = Material.DarkOakLeaves;
            for (int i = 148; i <= 150; i++)
                materials[i] = Material.DarkOakLog;
            materials[21] = Material.DarkOakPlanks;
            for (int i = 5728; i <= 5729; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 37; i <= 38; i++)
                materials[i] = Material.DarkOakSapling;
            for (int i = 4494; i <= 4525; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 11198; i <= 11203; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 10044; i <= 10123; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 6345; i <= 6408; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 5586; i <= 5593; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 4810; i <= 4817; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 207; i <= 209; i++)
                materials[i] = Material.DarkOakWood;
            materials[10465] = Material.DarkPrismarine;
            for (int i = 10718; i <= 10723; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 10626; i <= 10705; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 9191; i <= 9222; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 12815; i <= 12816; i++)
                materials[i] = Material.DeadBrainCoral;
            materials[12804] = Material.DeadBrainCoralBlock;
            for (int i = 12835; i <= 12836; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 12861; i <= 12868; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 12817; i <= 12818; i++)
                materials[i] = Material.DeadBubbleCoral;
            materials[12805] = Material.DeadBubbleCoralBlock;
            for (int i = 12837; i <= 12838; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 12869; i <= 12876; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            materials[2007] = Material.DeadBush;
            for (int i = 12819; i <= 12820; i++)
                materials[i] = Material.DeadFireCoral;
            materials[12806] = Material.DeadFireCoralBlock;
            for (int i = 12839; i <= 12840; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 12877; i <= 12884; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 12821; i <= 12822; i++)
                materials[i] = Material.DeadHornCoral;
            materials[12807] = Material.DeadHornCoralBlock;
            for (int i = 12841; i <= 12842; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 12885; i <= 12892; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 12813; i <= 12814; i++)
                materials[i] = Material.DeadTubeCoral;
            materials[12803] = Material.DeadTubeCoralBlock;
            for (int i = 12833; i <= 12834; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 12853; i <= 12860; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 26574; i <= 26589; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 24904; i <= 24906; i++)
                materials[i] = Material.Deepslate;
            for (int i = 26221; i <= 26226; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 26141; i <= 26220; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 26227; i <= 26550; i++)
                materials[i] = Material.DeepslateBrickWall;
            materials[26140] = Material.DeepslateBricks;
            materials[128] = Material.DeepslateCoalOre;
            materials[22943] = Material.DeepslateCopperOre;
            materials[4275] = Material.DeepslateDiamondOre;
            materials[7512] = Material.DeepslateEmeraldOre;
            materials[124] = Material.DeepslateGoldOre;
            materials[126] = Material.DeepslateIronOre;
            materials[521] = Material.DeepslateLapisOre;
            for (int i = 5736; i <= 5737; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 25810; i <= 25815; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 25730; i <= 25809; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 25816; i <= 26139; i++)
                materials[i] = Material.DeepslateTileWall;
            materials[25729] = Material.DeepslateTiles;
            for (int i = 1968; i <= 1991; i++)
                materials[i] = Material.DetectorRail;
            materials[4276] = Material.DiamondBlock;
            materials[4274] = Material.DiamondOre;
            materials[4] = Material.Diorite;
            for (int i = 14154; i <= 14159; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 14002; i <= 14081; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 18048; i <= 18371; i++)
                materials[i] = Material.DioriteWall;
            materials[10] = Material.Dirt;
            materials[12513] = Material.DirtPath;
            for (int i = 523; i <= 534; i++)
                materials[i] = Material.Dispenser;
            materials[7416] = Material.DragonEgg;
            for (int i = 9027; i <= 9058; i++)
                materials[i] = Material.DragonHead;
            for (int i = 9059; i <= 9066; i++)
                materials[i] = Material.DragonWallHead;
            materials[12787] = Material.DriedKelpBlock;
            materials[24768] = Material.DripstoneBlock;
            for (int i = 9344; i <= 9355; i++)
                materials[i] = Material.Dropper;
            materials[7665] = Material.EmeraldBlock;
            materials[7511] = Material.EmeraldOre;
            materials[7389] = Material.EnchantingTable;
            materials[12514] = Material.EndGateway;
            materials[7406] = Material.EndPortal;
            for (int i = 7407; i <= 7414; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 12334; i <= 12339; i++)
                materials[i] = Material.EndRod;
            materials[7415] = Material.EndStone;
            for (int i = 14112; i <= 14117; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 13362; i <= 13441; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 17724; i <= 18047; i++)
                materials[i] = Material.EndStoneBrickWall;
            materials[12494] = Material.EndStoneBricks;
            for (int i = 7513; i <= 7520; i++)
                materials[i] = Material.EnderChest;
            materials[22950] = Material.ExposedChiseledCopper;
            materials[22939] = Material.ExposedCopper;
            for (int i = 24696; i <= 24699; i++)
                materials[i] = Material.ExposedCopperBulb;
            for (int i = 23716; i <= 23779; i++)
                materials[i] = Material.ExposedCopperDoor;
            for (int i = 24678; i <= 24679; i++)
                materials[i] = Material.ExposedCopperGrate;
            for (int i = 24228; i <= 24291; i++)
                materials[i] = Material.ExposedCopperTrapdoor;
            materials[22946] = Material.ExposedCutCopper;
            for (int i = 23288; i <= 23293; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 23116; i <= 23195; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 4286; i <= 4293; i++)
                materials[i] = Material.Farmland;
            materials[2006] = Material.Fern;
            for (int i = 2360; i <= 2871; i++)
                materials[i] = Material.Fire;
            for (int i = 12829; i <= 12830; i++)
                materials[i] = Material.FireCoral;
            materials[12811] = Material.FireCoralBlock;
            for (int i = 12849; i <= 12850; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 12917; i <= 12924; i++)
                materials[i] = Material.FireCoralWallFan;
            materials[18437] = Material.FletchingTable;
            materials[8567] = Material.FlowerPot;
            materials[24825] = Material.FloweringAzalea;
            for (int i = 489; i <= 516; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            materials[26572] = Material.Frogspawn;
            for (int i = 12539; i <= 12542; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 4294; i <= 4301; i++)
                materials[i] = Material.Furnace;
            materials[20285] = Material.GildedBlackstone;
            materials[519] = Material.Glass;
            for (int i = 6779; i <= 6810; i++)
                materials[i] = Material.GlassPane;
            for (int i = 6869; i <= 6996; i++)
                materials[i] = Material.GlowLichen;
            materials[5863] = Material.Glowstone;
            materials[2091] = Material.GoldBlock;
            materials[123] = Material.GoldOre;
            materials[2] = Material.Granite;
            for (int i = 14130; i <= 14135; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 13682; i <= 13761; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 15456; i <= 15779; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            materials[118] = Material.Gravel;
            for (int i = 10871; i <= 10886; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 1800; i <= 1815; i++)
                materials[i] = Material.GrayBed;
            for (int i = 20853; i <= 20868; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 21013; i <= 21014; i++)
                materials[i] = Material.GrayCandleCake;
            materials[10735] = Material.GrayCarpet;
            materials[12735] = Material.GrayConcrete;
            materials[12751] = Material.GrayConcretePowder;
            for (int i = 12692; i <= 12695; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 12610; i <= 12615; i++)
                materials[i] = Material.GrayShulkerBox;
            materials[5952] = Material.GrayStainedGlass;
            for (int i = 9596; i <= 9627; i++)
                materials[i] = Material.GrayStainedGlassPane;
            materials[9363] = Material.GrayTerracotta;
            for (int i = 11043; i <= 11046; i++)
                materials[i] = Material.GrayWallBanner;
            materials[2054] = Material.GrayWool;
            for (int i = 10967; i <= 10982; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 1896; i <= 1911; i++)
                materials[i] = Material.GreenBed;
            for (int i = 20949; i <= 20964; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 21025; i <= 21026; i++)
                materials[i] = Material.GreenCandleCake;
            materials[10741] = Material.GreenCarpet;
            materials[12741] = Material.GreenConcrete;
            materials[12757] = Material.GreenConcretePowder;
            for (int i = 12716; i <= 12719; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 12646; i <= 12651; i++)
                materials[i] = Material.GreenShulkerBox;
            materials[5958] = Material.GreenStainedGlass;
            for (int i = 9788; i <= 9819; i++)
                materials[i] = Material.GreenStainedGlassPane;
            materials[9369] = Material.GreenTerracotta;
            for (int i = 11067; i <= 11070; i++)
                materials[i] = Material.GreenWallBanner;
            materials[2060] = Material.GreenWool;
            for (int i = 18438; i <= 18449; i++)
                materials[i] = Material.Grindstone;
            for (int i = 24900; i <= 24901; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 10725; i <= 10727; i++)
                materials[i] = Material.HayBlock;
            for (int i = 9159; i <= 9174; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            materials[19445] = Material.HoneyBlock;
            materials[19446] = Material.HoneycombBlock;
            for (int i = 9225; i <= 9234; i++)
                materials[i] = Material.Hopper;
            for (int i = 12831; i <= 12832; i++)
                materials[i] = Material.HornCoral;
            materials[12812] = Material.HornCoralBlock;
            for (int i = 12851; i <= 12852; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 12925; i <= 12932; i++)
                materials[i] = Material.HornCoralWallFan;
            materials[5780] = Material.Ice;
            materials[6548] = Material.InfestedChiseledStoneBricks;
            materials[6544] = Material.InfestedCobblestone;
            materials[6547] = Material.InfestedCrackedStoneBricks;
            for (int i = 26554; i <= 26556; i++)
                materials[i] = Material.InfestedDeepslate;
            materials[6546] = Material.InfestedMossyStoneBricks;
            materials[6543] = Material.InfestedStone;
            materials[6545] = Material.InfestedStoneBricks;
            for (int i = 6741; i <= 6772; i++)
                materials[i] = Material.IronBars;
            materials[2092] = Material.IronBlock;
            for (int i = 5652; i <= 5715; i++)
                materials[i] = Material.IronDoor;
            materials[125] = Material.IronOre;
            for (int i = 10399; i <= 10462; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 5870; i <= 5873; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 19360; i <= 19371; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 5815; i <= 5816; i++)
                materials[i] = Material.Jukebox;
            for (int i = 8683; i <= 8706; i++)
                materials[i] = Material.JungleButton;
            for (int i = 11950; i <= 12013; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 11630; i <= 11661; i++)
                materials[i] = Material.JungleFence;
            for (int i = 11374; i <= 11405; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 5154; i <= 5217; i++)
                materials[i] = Material.JungleHangingSign;
            for (int i = 321; i <= 348; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 139; i <= 141; i++)
                materials[i] = Material.JungleLog;
            materials[18] = Material.JunglePlanks;
            for (int i = 5722; i <= 5723; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 31; i <= 32; i++)
                materials[i] = Material.JungleSapling;
            for (int i = 4462; i <= 4493; i++)
                materials[i] = Material.JungleSign;
            for (int i = 11180; i <= 11185; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 7826; i <= 7905; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 6153; i <= 6216; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 5578; i <= 5585; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 4802; i <= 4809; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 198; i <= 200; i++)
                materials[i] = Material.JungleWood;
            for (int i = 12760; i <= 12785; i++)
                materials[i] = Material.Kelp;
            materials[12786] = Material.KelpPlant;
            for (int i = 4654; i <= 4661; i++)
                materials[i] = Material.Ladder;
            for (int i = 18503; i <= 18506; i++)
                materials[i] = Material.Lantern;
            materials[522] = Material.LapisBlock;
            materials[520] = Material.LapisOre;
            for (int i = 21045; i <= 21056; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 10757; i <= 10758; i++)
                materials[i] = Material.LargeFern;
            for (int i = 96; i <= 111; i++)
                materials[i] = Material.Lava;
            materials[7402] = Material.LavaCauldron;
            for (int i = 18450; i <= 18465; i++)
                materials[i] = Material.Lectern;
            for (int i = 5626; i <= 5649; i++)
                materials[i] = Material.Lever;
            for (int i = 10367; i <= 10398; i++)
                materials[i] = Material.Light;
            for (int i = 10807; i <= 10822; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 1736; i <= 1751; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 20789; i <= 20804; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 21005; i <= 21006; i++)
                materials[i] = Material.LightBlueCandleCake;
            materials[10731] = Material.LightBlueCarpet;
            materials[12731] = Material.LightBlueConcrete;
            materials[12747] = Material.LightBlueConcretePowder;
            for (int i = 12676; i <= 12679; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 12586; i <= 12591; i++)
                materials[i] = Material.LightBlueShulkerBox;
            materials[5948] = Material.LightBlueStainedGlass;
            for (int i = 9468; i <= 9499; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            materials[9359] = Material.LightBlueTerracotta;
            for (int i = 11027; i <= 11030; i++)
                materials[i] = Material.LightBlueWallBanner;
            materials[2050] = Material.LightBlueWool;
            for (int i = 10887; i <= 10902; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 1816; i <= 1831; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 20869; i <= 20884; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 21015; i <= 21016; i++)
                materials[i] = Material.LightGrayCandleCake;
            materials[10736] = Material.LightGrayCarpet;
            materials[12736] = Material.LightGrayConcrete;
            materials[12752] = Material.LightGrayConcretePowder;
            for (int i = 12696; i <= 12699; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 12616; i <= 12621; i++)
                materials[i] = Material.LightGrayShulkerBox;
            materials[5953] = Material.LightGrayStainedGlass;
            for (int i = 9628; i <= 9659; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            materials[9364] = Material.LightGrayTerracotta;
            for (int i = 11047; i <= 11050; i++)
                materials[i] = Material.LightGrayWallBanner;
            materials[2055] = Material.LightGrayWool;
            for (int i = 9143; i <= 9158; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 24724; i <= 24747; i++)
                materials[i] = Material.LightningRod;
            for (int i = 10749; i <= 10750; i++)
                materials[i] = Material.Lilac;
            materials[2088] = Material.LilyOfTheValley;
            materials[7271] = Material.LilyPad;
            for (int i = 10839; i <= 10854; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 1768; i <= 1783; i++)
                materials[i] = Material.LimeBed;
            for (int i = 20821; i <= 20836; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 21009; i <= 21010; i++)
                materials[i] = Material.LimeCandleCake;
            materials[10733] = Material.LimeCarpet;
            materials[12733] = Material.LimeConcrete;
            materials[12749] = Material.LimeConcretePowder;
            for (int i = 12684; i <= 12687; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 12598; i <= 12603; i++)
                materials[i] = Material.LimeShulkerBox;
            materials[5950] = Material.LimeStainedGlass;
            for (int i = 9532; i <= 9563; i++)
                materials[i] = Material.LimeStainedGlassPane;
            materials[9361] = Material.LimeTerracotta;
            for (int i = 11035; i <= 11038; i++)
                materials[i] = Material.LimeWallBanner;
            materials[2052] = Material.LimeWool;
            materials[19459] = Material.Lodestone;
            for (int i = 18404; i <= 18407; i++)
                materials[i] = Material.Loom;
            for (int i = 10791; i <= 10806; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 1720; i <= 1735; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 20773; i <= 20788; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 21003; i <= 21004; i++)
                materials[i] = Material.MagentaCandleCake;
            materials[10730] = Material.MagentaCarpet;
            materials[12730] = Material.MagentaConcrete;
            materials[12746] = Material.MagentaConcretePowder;
            for (int i = 12672; i <= 12675; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 12580; i <= 12585; i++)
                materials[i] = Material.MagentaShulkerBox;
            materials[5947] = Material.MagentaStainedGlass;
            for (int i = 9436; i <= 9467; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            materials[9358] = Material.MagentaTerracotta;
            for (int i = 11023; i <= 11026; i++)
                materials[i] = Material.MagentaWallBanner;
            materials[2049] = Material.MagentaWool;
            materials[12543] = Material.MagmaBlock;
            for (int i = 8779; i <= 8802; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 12206; i <= 12269; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 11758; i <= 11789; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 11502; i <= 11533; i++)
                materials[i] = Material.MangroveFenceGate;
            for (int i = 5410; i <= 5473; i++)
                materials[i] = Material.MangroveHangingSign;
            for (int i = 433; i <= 460; i++)
                materials[i] = Material.MangroveLeaves;
            for (int i = 151; i <= 153; i++)
                materials[i] = Material.MangroveLog;
            materials[22] = Material.MangrovePlanks;
            for (int i = 5730; i <= 5731; i++)
                materials[i] = Material.MangrovePressurePlate;
            for (int i = 39; i <= 78; i++)
                materials[i] = Material.MangrovePropagule;
            for (int i = 154; i <= 155; i++)
                materials[i] = Material.MangroveRoots;
            for (int i = 4526; i <= 4557; i++)
                materials[i] = Material.MangroveSign;
            for (int i = 11204; i <= 11209; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 10124; i <= 10203; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 6409; i <= 6472; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 5594; i <= 5601; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 4818; i <= 4825; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 210; i <= 212; i++)
                materials[i] = Material.MangroveWood;
            for (int i = 21057; i <= 21068; i++)
                materials[i] = Material.MediumAmethystBud;
            materials[6812] = Material.Melon;
            for (int i = 6829; i <= 6836; i++)
                materials[i] = Material.MelonStem;
            materials[24843] = Material.MossBlock;
            materials[24826] = Material.MossCarpet;
            materials[2353] = Material.MossyCobblestone;
            for (int i = 14106; i <= 14111; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 13282; i <= 13361; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 8243; i <= 8566; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 14094; i <= 14099; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 13122; i <= 13201; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 15132; i <= 15455; i++)
                materials[i] = Material.MossyStoneBrickWall;
            materials[6538] = Material.MossyStoneBricks;
            for (int i = 2063; i <= 2074; i++)
                materials[i] = Material.MovingPiston;
            materials[24903] = Material.Mud;
            for (int i = 11270; i <= 11275; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 7189; i <= 7268; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 16104; i <= 16427; i++)
                materials[i] = Material.MudBrickWall;
            materials[6542] = Material.MudBricks;
            for (int i = 156; i <= 158; i++)
                materials[i] = Material.MuddyMangroveRoots;
            for (int i = 6677; i <= 6740; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 7269; i <= 7270; i++)
                materials[i] = Material.Mycelium;
            for (int i = 7273; i <= 7304; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 11276; i <= 11281; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 7305; i <= 7384; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 16428; i <= 16751; i++)
                materials[i] = Material.NetherBrickWall;
            materials[7272] = Material.NetherBricks;
            materials[129] = Material.NetherGoldOre;
            for (int i = 5864; i <= 5865; i++)
                materials[i] = Material.NetherPortal;
            materials[9224] = Material.NetherQuartzOre;
            materials[18595] = Material.NetherSprouts;
            for (int i = 7385; i <= 7388; i++)
                materials[i] = Material.NetherWart;
            materials[12544] = Material.NetherWartBlock;
            materials[19447] = Material.NetheriteBlock;
            materials[5849] = Material.Netherrack;
            for (int i = 538; i <= 1687; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 8611; i <= 8634; i++)
                materials[i] = Material.OakButton;
            for (int i = 4590; i <= 4653; i++)
                materials[i] = Material.OakDoor;
            for (int i = 5817; i <= 5848; i++)
                materials[i] = Material.OakFence;
            for (int i = 6997; i <= 7028; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 4834; i <= 4897; i++)
                materials[i] = Material.OakHangingSign;
            for (int i = 237; i <= 264; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 130; i <= 132; i++)
                materials[i] = Material.OakLog;
            materials[15] = Material.OakPlanks;
            for (int i = 5716; i <= 5717; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 25; i <= 26; i++)
                materials[i] = Material.OakSapling;
            for (int i = 4302; i <= 4333; i++)
                materials[i] = Material.OakSign;
            for (int i = 11162; i <= 11167; i++)
                materials[i] = Material.OakSlab;
            for (int i = 2874; i <= 2953; i++)
                materials[i] = Material.OakStairs;
            for (int i = 5961; i <= 6024; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 5538; i <= 5545; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 4762; i <= 4769; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 189; i <= 191; i++)
                materials[i] = Material.OakWood;
            for (int i = 12550; i <= 12561; i++)
                materials[i] = Material.Observer;
            materials[2354] = Material.Obsidian;
            for (int i = 26563; i <= 26565; i++)
                materials[i] = Material.OchreFroglight;
            for (int i = 10775; i <= 10790; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 1704; i <= 1719; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 20757; i <= 20772; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 21001; i <= 21002; i++)
                materials[i] = Material.OrangeCandleCake;
            materials[10729] = Material.OrangeCarpet;
            materials[12729] = Material.OrangeConcrete;
            materials[12745] = Material.OrangeConcretePowder;
            for (int i = 12668; i <= 12671; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 12574; i <= 12579; i++)
                materials[i] = Material.OrangeShulkerBox;
            materials[5946] = Material.OrangeStainedGlass;
            for (int i = 9404; i <= 9435; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            materials[9357] = Material.OrangeTerracotta;
            materials[2082] = Material.OrangeTulip;
            for (int i = 11019; i <= 11022; i++)
                materials[i] = Material.OrangeWallBanner;
            materials[2048] = Material.OrangeWool;
            materials[2085] = Material.OxeyeDaisy;
            materials[22948] = Material.OxidizedChiseledCopper;
            materials[22941] = Material.OxidizedCopper;
            for (int i = 24704; i <= 24707; i++)
                materials[i] = Material.OxidizedCopperBulb;
            for (int i = 23780; i <= 23843; i++)
                materials[i] = Material.OxidizedCopperDoor;
            for (int i = 24682; i <= 24683; i++)
                materials[i] = Material.OxidizedCopperGrate;
            for (int i = 24292; i <= 24355; i++)
                materials[i] = Material.OxidizedCopperTrapdoor;
            materials[22944] = Material.OxidizedCutCopper;
            for (int i = 23276; i <= 23281; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 22956; i <= 23035; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            materials[10746] = Material.PackedIce;
            materials[6541] = Material.PackedMud;
            for (int i = 26569; i <= 26571; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 10753; i <= 10754; i++)
                materials[i] = Material.Peony;
            for (int i = 11246; i <= 11251; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 9067; i <= 9098; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 9099; i <= 9106; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 10855; i <= 10870; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 1784; i <= 1799; i++)
                materials[i] = Material.PinkBed;
            for (int i = 20837; i <= 20852; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 21011; i <= 21012; i++)
                materials[i] = Material.PinkCandleCake;
            materials[10734] = Material.PinkCarpet;
            materials[12734] = Material.PinkConcrete;
            materials[12750] = Material.PinkConcretePowder;
            for (int i = 12688; i <= 12691; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 24827; i <= 24842; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 12604; i <= 12609; i++)
                materials[i] = Material.PinkShulkerBox;
            materials[5951] = Material.PinkStainedGlass;
            for (int i = 9564; i <= 9595; i++)
                materials[i] = Material.PinkStainedGlassPane;
            materials[9362] = Material.PinkTerracotta;
            materials[2084] = Material.PinkTulip;
            for (int i = 11039; i <= 11042; i++)
                materials[i] = Material.PinkWallBanner;
            materials[2053] = Material.PinkWool;
            for (int i = 2011; i <= 2022; i++)
                materials[i] = Material.Piston;
            for (int i = 2023; i <= 2046; i++)
                materials[i] = Material.PistonHead;
            for (int i = 12497; i <= 12506; i++)
                materials[i] = Material.PitcherCrop;
            for (int i = 12507; i <= 12508; i++)
                materials[i] = Material.PitcherPlant;
            for (int i = 8947; i <= 8978; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 8979; i <= 8986; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            for (int i = 24748; i <= 24767; i++)
                materials[i] = Material.PointedDripstone;
            materials[7] = Material.PolishedAndesite;
            for (int i = 14148; i <= 14153; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 13922; i <= 14001; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 5855; i <= 5857; i++)
                materials[i] = Material.PolishedBasalt;
            materials[19871] = Material.PolishedBlackstone;
            for (int i = 19875; i <= 19880; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 19881; i <= 19960; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 19961; i <= 20284; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            materials[19872] = Material.PolishedBlackstoneBricks;
            for (int i = 20374; i <= 20397; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 20372; i <= 20373; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 20366; i <= 20371; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 20286; i <= 20365; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 20398; i <= 20721; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            materials[25318] = Material.PolishedDeepslate;
            for (int i = 25399; i <= 25404; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 25319; i <= 25398; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 25405; i <= 25728; i++)
                materials[i] = Material.PolishedDeepslateWall;
            materials[5] = Material.PolishedDiorite;
            for (int i = 14100; i <= 14105; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 13202; i <= 13281; i++)
                materials[i] = Material.PolishedDioriteStairs;
            materials[3] = Material.PolishedGranite;
            for (int i = 14082; i <= 14087; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 12962; i <= 13041; i++)
                materials[i] = Material.PolishedGraniteStairs;
            materials[21492] = Material.PolishedTuff;
            for (int i = 21493; i <= 21498; i++)
                materials[i] = Material.PolishedTuffSlab;
            for (int i = 21499; i <= 21578; i++)
                materials[i] = Material.PolishedTuffStairs;
            for (int i = 21579; i <= 21902; i++)
                materials[i] = Material.PolishedTuffWall;
            materials[2077] = Material.Poppy;
            for (int i = 8603; i <= 8610; i++)
                materials[i] = Material.Potatoes;
            materials[8573] = Material.PottedAcaciaSapling;
            materials[8581] = Material.PottedAllium;
            materials[26561] = Material.PottedAzaleaBush;
            materials[8582] = Material.PottedAzureBluet;
            materials[12957] = Material.PottedBamboo;
            materials[8571] = Material.PottedBirchSapling;
            materials[8580] = Material.PottedBlueOrchid;
            materials[8592] = Material.PottedBrownMushroom;
            materials[8594] = Material.PottedCactus;
            materials[8574] = Material.PottedCherrySapling;
            materials[8588] = Material.PottedCornflower;
            materials[19455] = Material.PottedCrimsonFungus;
            materials[19457] = Material.PottedCrimsonRoots;
            materials[8578] = Material.PottedDandelion;
            materials[8575] = Material.PottedDarkOakSapling;
            materials[8593] = Material.PottedDeadBush;
            materials[8577] = Material.PottedFern;
            materials[26562] = Material.PottedFloweringAzaleaBush;
            materials[8572] = Material.PottedJungleSapling;
            materials[8589] = Material.PottedLilyOfTheValley;
            materials[8576] = Material.PottedMangrovePropagule;
            materials[8569] = Material.PottedOakSapling;
            materials[8584] = Material.PottedOrangeTulip;
            materials[8587] = Material.PottedOxeyeDaisy;
            materials[8586] = Material.PottedPinkTulip;
            materials[8579] = Material.PottedPoppy;
            materials[8591] = Material.PottedRedMushroom;
            materials[8583] = Material.PottedRedTulip;
            materials[8570] = Material.PottedSpruceSapling;
            materials[8568] = Material.PottedTorchflower;
            materials[19456] = Material.PottedWarpedFungus;
            materials[19458] = Material.PottedWarpedRoots;
            materials[8585] = Material.PottedWhiteTulip;
            materials[8590] = Material.PottedWitherRose;
            materials[22318] = Material.PowderSnow;
            for (int i = 7403; i <= 7405; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 1944; i <= 1967; i++)
                materials[i] = Material.PoweredRail;
            materials[10463] = Material.Prismarine;
            for (int i = 10712; i <= 10717; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 10546; i <= 10625; i++)
                materials[i] = Material.PrismarineBrickStairs;
            materials[10464] = Material.PrismarineBricks;
            for (int i = 10706; i <= 10711; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 10466; i <= 10545; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 14484; i <= 14807; i++)
                materials[i] = Material.PrismarineWall;
            materials[6811] = Material.Pumpkin;
            for (int i = 6821; i <= 6828; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 10919; i <= 10934; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 1848; i <= 1863; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 20901; i <= 20916; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 21019; i <= 21020; i++)
                materials[i] = Material.PurpleCandleCake;
            materials[10738] = Material.PurpleCarpet;
            materials[12738] = Material.PurpleConcrete;
            materials[12754] = Material.PurpleConcretePowder;
            for (int i = 12704; i <= 12707; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 12628; i <= 12633; i++)
                materials[i] = Material.PurpleShulkerBox;
            materials[5955] = Material.PurpleStainedGlass;
            for (int i = 9692; i <= 9723; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            materials[9366] = Material.PurpleTerracotta;
            for (int i = 11055; i <= 11058; i++)
                materials[i] = Material.PurpleWallBanner;
            materials[2057] = Material.PurpleWool;
            materials[12410] = Material.PurpurBlock;
            for (int i = 12411; i <= 12413; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 11300; i <= 11305; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 12414; i <= 12493; i++)
                materials[i] = Material.PurpurStairs;
            materials[9235] = Material.QuartzBlock;
            materials[20724] = Material.QuartzBricks;
            for (int i = 9237; i <= 9239; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 11282; i <= 11287; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 9240; i <= 9319; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 4662; i <= 4681; i++)
                materials[i] = Material.Rail;
            materials[26559] = Material.RawCopperBlock;
            materials[26560] = Material.RawGoldBlock;
            materials[26558] = Material.RawIronBlock;
            for (int i = 10983; i <= 10998; i++)
                materials[i] = Material.RedBanner;
            for (int i = 1912; i <= 1927; i++)
                materials[i] = Material.RedBed;
            for (int i = 20965; i <= 20980; i++)
                materials[i] = Material.RedCandle;
            for (int i = 21027; i <= 21028; i++)
                materials[i] = Material.RedCandleCake;
            materials[10742] = Material.RedCarpet;
            materials[12742] = Material.RedConcrete;
            materials[12758] = Material.RedConcretePowder;
            for (int i = 12720; i <= 12723; i++)
                materials[i] = Material.RedGlazedTerracotta;
            materials[2090] = Material.RedMushroom;
            for (int i = 6613; i <= 6676; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 14142; i <= 14147; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 13842; i <= 13921; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 17076; i <= 17399; i++)
                materials[i] = Material.RedNetherBrickWall;
            materials[12545] = Material.RedNetherBricks;
            materials[117] = Material.RedSand;
            materials[11079] = Material.RedSandstone;
            for (int i = 11288; i <= 11293; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 11082; i <= 11161; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 14808; i <= 15131; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 12652; i <= 12657; i++)
                materials[i] = Material.RedShulkerBox;
            materials[5959] = Material.RedStainedGlass;
            for (int i = 9820; i <= 9851; i++)
                materials[i] = Material.RedStainedGlassPane;
            materials[9370] = Material.RedTerracotta;
            materials[2081] = Material.RedTulip;
            for (int i = 11071; i <= 11074; i++)
                materials[i] = Material.RedWallBanner;
            materials[2061] = Material.RedWool;
            materials[9223] = Material.RedstoneBlock;
            for (int i = 7417; i <= 7418; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 5734; i <= 5735; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 5738; i <= 5739; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 5740; i <= 5747; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 2978; i <= 4273; i++)
                materials[i] = Material.RedstoneWire;
            materials[26573] = Material.ReinforcedDeepslate;
            for (int i = 5881; i <= 5944; i++)
                materials[i] = Material.Repeater;
            for (int i = 12515; i <= 12526; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 19450; i <= 19454; i++)
                materials[i] = Material.RespawnAnchor;
            materials[24902] = Material.RootedDirt;
            for (int i = 10751; i <= 10752; i++)
                materials[i] = Material.RoseBush;
            materials[112] = Material.Sand;
            materials[535] = Material.Sandstone;
            for (int i = 11234; i <= 11239; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 7431; i <= 7510; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 17400; i <= 17723; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 18372; i <= 18403; i++)
                materials[i] = Material.Scaffolding;
            materials[22799] = Material.Sculk;
            for (int i = 22928; i <= 22929; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 22319; i <= 22414; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 22930; i <= 22937; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 22800; i <= 22927; i++)
                materials[i] = Material.SculkVein;
            materials[10724] = Material.SeaLantern;
            for (int i = 12933; i <= 12940; i++)
                materials[i] = Material.SeaPickle;
            materials[2008] = Material.Seagrass;
            materials[2005] = Material.ShortGrass;
            materials[18610] = Material.Shroomlight;
            for (int i = 12562; i <= 12567; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 8827; i <= 8858; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 8859; i <= 8866; i++)
                materials[i] = Material.SkeletonWallSkull;
            materials[10364] = Material.SlimeBlock;
            for (int i = 21069; i <= 21080; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 24884; i <= 24899; i++)
                materials[i] = Material.SmallDripleaf;
            materials[18466] = Material.SmithingTable;
            for (int i = 18420; i <= 18427; i++)
                materials[i] = Material.Smoker;
            materials[26557] = Material.SmoothBasalt;
            materials[11308] = Material.SmoothQuartz;
            for (int i = 14124; i <= 14129; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 13602; i <= 13681; i++)
                materials[i] = Material.SmoothQuartzStairs;
            materials[11309] = Material.SmoothRedSandstone;
            for (int i = 14088; i <= 14093; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 13042; i <= 13121; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            materials[11307] = Material.SmoothSandstone;
            for (int i = 14118; i <= 14123; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 13522; i <= 13601; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            materials[11306] = Material.SmoothStone;
            for (int i = 11228; i <= 11233; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 12800; i <= 12802; i++)
                materials[i] = Material.SnifferEgg;
            for (int i = 5772; i <= 5779; i++)
                materials[i] = Material.Snow;
            materials[5781] = Material.SnowBlock;
            for (int i = 18543; i <= 18574; i++)
                materials[i] = Material.SoulCampfire;
            materials[2872] = Material.SoulFire;
            for (int i = 18507; i <= 18510; i++)
                materials[i] = Material.SoulLantern;
            materials[5850] = Material.SoulSand;
            materials[5851] = Material.SoulSoil;
            materials[5858] = Material.SoulTorch;
            for (int i = 5859; i <= 5862; i++)
                materials[i] = Material.SoulWallTorch;
            materials[2873] = Material.Spawner;
            materials[517] = Material.Sponge;
            materials[24823] = Material.SporeBlossom;
            for (int i = 8635; i <= 8658; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 11822; i <= 11885; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 11566; i <= 11597; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 11310; i <= 11341; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 4898; i <= 4961; i++)
                materials[i] = Material.SpruceHangingSign;
            for (int i = 265; i <= 292; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 133; i <= 135; i++)
                materials[i] = Material.SpruceLog;
            materials[16] = Material.SprucePlanks;
            for (int i = 5718; i <= 5719; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 27; i <= 28; i++)
                materials[i] = Material.SpruceSapling;
            for (int i = 4334; i <= 4365; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 11168; i <= 11173; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 7666; i <= 7745; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 6025; i <= 6088; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 5546; i <= 5553; i++)
                materials[i] = Material.SpruceWallHangingSign;
            for (int i = 4770; i <= 4777; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 192; i <= 194; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 1992; i <= 2003; i++)
                materials[i] = Material.StickyPiston;
            materials[1] = Material.Stone;
            for (int i = 11264; i <= 11269; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 7109; i <= 7188; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 15780; i <= 16103; i++)
                materials[i] = Material.StoneBrickWall;
            materials[6537] = Material.StoneBricks;
            for (int i = 5748; i <= 5771; i++)
                materials[i] = Material.StoneButton;
            for (int i = 5650; i <= 5651; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 11222; i <= 11227; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 13442; i <= 13521; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 18467; i <= 18470; i++)
                materials[i] = Material.Stonecutter;
            for (int i = 171; i <= 173; i++)
                materials[i] = Material.StrippedAcaciaLog;
            for (int i = 225; i <= 227; i++)
                materials[i] = Material.StrippedAcaciaWood;
            for (int i = 186; i <= 188; i++)
                materials[i] = Material.StrippedBambooBlock;
            for (int i = 165; i <= 167; i++)
                materials[i] = Material.StrippedBirchLog;
            for (int i = 219; i <= 221; i++)
                materials[i] = Material.StrippedBirchWood;
            for (int i = 174; i <= 176; i++)
                materials[i] = Material.StrippedCherryLog;
            for (int i = 228; i <= 230; i++)
                materials[i] = Material.StrippedCherryWood;
            for (int i = 18605; i <= 18607; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 18599; i <= 18601; i++)
                materials[i] = Material.StrippedCrimsonStem;
            for (int i = 177; i <= 179; i++)
                materials[i] = Material.StrippedDarkOakLog;
            for (int i = 231; i <= 233; i++)
                materials[i] = Material.StrippedDarkOakWood;
            for (int i = 168; i <= 170; i++)
                materials[i] = Material.StrippedJungleLog;
            for (int i = 222; i <= 224; i++)
                materials[i] = Material.StrippedJungleWood;
            for (int i = 183; i <= 185; i++)
                materials[i] = Material.StrippedMangroveLog;
            for (int i = 234; i <= 236; i++)
                materials[i] = Material.StrippedMangroveWood;
            for (int i = 180; i <= 182; i++)
                materials[i] = Material.StrippedOakLog;
            for (int i = 213; i <= 215; i++)
                materials[i] = Material.StrippedOakWood;
            for (int i = 162; i <= 164; i++)
                materials[i] = Material.StrippedSpruceLog;
            for (int i = 216; i <= 218; i++)
                materials[i] = Material.StrippedSpruceWood;
            for (int i = 18588; i <= 18590; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 18582; i <= 18584; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 19356; i <= 19359; i++)
                materials[i] = Material.StructureBlock;
            materials[12549] = Material.StructureVoid;
            for (int i = 5799; i <= 5814; i++)
                materials[i] = Material.SugarCane;
            for (int i = 10747; i <= 10748; i++)
                materials[i] = Material.Sunflower;
            for (int i = 119; i <= 122; i++)
                materials[i] = Material.SuspiciousGravel;
            for (int i = 113; i <= 116; i++)
                materials[i] = Material.SuspiciousSand;
            for (int i = 18575; i <= 18578; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 10755; i <= 10756; i++)
                materials[i] = Material.TallGrass;
            for (int i = 2009; i <= 2010; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 19381; i <= 19396; i++)
                materials[i] = Material.Target;
            materials[10744] = Material.Terracotta;
            materials[22317] = Material.TintedGlass;
            for (int i = 2094; i <= 2095; i++)
                materials[i] = Material.Tnt;
            materials[2355] = Material.Torch;
            materials[2076] = Material.Torchflower;
            for (int i = 12495; i <= 12496; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 9119; i <= 9142; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 26638; i <= 26643; i++)
                materials[i] = Material.TrialSpawner;
            for (int i = 7537; i <= 7664; i++)
                materials[i] = Material.Tripwire;
            for (int i = 7521; i <= 7536; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 12823; i <= 12824; i++)
                materials[i] = Material.TubeCoral;
            materials[12808] = Material.TubeCoralBlock;
            for (int i = 12843; i <= 12844; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 12893; i <= 12900; i++)
                materials[i] = Material.TubeCoralWallFan;
            materials[21081] = Material.Tuff;
            for (int i = 21905; i <= 21910; i++)
                materials[i] = Material.TuffBrickSlab;
            for (int i = 21911; i <= 21990; i++)
                materials[i] = Material.TuffBrickStairs;
            for (int i = 21991; i <= 22314; i++)
                materials[i] = Material.TuffBrickWall;
            materials[21904] = Material.TuffBricks;
            for (int i = 21082; i <= 21087; i++)
                materials[i] = Material.TuffSlab;
            for (int i = 21088; i <= 21167; i++)
                materials[i] = Material.TuffStairs;
            for (int i = 21168; i <= 21491; i++)
                materials[i] = Material.TuffWall;
            for (int i = 12788; i <= 12799; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 18638; i <= 18663; i++)
                materials[i] = Material.TwistingVines;
            materials[18664] = Material.TwistingVinesPlant;
            for (int i = 26566; i <= 26568; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 6837; i <= 6868; i++)
                materials[i] = Material.Vine;
            materials[12958] = Material.VoidAir;
            for (int i = 2356; i <= 2359; i++)
                materials[i] = Material.WallTorch;
            for (int i = 19124; i <= 19147; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 19212; i <= 19275; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 18716; i <= 18747; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 18908; i <= 18939; i++)
                materials[i] = Material.WarpedFenceGate;
            materials[18592] = Material.WarpedFungus;
            for (int i = 5346; i <= 5409; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 18585; i <= 18587; i++)
                materials[i] = Material.WarpedHyphae;
            materials[18591] = Material.WarpedNylium;
            materials[18667] = Material.WarpedPlanks;
            for (int i = 18682; i <= 18683; i++)
                materials[i] = Material.WarpedPressurePlate;
            materials[18594] = Material.WarpedRoots;
            for (int i = 19308; i <= 19339; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 18674; i <= 18679; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 19020; i <= 19099; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 18579; i <= 18581; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 18812; i <= 18875; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 5610; i <= 5617; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 19348; i <= 19355; i++)
                materials[i] = Material.WarpedWallSign;
            materials[18593] = Material.WarpedWartBlock;
            for (int i = 80; i <= 95; i++)
                materials[i] = Material.Water;
            for (int i = 7399; i <= 7401; i++)
                materials[i] = Material.WaterCauldron;
            materials[22955] = Material.WaxedChiseledCopper;
            materials[23300] = Material.WaxedCopperBlock;
            for (int i = 24708; i <= 24711; i++)
                materials[i] = Material.WaxedCopperBulb;
            for (int i = 23908; i <= 23971; i++)
                materials[i] = Material.WaxedCopperDoor;
            for (int i = 24684; i <= 24685; i++)
                materials[i] = Material.WaxedCopperGrate;
            for (int i = 24420; i <= 24483; i++)
                materials[i] = Material.WaxedCopperTrapdoor;
            materials[23307] = Material.WaxedCutCopper;
            for (int i = 23646; i <= 23651; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 23548; i <= 23627; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            materials[22954] = Material.WaxedExposedChiseledCopper;
            materials[23302] = Material.WaxedExposedCopper;
            for (int i = 24712; i <= 24715; i++)
                materials[i] = Material.WaxedExposedCopperBulb;
            for (int i = 23972; i <= 24035; i++)
                materials[i] = Material.WaxedExposedCopperDoor;
            for (int i = 24686; i <= 24687; i++)
                materials[i] = Material.WaxedExposedCopperGrate;
            for (int i = 24484; i <= 24547; i++)
                materials[i] = Material.WaxedExposedCopperTrapdoor;
            materials[23306] = Material.WaxedExposedCutCopper;
            for (int i = 23640; i <= 23645; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 23468; i <= 23547; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            materials[22952] = Material.WaxedOxidizedChiseledCopper;
            materials[23303] = Material.WaxedOxidizedCopper;
            for (int i = 24720; i <= 24723; i++)
                materials[i] = Material.WaxedOxidizedCopperBulb;
            for (int i = 24036; i <= 24099; i++)
                materials[i] = Material.WaxedOxidizedCopperDoor;
            for (int i = 24690; i <= 24691; i++)
                materials[i] = Material.WaxedOxidizedCopperGrate;
            for (int i = 24548; i <= 24611; i++)
                materials[i] = Material.WaxedOxidizedCopperTrapdoor;
            materials[23304] = Material.WaxedOxidizedCutCopper;
            for (int i = 23628; i <= 23633; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 23308; i <= 23387; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            materials[22953] = Material.WaxedWeatheredChiseledCopper;
            materials[23301] = Material.WaxedWeatheredCopper;
            for (int i = 24716; i <= 24719; i++)
                materials[i] = Material.WaxedWeatheredCopperBulb;
            for (int i = 24100; i <= 24163; i++)
                materials[i] = Material.WaxedWeatheredCopperDoor;
            for (int i = 24688; i <= 24689; i++)
                materials[i] = Material.WaxedWeatheredCopperGrate;
            for (int i = 24612; i <= 24675; i++)
                materials[i] = Material.WaxedWeatheredCopperTrapdoor;
            materials[23305] = Material.WaxedWeatheredCutCopper;
            for (int i = 23634; i <= 23639; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 23388; i <= 23467; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            materials[22949] = Material.WeatheredChiseledCopper;
            materials[22940] = Material.WeatheredCopper;
            for (int i = 24700; i <= 24703; i++)
                materials[i] = Material.WeatheredCopperBulb;
            for (int i = 23844; i <= 23907; i++)
                materials[i] = Material.WeatheredCopperDoor;
            for (int i = 24680; i <= 24681; i++)
                materials[i] = Material.WeatheredCopperGrate;
            for (int i = 24356; i <= 24419; i++)
                materials[i] = Material.WeatheredCopperTrapdoor;
            materials[22945] = Material.WeatheredCutCopper;
            for (int i = 23282; i <= 23287; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 23036; i <= 23115; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 18611; i <= 18636; i++)
                materials[i] = Material.WeepingVines;
            materials[18637] = Material.WeepingVinesPlant;
            materials[518] = Material.WetSponge;
            for (int i = 4278; i <= 4285; i++)
                materials[i] = Material.Wheat;
            for (int i = 10759; i <= 10774; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 1688; i <= 1703; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 20741; i <= 20756; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 20999; i <= 21000; i++)
                materials[i] = Material.WhiteCandleCake;
            materials[10728] = Material.WhiteCarpet;
            materials[12728] = Material.WhiteConcrete;
            materials[12744] = Material.WhiteConcretePowder;
            for (int i = 12664; i <= 12667; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 12568; i <= 12573; i++)
                materials[i] = Material.WhiteShulkerBox;
            materials[5945] = Material.WhiteStainedGlass;
            for (int i = 9372; i <= 9403; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            materials[9356] = Material.WhiteTerracotta;
            materials[2083] = Material.WhiteTulip;
            for (int i = 11015; i <= 11018; i++)
                materials[i] = Material.WhiteWallBanner;
            materials[2047] = Material.WhiteWool;
            materials[2087] = Material.WitherRose;
            for (int i = 8867; i <= 8898; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 8899; i <= 8906; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 10823; i <= 10838; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 1752; i <= 1767; i++)
                materials[i] = Material.YellowBed;
            for (int i = 20805; i <= 20820; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 21007; i <= 21008; i++)
                materials[i] = Material.YellowCandleCake;
            materials[10732] = Material.YellowCarpet;
            materials[12732] = Material.YellowConcrete;
            materials[12748] = Material.YellowConcretePowder;
            for (int i = 12680; i <= 12683; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 12592; i <= 12597; i++)
                materials[i] = Material.YellowShulkerBox;
            materials[5949] = Material.YellowStainedGlass;
            for (int i = 9500; i <= 9531; i++)
                materials[i] = Material.YellowStainedGlassPane;
            materials[9360] = Material.YellowTerracotta;
            for (int i = 11031; i <= 11034; i++)
                materials[i] = Material.YellowWallBanner;
            materials[2051] = Material.YellowWool;
            for (int i = 8907; i <= 8938; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 8939; i <= 8946; i++)
                materials[i] = Material.ZombieWallHead;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
