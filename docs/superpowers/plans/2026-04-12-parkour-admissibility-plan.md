# Parkour Admissibility Hardening Implementation Plan

I'm using the writing-plans skill to create the implementation plan.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden MoveParkour by factoring conservative run-up, diagonal-shoulder, and landing-overshoot checks into a helper, tightening MoveParkour’s acceptance, and covering the regression cases with deterministic tests.

**Architecture:** Inject a new `ParkourFeasibility` helper that owns the admissibility rules so MoveParkour can simply call it before running the existing flight-path and destination checks; keep the helper self-contained so future moves can reuse it without touching the MoveParkour flow.

**Tech Stack:** .NET 10 / C# 14, xUnit, dotnet CLI

---

### Task 1: Create ParkourFeasibility helper

**Files:**
- Create: `MinecraftClient/Pathing/Moves/ParkourFeasibility.cs`

- [ ] **Step 1: Implement the helper class with the three checks**

```csharp
namespace MinecraftClient.Pathing.Moves;

internal static class ParkourFeasibility
{
    public static bool HasRunUp(
        CalculationContext ctx,
        int x,
        int y,
        int z,
        int xOffset,
        int zOffset,
        int yDelta)
    {
        double horiz = Math.Sqrt(xOffset * xOffset + zOffset * zOffset);
        double threshold = yDelta > 0 ? 2.5 : 3.5;
        if (horiz < threshold)
            return true;

        int backX = x - Math.Sign(xOffset);
        int backZ = z - Math.Sign(zOffset);
        if (!ctx.CanWalkOn(backX, y - 1, backZ))
            return false;
        return IsColumnPassable(ctx, backX, y, backZ);
    }

    public static bool HasDiagonalShoulderClearance(
        CalculationContext ctx,
        int x,
        int y,
        int z,
        int xOffset,
        int zOffset)
    {
        if (xOffset == 0 || zOffset == 0)
            return true;

        return IsColumnPassable(ctx, x + Math.Sign(xOffset), y, z)
            && IsColumnPassable(ctx, x, y, z + Math.Sign(zOffset));
    }

    public static bool HasLandingOvershootClearance(
        CalculationContext ctx,
        int destX,
        int destY,
        int destZ,
        int xSign,
        int zSign)
    {
        return IsColumnPassable(ctx, destX + xSign, destY, destZ + zSign);
    }

    private static bool IsColumnPassable(CalculationContext ctx, int x, int y, int z)
    {
        if (!ctx.CanWalkThrough(x, y, z) ||
            !ctx.CanWalkThrough(x, y + 1, z) ||
            !ctx.CanWalkThrough(x, y + 2, z))
            return false;

        return true;
    }
}
```

- [ ] **Step 2: Verify the helper compiles by building the solution**

Run: `dotnet build MinecraftClient.sln -c Release`
Expected: `Build succeeded.`

### Task 2: Update MoveParkour to rely on the helper

**Files:**
- Modify: `MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs`

- [ ] **Step 1: Replace the existing run-up block with the helper**

```csharp
if (!ParkourFeasibility.HasRunUp(ctx, x, y, z, XOffset, ZOffset, _yDelta))
{
    result.SetImpossible();
    return;
}
```

- [ ] **Step 2: Replace the diagonal shoulder + overshoot handling with helper calls**

```csharp
if (!ParkourFeasibility.HasDiagonalShoulderClearance(ctx, x, y, z, XOffset, ZOffset))
{
    result.SetImpossible();
    return;
}

if (!ParkourFeasibility.HasLandingOvershootClearance(ctx, destX, destY, destZ, xSign, zSign))
{
    result.SetImpossible();
    return;
}
```

### Task 3: Add MoveParkour unit tests

**Files:**
- Create: `MinecraftClient.Tests/Pathing/Moves/MoveParkourTests.cs`

- [ ] **Step 1: Add tests for the three scenarios**

```csharp
public sealed class MoveParkourTests
{
    private const int FloorY = 79;

    private static CalculationContext BuildContext(World world)
        => new(world, allowParkour: true, allowParkourAscend: true);

    [Fact]
    public void RejectsLongJumpWithoutRunUp()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(-1, FloorY, 0), Block.Air); // remove run-up
        var ctx = BuildContext(world);
        var move = new MoveParkour(3, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void AllowsShortJumpWithClearTakeoff()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        var ctx = BuildContext(world);
        var result = default(MoveResult);
        new MoveParkour(2, 0).Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(2, result.DestX);
    }

    [Fact]
    public void RejectsDiagonalWhenShoulderBlocked()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(1, FloorY + 1, 0), new Block(1));
        var ctx = BuildContext(world);
        var result = default(MoveResult);
        new MoveParkour(1, 1).Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }
}
```

- [ ] **Step 2: Run the new tests to confirm they fail until implementation completes**

Run: `dotnet test MinecraftClient.Tests --filter MoveParkourTests`
Expected: FAIL (the tests fail until Tasks 1–2 are finished)

### Task 4: Validation

**Files:** No new files; just validation commands.

- [ ] **Step 1: Run the targeted test suite after implementation changes**

Run: `dotnet test MinecraftClient.Tests --filter MoveParkourTests`
Expected: PASS all tests in the class.

### Task 5: Commit (optional after verification)

**Files:**
- Modify: the ones mentioned above (`ParkourFeasibility.cs`, `MoveParkour.cs`, `MoveParkourTests.cs`, plan/spec files)

- [ ] **Step 1: Stage the affected files**

```bash
git add MinecraftClient/Pathing/Moves/ParkourFeasibility.cs \
    MinecraftClient/Pathing/Moves/Impl/MoveParkour.cs \
    MinecraftClient.Tests/Pathing/Moves/MoveParkourTests.cs \
    docs/superpowers/specs/2026-04-12-parkour-admissibility-design.md \
    docs/superpowers/plans/2026-04-12-parkour-admissibility-plan.md
```

- [ ] **Step 2: Commit with a descriptive message**

```bash
git commit -m "feat: harden parkour admissibility"
```
