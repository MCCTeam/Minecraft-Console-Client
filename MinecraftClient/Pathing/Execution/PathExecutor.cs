using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution
{
    public enum PathExecutorState
    {
        InProgress,
        Failed,
        Complete
    }

    /// <summary>
    /// Drives a sequence of PathSegments by instantiating the correct IActionTemplate
    /// for each segment and ticking it every game tick.
    /// </summary>
    public sealed class PathExecutor
    {
        private readonly List<PathSegment> _segments;
        private int _currentIndex;
        private IActionTemplate? _currentTemplate;
        private readonly Action<string>? _debugLog;

        public bool IsComplete => _currentIndex >= _segments.Count && _currentTemplate is null;
        public int CurrentIndex => _currentIndex;
        public int TotalSegments => _segments.Count;
        public PathSegment? CurrentSegment =>
            _currentIndex < _segments.Count ? _segments[_currentIndex] : null;

        public PathExecutor(List<PathSegment> segments, Action<string>? debugLog = null)
        {
            _segments = segments;
            _currentIndex = 0;
            _debugLog = debugLog;
            AdvanceToNextSegment();
        }

        public PathExecutorState Tick(Location pos, PlayerPhysics physics, MovementInput input)
        {
            if (_currentTemplate is null)
                return PathExecutorState.Complete;

            var state = _currentTemplate.Tick(pos, physics, input);

            switch (state)
            {
                case TemplateState.Complete:
                    _debugLog?.Invoke($"[PathExec] Segment {_currentIndex} complete " +
                        $"({_segments[_currentIndex].MoveType}) at ({pos.X:F2},{pos.Y:F2},{pos.Z:F2})");
                    _currentIndex++;
                    if (_currentIndex >= _segments.Count)
                    {
                        _currentTemplate = null;
                        _debugLog?.Invoke("[PathExec] All segments complete!");
                        return PathExecutorState.Complete;
                    }
                    AdvanceToNextSegment();
                    return PathExecutorState.InProgress;

                case TemplateState.Failed:
                    _debugLog?.Invoke($"[PathExec] Segment {_currentIndex} FAILED " +
                        $"({_segments[_currentIndex].MoveType}) at ({pos.X:F2},{pos.Y:F2},{pos.Z:F2}), " +
                        $"target was ({_currentTemplate.ExpectedEnd.X:F2},{_currentTemplate.ExpectedEnd.Y:F2},{_currentTemplate.ExpectedEnd.Z:F2})");
                    return PathExecutorState.Failed;

                default:
                    return PathExecutorState.InProgress;
            }
        }

        private void AdvanceToNextSegment()
        {
            if (_currentIndex < _segments.Count)
            {
                var seg = _segments[_currentIndex];
                _currentTemplate = ActionTemplateFactory.Create(seg);
                _debugLog?.Invoke($"[PathExec] Starting segment {_currentIndex}/{_segments.Count}: {seg}");
            }
            else
            {
                _currentTemplate = null;
            }
        }
    }
}
