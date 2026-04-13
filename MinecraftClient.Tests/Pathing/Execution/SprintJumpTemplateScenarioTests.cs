using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
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
    public void SprintJumpTemplate_TwoBlockGap_LandingRecovery_CompletesOnTurnEntrySupportStrip()
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
        Assert.True(
            TemplateFootingHelper.IsCenterInsideSupportStrip(finalPos, segment.End, next.End),
            $"finalPos={finalPos} vel={physics.DeltaMovement}");
    }

    [Fact]
    public void SprintJumpTemplate_LandingRecoveryIntoTurn_CompletesWithLowResidualSpeed()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 108, max: 126);
        FlatWorldTestBuilder.ClearBox(world, 118, 79, 108, 126, 90, 112);
        FlatWorldTestBuilder.SetSolid(world, 120, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 123, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 123, 79, 111);

        var segment = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(123.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
        };
        var next = new PathSegment
        {
            Start = new Location(123.5, 80, 110.5),
            End = new Location(123.5, 80, 111.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.03, true, true, false, false, 12)
        };

        var template = new SprintJumpTemplate(segment, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 160, out Location finalPos);
        double horizontalSpeed = Math.Sqrt(physics.DeltaMovement.X * physics.DeltaMovement.X + physics.DeltaMovement.Z * physics.DeltaMovement.Z);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(
            TemplateFootingHelper.IsCenterInsideSupportStrip(finalPos, segment.End, next.End),
            $"finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.InRange(horizontalSpeed, 0.0, 0.04);
    }

    [Fact]
    public void SprintJumpTemplate_ThreeBlockGap_WithIsolatedTakeoffBlock_JumpsImmediately()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 108, max: 126);
        FlatWorldTestBuilder.ClearBox(world, 118, 79, 108, 126, 90, 112);
        FlatWorldTestBuilder.SetSolid(world, 120, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 123, 79, 110);

        var segment = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(123.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop,
        };

        var template = new SprintJumpTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);
        var input = new MovementInput();

        TemplateState state = template.Tick(segment.Start, physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.True(input.Forward);
        Assert.True(input.Sprint);
        Assert.True(input.Jump);
    }
}
