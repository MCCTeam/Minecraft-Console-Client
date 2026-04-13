using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Tests.Pathing.Execution.Contracts;

public readonly record struct PathingBlock(int X, int Y, int Z);

public sealed record PathingPlannerSegmentContract(
    MoveType Move,
    PathingBlock From,
    PathingBlock To);

public sealed record PathingPlannerContract(
    PathStatus ExpectedStatus,
    PathingPlannerSegmentContract[] Segments);
