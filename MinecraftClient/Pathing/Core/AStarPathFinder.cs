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
        private readonly record struct NodeKey(long PackedPosition, EntryPreparationState EntryPreparation);

        private readonly IMove[] _allMoves;
        private readonly IMoveExpander[] _expanders;
        private readonly int _totalExpanderCapacity;
        private readonly int _maxChunkBorderFetch;

        public Action<string>? DebugLog { get; set; }

        public AStarPathFinder(IMove[]? moves = null, int maxChunkBorderFetch = 64)
            : this(BuildExpanders(moves), moves ?? BuildDefaultMoves(), maxChunkBorderFetch)
        {
        }

        public AStarPathFinder(IMoveExpander[] expanders, int maxChunkBorderFetch = 64)
            : this(expanders, System.Array.Empty<IMove>(), maxChunkBorderFetch)
        {
        }

        private AStarPathFinder(IMoveExpander[] expanders, IMove[] allMoves, int maxChunkBorderFetch)
        {
            _expanders = expanders;
            _allMoves = allMoves;
            _maxChunkBorderFetch = maxChunkBorderFetch;

            int total = 0;
            for (int i = 0; i < expanders.Length; i++)
                total += expanders[i].MaxNeighbors;
            _totalExpanderCapacity = total;
        }

        private static IMoveExpander[] BuildExpanders(IMove[]? explicitMoves)
        {
            if (explicitMoves is null)
            {
                return BuildDefaultExpanders();
            }

            // Caller supplied a specific move set (e.g. tests). Wrap it as a
            // legacy expander so the old API keeps working.
            return [new LegacyMoveExpander(explicitMoves)];
        }

        public static IMoveExpander[] BuildDefaultExpanders()
        {
            IMove[] legacyMoves =
            [
                new MoveDescend(1, 0),
                new MoveDescend(-1, 0),
                new MoveDescend(0, 1),
                new MoveDescend(0, -1),
                new MoveSprintDescend(2, 0),
                new MoveSprintDescend(-2, 0),
                new MoveSprintDescend(0, 2),
                new MoveSprintDescend(0, -2),
                new MoveSprintDescend(1, 1),
                new MoveSprintDescend(1, -1),
                new MoveSprintDescend(-1, 1),
                new MoveSprintDescend(-1, -1),
                new MoveClimb(true),
                new MoveClimb(false),
                new MoveFall(),
            ];

            return [new JumpExpander(), new LegacyMoveExpander(legacyMoves)];
        }

        public static IMove[] BuildDefaultMoves()
        {
            var moves = new List<IMove>();

            int[] offsets = [1, -1];

            // ---- jump family (all unified as MoveJump with a JumpDescriptor) ----

            // Cardinal walk + 1-block ascend
            foreach (int dx in offsets)
            {
                moves.Add(MoveJump.Traverse(dx, 0));
                moves.Add(MoveJump.Ascend(dx, 0));
            }
            foreach (int dz in offsets)
            {
                moves.Add(MoveJump.Traverse(0, dz));
                moves.Add(MoveJump.Ascend(0, dz));
            }

            // Diagonal walk + diagonal ascend/descend (corner cases)
            foreach (int dx in offsets)
            {
                foreach (int dz in offsets)
                {
                    moves.Add(MoveJump.Diagonal(dx, dz));
                    moves.Add(MoveJump.DiagonalAscend(dx, dz));
                    moves.Add(MoveJump.DiagonalDescend(dx, dz));
                }
            }

            // Cardinal parkour (flat / +1 ascend / -1 -2 descend)
            foreach (int dx in offsets)
            {
                for (int dist = 2; dist <= 5; dist++)
                    moves.Add(MoveJump.Parkour(dx * dist, 0));
                for (int dist = 2; dist <= 3; dist++)
                    moves.Add(MoveJump.Parkour(dx * dist, 0, yDelta: 1));
                for (int dist = 2; dist <= 5; dist++)
                    moves.Add(MoveJump.Parkour(dx * dist, 0, yDelta: -1));
                for (int dist = 2; dist <= 5; dist++)
                    moves.Add(MoveJump.Parkour(dx * dist, 0, yDelta: -2));
            }
            foreach (int dz in offsets)
            {
                for (int dist = 2; dist <= 5; dist++)
                    moves.Add(MoveJump.Parkour(0, dz * dist));
                for (int dist = 2; dist <= 3; dist++)
                    moves.Add(MoveJump.Parkour(0, dz * dist, yDelta: 1));
                for (int dist = 2; dist <= 5; dist++)
                    moves.Add(MoveJump.Parkour(0, dz * dist, yDelta: -1));
                for (int dist = 2; dist <= 5; dist++)
                    moves.Add(MoveJump.Parkour(0, dz * dist, yDelta: -2));
            }

            // Diagonal parkour (flat + diagonal ascending/descending)
            foreach (int dx in offsets)
            {
                foreach (int dz in offsets)
                {
                    moves.Add(MoveJump.Parkour(dx * 2, dz * 1));
                    moves.Add(MoveJump.Parkour(dx * 1, dz * 2));
                    moves.Add(MoveJump.Parkour(dx * 2, dz * 2));
                    moves.Add(MoveJump.Parkour(dx * 3, dz * 1));
                    moves.Add(MoveJump.Parkour(dx * 1, dz * 3));

                    moves.Add(MoveJump.Parkour(dx * 2, dz * 1, yDelta: -1));
                    moves.Add(MoveJump.Parkour(dx * 1, dz * 2, yDelta: -1));
                    moves.Add(MoveJump.Parkour(dx * 2, dz * 2, yDelta: -1));

                    moves.Add(MoveJump.Parkour(dx * 2, dz * 1, yDelta: 1));
                    moves.Add(MoveJump.Parkour(dx * 1, dz * 2, yDelta: 1));
                    moves.Add(MoveJump.Parkour(dx * 2, dz * 2, yDelta: 1));
                }
            }

            // Sidewall parkour (dominant-axis sprint jumps using an inner wall)
            foreach (int dx in offsets)
            {
                foreach (int dz in offsets)
                {
                    foreach (int distance in new[] { 2, 3, 4, 5 })
                    {
                        moves.Add(MoveJump.Sidewall(dx, dz * distance));
                        moves.Add(MoveJump.Sidewall(dx * distance, dz));

                        if (distance <= 3)
                        {
                            moves.Add(MoveJump.Sidewall(dx, dz * distance, yDelta: 1));
                            moves.Add(MoveJump.Sidewall(dx * distance, dz, yDelta: 1));
                        }

                        moves.Add(MoveJump.Sidewall(dx, dz * distance, yDelta: -1));
                        moves.Add(MoveJump.Sidewall(dx * distance, dz, yDelta: -1));
                        moves.Add(MoveJump.Sidewall(dx, dz * distance, yDelta: -2));
                        moves.Add(MoveJump.Sidewall(dx * distance, dz, yDelta: -2));
                    }
                }
            }

            // ---- dynamic-landing family (kept separate: variable landing depth) ----
            foreach (int dx in offsets)
            {
                moves.Add(new MoveDescend(dx, 0));
                moves.Add(new MoveSprintDescend(dx * 2, 0));
                moves.Add(new MoveSprintDescend(dx, dx));
                moves.Add(new MoveSprintDescend(dx, -dx));
            }
            foreach (int dz in offsets)
            {
                moves.Add(new MoveDescend(0, dz));
                moves.Add(new MoveSprintDescend(0, dz * 2));
            }

            // ---- vertical / free fall ----
            moves.Add(new MoveClimb(true));
            moves.Add(new MoveClimb(false));
            moves.Add(new MoveFall());

            return [.. moves];
        }

        public PathResult Calculate(
            CalculationContext ctx,
            int startX, int startY, int startZ,
            IGoal goal,
            CancellationToken ct,
            long timeoutMs = 5000)
        {
            if (goal.IsInGoal(startX, startY, startZ))
            {
                DebugLog?.Invoke($"[A*] Already in goal at ({startX},{startY},{startZ})");
                return new PathResult(
                    PathStatus.Success,
                    [new PathNode(startX, startY, startZ)],
                    nodesExplored: 0,
                    elapsedMs: 0);
            }

            if (!IsGoalReachableFootPosition(ctx, goal))
            {
                DebugLog?.Invoke($"[A*] Goal {goal} is not a reachable foot position");
                return PathResult.Fail(nodesExplored: 0, elapsedMs: 0);
            }

            var sw = Stopwatch.StartNew();
            var openSet = new BinaryHeapOpenSet(4096);
            var nodeMap = new Dictionary<NodeKey, PathNode>(4096);

            var startNode = new PathNode(startX, startY, startZ)
            {
                GCost = 0,
                HCost = goal.Heuristic(startX, startY, startZ),
                IsOpen = true
            };
            openSet.Insert(startNode);
            nodeMap[new NodeKey(startNode.PackedPosition, startNode.EntryPreparation)] = startNode;

            int nodesExplored = 0;
            int unloadedChunkHits = 0;
            bool searchAborted = false;
            PathNode? bestPartialNode = startNode;
            double bestPartialScore = startNode.HCost + startNode.GCost * 0.5;

            // Per-node scratch buffer for IMoveExpander output. Size = sum of
            // MaxNeighbors across all expanders so no expander can overflow.
            Span<MoveNeighbor> neighborBuffer = _totalExpanderCapacity <= 512
                ? stackalloc MoveNeighbor[_totalExpanderCapacity]
                : new MoveNeighbor[_totalExpanderCapacity];

            DebugLog?.Invoke($"[A*] Start ({startX},{startY},{startZ}), goal={goal}");

            while (openSet.Count > 0)
            {
                if (ct.IsCancellationRequested)
                {
                    searchAborted = true;
                    DebugLog?.Invoke($"[A*] Cancelled after {nodesExplored} nodes, {sw.ElapsedMilliseconds}ms");
                    break;
                }

                if (sw.ElapsedMilliseconds > timeoutMs)
                {
                    searchAborted = true;
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

                ctx.PreviousMoveType = current.MoveUsed;
                ctx.CurrentEntryPreparation = current.EntryPreparation;

                int bufferOffset = 0;
                for (int ex = 0; ex < _expanders.Length; ex++)
                {
                    IMoveExpander expander = _expanders[ex];
                    Span<MoveNeighbor> slot = neighborBuffer.Slice(bufferOffset, expander.MaxNeighbors);
                    int produced = expander.Expand(ctx, current.X, current.Y, current.Z, slot);
                    bufferOffset += expander.MaxNeighbors;

                    for (int i = 0; i < produced; i++)
                    {
                        MoveNeighbor emitted = slot[i];
                        int nx = emitted.DestX;
                        int ny = emitted.DestY;
                        int nz = emitted.DestZ;

                        if (!ctx.IsChunkLoaded(nx, nz))
                        {
                            unloadedChunkHits++;
                            if (unloadedChunkHits > _maxChunkBorderFetch)
                                continue;
                        }

                        double tentativeG = current.GCost + emitted.Cost;
                        EntryPreparationState nextPreparation = ResolveEntryPreparation(
                            current, emitted.MoveType, emitted.DestX, emitted.DestY, emitted.DestZ);
                        var key = new NodeKey(PathNode.Pack(nx, ny, nz), nextPreparation);

                        if (nodeMap.TryGetValue(key, out var neighbor))
                        {
                            if (neighbor.IsClosed)
                                continue;
                            if (tentativeG >= neighbor.GCost)
                                continue;

                            neighbor.GCost = tentativeG;
                            neighbor.Parent = current;
                            neighbor.MoveUsed = emitted.MoveType;
                            neighbor.ParkourProfile = emitted.ParkourProfile;
                            neighbor.EntryPreparation = nextPreparation;
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
                                MoveUsed = emitted.MoveType,
                                ParkourProfile = emitted.ParkourProfile,
                                EntryPreparation = nextPreparation,
                                IsOpen = true
                            };
                            nodeMap[key] = neighbor;
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
            }

            if (bestPartialNode is not null
                && bestPartialNode != startNode
                && (searchAborted || unloadedChunkHits > 0))
            {
                DebugLog?.Invoke($"[A*] Partial path to ({bestPartialNode.X},{bestPartialNode.Y},{bestPartialNode.Z}), " +
                    $"{nodesExplored} nodes, {sw.ElapsedMilliseconds}ms");
                var path = ReconstructPath(bestPartialNode);
                return new PathResult(PathStatus.Partial, path, nodesExplored, sw.ElapsedMilliseconds);
            }

            DebugLog?.Invoke($"[A*] Failed, {nodesExplored} nodes, {sw.ElapsedMilliseconds}ms");
            return PathResult.Fail(nodesExplored, sw.ElapsedMilliseconds);
        }

        private EntryPreparationState ResolveEntryPreparation(
            PathNode current, MoveType moveType, int destX, int destY, int destZ)
        {
            EntryPreparationState advanced = AdvanceExistingPreparation(current, moveType, destX, destY, destZ);
            if (!advanced.IsNone)
                return advanced;

            if (TryStartSidewallRunupPreparation(current, moveType, destX, destY, destZ, out EntryPreparationState started))
                return started;

            return EntryPreparationState.None;
        }

        private static EntryPreparationState AdvanceExistingPreparation(
            PathNode current, MoveType moveType, int destX, int destY, int destZ)
        {
            EntryPreparationState state = current.EntryPreparation;
            if (state.IsNone)
                return EntryPreparationState.None;

            if (moveType != MoveType.Traverse || destY != current.Y)
                return EntryPreparationState.None;

            int stepX = destX - current.X;
            int stepZ = destZ - current.Z;

            if (state.BackwardSteps < state.RequiredSteps
                && stepX == -state.ForwardX
                && stepZ == -state.ForwardZ)
            {
                return state.AdvanceBackward();
            }

            if (state.BackwardSteps == state.RequiredSteps
                && state.ReturnSteps < state.RequiredSteps
                && stepX == state.ForwardX
                && stepZ == state.ForwardZ)
            {
                EntryPreparationState nextState = state.AdvanceReturn();
                if (nextState.IsPrepared
                    && (destX != state.OriginX
                        || destY != state.OriginY
                        || destZ != state.OriginZ))
                {
                    return EntryPreparationState.None;
                }

                return nextState;
            }

            return EntryPreparationState.None;
        }

        private static bool TryStartSidewallRunupPreparation(
            PathNode current, MoveType moveType, int destX, int destY, int destZ, out EntryPreparationState state)
        {
            state = EntryPreparationState.None;

            if (!current.EntryPreparation.IsNone
                || moveType != MoveType.Traverse
                || destY != current.Y)
            {
                return false;
            }

            int stepX = destX - current.X;
            int stepZ = destZ - current.Z;

            // Sidewall candidates are generated dynamically by JumpExpander's
            // cardinal probe now, so we probe each cardinal forward direction
            // directly instead of scanning a descriptor table. The only shape
            // TryGetRequiredStaticEntryRunupSteps flags as needing a static
            // runup today is (major=5, minor=1, yDelta=-1) -- we use that
            // canonical shape as the query (lateral=+1 is arbitrary; the
            // helper only looks at yDelta and major).
            ReadOnlySpan<(int fx, int fz)> forwards =
            [
                (1, 0),
                (-1, 0),
                (0, 1),
                (0, -1),
            ];

            for (int i = 0; i < forwards.Length; i++)
            {
                (int forwardX, int forwardZ) = forwards[i];
                if (stepX != -forwardX || stepZ != -forwardZ)
                    continue;

                int xOffset, zOffset;
                if (forwardX != 0)
                {
                    xOffset = forwardX * 5;
                    zOffset = 1;
                }
                else
                {
                    xOffset = 1;
                    zOffset = forwardZ * 5;
                }

                if (!ParkourFeasibility.TryGetRequiredStaticEntryRunupSteps(
                        current.MoveUsed,
                        xOffset,
                        zOffset,
                        yDelta: -1,
                        out int requiredSteps))
                {
                    continue;
                }

                state = new EntryPreparationState(
                    EntryPreparationKind.SidewallRunup,
                    current.X,
                    current.Y,
                    current.Z,
                    forwardX,
                    forwardZ,
                    (byte)requiredSteps,
                    BackwardSteps: 1,
                    ReturnSteps: 0);
                return true;
            }

            return false;
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

        private static bool IsGoalReachableFootPosition(CalculationContext ctx, IGoal goal)
        {
            if (goal is not GoalBlock blockGoal)
                return true;

            if (!ctx.IsChunkLoaded(blockGoal.X, blockGoal.Z))
                return true;

            if (blockGoal.Y == int.MinValue)
                return false;

            return ctx.CanWalkOn(blockGoal.X, blockGoal.Y - 1, blockGoal.Z)
                && ctx.CanWalkThrough(blockGoal.X, blockGoal.Y, blockGoal.Z)
                && ctx.CanWalkThrough(blockGoal.X, blockGoal.Y + 1, blockGoal.Z);
        }
    }
}
