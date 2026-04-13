using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class SprintJumpTemplateScenarioTests
{
    [Fact]
    public void SprintJumpTemplate_TwoBlockGap_FinalStop_Completes()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.ClearBox(world, 0, 79, 0, 4, 82, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(2.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new SprintJumpTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }

    [Fact]
    public void SprintJumpTemplate_ThreeBlockGap_FinalStop_Completes()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.ClearBox(world, 0, 79, 0, 5, 82, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 3, 79, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(3.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new SprintJumpTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }

    [Fact]
    public void SprintJumpTemplate_TwoBlockGap_LandingRecovery_CompletesInsideLandingBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.ClearBox(world, 0, 79, 0, 4, 82, 2);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, 80, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, 81, 1);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(2.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery
        };
        var next = new PathSegment
        {
            Start = new Location(2.5, 80, 0.5),
            End = new Location(2.5, 80, 1.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new SprintJumpTemplate(segment, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }
}
