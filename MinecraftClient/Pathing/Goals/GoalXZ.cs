using System;

namespace MinecraftClient.Pathing.Goals
{
    public sealed class GoalXZ : IGoal
    {
        public int X { get; }
        public int Z { get; }

        public GoalXZ(int x, int z)
        {
            X = x;
            Z = z;
        }

        public bool IsInGoal(int x, int y, int z)
            => x == X && z == Z;

        public double Heuristic(int x, int y, int z)
        {
            int dx = Math.Abs(x - X);
            int dz = Math.Abs(z - Z);
            return GoalBlock.DistanceHeuristic(dx, 0, dz);
        }

        public override string ToString() => $"GoalXZ({X}, {Z})";
    }
}
