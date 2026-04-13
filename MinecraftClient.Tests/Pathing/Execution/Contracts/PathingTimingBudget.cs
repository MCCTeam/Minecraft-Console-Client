using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Tests.Pathing.Execution.Contracts;

public sealed record PathingSegmentTimingBudget(
    MoveType Move,
    int BudgetMs);

public sealed record PathingTimingBudget(
    int TotalBudgetMs,
    PathingSegmentTimingBudget[] Segments);
