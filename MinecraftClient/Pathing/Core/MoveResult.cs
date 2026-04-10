namespace MinecraftClient.Pathing.Core
{
    /// <summary>
    /// Result of an IMove.Calculate() call. Mutable struct passed by ref for zero-alloc hot path.
    /// </summary>
    public struct MoveResult
    {
        public int DestX;
        public int DestY;
        public int DestZ;
        public double Cost;

        public void Set(int x, int y, int z, double cost)
        {
            DestX = x;
            DestY = y;
            DestZ = z;
            Cost = cost;
        }

        public void SetImpossible()
        {
            Cost = ActionCosts.CostInf;
        }

        public readonly bool IsImpossible => Cost >= ActionCosts.CostInf;
    }
}
