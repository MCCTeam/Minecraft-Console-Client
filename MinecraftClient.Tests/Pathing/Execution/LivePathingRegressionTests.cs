using System;
using System.Threading;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Pathing.Goals;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class LivePathingRegressionTests
{
    [Fact]
    public void AStar_ThreeByOneRejectionLayout_WithInvalidGoalBlock_RejectsBeforeExecution()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 135, max: 148);
        FlatWorldTestBuilder.ClearBox(world, 140, 80, 135, 148, 85, 140);
        // Match the live harness: the raised block is reachable, but the requested goal block is not standable.
        FlatWorldTestBuilder.SetSolid(world, 143, 80, 138);

        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var finder = new AStarPathFinder();

        PathResult result = finder.Calculate(
            ctx,
            startX: 141,
            startY: 80,
            startZ: 138,
            new GoalBlock(144, 81, 138),
            CancellationToken.None,
            timeoutMs: 2000);

        Assert.Equal(PathStatus.Failed, result.Status);
        Assert.Empty(PathSegmentBuilder.FromPath(result.Path));
    }

    [Fact]
    public void AStar_RepeatedSingleGapParkourChain_PrefersTwoLongJumpsOverFourShortJumps()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 578, max: 590);
        FlatWorldTestBuilder.ClearBox(world, 578, 79, 578, 590, 90, 582);
        FlatWorldTestBuilder.SetSolid(world, 580, 79, 580);
        FlatWorldTestBuilder.SetSolid(world, 582, 79, 580);
        FlatWorldTestBuilder.SetSolid(world, 584, 79, 580);
        FlatWorldTestBuilder.SetSolid(world, 586, 79, 580);
        FlatWorldTestBuilder.SetSolid(world, 588, 79, 580);

        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var finder = new AStarPathFinder();

        PathResult result = finder.Calculate(
            ctx,
            startX: 580,
            startY: 80,
            startZ: 580,
            new GoalBlock(588, 80, 580),
            CancellationToken.None,
            timeoutMs: 2000);

        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        Assert.Collection(
            segments,
            first =>
            {
                Assert.Equal(MoveType.Parkour, first.MoveType);
                Assert.Equal(new Location(580.5, 80, 580.5), first.Start);
                Assert.Equal(new Location(584.5, 80, 580.5), first.End);
            },
            second =>
            {
                Assert.Equal(MoveType.Parkour, second.MoveType);
                Assert.Equal(new Location(584.5, 80, 580.5), second.Start);
                Assert.Equal(new Location(588.5, 80, 580.5), second.End);
            });
    }

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
            ExitTransition = PathTransitionType.LandingRecovery,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
        };
        var next = new PathSegment
        {
            Start = new Location(122.5, 80, 110.5),
            End = new Location(122.5, 80, 111.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.03, true, true, false, false, 12)
        };

        var template = new SprintJumpTemplate(segment, next);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 270f);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 140, out Location finalPos);

        Assert.True(state == TemplateState.Complete, $"state={state} finalPos={finalPos} vel={physics.DeltaMovement}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, segment.End), $"finalPos={finalPos} vel={physics.DeltaMovement}");
        double horizontalSpeed = Math.Sqrt(physics.DeltaMovement.X * physics.DeltaMovement.X + physics.DeltaMovement.Z * physics.DeltaMovement.Z);
        Assert.InRange(horizontalSpeed, 0.0, 0.04);
    }
}
