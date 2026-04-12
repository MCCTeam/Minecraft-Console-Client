using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class LivePathingRegressionTests
{
    [Fact]
    public void SprintJumpTemplate_LandingRecoveryIntoTurn_CompletesInsideLandingBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 108, max: 126);
        FlatWorldTestBuilder.ClearBox(world, 118, 79, 108, 126, 90, 112);
        FlatWorldTestBuilder.SetSolid(world, 120, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 122, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 122, 79, 111);
        FlatWorldTestBuilder.SetSolid(world, 120, 80, 111);
        FlatWorldTestBuilder.SetSolid(world, 120, 81, 111);

        var segment = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(122.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery
        };
        var next = new PathSegment
        {
            Start = new Location(122.5, 80, 110.5),
            End = new Location(122.5, 80, 111.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new SprintJumpTemplate(segment, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End), $"finalPos={finalPos} vel={physics.DeltaMovement}");
    }
}
