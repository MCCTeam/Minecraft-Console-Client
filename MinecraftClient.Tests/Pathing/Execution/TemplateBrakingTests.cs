using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class TemplateBrakingTests
{
    [Fact]
    public void WalkTemplate_BackBrakes_WhenFinalStopIsTooClose()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop,
            PreserveSprint = false
        };

        var template = new WalkTemplate(segment, null);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.38, 80.0, 0.5),
            DeltaMovement = new Vec3d(0.156, 0.0, 0.0),
            OnGround = true,
            Yaw = 270f
        };
        var input = new MovementInput();

        TemplateState state = template.Tick(new Location(1.38, 80, 0.5), physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.False(input.Forward);
        Assert.False(input.Sprint);
        Assert.True(input.Back);
    }

    [Fact]
    public void WalkTemplate_KeepsForward_WhenTransitionContinuesStraight()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.ContinueStraight,
            PreserveSprint = true
        };
        var next = new PathSegment
        {
            Start = new Location(1.5, 80, 0.5),
            End = new Location(2.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new WalkTemplate(current, next);
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.10, 80.0, 0.5),
            DeltaMovement = new Vec3d(0.140, 0.0, 0.0),
            OnGround = true,
            Yaw = 270f
        };
        var input = new MovementInput();

        TemplateState state = template.Tick(new Location(1.10, 80, 0.5), physics, input, world);

        Assert.Equal(TemplateState.InProgress, state);
        Assert.True(input.Forward);
        Assert.True(input.Sprint);
    }
}
