using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Goals;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathSegmentManagerTests
{
    [Fact]
    public void Tick_AcceptedAscendChain_FromSterileStart_CompletesWithoutReplan()
    {
        var debugLogs = new List<string>();
        var infoLogs = new List<string>();
        var manager = new PathSegmentManager(debugLog: debugLogs.Add, infoLog: infoLogs.Add);
        var goal = new GoalBlock(177, 83, 162);
        var path = BuildNodes(
            (171, 80, 160, MoveType.Traverse),
            (172, 80, 161, MoveType.Diagonal),
            (173, 80, 162, MoveType.Diagonal),
            (174, 80, 162, MoveType.Traverse),
            (175, 81, 162, MoveType.Ascend),
            (176, 82, 162, MoveType.Ascend),
            (177, 83, 162, MoveType.Ascend));
        var result = new PathResult(PathStatus.Success, path, nodesExplored: 7, elapsedMs: 1);

        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 158, max: 180);
        FlatWorldTestBuilder.ClearBox(world, 170, 80, 160, 178, 85, 168);
        FlatWorldTestBuilder.SetSolid(world, 175, 80, 162);
        FlatWorldTestBuilder.SetSolid(world, 176, 81, 162);
        FlatWorldTestBuilder.SetSolid(world, 177, 82, 162);

        var physics = TemplateSimulationRunner.CreateGroundedPhysics(new Location(171.5, 80, 160.5), yaw: 315f);
        var input = new MovementInput();
        var recentTrace = new Queue<string>();

        manager.StartNavigation(goal, result);

        for (int tick = 0; tick < 420 && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
            recentTrace.Enqueue(
                $"tick={tick} pos={pos} vel={physics.DeltaMovement} onGround={physics.OnGround} yaw={physics.Yaw:F1} input(F={input.Forward},B={input.Back},J={input.Jump},S={input.Sprint})");
            if (recentTrace.Count > 40)
                recentTrace.Dequeue();
            if (!manager.IsNavigating)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Assert.True(!manager.IsNavigating && manager.ReplanCount == 0,
            $"replanCount={manager.ReplanCount}\ninfo={string.Join('\n', infoLogs)}\ndebug={string.Join('\n', debugLogs)}\ntrace={string.Join('\n', recentTrace)}");
    }

    [Fact]
    public void Tick_AcceptedDescendStaircase_FromSterileStart_CompletesWithoutReplan()
    {
        var debugLogs = new List<string>();
        var infoLogs = new List<string>();
        var manager = new PathSegmentManager(debugLog: debugLogs.Add, infoLog: infoLogs.Add);
        var goal = new GoalBlock(367, 80, 360);
        var path = BuildNodes(
            (362, 85, 360, MoveType.Traverse),
            (364, 83, 360, MoveType.Descend),
            (366, 81, 360, MoveType.Descend),
            (367, 80, 360, MoveType.Descend));
        var result = new PathResult(PathStatus.Success, path, nodesExplored: 4, elapsedMs: 1);

        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 358, max: 369);
        FlatWorldTestBuilder.ClearBox(world, 360, 79, 358, 369, 85, 362);
        FlatWorldTestBuilder.FillSolid(world, 362, 84, 359, 362, 84, 361);
        FlatWorldTestBuilder.FillSolid(world, 363, 83, 359, 363, 83, 361);
        FlatWorldTestBuilder.FillSolid(world, 364, 82, 359, 364, 82, 361);
        FlatWorldTestBuilder.FillSolid(world, 365, 81, 359, 365, 81, 361);
        FlatWorldTestBuilder.FillSolid(world, 366, 80, 359, 366, 80, 361);
        FlatWorldTestBuilder.FillSolid(world, 367, 79, 359, 367, 79, 361);

        var physics = TemplateSimulationRunner.CreateGroundedPhysics(new Location(362.5, 85, 360.5), yaw: 270f);
        var input = new MovementInput();
        var recentTrace = new Queue<string>();

        manager.StartNavigation(goal, result);

        for (int tick = 0; tick < 420 && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
            recentTrace.Enqueue(
                $"tick={tick} pos={pos} vel={physics.DeltaMovement} onGround={physics.OnGround} yaw={physics.Yaw:F1} input(F={input.Forward},B={input.Back},J={input.Jump},S={input.Sprint})");
            if (recentTrace.Count > 40)
                recentTrace.Dequeue();
            if (!manager.IsNavigating)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Assert.True(!manager.IsNavigating && manager.ReplanCount == 0,
            $"replanCount={manager.ReplanCount}\ninfo={string.Join('\n', infoLogs)}\ndebug={string.Join('\n', debugLogs)}\ntrace={string.Join('\n', recentTrace)}");
    }

    [Fact]
    public void Tick_CompletesNavigation_WhenReplanStartsInsideGoalBlock()
    {
        var infoLogs = new List<string>();
        var manager = new PathSegmentManager(infoLog: infoLogs.Add);
        var goal = new GoalBlock(1, 80, 0);
        var path = new[]
        {
            new PathNode(0, 80, 0),
            new PathNode(1, 80, 0) { MoveUsed = MoveType.Traverse }
        };
        var result = new PathResult(PathStatus.Success, path, nodesExplored: 2, elapsedMs: 1);
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 0, max: 4);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.10, 80.0, 0.5),
            DeltaMovement = Vec3d.Zero,
            OnGround = false,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };
        var input = new MovementInput();

        manager.StartNavigation(goal, result);

        for (int tick = 0; tick < 60 && manager.IsNavigating; tick++)
        {
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
        }

        Assert.False(manager.IsNavigating);
        Assert.Null(manager.Goal);
        Assert.Equal(1, manager.ReplanCount);
    }

    [Fact]
    public void Tick_ShortAcceptedPath_FromSterileStart_CompletesWithoutIncrementingReplanCount()
    {
        var infoLogs = new List<string>();
        var manager = new PathSegmentManager(infoLog: infoLogs.Add);
        var goal = new GoalBlock(103, 80, 100);
        var path = new[]
        {
            new PathNode(100, 80, 100),
            new PathNode(101, 80, 100) { MoveUsed = MoveType.Traverse },
            new PathNode(102, 80, 100) { MoveUsed = MoveType.Traverse },
            new PathNode(103, 80, 100) { MoveUsed = MoveType.Traverse }
        };
        var result = new PathResult(PathStatus.Success, path, nodesExplored: 4, elapsedMs: 1);
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 95, max: 115);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(new Location(100.5, 80, 100.5), yaw: 270f);
        var input = new MovementInput();

        manager.StartNavigation(goal, result);

        for (int tick = 0; tick < 260 && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
            if (!manager.IsNavigating)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Assert.False(manager.IsNavigating);
        Assert.Equal(0, manager.ReplanCount);
    }

    [Fact]
    public void Tick_ShortAcceptedPath_FromLiveSegmentZeroDriftState_CompletesWithoutReplan()
    {
        var debugLogs = new List<string>();
        var infoLogs = new List<string>();
        var manager = new PathSegmentManager(debugLog: debugLogs.Add, infoLog: infoLogs.Add);
        var goal = new GoalBlock(103, 80, 100);
        var path = new[]
        {
            new PathNode(100, 80, 100),
            new PathNode(101, 80, 100) { MoveUsed = MoveType.Traverse },
            new PathNode(102, 80, 100) { MoveUsed = MoveType.Traverse },
            new PathNode(103, 80, 100) { MoveUsed = MoveType.Traverse }
        };
        var result = new PathResult(PathStatus.Success, path, nodesExplored: 4, elapsedMs: 1);
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 95, max: 115);
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

        manager.StartNavigation(goal, result);

        for (int tick = 0; tick < 260 && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
            if (!manager.IsNavigating)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Assert.True(!manager.IsNavigating && manager.ReplanCount == 0,
            $"replanCount={manager.ReplanCount}\ninfo={string.Join('\n', infoLogs)}\ndebug={string.Join('\n', debugLogs)}");
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
