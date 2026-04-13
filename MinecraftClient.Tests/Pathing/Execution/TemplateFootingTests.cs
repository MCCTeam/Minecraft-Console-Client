using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class TemplateFootingTests
{
    [Fact]
    public void IsFootprintInsideTargetBlock_ReturnsTrue_WhenPlayerIsNearEdgeButStillInside()
    {
        bool inside = TemplateFootingHelper.IsFootprintInsideTargetBlock(
            new Location(10.69, 80.0, 4.50),
            new Location(10.50, 80.0, 4.50));

        Assert.True(inside);
    }

    [Fact]
    public void IsFootprintInsideTargetBlock_ReturnsFalse_WhenPlayerCrossesBlockEdge()
    {
        bool inside = TemplateFootingHelper.IsFootprintInsideTargetBlock(
            new Location(10.81, 80.0, 4.50),
            new Location(10.50, 80.0, 4.50));

        Assert.False(inside);
    }

    [Fact]
    public void WillLeaveTargetBlockNextTick_ReturnsTrue_WhenVelocityWouldCarryPastEdge()
    {
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(10.67, 80.0, 4.50),
            DeltaMovement = new Vec3d(0.060, 0.0, 0.0),
            OnGround = true
        };

        bool exitsNextTick = TemplateFootingHelper.WillLeaveTargetBlockNextTick(
            new Location(10.67, 80.0, 4.50),
            physics,
            new Location(10.50, 80.0, 4.50));

        Assert.True(exitsNextTick);
    }
}
