using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Scripting;

namespace MinecraftClient.Mcp;

public sealed class MccMcpCapabilities : IMccMcpCapabilities
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly double[] s_defaultDigAttemptDurations = [1.5, 3.0, 5.0];
    private const int CoordinateRoundingPrecision = 2;
    private const double SelfEntityDistanceThreshold = 0.2;
    private const int MaxBlockScanRadius = 12;
    private const int MaxBlockFindRadius = 32;
    private const double MaxRaycastDistance = 128.0;
    private const double DigReachDistance = 5.0;
    private const double DigReachDistanceSquared = DigReachDistance * DigReachDistance;
    private const int DefaultPathQueryTimeoutMs = 5000;
    private const int MinPathQueryTimeoutMs = 250;
    private const int MaxPathQueryTimeoutMs = 15000;
    private const int DefaultArrivalWaitMs = 3500;
    private const int MinArrivalWaitMs = 250;
    private const int MaxArrivalWaitMs = 15000;
    private const double DefaultArrivalTolerance = 1.5;
    private const int ArrivalPollIntervalMs = 125;
    private const int MaxBlockVerifyWaitMs = 12000;
    private const int DefaultContainerWaitMs = 5000;
    private const int MinContainerWaitMs = 250;
    private const int MaxContainerWaitMs = 20000;
    private const int DefaultInventoryActionWaitMs = 3500;
    private const int MaxPathPreviewWaypoints = 1000;

    private sealed class InternalCommandInfo
    {
        public required string Name { get; init; }
        public required string Usage { get; init; }
        public required string Description { get; init; }
    }

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

    private readonly record struct ContainerOpenState(int InventoryId, Container? Inventory);

    private enum InventoryTransferDirection
    {
        Deposit,
        Withdraw
    }

    private readonly Func<MccMcpCapabilityToggles> togglesProvider;
    private readonly MccGameApi game;

    public MccMcpCapabilities(Func<MccMcpCapabilityToggles> togglesProvider)
    {
        this.togglesProvider = togglesProvider;
        game = new MccGameApi(GetClient);
    }

    private static McClient? GetClient()
    {
        return McClient.Instance as McClient;
    }

    private static MccMcpResult NotConnected()
    {
        return MccMcpResult.Fail("disconnected");
    }

    private static MccMcpResult ToMcpResult(MccGameResult result)
    {
        return result.Success
            ? MccMcpResult.Ok(message: result.Message)
            : MccMcpResult.Fail(result.ErrorCode ?? "unknown", result.Message);
    }

    private static MccMcpResult ToMcpResult<T>(MccGameResult<T> result)
    {
        return result.Success
            ? MccMcpResult.Ok(result.Data, result.Message)
            : MccMcpResult.Fail(result.ErrorCode ?? "unknown", result.Message, result.Data);
    }

    private bool IsCategoryEnabled(Func<MccMcpCapabilityToggles, bool> selector)
    {
        return selector(togglesProvider());
    }

    public MccMcpResult GetSessionStatus()
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return client.InvokeOnMainThread(() =>
        {
            Location location = client.GetCurrentLocation();
            return MccMcpResult.Ok(new
            {
                host = client.GetServerHost(),
                port = client.GetServerPort(),
                username = client.GetUsername(),
                protocolVersion = client.GetProtocolVersion(),
                terrainEnabled = client.GetTerrainEnabled(),
                inventoryEnabled = client.GetInventoryEnabled(),
                entityEnabled = client.GetEntityHandlingEnabled(),
                location = ToCoordinate(location)
            });
        });
    }

    public MccMcpResult GetServerInfo()
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return client.InvokeOnMainThread(() => MccMcpResult.Ok(new
        {
            host = client.GetServerHost(),
            port = client.GetServerPort(),
            tps = client.GetServerTPS()
        }));
    }

    public MccMcpResult GetPlayerState()
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return client.InvokeOnMainThread(() =>
        {
            Location location = client.GetCurrentLocation();
            Dictionary<Effects, EffectData> effects = client.GetPlayerEffects();
            return MccMcpResult.Ok(new
            {
                nickname = client.GetUsername(),
                username = client.GetUsername(),
                health = client.GetHealth(),
                saturation = client.GetSaturation(),
                gamemode = client.GetGamemode(),
                currentSlot = client.GetCurrentSlot() + 1,
                yaw = client.GetYaw(),
                pitch = client.GetPitch(),
                location = ToCoordinate(location),
                effects = effects.Values.Select(effect => new
                {
                    id = effect.Effect.ToString(),
                    amplifier = effect.Amplifier,
                    remainingSeconds = effect.RemainingSeconds,
                    isInfinite = effect.IsInfinite
                }).ToArray()
            });
        });
    }

    public MccMcpResult GetWorldState()
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return client.InvokeOnMainThread(() =>
        {
            Location location = client.GetCurrentLocation();
            World world = client.GetWorld();
            Dimension dimension = World.GetDimension();
            MccRuntimeStateSnapshot runtimeState = game.GetRuntimeState();
            int totalChunkCount = world.chunkCnt;
            int pendingChunkCount = Math.Max(0, world.chunkLoadNotCompleted);
            int loadedChunkCount = GetLoadedChunkCount(world);

            return MccMcpResult.Ok(new
            {
                host = client.GetServerHost(),
                port = client.GetServerPort(),
                username = client.GetUsername(),
                protocol = client.GetProtocolVersion(),
                protocolVersion = client.GetProtocolVersion(),
                terrainEnabled = client.GetTerrainEnabled(),
                inventoryEnabled = client.GetInventoryEnabled(),
                entityEnabled = client.GetEntityHandlingEnabled(),
                entityHandlingEnabled = client.GetEntityHandlingEnabled(),
                location = ToCoordinate(location),
                tps = client.GetServerTPS(),
                dimension = dimension.Name,
                dimensionDetails = new
                {
                    name = dimension.Name,
                    minY = dimension.minY,
                    maxY = dimension.maxY,
                    height = dimension.height,
                    logicalHeight = dimension.logicalHeight,
                    coordinateScale = dimension.coordinateScale,
                    hasSkylight = dimension.hasSkylight,
                    hasCeiling = dimension.hasCeiling,
                    fixedTime = dimension.fixedTime >= 0 ? dimension.fixedTime : (long?)null
                },
                loadedChunkCount,
                pendingChunkCount,
                totalChunkCount,
                loadRatio = GetChunkLoadRatio(world),
                worldAge = runtimeState.WorldAge,
                timeOfDay = runtimeState.TimeOfDay,
                rainLevel = runtimeState.RainLevel,
                thunderLevel = runtimeState.ThunderLevel
            });
        });
    }

    public MccMcpResult GetChunkStatus(double? x, double? y, double? z)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        if (!HasCompleteCoordinateTriple(x, y, z))
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Location queryLocation = x.HasValue && y.HasValue && z.HasValue
                ? new Location(x.Value, y.Value, z.Value)
                : client.GetCurrentLocation();

            World world = client.GetWorld();
            ChunkColumn? chunkColumn = world.GetChunkColumn(queryLocation);
            return MccMcpResult.Ok(new
            {
                location = ToCoordinate(queryLocation),
                chunk = new
                {
                    x = queryLocation.ChunkX,
                    z = queryLocation.ChunkZ
                },
                chunkX = queryLocation.ChunkX,
                chunkZ = queryLocation.ChunkZ,
                loaded = chunkColumn is not null,
                fullyLoaded = chunkColumn?.FullyLoaded ?? false,
                loadedChunkCount = GetLoadedChunkCount(world),
                pendingChunkCount = Math.Max(0, world.chunkLoadNotCompleted),
                totalChunkCount = world.chunkCnt,
                loadRatio = GetChunkLoadRatio(world)
            });
        });
    }

    public MccMcpResult RaycastBlock(double maxDistance, bool includeNeighbors)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        if (maxDistance <= 0 || maxDistance > MaxRaycastDistance)
        {
            return MccMcpResult.Fail("invalid_args", data: new
            {
                parameter = "maxDistance",
                minExclusive = 0,
                max = MaxRaycastDistance
            });
        }

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Location playerLocation = client.GetCurrentLocation();
            Location eyeLocation = playerLocation.EyesLocation();
            Tuple<bool, Location, Block> raycast = RaycastHelper.RaycastBlock(client, maxDistance, includeFluids: false);
            if (!raycast.Item1)
            {
                return MccMcpResult.Ok(new
                {
                    hit = false,
                    maxDistance,
                    playerLocation = ToCoordinate(playerLocation),
                    eyeLocation = ToCoordinate(eyeLocation),
                    location = (object?)null,
                    block = (object?)null,
                    distance = (double?)null,
                    eyeDistance = (double?)null,
                    neighbors = (object?)null
                });
            }

            Location blockLocation = raycast.Item2;
            Block block = raycast.Item3;
            Location targetCenter = blockLocation.ToCenter();
            object? neighbors = includeNeighbors ? GetNeighborBlockSnapshot(client.GetWorld(), blockLocation) : null;

            return MccMcpResult.Ok(new
            {
                hit = true,
                maxDistance,
                playerLocation = ToCoordinate(playerLocation),
                eyeLocation = ToCoordinate(eyeLocation),
                location = ToCoordinate(blockLocation),
                block = ToBlockState(block),
                distance = playerLocation.Distance(targetCenter),
                eyeDistance = eyeLocation.Distance(targetCenter),
                neighbors
            });
        });
    }

    public MccMcpResult PreviewPath(double x, double y, double z, bool allowUnsafe, int maxOffset, int minOffset, int timeoutMs, int maxWaypoints)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.PreviewPath(x, y, z, allowUnsafe, maxOffset, minOffset, timeoutMs, maxWaypoints));
    }

    public MccMcpResult GetPlayersList()
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return client.InvokeOnMainThread(() => MccMcpResult.Ok(new
        {
            players = client.GetOnlinePlayers()
        }));
    }

    public MccMcpResult GetPlayersDetailed(bool includeSelf, bool includeCoordinates)
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.GetPlayersDetailed(includeSelf, includeCoordinates));
    }

    public MccMcpResult GetPlayerStats()
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return client.InvokeOnMainThread(() =>
        {
            Location location = client.GetCurrentLocation();
            return MccMcpResult.Ok(new
            {
                username = client.GetUsername(),
                health = client.GetHealth(),
                saturation = client.GetSaturation(),
                level = client.GetLevel(),
                totalExperience = client.GetTotalExperience(),
                gamemode = client.GetGamemode(),
                playerEntityId = client.GetPlayerEntityID(),
                currentSlot = client.GetCurrentSlot() + 1,
                yaw = client.GetYaw(),
                pitch = client.GetPitch(),
                location = ToCoordinate(location),
                tps = client.GetServerTPS()
            });
        });
    }

    public MccMcpResult GetStatusEffects()
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return client.InvokeOnMainThread(() =>
        {
            var effects = client.GetPlayerEffects()
                .Values
                .Where(effect => !effect.IsExpired)
                .OrderBy(effect => effect.Effect)
                .Select(effect => new
                {
                    id = effect.Effect.ToString(),
                    name = effect.GetDisplayName(),
                    amplifier = effect.Amplifier,
                    remainingSeconds = effect.RemainingSeconds,
                    isInfinite = effect.IsInfinite
                })
                .ToArray();

            return MccMcpResult.Ok(new
            {
                count = effects.Length,
                effects
            });
        });
    }

    public MccMcpResult GetRecentEvents(long afterId, int maxCount, string? typeFilter)
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return MccMcpResult.Ok(game.GetRecentEvents(afterId, maxCount, typeFilter));
    }

    public MccMcpResult GetLoadedBots()
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return client.InvokeOnMainThread(() =>
        {
            var bots = client.GetLoadedChatBots()
                .Select(bot => new
                {
                    name = bot.GetType().Name,
                    fullTypeName = bot.GetType().FullName,
                    isScript = bot is MinecraftClient.ChatBots.Script
                })
                .OrderBy(bot => bot.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return MccMcpResult.Ok(new
            {
                count = bots.Length,
                bots
            });
        });
    }

    public MccMcpResult GetChatHistory(int maxCount, bool includeJson)
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        if (GetClient() is null)
            return NotConnected();

        return MccMcpResult.Ok(game.GetChatHistory(maxCount, includeJson));
    }

    public MccMcpResult GetInternalCommands()
    {
        if (!IsCategoryEnabled(t => t.ChatAndCommands))
            return MccMcpResult.Fail("capability_disabled");

        Type[] commandTypes = Program.GetTypesInNamespace("MinecraftClient.Commands");
        List<InternalCommandInfo> commands = new();

        foreach (Type type in commandTypes)
        {
            if (!type.IsSubclassOf(typeof(Command)))
                continue;

            try
            {
                if (Activator.CreateInstance(type) is Command cmd)
                {
                    commands.Add(new InternalCommandInfo
                    {
                        Name = cmd.CmdName,
                        Usage = cmd.CmdUsage,
                        Description = ChatBot.GetVerbatim(cmd.CmdDesc)
                    });
                }
            }
            catch
            {
                // ignore command constructors that fail for reflection-only list generation.
            }
        }

        InternalCommandInfo[] ordered = commands
            .OrderBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return MccMcpResult.Ok(new
        {
            count = ordered.Length,
            commands = ordered.Select(command => new
            {
                name = command.Name,
                usage = command.Usage,
                description = command.Description
            }).ToArray()
        });
    }

    public MccMcpResult GetMaterialsList(string? filter, int maxCount)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        int limit = Math.Clamp(maxCount, 1, 5000);
        string? normalizedFilter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();
        Material[] allMaterials = Enum.GetValues<Material>();
        var materials = allMaterials
            .Select(material => new
            {
                name = material.ToString(),
                typeLabel = GetMaterialTypeLabel(material)
            })
            .Where(material => normalizedFilter is null
                || TextMatchesFilter(material.name, normalizedFilter)
                || TextMatchesFilter(material.typeLabel, normalizedFilter))
            .OrderBy(material => material.name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();

        return MccMcpResult.Ok(new
        {
            total = allMaterials.Length,
            count = materials.Length,
            filter = normalizedFilter,
            materials
        });
    }

    public MccMcpResult GetBlockTypesList(string? filter, int maxCount)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        int limit = Math.Clamp(maxCount, 1, 5000);
        string? normalizedFilter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();
        Material[] allMaterials = Enum.GetValues<Material>();
        var blockTypes = allMaterials
            .Select(material => new
            {
                name = material.ToString(),
                typeLabel = GetMaterialTypeLabel(material)
            })
            .Where(blockType => normalizedFilter is null
                || TextMatchesFilter(blockType.name, normalizedFilter)
                || TextMatchesFilter(blockType.typeLabel, normalizedFilter))
            .OrderBy(blockType => blockType.name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();

        return MccMcpResult.Ok(new
        {
            total = allMaterials.Length,
            count = blockTypes.Length,
            filter = normalizedFilter,
            blockTypes
        });
    }

    public MccMcpResult GetEntityTypesList(string? filter, int maxCount)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        int limit = Math.Clamp(maxCount, 1, 5000);
        string? normalizedFilter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();
        EntityType[] allEntityTypes = Enum.GetValues<EntityType>();
        var entityTypes = allEntityTypes
            .Select(entityType => new
            {
                name = entityType.ToString(),
                typeLabel = Entity.GetTypeString(entityType)
            })
            .Where(entityType => normalizedFilter is null
                || TextMatchesFilter(entityType.name, normalizedFilter)
                || TextMatchesFilter(entityType.typeLabel, normalizedFilter))
            .OrderBy(entityType => entityType.name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();

        return MccMcpResult.Ok(new
        {
            total = allEntityTypes.Length,
            count = entityTypes.Length,
            filter = normalizedFilter,
            entityTypes
        });
    }

    public MccMcpResult SendChat(string text)
    {
        if (!IsCategoryEnabled(t => t.ChatAndCommands))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(text))
            return MccMcpResult.Fail("invalid_args");

        string normalized = text.Trim();
        if (normalized.Equals("quit", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            return MccMcpResult.Fail("internal_command_text_blocked");
        }

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        bool sent = client.InvokeOnMainThread(() =>
        {
            client.SendText(normalized);
            return true;
        });

        return sent ? MccMcpResult.Ok() : MccMcpResult.Fail("action_failed");
    }

    public MccMcpResult QuitClient()
    {
        if (!IsCategoryEnabled(t => t.ChatAndCommands))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        _ = Task.Run(async () =>
        {
            await Task.Delay(150).ConfigureAwait(false);
            Program.Exit();
        });

        return MccMcpResult.Ok(new { quitting = true });
    }

    public MccMcpResult DisconnectClient()
    {
        if (!IsCategoryEnabled(t => t.ChatAndCommands))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        _ = Task.Run(async () =>
        {
            await Task.Delay(150).ConfigureAwait(false);
            client.Disconnect();
        });

        return MccMcpResult.Ok(new { disconnecting = true });
    }

    public MccMcpResult Respawn()
    {
        if (!IsCategoryEnabled(t => t.ChatAndCommands))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        float health = client.InvokeOnMainThread(client.GetHealth);
        if (health > 0)
        {
            return MccMcpResult.Fail("invalid_state", data: new
            {
                health
            });
        }

        bool ok = client.InvokeOnMainThread(client.SendRespawnPacket);
        return ok
            ? MccMcpResult.Ok(new { success = true })
            : MccMcpResult.Fail("action_failed", data: new { success = false });
    }

    public MccMcpResult RunInternalCommand(string command)
    {
        if (!IsCategoryEnabled(t => t.ChatAndCommands))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(command))
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        return ExecuteInternalCommand(client, command.Trim());
    }

    public MccMcpResult PlayAnimation(string hand)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(hand) || !Enum.TryParse(hand, true, out Hand parsedHand))
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        int animation = parsedHand == Hand.MainHand ? 1 : 0;
        bool ok = client.DoAnimation(animation);
        object resultData = new { success = ok, hand = parsedHand.ToString() };
        return ok
            ? MccMcpResult.Ok(resultData)
            : MccMcpResult.Fail("action_failed", data: resultData);
    }

    public MccMcpResult ToggleSneak(bool enabled)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        EntityActionType action = enabled ? EntityActionType.StartSneaking : EntityActionType.StopSneaking;
        bool ok = client.InvokeOnMainThread(() =>
        {
            bool actionResult = client.SendEntityAction(action);
            if (actionResult)
                client.IsSneaking = enabled;
            return actionResult;
        });

        object resultData = new { success = ok, enabled };
        return ok
            ? MccMcpResult.Ok(resultData)
            : MccMcpResult.Fail("action_failed", data: resultData);
    }

    public MccMcpResult ToggleSprint(bool enabled)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        EntityActionType action = enabled ? EntityActionType.StartSprinting : EntityActionType.StopSprinting;
        bool ok = client.SendEntityAction(action);
        object resultData = new { success = ok, enabled };
        return ok
            ? MccMcpResult.Ok(resultData)
            : MccMcpResult.Fail("action_failed", data: resultData);
    }

    public MccMcpResult UseItemOnHand()
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        bool ok = client.InvokeOnMainThread(() => client.UseItemOnHand());
        return MccMcpResult.Ok(new { success = ok });
    }

    public MccMcpResult ChangeHotbarSlot(int slot)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");

        if (slot is < 1 or > 9)
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetInventoryEnabled())
            return MccMcpResult.Fail("feature_disabled");

        bool ok = client.InvokeOnMainThread(() => client.ChangeSlot((short)(slot - 1)));
        return MccMcpResult.Ok(new { success = ok, slot });
    }

    public MccMcpResult SelectHotbarItem(string itemType, bool preferLowestSlot)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.SelectHotbarItem(itemType, preferLowestSlot));
    }

    public MccMcpResult UseItemOnBlock(double x, double y, double z)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        string sx = x.ToString(CultureInfo.InvariantCulture);
        string sy = y.ToString(CultureInfo.InvariantCulture);
        string sz = z.ToString(CultureInfo.InvariantCulture);
        return ExecuteInternalCommand(client, $"useitem {sx} {sy} {sz}");
    }

    public MccMcpResult DigBlock(double x, double y, double z, double durationSeconds)
    {
        return DigBlockAsync(x, y, z, durationSeconds).GetAwaiter().GetResult();
    }

    public async Task<MccMcpResult> DigBlockAsync(double x, double y, double z, double durationSeconds)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        if (durationSeconds < 0)
        {
            return MccMcpResult.Fail("invalid_args", data: new
            {
                parameter = "durationSeconds",
                min = 0
            });
        }

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        Location target = ToBlockLocation(x, y, z);
        Location currentLocation = client.InvokeOnMainThread(client.GetCurrentLocation);
        Location eyesLocation = currentLocation.EyesLocation();
        Location centeredTarget = target.ToCenter();
        Block beforeBlock = client.InvokeOnMainThread(() => client.GetWorld().GetBlock(target));
        if (beforeBlock.Type == Material.Air)
        {
            return MccMcpResult.Fail("invalid_state", data: new
            {
                target = ToCoordinate(target),
                beforeBlock = ToBlockState(beforeBlock)
            });
        }

        double distance = eyesLocation.Distance(centeredTarget);
        if (distance > DigReachDistance)
        {
            return MccMcpResult.Fail("action_incomplete", data: new
            {
                reason = "too_far",
                target = ToCoordinate(target),
                playerLocation = ToCoordinate(currentLocation),
                distance,
                maxReach = DigReachDistance,
                beforeBlock = ToBlockState(beforeBlock)
            });
        }

        double[] attemptDurations = GetDigAttemptDurations(durationSeconds);
        List<double> attemptedDurations = new();
        Block afterBlock = beforeBlock;
        bool changed = false;
        bool commandAccepted = false;

        foreach (double attemptDuration in attemptDurations)
        {
            attemptedDurations.Add(attemptDuration);
            bool accepted = client.InvokeOnMainThread(() => client.DigBlock(target, Direction.Down, duration: attemptDuration));
            commandAccepted |= accepted;
            if (!accepted)
                continue;

            (bool blockChanged, Block updatedBlock) =
                await WaitForBlockChangeAsync(client, target, beforeBlock, GetDigVerifyWaitMs(attemptDuration));
            afterBlock = updatedBlock;
            if (blockChanged)
            {
                changed = true;
                break;
            }
        }

        afterBlock = client.InvokeOnMainThread(() => client.GetWorld().GetBlock(target));
        object resultData = new
        {
            success = changed,
            target = ToCoordinate(target),
            beforeBlock = ToBlockState(beforeBlock),
            afterBlock = ToBlockState(afterBlock),
            commandAccepted,
            changed,
            destroyed = changed && afterBlock.Type == Material.Air,
            attempts = attemptedDurations.Count,
            attemptedDurationsSeconds = attemptedDurations.ToArray(),
            distance,
            playerLocation = ToCoordinate(currentLocation)
        };

        return changed
            ? MccMcpResult.Ok(resultData)
            : MccMcpResult.Fail("action_incomplete", data: resultData);
    }

    public MccMcpResult PlaceBlock(int x, int y, int z, string face, string hand, bool lookAtBlock)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        if (!Enum.TryParse(face, true, out Direction parsedFace))
            return MccMcpResult.Fail("invalid_args");

        if (!Enum.TryParse(hand, true, out Hand parsedHand))
            return MccMcpResult.Fail("invalid_args");

        Location location = new(x, y, z);
        bool ok = client.InvokeOnMainThread(() => client.PlaceBlock(location, parsedFace, parsedHand, lookAtBlock));
        return MccMcpResult.Ok(new { success = ok, x, y, z, face = parsedFace.ToString(), hand = parsedHand.ToString(), lookAtBlock });
    }

    public MccMcpResult InteractEntity(int entityId, string interaction, string hand)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetEntityHandlingEnabled())
            return MccMcpResult.Fail("feature_disabled");

        if (!Enum.TryParse(interaction, true, out InteractType interactType))
            return MccMcpResult.Fail("invalid_args");

        if (!Enum.TryParse(hand, true, out Hand parsedHand))
            return MccMcpResult.Fail("invalid_args");

        bool ok = client.InvokeOnMainThread(() => client.InteractEntity(entityId, interactType, parsedHand));
        return MccMcpResult.Ok(new { success = ok, entityId, interaction = interactType.ToString(), hand = parsedHand.ToString() });
    }

    public MccMcpResult AttackEntity(int entityId)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetEntityHandlingEnabled())
            return MccMcpResult.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            if (!client.GetEntities().ContainsKey(entityId))
                return MccMcpResult.Fail("invalid_state", data: new { entityId });

            bool ok = client.InteractEntity(entityId, InteractType.Attack);
            object resultData = new
            {
                success = ok,
                entityId,
                interaction = InteractType.Attack.ToString()
            };

            return ok
                ? MccMcpResult.Ok(resultData)
                : MccMcpResult.Fail("action_failed", data: resultData);
        });
    }

    public MccMcpResult ScanNearbyBlocks(int radius, int maxCount, string? materialFilter)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        if (radius is < 1 or > MaxBlockScanRadius)
        {
            return MccMcpResult.Fail("invalid_args", data: new
            {
                parameter = "radius",
                min = 1,
                max = MaxBlockScanRadius
            });
        }

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        int limit = Math.Clamp(maxCount, 1, 2000);
        string? filter = string.IsNullOrWhiteSpace(materialFilter) ? null : materialFilter.Trim();

        return client.InvokeOnMainThread(() =>
        {
            Location playerLocation = client.GetCurrentLocation();
            int cx = (int)Math.Floor(playerLocation.X);
            int cy = (int)Math.Floor(playerLocation.Y) - 1;
            int cz = (int)Math.Floor(playerLocation.Z);

            List<object> found = new();
            World world = client.GetWorld();
            for (int y = cy - radius; y <= cy + radius && found.Count < limit; y++)
            {
                for (int z = cz - radius; z <= cz + radius && found.Count < limit; z++)
                {
                    for (int x = cx - radius; x <= cx + radius && found.Count < limit; x++)
                    {
                        Block block = world.GetBlock(new Location(x, y, z));
                        if (block.Type == Material.Air)
                            continue;

                        string material = block.Type.ToString();
                        string typeLabel = block.GetTypeString();
                        if (filter is not null
                            && !TextMatchesFilter(material, filter)
                            && !TextMatchesFilter(typeLabel, filter))
                        {
                            continue;
                        }

                        double dx = x + 0.5 - playerLocation.X;
                        double dy = y + 0.5 - playerLocation.Y;
                        double dz = z + 0.5 - playerLocation.Z;
                        found.Add(new
                        {
                            x,
                            y,
                            z,
                            material,
                            typeLabel,
                            blockId = block.BlockId,
                            blockMeta = block.BlockMeta,
                            distance = Math.Sqrt(dx * dx + dy * dy + dz * dz)
                        });
                    }
                }
            }

            return MccMcpResult.Ok(new
            {
                center = new { x = cx, y = cy, z = cz },
                radius,
                count = found.Count,
                blocks = found.ToArray()
            });
        });
    }

    public MccMcpResult FindBlocks(string? query, int radius, int maxCount, bool exactMatch)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        if (radius is < 1 or > MaxBlockFindRadius)
        {
            return MccMcpResult.Fail("invalid_args", data: new
            {
                parameter = "radius",
                min = 1,
                max = MaxBlockFindRadius
            });
        }

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        int limit = Math.Clamp(maxCount, 1, 5000);
        string? filter = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        ParseBlockQuery(filter, out int? blockIdFilter, out int? blockMetaFilter);

        return client.InvokeOnMainThread(() =>
        {
            Location playerLocation = client.GetCurrentLocation();
            int cx = (int)Math.Floor(playerLocation.X);
            int cy = (int)Math.Floor(playerLocation.Y) - 1;
            int cz = (int)Math.Floor(playerLocation.Z);

            List<(int x, int y, int z, string material, string typeLabel, int blockId, byte blockMeta, double distance)> found = new();
            World world = client.GetWorld();

            for (int y = cy - radius; y <= cy + radius && found.Count < limit; y++)
            {
                for (int z = cz - radius; z <= cz + radius && found.Count < limit; z++)
                {
                    for (int x = cx - radius; x <= cx + radius && found.Count < limit; x++)
                    {
                        Block block = world.GetBlock(new Location(x, y, z));
                        if (block.Type == Material.Air)
                            continue;

                        if (!BlockMatches(block, filter, exactMatch, blockIdFilter, blockMetaFilter))
                            continue;

                        double dx = x + 0.5 - playerLocation.X;
                        double dy = y + 0.5 - playerLocation.Y;
                        double dz = z + 0.5 - playerLocation.Z;

                        found.Add((
                            x,
                            y,
                            z,
                            block.Type.ToString(),
                            block.GetTypeString(),
                            block.BlockId,
                            block.BlockMeta,
                            Math.Sqrt(dx * dx + dy * dy + dz * dz)));
                    }
                }
            }

            return MccMcpResult.Ok(new
            {
                center = new { x = cx, y = cy, z = cz },
                radius,
                query = filter,
                exactMatch,
                count = found.Count,
                blocks = found
                    .OrderBy(entry => entry.distance)
                    .Select(entry => new
                    {
                        entry.x,
                        entry.y,
                        entry.z,
                        entry.material,
                        entry.typeLabel,
                        entry.blockId,
                        entry.blockMeta,
                        entry.distance
                    })
                    .ToArray()
            });
        });
    }

    public MccMcpResult CanReachPosition(double x, double y, double z, bool allowUnsafe, int maxOffset, int minOffset, int timeoutMs)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        if (!AreValidPathOffsets(maxOffset, minOffset) || timeoutMs < 0)
        {
            return MccMcpResult.Fail("invalid_args", data: new
            {
                maxOffset,
                minOffset,
                timeoutMs
            });
        }

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

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
            ? GetDistance(waypoint, goal)
            : null;

        return MccMcpResult.Ok(new
        {
            reachable = path is not null,
            exactReachable = finalWaypoint is Location location && location.ToFloor() == goal.ToFloor(),
            target = ToCoordinate(goal),
            startLocation = ToCoordinate(startLocation),
            finalWaypoint = finalWaypoint is Location finalLocation ? ToCoordinate(finalLocation) : null,
            finalDistance,
            waypointCount = path?.Count ?? 0,
            allowUnsafe,
            maxOffset,
            minOffset,
            timeoutMs = effectiveTimeoutMs
        });
    }

    public MccMcpResult IsPlayerNearby(string? playerName, double radius, bool includeSelf)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.IsPlayerNearby(playerName, radius, includeSelf));
    }

    public MccMcpResult LocatePlayer(string playerName, bool includeSelf)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.LocatePlayer(playerName, includeSelf));
    }

    public MccMcpResult FindNearestEntity(string? typeFilter, string? nameFilter, double radius, bool includePlayers)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.FindNearestEntity(typeFilter, nameFilter, radius, includePlayers));
    }

    public MccMcpResult MoveTo(double x, double y, double z, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs)
    {
        return MoveToAsync(x, y, z, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeoutMs).GetAwaiter().GetResult();
    }

    public async Task<MccMcpResult> MoveToAsync(double x, double y, double z, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        if (!AreValidPathOffsets(maxOffset, minOffset) || timeoutMs < 0)
        {
            return MccMcpResult.Fail("invalid_args", data: new
            {
                maxOffset,
                minOffset,
                timeoutMs
            });
        }

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        Location goal = new(x, y, z);
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
        object resultData = new
        {
            pathFound,
            arrived,
            tolerance,
            verifyWaitMs,
            target = ToCoordinate(goal),
            startLocation = ToCoordinate(startLocation),
            finalLocation = ToCoordinate(finalLocation),
            finalDistance = GetDistance(finalLocation, goal),
            distanceMoved = GetDistance(startLocation, finalLocation),
            allowUnsafe,
            allowDirectTeleport,
            maxOffset,
            minOffset,
            timeoutMs
        };

        return pathFound && arrived
            ? MccMcpResult.Ok(resultData)
            : MccMcpResult.Fail("action_incomplete", data: resultData);
    }

    public MccMcpResult MoveToPlayer(string playerName, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs)
    {
        return MoveToPlayerAsync(playerName, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeoutMs).GetAwaiter().GetResult();
    }

    public async Task<MccMcpResult> MoveToPlayerAsync(string playerName, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(await game.MoveToPlayerAsync(playerName, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeoutMs));
    }

    public MccMcpResult LookAt(double x, double y, double z)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Location current = client.GetCurrentLocation();
            Location target = new(x, y, z);
            client.UpdateLocation(current, target);
            bool success = client.SendLocationUpdate();
            return success
                ? MccMcpResult.Ok(new
                {
                    success,
                    yaw = client.GetYaw(),
                    pitch = client.GetPitch(),
                    location = ToCoordinate(current),
                    target = ToCoordinate(target)
                })
                : MccMcpResult.Fail("action_failed", data: new
                {
                    success,
                    target = ToCoordinate(target)
                });
        });
    }

    public MccMcpResult LookDirection(string direction)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(direction) || !Enum.TryParse(direction, true, out Direction parsedDirection) || !IsSupportedLookDirection(parsedDirection))
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Location current = client.GetCurrentLocation();
            client.UpdateLocation(current, parsedDirection);
            bool success = client.SendLocationUpdate();
            return success
                ? MccMcpResult.Ok(new
            {
                success,
                direction = parsedDirection.ToString(),
                yaw = client.GetYaw(),
                pitch = client.GetPitch(),
                location = ToCoordinate(current)
            })
                : MccMcpResult.Fail("action_failed", data: new
            {
                success,
                direction = parsedDirection.ToString()
            });
        });
    }

    public MccMcpResult LookAngles(float yaw, float pitch)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Location current = client.GetCurrentLocation();
            client.UpdateLocation(current, yaw, pitch);
            bool success = client.SendLocationUpdate();
            return success
                ? MccMcpResult.Ok(new
            {
                success,
                yaw = client.GetYaw(),
                pitch = client.GetPitch(),
                location = ToCoordinate(current)
            })
                : MccMcpResult.Fail("action_failed", data: new
            {
                success,
                yaw,
                pitch,
                location = ToCoordinate(current)
            });
        });
    }

    public MccMcpResult GetInventorySnapshot(int inventoryId)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.GetInventorySnapshot(inventoryId));
    }

    public MccMcpResult SearchInventories(string query, int maxCount, bool exactMatch, bool includeContainers)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.SearchInventories(query, maxCount, exactMatch, includeContainers));
    }

    public MccMcpResult ListInventories()
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.ListInventories());
    }

    public MccMcpResult OpenContainerAt(int x, int y, int z, int timeoutMs, bool closeCurrent)
    {
        return OpenContainerAtAsync(x, y, z, timeoutMs, closeCurrent).GetAwaiter().GetResult();
    }

    public async Task<MccMcpResult> OpenContainerAtAsync(int x, int y, int z, int timeoutMs, bool closeCurrent)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetInventoryEnabled() || !client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        Location location = new(x, y, z);
        int waitMs = GetContainerWaitMs(timeoutMs);
        (Block block, int activeContainerId) state = client.InvokeOnMainThread(() =>
        {
            Block block = client.GetWorld().GetBlock(location);
            return (block, GetActiveContainerId(client));
        });

        if (!IsInteractableContainerMaterial(state.block.Type))
        {
            return MccMcpResult.Fail("invalid_state", data: new
            {
                x,
                y,
                z,
                block = ToBlockState(state.block),
                activeContainerId = state.activeContainerId
            });
        }

        return await OpenContainerCoreAsync(client, location, state.block, state.activeContainerId, waitMs, closeCurrent);
    }

    public MccMcpResult CloseContainer(int inventoryId, int timeoutMs)
    {
        return CloseContainerAsync(inventoryId, timeoutMs).GetAwaiter().GetResult();
    }

    public async Task<MccMcpResult> CloseContainerAsync(int inventoryId, int timeoutMs)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetInventoryEnabled())
            return MccMcpResult.Fail("feature_disabled");

        int waitMs = GetContainerWaitMs(timeoutMs);
        int resolvedInventoryId = client.InvokeOnMainThread(() => ResolveContainerInventoryId(client, inventoryId));
        if (resolvedInventoryId <= 0)
        {
            if (inventoryId < 0)
            {
                return MccMcpResult.Ok(new
                {
                    success = true,
                    closed = false
                });
            }

            return MccMcpResult.Fail("invalid_state", data: new { inventoryId });
        }

        bool closeAccepted = client.CloseInventory(resolvedInventoryId);
        bool closed = closeAccepted && await WaitForContainerCloseAsync(client, resolvedInventoryId, waitMs);
        var resultData = new
        {
            success = closeAccepted && closed,
            closeAccepted,
            closed,
            inventoryId = resolvedInventoryId,
            timeoutMs = waitMs
        };

        return closeAccepted && closed
            ? MccMcpResult.Ok(resultData)
            : MccMcpResult.Fail("action_incomplete", data: resultData);
    }

    public MccMcpResult InventoryWindowAction(int inventoryId, int slotId, string actionType)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(actionType))
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetInventoryEnabled())
            return MccMcpResult.Fail("feature_disabled");

        if (!TryParseWindowAction(actionType, out WindowActionType parsedAction))
            return MccMcpResult.Fail("invalid_args");

        bool ok = client.InvokeOnMainThread(() => client.DoWindowAction(inventoryId, slotId, parsedAction));
        return MccMcpResult.Ok(new { success = ok, normalizedActionType = parsedAction.ToString() });
    }

    public MccMcpResult DropInventoryItem(string itemType, int count, int inventoryId, bool preferStack)
    {
        return DropInventoryItemAsync(itemType, count, inventoryId, preferStack).GetAwaiter().GetResult();
    }

    public async Task<MccMcpResult> DropInventoryItemAsync(string itemType, int count, int inventoryId, bool preferStack)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(itemType) || count <= 0)
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetInventoryEnabled())
            return MccMcpResult.Fail("feature_disabled");

        if (!TryParseItemType(itemType, out ItemType parsedItemType))
        {
            return MccMcpResult.Fail("invalid_args", data: new
            {
                itemType = itemType.Trim()
            });
        }

        return await Task.Run(async () =>
        {
            Container? inventory = client.GetInventory(inventoryId);
            if (inventory is null)
                return MccMcpResult.Fail("invalid_state");

            int cursorCount = GetCursorItemCount(inventory, parsedItemType);
            var matchingSlotQuery = inventory.Items
                .Where(pair => IsDroppableInventorySlot(inventory, pair.Key))
                .Where(pair => pair.Value.Type == parsedItemType && pair.Value.Count > 0)
                .Select(pair => new
                {
                    slot = pair.Key,
                    count = pair.Value.Count,
                    hotbarPriority = inventoryId == 0 && IsHotbarSlot(inventory, pair.Key) ? 0 : 1
                });
            var matchingSlots = (preferStack
                    ? matchingSlotQuery.OrderBy(pair => pair.hotbarPriority).ThenByDescending(pair => pair.count).ThenBy(pair => pair.slot)
                    : matchingSlotQuery.OrderBy(pair => pair.hotbarPriority).ThenBy(pair => pair.count).ThenBy(pair => pair.slot))
                .ToArray();

            int beforeCount = matchingSlots.Sum(pair => pair.count);
            if (beforeCount < count)
            {
                return MccMcpResult.Fail("invalid_state", data: new
                {
                    itemType = parsedItemType.ToString(),
                    requestedCount = count,
                    availableCount = beforeCount,
                    cursorCount,
                    inventoryId
                });
            }

            int remaining = count;
            List<int> touchedSlots = new();

            foreach (var entry in matchingSlots)
            {
                if (remaining <= 0)
                    break;

                if (!inventory.Items.TryGetValue(entry.slot, out Item? currentItem) || currentItem.Count <= 0)
                    continue;

                int dropFromSlot = Math.Min(remaining, currentItem.Count);
                touchedSlots.Add(entry.slot);
                (bool ok, int droppedFromSlot) = await TryDropInventorySlotItemsAsync(client, inventoryId, inventory, entry.slot, parsedItemType, dropFromSlot);

                if (!ok)
                {
                    Container? failedInventory = client.GetInventory(inventoryId);
                    int currentCount = failedInventory is null
                        ? 0
                        : failedInventory.Items
                            .Where(pair => IsDroppableInventorySlot(failedInventory, pair.Key))
                            .Where(pair => pair.Value.Type == parsedItemType)
                            .Sum(pair => pair.Value.Count);
                    return MccMcpResult.Fail("action_failed", data: new
                    {
                        itemType = parsedItemType.ToString(),
                        requestedCount = count,
                        droppedCount = count - remaining + droppedFromSlot,
                        remainingCount = remaining,
                        currentCount,
                        inventoryId,
                        touchedSlots = touchedSlots.ToArray()
                    });
                }

                remaining -= droppedFromSlot;
            }

            Container? finalInventory = client.GetInventory(inventoryId);
            int afterCount = finalInventory is null
                ? 0
                : finalInventory.Items
                    .Where(pair => IsDroppableInventorySlot(finalInventory, pair.Key))
                    .Where(pair => pair.Value.Type == parsedItemType)
                    .Sum(pair => pair.Value.Count);
            int droppedCount = beforeCount - afterCount;

            if (remaining > 0)
            {
                return MccMcpResult.Fail("action_failed", data: new
                {
                    itemType = parsedItemType.ToString(),
                    requestedCount = count,
                    droppedCount,
                    remainingCount = remaining,
                    beforeCount,
                    afterCount,
                    inventoryId,
                    touchedSlots = touchedSlots.ToArray()
                });
            }

            return MccMcpResult.Ok(new
            {
                success = true,
                itemType = parsedItemType.ToString(),
                requestedCount = count,
                droppedCount,
                beforeCount,
                afterCount,
                inventoryId,
                touchedSlots = touchedSlots.ToArray()
            });
        });
    }

    public MccMcpResult DepositContainerItem(string itemType, int count, int inventoryId, bool preferLargestStack)
    {
        return DepositContainerItemAsync(itemType, count, inventoryId, preferLargestStack).GetAwaiter().GetResult();
    }

    public Task<MccMcpResult> DepositContainerItemAsync(string itemType, int count, int inventoryId, bool preferLargestStack)
    {
        return TransferContainerItemAsync(itemType, count, inventoryId, preferLargestStack, InventoryTransferDirection.Deposit);
    }

    public MccMcpResult WithdrawContainerItem(string itemType, int count, int inventoryId, bool preferLargestStack)
    {
        return WithdrawContainerItemAsync(itemType, count, inventoryId, preferLargestStack).GetAwaiter().GetResult();
    }

    public Task<MccMcpResult> WithdrawContainerItemAsync(string itemType, int count, int inventoryId, bool preferLargestStack)
    {
        return TransferContainerItemAsync(itemType, count, inventoryId, preferLargestStack, InventoryTransferDirection.Withdraw);
    }

    public MccMcpResult QueryEntities(int maxCount)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.QueryEntities(maxCount));
    }

    public MccMcpResult ListEntities(int maxCount, string? typeFilter, double radius)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.ListEntities(maxCount, typeFilter, radius));
    }

    public MccMcpResult GetEntityInfo(int entityId, bool includeMetadata, bool includeEquipment, bool includeEffects)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.GetEntityInfo(entityId, includeMetadata, includeEquipment, includeEffects));
    }

    public MccMcpResult FindSigns(string text, bool exactMatch, int radius, int maxCount, bool includeBackText)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(text) || radius is < 1 or > MaxBlockFindRadius)
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        string filter = text.Trim();
        int limit = Math.Clamp(maxCount, 1, 500);

        return client.InvokeOnMainThread(() =>
        {
            Location playerLocation = client.GetCurrentLocation();
            World world = client.GetWorld();
            var signs = client.GetKnownSigns()
                .Select(sign =>
                {
                    double dx = sign.location.X + 0.5 - playerLocation.X;
                    double dy = sign.location.Y + 0.5 - playerLocation.Y;
                    double dz = sign.location.Z + 0.5 - playerLocation.Z;
                    return new
                    {
                        sign,
                        distance = Math.Sqrt(dx * dx + dy * dy + dz * dz)
                    };
                })
                .Where(entry => entry.distance <= radius)
                .Where(entry => IsSignMaterial(world.GetBlock(entry.sign.location).Type))
                .Select(entry =>
                {
                    string[] frontText = entry.sign.frontText.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                    string[] backText = includeBackText
                        ? entry.sign.backText.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray()
                        : [];
                    string[] matchedLines = frontText
                        .Concat(backText)
                        .Where(line => exactMatch ? TextEqualsFilter(line, filter) : TextMatchesFilter(line, filter))
                        .Distinct(NameComparer)
                        .ToArray();

                    return new
                    {
                        entry.sign,
                        entry.distance,
                        frontText,
                        backText,
                        matchedLines
                    };
                })
                .Where(entry => entry.matchedLines.Length > 0)
                .OrderBy(entry => entry.distance)
                .Take(limit)
                .Select(entry => new
                {
                    x = (int)Math.Floor(entry.sign.location.X),
                    y = (int)Math.Floor(entry.sign.location.Y),
                    z = (int)Math.Floor(entry.sign.location.Z),
                    material = entry.sign.material,
                    typeLabel = entry.sign.typeLabel,
                    distance = entry.distance,
                    isWaxed = entry.sign.isWaxed,
                    frontText = entry.frontText,
                    backText = entry.backText,
                    matchedLines = entry.matchedLines
                })
                .ToArray();

            return MccMcpResult.Ok(new
            {
                text = filter,
                exactMatch,
                radius,
                includeBackText,
                count = signs.Length,
                signs
            });
        });
    }

    public MccMcpResult ListItemEntities(string? itemType, double radius, int maxCount)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(game.ListItemEntities(itemType, radius, maxCount));
    }

    public MccMcpResult PickupItems(string itemType, double radius, int maxItems, bool allowUnsafe, int timeoutMs)
    {
        return PickupItemsAsync(itemType, radius, maxItems, allowUnsafe, timeoutMs).GetAwaiter().GetResult();
    }

    public async Task<MccMcpResult> PickupItemsAsync(string itemType, double radius, int maxItems, bool allowUnsafe, int timeoutMs)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld) || !IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");
        return ToMcpResult(await game.PickupItemsAsync(itemType, radius, maxItems, allowUnsafe, timeoutMs));
    }

    public MccMcpResult GetWorldBlockAt(int x, int y, int z)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Location location = new(x, y, z);
            Block block = client.GetWorld().GetBlock(location);
            return MccMcpResult.Ok(new
            {
                x,
                y,
                z,
                material = block.Type.ToString(),
                blockId = block.BlockId,
                blockMeta = block.BlockMeta
            });
        });
    }

    private static async Task<MccMcpResult> OpenContainerCoreAsync(McClient client, Location location, Block block, int activeContainerId, int waitMs, bool closeCurrent)
    {
        if (activeContainerId > 0)
        {
            if (!closeCurrent)
            {
                return MccMcpResult.Fail("invalid_state", data: new
                {
                    reason = "container_already_open",
                    activeContainerId,
                    x = location.X,
                    y = location.Y,
                    z = location.Z,
                    block = ToBlockState(block)
                });
            }

            bool closeAccepted = client.CloseInventory(activeContainerId);
            bool closed = closeAccepted && await WaitForContainerCloseAsync(client, activeContainerId, waitMs);
            if (!closeAccepted || !closed)
            {
                return MccMcpResult.Fail("action_incomplete", data: new
                {
                    action = "close_previous_container",
                    activeContainerId,
                    closeAccepted,
                    closed,
                    timeoutMs = waitMs
                });
            }
        }

        HashSet<int> beforeIds = client.InvokeOnMainThread(() => client.GetInventories().Keys.Where(id => id > 0).ToHashSet());
        int openedInventoryId = 0;
        Container? openedInventory = null;
        bool openAccepted = client.InvokeOnMainThread(() => client.PlaceBlock(location, Direction.Down, Hand.MainHand, lookAtBlock: true));
        bool opened = openAccepted && await WaitForContainerOpenAsync(client, beforeIds, waitMs, result =>
        {
            openedInventoryId = result.InventoryId;
            openedInventory = result.Inventory;
        });
        var resultData = new
        {
            success = openAccepted && opened && openedInventory is not null,
            openAccepted,
            opened,
            timeoutMs = waitMs,
            x = location.X,
            y = location.Y,
            z = location.Z,
            block = ToBlockState(block),
            inventory = openedInventory is null
                ? null
                : new
                {
                    id = openedInventoryId,
                    type = openedInventory.Type.ToString(),
                    title = openedInventory.Title,
                    slotCount = openedInventory.Type.SlotCount(),
                    nonEmptySlots = openedInventory.Items.Count
                }
        };

        return openAccepted && opened && openedInventory is not null
            ? MccMcpResult.Ok(resultData)
            : MccMcpResult.Fail("action_incomplete", data: resultData);
    }

    private MccMcpResult TransferContainerItem(string itemType, int count, int inventoryId, bool preferLargestStack, InventoryTransferDirection direction)
    {
        return TransferContainerItemAsync(itemType, count, inventoryId, preferLargestStack, direction).GetAwaiter().GetResult();
    }

    private async Task<MccMcpResult> TransferContainerItemAsync(string itemType, int count, int inventoryId, bool preferLargestStack, InventoryTransferDirection direction)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(itemType) || count <= 0)
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetInventoryEnabled())
            return MccMcpResult.Fail("feature_disabled");

        if (!TryParseItemType(itemType, out ItemType parsedItemType))
        {
            return MccMcpResult.Fail("invalid_args", data: new
            {
                itemType = itemType.Trim()
            });
        }

        if (TryGetCursorItem(client, out Item? cursorItem))
        {
            return MccMcpResult.Fail("invalid_state", data: new
            {
                reason = "cursor_item_present",
                cursor = new { type = cursorItem!.Type.ToString(), count = cursorItem.Count }
            });
        }

        int resolvedInventoryId = client.InvokeOnMainThread(() => ResolveContainerInventoryId(client, inventoryId));
        if (resolvedInventoryId <= 0)
        {
            return MccMcpResult.Fail("invalid_state", data: new
            {
                inventoryId
            });
        }

        Container? initialInventory = client.InvokeOnMainThread(() => client.GetInventory(resolvedInventoryId));
        if (initialInventory is null)
            return MccMcpResult.Fail("invalid_state", data: new { inventoryId = resolvedInventoryId });

        if (!TryGetContainerSlotRanges(initialInventory.Type, out int containerStart, out int containerEnd, out int playerStart, out int playerEnd))
        {
            return MccMcpResult.Fail("invalid_state", data: new
            {
                reason = "unsupported_container_type",
                inventoryId = resolvedInventoryId,
                type = initialInventory.Type.ToString()
            });
        }

        int sourceStart = direction == InventoryTransferDirection.Deposit ? playerStart : containerStart;
        int sourceEnd = direction == InventoryTransferDirection.Deposit ? playerEnd : containerEnd;
        int targetStart = direction == InventoryTransferDirection.Deposit ? containerStart : playerStart;
        int targetEnd = direction == InventoryTransferDirection.Deposit ? containerEnd : playerEnd;

        int beforePlayerCount = CountItemInRange(initialInventory, parsedItemType, playerStart, playerEnd);
        int beforeContainerCount = CountItemInRange(initialInventory, parsedItemType, containerStart, containerEnd);
        int availableCount = CountItemInRange(initialInventory, parsedItemType, sourceStart, sourceEnd);
        if (availableCount < count)
        {
            return MccMcpResult.Fail("invalid_state", data: new
            {
                itemType = parsedItemType.ToString(),
                requestedCount = count,
                availableCount,
                inventoryId = resolvedInventoryId,
                direction = direction.ToString()
            });
        }

        int remaining = count;
        List<int> touchedSourceSlots = new();
        List<int> touchedTargetSlots = new();

        while (remaining > 0)
        {
            Container? inventory = client.InvokeOnMainThread(() => client.GetInventory(resolvedInventoryId));
            if (inventory is null)
                return MccMcpResult.Fail("invalid_state", data: new { inventoryId = resolvedInventoryId });

            if (TryGetCursorItem(client, out cursorItem))
            {
                return MccMcpResult.Fail("invalid_state", data: new
                {
                    reason = "cursor_item_present_mid_transfer",
                    cursor = new { type = cursorItem!.Type.ToString(), count = cursorItem.Count }
                });
            }

            var sourceSlots = GetOrderedItemSlots(inventory, parsedItemType, sourceStart, sourceEnd, preferLargestStack);
            if (sourceSlots.Length == 0)
                break;

            int beforeSourceCount = CountItemInRange(inventory, parsedItemType, sourceStart, sourceEnd);
            int beforeTargetCount = CountItemInRange(inventory, parsedItemType, targetStart, targetEnd);
            (int slot, int sourceCount) = sourceSlots[0];
            touchedSourceSlots.Add(slot);

            int movedCount;
            List<int> usedTargetSlots = new();
            if (sourceCount <= remaining || direction == InventoryTransferDirection.Withdraw)
            {
                if (!client.DoWindowAction(resolvedInventoryId, slot, WindowActionType.ShiftClick))
                {
                    return MccMcpResult.Fail("action_failed", data: new
                    {
                        itemType = parsedItemType.ToString(),
                        requestedCount = count,
                        remainingCount = remaining,
                        inventoryId = resolvedInventoryId,
                        sourceSlot = slot,
                        direction = direction.ToString()
                    });
                }

                if (direction == InventoryTransferDirection.Withdraw)
                {
                    Container? afterShift = null;
                    int afterSourceCount = beforeSourceCount;
                    if (!await WaitForRangeCountAsync(client, resolvedInventoryId, parsedItemType, sourceStart, sourceEnd, countAfterShift => countAfterShift < beforeSourceCount, DefaultInventoryActionWaitMs, result =>
                        {
                            afterShift = result.Inventory;
                            afterSourceCount = result.ItemCount;
                        },
                        onInitialize: () =>
                        {
                            afterShift = null;
                            afterSourceCount = beforeSourceCount;
                        }))
                    {
                        afterShift = client.InvokeOnMainThread(() => client.GetInventory(resolvedInventoryId));
                        afterSourceCount = afterShift is null ? beforeSourceCount : CountItemInRange(afterShift, parsedItemType, sourceStart, sourceEnd);
                    }

                    movedCount = beforeSourceCount - afterSourceCount;
                }
                else
                {
                    Container? afterShift = null;
                    int afterTargetCount = beforeTargetCount;
                    if (!await WaitForRangeCountAsync(client, resolvedInventoryId, parsedItemType, targetStart, targetEnd, countAfterShift => countAfterShift > beforeTargetCount, DefaultInventoryActionWaitMs, result =>
                        {
                            afterShift = result.Inventory;
                            afterTargetCount = result.ItemCount;
                        },
                        onInitialize: () =>
                        {
                            afterShift = null;
                            afterTargetCount = beforeTargetCount;
                        }))
                    {
                        afterShift = client.InvokeOnMainThread(() => client.GetInventory(resolvedInventoryId));
                        afterTargetCount = afterShift is null ? beforeTargetCount : CountItemInRange(afterShift, parsedItemType, targetStart, targetEnd);
                    }

                    movedCount = afterTargetCount - beforeTargetCount;
                }

                if (direction == InventoryTransferDirection.Withdraw && movedCount > remaining)
                {
                    int excessCount = movedCount - remaining;
                    MccMcpResult returnExcess = await TransferContainerItemAsync(parsedItemType.ToString(), excessCount, resolvedInventoryId, preferLargestStack, InventoryTransferDirection.Deposit);
                    if (!returnExcess.Success)
                    {
                        return MccMcpResult.Fail("action_incomplete", data: new
                        {
                            itemType = parsedItemType.ToString(),
                            requestedCount = count,
                            remainingCount = remaining,
                            inventoryId = resolvedInventoryId,
                            sourceSlot = slot,
                            direction = direction.ToString(),
                            excessCount,
                            returnExcess
                        });
                    }

                    movedCount -= excessCount;
                }
            }
            else
            {
                movedCount = await TransferPartialFromSlotAsync(
                    client,
                    resolvedInventoryId,
                    slot,
                    parsedItemType,
                    remaining,
                    sourceStart,
                    sourceEnd,
                    targetStart,
                    targetEnd,
                    usedTargetSlots);
            }

            if (movedCount <= 0)
            {
                Container? afterFailure = client.InvokeOnMainThread(() => client.GetInventory(resolvedInventoryId));
                return MccMcpResult.Fail("action_incomplete", data: new
                {
                    itemType = parsedItemType.ToString(),
                    requestedCount = count,
                    remainingCount = remaining,
                    inventoryId = resolvedInventoryId,
                    sourceSlot = slot,
                    direction = direction.ToString(),
                    playerCount = afterFailure is null ? 0 : CountItemInRange(afterFailure, parsedItemType, playerStart, playerEnd),
                    containerCount = afterFailure is null ? 0 : CountItemInRange(afterFailure, parsedItemType, containerStart, containerEnd)
                });
            }

            remaining -= movedCount;
            touchedTargetSlots.AddRange(usedTargetSlots);
        }

        Container? finalInventory = client.InvokeOnMainThread(() => client.GetInventory(resolvedInventoryId));
        if (finalInventory is null)
            return MccMcpResult.Fail("invalid_state", data: new { inventoryId = resolvedInventoryId });

        int afterPlayerCount = CountItemInRange(finalInventory, parsedItemType, playerStart, playerEnd);
        int afterContainerCount = CountItemInRange(finalInventory, parsedItemType, containerStart, containerEnd);
        int playerDelta = afterPlayerCount - beforePlayerCount;
        int containerDelta = afterContainerCount - beforeContainerCount;
        int movedTotal = direction == InventoryTransferDirection.Deposit
            ? afterContainerCount - beforeContainerCount
            : beforeContainerCount - afterContainerCount;
        bool countsVerified = direction == InventoryTransferDirection.Deposit
            ? containerDelta == count
            : containerDelta == -count;
        bool playerCountsMatchExpected = direction == InventoryTransferDirection.Deposit
            ? playerDelta == -count
            : playerDelta == count;
        bool succeeded = remaining == 0 && countsVerified;
        var resultData = new
        {
            success = succeeded,
            direction = direction.ToString().ToLowerInvariant(),
            itemType = parsedItemType.ToString(),
            requestedCount = count,
            movedCount = movedTotal,
            beforePlayerCount,
            afterPlayerCount,
            beforeContainerCount,
            afterContainerCount,
            playerDelta,
            containerDelta,
            playerCountsMatchExpected,
            verificationBasis = "container_delta",
            inventoryId = resolvedInventoryId,
            containerType = finalInventory.Type.ToString(),
            touchedSourceSlots = touchedSourceSlots.Distinct().OrderBy(slot => slot).ToArray(),
            touchedTargetSlots = touchedTargetSlots.Distinct().OrderBy(slot => slot).ToArray()
        };

        return succeeded
            ? MccMcpResult.Ok(resultData)
            : MccMcpResult.Fail("action_incomplete", data: resultData);
    }

    private static MccMcpResult ExecuteInternalCommand(McClient client, string command)
    {
        return client.InvokeOnMainThread(() =>
        {
            CmdResult result = new();
            bool ok = client.PerformInternalCommand(command, ref result);
            return MccMcpResult.Ok(new
            {
                success = ok,
                status = result.status.ToString(),
                output = result.ToString()
            });
        });
    }

    private static bool TryGetCursorItem(McClient client, out Item? cursorItem)
    {
        cursorItem = client.InvokeOnMainThread(() =>
        {
            Container? playerInventory = client.GetInventory(0);
            return playerInventory is not null && playerInventory.Items.TryGetValue(-1, out Item? item) ? item : null;
        });
        return cursorItem is not null;
    }

    private static int ResolveContainerInventoryId(McClient client, int inventoryId)
    {
        if (inventoryId > 0)
        {
            Container? inventory = client.GetInventory(inventoryId);
            return inventory is not null && inventoryId != 0 ? inventoryId : 0;
        }

        return GetActiveContainerId(client);
    }

    private static int GetActiveContainerId(McClient client)
    {
        return client.GetInventories().Keys.Where(id => id > 0).DefaultIfEmpty(0).Max();
    }

    private static bool WaitForContainerOpen(McClient client, ISet<int> beforeIds, int waitMs, out int inventoryId, out Container? inventory)
    {
        inventoryId = 0;
        inventory = null;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            (int activeId, Container? activeInventory) state = client.InvokeOnMainThread(() =>
            {
                int activeId = GetActiveContainerId(client);
                Container? activeInventory = activeId > 0 ? client.GetInventory(activeId) : null;
                return (activeId, activeInventory);
            });

            if (state.activeId > 0 && (!beforeIds.Contains(state.activeId) || beforeIds.Count == 0) && state.activeInventory is not null)
            {
                inventoryId = state.activeId;
                inventory = state.activeInventory;
                return true;
            }

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static async Task<bool> WaitForContainerOpenAsync(McClient client, ISet<int> beforeIds, int waitMs, Action<ContainerOpenState> onOpened)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            (int activeId, Container? activeInventory) state = client.InvokeOnMainThread(() =>
            {
                int activeId = GetActiveContainerId(client);
                Container? activeInventory = activeId > 0 ? client.GetInventory(activeId) : null;
                return (activeId, activeInventory);
            });

            if (state.activeId > 0 && (!beforeIds.Contains(state.activeId) || beforeIds.Count == 0) && state.activeInventory is not null)
            {
                onOpened(new ContainerOpenState(state.activeId, state.activeInventory));
                return true;
            }

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static bool WaitForContainerClose(McClient client, int inventoryId, int waitMs)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            bool stillOpen = client.InvokeOnMainThread(() => client.GetInventories().ContainsKey(inventoryId));
            if (!stillOpen)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static async Task<bool> WaitForContainerCloseAsync(McClient client, int inventoryId, int waitMs)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            bool stillOpen = client.InvokeOnMainThread(() => client.GetInventories().ContainsKey(inventoryId));
            if (!stillOpen)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static int GetContainerWaitMs(int timeoutMs)
    {
        if (timeoutMs <= 0)
            return DefaultContainerWaitMs;
        return Math.Clamp(timeoutMs, MinContainerWaitMs, MaxContainerWaitMs);
    }

    private static bool TryGetContainerSlotRanges(ContainerType type, out int containerStart, out int containerEnd, out int playerStart, out int playerEnd)
    {
        containerStart = 0;
        containerEnd = -1;
        playerStart = 0;
        playerEnd = -1;

        int containerSlots = type switch
        {
            ContainerType.Generic_9x1 => 9,
            ContainerType.Generic_9x2 => 18,
            ContainerType.Generic_9x3 => 27,
            ContainerType.Generic_9x4 => 36,
            ContainerType.Generic_9x5 => 45,
            ContainerType.Generic_9x6 => 54,
            ContainerType.Generic_3x3 => 9,
            ContainerType.Hopper => 5,
            ContainerType.ShulkerBox => 27,
            ContainerType.Furnace or ContainerType.BlastFurnace or ContainerType.Smoker => 3,
            ContainerType.Crafter => 9,
            _ => -1
        };

        if (containerSlots <= 0)
            return false;

        int slotCount = type.SlotCount();
        if (slotCount <= containerSlots)
            return false;

        containerEnd = containerSlots - 1;
        playerStart = containerSlots;
        playerEnd = slotCount - 1;
        return true;
    }

    private static bool IsSnapshotInventorySlot(Container inventory, int slotId)
    {
        return slotId >= 0 && slotId < inventory.Type.SlotCount();
    }

    private static bool IsDroppableInventorySlot(Container inventory, int slotId)
    {
        if (!IsSnapshotInventorySlot(inventory, slotId))
            return false;

        return !(slotId == 0 && (inventory.Type == ContainerType.PlayerInventory || inventory.Type == ContainerType.Crafting));
    }

    private static bool IsHotbarSlot(Container inventory, int slotId)
    {
        return inventory.IsHotbar(slotId, out _);
    }

    private static object? TryBuildCursorSnapshot(Container inventory)
    {
        return inventory.Items.TryGetValue(-1, out Item? cursorItem) && cursorItem.Count > 0
            ? new
            {
                type = cursorItem.Type.ToString(),
                count = cursorItem.Count
            }
            : null;
    }

    private static int GetCursorItemCount(Container inventory, ItemType itemType)
    {
        return inventory.Items.TryGetValue(-1, out Item? cursorItem) && cursorItem.Type == itemType
            ? cursorItem.Count
            : 0;
    }

    private static bool TryDropInventorySlotItems(McClient client, int inventoryId, Container inventory, int slotId, ItemType itemType, int dropCount, out int droppedCount)
    {
        (bool success, int actualDroppedCount) =
            TryDropInventorySlotItemsAsync(client, inventoryId, inventory, slotId, itemType, dropCount).GetAwaiter().GetResult();
        droppedCount = actualDroppedCount;
        return success;
    }

    private static int CountItemInRange(Container inventory, ItemType itemType, int startSlot, int endSlot)
    {
        return inventory.Items
            .Where(entry => entry.Key >= startSlot && entry.Key <= endSlot)
            .Where(entry => entry.Value.Type == itemType)
            .Sum(entry => entry.Value.Count);
    }

    private static (int slot, int count)[] GetOrderedItemSlots(Container inventory, ItemType itemType, int startSlot, int endSlot, bool preferLargestStack)
    {
        var query = inventory.Items
            .Where(entry => entry.Key >= startSlot && entry.Key <= endSlot)
            .Where(entry => entry.Value.Type == itemType && entry.Value.Count > 0)
            .Select(entry => (slot: entry.Key, count: entry.Value.Count));

        return (preferLargestStack
                ? query.OrderByDescending(entry => entry.count).ThenBy(entry => entry.slot)
                : query.OrderBy(entry => entry.count).ThenBy(entry => entry.slot))
            .ToArray();
    }

    private static async Task<int> TransferPartialFromSlotAsync(McClient client, int inventoryId, int sourceSlot, ItemType itemType, int requestedCount, int sourceStart, int sourceEnd, int targetStart, int targetEnd, List<int> touchedTargetSlots)
    {
        Container? inventory = client.InvokeOnMainThread(() => client.GetInventory(inventoryId));
        if (inventory is null || !inventory.Items.TryGetValue(sourceSlot, out Item? sourceItem) || sourceItem.Count <= 0)
            return 0;

        int amountToMove = Math.Min(requestedCount, sourceItem.Count);
        if (!client.DoWindowAction(inventoryId, sourceSlot, WindowActionType.LeftClick))
            return 0;

        if (!await WaitForCursorItemAsync(client, itemType, DefaultInventoryActionWaitMs))
            return 0;

        int moved = 0;
        while (moved < amountToMove)
        {
            inventory = client.InvokeOnMainThread(() => client.GetInventory(inventoryId));
            if (inventory is null || !TryGetCursorItem(client, out Item? cursorItem) || cursorItem is null || cursorItem.Type != itemType)
                break;

            if (!TryFindTransferTargetSlot(inventory, itemType, targetStart, targetEnd, out int targetSlot, out int capacity))
                break;

            int step = Math.Min(amountToMove - moved, Math.Min(capacity, cursorItem.Count));
            int beforeTargetCount = GetSlotItemCount(inventory, targetSlot, itemType);
            int beforeCursorCount = cursorItem.Count;
            if (step <= 0 || !PlaceItemsFromCursor(client, inventoryId, targetSlot, step))
                break;

            if (!await WaitForPlacementAsync(client, inventoryId, targetSlot, itemType, beforeTargetCount, beforeCursorCount, step))
                break;

            touchedTargetSlots.Add(targetSlot);
            moved += step;
        }

        if (TryGetCursorItem(client, out Item? remainingCursor) && remainingCursor is not null && remainingCursor.Count > 0)
        {
            inventory = client.InvokeOnMainThread(() => client.GetInventory(inventoryId));
            if (inventory is null)
                return 0;

            int returnSlot = GetReturnSlot(inventory, itemType, sourceStart, sourceEnd, sourceSlot);
            if (!client.DoWindowAction(inventoryId, returnSlot, WindowActionType.LeftClick))
                return 0;

            if (!await WaitForCursorClearAsync(client, DefaultInventoryActionWaitMs))
                return 0;
        }

        return TryGetCursorItem(client, out _)
            ? 0
            : moved;
    }

    private static bool TryFindTransferTargetSlot(Container inventory, ItemType itemType, int startSlot, int endSlot, out int targetSlot, out int capacity)
    {
        int maxStack = itemType.StackCount();
        for (int slot = startSlot; slot <= endSlot; slot++)
        {
            if (inventory.Items.TryGetValue(slot, out Item? item) && item.Type == itemType && item.Count < maxStack)
            {
                targetSlot = slot;
                capacity = maxStack - item.Count;
                return true;
            }
        }

        for (int slot = startSlot; slot <= endSlot; slot++)
        {
            if (!inventory.Items.ContainsKey(slot))
            {
                targetSlot = slot;
                capacity = maxStack;
                return true;
            }
        }

        targetSlot = -1;
        capacity = 0;
        return false;
    }

    private static bool PlaceItemsFromCursor(McClient client, int inventoryId, int targetSlot, int count)
    {
        if (count <= 0 || !TryGetCursorItem(client, out Item? cursorItem) || cursorItem is null)
            return false;

        if (count == cursorItem.Count)
            return client.DoWindowAction(inventoryId, targetSlot, WindowActionType.LeftClick);

        for (int i = 0; i < count; i++)
        {
            if (!client.DoWindowAction(inventoryId, targetSlot, WindowActionType.RightClick))
                return false;
        }

        return true;
    }

    private static int GetReturnSlot(Container inventory, ItemType itemType, int startSlot, int endSlot, int originalSourceSlot)
    {
        if (originalSourceSlot != 0)
            return originalSourceSlot;

        int maxStack = itemType.StackCount();
        for (int slot = startSlot; slot <= endSlot; slot++)
        {
            if (slot == 0)
                continue;

            if (inventory.Items.TryGetValue(slot, out Item? item) && item.Type == itemType && item.Count < maxStack)
                return slot;
        }

        for (int slot = startSlot; slot <= endSlot; slot++)
        {
            if (slot == 0)
                continue;

            if (!inventory.Items.ContainsKey(slot))
                return slot;
        }

        return originalSourceSlot;
    }

    private static int GetSlotItemCount(Container inventory, int slot, ItemType itemType)
    {
        return inventory.Items.TryGetValue(slot, out Item? item) && item.Type == itemType ? item.Count : 0;
    }

    private static async Task<(bool Success, int DroppedCount)> TryDropHotbarSlotItemsAsync(McClient client, int slotId, int hotbarSlot, ItemType itemType, int dropCount, int availableInSlot)
    {
        int droppedCount = 0;
        byte previousSlot = client.InvokeOnMainThread(client.GetCurrentSlot);
        bool restoreSlot = previousSlot != hotbarSlot;

        if (restoreSlot && !client.ChangeSlot((short)hotbarSlot))
            return (false, 0);

        try
        {
            if (dropCount >= availableInSlot)
            {
                if (!client.DropSelectedItem(dropEntireStack: true))
                    return (false, droppedCount);

                if (!await WaitForSlotItemCountAsync(client, 0, slotId, itemType, count => count == 0, DefaultInventoryActionWaitMs))
                    return (false, droppedCount);

                droppedCount = availableInSlot;
                return (true, droppedCount);
            }

            int remaining = dropCount;
            while (remaining > 0)
            {
                Container? currentInventory = client.GetInventory(0);
                if (currentInventory is null)
                    return (false, droppedCount);

                int beforeSlotCount = GetSlotItemCount(currentInventory, slotId, itemType);
                if (beforeSlotCount <= 0)
                    break;

                if (!client.DropSelectedItem(dropEntireStack: false))
                    return (false, droppedCount);

                if (!await WaitForSlotItemCountAsync(client, 0, slotId, itemType, count => count <= beforeSlotCount - 1, DefaultInventoryActionWaitMs))
                    return (false, droppedCount);

                remaining--;
                droppedCount++;
            }

            return (remaining == 0, droppedCount);
        }
        finally
        {
            if (restoreSlot)
                client.ChangeSlot((short)previousSlot);
        }
    }

    private static async Task<(bool Success, int DroppedCount)> TryDropWindowSlotItemsAsync(McClient client, int inventoryId, int slotId, ItemType itemType, int dropCount, int availableInSlot)
    {
        int droppedCount = 0;

        if (dropCount >= availableInSlot)
        {
            if (!client.DoWindowAction(inventoryId, slotId, WindowActionType.DropItemStack))
                return (false, droppedCount);

            if (!await WaitForSlotItemCountAsync(client, inventoryId, slotId, itemType, count => count == 0, DefaultInventoryActionWaitMs))
                return (false, droppedCount);

            droppedCount = availableInSlot;
            return (true, droppedCount);
        }

        int remaining = dropCount;
        while (remaining > 0)
        {
            Container? currentInventory = client.GetInventory(inventoryId);
            if (currentInventory is null)
                return (false, droppedCount);

            int beforeSlotCount = GetSlotItemCount(currentInventory, slotId, itemType);
            if (beforeSlotCount <= 0)
                break;

            if (!client.DoWindowAction(inventoryId, slotId, WindowActionType.DropItem))
                return (false, droppedCount);

            if (!await WaitForSlotItemCountAsync(client, inventoryId, slotId, itemType, count => count <= beforeSlotCount - 1, DefaultInventoryActionWaitMs))
                return (false, droppedCount);

            remaining--;
            droppedCount++;
        }

        return (remaining == 0, droppedCount);
    }

    private static Task<(bool Success, int DroppedCount)> TryDropInventorySlotItemsAsync(McClient client, int inventoryId, Container inventory, int slotId, ItemType itemType, int dropCount)
    {
        if (!inventory.Items.TryGetValue(slotId, out Item? currentItem) || currentItem.Type != itemType || currentItem.Count <= 0)
            return Task.FromResult((false, 0));

        if (inventoryId == 0 && inventory.IsHotbar(slotId, out int hotbarSlot))
            return TryDropHotbarSlotItemsAsync(client, slotId, hotbarSlot, itemType, dropCount, currentItem.Count);

        return TryDropWindowSlotItemsAsync(client, inventoryId, slotId, itemType, dropCount, currentItem.Count);
    }

    private readonly record struct InventoryCountState(Container? Inventory, int ItemCount);

    private static async Task<bool> WaitForCursorItemAsync(McClient client, ItemType itemType, int waitMs)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            if (TryGetCursorItem(client, out Item? cursorItem) && cursorItem is not null && cursorItem.Type == itemType)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static async Task<bool> WaitForCursorClearAsync(McClient client, int waitMs)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            if (!TryGetCursorItem(client, out _))
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static async Task<bool> WaitForPlacementAsync(McClient client, int inventoryId, int targetSlot, ItemType itemType, int beforeTargetCount, int beforeCursorCount, int placedCount)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(DefaultInventoryActionWaitMs);
        while (true)
        {
            bool targetUpdated = false;
            bool cursorUpdated = false;

            Container? inventory = client.GetInventory(inventoryId);
            if (inventory is not null)
            {
                int currentTargetCount = GetSlotItemCount(inventory, targetSlot, itemType);
                targetUpdated = currentTargetCount >= beforeTargetCount + placedCount;
            }

            if (placedCount >= beforeCursorCount)
            {
                cursorUpdated = !TryGetCursorItem(client, out _);
            }
            else if (TryGetCursorItem(client, out Item? cursorItem) && cursorItem is not null && cursorItem.Type == itemType)
            {
                cursorUpdated = cursorItem.Count <= beforeCursorCount - placedCount;
            }

            if (targetUpdated && cursorUpdated)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static async Task<bool> WaitForSlotItemCountAsync(McClient client, int inventoryId, int slotId, ItemType itemType, Func<int, bool> predicate, int waitMs)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            Container? inventory = client.GetInventory(inventoryId);
            if (inventory is not null)
            {
                int itemCount = GetSlotItemCount(inventory, slotId, itemType);
                if (predicate(itemCount))
                    return true;
            }

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static async Task<bool> WaitForRangeCountAsync(McClient client, int inventoryId, ItemType itemType, int startSlot, int endSlot, Func<int, bool> predicate, int waitMs, Action<InventoryCountState> onObserved, Action onInitialize)
    {
        onInitialize();
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            Container? inventory = client.GetInventory(inventoryId);
            int itemCount = 0;
            if (inventory is not null)
            {
                itemCount = CountItemInRange(inventory, itemType, startSlot, endSlot);
                onObserved(new InventoryCountState(inventory, itemCount));
                if (predicate(itemCount))
                    return true;
            }

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static bool WaitForCursorItem(McClient client, ItemType itemType, int waitMs, out Item? cursorItem)
    {
        cursorItem = null;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            if (TryGetCursorItem(client, out cursorItem) && cursorItem is not null && cursorItem.Type == itemType)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static bool WaitForCursorClear(McClient client, int waitMs)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            if (!TryGetCursorItem(client, out _))
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static bool WaitForPlacement(McClient client, int inventoryId, int targetSlot, ItemType itemType, int beforeTargetCount, int beforeCursorCount, int placedCount)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(DefaultInventoryActionWaitMs);
        while (true)
        {
            bool targetUpdated = false;
            bool cursorUpdated = false;

            Container? inventory = client.InvokeOnMainThread(() => client.GetInventory(inventoryId));
            if (inventory is not null)
            {
                int currentTargetCount = GetSlotItemCount(inventory, targetSlot, itemType);
                targetUpdated = currentTargetCount >= beforeTargetCount + placedCount;
            }

            if (placedCount >= beforeCursorCount)
            {
                cursorUpdated = !TryGetCursorItem(client, out _);
            }
            else if (TryGetCursorItem(client, out Item? cursorItem) && cursorItem is not null && cursorItem.Type == itemType)
            {
                cursorUpdated = cursorItem.Count <= beforeCursorCount - placedCount;
            }

            if (targetUpdated && cursorUpdated)
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static bool WaitForSlotItemCount(McClient client, int inventoryId, int slotId, ItemType itemType, Func<int, bool> predicate, int waitMs, out Container? inventory, out int itemCount)
    {
        inventory = null;
        itemCount = 0;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            inventory = client.GetInventory(inventoryId);
            if (inventory is not null)
            {
                itemCount = GetSlotItemCount(inventory, slotId, itemType);
                if (predicate(itemCount))
                    return true;
            }

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static bool WaitForRangeCount(McClient client, int inventoryId, ItemType itemType, int startSlot, int endSlot, Func<int, bool> predicate, int waitMs, out Container? inventory, out int itemCount)
    {
        inventory = null;
        itemCount = 0;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            inventory = client.InvokeOnMainThread(() => client.GetInventory(inventoryId));
            if (inventory is not null)
            {
                itemCount = CountItemInRange(inventory, itemType, startSlot, endSlot);
                if (predicate(itemCount))
                    return true;
            }

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
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

    private static object? DescribeMetadataValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            bool b => b,
            byte b => b,
            sbyte b => b,
            short s => s,
            ushort s => s,
            int i => i,
            uint i => i,
            long l => l,
            ulong l => l,
            float f => f,
            double d => d,
            decimal d => d,
            Enum e => e.ToString(),
            Location location => ToCoordinate(location),
            Item item => new { type = item.Type.ToString(), count = item.Count },
            byte[] data => new { bytes = data.Length },
            _ => value.ToString()
        };
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

    private static bool TryParseWindowAction(string rawActionType, out WindowActionType actionType)
    {
        if (Enum.TryParse(rawActionType, true, out actionType))
            return true;

        string normalized = NormalizeToken(rawActionType);
        if (normalized.Length == 0)
            return false;

        return normalized switch
        {
            "left" or "leftclick" => SetAction(WindowActionType.LeftClick, out actionType),
            "right" or "rightclick" => SetAction(WindowActionType.RightClick, out actionType),
            "middle" or "mid" or "middleclick" => SetAction(WindowActionType.MiddleClick, out actionType),
            "shift" or "shiftclick" => SetAction(WindowActionType.ShiftClick, out actionType),
            "shiftright" or "shiftrightclick" => SetAction(WindowActionType.ShiftRightClick, out actionType),
            "drop" or "dropitem" or "q" => SetAction(WindowActionType.DropItem, out actionType),
            "dropstack" or "dropall" or "dropitemstack" or "ctrlq" or "ctrldrop" => SetAction(WindowActionType.DropItemStack, out actionType),
            _ => false
        };
    }

    private static bool SetAction(WindowActionType value, out WindowActionType actionType)
    {
        actionType = value;
        return true;
    }

    private static bool TryParseItemType(string rawItemType, out ItemType itemType)
    {
        if (Enum.TryParse(rawItemType, true, out itemType) && itemType is not (ItemType.Unknown or ItemType.Null))
            return true;

        string normalized = NormalizeToken(rawItemType);
        if (normalized.Length == 0)
        {
            itemType = ItemType.Unknown;
            return false;
        }

        foreach (ItemType candidate in Enum.GetValues<ItemType>())
        {
            if (candidate is ItemType.Unknown or ItemType.Null)
                continue;
            if (NormalizeToken(candidate.ToString()) == normalized)
            {
                itemType = candidate;
                return true;
            }
        }

        itemType = ItemType.Unknown;
        return false;
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        char[] buffer = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(buffer);
    }

    private static bool WaitForArrival(McClient client, Location goal, int waitMs, double tolerance, out Location? finalLocation)
    {
        finalLocation = null;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            Location location = client.InvokeOnMainThread(client.GetCurrentLocation);
            finalLocation = location;
            double distance = GetDistance(location, goal);
            if (distance <= tolerance)
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
            double distance = GetDistance(finalLocation, goal);
            if (distance <= tolerance)
                return (true, finalLocation);

            if (DateTime.UtcNow >= deadline)
                return (false, finalLocation);

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static double GetDistance(Location from, Location to)
    {
        double dx = from.X - to.X;
        double dy = from.Y - to.Y;
        double dz = from.Z - to.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
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

    private static int GetPathQueryTimeoutMs(int timeoutMs)
    {
        if (timeoutMs <= 0)
            return DefaultPathQueryTimeoutMs;
        return Math.Clamp(timeoutMs, MinPathQueryTimeoutMs, MaxPathQueryTimeoutMs);
    }

    private static bool WaitForBlockChange(McClient client, Location target, Block beforeBlock, int waitMs, out Block afterBlock)
    {
        afterBlock = beforeBlock;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            Block current = client.InvokeOnMainThread(() => client.GetWorld().GetBlock(target));
            afterBlock = current;
            if (!AreEquivalentBlocks(current, beforeBlock))
                return true;

            if (DateTime.UtcNow >= deadline)
                return false;

            Thread.Sleep(ArrivalPollIntervalMs);
        }
    }

    private static async Task<(bool Changed, Block AfterBlock)> WaitForBlockChangeAsync(McClient client, Location target, Block beforeBlock, int waitMs)
    {
        Block afterBlock = beforeBlock;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
        while (true)
        {
            Block current = client.InvokeOnMainThread(() => client.GetWorld().GetBlock(target));
            afterBlock = current;
            if (!AreEquivalentBlocks(current, beforeBlock))
                return (true, afterBlock);

            if (DateTime.UtcNow >= deadline)
                return (false, afterBlock);

            await Task.Delay(ArrivalPollIntervalMs);
        }
    }

    private static bool AreEquivalentBlocks(Block left, Block right)
    {
        return left.BlockId == right.BlockId
            && left.BlockMeta == right.BlockMeta
            && left.Type == right.Type;
    }

    private static double[] GetDigAttemptDurations(double durationSeconds)
    {
        if (durationSeconds > 0)
            return [durationSeconds];
        return s_defaultDigAttemptDurations;
    }

    private static int GetDigVerifyWaitMs(double durationSeconds)
    {
        int waitMs = (int)Math.Ceiling(durationSeconds * 1000) + 2000;
        return Math.Clamp(waitMs, 1500, MaxBlockVerifyWaitMs);
    }

    private static bool AreValidPathOffsets(int maxOffset, int minOffset)
    {
        return maxOffset >= 0 && minOffset >= 0 && minOffset <= maxOffset;
    }

    private static bool HasCompleteCoordinateTriple(double? x, double? y, double? z)
    {
        return x.HasValue == y.HasValue && y.HasValue == z.HasValue;
    }

    private static int GetLoadedChunkCount(World world)
    {
        return Math.Max(0, world.chunkCnt - Math.Max(0, world.chunkLoadNotCompleted));
    }

    private static double GetChunkLoadRatio(World world)
    {
        return world.chunkCnt > 0
            ? GetLoadedChunkCount(world) / (double)world.chunkCnt
            : 0.0;
    }

    private static object GetNeighborBlockSnapshot(World world, Location location)
    {
        Location blockLocation = location.ToFloor();
        Location north = new(blockLocation.X, blockLocation.Y, blockLocation.Z - 1);
        Location south = new(blockLocation.X, blockLocation.Y, blockLocation.Z + 1);
        Location east = new(blockLocation.X + 1, blockLocation.Y, blockLocation.Z);
        Location west = new(blockLocation.X - 1, blockLocation.Y, blockLocation.Z);
        Location above = new(blockLocation.X, blockLocation.Y + 1, blockLocation.Z);
        Location below = new(blockLocation.X, blockLocation.Y - 1, blockLocation.Z);

        return new
        {
            north = new { location = ToCoordinate(north), block = ToBlockState(world.GetBlock(north)) },
            south = new { location = ToCoordinate(south), block = ToBlockState(world.GetBlock(south)) },
            east = new { location = ToCoordinate(east), block = ToBlockState(world.GetBlock(east)) },
            west = new { location = ToCoordinate(west), block = ToBlockState(world.GetBlock(west)) },
            above = new { location = ToCoordinate(above), block = ToBlockState(world.GetBlock(above)) },
            below = new { location = ToCoordinate(below), block = ToBlockState(world.GetBlock(below)) }
        };
    }

    private static bool ItemMatches(Item item, string query, bool exactMatch, ItemType? exactItemType)
    {
        if (exactItemType.HasValue)
            return item.Type == exactItemType.Value;

        string typeName = item.Type.ToString();
        string typeLabel = item.GetTypeString();
        return exactMatch
            ? TextEqualsFilter(typeName, query) || TextEqualsFilter(typeLabel, query)
            : TextMatchesFilter(typeName, query) || TextMatchesFilter(typeLabel, query);
    }

    private static bool EntityNameMatches(string? name, string? customName, string filter)
    {
        return (!string.IsNullOrWhiteSpace(name) && TextMatchesFilter(name, filter))
            || (!string.IsNullOrWhiteSpace(customName) && TextMatchesFilter(customName, filter));
    }

    private static bool IsSupportedLookDirection(Direction direction)
    {
        return direction is Direction.Up or Direction.Down or Direction.North or Direction.South or Direction.East or Direction.West;
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

    private static int GetInventoryItemCount(McClient client, ItemType itemType)
    {
        Container? inventory = client.GetInventory(0);
        if (inventory is null)
            return 0;

        return inventory.Items.Values
            .Where(item => item.Type == itemType)
            .Sum(item => item.Count);
    }

    private static object ToCoordinate(Location location)
    {
        return ToCoordinate(location.X, location.Y, location.Z);
    }

    private static object ToCoordinate(double x, double y, double z)
    {
        return new
        {
            x = RoundCoordinate(x),
            y = RoundCoordinate(y),
            z = RoundCoordinate(z)
        };
    }

    private static double RoundCoordinate(double value)
    {
        return Math.Round(value, CoordinateRoundingPrecision, MidpointRounding.AwayFromZero);
    }

    private static Location ToBlockLocation(double x, double y, double z)
    {
        return new Location(Math.Floor(x), Math.Floor(y), Math.Floor(z));
    }

    private static object ToBlockState(Block block)
    {
        return new
        {
            material = block.Type.ToString(),
            typeLabel = block.GetTypeString(),
            blockId = block.BlockId,
            blockMeta = block.BlockMeta
        };
    }

    private static string GetMaterialTypeLabel(Material material)
    {
        string key = "block.minecraft." + ToTranslationKey(material.ToString());
        string? translation = ChatParser.TranslateString(key);
        return string.IsNullOrEmpty(translation) ? material.ToString() : translation;
    }

    private static string ToTranslationKey(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        List<char> chars = new(value.Length * 2);
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsUpper(current) && i > 0 && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1])))
                chars.Add('_');
            chars.Add(char.ToLowerInvariant(current));
        }

        return new string(chars.ToArray());
    }

    private static bool IsSignMaterial(Material material)
    {
        return material.ToString().Contains("Sign", StringComparison.Ordinal);
    }

    private static bool IsInteractableContainerMaterial(Material material)
    {
        string name = material.ToString();
        return name.Contains("Chest", StringComparison.Ordinal)
            || name.Contains("Barrel", StringComparison.Ordinal)
            || name.Contains("ShulkerBox", StringComparison.Ordinal)
            || name.Contains("Hopper", StringComparison.Ordinal)
            || name.Contains("Dispenser", StringComparison.Ordinal)
            || name.Contains("Dropper", StringComparison.Ordinal)
            || name.Contains("Furnace", StringComparison.Ordinal)
            || name.Contains("Smoker", StringComparison.Ordinal)
            || name.Contains("BlastFurnace", StringComparison.Ordinal)
            || name.Contains("Crafter", StringComparison.Ordinal);
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

    private static bool BlockMatches(Block block, string? filter, bool exactMatch, int? blockIdFilter, int? blockMetaFilter)
    {
        if (blockIdFilter.HasValue)
        {
            if (block.BlockId != blockIdFilter.Value)
                return false;
            if (blockMetaFilter.HasValue && block.BlockMeta != blockMetaFilter.Value)
                return false;
            return true;
        }

        if (filter is null)
            return true;

        string material = block.Type.ToString();
        string typeLabel = block.GetTypeString();
        if (exactMatch)
        {
            return TextEqualsFilter(material, filter)
                || TextEqualsFilter(typeLabel, filter);
        }

        return TextMatchesFilter(material, filter)
            || TextMatchesFilter(typeLabel, filter);
    }

    private static bool TextEqualsFilter(string text, string filter)
    {
        return text.Equals(filter, StringComparison.OrdinalIgnoreCase)
            || NormalizeToken(text) == NormalizeToken(filter);
    }

    private static bool TextMatchesFilter(string text, string filter)
    {
        if (text.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;

        string normalizedFilter = NormalizeToken(filter);
        if (normalizedFilter.Length == 0)
            return false;

        return NormalizeToken(text).Contains(normalizedFilter, StringComparison.Ordinal);
    }

    private static void ParseBlockQuery(string? query, out int? blockId, out int? blockMeta)
    {
        blockId = null;
        blockMeta = null;
        if (string.IsNullOrWhiteSpace(query))
            return;

        string trimmed = query.Trim();
        int separator = trimmed.IndexOf(':');
        if (separator >= 0)
        {
            string idPart = trimmed[..separator].Trim();
            string metaPart = trimmed[(separator + 1)..].Trim();
            if (int.TryParse(idPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedId))
            {
                blockId = parsedId;
                if (int.TryParse(metaPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMeta))
                    blockMeta = parsedMeta;
            }
            return;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int blockStateId))
            blockId = blockStateId;
    }
}
