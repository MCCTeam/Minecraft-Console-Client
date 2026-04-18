using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;
using System.Reflection;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathExecutorCompletionTests
{
    [Fact]
    public void Tick_ClearsMovementInput_WhenSegmentCompletes()
    {
        var executor = new PathExecutor(new List<PathSegment>
        {
            new()
            {
                Start = new Location(0.5, 80, 0.5),
                End = new Location(1.5, 80, 0.5),
                MoveType = MoveType.Traverse
            }
        });

        var physics = new PlayerPhysics
        {
            Yaw = 270f,
            Pitch = 0f,
            OnGround = true
        };
        var input = new MovementInput
        {
            Forward = true,
            Sprint = true,
            Jump = true,
            Back = true
        };
        var pos = new Location(1.48, 80, 0.5);
        World world = FlatWorldTestBuilder.CreateStoneFloor();

        var state = executor.Tick(pos, physics, input, world);

        Assert.Equal(PathExecutorState.Complete, state);
        Assert.False(input.Forward);
        Assert.False(input.Sprint);
        Assert.False(input.Jump);
        Assert.False(input.Back);
    }

    [Fact]
    public void Tick_PreservesCarryInput_WhenAdvancingIntoNextSegment()
    {
        var executor = new PathExecutor(new List<PathSegment>
        {
            new()
            {
                Start = new Location(0.5, 80, 0.5),
                End = new Location(1.5, 80, 0.5),
                MoveType = MoveType.Traverse,
                ExitTransition = PathTransitionType.PrepareJump,
                ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
                PreserveSprint = true
            },
            new()
            {
                Start = new Location(1.5, 80, 0.5),
                End = new Location(4.5, 80, 0.5),
                MoveType = MoveType.Parkour,
                ExitTransition = PathTransitionType.FinalStop
            }
        });

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.48, 80.00, 0.50),
            DeltaMovement = new Vec3d(0.1178, -0.0784, 0.0),
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f,
            Pitch = 0f
        };
        var input = new MovementInput();
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -2, max: 8);

        PathExecutorState state = executor.Tick(new Location(physics.Position.X, physics.Position.Y, physics.Position.Z), physics, input, world);

        Assert.Equal(PathExecutorState.InProgress, state);
        Assert.Equal(1, executor.CurrentIndex);
        Assert.True(input.Forward, $"input(F={input.Forward},S={input.Sprint},J={input.Jump},B={input.Back})");
        Assert.True(input.Sprint, $"input(F={input.Forward},S={input.Sprint},J={input.Jump},B={input.Back})");
        Assert.False(input.Back, $"input(F={input.Forward},S={input.Sprint},J={input.Jump},B={input.Back})");
    }

    [Fact]
    public void Tick_AdvanceFromParkourIntoParkour_IssuesJumpOnSameTick()
    {
        var executor = new PathExecutor(new List<PathSegment>
        {
            new()
            {
                Start = new Location(0.5, 80, 0.5),
                End = new Location(3.5, 80, 0.5),
                MoveType = MoveType.Parkour,
                ExitTransition = PathTransitionType.PrepareJump,
                ExitHints = new PathTransitionHints(1, 0, 0.10, double.PositiveInfinity, false, true, true, false, 10),
                PreserveSprint = true
            },
            new()
            {
                Start = new Location(3.5, 80, 0.5),
                End = new Location(6.5, 80, 0.5),
                MoveType = MoveType.Parkour,
                ExitTransition = PathTransitionType.FinalStop
            }
        });

        SetCurrentTemplate(
            executor,
            new CompletingTemplate(
                new Location(0.5, 80, 0.5),
                new Location(3.5, 80, 0.5)));

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(3.50, 80.00, 0.50),
            DeltaMovement = new Vec3d(0.2200, 0.0, 0.0),
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f,
            Pitch = 0f
        };
        var input = new MovementInput();
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -2, max: 10);
        FlatWorldTestBuilder.ClearBox(world, 1, 79, 0, 6, 82, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 3, 79, 0);
        FlatWorldTestBuilder.SetSolid(world, 6, 79, 0);

        PathExecutorState state = executor.Tick(new Location(physics.Position.X, physics.Position.Y, physics.Position.Z), physics, input, world);

        Assert.Equal(PathExecutorState.InProgress, state);
        Assert.Equal(1, executor.CurrentIndex);
        Assert.True(input.Forward, $"input(F={input.Forward},S={input.Sprint},J={input.Jump},B={input.Back})");
        Assert.True(input.Sprint, $"input(F={input.Forward},S={input.Sprint},J={input.Jump},B={input.Back})");
        Assert.True(input.Jump, $"input(F={input.Forward},S={input.Sprint},J={input.Jump},B={input.Back})");
        Assert.False(input.Back, $"input(F={input.Forward},S={input.Sprint},J={input.Jump},B={input.Back})");
    }

    [Fact]
    public void Tick_CompletesStraightThreeSegmentFlatPath()
    {
        List<PathSegment> segments = PathSegmentBuilder.FromPath(BuildNodes(
            (100, 80, 100, MoveType.Traverse),
            (101, 80, 100, MoveType.Traverse),
            (102, 80, 100, MoveType.Traverse),
            (103, 80, 100, MoveType.Traverse)));

        var executor = new PathExecutor(segments);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segments[0].Start, yaw: 270f);
        var input = new MovementInput();
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 95, max: 115);

        PathExecutorState state = PathExecutorState.InProgress;
        for (int tick = 0; tick < 260; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = executor.Tick(pos, physics, input, world);
            if (state != PathExecutorState.InProgress)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Assert.Equal(PathExecutorState.Complete, state);
    }

    [Fact]
    public void Tick_ShortAcceptedPath_FromLiveSegmentZeroDriftState_CompletesWithoutFailure()
    {
        List<PathSegment> segments = PathSegmentBuilder.FromPath(BuildNodes(
            (100, 80, 100, MoveType.Traverse),
            (101, 80, 100, MoveType.Traverse),
            (102, 80, 100, MoveType.Traverse),
            (103, 80, 100, MoveType.Traverse)));

        var debugLogs = new List<string>();
        var executor = new PathExecutor(segments, debugLogs.Add);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(101.56, 80.00, 100.74),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f,
            Pitch = 0f
        };
        var input = new MovementInput();
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 95, max: 115);

        PathExecutorState state = PathExecutorState.InProgress;
        for (int tick = 0; tick < 220; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = executor.Tick(pos, physics, input, world);
            if (state != PathExecutorState.InProgress)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Assert.True(state == PathExecutorState.Complete, $"state={state}\n{string.Join('\n', debugLogs)}");
    }

    [Fact]
    public void Tick_LinearDescendGap0DyMinus2_DoesNotTurnInPlace()
    {
        List<PathSegment> segments = PathSegmentBuilder.FromPath(BuildNodes(
            (0, 80, 0, MoveType.Traverse),
            (1, 80, 0, MoveType.Traverse),
            (2, 80, 0, MoveType.Traverse),
            (3, 80, 0, MoveType.Traverse),
            (4, 78, 0, MoveType.Descend),
            (5, 76, 0, MoveType.Descend),
            (6, 74, 0, MoveType.Descend)));

        var debugLogs = new List<string>();
        var executor = new PathExecutor(segments, debugLogs.Add);
        World world = LinearParkourScenarioBuilder.Create("linear-descend-gap0-dy-2", gap: 0, deltaY: -2).BuildWorld();
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(new Location(0.5, 80, 0.5), yaw: 270f);
        var input = new MovementInput();
        var samples = new List<TurnSample>();

        PathExecutorState state = PathExecutorState.InProgress;
        for (int tick = 0; tick < 420; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = executor.Tick(pos, physics, input, world);
            if (state != PathExecutorState.InProgress)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
            samples.Add(new TurnSample(physics.Position.X, physics.Position.Y, physics.Position.Z, physics.Yaw));
        }

        int turnStalls = CountTurnStalls(samples, out string stallTrace);

        Assert.True(state == PathExecutorState.Complete, $"state={state}\n{string.Join('\n', debugLogs)}");
        Assert.True(turnStalls == 0, $"turnStalls={turnStalls}\n{stallTrace}\n{string.Join('\n', debugLogs)}");
    }

    [Fact]
    public void Tick_PlannedLinearDescendGap0DyMinus2_DoesNotTurnInPlace()
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create("linear-descend-gap0-dy-2", gap: 0, deltaY: -2);
        PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(planResult.Path);

        var debugLogs = new List<string>();
        var executor = new PathExecutor(segments, debugLogs.Add);
        World world = scenario.BuildWorld();
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(scenario.Start, yaw: scenario.StartYaw);
        var input = new MovementInput();
        var samples = new List<TurnSample>();

        PathExecutorState state = PathExecutorState.InProgress;
        for (int tick = 0; tick < scenario.MaxExecutionTicks; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = executor.Tick(pos, physics, input, world);
            if (state != PathExecutorState.InProgress)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
            samples.Add(new TurnSample(physics.Position.X, physics.Position.Y, physics.Position.Z, physics.Yaw));
        }

        int turnStalls = CountTurnStalls(samples, out string stallTrace);
        string segmentTrace = string.Join('\n', segments.ConvertAll(static segment => segment.ToString()));

        Assert.Equal(PathStatus.Success, planResult.Status);
        Assert.True(state == PathExecutorState.Complete, $"state={state}\nsegments:\n{segmentTrace}\n{string.Join('\n', debugLogs)}");
        Assert.True(turnStalls == 0, $"turnStalls={turnStalls}\n{stallTrace}\nsegments:\n{segmentTrace}\n{string.Join('\n', debugLogs)}");
    }

    [Fact]
    public void Tick_LinearFlatGap2_CompletesWithoutFailure()
    {
        AssertLinearScenarioCompletes("linear-flat-gap2", gap: 2, deltaY: 0);
    }

    [Fact]
    public void Tick_LinearAscendGap2DyPlus1_CompletesWithoutFailure()
    {
        AssertLinearScenarioCompletes("linear-ascend-gap2-dy+1", gap: 2, deltaY: 1);
    }

    [Fact]
    public void Tick_LinearDescendGap2DyMinus1_CompletesWithoutFailure()
    {
        AssertLinearScenarioCompletes("linear-descend-gap2-dy-1", gap: 2, deltaY: -1);
    }

    private static List<PathNode> BuildNodes(params (int x, int y, int z, MoveType moveUsed)[] raw)
    {
        var result = new List<PathNode>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            var node = new PathNode(raw[i].x, raw[i].y, raw[i].z);
            if (i > 0)
                node.MoveUsed = raw[i].moveUsed;
            result.Add(node);
        }

        return result;
    }

    private readonly record struct TurnSample(double X, double Y, double Z, float Yaw);

    private sealed class CompletingTemplate(Location start, Location end) : IActionTemplate
    {
        public Location ExpectedStart { get; } = start;
        public Location ExpectedEnd { get; } = end;

        public TemplateState Tick(Location currentPos, PlayerPhysics physics, MovementInput input, World world)
        {
            return TemplateState.Complete;
        }
    }

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
                    traces.Add(
                        $"start={windowStart} end={idx} base=({baseSample.X:F2},{baseSample.Y:F2},{baseSample.Z:F2},{baseSample.Yaw:F1}) " +
                        $"last=({sample.X:F2},{sample.Y:F2},{sample.Z:F2},{sample.Yaw:F1}) yaw={cumulativeYaw:F1}");
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

    private static void SetCurrentTemplate(PathExecutor executor, IActionTemplate template)
    {
        FieldInfo field = typeof(PathExecutor).GetField("_currentTemplate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PathExecutor._currentTemplate not found");
        field.SetValue(executor, template);
    }

    private static void AssertLinearScenarioCompletes(string scenarioId, int gap, int deltaY)
    {
        PathingExecutionScenario scenario = LinearParkourScenarioBuilder.Create(scenarioId, gap, deltaY);
        PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);
        List<PathSegment> segments = PathSegmentBuilder.FromPath(planResult.Path);

        var debugLogs = new List<string>();
        var executor = new PathExecutor(segments, debugLogs.Add);
        World world = scenario.BuildWorld();
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(scenario.Start, scenario.StartYaw);
        var input = new MovementInput();

        PathExecutorState state = PathExecutorState.InProgress;
        for (int tick = 0; tick < scenario.MaxExecutionTicks; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = executor.Tick(pos, physics, input, world);
            if (state != PathExecutorState.InProgress)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Location finalPos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
        string segmentTrace = string.Join('\n', segments.ConvertAll(static segment => segment.ToString()));

        Assert.Equal(PathStatus.Success, planResult.Status);
        Assert.True(
            state == PathExecutorState.Complete,
            $"scenario={scenarioId} state={state} finalPos={finalPos} vel={physics.DeltaMovement}\nsegments:\n{segmentTrace}\n{string.Join('\n', debugLogs)}");
    }
}
