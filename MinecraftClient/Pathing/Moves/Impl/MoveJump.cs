using System;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Unified jump-family move. A single IMove implementation that dispatches
    /// on <see cref="JumpFlavor"/> to cover every combination previously
    /// implemented by seven separate classes (Traverse, Diagonal, Ascend,
    /// DiagonalAscend, DiagonalDescend, Parkour, SidewallParkour).
    ///
    /// All feasibility and cost logic lives in <see cref="JumpFeasibility"/>.
    /// Factory helpers (<see cref="Traverse"/>, <see cref="Parkour"/>, ...)
    /// produce the right descriptor for each use site without requiring
    /// callers to remember the mapping between flavor and <see cref="MoveType"/>.
    /// </summary>
    public sealed class MoveJump : IMove
    {
        public JumpDescriptor Descriptor { get; }
        public MoveType Type { get; }
        public int XOffset => Descriptor.XOffset;
        public int ZOffset => Descriptor.ZOffset;
        public int YDelta => Descriptor.YDelta;
        public JumpFlavor Flavor => Descriptor.Flavor;
        public bool DynamicY => false;

        public MoveJump(JumpDescriptor descriptor)
        {
            Descriptor = descriptor;
            Type = DeriveMoveType(descriptor);
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
            => JumpFeasibility.Evaluate(ctx, x, y, z, Descriptor, ref result);

        public override string ToString()
        {
            double horiz = Math.Sqrt((double)(XOffset * XOffset + ZOffset * ZOffset));
            return $"MoveJump({Flavor}, off=({XOffset},{ZOffset}), dy={YDelta}, dist={horiz:F2})";
        }

        // -----------------------------------------------------------------
        //   Factory helpers
        // -----------------------------------------------------------------

        public static MoveJump Traverse(int dx, int dz)
            => new(new JumpDescriptor(dx, dz, 0, JumpFlavor.Walk));

        public static MoveJump Diagonal(int dx, int dz)
            => new(new JumpDescriptor(dx, dz, 0, JumpFlavor.Walk));

        public static MoveJump Ascend(int dx, int dz)
            => new(new JumpDescriptor(dx, dz, 1, JumpFlavor.Step));

        public static MoveJump DiagonalAscend(int dx, int dz)
            => new(new JumpDescriptor(dx, dz, 1, JumpFlavor.Step));

        public static MoveJump DiagonalDescend(int dx, int dz)
            => new(new JumpDescriptor(dx, dz, -1, JumpFlavor.Step));

        public static MoveJump Parkour(int dx, int dz, int yDelta = 0)
            => new(new JumpDescriptor(dx, dz, yDelta, JumpFlavor.SprintJump));

        public static MoveJump Sidewall(int dx, int dz, int yDelta = 0)
            => new(new JumpDescriptor(dx, dz, yDelta, JumpFlavor.Sidewall));

        private static MoveType DeriveMoveType(JumpDescriptor d)
        {
            return d.Flavor switch
            {
                JumpFlavor.Walk => d.IsCardinal ? MoveType.Traverse : MoveType.Diagonal,
                JumpFlavor.Step => d.YDelta > 0 ? MoveType.Ascend : MoveType.Descend,
                JumpFlavor.SprintJump => MoveType.Parkour,
                JumpFlavor.Sidewall => MoveType.Parkour,
                _ => MoveType.Traverse,
            };
        }
    }
}
