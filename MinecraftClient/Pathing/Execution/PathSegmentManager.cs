using System;
using System.Threading;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution.Telemetry;
using MinecraftClient.Pathing.Goals;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution
{
    /// <summary>
    /// Top-level navigation controller. Holds a PathExecutor, monitors its progress,
    /// and triggers replanning on failure or deviation.
    /// </summary>
    public sealed class PathSegmentManager
    {
        private PathExecutor? _executor;
        private IGoal? _goal;
        private int _replanCount;
        private const int MaxReplans = 5;

        private readonly Action<string>? _debugLog;
        private readonly Action<string>? _infoLog;
        private readonly IPathExecutionObserver? _observer;

        public bool IsNavigating => _executor is not null && !_executor.IsComplete;
        public int ReplanCount => _replanCount;
        public IGoal? Goal => _goal;

        public PathSegmentManager(Action<string>? debugLog = null, Action<string>? infoLog = null, IPathExecutionObserver? observer = null)
        {
            _debugLog = debugLog;
            _infoLog = infoLog;
            _observer = observer;
        }

        public void StartNavigation(IGoal goal, PathResult result)
        {
            _goal = goal;
            _replanCount = 0;
            if (result.Status == PathStatus.Failed || result.Path.Count < 2)
            {
                _infoLog?.Invoke("[PathMgr] Navigation rejected -- no path found.");
                _executor = null;
                _goal = null;
                return;
            }

            var segments = PathSegmentBuilder.FromPath(result.Path);
            _executor = new PathExecutor(segments, _debugLog, _observer);
            _infoLog?.Invoke($"[PathMgr] Navigation started: {segments.Count} segments");
        }

        public void Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            if (_executor is null)
                return;

            var state = _executor.Tick(pos, physics, input, world);

            switch (state)
            {
                case PathExecutorState.Complete:
                    if (_goal is not null)
                    {
                        int px = (int)Math.Floor(pos.X);
                        int py = (int)Math.Floor(pos.Y);
                        int pz = (int)Math.Floor(pos.Z);
                        if (!_goal.IsInGoal(px, py, pz))
                        {
                            _infoLog?.Invoke("[PathMgr] Planned route ended before reaching goal, replanning...");
                            Replan(pos, world);
                            break;
                        }
                    }

                    _observer?.OnNavigationCompleted(_executor.TotalTicks);
                    _infoLog?.Invoke("[PathMgr] Navigation complete!");
                    _executor = null;
                    _goal = null;
                    break;

                case PathExecutorState.Failed:
                    _infoLog?.Invoke("[PathMgr] Segment failed, replanning...");
                    Replan(pos, world);
                    break;
            }
        }

        public void Cancel()
        {
            if (_executor is not null)
            {
                _infoLog?.Invoke("[PathMgr] Navigation cancelled.");
                _executor = null;
                _goal = null;
            }
        }

        private void Replan(Location pos, World world)
        {
            _replanCount++;
            _observer?.OnReplanStarted(_replanCount, pos);
            if (_replanCount > MaxReplans)
            {
                _observer?.OnReplanFailed(_replanCount, pos);
                _infoLog?.Invoke($"[PathMgr] Giving up after {MaxReplans} replans.");
                _executor = null;
                _goal = null;
                return;
            }

            if (_goal is null)
            {
                _executor = null;
                return;
            }

            _debugLog?.Invoke($"[PathMgr] Replan #{_replanCount} from ({pos.X:F2},{pos.Y:F2},{pos.Z:F2})");

            var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
            var finder = new AStarPathFinder();
            finder.DebugLog = _debugLog;

            int sx = (int)Math.Floor(pos.X);
            int sy = (int)Math.Floor(pos.Y);
            int sz = (int)Math.Floor(pos.Z);

            if (!ctx.CanWalkThrough(sx, sy, sz) && ctx.CanWalkThrough(sx, sy + 1, sz))
                sy++;

            using var cts = new CancellationTokenSource();
            var result = finder.Calculate(ctx, sx, sy, sz, _goal, cts.Token, 3000);

            bool alreadyInGoal = _goal.IsInGoal(sx, sy, sz)
                || (result.Path.Count == 1 && _goal.IsInGoal(result.Path[0].X, result.Path[0].Y, result.Path[0].Z));
            if (alreadyInGoal)
            {
                _infoLog?.Invoke("[PathMgr] Navigation complete!");
                _executor = null;
                _goal = null;
                return;
            }

            if (result.Status == PathStatus.Failed || result.Path.Count < 2)
            {
                _observer?.OnReplanFailed(_replanCount, pos);
                _infoLog?.Invoke("[PathMgr] Replan failed -- no path found.");
                _executor = null;
                _goal = null;
                return;
            }

            var segments = PathSegmentBuilder.FromPath(result.Path);
            _observer?.OnReplanSucceeded(_replanCount, segments);
            _executor = new PathExecutor(segments, _debugLog, _observer);
            _infoLog?.Invoke($"[PathMgr] Replanned: {segments.Count} segments (replan #{_replanCount})");
        }
    }
}
