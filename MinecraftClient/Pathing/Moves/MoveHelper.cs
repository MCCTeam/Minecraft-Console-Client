using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves
{
    /// <summary>
    /// Block passability checks for path planning.
    /// Uses Material-level checks initially; designed to allow future BlockShapes upgrade.
    /// </summary>
    public static class MoveHelper
    {
        /// <summary>
        /// Can a player's body/head occupy this block position? (air, open door, tall grass, etc.)
        /// </summary>
        public static bool CanWalkThrough(CalculationContext ctx, int x, int y, int z)
        {
            Material mat = ctx.GetMaterial(x, y, z);
            if (mat == Material.Air || mat == Material.CaveAir || mat == Material.VoidAir)
                return true;
            if (mat.IsLiquid())
                return false;
            if (mat.CanBeClimbedOn())
                return true;
            if (IsOpenGate(mat))
                return true;
            if (mat.IsSolid())
                return false;
            if (mat.CanHarmPlayers())
                return false;
            return true;
        }

        /// <summary>
        /// Can a player stand on top of this block? (solid upper surface)
        /// </summary>
        public static bool CanWalkOn(CalculationContext ctx, int x, int y, int z)
        {
            Material mat = ctx.GetMaterial(x, y, z);
            if (mat == Material.Air || mat == Material.CaveAir || mat == Material.VoidAir)
                return false;
            if (mat.IsLiquid())
                return false;
            if (mat.CanHarmPlayers())
                return false;
            if (mat.CanBeClimbedOn())
                return false;
            if (IsOpenGate(mat))
                return false;
            return mat.IsSolid();
        }

        /// <summary>
        /// Is this block completely passable with no slowdown or interaction?
        /// Stricter than CanWalkThrough -- excludes water, cobwebs, etc.
        /// </summary>
        public static bool IsFullyPassable(CalculationContext ctx, int x, int y, int z)
        {
            Material mat = ctx.GetMaterial(x, y, z);
            return mat == Material.Air || mat == Material.CaveAir || mat == Material.VoidAir;
        }

        public static bool IsClimbable(Material mat)
        {
            return mat.CanBeClimbedOn();
        }

        public static bool IsHazardous(Material mat)
        {
            return mat.CanHarmPlayers();
        }

        public static bool IsWater(Material mat)
        {
            return mat == Material.Water;
        }

        /// <summary>
        /// Conservative check for gate-type blocks. Since we cannot read block state
        /// (open/closed) during planning, treat all fence gates as passable.
        /// </summary>
        private static bool IsOpenGate(Material mat)
        {
            return mat is Material.AcaciaFenceGate or Material.BirchFenceGate
                or Material.CrimsonFenceGate or Material.DarkOakFenceGate
                or Material.JungleFenceGate or Material.MangroveWood
                or Material.OakFenceGate or Material.SpruceFenceGate
                or Material.WarpedFenceGate or Material.CherryFenceGate
                or Material.BambooFenceGate or Material.PaleOakFenceGate;
        }
    }
}
