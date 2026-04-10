using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Moves;

namespace MinecraftClient.Pathing.Core
{
    /// <summary>
    /// Thread-safe snapshot of world state and player capabilities for path planning.
    /// Created once at the start of a search; all move calculations read from this.
    /// </summary>
    public sealed class CalculationContext
    {
        public World World { get; }
        public bool CanSprint { get; }
        public bool AllowParkour { get; }
        public bool AllowParkourAscend { get; }
        public bool AllowDiagonalDescend { get; }
        public int MaxFallHeight { get; }
        public double JumpPenalty { get; }
        public double WalkCost { get; }
        public double SprintCost { get; }
        public double SneakCost { get; }

        public CalculationContext(
            World world,
            bool canSprint = true,
            bool allowParkour = false,
            bool allowParkourAscend = false,
            bool allowDiagonalDescend = true,
            int maxFallHeight = 3,
            double jumpPenalty = ActionCosts.JumpPenalty)
        {
            World = world;
            CanSprint = canSprint;
            AllowParkour = allowParkour;
            AllowParkourAscend = allowParkourAscend;
            AllowDiagonalDescend = allowDiagonalDescend;
            MaxFallHeight = maxFallHeight;
            JumpPenalty = jumpPenalty;
            WalkCost = ActionCosts.WalkOneBlock;
            SprintCost = CanSprint ? ActionCosts.SprintOneBlock : ActionCosts.WalkOneBlock;
            SneakCost = ActionCosts.SneakOneBlock;
        }

        public Block GetBlock(int x, int y, int z)
            => World.GetBlock(new Location(x, y, z));

        public Material GetMaterial(int x, int y, int z)
            => GetBlock(x, y, z).Type;

        public bool CanWalkThrough(int x, int y, int z)
            => MoveHelper.CanWalkThrough(this, x, y, z);

        public bool CanWalkOn(int x, int y, int z)
            => MoveHelper.CanWalkOn(this, x, y, z);

        public bool IsFullyPassable(int x, int y, int z)
            => MoveHelper.IsFullyPassable(this, x, y, z);

        public bool IsChunkLoaded(int x, int z)
        {
            int cx = x >> 4;
            int cz = z >> 4;
            var col = World[cx, cz];
            return col is not null && col.FullyLoaded;
        }
    }
}
