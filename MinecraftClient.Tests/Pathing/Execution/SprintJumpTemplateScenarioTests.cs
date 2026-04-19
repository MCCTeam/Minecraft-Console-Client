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
    public void SprintJumpTemplate_LinearFlatGap4_PrepareJump_CompletesFromTraverseCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap4", gap: 4, deltaY: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 2);

        TemplateState state = RunSegment(segments, index: 3, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[3].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearFlatGap1_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap1", gap: 1, deltaY: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 3);

        TemplateState state = RunSegment(segments, index: 4, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[4]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[4].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[4]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearFlatGap1_PrepareJump_HandoffStaysInsideTargetBlock()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap1", gap: 1, deltaY: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 2);

        TemplateState state = RunSegment(segments, index: 3, physics, world, out Location currentPos, out string trace);

        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(currentPos, segments[4].Start),
            $"state={state} currentPos={currentPos} vel={physics.DeltaMovement} segment={segments[3]} next={segments[4]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearAscendGap2DyPlus1_PrepareJump_CompletesFromTraverseCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-ascend-gap2-dy+1", gap: 2, deltaY: 1);
        PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.PlanResult.Path);
        PathSegmentRun? segmentRun = FindSegmentRun(result, segmentIndex: 3);

        Assert.True(
            result.Completed && result.ReplanCount == 0,
            $"completed={result.Completed} replans={result.ReplanCount} finalPos={result.FinalPosition}\n" +
            $"info={string.Join('\n', result.InfoLogs)}\ndebug={string.Join('\n', result.DebugLogs)}");
        Assert.NotNull(segmentRun);
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(segmentRun.Position, segments[3].End),
            $"segmentPos={segmentRun.Position} target={segments[3].End} finalPos={result.FinalPosition}\n" +
            $"info={string.Join('\n', result.InfoLogs)}\ndebug={string.Join('\n', result.DebugLogs)}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearAscendGap2DyPlus1_SecondPrepareJump_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-ascend-gap2-dy+1", gap: 2, deltaY: 1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 3);

        TemplateState state = RunSegment(segments, index: 4, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[4]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[4].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[4]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearAscendGap1DyPlus1_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-ascend-gap1-dy+1", gap: 1, deltaY: 1);
        PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.PlanResult.Path);
        PathSegmentRun? segmentRun = FindSegmentRun(result, segmentIndex: 5);

        Assert.True(
            result.Completed && result.ReplanCount == 0,
            $"completed={result.Completed} replans={result.ReplanCount} finalPos={result.FinalPosition}\n" +
            $"info={string.Join('\n', result.InfoLogs)}\ndebug={string.Join('\n', result.DebugLogs)}");
        Assert.NotNull(segmentRun);
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(segmentRun.Position, segments[5].End),
            $"segmentPos={segmentRun.Position} target={segments[5].End} finalPos={result.FinalPosition}\n" +
            $"info={string.Join('\n', result.InfoLogs)}\ndebug={string.Join('\n', result.DebugLogs)}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap2DyMinus2_PrepareJump_CompletesFromTraverseCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap2-dy-2", gap: 2, deltaY: -2);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 2);

        TemplateState state = RunSegment(segments, index: 3, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[3].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap2DyMinus2_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap2-dy-2", gap: 2, deltaY: -2);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 4);

        TemplateState state = RunSegment(segments, index: 5, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[5].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap2DyMinus1_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap2-dy-1", gap: 2, deltaY: -1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 4);

        TemplateState state = RunSegment(segments, index: 5, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[5].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap2DyMinus1_SecondPrepareJump_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap2-dy-1", gap: 2, deltaY: -1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 3);

        TemplateState state = RunSegment(segments, index: 4, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[4]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[4].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[4]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap3DyMinus2_PrepareJump_CompletesFromTraverseCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap3-dy-2", gap: 3, deltaY: -2);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 2);

        TemplateState state = RunSegment(segments, index: 3, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[3].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap3DyMinus2_PrepareJump_HandoffStaysInsideTargetBlock()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap3-dy-2", gap: 3, deltaY: -2);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 2);

        TemplateState state = RunSegment(segments, index: 3, physics, world, out Location currentPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} currentPos={currentPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(currentPos, segments[4].Start),
            $"state={state} currentPos={currentPos} vel={physics.DeltaMovement} segment={segments[3]} next={segments[4]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap3DyMinus1_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap3-dy-1", gap: 3, deltaY: -1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 4);

        TemplateState state = RunSegment(segments, index: 5, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[5].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap4DyMinus1_PrepareJump_CompletesFromTraverseCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap4-dy-1", gap: 4, deltaY: -1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 2);

        TemplateState state = RunSegment(segments, index: 3, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[3].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[3]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearDescendGap4DyMinus1_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap4-dy-1", gap: 4, deltaY: -1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 4);

        TemplateState state = RunSegment(segments, index: 5, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[5].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearFlatGap2_SecondPrepareJump_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap2", gap: 2, deltaY: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 3);

        TemplateState state = RunSegment(segments, index: 4, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[4]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[4].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[4]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_LinearFlatGap2_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap2", gap: 2, deltaY: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedLinearScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 4);

        TemplateState state = RunSegment(segments, index: 5, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[5].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[5]}\n{trace}");
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

    [Fact]
    public void SprintJumpTemplate_SidewallFlatGap2_FinalStop_CompletesInsideLandingBlock()
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap: 2, deltaY: 0, wallOffset: 0);
        var segment = new PathSegment
        {
            Start = new Location(100.5, 80, 100.5),
            End = new Location(99.5, 80, 102.5),
            MoveType = MoveType.Parkour,
            ParkourProfile = ParkourProfile.Sidewall,
            ExitTransition = PathTransitionType.FinalStop
        };

        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 0f);
        PathSegment[] segments = [segment];

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(
            state == TemplateState.Complete,
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segment}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segment}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallFlatGap2_SecondPrepareJump_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-flat-gap2-wo0", gap: 2, deltaY: 0, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 0);

        TemplateState state = RunSegment(segments, index: 1, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[1]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[1].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[1]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallFlatGap3Wo1_FirstPrepareJump_CompletesFromStart()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-flat-gap3-wo1", gap: 3, deltaY: 0, wallOffset: 1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[0].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallFlatGap4Wo0_FirstPrepareJump_CompletesFromStart()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-flat-gap4-wo0", gap: 4, deltaY: 0, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[0].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallFlatGap3Wo1_SecondPrepareJump_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-flat-gap3-wo1", gap: 3, deltaY: 0, wallOffset: 1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 0);

        TemplateState state = RunSegment(segments, index: 1, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[1]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[1].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[1]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallAscendGap3Wo1_FirstPrepareJump_CompletesFromStart()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-ascend-gap3-dy+1-wo1", gap: 3, deltaY: 1, wallOffset: 1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[0].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap2DyMinus1Wo0_FirstPrepareJump_CompletesFromStart()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap2-dy-1-wo0", gap: 2, deltaY: -1, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[0].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap2DyMinus2Wo0_FirstPrepareJump_CompletesFromStart()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap2-dy-2-wo0", gap: 2, deltaY: -2, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[0].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap2DyMinus1Wo0_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap2-dy-1-wo0", gap: 2, deltaY: -1, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 1);

        TemplateState state = RunSegment(segments, index: 2, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[2].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap2DyMinus2Wo0_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap2-dy-2-wo0", gap: 2, deltaY: -2, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 1);

        TemplateState state = RunSegment(segments, index: 2, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[2].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap5DyMinus1Wo0_FirstPrepareJump_CompletesFromStart()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap5-dy-1-wo0", gap: 5, deltaY: -1, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[0].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap5DyMinus1Wo1_FirstPrepareJump_CompletesFromStart()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap5-dy-1-wo1", gap: 5, deltaY: -1, wallOffset: 1);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[0].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap5DyMinus2Wo0_FirstPrepareJump_CompletesFromStart()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap5-dy-2-wo0", gap: 5, deltaY: -2, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        TemplateState state = RunSegment(segments, index: 0, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[0].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[0]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap5DyMinus2Wo0_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap5-dy-2-wo0", gap: 5, deltaY: -2, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 1);

        TemplateState state = RunSegment(segments, index: 2, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[2].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap3DyMinus1Wo0_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap3-dy-1-wo0", gap: 3, deltaY: -1, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 1);

        TemplateState state = RunSegment(segments, index: 2, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[2].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallFlatGap2_FinalStop_CompletesFromChainCarry()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-flat-gap2-wo0", gap: 2, deltaY: 0, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 1);

        TemplateState state = RunSegment(segments, index: 2, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[2].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
    }

    [Fact]
    public void SprintJumpTemplate_SidewallDescendGap4DyMinus1Wo0_FinalStop_CompletesInsideLandingBlock()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create("sidewall-descend-gap4-dy-1-wo0", gap: 4, deltaY: -1, wallOffset: 0);
        (World world, List<PathSegment> segments, PlayerPhysics physics) = BuildPlannedScenario(scenario);

        RunSegmentsThrough(segments, world, physics, lastCompletedIndex: 1);

        TemplateState state = RunSegment(segments, index: 2, physics, world, out Location finalPos, out string trace);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
        Assert.True(
            TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segments[2].End),
            $"state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[2]}\n{trace}");
    }

    private static (World World, List<PathSegment> Segments, PlayerPhysics Physics) BuildPlannedLinearScenario(PathingExecutionScenario scenario)
    {
        return BuildPlannedScenario(scenario);
    }

    private static (World World, List<PathSegment> Segments, PlayerPhysics Physics) BuildPlannedScenario(PathingExecutionScenario scenario)
    {
        PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);

        Assert.Equal(PathStatus.Success, planResult.Status);

        return (
            scenario.BuildWorld(),
            PathSegmentBuilder.FromPath(planResult.Path),
            TemplateSimulationRunner.CreateGroundedPhysics(scenario.Start, scenario.StartYaw));
    }

    private static void RunSegmentsThrough(IReadOnlyList<PathSegment> segments, World world, PlayerPhysics physics, int lastCompletedIndex)
    {
        for (int index = 0; index <= lastCompletedIndex; index++)
        {
            TemplateState state = RunSegment(segments, index, physics, world, out Location finalPos, out string trace);
            Assert.True(
                state == TemplateState.Complete,
                $"segmentIndex={index} state={state} finalPos={finalPos} vel={physics.DeltaMovement} segment={segments[index]}\n{trace}");
        }
    }

    private static TemplateState RunSegment(
        IReadOnlyList<PathSegment> segments,
        int index,
        PlayerPhysics physics,
        World world,
        out Location finalPos,
        out string trace)
    {
        PathSegment segment = segments[index];
        PathSegment? next = index + 1 < segments.Count ? segments[index + 1] : null;
        IActionTemplate template = ActionTemplateFactory.Create(segment, next);
        var input = new MovementInput();
        var tail = new Queue<string>();
        TemplateState state = TemplateState.InProgress;

        for (int tick = 0; tick < 160; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            tail.Enqueue(
                $"tick={tick} state={state} pos={pos} vel={physics.DeltaMovement} yaw={physics.Yaw:F1} onGround={physics.OnGround} " +
                $"input(F={input.Forward},B={input.Back},L={input.Left},R={input.Right},J={input.Jump},S={input.Sprint})");
            if (tail.Count > 40)
                tail.Dequeue();

            if (state != TemplateState.InProgress)
            {
                if (state == TemplateState.Complete && next is not null)
                {
                    physics.ApplyInput(input);
                    physics.Tick(world);
                }
                break;
            }

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        trace = string.Join('\n', tail);
        return state;
    }

    private static PathSegmentRun? FindSegmentRun(PathingScenarioResult result, int segmentIndex)
    {
        foreach (PathSegmentRun run in result.SegmentRuns)
        {
            if (run.SegmentIndex == segmentIndex)
                return run;
        }

        return null;
    }
}
