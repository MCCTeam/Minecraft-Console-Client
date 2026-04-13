using System.Collections.Generic;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathTransitionHintsTests
{
    [Fact]
    public void FromPath_AssignsTurnHints_WhenNextSegmentChangesHeading()
    {
        var nodes = BuildNodes(
            (0, 80, 0, MoveType.Traverse),
            (1, 80, 0, MoveType.Traverse),
            (1, 80, 1, MoveType.Traverse));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);
        PathTransitionHints hints = segments[0].ExitHints;

        Assert.Equal(PathTransitionType.Turn, segments[0].ExitTransition);
        Assert.True(hints.RequireStableFooting);
        Assert.True(hints.RequireGrounded);
        Assert.Equal(0, hints.DesiredHeadingX);
        Assert.Equal(1, hints.DesiredHeadingZ);
        Assert.InRange(hints.MaxExitSpeed, 0.0, 0.05);
    }

    [Fact]
    public void FromPath_AssignsJumpReadyHints_WhenNextSegmentIsParkour()
    {
        var nodes = BuildNodes(
            (120, 80, 110, MoveType.Traverse),
            (121, 80, 110, MoveType.Traverse),
            (123, 80, 110, MoveType.Parkour));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);
        PathTransitionHints hints = segments[0].ExitHints;

        Assert.Equal(PathTransitionType.PrepareJump, segments[0].ExitTransition);
        Assert.True(hints.RequireJumpReady);
        Assert.False(hints.RequireStableFooting);
        Assert.Equal(1, hints.DesiredHeadingX);
        Assert.Equal(0, hints.DesiredHeadingZ);
        Assert.True(hints.MinExitSpeed >= 0.10, $"MinExitSpeed={hints.MinExitSpeed}");
    }

    [Fact]
    public void FromPath_AssignsZeroRunUpSpeedHints_WhenNextSegmentIsAscend()
    {
        var nodes = BuildNodes(
            (174, 80, 162, MoveType.Traverse),
            (175, 80, 162, MoveType.Traverse),
            (176, 81, 162, MoveType.Ascend));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);
        PathTransitionHints hints = segments[0].ExitHints;

        Assert.Equal(PathTransitionType.PrepareJump, segments[0].ExitTransition);
        Assert.True(hints.RequireJumpReady);
        Assert.Equal(0.0, hints.MinExitSpeed);
    }

    [Fact]
    public void FromPath_AssignsPreciseStopHints_WhenSegmentIsFinalStop()
    {
        var nodes = BuildNodes(
            (10, 80, 10, MoveType.Traverse),
            (11, 80, 10, MoveType.Traverse));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);
        PathTransitionHints hints = segments[0].ExitHints;

        Assert.Equal(PathTransitionType.FinalStop, segments[0].ExitTransition);
        Assert.True(hints.RequireStableFooting);
        Assert.True(hints.RequireGrounded);
        Assert.InRange(hints.MaxExitSpeed, 0.0, 0.02);
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
