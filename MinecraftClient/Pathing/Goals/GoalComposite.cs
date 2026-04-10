using System;
using System.Collections.Generic;

namespace MinecraftClient.Pathing.Goals
{
    public sealed class GoalComposite : IGoal
    {
        private readonly IGoal[] _goals;

        public GoalComposite(params IGoal[] goals)
        {
            ArgumentNullException.ThrowIfNull(goals);
            _goals = goals;
        }

        public GoalComposite(IEnumerable<IGoal> goals)
        {
            ArgumentNullException.ThrowIfNull(goals);
            _goals = goals is IGoal[] arr ? arr : [.. goals];
        }

        public bool IsInGoal(int x, int y, int z)
        {
            foreach (var g in _goals)
            {
                if (g.IsInGoal(x, y, z))
                    return true;
            }
            return false;
        }

        public double Heuristic(int x, int y, int z)
        {
            double min = double.MaxValue;
            foreach (var g in _goals)
            {
                double h = g.Heuristic(x, y, z);
                if (h < min) min = h;
            }
            return min;
        }

        public override string ToString() => $"GoalComposite({_goals.Length} goals)";
    }
}
