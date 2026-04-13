using System;

namespace MinecraftClient.Pathing.Goals
{
    public sealed class GoalNear : IGoal
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public int Range { get; }
        private readonly int _rangeSq;

        public GoalNear(int x, int y, int z, int range)
        {
            X = x;
            Y = y;
            Z = z;
            Range = range;
            _rangeSq = range * range;
        }

        public bool IsInGoal(int x, int y, int z)
        {
            int dx = x - X;
            int dy = y - Y;
            int dz = z - Z;
            return dx * dx + dy * dy + dz * dz <= _rangeSq;
        }

        public double Heuristic(int x, int y, int z)
        {
            int dx = Math.Abs(x - X);
            int dy = Math.Abs(y - Y);
            int dz = Math.Abs(z - Z);
            double h = GoalBlock.DistanceHeuristic(dx, dy, dz);
            double reduction = Range * Core.ActionCosts.SprintOneBlock;
            return Math.Max(0, h - reduction);
        }

        public override string ToString() => $"GoalNear({X}, {Y}, {Z}, range={Range})";
    }
}
