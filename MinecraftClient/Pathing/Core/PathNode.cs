namespace MinecraftClient.Pathing.Core
{
    /// <summary>
    /// A* search node. Stored in the open/closed sets during pathfinding.
    /// </summary>
    public sealed class PathNode
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public double GCost;
        public double HCost;
        public double FCost => GCost + HCost;

        public PathNode? Parent;
        public MoveType MoveUsed;

        public int HeapIndex;
        public bool IsOpen;
        public bool IsClosed;

        public PathNode(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public long PackedPosition => Pack(X, Y, Z);

        public static long Pack(int x, int y, int z)
        {
            return ((long)(x + 30_000_000) << 36)
                 | ((long)(z + 30_000_000) << 12)
                 | (long)((y + 64) & 0xFFF);
        }
    }
}
