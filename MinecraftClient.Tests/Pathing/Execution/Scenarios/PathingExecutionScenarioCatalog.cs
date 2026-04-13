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
}
