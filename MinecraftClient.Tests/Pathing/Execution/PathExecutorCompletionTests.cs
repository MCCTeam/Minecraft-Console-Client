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
            Pitch = 0f
        };
        var input = new MovementInput();
        var pos = new Location(1.48, 80, 0.5);
        World world = FlatWorldTestBuilder.CreateStoneFloor();

        var state = executor.Tick(pos, physics, input, world);

        Assert.Equal(PathExecutorState.Complete, state);
        Assert.False(input.Forward);
        Assert.False(input.Sprint);
        Assert.False(input.Jump);
        Assert.False(input.Back);
    }
}
