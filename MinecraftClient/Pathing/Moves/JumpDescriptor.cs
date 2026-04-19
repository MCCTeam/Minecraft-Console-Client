namespace MinecraftClient.Pathing.Moves;

/// <summary>
/// Kind of jump-family move. Each flavor selects a different evaluator path
/// inside <see cref="JumpFeasibility"/> while sharing low-level primitives
/// (head clearance, destination clearance, flight-path sweep, cost model).
/// </summary>
public enum JumpFlavor
{
    /// <summary>
    /// Single-block cardinal or diagonal walk at the same Y. No jump input.
    /// Covers the old <c>MoveTraverse</c> and <c>MoveDiagonal</c>.
    /// </summary>
    Walk,

    /// <summary>
    /// Single-block vertical step (dy = +1 up or dy = -1 down), cardinal or
    /// diagonal. Covers <c>MoveAscend</c>, <c>MoveDiagonalAscend</c>, and
    /// <c>MoveDiagonalDescend</c>.
    /// </summary>
    Step,

    /// <summary>
    /// Multi-block sprint jump, cardinal or diagonal. Covers <c>MoveParkour</c>.
    /// </summary>
    SprintJump,

    /// <summary>
    /// Dominant-axis sprint jump that uses an inner wall for support. Covers
    /// <c>MoveSidewallParkour</c>.
    /// </summary>
    Sidewall,
}

/// <summary>
/// Fully describes a single jump-family move (Walk / Step / SprintJump /
/// Sidewall). All geometry that downstream planners or templates need can be
/// derived from this value, so A* only needs to enumerate descriptors rather
/// than hard-coded IMove subclasses.
/// </summary>
public readonly record struct JumpDescriptor(
    int XOffset,
    int ZOffset,
    int YDelta,
    JumpFlavor Flavor)
{
    public bool IsCardinal => (XOffset == 0) != (ZOffset == 0);

    public bool IsDiagonal => XOffset != 0 && ZOffset != 0;

    public int HorizontalMajor
        => System.Math.Max(System.Math.Abs(XOffset), System.Math.Abs(ZOffset));

    public int HorizontalMinor
        => System.Math.Min(System.Math.Abs(XOffset), System.Math.Abs(ZOffset));
}
