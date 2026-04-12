using System;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution.Templates;

namespace MinecraftClient.Pathing.Execution
{
    /// <summary>
    /// Maps a PathSegment (MoveType + start/end) to the appropriate IActionTemplate.
    /// </summary>
    public static class ActionTemplateFactory
    {
        public static IActionTemplate Create(PathSegment segment, PathSegment? nextSegment)
        {
            return segment.MoveType switch
            {
                MoveType.Traverse => new WalkTemplate(segment, nextSegment),
                MoveType.Diagonal => new WalkTemplate(segment, nextSegment),
                MoveType.Ascend   => new AscendTemplate(segment, nextSegment),
                MoveType.Descend  => new DescendTemplate(segment, nextSegment),
                MoveType.Fall     => new FallTemplate(segment, nextSegment),
                MoveType.Climb    => new ClimbTemplate(segment, nextSegment),
                MoveType.Parkour  => new SprintJumpTemplate(segment, nextSegment),
                _ => throw new ArgumentException($"Unknown MoveType: {segment.MoveType}")
            };
        }
    }
}
