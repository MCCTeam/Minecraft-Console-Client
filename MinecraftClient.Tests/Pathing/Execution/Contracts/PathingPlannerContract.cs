using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Tests.Pathing.Execution.Contracts;

public readonly record struct PathingBlock(int X, int Y, int Z);

public sealed record PathingPlannerSegmentContract(
    MoveType MoveType,
    PathingBlock StartBlock,
    PathingBlock EndBlock);

public sealed record PathingPlannerContract(
    string ScenarioId,
    PathStatus ExpectedStatus,
    IReadOnlyList<PathingPlannerSegmentContract> Segments);
