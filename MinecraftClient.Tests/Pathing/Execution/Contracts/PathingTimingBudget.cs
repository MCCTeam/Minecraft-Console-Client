using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Tests.Pathing.Execution.Contracts;

public sealed record PathingSegmentTimingBudget(
    MoveType MoveType,
    int ExpectedTicks,
    int MaxTicks);

public sealed record PathingTimingBudget(
    string ScenarioId,
    int ExpectedTotalTicks,
    int MaxTotalTicks,
    IReadOnlyList<PathingSegmentTimingBudget> Segments);
