using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class GroundedTemplateConvergenceTests
{
    [Fact]
    public void WalkTemplate_FinalStop_Completes_WhenFootprintStaysInsideTargetBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 160, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }

    [Fact]
    public void WalkTemplate_PrepareJump_CompletesWithoutSettlingOnRunUpBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(1.5, 80, 0.5),
            End = new Location(3.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(current, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(current.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 60, out _);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(physics.DeltaMovement.X > 0.02);
    }

    [Fact]
    public void DescendTemplate_LandingRecovery_CompletesOnLandingBlock()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        FlatWorldTestBuilder.ClearBox(world, 1, 79, 0, 1, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 1, 78, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 79, 0.5),
            MoveType = MoveType.Descend,
            ExitTransition = PathTransitionType.LandingRecovery
        };

        var template = new DescendTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 240, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }
}
