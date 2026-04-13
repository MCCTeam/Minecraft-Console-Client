using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class ClimbFallTemplateTests
{
    [Fact]
    public void ClimbTemplate_AscendsLadderColumn_CompletesOverTarget()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -2, max: 2);
        BuildLadder(world, x: 0, z: 0, bottomY: 80, topY: 84);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(0.5, 84, 0.5),
            MoveType = MoveType.Climb,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new ClimbTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 0f);
        physics.OnClimbable = true;

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 220, out Location finalPos);

        Assert.Equal(TemplateState.Complete, state);
        AssertNearTargetBlock(finalPos, segment.End);
    }

    [Fact]
    public void ClimbTemplate_DescendsLadderColumn_CompletesOverTarget()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -2, max: 2);
        BuildLadder(world, x: 0, z: 0, bottomY: 80, topY: 84);

        var segment = new PathSegment
        {
            Start = new Location(0.5, 84, 0.5),
            End = new Location(0.5, 80, 0.5),
            MoveType = MoveType.Climb,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new ClimbTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 180f);
        physics.OnClimbable = true;

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 220, out Location finalPos);

        Assert.Equal(TemplateState.Complete, state);
        AssertNearTargetBlock(finalPos, segment.End);
    }

    [Fact]
    public void FallTemplate_DropsStraightDown_CompletesOnFloor()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 4, max: 8);

        var segment = new PathSegment
        {
            Start = new Location(5.5, 85, 5.5),
            End = new Location(5.5, 80, 5.5),
            MoveType = MoveType.Fall,
            ExitTransition = PathTransitionType.FinalStop
        };

        var template = new FallTemplate(segment, null);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(segment.Start, yaw: 90f);
        physics.OnGround = false;
        physics.DeltaMovement = new Vec3d(0, -0.15, 0);

        TemplateState state = TemplateSimulationRunner.Run(template, physics, world, maxTicks: 260, out Location finalPos);

        Assert.Equal(TemplateState.Complete, state);
        AssertNearTargetBlock(finalPos, segment.End);
    }

    private static void AssertNearTargetBlock(Location actual, Location target)
    {
        Assert.True(Math.Abs(actual.Y - target.Y) < 0.6, $"Expected final Y near {target.Y:F2}, got {actual.Y:F2}");
        Assert.True(TemplateFootingHelper.IsFootprintInsideTargetBlock(actual, target),
            $"Expected the final footprint to stay within {target}, got {actual}");
    }

    private static void BuildLadder(World world, int x, int z, int bottomY, int topY)
    {
        for (int y = bottomY; y <= topY; y++)
        {
            FlatWorldTestBuilder.SetClimbable(world, x, y, z);
        }
    }
}
