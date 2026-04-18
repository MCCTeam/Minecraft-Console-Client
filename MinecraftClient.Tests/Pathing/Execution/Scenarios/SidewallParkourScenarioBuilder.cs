using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Goals;

namespace MinecraftClient.Tests.Pathing.Execution;

public static class SidewallParkourScenarioBuilder
{
    private const int SegmentCount = 3;
    private const int BaseX = 100;
    private const int BaseY = 80;
    private const int BaseZ = 100;
    private const int FloorY = BaseY - 1;

    public static IEnumerable<object[]> AcceptedCases()
    {
        yield return ["sidewall-ascend-gap2-dy+1-wo0", 2, 1, 0];
        yield return ["sidewall-ascend-gap3-dy+1-wo0", 3, 1, 0];
        yield return ["sidewall-ascend-gap3-dy+1-wo1", 3, 1, 1];
        yield return ["sidewall-descend-gap2-dy-2-wo0", 2, -2, 0];
        yield return ["sidewall-descend-gap3-dy-2-wo0", 3, -2, 0];
        yield return ["sidewall-descend-gap4-dy-2-wo0", 4, -2, 0];
        yield return ["sidewall-descend-gap5-dy-2-wo0", 5, -2, 0];
        yield return ["sidewall-descend-gap3-dy-2-wo1", 3, -2, 1];
        yield return ["sidewall-descend-gap4-dy-2-wo1", 4, -2, 1];
        yield return ["sidewall-descend-gap5-dy-2-wo1", 5, -2, 1];
        yield return ["sidewall-descend-gap2-dy-1-wo0", 2, -1, 0];
        yield return ["sidewall-descend-gap3-dy-1-wo0", 3, -1, 0];
        yield return ["sidewall-descend-gap4-dy-1-wo0", 4, -1, 0];
        yield return ["sidewall-descend-gap5-dy-1-wo0", 5, -1, 0];
        yield return ["sidewall-descend-gap3-dy-1-wo1", 3, -1, 1];
        yield return ["sidewall-descend-gap4-dy-1-wo1", 4, -1, 1];
        yield return ["sidewall-descend-gap5-dy-1-wo1", 5, -1, 1];
        yield return ["sidewall-flat-gap2-wo0", 2, 0, 0];
        yield return ["sidewall-flat-gap3-wo0", 3, 0, 0];
        yield return ["sidewall-flat-gap4-wo0", 4, 0, 0];
        yield return ["sidewall-flat-gap3-wo1", 3, 0, 1];
        yield return ["sidewall-flat-gap4-wo1", 4, 0, 1];
    }

    public static IEnumerable<object[]> RejectedCases()
    {
        yield return ["sidewall-ascend-gap4-dy+1-wo0", 4, 1, 0];
        yield return ["sidewall-ascend-gap4-dy+1-wo1", 4, 1, 1];
        yield return ["sidewall-descend-gap6-dy-2-wo0", 6, -2, 0];
        yield return ["sidewall-descend-gap6-dy-2-wo1", 6, -2, 1];
        yield return ["sidewall-descend-gap6-dy-1-wo0", 6, -1, 0];
        yield return ["sidewall-descend-gap6-dy-1-wo1", 6, -1, 1];
        yield return ["sidewall-flat-gap5-wo0", 5, 0, 0];
        yield return ["sidewall-flat-gap5-wo1", 5, 0, 1];
    }

    internal static PathingExecutionScenario Create(string scenarioId, int gap, int deltaY, int wallOffset, int maxExecutionTicks = 700)
    {
        return new PathingExecutionScenario
        {
            Id = scenarioId,
            BuildWorld = () => BuildWorld(gap, deltaY, wallOffset),
            Start = new Location(BaseX + 0.5, BaseY, BaseZ + 0.5),
            Goal = new GoalBlock(BaseX - SegmentCount, FloorY + (deltaY * SegmentCount) + 1, BaseZ + (gap * SegmentCount)),
            StartYaw = 0f,
            MaxExecutionTicks = maxExecutionTicks,
        };
    }

    internal static World BuildWorld(int gap, int deltaY, int wallOffset)
    {
        int maxZ = BaseZ + (gap * SegmentCount);
        World world = FlatWorldTestBuilder.CreateStoneFloor(floorY: 0, min: 80, max: maxZ + 8);
        FlatWorldTestBuilder.ClearBox(world, 90, 70, 90, 110, 96, maxZ + 8);

        int curX = BaseX;
        int curY = FloorY;
        int curZ = BaseZ;

        FlatWorldTestBuilder.FillSolid(world, curX, curY, curZ - 2, curX, curY, curZ);

        for (int segment = 0; segment < SegmentCount; segment++)
        {
            int landY = curY + deltaY;
            int wallX = curX - 1;

            FlatWorldTestBuilder.FillSolid(
                world,
                wallX,
                Math.Min(curY, landY) - 1,
                curZ,
                wallX,
                Math.Max(curY, landY) + 7,
                curZ + wallOffset);

            int landX = curX - 1;
            int landZ = curZ + gap;

            FlatWorldTestBuilder.SetSolid(world, landX, landY, landZ);

            curX = landX;
            curY = landY;
            curZ = landZ;
        }

        return world;
    }
}
