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
}
