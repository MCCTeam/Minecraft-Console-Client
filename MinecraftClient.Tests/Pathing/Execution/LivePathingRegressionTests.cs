using System;
using System.Collections.Generic;
using System.Threading;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Pathing.Goals;
using MinecraftClient.Physics;
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

    [Fact]
    public void AStar_LinearFlatGapFourChain_PlansThroughAllThreeJumps()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap4", gap: 4, deltaY: 0);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        Assert.NotEmpty(segments);
        Assert.Equal(scenario.Goal.X + 0.5, segments[^1].End.X);
        Assert.Equal(scenario.Goal.Y, segments[^1].End.Y);
        Assert.Equal(scenario.Goal.Z + 0.5, segments[^1].End.Z);
    }

    [Fact]
    public void AStar_LinearFlatGapFourChain_TagsParkourSegmentsAsDefaultProfile()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap4", gap: 4, deltaY: 0);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        Assert.Equal(3, segments.Count(segment => segment.MoveType == MoveType.Parkour));
        Assert.All(
            segments.Where(segment => segment.MoveType == MoveType.Parkour),
            segment => Assert.Equal(ParkourProfile.Default, segment.ParkourProfile));
    }

    [Fact]
    public void AStar_LinearFlatGap4_DoesNotInsertRunupSetupSegments()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-flat-gap4", gap: 4, deltaY: 0);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        int firstParkourIndex = segments.FindIndex(segment => segment.MoveType == MoveType.Parkour);

        Assert.Equal(3, firstParkourIndex);
        Assert.All(
            segments.Take(firstParkourIndex),
            segment =>
            {
                Assert.Equal(MoveType.Traverse, segment.MoveType);
                Assert.True(segment.End.X > segment.Start.X, segment.ToString());
            });
        Assert.Equal(new Location(3.5, 80, 0.5), segments[firstParkourIndex - 1].End);
    }

    [Theory]
    [InlineData("linear-ascend-gap2-dy+1", 2, 1)]
    [InlineData("linear-descend-gap4-dy-1", 4, -1)]
    public void AStar_LinearChainCases_PlansThroughAllThreeJumps(string scenarioId, int gap, int deltaY)
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create(scenarioId, gap, deltaY);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        Assert.NotEmpty(segments);
        Assert.Equal(scenario.Goal.X + 0.5, segments[^1].End.X);
        Assert.Equal(scenario.Goal.Y, segments[^1].End.Y);
        Assert.Equal(scenario.Goal.Z + 0.5, segments[^1].End.Z);
    }

    [Fact]
    public void AStar_LinearDescendGap2DyMinus1_DoesNotSkipIntermediateLanding()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap2-dy-1", gap: 2, deltaY: -1);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        Assert.DoesNotContain(
            segments,
            segment => segment.MoveType == MoveType.Parkour
                && (Math.Abs(segment.End.X - segment.Start.X) + Math.Abs(segment.End.Z - segment.Start.Z)) > 3.1);
        Assert.Equal(scenario.Goal.X + 0.5, segments[^1].End.X);
        Assert.Equal(scenario.Goal.Y, segments[^1].End.Y);
        Assert.Equal(scenario.Goal.Z + 0.5, segments[^1].End.Z);
    }

    [Theory]
    [InlineData("linear-descend-gap4-dy-2", 4, -2)]
    public void AStar_LinearExtendedChainCases_PlansThroughAllThreeJumps(string scenarioId, int gap, int deltaY)
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create(scenarioId, gap, deltaY);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        Assert.NotEmpty(segments);
        Assert.Equal(scenario.Goal.X + 0.5, segments[^1].End.X);
        Assert.Equal(scenario.Goal.Y, segments[^1].End.Y);
        Assert.Equal(scenario.Goal.Z + 0.5, segments[^1].End.Z);
    }

    [Theory]
    [InlineData("linear-ascend-gap3-dy+1", 3, 1)]
    [InlineData("linear-descend-gap5-dy-1", 5, -1)]
    [InlineData("linear-descend-gap5-dy-2", 5, -2)]
    public void AStar_LinearRejectedCases_RejectBeforeExecution(string scenarioId, int gap, int deltaY)
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create(scenarioId, gap, deltaY);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);

        Assert.Equal(PathStatus.Failed, result.Status);
        Assert.Empty(PathSegmentBuilder.FromPath(result.Path));
    }

    [Theory]
    [MemberData(nameof(SidewallParkourScenarioBuilder.AcceptedCases), MemberType = typeof(SidewallParkourScenarioBuilder))]
    public void AStar_SidewallAcceptedCases_PlanThroughAllThreeJumps(string scenarioId, int gap, int deltaY, int wallOffset)
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(scenarioId, gap, deltaY, wallOffset);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        Assert.Equal(3, segments.FindAll(segment => segment.MoveType == MoveType.Parkour).Count);
        Assert.All(
            segments.Where(segment => segment.MoveType == MoveType.Parkour),
            segment => Assert.Equal(ParkourProfile.Sidewall, segment.ParkourProfile));
        Assert.Equal(scenario.Goal.X + 0.5, segments[^1].End.X);
        Assert.Equal(scenario.Goal.Y, segments[^1].End.Y);
        Assert.Equal(scenario.Goal.Z + 0.5, segments[^1].End.Z);
    }

    [Theory]
    [InlineData("sidewall-descend-gap5-dy-1-wo0", 5, 0)]
    [InlineData("sidewall-descend-gap5-dy-1-wo1", 5, 1)]
    public void AStar_SidewallLongDescendStaticEntry_PrependsExplicitRunupTraverses(string scenarioId, int gap, int wallOffset)
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(
            scenarioId,
            gap,
            deltaY: -1,
            wallOffset);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);

        int firstParkourIndex = segments.FindIndex(segment => segment.MoveType == MoveType.Parkour);
        Assert.True(firstParkourIndex >= 2, string.Join('\n', segments));
        Assert.All(
            segments.Take(firstParkourIndex),
            segment => Assert.Equal(MoveType.Traverse, segment.MoveType));
        Assert.Equal(new Location(100.5, 80, 100.5), segments[firstParkourIndex - 1].End);
        Assert.Equal(ParkourProfile.Sidewall, segments[firstParkourIndex].ParkourProfile);
    }

    [Theory]
    [MemberData(nameof(SidewallParkourScenarioBuilder.RejectedCases), MemberType = typeof(SidewallParkourScenarioBuilder))]
    public void AStar_SidewallRejectedCases_RejectBeforeExecution(string scenarioId, int gap, int deltaY, int wallOffset)
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(scenarioId, gap, deltaY, wallOffset);
        PathResult result = PathingScenarioRunner.PlanOnly(scenario);

        Assert.Equal(PathStatus.Failed, result.Status);
        Assert.Empty(PathSegmentBuilder.FromPath(result.Path));
    }

    [Fact]
    public void AStar_LiveCoordinateLinearDescendGap3DyMinus2_PlansThroughAllThreeJumps()
    {
        const int baseX = 100;
        const int baseY = 80;
        const int baseZ = 180;

        World world = BuildLiveLinearWorld(baseX, baseY, baseZ, gap: 3, deltaY: -2, segments: 3);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var finder = new AStarPathFinder();

        PathResult result = finder.Calculate(
            ctx,
            startX: baseX,
            startY: baseY,
            startZ: baseZ,
            new GoalBlock(115, 74, 180),
            CancellationToken.None,
            timeoutMs: 2000);

        List<PathSegment> segments = PathSegmentBuilder.FromPath(result.Path);

        Assert.Equal(PathStatus.Success, result.Status);
        Assert.NotEmpty(segments);
        Assert.Equal(new Location(115.5, 74, 180.5), segments[^1].End);
    }

    [Fact]
    public void PathSegmentManager_LiveCoordinateLinearFlatGap2_CompletesWithoutReplan()
    {
        const int baseX = 100;
        const int baseY = 80;
        const int baseZ = 297;

        World world = BuildLiveLinearWorld(baseX, baseY, baseZ, gap: 2, deltaY: 0, segments: 3);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var finder = new AStarPathFinder();
        PathResult planResult = finder.Calculate(
            ctx,
            startX: baseX,
            startY: baseY,
            startZ: baseZ,
            new GoalBlock(112, 80, 297),
            CancellationToken.None,
            timeoutMs: 2000);

        var debugLogs = new List<string>();
        var infoLogs = new List<string>();
        var manager = new PathSegmentManager(debugLogs.Add, infoLogs.Add);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(new Location(baseX + 0.5, baseY, baseZ + 0.5), yaw: 270f);
        var input = new MovementInput();
        var recentTrace = new Queue<string>();
        Location finalPos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);

        Assert.Equal(PathStatus.Success, planResult.Status);
        manager.StartNavigation(new GoalBlock(112, 80, 297), planResult);

        for (int tick = 0; tick < 200 && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
            recentTrace.Enqueue(
                $"tick={tick} pos={pos} vel={physics.DeltaMovement} yaw={physics.Yaw:F1} onGround={physics.OnGround} " +
                $"input(F={input.Forward},B={input.Back},J={input.Jump},S={input.Sprint})");
            if (recentTrace.Count > 80)
                recentTrace.Dequeue();

            if (!manager.IsNavigating)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
            finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        }

        Assert.True(
            !manager.IsNavigating
                && manager.Goal is null
                && manager.ReplanCount == 0
                && TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, new Location(112.5, 80, 297.5)),
            $"replans={manager.ReplanCount} final={finalPos}\ninfo={string.Join('\n', infoLogs)}\ndebug={string.Join('\n', debugLogs)}\ntrace={string.Join('\n', recentTrace)}");
    }

    [Fact]
    public void PathSegmentManager_LiveCoordinateLinearDescendGap3DyMinus2_CompletesWithoutReplan()
    {
        const int baseX = 100;
        const int baseY = 80;
        const int baseZ = 180;

        World world = BuildLiveLinearWorld(baseX, baseY, baseZ, gap: 3, deltaY: -2, segments: 3);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var finder = new AStarPathFinder();
        PathResult planResult = finder.Calculate(
            ctx,
            startX: baseX,
            startY: baseY,
            startZ: baseZ,
            new GoalBlock(115, 74, 180),
            CancellationToken.None,
            timeoutMs: 2000);

        var debugLogs = new List<string>();
        var infoLogs = new List<string>();
        var manager = new PathSegmentManager(debugLogs.Add, infoLogs.Add);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(new Location(baseX + 0.5, baseY, baseZ + 0.5), yaw: 270f);
        var input = new MovementInput();
        var recentTrace = new Queue<string>();
        Location finalPos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);

        Assert.Equal(PathStatus.Success, planResult.Status);
        manager.StartNavigation(new GoalBlock(115, 74, 180), planResult);

        for (int tick = 0; tick < 240 && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
            recentTrace.Enqueue(
                $"tick={tick} pos={pos} vel={physics.DeltaMovement} yaw={physics.Yaw:F1} onGround={physics.OnGround} " +
                $"input(F={input.Forward},B={input.Back},J={input.Jump},S={input.Sprint})");
            if (recentTrace.Count > 100)
                recentTrace.Dequeue();

            if (!manager.IsNavigating)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
            finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        }

        Assert.True(
            !manager.IsNavigating
                && manager.Goal is null
                && manager.ReplanCount == 0
                && TemplateFootingHelper.IsFootprintInsideTargetBlock(finalPos, new Location(115.5, 74, 180.5)),
            $"replans={manager.ReplanCount} final={finalPos}\ninfo={string.Join('\n', infoLogs)}\ndebug={string.Join('\n', debugLogs)}\ntrace={string.Join('\n', recentTrace)}");
    }

    [Fact]
    public void PathSegmentManager_LinearDescendGap0DyMinus2_DoesNotTurnInPlace()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap0-dy-2", gap: 0, deltaY: -2);
        PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);
        World world = scenario.BuildWorld();
        var debugLogs = new List<string>();
        var infoLogs = new List<string>();
        var manager = new PathSegmentManager(debugLogs.Add, infoLogs.Add);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(scenario.Start, scenario.StartYaw);
        var input = new MovementInput();
        var samples = new List<TurnSample>();

        Assert.Equal(PathStatus.Success, planResult.Status);
        manager.StartNavigation(scenario.Goal, planResult);

        for (int tick = 0; tick < scenario.MaxExecutionTicks && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);

            physics.ApplyInput(input);
            physics.Tick(world);

            samples.Add(new TurnSample(physics.Position.X, physics.Position.Y, physics.Position.Z, physics.Yaw));
        }

        Assert.True(!manager.IsNavigating && manager.Goal is null && manager.ReplanCount == 0,
            $"replans={manager.ReplanCount}\ninfo={string.Join('\n', infoLogs)}\ndebug={string.Join('\n', debugLogs)}");
        int turnStalls = CountTurnStalls(samples, out string stallTrace);
        Assert.True(turnStalls == 0, $"turnStalls={turnStalls}\n{stallTrace}");
    }

    private readonly record struct TurnSample(double X, double Y, double Z, float Yaw);

    private static int CountTurnStalls(IReadOnlyList<TurnSample> samples, out string trace)
    {
        const int MinSamples = 4;
        const double WindowMaxTravel = 0.35;
        const double MinCumulativeYaw = 180.0;
        const double MinPerStepYaw = 35.0;
        var traces = new List<string>();

        if (samples.Count < MinSamples)
        {
            trace = string.Empty;
            return 0;
        }

        static double NormalizeYawDelta(float previousYaw, float currentYaw)
        {
            double delta = (currentYaw - previousYaw + 180.0) % 360.0 - 180.0;
            return Math.Abs(delta);
        }

        static double HorizontalDistance(in TurnSample a, in TurnSample b)
        {
            double dx = a.X - b.X;
            double dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        int count = 0;
        int windowStart = 0;
        while (windowStart <= samples.Count - MinSamples)
        {
            TurnSample baseSample = samples[windowStart];
            double cumulativeYaw = 0.0;
            int largeSwings = 0;
            bool matched = false;

            for (int idx = windowStart + 1; idx < samples.Count; idx++)
            {
                TurnSample sample = samples[idx];
                if (HorizontalDistance(baseSample, sample) > WindowMaxTravel)
                    break;

                double yawDelta = NormalizeYawDelta(samples[idx - 1].Yaw, sample.Yaw);
                cumulativeYaw += yawDelta;
                if (yawDelta >= MinPerStepYaw)
                    largeSwings++;

                int sampleCount = idx - windowStart + 1;
                if (sampleCount >= MinSamples
                    && largeSwings >= MinSamples - 1
                    && cumulativeYaw >= MinCumulativeYaw)
                {
                    var windowSamples = new List<string>();
                    for (int traceIdx = windowStart; traceIdx <= idx; traceIdx++)
                    {
                        TurnSample traceSample = samples[traceIdx];
                        windowSamples.Add($"#{traceIdx}=({traceSample.X:F2},{traceSample.Y:F2},{traceSample.Z:F2},{traceSample.Yaw:F1})");
                    }
                    traces.Add(
                        $"start={windowStart} end={idx} base=({baseSample.X:F2},{baseSample.Y:F2},{baseSample.Z:F2},{baseSample.Yaw:F1}) " +
                        $"last=({sample.X:F2},{sample.Y:F2},{sample.Z:F2},{sample.Yaw:F1}) yaw={cumulativeYaw:F1}\n" +
                        string.Join(' ', windowSamples));
                    count++;
                    windowStart = idx + 1;
                    matched = true;
                    break;
                }
            }

            if (!matched)
                windowStart++;
        }

        trace = string.Join('\n', traces);
        return count;
    }

    private static World BuildLiveLinearWorld(int baseX, int baseY, int baseZ, int gap, int deltaY, int segments)
    {
        int endX = baseX + 3 + ((gap + 1) * segments);
        int min = Math.Min(baseX - 8, baseZ - 8);
        int max = Math.Max(endX + 8, baseZ + 8);
        World world = FlatWorldTestBuilder.CreateStoneFloor(floorY: 0, min: min, max: max);
        FlatWorldTestBuilder.ClearBox(world, baseX - 8, 1, baseZ - 2, endX + 8, baseY + 12, baseZ + 2);

        int floorY = baseY - 1;
        FlatWorldTestBuilder.FillSolid(world, baseX, floorY, baseZ, baseX + 3, floorY, baseZ);

        int lastX = baseX + 3;
        int lastFloorY = floorY;
        for (int segment = 0; segment < segments; segment++)
        {
            int platformX = lastX + gap + 1;
            int platformY = lastFloorY + deltaY;
            FlatWorldTestBuilder.SetSolid(world, platformX, platformY, baseZ);
            lastX = platformX;
            lastFloorY = platformY;
        }

        return world;
    }
}
