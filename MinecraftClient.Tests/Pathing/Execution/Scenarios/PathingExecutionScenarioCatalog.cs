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
}
