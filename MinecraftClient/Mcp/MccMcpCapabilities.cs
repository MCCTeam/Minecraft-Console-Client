using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;

namespace MinecraftClient.Mcp;

public sealed class MccMcpCapabilities : IMccMcpCapabilities
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;
    private const int CoordinateRoundingPrecision = 2;
    private const double SelfEntityDistanceThreshold = 0.2;
    private const int DefaultArrivalWaitMs = 3500;
    private const int MinArrivalWaitMs = 250;
    private const int MaxArrivalWaitMs = 15000;
    private const double DefaultArrivalTolerance = 1.5;
    private const int ArrivalPollIntervalMs = 125;

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

    private readonly Func<MccMcpCapabilityToggles> togglesProvider;

    public MccMcpCapabilities(Func<MccMcpCapabilityToggles> togglesProvider)
    {
        this.togglesProvider = togglesProvider;
    }

    private static McClient? GetClient()
    {
        return McClient.Instance as McClient;
    }

    private static MccMcpResult NotConnected()
    {
        return MccMcpResult.Fail("disconnected");
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

    public MccMcpResult GetChatHistory(int maxCount, bool includeJson)
    {
        if (!IsCategoryEnabled(t => t.SessionStatus))
            return MccMcpResult.Fail("capability_disabled");

        int count = Math.Clamp(maxCount, 1, 500);
        MccMcpChatHistoryEntry[] entries = MccMcpChatHistoryStore.GetLatest(count);
        return MccMcpResult.Ok(new
        {
            count = entries.Length,
            entries = entries.Select(entry => new
            {
                timestampUtc = entry.TimestampUtc,
                kind = entry.Kind,
                text = entry.Text,
                sender = entry.Sender,
                message = entry.Message,
                json = includeJson ? entry.Json : null
            }).ToArray()
        });
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
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        if (durationSeconds < 0)
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        string sx = x.ToString(CultureInfo.InvariantCulture);
        string sy = y.ToString(CultureInfo.InvariantCulture);
        string sz = z.ToString(CultureInfo.InvariantCulture);
        string command = durationSeconds > 0
            ? $"dig {sx} {sy} {sz} {durationSeconds.ToString(CultureInfo.InvariantCulture)}"
            : $"dig {sx} {sy} {sz}";
        return ExecuteInternalCommand(client, command);
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

    public MccMcpResult ScanNearbyBlocks(int radius, int maxCount, string? materialFilter)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        if (radius is < 1 or > 8)
            return MccMcpResult.Fail("invalid_args");

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
                        if (filter is not null && !material.Contains(filter, StringComparison.OrdinalIgnoreCase))
                            continue;

                        double dx = x + 0.5 - playerLocation.X;
                        double dy = y + 0.5 - playerLocation.Y;
                        double dz = z + 0.5 - playerLocation.Z;
                        found.Add(new
                        {
                            x,
                            y,
                            z,
                            material,
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

        if (radius is < 1 or > 16)
            return MccMcpResult.Fail("invalid_args");

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

    public MccMcpResult IsPlayerNearby(string? playerName, double radius, bool includeSelf)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        if (radius <= 0 || radius > 1024)
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetEntityHandlingEnabled())
            return MccMcpResult.Fail("feature_disabled");

        string? nameFilter = string.IsNullOrWhiteSpace(playerName) ? null : playerName.Trim();

        return client.InvokeOnMainThread(() =>
        {
            double radiusValue = radius;
            List<NearbyPlayerSnapshot> trackedPlayers = BuildTrackedPlayerSnapshots(client, includeSelf);

            var players = trackedPlayers
                .Where(player => player.Distance <= radiusValue)
                .Where(player =>
                {
                    if (nameFilter is null)
                        return true;
                    return PlayerNameMatches(player, nameFilter);
                })
                .OrderBy(player => player.Distance)
                .Select(player => new
                {
                    entityId = player.EntityId,
                    uuid = player.Uuid,
                    name = player.Name,
                    customName = player.CustomName,
                    x = RoundCoordinate(player.X),
                    y = RoundCoordinate(player.Y),
                    z = RoundCoordinate(player.Z),
                    distance = player.Distance,
                    latency = player.Latency
                })
                .ToArray();

            return MccMcpResult.Ok(new
            {
                radius = radiusValue,
                playerName = nameFilter,
                includeSelf,
                anyNearby = players.Length > 0,
                count = players.Length,
                players
            });
        });
    }

    public MccMcpResult LocatePlayer(string playerName, bool includeSelf)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(playerName))
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetEntityHandlingEnabled())
            return MccMcpResult.Fail("feature_disabled");

        string nameFilter = playerName.Trim();
        return client.InvokeOnMainThread(() =>
        {
            List<NearbyPlayerSnapshot> trackedPlayers = BuildTrackedPlayerSnapshots(client, includeSelf);
            NearbyPlayerSnapshot[] matches = trackedPlayers
                .Where(player => PlayerNameMatches(player, nameFilter))
                .OrderBy(player => player.Distance)
                .ToArray();

            if (matches.Length == 0)
            {
                return MccMcpResult.Fail("invalid_state", data: new
                {
                    playerName = nameFilter,
                    trackedPlayers = trackedPlayers
                        .Select(player => player.Name)
                        .OfType<string>()
                        .Distinct(NameComparer)
                        .ToArray()
                });
            }

            NearbyPlayerSnapshot selected = matches[0];
            return MccMcpResult.Ok(new
            {
                playerName = nameFilter,
                matchedName = selected.Name,
                entityId = selected.EntityId,
                uuid = selected.Uuid,
                x = RoundCoordinate(selected.X),
                y = RoundCoordinate(selected.Y),
                z = RoundCoordinate(selected.Z),
                distance = selected.Distance
            });
        });
    }

    public MccMcpResult MoveTo(double x, double y, double z, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        Location goal = new(x, y, z);
        TimeSpan? timeout = timeoutMs > 0 ? TimeSpan.FromMilliseconds(timeoutMs) : null;
        bool pathFound = client.InvokeOnMainThread(() => client.MoveTo(goal, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeout));

        int verifyWaitMs = GetArrivalWaitMs(timeoutMs);
        double tolerance = GetArrivalTolerance(maxOffset, minOffset);
        Location? finalLocation = null;
        bool arrived = pathFound && WaitForArrival(client, goal, verifyWaitMs, tolerance, out finalLocation);

        return MccMcpResult.Ok(new
        {
            pathFound,
            arrived,
            tolerance,
            verifyWaitMs,
            target = ToCoordinate(goal),
            finalLocation = finalLocation is Location location ? ToCoordinate(location) : null
        });
    }

    public MccMcpResult MoveToPlayer(string playerName, bool allowUnsafe, bool allowDirectTeleport, int maxOffset, int minOffset, int timeoutMs)
    {
        if (!IsCategoryEnabled(t => t.Movement))
            return MccMcpResult.Fail("capability_disabled");

        if (string.IsNullOrWhiteSpace(playerName))
            return MccMcpResult.Fail("invalid_args");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetTerrainEnabled())
            return MccMcpResult.Fail("feature_disabled");

        if (!client.GetEntityHandlingEnabled())
            return MccMcpResult.Fail("feature_disabled");

        string nameFilter = playerName.Trim();
        return client.InvokeOnMainThread(() =>
        {
            List<NearbyPlayerSnapshot> trackedPlayers = BuildTrackedPlayerSnapshots(client, includeSelf: false);
            NearbyPlayerSnapshot? target = trackedPlayers
                .Where(player => PlayerNameMatches(player, nameFilter))
                .OrderBy(player => player.Distance)
                .FirstOrDefault();

            if (target is null)
            {
                return MccMcpResult.Fail("invalid_state", data: new
                {
                    playerName = nameFilter,
                    trackedPlayers = trackedPlayers
                        .Select(player => player.Name)
                        .OfType<string>()
                        .Distinct(NameComparer)
                        .ToArray()
                });
            }

            Location goal = new(target.X, target.Y, target.Z);
            TimeSpan? timeout = timeoutMs > 0 ? TimeSpan.FromMilliseconds(timeoutMs) : null;
            bool pathFound = client.MoveTo(goal, allowUnsafe, allowDirectTeleport, maxOffset, minOffset, timeout);

            int verifyWaitMs = GetArrivalWaitMs(timeoutMs);
            double tolerance = GetArrivalTolerance(maxOffset, minOffset);
            Location? finalLocation = null;
            bool arrived = pathFound && WaitForArrival(client, goal, verifyWaitMs, tolerance, out finalLocation);

            return MccMcpResult.Ok(new
            {
                pathFound,
                arrived,
                tolerance,
                verifyWaitMs,
                target = new
                {
                    playerName = target.Name,
                    entityId = target.EntityId,
                    x = RoundCoordinate(target.X),
                    y = RoundCoordinate(target.Y),
                    z = RoundCoordinate(target.Z)
                },
                finalLocation = finalLocation is Location location ? ToCoordinate(location) : null
            });
        });
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

        Location target = new(x, y, z);
        client.InvokeOnMainThread(() => client.UpdateLocation(client.GetCurrentLocation(), target));
        return MccMcpResult.Ok();
    }

    public MccMcpResult GetInventorySnapshot(int inventoryId)
    {
        if (!IsCategoryEnabled(t => t.Inventory))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetInventoryEnabled())
            return MccMcpResult.Fail("feature_disabled");

        return client.InvokeOnMainThread(() =>
        {
            Dictionary<int, Container> inventories = client.GetInventories();
            if (!inventories.TryGetValue(inventoryId, out Container? inventory))
                return MccMcpResult.Fail("invalid_state");

            var slots = inventory.Items.Select(item => new
            {
                slot = item.Key,
                type = item.Value.Type.ToString(),
                count = item.Value.Count
            }).ToArray();

            return MccMcpResult.Ok(new
            {
                id = inventory.ID,
                type = inventory.Type.ToString(),
                title = inventory.Title,
                slotCount = inventory.Type.SlotCount(),
                slots
            });
        });
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

        return client.InvokeOnMainThread(() =>
        {
            Dictionary<int, Container> inventories = client.GetInventories();
            if (!inventories.TryGetValue(inventoryId, out Container? inventory))
                return MccMcpResult.Fail("invalid_state");

            var matchingSlotQuery = inventory.Items
                .Where(pair => pair.Value.Type == parsedItemType && pair.Value.Count > 0)
                .Select(pair => new { slot = pair.Key, count = pair.Value.Count });
            var matchingSlots = (preferStack
                    ? matchingSlotQuery.OrderByDescending(pair => pair.count).ThenBy(pair => pair.slot)
                    : matchingSlotQuery.OrderBy(pair => pair.count).ThenBy(pair => pair.slot))
                .ToArray();

            int beforeCount = matchingSlots.Sum(pair => pair.count);
            if (beforeCount < count)
            {
                return MccMcpResult.Fail("invalid_state", data: new
                {
                    itemType = parsedItemType.ToString(),
                    requestedCount = count,
                    availableCount = beforeCount,
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
                bool ok = true;

                if (dropFromSlot == currentItem.Count)
                {
                    ok = client.DoWindowAction(inventoryId, entry.slot, WindowActionType.DropItemStack);
                }
                else
                {
                    for (int i = 0; i < dropFromSlot; i++)
                    {
                        if (!client.DoWindowAction(inventoryId, entry.slot, WindowActionType.DropItem))
                        {
                            ok = false;
                            break;
                        }
                    }
                }

                if (!ok)
                {
                    return MccMcpResult.Fail("action_failed", data: new
                    {
                        itemType = parsedItemType.ToString(),
                        requestedCount = count,
                        droppedCount = count - remaining,
                        remainingCount = remaining,
                        inventoryId,
                        touchedSlots = touchedSlots.ToArray()
                    });
                }

                remaining -= dropFromSlot;
            }

            int afterCount = inventory.Items
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

    public MccMcpResult QueryEntities(int maxCount)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetEntityHandlingEnabled())
            return MccMcpResult.Fail("feature_disabled");

        int count = Math.Clamp(maxCount, 1, 1000);
        return client.InvokeOnMainThread(() =>
        {
            Dictionary<int, Entity> entities = client.GetEntities();
            Dictionary<int, string?> playerNamesByEntityId = BuildTrackedPlayerSnapshots(client, includeSelf: true)
                .ToDictionary(player => player.EntityId, player => player.Name);
            var data = entities.Take(count)
                .Select(pair => new
                {
                    id = pair.Key,
                    type = pair.Value.Type.ToString(),
                    name = pair.Value.Type == EntityType.Player
                        && playerNamesByEntityId.TryGetValue(pair.Key, out string? mappedName)
                        ? mappedName
                        : pair.Value.Name,
                    uuid = pair.Value.UUID,
                    x = RoundCoordinate(pair.Value.Location.X),
                    y = RoundCoordinate(pair.Value.Location.Y),
                    z = RoundCoordinate(pair.Value.Location.Z)
                })
                .ToArray();

            return MccMcpResult.Ok(new
            {
                count = entities.Count,
                entities = data
            });
        });
    }

    public MccMcpResult ListEntities(int maxCount, string? typeFilter, double radius)
    {
        if (!IsCategoryEnabled(t => t.EntityWorld))
            return MccMcpResult.Fail("capability_disabled");

        McClient? client = GetClient();
        if (client is null)
            return NotConnected();

        if (!client.GetEntityHandlingEnabled())
            return MccMcpResult.Fail("feature_disabled");

        int count = Math.Clamp(maxCount, 1, 1000);
        string? filter = string.IsNullOrWhiteSpace(typeFilter) ? null : typeFilter.Trim();
        double radiusValue = Math.Max(radius, 0);

        return client.InvokeOnMainThread(() =>
        {
            Dictionary<int, Entity> entities = client.GetEntities();
            Location playerLocation = client.GetCurrentLocation();
            Dictionary<int, string?> playerNamesByEntityId = BuildTrackedPlayerSnapshots(client, includeSelf: true)
                .ToDictionary(player => player.EntityId, player => player.Name);

            var data = entities.Values
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
                        distance = Math.Sqrt(dx * dx + dy * dy + dz * dz),
                        resolvedName
                    };
                })
                .Where(item => radiusValue <= 0 || item.distance <= radiusValue)
                .Where(item =>
                {
                    if (filter is null)
                        return true;
                    return item.entity.Type.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)
                        || item.entity.GetTypeString().Contains(filter, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(item => item.distance)
                .Take(count)
                .Select(item => new
                {
                    id = item.entity.ID,
                    type = item.entity.Type.ToString(),
                    typeLabel = item.entity.GetTypeString(),
                    uuid = item.entity.UUID,
                    name = item.resolvedName,
                    customName = item.entity.CustomName,
                    x = RoundCoordinate(item.entity.Location.X),
                    y = RoundCoordinate(item.entity.Location.Y),
                    z = RoundCoordinate(item.entity.Location.Z),
                    distance = item.distance,
                    health = item.entity.Health,
                    pose = item.entity.Pose.ToString(),
                    latency = item.entity.Latency
                })
                .ToArray();

            return MccMcpResult.Ok(new
            {
                totalTracked = entities.Count,
                count = data.Length,
                entities = data
            });
        });
    }

    public MccMcpResult GetEntityInfo(int entityId, bool includeMetadata, bool includeEquipment, bool includeEffects)
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
            Dictionary<int, Entity> entities = client.GetEntities();
            if (!entities.TryGetValue(entityId, out Entity? entity))
                return MccMcpResult.Fail("invalid_state");

            string? resolvedName = entity.Name;
            if (entity.Type == EntityType.Player)
            {
                Dictionary<int, string?> playerNamesByEntityId = BuildTrackedPlayerSnapshots(client, includeSelf: true)
                    .ToDictionary(player => player.EntityId, player => player.Name);
                if (playerNamesByEntityId.TryGetValue(entityId, out string? mappedName))
                    resolvedName = mappedName;
            }

            object? metadata = includeMetadata
                ? entity.Metadata?.ToDictionary(
                    pair => pair.Key.ToString(CultureInfo.InvariantCulture),
                    pair => DescribeMetadataValue(pair.Value))
                : null;

            object? equipment = includeEquipment
                ? entity.Equipment.Select(pair => new
                {
                    slot = pair.Key,
                    type = pair.Value.Type.ToString(),
                    count = pair.Value.Count
                }).ToArray()
                : null;

            object? activeEffects = includeEffects
                ? entity.ActiveEffects.Values.Select(effect => new
                {
                    id = effect.Effect.ToString(),
                    amplifier = effect.Amplifier,
                    remainingSeconds = effect.RemainingSeconds,
                    isInfinite = effect.IsInfinite
                }).ToArray()
                : null;

            return MccMcpResult.Ok(new
            {
                id = entity.ID,
                type = entity.Type.ToString(),
                typeLabel = entity.GetTypeString(),
                uuid = entity.UUID,
                name = resolvedName,
                customName = entity.CustomName,
                customNameVisible = entity.IsCustomNameVisible,
                x = RoundCoordinate(entity.Location.X),
                y = RoundCoordinate(entity.Location.Y),
                z = RoundCoordinate(entity.Location.Z),
                yaw = entity.Yaw,
                pitch = entity.Pitch,
                health = entity.Health,
                pose = entity.Pose.ToString(),
                latency = entity.Latency,
                objectData = entity.ObjectData,
                metadata,
                equipment,
                activeEffects
            });
        });
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
            return material.Equals(filter, StringComparison.OrdinalIgnoreCase)
                || typeLabel.Equals(filter, StringComparison.OrdinalIgnoreCase);
        }

        return material.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || typeLabel.Contains(filter, StringComparison.OrdinalIgnoreCase);
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
