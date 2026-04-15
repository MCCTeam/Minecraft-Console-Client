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
    public void SprintJumpTemplate_TwoBlockGap_FinalStop_CompletesFromOppositeYaw()
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
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End));
    }

    [Fact]
    public void SprintJumpTemplate_Approach_SnapsYawImmediatelyFromOppositeYaw()
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
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
        var input = new MovementInput();

        TemplateState state = template.Tick(segment.Start, physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.InRange(physics.Yaw, 269.9f, 270.1f);
        Assert.True(input.Forward);
        Assert.True(input.Sprint);
        Assert.True(input.Jump);
    }

    [Fact]
    public void SprintJumpTemplate_TwoBlockGap_FinalStop_CompletesFromOppositeYawWithinTwentyTicks()
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
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
        var input = new MovementInput();
        TemplateState state = TemplateState.InProgress;
        int elapsedTicks = 0;
        Location finalPos = segment.Start;
        var trace = new List<string>();
        for (; elapsedTicks < 80; elapsedTicks++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            if (elapsedTicks < 20 || state != TemplateState.InProgress)
            {
                trace.Add(
                    $"tick={elapsedTicks} state={state} pos={pos} yaw={physics.Yaw:F1} vel={physics.DeltaMovement} " +
                    $"onGround={physics.OnGround} input(F={input.Forward},J={input.Jump},S={input.Sprint})");
            }

            if (state != TemplateState.InProgress)
            {
                finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
                break;
            }

            physics.ApplyInput(input);
            physics.Tick(world);
            finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        }

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(elapsedTicks <= 20, $"elapsedTicks={elapsedTicks} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End), $"elapsedTicks={elapsedTicks} finalPos={finalPos} vel={physics.DeltaMovement}\n{string.Join('\n', trace)}");
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
    public void SprintJumpTemplate_PrepareJumpIntoSecondParkour_CompletesWithoutSettling()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.ClearBox(world, 0, 79, 0, 6, 82, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 4, 79, 0);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(2.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(2.5, 80, 0.5),
            End = new Location(4.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new SprintJumpTemplate(segment, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);
        double horizontalSpeed = Math.Sqrt(physics.DeltaMovement.X * physics.DeltaMovement.X + physics.DeltaMovement.Z * physics.DeltaMovement.Z);

        Assert.Equal(TemplateState.Complete, state);
        Assert.True(TemplateFootingHelper.IsCenterInsideTargetBlock(finalPos, segment.End), $"finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(horizontalSpeed > 0.02, $"finalPos={finalPos} vel={physics.DeltaMovement}");
    }

    [Fact]
    public void SprintJumpTemplate_FinalStop_CompletesAfterPrepareJumpCarry()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 16);
        FlatWorldTestBuilder.ClearBox(world, 0, 79, 0, 6, 82, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 4, 79, 0);

        var first = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(2.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };
        var second = new PathSegment
        {
            Start = new Location(2.5, 80, 0.5),
            End = new Location(4.5, 80, 0.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.FinalStop
        };

        var physics = TemplateSimulationRunner.CreateGroundedPhysics(first.Start, yaw: 270f);

        TemplateState firstState = TemplateSimulationRunner.Run(new SprintJumpTemplate(first, second), physics, world, maxTicks: 140, out Location handoffPos);
        TemplateState secondState = TemplateSimulationRunner.Run(new SprintJumpTemplate(second, null), physics, world, maxTicks: 140, out Location finalPos);

        Assert.Equal(TemplateState.Complete, firstState);
        Assert.True(
            secondState == TemplateState.Complete,
            $"secondState={secondState} handoffPos={handoffPos} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, second.End), $"handoffPos={handoffPos} finalPos={finalPos} vel={physics.DeltaMovement}");
    }

    [Fact]
    public void SprintJumpTemplate_DiagonalLandingRecovery_HandsOffToTurnTraverse()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -1, max: 8);
        FlatWorldTestBuilder.ClearBox(world, -1, 79, -1, 8, 82, 4);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, 79, 1);
        FlatWorldTestBuilder.SetSolid(world, 3, 79, 1);
        FlatWorldTestBuilder.SetSolid(world, 4, 79, 2);

        var parkour = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(2.5, 80, 1.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery,
            ExitHints = new PathTransitionHints(1, 0, 0.0, 0.05, true, true, false, true, 12)
        };
        var traverse = new PathSegment
        {
            Start = new Location(2.5, 80, 1.5),
            End = new Location(3.5, 80, 1.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.Turn,
            ExitHints = new PathTransitionHints(1, 1, 0.0, 0.05, true, true, false, true, 12)
        };
        var next = new PathSegment
        {
            Start = new Location(3.5, 80, 1.5),
            End = new Location(4.5, 80, 2.5),
            MoveType = MoveType.Diagonal,
            ExitTransition = PathTransitionType.FinalStop
        };

        var physics = TemplateSimulationRunner.CreateGroundedPhysics(parkour.Start, yaw: 315f);

        TemplateState parkourState = TemplateSimulationRunner.Run(new SprintJumpTemplate(parkour, traverse), physics, world, maxTicks: 160, out Location handoffPos);
        TemplateState traverseState = TemplateSimulationRunner.Run(new WalkTemplate(traverse, next), physics, world, maxTicks: 160, out Location finalPos);

        Assert.Equal(TemplateState.Complete, parkourState);
        Assert.True(
            traverseState == TemplateState.Complete,
            $"traverseState={traverseState} handoffPos={handoffPos} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(
            TemplateFootingHelper.IsCenterInsideTargetBlock(finalPos, traverse.End),
            $"handoffPos={handoffPos} finalPos={finalPos} vel={physics.DeltaMovement}");
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
