using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MinecraftClient.Pathing.Goals;
using MinecraftClient.Pathing.Moves;
using MinecraftClient.Pathing.Moves.Impl;

namespace MinecraftClient.Pathing.Core
{
    public sealed class AStarPathFinder
    {
        private readonly IMove[] _allMoves;
        private readonly int _maxChunkBorderFetch;

        public Action<string>? DebugLog { get; set; }

        public AStarPathFinder(IMove[]? moves = null, int maxChunkBorderFetch = 64)
        {
            _allMoves = moves ?? BuildDefaultMoves();
            _maxChunkBorderFetch = maxChunkBorderFetch;
        }

        public static IMove[] BuildDefaultMoves()
        {
            var moves = new List<IMove>();

            int[] offsets = [1, -1];
            foreach (int dx in offsets)
            {
                moves.Add(new MoveTraverse(dx, 0));
                moves.Add(new MoveAscend(dx, 0));
                moves.Add(new MoveDescend(dx, 0));
            }
            foreach (int dz in offsets)
            {
                moves.Add(new MoveTraverse(0, dz));
                moves.Add(new MoveAscend(0, dz));
                moves.Add(new MoveDescend(0, dz));
            }

            moves.Add(new MoveDiagonal(1, 1));
            moves.Add(new MoveDiagonal(1, -1));
            moves.Add(new MoveDiagonal(-1, 1));
            moves.Add(new MoveDiagonal(-1, -1));

            moves.Add(new MoveClimb(true));
            moves.Add(new MoveClimb(false));

            return [.. moves];
        }

        public PathResult Calculate(
            CalculationContext ctx,
            int startX, int startY, int startZ,
            IGoal goal,
            CancellationToken ct,
            long timeoutMs = 5000)
        {
            var sw = Stopwatch.StartNew();
            var openSet = new BinaryHeapOpenSet(4096);
            var nodeMap = new Dictionary<long, PathNode>(4096);

            var startNode = new PathNode(startX, startY, startZ)
            {
                GCost = 0,
                HCost = goal.Heuristic(startX, startY, startZ),
                IsOpen = true
            };
            openSet.Insert(startNode);
            nodeMap[startNode.PackedPosition] = startNode;

            int nodesExplored = 0;
            int unloadedChunkHits = 0;
            PathNode? bestPartialNode = startNode;
            double bestPartialScore = startNode.HCost + startNode.GCost * 0.5;
            MoveResult moveResult = default;

            DebugLog?.Invoke($"[A*] Start ({startX},{startY},{startZ}), goal={goal}");

            while (openSet.Count > 0)
            {
                if (ct.IsCancellationRequested)
                {
                    DebugLog?.Invoke($"[A*] Cancelled after {nodesExplored} nodes, {sw.ElapsedMilliseconds}ms");
                    break;
                }

                if (sw.ElapsedMilliseconds > timeoutMs)
                {
                    DebugLog?.Invoke($"[A*] Timeout ({timeoutMs}ms) after {nodesExplored} nodes");
                    break;
                }

                var current = openSet.RemoveMin();
                current.IsClosed = true;
                nodesExplored++;

                if (goal.IsInGoal(current.X, current.Y, current.Z))
                {
                    DebugLog?.Invoke($"[A*] Goal reached! {nodesExplored} nodes, {sw.ElapsedMilliseconds}ms");
                    var path = ReconstructPath(current);
                    return new PathResult(PathStatus.Success, path, nodesExplored, sw.ElapsedMilliseconds);
                }

                foreach (var move in _allMoves)
                {
                    moveResult.Cost = 0;
                    move.Calculate(ctx, current.X, current.Y, current.Z, ref moveResult);

                    if (moveResult.IsImpossible)
                        continue;

                    int nx = moveResult.DestX;
                    int ny = moveResult.DestY;
                    int nz = moveResult.DestZ;

                    if (!ctx.IsChunkLoaded(nx, nz))
                    {
                        unloadedChunkHits++;
                        if (unloadedChunkHits > _maxChunkBorderFetch)
                            continue;
                    }

                    double tentativeG = current.GCost + moveResult.Cost;
                    long packed = PathNode.Pack(nx, ny, nz);

                    if (nodeMap.TryGetValue(packed, out var neighbor))
                    {
                        if (neighbor.IsClosed)
                            continue;
                        if (tentativeG >= neighbor.GCost)
                            continue;

                        neighbor.GCost = tentativeG;
                        neighbor.Parent = current;
                        neighbor.MoveUsed = move.Type;
                        if (neighbor.IsOpen)
                            openSet.Update(neighbor);
                    }
                    else
                    {
                        neighbor = new PathNode(nx, ny, nz)
                        {
                            GCost = tentativeG,
                            HCost = goal.Heuristic(nx, ny, nz),
                            Parent = current,
                            MoveUsed = move.Type,
                            IsOpen = true
                        };
                        nodeMap[packed] = neighbor;
                        openSet.Insert(neighbor);
                    }

                    double partialScore = neighbor.HCost + neighbor.GCost * 0.5;
                    if (partialScore < bestPartialScore)
                    {
                        bestPartialScore = partialScore;
                        bestPartialNode = neighbor;
                    }
                }
            }

            if (bestPartialNode is not null && bestPartialNode != startNode)
            {
                DebugLog?.Invoke($"[A*] Partial path to ({bestPartialNode.X},{bestPartialNode.Y},{bestPartialNode.Z}), " +
                    $"{nodesExplored} nodes, {sw.ElapsedMilliseconds}ms");
                var path = ReconstructPath(bestPartialNode);
                return new PathResult(PathStatus.Partial, path, nodesExplored, sw.ElapsedMilliseconds);
            }

            DebugLog?.Invoke($"[A*] Failed, {nodesExplored} nodes, {sw.ElapsedMilliseconds}ms");
            return PathResult.Fail(nodesExplored, sw.ElapsedMilliseconds);
        }

        private static List<PathNode> ReconstructPath(PathNode end)
        {
            var path = new List<PathNode>();
            var current = end;
            while (current is not null)
            {
                path.Add(current);
                current = current.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}
