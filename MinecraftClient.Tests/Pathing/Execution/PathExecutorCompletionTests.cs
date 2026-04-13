using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;
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
}
