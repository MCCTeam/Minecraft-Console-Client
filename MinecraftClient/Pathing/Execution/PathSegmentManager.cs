using System;
using System.Threading;
using System.Threading.Tasks;
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
    ///
    /// Replans and look-ahead plans run on a background Task (Baritone equivalent:
    /// findPathInNewThread). The main tick only reads task state: either applies a
    /// completed plan or installs the next one. A look-ahead plan is speculatively
    /// started as the current executor nears the end of its segment list so that the
    /// next executor is ready to splice in without a user-visible pause.
    /// </summary>
    public sealed class PathSegmentManager
    {
        private const int MaxReplans = 5;
        private const int ReplanTimeoutMs = 3000;
        private const int LookaheadTriggerSegmentsRemaining = 2;

        private readonly Action<string>? _debugLog;
        private readonly Action<string>? _infoLog;
        private readonly IPathExecutionObserver? _observer;

        private PathExecutor? _executor;
        private PathExecutor? _nextExecutor;
        private IGoal? _goal;
        private int _replanCount;

        private Task<PathResult>? _pendingReplan;
        private CancellationTokenSource? _pendingReplanCts;
        private Task<PathResult>? _pendingLookahead;
        private CancellationTokenSource? _pendingLookaheadCts;
        private (int x, int y, int z)? _pendingLookaheadAnchor;

        public bool IsNavigating =>
            (_executor is not null && !_executor.IsComplete)
            || _nextExecutor is not null
            || _pendingReplan is not null;

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
            CancelPendingTasks();
            _nextExecutor = null;
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
            DrainPendingReplan(pos, world);
            DrainPendingLookahead();

            if (_executor is null)
            {
                // Navigation is still alive if we are waiting on a background plan to
                // come back. The next tick will promote it into _executor.
                if (_pendingReplan is not null || _nextExecutor is not null)
                {
                    TryPromoteNextExecutor();
                    input.Reset();
                    return;
                }

                return;
            }

            var state = _executor.Tick(pos, physics, input, world);

            switch (state)
            {
                case PathExecutorState.Complete:
                    HandleExecutorComplete(pos, world, input);
                    break;

                case PathExecutorState.Failed:
                    _infoLog?.Invoke("[PathMgr] Segment failed, replanning...");
                    // The prepared next path assumes we finished the current segment
                    // cleanly, so drop it when we fail.
                    DiscardLookahead();
                    _nextExecutor = null;
                    StartReplanAsync(pos, world);
                    break;

                case PathExecutorState.InProgress:
                    MaybeStartLookahead(world);
                    break;
            }
        }

        public void Cancel()
        {
            if (_executor is not null || _pendingReplan is not null || _nextExecutor is not null)
            {
                _infoLog?.Invoke("[PathMgr] Navigation cancelled.");
            }

            CancelPendingTasks();
            _executor = null;
            _nextExecutor = null;
            _goal = null;
        }

        private void HandleExecutorComplete(Location pos, World world, MovementInput input)
        {
            if (_goal is not null)
            {
                int px = (int)Math.Floor(pos.X);
                int py = (int)Math.Floor(pos.Y);
                int pz = (int)Math.Floor(pos.Z);
                if (!_goal.IsInGoal(px, py, pz))
                {
                    if (TryPromoteNextExecutor())
                    {
                        _debugLog?.Invoke("[PathMgr] Spliced to prepared next segment chain.");
                        return;
                    }

                    _infoLog?.Invoke("[PathMgr] Planned route ended before reaching goal, replanning...");
                    StartReplanAsync(pos, world);
                    input.Reset();
                    return;
                }
            }

            _observer?.OnNavigationCompleted(_executor!.TotalTicks);
            _infoLog?.Invoke("[PathMgr] Navigation complete!");
            CancelPendingTasks();
            _executor = null;
            _nextExecutor = null;
            _goal = null;
        }

        private bool TryPromoteNextExecutor()
        {
            if (_nextExecutor is null)
                return false;

            _executor = _nextExecutor;
            _nextExecutor = null;
            return true;
        }

        private void StartReplanAsync(Location pos, World world)
        {
            _replanCount++;
            _observer?.OnReplanStarted(_replanCount, pos);
            if (_replanCount > MaxReplans)
            {
                _observer?.OnReplanFailed(_replanCount, pos);
                _infoLog?.Invoke($"[PathMgr] Giving up after {MaxReplans} replans.");
                CancelPendingTasks();
                _executor = null;
                _nextExecutor = null;
                _goal = null;
                return;
            }

            if (_goal is null)
            {
                _executor = null;
                return;
            }

            // Once we decide to replan, the currently-installed executor is discarded;
            // the new plan will replace it. We keep the executor reference until the
            // plan returns so IsNavigating reflects that work is in flight.
            if (_pendingReplan is not null)
                return;

            int sx = (int)Math.Floor(pos.X);
            int sy = (int)Math.Floor(pos.Y);
            int sz = (int)Math.Floor(pos.Z);

            var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
            if (!ctx.CanWalkThrough(sx, sy, sz) && ctx.CanWalkThrough(sx, sy + 1, sz))
                sy++;

            IGoal goal = _goal;
            _debugLog?.Invoke($"[PathMgr] Replan #{_replanCount} kicked off from ({pos.X:F2},{pos.Y:F2},{pos.Z:F2})");

            _pendingReplanCts = new CancellationTokenSource();
            CancellationToken token = _pendingReplanCts.Token;
            _pendingReplan = Task.Run(() =>
            {
                var finder = new AStarPathFinder { DebugLog = _debugLog };
                return finder.Calculate(ctx, sx, sy, sz, goal, token, ReplanTimeoutMs);
            }, token);

            // Clear the current executor so IsNavigating stays true via the pending
            // task branch. This prevents the main client from believing navigation
            // ended between "plan completes" and "plan gets installed".
            _executor = null;
        }

        private void DrainPendingReplan(Location pos, World world)
        {
            if (_pendingReplan is null)
                return;

            // When the current executor has been cleared (we are waiting for a plan
            // to install), give the background task a short budget to finish. The
            // player is standing still anyway, so trading a few ms of pause for
            // installing the new plan immediately is a clear win over letting the
            // tick return with _executor == null. This also makes tests with a
            // tight Tick poll loop deterministic: Task.Run needs the caller to
            // yield at some point so the thread-pool worker can complete.
            if (!_pendingReplan.IsCompleted && _executor is null && _nextExecutor is null)
            {
                try
                {
                    _pendingReplan.Wait(20);
                }
                catch
                {
                    // Exceptions are inspected via Task.IsFaulted below.
                }
            }

            if (!_pendingReplan.IsCompleted)
                return;

            Task<PathResult> task = _pendingReplan;
            _pendingReplan = null;
            var cts = _pendingReplanCts;
            _pendingReplanCts = null;
            cts?.Dispose();

            if (task.IsFaulted || task.IsCanceled)
            {
                _infoLog?.Invoke("[PathMgr] Replan task failed or was cancelled.");
                _observer?.OnReplanFailed(_replanCount, pos);
                _goal = null;
                _executor = null;
                _nextExecutor = null;
                return;
            }

            PathResult result = task.Result;

            int sx = (int)Math.Floor(pos.X);
            int sy = (int)Math.Floor(pos.Y);
            int sz = (int)Math.Floor(pos.Z);
            bool alreadyInGoal = _goal is not null && (_goal.IsInGoal(sx, sy, sz)
                || (result.Path.Count == 1 && _goal.IsInGoal(result.Path[0].X, result.Path[0].Y, result.Path[0].Z)));

            if (alreadyInGoal)
            {
                _infoLog?.Invoke("[PathMgr] Navigation complete!");
                _executor = null;
                _nextExecutor = null;
                _goal = null;
                return;
            }

            if (result.Status == PathStatus.Failed || result.Path.Count < 2)
            {
                _observer?.OnReplanFailed(_replanCount, pos);
                _infoLog?.Invoke("[PathMgr] Replan failed -- no path found.");
                _executor = null;
                _nextExecutor = null;
                _goal = null;
                return;
            }

            var segments = PathSegmentBuilder.FromPath(result.Path);
            _observer?.OnReplanSucceeded(_replanCount, segments);
            _executor = new PathExecutor(segments, _debugLog, _observer);
            _nextExecutor = null;
            _infoLog?.Invoke($"[PathMgr] Replanned: {segments.Count} segments (replan #{_replanCount})");
        }

        private void MaybeStartLookahead(World world)
        {
            if (_pendingLookahead is not null || _nextExecutor is not null || _goal is null || _executor is null)
                return;

            int total = _executor.TotalSegments;
            int current = _executor.CurrentIndex;
            if (total - current > LookaheadTriggerSegmentsRemaining)
                return;

            // Anchor the lookahead at the final segment's end; that is where execution
            // will arrive if the current executor finishes without drift.
            PathSegment? lastSegment = _executor.LastSegment;
            if (lastSegment is null)
                return;

            int ax = (int)Math.Floor(lastSegment.End.X);
            int ay = (int)Math.Floor(lastSegment.End.Y);
            int az = (int)Math.Floor(lastSegment.End.Z);

            if (_goal.IsInGoal(ax, ay, az))
                return;

            // Avoid re-planning from the same anchor repeatedly.
            if (_pendingLookaheadAnchor is { } prev && prev == (ax, ay, az))
                return;

            var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
            IGoal goal = _goal;
            _debugLog?.Invoke($"[PathMgr] Lookahead plan kicked off from ({ax},{ay},{az})");

            _pendingLookaheadCts = new CancellationTokenSource();
            CancellationToken token = _pendingLookaheadCts.Token;
            _pendingLookaheadAnchor = (ax, ay, az);
            _pendingLookahead = Task.Run(() =>
            {
                var finder = new AStarPathFinder { DebugLog = _debugLog };
                return finder.Calculate(ctx, ax, ay, az, goal, token, ReplanTimeoutMs);
            }, token);
        }

        private void DrainPendingLookahead()
        {
            if (_pendingLookahead is null || !_pendingLookahead.IsCompleted)
                return;

            Task<PathResult> task = _pendingLookahead;
            _pendingLookahead = null;
            var cts = _pendingLookaheadCts;
            _pendingLookaheadCts = null;
            cts?.Dispose();

            if (task.IsFaulted || task.IsCanceled)
                return;

            PathResult result = task.Result;
            if (result.Status == PathStatus.Failed || result.Path.Count < 2)
                return;

            // The lookahead should continue from the anchor point. If the live
            // executor is still running, splice the prepared plan as _nextExecutor
            // so it can take over without a wait.
            var segments = PathSegmentBuilder.FromPath(result.Path);
            _nextExecutor = new PathExecutor(segments, _debugLog, _observer);
            _debugLog?.Invoke($"[PathMgr] Lookahead ready: {segments.Count} segments spliced into _nextExecutor");
        }

        private void DiscardLookahead()
        {
            _pendingLookaheadCts?.Cancel();
            _pendingLookaheadCts?.Dispose();
            _pendingLookaheadCts = null;
            _pendingLookahead = null;
            _pendingLookaheadAnchor = null;
        }

        private void CancelPendingTasks()
        {
            _pendingReplanCts?.Cancel();
            _pendingReplanCts?.Dispose();
            _pendingReplanCts = null;
            _pendingReplan = null;

            DiscardLookahead();
        }

    }
}
