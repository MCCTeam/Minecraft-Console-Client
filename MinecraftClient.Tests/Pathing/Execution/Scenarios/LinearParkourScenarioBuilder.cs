using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Goals;

namespace MinecraftClient.Tests.Pathing.Execution;

public static class LinearParkourScenarioBuilder
{
    private const int SegmentCount = 3;
    private const int BaseY = 80;
    private const int FloorY = BaseY - 1;

    public static IEnumerable<object[]> AcceptedCases()
    {
        yield return ["linear-ascend-gap1-dy+1", 1, 1];
        yield return ["linear-ascend-gap2-dy+1", 2, 1];
        yield return ["linear-descend-gap2-dy-2", 2, -2];
        yield return ["linear-descend-gap3-dy-1", 3, -1];
        yield return ["linear-descend-gap4-dy-1", 4, -1];
        yield return ["linear-flat-gap1", 1, 0];
        yield return ["linear-flat-gap4", 4, 0];
    }

    internal static PathingExecutionScenario Create(string scenarioId, int gap, int deltaY, int maxExecutionTicks = 600)
    {
        int endX = 3 + ((gap + 1) * SegmentCount);
        int endFloorY = FloorY + (deltaY * SegmentCount);

        return new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = () => BuildWorld(gap, deltaY),
            Start = new Location(0.5, BaseY, 0.5),
            Goal = new GoalBlock(endX, endFloorY + 1, 0),
            StartYaw = 270f,
            MaxExecutionTicks = maxExecutionTicks,
        };
    }

    internal static World BuildWorld(int gap, int deltaY)
    {
        int endX = 3 + ((gap + 1) * SegmentCount);
        World world = FlatWorldTestBuilder.CreateStoneFloor(floorY: 0, min: -8, max: endX + 8);
        FlatWorldTestBuilder.ClearBox(world, -8, 1, -2, endX + 8, BaseY + 12, 2);
        FlatWorldTestBuilder.FillSolid(world, 0, FloorY, 0, 3, FloorY, 0);

        int lastX = 3;
        int lastFloorY = FloorY;
        for (int segment = 0; segment < SegmentCount; segment++)
        {
            int platformX = lastX + gap + 1;
            int platformY = lastFloorY + deltaY;
            FlatWorldTestBuilder.SetSolid(world, platformX, platformY, 0);
            lastX = platformX;
            lastFloorY = platformY;
        }

        return world;
    }
}
