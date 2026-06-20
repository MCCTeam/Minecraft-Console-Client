using System;
using System.Collections.Generic;

namespace MinecraftClient.Scripting;

public sealed class MccRecentEventsResult
{
    public required long AfterId { get; init; }
    public required long LatestId { get; init; }
    public required int Count { get; init; }
    public required MccRecentEventEntry[] Events { get; init; }
}

public sealed class MccChatHistoryResult
{
    public required int Count { get; init; }
    public required MccChatHistoryEntry[] Entries { get; init; }
}

public sealed class MccPathPreviewResult
{
    public required bool PathFound { get; init; }
    public required bool ExactReachable { get; init; }
    public required MccCoordinate Target { get; init; }
    public required MccCoordinate StartLocation { get; init; }
    public MccCoordinate? FinalWaypoint { get; init; }
    public double? FinalDistance { get; init; }
    public required int WaypointCount { get; init; }
    public required bool Truncated { get; init; }
    public required MccCoordinate[] Waypoints { get; init; }
    public required bool AllowUnsafe { get; init; }
    public required int MaxOffset { get; init; }
    public required int MinOffset { get; init; }
    public required int TimeoutMs { get; init; }
}

public sealed class MccReachabilityResult
{
    public required bool Reachable { get; init; }
    public required bool ExactReachable { get; init; }
    public required MccCoordinate Target { get; init; }
    public required MccCoordinate StartLocation { get; init; }
    public MccCoordinate? FinalWaypoint { get; init; }
    public double? FinalDistance { get; init; }
    public required int WaypointCount { get; init; }
    public required bool AllowUnsafe { get; init; }
    public required int MaxOffset { get; init; }
    public required int MinOffset { get; init; }
    public required int TimeoutMs { get; init; }
}

public sealed class MccPlayersDetailedEntry
{
    public string? Name { get; init; }
    public required Guid Uuid { get; init; }
    public required int Ping { get; init; }
    public required int Gamemode { get; init; }
    public required bool Listed { get; init; }
    public string? DisplayName { get; init; }
    public int? EntityId { get; init; }
    public double? X { get; init; }
    public double? Y { get; init; }
    public double? Z { get; init; }
}

public sealed class MccPlayersDetailedResult
{
    public required bool IncludeSelf { get; init; }
    public required bool IncludeCoordinates { get; init; }
    public required int Count { get; init; }
    public required MccPlayersDetailedEntry[] Players { get; init; }
}

public sealed class MccNearbyPlayerEntry
{
    public required int EntityId { get; init; }
    public required Guid Uuid { get; init; }
    public string? Name { get; init; }
    public string? CustomName { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
    public required double Distance { get; init; }
    public required int Latency { get; init; }
}

public sealed class MccPlayerNearbyResult
{
    public required double Radius { get; init; }
    public string? PlayerName { get; init; }
    public required bool IncludeSelf { get; init; }
    public required bool AnyNearby { get; init; }
    public required int Count { get; init; }
    public required MccNearbyPlayerEntry[] Players { get; init; }
}

public sealed class MccLocatedPlayerResult
{
    public required string PlayerName { get; init; }
    public string? MatchedName { get; init; }
    public required int EntityId { get; init; }
    public required Guid Uuid { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
    public required double Distance { get; init; }
}

public sealed class MccEntitySummary
{
    public required int Id { get; init; }
    public required string Type { get; init; }
    public required string TypeLabel { get; init; }
    public required Guid Uuid { get; init; }
    public string? Name { get; init; }
    public string? CustomName { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
    public double? Distance { get; init; }
    public float? Health { get; init; }
    public string? Pose { get; init; }
    public int? Latency { get; init; }
}

public sealed class MccQueryEntitiesResult
{
    public required int Count { get; init; }
    public required MccEntitySummary[] Entities { get; init; }
}

public sealed class MccListEntitiesResult
{
    public required int TotalTracked { get; init; }
    public required int Count { get; init; }
    public required MccEntitySummary[] Entities { get; init; }
}

public sealed class MccEntityEquipmentEntry
{
    public required int Slot { get; init; }
    public required string Type { get; init; }
    public required int Count { get; init; }
}

public sealed class MccEffectSnapshot
{
    public required string Id { get; init; }
    public string? Name { get; init; }
    public required int Amplifier { get; init; }
    public required int RemainingSeconds { get; init; }
    public required bool IsInfinite { get; init; }
}

public sealed class MccEntityInfoResult
{
    public required int Id { get; init; }
    public required string Type { get; init; }
    public required string TypeLabel { get; init; }
    public required Guid Uuid { get; init; }
    public string? Name { get; init; }
    public string? CustomName { get; init; }
    public required bool CustomNameVisible { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
    public required float Yaw { get; init; }
    public required float Pitch { get; init; }
    public required float Health { get; init; }
    public string? Pose { get; init; }
    public int? Latency { get; init; }
    public int? ObjectData { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public MccEntityEquipmentEntry[]? Equipment { get; init; }
    public MccEffectSnapshot[]? ActiveEffects { get; init; }
}

public sealed class MccMoveToPlayerTarget
{
    public string? PlayerName { get; init; }
    public required int EntityId { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
}

public sealed class MccMoveToPlayerResult
{
    public required bool PathFound { get; init; }
    public required bool Arrived { get; init; }
    public required double Tolerance { get; init; }
    public required int VerifyWaitMs { get; init; }
    public required MccMoveToPlayerTarget Target { get; init; }
    public required MccCoordinate StartLocation { get; init; }
    public required MccCoordinate FinalLocation { get; init; }
    public required double FinalDistance { get; init; }
    public required double DistanceMoved { get; init; }
    public required bool AllowUnsafe { get; init; }
    public required bool AllowDirectTeleport { get; init; }
    public required int MaxOffset { get; init; }
    public required int MinOffset { get; init; }
    public required int TimeoutMs { get; init; }
}

public sealed class MccHotbarSelectionResult
{
    public required bool Success { get; init; }
    public required string ItemType { get; init; }
    public required int InventorySlot { get; init; }
    public required int SelectedSlot { get; init; }
    public required int Count { get; init; }
}

public sealed class MccInventorySnapshotSlot
{
    public required int Slot { get; init; }
    public required string Type { get; init; }
    public required int Count { get; init; }
}

public sealed class MccInventorySnapshotResult
{
    public required int Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required int SlotCount { get; init; }
    public required MccInventorySnapshotSlot[] Slots { get; init; }
    public MccItemStackSnapshot? Cursor { get; init; }
}

public sealed class MccInventorySearchMatch
{
    public required int InventoryId { get; init; }
    public required string InventoryType { get; init; }
    public required string InventoryTitle { get; init; }
    public required int Slot { get; init; }
    public required string ItemType { get; init; }
    public required string TypeLabel { get; init; }
    public required int Count { get; init; }
    public required bool IsPlayerInventory { get; init; }
    public int? HotbarSlot { get; init; }
}

public sealed class MccInventorySearchResult
{
    public required string Query { get; init; }
    public required bool ExactMatch { get; init; }
    public required bool IncludeContainers { get; init; }
    public required int Count { get; init; }
    public required MccInventorySearchMatch[] Matches { get; init; }
}

public sealed class MccInventoryListEntry
{
    public required int Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required int SlotCount { get; init; }
    public required int NonEmptySlots { get; init; }
    public required bool Active { get; init; }
}

public sealed class MccInventoryListResult
{
    public required int Count { get; init; }
    public required MccInventoryListEntry[] Inventories { get; init; }
}

public sealed class MccItemEntityEntry
{
    public required int EntityId { get; init; }
    public required string ItemType { get; init; }
    public required string TypeLabel { get; init; }
    public required int Count { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
    public required double Distance { get; init; }
}

public sealed class MccItemEntitiesResult
{
    public string? ItemType { get; init; }
    public required double Radius { get; init; }
    public required int Count { get; init; }
    public required MccItemEntityEntry[] Items { get; init; }
}

public sealed class MccPickupAttempt
{
    public required int EntityId { get; init; }
    public required string ItemType { get; init; }
    public required string TypeLabel { get; init; }
    public required int ExpectedCount { get; init; }
    public required MccCoordinate Target { get; init; }
    public required bool PathFound { get; init; }
    public required bool Arrived { get; init; }
    public required bool EntityGone { get; init; }
    public required int InventoryDelta { get; init; }
    public required MccCoordinate StartLocation { get; init; }
    public required MccCoordinate FinalLocation { get; init; }
    public required double FinalDistance { get; init; }
}

public sealed class MccPickupItemsResult
{
    public required string ItemType { get; init; }
    public required double Radius { get; init; }
    public required int MaxItems { get; init; }
    public required bool AllowUnsafe { get; init; }
    public required int TimeoutMs { get; init; }
    public required int Attempted { get; init; }
    public required int SuccessfulPickups { get; init; }
    public required int CollectedCount { get; init; }
    public int? InitialInventoryCount { get; init; }
    public int? FinalInventoryCount { get; init; }
    public required int RemainingNearby { get; init; }
    public required MccPickupAttempt[] Attempts { get; init; }
}
