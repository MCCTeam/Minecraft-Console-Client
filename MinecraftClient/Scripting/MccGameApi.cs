using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol;

namespace MinecraftClient.Scripting;

/// <summary>
/// Transport-neutral shared gameplay and observed-state API used by MCP and bots/scripts.
/// </summary>
public sealed class MccGameApi
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    private const double SelfEntityDistanceThreshold = 0.2;
    private const int MaxPathPreviewWaypoints = 1000;
    private const int DefaultPathQueryTimeoutMs = 5000;
    private const int MinPathQueryTimeoutMs = 250;
    private const int MaxPathQueryTimeoutMs = 15000;
    private const int DefaultArrivalWaitMs = 3500;
    private const int MinArrivalWaitMs = 250;
    private const int MaxArrivalWaitMs = 15000;
    private const double DefaultArrivalTolerance = 1.5;
    private const int ArrivalPollIntervalMs = 125;

    private readonly Func<McClient?> clientProvider;

    private sealed class NearbyPlayerSnapshot
    {
        public required int EntityId { get; init; }
        public required Guid Uuid { get; init; }
        public string? Name { get; set; }
        public string? CustomName { get; init; }
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Z { get; init; }
        public required double Distance { get; init; }
        public required int Latency { get; init; }
    }

    private sealed class NearbyItemSnapshot
    {
        public required int EntityId { get; init; }
        public required ItemType ItemType { get; init; }
        public required string TypeLabel { get; init; }
        public required int Count { get; init; }
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Z { get; init; }
        public required double Distance { get; init; }
    }

    public MccGameApi(Func<McClient?> clientProvider)
    {
        ArgumentNullException.ThrowIfNull(clientProvider);
        this.clientProvider = clientProvider;
    }

    /// <summary>
    /// Get the latest shared world time and weather snapshot.
    /// </summary>
    public MccRuntimeStateSnapshot GetRuntimeState()
    {
        return MccObservedStateStore.GetRuntimeStateSnapshot();
    }

    /// <summary>
    /// Get recent high-signal runtime events recorded by MCC.
    /// </summary>
    public MccRecentEventsResult GetRecentEvents(long afterId = 0, int maxCount = 50, string? typeFilter = null)
    {
        MccRecentEventEntry[] events = MccObservedStateStore.GetRecentEventsAfter(afterId, maxCount, typeFilter);
        return new MccRecentEventsResult
        {
            AfterId = afterId,
            LatestId = MccObservedStateStore.GetLatestRecentEventId(),
            Count = events.Length,
            Events = events
        };
    }

    /// <summary>
    /// Get recent chat and system lines observed by MCC.
    /// </summary>
    public MccChatHistoryResult GetChatHistory(int maxCount = 50, bool includeJson = false)
    {
        MccChatHistoryEntry[] entries = MccObservedStateStore.GetLatestChatHistory(maxCount);
        if (!includeJson)
        {
            entries = entries
                .Select(entry => new MccChatHistoryEntry
                {
                    TimestampUtc = entry.TimestampUtc,
                    Kind = entry.Kind,
                    Text = entry.Text,
                    Sender = entry.Sender,
                    Message = entry.Message
                })
                .ToArray();
        }

        return new MccChatHistoryResult
        {
            Count = entries.Length,
            Entries = entries
        };
    }

    /// <summary>
    /// Compute a path preview to a location without moving there.
    /// </summary>
    public MccGameResult<MccPathPreviewResult> PreviewPath(double x, double y, double z, bool allowUnsafe = false, int maxOffset = 0, int minOffset = 0, int timeoutMs = 0, int maxWaypoints = 128)
    {
        if (!AreValidPathOffsets(maxOffset, minOffset) || timeoutMs < 0 || maxWaypoints <= 0)
        {
            return MccGameResult<MccPathPreviewResult>.Fail("invalid_args");
        }

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccPathPreviewResult>();

        if (!client.GetTerrainEnabled())
            return MccGameResult<MccPathPreviewResult>.Fail("feature_disabled");

        Location goal = new(x, y, z);
        Location startLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
        World world = client.InvokeOnMainThread(client.GetWorld);
        int effectiveTimeoutMs = GetPathQueryTimeoutMs(timeoutMs);
        int waypointLimit = Math.Clamp(maxWaypoints, 1, MaxPathPreviewWaypoints);
        Queue<Location>? path = Movement.CalculatePath(
            world,
            startLocation,
            goal,
            allowUnsafe,
            maxOffset,
            minOffset,
            TimeSpan.FromMilliseconds(effectiveTimeoutMs));
        Location[] waypoints = path?.Take(waypointLimit).ToArray() ?? [];
        Location? finalWaypoint = path is not null && path.Count > 0 ? path.Last() : null;

        return MccGameResult<MccPathPreviewResult>.Ok(new MccPathPreviewResult
        {
            PathFound = path is not null,
            ExactReachable = finalWaypoint is Location location && location.ToFloor() == goal.ToFloor(),
            Target = MccGameCommon.ToCoordinate(goal),
            StartLocation = MccGameCommon.ToCoordinate(startLocation),
            FinalWaypoint = finalWaypoint is Location waypoint ? MccGameCommon.ToCoordinate(waypoint) : null,
            FinalDistance = finalWaypoint is Location endWaypoint ? MccGameCommon.GetDistance(endWaypoint, goal) : null,
            WaypointCount = path?.Count ?? 0,
            Truncated = path is not null && path.Count > waypointLimit,
            Waypoints = waypoints.Select(MccGameCommon.ToCoordinate).ToArray(),
            AllowUnsafe = allowUnsafe,
            MaxOffset = maxOffset,
            MinOffset = minOffset,
            TimeoutMs = effectiveTimeoutMs
        });
    }

    /// <summary>
    /// Check whether MCC can currently path to a location without moving there.
    /// </summary>
    public MccGameResult<MccReachabilityResult> CanReachPosition(double x, double y, double z, bool allowUnsafe = false, int maxOffset = 0, int minOffset = 0, int timeoutMs = 0)
    {
        if (!AreValidPathOffsets(maxOffset, minOffset) || timeoutMs < 0)
            return MccGameResult<MccReachabilityResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccReachabilityResult>();

        if (!client.GetTerrainEnabled())
            return MccGameResult<MccReachabilityResult>.Fail("feature_disabled");

        Location goal = new(x, y, z);
        Location startLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
        World world = client.InvokeOnMainThread(client.GetWorld);
        int effectiveTimeoutMs = GetPathQueryTimeoutMs(timeoutMs);
        Queue<Location>? path = Movement.CalculatePath(
            world,
            startLocation,
            goal,
            allowUnsafe,
            maxOffset,
            minOffset,
            TimeSpan.FromMilliseconds(effectiveTimeoutMs));
        Location? finalWaypoint = path?.LastOrDefault();
        double? finalDistance = finalWaypoint is Location waypoint
            ? MccGameCommon.GetDistance(waypoint, goal)
            : null;

        return MccGameResult<MccReachabilityResult>.Ok(new MccReachabilityResult
        {
            Reachable = path is not null,
            ExactReachable = finalWaypoint is Location location && location.ToFloor() == goal.ToFloor(),
            Target = MccGameCommon.ToCoordinate(goal),
            StartLocation = MccGameCommon.ToCoordinate(startLocation),
            FinalWaypoint = finalWaypoint is Location finalLocation ? MccGameCommon.ToCoordinate(finalLocation) : null,
            FinalDistance = finalDistance,
            WaypointCount = path?.Count ?? 0,
            AllowUnsafe = allowUnsafe,
            MaxOffset = maxOffset,
            MinOffset = minOffset,
            TimeoutMs = effectiveTimeoutMs
        });
    }

    /// <summary>
    /// Get online players with tracked coordinates and latency when available.
    /// </summary>
    public MccGameResult<MccPlayersDetailedResult> GetPlayersDetailed(bool includeSelf = false, bool includeCoordinates = true)
    {
        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccPlayersDetailedResult>();

        return client.InvokeOnMainThread(() =>
        {
            Dictionary<string, string> onlinePlayers = client.GetOnlinePlayersWithUUID();
            Dictionary<Guid, NearbyPlayerSnapshot>? trackedPlayers = client.GetEntityHandlingEnabled()
                ? BuildTrackedPlayerSnapshots(client, includeSelf: true).ToDictionary(player => player.Uuid)
                : null;
            Guid selfUuid = client.GetUserUuid();
            string selfName = client.GetUsername();

            MccPlayersDetailedEntry[] players = onlinePlayers
                .Select(pair =>
                {
                    if (!Guid.TryParse(pair.Key, out Guid uuid))
                        return null;

                    bool isSelf = uuid == selfUuid || NameComparer.Equals(pair.Value, selfName);
                    if (!includeSelf && isSelf)
                        return null;

                    PlayerInfo? playerInfo = client.GetPlayerInfo(uuid);
                    NearbyPlayerSnapshot? trackedPlayer = trackedPlayers is not null
                        && trackedPlayers.TryGetValue(uuid, out NearbyPlayerSnapshot? resolvedTrackedPlayer)
                            ? resolvedTrackedPlayer
                            : null;
                    Location? selfLocation = isSelf ? client.GetCurrentLocation() : null;
                    int? entityId = trackedPlayer?.EntityId ?? (isSelf ? client.GetPlayerEntityID() : null);
                    double? x = includeCoordinates
                        ? trackedPlayer?.X is double trackedX ? MccGameCommon.RoundCoordinate(trackedX)
                        : selfLocation.HasValue ? MccGameCommon.RoundCoordinate(selfLocation.Value.X)
                        : null
                        : null;
                    double? y = includeCoordinates
                        ? trackedPlayer?.Y is double trackedY ? MccGameCommon.RoundCoordinate(trackedY)
                        : selfLocation.HasValue ? MccGameCommon.RoundCoordinate(selfLocation.Value.Y)
                        : null
                        : null;
                    double? z = includeCoordinates
                        ? trackedPlayer?.Z is double trackedZ ? MccGameCommon.RoundCoordinate(trackedZ)
                        : selfLocation.HasValue ? MccGameCommon.RoundCoordinate(selfLocation.Value.Z)
                        : null
                        : null;

                    return new MccPlayersDetailedEntry
                    {
                        Name = playerInfo?.Name ?? pair.Value,
                        Uuid = uuid,
                        Ping = playerInfo?.Ping ?? trackedPlayer?.Latency ?? 0,
                        Gamemode = playerInfo?.Gamemode ?? -1,
                        Listed = playerInfo?.Listed ?? true,
                        DisplayName = playerInfo?.DisplayName,
                        EntityId = entityId,
                        X = x,
                        Y = y,
                        Z = z
                    };
                })
                .Where(player => player is not null)
                .Select(player => player!)
                .OrderBy(player => player.Name, NameComparer)
                .ToArray();

            return MccGameResult<MccPlayersDetailedResult>.Ok(new MccPlayersDetailedResult
            {
                IncludeSelf = includeSelf,
                IncludeCoordinates = includeCoordinates,
                Count = players.Length,
                Players = players
            });
        });
    }

    /// <summary>
    /// Check whether any player, or a specific player, is nearby.
    /// </summary>
    public MccGameResult<MccPlayerNearbyResult> IsPlayerNearby(string? playerName = null, double radius = 32, bool includeSelf = false)
    {
        if (radius <= 0 || radius > 1024)
            return MccGameResult<MccPlayerNearbyResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccPlayerNearbyResult>();

        if (!client.GetEntityHandlingEnabled())
            return MccGameResult<MccPlayerNearbyResult>.Fail("feature_disabled");

        string? nameFilter = string.IsNullOrWhiteSpace(playerName) ? null : playerName.Trim();
        return client.InvokeOnMainThread(() =>
        {
            double radiusValue = radius;
            MccNearbyPlayerEntry[] players = BuildTrackedPlayerSnapshots(client, includeSelf)
                .Where(player => player.Distance <= radiusValue)
                .Where(player => nameFilter is null || PlayerNameMatches(player, nameFilter))
                .OrderBy(player => player.Distance)
                .Select(player => new MccNearbyPlayerEntry
                {
                    EntityId = player.EntityId,
                    Uuid = player.Uuid,
                    Name = player.Name,
                    CustomName = player.CustomName,
                    X = MccGameCommon.RoundCoordinate(player.X),
                    Y = MccGameCommon.RoundCoordinate(player.Y),
                    Z = MccGameCommon.RoundCoordinate(player.Z),
                    Distance = player.Distance,
                    Latency = player.Latency
                })
                .ToArray();

            return MccGameResult<MccPlayerNearbyResult>.Ok(new MccPlayerNearbyResult
            {
                Radius = radiusValue,
                PlayerName = nameFilter,
                IncludeSelf = includeSelf,
                AnyNearby = players.Length > 0,
                Count = players.Length,
                Players = players
            });
        });
    }

    /// <summary>
    /// Locate a tracked player by name.
    /// </summary>
    public MccGameResult<MccLocatedPlayerResult> LocatePlayer(string playerName, bool includeSelf = false)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return MccGameResult<MccLocatedPlayerResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccLocatedPlayerResult>();

        if (!client.GetEntityHandlingEnabled())
            return MccGameResult<MccLocatedPlayerResult>.Fail("feature_disabled");

        string nameFilter = playerName.Trim();
        return client.InvokeOnMainThread(() =>
        {
            NearbyPlayerSnapshot[] matches = BuildTrackedPlayerSnapshots(client, includeSelf)
                .Where(player => PlayerNameMatches(player, nameFilter))
                .OrderBy(player => player.Distance)
                .ToArray();

            if (matches.Length == 0)
            {
                string[] trackedPlayers = BuildTrackedPlayerSnapshots(client, includeSelf)
                    .Select(player => player.Name)
                    .OfType<string>()
                    .Distinct(NameComparer)
                    .ToArray();
                return MccGameResult<MccLocatedPlayerResult>.Fail("invalid_state", data: new MccLocatedPlayerResult
                {
                    PlayerName = nameFilter,
                    MatchedName = trackedPlayers.Length > 0 ? string.Join(", ", trackedPlayers) : null,
                    EntityId = 0,
                    Uuid = Guid.Empty,
                    X = 0,
                    Y = 0,
                    Z = 0,
                    Distance = 0
                });
            }

            NearbyPlayerSnapshot selected = matches[0];
            return MccGameResult<MccLocatedPlayerResult>.Ok(new MccLocatedPlayerResult
            {
                PlayerName = nameFilter,
                MatchedName = selected.Name,
                EntityId = selected.EntityId,
                Uuid = selected.Uuid,
                X = MccGameCommon.RoundCoordinate(selected.X),
                Y = MccGameCommon.RoundCoordinate(selected.Y),
                Z = MccGameCommon.RoundCoordinate(selected.Z),
                Distance = selected.Distance
            });
        });
    }

    /// <summary>
    /// Return tracked entities without distance filtering.
    /// </summary>
    public MccGameResult<MccQueryEntitiesResult> QueryEntities(int maxCount = 200)
    {
        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccQueryEntitiesResult>();

        if (!client.GetEntityHandlingEnabled())
            return MccGameResult<MccQueryEntitiesResult>.Fail("feature_disabled");

        int count = Math.Clamp(maxCount, 1, 1000);
        return client.InvokeOnMainThread(() =>
        {
            Dictionary<int, string?> playerNamesByEntityId = BuildTrackedPlayerSnapshots(client, includeSelf: true)
                .ToDictionary(player => player.EntityId, player => player.Name);
            MccEntitySummary[] data = client.GetEntities()
                .Take(count)
                .Select(pair =>
                {
                    string? resolvedName = pair.Value.Type == EntityType.Player
                        && playerNamesByEntityId.TryGetValue(pair.Key, out string? mappedName)
                        ? mappedName
                        : pair.Value.Name;
                    return BuildEntitySummary(pair.Value, resolvedName);
                })
                .ToArray();

            return MccGameResult<MccQueryEntitiesResult>.Ok(new MccQueryEntitiesResult
            {
                Count = client.GetEntities().Count,
                Entities = data
            });
        });
    }

    /// <summary>
    /// List tracked entities with optional filtering.
    /// </summary>
    public MccGameResult<MccListEntitiesResult> ListEntities(int maxCount = 200, string? typeFilter = null, double radius = 0)
    {
        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccListEntitiesResult>();

        if (!client.GetEntityHandlingEnabled())
            return MccGameResult<MccListEntitiesResult>.Fail("feature_disabled");

        int count = Math.Clamp(maxCount, 1, 1000);
        string? filter = string.IsNullOrWhiteSpace(typeFilter) ? null : typeFilter.Trim();
        double radiusValue = Math.Max(radius, 0);

        return client.InvokeOnMainThread(() =>
        {
            Dictionary<int, Entity> entities = client.GetEntities();
            Location playerLocation = client.GetCurrentLocation();
            Dictionary<int, string?> playerNamesByEntityId = BuildTrackedPlayerSnapshots(client, includeSelf: true)
                .ToDictionary(player => player.EntityId, player => player.Name);

            MccEntitySummary[] data = entities.Values
                .Select(entity =>
                {
                    double dx = entity.Location.X - playerLocation.X;
                    double dy = entity.Location.Y - playerLocation.Y;
                    double dz = entity.Location.Z - playerLocation.Z;
                    string? resolvedName = entity.Type == EntityType.Player
                        && playerNamesByEntityId.TryGetValue(entity.ID, out string? mappedName)
                        ? mappedName
                        : entity.Name;
                    return new
                    {
                        entity,
                        resolvedName,
                        distance = Math.Sqrt(dx * dx + dy * dy + dz * dz)
                    };
                })
                .Where(item => radiusValue <= 0 || item.distance <= radiusValue)
                .Where(item => filter is null
                    || item.entity.Type.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || item.entity.GetTypeString().Contains(filter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.distance)
                .Take(count)
                .Select(item => BuildEntitySummary(item.entity, item.resolvedName, item.distance))
                .ToArray();

            return MccGameResult<MccListEntitiesResult>.Ok(new MccListEntitiesResult
            {
                TotalTracked = entities.Count,
                Count = data.Length,
                Entities = data
            });
        });
    }

    /// <summary>
    /// Get detailed information for a tracked entity.
    /// </summary>
    public MccGameResult<MccEntityInfoResult> GetEntityInfo(int entityId, bool includeMetadata = false, bool includeEquipment = false, bool includeEffects = false)
    {
        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccEntityInfoResult>();

        if (!client.GetEntityHandlingEnabled())
            return MccGameResult<MccEntityInfoResult>.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Dictionary<int, Entity> entities = client.GetEntities();
            if (!entities.TryGetValue(entityId, out Entity? entity))
                return MccGameResult<MccEntityInfoResult>.Fail("invalid_state");

            string? resolvedName = entity.Name;
            if (entity.Type == EntityType.Player)
            {
                Dictionary<int, string?> playerNamesByEntityId = BuildTrackedPlayerSnapshots(client, includeSelf: true)
                    .ToDictionary(player => player.EntityId, player => player.Name);
                if (playerNamesByEntityId.TryGetValue(entityId, out string? mappedName))
                    resolvedName = mappedName;
            }

            Dictionary<string, object?>? metadata = includeMetadata
                ? entity.Metadata?.ToDictionary(
                    pair => pair.Key.ToString(),
                    pair => MccGameCommon.DescribeMetadataValue(pair.Value))
                : null;
            MccEntityEquipmentEntry[]? equipment = includeEquipment
                ? entity.Equipment
                    .Select(pair => new MccEntityEquipmentEntry
                    {
                        Slot = pair.Key,
                        Type = pair.Value.Type.ToString(),
                        Count = pair.Value.Count
                    })
                    .ToArray()
                : null;
            MccEffectSnapshot[]? activeEffects = includeEffects
                ? entity.ActiveEffects.Values
                    .Select(effect => new MccEffectSnapshot
                    {
                        Id = effect.Effect.ToString(),
                        Amplifier = effect.Amplifier,
                        RemainingSeconds = effect.RemainingSeconds,
                        IsInfinite = effect.IsInfinite
                    })
                    .ToArray()
                : null;

            return MccGameResult<MccEntityInfoResult>.Ok(new MccEntityInfoResult
            {
                Id = entity.ID,
                Type = entity.Type.ToString(),
                TypeLabel = entity.GetTypeString(),
                Uuid = entity.UUID,
                Name = resolvedName,
                CustomName = entity.CustomName,
                CustomNameVisible = entity.IsCustomNameVisible,
                X = MccGameCommon.RoundCoordinate(entity.Location.X),
                Y = MccGameCommon.RoundCoordinate(entity.Location.Y),
                Z = MccGameCommon.RoundCoordinate(entity.Location.Z),
                Yaw = entity.Yaw,
                Pitch = entity.Pitch,
                Health = entity.Health,
                Pose = entity.Pose.ToString(),
                Latency = entity.Latency,
                ObjectData = entity.ObjectData,
                Metadata = metadata,
                Equipment = equipment,
                ActiveEffects = activeEffects
            });
        });
    }

    /// <summary>
    /// Return the nearest tracked entity matching the requested filters.
    /// </summary>
    public MccGameResult<MccEntitySummary> FindNearestEntity(string? typeFilter = null, string? nameFilter = null, double radius = 64.0, bool includePlayers = true)
    {
        if (radius <= 0 || radius > 1024)
            return MccGameResult<MccEntitySummary>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccEntitySummary>();

        if (!client.GetEntityHandlingEnabled())
            return MccGameResult<MccEntitySummary>.Fail("feature_disabled");

        string? normalizedTypeFilter = string.IsNullOrWhiteSpace(typeFilter) ? null : typeFilter.Trim();
        string? normalizedNameFilter = string.IsNullOrWhiteSpace(nameFilter) ? null : nameFilter.Trim();

        return client.InvokeOnMainThread(() =>
        {
            Location playerLocation = client.GetCurrentLocation();
            Dictionary<int, string?> playerNamesByEntityId = BuildTrackedPlayerSnapshots(client, includeSelf: true)
                .ToDictionary(player => player.EntityId, player => player.Name);

            var nearest = client.GetEntities().Values
                .Where(entity => includePlayers || entity.Type != EntityType.Player)
                .Select(entity =>
                {
                    double dx = entity.Location.X - playerLocation.X;
                    double dy = entity.Location.Y - playerLocation.Y;
                    double dz = entity.Location.Z - playerLocation.Z;
                    string? resolvedName = entity.Type == EntityType.Player
                        && playerNamesByEntityId.TryGetValue(entity.ID, out string? mappedName)
                            ? mappedName
                            : entity.Name;
                    return new
                    {
                        entity,
                        resolvedName,
                        distance = Math.Sqrt(dx * dx + dy * dy + dz * dz)
                    };
                })
                .Where(item => item.distance <= radius)
                .Where(item => normalizedTypeFilter is null
                    || MccGameCommon.TextMatchesFilter(item.entity.Type.ToString(), normalizedTypeFilter)
                    || MccGameCommon.TextMatchesFilter(item.entity.GetTypeString(), normalizedTypeFilter))
                .Where(item => normalizedNameFilter is null
                    || EntityNameMatches(item.resolvedName, item.entity.CustomName, normalizedNameFilter))
                .OrderBy(item => item.distance)
                .FirstOrDefault();

            if (nearest is null)
                return MccGameResult<MccEntitySummary>.Fail("invalid_state");

            return MccGameResult<MccEntitySummary>.Ok(BuildEntitySummary(nearest.entity, nearest.resolvedName, nearest.distance));
        });
    }

    /// <summary>
    /// Move to a tracked player by name and verify arrival.
    /// Callers running on ChatBot update callbacks should prefer <see cref="MoveToPlayerAsync"/>
    /// so the main MCC updater thread is not blocked while pathfinding completes.
    /// </summary>
    public MccGameResult<MccMoveToPlayerResult> MoveToPlayer(string playerName, bool allowUnsafe = false, bool allowDirectTeleport = false, int maxOffset = 0, int minOffset = 0, int timeoutMs = 0)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return MccGameResult<MccMoveToPlayerResult>.Fail("invalid_args");

        if (!AreValidPathOffsets(maxOffset, minOffset) || timeoutMs < 0)
            return MccGameResult<MccMoveToPlayerResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccMoveToPlayerResult>();

        if (!client.GetTerrainEnabled() || !client.GetEntityHandlingEnabled())
            return MccGameResult<MccMoveToPlayerResult>.Fail("feature_disabled");

        string nameFilter = playerName.Trim();
        NearbyPlayerSnapshot? target = client.InvokeOnMainThread(() =>
        {
            return BuildTrackedPlayerSnapshots(client, includeSelf: false)
                .Where(player => PlayerNameMatches(player, nameFilter))
                .OrderBy(player => player.Distance)
                .FirstOrDefault();
        });

        if (target is null)
            return MccGameResult<MccMoveToPlayerResult>.Fail("invalid_state");

        Location goal = new(target.X, target.Y, target.Z);
        Location startLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
        TimeSpan? timeout = timeoutMs > 0 ? TimeSpan.FromMilliseconds(timeoutMs) : null;
        bool pathFound = client.InvokeOnMainThread(() => client.MoveTo(goal, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeout));

        int verifyWaitMs = GetArrivalWaitMs(timeoutMs);
        double tolerance = GetArrivalTolerance(maxOffset, minOffset);
        Location? finalLocation = null;
        bool arrived = pathFound && WaitForArrival(client, goal, verifyWaitMs, tolerance, out finalLocation);
        finalLocation ??= client.InvokeOnMainThread(client.GetCurrentLocation);

        MccMoveToPlayerResult resultData = new()
        {
            PathFound = pathFound,
            Arrived = arrived,
            Tolerance = tolerance,
            VerifyWaitMs = verifyWaitMs,
            Target = new MccMoveToPlayerTarget
            {
                PlayerName = target.Name,
                EntityId = target.EntityId,
                X = MccGameCommon.RoundCoordinate(target.X),
                Y = MccGameCommon.RoundCoordinate(target.Y),
                Z = MccGameCommon.RoundCoordinate(target.Z)
            },
            StartLocation = MccGameCommon.ToCoordinate(startLocation),
            FinalLocation = MccGameCommon.ToCoordinate(finalLocation.Value),
            FinalDistance = MccGameCommon.GetDistance(finalLocation.Value, goal),
            DistanceMoved = MccGameCommon.GetDistance(startLocation, finalLocation.Value),
            AllowUnsafe = allowUnsafe,
            AllowDirectTeleport = allowDirectTeleport,
            MaxOffset = maxOffset,
            MinOffset = minOffset,
            TimeoutMs = timeoutMs
        };

        return pathFound && arrived
            ? MccGameResult<MccMoveToPlayerResult>.Ok(resultData)
            : MccGameResult<MccMoveToPlayerResult>.Fail("action_incomplete", data: resultData);
    }

    /// <summary>
    /// Run <see cref="MoveToPlayer"/> on a worker thread so ChatBot callbacks can poll the result without blocking MCC updates.
    /// </summary>
    public async Task<MccGameResult<MccMoveToPlayerResult>> MoveToPlayerAsync(string playerName, bool allowUnsafe = false, bool allowDirectTeleport = false, int maxOffset = 0, int minOffset = 0, int timeoutMs = 0)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return MccGameResult<MccMoveToPlayerResult>.Fail("invalid_args");

        if (!AreValidPathOffsets(maxOffset, minOffset) || timeoutMs < 0)
            return MccGameResult<MccMoveToPlayerResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccMoveToPlayerResult>();

        if (!client.GetTerrainEnabled() || !client.GetEntityHandlingEnabled())
            return MccGameResult<MccMoveToPlayerResult>.Fail("feature_disabled");

        string nameFilter = playerName.Trim();
        NearbyPlayerSnapshot? target = client.InvokeOnMainThread(() =>
        {
            return BuildTrackedPlayerSnapshots(client, includeSelf: false)
                .Where(player => PlayerNameMatches(player, nameFilter))
                .OrderBy(player => player.Distance)
                .FirstOrDefault();
        });

        if (target is null)
            return MccGameResult<MccMoveToPlayerResult>.Fail("invalid_state");

        Location goal = new(target.X, target.Y, target.Z);
        Location startLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
        TimeSpan? timeout = timeoutMs > 0 ? TimeSpan.FromMilliseconds(timeoutMs) : null;
        bool pathFound = client.InvokeOnMainThread(() => client.MoveTo(goal, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeout));

        int verifyWaitMs = GetArrivalWaitMs(timeoutMs);
        double tolerance = GetArrivalTolerance(maxOffset, minOffset);
        Location finalLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
        bool arrived = false;

        if (pathFound)
        {
            (arrived, finalLocation) = await WaitForArrivalAsync(client, goal, verifyWaitMs, tolerance);
        }

        MccMoveToPlayerResult resultData = new()
        {
            PathFound = pathFound,
            Arrived = arrived,
            Tolerance = tolerance,
            VerifyWaitMs = verifyWaitMs,
            Target = new MccMoveToPlayerTarget
            {
                PlayerName = target.Name,
                EntityId = target.EntityId,
                X = MccGameCommon.RoundCoordinate(target.X),
                Y = MccGameCommon.RoundCoordinate(target.Y),
                Z = MccGameCommon.RoundCoordinate(target.Z)
            },
            StartLocation = MccGameCommon.ToCoordinate(startLocation),
            FinalLocation = MccGameCommon.ToCoordinate(finalLocation),
            FinalDistance = MccGameCommon.GetDistance(finalLocation, goal),
            DistanceMoved = MccGameCommon.GetDistance(startLocation, finalLocation),
            AllowUnsafe = allowUnsafe,
            AllowDirectTeleport = allowDirectTeleport,
            MaxOffset = maxOffset,
            MinOffset = minOffset,
            TimeoutMs = timeoutMs
        };

        return pathFound && arrived
            ? MccGameResult<MccMoveToPlayerResult>.Ok(resultData)
            : MccGameResult<MccMoveToPlayerResult>.Fail("action_incomplete", data: resultData);
    }

    /// <summary>
    /// Select a hotbar item by item type without moving items around.
    /// </summary>
    public MccGameResult<MccHotbarSelectionResult> SelectHotbarItem(string itemType, bool preferLowestSlot = true)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            return MccGameResult<MccHotbarSelectionResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccHotbarSelectionResult>();

        if (!client.GetInventoryEnabled())
            return MccGameResult<MccHotbarSelectionResult>.Fail("feature_disabled");

        if (!MccGameCommon.TryParseItemType(itemType, out ItemType parsedItemType))
            return MccGameResult<MccHotbarSelectionResult>.Fail("invalid_args");

        return client.InvokeOnMainThread(() =>
        {
            Container? inventory = client.GetInventory(0);
            if (inventory is null)
                return MccGameResult<MccHotbarSelectionResult>.Fail("invalid_state");

            var matches = inventory.Items
                .Where(pair => pair.Value.Type == parsedItemType && pair.Value.Count > 0)
                .Select(pair =>
                {
                    bool isHotbar = inventory.IsHotbar(pair.Key, out int hotbar);
                    return new
                    {
                        InventorySlot = pair.Key,
                        Hotbar = hotbar,
                        IsHotbar = isHotbar,
                        Count = pair.Value.Count
                    };
                })
                .Where(match => match.IsHotbar)
                .OrderBy(match => preferLowestSlot ? match.Hotbar : -match.Hotbar)
                .ToArray();

            if (matches.Length == 0)
                return MccGameResult<MccHotbarSelectionResult>.Fail("invalid_state");

            var selected = matches[0];
            bool ok = client.ChangeSlot((short)selected.Hotbar);
            MccHotbarSelectionResult resultData = new()
            {
                Success = ok,
                ItemType = parsedItemType.ToString(),
                InventorySlot = selected.InventorySlot,
                SelectedSlot = selected.Hotbar + 1,
                Count = selected.Count
            };

            return ok
                ? MccGameResult<MccHotbarSelectionResult>.Ok(resultData)
                : MccGameResult<MccHotbarSelectionResult>.Fail("action_failed", data: resultData);
        });
    }

    /// <summary>
    /// Get a snapshot of an inventory.
    /// </summary>
    public MccGameResult<MccInventorySnapshotResult> GetInventorySnapshot(int inventoryId)
    {
        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccInventorySnapshotResult>();

        if (!client.GetInventoryEnabled())
            return MccGameResult<MccInventorySnapshotResult>.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Dictionary<int, Container> inventories = client.GetInventories();
            if (!inventories.TryGetValue(inventoryId, out Container? inventory))
                return MccGameResult<MccInventorySnapshotResult>.Fail("invalid_state");

            MccInventorySnapshotSlot[] slots = inventory.Items
                .Where(item => IsSnapshotInventorySlot(inventory, item.Key))
                .OrderBy(item => item.Key)
                .Select(item => new MccInventorySnapshotSlot
                {
                    Slot = item.Key,
                    Type = item.Value.Type.ToString(),
                    Count = item.Value.Count
                })
                .ToArray();

            return MccGameResult<MccInventorySnapshotResult>.Ok(new MccInventorySnapshotResult
            {
                Id = inventory.ID,
                Type = inventory.Type.ToString(),
                Title = inventory.Title,
                SlotCount = inventory.Type.SlotCount(),
                Slots = slots,
                Cursor = TryBuildCursorSnapshot(inventory)
            });
        });
    }

    /// <summary>
    /// Search player and container inventories for matching items.
    /// </summary>
    public MccGameResult<MccInventorySearchResult> SearchInventories(string query, int maxCount = 100, bool exactMatch = false, bool includeContainers = false)
    {
        if (string.IsNullOrWhiteSpace(query))
            return MccGameResult<MccInventorySearchResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccInventorySearchResult>();

        if (!client.GetInventoryEnabled())
            return MccGameResult<MccInventorySearchResult>.Fail("feature_disabled");

        string normalizedQuery = query.Trim();
        ItemType? parsedItemType = exactMatch && MccGameCommon.TryParseItemType(normalizedQuery, out ItemType exactItemType)
            ? exactItemType
            : null;
        int limit = Math.Clamp(maxCount, 1, 1000);

        return client.InvokeOnMainThread(() =>
        {
            MccInventorySearchMatch[] matches = client.GetInventories()
                .Where(entry => includeContainers || entry.Key == 0)
                .OrderBy(entry => entry.Key)
                .SelectMany(entry =>
                {
                    Container inventory = entry.Value;
                    return inventory.Items
                        .Where(pair => pair.Key >= 0 && pair.Value.Count > 0)
                        .Where(pair => ItemMatches(pair.Value, normalizedQuery, exactMatch, parsedItemType))
                        .Select(pair =>
                        {
                            bool isHotbar = inventory.IsHotbar(pair.Key, out int hotbar);
                            return new MccInventorySearchMatch
                            {
                                InventoryId = entry.Key,
                                InventoryType = inventory.Type.ToString(),
                                InventoryTitle = inventory.Title,
                                Slot = pair.Key,
                                ItemType = pair.Value.Type.ToString(),
                                TypeLabel = pair.Value.GetTypeString(),
                                Count = pair.Value.Count,
                                IsPlayerInventory = entry.Key == 0,
                                HotbarSlot = isHotbar ? hotbar + 1 : null
                            };
                        });
                })
                .Take(limit)
                .ToArray();

            return MccGameResult<MccInventorySearchResult>.Ok(new MccInventorySearchResult
            {
                Query = normalizedQuery,
                ExactMatch = exactMatch,
                IncludeContainers = includeContainers,
                Count = matches.Length,
                Matches = matches
            });
        });
    }

    /// <summary>
    /// List active inventories.
    /// </summary>
    public MccGameResult<MccInventoryListResult> ListInventories()
    {
        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccInventoryListResult>();

        if (!client.GetInventoryEnabled())
            return MccGameResult<MccInventoryListResult>.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            MccInventoryListEntry[] inventories = client.GetInventories()
                .OrderBy(entry => entry.Key)
                .Select(entry => new MccInventoryListEntry
                {
                    Id = entry.Key,
                    Type = entry.Value.Type.ToString(),
                    Title = entry.Value.Title,
                    SlotCount = entry.Value.Type.SlotCount(),
                    NonEmptySlots = entry.Value.Items.Count(item => IsSnapshotInventorySlot(entry.Value, item.Key)),
                    Active = entry.Key > 0 && entry.Key == GetActiveContainerId(client)
                })
                .ToArray();

            return MccGameResult<MccInventoryListResult>.Ok(new MccInventoryListResult
            {
                Count = inventories.Length,
                Inventories = inventories
            });
        });
    }

    /// <summary>
    /// List nearby item entities.
    /// </summary>
    public MccGameResult<MccItemEntitiesResult> ListItemEntities(string? itemType = null, double radius = 32, int maxCount = 50)
    {
        if (radius <= 0 || radius > 1024)
            return MccGameResult<MccItemEntitiesResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccItemEntitiesResult>();

        if (!client.GetEntityHandlingEnabled())
            return MccGameResult<MccItemEntitiesResult>.Fail("feature_disabled");

        ItemType? parsedItemType = null;
        string? itemTypeFilter = null;
        if (!string.IsNullOrWhiteSpace(itemType))
        {
            itemTypeFilter = itemType.Trim();
            if (!MccGameCommon.TryParseItemType(itemTypeFilter, out ItemType resolvedType))
                return MccGameResult<MccItemEntitiesResult>.Fail("invalid_args");

            parsedItemType = resolvedType;
        }

        int limit = Math.Clamp(maxCount, 1, 500);
        return client.InvokeOnMainThread(() =>
        {
            MccItemEntityEntry[] items = BuildNearbyItemSnapshots(client, parsedItemType, radius, limit)
                .Select(item => new MccItemEntityEntry
                {
                    EntityId = item.EntityId,
                    ItemType = item.ItemType.ToString(),
                    TypeLabel = item.TypeLabel,
                    Count = item.Count,
                    X = MccGameCommon.RoundCoordinate(item.X),
                    Y = MccGameCommon.RoundCoordinate(item.Y),
                    Z = MccGameCommon.RoundCoordinate(item.Z),
                    Distance = item.Distance
                })
                .ToArray();

            return MccGameResult<MccItemEntitiesResult>.Ok(new MccItemEntitiesResult
            {
                ItemType = parsedItemType?.ToString() ?? itemTypeFilter,
                Radius = radius,
                Count = items.Length,
                Items = items
            });
        });
    }

    /// <summary>
    /// Move to nearby dropped items and verify pickup completion.
    /// Callers running on ChatBot update callbacks should prefer <see cref="PickupItemsAsync"/>
    /// so the main MCC updater thread is not blocked while movement and pickup verification complete.
    /// </summary>
    public MccGameResult<MccPickupItemsResult> PickupItems(string itemType, double radius = 16, int maxItems = 10, bool allowUnsafe = false, int timeoutMs = 0)
    {
        if (string.IsNullOrWhiteSpace(itemType) || radius <= 0 || radius > 1024 || maxItems < 1 || timeoutMs < 0)
            return MccGameResult<MccPickupItemsResult>.Fail("invalid_args");

        if (!MccGameCommon.TryParseItemType(itemType.Trim(), out ItemType parsedItemType))
            return MccGameResult<MccPickupItemsResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccPickupItemsResult>();

        if (!client.GetTerrainEnabled() || !client.GetEntityHandlingEnabled())
            return MccGameResult<MccPickupItemsResult>.Fail("feature_disabled");

        int limit = Math.Clamp(maxItems, 1, 50);
        NearbyItemSnapshot[] targets = client.InvokeOnMainThread(() => BuildNearbyItemSnapshots(client, parsedItemType, radius, limit));
        if (targets.Length == 0)
            return MccGameResult<MccPickupItemsResult>.Fail("invalid_state");

        bool inventoryEnabled = client.GetInventoryEnabled();
        int beforeCount = inventoryEnabled ? client.InvokeOnMainThread(() => GetInventoryItemCount(client, parsedItemType)) : 0;
        int initialCount = beforeCount;
        int verifyWaitMs = timeoutMs > 0 ? Math.Clamp(timeoutMs, MinArrivalWaitMs, MaxArrivalWaitMs) : 2500;
        List<MccPickupAttempt> attempts = new(targets.Length);
        int successfulPickups = 0;

        foreach (NearbyItemSnapshot target in targets)
        {
            Location targetLocation = new(target.X, target.Y, target.Z);
            Location startLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
            TimeSpan? moveTimeout = timeoutMs > 0 ? TimeSpan.FromMilliseconds(timeoutMs) : null;
            bool pathFound = client.InvokeOnMainThread(() => client.MoveTo(targetLocation, allowUnsafe, false, 0, 0, moveTimeout));
            Location? finalLocation = null;
            bool arrived = pathFound && WaitForArrival(client, targetLocation, verifyWaitMs, 2.0, out finalLocation);
            finalLocation ??= client.InvokeOnMainThread(client.GetCurrentLocation);
            bool entityGone = WaitForEntityRemoval(client, target.EntityId, verifyWaitMs);
            int afterCount = inventoryEnabled ? client.InvokeOnMainThread(() => GetInventoryItemCount(client, parsedItemType)) : beforeCount;
            int inventoryDelta = inventoryEnabled ? Math.Max(0, afterCount - beforeCount) : 0;
            bool pickedUp = entityGone || inventoryDelta > 0;
            if (pickedUp)
                successfulPickups++;

            attempts.Add(new MccPickupAttempt
            {
                EntityId = target.EntityId,
                ItemType = target.ItemType.ToString(),
                TypeLabel = target.TypeLabel,
                ExpectedCount = target.Count,
                Target = MccGameCommon.ToCoordinate(target.X, target.Y, target.Z),
                PathFound = pathFound,
                Arrived = arrived,
                EntityGone = entityGone,
                InventoryDelta = inventoryDelta,
                StartLocation = MccGameCommon.ToCoordinate(startLocation),
                FinalLocation = MccGameCommon.ToCoordinate(finalLocation.Value),
                FinalDistance = MccGameCommon.GetDistance(finalLocation.Value, targetLocation)
            });

            beforeCount = afterCount;
        }

        int remainingNearby = client.InvokeOnMainThread(() => BuildNearbyItemSnapshots(client, parsedItemType, radius, 1000).Length);
        int collectedCount = inventoryEnabled ? Math.Max(0, beforeCount - initialCount) : successfulPickups;
        MccPickupItemsResult resultData = new()
        {
            ItemType = parsedItemType.ToString(),
            Radius = radius,
            MaxItems = limit,
            AllowUnsafe = allowUnsafe,
            TimeoutMs = verifyWaitMs,
            Attempted = attempts.Count,
            SuccessfulPickups = successfulPickups,
            CollectedCount = collectedCount,
            InitialInventoryCount = inventoryEnabled ? initialCount : null,
            FinalInventoryCount = inventoryEnabled ? beforeCount : null,
            RemainingNearby = remainingNearby,
            Attempts = attempts.ToArray()
        };

        return successfulPickups > 0 || collectedCount > 0
            ? MccGameResult<MccPickupItemsResult>.Ok(resultData)
            : MccGameResult<MccPickupItemsResult>.Fail("action_incomplete", data: resultData);
    }

    /// <summary>
    /// Run <see cref="PickupItems"/> on a worker thread so ChatBot callbacks can poll the result without blocking MCC updates.
    /// </summary>
    public async Task<MccGameResult<MccPickupItemsResult>> PickupItemsAsync(string itemType, double radius = 16, int maxItems = 10, bool allowUnsafe = false, int timeoutMs = 0)
    {
        if (string.IsNullOrWhiteSpace(itemType) || radius <= 0 || radius > 1024 || maxItems < 1 || timeoutMs < 0)
            return MccGameResult<MccPickupItemsResult>.Fail("invalid_args");

        if (!MccGameCommon.TryParseItemType(itemType.Trim(), out ItemType parsedItemType))
            return MccGameResult<MccPickupItemsResult>.Fail("invalid_args");

        McClient? client = clientProvider();
        if (client is null)
            return NotConnected<MccPickupItemsResult>();

        if (!client.GetTerrainEnabled() || !client.GetEntityHandlingEnabled())
            return MccGameResult<MccPickupItemsResult>.Fail("feature_disabled");

        int limit = Math.Clamp(maxItems, 1, 50);
        NearbyItemSnapshot[] targets = client.InvokeOnMainThread(() => BuildNearbyItemSnapshots(client, parsedItemType, radius, limit));
        if (targets.Length == 0)
            return MccGameResult<MccPickupItemsResult>.Fail("invalid_state");

        bool inventoryEnabled = client.GetInventoryEnabled();
        int beforeCount = inventoryEnabled ? client.InvokeOnMainThread(() => GetInventoryItemCount(client, parsedItemType)) : 0;
        int initialCount = beforeCount;
        int verifyWaitMs = timeoutMs > 0 ? Math.Clamp(timeoutMs, MinArrivalWaitMs, MaxArrivalWaitMs) : 2500;
        List<MccPickupAttempt> attempts = new(targets.Length);
        int successfulPickups = 0;

        foreach (NearbyItemSnapshot target in targets)
        {
            Location targetLocation = new(target.X, target.Y, target.Z);
            Location startLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
            TimeSpan? moveTimeout = timeoutMs > 0 ? TimeSpan.FromMilliseconds(timeoutMs) : null;
            bool pathFound = client.InvokeOnMainThread(() => client.MoveTo(targetLocation, allowUnsafe, false, 0, 0, moveTimeout));

            Location finalLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
            bool arrived = false;
            if (pathFound)
            {
                (arrived, finalLocation) = await WaitForArrivalAsync(client, targetLocation, verifyWaitMs, 2.0);
            }

            bool entityGone = await WaitForEntityRemovalAsync(client, target.EntityId, verifyWaitMs);
            int afterCount = inventoryEnabled ? client.InvokeOnMainThread(() => GetInventoryItemCount(client, parsedItemType)) : beforeCount;
            int inventoryDelta = inventoryEnabled ? Math.Max(0, afterCount - beforeCount) : 0;
            bool pickedUp = entityGone || inventoryDelta > 0;
            if (pickedUp)
                successfulPickups++;

            attempts.Add(new MccPickupAttempt
            {
                EntityId = target.EntityId,
                ItemType = target.ItemType.ToString(),
                TypeLabel = target.TypeLabel,
                ExpectedCount = target.Count,
                Target = MccGameCommon.ToCoordinate(target.X, target.Y, target.Z),
                PathFound = pathFound,
                Arrived = arrived,
                EntityGone = entityGone,
                InventoryDelta = inventoryDelta,
                StartLocation = MccGameCommon.ToCoordinate(startLocation),
                FinalLocation = MccGameCommon.ToCoordinate(finalLocation),
                FinalDistance = MccGameCommon.GetDistance(finalLocation, targetLocation)
            });

            beforeCount = afterCount;
        }

        int remainingNearby = client.InvokeOnMainThread(() => BuildNearbyItemSnapshots(client, parsedItemType, radius, 1000).Length);
        int collectedCount = inventoryEnabled ? Math.Max(0, beforeCount - initialCount) : successfulPickups;
        MccPickupItemsResult resultData = new()
        {
            ItemType = parsedItemType.ToString(),
            Radius = radius,
            MaxItems = limit,
            AllowUnsafe = allowUnsafe,
            TimeoutMs = verifyWaitMs,
            Attempted = attempts.Count,
            SuccessfulPickups = successfulPickups,
            CollectedCount = collectedCount,
            InitialInventoryCount = inventoryEnabled ? initialCount : null,
            FinalInventoryCount = inventoryEnabled ? beforeCount : null,
            RemainingNearby = remainingNearby,
            Attempts = attempts.ToArray()
        };

        return successfulPickups > 0
            ? MccGameResult<MccPickupItemsResult>.Ok(resultData)
            : MccGameResult<MccPickupItemsResult>.Fail("action_incomplete", data: resultData);
    }

    private static MccGameResult<T> NotConnected<T>()
    {
        return MccGameResult<T>.Fail("disconnected");
    }

    private static MccEntitySummary BuildEntitySummary(Entity entity, string? resolvedName, double? distance = null)
    {
        return new MccEntitySummary
        {
            Id = entity.ID,
            Type = entity.Type.ToString(),
            TypeLabel = entity.GetTypeString(),
            Uuid = entity.UUID,
            Name = resolvedName,
            CustomName = entity.CustomName,
            X = MccGameCommon.RoundCoordinate(entity.Location.X),
            Y = MccGameCommon.RoundCoordinate(entity.Location.Y),
            Z = MccGameCommon.RoundCoordinate(entity.Location.Z),
            Distance = distance,
            Health = entity.Health,
            Pose = entity.Pose.ToString(),
            Latency = entity.Latency
        };
    }

    private static bool IsSnapshotInventorySlot(Container inventory, int slotId)
    {
        return slotId >= 0 && slotId < inventory.Type.SlotCount();
    }

    private static MccItemStackSnapshot? TryBuildCursorSnapshot(Container inventory)
    {
        return inventory.Items.TryGetValue(-1, out Item? cursorItem) && cursorItem.Count > 0
            ? MccGameCommon.ToItemStack(cursorItem)
            : null;
    }

    private static int GetActiveContainerId(McClient client)
    {
        return client.GetInventories().Keys.Where(id => id > 0).DefaultIfEmpty(0).Max();
    }

    private static bool ItemMatches(Item item, string query, bool exactMatch, ItemType? exactItemType)
    {
        if (exactItemType.HasValue)
            return item.Type == exactItemType.Value;

        string typeName = item.Type.ToString();
        string typeLabel = item.GetTypeString();
        return exactMatch
            ? MccGameCommon.TextEqualsFilter(typeName, query) || MccGameCommon.TextEqualsFilter(typeLabel, query)
            : MccGameCommon.TextMatchesFilter(typeName, query) || MccGameCommon.TextMatchesFilter(typeLabel, query);
    }

    private static bool WaitForArrival(McClient client, Location goal, int waitMs, double tolerance, out Location? finalLocation)
    {
        finalLocation = null;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            Location location = client.InvokeOnMainThread(client.GetCurrentLocation);
            finalLocation = location;
            if (MccGameCommon.GetDistance(location, goal) <= tolerance)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static async Task<(bool Arrived, Location FinalLocation)> WaitForArrivalAsync(McClient client, Location goal, int waitMs, double tolerance)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        Location finalLocation = client.InvokeOnMainThread(client.GetCurrentLocation);

        while (true)
        {
            finalLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
            if (MccGameCommon.GetDistance(finalLocation, goal) <= tolerance)
                return (true, finalLocation);

            if (DateTime.UtcNow >= deadline)
                return (false, finalLocation);

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static bool WaitForEntityRemoval(McClient client, int entityId, int waitMs)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            bool exists = client.InvokeOnMainThread(() => client.GetEntities().ContainsKey(entityId));
            if (!exists)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static async Task<bool> WaitForEntityRemovalAsync(McClient client, int entityId, int waitMs)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            bool exists = client.InvokeOnMainThread(() => client.GetEntities().ContainsKey(entityId));
            if (!exists)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static int GetInventoryItemCount(McClient client, ItemType itemType)
    {
        Container? inventory = client.GetInventory(0);
        if (inventory is null)
            return 0;

        return inventory.Items.Values
            .Where(item => item.Type == itemType)
            .Sum(item => item.Count);
    }

    private static int GetPathQueryTimeoutMs(int timeoutMs)
    {
        if (timeoutMs <= 0)
            return DefaultPathQueryTimeoutMs;
        return Math.Clamp(timeoutMs, MinPathQueryTimeoutMs, MaxPathQueryTimeoutMs);
    }

    private static int GetArrivalWaitMs(int timeoutMs)
    {
        if (timeoutMs <= 0)
            return DefaultArrivalWaitMs;
        return Math.Clamp(timeoutMs, MinArrivalWaitMs, MaxArrivalWaitMs);
    }

    private static double GetArrivalTolerance(int maxOffset, int minOffset)
    {
        double toleranceFromOffset = Math.Max(maxOffset, minOffset) + 1.0;
        return Math.Max(DefaultArrivalTolerance, toleranceFromOffset);
    }

    private static bool AreValidPathOffsets(int maxOffset, int minOffset)
    {
        return maxOffset >= 0 && minOffset >= 0 && minOffset <= maxOffset;
    }

    private static bool EntityNameMatches(string? name, string? customName, string filter)
    {
        return (!string.IsNullOrWhiteSpace(name) && MccGameCommon.TextMatchesFilter(name, filter))
            || (!string.IsNullOrWhiteSpace(customName) && MccGameCommon.TextMatchesFilter(customName, filter));
    }

    private static bool PlayerNameMatches(NearbyPlayerSnapshot player, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        string trimmed = filter.Trim();
        if (!string.IsNullOrWhiteSpace(player.Name) && player.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(player.CustomName) && player.CustomName.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static NearbyItemSnapshot[] BuildNearbyItemSnapshots(McClient client, ItemType? itemType, double radius, int maxCount)
    {
        Location playerLocation = client.GetCurrentLocation();
        return client.GetEntities().Values
            .Where(entity => entity.Type == EntityType.Item && !entity.Item.IsEmpty)
            .Where(entity => !itemType.HasValue || entity.Item.Type == itemType.Value)
            .Select(entity =>
            {
                double dx = entity.Location.X - playerLocation.X;
                double dy = entity.Location.Y - playerLocation.Y;
                double dz = entity.Location.Z - playerLocation.Z;
                return new NearbyItemSnapshot
                {
                    EntityId = entity.ID,
                    ItemType = entity.Item.Type,
                    TypeLabel = entity.Item.GetTypeString(),
                    Count = entity.Item.Count,
                    X = entity.Location.X,
                    Y = entity.Location.Y,
                    Z = entity.Location.Z,
                    Distance = Math.Sqrt(dx * dx + dy * dy + dz * dz)
                };
            })
            .Where(item => item.Distance <= radius)
            .OrderBy(item => item.Distance)
            .Take(maxCount)
            .ToArray();
    }

    private static List<NearbyPlayerSnapshot> BuildTrackedPlayerSnapshots(McClient client, bool includeSelf)
    {
        Location playerLocation = client.GetCurrentLocation();
        string username = client.GetUsername();
        Dictionary<string, string> uuidToName = client.GetOnlinePlayersWithUUID();
        string[] onlinePlayers = client.GetOnlinePlayers();

        List<NearbyPlayerSnapshot> trackedPlayers = client.GetEntities().Values
            .Where(entity => entity.Type == EntityType.Player)
            .Select(entity =>
            {
                double dx = entity.Location.X - playerLocation.X;
                double dy = entity.Location.Y - playerLocation.Y;
                double dz = entity.Location.Z - playerLocation.Z;
                double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                string? rawName = ResolvePlayerEntityName(entity, uuidToName);
                return new NearbyPlayerSnapshot
                {
                    EntityId = entity.ID,
                    Uuid = entity.UUID,
                    Name = rawName,
                    CustomName = entity.CustomName,
                    X = entity.Location.X,
                    Y = entity.Location.Y,
                    Z = entity.Location.Z,
                    Distance = distance,
                    Latency = entity.Latency
                };
            })
            .ToList();

        if (!includeSelf)
        {
            trackedPlayers = trackedPlayers
                .Where(player => !string.Equals(player.Name, username, StringComparison.OrdinalIgnoreCase))
                .Where(player => player.Distance > SelfEntityDistanceThreshold)
                .ToList();
        }

        List<NearbyPlayerSnapshot> unnamedTracked = trackedPlayers
            .Where(player => string.IsNullOrWhiteSpace(player.Name))
            .OrderBy(player => player.Distance)
            .ToList();
        if (unnamedTracked.Count == 0)
            return trackedPlayers;

        HashSet<string> assignedNames = trackedPlayers
            .Select(player => player.Name)
            .OfType<string>()
            .ToHashSet(NameComparer);

        string[] unmatchedOnline = onlinePlayers
            .Where(name => includeSelf || !string.Equals(name, username, StringComparison.OrdinalIgnoreCase))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !assignedNames.Contains(name))
            .Distinct(NameComparer)
            .ToArray();

        if (unmatchedOnline.Length == 0)
            return trackedPlayers;

        if (unnamedTracked.Count == 1 && unmatchedOnline.Length == 1)
        {
            unnamedTracked[0].Name = unmatchedOnline[0];
            return trackedPlayers;
        }

        int pairCount = Math.Min(unnamedTracked.Count, unmatchedOnline.Length);
        string[] sortedNames = unmatchedOnline
            .OrderBy(name => name, NameComparer)
            .ToArray();
        for (int i = 0; i < pairCount; i++)
            unnamedTracked[i].Name = sortedNames[i];

        return trackedPlayers;
    }

    private static string? ResolvePlayerEntityName(Entity entity, IReadOnlyDictionary<string, string> uuidToName)
    {
        if (!string.IsNullOrWhiteSpace(entity.Name))
            return entity.Name;

        if (entity.UUID != Guid.Empty
            && uuidToName.TryGetValue(entity.UUID.ToString(), out string? mappedName)
            && !string.IsNullOrWhiteSpace(mappedName))
        {
            return mappedName;
        }

        if (!string.IsNullOrWhiteSpace(entity.CustomName))
            return entity.CustomName;

        return null;
    }
}
