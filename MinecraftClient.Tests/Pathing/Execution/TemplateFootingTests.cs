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

    [Fact]
    public void IsCenterInsideTargetBlock_ReturnsTrue_WhenPlayerStopsNearEdge()
    {
        bool inside = TemplateFootingHelper.IsCenterInsideTargetBlock(
            new Location(10.29, 80.0, 4.50),
            new Location(10.50, 80.0, 4.50));

        Assert.True(inside);
    }

    [Fact]
    public void IsFootprintInsideSupportStrip_ReturnsTrue_WhenPlayerStraddlesTurnEntryBlocks()
    {
        bool inside = TemplateFootingHelper.IsFootprintInsideSupportStrip(
            new Location(123.46, 80.0, 110.77),
            new Location(123.50, 80.0, 110.50),
            new Location(123.50, 80.0, 111.50));

        Assert.True(inside);
    }

    [Fact]
    public void WillLeaveSupportStripNextTick_ReturnsFalse_WhenLowSpeedStaysOnTurnEntryBlocks()
    {
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(123.46, 80.0, 110.77),
            DeltaMovement = new Vec3d(-0.0199, 0.0, 0.0040),
            OnGround = true
        };

        bool exitsNextTick = TemplateFootingHelper.WillLeaveSupportStripNextTick(
            new Location(123.46, 80.0, 110.77),
            physics,
            new Location(123.50, 80.0, 110.50),
            new Location(123.50, 80.0, 111.50));

        Assert.False(exitsNextTick);
    }

    [Fact]
    public void IsCenterInsideSupportStrip_ReturnsTrue_WhenLowSpeedTurnEntryStopsOnSeam()
    {
        bool inside = TemplateFootingHelper.IsCenterInsideSupportStrip(
            new Location(123.46, 80.0, 110.77),
            new Location(123.50, 80.0, 110.50),
            new Location(123.50, 80.0, 111.50));

        Assert.True(inside);
    }
}
