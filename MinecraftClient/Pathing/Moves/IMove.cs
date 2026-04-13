using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves
{
    /// <summary>
    /// Represents one type of movement action for path planning.
    /// Each implementation defines its spatial check pattern and cost model.
    /// </summary>
    public interface IMove
    {
        MoveType Type { get; }
        int XOffset { get; }
        int ZOffset { get; }
        bool DynamicY { get; }

        void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result);
    }
}
