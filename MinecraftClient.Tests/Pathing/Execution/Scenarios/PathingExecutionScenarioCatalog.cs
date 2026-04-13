using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Goals;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class PathingExecutionScenarioCatalog
{
    internal static PathingExecutionScenario Get(string scenarioId) => scenarioId switch
    {
        "manager-accepted-ascend-chain" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildManagerAcceptedAscendChain,
            Start = new Location(171.5, 80, 160.5),
            Goal = new GoalBlock(177, 83, 162),
            StartYaw = 315f,
            MaxExecutionTicks = 420
        },
        "same-move-ascend-staircase" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildSameMoveAscendStaircase,
            Start = new Location(340.5, 80, 340.5),
            Goal = new GoalBlock(345, 85, 340),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "same-move-descend-staircase" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildSameMoveDescendStaircase,
            Start = new Location(362.5, 85, 360.5),
            Goal = new GoalBlock(367, 80, 360),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "rejected-3x1-invalid-goal" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildRejectedThreeByOneInvalidGoal,
            Start = new Location(141.5, 80, 138.5),
            Goal = new GoalBlock(144, 81, 138),
            StartYaw = 270f,
            MaxExecutionTicks = 80
        },
        "repeated-cardinal-parkour-chain" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildRepeatedCardinalParkourChain,
            Start = new Location(580.5, 80, 580.5),
            Goal = new GoalBlock(588, 80, 580),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "repeated-diagonal-parkour-chain" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildRepeatedDiagonalParkourChain,
            Start = new Location(600.5, 80, 600.5),
            Goal = new GoalBlock(606, 80, 606),
            StartYaw = 315f,
            MaxExecutionTicks = 420
        },
        "obstructed-parkour-l-turns" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildObstructedParkourLTurns,
            Start = new Location(620.5, 80, 620.5),
            Goal = new GoalBlock(626, 80, 622),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "vertical-jump-mix" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildVerticalJumpMix,
            Start = new Location(640.5, 80, 620.5),
            Goal = new GoalBlock(648, 80, 620),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "diagonal-vertical-mix" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildDiagonalVerticalMix,
            Start = new Location(680.5, 80, 620.5),
            Goal = new GoalBlock(684, 80, 624),
            StartYaw = 315f,
            MaxExecutionTicks = 420
        },
        "same-move-straight-traverse-chain" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildSameMoveStraightTraverseChain,
            Start = new Location(300.5, 80, 300.5),
            Goal = new GoalBlock(312, 80, 300),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "same-move-diagonal-chain" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildSameMoveDiagonalChain,
            Start = new Location(320.5, 80, 320.5),
            Goal = new GoalBlock(327, 80, 327),
            StartYaw = 315f,
            MaxExecutionTicks = 420
        },
        "same-move-aligned-parkour-chain" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildSameMoveAlignedParkourChain,
            Start = new Location(380.5, 80, 380.5),
            Goal = new GoalBlock(388, 80, 380),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "mixed-traverse-turn-parkour-turn-traverse" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildMixedTraverseTurnParkourTurnTraverse,
            Start = new Location(400.5, 80, 400.5),
            Goal = new GoalBlock(408, 80, 404),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "mixed-diagonal-ascend-traverse-descend" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildMixedDiagonalAscendTraverseDescend,
            Start = new Location(420.5, 80, 420.5),
            Goal = new GoalBlock(428, 80, 422),
            StartYaw = 315f,
            MaxExecutionTicks = 420
        },
        "mixed-traverse-ascend-parkour-descend" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildMixedTraverseAscendParkourDescend,
            Start = new Location(440.5, 80, 440.5),
            Goal = new GoalBlock(448, 80, 440),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "turn-density-alternating-traverse-diagonal-chain" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildTurnDensityAlternatingTraverseDiagonalChain,
            Start = new Location(460.5, 80, 460.5),
            Goal = new GoalBlock(466, 80, 466),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "speed-carry-repeated-traverse-ascend" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildSpeedCarryRepeatedTraverseAscend,
            Start = new Location(480.5, 80, 480.5),
            Goal = new GoalBlock(488, 84, 480),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "speed-carry-repeated-traverse-descend" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildSpeedCarryRepeatedTraverseDescend,
            Start = new Location(500.5, 83, 500.5),
            Goal = new GoalBlock(507, 80, 500),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        "speed-carry-repeated-traverse-parkour" => new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = BuildSpeedCarryRepeatedTraverseParkour,
            Start = new Location(520.5, 80, 520.5),
            Goal = new GoalBlock(529, 80, 520),
            StartYaw = 270f,
            MaxExecutionTicks = 420
        },
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, null)
    };

    private static World BuildManagerAcceptedAscendChain()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 158, max: 180);
        FlatWorldTestBuilder.ClearBox(world, 170, 80, 160, 178, 85, 168);
        FlatWorldTestBuilder.SetSolid(world, 175, 80, 162);
        FlatWorldTestBuilder.SetSolid(world, 176, 81, 162);
        FlatWorldTestBuilder.SetSolid(world, 177, 82, 162);
        return world;
    }

    private static World BuildSameMoveAscendStaircase()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 338, max: 347);
        FlatWorldTestBuilder.ClearBox(world, 340, 80, 338, 347, 86, 342);
        FlatWorldTestBuilder.FillSolid(world, 341, 80, 339, 341, 80, 341);
        FlatWorldTestBuilder.FillSolid(world, 342, 81, 339, 342, 81, 341);
        FlatWorldTestBuilder.FillSolid(world, 343, 82, 339, 343, 82, 341);
        FlatWorldTestBuilder.FillSolid(world, 344, 83, 339, 344, 83, 341);
        FlatWorldTestBuilder.FillSolid(world, 345, 84, 339, 345, 84, 341);
        return world;
    }

    private static World BuildSameMoveDescendStaircase()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 358, max: 369);
        FlatWorldTestBuilder.ClearBox(world, 360, 79, 358, 369, 85, 362);
        FlatWorldTestBuilder.FillSolid(world, 362, 84, 359, 362, 84, 361);
        FlatWorldTestBuilder.FillSolid(world, 363, 83, 359, 363, 83, 361);
        FlatWorldTestBuilder.FillSolid(world, 364, 82, 359, 364, 82, 361);
        FlatWorldTestBuilder.FillSolid(world, 365, 81, 359, 365, 81, 361);
        FlatWorldTestBuilder.FillSolid(world, 366, 80, 359, 366, 80, 361);
        FlatWorldTestBuilder.FillSolid(world, 367, 79, 359, 367, 79, 361);
        return world;
    }

    private static World BuildRejectedThreeByOneInvalidGoal()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 135, max: 148);
        FlatWorldTestBuilder.ClearBox(world, 140, 80, 135, 148, 85, 140);
        FlatWorldTestBuilder.SetSolid(world, 143, 80, 138);
        return world;
    }

    private static World BuildRepeatedCardinalParkourChain()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 578, max: 590);
        FlatWorldTestBuilder.ClearBox(world, 578, 79, 578, 590, 90, 582);
        FlatWorldTestBuilder.SetSolid(world, 580, 79, 580);
        FlatWorldTestBuilder.SetSolid(world, 582, 79, 580);
        FlatWorldTestBuilder.SetSolid(world, 584, 79, 580);
        FlatWorldTestBuilder.SetSolid(world, 586, 79, 580);
        FlatWorldTestBuilder.SetSolid(world, 588, 79, 580);
        return world;
    }

    private static World BuildRepeatedDiagonalParkourChain()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 598, max: 608);
        FlatWorldTestBuilder.ClearBox(world, 598, 79, 598, 608, 90, 608);
        FlatWorldTestBuilder.SetSolid(world, 600, 79, 600);
        FlatWorldTestBuilder.SetSolid(world, 602, 79, 602);
        FlatWorldTestBuilder.SetSolid(world, 604, 79, 604);
        FlatWorldTestBuilder.SetSolid(world, 606, 79, 606);
        return world;
    }

    private static World BuildObstructedParkourLTurns()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 618, max: 628);
        FlatWorldTestBuilder.ClearBox(world, 618, 79, 618, 628, 90, 624);
        FlatWorldTestBuilder.SetSolid(world, 620, 79, 620);
        FlatWorldTestBuilder.SetSolid(world, 622, 79, 620);
        FlatWorldTestBuilder.SetSolid(world, 622, 79, 621);
        FlatWorldTestBuilder.SetSolid(world, 624, 79, 621);
        FlatWorldTestBuilder.SetSolid(world, 624, 79, 622);
        FlatWorldTestBuilder.SetSolid(world, 626, 79, 622);
        FlatWorldTestBuilder.SetSolid(world, 620, 80, 621);
        FlatWorldTestBuilder.SetSolid(world, 620, 81, 621);
        FlatWorldTestBuilder.SetSolid(world, 622, 80, 622);
        FlatWorldTestBuilder.SetSolid(world, 622, 81, 622);
        return world;
    }

    private static World BuildVerticalJumpMix()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 618, max: 650);
        FlatWorldTestBuilder.ClearBox(world, 638, 79, 618, 650, 80, 622);
        FlatWorldTestBuilder.ClearBox(world, 638, 81, 618, 650, 92, 622);
        FlatWorldTestBuilder.SetSolid(world, 640, 79, 620);
        FlatWorldTestBuilder.SetSolid(world, 642, 80, 620);
        FlatWorldTestBuilder.SetSolid(world, 644, 79, 620);
        FlatWorldTestBuilder.SetSolid(world, 646, 80, 620);
        FlatWorldTestBuilder.SetSolid(world, 648, 79, 620);
        return world;
    }

    private static World BuildDiagonalVerticalMix()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 618, max: 686);
        FlatWorldTestBuilder.ClearBox(world, 678, 79, 618, 686, 80, 626);
        FlatWorldTestBuilder.ClearBox(world, 678, 81, 618, 686, 92, 626);
        FlatWorldTestBuilder.SetSolid(world, 680, 79, 620);
        FlatWorldTestBuilder.SetSolid(world, 681, 80, 621);
        FlatWorldTestBuilder.SetSolid(world, 682, 79, 622);
        FlatWorldTestBuilder.SetSolid(world, 683, 80, 623);
        FlatWorldTestBuilder.SetSolid(world, 684, 79, 624);
        return world;
    }

    private static World BuildSameMoveStraightTraverseChain()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 298, max: 314);
        FlatWorldTestBuilder.ClearBox(world, 298, 79, 298, 314, 90, 302);
        FlatWorldTestBuilder.FillSolid(world, 300, 79, 300, 312, 79, 300);
        return world;
    }

    private static World BuildSameMoveDiagonalChain()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 318, max: 330);
        FlatWorldTestBuilder.ClearBox(world, 318, 79, 318, 330, 90, 330);
        FlatWorldTestBuilder.SetSolid(world, 320, 79, 320);
        FlatWorldTestBuilder.SetSolid(world, 321, 79, 321);
        FlatWorldTestBuilder.SetSolid(world, 322, 79, 322);
        FlatWorldTestBuilder.SetSolid(world, 323, 79, 323);
        FlatWorldTestBuilder.SetSolid(world, 324, 79, 324);
        FlatWorldTestBuilder.SetSolid(world, 325, 79, 325);
        FlatWorldTestBuilder.SetSolid(world, 326, 79, 326);
        FlatWorldTestBuilder.SetSolid(world, 327, 79, 327);
        return world;
    }

    private static World BuildSameMoveAlignedParkourChain()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 378, max: 390);
        FlatWorldTestBuilder.ClearBox(world, 378, 79, 378, 390, 90, 382);
        FlatWorldTestBuilder.SetSolid(world, 380, 79, 380);
        FlatWorldTestBuilder.SetSolid(world, 382, 79, 380);
        FlatWorldTestBuilder.SetSolid(world, 384, 79, 380);
        FlatWorldTestBuilder.SetSolid(world, 386, 79, 380);
        FlatWorldTestBuilder.SetSolid(world, 388, 79, 380);
        return world;
    }

    private static World BuildMixedTraverseTurnParkourTurnTraverse()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 398, max: 410);
        FlatWorldTestBuilder.ClearBox(world, 398, 79, 398, 410, 90, 406);
        FlatWorldTestBuilder.SetSolid(world, 400, 79, 400);
        FlatWorldTestBuilder.SetSolid(world, 401, 79, 400);
        FlatWorldTestBuilder.SetSolid(world, 402, 79, 400);
        FlatWorldTestBuilder.SetSolid(world, 402, 79, 401);
        FlatWorldTestBuilder.SetSolid(world, 402, 79, 402);
        FlatWorldTestBuilder.SetSolid(world, 404, 79, 402);
        FlatWorldTestBuilder.SetSolid(world, 405, 79, 402);
        FlatWorldTestBuilder.SetSolid(world, 406, 79, 402);
        FlatWorldTestBuilder.SetSolid(world, 406, 79, 403);
        FlatWorldTestBuilder.SetSolid(world, 406, 79, 404);
        FlatWorldTestBuilder.SetSolid(world, 407, 79, 404);
        FlatWorldTestBuilder.SetSolid(world, 408, 79, 404);
        return world;
    }

    private static World BuildMixedDiagonalAscendTraverseDescend()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 418, max: 430);
        FlatWorldTestBuilder.ClearBox(world, 418, 79, 418, 430, 92, 424);
        FlatWorldTestBuilder.SetSolid(world, 420, 79, 420);
        FlatWorldTestBuilder.SetSolid(world, 421, 79, 421);
        FlatWorldTestBuilder.SetSolid(world, 422, 79, 422);
        FlatWorldTestBuilder.SetSolid(world, 423, 80, 422);
        FlatWorldTestBuilder.SetSolid(world, 424, 81, 422);
        FlatWorldTestBuilder.SetSolid(world, 425, 81, 422);
        FlatWorldTestBuilder.SetSolid(world, 426, 81, 422);
        FlatWorldTestBuilder.SetSolid(world, 427, 80, 422);
        FlatWorldTestBuilder.SetSolid(world, 428, 79, 422);
        return world;
    }

    private static World BuildMixedTraverseAscendParkourDescend()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 438, max: 450);
        FlatWorldTestBuilder.ClearBox(world, 438, 79, 438, 450, 92, 442);
        FlatWorldTestBuilder.SetSolid(world, 440, 79, 440);
        FlatWorldTestBuilder.SetSolid(world, 441, 79, 440);
        FlatWorldTestBuilder.SetSolid(world, 442, 80, 440);
        FlatWorldTestBuilder.SetSolid(world, 443, 81, 440);
        FlatWorldTestBuilder.SetSolid(world, 444, 81, 440);
        FlatWorldTestBuilder.SetSolid(world, 446, 81, 440);
        FlatWorldTestBuilder.SetSolid(world, 447, 80, 440);
        FlatWorldTestBuilder.SetSolid(world, 448, 79, 440);
        return world;
    }

    private static World BuildTurnDensityAlternatingTraverseDiagonalChain()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 458, max: 468);
        FlatWorldTestBuilder.ClearBox(world, 458, 79, 458, 468, 90, 468);
        FlatWorldTestBuilder.SetSolid(world, 460, 79, 460);
        FlatWorldTestBuilder.SetSolid(world, 461, 79, 460);
        FlatWorldTestBuilder.SetSolid(world, 461, 79, 461);
        FlatWorldTestBuilder.SetSolid(world, 462, 79, 462);
        FlatWorldTestBuilder.SetSolid(world, 463, 79, 462);
        FlatWorldTestBuilder.SetSolid(world, 463, 79, 463);
        FlatWorldTestBuilder.SetSolid(world, 464, 79, 464);
        FlatWorldTestBuilder.SetSolid(world, 465, 79, 464);
        FlatWorldTestBuilder.SetSolid(world, 465, 79, 465);
        FlatWorldTestBuilder.SetSolid(world, 466, 79, 466);
        return world;
    }

    private static World BuildSpeedCarryRepeatedTraverseAscend()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 478, max: 490);
        FlatWorldTestBuilder.ClearBox(world, 478, 79, 478, 490, 94, 482);
        FlatWorldTestBuilder.SetSolid(world, 480, 79, 480);
        FlatWorldTestBuilder.SetSolid(world, 481, 79, 480);
        FlatWorldTestBuilder.SetSolid(world, 482, 80, 480);
        FlatWorldTestBuilder.SetSolid(world, 483, 80, 480);
        FlatWorldTestBuilder.SetSolid(world, 484, 81, 480);
        FlatWorldTestBuilder.SetSolid(world, 485, 81, 480);
        FlatWorldTestBuilder.SetSolid(world, 486, 82, 480);
        FlatWorldTestBuilder.SetSolid(world, 487, 82, 480);
        FlatWorldTestBuilder.SetSolid(world, 488, 83, 480);
        return world;
    }

    private static World BuildSpeedCarryRepeatedTraverseDescend()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 498, max: 510);
        FlatWorldTestBuilder.ClearBox(world, 498, 79, 498, 510, 94, 502);
        FlatWorldTestBuilder.SetSolid(world, 500, 82, 500);
        FlatWorldTestBuilder.SetSolid(world, 501, 82, 500);
        FlatWorldTestBuilder.SetSolid(world, 502, 81, 500);
        FlatWorldTestBuilder.SetSolid(world, 503, 81, 500);
        FlatWorldTestBuilder.SetSolid(world, 504, 80, 500);
        FlatWorldTestBuilder.SetSolid(world, 505, 80, 500);
        FlatWorldTestBuilder.SetSolid(world, 506, 79, 500);
        FlatWorldTestBuilder.SetSolid(world, 507, 79, 500);
        return world;
    }

    private static World BuildSpeedCarryRepeatedTraverseParkour()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 518, max: 532);
        FlatWorldTestBuilder.ClearBox(world, 518, 79, 518, 532, 90, 522);
        FlatWorldTestBuilder.SetSolid(world, 520, 79, 520);
        FlatWorldTestBuilder.SetSolid(world, 521, 79, 520);
        FlatWorldTestBuilder.SetSolid(world, 523, 79, 520);
        FlatWorldTestBuilder.SetSolid(world, 524, 79, 520);
        FlatWorldTestBuilder.SetSolid(world, 526, 79, 520);
        FlatWorldTestBuilder.SetSolid(world, 527, 79, 520);
        FlatWorldTestBuilder.SetSolid(world, 529, 79, 520);
        return world;
    }
}
