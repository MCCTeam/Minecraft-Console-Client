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
        public static IActionTemplate Create(PathSegment segment)
        {
            return segment.MoveType switch
            {
                MoveType.Traverse => new WalkTemplate(segment.Start, segment.End),
                MoveType.Diagonal => new WalkTemplate(segment.Start, segment.End),
                MoveType.Ascend   => new AscendTemplate(segment.Start, segment.End),
                MoveType.Descend  => new DescendTemplate(segment.Start, segment.End),
                MoveType.Fall     => new FallTemplate(segment.Start, segment.End),
                MoveType.Climb    => new ClimbTemplate(segment.Start, segment.End),
                MoveType.Parkour  => new SprintJumpTemplate(segment.Start, segment.End),
                _ => throw new ArgumentException($"Unknown MoveType: {segment.MoveType}")
            };
        }
    }
}
