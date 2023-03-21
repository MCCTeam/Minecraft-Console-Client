using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette1194 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette1194()
        {
            for (int i = 8703; i <= 8726; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 11869; i <= 11932; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 11517; i <= 11548; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 11261; i <= 11292; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 5022; i <= 5085; i++)
                materials[i] = Material.AcaciaHangingSign;
            for (int i = 345; i <= 372; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 138; i <= 140; i++)
                materials[i] = Material.AcaciaLog;
            materials[19] = Material.AcaciaPlanks;
            for (int i = 5720; i <= 5721; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 33; i <= 34; i++)
                materials[i] = Material.AcaciaSapling;
            for (int i = 4394; i <= 4425; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 11041; i <= 11046; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 9740; i <= 9819; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 6214; i <= 6277; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 5558; i <= 5565; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 4782; i <= 4789; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 197; i <= 199; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 9176; i <= 9199; i++)
                materials[i] = Material.ActivatorRail;
            materials[0] = Material.Air;
            materials[2075] = Material.Allium;
            materials[20872] = Material.AmethystBlock;
            for (int i = 20874; i <= 20885; i++)
                materials[i] = Material.AmethystCluster;
            materials[19289] = Material.AncientDebris;
            materials[6] = Material.Andesite;
            for (int i = 13977; i <= 13982; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 13603; i <= 13682; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 16593; i <= 16916; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 8963; i <= 8966; i++)
                materials[i] = Material.Anvil;
            for (int i = 6813; i <= 6816; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 6809; i <= 6812; i++)
                materials[i] = Material.AttachedPumpkinStem;
            materials[21967] = Material.Azalea;
            for (int i = 457; i <= 484; i++)
                materials[i] = Material.AzaleaLeaves;
            materials[2076] = Material.AzureBluet;
            for (int i = 12786; i <= 12797; i++)
                materials[i] = Material.Bamboo;
            for (int i = 155; i <= 157; i++)
                materials[i] = Material.BambooBlock;
            for (int i = 8799; i <= 8822; i++)
                materials[i] = Material.BambooButton;
            for (int i = 12125; i <= 12188; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 11645; i <= 11676; i++)
                materials[i] = Material.BambooFence;
            for (int i = 11389; i <= 11420; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 5470; i <= 5533; i++)
                materials[i] = Material.BambooHangingSign;
            materials[24] = Material.BambooMosaic;
            for (int i = 11071; i <= 11076; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 10140; i <= 10219; i++)
                materials[i] = Material.BambooMosaicStairs;
            materials[23] = Material.BambooPlanks;
            for (int i = 5728; i <= 5729; i++)
                materials[i] = Material.BambooPressurePlate;
            materials[12785] = Material.BambooSapling;
            for (int i = 4554; i <= 4585; i++)
                materials[i] = Material.BambooSign;
            for (int i = 11065; i <= 11070; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 10060; i <= 10139; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 6470; i <= 6533; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 5614; i <= 5621; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 4822; i <= 4829; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 18249; i <= 18260; i++)
                materials[i] = Material.Barrel;
            materials[10221] = Material.Barrier;
            for (int i = 5849; i <= 5851; i++)
                materials[i] = Material.Basalt;
            materials[7914] = Material.Beacon;
            materials[79] = Material.Bedrock;
            for (int i = 19238; i <= 19261; i++)
                materials[i] = Material.BeeNest;
            for (int i = 19262; i <= 19285; i++)
                materials[i] = Material.Beehive;
            for (int i = 12353; i <= 12356; i++)
                materials[i] = Material.Beetroots;
            for (int i = 18312; i <= 18343; i++)
                materials[i] = Material.Bell;
            for (int i = 21987; i <= 22018; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 22019; i <= 22026; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 8655; i <= 8678; i++)
                materials[i] = Material.BirchButton;
            for (int i = 11741; i <= 11804; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 11453; i <= 11484; i++)
                materials[i] = Material.BirchFence;
            for (int i = 11197; i <= 11228; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 4958; i <= 5021; i++)
                materials[i] = Material.BirchHangingSign;
            for (int i = 289; i <= 316; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 132; i <= 134; i++)
                materials[i] = Material.BirchLog;
            materials[17] = Material.BirchPlanks;
            for (int i = 5716; i <= 5717; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 29; i <= 30; i++)
                materials[i] = Material.BirchSapling;
            for (int i = 4362; i <= 4393; i++)
                materials[i] = Material.BirchSign;
            for (int i = 11029; i <= 11034; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 7742; i <= 7821; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 6086; i <= 6149; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 5550; i <= 5557; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 4774; i <= 4781; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 191; i <= 193; i++)
                materials[i] = Material.BirchWood;
            for (int i = 10854; i <= 10869; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 1924; i <= 1939; i++)
                materials[i] = Material.BlackBed;
            for (int i = 20822; i <= 20837; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 20870; i <= 20871; i++)
                materials[i] = Material.BlackCandleCake;
            materials[10598] = Material.BlackCarpet;
            materials[12587] = Material.BlackConcrete;
            materials[12603] = Material.BlackConcretePowder;
            for (int i = 12568; i <= 12571; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 12502; i <= 12507; i++)
                materials[i] = Material.BlackShulkerBox;
            materials[5957] = Material.BlackStainedGlass;
            for (int i = 9708; i <= 9739; i++)
                materials[i] = Material.BlackStainedGlassPane;
            materials[9227] = Material.BlackTerracotta;
            for (int i = 10930; i <= 10933; i++)
                materials[i] = Material.BlackWallBanner;
            materials[2058] = Material.BlackWool;
            materials[19301] = Material.Blackstone;
            for (int i = 19706; i <= 19711; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 19302; i <= 19381; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 19382; i <= 19705; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 18269; i <= 18276; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 10790; i <= 10805; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 1860; i <= 1875; i++)
                materials[i] = Material.BlueBed;
            for (int i = 20758; i <= 20773; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 20862; i <= 20863; i++)
                materials[i] = Material.BlueCandleCake;
            materials[10594] = Material.BlueCarpet;
            materials[12583] = Material.BlueConcrete;
            materials[12599] = Material.BlueConcretePowder;
            for (int i = 12552; i <= 12555; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            materials[12782] = Material.BlueIce;
            materials[2074] = Material.BlueOrchid;
            for (int i = 12478; i <= 12483; i++)
                materials[i] = Material.BlueShulkerBox;
            materials[5953] = Material.BlueStainedGlass;
            for (int i = 9580; i <= 9611; i++)
                materials[i] = Material.BlueStainedGlassPane;
            materials[9223] = Material.BlueTerracotta;
            for (int i = 10914; i <= 10917; i++)
                materials[i] = Material.BlueWallBanner;
            materials[2054] = Material.BlueWool;
            for (int i = 12390; i <= 12392; i++)
                materials[i] = Material.BoneBlock;
            materials[2092] = Material.Bookshelf;
            for (int i = 12666; i <= 12667; i++)
                materials[i] = Material.BrainCoral;
            materials[12650] = Material.BrainCoralBlock;
            for (int i = 12686; i <= 12687; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 12742; i <= 12749; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 7386; i <= 7393; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 11113; i <= 11118; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 7025; i <= 7104; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 14001; i <= 14324; i++)
                materials[i] = Material.BrickWall;
            materials[2089] = Material.Bricks;
            for (int i = 10806; i <= 10821; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 1876; i <= 1891; i++)
                materials[i] = Material.BrownBed;
            for (int i = 20774; i <= 20789; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 20864; i <= 20865; i++)
                materials[i] = Material.BrownCandleCake;
            materials[10595] = Material.BrownCarpet;
            materials[12584] = Material.BrownConcrete;
            materials[12600] = Material.BrownConcretePowder;
            for (int i = 12556; i <= 12559; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            materials[2085] = Material.BrownMushroom;
            for (int i = 6546; i <= 6609; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 12484; i <= 12489; i++)
                materials[i] = Material.BrownShulkerBox;
            materials[5954] = Material.BrownStainedGlass;
            for (int i = 9612; i <= 9643; i++)
                materials[i] = Material.BrownStainedGlassPane;
            materials[9224] = Material.BrownTerracotta;
            for (int i = 10918; i <= 10921; i++)
                materials[i] = Material.BrownWallBanner;
            materials[2055] = Material.BrownWool;
            for (int i = 12801; i <= 12802; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 12668; i <= 12669; i++)
                materials[i] = Material.BubbleCoral;
            materials[12651] = Material.BubbleCoralBlock;
            for (int i = 12688; i <= 12689; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 12750; i <= 12757; i++)
                materials[i] = Material.BubbleCoralWallFan;
            materials[20873] = Material.BuddingAmethyst;
            for (int i = 5778; i <= 5793; i++)
                materials[i] = Material.Cactus;
            for (int i = 5871; i <= 5877; i++)
                materials[i] = Material.Cake;
            materials[20923] = Material.Calcite;
            for (int i = 18352; i <= 18383; i++)
                materials[i] = Material.Campfire;
            for (int i = 20566; i <= 20581; i++)
                materials[i] = Material.Candle;
            for (int i = 20838; i <= 20839; i++)
                materials[i] = Material.CandleCake;
            for (int i = 8591; i <= 8598; i++)
                materials[i] = Material.Carrots;
            materials[18277] = Material.CartographyTable;
            for (int i = 5863; i <= 5866; i++)
                materials[i] = Material.CarvedPumpkin;
            materials[7394] = Material.Cauldron;
            materials[12800] = Material.CaveAir;
            for (int i = 21912; i <= 21963; i++)
                materials[i] = Material.CaveVines;
            for (int i = 21964; i <= 21965; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 6770; i <= 6775; i++)
                materials[i] = Material.Chain;
            for (int i = 12371; i <= 12382; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 8727; i <= 8750; i++)
                materials[i] = Material.CherryButton;
            for (int i = 11933; i <= 11996; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 11549; i <= 11580; i++)
                materials[i] = Material.CherryFence;
            for (int i = 11293; i <= 11324; i++)
                materials[i] = Material.CherryFenceGate;
            for (int i = 5086; i <= 5149; i++)
                materials[i] = Material.CherryHangingSign;
            for (int i = 373; i <= 400; i++)
                materials[i] = Material.CherryLeaves;
            for (int i = 141; i <= 143; i++)
                materials[i] = Material.CherryLog;
            materials[20] = Material.CherryPlanks;
            for (int i = 5722; i <= 5723; i++)
                materials[i] = Material.CherryPressurePlate;
            for (int i = 35; i <= 36; i++)
                materials[i] = Material.CherrySapling;
            for (int i = 4426; i <= 4457; i++)
                materials[i] = Material.CherrySign;
            for (int i = 11047; i <= 11052; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 9820; i <= 9899; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 6278; i <= 6341; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 5566; i <= 5573; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 4790; i <= 4797; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 200; i <= 202; i++)
                materials[i] = Material.CherryWood;
            for (int i = 2950; i <= 2973; i++)
                materials[i] = Material.Chest;
            for (int i = 8967; i <= 8970; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 2093; i <= 2348; i++)
                materials[i] = Material.ChiseledBookshelf;
            materials[23694] = Material.ChiseledDeepslate;
            materials[20563] = Material.ChiseledNetherBricks;
            materials[19715] = Material.ChiseledPolishedBlackstone;
            materials[9092] = Material.ChiseledQuartzBlock;
            materials[10935] = Material.ChiseledRedSandstone;
            materials[532] = Material.ChiseledSandstone;
            materials[6537] = Material.ChiseledStoneBricks;
            for (int i = 12259; i <= 12264; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 12195; i <= 12258; i++)
                materials[i] = Material.ChorusPlant;
            materials[5794] = Material.Clay;
            materials[10600] = Material.CoalBlock;
            materials[123] = Material.CoalOre;
            materials[11] = Material.CoarseDirt;
            materials[22050] = Material.CobbledDeepslate;
            for (int i = 22131; i <= 22136; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 22051; i <= 22130; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 22137; i <= 22460; i++)
                materials[i] = Material.CobbledDeepslateWall;
            materials[14] = Material.Cobblestone;
            for (int i = 11107; i <= 11112; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 4678; i <= 4757; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 7915; i <= 8238; i++)
                materials[i] = Material.CobblestoneWall;
            materials[2000] = Material.Cobweb;
            for (int i = 7415; i <= 7426; i++)
                materials[i] = Material.Cocoa;
            for (int i = 7902; i <= 7913; i++)
                materials[i] = Material.CommandBlock;
            for (int i = 9031; i <= 9046; i++)
                materials[i] = Material.Comparator;
            for (int i = 19213; i <= 19221; i++)
                materials[i] = Material.Composter;
            for (int i = 12783; i <= 12784; i++)
                materials[i] = Material.Conduit;
            materials[21164] = Material.CopperBlock;
            materials[21165] = Material.CopperOre;
            materials[2082] = Material.Cornflower;
            materials[23695] = Material.CrackedDeepslateBricks;
            materials[23696] = Material.CrackedDeepslateTiles;
            materials[20564] = Material.CrackedNetherBricks;
            materials[19714] = Material.CrackedPolishedBlackstoneBricks;
            materials[6536] = Material.CrackedStoneBricks;
            materials[4273] = Material.CraftingTable;
            for (int i = 8903; i <= 8918; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 8919; i <= 8922; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 18941; i <= 18964; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 18989; i <= 19052; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 18525; i <= 18556; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 18717; i <= 18748; i++)
                materials[i] = Material.CrimsonFenceGate;
            materials[18450] = Material.CrimsonFungus;
            for (int i = 5278; i <= 5341; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 18443; i <= 18445; i++)
                materials[i] = Material.CrimsonHyphae;
            materials[18449] = Material.CrimsonNylium;
            materials[18507] = Material.CrimsonPlanks;
            for (int i = 18521; i <= 18522; i++)
                materials[i] = Material.CrimsonPressurePlate;
            materials[18506] = Material.CrimsonRoots;
            for (int i = 19117; i <= 19148; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 18509; i <= 18514; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 18781; i <= 18860; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 18437; i <= 18439; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 18589; i <= 18652; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 5598; i <= 5605; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 19181; i <= 19188; i++)
                materials[i] = Material.CrimsonWallSign;
            materials[19290] = Material.CryingObsidian;
            materials[21170] = Material.CutCopper;
            for (int i = 21509; i <= 21514; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 21411; i <= 21490; i++)
                materials[i] = Material.CutCopperStairs;
            materials[10936] = Material.CutRedSandstone;
            for (int i = 11149; i <= 11154; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            materials[533] = Material.CutSandstone;
            for (int i = 11095; i <= 11100; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 10758; i <= 10773; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 1828; i <= 1843; i++)
                materials[i] = Material.CyanBed;
            for (int i = 20726; i <= 20741; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 20858; i <= 20859; i++)
                materials[i] = Material.CyanCandleCake;
            materials[10592] = Material.CyanCarpet;
            materials[12581] = Material.CyanConcrete;
            materials[12597] = Material.CyanConcretePowder;
            for (int i = 12544; i <= 12547; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 12466; i <= 12471; i++)
                materials[i] = Material.CyanShulkerBox;
            materials[5951] = Material.CyanStainedGlass;
            for (int i = 9516; i <= 9547; i++)
                materials[i] = Material.CyanStainedGlassPane;
            materials[9221] = Material.CyanTerracotta;
            for (int i = 10906; i <= 10909; i++)
                materials[i] = Material.CyanWallBanner;
            materials[2052] = Material.CyanWool;
            for (int i = 8971; i <= 8974; i++)
                materials[i] = Material.DamagedAnvil;
            materials[2071] = Material.Dandelion;
            for (int i = 8751; i <= 8774; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 11997; i <= 12060; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 11581; i <= 11612; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 11325; i <= 11356; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 5214; i <= 5277; i++)
                materials[i] = Material.DarkOakHangingSign;
            for (int i = 401; i <= 428; i++)
                materials[i] = Material.DarkOakLeaves;
            for (int i = 144; i <= 146; i++)
                materials[i] = Material.DarkOakLog;
            materials[21] = Material.DarkOakPlanks;
            for (int i = 5724; i <= 5725; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 37; i <= 38; i++)
                materials[i] = Material.DarkOakSapling;
            for (int i = 4490; i <= 4521; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 11053; i <= 11058; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 9900; i <= 9979; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 6342; i <= 6405; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 5582; i <= 5589; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 4806; i <= 4813; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 203; i <= 205; i++)
                materials[i] = Material.DarkOakWood;
            materials[10320] = Material.DarkPrismarine;
            for (int i = 10573; i <= 10578; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 10481; i <= 10560; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 9047; i <= 9078; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 12656; i <= 12657; i++)
                materials[i] = Material.DeadBrainCoral;
            materials[12645] = Material.DeadBrainCoralBlock;
            for (int i = 12676; i <= 12677; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 12702; i <= 12709; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 12658; i <= 12659; i++)
                materials[i] = Material.DeadBubbleCoral;
            materials[12646] = Material.DeadBubbleCoralBlock;
            for (int i = 12678; i <= 12679; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 12710; i <= 12717; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            materials[2003] = Material.DeadBush;
            for (int i = 12660; i <= 12661; i++)
                materials[i] = Material.DeadFireCoral;
            materials[12647] = Material.DeadFireCoralBlock;
            for (int i = 12680; i <= 12681; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 12718; i <= 12725; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 12662; i <= 12663; i++)
                materials[i] = Material.DeadHornCoral;
            materials[12648] = Material.DeadHornCoralBlock;
            for (int i = 12682; i <= 12683; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 12726; i <= 12733; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 12654; i <= 12655; i++)
                materials[i] = Material.DeadTubeCoral;
            materials[12644] = Material.DeadTubeCoralBlock;
            for (int i = 12674; i <= 12675; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 12694; i <= 12701; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 23717; i <= 23724; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 22047; i <= 22049; i++)
                materials[i] = Material.Deepslate;
            for (int i = 23364; i <= 23369; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 23284; i <= 23363; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 23370; i <= 23693; i++)
                materials[i] = Material.DeepslateBrickWall;
            materials[23283] = Material.DeepslateBricks;
            materials[124] = Material.DeepslateCoalOre;
            materials[21166] = Material.DeepslateCopperOre;
            materials[4271] = Material.DeepslateDiamondOre;
            materials[7508] = Material.DeepslateEmeraldOre;
            materials[120] = Material.DeepslateGoldOre;
            materials[122] = Material.DeepslateIronOre;
            materials[517] = Material.DeepslateLapisOre;
            for (int i = 5732; i <= 5733; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 22953; i <= 22958; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 22873; i <= 22952; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 22959; i <= 23282; i++)
                materials[i] = Material.DeepslateTileWall;
            materials[22872] = Material.DeepslateTiles;
            for (int i = 1964; i <= 1987; i++)
                materials[i] = Material.DetectorRail;
            materials[4272] = Material.DiamondBlock;
            materials[4270] = Material.DiamondOre;
            materials[4] = Material.Diorite;
            for (int i = 13995; i <= 14000; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 13843; i <= 13922; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 17889; i <= 18212; i++)
                materials[i] = Material.DioriteWall;
            materials[10] = Material.Dirt;
            materials[12357] = Material.DirtPath;
            for (int i = 519; i <= 530; i++)
                materials[i] = Material.Dispenser;
            materials[7412] = Material.DragonEgg;
            for (int i = 8923; i <= 8938; i++)
                materials[i] = Material.DragonHead;
            for (int i = 8939; i <= 8942; i++)
                materials[i] = Material.DragonWallHead;
            materials[12631] = Material.DriedKelpBlock;
            materials[21911] = Material.DripstoneBlock;
            for (int i = 9200; i <= 9211; i++)
                materials[i] = Material.Dropper;
            materials[7661] = Material.EmeraldBlock;
            materials[7507] = Material.EmeraldOre;
            materials[7385] = Material.EnchantingTable;
            materials[12358] = Material.EndGateway;
            materials[7402] = Material.EndPortal;
            for (int i = 7403; i <= 7410; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 12189; i <= 12194; i++)
                materials[i] = Material.EndRod;
            materials[7411] = Material.EndStone;
            for (int i = 13953; i <= 13958; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 13203; i <= 13282; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 17565; i <= 17888; i++)
                materials[i] = Material.EndStoneBrickWall;
            materials[12349] = Material.EndStoneBricks;
            for (int i = 7509; i <= 7516; i++)
                materials[i] = Material.EnderChest;
            materials[21163] = Material.ExposedCopper;
            materials[21169] = Material.ExposedCutCopper;
            for (int i = 21503; i <= 21508; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 21331; i <= 21410; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 4282; i <= 4289; i++)
                materials[i] = Material.Farmland;
            materials[2002] = Material.Fern;
            for (int i = 2356; i <= 2867; i++)
                materials[i] = Material.Fire;
            for (int i = 12670; i <= 12671; i++)
                materials[i] = Material.FireCoral;
            materials[12652] = Material.FireCoralBlock;
            for (int i = 12690; i <= 12691; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 12758; i <= 12765; i++)
                materials[i] = Material.FireCoralWallFan;
            materials[18278] = Material.FletchingTable;
            materials[8563] = Material.FlowerPot;
            materials[21968] = Material.FloweringAzalea;
            for (int i = 485; i <= 512; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            materials[23715] = Material.Frogspawn;
            for (int i = 12383; i <= 12386; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 4290; i <= 4297; i++)
                materials[i] = Material.Furnace;
            materials[20126] = Material.GildedBlackstone;
            materials[515] = Material.Glass;
            for (int i = 6776; i <= 6807; i++)
                materials[i] = Material.GlassPane;
            for (int i = 6865; i <= 6992; i++)
                materials[i] = Material.GlowLichen;
            materials[5860] = Material.Glowstone;
            materials[2087] = Material.GoldBlock;
            materials[119] = Material.GoldOre;
            materials[2] = Material.Granite;
            for (int i = 13971; i <= 13976; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 13523; i <= 13602; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 15297; i <= 15620; i++)
                materials[i] = Material.GraniteWall;
            materials[2001] = Material.Grass;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            materials[118] = Material.Gravel;
            for (int i = 10726; i <= 10741; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 1796; i <= 1811; i++)
                materials[i] = Material.GrayBed;
            for (int i = 20694; i <= 20709; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 20854; i <= 20855; i++)
                materials[i] = Material.GrayCandleCake;
            materials[10590] = Material.GrayCarpet;
            materials[12579] = Material.GrayConcrete;
            materials[12595] = Material.GrayConcretePowder;
            for (int i = 12536; i <= 12539; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 12454; i <= 12459; i++)
                materials[i] = Material.GrayShulkerBox;
            materials[5949] = Material.GrayStainedGlass;
            for (int i = 9452; i <= 9483; i++)
                materials[i] = Material.GrayStainedGlassPane;
            materials[9219] = Material.GrayTerracotta;
            for (int i = 10898; i <= 10901; i++)
                materials[i] = Material.GrayWallBanner;
            materials[2050] = Material.GrayWool;
            for (int i = 10822; i <= 10837; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 1892; i <= 1907; i++)
                materials[i] = Material.GreenBed;
            for (int i = 20790; i <= 20805; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 20866; i <= 20867; i++)
                materials[i] = Material.GreenCandleCake;
            materials[10596] = Material.GreenCarpet;
            materials[12585] = Material.GreenConcrete;
            materials[12601] = Material.GreenConcretePowder;
            for (int i = 12560; i <= 12563; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 12490; i <= 12495; i++)
                materials[i] = Material.GreenShulkerBox;
            materials[5955] = Material.GreenStainedGlass;
            for (int i = 9644; i <= 9675; i++)
                materials[i] = Material.GreenStainedGlassPane;
            materials[9225] = Material.GreenTerracotta;
            for (int i = 10922; i <= 10925; i++)
                materials[i] = Material.GreenWallBanner;
            materials[2056] = Material.GreenWool;
            for (int i = 18279; i <= 18290; i++)
                materials[i] = Material.Grindstone;
            for (int i = 22043; i <= 22044; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 10580; i <= 10582; i++)
                materials[i] = Material.HayBlock;
            for (int i = 9015; i <= 9030; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            materials[19286] = Material.HoneyBlock;
            materials[19287] = Material.HoneycombBlock;
            for (int i = 9081; i <= 9090; i++)
                materials[i] = Material.Hopper;
            for (int i = 12672; i <= 12673; i++)
                materials[i] = Material.HornCoral;
            materials[12653] = Material.HornCoralBlock;
            for (int i = 12692; i <= 12693; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 12766; i <= 12773; i++)
                materials[i] = Material.HornCoralWallFan;
            materials[5776] = Material.Ice;
            materials[6545] = Material.InfestedChiseledStoneBricks;
            materials[6541] = Material.InfestedCobblestone;
            materials[6544] = Material.InfestedCrackedStoneBricks;
            for (int i = 23697; i <= 23699; i++)
                materials[i] = Material.InfestedDeepslate;
            materials[6543] = Material.InfestedMossyStoneBricks;
            materials[6540] = Material.InfestedStone;
            materials[6542] = Material.InfestedStoneBricks;
            for (int i = 6738; i <= 6769; i++)
                materials[i] = Material.IronBars;
            materials[2088] = Material.IronBlock;
            for (int i = 5648; i <= 5711; i++)
                materials[i] = Material.IronDoor;
            materials[121] = Material.IronOre;
            for (int i = 10254; i <= 10317; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 5867; i <= 5870; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 19201; i <= 19212; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 5811; i <= 5812; i++)
                materials[i] = Material.Jukebox;
            for (int i = 8679; i <= 8702; i++)
                materials[i] = Material.JungleButton;
            for (int i = 11805; i <= 11868; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 11485; i <= 11516; i++)
                materials[i] = Material.JungleFence;
            for (int i = 11229; i <= 11260; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 5150; i <= 5213; i++)
                materials[i] = Material.JungleHangingSign;
            for (int i = 317; i <= 344; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 135; i <= 137; i++)
                materials[i] = Material.JungleLog;
            materials[18] = Material.JunglePlanks;
            for (int i = 5718; i <= 5719; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 31; i <= 32; i++)
                materials[i] = Material.JungleSapling;
            for (int i = 4458; i <= 4489; i++)
                materials[i] = Material.JungleSign;
            for (int i = 11035; i <= 11040; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 7822; i <= 7901; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 6150; i <= 6213; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 5574; i <= 5581; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 4798; i <= 4805; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 194; i <= 196; i++)
                materials[i] = Material.JungleWood;
            for (int i = 12604; i <= 12629; i++)
                materials[i] = Material.Kelp;
            materials[12630] = Material.KelpPlant;
            for (int i = 4650; i <= 4657; i++)
                materials[i] = Material.Ladder;
            for (int i = 18344; i <= 18347; i++)
                materials[i] = Material.Lantern;
            materials[518] = Material.LapisBlock;
            materials[516] = Material.LapisOre;
            for (int i = 20886; i <= 20897; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 10612; i <= 10613; i++)
                materials[i] = Material.LargeFern;
            for (int i = 96; i <= 111; i++)
                materials[i] = Material.Lava;
            materials[7398] = Material.LavaCauldron;
            for (int i = 18291; i <= 18306; i++)
                materials[i] = Material.Lectern;
            for (int i = 5622; i <= 5645; i++)
                materials[i] = Material.Lever;
            for (int i = 10222; i <= 10253; i++)
                materials[i] = Material.Light;
            for (int i = 10662; i <= 10677; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 1732; i <= 1747; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 20630; i <= 20645; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 20846; i <= 20847; i++)
                materials[i] = Material.LightBlueCandleCake;
            materials[10586] = Material.LightBlueCarpet;
            materials[12575] = Material.LightBlueConcrete;
            materials[12591] = Material.LightBlueConcretePowder;
            for (int i = 12520; i <= 12523; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 12430; i <= 12435; i++)
                materials[i] = Material.LightBlueShulkerBox;
            materials[5945] = Material.LightBlueStainedGlass;
            for (int i = 9324; i <= 9355; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            materials[9215] = Material.LightBlueTerracotta;
            for (int i = 10882; i <= 10885; i++)
                materials[i] = Material.LightBlueWallBanner;
            materials[2046] = Material.LightBlueWool;
            for (int i = 10742; i <= 10757; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 1812; i <= 1827; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 20710; i <= 20725; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 20856; i <= 20857; i++)
                materials[i] = Material.LightGrayCandleCake;
            materials[10591] = Material.LightGrayCarpet;
            materials[12580] = Material.LightGrayConcrete;
            materials[12596] = Material.LightGrayConcretePowder;
            for (int i = 12540; i <= 12543; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 12460; i <= 12465; i++)
                materials[i] = Material.LightGrayShulkerBox;
            materials[5950] = Material.LightGrayStainedGlass;
            for (int i = 9484; i <= 9515; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            materials[9220] = Material.LightGrayTerracotta;
            for (int i = 10902; i <= 10905; i++)
                materials[i] = Material.LightGrayWallBanner;
            materials[2051] = Material.LightGrayWool;
            for (int i = 8999; i <= 9014; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 21867; i <= 21890; i++)
                materials[i] = Material.LightningRod;
            for (int i = 10604; i <= 10605; i++)
                materials[i] = Material.Lilac;
            materials[2084] = Material.LilyOfTheValley;
            materials[7267] = Material.LilyPad;
            for (int i = 10694; i <= 10709; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 1764; i <= 1779; i++)
                materials[i] = Material.LimeBed;
            for (int i = 20662; i <= 20677; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 20850; i <= 20851; i++)
                materials[i] = Material.LimeCandleCake;
            materials[10588] = Material.LimeCarpet;
            materials[12577] = Material.LimeConcrete;
            materials[12593] = Material.LimeConcretePowder;
            for (int i = 12528; i <= 12531; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 12442; i <= 12447; i++)
                materials[i] = Material.LimeShulkerBox;
            materials[5947] = Material.LimeStainedGlass;
            for (int i = 9388; i <= 9419; i++)
                materials[i] = Material.LimeStainedGlassPane;
            materials[9217] = Material.LimeTerracotta;
            for (int i = 10890; i <= 10893; i++)
                materials[i] = Material.LimeWallBanner;
            materials[2048] = Material.LimeWool;
            materials[19300] = Material.Lodestone;
            for (int i = 18245; i <= 18248; i++)
                materials[i] = Material.Loom;
            for (int i = 10646; i <= 10661; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 1716; i <= 1731; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 20614; i <= 20629; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 20844; i <= 20845; i++)
                materials[i] = Material.MagentaCandleCake;
            materials[10585] = Material.MagentaCarpet;
            materials[12574] = Material.MagentaConcrete;
            materials[12590] = Material.MagentaConcretePowder;
            for (int i = 12516; i <= 12519; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 12424; i <= 12429; i++)
                materials[i] = Material.MagentaShulkerBox;
            materials[5944] = Material.MagentaStainedGlass;
            for (int i = 9292; i <= 9323; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            materials[9214] = Material.MagentaTerracotta;
            for (int i = 10878; i <= 10881; i++)
                materials[i] = Material.MagentaWallBanner;
            materials[2045] = Material.MagentaWool;
            materials[12387] = Material.MagmaBlock;
            for (int i = 8775; i <= 8798; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 12061; i <= 12124; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 11613; i <= 11644; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 11357; i <= 11388; i++)
                materials[i] = Material.MangroveFenceGate;
            for (int i = 5406; i <= 5469; i++)
                materials[i] = Material.MangroveHangingSign;
            for (int i = 429; i <= 456; i++)
                materials[i] = Material.MangroveLeaves;
            for (int i = 147; i <= 149; i++)
                materials[i] = Material.MangroveLog;
            materials[22] = Material.MangrovePlanks;
            for (int i = 5726; i <= 5727; i++)
                materials[i] = Material.MangrovePressurePlate;
            for (int i = 39; i <= 78; i++)
                materials[i] = Material.MangrovePropagule;
            for (int i = 150; i <= 151; i++)
                materials[i] = Material.MangroveRoots;
            for (int i = 4522; i <= 4553; i++)
                materials[i] = Material.MangroveSign;
            for (int i = 11059; i <= 11064; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 9980; i <= 10059; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 6406; i <= 6469; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 5590; i <= 5597; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 4814; i <= 4821; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 206; i <= 208; i++)
                materials[i] = Material.MangroveWood;
            for (int i = 20898; i <= 20909; i++)
                materials[i] = Material.MediumAmethystBud;
            materials[6808] = Material.Melon;
            for (int i = 6825; i <= 6832; i++)
                materials[i] = Material.MelonStem;
            materials[21986] = Material.MossBlock;
            materials[21969] = Material.MossCarpet;
            materials[2349] = Material.MossyCobblestone;
            for (int i = 13947; i <= 13952; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 13123; i <= 13202; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 8239; i <= 8562; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 13935; i <= 13940; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 12963; i <= 13042; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 14973; i <= 15296; i++)
                materials[i] = Material.MossyStoneBrickWall;
            materials[6535] = Material.MossyStoneBricks;
            for (int i = 2059; i <= 2070; i++)
                materials[i] = Material.MovingPiston;
            materials[22046] = Material.Mud;
            for (int i = 11125; i <= 11130; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 7185; i <= 7264; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 15945; i <= 16268; i++)
                materials[i] = Material.MudBrickWall;
            materials[6539] = Material.MudBricks;
            for (int i = 152; i <= 154; i++)
                materials[i] = Material.MuddyMangroveRoots;
            for (int i = 6674; i <= 6737; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 7265; i <= 7266; i++)
                materials[i] = Material.Mycelium;
            for (int i = 7269; i <= 7300; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 11131; i <= 11136; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 7301; i <= 7380; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 16269; i <= 16592; i++)
                materials[i] = Material.NetherBrickWall;
            materials[7268] = Material.NetherBricks;
            materials[125] = Material.NetherGoldOre;
            for (int i = 5861; i <= 5862; i++)
                materials[i] = Material.NetherPortal;
            materials[9080] = Material.NetherQuartzOre;
            materials[18436] = Material.NetherSprouts;
            for (int i = 7381; i <= 7384; i++)
                materials[i] = Material.NetherWart;
            materials[12388] = Material.NetherWartBlock;
            materials[19288] = Material.NetheriteBlock;
            materials[5846] = Material.Netherrack;
            for (int i = 534; i <= 1683; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 8607; i <= 8630; i++)
                materials[i] = Material.OakButton;
            for (int i = 4586; i <= 4649; i++)
                materials[i] = Material.OakDoor;
            for (int i = 5813; i <= 5844; i++)
                materials[i] = Material.OakFence;
            for (int i = 6993; i <= 7024; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 4830; i <= 4893; i++)
                materials[i] = Material.OakHangingSign;
            for (int i = 233; i <= 260; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 126; i <= 128; i++)
                materials[i] = Material.OakLog;
            materials[15] = Material.OakPlanks;
            for (int i = 5712; i <= 5713; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 25; i <= 26; i++)
                materials[i] = Material.OakSapling;
            for (int i = 4298; i <= 4329; i++)
                materials[i] = Material.OakSign;
            for (int i = 11017; i <= 11022; i++)
                materials[i] = Material.OakSlab;
            for (int i = 2870; i <= 2949; i++)
                materials[i] = Material.OakStairs;
            for (int i = 5958; i <= 6021; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 5534; i <= 5541; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 4758; i <= 4765; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 185; i <= 187; i++)
                materials[i] = Material.OakWood;
            for (int i = 12394; i <= 12405; i++)
                materials[i] = Material.Observer;
            materials[2350] = Material.Obsidian;
            for (int i = 23706; i <= 23708; i++)
                materials[i] = Material.OchreFroglight;
            for (int i = 10630; i <= 10645; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 1700; i <= 1715; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 20598; i <= 20613; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 20842; i <= 20843; i++)
                materials[i] = Material.OrangeCandleCake;
            materials[10584] = Material.OrangeCarpet;
            materials[12573] = Material.OrangeConcrete;
            materials[12589] = Material.OrangeConcretePowder;
            for (int i = 12512; i <= 12515; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 12418; i <= 12423; i++)
                materials[i] = Material.OrangeShulkerBox;
            materials[5943] = Material.OrangeStainedGlass;
            for (int i = 9260; i <= 9291; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            materials[9213] = Material.OrangeTerracotta;
            materials[2078] = Material.OrangeTulip;
            for (int i = 10874; i <= 10877; i++)
                materials[i] = Material.OrangeWallBanner;
            materials[2044] = Material.OrangeWool;
            materials[2081] = Material.OxeyeDaisy;
            materials[21161] = Material.OxidizedCopper;
            materials[21167] = Material.OxidizedCutCopper;
            for (int i = 21491; i <= 21496; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 21171; i <= 21250; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            materials[10601] = Material.PackedIce;
            materials[6538] = Material.PackedMud;
            for (int i = 23712; i <= 23714; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 10608; i <= 10609; i++)
                materials[i] = Material.Peony;
            for (int i = 11101; i <= 11106; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 8943; i <= 8958; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 8959; i <= 8962; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 10710; i <= 10725; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 1780; i <= 1795; i++)
                materials[i] = Material.PinkBed;
            for (int i = 20678; i <= 20693; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 20852; i <= 20853; i++)
                materials[i] = Material.PinkCandleCake;
            materials[10589] = Material.PinkCarpet;
            materials[12578] = Material.PinkConcrete;
            materials[12594] = Material.PinkConcretePowder;
            for (int i = 12532; i <= 12535; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 21970; i <= 21985; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 12448; i <= 12453; i++)
                materials[i] = Material.PinkShulkerBox;
            materials[5948] = Material.PinkStainedGlass;
            for (int i = 9420; i <= 9451; i++)
                materials[i] = Material.PinkStainedGlassPane;
            materials[9218] = Material.PinkTerracotta;
            materials[2080] = Material.PinkTulip;
            for (int i = 10894; i <= 10897; i++)
                materials[i] = Material.PinkWallBanner;
            materials[2049] = Material.PinkWool;
            for (int i = 2007; i <= 2018; i++)
                materials[i] = Material.Piston;
            for (int i = 2019; i <= 2042; i++)
                materials[i] = Material.PistonHead;
            for (int i = 8883; i <= 8898; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 8899; i <= 8902; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            for (int i = 21891; i <= 21910; i++)
                materials[i] = Material.PointedDripstone;
            materials[7] = Material.PolishedAndesite;
            for (int i = 13989; i <= 13994; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 13763; i <= 13842; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 5852; i <= 5854; i++)
                materials[i] = Material.PolishedBasalt;
            materials[19712] = Material.PolishedBlackstone;
            for (int i = 19716; i <= 19721; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 19722; i <= 19801; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 19802; i <= 20125; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            materials[19713] = Material.PolishedBlackstoneBricks;
            for (int i = 20215; i <= 20238; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 20213; i <= 20214; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 20207; i <= 20212; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 20127; i <= 20206; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 20239; i <= 20562; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            materials[22461] = Material.PolishedDeepslate;
            for (int i = 22542; i <= 22547; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 22462; i <= 22541; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 22548; i <= 22871; i++)
                materials[i] = Material.PolishedDeepslateWall;
            materials[5] = Material.PolishedDiorite;
            for (int i = 13941; i <= 13946; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 13043; i <= 13122; i++)
                materials[i] = Material.PolishedDioriteStairs;
            materials[3] = Material.PolishedGranite;
            for (int i = 13923; i <= 13928; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 12803; i <= 12882; i++)
                materials[i] = Material.PolishedGraniteStairs;
            materials[2073] = Material.Poppy;
            for (int i = 8599; i <= 8606; i++)
                materials[i] = Material.Potatoes;
            materials[8569] = Material.PottedAcaciaSapling;
            materials[8577] = Material.PottedAllium;
            materials[23704] = Material.PottedAzaleaBush;
            materials[8578] = Material.PottedAzureBluet;
            materials[12798] = Material.PottedBamboo;
            materials[8567] = Material.PottedBirchSapling;
            materials[8576] = Material.PottedBlueOrchid;
            materials[8588] = Material.PottedBrownMushroom;
            materials[8590] = Material.PottedCactus;
            materials[8570] = Material.PottedCherrySapling;
            materials[8584] = Material.PottedCornflower;
            materials[19296] = Material.PottedCrimsonFungus;
            materials[19298] = Material.PottedCrimsonRoots;
            materials[8574] = Material.PottedDandelion;
            materials[8571] = Material.PottedDarkOakSapling;
            materials[8589] = Material.PottedDeadBush;
            materials[8573] = Material.PottedFern;
            materials[23705] = Material.PottedFloweringAzaleaBush;
            materials[8568] = Material.PottedJungleSapling;
            materials[8585] = Material.PottedLilyOfTheValley;
            materials[8572] = Material.PottedMangrovePropagule;
            materials[8565] = Material.PottedOakSapling;
            materials[8580] = Material.PottedOrangeTulip;
            materials[8583] = Material.PottedOxeyeDaisy;
            materials[8582] = Material.PottedPinkTulip;
            materials[8575] = Material.PottedPoppy;
            materials[8587] = Material.PottedRedMushroom;
            materials[8579] = Material.PottedRedTulip;
            materials[8566] = Material.PottedSpruceSapling;
            materials[8564] = Material.PottedTorchflower;
            materials[19297] = Material.PottedWarpedFungus;
            materials[19299] = Material.PottedWarpedRoots;
            materials[8581] = Material.PottedWhiteTulip;
            materials[8586] = Material.PottedWitherRose;
            materials[20925] = Material.PowderSnow;
            for (int i = 7399; i <= 7401; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 1940; i <= 1963; i++)
                materials[i] = Material.PoweredRail;
            materials[10318] = Material.Prismarine;
            for (int i = 10567; i <= 10572; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 10401; i <= 10480; i++)
                materials[i] = Material.PrismarineBrickStairs;
            materials[10319] = Material.PrismarineBricks;
            for (int i = 10561; i <= 10566; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 10321; i <= 10400; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 14325; i <= 14648; i++)
                materials[i] = Material.PrismarineWall;
            materials[5845] = Material.Pumpkin;
            for (int i = 6817; i <= 6824; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 10774; i <= 10789; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 1844; i <= 1859; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 20742; i <= 20757; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 20860; i <= 20861; i++)
                materials[i] = Material.PurpleCandleCake;
            materials[10593] = Material.PurpleCarpet;
            materials[12582] = Material.PurpleConcrete;
            materials[12598] = Material.PurpleConcretePowder;
            for (int i = 12548; i <= 12551; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 12472; i <= 12477; i++)
                materials[i] = Material.PurpleShulkerBox;
            materials[5952] = Material.PurpleStainedGlass;
            for (int i = 9548; i <= 9579; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            materials[9222] = Material.PurpleTerracotta;
            for (int i = 10910; i <= 10913; i++)
                materials[i] = Material.PurpleWallBanner;
            materials[2053] = Material.PurpleWool;
            materials[12265] = Material.PurpurBlock;
            for (int i = 12266; i <= 12268; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 11155; i <= 11160; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 12269; i <= 12348; i++)
                materials[i] = Material.PurpurStairs;
            materials[9091] = Material.QuartzBlock;
            materials[20565] = Material.QuartzBricks;
            for (int i = 9093; i <= 9095; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 11137; i <= 11142; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 9096; i <= 9175; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 4658; i <= 4677; i++)
                materials[i] = Material.Rail;
            materials[23702] = Material.RawCopperBlock;
            materials[23703] = Material.RawGoldBlock;
            materials[23701] = Material.RawIronBlock;
            for (int i = 10838; i <= 10853; i++)
                materials[i] = Material.RedBanner;
            for (int i = 1908; i <= 1923; i++)
                materials[i] = Material.RedBed;
            for (int i = 20806; i <= 20821; i++)
                materials[i] = Material.RedCandle;
            for (int i = 20868; i <= 20869; i++)
                materials[i] = Material.RedCandleCake;
            materials[10597] = Material.RedCarpet;
            materials[12586] = Material.RedConcrete;
            materials[12602] = Material.RedConcretePowder;
            for (int i = 12564; i <= 12567; i++)
                materials[i] = Material.RedGlazedTerracotta;
            materials[2086] = Material.RedMushroom;
            for (int i = 6610; i <= 6673; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 13983; i <= 13988; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 13683; i <= 13762; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 16917; i <= 17240; i++)
                materials[i] = Material.RedNetherBrickWall;
            materials[12389] = Material.RedNetherBricks;
            materials[117] = Material.RedSand;
            materials[10934] = Material.RedSandstone;
            for (int i = 11143; i <= 11148; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 10937; i <= 11016; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 14649; i <= 14972; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 12496; i <= 12501; i++)
                materials[i] = Material.RedShulkerBox;
            materials[5956] = Material.RedStainedGlass;
            for (int i = 9676; i <= 9707; i++)
                materials[i] = Material.RedStainedGlassPane;
            materials[9226] = Material.RedTerracotta;
            materials[2077] = Material.RedTulip;
            for (int i = 10926; i <= 10929; i++)
                materials[i] = Material.RedWallBanner;
            materials[2057] = Material.RedWool;
            materials[9079] = Material.RedstoneBlock;
            for (int i = 7413; i <= 7414; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 5730; i <= 5731; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 5734; i <= 5735; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 5736; i <= 5743; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 2974; i <= 4269; i++)
                materials[i] = Material.RedstoneWire;
            materials[23716] = Material.ReinforcedDeepslate;
            for (int i = 5878; i <= 5941; i++)
                materials[i] = Material.Repeater;
            for (int i = 12359; i <= 12370; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 19291; i <= 19295; i++)
                materials[i] = Material.RespawnAnchor;
            materials[22045] = Material.RootedDirt;
            for (int i = 10606; i <= 10607; i++)
                materials[i] = Material.RoseBush;
            materials[112] = Material.Sand;
            materials[531] = Material.Sandstone;
            for (int i = 11089; i <= 11094; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 7427; i <= 7506; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 17241; i <= 17564; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 18213; i <= 18244; i++)
                materials[i] = Material.Scaffolding;
            materials[21022] = Material.Sculk;
            for (int i = 21151; i <= 21152; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 20926; i <= 21021; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 21153; i <= 21160; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 21023; i <= 21150; i++)
                materials[i] = Material.SculkVein;
            materials[10579] = Material.SeaLantern;
            for (int i = 12774; i <= 12781; i++)
                materials[i] = Material.SeaPickle;
            materials[2004] = Material.Seagrass;
            materials[18451] = Material.Shroomlight;
            for (int i = 12406; i <= 12411; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 8823; i <= 8838; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 8839; i <= 8842; i++)
                materials[i] = Material.SkeletonWallSkull;
            materials[10220] = Material.SlimeBlock;
            for (int i = 20910; i <= 20921; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 22027; i <= 22042; i++)
                materials[i] = Material.SmallDripleaf;
            materials[18307] = Material.SmithingTable;
            for (int i = 18261; i <= 18268; i++)
                materials[i] = Material.Smoker;
            materials[23700] = Material.SmoothBasalt;
            materials[11163] = Material.SmoothQuartz;
            for (int i = 13965; i <= 13970; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 13443; i <= 13522; i++)
                materials[i] = Material.SmoothQuartzStairs;
            materials[11164] = Material.SmoothRedSandstone;
            for (int i = 13929; i <= 13934; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 12883; i <= 12962; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            materials[11162] = Material.SmoothSandstone;
            for (int i = 13959; i <= 13964; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 13363; i <= 13442; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            materials[11161] = Material.SmoothStone;
            for (int i = 11083; i <= 11088; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 5768; i <= 5775; i++)
                materials[i] = Material.Snow;
            materials[5777] = Material.SnowBlock;
            for (int i = 18384; i <= 18415; i++)
                materials[i] = Material.SoulCampfire;
            materials[2868] = Material.SoulFire;
            for (int i = 18348; i <= 18351; i++)
                materials[i] = Material.SoulLantern;
            materials[5847] = Material.SoulSand;
            materials[5848] = Material.SoulSoil;
            materials[5855] = Material.SoulTorch;
            for (int i = 5856; i <= 5859; i++)
                materials[i] = Material.SoulWallTorch;
            materials[2869] = Material.Spawner;
            materials[513] = Material.Sponge;
            materials[21966] = Material.SporeBlossom;
            for (int i = 8631; i <= 8654; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 11677; i <= 11740; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 11421; i <= 11452; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 11165; i <= 11196; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 4894; i <= 4957; i++)
                materials[i] = Material.SpruceHangingSign;
            for (int i = 261; i <= 288; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 129; i <= 131; i++)
                materials[i] = Material.SpruceLog;
            materials[16] = Material.SprucePlanks;
            for (int i = 5714; i <= 5715; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 27; i <= 28; i++)
                materials[i] = Material.SpruceSapling;
            for (int i = 4330; i <= 4361; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 11023; i <= 11028; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 7662; i <= 7741; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 6022; i <= 6085; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 5542; i <= 5549; i++)
                materials[i] = Material.SpruceWallHangingSign;
            for (int i = 4766; i <= 4773; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 188; i <= 190; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 1988; i <= 1999; i++)
                materials[i] = Material.StickyPiston;
            materials[1] = Material.Stone;
            for (int i = 11119; i <= 11124; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 7105; i <= 7184; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 15621; i <= 15944; i++)
                materials[i] = Material.StoneBrickWall;
            materials[6534] = Material.StoneBricks;
            for (int i = 5744; i <= 5767; i++)
                materials[i] = Material.StoneButton;
            for (int i = 5646; i <= 5647; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 11077; i <= 11082; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 13283; i <= 13362; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 18308; i <= 18311; i++)
                materials[i] = Material.Stonecutter;
            for (int i = 167; i <= 169; i++)
                materials[i] = Material.StrippedAcaciaLog;
            for (int i = 221; i <= 223; i++)
                materials[i] = Material.StrippedAcaciaWood;
            for (int i = 182; i <= 184; i++)
                materials[i] = Material.StrippedBambooBlock;
            for (int i = 161; i <= 163; i++)
                materials[i] = Material.StrippedBirchLog;
            for (int i = 215; i <= 217; i++)
                materials[i] = Material.StrippedBirchWood;
            for (int i = 170; i <= 172; i++)
                materials[i] = Material.StrippedCherryLog;
            for (int i = 224; i <= 226; i++)
                materials[i] = Material.StrippedCherryWood;
            for (int i = 18446; i <= 18448; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 18440; i <= 18442; i++)
                materials[i] = Material.StrippedCrimsonStem;
            for (int i = 173; i <= 175; i++)
                materials[i] = Material.StrippedDarkOakLog;
            for (int i = 227; i <= 229; i++)
                materials[i] = Material.StrippedDarkOakWood;
            for (int i = 164; i <= 166; i++)
                materials[i] = Material.StrippedJungleLog;
            for (int i = 218; i <= 220; i++)
                materials[i] = Material.StrippedJungleWood;
            for (int i = 179; i <= 181; i++)
                materials[i] = Material.StrippedMangroveLog;
            for (int i = 230; i <= 232; i++)
                materials[i] = Material.StrippedMangroveWood;
            for (int i = 176; i <= 178; i++)
                materials[i] = Material.StrippedOakLog;
            for (int i = 209; i <= 211; i++)
                materials[i] = Material.StrippedOakWood;
            for (int i = 158; i <= 160; i++)
                materials[i] = Material.StrippedSpruceLog;
            for (int i = 212; i <= 214; i++)
                materials[i] = Material.StrippedSpruceWood;
            for (int i = 18429; i <= 18431; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 18423; i <= 18425; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 19197; i <= 19200; i++)
                materials[i] = Material.StructureBlock;
            materials[12393] = Material.StructureVoid;
            for (int i = 5795; i <= 5810; i++)
                materials[i] = Material.SugarCane;
            for (int i = 10602; i <= 10603; i++)
                materials[i] = Material.Sunflower;
            for (int i = 113; i <= 116; i++)
                materials[i] = Material.SuspiciousSand;
            for (int i = 18416; i <= 18419; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 10610; i <= 10611; i++)
                materials[i] = Material.TallGrass;
            for (int i = 2005; i <= 2006; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 19222; i <= 19237; i++)
                materials[i] = Material.Target;
            materials[10599] = Material.Terracotta;
            materials[20924] = Material.TintedGlass;
            for (int i = 2090; i <= 2091; i++)
                materials[i] = Material.Tnt;
            materials[2351] = Material.Torch;
            materials[2072] = Material.Torchflower;
            for (int i = 12350; i <= 12352; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 8975; i <= 8998; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 7533; i <= 7660; i++)
                materials[i] = Material.Tripwire;
            for (int i = 7517; i <= 7532; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 12664; i <= 12665; i++)
                materials[i] = Material.TubeCoral;
            materials[12649] = Material.TubeCoralBlock;
            for (int i = 12684; i <= 12685; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 12734; i <= 12741; i++)
                materials[i] = Material.TubeCoralWallFan;
            materials[20922] = Material.Tuff;
            for (int i = 12632; i <= 12643; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 18479; i <= 18504; i++)
                materials[i] = Material.TwistingVines;
            materials[18505] = Material.TwistingVinesPlant;
            for (int i = 23709; i <= 23711; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 6833; i <= 6864; i++)
                materials[i] = Material.Vine;
            materials[12799] = Material.VoidAir;
            for (int i = 2352; i <= 2355; i++)
                materials[i] = Material.WallTorch;
            for (int i = 18965; i <= 18988; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 19053; i <= 19116; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 18557; i <= 18588; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 18749; i <= 18780; i++)
                materials[i] = Material.WarpedFenceGate;
            materials[18433] = Material.WarpedFungus;
            for (int i = 5342; i <= 5405; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 18426; i <= 18428; i++)
                materials[i] = Material.WarpedHyphae;
            materials[18432] = Material.WarpedNylium;
            materials[18508] = Material.WarpedPlanks;
            for (int i = 18523; i <= 18524; i++)
                materials[i] = Material.WarpedPressurePlate;
            materials[18435] = Material.WarpedRoots;
            for (int i = 19149; i <= 19180; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 18515; i <= 18520; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 18861; i <= 18940; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 18420; i <= 18422; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 18653; i <= 18716; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 5606; i <= 5613; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 19189; i <= 19196; i++)
                materials[i] = Material.WarpedWallSign;
            materials[18434] = Material.WarpedWartBlock;
            for (int i = 80; i <= 95; i++)
                materials[i] = Material.Water;
            for (int i = 7395; i <= 7397; i++)
                materials[i] = Material.WaterCauldron;
            materials[21515] = Material.WaxedCopperBlock;
            materials[21522] = Material.WaxedCutCopper;
            for (int i = 21861; i <= 21866; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 21763; i <= 21842; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            materials[21517] = Material.WaxedExposedCopper;
            materials[21521] = Material.WaxedExposedCutCopper;
            for (int i = 21855; i <= 21860; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 21683; i <= 21762; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            materials[21518] = Material.WaxedOxidizedCopper;
            materials[21519] = Material.WaxedOxidizedCutCopper;
            for (int i = 21843; i <= 21848; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 21523; i <= 21602; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            materials[21516] = Material.WaxedWeatheredCopper;
            materials[21520] = Material.WaxedWeatheredCutCopper;
            for (int i = 21849; i <= 21854; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 21603; i <= 21682; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            materials[21162] = Material.WeatheredCopper;
            materials[21168] = Material.WeatheredCutCopper;
            for (int i = 21497; i <= 21502; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 21251; i <= 21330; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 18452; i <= 18477; i++)
                materials[i] = Material.WeepingVines;
            materials[18478] = Material.WeepingVinesPlant;
            materials[514] = Material.WetSponge;
            for (int i = 4274; i <= 4281; i++)
                materials[i] = Material.Wheat;
            for (int i = 10614; i <= 10629; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 1684; i <= 1699; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 20582; i <= 20597; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 20840; i <= 20841; i++)
                materials[i] = Material.WhiteCandleCake;
            materials[10583] = Material.WhiteCarpet;
            materials[12572] = Material.WhiteConcrete;
            materials[12588] = Material.WhiteConcretePowder;
            for (int i = 12508; i <= 12511; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 12412; i <= 12417; i++)
                materials[i] = Material.WhiteShulkerBox;
            materials[5942] = Material.WhiteStainedGlass;
            for (int i = 9228; i <= 9259; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            materials[9212] = Material.WhiteTerracotta;
            materials[2079] = Material.WhiteTulip;
            for (int i = 10870; i <= 10873; i++)
                materials[i] = Material.WhiteWallBanner;
            materials[2043] = Material.WhiteWool;
            materials[2083] = Material.WitherRose;
            for (int i = 8843; i <= 8858; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 8859; i <= 8862; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 10678; i <= 10693; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 1748; i <= 1763; i++)
                materials[i] = Material.YellowBed;
            for (int i = 20646; i <= 20661; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 20848; i <= 20849; i++)
                materials[i] = Material.YellowCandleCake;
            materials[10587] = Material.YellowCarpet;
            materials[12576] = Material.YellowConcrete;
            materials[12592] = Material.YellowConcretePowder;
            for (int i = 12524; i <= 12527; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 12436; i <= 12441; i++)
                materials[i] = Material.YellowShulkerBox;
            materials[5946] = Material.YellowStainedGlass;
            for (int i = 9356; i <= 9387; i++)
                materials[i] = Material.YellowStainedGlassPane;
            materials[9216] = Material.YellowTerracotta;
            for (int i = 10886; i <= 10889; i++)
                materials[i] = Material.YellowWallBanner;
            materials[2047] = Material.YellowWool;
            for (int i = 8863; i <= 8878; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 8879; i <= 8882; i++)
                materials[i] = Material.ZombieWallHead;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
