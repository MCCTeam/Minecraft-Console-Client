using System.Collections.Generic;

namespace MinecraftClient.Pathing.Core
{
    public enum PathStatus
    {
        Success,
        Partial,
        Failed
    }

    public sealed class PathResult
    {
        public PathStatus Status { get; }
        public IReadOnlyList<PathNode> Path { get; }
        public int NodesExplored { get; }
        public long ElapsedMs { get; }

        public PathResult(PathStatus status, IReadOnlyList<PathNode> path, int nodesExplored, long elapsedMs)
        {
            Status = status;
            Path = path;
            NodesExplored = nodesExplored;
            ElapsedMs = elapsedMs;
        }

        public static PathResult Fail(int nodesExplored, long elapsedMs)
            => new(PathStatus.Failed, [], nodesExplored, elapsedMs);
    }
}
