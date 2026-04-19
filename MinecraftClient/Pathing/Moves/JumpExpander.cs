using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves.Impl;

namespace MinecraftClient.Pathing.Moves;

/// <summary>
/// Dynamic expander for every move in the jump family (Walk, Step,
/// SprintJump, Sidewall). Iterates a declarative descriptor table and calls
/// <see cref="JumpFeasibility.Evaluate"/> for each entry without allocating
/// an IMove object per direction.
///
/// The hot path hoists per-node guards (AllowParkour, head clearance,
/// takeoff material, adjacent-wall presence) and precomputes an 8-direction
/// "first-step has no floor" table so entire descriptor groups can be
/// rejected in O(1) before touching <see cref="JumpFeasibility"/>. Ordinary
/// ground-walking nodes skip all ~170 jump descriptors this way; nodes
/// without any adjacent wall skip all 112 sidewall descriptors.
/// </summary>
public sealed class JumpExpander : IMoveExpander
{
    private static readonly JumpDescriptor[] _descriptors = BuildDescriptors();

    public int MaxNeighbors => _descriptors.Length;

    public int Expand(CalculationContext ctx, int x, int y, int z, Span<MoveNeighbor> buffer)
    {
        int count = 0;
        MoveResult result = default;

        // ---- Per-node preconditions (shared by every SprintJump + Sidewall descriptor) ----
        // These are the first checks JumpFeasibility.Evaluate* would make. Hoisting
        // them once turns ~170 method calls per node into one branch in the hot path.
        bool jumpFamilyAllowed = ctx.AllowParkour && ctx.CanSprint;
        bool canSprintTakeoff = false;
        bool hasAdjacentWall = false;
        if (jumpFamilyAllowed)
        {
            Material standingOn = ctx.GetMaterial(x, y - 1, z);
            Material atFeet = ctx.GetMaterial(x, y, z);
            canSprintTakeoff =
                !standingOn.CanBeClimbedOn()
                && !atFeet.IsLiquid()
                && ctx.CanWalkThrough(x, y + 2, z);

            if (canSprintTakeoff)
                hasAdjacentWall = HasAnyAdjacentWall(ctx, x, y, z);
        }

        // ---- Per-direction gap table (SprintJump only) ----
        // Gap check: "first block adjacent to start must lack ground" so A* can't
        // pick a cheaper walking path. For an octant (sx, sz) the cell is at
        // (x+sx, y-1, z+sz). Index = (sx+1)*3 + (sz+1) over sx,sz in {-1,0,1}.
        // If the floor is present for a direction, every SprintJump descriptor in
        // that octant is infeasible. 9 slots (center slot 4 unused) fit cleanly
        // on the stack.
        Span<bool> directionGapOpen = stackalloc bool[9];
        if (canSprintTakeoff)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;
                    int idx = ((dx + 1) * 3) + (dz + 1);
                    directionGapOpen[idx] = !ctx.CanWalkOn(x + dx, y - 1, z + dz);
                }
            }
        }

        for (int i = 0; i < _descriptors.Length; i++)
        {
            JumpDescriptor desc = _descriptors[i];

            switch (desc.Flavor)
            {
                case JumpFlavor.SprintJump:
                    if (!canSprintTakeoff)
                        continue;
                    {
                        int sx = Math.Sign(desc.XOffset);
                        int sz = Math.Sign(desc.ZOffset);
                        int idx = ((sx + 1) * 3) + (sz + 1);
                        if (!directionGapOpen[idx])
                            continue;
                    }
                    break;
                case JumpFlavor.Sidewall:
                    if (!canSprintTakeoff || !hasAdjacentWall)
                        continue;
                    break;
                default:
                    break;
            }

            result.Cost = 0;
            JumpFeasibility.Evaluate(ctx, x, y, z, desc, ref result);
            if (result.IsImpossible)
                continue;

            MoveType type = DeriveMoveType(desc);
            if (count < buffer.Length)
                buffer[count++] = new MoveNeighbor(result, type);
        }
        return count;
    }

    /// <summary>
    /// Conservative O(1) short-circuit for the Sidewall family. Every sidewall
    /// descriptor needs a solid block one lateral step from the takeoff at
    /// <c>y</c> or <c>y+1</c>, i.e. at a cardinal neighbor. If all four
    /// cardinal neighbors at both heights are walk-through, there is no wall
    /// to cling to and all 112 sidewall descriptors can be skipped without
    /// calling <see cref="JumpFeasibility"/>.
    /// </summary>
    private static bool HasAnyAdjacentWall(CalculationContext ctx, int x, int y, int z)
    {
        return !ctx.CanWalkThrough(x + 1, y, z) || !ctx.CanWalkThrough(x + 1, y + 1, z)
            || !ctx.CanWalkThrough(x - 1, y, z) || !ctx.CanWalkThrough(x - 1, y + 1, z)
            || !ctx.CanWalkThrough(x, y, z + 1) || !ctx.CanWalkThrough(x, y + 1, z + 1)
            || !ctx.CanWalkThrough(x, y, z - 1) || !ctx.CanWalkThrough(x, y + 1, z - 1);
    }

    private static MoveType DeriveMoveType(JumpDescriptor d) => d.Flavor switch
    {
        JumpFlavor.Walk => d.IsCardinal ? MoveType.Traverse : MoveType.Diagonal,
        JumpFlavor.Step => d.YDelta > 0 ? MoveType.Ascend : MoveType.Descend,
        JumpFlavor.SprintJump => MoveType.Parkour,
        JumpFlavor.Sidewall => MoveType.Parkour,
        _ => MoveType.Traverse,
    };

    private static JumpDescriptor[] BuildDescriptors()
    {
        var list = new System.Collections.Generic.List<JumpDescriptor>(256);
        int[] offsets = [1, -1];

        // Cardinal walk + 1-block ascend
        foreach (int dx in offsets)
        {
            list.Add(new JumpDescriptor(dx, 0, 0, JumpFlavor.Walk));
            list.Add(new JumpDescriptor(dx, 0, 1, JumpFlavor.Step));
        }
        foreach (int dz in offsets)
        {
            list.Add(new JumpDescriptor(0, dz, 0, JumpFlavor.Walk));
            list.Add(new JumpDescriptor(0, dz, 1, JumpFlavor.Step));
        }

        // Diagonal walk + diagonal ascend/descend
        foreach (int dx in offsets)
        {
            foreach (int dz in offsets)
            {
                list.Add(new JumpDescriptor(dx, dz, 0, JumpFlavor.Walk));
                list.Add(new JumpDescriptor(dx, dz, 1, JumpFlavor.Step));
                list.Add(new JumpDescriptor(dx, dz, -1, JumpFlavor.Step));
            }
        }

        // Cardinal parkour (flat / +1 / -1 / -2)
        foreach (int dx in offsets)
        {
            for (int d = 2; d <= 5; d++)
                list.Add(new JumpDescriptor(dx * d, 0, 0, JumpFlavor.SprintJump));
            for (int d = 2; d <= 3; d++)
                list.Add(new JumpDescriptor(dx * d, 0, 1, JumpFlavor.SprintJump));
            for (int d = 2; d <= 5; d++)
                list.Add(new JumpDescriptor(dx * d, 0, -1, JumpFlavor.SprintJump));
            for (int d = 2; d <= 5; d++)
                list.Add(new JumpDescriptor(dx * d, 0, -2, JumpFlavor.SprintJump));
        }
        foreach (int dz in offsets)
        {
            for (int d = 2; d <= 5; d++)
                list.Add(new JumpDescriptor(0, dz * d, 0, JumpFlavor.SprintJump));
            for (int d = 2; d <= 3; d++)
                list.Add(new JumpDescriptor(0, dz * d, 1, JumpFlavor.SprintJump));
            for (int d = 2; d <= 5; d++)
                list.Add(new JumpDescriptor(0, dz * d, -1, JumpFlavor.SprintJump));
            for (int d = 2; d <= 5; d++)
                list.Add(new JumpDescriptor(0, dz * d, -2, JumpFlavor.SprintJump));
        }

        // Diagonal parkour
        foreach (int dx in offsets)
        {
            foreach (int dz in offsets)
            {
                list.Add(new JumpDescriptor(dx * 2, dz * 1, 0, JumpFlavor.SprintJump));
                list.Add(new JumpDescriptor(dx * 1, dz * 2, 0, JumpFlavor.SprintJump));
                list.Add(new JumpDescriptor(dx * 2, dz * 2, 0, JumpFlavor.SprintJump));
                list.Add(new JumpDescriptor(dx * 3, dz * 1, 0, JumpFlavor.SprintJump));
                list.Add(new JumpDescriptor(dx * 1, dz * 3, 0, JumpFlavor.SprintJump));

                list.Add(new JumpDescriptor(dx * 2, dz * 1, -1, JumpFlavor.SprintJump));
                list.Add(new JumpDescriptor(dx * 1, dz * 2, -1, JumpFlavor.SprintJump));
                list.Add(new JumpDescriptor(dx * 2, dz * 2, -1, JumpFlavor.SprintJump));

                list.Add(new JumpDescriptor(dx * 2, dz * 1, 1, JumpFlavor.SprintJump));
                list.Add(new JumpDescriptor(dx * 1, dz * 2, 1, JumpFlavor.SprintJump));
                list.Add(new JumpDescriptor(dx * 2, dz * 2, 1, JumpFlavor.SprintJump));
            }
        }

        // Sidewall parkour
        foreach (int dx in offsets)
        {
            foreach (int dz in offsets)
            {
                foreach (int distance in new[] { 2, 3, 4, 5 })
                {
                    list.Add(new JumpDescriptor(dx, dz * distance, 0, JumpFlavor.Sidewall));
                    list.Add(new JumpDescriptor(dx * distance, dz, 0, JumpFlavor.Sidewall));

                    if (distance <= 3)
                    {
                        list.Add(new JumpDescriptor(dx, dz * distance, 1, JumpFlavor.Sidewall));
                        list.Add(new JumpDescriptor(dx * distance, dz, 1, JumpFlavor.Sidewall));
                    }

                    list.Add(new JumpDescriptor(dx, dz * distance, -1, JumpFlavor.Sidewall));
                    list.Add(new JumpDescriptor(dx * distance, dz, -1, JumpFlavor.Sidewall));
                    list.Add(new JumpDescriptor(dx, dz * distance, -2, JumpFlavor.Sidewall));
                    list.Add(new JumpDescriptor(dx * distance, dz, -2, JumpFlavor.Sidewall));
                }
            }
        }

        return list.ToArray();
    }

    /// <summary>
    /// Read-only snapshot of the descriptor table used by this expander. Exposed
    /// for callers that need to enumerate the jump family directly (e.g. A*'s
    /// sidewall-runup preparation logic).
    /// </summary>
    public static ReadOnlySpan<JumpDescriptor> Descriptors => _descriptors;
}

/// <summary>
/// Thin adapter that wraps an array of legacy <see cref="IMove"/> instances as
/// an <see cref="IMoveExpander"/>. Used for the dynamic-landing and vertical
/// move families (<c>MoveDescend</c>, <c>MoveSprintDescend</c>,
/// <c>MoveClimb</c>, <c>MoveFall</c>) which do not fit the JumpDescriptor model.
/// </summary>
public sealed class LegacyMoveExpander : IMoveExpander
{
    private readonly IMove[] _moves;

    public LegacyMoveExpander(IMove[] moves)
    {
        _moves = moves ?? throw new ArgumentNullException(nameof(moves));
    }

    public int MaxNeighbors => _moves.Length;

    public int Expand(CalculationContext ctx, int x, int y, int z, Span<MoveNeighbor> buffer)
    {
        int count = 0;
        MoveResult result = default;
        for (int i = 0; i < _moves.Length; i++)
        {
            IMove move = _moves[i];
            result.Cost = 0;
            move.Calculate(ctx, x, y, z, ref result);
            if (result.IsImpossible)
                continue;

            if (count < buffer.Length)
                buffer[count++] = new MoveNeighbor(result, move.Type);
        }
        return count;
    }
}
