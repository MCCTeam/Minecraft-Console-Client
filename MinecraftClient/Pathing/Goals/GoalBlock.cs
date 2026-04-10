using System;

namespace MinecraftClient.Pathing.Goals
{
    public sealed class GoalBlock : IGoal
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public GoalBlock(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool IsInGoal(int x, int y, int z)
            => x == X && y == Y && z == Z;

        public double Heuristic(int x, int y, int z)
        {
            int dx = Math.Abs(x - X);
            int dy = Math.Abs(y - Y);
            int dz = Math.Abs(z - Z);
            return DistanceHeuristic(dx, dy, dz);
        }

        internal static double DistanceHeuristic(int dx, int dy, int dz)
        {
            int horizontal = Math.Max(dx, dz);
            int diagonal = Math.Min(dx, dz);
            int straight = horizontal - diagonal;
            double cost = diagonal * Core.ActionCosts.SprintOneBlock * Core.ActionCosts.DiagonalMultiplier
                        + straight * Core.ActionCosts.SprintOneBlock
                        + Math.Abs(dy) * Core.ActionCosts.SprintOneBlock;
            return cost;
        }

        public override string ToString() => $"GoalBlock({X}, {Y}, {Z})";
    }
}
