using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette120 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette120()
        {
            for (int i = 8707; i <= 8730; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 11873; i <= 11936; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 11521; i <= 11552; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 11265; i <= 11296; i++)
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
            for (int i = 11045; i <= 11050; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 9744; i <= 9823; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 6218; i <= 6281; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 5562; i <= 5569; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 4786; i <= 4793; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 201; i <= 203; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 9180; i <= 9203; i++)
                materials[i] = Material.ActivatorRail;
            materials[0] = Material.Air;
            materials[2079] = Material.Allium;
            materials[20890] = Material.AmethystBlock;
            for (int i = 20892; i <= 20903; i++)
                materials[i] = Material.AmethystCluster;
            materials[19307] = Material.AncientDebris;
            materials[6] = Material.Andesite;
            for (int i = 13995; i <= 14000; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 13621; i <= 13700; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 16611; i <= 16934; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 8967; i <= 8970; i++)
                materials[i] = Material.Anvil;
            for (int i = 6817; i <= 6820; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 6813; i <= 6816; i++)
                materials[i] = Material.AttachedPumpkinStem;
            materials[22369] = Material.Azalea;
            for (int i = 461; i <= 488; i++)
                materials[i] = Material.AzaleaLeaves;
            materials[2080] = Material.AzureBluet;
            for (int i = 12804; i <= 12815; i++)
                materials[i] = Material.Bamboo;
            for (int i = 159; i <= 161; i++)
                materials[i] = Material.BambooBlock;
            for (int i = 8803; i <= 8826; i++)
                materials[i] = Material.BambooButton;
            for (int i = 12129; i <= 12192; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 11649; i <= 11680; i++)
                materials[i] = Material.BambooFence;
            for (int i = 11393; i <= 11424; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 5474; i <= 5537; i++)
                materials[i] = Material.BambooHangingSign;
            materials[24] = Material.BambooMosaic;
            for (int i = 11075; i <= 11080; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 10144; i <= 10223; i++)
                materials[i] = Material.BambooMosaicStairs;
            materials[23] = Material.BambooPlanks;
            for (int i = 5732; i <= 5733; i++)
                materials[i] = Material.BambooPressurePlate;
            materials[12803] = Material.BambooSapling;
            for (int i = 4558; i <= 4589; i++)
                materials[i] = Material.BambooSign;
            for (int i = 11069; i <= 11074; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 10064; i <= 10143; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 6474; i <= 6537; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 5618; i <= 5625; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 4826; i <= 4833; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 18267; i <= 18278; i++)
                materials[i] = Material.Barrel;
            materials[10225] = Material.Barrier;
            for (int i = 5853; i <= 5855; i++)
                materials[i] = Material.Basalt;
            materials[7918] = Material.Beacon;
            materials[79] = Material.Bedrock;
            for (int i = 19256; i <= 19279; i++)
                materials[i] = Material.BeeNest;
            for (int i = 19280; i <= 19303; i++)
                materials[i] = Material.Beehive;
            for (int i = 12368; i <= 12371; i++)
                materials[i] = Material.Beetroots;
            for (int i = 18330; i <= 18361; i++)
                materials[i] = Material.Bell;
            for (int i = 22389; i <= 22420; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 22421; i <= 22428; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 8659; i <= 8682; i++)
                materials[i] = Material.BirchButton;
            for (int i = 11745; i <= 11808; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 11457; i <= 11488; i++)
                materials[i] = Material.BirchFence;
            for (int i = 11201; i <= 11232; i++)
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
            for (int i = 11033; i <= 11038; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 7746; i <= 7825; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 6090; i <= 6153; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 5554; i <= 5561; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 4778; i <= 4785; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 195; i <= 197; i++)
                materials[i] = Material.BirchWood;
            for (int i = 10858; i <= 10873; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 1928; i <= 1943; i++)
                materials[i] = Material.BlackBed;
            for (int i = 20840; i <= 20855; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 20888; i <= 20889; i++)
                materials[i] = Material.BlackCandleCake;
            materials[10602] = Material.BlackCarpet;
            materials[12602] = Material.BlackConcrete;
            materials[12618] = Material.BlackConcretePowder;
            for (int i = 12583; i <= 12586; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 12517; i <= 12522; i++)
                materials[i] = Material.BlackShulkerBox;
            materials[5961] = Material.BlackStainedGlass;
            for (int i = 9712; i <= 9743; i++)
                materials[i] = Material.BlackStainedGlassPane;
            materials[9231] = Material.BlackTerracotta;
            for (int i = 10934; i <= 10937; i++)
                materials[i] = Material.BlackWallBanner;
            materials[2062] = Material.BlackWool;
            materials[19319] = Material.Blackstone;
            for (int i = 19724; i <= 19729; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 19320; i <= 19399; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 19400; i <= 19723; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 18287; i <= 18294; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 10794; i <= 10809; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 1864; i <= 1879; i++)
                materials[i] = Material.BlueBed;
            for (int i = 20776; i <= 20791; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 20880; i <= 20881; i++)
                materials[i] = Material.BlueCandleCake;
            materials[10598] = Material.BlueCarpet;
            materials[12598] = Material.BlueConcrete;
            materials[12614] = Material.BlueConcretePowder;
            for (int i = 12567; i <= 12570; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            materials[12800] = Material.BlueIce;
            materials[2078] = Material.BlueOrchid;
            for (int i = 12493; i <= 12498; i++)
                materials[i] = Material.BlueShulkerBox;
            materials[5957] = Material.BlueStainedGlass;
            for (int i = 9584; i <= 9615; i++)
                materials[i] = Material.BlueStainedGlassPane;
            materials[9227] = Material.BlueTerracotta;
            for (int i = 10918; i <= 10921; i++)
                materials[i] = Material.BlueWallBanner;
            materials[2058] = Material.BlueWool;
            for (int i = 12405; i <= 12407; i++)
                materials[i] = Material.BoneBlock;
            materials[2096] = Material.Bookshelf;
            for (int i = 12684; i <= 12685; i++)
                materials[i] = Material.BrainCoral;
            materials[12668] = Material.BrainCoralBlock;
            for (int i = 12704; i <= 12705; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 12760; i <= 12767; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 7390; i <= 7397; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 11117; i <= 11122; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 7029; i <= 7108; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 14019; i <= 14342; i++)
                materials[i] = Material.BrickWall;
            materials[2093] = Material.Bricks;
            for (int i = 10810; i <= 10825; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 1880; i <= 1895; i++)
                materials[i] = Material.BrownBed;
            for (int i = 20792; i <= 20807; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 20882; i <= 20883; i++)
                materials[i] = Material.BrownCandleCake;
            materials[10599] = Material.BrownCarpet;
            materials[12599] = Material.BrownConcrete;
            materials[12615] = Material.BrownConcretePowder;
            for (int i = 12571; i <= 12574; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            materials[2089] = Material.BrownMushroom;
            for (int i = 6550; i <= 6613; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 12499; i <= 12504; i++)
                materials[i] = Material.BrownShulkerBox;
            materials[5958] = Material.BrownStainedGlass;
            for (int i = 9616; i <= 9647; i++)
                materials[i] = Material.BrownStainedGlassPane;
            materials[9228] = Material.BrownTerracotta;
            for (int i = 10922; i <= 10925; i++)
                materials[i] = Material.BrownWallBanner;
            materials[2059] = Material.BrownWool;
            for (int i = 12819; i <= 12820; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 12686; i <= 12687; i++)
                materials[i] = Material.BubbleCoral;
            materials[12669] = Material.BubbleCoralBlock;
            for (int i = 12706; i <= 12707; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 12768; i <= 12775; i++)
                materials[i] = Material.BubbleCoralWallFan;
            materials[20891] = Material.BuddingAmethyst;
            for (int i = 5782; i <= 5797; i++)
                materials[i] = Material.Cactus;
            for (int i = 5875; i <= 5881; i++)
                materials[i] = Material.Cake;
            materials[20941] = Material.Calcite;
            for (int i = 21040; i <= 21423; i++)
                materials[i] = Material.CalibratedSculkSensor;
            for (int i = 18370; i <= 18401; i++)
                materials[i] = Material.Campfire;
            for (int i = 20584; i <= 20599; i++)
                materials[i] = Material.Candle;
            for (int i = 20856; i <= 20857; i++)
                materials[i] = Material.CandleCake;
            for (int i = 8595; i <= 8602; i++)
                materials[i] = Material.Carrots;
            materials[18295] = Material.CartographyTable;
            for (int i = 5867; i <= 5870; i++)
                materials[i] = Material.CarvedPumpkin;
            materials[7398] = Material.Cauldron;
            materials[12818] = Material.CaveAir;
            for (int i = 22314; i <= 22365; i++)
                materials[i] = Material.CaveVines;
            for (int i = 22366; i <= 22367; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 6774; i <= 6779; i++)
                materials[i] = Material.Chain;
            for (int i = 12386; i <= 12397; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 8731; i <= 8754; i++)
                materials[i] = Material.CherryButton;
            for (int i = 11937; i <= 12000; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 11553; i <= 11584; i++)
                materials[i] = Material.CherryFence;
            for (int i = 11297; i <= 11328; i++)
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
            for (int i = 11051; i <= 11056; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 9824; i <= 9903; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 6282; i <= 6345; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 5570; i <= 5577; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 4794; i <= 4801; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 204; i <= 206; i++)
                materials[i] = Material.CherryWood;
            for (int i = 2954; i <= 2977; i++)
                materials[i] = Material.Chest;
            for (int i = 8971; i <= 8974; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 2097; i <= 2352; i++)
                materials[i] = Material.ChiseledBookshelf;
            materials[24096] = Material.ChiseledDeepslate;
            materials[20581] = Material.ChiseledNetherBricks;
            materials[19733] = Material.ChiseledPolishedBlackstone;
            materials[9096] = Material.ChiseledQuartzBlock;
            materials[10939] = Material.ChiseledRedSandstone;
            materials[536] = Material.ChiseledSandstone;
            materials[6541] = Material.ChiseledStoneBricks;
            for (int i = 12263; i <= 12268; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 12199; i <= 12262; i++)
                materials[i] = Material.ChorusPlant;
            materials[5798] = Material.Clay;
            materials[10604] = Material.CoalBlock;
            materials[127] = Material.CoalOre;
            materials[11] = Material.CoarseDirt;
            materials[22452] = Material.CobbledDeepslate;
            for (int i = 22533; i <= 22538; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 22453; i <= 22532; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 22539; i <= 22862; i++)
                materials[i] = Material.CobbledDeepslateWall;
            materials[14] = Material.Cobblestone;
            for (int i = 11111; i <= 11116; i++)
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
            for (int i = 9035; i <= 9050; i++)
                materials[i] = Material.Comparator;
            for (int i = 19231; i <= 19239; i++)
                materials[i] = Material.Composter;
            for (int i = 12801; i <= 12802; i++)
                materials[i] = Material.Conduit;
            materials[21566] = Material.CopperBlock;
            materials[21567] = Material.CopperOre;
            materials[2086] = Material.Cornflower;
            materials[24097] = Material.CrackedDeepslateBricks;
            materials[24098] = Material.CrackedDeepslateTiles;
            materials[20582] = Material.CrackedNetherBricks;
            materials[19732] = Material.CrackedPolishedBlackstoneBricks;
            materials[6540] = Material.CrackedStoneBricks;
            materials[4277] = Material.CraftingTable;
            for (int i = 8907; i <= 8922; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 8923; i <= 8926; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 18959; i <= 18982; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 19007; i <= 19070; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 18543; i <= 18574; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 18735; i <= 18766; i++)
                materials[i] = Material.CrimsonFenceGate;
            materials[18468] = Material.CrimsonFungus;
            for (int i = 5282; i <= 5345; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 18461; i <= 18463; i++)
                materials[i] = Material.CrimsonHyphae;
            materials[18467] = Material.CrimsonNylium;
            materials[18525] = Material.CrimsonPlanks;
            for (int i = 18539; i <= 18540; i++)
                materials[i] = Material.CrimsonPressurePlate;
            materials[18524] = Material.CrimsonRoots;
            for (int i = 19135; i <= 19166; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 18527; i <= 18532; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 18799; i <= 18878; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 18455; i <= 18457; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 18607; i <= 18670; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 5602; i <= 5609; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 19199; i <= 19206; i++)
                materials[i] = Material.CrimsonWallSign;
            materials[19308] = Material.CryingObsidian;
            materials[21572] = Material.CutCopper;
            for (int i = 21911; i <= 21916; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 21813; i <= 21892; i++)
                materials[i] = Material.CutCopperStairs;
            materials[10940] = Material.CutRedSandstone;
            for (int i = 11153; i <= 11158; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            materials[537] = Material.CutSandstone;
            for (int i = 11099; i <= 11104; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 10762; i <= 10777; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 1832; i <= 1847; i++)
                materials[i] = Material.CyanBed;
            for (int i = 20744; i <= 20759; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 20876; i <= 20877; i++)
                materials[i] = Material.CyanCandleCake;
            materials[10596] = Material.CyanCarpet;
            materials[12596] = Material.CyanConcrete;
            materials[12612] = Material.CyanConcretePowder;
            for (int i = 12559; i <= 12562; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 12481; i <= 12486; i++)
                materials[i] = Material.CyanShulkerBox;
            materials[5955] = Material.CyanStainedGlass;
            for (int i = 9520; i <= 9551; i++)
                materials[i] = Material.CyanStainedGlassPane;
            materials[9225] = Material.CyanTerracotta;
            for (int i = 10910; i <= 10913; i++)
                materials[i] = Material.CyanWallBanner;
            materials[2056] = Material.CyanWool;
            for (int i = 8975; i <= 8978; i++)
                materials[i] = Material.DamagedAnvil;
            materials[2075] = Material.Dandelion;
            for (int i = 8755; i <= 8778; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 12001; i <= 12064; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 11585; i <= 11616; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 11329; i <= 11360; i++)
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
            for (int i = 11057; i <= 11062; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 9904; i <= 9983; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 6346; i <= 6409; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 5586; i <= 5593; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 4810; i <= 4817; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 207; i <= 209; i++)
                materials[i] = Material.DarkOakWood;
            materials[10324] = Material.DarkPrismarine;
            for (int i = 10577; i <= 10582; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 10485; i <= 10564; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 9051; i <= 9082; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 12674; i <= 12675; i++)
                materials[i] = Material.DeadBrainCoral;
            materials[12663] = Material.DeadBrainCoralBlock;
            for (int i = 12694; i <= 12695; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 12720; i <= 12727; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 12676; i <= 12677; i++)
                materials[i] = Material.DeadBubbleCoral;
            materials[12664] = Material.DeadBubbleCoralBlock;
            for (int i = 12696; i <= 12697; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 12728; i <= 12735; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            materials[2007] = Material.DeadBush;
            for (int i = 12678; i <= 12679; i++)
                materials[i] = Material.DeadFireCoral;
            materials[12665] = Material.DeadFireCoralBlock;
            for (int i = 12698; i <= 12699; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 12736; i <= 12743; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 12680; i <= 12681; i++)
                materials[i] = Material.DeadHornCoral;
            materials[12666] = Material.DeadHornCoralBlock;
            for (int i = 12700; i <= 12701; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 12744; i <= 12751; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 12672; i <= 12673; i++)
                materials[i] = Material.DeadTubeCoral;
            materials[12662] = Material.DeadTubeCoralBlock;
            for (int i = 12692; i <= 12693; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 12712; i <= 12719; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 24119; i <= 24134; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 22449; i <= 22451; i++)
                materials[i] = Material.Deepslate;
            for (int i = 23766; i <= 23771; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 23686; i <= 23765; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 23772; i <= 24095; i++)
                materials[i] = Material.DeepslateBrickWall;
            materials[23685] = Material.DeepslateBricks;
            materials[128] = Material.DeepslateCoalOre;
            materials[21568] = Material.DeepslateCopperOre;
            materials[4275] = Material.DeepslateDiamondOre;
            materials[7512] = Material.DeepslateEmeraldOre;
            materials[124] = Material.DeepslateGoldOre;
            materials[126] = Material.DeepslateIronOre;
            materials[521] = Material.DeepslateLapisOre;
            for (int i = 5736; i <= 5737; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 23355; i <= 23360; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 23275; i <= 23354; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 23361; i <= 23684; i++)
                materials[i] = Material.DeepslateTileWall;
            materials[23274] = Material.DeepslateTiles;
            for (int i = 1968; i <= 1991; i++)
                materials[i] = Material.DetectorRail;
            materials[4276] = Material.DiamondBlock;
            materials[4274] = Material.DiamondOre;
            materials[4] = Material.Diorite;
            for (int i = 14013; i <= 14018; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 13861; i <= 13940; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 17907; i <= 18230; i++)
                materials[i] = Material.DioriteWall;
            materials[10] = Material.Dirt;
            materials[12372] = Material.DirtPath;
            for (int i = 523; i <= 534; i++)
                materials[i] = Material.Dispenser;
            materials[7416] = Material.DragonEgg;
            for (int i = 8927; i <= 8942; i++)
                materials[i] = Material.DragonHead;
            for (int i = 8943; i <= 8946; i++)
                materials[i] = Material.DragonWallHead;
            materials[12646] = Material.DriedKelpBlock;
            materials[22313] = Material.DripstoneBlock;
            for (int i = 9204; i <= 9215; i++)
                materials[i] = Material.Dropper;
            materials[7665] = Material.EmeraldBlock;
            materials[7511] = Material.EmeraldOre;
            materials[7389] = Material.EnchantingTable;
            materials[12373] = Material.EndGateway;
            materials[7406] = Material.EndPortal;
            for (int i = 7407; i <= 7414; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 12193; i <= 12198; i++)
                materials[i] = Material.EndRod;
            materials[7415] = Material.EndStone;
            for (int i = 13971; i <= 13976; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 13221; i <= 13300; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 17583; i <= 17906; i++)
                materials[i] = Material.EndStoneBrickWall;
            materials[12353] = Material.EndStoneBricks;
            for (int i = 7513; i <= 7520; i++)
                materials[i] = Material.EnderChest;
            materials[21565] = Material.ExposedCopper;
            materials[21571] = Material.ExposedCutCopper;
            for (int i = 21905; i <= 21910; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 21733; i <= 21812; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 4286; i <= 4293; i++)
                materials[i] = Material.Farmland;
            materials[2006] = Material.Fern;
            for (int i = 2360; i <= 2871; i++)
                materials[i] = Material.Fire;
            for (int i = 12688; i <= 12689; i++)
                materials[i] = Material.FireCoral;
            materials[12670] = Material.FireCoralBlock;
            for (int i = 12708; i <= 12709; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 12776; i <= 12783; i++)
                materials[i] = Material.FireCoralWallFan;
            materials[18296] = Material.FletchingTable;
            materials[8567] = Material.FlowerPot;
            materials[22370] = Material.FloweringAzalea;
            for (int i = 489; i <= 516; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            materials[24117] = Material.Frogspawn;
            for (int i = 12398; i <= 12401; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 4294; i <= 4301; i++)
                materials[i] = Material.Furnace;
            materials[20144] = Material.GildedBlackstone;
            materials[519] = Material.Glass;
            for (int i = 6780; i <= 6811; i++)
                materials[i] = Material.GlassPane;
            for (int i = 6869; i <= 6996; i++)
                materials[i] = Material.GlowLichen;
            materials[5864] = Material.Glowstone;
            materials[2091] = Material.GoldBlock;
            materials[123] = Material.GoldOre;
            materials[2] = Material.Granite;
            for (int i = 13989; i <= 13994; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 13541; i <= 13620; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 15315; i <= 15638; i++)
                materials[i] = Material.GraniteWall;
            materials[2005] = Material.Grass;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            materials[118] = Material.Gravel;
            for (int i = 10730; i <= 10745; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 1800; i <= 1815; i++)
                materials[i] = Material.GrayBed;
            for (int i = 20712; i <= 20727; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 20872; i <= 20873; i++)
                materials[i] = Material.GrayCandleCake;
            materials[10594] = Material.GrayCarpet;
            materials[12594] = Material.GrayConcrete;
            materials[12610] = Material.GrayConcretePowder;
            for (int i = 12551; i <= 12554; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 12469; i <= 12474; i++)
                materials[i] = Material.GrayShulkerBox;
            materials[5953] = Material.GrayStainedGlass;
            for (int i = 9456; i <= 9487; i++)
                materials[i] = Material.GrayStainedGlassPane;
            materials[9223] = Material.GrayTerracotta;
            for (int i = 10902; i <= 10905; i++)
                materials[i] = Material.GrayWallBanner;
            materials[2054] = Material.GrayWool;
            for (int i = 10826; i <= 10841; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 1896; i <= 1911; i++)
                materials[i] = Material.GreenBed;
            for (int i = 20808; i <= 20823; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 20884; i <= 20885; i++)
                materials[i] = Material.GreenCandleCake;
            materials[10600] = Material.GreenCarpet;
            materials[12600] = Material.GreenConcrete;
            materials[12616] = Material.GreenConcretePowder;
            for (int i = 12575; i <= 12578; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 12505; i <= 12510; i++)
                materials[i] = Material.GreenShulkerBox;
            materials[5959] = Material.GreenStainedGlass;
            for (int i = 9648; i <= 9679; i++)
                materials[i] = Material.GreenStainedGlassPane;
            materials[9229] = Material.GreenTerracotta;
            for (int i = 10926; i <= 10929; i++)
                materials[i] = Material.GreenWallBanner;
            materials[2060] = Material.GreenWool;
            for (int i = 18297; i <= 18308; i++)
                materials[i] = Material.Grindstone;
            for (int i = 22445; i <= 22446; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 10584; i <= 10586; i++)
                materials[i] = Material.HayBlock;
            for (int i = 9019; i <= 9034; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            materials[19304] = Material.HoneyBlock;
            materials[19305] = Material.HoneycombBlock;
            for (int i = 9085; i <= 9094; i++)
                materials[i] = Material.Hopper;
            for (int i = 12690; i <= 12691; i++)
                materials[i] = Material.HornCoral;
            materials[12671] = Material.HornCoralBlock;
            for (int i = 12710; i <= 12711; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 12784; i <= 12791; i++)
                materials[i] = Material.HornCoralWallFan;
            materials[5780] = Material.Ice;
            materials[6549] = Material.InfestedChiseledStoneBricks;
            materials[6545] = Material.InfestedCobblestone;
            materials[6548] = Material.InfestedCrackedStoneBricks;
            for (int i = 24099; i <= 24101; i++)
                materials[i] = Material.InfestedDeepslate;
            materials[6547] = Material.InfestedMossyStoneBricks;
            materials[6544] = Material.InfestedStone;
            materials[6546] = Material.InfestedStoneBricks;
            for (int i = 6742; i <= 6773; i++)
                materials[i] = Material.IronBars;
            materials[2092] = Material.IronBlock;
            for (int i = 5652; i <= 5715; i++)
                materials[i] = Material.IronDoor;
            materials[125] = Material.IronOre;
            for (int i = 10258; i <= 10321; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 5871; i <= 5874; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 19219; i <= 19230; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 5815; i <= 5816; i++)
                materials[i] = Material.Jukebox;
            for (int i = 8683; i <= 8706; i++)
                materials[i] = Material.JungleButton;
            for (int i = 11809; i <= 11872; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 11489; i <= 11520; i++)
                materials[i] = Material.JungleFence;
            for (int i = 11233; i <= 11264; i++)
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
            for (int i = 11039; i <= 11044; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 7826; i <= 7905; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 6154; i <= 6217; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 5578; i <= 5585; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 4802; i <= 4809; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 198; i <= 200; i++)
                materials[i] = Material.JungleWood;
            for (int i = 12619; i <= 12644; i++)
                materials[i] = Material.Kelp;
            materials[12645] = Material.KelpPlant;
            for (int i = 4654; i <= 4661; i++)
                materials[i] = Material.Ladder;
            for (int i = 18362; i <= 18365; i++)
                materials[i] = Material.Lantern;
            materials[522] = Material.LapisBlock;
            materials[520] = Material.LapisOre;
            for (int i = 20904; i <= 20915; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 10616; i <= 10617; i++)
                materials[i] = Material.LargeFern;
            for (int i = 96; i <= 111; i++)
                materials[i] = Material.Lava;
            materials[7402] = Material.LavaCauldron;
            for (int i = 18309; i <= 18324; i++)
                materials[i] = Material.Lectern;
            for (int i = 5626; i <= 5649; i++)
                materials[i] = Material.Lever;
            for (int i = 10226; i <= 10257; i++)
                materials[i] = Material.Light;
            for (int i = 10666; i <= 10681; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 1736; i <= 1751; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 20648; i <= 20663; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 20864; i <= 20865; i++)
                materials[i] = Material.LightBlueCandleCake;
            materials[10590] = Material.LightBlueCarpet;
            materials[12590] = Material.LightBlueConcrete;
            materials[12606] = Material.LightBlueConcretePowder;
            for (int i = 12535; i <= 12538; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 12445; i <= 12450; i++)
                materials[i] = Material.LightBlueShulkerBox;
            materials[5949] = Material.LightBlueStainedGlass;
            for (int i = 9328; i <= 9359; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            materials[9219] = Material.LightBlueTerracotta;
            for (int i = 10886; i <= 10889; i++)
                materials[i] = Material.LightBlueWallBanner;
            materials[2050] = Material.LightBlueWool;
            for (int i = 10746; i <= 10761; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 1816; i <= 1831; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 20728; i <= 20743; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 20874; i <= 20875; i++)
                materials[i] = Material.LightGrayCandleCake;
            materials[10595] = Material.LightGrayCarpet;
            materials[12595] = Material.LightGrayConcrete;
            materials[12611] = Material.LightGrayConcretePowder;
            for (int i = 12555; i <= 12558; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 12475; i <= 12480; i++)
                materials[i] = Material.LightGrayShulkerBox;
            materials[5954] = Material.LightGrayStainedGlass;
            for (int i = 9488; i <= 9519; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            materials[9224] = Material.LightGrayTerracotta;
            for (int i = 10906; i <= 10909; i++)
                materials[i] = Material.LightGrayWallBanner;
            materials[2055] = Material.LightGrayWool;
            for (int i = 9003; i <= 9018; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 22269; i <= 22292; i++)
                materials[i] = Material.LightningRod;
            for (int i = 10608; i <= 10609; i++)
                materials[i] = Material.Lilac;
            materials[2088] = Material.LilyOfTheValley;
            materials[7271] = Material.LilyPad;
            for (int i = 10698; i <= 10713; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 1768; i <= 1783; i++)
                materials[i] = Material.LimeBed;
            for (int i = 20680; i <= 20695; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 20868; i <= 20869; i++)
                materials[i] = Material.LimeCandleCake;
            materials[10592] = Material.LimeCarpet;
            materials[12592] = Material.LimeConcrete;
            materials[12608] = Material.LimeConcretePowder;
            for (int i = 12543; i <= 12546; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 12457; i <= 12462; i++)
                materials[i] = Material.LimeShulkerBox;
            materials[5951] = Material.LimeStainedGlass;
            for (int i = 9392; i <= 9423; i++)
                materials[i] = Material.LimeStainedGlassPane;
            materials[9221] = Material.LimeTerracotta;
            for (int i = 10894; i <= 10897; i++)
                materials[i] = Material.LimeWallBanner;
            materials[2052] = Material.LimeWool;
            materials[19318] = Material.Lodestone;
            for (int i = 18263; i <= 18266; i++)
                materials[i] = Material.Loom;
            for (int i = 10650; i <= 10665; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 1720; i <= 1735; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 20632; i <= 20647; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 20862; i <= 20863; i++)
                materials[i] = Material.MagentaCandleCake;
            materials[10589] = Material.MagentaCarpet;
            materials[12589] = Material.MagentaConcrete;
            materials[12605] = Material.MagentaConcretePowder;
            for (int i = 12531; i <= 12534; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 12439; i <= 12444; i++)
                materials[i] = Material.MagentaShulkerBox;
            materials[5948] = Material.MagentaStainedGlass;
            for (int i = 9296; i <= 9327; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            materials[9218] = Material.MagentaTerracotta;
            for (int i = 10882; i <= 10885; i++)
                materials[i] = Material.MagentaWallBanner;
            materials[2049] = Material.MagentaWool;
            materials[12402] = Material.MagmaBlock;
            for (int i = 8779; i <= 8802; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 12065; i <= 12128; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 11617; i <= 11648; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 11361; i <= 11392; i++)
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
            for (int i = 11063; i <= 11068; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 9984; i <= 10063; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 6410; i <= 6473; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 5594; i <= 5601; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 4818; i <= 4825; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 210; i <= 212; i++)
                materials[i] = Material.MangroveWood;
            for (int i = 20916; i <= 20927; i++)
                materials[i] = Material.MediumAmethystBud;
            materials[6812] = Material.Melon;
            for (int i = 6829; i <= 6836; i++)
                materials[i] = Material.MelonStem;
            materials[22388] = Material.MossBlock;
            materials[22371] = Material.MossCarpet;
            materials[2353] = Material.MossyCobblestone;
            for (int i = 13965; i <= 13970; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 13141; i <= 13220; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 8243; i <= 8566; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 13953; i <= 13958; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 12981; i <= 13060; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 14991; i <= 15314; i++)
                materials[i] = Material.MossyStoneBrickWall;
            materials[6539] = Material.MossyStoneBricks;
            for (int i = 2063; i <= 2074; i++)
                materials[i] = Material.MovingPiston;
            materials[22448] = Material.Mud;
            for (int i = 11129; i <= 11134; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 7189; i <= 7268; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 15963; i <= 16286; i++)
                materials[i] = Material.MudBrickWall;
            materials[6543] = Material.MudBricks;
            for (int i = 156; i <= 158; i++)
                materials[i] = Material.MuddyMangroveRoots;
            for (int i = 6678; i <= 6741; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 7269; i <= 7270; i++)
                materials[i] = Material.Mycelium;
            for (int i = 7273; i <= 7304; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 11135; i <= 11140; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 7305; i <= 7384; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 16287; i <= 16610; i++)
                materials[i] = Material.NetherBrickWall;
            materials[7272] = Material.NetherBricks;
            materials[129] = Material.NetherGoldOre;
            for (int i = 5865; i <= 5866; i++)
                materials[i] = Material.NetherPortal;
            materials[9084] = Material.NetherQuartzOre;
            materials[18454] = Material.NetherSprouts;
            for (int i = 7385; i <= 7388; i++)
                materials[i] = Material.NetherWart;
            materials[12403] = Material.NetherWartBlock;
            materials[19306] = Material.NetheriteBlock;
            materials[5850] = Material.Netherrack;
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
            for (int i = 11021; i <= 11026; i++)
                materials[i] = Material.OakSlab;
            for (int i = 2874; i <= 2953; i++)
                materials[i] = Material.OakStairs;
            for (int i = 5962; i <= 6025; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 5538; i <= 5545; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 4762; i <= 4769; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 189; i <= 191; i++)
                materials[i] = Material.OakWood;
            for (int i = 12409; i <= 12420; i++)
                materials[i] = Material.Observer;
            materials[2354] = Material.Obsidian;
            for (int i = 24108; i <= 24110; i++)
                materials[i] = Material.OchreFroglight;
            for (int i = 10634; i <= 10649; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 1704; i <= 1719; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 20616; i <= 20631; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 20860; i <= 20861; i++)
                materials[i] = Material.OrangeCandleCake;
            materials[10588] = Material.OrangeCarpet;
            materials[12588] = Material.OrangeConcrete;
            materials[12604] = Material.OrangeConcretePowder;
            for (int i = 12527; i <= 12530; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 12433; i <= 12438; i++)
                materials[i] = Material.OrangeShulkerBox;
            materials[5947] = Material.OrangeStainedGlass;
            for (int i = 9264; i <= 9295; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            materials[9217] = Material.OrangeTerracotta;
            materials[2082] = Material.OrangeTulip;
            for (int i = 10878; i <= 10881; i++)
                materials[i] = Material.OrangeWallBanner;
            materials[2048] = Material.OrangeWool;
            materials[2085] = Material.OxeyeDaisy;
            materials[21563] = Material.OxidizedCopper;
            materials[21569] = Material.OxidizedCutCopper;
            for (int i = 21893; i <= 21898; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 21573; i <= 21652; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            materials[10605] = Material.PackedIce;
            materials[6542] = Material.PackedMud;
            for (int i = 24114; i <= 24116; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 10612; i <= 10613; i++)
                materials[i] = Material.Peony;
            for (int i = 11105; i <= 11110; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 8947; i <= 8962; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 8963; i <= 8966; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 10714; i <= 10729; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 1784; i <= 1799; i++)
                materials[i] = Material.PinkBed;
            for (int i = 20696; i <= 20711; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 20870; i <= 20871; i++)
                materials[i] = Material.PinkCandleCake;
            materials[10593] = Material.PinkCarpet;
            materials[12593] = Material.PinkConcrete;
            materials[12609] = Material.PinkConcretePowder;
            for (int i = 12547; i <= 12550; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 22372; i <= 22387; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 12463; i <= 12468; i++)
                materials[i] = Material.PinkShulkerBox;
            materials[5952] = Material.PinkStainedGlass;
            for (int i = 9424; i <= 9455; i++)
                materials[i] = Material.PinkStainedGlassPane;
            materials[9222] = Material.PinkTerracotta;
            materials[2084] = Material.PinkTulip;
            for (int i = 10898; i <= 10901; i++)
                materials[i] = Material.PinkWallBanner;
            materials[2053] = Material.PinkWool;
            for (int i = 2011; i <= 2022; i++)
                materials[i] = Material.Piston;
            for (int i = 2023; i <= 2046; i++)
                materials[i] = Material.PistonHead;
            for (int i = 12356; i <= 12365; i++)
                materials[i] = Material.PitcherCrop;
            for (int i = 12366; i <= 12367; i++)
                materials[i] = Material.PitcherPlant;
            for (int i = 8887; i <= 8902; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 8903; i <= 8906; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            for (int i = 22293; i <= 22312; i++)
                materials[i] = Material.PointedDripstone;
            materials[7] = Material.PolishedAndesite;
            for (int i = 14007; i <= 14012; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 13781; i <= 13860; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 5856; i <= 5858; i++)
                materials[i] = Material.PolishedBasalt;
            materials[19730] = Material.PolishedBlackstone;
            for (int i = 19734; i <= 19739; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 19740; i <= 19819; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 19820; i <= 20143; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            materials[19731] = Material.PolishedBlackstoneBricks;
            for (int i = 20233; i <= 20256; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 20231; i <= 20232; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 20225; i <= 20230; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 20145; i <= 20224; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 20257; i <= 20580; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            materials[22863] = Material.PolishedDeepslate;
            for (int i = 22944; i <= 22949; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 22864; i <= 22943; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 22950; i <= 23273; i++)
                materials[i] = Material.PolishedDeepslateWall;
            materials[5] = Material.PolishedDiorite;
            for (int i = 13959; i <= 13964; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 13061; i <= 13140; i++)
                materials[i] = Material.PolishedDioriteStairs;
            materials[3] = Material.PolishedGranite;
            for (int i = 13941; i <= 13946; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 12821; i <= 12900; i++)
                materials[i] = Material.PolishedGraniteStairs;
            materials[2077] = Material.Poppy;
            for (int i = 8603; i <= 8610; i++)
                materials[i] = Material.Potatoes;
            materials[8573] = Material.PottedAcaciaSapling;
            materials[8581] = Material.PottedAllium;
            materials[24106] = Material.PottedAzaleaBush;
            materials[8582] = Material.PottedAzureBluet;
            materials[12816] = Material.PottedBamboo;
            materials[8571] = Material.PottedBirchSapling;
            materials[8580] = Material.PottedBlueOrchid;
            materials[8592] = Material.PottedBrownMushroom;
            materials[8594] = Material.PottedCactus;
            materials[8574] = Material.PottedCherrySapling;
            materials[8588] = Material.PottedCornflower;
            materials[19314] = Material.PottedCrimsonFungus;
            materials[19316] = Material.PottedCrimsonRoots;
            materials[8578] = Material.PottedDandelion;
            materials[8575] = Material.PottedDarkOakSapling;
            materials[8593] = Material.PottedDeadBush;
            materials[8577] = Material.PottedFern;
            materials[24107] = Material.PottedFloweringAzaleaBush;
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
            materials[19315] = Material.PottedWarpedFungus;
            materials[19317] = Material.PottedWarpedRoots;
            materials[8585] = Material.PottedWhiteTulip;
            materials[8590] = Material.PottedWitherRose;
            materials[20943] = Material.PowderSnow;
            for (int i = 7403; i <= 7405; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 1944; i <= 1967; i++)
                materials[i] = Material.PoweredRail;
            materials[10322] = Material.Prismarine;
            for (int i = 10571; i <= 10576; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 10405; i <= 10484; i++)
                materials[i] = Material.PrismarineBrickStairs;
            materials[10323] = Material.PrismarineBricks;
            for (int i = 10565; i <= 10570; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 10325; i <= 10404; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 14343; i <= 14666; i++)
                materials[i] = Material.PrismarineWall;
            materials[5849] = Material.Pumpkin;
            for (int i = 6821; i <= 6828; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 10778; i <= 10793; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 1848; i <= 1863; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 20760; i <= 20775; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 20878; i <= 20879; i++)
                materials[i] = Material.PurpleCandleCake;
            materials[10597] = Material.PurpleCarpet;
            materials[12597] = Material.PurpleConcrete;
            materials[12613] = Material.PurpleConcretePowder;
            for (int i = 12563; i <= 12566; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 12487; i <= 12492; i++)
                materials[i] = Material.PurpleShulkerBox;
            materials[5956] = Material.PurpleStainedGlass;
            for (int i = 9552; i <= 9583; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            materials[9226] = Material.PurpleTerracotta;
            for (int i = 10914; i <= 10917; i++)
                materials[i] = Material.PurpleWallBanner;
            materials[2057] = Material.PurpleWool;
            materials[12269] = Material.PurpurBlock;
            for (int i = 12270; i <= 12272; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 11159; i <= 11164; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 12273; i <= 12352; i++)
                materials[i] = Material.PurpurStairs;
            materials[9095] = Material.QuartzBlock;
            materials[20583] = Material.QuartzBricks;
            for (int i = 9097; i <= 9099; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 11141; i <= 11146; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 9100; i <= 9179; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 4662; i <= 4681; i++)
                materials[i] = Material.Rail;
            materials[24104] = Material.RawCopperBlock;
            materials[24105] = Material.RawGoldBlock;
            materials[24103] = Material.RawIronBlock;
            for (int i = 10842; i <= 10857; i++)
                materials[i] = Material.RedBanner;
            for (int i = 1912; i <= 1927; i++)
                materials[i] = Material.RedBed;
            for (int i = 20824; i <= 20839; i++)
                materials[i] = Material.RedCandle;
            for (int i = 20886; i <= 20887; i++)
                materials[i] = Material.RedCandleCake;
            materials[10601] = Material.RedCarpet;
            materials[12601] = Material.RedConcrete;
            materials[12617] = Material.RedConcretePowder;
            for (int i = 12579; i <= 12582; i++)
                materials[i] = Material.RedGlazedTerracotta;
            materials[2090] = Material.RedMushroom;
            for (int i = 6614; i <= 6677; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 14001; i <= 14006; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 13701; i <= 13780; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 16935; i <= 17258; i++)
                materials[i] = Material.RedNetherBrickWall;
            materials[12404] = Material.RedNetherBricks;
            materials[117] = Material.RedSand;
            materials[10938] = Material.RedSandstone;
            for (int i = 11147; i <= 11152; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 10941; i <= 11020; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 14667; i <= 14990; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 12511; i <= 12516; i++)
                materials[i] = Material.RedShulkerBox;
            materials[5960] = Material.RedStainedGlass;
            for (int i = 9680; i <= 9711; i++)
                materials[i] = Material.RedStainedGlassPane;
            materials[9230] = Material.RedTerracotta;
            materials[2081] = Material.RedTulip;
            for (int i = 10930; i <= 10933; i++)
                materials[i] = Material.RedWallBanner;
            materials[2061] = Material.RedWool;
            materials[9083] = Material.RedstoneBlock;
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
            materials[24118] = Material.ReinforcedDeepslate;
            for (int i = 5882; i <= 5945; i++)
                materials[i] = Material.Repeater;
            for (int i = 12374; i <= 12385; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 19309; i <= 19313; i++)
                materials[i] = Material.RespawnAnchor;
            materials[22447] = Material.RootedDirt;
            for (int i = 10610; i <= 10611; i++)
                materials[i] = Material.RoseBush;
            materials[112] = Material.Sand;
            materials[535] = Material.Sandstone;
            for (int i = 11093; i <= 11098; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 7431; i <= 7510; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 17259; i <= 17582; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 18231; i <= 18262; i++)
                materials[i] = Material.Scaffolding;
            materials[21424] = Material.Sculk;
            for (int i = 21553; i <= 21554; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 20944; i <= 21039; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 21555; i <= 21562; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 21425; i <= 21552; i++)
                materials[i] = Material.SculkVein;
            materials[10583] = Material.SeaLantern;
            for (int i = 12792; i <= 12799; i++)
                materials[i] = Material.SeaPickle;
            materials[2008] = Material.Seagrass;
            materials[18469] = Material.Shroomlight;
            for (int i = 12421; i <= 12426; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 8827; i <= 8842; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 8843; i <= 8846; i++)
                materials[i] = Material.SkeletonWallSkull;
            materials[10224] = Material.SlimeBlock;
            for (int i = 20928; i <= 20939; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 22429; i <= 22444; i++)
                materials[i] = Material.SmallDripleaf;
            materials[18325] = Material.SmithingTable;
            for (int i = 18279; i <= 18286; i++)
                materials[i] = Material.Smoker;
            materials[24102] = Material.SmoothBasalt;
            materials[11167] = Material.SmoothQuartz;
            for (int i = 13983; i <= 13988; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 13461; i <= 13540; i++)
                materials[i] = Material.SmoothQuartzStairs;
            materials[11168] = Material.SmoothRedSandstone;
            for (int i = 13947; i <= 13952; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 12901; i <= 12980; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            materials[11166] = Material.SmoothSandstone;
            for (int i = 13977; i <= 13982; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 13381; i <= 13460; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            materials[11165] = Material.SmoothStone;
            for (int i = 11087; i <= 11092; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 12659; i <= 12661; i++)
                materials[i] = Material.SnifferEgg;
            for (int i = 5772; i <= 5779; i++)
                materials[i] = Material.Snow;
            materials[5781] = Material.SnowBlock;
            for (int i = 18402; i <= 18433; i++)
                materials[i] = Material.SoulCampfire;
            materials[2872] = Material.SoulFire;
            for (int i = 18366; i <= 18369; i++)
                materials[i] = Material.SoulLantern;
            materials[5851] = Material.SoulSand;
            materials[5852] = Material.SoulSoil;
            materials[5859] = Material.SoulTorch;
            for (int i = 5860; i <= 5863; i++)
                materials[i] = Material.SoulWallTorch;
            materials[2873] = Material.Spawner;
            materials[517] = Material.Sponge;
            materials[22368] = Material.SporeBlossom;
            for (int i = 8635; i <= 8658; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 11681; i <= 11744; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 11425; i <= 11456; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 11169; i <= 11200; i++)
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
            for (int i = 11027; i <= 11032; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 7666; i <= 7745; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 6026; i <= 6089; i++)
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
            for (int i = 11123; i <= 11128; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 7109; i <= 7188; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 15639; i <= 15962; i++)
                materials[i] = Material.StoneBrickWall;
            materials[6538] = Material.StoneBricks;
            for (int i = 5748; i <= 5771; i++)
                materials[i] = Material.StoneButton;
            for (int i = 5650; i <= 5651; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 11081; i <= 11086; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 13301; i <= 13380; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 18326; i <= 18329; i++)
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
            for (int i = 18464; i <= 18466; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 18458; i <= 18460; i++)
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
            for (int i = 18447; i <= 18449; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 18441; i <= 18443; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 19215; i <= 19218; i++)
                materials[i] = Material.StructureBlock;
            materials[12408] = Material.StructureVoid;
            for (int i = 5799; i <= 5814; i++)
                materials[i] = Material.SugarCane;
            for (int i = 10606; i <= 10607; i++)
                materials[i] = Material.Sunflower;
            for (int i = 119; i <= 122; i++)
                materials[i] = Material.SuspiciousGravel;
            for (int i = 113; i <= 116; i++)
                materials[i] = Material.SuspiciousSand;
            for (int i = 18434; i <= 18437; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 10614; i <= 10615; i++)
                materials[i] = Material.TallGrass;
            for (int i = 2009; i <= 2010; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 19240; i <= 19255; i++)
                materials[i] = Material.Target;
            materials[10603] = Material.Terracotta;
            materials[20942] = Material.TintedGlass;
            for (int i = 2094; i <= 2095; i++)
                materials[i] = Material.Tnt;
            materials[2355] = Material.Torch;
            materials[2076] = Material.Torchflower;
            for (int i = 12354; i <= 12355; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 8979; i <= 9002; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 7537; i <= 7664; i++)
                materials[i] = Material.Tripwire;
            for (int i = 7521; i <= 7536; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 12682; i <= 12683; i++)
                materials[i] = Material.TubeCoral;
            materials[12667] = Material.TubeCoralBlock;
            for (int i = 12702; i <= 12703; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 12752; i <= 12759; i++)
                materials[i] = Material.TubeCoralWallFan;
            materials[20940] = Material.Tuff;
            for (int i = 12647; i <= 12658; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 18497; i <= 18522; i++)
                materials[i] = Material.TwistingVines;
            materials[18523] = Material.TwistingVinesPlant;
            for (int i = 24111; i <= 24113; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 6837; i <= 6868; i++)
                materials[i] = Material.Vine;
            materials[12817] = Material.VoidAir;
            for (int i = 2356; i <= 2359; i++)
                materials[i] = Material.WallTorch;
            for (int i = 18983; i <= 19006; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 19071; i <= 19134; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 18575; i <= 18606; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 18767; i <= 18798; i++)
                materials[i] = Material.WarpedFenceGate;
            materials[18451] = Material.WarpedFungus;
            for (int i = 5346; i <= 5409; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 18444; i <= 18446; i++)
                materials[i] = Material.WarpedHyphae;
            materials[18450] = Material.WarpedNylium;
            materials[18526] = Material.WarpedPlanks;
            for (int i = 18541; i <= 18542; i++)
                materials[i] = Material.WarpedPressurePlate;
            materials[18453] = Material.WarpedRoots;
            for (int i = 19167; i <= 19198; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 18533; i <= 18538; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 18879; i <= 18958; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 18438; i <= 18440; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 18671; i <= 18734; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 5610; i <= 5617; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 19207; i <= 19214; i++)
                materials[i] = Material.WarpedWallSign;
            materials[18452] = Material.WarpedWartBlock;
            for (int i = 80; i <= 95; i++)
                materials[i] = Material.Water;
            for (int i = 7399; i <= 7401; i++)
                materials[i] = Material.WaterCauldron;
            materials[21917] = Material.WaxedCopperBlock;
            materials[21924] = Material.WaxedCutCopper;
            for (int i = 22263; i <= 22268; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 22165; i <= 22244; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            materials[21919] = Material.WaxedExposedCopper;
            materials[21923] = Material.WaxedExposedCutCopper;
            for (int i = 22257; i <= 22262; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 22085; i <= 22164; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            materials[21920] = Material.WaxedOxidizedCopper;
            materials[21921] = Material.WaxedOxidizedCutCopper;
            for (int i = 22245; i <= 22250; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 21925; i <= 22004; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            materials[21918] = Material.WaxedWeatheredCopper;
            materials[21922] = Material.WaxedWeatheredCutCopper;
            for (int i = 22251; i <= 22256; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 22005; i <= 22084; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            materials[21564] = Material.WeatheredCopper;
            materials[21570] = Material.WeatheredCutCopper;
            for (int i = 21899; i <= 21904; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 21653; i <= 21732; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 18470; i <= 18495; i++)
                materials[i] = Material.WeepingVines;
            materials[18496] = Material.WeepingVinesPlant;
            materials[518] = Material.WetSponge;
            for (int i = 4278; i <= 4285; i++)
                materials[i] = Material.Wheat;
            for (int i = 10618; i <= 10633; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 1688; i <= 1703; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 20600; i <= 20615; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 20858; i <= 20859; i++)
                materials[i] = Material.WhiteCandleCake;
            materials[10587] = Material.WhiteCarpet;
            materials[12587] = Material.WhiteConcrete;
            materials[12603] = Material.WhiteConcretePowder;
            for (int i = 12523; i <= 12526; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 12427; i <= 12432; i++)
                materials[i] = Material.WhiteShulkerBox;
            materials[5946] = Material.WhiteStainedGlass;
            for (int i = 9232; i <= 9263; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            materials[9216] = Material.WhiteTerracotta;
            materials[2083] = Material.WhiteTulip;
            for (int i = 10874; i <= 10877; i++)
                materials[i] = Material.WhiteWallBanner;
            materials[2047] = Material.WhiteWool;
            materials[2087] = Material.WitherRose;
            for (int i = 8847; i <= 8862; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 8863; i <= 8866; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 10682; i <= 10697; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 1752; i <= 1767; i++)
                materials[i] = Material.YellowBed;
            for (int i = 20664; i <= 20679; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 20866; i <= 20867; i++)
                materials[i] = Material.YellowCandleCake;
            materials[10591] = Material.YellowCarpet;
            materials[12591] = Material.YellowConcrete;
            materials[12607] = Material.YellowConcretePowder;
            for (int i = 12539; i <= 12542; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 12451; i <= 12456; i++)
                materials[i] = Material.YellowShulkerBox;
            materials[5950] = Material.YellowStainedGlass;
            for (int i = 9360; i <= 9391; i++)
                materials[i] = Material.YellowStainedGlassPane;
            materials[9220] = Material.YellowTerracotta;
            for (int i = 10890; i <= 10893; i++)
                materials[i] = Material.YellowWallBanner;
            materials[2051] = Material.YellowWool;
            for (int i = 8867; i <= 8882; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 8883; i <= 8886; i++)
                materials[i] = Material.ZombieWallHead;
        }

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }
    }
}
