using MinecraftClient.Pathing.Core;
using MinecraftClient.Tests.Pathing.Execution.Contracts;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathPlanningContractTests
{
    [Fact]
    public void Get_ManagerAcceptedAscendChain_LoadsExactPlannerContract()
    {
        var store = PathingContractStore.LoadFromRepositoryRoot();

        PathingPlannerContract contract = store.GetPlanner("manager-accepted-ascend-chain");

        Assert.Equal(PathStatus.Success, contract.ExpectedStatus);
        Assert.Equal(6, contract.Segments.Length);

        PathingPlannerSegmentContract firstSegment = contract.Segments[0];
        Assert.Equal(MoveType.Diagonal, firstSegment.Move);
        Assert.Equal(new PathingBlock(171, 80, 160), firstSegment.From);
        Assert.Equal(new PathingBlock(172, 80, 161), firstSegment.To);

        PathingPlannerSegmentContract lastSegment = contract.Segments[5];
        Assert.Equal(MoveType.Ascend, lastSegment.Move);
        Assert.Equal(new PathingBlock(176, 82, 162), lastSegment.From);
        Assert.Equal(new PathingBlock(177, 83, 162), lastSegment.To);
    }
}
