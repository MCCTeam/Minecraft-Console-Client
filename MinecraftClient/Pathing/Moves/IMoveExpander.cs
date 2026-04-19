using System;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves;

/// <summary>
/// A feasible neighbor emitted by an <see cref="IMoveExpander"/>. Carries the
/// per-node data the A* main loop needs to update its open set (destination,
/// cost, parkour profile, move type).
/// </summary>
public readonly struct MoveNeighbor
{
    public readonly int DestX;
    public readonly int DestY;
    public readonly int DestZ;
    public readonly double Cost;
    public readonly ParkourProfile ParkourProfile;
    public readonly MoveType MoveType;

    public MoveNeighbor(in MoveResult result, MoveType moveType)
    {
        DestX = result.DestX;
        DestY = result.DestY;
        DestZ = result.DestZ;
        Cost = result.Cost;
        ParkourProfile = result.ParkourProfile;
        MoveType = moveType;
    }
}

/// <summary>
/// Emits feasible neighbors from a given node. Replaces the old
/// "iterate every pre-instantiated IMove" pattern: A* asks each expander to
/// fill a stack-allocated buffer with all feasible neighbors, and the
/// expander can prune whole categories (e.g. skip Sidewall when no wall is
/// near) without instantiating per-direction IMove objects.
/// </summary>
public interface IMoveExpander
{
    /// <summary>
    /// Probe the world and populate <paramref name="buffer"/> with feasible
    /// neighbors. Returns the number of neighbors written. Implementations
    /// must not write past the buffer; callers must size it to the expander's
    /// max output.
    /// </summary>
    int Expand(CalculationContext ctx, int x, int y, int z, Span<MoveNeighbor> buffer);

    /// <summary>
    /// Upper bound on the number of neighbors this expander can emit from a
    /// single node. Used by the driver to size the per-node buffer.
    /// </summary>
    int MaxNeighbors { get; }
}
