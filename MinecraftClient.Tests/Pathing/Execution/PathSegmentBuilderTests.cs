using System.Collections.Generic;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathSegmentBuilderTests
{
    [Fact]
    public void FromPath_AnnotatesStraightTraverse_AsContinueStraight()
    {
        var nodes = BuildNodes(
            (0, 80, 0, MoveType.Traverse),
            (1, 80, 0, MoveType.Traverse),
            (2, 80, 0, MoveType.Traverse));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);

        Assert.Equal(PathTransitionType.ContinueStraight, segments[0].ExitTransition);
        Assert.True(segments[0].PreserveSprint);
    }

    [Fact]
    public void FromPath_AnnotatesOrthogonalTraverse_AsTurn()
    {
        var nodes = BuildNodes(
            (0, 80, 0, MoveType.Traverse),
            (1, 80, 0, MoveType.Traverse),
            (1, 80, 1, MoveType.Traverse));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);

        Assert.Equal(PathTransitionType.Turn, segments[0].ExitTransition);
        Assert.False(segments[0].PreserveSprint);
    }

    [Fact]
    public void FromPath_AnnotatesTraverseIntoParkour_AsPrepareJump()
    {
        var nodes = BuildNodes(
            (120, 80, 110, MoveType.Traverse),
            (121, 80, 110, MoveType.Traverse),
            (123, 80, 110, MoveType.Parkour));

        List<PathSegment> segments = PathSegmentBuilder.FromPath(nodes);

        Assert.Equal(PathTransitionType.PrepareJump, segments[0].ExitTransition);
        Assert.True(segments[0].PreserveSprint);
    }

    [Fact]
    public void FromPath_CopiesParkourProfile_ToRuntimeSegment()
    {
        var start = new PathNode(100, 80, 100);
        var end = new PathNode(99, 80, 102)
        {
            MoveUsed = MoveType.Parkour,
            ParkourProfile = ParkourProfile.Sidewall
        };

        List<PathSegment> segments = PathSegmentBuilder.FromPath([start, end]);

        Assert.Single(segments);
        Assert.Equal(ParkourProfile.Sidewall, segments[0].ParkourProfile);
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
