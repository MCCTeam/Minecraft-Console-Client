using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves.Impl;

namespace MinecraftClient.Pathing.Moves;

/// <summary>
/// Dynamic expander for every move in the jump family (Walk, Step,
/// SprintJump, Sidewall). Walk, Step, diagonal SprintJump and Sidewall are
/// still driven by a declarative descriptor table that calls
/// <see cref="JumpFeasibility.Evaluate"/> one entry at a time. Cardinal
/// SprintJumps are produced by <see cref="ProbeCardinal"/>, a Baritone-style
/// near-to-far scan that emits at most one candidate per direction -- letting
/// A* re-probe from each landing instead of enumerating every (distance,
/// yDelta) combination.
///
/// The hot path hoists per-node guards (AllowParkour, head clearance,
/// takeoff material, adjacent-wall presence) and precomputes an 8-direction
/// "first-step has no floor" table so entire descriptor groups can be
/// rejected in O(1) before touching <see cref="JumpFeasibility"/>. Ordinary
/// ground-walking nodes skip every jump descriptor this way; nodes without
/// any adjacent wall skip all sidewall descriptors.
/// </summary>
public sealed class JumpExpander : IMoveExpander
{
    /// <summary>
    /// Extra slots in the neighbor buffer for the 4 cardinal probes. Each
    /// probe may emit up to one sprint-jump candidate per yDelta (4 total)
    /// plus up to one sidewall candidate per (lateral sign, yDelta) (8
    /// total), so 4 directions * (4 + 8) = 48 slots. The probe almost never
    /// fills every slot; this is a generous upper bound that keeps the
    /// neighbor buffer stack-allocated.
    /// </summary>
    private const int CardinalProbeSlots = 48;

    private static readonly JumpDescriptor[] _descriptors = BuildDescriptors();

    public int MaxNeighbors => _descriptors.Length + CardinalProbeSlots;

    public int Expand(CalculationContext ctx, int x, int y, int z, Span<MoveNeighbor> buffer)
    {
        int count = 0;
        MoveResult result = default;

        // ---- Per-node preconditions shared by every jump-family move ----
        // These are the first checks JumpFeasibility.Evaluate* would make.
        // Hoisting them once turns many method calls per node into one
        // branch in the hot path. "canSprintTakeoff" gates both the
        // descriptor loop's SprintJump entries and all four cardinal probes.
        bool jumpFamilyAllowed = ctx.AllowParkour && ctx.CanSprint;
        bool canSprintTakeoff = false;
        if (jumpFamilyAllowed)
        {
            Material standingOn = ctx.GetMaterial(x, y - 1, z);
            Material atFeet = ctx.GetMaterial(x, y, z);
            canSprintTakeoff =
                !standingOn.CanBeClimbedOn()
                && !atFeet.IsLiquid()
                && ctx.CanWalkThrough(x, y + 2, z);
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
                    // Sidewall candidates are produced by ProbeCardinal now.
                    continue;
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

        // ---- Cardinal SprintJump probes (Baritone-style near-to-far scan) ----
        // Each cardinal direction probes distance 2..5 and emits at most one
        // candidate (the closest feasible landing). A* re-probes from that
        // landing to discover longer variants, which keeps the frontier small
        // while preserving reachability.
        if (canSprintTakeoff)
        {
            ProbeCardinal(ctx, x, y, z, +1, 0, directionGapOpen, buffer, ref count, ref result);
            ProbeCardinal(ctx, x, y, z, -1, 0, directionGapOpen, buffer, ref count, ref result);
            ProbeCardinal(ctx, x, y, z, 0, +1, directionGapOpen, buffer, ref count, ref result);
            ProbeCardinal(ctx, x, y, z, 0, -1, directionGapOpen, buffer, ref count, ref result);
        }

        return count;
    }

    /// <summary>
    /// Scans a single cardinal direction <c>(fx, fz)</c> for both sprint-jump
    /// and sidewall landings. The forward air corridor is swept once
    /// (Baritone-style monotonic scan with early break on obstruction) and
    /// every feasible landing shape shares that sweep. Per-(lateral, yDelta)
    /// sidewall candidates use the same <c>i</c> iteration to locate their
    /// landing on the lateral column, so a single O(5) scan replaces the
    /// ~8 + 16 static descriptor entries this direction used to need.
    ///
    /// Instead of emitting the closest valid landing (Baritone's choice),
    /// the probe records the farthest valid landing per shape bucket and
    /// emits one candidate each. Preferring the longer jump keeps A*'s path
    /// cost low and avoids chains of short d=2 parkour jumps that MCC's
    /// template can overshoot when sprint momentum is carried over.
    /// </summary>
    private static void ProbeCardinal(
        CalculationContext ctx,
        int x, int y, int z,
        int fx, int fz,
        ReadOnlySpan<bool> directionGapOpen,
        Span<MoveNeighbor> buffer,
        ref int count,
        ref MoveResult result)
    {
        // If the first step has a floor, a cheaper Walk move covers this
        // direction already (Baritone: "don't parkour if we could just
        // traverse"). Use the precomputed gap table.
        int firstStepIdx = ((fx + 1) * 3) + (fz + 1);
        if (!directionGapOpen[firstStepIdx])
            return;

        // The first step's column (y, y+1) must be passable; without it the
        // player hits a wall before leaving the takeoff block. (y+2 over the
        // takeoff itself is guaranteed by canSprintTakeoff.)
        int sx1 = x + fx;
        int sz1 = z + fz;
        if (!ctx.CanWalkThrough(sx1, y, sz1) || !ctx.CanWalkThrough(sx1, y + 1, sz1))
            return;

        // Lateral unit vectors perpendicular to (fx, fz). Positive and
        // negative sides are tracked independently so the wall presence
        // short-circuit applies per side.
        int lxP, lzP, lxN, lzN;
        if (fx != 0)
        {
            lxP = 0; lzP = +1;
            lxN = 0; lzN = -1;
        }
        else
        {
            lxP = +1; lzP = 0;
            lxN = -1; lzN = 0;
        }

        // Sidewall needs a solid block immediately lateral to the takeoff
        // (step=0 in HasSidewallArcClearance). If that cell is walk-through
        // at both y and y+1, no sidewall candidate from this takeoff can
        // succeed along that lateral sign.
        bool wallP = !ctx.CanWalkThrough(x + lxP, y, z + lzP)
                   || !ctx.CanWalkThrough(x + lxP, y + 1, z + lzP);
        bool wallN = !ctx.CanWalkThrough(x + lxN, y, z + lzN)
                   || !ctx.CanWalkThrough(x + lxN, y + 1, z + lzN);

        // Farthest valid i for each sprint-jump shape.
        int bestAscend = 0;
        int bestFlat = 0;
        int bestDescend1 = 0;
        int bestDescend2 = 0;

        // Farthest valid i per (lateral sign, yDelta) for sidewall.
        // yDelta indices: 0=+1, 1=0, 2=-1, 3=-2.
        int bestSwP0 = 0, bestSwP1 = 0, bestSwP2 = 0, bestSwP3 = 0;
        int bestSwN0 = 0, bestSwN1 = 0, bestSwN2 = 0, bestSwN3 = 0;

        const int MaxJumpDistance = 5;
        for (int i = 2; i <= MaxJumpDistance; i++)
        {
            int dx = x + fx * i;
            int dz = z + fz * i;

            // Shared head-height air corridor. If blocked the whole arc is
            // interrupted; every larger i is also unreachable for both
            // sprint jump and sidewall.
            if (!ctx.CanWalkThrough(dx, y + 1, dz) || !ctx.CanWalkThrough(dx, y + 2, dz))
                break;

            if (!ctx.CanWalkThrough(dx, y, dz))
            {
                // Foot-height is blocked. Only sprint-jump ascend is
                // potentially viable here, and only for i <= 3. Sidewall's
                // HasSidewallArcClearance requires a clear forward column
                // at every step, so no sidewall candidate survives past
                // this obstruction either.
                if (i <= 3 && ctx.CanWalkOn(dx, y, dz))
                    bestAscend = i;
                break;
            }

            // Foot-height is clear; record the best forward-axis landing.
            if (ctx.CanWalkOn(dx, y - 1, dz))
                bestFlat = i;
            else if (ctx.CanWalkOn(dx, y - 2, dz))
                bestDescend1 = i;
            else if (ctx.CanWalkOn(dx, y - 3, dz))
                bestDescend2 = i;

            // Sidewall candidates land on the lateral column. The forward
            // corridor has already been validated above; HasSidewallArc-
            // Clearance's wall-depth and outside-lateral checks are deferred
            // to EvaluateSidewall.
            if (wallP)
                TrackSidewallCandidates(ctx, dx, y, dz, lxP, lzP, i,
                    ref bestSwP0, ref bestSwP1, ref bestSwP2, ref bestSwP3);
            if (wallN)
                TrackSidewallCandidates(ctx, dx, y, dz, lxN, lzN, i,
                    ref bestSwN0, ref bestSwN1, ref bestSwN2, ref bestSwN3);
        }

        // Emit sprint-jump bests (MoveType.Parkour).
        if (bestAscend > 0)
            TryEmitSprintJump(ctx, x, y, z, fx * bestAscend, fz * bestAscend, +1, buffer, ref count, ref result);
        if (bestFlat > 0)
            TryEmitSprintJump(ctx, x, y, z, fx * bestFlat, fz * bestFlat, 0, buffer, ref count, ref result);
        if (bestDescend1 > 0)
            TryEmitSprintJump(ctx, x, y, z, fx * bestDescend1, fz * bestDescend1, -1, buffer, ref count, ref result);
        if (bestDescend2 > 0)
            TryEmitSprintJump(ctx, x, y, z, fx * bestDescend2, fz * bestDescend2, -2, buffer, ref count, ref result);

        // Emit sidewall bests, one candidate per (lateral sign, yDelta).
        EmitSidewallIfAny(ctx, x, y, z, fx, fz, lxP, lzP, +1, bestSwP0, buffer, ref count, ref result);
        EmitSidewallIfAny(ctx, x, y, z, fx, fz, lxP, lzP, 0, bestSwP1, buffer, ref count, ref result);
        EmitSidewallIfAny(ctx, x, y, z, fx, fz, lxP, lzP, -1, bestSwP2, buffer, ref count, ref result);
        EmitSidewallIfAny(ctx, x, y, z, fx, fz, lxP, lzP, -2, bestSwP3, buffer, ref count, ref result);

        EmitSidewallIfAny(ctx, x, y, z, fx, fz, lxN, lzN, +1, bestSwN0, buffer, ref count, ref result);
        EmitSidewallIfAny(ctx, x, y, z, fx, fz, lxN, lzN, 0, bestSwN1, buffer, ref count, ref result);
        EmitSidewallIfAny(ctx, x, y, z, fx, fz, lxN, lzN, -1, bestSwN2, buffer, ref count, ref result);
        EmitSidewallIfAny(ctx, x, y, z, fx, fz, lxN, lzN, -2, bestSwN3, buffer, ref count, ref result);
    }

    /// <summary>
    /// Cheap per-<c>i</c> pre-check for sidewall candidates. Updates the
    /// per-yDelta "farthest valid i" buckets whenever the lateral landing
    /// column matches the y offset. The expensive full feasibility check
    /// (<see cref="ParkourFeasibility.HasSidewallArcClearance"/> etc.) is
    /// still performed by <see cref="JumpFeasibility.EvaluateSidewall"/>
    /// on emission; this pre-check just filters out trivially-impossible
    /// iterations so Evaluate runs at most 8 times per direction.
    /// </summary>
    private static void TrackSidewallCandidates(
        CalculationContext ctx,
        int dx, int y, int dz,
        int lateralX, int lateralZ,
        int i,
        ref int bestPlus1,
        ref int bestFlat,
        ref int bestMinus1,
        ref int bestMinus2)
    {
        int lx = dx + lateralX;
        int lz = dz + lateralZ;

        // yDelta = +1 (ascend). Only meaningful for i <= 3.
        if (i <= 3
            && ctx.CanWalkOn(lx, y, lz)
            && ctx.CanWalkThrough(lx, y + 1, lz)
            && ctx.CanWalkThrough(lx, y + 2, lz))
        {
            bestPlus1 = i;
        }

        // Destination column body clearance at flat/descend heights.
        if (!ctx.CanWalkThrough(lx, y, lz) || !ctx.CanWalkThrough(lx, y + 1, lz))
            return;

        if (ctx.CanWalkOn(lx, y - 1, lz))
            bestFlat = i;
        else if (ctx.CanWalkOn(lx, y - 2, lz))
            bestMinus1 = i;
        else if (ctx.CanWalkOn(lx, y - 3, lz))
            bestMinus2 = i;
    }

    private static void EmitSidewallIfAny(
        CalculationContext ctx,
        int x, int y, int z,
        int fx, int fz,
        int lateralX, int lateralZ,
        int yDelta,
        int bestI,
        Span<MoveNeighbor> buffer,
        ref int count,
        ref MoveResult result)
    {
        if (bestI <= 0)
            return;

        int xOffset = fx * bestI + lateralX;
        int zOffset = fz * bestI + lateralZ;
        JumpDescriptor desc = new(xOffset, zOffset, yDelta, JumpFlavor.Sidewall);
        result.Cost = 0;
        JumpFeasibility.Evaluate(ctx, x, y, z, desc, ref result);
        if (result.IsImpossible)
            return;

        if (count < buffer.Length)
            buffer[count++] = new MoveNeighbor(result, MoveType.Parkour);
    }

    /// <summary>
    /// Builds a cardinal <see cref="JumpFlavor.SprintJump"/> descriptor for
    /// the probed shape and delegates to <see cref="JumpFeasibility.Evaluate"/>.
    /// The descriptor table and this probe share a single source of truth for
    /// run-up, flight path, overshoot, cost, and entry preparation.
    /// </summary>
    private static void TryEmitSprintJump(
        CalculationContext ctx,
        int x, int y, int z,
        int xOffset, int zOffset, int yDelta,
        Span<MoveNeighbor> buffer,
        ref int count,
        ref MoveResult result)
    {
        JumpDescriptor desc = new(xOffset, zOffset, yDelta, JumpFlavor.SprintJump);
        result.Cost = 0;
        JumpFeasibility.Evaluate(ctx, x, y, z, desc, ref result);
        if (result.IsImpossible)
            return;

        if (count < buffer.Length)
            buffer[count++] = new MoveNeighbor(result, MoveType.Parkour);
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

        // Cardinal parkour is handled dynamically by ProbeCardinal; only the
        // diagonal SprintJump variants remain as static descriptors.

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

        // Sidewall parkour is produced by ProbeCardinal alongside cardinal
        // sprint jumps -- the probe shares a single forward-corridor scan
        // with the sprint-jump candidates and emits a sidewall candidate
        // whenever a lateral wall supports it.

        return list.ToArray();
    }

    /// <summary>
    /// Read-only snapshot of the descriptor table used by this expander.
    /// Contains only moves that are enumerated statically (Walk, Step,
    /// diagonal SprintJump); cardinal SprintJump and Sidewall are produced
    /// dynamically by <see cref="ProbeCardinal"/>.
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
