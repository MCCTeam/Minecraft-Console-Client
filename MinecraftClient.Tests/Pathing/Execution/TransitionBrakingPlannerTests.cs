using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class TransitionBrakingPlannerTests
{
    [Fact]
    public void Plan_ReturnsCarryMomentum_ForContinueStraight()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var physics = CreatePhysics(0.156, 0.0, onGround: true);
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.ContinueStraight,
            PreserveSprint = true
        };

        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(current, null, new Location(1.05, 80, 0.5), physics, world);

        Assert.True(decision.HoldForward);
        Assert.True(decision.HoldSprint);
        Assert.False(decision.HoldBack);
    }

    [Fact]
    public void Plan_BackBrakes_ForFinalStop_WhenRemainingRunwayIsTooShort()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var physics = CreatePhysics(0.156, 0.0, onGround: true);
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop,
            PreserveSprint = false
        };

        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(current, null, new Location(1.38, 80, 0.5), physics, world);

        Assert.False(decision.HoldForward);
        Assert.False(decision.HoldSprint);
        Assert.True(decision.HoldBack);
    }

    [Fact]
    public void Plan_NudgesForward_ForFinalStop_WhenAlreadySlowButStillShort()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var physics = CreatePhysics(0.0, 0.0, onGround: true);
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop,
            PreserveSprint = false
        };

        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(current, null, new Location(1.41, 80, 0.5), physics, world);

        Assert.True(decision.HoldForward);
        Assert.False(decision.HoldSprint);
        Assert.False(decision.HoldBack);
    }

    [Fact]
    public void ShouldReleaseForwardInAir_ReturnsTrue_ForParkourIntoTurn()
    {
        var physics = CreatePhysics(0.32, 0.0, onGround: false);
        var current = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(123.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.Turn,
            PreserveSprint = false
        };
        var next = new PathSegment
        {
            Start = new Location(123.5, 80, 110.5),
            End = new Location(123.5, 80, 111.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.FinalStop
        };

        bool release = TransitionBrakingPlanner.ShouldReleaseForwardInAir(current, next, new Location(123.18, 80.92, 110.5), physics);

        Assert.True(release);
    }

    private static PlayerPhysics CreatePhysics(double deltaX, double deltaZ, bool onGround)
    {
        return new PlayerPhysics
        {
            Position = new Vec3d(0.0, 80.0, 0.0),
            DeltaMovement = new Vec3d(deltaX, 0.0, deltaZ),
            OnGround = onGround,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };
    }
}
