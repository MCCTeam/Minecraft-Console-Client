using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Logger;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Handlers.packet.s2c;
using MinecraftClient.Protocol.Handlers.PacketPalettes;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using MinecraftClient.Scripting;
using Sentry;
using static MinecraftClient.Settings;
using static MinecraftClient.Settings.MainConfigHelper.MainConfig.GeneralConfig;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.8.X+ Protocols
    /// </summary>
    /// <remarks>
    /// Typical update steps for implementing protocol changes for a new Minecraft version:
    ///  - Perform a diff between latest supported version in MCC and new stable version to support on https://wiki.vg/Protocol
    ///  - If there are any changes in packets implemented by MCC, add MCXXXVersion field below and implement new packet layouts
    ///  - Add the packet type palette for that Minecraft version. Please see PacketTypePalette.cs for more information
    ///  - Also see Material.cs and ItemType.cs for updating block and item data inside MCC
    /// </remarks>
    class Protocol18Handler : IMinecraftCom
    {
        internal const int MC_1_8_Version = 47;
        internal const int MC_1_9_Version = 107;
        internal const int MC_1_9_1_Version = 108;
        internal const int MC_1_10_Version = 210;
        internal const int MC_1_11_Version = 315;
        internal const int MC_1_11_2_Version = 316;
        internal const int MC_1_12_Version = 335;
        internal const int MC_1_12_2_Version = 340;
        internal const int MC_1_13_Version = 393;
        internal const int MC_1_13_2_Version = 404;
        internal const int MC_1_14_Version = 477;
        internal const int MC_1_15_Version = 573;
        internal const int MC_1_15_2_Version = 578;
        internal const int MC_1_16_Version = 735;
        internal const int MC_1_16_1_Version = 736;
        internal const int MC_1_16_2_Version = 751;
        internal const int MC_1_16_3_Version = 753;
        internal const int MC_1_16_5_Version = 754;
        internal const int MC_1_17_Version = 755;
        internal const int MC_1_17_1_Version = 756;
        internal const int MC_1_18_1_Version = 757;
        internal const int MC_1_18_2_Version = 758;
        internal const int MC_1_19_Version = 759;
        internal const int MC_1_19_2_Version = 760;
        internal const int MC_1_19_3_Version = 761;
        internal const int MC_1_19_4_Version = 762;
        internal const int MC_1_20_Version = 763;
        internal const int MC_1_20_2_Version = 764;
        internal const int MC_1_20_4_Version = 765;
        internal const int MC_1_20_6_Version = 766;
        internal const int MC_1_21_Version = 767;
        internal const int MC_1_21_2_Version = 768;
        internal const int MC_1_21_4_Version = 769;
        internal const int MC_1_21_5_Version = 770;
        internal const int MC_1_21_6_Version = 771;
        internal const int MC_1_21_7_Version = 772;
        internal const int MC_1_21_9_Version = 773;
        internal const int MC_1_21_11_Version = 774;
        internal const int MC_26_1_Version = 775;

        private int compression_treshold = -1;
        private int autocomplete_transaction_id = 0;
        private readonly Dictionary<int, short> window_actions = new();
        private CurrentState currentState = CurrentState.Login;
        private readonly int protocolVersion;
        private readonly int rawProtocolVersion;
        private int currentDimension;
        private bool isOnlineMode = false;
        private readonly BlockingCollection<Tuple<int, Queue<byte>>> packetQueue = new();
        private readonly Dictionary<string, bool> legacyAchievementProgress = new(StringComparer.Ordinal);
        private float LastYaw, LastPitch;
        private double lastSentX, lastSentY, lastSentZ;
        private float lastSentYaw, lastSentPitch;
        private bool lastSentOnGround;
        private bool lastSentHorizontalCollision;
        private int positionReminder;
        private long chunkBatchStartTime;
        private double aggregatedNanosPerChunk = 2000000.0;
        private int oldSamplesWeight = 1;

        private bool receiveDeclareCommands = false, receivePlayerInfo = false;
        private readonly Lock MessageSigningLock = new();
        private Guid chatUuid = Guid.NewGuid();
        private int pendingAcknowledgments = 0, messageIndex = 0;
        private LastSeenMessagesCollector lastSeenMessagesCollector;
        private LastSeenMessageList.AcknowledgedMessage? lastReceivedMessage = null;
        readonly Protocol18Forge pForge;
        readonly Protocol18Terrain pTerrain;
        readonly IMinecraftComHandler handler;
        readonly EntityPalette entityPalette;
        readonly EntityMetadataPalette entityMetadataPalette;
        readonly ItemPalette itemPalette;
        readonly PacketTypePalette packetPalette;
        readonly SocketWrapper socketWrapper;
        readonly DataTypes dataTypes;
        Tuple<Thread, CancellationTokenSource>? netMain = null; // main thread
        Tuple<Thread, CancellationTokenSource>? netReader = null; // reader thread
        readonly ILogger log;
        readonly RandomNumberGenerator randomGen;
        private bool legacyAchievementsInitialized;

        public Protocol18Handler(TcpClient Client, int protocolVersion, IMinecraftComHandler handler,
            ForgeInfo? forgeInfo, int rawProtocolVersion = 0)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            socketWrapper = new SocketWrapper(Client);
            dataTypes = new DataTypes(protocolVersion);
            this.protocolVersion = protocolVersion;
            this.rawProtocolVersion = rawProtocolVersion != 0 ? rawProtocolVersion : protocolVersion;
            this.handler = handler;
            pForge = new Protocol18Forge(forgeInfo, protocolVersion, dataTypes, this, handler);
            pTerrain = new Protocol18Terrain(protocolVersion, dataTypes, handler);
            packetPalette = new PacketTypeHandler(protocolVersion, forgeInfo is not null).GetTypeHandler();
            log = handler.GetLogger();
            randomGen = RandomNumberGenerator.Create();
            lastSeenMessagesCollector = protocolVersion >= MC_1_19_3_Version ? new(20) : new(5);
            chunkBatchStartTime = GetNanos();

            if (handler.GetTerrainEnabled() && protocolVersion > MC_26_1_Version)
            {
                log.Error($"§c{Translations.extra_terrainandmovement_disabled}");
                handler.SetTerrainEnabled(false);
            }

            if (handler.GetInventoryEnabled() &&
                protocolVersion is < MC_1_8_Version or > MC_26_1_Version)
            {
                log.Error($"§c{Translations.extra_inventory_disabled}");
                handler.SetInventoryEnabled(false);
            }

            if (handler.GetEntityHandlingEnabled() &&
                protocolVersion is < MC_1_8_Version or > MC_26_1_Version)
            {
                log.Error($"§c{Translations.extra_entity_disabled}");
                handler.SetEntityHandlingEnabled(false);
            }

            Block.Palette = protocolVersion switch
            {
                // Block palette
                > MC_26_1_Version when handler.GetTerrainEnabled() =>
                    throw new NotImplementedException(Translations.exception_palette_block),
                >= MC_26_1_Version => new Palette261(),
                >= MC_1_21_9_Version => new Palette1219(),
                >= MC_1_21_6_Version => new Palette1216(),  // 1.21.7/1.21.8 blocks unchanged, reuse 1216
                >= MC_1_21_5_Version => new Palette1215(),
                >= MC_1_21_4_Version => new Palette1214(),
                >= MC_1_21_2_Version => new Palette1212(),
                >= MC_1_20_6_Version => new Palette1206(),
                >= MC_1_20_4_Version => new Palette1204(),
                >= MC_1_20_Version => new Palette120(),
                MC_1_19_4_Version => new Palette1194(),
                MC_1_19_3_Version => new Palette1193(),
                >= MC_1_19_Version => new Palette119(),
                >= MC_1_17_Version => new Palette117(),
                >= MC_1_16_Version => new Palette116(),
                >= MC_1_15_Version => new Palette115(),
                >= MC_1_14_Version => new Palette114(),
                >= MC_1_13_Version => new Palette113(),
                _ => new Palette112()
            };

            entityPalette = protocolVersion switch
            {
                // Entity palette
                > MC_26_1_Version when handler.GetEntityHandlingEnabled() =>
                    throw new NotImplementedException(Translations.exception_palette_entity),
                >= MC_26_1_Version => new EntityPalette261(),
                >= MC_1_21_11_Version => new EntityPalette12111(),
                >= MC_1_21_9_Version => new EntityPalette1219(),
                >= MC_1_21_6_Version => new EntityPalette1216(),  // 1.21.7/1.21.8 entities unchanged, reuse 1216
                >= MC_1_21_5_Version => new EntityPalette1215(),
                >= MC_1_21_4_Version => new EntityPalette1214(),
                >= MC_1_21_2_Version => new EntityPalette1212(),
                >= MC_1_20_6_Version => new EntityPalette1206(),
                >= MC_1_20_4_Version => new EntityPalette1204(),
                >= MC_1_20_Version => new EntityPalette120(),
                MC_1_19_4_Version => new EntityPalette1194(),
                MC_1_19_3_Version => new EntityPalette1193(),
                >= MC_1_19_Version => new EntityPalette119(),
                >= MC_1_17_Version => new EntityPalette117(),
                >= MC_1_16_2_Version => new EntityPalette1162(),
                >= MC_1_16_Version => new EntityPalette1161(),
                >= MC_1_15_Version => new EntityPalette115(),
                >= MC_1_14_Version => new EntityPalette114(),
                >= MC_1_13_Version => new EntityPalette113(),
                >= MC_1_12_Version => new EntityPalette112(),
                _ => new EntityPalette18()
            };

            entityMetadataPalette = EntityMetadataPalette.GetPalette(protocolVersion);

            itemPalette = protocolVersion switch
            {
                // Item palette
                > MC_26_1_Version when handler.GetInventoryEnabled() =>
                    throw new NotImplementedException(Translations.exception_palette_item),
                >= MC_26_1_Version => new ItemPalette261(),
                >= MC_1_21_11_Version => new ItemPalette12111(),
                >= MC_1_21_9_Version => new ItemPalette1219(),
                >= MC_1_21_7_Version => new ItemPalette1217(),
                >= MC_1_21_6_Version => new ItemPalette1216(),
                >= MC_1_21_5_Version => new ItemPalette1215(),
                >= MC_1_21_4_Version => new ItemPalette1214(),
                >= MC_1_21_2_Version => new ItemPalette1212(),
                >= MC_1_21_Version => new ItemPalette121(),
                >= MC_1_20_6_Version => new ItemPalette1206(),
                >= MC_1_20_4_Version => new ItemPalette1204(),
                >= MC_1_20_Version => new ItemPalette120(),
                MC_1_19_4_Version => new ItemPalette1194(),
                MC_1_19_3_Version => new ItemPalette1193(),
                >= MC_1_19_Version => new ItemPalette119(),
                >= MC_1_18_1_Version => new ItemPalette118(),
                >= MC_1_17_Version => new ItemPalette117(),
                >= MC_1_16_2_Version => new ItemPalette1162(),
                >= MC_1_16_1_Version => new ItemPalette1161(),
                >= MC_1_15_Version => new ItemPalette115(),
                >= MC_1_12_Version => new ItemPalette112(),
                >= MC_1_11_Version => new ItemPalette111(),
                >= MC_1_10_Version => new ItemPalette110(),
                >= MC_1_9_Version => new ItemPalette19(),
                _ => new ItemPalette18()
            };

            ChatParser.ChatId2Type = this.protocolVersion switch
            {
                // MessageType 
                // You can find it in https://wiki.vg/Protocol#Player_Chat_Message or /net/minecraft/network/message/MessageType.java
                >= MC_1_19_2_Version => new()
                {
                    { 0, ChatParser.MessageType.CHAT },
                    { 1, ChatParser.MessageType.SAY_COMMAND },
                    { 2, ChatParser.MessageType.MSG_COMMAND_INCOMING },
                    { 3, ChatParser.MessageType.MSG_COMMAND_OUTGOING },
                    { 4, ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING },
                    { 5, ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING },
                    { 6, ChatParser.MessageType.EMOTE_COMMAND },
                },
                MC_1_19_Version => new()
                {
                    { 0, ChatParser.MessageType.CHAT },
                    { 1, ChatParser.MessageType.RAW_MSG },
                    { 2, ChatParser.MessageType.RAW_MSG },
                    { 3, ChatParser.MessageType.SAY_COMMAND },
                    { 4, ChatParser.MessageType.MSG_COMMAND_INCOMING },
                    { 5, ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING },
                    { 6, ChatParser.MessageType.EMOTE_COMMAND },
                    { 7, ChatParser.MessageType.RAW_MSG },
                },
                _ => ChatParser.ChatId2Type
            };
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>
        private void Updater(object? o)
        {
            var cancelToken = (CancellationToken)o!;

            if (cancelToken.IsCancellationRequested)
                return;

            try
            {
                Stopwatch stopWatch = Stopwatch.StartNew();
                long nextUpdateDue = 0;
                while (!packetQueue.IsAddingCompleted)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    long elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
                    while (elapsedMilliseconds >= nextUpdateDue)
                    {
                        handler.OnUpdate();
                        nextUpdateDue += ClientTickIntervalMilliseconds;
                        elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
                    }

                    if (packetQueue.TryTake(out var packetInfo, 1))
                    {
                        var (packetId, packetData) = packetInfo;
                        HandlePacket(packetId, packetData);
                        continue;
                    }

                    long sleepLength = nextUpdateDue - stopWatch.ElapsedMilliseconds;
                    if (sleepLength > 1)
                        Thread.Sleep((int)Math.Min(sleepLength, ClientTickIntervalMilliseconds));
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (NullReferenceException)
            {
            }
            catch (SocketException)
            {
            }
            catch (System.IO.IOException)
            {
            }

            if (cancelToken.IsCancellationRequested)
                return;

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        /// <summary>
        /// Read and decompress packets.
        /// </summary>
        internal void PacketReader(object? o)
        {
            var cancelToken = (CancellationToken)o!;
            while (socketWrapper.IsConnected() && !cancelToken.IsCancellationRequested)
            {
                try
                {
                    while (socketWrapper.HasDataAvailable())
                    {
                        packetQueue.Add(ReadNextPacket(), cancelToken);

                        if (cancelToken.IsCancellationRequested)
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (System.IO.IOException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }
                catch (NullReferenceException)
                {
                    break;
                }
                catch (System.IO.InvalidDataException)
                {
                    break;
                }

                if (cancelToken.IsCancellationRequested)
                    break;

                Thread.Sleep(10);
            }

            packetQueue.CompleteAdding();
        }

        /// <summary>
        /// Read the next packet from the network
        /// </summary>
        /// <param name="packetId">will contain packet ID</param>
        /// <param name="packetData">will contain raw packet Data</param>
        internal Tuple<int, Queue<byte>> ReadNextPacket()
        {
            var size = dataTypes.ReadNextVarIntRAW(socketWrapper); //Packet size
            Queue<byte> packetData = new(socketWrapper.ReadDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (protocolVersion >= MC_1_8_Version
                && compression_treshold >= 0)
            {
                var sizeUncompressed = dataTypes.ReadNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    var toDecompress = packetData.ToArray();
                    var uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData = new Queue<byte>(uncompressed);
                }
            }

            var packetId = dataTypes.ReadNextVarInt(packetData); // Packet ID
            if (handler.GetNetworkPacketCaptureEnabled())
                handler.OnNetworkPacket(packetId, packetData.ToList(), currentState == CurrentState.Login, true);

            return new(packetId, packetData);
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetId">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        internal bool HandlePacket(int packetId, Queue<byte> packetData)
        {
            // This copy is necessary because by the time we get to the catch block,
            // the packetData queue will have been processed and the data will be lost
            var _copy = packetData.ToArray();

            try
            {
                switch (currentState)
                {
                    // https://wiki.vg/Protocol#Login
                    case CurrentState.Login:
                        switch (packetId)
                        {
                            // Set Compression
                            case 0x03:
                                if (protocolVersion >= MC_1_8_Version)
                                    compression_treshold = dataTypes.ReadNextVarInt(packetData);
                                break;

                            // Login Plugin Request
                            case 0x04:
                                var messageId = dataTypes.ReadNextVarInt(packetData);
                                var channel = dataTypes.ReadNextString(packetData);
                                List<byte> responseData = new();
                                var understood = pForge.HandleLoginPluginRequest(channel, packetData, ref responseData);
                                SendLoginPluginResponse(messageId, understood, responseData.ToArray());
                                break;

                            // Cookie Request
                            case 0x05:
                                var cookieName = dataTypes.ReadNextString(packetData);
                                var cookieData = null as byte[];
                                McClient.Instance?.GetCookie(cookieName, out cookieData);
                                SendCookieResponse(cookieName, cookieData);
                                break;

                            // Ignore other packets at this stage
                            default:
                                return true;
                        }

                        break;

                    // https://wiki.vg/Protocol#Configuration
                    case CurrentState.Configuration:
                        if (!packetPalette.GetMappingInConfiguration().TryGetValue(packetId, out var configurationPacketType))
                        {
                            if (packetPalette.GetMappingIn().ContainsKey(packetId))
                            {
                                currentState = CurrentState.Play;
                                return HandlePlayPackets(packetId, packetData);
                            }

                            throw new KeyNotFoundException("Configuration Packet ID of 0x" + packetId.ToString("X2") +
                                                           " doesn't exist!");
                        }

                        switch (configurationPacketType)
                        {
                            case ConfigurationPacketTypesIn.CookieRequest:
                                var cookieName = dataTypes.ReadNextString(packetData);
                                var cookieData = null as byte[];
                                McClient.Instance?.GetCookie(cookieName, out cookieData);
                                SendCookieResponse(cookieName, cookieData);
                                break;

                            case ConfigurationPacketTypesIn.Disconnect:
                                handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick,
                                    dataTypes.ReadNextChat(packetData));
                                return false;

                            case ConfigurationPacketTypesIn.FinishConfiguration:
                                currentState = CurrentState.Play;
                                SendPacket(ConfigurationPacketTypesOut.FinishConfiguration, new List<byte>());
                                break;

                            case ConfigurationPacketTypesIn.KeepAlive:
                                SendPacket(ConfigurationPacketTypesOut.KeepAlive, packetData);
                                break;

                            case ConfigurationPacketTypesIn.Ping:
                                SendPacket(ConfigurationPacketTypesOut.Pong, packetData);
                                break;

                            case ConfigurationPacketTypesIn.RegistryData:
                                if (protocolVersion < MC_1_20_6_Version)
                                {
                                    var registryCodec = dataTypes.ReadNextNbt(packetData);
                                    ChatParser.ReadChatType(registryCodec);

                                    if (handler.GetTerrainEnabled())
                                        World.StoreDimensionList(registryCodec);
                                }
                                else
                                {
                                    var registryId = dataTypes.ReadNextString(packetData);
                                    var entryCount = dataTypes.ReadNextVarInt(packetData);

                                    var isChat = registryId == "minecraft:chat_type";
                                    var isDimension = registryId == "minecraft:dimension_type";
                                    var isAttribute = registryId == "minecraft:attribute";
                                    var isEnchantment = registryId == "minecraft:enchantment";

                                    var availableChats = isChat ? new Dictionary<int, string>() : null;
                                    var dimensionIdMap = isDimension ? new Dictionary<int, string>() : null;
                                    var attributeIdMap = isAttribute ? new Dictionary<int, string>() : null;
                                    var enchantmentIdMap = isEnchantment ? new Dictionary<int, string>() : null;

                                    for (var i = 0; i < entryCount; i++)
                                    {
                                        var entryId = dataTypes.ReadNextString(packetData);
                                        var hasData = dataTypes.ReadNextBool(packetData);

                                        Dictionary<string, object>? nbtData = null;
                                        if (hasData)
                                            nbtData = dataTypes.ReadNextNbt(packetData);

                                        if (isChat)
                                            availableChats!.Add(i, entryId);
                                        else if (isDimension)
                                        {
                                            dimensionIdMap!.Add(i, entryId);
                                            if (nbtData is not null && handler.GetTerrainEnabled())
                                                World.StoreOneDimension(entryId, nbtData);
                                        }
                                        else if (isAttribute)
                                        {
                                            var attrName = entryId.StartsWith("minecraft:")
                                                ? entryId.Substring("minecraft:".Length)
                                                : entryId;
                                            attributeIdMap!.Add(i, attrName);
                                        }
                                        else if (isEnchantment)
                                            enchantmentIdMap!.Add(i, entryId);
                                    }

                                    if (isChat)
                                        ChatParser.ReadChatType(availableChats!);
                                    else if (isDimension)
                                    {
                                        World.SetDimensionIdMap(dimensionIdMap!);
                                        if (!handler.GetTerrainEnabled() || !World.HasAnyDimension())
                                            World.LoadDefaultDimensions1206Plus();
                                    }
                                    else if (isAttribute)
                                        World.SetAttributeIdMap(attributeIdMap!);
                                    else if (isEnchantment)
                                        EnchantmentMapping.SetDynamicEnchantmentIdMap(enchantmentIdMap!);
                                }

                                break;

                            case ConfigurationPacketTypesIn.RemoveResourcePack:
                                if (dataTypes.ReadNextBool(packetData)) // Has UUID
                                    dataTypes.ReadNextUUID(packetData); // UUID
                                break;

                            case ConfigurationPacketTypesIn.ResourcePack:
                                HandleResourcePackPacket(packetData);
                                break;

                            case ConfigurationPacketTypesIn.StoreCookie:
                                var name = dataTypes.ReadNextString(packetData);
                                var data = dataTypes.ReadNextByteArray(packetData);
                                McClient.Instance?.SetCookie(name, data);
                                break;

                            case ConfigurationPacketTypesIn.Transfer:
                                var host = dataTypes.ReadNextString(packetData);
                                var port = dataTypes.ReadNextVarInt(packetData);

                                McClient.Instance?.Transfer(host, port);
                                break;

                            case ConfigurationPacketTypesIn.KnownDataPacks:
                                var knownPacksCount = dataTypes.ReadNextVarInt(packetData);
                                List<(string, string, string)> knownDataPacks = new();

                                for (var i = 0; i < knownPacksCount; i++)
                                {
                                    var nameSpace = dataTypes.ReadNextString(packetData);
                                    var id = dataTypes.ReadNextString(packetData);
                                    var version = dataTypes.ReadNextString(packetData);
                                    knownDataPacks.Add((nameSpace, id, version));
                                }

                                var vanillaPacks = knownDataPacks
                                    .Where(p => p.Item1 == "minecraft")
                                    .ToList();
                                SendKnownDataPacks(vanillaPacks);
                                break;

                            case ConfigurationPacketTypesIn.CustomReportDetails:
                                var cfgDetailsCount = dataTypes.ReadNextVarInt(packetData);
                                for (var i = 0; i < cfgDetailsCount; i++)
                                {
                                    dataTypes.ReadNextString(packetData); // Title
                                    dataTypes.ReadNextString(packetData); // Description
                                }
                                break;

                            case ConfigurationPacketTypesIn.ServerLinks:
                                var cfgLinksCount = dataTypes.ReadNextVarInt(packetData);
                                for (var i = 0; i < cfgLinksCount; i++)
                                {
                                    var cfgIsBuiltIn = dataTypes.ReadNextBool(packetData);
                                    if (cfgIsBuiltIn)
                                        dataTypes.ReadNextVarInt(packetData); // Known type ID
                                    else
                                        dataTypes.ReadNextChat(packetData); // Component label
                                    dataTypes.ReadNextString(packetData); // URL
                                }
                                break;

                            // Ignore other packets at this stage
                            default:
                                return true;
                        }

                        break;

                    // https://wiki.vg/Protocol#Play
                    case CurrentState.Play:
                        return HandlePlayPackets(packetId, packetData);

                    default:
                        return true;
                }
            }
            catch (Exception innerException)
            {
                if (innerException is ThreadAbortException || innerException is SocketException ||
                    innerException.InnerException is SocketException)
                    throw; //Thread abort or Connection lost rather than invalid data

                var exception = new System.IO.InvalidDataException(
                    string.Format(Translations.exception_packet_process,
                        packetPalette.GetIncomingTypeById(packetId),
                        packetId,
                        protocolVersion,
                        currentState == CurrentState.Login,
                        innerException.GetType()),
                    innerException);

                SentrySdk.AddBreadcrumb(new Breadcrumb("S -> C Packet", "network", new Dictionary<string, string>()
                {
                    { "Packet ID", packetId.ToString() },
                    { "Packet Type ", packetPalette.GetIncomingTypeById(packetId).ToString() },
                    { "Protocol Version", protocolVersion.ToString() },
                    { "Minecraft Version", ProtocolHandler.ProtocolVersion2MCVer(protocolVersion) },
                    { "Current State", currentState.ToString() },
                    { "Packet Data", string.Join(" ", _copy.Select(b => b.ToString("X2"))) },
                    { "Inner Exception", innerException.GetType().ToString() }
                }, "packet", BreadcrumbLevel.Error));

                SentrySdk.CaptureException(exception);

                throw exception;
            }

            return true;
        }

        public void HandleResourcePackPacket(Queue<byte> packetData)
        {
            var uuid = Guid.Empty;

            if (protocolVersion >= MC_1_20_4_Version)
                uuid = dataTypes.ReadNextUUID(packetData);

            var url = dataTypes.ReadNextString(packetData);
            var hash = dataTypes.ReadNextString(packetData);

            if (protocolVersion >= MC_1_17_Version)
            {
                dataTypes.ReadNextBool(packetData); // Forced
                if (dataTypes.ReadNextBool(packetData)) // Has Prompt Message
                    dataTypes.ReadNextChat(packetData); // Prompt Message
            }

            // Some server plugins may send invalid resource packs to probe the client and we need to ignore them (issue #1056)
            if (!url.StartsWith("http") &&
                hash.Length != 40) // Some server may have null hash value
                return;

            //Send back "accepted" and "successfully loaded" responses for plugins or server config making use of resource pack mandatory
            var responseHeader =
                protocolVersion < MC_1_10_Version // After 1.10, the MC does not include resource pack hash in responses
                    ? dataTypes.ConcatBytes(DataTypes.GetVarInt(hash.Length), Encoding.UTF8.GetBytes(hash))
                    : [];

            var basePacketData = protocolVersion >= MC_1_20_4_Version && uuid != Guid.Empty
                ? dataTypes.ConcatBytes(responseHeader, DataTypes.GetUUID(uuid))
                : responseHeader;

            var acceptedResourcePackData = dataTypes.ConcatBytes(basePacketData, DataTypes.GetVarInt(3));
            var loadedResourcePackData = dataTypes.ConcatBytes(basePacketData, DataTypes.GetVarInt(0));

            if (currentState == CurrentState.Configuration)
            {
                SendPacket(ConfigurationPacketTypesOut.ResourcePackResponse, acceptedResourcePackData); // Accepted
                SendPacket(ConfigurationPacketTypesOut.ResourcePackResponse,
                    loadedResourcePackData); // Successfully loaded
            }
            else
            {
                SendPacket(PacketTypesOut.ResourcePackStatus, acceptedResourcePackData); // Accepted
                SendPacket(PacketTypesOut.ResourcePackStatus, loadedResourcePackData); // Successfully loaded
            }
        }

        private bool HandlePlayPackets(int packetId, Queue<byte> packetData)
        {
            switch (packetPalette.GetIncomingTypeById(packetId))
            {
                case PacketTypesIn.KeepAlive: // Keep Alive (Play)
                    SendPacket(PacketTypesOut.KeepAlive, packetData);
                    handler.OnServerKeepAlive();
                    break;

                case PacketTypesIn.Ping:
                    SendPacket(PacketTypesOut.Pong, packetData);
                    break;

                case PacketTypesIn.JoinGame:
                    // Temporary fix
                    log.Debug("Receive JoinGame");

                    receiveDeclareCommands = receivePlayerInfo = false;

                    messageIndex = 0;
                    pendingAcknowledgments = 0;

                    lastReceivedMessage = null;
                    lastSeenMessagesCollector = protocolVersion >= MC_1_19_3_Version ? new(20) : new(5);

                    handler.OnGameJoined(isOnlineMode);

                    var playerEntityId = dataTypes.ReadNextInt(packetData);
                    handler.OnReceivePlayerEntityID(playerEntityId);

                    if (protocolVersion >= MC_1_16_2_Version)
                        dataTypes.ReadNextBool(packetData); // Is hardcore - 1.16.2 and above

                    if (protocolVersion < MC_1_20_2_Version)
                        handler.OnGamemodeUpdate(Guid.Empty, dataTypes.ReadNextByte(packetData));

                    if (protocolVersion >= MC_1_16_Version)
                    {
                        if (protocolVersion < MC_1_20_2_Version)
                            dataTypes.ReadNextByte(packetData); // Previous Gamemode - 1.16 - 1.20.2

                        var worldCount =
                            dataTypes.ReadNextVarInt(
                                packetData); // Dimension Count (World Count) - 1.16 and above
                        for (var i = 0; i < worldCount; i++)
                            dataTypes.ReadNextString(
                                packetData); // Dimension Names (World Names) - 1.16 and above

                        if (protocolVersion < MC_1_20_2_Version)
                        {
                            var registryCodec =
                                dataTypes.ReadNextNbt(
                                    packetData); // Registry Codec (Dimension Codec) - 1.16 - 1.20.1
                            if (protocolVersion >= MC_1_19_Version)
                                ChatParser.ReadChatType(registryCodec);
                            if (handler.GetTerrainEnabled())
                                World.StoreDimensionList(registryCodec);
                        }
                    }

                    if (protocolVersion < MC_1_20_2_Version)
                    {
                        // Current dimension
                        //   String: 1.19 and above
                        //   NBT Tag Compound: [1.16.2 to 1.18.2]
                        //   String identifier: 1.16 and 1.16.1
                        //   varInt: [1.9.1 to 1.15.2]
                        //   byte: below 1.9.1
                        string? dimensionTypeName = null;
                        Dictionary<string, object>? dimensionType = null;
                        switch (protocolVersion)
                        {
                            case >= MC_1_16_Version:
                                {
                                    switch (protocolVersion)
                                    {
                                        case >= MC_1_19_Version:
                                            dimensionTypeName =
                                                dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                            break;
                                        case >= MC_1_16_2_Version:
                                            dimensionType =
                                                dataTypes.ReadNextNbt(
                                                    packetData); // Dimension Type: NBT Tag Compound
                                            break;
                                        default:
                                            dataTypes.ReadNextString(packetData);
                                            break;
                                    }

                                    currentDimension = 0;
                                    break;
                                }
                            case >= MC_1_9_1_Version:
                                currentDimension = dataTypes.ReadNextInt(packetData);
                                break;
                            default:
                                currentDimension = (sbyte)dataTypes.ReadNextByte(packetData);
                                break;
                        }

                        switch (protocolVersion)
                        {
                            case < MC_1_14_Version:
                                dataTypes.ReadNextByte(packetData); // Difficulty - 1.13 and below
                                break;
                            case >= MC_1_16_Version:
                                {
                                    var dimensionName =
                                        dataTypes.ReadNextString(
                                            packetData); // Dimension Name (World Name) - 1.16 and above

                                    if (handler.GetTerrainEnabled())
                                    {
                                        switch (protocolVersion)
                                        {
                                            case >= MC_1_16_2_Version and <= MC_1_18_2_Version:
                                                World.StoreOneDimension(dimensionName, dimensionType!);
                                                World.SetDimension(dimensionName);
                                                break;
                                            default:
                                                World.SetDimension(dimensionTypeName!);
                                                break;
                                        }
                                    }

                                    break;
                                }
                        }
                    }

                    if (protocolVersion is >= MC_1_15_Version and < MC_1_20_2_Version)
                        dataTypes.ReadNextLong(packetData); // Hashed world seed - 1.15 - 1.20.2
                    if (protocolVersion >= MC_1_16_2_Version)
                        dataTypes.ReadNextVarInt(packetData); // Max Players - 1.16.2 and above
                    else
                        dataTypes.ReadNextByte(packetData); // Max Players - 1.16.1 and below
                    if (protocolVersion < MC_1_16_Version)
                        dataTypes.SkipNextString(packetData); // Level Type - 1.15 and below
                    if (protocolVersion >= MC_1_14_Version)
                        dataTypes.ReadNextVarInt(packetData); // View distance - 1.14 and above
                    if (protocolVersion >= MC_1_18_1_Version)
                        dataTypes.ReadNextVarInt(packetData); // Simulation Distance - 1.18 and above
                    if (protocolVersion >= MC_1_8_Version)
                        dataTypes.ReadNextBool(packetData); // Reduced debug info - 1.8 and above
                    if (protocolVersion >= MC_1_15_Version)
                        dataTypes.ReadNextBool(packetData); // Enable respawn screen - 1.15 and above

                    if (protocolVersion < MC_1_20_2_Version)
                    {
                        if (protocolVersion >= MC_1_16_Version)
                        {
                            dataTypes.ReadNextBool(packetData); // Is Debug - 1.16 and 1.20.2
                            dataTypes.ReadNextBool(packetData); // Is Flat - 1.16 and 1.20.2
                        }

                        if (protocolVersion >= MC_1_19_Version)
                        {
                            if (dataTypes.ReadNextBool(packetData)) // Has death location
                            {
                                dataTypes.SkipNextString(packetData); // Death dimension name: Identifier
                                dataTypes.ReadNextLocation(packetData); // Death location
                            }
                        }

                        if (protocolVersion >= MC_1_20_Version)
                            dataTypes.ReadNextVarInt(packetData); // Portal Cooldown - 1.20 and above
                    }
                    else
                    {
                        dataTypes.ReadNextBool(packetData); // Do limited crafting

                        var dimensionTypeName = protocolVersion < MC_1_20_6_Version
                            ? dataTypes.ReadNextString(packetData)
                            : World.GetDimensionNameById(dataTypes.ReadNextVarInt(packetData));

                        dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above

                        if (handler.GetTerrainEnabled())
                            World.SetDimension(dimensionTypeName);

                        dataTypes.ReadNextLong(packetData); // Hashed world seed
                        handler.OnGamemodeUpdate(Guid.Empty, dataTypes.ReadNextByte(packetData));
                        dataTypes.ReadNextByte(packetData); // Previous Gamemode
                        dataTypes.ReadNextBool(packetData); // Is Debug
                        dataTypes.ReadNextBool(packetData); // Is Flat
                        var hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                        if (hasDeathLocation)
                        {
                            dataTypes.SkipNextString(packetData); // Death dimension name: Identifier
                            dataTypes.ReadNextLocation(packetData); // Death location
                        }

                        dataTypes.ReadNextVarInt(packetData); // Portal Cooldown

                        if (protocolVersion >= MC_1_20_6_Version)
                            dataTypes.ReadNextBool(packetData); // Enforoces Secure Chat
                    }

                    if (protocolVersion >= MC_1_21_4_Version)
                        SendPacket(PacketTypesOut.PlayerLoaded, new List<byte>());

                    break;
                case PacketTypesIn.SpawnPainting: // Just skip, no need for this
                    return true;
                case PacketTypesIn.DeclareCommands:
                    if (protocolVersion >= MC_1_19_Version)
                    {
                        log.Debug("Receive DeclareCommands");
                        DeclareCommands.Read(dataTypes, packetData, protocolVersion);
                        receiveDeclareCommands = true;
                        if (receivePlayerInfo)
                            handler.SetCanSendMessage(true);
                    }

                    break;
                case PacketTypesIn.ChatMessage:
                    var messageType = 0;

                    if (protocolVersion <= MC_1_18_2_Version) // 1.18 and bellow
                    {
                        var message = dataTypes.ReadNextString(packetData);

                        Guid senderUuid;
                        if (protocolVersion >= MC_1_8_Version)
                        {
                            //Hide system messages or xp bar messages?
                            messageType = dataTypes.ReadNextByte(packetData);
                            if (messageType == 1 && !Config.Main.Advanced.ShowSystemMessages
                                || messageType == 2 && !Config.Main.Advanced.ShowSystemMessages)
                                break;

                            senderUuid = protocolVersion >= MC_1_16_5_Version
                                ? dataTypes.ReadNextUUID(packetData)
                                : Guid.Empty;
                        }
                        else
                            senderUuid = Guid.Empty;

                        handler.OnTextReceived(new(message, null, true, messageType, senderUuid));
                    }
                    else if (protocolVersion == MC_1_19_Version) // 1.19
                    {
                        var signedChat = dataTypes.ReadNextString(packetData);
                        var hasUnsignedChatContent = dataTypes.ReadNextBool(packetData);
                        var unsignedChatContent = hasUnsignedChatContent ? dataTypes.ReadNextString(packetData) : null;

                        messageType = dataTypes.ReadNextVarInt(packetData);
                        if (messageType == 1 && !Config.Main.Advanced.ShowSystemMessages
                            || messageType == 2 && !Config.Main.Advanced.ShowXPBarMessages)
                            break;

                        var senderUuid = dataTypes.ReadNextUUID(packetData);
                        var senderDisplayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                        var hasSenderTeamName = dataTypes.ReadNextBool(packetData);
                        var senderTeamName = hasSenderTeamName
                            ? ChatParser.ParseText(dataTypes.ReadNextString(packetData))
                            : null;

                        var timestamp = dataTypes.ReadNextLong(packetData);
                        var salt = dataTypes.ReadNextLong(packetData);
                        var messageSignature = dataTypes.ReadNextByteArray(packetData);

                        bool verifyResult;
                        if (!isOnlineMode)
                            verifyResult = false;
                        else if (senderUuid == handler.GetUserUuid())
                            verifyResult = true;
                        else
                        {
                            var player = handler.GetPlayerInfo(senderUuid);
                            verifyResult = player is not null && player.VerifyMessage(signedChat, timestamp, salt,
                                ref messageSignature);
                        }

                        ChatMessage chat = new(signedChat, true, messageType, senderUuid, unsignedChatContent,
                            senderDisplayName, senderTeamName, timestamp, messageSignature, verifyResult);
                        handler.OnTextReceived(chat);
                    }
                    else if (protocolVersion == MC_1_19_2_Version)
                    {
                        // 1.19.1 - 1.19.2
                        var precedingSignature = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextByteArray(packetData)
                            : null;
                        var senderUuid = dataTypes.ReadNextUUID(packetData);
                        var headerSignature = dataTypes.ReadNextByteArray(packetData);
                        var signedChat = dataTypes.ReadNextString(packetData);
                        var decorated = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextString(packetData)
                            : null;

                        var timestamp = dataTypes.ReadNextLong(packetData);
                        var salt = dataTypes.ReadNextLong(packetData);

                        var lastSeenMessageListLen = dataTypes.ReadNextVarInt(packetData);
                        var lastSeenMessageList =
                            new LastSeenMessageList.AcknowledgedMessage[lastSeenMessageListLen];

                        for (var i = 0; i < lastSeenMessageListLen; ++i)
                        {
                            var user = dataTypes.ReadNextUUID(packetData);
                            var lastSignature = dataTypes.ReadNextByteArray(packetData);
                            lastSeenMessageList[i] = new(user, lastSignature, true);
                        }

                        LastSeenMessageList lastSeenMessages = new(lastSeenMessageList);
                        var unsignedChatContent = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextString(packetData)
                            : null;

                        var filterEnum = (MessageFilterType)dataTypes.ReadNextVarInt(packetData);
                        if (filterEnum == MessageFilterType.PartiallyFiltered)
                            dataTypes.ReadNextULongArray(packetData);

                        var chatTypeId = dataTypes.ReadNextVarInt(packetData);
                        var chatName = dataTypes.ReadNextString(packetData);
                        var targetName = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextString(packetData)
                            : null;

                        var chatInfo = Json.ParseJson(chatName)?.AsObject();
                        var senderDisplayName = chatInfo is not null && chatInfo.Count > 0
                            ? (chatInfo.ContainsKey("insertion") ? chatInfo["insertion"] : chatInfo["text"])
                            .GetStringValue()
                            : "";
                        string? senderTeamName = null;
                        var messageTypeEnum =
                            ChatParser.ChatId2Type!.GetValueOrDefault(chatTypeId, ChatParser.MessageType.CHAT);

                        if (targetName is not null &&
                            (messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING ||
                             messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING))
                            senderTeamName = Json.ParseJson(targetName)!["with"]![0]!
                                ["text"]!.GetStringValue();

                        if (string.IsNullOrWhiteSpace(senderDisplayName))
                        {
                            var player = handler.GetPlayerInfo(senderUuid);
                            if (player is not null && (player.DisplayName is not null || player is { Name: not null }) &&
                                string.IsNullOrWhiteSpace(senderDisplayName))
                            {
                                senderDisplayName = ChatParser.ParseText(player.DisplayName ?? player.Name);
                                if (string.IsNullOrWhiteSpace(senderDisplayName))
                                    senderDisplayName = player.DisplayName ?? player.Name;
                                else
                                    senderDisplayName += "§r";
                            }
                        }

                        bool verifyResult;
                        if (!isOnlineMode)
                            verifyResult = false;
                        else if (senderUuid == handler.GetUserUuid())
                            verifyResult = true;
                        else
                        {
                            var player = handler.GetPlayerInfo(senderUuid);
                            if (player is null || !player.IsMessageChainLegal())
                                verifyResult = false;
                            else
                            {
                                var lastVerifyResult = player.IsMessageChainLegal();
                                verifyResult = player.VerifyMessage(signedChat, timestamp, salt,
                                    ref headerSignature, ref precedingSignature, lastSeenMessages);
                                if (lastVerifyResult && !verifyResult)
                                    log.Warn(string.Format(Translations.chat_message_chain_broken,
                                        senderDisplayName));
                            }
                        }

                        ChatMessage chat = new(signedChat, false, chatTypeId, senderUuid, unsignedChatContent,
                            senderDisplayName, senderTeamName, timestamp, headerSignature, verifyResult);
                        if (isOnlineMode && !chat.LacksSender())
                            Acknowledge(chat);
                        handler.OnTextReceived(chat);
                    }
                    else if (protocolVersion >= MC_1_19_3_Version)
                    {
                        // 1.19.3+
                        // Header section
                        // net.minecraft.network.packet.s2c.play.ChatMessageS2CPacket#write

                        // 1.21.5+: globalIndex prepended before sender UUID
                        if (protocolVersion >= MC_1_21_5_Version)
                            dataTypes.ReadNextVarInt(packetData);

                        var senderUuid = dataTypes.ReadNextUUID(packetData);
                        var index = dataTypes.ReadNextVarInt(packetData);
                        // Signature is fixed size of 256 bytes
                        var messageSignature = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextByteArray(packetData, 256)
                            : null;

                        // Body
                        // net.minecraft.network.message.MessageBody.Serialized#write
                        var message = dataTypes.ReadNextString(packetData);
                        var timestamp = dataTypes.ReadNextLong(packetData);
                        var salt = dataTypes.ReadNextLong(packetData);

                        // Previous Messages
                        // net.minecraft.network.message.LastSeenMessageList.Indexed#write
                        // net.minecraft.network.message.MessageSignatureData.Indexed#write
                        var totalPreviousMessages = dataTypes.ReadNextVarInt(packetData);
                        var previousMessageSignatures = new Tuple<int, byte[]?>[totalPreviousMessages];

                        for (var i = 0; i < totalPreviousMessages; i++)
                        {
                            // net.minecraft.network.message.MessageSignatureData.Indexed#fromBuf
                            var messageId = dataTypes.ReadNextVarInt(packetData) - 1;
                            if (messageId == -1)
                                previousMessageSignatures[i] = new Tuple<int, byte[]?>(messageId,
                                    dataTypes.ReadNextByteArray(packetData, 256));
                            else
                                previousMessageSignatures[i] = new Tuple<int, byte[]?>(messageId, null);
                        }

                        // Other
                        var unsignedChatContent = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextChat(packetData)
                            : null;

                        var filterType = (MessageFilterType)dataTypes.ReadNextVarInt(packetData);

                        if (filterType == MessageFilterType.PartiallyFiltered)
                            dataTypes.ReadNextULongArray(packetData);

                        // Network Target
                        // net.minecraft.network.message.MessageType.Serialized#write
                        var chatTypeId = dataTypes.ReadNextVarInt(packetData);
                        var chatName = dataTypes.ReadNextChat(packetData);
                        var targetName = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextChat(packetData)
                            : null;

                        var messageTypeEnum =
                            ChatParser.ChatId2Type!.GetValueOrDefault(chatTypeId, ChatParser.MessageType.CHAT);

                        //var chatInfo = Json.ParseJson(targetName ?? chatName).Properties;
                        var senderDisplayName = chatName;
                        string? senderTeamName = targetName;

                        if (string.IsNullOrWhiteSpace(senderDisplayName))
                        {
                            var player = handler.GetPlayerInfo(senderUuid);
                            if (player is not null && (player.DisplayName is not null || player.Name is not null) &&
                                string.IsNullOrWhiteSpace(senderDisplayName))
                            {
                                senderDisplayName = player.DisplayName ?? player.Name;
                                if (string.IsNullOrWhiteSpace(senderDisplayName))
                                    senderDisplayName = player.DisplayName ?? player.Name;
                                else
                                    senderDisplayName += "§r";
                            }
                        }

                        bool verifyResult;
                        if (!isOnlineMode || messageSignature is null)
                            verifyResult = false;
                        else
                        {
                            if (senderUuid == handler.GetUserUuid())
                                verifyResult = true;
                            else
                            {
                                var player = handler.GetPlayerInfo(senderUuid);
                                if (player is null || !player.IsMessageChainLegal())
                                    verifyResult = false;
                                else
                                {
                                    verifyResult = player.VerifyMessage(message, senderUuid, player.ChatUuid,
                                        index, timestamp, salt, ref messageSignature,
                                        previousMessageSignatures);
                                }
                            }
                        }

                        ChatMessage chat = new(message, false, chatTypeId, senderUuid, unsignedChatContent,
                            senderDisplayName, senderTeamName, timestamp, messageSignature, verifyResult);
                        lock (MessageSigningLock)
                            Acknowledge(chat);
                        handler.OnTextReceived(chat);
                    }

                    break;
                case PacketTypesIn.ChunkBatchFinished:
                    var batchSize = dataTypes.ReadNextVarInt(packetData); // Number of chunks received

                    if (batchSize > 0)
                    {
                        var d = GetNanos() - chunkBatchStartTime;
                        var d2 = d / (double)batchSize;
                        var d3 = Math.Clamp(d2, aggregatedNanosPerChunk / 3.0, aggregatedNanosPerChunk * 3.0);
                        aggregatedNanosPerChunk =
                            (aggregatedNanosPerChunk * oldSamplesWeight + d3) / (oldSamplesWeight + 1);
                        oldSamplesWeight = Math.Min(49, oldSamplesWeight + 1);
                    }

                    SendChunkBatchReceived((float)(7000000.0 / aggregatedNanosPerChunk));
                    break;
                case PacketTypesIn.ChunkBatchStarted:
                    chunkBatchStartTime = GetNanos();
                    break;
                case PacketTypesIn.StartConfiguration:
                    currentState = CurrentState.Configuration;
                    SendAcknowledgeConfiguration();
                    break;
                case PacketTypesIn.HideMessage:
                    var hideMessageSignature = dataTypes.ReadNextByteArray(packetData);
                    ConsoleIO.WriteLine(
                        $"HideMessage was not processed! (SigLen={hideMessageSignature.Length})");
                    break;
                case PacketTypesIn.SystemChat:
                    var systemMessage = dataTypes.ReadNextChat(packetData);

                    if (protocolVersion >= MC_1_19_3_Version)
                    {
                        var isOverlay = dataTypes.ReadNextBool(packetData);
                        if (isOverlay)
                        {
                            if (!Config.Main.Advanced.ShowXPBarMessages)
                                break;
                        }
                        else
                        {
                            if (!Config.Main.Advanced.ShowSystemMessages)
                                break;
                        }

                        handler.OnTextReceived(new(systemMessage, null, false, -1, Guid.Empty, true));
                    }
                    else
                    {
                        var msgType = dataTypes.ReadNextVarInt(packetData);
                        if (msgType == 1 && !Config.Main.Advanced.ShowSystemMessages)
                            break;
                        handler.OnTextReceived(new(systemMessage, null, true, msgType, Guid.Empty, true));
                    }

                    break;
                case PacketTypesIn.ProfilelessChatMessage:
                    var message_ = dataTypes.ReadNextChat(packetData);
                    var messageType_ = dataTypes.ReadNextVarInt(packetData);
                    var messageName = dataTypes.ReadNextChat(packetData);
                    var targetName_ = dataTypes.ReadNextBool(packetData)
                        ? dataTypes.ReadNextChat(packetData)
                        : null;
                    ChatMessage profilelessChat = new(message_, targetName_ ?? messageName, false, messageType_,
                        Guid.Empty, true);
                    profilelessChat.isSenderJson = false;
                    handler.OnTextReceived(profilelessChat);
                    break;
                case PacketTypesIn.CombatEvent:
                    // 1.8 - 1.16.5
                    if (protocolVersion is >= MC_1_8_Version and <= MC_1_16_5_Version)
                    {
                        var eventType = (CombatEventType)dataTypes.ReadNextVarInt(packetData);

                        if (eventType == CombatEventType.EntityDead)
                        {
                            dataTypes.SkipNextVarInt(packetData);

                            handler.OnPlayerKilled(
                                dataTypes.ReadNextInt(packetData),
                                ChatParser.ParseText(dataTypes.ReadNextString(packetData))
                            );
                        }
                    }

                    break;
                case PacketTypesIn.DeathCombatEvent:
                    dataTypes.SkipNextVarInt(packetData);

                    handler.OnPlayerKilled(
                        protocolVersion >= MC_1_20_Version ? -1 : dataTypes.ReadNextInt(packetData),
                        ChatParser.ParseText(dataTypes.ReadNextChat(packetData))
                    );

                    break;
                case PacketTypesIn.DamageEvent: // 1.19.4
                    if (handler.GetEntityHandlingEnabled() && protocolVersion >= MC_1_19_4_Version)
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        var sourceTypeId = dataTypes.ReadNextVarInt(packetData);
                        var sourceCauseId = dataTypes.ReadNextVarInt(packetData);
                        var sourceDirectId = dataTypes.ReadNextVarInt(packetData);

                        Location? sourcePosition;
                        if (dataTypes.ReadNextBool(packetData))
                        {
                            sourcePosition = new Location()
                            {
                                X = dataTypes.ReadNextDouble(packetData),
                                Y = dataTypes.ReadNextDouble(packetData),
                                Z = dataTypes.ReadNextDouble(packetData)
                            };
                        }

                        // Maybe write a function to use this data ? But seems not too useful
                    }

                    break;
                case PacketTypesIn.MessageHeader: // 1.19.2 only
                    if (protocolVersion == MC_1_19_2_Version)
                    {
                        var precedingSignature = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextByteArray(packetData)
                            : null;

                        var senderUuid = dataTypes.ReadNextUUID(packetData);
                        var headerSignature = dataTypes.ReadNextByteArray(packetData);
                        var bodyDigest = dataTypes.ReadNextByteArray(packetData);

                        bool verifyResult;

                        if (!isOnlineMode)
                            verifyResult = false;
                        else if (senderUuid == handler.GetUserUuid())
                            verifyResult = true;
                        else
                        {
                            var player = handler.GetPlayerInfo(senderUuid);

                            if (player is null || !player.IsMessageChainLegal())
                                verifyResult = false;
                            else
                            {
                                var lastVerifyResult = player.IsMessageChainLegal();
                                verifyResult = player.VerifyMessageHead(ref precedingSignature,
                                    ref headerSignature, ref bodyDigest);
                                if (lastVerifyResult && !verifyResult)
                                    log.Warn(string.Format(Translations.chat_message_chain_broken,
                                        player.Name));
                            }
                        }
                    }

                    break;
                case PacketTypesIn.Respawn:
                    string? dimensionTypeNameRespawn = null;
                    Dictionary<string, object>? dimensionTypeRespawn = null;

                    if (protocolVersion >= MC_1_16_Version)
                    {
                        switch (protocolVersion)
                        {
                            case >= MC_1_20_6_Version:
                                dimensionTypeNameRespawn = World.GetDimensionNameById(dataTypes.ReadNextVarInt(packetData));
                                break;
                            case >= MC_1_19_Version:
                                dimensionTypeNameRespawn =
                                    dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                break;
                            case >= MC_1_16_2_Version:
                                dimensionTypeRespawn =
                                    dataTypes.ReadNextNbt(packetData); // Dimension Type: NBT Tag Compound
                                break;
                            default:
                                dataTypes.ReadNextString(packetData);
                                break;
                        }

                        currentDimension = 0;
                    }
                    else
                    {
                        // 1.15 and below
                        currentDimension = dataTypes.ReadNextInt(packetData);
                    }

                    switch (protocolVersion)
                    {
                        case >= MC_1_16_Version:
                            {
                                var dimensionName =
                                    dataTypes.ReadNextString(
                                        packetData); // Dimension Name (World Name) - 1.16 and above

                                if (handler.GetTerrainEnabled())
                                {
                                    switch (protocolVersion)
                                    {
                                        case >= MC_1_16_2_Version and <= MC_1_18_2_Version:
                                            World.StoreOneDimension(dimensionName, dimensionTypeRespawn!);
                                            World.SetDimension(dimensionName);
                                            break;
                                        default:
                                            World.SetDimension(dimensionTypeNameRespawn!);
                                            break;
                                    }
                                }

                                break;
                            }
                        case < MC_1_14_Version:
                            dataTypes.ReadNextByte(packetData); // Difficulty - 1.13 and below
                            break;
                    }

                    if (protocolVersion >= MC_1_15_Version)
                        dataTypes.ReadNextLong(packetData); // Hashed world seed - 1.15 and above
                    dataTypes.ReadNextByte(packetData); // Gamemode

                    switch (protocolVersion)
                    {
                        case >= MC_1_16_Version:
                            dataTypes.ReadNextByte(packetData); // Previous Game mode - 1.16 and above
                            break;
                        case < MC_1_16_Version:
                            dataTypes.SkipNextString(packetData); // Level Type - 1.15 and below
                            break;
                    }

                    if (protocolVersion >= MC_1_16_Version)
                    {
                        dataTypes.ReadNextBool(packetData); // Is Debug - 1.16 and above
                        dataTypes.ReadNextBool(packetData); // Is Flat - 1.16 and above

                        if (protocolVersion < MC_1_20_2_Version)
                            dataTypes.ReadNextBool(packetData); // Copy metadata (Data Kept) - 1.16 - 1.20.2
                    }

                    if (protocolVersion >= MC_1_19_Version)
                    {
                        if (dataTypes.ReadNextBool(packetData)) // Has death location
                        {
                            dataTypes.ReadNextString(packetData); // Death dimension name: Identifier
                            dataTypes.ReadNextLocation(packetData); // Death location
                        }
                    }

                    if (protocolVersion >= MC_1_20_Version)
                        dataTypes.ReadNextVarInt(packetData); // Portal Cooldown

                    if (protocolVersion >= MC_1_20_2_Version)
                        dataTypes.ReadNextBool(packetData); // Copy metadata (Data Kept) - 1.20.2 and above

                    handler.OnRespawn();
                    break;
                case PacketTypesIn.PlayerPositionAndLook:
                    {
                        int teleportId;
                        Location location;
                        float yaw, pitch;
                        int locMask;

                        if (protocolVersion >= MC_1_21_2_Version)
                        {
                            teleportId = dataTypes.ReadNextVarInt(packetData);
                            location = new Location(
                                dataTypes.ReadNextDouble(packetData), // X
                                dataTypes.ReadNextDouble(packetData), // Y
                                dataTypes.ReadNextDouble(packetData)  // Z
                            );
                            dataTypes.ReadNextDouble(packetData); // Delta X
                            dataTypes.ReadNextDouble(packetData); // Delta Y
                            dataTypes.ReadNextDouble(packetData); // Delta Z
                            yaw = dataTypes.ReadNextFloat(packetData);
                            pitch = dataTypes.ReadNextFloat(packetData);
                            locMask = dataTypes.ReadNextInt(packetData); // Int flags (was Byte before 1.21.2)
                        }
                        else
                        {
                            location = new Location(
                                dataTypes.ReadNextDouble(packetData), // X
                                dataTypes.ReadNextDouble(packetData), // Y
                                dataTypes.ReadNextDouble(packetData)  // Z
                            );
                            yaw = dataTypes.ReadNextFloat(packetData);
                            pitch = dataTypes.ReadNextFloat(packetData);
                            locMask = dataTypes.ReadNextByte(packetData);
                            teleportId = protocolVersion >= MC_1_9_Version
                                ? dataTypes.ReadNextVarInt(packetData) : -1;
                        }

                        if (handler.GetTerrainEnabled() || handler.GetEntityHandlingEnabled())
                        {
                            if (protocolVersion >= MC_1_8_Version)
                            {
                                var currentLocation = handler.GetCurrentLocation();
                                location.X = (locMask & 1 << 0) != 0 ? currentLocation.X + location.X : location.X;
                                location.Y = (locMask & 1 << 1) != 0 ? currentLocation.Y + location.Y : location.Y;
                                location.Z = (locMask & 1 << 2) != 0 ? currentLocation.Z + location.Z : location.Z;
                            }
                        }

                        if (teleportId >= 0)
                        {
                            LastYaw = yaw;
                            LastPitch = pitch;
                            handler.UpdateLocation(location, yaw, pitch);
                            SendPacket(PacketTypesOut.TeleportConfirm, DataTypes.GetVarInt(teleportId));

                            if (Config.Main.Advanced.TemporaryFixBadpacket)
                            {
                                SendLocationUpdate(location, true, false, yaw, pitch, true);

                                if (teleportId == 1)
                                    SendLocationUpdate(location, true, false, yaw, pitch, true);
                            }
                        }
                        else
                        {
                            handler.UpdateLocation(location, yaw, pitch);
                            LastYaw = yaw;
                            LastPitch = pitch;
                        }

                        if (protocolVersion is >= MC_1_17_Version and < MC_1_19_4_Version)
                            dataTypes.ReadNextBool(packetData); // Dismount Vehicle    - 1.17 to 1.19.3
                    }
                    break;
                case PacketTypesIn.ChunkData:
                    if (handler.GetTerrainEnabled())
                    {
                        Interlocked.Increment(ref handler.GetWorld().chunkCnt);
                        Interlocked.Increment(ref handler.GetWorld().chunkLoadNotCompleted);

                        var chunkX = dataTypes.ReadNextInt(packetData);
                        var chunkZ = dataTypes.ReadNextInt(packetData);
                        if (protocolVersion >= MC_1_17_Version)
                        {
                            ulong[]? verticalStripBitmask = null;

                            if (protocolVersion is MC_1_17_Version or MC_1_17_1_Version)
                                verticalStripBitmask =
                                    dataTypes.ReadNextULongArray(
                                        packetData); // Bit Mask Length  and  Primary Bit Mask

                            if (protocolVersion >= MC_1_21_5_Version)
                            {
                                // 1.21.5: Heightmaps encoded as map<VarInt, long[]> instead of NBT
                                var hmCount = dataTypes.ReadNextVarInt(packetData);
                                for (var hm = 0; hm < hmCount; hm++)
                                {
                                    dataTypes.ReadNextVarInt(packetData); // Heightmap type id
                                    var longCount = dataTypes.ReadNextVarInt(packetData);
                                    for (var l = 0; l < longCount; l++)
                                        dataTypes.ReadNextLong(packetData);
                                }
                            }
                            else
                            {
                                dataTypes.ReadNextNbt(packetData); // Heightmaps (NBT format)
                            }

                            if (protocolVersion is MC_1_17_Version or MC_1_17_1_Version)
                            {
                                var biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length
                                for (var i = 0; i < biomesLength; i++)
                                    dataTypes.SkipNextVarInt(packetData); // Biomes
                            }

                            var dataSize = dataTypes.ReadNextVarInt(packetData); // Size

                            pTerrain.ProcessChunkColumnData(chunkX, chunkZ, verticalStripBitmask, packetData);
                            ProcessChunkBlockEntityData(chunkX, chunkZ, packetData);
                            Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);

                            // Block Entity data: ignored
                            // Trust edges: ignored (Removed in 1.20)
                            // Light data: ignored
                        }
                        else
                        {
                            var chunksContinuous = dataTypes.ReadNextBool(packetData);
                            if (protocolVersion is >= MC_1_16_Version and <= MC_1_16_1_Version)
                                dataTypes.ReadNextBool(packetData); // Ignore old data - 1.16 to 1.16.1 only
                            var chunkMask = protocolVersion >= MC_1_9_Version
                                ? (ushort)dataTypes.ReadNextVarInt(packetData)
                                : dataTypes.ReadNextUShort(packetData);
                            if (protocolVersion < MC_1_8_Version)
                            {
                                var addBitmap = dataTypes.ReadNextUShort(packetData);
                                var compressedDataSize = dataTypes.ReadNextInt(packetData);
                                var compressed = dataTypes.ReadData(compressedDataSize, packetData);
                                var decompressed = ZlibUtils.Decompress(compressed);

                                pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap,
                                    currentDimension == 0, chunksContinuous, currentDimension,
                                    new Queue<byte>(decompressed));
                                Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);
                            }
                            else
                            {
                                if (protocolVersion >= MC_1_14_Version)
                                    dataTypes.ReadNextNbt(packetData); // Heightmaps - 1.14 and above
                                var biomesLength = 0;
                                if (protocolVersion >= MC_1_16_2_Version)
                                    if (chunksContinuous)
                                        biomesLength =
                                            dataTypes.ReadNextVarInt(
                                                packetData); // Biomes length - 1.16.2 and above
                                if (protocolVersion >= MC_1_15_Version && chunksContinuous)
                                {
                                    if (protocolVersion >= MC_1_16_2_Version)
                                    {
                                        for (var i = 0; i < biomesLength; i++)
                                        {
                                            // Biomes - 1.16.2 and above
                                            // Don't use ReadNextVarInt because it cost too much time
                                            dataTypes.SkipNextVarInt(packetData);
                                        }
                                    }
                                    else dataTypes.DropData(1024 * 4, packetData); // Biomes - 1.15 and above
                                }

                                var dataSize = dataTypes.ReadNextVarInt(packetData);
                                pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false,
                                    chunksContinuous, currentDimension, packetData);
                                Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);
                            }
                        }
                    }

                    break;
                case PacketTypesIn.ChunksBiomes: // 1.19.4
                    // Biomes are not handled by MCC
                    break;
                case PacketTypesIn.MapData:
                    if (protocolVersion < MC_1_8_Version)
                        break;

                    var mapId = dataTypes.ReadNextVarInt(packetData);
                    var scale = dataTypes.ReadNextByte(packetData);


                    // 1.9 +
                    var trackingPosition = true;

                    // 1.14+
                    var locked = false;

                    // 1.17+ (locked and trackingPosition switched places)
                    if (protocolVersion >= MC_1_17_Version)
                    {
                        if (protocolVersion >= MC_1_14_Version)
                            locked = dataTypes.ReadNextBool(packetData);

                        if (protocolVersion >= MC_1_9_Version)
                            trackingPosition = dataTypes.ReadNextBool(packetData);
                    }
                    else
                    {
                        if (protocolVersion >= MC_1_9_Version)
                            trackingPosition = dataTypes.ReadNextBool(packetData);

                        if (protocolVersion >= MC_1_14_Version)
                            locked = dataTypes.ReadNextBool(packetData);
                    }

                    var iconCount = 0;
                    List<MapIcon> icons = new();

                    // 1,9 + = needs tracking position to be true to get the icons
                    if (protocolVersion <= MC_1_16_5_Version || trackingPosition)
                    {
                        iconCount = dataTypes.ReadNextVarInt(packetData);

                        for (var i = 0; i < iconCount; i++)
                        {
                            MapIcon mapIcon = new();

                            switch (protocolVersion)
                            {
                                // 1.8 - 1.13
                                case < MC_1_13_2_Version:
                                    {
                                        var directionAndType = dataTypes.ReadNextByte(packetData);
                                        byte direction, type;

                                        // 1.12.2+
                                        if (protocolVersion >= MC_1_12_2_Version)
                                        {
                                            direction = (byte)(directionAndType & 0xF);
                                            type = (byte)(directionAndType >> 4 & 0xF);
                                        }
                                        else // 1.8 - 1.12
                                        {
                                            direction = (byte)(directionAndType >> 4 & 0xF);
                                            type = (byte)(directionAndType & 0xF);
                                        }

                                        mapIcon.Type = (MapIconType)type;
                                        mapIcon.Direction = direction;
                                        break;
                                    }
                                // 1.13.2+
                                case >= MC_1_13_2_Version:
                                    mapIcon.Type = (MapIconType)dataTypes.ReadNextVarInt(packetData);
                                    break;
                            }

                            mapIcon.X = dataTypes.ReadNextByte(packetData);
                            mapIcon.Z = dataTypes.ReadNextByte(packetData);

                            // 1.13.2+
                            if (protocolVersion >= MC_1_13_2_Version)
                            {
                                mapIcon.Direction = dataTypes.ReadNextByte(packetData);

                                if (dataTypes.ReadNextBool(packetData)) // Has Display Name?
                                    mapIcon.DisplayName =
                                        ChatParser.ParseText(dataTypes.ReadNextChat(packetData));
                            }

                            icons.Add(mapIcon);
                        }
                    }

                    var columnsUpdated = dataTypes.ReadNextByte(packetData); // width
                    byte rowsUpdated = 0; // height
                    byte mapColumnX = 0;
                    byte mapRowZ = 0;
                    byte[]? colors = null;

                    if (columnsUpdated > 0)
                    {
                        rowsUpdated = dataTypes.ReadNextByte(packetData); // height
                        mapColumnX = dataTypes.ReadNextByte(packetData);
                        mapRowZ = dataTypes.ReadNextByte(packetData);
                        colors = dataTypes.ReadNextByteArray(packetData);
                    }

                    handler.OnMapData(mapId, scale, trackingPosition, locked, icons, columnsUpdated,
                        rowsUpdated, mapColumnX, mapRowZ, colors);
                    break;
                case PacketTypesIn.TradeList:
                    if (protocolVersion >= MC_1_14_Version && handler.GetInventoryEnabled())
                    {
                        // MC 1.14 or greater
                        var windowId = dataTypes.ReadNextVarInt(packetData);
                        int size = dataTypes.ReadNextByte(packetData);

                        List<VillagerTrade> trades = new();
                        for (var tradeId = 0; tradeId < size; tradeId++)
                        {
                            var trade = dataTypes.ReadNextTrade(packetData, itemPalette);
                            trades.Add(trade);
                        }

                        VillagerInfo villagerInfo = new()
                        {
                            Level = dataTypes.ReadNextVarInt(packetData),
                            Experience = dataTypes.ReadNextVarInt(packetData),
                            IsRegularVillager = dataTypes.ReadNextBool(packetData),
                            CanRestock = dataTypes.ReadNextBool(packetData)
                        };
                        handler.OnTradeList(windowId, trades, villagerInfo);
                    }

                    break;
                case PacketTypesIn.Title:
                    if (protocolVersion >= MC_1_8_Version)
                    {
                        var action2 = dataTypes.ReadNextVarInt(packetData);
                        var titleText = string.Empty;
                        var subtitleText = string.Empty;
                        var actionBarText = string.Empty;
                        var json = string.Empty;
                        var fadein = -1;
                        var stay = -1;
                        var fadeout = -1;

                        if (protocolVersion >= MC_1_10_Version)
                        {
                            switch (action2)
                            {
                                case 0:
                                    json = titleText;
                                    titleText = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    break;
                                case 1:
                                    json = subtitleText;
                                    subtitleText = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    break;
                                case 2:
                                    json = actionBarText;
                                    actionBarText = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    break;
                                case 3:
                                    fadein = dataTypes.ReadNextInt(packetData);
                                    stay = dataTypes.ReadNextInt(packetData);
                                    fadeout = dataTypes.ReadNextInt(packetData);
                                    break;
                            }
                        }
                        else
                        {
                            switch (action2)
                            {
                                case 0:
                                    json = titleText;
                                    titleText = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    break;
                                case 1:
                                    json = subtitleText;
                                    subtitleText = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    break;
                                case 2:
                                    fadein = dataTypes.ReadNextInt(packetData);
                                    stay = dataTypes.ReadNextInt(packetData);
                                    fadeout = dataTypes.ReadNextInt(packetData);
                                    break;
                            }
                        }

                        handler.OnTitle(action2, titleText, subtitleText, actionBarText, fadein, stay, fadeout,
                            json);
                    }

                    break;
                case PacketTypesIn.MultiBlockChange:
                    if (handler.GetTerrainEnabled())
                    {
                        if (protocolVersion >= MC_1_16_2_Version)
                        {
                            var chunkSection = dataTypes.ReadNextLong(packetData);
                            var sectionX = (int)(chunkSection >> 42);
                            var sectionY = (int)(chunkSection << 44 >> 44);
                            var sectionZ = (int)(chunkSection << 22 >> 42);

                            if (protocolVersion < MC_1_20_Version)
                                dataTypes.ReadNextBool(packetData); // Useless boolean (Related to light update)

                            var blocksSize = dataTypes.ReadNextVarInt(packetData);
                            for (var i = 0; i < blocksSize; i++)
                            {
                                var chunkSectionPosition = (ulong)dataTypes.ReadNextVarLong(packetData);
                                var blockId = (int)(chunkSectionPosition >> 12);
                                var localX = (int)(chunkSectionPosition >> 8 & 0x0F);
                                var localZ = (int)(chunkSectionPosition >> 4 & 0x0F);
                                var localY = (int)(chunkSectionPosition & 0x0F);

                                var block = new Block((ushort)blockId);
                                var blockX = sectionX * 16 + localX;
                                var blockY = sectionY * 16 + localY;
                                var blockZ = sectionZ * 16 + localZ;

                                Location location = new(blockX, blockY, blockZ);

                                handler.OnBlockChange(location, block);
                            }
                        }
                        else
                        {
                            var chunkX = dataTypes.ReadNextInt(packetData);
                            var chunkZ = dataTypes.ReadNextInt(packetData);
                            var recordCount = protocolVersion < MC_1_8_Version
                                ? dataTypes.ReadNextShort(packetData)
                                : dataTypes.ReadNextVarInt(packetData);

                            for (var i = 0; i < recordCount; i++)
                            {
                                byte locationXZ;
                                ushort blockIdMeta;
                                int blockY;

                                if (protocolVersion < MC_1_8_Version)
                                {
                                    blockIdMeta = dataTypes.ReadNextUShort(packetData);
                                    blockY = dataTypes.ReadNextByte(packetData);
                                    locationXZ = dataTypes.ReadNextByte(packetData);
                                }
                                else
                                {
                                    locationXZ = dataTypes.ReadNextByte(packetData);
                                    blockY = dataTypes.ReadNextByte(packetData);
                                    blockIdMeta = (ushort)dataTypes.ReadNextVarInt(packetData);
                                }

                                var blockX = locationXZ >> 4;
                                var blockZ = locationXZ & 0x0F;

                                Location location = new(chunkX, chunkZ, blockX, blockY, blockZ);
                                Block block = new(blockIdMeta);
                                handler.OnBlockChange(location, block);
                            }
                        }
                    }

                    break;
                case PacketTypesIn.ServerData:
                    var motd = "-";

                    var hasMotd = false;
                    if (protocolVersion < MC_1_19_4_Version)
                    {
                        hasMotd = dataTypes.ReadNextBool(packetData);

                        if (hasMotd)
                            motd = ChatParser.ParseText(dataTypes.ReadNextChat(packetData));
                    }
                    else
                    {
                        hasMotd = true;
                        motd = ChatParser.ParseText(dataTypes.ReadNextChat(packetData));
                    }

                    var iconBase64 = "-";
                    var hasIcon = dataTypes.ReadNextBool(packetData);
                    if (hasIcon)
                    {
                        if (protocolVersion < MC_1_20_2_Version)
                            iconBase64 = dataTypes.ReadNextString(packetData);
                        else
                        {
                            var pngData = dataTypes.ReadNextByteArray(packetData);
                            iconBase64 = Convert.ToBase64String(pngData);
                        }
                    }

                    var previewsChat = false;
                    if (protocolVersion < MC_1_19_3_Version)
                        previewsChat = dataTypes.ReadNextBool(packetData);

                    handler.OnServerDataRecived(hasMotd, motd, hasIcon, iconBase64, previewsChat);
                    break;
                case PacketTypesIn.BlockChange:
                    if (handler.GetTerrainEnabled())
                    {
                        if (protocolVersion < MC_1_8_Version)
                        {
                            var blockX = dataTypes.ReadNextInt(packetData);
                            var blockY = dataTypes.ReadNextByte(packetData);
                            var blockZ = dataTypes.ReadNextInt(packetData);
                            var blockId = (short)dataTypes.ReadNextVarInt(packetData);
                            var blockMeta = dataTypes.ReadNextByte(packetData);

                            Location location = new(blockX, blockY, blockZ);
                            Block block = new(blockId, blockMeta);
                            handler.OnBlockChange(location, block);
                        }
                        else
                        {
                            var location = dataTypes.ReadNextLocation(packetData);
                            Block block = new((ushort)dataTypes.ReadNextVarInt(packetData));
                            handler.OnBlockChange(location, block);
                        }
                    }

                    break;
                case PacketTypesIn.SetDisplayChatPreview:
                    handler.OnChatPreviewSettingUpdate(dataTypes.ReadNextBool(packetData)); // Preview Chat Settings
                    break;
                case PacketTypesIn.ChatSuggestions:
                    break;
                case PacketTypesIn.MapChunkBulk:
                    if (protocolVersion < MC_1_9_Version && handler.GetTerrainEnabled())
                    {
                        int chunkCount;
                        bool hasSkyLight;
                        var chunkData = packetData;

                        //Read global fields
                        if (protocolVersion < MC_1_8_Version)
                        {
                            chunkCount = dataTypes.ReadNextShort(packetData);
                            var compressedDataSize = dataTypes.ReadNextInt(packetData);
                            hasSkyLight = dataTypes.ReadNextBool(packetData);
                            var compressed = dataTypes.ReadData(compressedDataSize, packetData);
                            var decompressed = ZlibUtils.Decompress(compressed);
                            chunkData = new Queue<byte>(decompressed);
                        }
                        else
                        {
                            hasSkyLight = dataTypes.ReadNextBool(packetData);
                            chunkCount = dataTypes.ReadNextVarInt(packetData);
                        }

                        //Read chunk records
                        var chunkXs = new int[chunkCount];
                        var chunkZs = new int[chunkCount];
                        var chunkMasks = new ushort[chunkCount];
                        var addBitmaps = new ushort[chunkCount];
                        for (var chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                        {
                            chunkXs[chunkColumnNo] = dataTypes.ReadNextInt(packetData);
                            chunkZs[chunkColumnNo] = dataTypes.ReadNextInt(packetData);
                            chunkMasks[chunkColumnNo] = dataTypes.ReadNextUShort(packetData);
                            addBitmaps[chunkColumnNo] = protocolVersion < MC_1_8_Version
                                ? dataTypes.ReadNextUShort(packetData)
                                : (ushort)0;
                        }

                        //Process chunk records
                        for (var chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                        {
                            pTerrain.ProcessChunkColumnData(chunkXs[chunkColumnNo], chunkZs[chunkColumnNo],
                                chunkMasks[chunkColumnNo], addBitmaps[chunkColumnNo], hasSkyLight, true,
                                currentDimension, chunkData);
                            Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);
                        }
                    }

                    break;
                case PacketTypesIn.UnloadChunk:
                    if (protocolVersion >= MC_1_9_Version && handler.GetTerrainEnabled())
                    {
                        var chunkX = dataTypes.ReadNextInt(packetData);
                        var chunkZ = dataTypes.ReadNextInt(packetData);

                        // Warning: It is legal to include unloaded chunks in the UnloadChunk packet.
                        // Since chunks that have not been loaded are not recorded, this may result
                        // in loading chunks that should be unloaded and inaccurate statistics.
                        if (handler.GetWorld()[chunkX, chunkZ] is not null)
                            Interlocked.Decrement(ref handler.GetWorld().chunkCnt);

                        handler.GetWorld()[chunkX, chunkZ] = null;
                    }

                    break;
                case PacketTypesIn.ChangeGameState:
                    if (protocolVersion >= MC_1_15_2_Version)
                    {
                        var reason = dataTypes.ReadNextByte(packetData);
                        var state = dataTypes.ReadNextFloat(packetData);
                        handler.OnGameEvent(reason, state);
                    }

                    break;
                case PacketTypesIn.PlayerInfo:
                    if (protocolVersion >= MC_1_19_3_Version)
                    {
                        var actionBitset = dataTypes.ReadNextByte(packetData);
                        var entryCount = dataTypes.ReadNextVarInt(packetData);
                        for (var i = 0; i < entryCount; i++)
                        {
                            var playerUuid = dataTypes.ReadNextUUID(packetData);

                            PlayerInfo player;
                            if ((actionBitset & 1 << 0) > 0) // Actions bit 0: add player
                            {
                                var name = dataTypes.ReadNextString(packetData);
                                var numberOfProperties = dataTypes.ReadNextVarInt(packetData);
                                for (var j = 0; j < numberOfProperties; ++j)
                                {
                                    dataTypes.SkipNextString(packetData);
                                    dataTypes.SkipNextString(packetData);
                                    if (dataTypes.ReadNextBool(packetData))
                                        dataTypes.SkipNextString(packetData);
                                }

                                player = new(name, playerUuid);
                                handler.OnPlayerJoin(player);
                            }
                            else
                            {
                                var playerGet = handler.GetPlayerInfo(playerUuid);
                                if (playerGet is null)
                                {
                                    player = new(string.Empty, playerUuid);
                                    handler.OnPlayerJoin(player);
                                }
                                else
                                {
                                    player = playerGet;
                                }
                            }

                            if ((actionBitset & 1 << 1) > 0) // Actions bit 1: initialize chat
                            {
                                var hasSignatureData = dataTypes.ReadNextBool(packetData);

                                if (hasSignatureData)
                                {
                                    var chatUuid = dataTypes.ReadNextUUID(packetData);
                                    var publicKeyExpiryTime = dataTypes.ReadNextLong(packetData);
                                    var encodedPublicKey = dataTypes.ReadNextByteArray(packetData);
                                    var publicKeySignature = dataTypes.ReadNextByteArray(packetData);
                                    player.SetPublicKey(chatUuid, publicKeyExpiryTime, encodedPublicKey,
                                        publicKeySignature);

                                    if (playerUuid == handler.GetUserUuid())
                                    {
                                        log.Debug($"Receive ChatUuid = {chatUuid}");
                                        this.chatUuid = chatUuid;
                                    }
                                }
                                else
                                {
                                    player.ClearPublicKey();

                                    if (playerUuid == handler.GetUserUuid())
                                        log.Debug("Receive ChatUuid = Empty");
                                }

                                if (playerUuid == handler.GetUserUuid())
                                {
                                    receivePlayerInfo = true;
                                    if (receiveDeclareCommands)
                                        handler.SetCanSendMessage(true);
                                }
                            }

                            if ((actionBitset & 1 << 2) > 0) // Actions bit 2: update gamemode
                                handler.OnGamemodeUpdate(playerUuid, dataTypes.ReadNextVarInt(packetData));

                            if ((actionBitset & 1 << 3) > 0) // Actions bit 3: update listed
                                player.Listed = dataTypes.ReadNextBool(packetData);

                            if ((actionBitset & 1 << 4) > 0) // Actions bit 4: update latency
                            {
                                var latency = dataTypes.ReadNextVarInt(packetData);
                                handler.OnLatencyUpdate(playerUuid, latency); //Update latency;
                            }

                            // Actions bit 5: update display name
                            if ((actionBitset & 1 << 5) > 0)
                            {
                                player.DisplayName = dataTypes.ReadNextBool(packetData)
                                    ? dataTypes.ReadNextChat(packetData)
                                    : null;
                            }

                            // Consume all action-selected fields to keep entry boundaries aligned.
                            if (protocolVersion >= MC_1_21_2_Version && (actionBitset & 1 << 6) > 0) // Actions bit 6: update list order
                                player.TabListOrder = dataTypes.ReadNextVarInt(packetData);

                            if (protocolVersion >= MC_1_21_4_Version && (actionBitset & 1 << 7) > 0) // Actions bit 7: update hat
                                dataTypes.ReadNextBool(packetData);
                        }
                    }
                    else if (protocolVersion >= MC_1_8_Version)
                    {
                        var action = dataTypes.ReadNextVarInt(packetData); // Action Name
                        var numberOfPlayers = dataTypes.ReadNextVarInt(packetData); // Number Of Players 

                        for (var i = 0; i < numberOfPlayers; i++)
                        {
                            var uuid = dataTypes.ReadNextUUID(packetData); // Player UUID

                            switch (action)
                            {
                                case 0x00: //Player Join (Add player since 1.19)
                                    var name = dataTypes.ReadNextString(packetData); // Player name
                                    var propNum =
                                        dataTypes.ReadNextVarInt(
                                            packetData); // Number of properties in the following array

                                    // Property: Tuple<Name, Value, Signature(empty if there is no signature)
                                    // The Property field looks as in the response of https://wiki.vg/Mojang_API#UUID_to_Profile_and_Skin.2FCape
                                    const bool useProperty = false;
#pragma warning disable CS0162 // Unreachable code detected
                                    var properties =
                                        useProperty ? new Tuple<string, string, string?>[propNum] : null;
                                    for (var p = 0; p < propNum; p++)
                                    {
                                        var propertyName =
                                            dataTypes.ReadNextString(packetData); // Name: String (32767)
                                        var val =
                                            dataTypes.ReadNextString(packetData); // Value: String (32767)
                                        string? propertySignature = null;
                                        if (dataTypes.ReadNextBool(packetData)) // Is Signed
                                            propertySignature =
                                                dataTypes.ReadNextString(
                                                    packetData); // Signature: String (32767)
                                        if (useProperty)
                                            properties![p] = new(propertyName, val, propertySignature);
                                    }
#pragma warning restore CS0162 // Unreachable code detected

                                    var gameMode = dataTypes.ReadNextVarInt(packetData); // Gamemode
                                    handler.OnGamemodeUpdate(uuid, gameMode);

                                    var ping = dataTypes.ReadNextVarInt(packetData); // Ping

                                    string? displayName = null;
                                    if (dataTypes.ReadNextBool(packetData)) // Has display name
                                        displayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData)); // Display name

                                    // 1.19 Additions
                                    long? keyExpiration = null;
                                    byte[]? publicKey = null, signature = null;
                                    if (protocolVersion >= MC_1_19_Version)
                                    {
                                        if (dataTypes.ReadNextBool(
                                                packetData)) // Has Sig Data (if true, red the following fields)
                                        {
                                            keyExpiration = dataTypes.ReadNextLong(packetData); // Timestamp

                                            var publicKeyLength =
                                                dataTypes.ReadNextVarInt(packetData); // Public Key Length 
                                            if (publicKeyLength > 0)
                                                publicKey = dataTypes.ReadData(publicKeyLength,
                                                    packetData); // Public key

                                            var signatureLength =
                                                dataTypes.ReadNextVarInt(packetData); // Signature Length 
                                            if (signatureLength > 0)
                                                signature = dataTypes.ReadData(signatureLength,
                                                    packetData); // Public key
                                        }
                                    }

                                    handler.OnPlayerJoin(new PlayerInfo(uuid, name, properties, gameMode, ping,
                                        displayName, keyExpiration, publicKey, signature));
                                    break;
                                case 0x01: // Update Gamemode
                                    handler.OnGamemodeUpdate(uuid, dataTypes.ReadNextVarInt(packetData));
                                    break;
                                case 0x02: // Update latency
                                    var latency = dataTypes.ReadNextVarInt(packetData);
                                    handler.OnLatencyUpdate(uuid, latency); // Update latency;
                                    break;
                                case 0x03: // Update display name
                                    if (dataTypes.ReadNextBool(packetData))
                                    {
                                        var player = handler.GetPlayerInfo(uuid);
                                        if (player is not null)
                                            player.DisplayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                        else
                                            dataTypes.SkipNextString(packetData);
                                    }

                                    break;
                                case 0x04: // Player Leave
                                    handler.OnPlayerLeave(uuid);
                                    break;
                                default:
                                    // Unknown player list item type
                                    break;
                            }
                        }
                    }
                    else // MC 1.7.X does not provide UUID in tab-list updates
                    {
                        var name = dataTypes.ReadNextString(packetData);
                        var online = dataTypes.ReadNextBool(packetData);
                        var ping = dataTypes.ReadNextShort(packetData);
                        var fakeUuid = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16)
                            .ToArray());
                        if (online)
                            handler.OnPlayerJoin(new PlayerInfo(name, fakeUuid));
                        else handler.OnPlayerLeave(fakeUuid);
                    }

                    break;
                case PacketTypesIn.PlayerRemove:
                    var numberOfLeavePlayers = dataTypes.ReadNextVarInt(packetData);
                    for (var i = 0; i < numberOfLeavePlayers; ++i)
                    {
                        var playerUuid = dataTypes.ReadNextUUID(packetData);
                        handler.OnPlayerLeave(playerUuid);
                    }

                    break;
                case PacketTypesIn.TabComplete:
                    var oldTransactionId = autocomplete_transaction_id;
                    if (protocolVersion >= MC_1_13_Version)
                    {
                        autocomplete_transaction_id = dataTypes.ReadNextVarInt(packetData);
                        dataTypes.ReadNextVarInt(packetData); // Start of text to replace
                        dataTypes.ReadNextVarInt(packetData); // Length of text to replace
                    }

                    var autocompleteCount = dataTypes.ReadNextVarInt(packetData);
                    var autocompleteResult = new string[autocompleteCount];
                    for (var i = 0; i < autocompleteCount; i++)
                    {
                        autocompleteResult[i] = dataTypes.ReadNextString(packetData);
                        if (protocolVersion < MC_1_13_Version) continue;

                        // Skip optional tooltip for each tab-complete resul`t
                        if (dataTypes.ReadNextBool(packetData))
                            dataTypes.ReadNextChat(packetData);
                    }

                    handler.OnAutoCompleteDone(oldTransactionId, autocompleteResult);
                    break;
                case PacketTypesIn.PluginMessage:
                    var channel = dataTypes.ReadNextString(packetData);
                    // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
                    if (protocolVersion < MC_1_8_Version)
                        pForge.ReadNextVarShort(packetData);
                    handler.OnPluginChannelMessage(channel, packetData.ToArray());
                    return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                case PacketTypesIn.Disconnect:
                    handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick,
                        dataTypes.ReadNextChat(packetData));
                    return false;
                case PacketTypesIn.SetCompression:
                    if (protocolVersion is >= MC_1_8_Version and < MC_1_9_Version)
                        compression_treshold = dataTypes.ReadNextVarInt(packetData);
                    break;
                case PacketTypesIn.OpenWindow:
                    if (handler.GetInventoryEnabled())
                    {
                        if (protocolVersion < MC_1_14_Version)
                        {
                            // MC 1.13 or lower
                            var windowId = dataTypes.ReadNextByte(packetData);
                            var type = dataTypes.ReadNextString(packetData).Replace("minecraft:", "")
                                .ToUpper();
                            var inventoryType =
                                (ContainerTypeOld)Enum.Parse(typeof(ContainerTypeOld), type);
                            var title = dataTypes.ReadNextChat(packetData);
                            var slots = dataTypes.ReadNextByte(packetData);
                            Container inventory = new(windowId, inventoryType, ChatParser.ParseText(title));
                            handler.OnInventoryOpen(windowId, inventory);
                        }
                        else
                        {
                            // MC 1.14 or greater
                            var windowId = dataTypes.ReadNextVarInt(packetData);
                            var windowType = dataTypes.ReadNextVarInt(packetData);
                            var title = dataTypes.ReadNextChat(packetData);
                            Container inventory = new(windowId, windowType, ChatParser.ParseText(title), protocolVersion);
                            handler.OnInventoryOpen(windowId, inventory);
                        }
                    }

                    break;
                case PacketTypesIn.CloseWindow:
                    if (handler.GetInventoryEnabled())
                    {
                        var windowId = protocolVersion >= MC_1_21_2_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextByte(packetData);
                        lock (window_actions)
                        {
                            window_actions[windowId] = 0;
                        }

                        handler.OnInventoryClose(windowId);
                    }

                    break;
                case PacketTypesIn.WindowItems:
                    if (handler.GetInventoryEnabled())
                    {
                        var windowId = (byte)(protocolVersion >= MC_1_21_2_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextByte(packetData));
                        var stateId = -1;
                        int elements;

                        if (protocolVersion >= MC_1_17_1_Version)
                        {
                            // State ID and Elements as VarInt - 1.17.1 and above
                            stateId = dataTypes.ReadNextVarInt(packetData);
                            elements = dataTypes.ReadNextVarInt(packetData);
                        }
                        else
                        {
                            // Elements as Short - 1.17.0 and below
                            elements = dataTypes.ReadNextShort(packetData);
                        }

                        Dictionary<int, Item> inventorySlots = new();
                        for (var slotId = 0; slotId < elements; slotId++)
                        {
                            var item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                            if (item is not null)
                                inventorySlots[slotId] = item;
                        }

                        if (protocolVersion >= MC_1_17_1_Version) // Carried Item - 1.17.1 and above
                            dataTypes.ReadNextItemSlot(packetData, itemPalette);

                        handler.OnWindowItems(windowId, inventorySlots, stateId);
                    }

                    break;
                case PacketTypesIn.WindowProperty:
                    var containerId = (byte)(protocolVersion >= MC_1_21_2_Version
                        ? dataTypes.ReadNextVarInt(packetData)
                        : dataTypes.ReadNextByte(packetData));
                    var propertyId = dataTypes.ReadNextShort(packetData);
                    var propertyValue = dataTypes.ReadNextShort(packetData);
                    handler.OnWindowProperties(containerId, propertyId, propertyValue);
                    break;
                case PacketTypesIn.SetSlot:
                    if (handler.GetInventoryEnabled())
                    {
                        var windowId = (byte)(protocolVersion >= MC_1_21_2_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextByte(packetData));
                        var stateId = -1;
                        if (protocolVersion >= MC_1_17_1_Version)
                            stateId = dataTypes.ReadNextVarInt(packetData); // State ID - 1.17.1 and above
                        var slotId = dataTypes.ReadNextShort(packetData);
                        var item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                        handler.OnSetSlot(windowId, slotId, item, stateId);
                    }

                    break;
                case PacketTypesIn.WindowConfirmation:
                    if (handler.GetInventoryEnabled())
                    {
                        var windowId = dataTypes.ReadNextByte(packetData);
                        var actionId = dataTypes.ReadNextShort(packetData);
                        var accepted = dataTypes.ReadNextBool(packetData);
                        if (!accepted)
                            SendWindowConfirmation(windowId, actionId, true);
                    }

                    break;
                case PacketTypesIn.RemoveResourcePack:
                    if (dataTypes.ReadNextBool(packetData)) // Has UUID
                        dataTypes.ReadNextUUID(packetData); // UUID
                    break;
                case PacketTypesIn.ResourcePackSend:
                    HandleResourcePackPacket(packetData);
                    break;
                case PacketTypesIn.ResetScore:
                    dataTypes.ReadNextString(packetData); // Entity Name
                    if (dataTypes.ReadNextBool(packetData)) // Has Objective Name
                        dataTypes.ReadNextString(packetData); // Objective Name

                    break;
                case PacketTypesIn.SpawnEntity:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entity = dataTypes.ReadNextEntity(packetData, entityPalette, false);

                        if (protocolVersion >= MC_1_20_2_Version)
                        {
                            if (entity.Type == EntityType.Player)
                                handler.OnSpawnPlayer(entity.ID, entity.UUID, entity.Location, (byte)entity.Yaw, (byte)entity.Pitch);
                            else
                                handler.OnSpawnEntity(entity);

                            break;
                        }

                        handler.OnSpawnEntity(entity);
                    }

                    break;
                case PacketTypesIn.EntityEquipment:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        if (protocolVersion >= MC_1_16_Version)
                        {
                            bool hasNext;
                            do
                            {
                                var bitsData = dataTypes.ReadNextByte(packetData);
                                //  Top bit set if another entry follows, and otherwise unset if this is the last item in the array
                                hasNext = bitsData >> 7 == 1;
                                var slot2 = bitsData & 0x7F;
                                var item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                handler.OnEntityEquipment(entityId, slot2, item);
                            } while (hasNext);
                        }
                        else
                        {
                            var slot2 = protocolVersion < MC_1_9_Version
                                ? dataTypes.ReadNextShort(packetData)
                                : dataTypes.ReadNextVarInt(packetData);

                            var item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                            handler.OnEntityEquipment(entityId, slot2, item);
                        }
                    }

                    break;
                case PacketTypesIn.SpawnLivingEntity:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entity = dataTypes.ReadNextEntity(packetData, entityPalette, true);
                        // packet before 1.15 has metadata at the end
                        // this is not handled in dataTypes.ReadNextEntity()
                        // we are simply ignoring leftover data in packet
                        handler.OnSpawnEntity(entity);
                    }

                    break;
                case PacketTypesIn.SpawnPlayer:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        var uuid = dataTypes.ReadNextUUID(packetData);
                        double x, y, z;

                        if (protocolVersion < MC_1_9_Version)
                        {
                            x = dataTypes.ReadNextInt(packetData) / 32.0D;
                            y = dataTypes.ReadNextInt(packetData) / 32.0D;
                            z = dataTypes.ReadNextInt(packetData) / 32.0D;
                        }
                        else
                        {
                            x = dataTypes.ReadNextDouble(packetData);
                            y = dataTypes.ReadNextDouble(packetData);
                            z = dataTypes.ReadNextDouble(packetData);
                        }

                        var yaw = dataTypes.ReadNextByte(packetData);
                        var pitch = dataTypes.ReadNextByte(packetData);
                        handler.OnSpawnPlayer(entityId, uuid, new(x, y, z), yaw, pitch);
                    }

                    break;
                case PacketTypesIn.EntityEffect:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        var effectId = protocolVersion >= MC_1_20_4_Version
                            ? dataTypes.ReadNextVarInt(packetData) + 1
                            : dataTypes.ReadNextByte(packetData);

                        if (Enum.IsDefined(typeof(Effects), effectId))
                        {
                            var effect = (Effects)effectId;
                            var amplifier = dataTypes.ReadNextByte(packetData);
                            var duration = dataTypes.ReadNextVarInt(packetData);
                            var flags = dataTypes.ReadNextByte(packetData);
                            var hasFactorData = false;
                            Dictionary<string, object>? factorCodec = null;

                            if (protocolVersion >= MC_1_19_Version && protocolVersion < MC_1_20_6_Version)
                            {
                                hasFactorData = dataTypes.ReadNextBool(packetData);
                                if (hasFactorData)
                                    factorCodec = dataTypes.ReadNextNbt(packetData);
                            }

                            handler.OnEntityEffect(entityId, effect, amplifier, duration, flags, hasFactorData,
                                factorCodec);
                        }
                    }

                    break;
                case PacketTypesIn.RemoveEntityEffect:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        var effectId = protocolVersion >= MC_1_20_4_Version
                            ? dataTypes.ReadNextVarInt(packetData) + 1
                            : dataTypes.ReadNextByte(packetData);

                        if (Enum.IsDefined(typeof(Effects), effectId))
                        {
                            var effect = (Effects)effectId;
                            handler.OnRemoveEntityEffect(entityId, effect);
                        }
                    }

                    break;
                case PacketTypesIn.DestroyEntities:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityCount = 1; // 1.17.0 has only one entity per packet
                        if (protocolVersion != MC_1_17_Version)
                            entityCount =
                                dataTypes.ReadNextVarInt(packetData); // All other versions have a "count" field

                        var entityList = new int[entityCount];
                        for (var i = 0; i < entityCount; i++)
                            entityList[i] = dataTypes.ReadNextVarInt(packetData);

                        handler.OnDestroyEntities(entityList);
                    }

                    break;
                case PacketTypesIn.EntityPosition:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        double deltaX, deltaY, deltaZ;

                        if (protocolVersion < MC_1_9_Version)
                        {
                            deltaX = Convert.ToDouble(dataTypes.ReadNextByte(packetData));
                            deltaY = Convert.ToDouble(dataTypes.ReadNextByte(packetData));
                            deltaZ = Convert.ToDouble(dataTypes.ReadNextByte(packetData));
                        }
                        else
                        {
                            deltaX = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            deltaY = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            deltaZ = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                        }

                        var isOnGround = dataTypes.ReadNextBool(packetData);
                        deltaX = deltaX / (128 * 32);
                        deltaY = deltaY / (128 * 32);
                        deltaZ = deltaZ / (128 * 32);

                        handler.OnEntityPosition(entityId, deltaX, deltaY, deltaZ, isOnGround);
                    }

                    break;
                case PacketTypesIn.EntityPositionAndRotation:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        double deltaX, deltaY, deltaZ;

                        if (protocolVersion < MC_1_9_Version)
                        {
                            deltaX = dataTypes.ReadNextByte(packetData) / 32.0D;
                            deltaY = dataTypes.ReadNextByte(packetData) / 32.0D;
                            deltaZ = dataTypes.ReadNextByte(packetData) / 32.0D;
                        }
                        else
                        {
                            deltaX = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            deltaY = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            deltaZ = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                        }


                        var yaw = dataTypes.ReadNextByte(packetData) * (1F / 256) * 360;
                        var pitch = dataTypes.ReadNextByte(packetData) * (1F / 256) * 360;
                        var isOnGround = dataTypes.ReadNextBool(packetData);
                        deltaX = deltaX / (128 * 32);
                        deltaY = deltaY / (128 * 32);
                        deltaZ = deltaZ / (128 * 32);

                        handler.OnEntityPosition(entityId, deltaX, deltaY, deltaZ, yaw, pitch, isOnGround);
                    }

                    break;
                case PacketTypesIn.EntityRotation:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        var yaw = dataTypes.ReadNextByte(packetData) * (1F / 256) * 360;
                        var pitch = dataTypes.ReadNextByte(packetData) * (1F / 256) * 360;
                        var isOnGround = dataTypes.ReadNextBool(packetData);

                        handler.OnEntityRotation(entityId, yaw, pitch, isOnGround);
                    }

                    break;
                case PacketTypesIn.EntityVelocity:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        double velocityX, velocityY, velocityZ;

                        if (protocolVersion >= MC_1_21_9_Version)
                        {
                            (velocityX, velocityY, velocityZ) = dataTypes.ReadNextLpVec3Values(packetData);
                        }
                        else
                        {
                            velocityX = dataTypes.ReadNextShort(packetData) / 8000.0D;
                            velocityY = dataTypes.ReadNextShort(packetData) / 8000.0D;
                            velocityZ = dataTypes.ReadNextShort(packetData) / 8000.0D;
                        }

                        handler.OnEntityVelocity(entityId, velocityX, velocityY, velocityZ);
                    }

                    break;
                case PacketTypesIn.EntityProperties:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        var numberOfProperties = protocolVersion >= MC_1_17_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextInt(packetData);

                        Dictionary<string, double> keys = new();
                        for (var i = 0; i < numberOfProperties; i++)
                        {
                            string propertyKey;
                            if (protocolVersion < MC_1_20_6_Version)
                            {
                                propertyKey = dataTypes.ReadNextString(packetData);
                            }
                            else
                            {
                                var attrId = dataTypes.ReadNextVarInt(packetData);
                                propertyKey = World.GetAttributeNameById(attrId) ?? "unknown";
                            }
                            var propertyValue2 = dataTypes.ReadNextDouble(packetData);

                            List<double> op0 = new();
                            List<double> op1 = new();
                            List<double> op2 = new();

                            var numberOfModifiers = dataTypes.ReadNextVarInt(packetData);
                            for (var j = 0; j < numberOfModifiers; j++)
                            {
                                var modifierId = protocolVersion < MC_1_21_Version ? dataTypes.ReadNextUUID(packetData).ToString() : dataTypes.ReadNextString(packetData);
                                var amount = dataTypes.ReadNextDouble(packetData);
                                var operation = dataTypes.ReadNextByte(packetData);
                                switch (operation)
                                {
                                    case 0:
                                        op0.Add(amount);
                                        break;
                                    case 1:
                                        op1.Add(amount);
                                        break;
                                    case 2:
                                        op2.Add(amount + 1);
                                        break;
                                }
                            }

                            if (op0.Count > 0) propertyValue2 += op0.Sum();
                            if (op1.Count > 0) propertyValue2 *= 1 + op1.Sum();
                            if (op2.Count > 0) propertyValue2 *= op2.Aggregate((a, _x) => a * _x);
                            keys[propertyKey] = propertyValue2;
                        }

                        handler.OnEntityProperties(entityId, keys);
                    }

                    break;
                case PacketTypesIn.EntityMetadata:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        var metadata = dataTypes.ReadNextMetadata(packetData, itemPalette, entityMetadataPalette);

                        // Also make a palette for field? Will be a lot of work
                        var healthField = protocolVersion switch
                        {
                            > MC_26_1_Version => throw new NotImplementedException(Translations
                                .exception_palette_healthfield),
                            // 1.17 and above
                            >= MC_1_17_Version => 9,
                            // 1.14 and above
                            >= MC_1_14_Version => 8,
                            // 1.10 and above
                            >= MC_1_10_Version => 7,
                            // 1.8 and above
                            >= MC_1_8_Version => 6,
                            _ => throw new NotImplementedException(Translations.exception_palette_healthfield)
                        };

                        if (metadata.TryGetValue(healthField, out var healthObj) && healthObj is float healthObj2)
                            handler.OnEntityHealth(entityId, healthObj2);

                        handler.OnEntityMetadata(entityId, metadata);
                    }

                    break;
                case PacketTypesIn.EntityStatus:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextInt(packetData);
                        var status = dataTypes.ReadNextByte(packetData);
                        handler.OnEntityStatus(entityId, status);
                    }

                    break;
                case PacketTypesIn.TimeUpdate:
                    if (protocolVersion >= MC_26_1_Version)
                    {
                        var worldAge = dataTypes.ReadNextLong(packetData);
                        long timeOfDay = 0;
                        var clockCount = dataTypes.ReadNextVarInt(packetData);
                        for (int i = 0; i < clockCount; i++)
                        {
                            dataTypes.ReadNextVarInt(packetData); // clock holder id
                            var totalTicks = dataTypes.ReadNextVarLong(packetData);
                            dataTypes.ReadNextFloat(packetData); // partialTick
                            dataTypes.ReadNextFloat(packetData); // rate
                            if (i == 0)
                                timeOfDay = totalTicks;
                        }
                        handler.OnTimeUpdate(worldAge, timeOfDay);
                    }
                    else
                    {
                        var worldAge = dataTypes.ReadNextLong(packetData);
                        var timeOfDay = dataTypes.ReadNextLong(packetData);
                        if (protocolVersion >= MC_1_21_2_Version)
                            dataTypes.ReadNextBool(packetData); // Tick day time
                        handler.OnTimeUpdate(worldAge, timeOfDay);
                    }
                    break;
                case PacketTypesIn.EntityTeleport:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        double x, y, z;

                        if (protocolVersion >= MC_1_21_2_Version)
                        {
                            // 1.21.2+: PositionMoveRotation + relative flags
                            x = dataTypes.ReadNextDouble(packetData);
                            y = dataTypes.ReadNextDouble(packetData);
                            z = dataTypes.ReadNextDouble(packetData);
                            dataTypes.ReadNextDouble(packetData); // Delta movement X
                            dataTypes.ReadNextDouble(packetData); // Delta movement Y
                            dataTypes.ReadNextDouble(packetData); // Delta movement Z
                            dataTypes.ReadNextFloat(packetData);  // Yaw
                            dataTypes.ReadNextFloat(packetData);  // Pitch
                            dataTypes.ReadNextInt(packetData);    // Relative flags bitmask
                            var isOnGround = dataTypes.ReadNextBool(packetData);
                            handler.OnEntityTeleport(entityId, x, y, z, isOnGround);
                        }
                        else if (protocolVersion < MC_1_9_Version)
                        {
                            x = dataTypes.ReadNextInt(packetData) / 32.0D;
                            y = dataTypes.ReadNextInt(packetData) / 32.0D;
                            z = dataTypes.ReadNextInt(packetData) / 32.0D;
                            dataTypes.ReadNextByte(packetData); // Yaw
                            dataTypes.ReadNextByte(packetData); // Pitch
                            var isOnGround = dataTypes.ReadNextBool(packetData);
                            handler.OnEntityTeleport(entityId, x, y, z, isOnGround);
                        }
                        else
                        {
                            x = dataTypes.ReadNextDouble(packetData);
                            y = dataTypes.ReadNextDouble(packetData);
                            z = dataTypes.ReadNextDouble(packetData);
                            dataTypes.ReadNextByte(packetData); // Yaw
                            dataTypes.ReadNextByte(packetData); // Pitch
                            var isOnGround = dataTypes.ReadNextBool(packetData);
                            handler.OnEntityTeleport(entityId, x, y, z, isOnGround);
                        }
                    }

                    break;
                case PacketTypesIn.UpdateHealth:
                    var health = dataTypes.ReadNextFloat(packetData);
                    var food = protocolVersion >= MC_1_8_Version
                        ? dataTypes.ReadNextVarInt(packetData)
                        : dataTypes.ReadNextShort(packetData);
                    dataTypes.ReadNextFloat(packetData); // Food Saturation
                    handler.OnUpdateHealth(health, food);
                    break;
                case PacketTypesIn.SetExperience:
                    var experienceBar = dataTypes.ReadNextFloat(packetData);
                    var level = dataTypes.ReadNextVarInt(packetData);
                    var totalExperience = dataTypes.ReadNextVarInt(packetData);
                    handler.OnSetExperience(experienceBar, level, totalExperience);
                    break;
                case PacketTypesIn.Explosion:
                    Location explosionLocation;
                    float explosionStrength;
                    int explosionBlockCount;

                    if (protocolVersion >= MC_1_21_2_Version)
                    {
                        explosionLocation = new(dataTypes.ReadNextDouble(packetData),
                            dataTypes.ReadNextDouble(packetData), dataTypes.ReadNextDouble(packetData));

                        if (protocolVersion >= MC_1_21_9_Version)
                        {
                            // 1.21.9+: added radius (float), blockCount (int), and blockParticles (WeightedList)
                            explosionStrength = dataTypes.ReadNextFloat(packetData);   // Radius
                            explosionBlockCount = dataTypes.ReadNextInt(packetData);   // Block count
                        }
                        else
                        {
                            // 1.21.2–1.21.8: no strength/block-count fields
                            explosionStrength = 0;
                            explosionBlockCount = 0;
                        }

                        if (dataTypes.ReadNextBool(packetData)) // Has player knockback
                        {
                            dataTypes.ReadNextDouble(packetData); // Knockback X
                            dataTypes.ReadNextDouble(packetData); // Knockback Y
                            dataTypes.ReadNextDouble(packetData); // Knockback Z
                        }

                        dataTypes.ReadParticleData(packetData, itemPalette); // Explosion particle

                        var soundHolderId = dataTypes.ReadNextVarInt(packetData);
                        if (soundHolderId == 0)
                        {
                            dataTypes.ReadNextString(packetData); // Sound ResourceLocation
                            if (dataTypes.ReadNextBool(packetData))
                                dataTypes.ReadNextFloat(packetData); // Fixed range
                        }

                        if (protocolVersion >= MC_1_21_9_Version)
                        {
                            // 1.21.9+: WeightedList<ExplosionParticleInfo> blockParticles
                            // Each entry: particle + float scaling + float speed + VarInt weight
                            var blockParticleCount = dataTypes.ReadNextVarInt(packetData);
                            for (var i = 0; i < blockParticleCount; i++)
                            {
                                dataTypes.ReadParticleData(packetData, itemPalette); // Particle type
                                dataTypes.ReadNextFloat(packetData); // Scaling
                                dataTypes.ReadNextFloat(packetData); // Speed
                                dataTypes.ReadNextVarInt(packetData); // Weight
                            }
                        }
                    }
                    else
                    {
                        if (protocolVersion >= MC_1_19_3_Version)
                            explosionLocation = new(dataTypes.ReadNextDouble(packetData),
                                dataTypes.ReadNextDouble(packetData), dataTypes.ReadNextDouble(packetData));
                        else
                            explosionLocation = new(dataTypes.ReadNextFloat(packetData),
                                dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData));

                        explosionStrength = dataTypes.ReadNextFloat(packetData);
                        explosionBlockCount = protocolVersion >= MC_1_17_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextInt(packetData);

                        for (var i = 0; i < explosionBlockCount; i++)
                            dataTypes.ReadNextByteArray(packetData, 3);

                        dataTypes.ReadNextFloat(packetData); // Player Motion X
                        dataTypes.ReadNextFloat(packetData); // Player Motion Y
                        dataTypes.ReadNextFloat(packetData); // Player Motion Z

                        if (protocolVersion >= MC_1_20_4_Version)
                        {
                            dataTypes.ReadNextVarInt(packetData); // Block Interaction
                            dataTypes.ReadParticleData(packetData, itemPalette); // Small Explosion Particles
                            dataTypes.ReadParticleData(packetData, itemPalette); // Large Explosion Particles

                            var soundHolderId = dataTypes.ReadNextVarInt(packetData);
                            if (soundHolderId == 0)
                            {
                                dataTypes.ReadNextString(packetData); // Sound ResourceLocation
                                if (dataTypes.ReadNextBool(packetData))
                                    dataTypes.ReadNextFloat(packetData); // Fixed range
                            }
                        }
                    }

                    handler.OnExplosion(explosionLocation, explosionStrength, explosionBlockCount);
                    break;
                case PacketTypesIn.NamedSoundEffect:
                {
                    string? soundName = dataTypes.ReadNextString(packetData);
                    int category = dataTypes.ReadNextVarInt(packetData);
                    double x = dataTypes.ReadNextInt(packetData) / 8.0D;
                    double y = dataTypes.ReadNextInt(packetData) / 8.0D;
                    double z = dataTypes.ReadNextInt(packetData) / 8.0D;
                    float volume = dataTypes.ReadNextFloat(packetData);
                    float pitch = dataTypes.ReadNextFloat(packetData);

                    handler.OnSoundEffect(soundName, new Location(x, y, z), category, volume, pitch, null);
                    break;
                }
                case PacketTypesIn.SoundEffect:
                {
                    string? soundName;
                    if (protocolVersion >= MC_1_19_Version)
                        soundName = ReadSoundEventHolderName(packetData);
                    else
                    {
                        dataTypes.ReadNextVarInt(packetData); // Sound id
                        soundName = null;
                    }

                    int category = dataTypes.ReadNextVarInt(packetData);
                    double x = dataTypes.ReadNextInt(packetData) / 8.0D;
                    double y = dataTypes.ReadNextInt(packetData) / 8.0D;
                    double z = dataTypes.ReadNextInt(packetData) / 8.0D;
                    float volume = dataTypes.ReadNextFloat(packetData);
                    float pitch = dataTypes.ReadNextFloat(packetData);

                    if (protocolVersion >= MC_1_19_Version)
                        dataTypes.ReadNextLong(packetData); // Seed

                    handler.OnSoundEffect(soundName, new Location(x, y, z), category, volume, pitch, null);
                    break;
                }
                case PacketTypesIn.EntitySoundEffect:
                {
                    string? soundName;
                    if (protocolVersion >= MC_1_19_Version)
                        soundName = ReadSoundEventHolderName(packetData);
                    else
                    {
                        dataTypes.ReadNextVarInt(packetData); // Sound id
                        soundName = null;
                    }

                    int category = dataTypes.ReadNextVarInt(packetData);
                    int entityId = dataTypes.ReadNextVarInt(packetData);
                    float volume = dataTypes.ReadNextFloat(packetData);
                    float pitch = dataTypes.ReadNextFloat(packetData);

                    if (protocolVersion >= MC_1_19_Version)
                        dataTypes.ReadNextLong(packetData); // Seed

                    handler.OnSoundEffect(soundName, null, category, volume, pitch, entityId);
                    break;
                }
                case PacketTypesIn.HeldItemChange:
                case PacketTypesIn.SetHeldSlot:
                    handler.OnHeldItemChange(dataTypes.ReadNextByte(packetData)); // Slot
                    break;
                case PacketTypesIn.ScoreboardObjective:
                    var objectiveName = dataTypes.ReadNextString(packetData);
                    var mode = dataTypes.ReadNextByte(packetData);

                    var objectiveValue = string.Empty;
                    var objectiveType = -1;
                    var numberFormat = 0;

                    if (mode is 0 or 2)
                    {
                        objectiveValue = dataTypes.ReadNextChat(packetData);
                        objectiveType = dataTypes.ReadNextVarInt(packetData);

                        if (protocolVersion >= MC_1_20_4_Version)
                        {
                            if (dataTypes.ReadNextBool(packetData)) // Has Number Format
                                numberFormat = dataTypes.ReadNextVarInt(packetData); // Number Format
                        }
                    }

                    handler.OnScoreboardObjective(objectiveName, mode, objectiveValue, objectiveType, numberFormat);
                    break;
                case PacketTypesIn.UpdateScore:
                    var entityName = dataTypes.ReadNextString(packetData);

                    var action3 = 0;
                    var objectiveName3 = string.Empty;
                    var objectiveValue2 = -1;
                    var objectiveDisplayName3 = string.Empty;
                    var numberFormat2 = 0;

                    if (protocolVersion >= MC_1_20_4_Version)
                    {
                        objectiveName3 = dataTypes.ReadNextString(packetData); // Objective Name
                        objectiveValue2 = dataTypes.ReadNextVarInt(packetData); // Value

                        if (dataTypes.ReadNextBool(packetData)) // Has Display Name
                            objectiveDisplayName3 =
                                ChatParser.ParseText(dataTypes.ReadNextString(packetData)); // Has Display Name

                        if (dataTypes.ReadNextBool(packetData)) // Has Number Format
                            numberFormat2 = dataTypes.ReadNextVarInt(packetData); // Number Format
                    }
                    else
                    {
                        action3 = protocolVersion >= MC_1_18_2_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextByte(packetData);

                        if (action3 != 1 || protocolVersion >= MC_1_8_Version)
                            objectiveName3 = dataTypes.ReadNextString(packetData);

                        if (action3 != 1)
                            objectiveValue2 = dataTypes.ReadNextVarInt(packetData);
                    }

                    handler.OnUpdateScore(entityName, action3, objectiveName3, objectiveDisplayName3, objectiveValue2,
                        numberFormat2);
                    break;
                case PacketTypesIn.Teams:
                    // Wire format per version:
                    //   All versions: name (string), method (byte)
                    //   1.8/1.8.9 method 0/2:
                    //                 displayName (string), prefix (string), suffix (string),
                    //                 options (byte), nameTagVisibility, color (byte)
                    //   1.9-1.12.2 method 0/2:
                    //                 displayName (string), prefix (string), suffix (string),
                    //                 options (byte), nameTagVisibility, collisionRule,
                    //                 color (byte)
                    //   1.13-1.21.4 method 0/2:
                    //                 displayName (component), options (byte),
                    //                 nameTagVisibility, collisionRule, color (VarInt),
                    //                 prefix (component), suffix (component)
                    //   1.21.5+ method 0/2:
                    //                 displayName (component), options (byte),
                    //                 nameTagVisibility (VarInt), collisionRule (VarInt),
                    //                 color (VarInt), prefix (component), suffix (component)
                    //   method 0/3/4: players list (VarInt count + strings)
                    var teamName = dataTypes.ReadNextString(packetData);
                    var teamMethod = dataTypes.ReadNextByte(packetData);

                    var teamDisplayName = string.Empty;
                    byte teamFriendlyFlags = 0;
                    var teamNameTagVisibility = string.Empty;
                    var teamCollisionRule = string.Empty;
                    var teamColor = -1;
                    var teamPrefix = string.Empty;
                    var teamSuffix = string.Empty;

                    if (teamMethod is 0 or 2)
                    {
                        if (protocolVersion < MC_1_13_Version)
                        {
                            teamDisplayName = dataTypes.ReadNextString(packetData);
                            teamPrefix = dataTypes.ReadNextString(packetData);
                            teamSuffix = dataTypes.ReadNextString(packetData);
                            teamFriendlyFlags = dataTypes.ReadNextByte(packetData);
                            teamNameTagVisibility = dataTypes.ReadNextString(packetData);

                            if (protocolVersion >= MC_1_9_Version)
                                teamCollisionRule = dataTypes.ReadNextString(packetData);

                            teamColor = unchecked((sbyte)dataTypes.ReadNextByte(packetData));
                        }
                        else
                        {
                            teamDisplayName = dataTypes.ReadNextChat(packetData);
                            teamFriendlyFlags = dataTypes.ReadNextByte(packetData);

                            // nameTagVisibility
                            if (protocolVersion >= MC_1_21_5_Version)
                            {
                                // STREAM_CODEC: 0=always, 1=never, 2=hideForOtherTeams, 3=hideForOwnTeam
                                teamNameTagVisibility = dataTypes.ReadNextVarInt(packetData) switch
                                {
                                    0 => "always",
                                    1 => "never",
                                    2 => "hideForOtherTeams",
                                    3 => "hideForOwnTeam",
                                    _ => "always"
                                };
                            }
                            else
                            {
                                teamNameTagVisibility = dataTypes.ReadNextString(packetData);
                            }

                            // collisionRule
                            if (protocolVersion >= MC_1_21_5_Version)
                            {
                                // STREAM_CODEC: 0=always, 1=never, 2=pushOtherTeams, 3=pushOwnTeam
                                teamCollisionRule = dataTypes.ReadNextVarInt(packetData) switch
                                {
                                    0 => "always",
                                    1 => "never",
                                    2 => "pushOtherTeams",
                                    3 => "pushOwnTeam",
                                    _ => "always"
                                };
                            }
                            else
                            {
                                teamCollisionRule = dataTypes.ReadNextString(packetData);
                            }

                            teamColor = dataTypes.ReadNextVarInt(packetData);
                            teamPrefix = dataTypes.ReadNextChat(packetData);
                            teamSuffix = dataTypes.ReadNextChat(packetData);
                        }
                    }

                    var teamPlayers = new List<string>();
                    if (teamMethod is 0 or 3 or 4)
                    {
                        int playerCount = dataTypes.ReadNextVarInt(packetData);
                        for (int i = 0; i < playerCount; i++)
                            teamPlayers.Add(dataTypes.ReadNextString(packetData));
                    }

                    handler.OnTeam(teamName, teamMethod, teamDisplayName, teamFriendlyFlags,
                        teamNameTagVisibility, teamCollisionRule, teamColor,
                        teamPrefix, teamSuffix, teamPlayers);
                    break;
                case PacketTypesIn.BlockChangedAck:
                    handler.OnBlockChangeAck(dataTypes.ReadNextVarInt(packetData));
                    break;
                case PacketTypesIn.BlockBreakAnimation:
                    if (handler.GetEntityHandlingEnabled() && handler.GetTerrainEnabled())
                    {
                        var playerId = dataTypes.ReadNextVarInt(packetData);
                        var blockLocation = dataTypes.ReadNextLocation(packetData);
                        var stage = dataTypes.ReadNextByte(packetData);
                        handler.OnBlockBreakAnimation(playerId, blockLocation, stage);
                    }

                    break;
                case PacketTypesIn.EntityAnimation:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var playerId = dataTypes.ReadNextVarInt(packetData);
                        var animation = dataTypes.ReadNextByte(packetData);
                        handler.OnEntityAnimation(playerId, animation);
                    }

                    break;

                case PacketTypesIn.OpenSignEditor:
                    var signLocation = dataTypes.ReadNextLocation(packetData);
                    var isFrontText = true;

                    if (protocolVersion >= MC_1_20_Version)
                        isFrontText = dataTypes.ReadNextBool(packetData);

                    // TODO: Use
                    break;
                case PacketTypesIn.BlockEntityData:
                    if (handler.GetTerrainEnabled() && protocolVersion >= MC_1_17_Version)
                    {
                        var location_ = dataTypes.ReadNextLocation(packetData);
                        dataTypes.ReadNextVarInt(packetData); // Block entity type registry id
                        var nbt = dataTypes.ReadNextNbt(packetData);
                        handler.OnBlockEntityData(location_, nbt);
                    }

                    break;

                case PacketTypesIn.SetTickingState:
                    dataTypes.ReadNextFloat(packetData);
                    dataTypes.ReadNextBool(packetData);
                    break;

                case PacketTypesIn.CookieRequest:
                    var cookieName = dataTypes.ReadNextString(packetData);
                    var cookieData = null as byte[];
                    McClient.Instance?.GetCookie(cookieName, out cookieData);
                    SendCookieResponse(cookieName, cookieData);
                    break;

                case PacketTypesIn.StoreCookie:
                    var cookieName2 = dataTypes.ReadNextString(packetData);
                    var cookieData2 = dataTypes.ReadNextByteArray(packetData);
                    McClient.Instance?.SetCookie(cookieName2, cookieData2);
                    break;

                case PacketTypesIn.Transfer:
                    var host = dataTypes.ReadNextString(packetData);
                    var port = dataTypes.ReadNextVarInt(packetData);

                    McClient.Instance?.Transfer(host, port);
                    break;

                case PacketTypesIn.ProjectilePower:
                    dataTypes.ReadNextVarInt(packetData); // Entity ID
                    if (protocolVersion >= MC_1_21_Version)
                    {
                        dataTypes.ReadNextDouble(packetData); // Acceleration Power
                    }
                    else
                    {
                        dataTypes.ReadNextDouble(packetData); // X Power
                        dataTypes.ReadNextDouble(packetData); // Y Power
                        dataTypes.ReadNextDouble(packetData); // Z Power
                    }
                    break;

                case PacketTypesIn.CustomReportDetails:
                    var detailsCount = dataTypes.ReadNextVarInt(packetData);
                    for (var i = 0; i < detailsCount; i++)
                    {
                        dataTypes.ReadNextString(packetData); // Title
                        dataTypes.ReadNextString(packetData); // Description
                    }
                    break;

                case PacketTypesIn.ServerLinks:
                    var linksCount = dataTypes.ReadNextVarInt(packetData);
                    for (var i = 0; i < linksCount; i++)
                    {
                        var isBuiltIn = dataTypes.ReadNextBool(packetData);
                        if (isBuiltIn)
                            dataTypes.ReadNextVarInt(packetData); // Known type ID
                        else
                            dataTypes.ReadNextChat(packetData); // Component label
                        dataTypes.ReadNextString(packetData); // URL
                    }
                    break;

                // 1.21.2+ new packets
                case PacketTypesIn.SetCursorItem:
                    if (handler.GetInventoryEnabled())
                    {
                        dataTypes.ReadNextItemSlot(packetData, itemPalette);
                    }
                    break;

                case PacketTypesIn.SetPlayerInventory:
                    if (handler.GetInventoryEnabled())
                    {
                        var slotId = dataTypes.ReadNextVarInt(packetData);
                        var item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                        handler.OnSetSlot(0, (short)slotId, item, -1);
                    }
                    break;

                case PacketTypesIn.EntityPositionSync:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
                        var x = dataTypes.ReadNextDouble(packetData);
                        var y = dataTypes.ReadNextDouble(packetData);
                        var z = dataTypes.ReadNextDouble(packetData);
                        dataTypes.ReadNextDouble(packetData); // Delta movement X
                        dataTypes.ReadNextDouble(packetData); // Delta movement Y
                        dataTypes.ReadNextDouble(packetData); // Delta movement Z
                        var yaw = dataTypes.ReadNextFloat(packetData);
                        var pitch = dataTypes.ReadNextFloat(packetData);
                        var isOnGround = dataTypes.ReadNextBool(packetData);
                        handler.OnEntityTeleport(entityId, x, y, z, isOnGround);
                    }
                    break;

                case PacketTypesIn.PlayerRotation:
                    dataTypes.ReadNextFloat(packetData); // Yaw
                    dataTypes.ReadNextFloat(packetData); // Pitch
                    break;

                case PacketTypesIn.MoveMinecartAlongTrack:
                    {
                        dataTypes.ReadNextVarInt(packetData); // Entity ID
                        var stepCount = dataTypes.ReadNextVarInt(packetData);
                        for (var i = 0; i < stepCount; i++)
                        {
                            dataTypes.ReadNextDouble(packetData); // Pos X
                            dataTypes.ReadNextDouble(packetData); // Pos Y
                            dataTypes.ReadNextDouble(packetData); // Pos Z
                            dataTypes.ReadNextDouble(packetData); // Movement X
                            dataTypes.ReadNextDouble(packetData); // Movement Y
                            dataTypes.ReadNextDouble(packetData); // Movement Z
                            dataTypes.ReadNextByte(packetData);   // Yaw
                            dataTypes.ReadNextByte(packetData);   // Pitch
                            dataTypes.ReadNextFloat(packetData);  // Weight
                        }
                    }
                    break;

                case PacketTypesIn.UnlockRecipes:
                    if (protocolVersion >= MC_1_13_Version)
                        HandleUnlockRecipes(packetData);
                    break;

                case PacketTypesIn.RecipeBookAdd:
                    if (protocolVersion >= MC_1_21_2_Version)
                        HandleRecipeBookAdd(packetData);
                    break;
                case PacketTypesIn.RecipeBookRemove:
                    if (protocolVersion >= MC_1_21_2_Version)
                        handler.OnRecipeBookRemove(ReadRecipeBookDisplayIds(packetData));
                    break;
                case PacketTypesIn.RecipeBookSettings:
                    break;

                case PacketTypesIn.Statistics:
                    if (protocolVersion < MC_1_12_Version)
                        HandleLegacyStatistics(packetData);
                    break;

                case PacketTypesIn.Advancements:
                    HandleAdvancements(packetData);
                    break;

                case PacketTypesIn.SelectAdvancementTab:
                    HandleSelectAdvancementTab(packetData);
                    break;

                default:
                    return false; //Ignored packet
            }

            return true; //Packet processed
        }

        /// <summary>
        /// Read a Holder&lt;SoundEvent&gt; from packet data and return its key when inline.
        /// Returns null when the holder is a registry reference.
        /// </summary>
        private string? ReadSoundEventHolderName(Queue<byte> packetData)
        {
            int soundHolderId = dataTypes.ReadNextVarInt(packetData);
            if (soundHolderId != 0)
                return null;

            string soundName = dataTypes.ReadNextString(packetData);
            bool hasFixedRange = dataTypes.ReadNextBool(packetData);
            if (hasFixedRange)
                dataTypes.ReadNextFloat(packetData);
            return soundName;
        }

        /// <summary>
        /// Handle the Statistics packet for pre-1.12 legacy achievements.
        /// </summary>
        private void HandleLegacyStatistics(Queue<byte> packetData)
        {
            int statCount = dataTypes.ReadNextVarInt(packetData);

            for (int i = 0; i < statCount; i++)
            {
                string statId = dataTypes.ReadNextString(packetData);
                int value = dataTypes.ReadNextVarInt(packetData);

                if (statId.StartsWith("achievement.", StringComparison.Ordinal))
                    legacyAchievementProgress[statId] = value > 0;
            }

            List<Achievement> added = new(LegacyAchievementCatalog.Ids.Count + legacyAchievementProgress.Count);

            foreach (string achievementId in LegacyAchievementCatalog.Ids)
                added.Add(CreateLegacyAchievement(achievementId, legacyAchievementProgress.TryGetValue(achievementId, out bool completed) && completed));

            foreach (var (achievementId, completed) in legacyAchievementProgress)
            {
                if (!LegacyAchievementCatalog.Contains(achievementId))
                    added.Add(CreateLegacyAchievement(achievementId, completed));
            }

            handler.OnAchievementsUpdate(added, [], reset: !legacyAchievementsInitialized);
            legacyAchievementsInitialized = true;
        }

        /// <summary>
        /// Handle the Advancements packet (1.12+).
        /// </summary>
        private void HandleAdvancements(Queue<byte> packetData)
        {
            bool reset = dataTypes.ReadNextBool(packetData);

            // --- Added advancements ---
            int addedCount = dataTypes.ReadNextVarInt(packetData);
            var added = new List<Achievement>(addedCount);
            var addedDefinitions = new Dictionary<string, (string? title, string? description, AchievementType type, bool isHidden, List<List<string>> requirements)>(addedCount);

            for (int i = 0; i < addedCount; i++)
            {
                string id = dataTypes.ReadNextString(packetData);

                // Parent
                bool hasParent = dataTypes.ReadNextBool(packetData);
                if (hasParent)
                    dataTypes.ReadNextString(packetData); // parentId - read and discard

                // Display
                string? title = null;
                string? description = null;
                var type = AchievementType.Task;
                bool isHidden = false;

                bool hasDisplay = dataTypes.ReadNextBool(packetData);
                if (hasDisplay)
                {
                    title = dataTypes.ReadNextChat(packetData);
                    description = dataTypes.ReadNextChat(packetData);
                    dataTypes.ReadNextItemSlot(packetData, itemPalette); // icon - read and discard

                    int frameType = dataTypes.ReadNextVarInt(packetData);
                    type = frameType switch
                    {
                        1 => AchievementType.Challenge,
                        2 => AchievementType.Goal,
                        _ => AchievementType.Task
                    };

                    int flags = dataTypes.ReadNextInt(packetData);
                    isHidden = (flags & 0x04) != 0;
                    if ((flags & 0x01) != 0)
                        dataTypes.ReadNextString(packetData); // background texture - read and discard

                    dataTypes.ReadNextFloat(packetData); // x
                    dataTypes.ReadNextFloat(packetData); // y
                }

                // Criteria and requirements differ by version
                var requirements = new List<List<string>>();

                if (protocolVersion < MC_1_20_2_Version)
                {
                    // Builder-based (pre-1.20.2): criteria names list, then requirements
                    int criteriaCount = dataTypes.ReadNextVarInt(packetData);
                    for (int c = 0; c < criteriaCount; c++)
                        dataTypes.ReadNextString(packetData); // criterion name only, no trigger data
                }

                // Requirements (all versions)
                int reqGroupCount = dataTypes.ReadNextVarInt(packetData);
                for (int g = 0; g < reqGroupCount; g++)
                {
                    int groupSize = dataTypes.ReadNextVarInt(packetData);
                    var group = new List<string>(groupSize);
                    for (int s = 0; s < groupSize; s++)
                        group.Add(dataTypes.ReadNextString(packetData));
                    requirements.Add(group);
                }

                // sendsTelemetryEvent (added in 1.20, present in all versions since)
                if (protocolVersion >= MC_1_20_Version)
                    dataTypes.ReadNextBool(packetData);

                addedDefinitions[id] = (title, description, type, isHidden, requirements);
            }

            // --- Removed advancement IDs ---
            int removedCount = dataTypes.ReadNextVarInt(packetData);
            var removedIds = new List<string>(removedCount);
            for (int i = 0; i < removedCount; i++)
                removedIds.Add(dataTypes.ReadNextString(packetData));

            // --- Progress updates ---
            int progressCount = dataTypes.ReadNextVarInt(packetData);
            var progressMap = new Dictionary<string, Dictionary<string, bool>>(progressCount);

            for (int i = 0; i < progressCount; i++)
            {
                string id = dataTypes.ReadNextString(packetData);
                int criteriaEntries = dataTypes.ReadNextVarInt(packetData);
                var criteria = new Dictionary<string, bool>(criteriaEntries);

                for (int c = 0; c < criteriaEntries; c++)
                {
                    string criterionName = dataTypes.ReadNextString(packetData);
                    bool isDone = dataTypes.ReadNextBool(packetData);
                    if (isDone)
                        dataTypes.ReadNextLong(packetData); // epochMs - read and discard
                    criteria[criterionName] = isDone;
                }

                progressMap[id] = criteria;
            }

            // showAdvancements boolean added in 1.21.11+
            if (protocolVersion >= MC_1_21_11_Version)
                dataTypes.ReadNextBool(packetData); // showAdvancements - read and discard

            // Build Achievement records from definitions + progress
            foreach (var (id, def) in addedDefinitions)
            {
                progressMap.TryGetValue(id, out var criteria);
                criteria ??= new Dictionary<string, bool>();

                bool isCompleted = ComputeAdvancementCompleted(def.requirements, criteria);

                var readOnlyReqs = def.requirements.ConvertAll<IReadOnlyList<string>>(static g => g.AsReadOnly());
                added.Add(new Achievement(id, def.title, def.description, def.type, def.isHidden, isCompleted, readOnlyReqs.AsReadOnly(), criteria));
            }

            // Also build Achievement records for progress-only updates (no definition change)
            foreach (var (id, criteria) in progressMap)
            {
                if (!addedDefinitions.ContainsKey(id))
                    added.Add(new Achievement(id, null, null, AchievementType.Task, false, false, [], criteria));
            }

            handler.OnAchievementsUpdate(added, removedIds, reset);
        }

        private static Achievement CreateLegacyAchievement(string id, bool isCompleted)
        {
            Dictionary<string, bool> criteria = new(StringComparer.Ordinal)
            {
                [id] = isCompleted
            };
            IReadOnlyList<string>[] requirements = [[id]];
            return new Achievement(id, null, null, AchievementType.Legacy, false, isCompleted, requirements, criteria);
        }

        /// <summary>
        /// Compute whether an advancement is completed based on AND-of-ORs requirements.
        /// </summary>
        private static bool ComputeAdvancementCompleted(List<List<string>> requirements, Dictionary<string, bool> criteria)
        {
            // Zero requirements = automatically done
            if (requirements.Count == 0)
                return true;

            // Each OR-group must have at least one satisfied criterion
            foreach (var group in requirements)
            {
                bool groupSatisfied = false;
                foreach (string criterion in group)
                {
                    if (criteria.TryGetValue(criterion, out bool done) && done)
                    {
                        groupSatisfied = true;
                        break;
                    }
                }
                if (!groupSatisfied)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Handle the SelectAdvancementTab packet.
        /// </summary>
        private void HandleSelectAdvancementTab(Queue<byte> packetData)
        {
            bool hasTab = dataTypes.ReadNextBool(packetData);
            string? tabId = hasTab ? dataTypes.ReadNextString(packetData) : null;
            handler.OnSelectAdvancementTab(tabId);
        }

        private void HandleUnlockRecipes(Queue<byte> packetData)
        {
            int action = dataTypes.ReadNextVarInt(packetData);
            if (!SkipRecipeBookSettings(packetData))
                return;

            string[] recipeIds = ReadRecipeBookRecipeIds(packetData);
            RecipeBookRecipeEntry[] recipeEntries = recipeIds.Select(static recipeId => new RecipeBookRecipeEntry(recipeId, recipeId)).ToArray();

            switch (action)
            {
                case 0:
                    handler.OnRecipeBookAdd(recipeEntries, replace: true);
                    // INIT packets also include a second "to be displayed" recipe list.
                    // MCC only needs the unlocked recipe identifiers for listing/crafting.
                    _ = ReadRecipeBookRecipeIds(packetData);
                    break;
                case 1:
                case 3:
                    // Action 3 is the silent-add variant, so MCC tracks it like a regular add.
                    handler.OnRecipeBookAdd(recipeEntries, replace: false);
                    break;
                case 2:
                    handler.OnRecipeBookRemove(recipeIds);
                    break;
            }
        }

        private void HandleRecipeBookAdd(Queue<byte> packetData)
        {
            int entryCount = dataTypes.ReadNextVarInt(packetData);
            RecipeBookRecipeEntry[] recipeEntries = new RecipeBookRecipeEntry[entryCount];

            // 1.21.2+ RecipeBookAdd contains one display entry per recipe:
            // RecipeDisplayEntry (display id, recipe display, group, category, optional requirements), then flags.
            for (int i = 0; i < entryCount; i++)
            {
                recipeEntries[i] = ReadRecipeBookDisplayEntry(packetData);
                _ = dataTypes.ReadNextByte(packetData); // flags
            }

            bool replace = dataTypes.ReadNextBool(packetData);
            handler.OnRecipeBookAdd(recipeEntries, replace);
        }

        private string[] ReadRecipeBookRecipeIds(Queue<byte> packetData)
        {
            int recipeCount = dataTypes.ReadNextVarInt(packetData);
            string[] recipeIds = new string[recipeCount];

            for (int i = 0; i < recipeCount; i++)
                recipeIds[i] = dataTypes.ReadNextString(packetData);

            return recipeIds;
        }

        private string[] ReadRecipeBookDisplayIds(Queue<byte> packetData)
        {
            int recipeCount = dataTypes.ReadNextVarInt(packetData);
            string[] recipeIds = new string[recipeCount];

            for (int i = 0; i < recipeCount; i++)
                recipeIds[i] = dataTypes.ReadNextVarInt(packetData).ToString(CultureInfo.InvariantCulture);

            return recipeIds;
        }

        private RecipeBookRecipeEntry ReadRecipeBookDisplayEntry(Queue<byte> packetData)
        {
            int displayId = dataTypes.ReadNextVarInt(packetData);
            string resultLabel = ReadRecipeDisplayResultLabel(packetData);

            _ = dataTypes.ReadNextVarInt(packetData); // Optional group, encoded as varint+1 or 0
            _ = dataTypes.ReadNextVarInt(packetData); // Recipe book category registry id
            SkipOptionalCraftingRequirements(packetData);

            string commandId = displayId.ToString(CultureInfo.InvariantCulture);
            string displayText = $"{commandId}: {resultLabel}";
            return new RecipeBookRecipeEntry(commandId, displayText);
        }

        private string ReadRecipeDisplayResultLabel(Queue<byte> packetData)
        {
            int displayType = dataTypes.ReadNextVarInt(packetData);
            return displayType switch
            {
                0 => ReadShapelessRecipeDisplayResultLabel(packetData),
                1 => ReadShapedRecipeDisplayResultLabel(packetData),
                2 => ReadFurnaceRecipeDisplayResultLabel(packetData),
                3 => ReadStonecutterRecipeDisplayResultLabel(packetData),
                4 => ReadSmithingRecipeDisplayResultLabel(packetData),
                _ => $"recipe_display_{displayType}",
            };
        }

        private string ReadShapelessRecipeDisplayResultLabel(Queue<byte> packetData)
        {
            int ingredientCount = dataTypes.ReadNextVarInt(packetData);
            for (int i = 0; i < ingredientCount; i++)
                _ = ReadSlotDisplayLabel(packetData);

            string result = ReadSlotDisplayLabel(packetData);
            _ = ReadSlotDisplayLabel(packetData); // crafting station
            return result;
        }

        private string ReadShapedRecipeDisplayResultLabel(Queue<byte> packetData)
        {
            _ = dataTypes.ReadNextVarInt(packetData); // width
            _ = dataTypes.ReadNextVarInt(packetData); // height
            int ingredientCount = dataTypes.ReadNextVarInt(packetData);
            for (int i = 0; i < ingredientCount; i++)
                _ = ReadSlotDisplayLabel(packetData);

            string result = ReadSlotDisplayLabel(packetData);
            _ = ReadSlotDisplayLabel(packetData); // crafting station
            return result;
        }

        private string ReadFurnaceRecipeDisplayResultLabel(Queue<byte> packetData)
        {
            _ = ReadSlotDisplayLabel(packetData); // ingredient
            _ = ReadSlotDisplayLabel(packetData); // fuel
            string result = ReadSlotDisplayLabel(packetData);
            _ = ReadSlotDisplayLabel(packetData); // crafting station
            _ = dataTypes.ReadNextVarInt(packetData); // duration
            _ = dataTypes.ReadNextFloat(packetData); // experience
            return result;
        }

        private string ReadStonecutterRecipeDisplayResultLabel(Queue<byte> packetData)
        {
            _ = ReadSlotDisplayLabel(packetData); // input
            string result = ReadSlotDisplayLabel(packetData);
            _ = ReadSlotDisplayLabel(packetData); // crafting station
            return result;
        }

        private string ReadSmithingRecipeDisplayResultLabel(Queue<byte> packetData)
        {
            _ = ReadSlotDisplayLabel(packetData); // template
            _ = ReadSlotDisplayLabel(packetData); // base
            _ = ReadSlotDisplayLabel(packetData); // addition
            string result = ReadSlotDisplayLabel(packetData);
            _ = ReadSlotDisplayLabel(packetData); // crafting station
            return result;
        }

        private string ReadSlotDisplayLabel(Queue<byte> packetData)
        {
            int slotDisplayType = dataTypes.ReadNextVarInt(packetData);

            // 26.1 changed the slot display registry order, inserting 3 new types:
            //   Pre-26.1: 0=empty, 1=any_fuel, 2=item, 3=item_stack, 4=tag, 5=smithing_trim, 6=with_remainder, 7=composite
            //   26.1+:    0=empty, 1=any_fuel, 2=with_any_potion, 3=only_with_component, 4=item, 5=item_stack, 6=tag, 7=dyed, 8=smithing_trim, 9=with_remainder, 10=composite
            if (protocolVersion >= MC_26_1_Version)
            {
                return slotDisplayType switch
                {
                    0 => "Empty",
                    1 => "Any Fuel",
                    2 => ReadWithAnyPotionSlotDisplayLabel(packetData),
                    3 => ReadOnlyWithComponentSlotDisplayLabel(packetData),
                    4 => Item.GetTypeString(itemPalette.FromId(dataTypes.ReadNextVarInt(packetData))),
                    5 => ReadItemStackTemplateLabel(packetData),
                    6 => "#" + dataTypes.ReadNextString(packetData),
                    7 => ReadDyedSlotDisplayLabel(packetData),
                    8 => ReadSmithingTrimSlotDisplayLabel(packetData),
                    9 => ReadWithRemainderSlotDisplayLabel(packetData),
                    10 => ReadCompositeSlotDisplayLabel(packetData),
                    _ => $"slot_display_{slotDisplayType}",
                };
            }

            return slotDisplayType switch
            {
                0 => "Empty",
                1 => "Any Fuel",
                2 => Item.GetTypeString(itemPalette.FromId(dataTypes.ReadNextVarInt(packetData))),
                3 => dataTypes.ReadNextItemSlot(packetData, itemPalette)?.GetTypeString() ?? "Empty",
                4 => "#" + dataTypes.ReadNextString(packetData),
                5 => ReadSmithingTrimSlotDisplayLabel(packetData),
                6 => ReadWithRemainderSlotDisplayLabel(packetData),
                7 => ReadCompositeSlotDisplayLabel(packetData),
                _ => $"slot_display_{slotDisplayType}",
            };
        }

        /// <summary>
        /// Reads a with_any_potion slot display (26.1+): contains a nested SlotDisplay.
        /// </summary>
        private string ReadWithAnyPotionSlotDisplayLabel(Queue<byte> packetData)
        {
            return ReadSlotDisplayLabel(packetData);
        }

        /// <summary>
        /// Reads an only_with_component slot display (26.1+): contains a nested SlotDisplay and a DataComponentType VarInt ID.
        /// </summary>
        private string ReadOnlyWithComponentSlotDisplayLabel(Queue<byte> packetData)
        {
            string sourceLabel = ReadSlotDisplayLabel(packetData);
            _ = dataTypes.ReadNextVarInt(packetData); // DataComponentType registry id
            return sourceLabel;
        }

        /// <summary>
        /// Reads a dyed slot display (26.1+): contains two nested SlotDisplays (dye + target).
        /// </summary>
        private string ReadDyedSlotDisplayLabel(Queue<byte> packetData)
        {
            _ = ReadSlotDisplayLabel(packetData); // dye
            string targetLabel = ReadSlotDisplayLabel(packetData); // target
            return targetLabel;
        }

        private string ReadSmithingTrimSlotDisplayLabel(Queue<byte> packetData)
        {
            string baseLabel = ReadSlotDisplayLabel(packetData);
            _ = ReadSlotDisplayLabel(packetData); // material
            _ = dataTypes.ReadNextVarInt(packetData); // trim pattern registry id
            return baseLabel;
        }

        private string ReadWithRemainderSlotDisplayLabel(Queue<byte> packetData)
        {
            string inputLabel = ReadSlotDisplayLabel(packetData);
            _ = ReadSlotDisplayLabel(packetData); // remainder
            return inputLabel;
        }

        private string ReadCompositeSlotDisplayLabel(Queue<byte> packetData)
        {
            int optionCount = dataTypes.ReadNextVarInt(packetData);
            string label = "Composite";

            for (int i = 0; i < optionCount; i++)
            {
                string optionLabel = ReadSlotDisplayLabel(packetData);
                if (label == "Composite" && optionLabel is not "Empty" and not "Composite")
                    label = optionLabel;
            }

            return label;
        }

        /// <summary>
        /// Read an ItemStackTemplate (26.1+) which encodes fields in a different order
        /// than ItemStack: item_id (VarInt), count (VarInt), DataComponentPatch.
        /// </summary>
        private string ReadItemStackTemplateLabel(Queue<byte> packetData)
        {
            return dataTypes.ReadNextItemStackTemplate(packetData, itemPalette).GetTypeString();
        }

        private void SkipOptionalCraftingRequirements(Queue<byte> packetData)
        {
            if (!dataTypes.ReadNextBool(packetData))
                return;

            int ingredientCount = dataTypes.ReadNextVarInt(packetData);
            for (int i = 0; i < ingredientCount; i++)
                SkipItemHolderSet(packetData);
        }

        private void SkipItemHolderSet(Queue<byte> packetData)
        {
            int entryCount = dataTypes.ReadNextVarInt(packetData) - 1;
            if (entryCount == -1)
            {
                _ = dataTypes.ReadNextString(packetData);
                return;
            }

            for (int i = 0; i < entryCount; i++)
                _ = dataTypes.ReadNextVarInt(packetData);
        }

        private bool SkipRecipeBookSettings(Queue<byte> packetData)
        {
            // MC 1.13 uses 4 booleans for the crafting/smelting recipe book states.
            // MC 1.14+ expands this to 8 booleans by adding blast furnace and smoker states.
            int boolCount = protocolVersion >= MC_1_14_Version ? 8 : 4;
            if (packetData.Count < boolCount)
                return false;

            for (int i = 0; i < boolCount; i++)
                _ = dataTypes.ReadNextBool(packetData);

            return true;
        }

        /// <summary>
        /// Start the updating thread. Should be called after login success.
        /// </summary>
        private void StartUpdating()
        {
            Thread threadUpdater = new(new ParameterizedThreadStart(Updater))
            {
                Name = "ProtocolPacketHandler"
            };
            netMain = new Tuple<Thread, CancellationTokenSource>(threadUpdater, new CancellationTokenSource());
            threadUpdater.Start(netMain.Item2.Token);

            Thread threadReader = new(new ParameterizedThreadStart(PacketReader))
            {
                Name = "ProtocolPacketReader"
            };
            netReader = new Tuple<Thread, CancellationTokenSource>(threadReader, new CancellationTokenSource());
            threadReader.Start(netReader.Item2.Token);
        }

        /// <summary>
        /// Get net read thread (main thread) ID
        /// </summary>
        /// <returns>Net read thread ID</returns>
        public int GetNetMainThreadId()
        {
            return netMain is not null ? netMain.Item1.ManagedThreadId : -1;
        }

        /// <summary>
        /// Disconnect from the server, cancel network reading.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (netMain is not null)
                {
                    netMain.Item2.Cancel();
                }

                if (netReader is not null)
                {
                    netReader.Item2.Cancel();
                    socketWrapper.Disconnect();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Send a packet to the server. Packet ID, compression, and encryption will be handled automatically.
        /// </summary>
        /// <param name="packet">packet type</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(PacketTypesOut packet, IEnumerable<byte> packetData)
        {
            SendPacket(packetPalette.GetOutgoingIdByType(packet), packetData);
        }

        private void ProcessChunkBlockEntityData(int chunkX, int chunkZ, Queue<byte> packetData)
        {
            if (protocolVersion < MC_1_17_Version || packetData.Count == 0)
                return;

            int blockEntityCount = dataTypes.ReadNextVarInt(packetData);
            for (int i = 0; i < blockEntityCount; i++)
            {
                int packedXZ = dataTypes.ReadNextByte(packetData);
                int y = dataTypes.ReadNextShort(packetData);
                dataTypes.ReadNextVarInt(packetData); // Block entity type registry id
                Dictionary<string, object>? nbt = dataTypes.ReadNextNbt(packetData);
                int blockX = chunkX * Chunk.SizeX + ((packedXZ >> 4) & 0x0F);
                int blockZ = chunkZ * Chunk.SizeZ + (packedXZ & 0x0F);
                handler.OnBlockEntityData(new Location(blockX, y, blockZ), nbt);
            }
        }

        /// <summary>
        /// Send a configuration packet to the server. Packet ID, compression, and encryption will be handled automatically.
        /// </summary>
        /// <param name="packet">packet type</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(ConfigurationPacketTypesOut packet, IEnumerable<byte> packetData)
        {
            SendPacket(packetPalette.GetOutgoingIdByTypeConfiguration(packet), packetData);
        }

        /// <summary>
        /// Send a packet to the server. Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="packetId">packet ID</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetId, IEnumerable<byte> packetData)
        {
            byte[] payload = packetData as byte[] ?? packetData.ToArray();

            if (handler.GetNetworkPacketCaptureEnabled())
            {
                handler.OnNetworkPacket(packetId, payload.ToList(), currentState == CurrentState.Login, false);
            }

            //log.Info($"[C -> S] Sending packet {packetId:X} > {dataTypes.ByteArrayToString(packetData.ToArray())}");

            //The inner packet
            byte[] packetIdBytes = DataTypes.GetVarInt(packetId);
            byte[] thePacket = new byte[packetIdBytes.Length + payload.Length];
            Buffer.BlockCopy(packetIdBytes, 0, thePacket, 0, packetIdBytes.Length);
            Buffer.BlockCopy(payload, 0, thePacket, packetIdBytes.Length, payload.Length);

            if (compression_treshold >= 0) //Compression enabled?
            {
                byte[] compressedHeader = thePacket.Length >= compression_treshold
                    ? DataTypes.GetVarInt(thePacket.Length)
                    : DataTypes.GetVarInt(0);
                byte[] compressedPayload = thePacket.Length >= compression_treshold
                    ? ZlibUtils.Compress(thePacket)
                    : thePacket;

                byte[] compressedPacket = new byte[compressedHeader.Length + compressedPayload.Length];
                Buffer.BlockCopy(compressedHeader, 0, compressedPacket, 0, compressedHeader.Length);
                Buffer.BlockCopy(compressedPayload, 0, compressedPacket, compressedHeader.Length, compressedPayload.Length);
                thePacket = compressedPacket;
            }

            //log.Debug("[C -> S] Sending packet " + packetId + " > " + dataTypes.ByteArrayToString(dataTypes.ConcatBytes(dataTypes.GetVarInt(thePacket.Length), thePacket)));
            byte[] packetLengthBytes = DataTypes.GetVarInt(thePacket.Length);
            byte[] fullPacket = new byte[packetLengthBytes.Length + thePacket.Length];
            Buffer.BlockCopy(packetLengthBytes, 0, fullPacket, 0, packetLengthBytes.Length);
            Buffer.BlockCopy(thePacket, 0, fullPacket, packetLengthBytes.Length, thePacket.Length);
            socketWrapper.SendDataRAW(fullPacket);
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login(PlayerKeyPair? playerKeyPair, SessionToken session, bool isTransfer = false)
        {
            int nextState = isTransfer && protocolVersion >= MC_1_20_6_Version ? 3 : 2;

            if (nextState == 3)
                log.Debug("Using transfer handshake intent for transferred login.");

            // 1. Send the handshake packet
            SendPacket(0x00, dataTypes.ConcatBytes(
                    // Protocol Version (use raw version for snapshot/RC servers)
                    DataTypes.GetVarInt(rawProtocolVersion),

                    // Server Address
                    dataTypes.GetString(pForge.GetServerAddress(handler.GetServerHost())),

                    // Server Port
                    dataTypes.GetUShort((ushort)handler.GetServerPort()),

                    // Next State
                    DataTypes.GetVarInt(nextState)) // 2 is Login, 3 is Transfer
            );

            // 2. Send the Login Start packet
            List<byte> fullLoginPacket = new();
            fullLoginPacket.AddRange(dataTypes.GetString(handler.GetUsername())); // Username

            // 1.19 - 1.19.2
            if (protocolVersion is >= MC_1_19_Version and < MC_1_19_3_Version)
            {
                if (playerKeyPair is null)
                    fullLoginPacket.AddRange(dataTypes.GetBool(false)); // Has Sig Data
                else
                {
                    fullLoginPacket.AddRange(dataTypes.GetBool(true)); // Has Sig Data
                    fullLoginPacket.AddRange(
                        DataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds())); // Expiration time
                    fullLoginPacket.AddRange(
                        dataTypes.GetArray(playerKeyPair.PublicKey.Key)); // Public key received from Microsoft API
                    if (protocolVersion >= MC_1_19_2_Version)
                        fullLoginPacket.AddRange(
                            dataTypes.GetArray(playerKeyPair.PublicKey
                                .SignatureV2!)); // Public key signature received from Microsoft API
                    else
                        fullLoginPacket.AddRange(
                            dataTypes.GetArray(playerKeyPair.PublicKey
                                .Signature!)); // Public key signature received from Microsoft API
                }
            }

            var uuid = handler.GetUserUuid();
            switch (protocolVersion)
            {
                case >= MC_1_19_2_Version and < MC_1_20_2_Version:
                    {
                        if (uuid == Guid.Empty)
                            fullLoginPacket.AddRange(dataTypes.GetBool(false)); // Has UUID
                        else
                        {
                            fullLoginPacket.AddRange(dataTypes.GetBool(true)); // Has UUID
                            fullLoginPacket.AddRange(DataTypes.GetUUID(uuid)); // UUID
                        }

                        break;
                    }
                case >= MC_1_20_2_Version:
                    uuid = handler.GetUserUuid();

                    if (uuid == Guid.Empty)
                        uuid = Guid.NewGuid();

                    fullLoginPacket.AddRange(DataTypes.GetUUID(uuid)); // UUID
                    break;
            }

            SendPacket(0x00, fullLoginPacket);

            // 3. Encryption Request - 9. Login Acknowledged
            while (true)
            {
                var (packetId, packetData) = ReadNextPacket();

                switch (packetId)
                {
                    // Login rejected
                    case 0x00:
                        handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected,
                            ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                        return false;

                    // Encryption request
                    case 0x01:
                        {
                            isOnlineMode = true;
                            var serverId = dataTypes.ReadNextString(packetData);
                            var serverPublicKey = dataTypes.ReadNextByteArray(packetData);
                            var token = dataTypes.ReadNextByteArray(packetData);

                            var shouldAuthetnicate = false;

                            if (protocolVersion >= MC_1_20_6_Version)
                                shouldAuthetnicate = dataTypes.ReadNextBool(packetData);

                            return StartEncryption(handler.GetUserUuidStr(), handler.GetSessionID(),
                                Config.Main.General.AccountType, token, serverId,
                                serverPublicKey, playerKeyPair, session, shouldAuthetnicate);
                        }

                    // Login successful
                    case 0x02:
                        {
                            log.Info($"§8{Translations.mcc_server_offline}");
                            currentState = protocolVersion < MC_1_20_2_Version
                                ? CurrentState.Play
                                : CurrentState.Configuration;

                            if (protocolVersion >= MC_1_20_2_Version)
                                SendPacket(0x03, new List<byte>());

                            if (!pForge.CompleteForgeHandshake())
                            {
                                log.Error($"§8{Translations.error_forge}");
                                return false;
                            }

                            StartUpdating();
                            return true; //No need to check session or start encryption
                        }
                    default:
                        HandlePacket(packetId, packetData);
                        break;
                }
            }
        }

        /// <summary>
        /// Start network encryption. Automatically called by Login() if the server requests encryption.
        /// </summary>
        /// <returns>True if encryption was successful</returns>
        private bool StartEncryption(string uuid, string sessionID, LoginType type, byte[] token, string serverIDhash,
            byte[] serverPublicKey, PlayerKeyPair? playerKeyPair, SessionToken session, bool shouldAuthetnicate)
        {
            var RSAService = CryptoHandler.DecodeRSAPublicKey(serverPublicKey)!;
            var secretKey = CryptoHandler.ClientAESPrivateKey ?? CryptoHandler.GenerateAESPrivateKey();

            log.Debug($"§8{Translations.debug_crypto}");

            if (serverIDhash != "-")
            {
                log.Info(Translations.mcc_session);

                var needCheckSession = true;
                if (session is { ServerPublicKey: not null, SessionPreCheckTask: not null }
                    && serverIDhash == session.ServerIDhash &&
                    serverPublicKey.SequenceEqual(session.ServerPublicKey))
                {
                    session.SessionPreCheckTask.Wait();
                    if (session.SessionPreCheckTask.Result) // PreCheck Success
                        needCheckSession = false;
                }

                // 1.20.6++
                if (shouldAuthetnicate)
                    needCheckSession = true;

                if (needCheckSession)
                {
                    var serverHash = CryptoHandler.GetServerHash(serverIDhash, serverPublicKey, secretKey);
                    if (ProtocolHandler.SessionCheck(uuid, sessionID, serverHash, type))
                    {
                        session.ServerIDhash = serverIDhash;
                        session.ServerPublicKey = serverPublicKey;
                        SessionCache.Store(InternalConfig.Account.Login.ToLower(), session);
                    }
                    else
                    {
                        handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, Translations.mcc_session_fail);
                        return false;
                    }
                }
            }

            // Encryption Response packet
            List<byte> encryptionResponse = new();
            encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(secretKey, false))); // Shared Secret

            // 1.19 - 1.19.2
            if (protocolVersion is >= MC_1_19_Version and < MC_1_19_3_Version)
            {
                if (playerKeyPair is null)
                {
                    encryptionResponse.AddRange(dataTypes.GetBool(true)); // Has Verify Token
                    encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(token, false))); // Verify Token
                }
                else
                {
                    var salt = GenerateSalt();
                    var messageSignature = playerKeyPair.PrivateKey.SignData(dataTypes.ConcatBytes(token, salt));

                    encryptionResponse.AddRange(dataTypes.GetBool(false)); // Has Verify Token
                    encryptionResponse.AddRange(salt); // Salt
                    encryptionResponse.AddRange(dataTypes.GetArray(messageSignature)); // Message Signature
                }
            }
            else
            {
                encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(token, false))); // Verify Token
            }

            SendPacket(0x01, encryptionResponse);

            // Start client-side encryption
            socketWrapper.SwitchToEncrypted(secretKey); // pre switch

            // Process the next packet
            int loopPrevention = ushort.MaxValue;
            while (true)
            {
                var (packetId, packetData) = ReadNextPacket();
                if (packetId < 0 || loopPrevention-- < 0) // Failed to read packet or too many iterations (issue #1150)
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost,
                        $"§8{Translations.error_invalid_encrypt}");
                    return false;
                }

                switch (packetId)
                {
                    //Login rejected
                    case 0x00:
                        handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected,
                            ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                        return false;

                    //Login successful
                    case 0x02:
                        {
                            var uuidReceived = protocolVersion >= MC_1_16_Version
                                ? dataTypes.ReadNextUUID(packetData)
                                : Guid.Parse(dataTypes.ReadNextString(packetData));
                            var userName = dataTypes.ReadNextString(packetData);
                            Tuple<string, string, string>[]? playerProperty = null;
                            if (protocolVersion >= MC_1_19_Version)
                            {
                                var count = dataTypes.ReadNextVarInt(packetData); // Number Of Properties
                                playerProperty = new Tuple<string, string, string>[count];
                                for (var i = 0; i < count; ++i)
                                {
                                    var name = dataTypes.ReadNextString(packetData);
                                    var value = dataTypes.ReadNextString(packetData);
                                    var isSigned = dataTypes.ReadNextBool(packetData);
                                    var signature = isSigned ? dataTypes.ReadNextString(packetData) : string.Empty;
                                    playerProperty[i] = new Tuple<string, string, string>(name, value, signature);
                                }
                            }

                            // Strict Error Handling (removed in 1.21.2)
                            if (protocolVersion >= MC_1_20_6_Version && protocolVersion < MC_1_21_2_Version)
                                dataTypes.ReadNextBool(packetData);

                            currentState = protocolVersion < MC_1_20_2_Version
                                ? CurrentState.Play
                                : CurrentState.Configuration;

                            if (protocolVersion >= MC_1_20_2_Version)
                                SendPacket(0x03, new List<byte>());

                            handler.OnLoginSuccess(uuidReceived, userName, playerProperty);

                            if (!pForge.CompleteForgeHandshake())
                            {
                                log.Error($"§8{Translations.error_forge_encrypt}");
                                return false;
                            }

                            StartUpdating();
                            return true;
                        }
                    default:
                        HandlePacket(packetId, packetData);
                        break;
                }
            }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            socketWrapper.Disconnect();
        }

        /// <summary>
        /// Autocomplete text while typing username or command
        /// </summary>
        /// <param name="BehindCursor">Text behind cursor</param>
        /// <returns>Completed text</returns>
        int IAutoComplete.AutoComplete(string BehindCursor)
        {
            if (string.IsNullOrEmpty(BehindCursor))
                return -1;

            var transactionId = DataTypes.GetVarInt(autocomplete_transaction_id);
            byte[] assumeCommand = [0x00];
            byte[] hasPosition = [0x00];
            byte[] tabCompletePacket = [];

            switch (protocolVersion)
            {
                case >= MC_1_8_Version and >= MC_1_13_Version:
                    tabCompletePacket = dataTypes.ConcatBytes(tabCompletePacket, transactionId);
                    tabCompletePacket = dataTypes.ConcatBytes(tabCompletePacket,
                        dataTypes.GetString(BehindCursor.Replace(' ', (char)0x00)));
                    break;
                case >= MC_1_8_Version:
                    {
                        tabCompletePacket = dataTypes.ConcatBytes(tabCompletePacket, dataTypes.GetString(BehindCursor));

                        if (protocolVersion >= MC_1_9_Version)
                            tabCompletePacket = dataTypes.ConcatBytes(tabCompletePacket, assumeCommand);

                        tabCompletePacket = dataTypes.ConcatBytes(tabCompletePacket, hasPosition);
                        break;
                    }
                default:
                    tabCompletePacket = dataTypes.ConcatBytes(dataTypes.GetString(BehindCursor));
                    break;
            }

            ConsoleIO.AutoCompleteDone = false;
            SendPacket(PacketTypesOut.TabComplete, tabCompletePacket);
            return autocomplete_transaction_id;
        }

        /// <summary>
        /// Ping a Minecraft server to get information about the server
        /// </summary>
        /// <returns>True if ping was successful</returns>
        public static bool DoPing(string host, int port, ref int protocolVersion, ref ForgeInfo? forgeInfo)
        {
            SocketWrapper? socketWrapper = null;

            try
            {
                var version = "";
                var tcp = ProxyHandler.NewTcpClient(host, port);
                tcp.ReceiveTimeout = 30000; // 30 seconds
                tcp.ReceiveBufferSize = 1024 * 1024;
                socketWrapper = new SocketWrapper(tcp);
                DataTypes dataTypes = new(MC_1_8_Version);

                var serverPort = BitConverter.GetBytes((ushort)port);
                Array.Reverse(serverPort);

                // Ping Packet
                var pingPacket = dataTypes.ConcatBytes(
                    // Packet Id
                    DataTypes.GetVarInt(0),

                    // Protocol Version
                    DataTypes.GetVarInt(-1),

                    // Server IP (Host)
                    dataTypes.GetString(host),

                    // Server port
                    serverPort,

                    // Next State
                    DataTypes.GetVarInt(1));

                socketWrapper.SendDataRAW(dataTypes.ConcatBytes(DataTypes.GetVarInt(pingPacket.Length), pingPacket));

                // Status Request Packet
                var statusRequest = DataTypes.GetVarInt(0);
                socketWrapper.SendDataRAW(dataTypes.ConcatBytes(DataTypes.GetVarInt(statusRequest.Length), statusRequest));

                // Read Response length
                var packetLength = dataTypes.ReadNextVarIntRAW(socketWrapper);
                if (packetLength <= 0)
                    return false;

                // Read the Packet Id
                var packetData = new Queue<byte>(socketWrapper.ReadDataRAW(packetLength));
                if (dataTypes.ReadNextVarInt(packetData) != 0x00)
                    return false;

                // Get the Json data
                var result = dataTypes.ReadNextString(packetData);

                if (Config.Logging.DebugMessages)
                {
                    // May contain formatting codes, cannot use WriteLineFormatted
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    ConsoleIO.WriteLine(result);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (string.IsNullOrEmpty(result) || !result.StartsWith("{") || !result.EndsWith("}"))
                    return false;

                var jsonData = Json.ParseJson(result);
                if (jsonData is not System.Text.Json.Nodes.JsonObject jsonObj || !jsonObj.ContainsKey("version"))
                    return false;

                var versionData = jsonObj["version"]!.AsObject();

                // Retrieve display name of the Minecraft version
                if (versionData["name"] is { } nameNode)
                    version = nameNode.GetStringValue();

                // Retrieve protocol version number for handling this server
                if (versionData["protocol"] is { } protocolNode)
                    protocolVersion = int.Parse(protocolNode.GetStringValue(),
                        NumberStyles.Any, CultureInfo.CurrentCulture);

                // Check for forge on the server.
                Protocol18Forge.ServerInfoCheckForge(jsonObj, ref forgeInfo);

                int onlinePlayers = 0, maxPlayers = 0;
                List<ServerStatusInfo.SamplePlayer> samplePlayers = [];

                if (jsonObj["players"] is System.Text.Json.Nodes.JsonObject playersObj)
                {
                    if (playersObj["online"] is { } onlineNode)
                        onlinePlayers = int.Parse(onlineNode.GetStringValue(), NumberStyles.Any, CultureInfo.CurrentCulture);
                    if (playersObj["max"] is { } maxNode)
                        maxPlayers = int.Parse(maxNode.GetStringValue(), NumberStyles.Any, CultureInfo.CurrentCulture);
                    if (playersObj["sample"] is System.Text.Json.Nodes.JsonArray sampleArray)
                    {
                        foreach (var entry in sampleArray)
                        {
                            if (entry is not System.Text.Json.Nodes.JsonObject playerObj) continue;
                            samplePlayers.Add(new ServerStatusInfo.SamplePlayer
                            {
                                Name = playerObj["name"]?.GetStringValue() ?? "",
                                Id = playerObj["id"]?.GetStringValue() ?? ""
                            });
                        }
                    }
                }

                string motdRaw = "";
                if (jsonObj["description"] is { } descNode)
                    motdRaw = descNode.ToJsonString();

                string? faviconBase64 = null;
                if (jsonObj["favicon"] is { } faviconNode)
                {
                    var faviconStr = faviconNode.GetStringValue();
                    const string prefix = "data:image/png;base64,";
                    faviconBase64 = faviconStr.StartsWith(prefix, StringComparison.Ordinal)
                        ? faviconStr[prefix.Length..]
                        : faviconStr;
                }

                long pingMs = -1;
                try
                {
                    long pingPayload = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var pingRequest = dataTypes.ConcatBytes(DataTypes.GetVarInt(0x01), DataTypes.GetLong(pingPayload));
                    socketWrapper.SendDataRAW(dataTypes.ConcatBytes(DataTypes.GetVarInt(pingRequest.Length), pingRequest));

                    packetLength = dataTypes.ReadNextVarIntRAW(socketWrapper);
                    if (packetLength > 0)
                    {
                        packetData = new Queue<byte>(socketWrapper.ReadDataRAW(packetLength));
                        if (dataTypes.ReadNextVarInt(packetData) == 0x01)
                        {
                            long pongPayload = dataTypes.ReadNextLong(packetData);
                            pingMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - pingPayload;
                        }
                    }
                }
                catch
                {
                    // Some servers may close the probe connection immediately after the status response.
                }

                var statusInfo = new ServerStatusInfo
                {
                    Host = host,
                    Port = port,
                    VersionName = version,
                    ProtocolVersion = protocolVersion,
                    OnlinePlayers = onlinePlayers,
                    MaxPlayers = maxPlayers,
                    SamplePlayers = samplePlayers,
                    MotdRaw = motdRaw,
                    FaviconBase64 = faviconBase64,
                    PingMs = pingMs
                };

                ProtocolHandler.TryUpgradeProtocolVersion(version, ref protocolVersion);
                statusInfo.ResolvedProtocol = protocolVersion;

                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_server_protocol, version,
                    protocolVersion + (forgeInfo is not null ? Translations.mcc_with_forge : "")));

                ServerStatusDisplay.Show(statusInfo);

                return true;
            }
            finally
            {
                socketWrapper?.Disconnect();
            }
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            int configOverride = Settings.MainConfigHelper.Config.Advanced.MaxChatMessageLength;
            if (configOverride > 0)
                return configOverride;
            return protocolVersion > MC_1_10_Version ? 256 : 100;
        }

        /// <summary>
        /// Get the current protocol version.
        /// </summary>
        /// <remarks>
        /// Version-specific operations should be handled inside the Protocol handled whenever possible.
        /// </remarks>
        /// <returns>Minecraft Protocol version number</returns>
        public int GetProtocolVersion()
        {
            return protocolVersion;
        }

        /// <summary>
        /// Send MessageAcknowledgment packet
        /// </summary>
        /// <param name="acknowledgment">Message acknowledgment</param>
        /// <returns>True if properly sent</returns>
        public bool SendMessageAcknowledgment(LastSeenMessageList.Acknowledgment acknowledgment)
        {
            try
            {
                var fields = dataTypes.GetAcknowledgment(acknowledgment,
                    isOnlineMode && Config.Signature.LoginWithSecureProfile);

                SendPacket(PacketTypesOut.MessageAcknowledgment, fields);

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Send MessageAcknowledgment packet
        /// </summary>
        /// <param name="acknowledgment">Message acknowledgment</param>
        /// <returns>True if properly sent</returns>
        public bool SendMessageAcknowledgment(int messageCount)
        {
            try
            {
                var fields = DataTypes.GetVarInt(messageCount);
                SendPacket(PacketTypesOut.MessageAcknowledgment, fields);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public LastSeenMessageList.Acknowledgment ConsumeAcknowledgment()
        {
            pendingAcknowledgments = 0;
            return new LastSeenMessageList.Acknowledgment(lastSeenMessagesCollector.GetLastSeenMessages(),
                lastReceivedMessage);
        }

        public void Acknowledge(ChatMessage message)
        {
            var entry = message.ToLastSeenMessageEntry();
            if (entry is null) return;

            if (protocolVersion >= MC_1_19_3_Version)
            {
                if (!lastSeenMessagesCollector.Add_1_19_3(entry, true)) return;
                if (lastSeenMessagesCollector.messageCount <= 64) return;

                var messageCount = lastSeenMessagesCollector.ResetMessageCount();
                if (messageCount > 0)
                    SendMessageAcknowledgment(messageCount);
            }
            else
            {
                lastSeenMessagesCollector.Add_1_19_2(entry);
                lastReceivedMessage = null;
                if (pendingAcknowledgments++ > 64)
                    SendMessageAcknowledgment(ConsumeAcknowledgment());
            }
        }

        /// <summary>
        /// Send a chat command to the server, with or without signing based on the online mode and version.
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="playerKeyPair">PlayerKeyPair (optional)</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatCommand(string command, PlayerKeyPair? playerKeyPair = null)
        {
            if (string.IsNullOrEmpty(command))
                return true;

            command = Regex.Replace(command, @"\s+", " ");
            command = Regex.Replace(command, @"\s$", string.Empty);

            log.Debug($"chat command = {command}");

            if (protocolVersion >= MC_1_20_6_Version && !isOnlineMode)
            {
                List<byte> fields = new();
                fields.AddRange(dataTypes.GetString(command));
                SendPacket(PacketTypesOut.ChatCommand, fields);
                return true;
            }

            try
            {
                List<Tuple<string, string>>? needSigned = null;
                bool canSignCommand = protocolVersion >= MC_1_19_Version &&
                                      isOnlineMode &&
                                      playerKeyPair is not null &&
                                      Config.Signature.LoginWithSecureProfile &&
                                      Config.Signature.SignMessageInCommand;

                if (canSignCommand)
                {
                    if (DeclareCommands.IsCommandTreeAvailable)
                    {
                        needSigned = DeclareCommands.CollectSignArguments(command);
                    }
                    else
                    {
                        needSigned = [];
                        log.Debug("DeclareCommands tree unavailable, sending command without signed arguments.");
                    }
                }

                lock (MessageSigningLock)
                {
                    var acknowledgment1192 = protocolVersion == MC_1_19_2_Version ? ConsumeAcknowledgment() : null;

                    var (acknowledgment1193, bitset1193, messageCount1193) = protocolVersion >= MC_1_19_3_Version
                        ? lastSeenMessagesCollector.Collect_1_19_3()
                        : new(Array.Empty<LastSeenMessageList.AcknowledgedMessage>(), Array.Empty<byte>(), 0);

                    List<byte> fields = new();
                    fields.AddRange(dataTypes.GetString(command));
                    var timeNow = DateTimeOffset.UtcNow;
                    fields.AddRange(DataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                    if (needSigned is null || needSigned.Count == 0)
                    {
                        fields.AddRange(DataTypes.GetLong(0));
                        fields.AddRange(DataTypes.GetVarInt(0));
                    }
                    else
                    {
                        var uuid = handler.GetUserUuid();
                        var salt = GenerateSalt();
                        fields.AddRange(salt);
                        fields.AddRange(DataTypes.GetVarInt(needSigned.Count));

                        foreach (var (argName, message) in needSigned)
                        {
                            fields.AddRange(dataTypes.GetString(argName));
                            var sign = protocolVersion switch
                            {
                                MC_1_19_Version => playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow, ref salt),
                                MC_1_19_2_Version => playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow, ref salt, acknowledgment1192!.lastSeen),
                                _ => playerKeyPair!.PrivateKey.SignMessage(message, uuid, chatUuid, messageIndex++, timeNow, ref salt, acknowledgment1193)
                            };

                            if (protocolVersion <= MC_1_19_2_Version)
                                fields.AddRange(DataTypes.GetVarInt(sign.Length));

                            fields.AddRange(sign);
                        }
                    }

                    if (protocolVersion <= MC_1_19_2_Version)
                        fields.AddRange(dataTypes.GetBool(false));

                    switch (protocolVersion)
                    {
                        case MC_1_19_2_Version:
                            fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment1192!, isOnlineMode && Config.Signature.LoginWithSecureProfile));
                            break;
                        case >= MC_1_19_3_Version:
                            fields.AddRange(DataTypes.GetVarInt(messageCount1193));
                            fields.AddRange(bitset1193);

                            // Checksum: Byte (1.21.5+, 0 = skip verification)
                            if (protocolVersion >= MC_1_21_5_Version)
                                fields.Add(0);
                            break;
                    }

                    SendPacket(protocolVersion < MC_1_20_6_Version ? PacketTypesOut.ChatCommand : PacketTypesOut.SignedChatCommand, fields);
                }

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="playerKeyPair">PlayerKeyPair</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatMessage(string message, PlayerKeyPair? playerKeyPair)
        {
            if (string.IsNullOrEmpty(message))
                return true;

            // Process Chat Command - 1.19 and above
            if (protocolVersion >= MC_1_19_Version && message.StartsWith('/'))
                return SendChatCommand(message[1..], playerKeyPair);

            try
            {
                List<byte> fields = new();

                // 	Message: String (up to 256 chars)
                fields.AddRange(dataTypes.GetString(message));

                if (protocolVersion >= MC_1_19_Version)
                {
                    lock (MessageSigningLock)
                    {
                        var acknowledgment1192 =
                            protocolVersion == MC_1_19_2_Version ? ConsumeAcknowledgment() : null;

                        var (acknowledgment1193, bitset1193, messageCount1193) =
                            protocolVersion >= MC_1_19_3_Version
                                ? lastSeenMessagesCollector.Collect_1_19_3()
                                : new(Array.Empty<LastSeenMessageList.AcknowledgedMessage>(), Array.Empty<byte>(), 0);

                        // Timestamp: Instant(Long)
                        var timeNow = DateTimeOffset.UtcNow;
                        fields.AddRange(DataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                        if (!isOnlineMode || playerKeyPair is null || !Config.Signature.LoginWithSecureProfile ||
                            !Config.Signature.SignChat)
                        {
                            fields.AddRange(DataTypes.GetLong(0)); // Salt: Long
                            fields.AddRange(protocolVersion < MC_1_19_3_Version
                                ? DataTypes.GetVarInt(0) // Signature Length: VarInt (1.19 - 1.19.2)
                                : dataTypes.GetBool(false)); // Has signature: bool (1.19.3)
                        }
                        else
                        {
                            // Salt: Long
                            var salt = GenerateSalt();
                            fields.AddRange(salt);

                            // Signature Length & Signature: (VarInt) and Byte Array
                            var playerUuid = handler.GetUserUuid();
                            var sign = protocolVersion switch
                            {
                                MC_1_19_Version => playerKeyPair.PrivateKey.SignMessage(message, playerUuid, timeNow,
                                    ref salt),
                                MC_1_19_2_Version => playerKeyPair.PrivateKey.SignMessage(message, playerUuid, timeNow,
                                    ref salt, acknowledgment1192!.lastSeen),
                                _ => playerKeyPair.PrivateKey.SignMessage(message, playerUuid, chatUuid, messageIndex++,
                                    timeNow, ref salt, acknowledgment1193)
                            };

                            fields.AddRange(protocolVersion >= MC_1_19_3_Version
                                ? dataTypes.GetBool(true)
                                : DataTypes.GetVarInt(sign.Length));
                            fields.AddRange(sign);
                        }

                        if (protocolVersion <= MC_1_19_2_Version)
                            fields.AddRange(dataTypes.GetBool(false)); // Signed Preview: Boolean

                        switch (protocolVersion)
                        {
                            case >= MC_1_19_3_Version:
                                // message count
                                fields.AddRange(DataTypes.GetVarInt(messageCount1193));

                                // Acknowledged: BitSet
                                fields.AddRange(bitset1193);

                                // Checksum: Byte (1.21.5+, 0 = skip verification)
                                if (protocolVersion >= MC_1_21_5_Version)
                                    fields.Add(0);
                                break;
                            case MC_1_19_2_Version:
                                // Message Acknowledgment
                                fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment1192!,
                                    isOnlineMode && Config.Signature.LoginWithSecureProfile));
                                break;
                        }
                    }
                }

                SendPacket(PacketTypesOut.ChatMessage, fields);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendEntityAction(int PlayerEntityID, int ActionID)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetVarInt(PlayerEntityID));
                fields.AddRange(DataTypes.GetVarInt(ActionID));
                fields.AddRange(DataTypes.GetVarInt(0));
                SendPacket(PacketTypesOut.EntityAction, fields);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Send a respawn packet to the server
        /// </summary>
        /// <returns>True if properly sent</returns>
        public bool SendRespawnPacket()
        {
            try
            {
                SendPacket(PacketTypesOut.ClientStatus, [0]);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Tell the server what client is being used to connect to the server
        /// </summary>
        /// <param name="brandInfo">Client string describing the client</param>
        /// <returns>True if brand info was successfully sent</returns>
        public bool SendBrandInfo(string brandInfo)
        {
            if (string.IsNullOrEmpty(brandInfo))
                return false;

            // Plugin channels were significantly changed between Minecraft 1.12 and 1.13
            // https://wiki.vg/index.php?title=Pre-release_protocol&oldid=14132#Plugin_Channels
            return SendPluginChannelPacket(protocolVersion >= MC_1_13_Version ? "minecraft:brand" : "MC|Brand",
                dataTypes.GetString(brandInfo));
        }

        /// <summary>
        /// Inform the server of the client's Minecraft settings
        /// </summary>
        /// <param name="language">Client language eg en_US</param>
        /// <param name="viewDistance">View distance, in chunks</param>
        /// <param name="difficulty">Game difficulty (client-side...)</param>
        /// <param name="chatMode">Chat mode (allows muting yourself)</param>
        /// <param name="chatColors">Show chat colors</param>
        /// <param name="skinParts">Show skin layers</param>
        /// <param name="mainHand">1.9+ main hand</param>
        /// <returns>True if client settings were successfully sent</returns>
        public bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode,
            bool chatColors, byte skinParts, byte mainHand)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(dataTypes.GetString(language));
                fields.Add(viewDistance);

                fields.AddRange(protocolVersion >= MC_1_9_Version
                    ? DataTypes.GetVarInt(chatMode)
                    : [chatMode]);

                fields.Add(chatColors ? (byte)1 : (byte)0);
                if (protocolVersion < MC_1_8_Version)
                {
                    fields.Add(difficulty);
                    fields.Add((byte)(skinParts & 0x1)); //show cape
                }
                else fields.Add(skinParts);

                if (protocolVersion >= MC_1_9_Version)
                    fields.AddRange(DataTypes.GetVarInt(mainHand));
                if (protocolVersion >= MC_1_17_Version)
                {
                    fields.Add(protocolVersion >= MC_1_18_1_Version
                        ? (byte)0
                        : (byte)1); // 1.17 and 1.17.1 - Disable text filtering. (Always true)
                    // 1.18 and above - Enable text filtering. (Always false)
                }

                if (protocolVersion >= MC_1_18_1_Version)
                    fields.Add(1); // 1.18 and above - Allow server listings

                if (protocolVersion >= MC_1_21_2_Version)
                    fields.AddRange(DataTypes.GetVarInt(0)); // 1.21.2+ Particle status: 0=All, 1=Decreased, 2=Minimal
                SendPacket(PacketTypesOut.ClientSettings, fields);
            }
            catch (SocketException)
            {
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }

            return false;
        }


        /// <summary>
        /// Send a location update to the server
        /// </summary>
        /// <param name="location">The new location of the player</param>
        /// <param name="onGround">True if the player is on the ground</param>
        /// <param name="horizontalCollision">True if the player is colliding horizontally</param>
        /// <param name="yaw">Optional new yaw for updating player look</param>
        /// <param name="pitch">Optional new pitch for updating player look</param>
        /// <returns>True if the location update was successfully sent</returns>
        public bool SendLocationUpdate(Location location, bool onGround, bool horizontalCollision, float? yaw, float? pitch)
        {
            return SendLocationUpdate(location, onGround, horizontalCollision, yaw, pitch, true);
        }

        public bool SendLocationUpdate(Location location, bool onGround, bool horizontalCollision, float? yaw = null, float? pitch = null,
            bool forceUpdate = false)
        {
            if (handler.GetTerrainEnabled())
            {
                bool legacyMovementCadence = protocolVersion < MC_1_9_Version;
                bool supportsHorizontalCollision = protocolVersion >= MC_1_21_5_Version;
                int positionReminderInterval = ClientTicksPerSecond;

                double dx = location.X - lastSentX;
                double dy = location.Y - lastSentY;
                double dz = location.Z - lastSentZ;
                double distSqr = dx * dx + dy * dy + dz * dz;

                bool rotationChanged = false;
                if (yaw.HasValue && pitch.HasValue)
                    rotationChanged = forceUpdate || yaw.Value != lastSentYaw || pitch.Value != lastSentPitch;

                positionReminder++;

                try
                {
                    PacketTypesOut packetType;
                    byte[] payload;
                    byte flags = (byte)(onGround ? 1 : 0);
                    bool positionChanged;

                    if (legacyMovementCadence)
                    {
                        // 1.7.2-1.8.9 mirrors EntityPlayerSP#onUpdateWalkingPlayer:
                        // send an idle PlayerMovement packet every client tick and force
                        // a position refresh every 20 ticks even if the player is standing still.
                        positionChanged = distSqr > 9.0E-4 || positionReminder >= positionReminderInterval;

                        if (positionChanged && rotationChanged && yaw.HasValue && pitch.HasValue)
                        {
                            packetType = PacketTypesOut.PlayerPositionAndRotation;
                            payload = dataTypes.ConcatBytes(
                                dataTypes.GetDouble(location.X),
                                dataTypes.GetDouble(location.Y),
                                protocolVersion < MC_1_8_Version
                                    ? dataTypes.GetDouble(location.Y + 1.62)
                                    : [],
                                dataTypes.GetDouble(location.Z),
                                dataTypes.GetFloat(yaw.Value),
                                dataTypes.GetFloat(pitch.Value),
                                new[] { flags });
                            lastSentYaw = yaw.Value;
                            lastSentPitch = pitch.Value;
                            LastYaw = yaw.Value;
                            LastPitch = pitch.Value;
                        }
                        else if (positionChanged)
                        {
                            packetType = PacketTypesOut.PlayerPosition;
                            payload = dataTypes.ConcatBytes(
                                dataTypes.GetDouble(location.X),
                                dataTypes.GetDouble(location.Y),
                                protocolVersion < MC_1_8_Version
                                    ? dataTypes.GetDouble(location.Y + 1.62)
                                    : [],
                                dataTypes.GetDouble(location.Z),
                                new[] { flags });
                        }
                        else if (rotationChanged && yaw.HasValue && pitch.HasValue)
                        {
                            packetType = PacketTypesOut.PlayerRotation;
                            payload = dataTypes.ConcatBytes(
                                dataTypes.GetFloat(yaw.Value),
                                dataTypes.GetFloat(pitch.Value),
                                new[] { flags });
                            lastSentYaw = yaw.Value;
                            lastSentPitch = pitch.Value;
                            LastYaw = yaw.Value;
                            LastPitch = pitch.Value;
                        }
                        else
                        {
                            packetType = PacketTypesOut.PlayerMovement;
                            payload = new[] { flags };
                        }
                    }
                    else
                    {
                        positionChanged = distSqr > 4.0E-8 || positionReminder >= positionReminderInterval;
                        bool movementStateChanged = onGround != lastSentOnGround
                            || (supportsHorizontalCollision && horizontalCollision != lastSentHorizontalCollision);

                        if (!positionChanged && !rotationChanged && !movementStateChanged)
                            return true; // Nothing to send

                        if (supportsHorizontalCollision && horizontalCollision)
                            flags |= 0x2;

                        if (positionChanged && rotationChanged && yaw.HasValue && pitch.HasValue)
                        {
                            packetType = PacketTypesOut.PlayerPositionAndRotation;
                            payload = dataTypes.ConcatBytes(
                                dataTypes.GetDouble(location.X),
                                dataTypes.GetDouble(location.Y),
                                protocolVersion < MC_1_8_Version
                                    ? dataTypes.GetDouble(location.Y + 1.62)
                                    : [],
                                dataTypes.GetDouble(location.Z),
                                dataTypes.GetFloat(yaw.Value),
                                dataTypes.GetFloat(pitch.Value),
                                new[] { flags });
                            lastSentYaw = yaw.Value;
                            lastSentPitch = pitch.Value;
                            LastYaw = yaw.Value;
                            LastPitch = pitch.Value;
                        }
                        else if (positionChanged)
                        {
                            packetType = PacketTypesOut.PlayerPosition;
                            payload = dataTypes.ConcatBytes(
                                dataTypes.GetDouble(location.X),
                                dataTypes.GetDouble(location.Y),
                                protocolVersion < MC_1_8_Version
                                    ? dataTypes.GetDouble(location.Y + 1.62)
                                    : [],
                                dataTypes.GetDouble(location.Z),
                                new[] { flags });
                        }
                        else if (rotationChanged && yaw.HasValue && pitch.HasValue)
                        {
                            packetType = PacketTypesOut.PlayerRotation;
                            payload = dataTypes.ConcatBytes(
                                dataTypes.GetFloat(yaw.Value),
                                dataTypes.GetFloat(pitch.Value),
                                new[] { flags });
                            lastSentYaw = yaw.Value;
                            lastSentPitch = pitch.Value;
                            LastYaw = yaw.Value;
                            LastPitch = pitch.Value;
                        }
                        else
                        {
                            packetType = PacketTypesOut.PlayerMovement;
                            payload = new[] { flags };
                        }
                    }

                    if (positionChanged)
                    {
                        lastSentX = location.X;
                        lastSentY = location.Y;
                        lastSentZ = location.Z;
                        positionReminder = 0;
                    }
                    lastSentOnGround = onGround;
                    lastSentHorizontalCollision = horizontalCollision;

                    SendPacket(packetType, payload);
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
                catch (System.IO.IOException)
                {
                    return false;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
            else return false;
        }

        /// <summary>
        /// Send a plugin channel packet (0x17) to the server, compression and encryption will be handled automatically
        /// </summary>
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        public bool SendPluginChannelPacket(string channel, byte[] data)
        {
            try
            {
                // In 1.7, length needs to be included.
                // In 1.8, it must not be.
                if (protocolVersion < MC_1_8_Version)
                {
                    var length = BitConverter.GetBytes((short)data.Length);
                    Array.Reverse(length);

                    SendPacket(PacketTypesOut.PluginMessage,
                        dataTypes.ConcatBytes(dataTypes.GetString(channel), length, data));
                }
                else
                {
                    SendPacket(PacketTypesOut.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), data));
                }

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Send a Login Plugin Response packet (0x02)
        /// </summary>
        /// <param name="messageId">Login Plugin Request message Id </param>
        /// <param name="understood">TRUE if the request was understood</param>
        /// <param name="data">Response to the request</param>
        /// <returns>TRUE if successfully sent</returns>
        public bool SendLoginPluginResponse(int messageId, bool understood, byte[] data)
        {
            try
            {
                SendPacket(0x02,
                    dataTypes.ConcatBytes(DataTypes.GetVarInt(messageId), dataTypes.GetBool(understood), data));
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Send an Interact Entity Packet to server
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool SendInteractEntity(int EntityID, int type)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetVarInt(EntityID));
                fields.AddRange(DataTypes.GetVarInt(type));

                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));

                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        // TODO: Interact at block location (e.g. chest minecart)
        public bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z, int hand)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetVarInt(EntityID));
                fields.AddRange(DataTypes.GetVarInt(type));
                fields.AddRange(dataTypes.GetFloat(X));
                fields.AddRange(dataTypes.GetFloat(Y));
                fields.AddRange(dataTypes.GetFloat(Z));
                fields.AddRange(DataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendInteractEntity(int EntityID, int type, int hand)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetVarInt(EntityID));
                fields.AddRange(DataTypes.GetVarInt(type));
                fields.AddRange(DataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z)
        {
            return false;
        }

        public bool SendUseItem(int hand, int sequenceId)
        {
            if (protocolVersion < MC_1_9_Version)
                return false; // Packet does not exist prior to MC 1.9
            // According to https://wiki.vg/index.php?title=Protocol&oldid=5486#Player_Block_Placement
            // MC 1.7 does this using Player Block Placement with special values
            // TODO once Player Block Placement is implemented for older versions
            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetVarInt(hand));
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(DataTypes.GetVarInt(sequenceId));

                if (protocolVersion >= MC_1_21_Version)
                {
                    packet.AddRange(dataTypes.GetFloat(LastYaw));
                    packet.AddRange(dataTypes.GetFloat(LastPitch));
                }

                SendPacket(PacketTypesOut.UseItem, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendPlayerDigging(int status, Location location, Direction face, int sequenceId)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetVarInt(status));
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(DataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(DataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.PlayerDigging, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendPlayerBlockPlacement(int hand, Location location, Direction face, int sequenceId)
        {
            try
            {
                var packet = new List<byte>();
                var (cursorX, cursorY, cursorZ) = GetFaceHitCursor(face);

                switch (protocolVersion)
                {
                    case < MC_1_9_Version:
                        packet.AddRange(dataTypes.GetLocation(location));
                        packet.Add(dataTypes.GetBlockFace(face));

                        var playerInventory = handler.GetInventory(0);

                        if (playerInventory?.Items is null)
                            return false;

                        int[] slotWindowIds = [36, 37, 38, 39, 40, 41, 42, 43, 44];
                        var currentSlot = ((McClient)handler).GetCurrentSlot();

                        playerInventory.Items.TryGetValue(slotWindowIds[currentSlot], out var item);
                        packet.AddRange(dataTypes.GetItemSlot(item, itemPalette));

                        packet.Add(0); // cursorX
                        packet.Add(0); // cursorY
                        packet.Add(0); // cursorZ

                        return true;
                    case < MC_1_14_Version:
                        packet.AddRange(dataTypes.GetLocation(location));
                        packet.AddRange(DataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                        packet.AddRange(DataTypes.GetVarInt(hand));
                        break;
                    default:
                        packet.AddRange(DataTypes.GetVarInt(hand));
                        packet.AddRange(dataTypes.GetLocation(location));
                        packet.AddRange(DataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                        break;
                }

                packet.AddRange(dataTypes.GetFloat(cursorX)); // cursorX
                packet.AddRange(dataTypes.GetFloat(cursorY)); // cursorY
                packet.AddRange(dataTypes.GetFloat(cursorZ)); // cursorZ

                if (protocolVersion >= MC_1_14_Version)
                    packet.Add(0); // insideBlock = false

                if (protocolVersion >= MC_1_21_2_Version)
                    packet.Add(0); // worldBorderHit = false

                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(DataTypes.GetVarInt(sequenceId));

                SendPacket(PacketTypesOut.PlayerBlockPlacement, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private static (float x, float y, float z) GetFaceHitCursor(Direction face) => face switch
        {
            Direction.Up => (0.5f, 1.0f, 0.5f),
            Direction.Down => (0.5f, 0.0f, 0.5f),
            Direction.North => (0.5f, 0.5f, 0.0f),
            Direction.South => (0.5f, 0.5f, 1.0f),
            Direction.West => (0.0f, 0.5f, 0.5f),
            Direction.East => (1.0f, 0.5f, 0.5f),
            _ => (0.5f, 0.5f, 0.5f),
        };

        public bool SendHeldItemChange(short slot)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(dataTypes.GetShort(slot));
                SendPacket(PacketTypesOut.HeldItemChange, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, Item? item,
            List<Tuple<short, Item?>> changedSlots, int stateId)
        {
            try
            {
                short actionNumber;
                lock (window_actions)
                {
                    if (!window_actions.ContainsKey(windowId))
                        window_actions[windowId] = 0;
                    actionNumber = (short)(window_actions[windowId] + 1);
                    window_actions[windowId] = actionNumber;
                }

                byte button = 0;
                byte mode = 0;

                switch (action)
                {
                    case WindowActionType.LeftClick:
                        button = 0;
                        break;
                    case WindowActionType.RightClick:
                        button = 1;
                        break;
                    case WindowActionType.MiddleClick:
                        button = 2;
                        mode = 3;
                        break;
                    case WindowActionType.ShiftClick:
                        button = 0;
                        mode = 1;
                        item = new Item(ItemType.Null, 0, null);
                        break;
                    case WindowActionType.ShiftRightClick: // Right-shift click uses button 1
                        button = 1;
                        mode = 1;
                        item = new Item(ItemType.Null, 0, null);
                        break;
                    case WindowActionType.DropItem:
                        button = 0;
                        mode = 4;
                        item = new Item(ItemType.Null, 0, null);
                        break;
                    case WindowActionType.DropItemStack:
                        button = 1;
                        mode = 4;
                        item = new Item(ItemType.Null, 0, null);
                        break;
                    case WindowActionType.StartDragLeft:
                        button = 0;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        slotId = -999;
                        break;
                    case WindowActionType.StartDragRight:
                        button = 4;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        slotId = -999;
                        break;
                    case WindowActionType.StartDragMiddle:
                        button = 8;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        slotId = -999;
                        break;
                    case WindowActionType.EndDragLeft:
                        button = 2;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        slotId = -999;
                        break;
                    case WindowActionType.EndDragRight:
                        button = 6;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        slotId = -999;
                        break;
                    case WindowActionType.EndDragMiddle:
                        button = 10;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        slotId = -999;
                        break;
                    case WindowActionType.AddDragLeft:
                        button = 1;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        break;
                    case WindowActionType.AddDragRight:
                        button = 5;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        break;
                    case WindowActionType.AddDragMiddle:
                        button = 9;
                        mode = 5;
                        item = new Item(ItemType.Null, 0, null);
                        break;
                }

                List<byte> packet = new();
                if (protocolVersion >= MC_1_21_2_Version)
                    packet.AddRange(DataTypes.GetVarInt(windowId)); // Window ID (VarInt in 1.21.2+)
                else
                    packet.Add((byte)windowId); // Window ID (byte before 1.21.2)

                switch (protocolVersion)
                {
                    // 1.18+
                    case >= MC_1_18_1_Version:
                        packet.AddRange(DataTypes.GetVarInt(stateId)); // State ID
                        packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                        break;
                    // 1.17.1
                    case MC_1_17_1_Version:
                        packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                        packet.AddRange(DataTypes.GetVarInt(stateId)); // State ID
                        break;
                    // Older
                    default:
                        packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                        break;
                }

                packet.Add(button); // Button

                if (protocolVersion < MC_1_17_Version)
                    packet.AddRange(dataTypes.GetShort(actionNumber));

                if (protocolVersion >= MC_1_9_Version)
                    packet.AddRange(DataTypes.GetVarInt(mode)); // Mode
                else packet.Add(mode);

                // 1.17+  Array of changed slots
                if (protocolVersion >= MC_1_17_Version)
                {
                    packet.AddRange(DataTypes.GetVarInt(changedSlots.Count)); // Length of the array
                    foreach (var slot in changedSlots)
                    {
                        packet.AddRange(dataTypes.GetShort(slot.Item1)); // slot ID
                        // 1.21.5+ uses HashedStack instead of ItemStack for container_click
                        if (protocolVersion >= MC_1_21_5_Version)
                            packet.AddRange(dataTypes.GetHashedItemSlot(slot.Item2, itemPalette));
                        else
                            packet.AddRange(dataTypes.GetItemSlot(slot.Item2, itemPalette));
                    }
                }

                // 1.21.5+ uses HashedStack instead of ItemStack for carried item
                if (protocolVersion >= MC_1_21_5_Version)
                    packet.AddRange(dataTypes.GetHashedItemSlot(item, itemPalette));
                else
                    packet.AddRange(dataTypes.GetItemSlot(item, itemPalette));

                SendPacket(PacketTypesOut.ClickWindow, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendCreativeInventoryAction(int slot, ItemType itemType, int count, Dictionary<string, object>? nbt)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(dataTypes.GetShort((short)slot));
                packet.AddRange(dataTypes.GetItemSlot(new Item(itemType, count, nbt), itemPalette));
                SendPacket(PacketTypesOut.CreativeInventoryAction, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendChunkBatchReceived(float desiredNumberOfChunksPerBatch)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(dataTypes.GetFloat(desiredNumberOfChunksPerBatch));
                SendPacket(PacketTypesOut.ChunkBatchReceived, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendAcknowledgeConfiguration()
        {
            try
            {
                SendPacket(PacketTypesOut.AcknowledgeConfiguration, new List<byte>());
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool ClickContainerButton(int windowId, int buttonId)
        {
            try
            {
                var packet = new List<byte>
                {
                    (byte)windowId,
                    (byte)buttonId
                };
                SendPacket(PacketTypesOut.ClickWindowButton, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendPlaceRecipe(int windowId, string recipeId, bool makeAll)
        {
            try
            {
                List<byte> packet = new();
                if (protocolVersion < MC_1_13_Version)
                    return false;

                packet.AddRange(DataTypes.GetVarInt(windowId));
                if (protocolVersion >= MC_1_21_2_Version)
                    packet.AddRange(DataTypes.GetVarInt(int.Parse(recipeId, CultureInfo.InvariantCulture)));
                else
                    packet.AddRange(dataTypes.GetString(recipeId));
                packet.AddRange(dataTypes.GetBool(makeAll));
                SendPacket(PacketTypesOut.CraftRecipeRequest, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendAnimation(int animation, int playerId)
        {
            try
            {
                if (animation is not (0 or 1)) return false;

                List<byte> packet = new();

                switch (protocolVersion)
                {
                    case < MC_1_8_Version:
                        packet.AddRange(DataTypes.GetInt(playerId));
                        packet.Add(1); // Swing arm
                        break;
                    case < MC_1_9_Version:
                        // No fields in 1.8.X
                        break;
                    // MC 1.9+
                    default:
                        packet.AddRange(DataTypes.GetVarInt(animation));
                        break;
                }

                SendPacket(PacketTypesOut.Animation, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendCloseWindow(int windowId)
        {
            try
            {
                lock (window_actions)
                {
                    if (window_actions.ContainsKey(windowId))
                        window_actions[windowId] = 0;
                }

                SendPacket(PacketTypesOut.CloseWindow,
                    protocolVersion >= MC_1_21_2_Version
                        ? DataTypes.GetVarInt(windowId)
                        : new[] { (byte)windowId });
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendUpdateSign(Location sign, string line1, string line2, string line3, string line4,
            bool isFrontText = true)
        {
            try
            {
                if (line1.Length > 23)
                    line1 = line1[..23];
                if (line2.Length > 23)
                    line2 = line2[..23];
                if (line3.Length > 23)
                    line3 = line3[..23];
                if (line4.Length > 23)
                    line4 = line4[..23];

                List<byte> packet = new();
                packet.AddRange(dataTypes.GetLocation(sign));
                if (protocolVersion >= MC_1_20_Version)
                    packet.AddRange(dataTypes.GetBool(isFrontText));
                packet.AddRange(dataTypes.GetString(line1));
                packet.AddRange(dataTypes.GetString(line2));
                packet.AddRange(dataTypes.GetString(line3));
                packet.AddRange(dataTypes.GetString(line4));
                SendPacket(PacketTypesOut.UpdateSign, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool UpdateCommandBlock(Location location, string command, CommandBlockMode mode,
            CommandBlockFlags flags)
        {
            if (protocolVersion > MC_1_13_Version) return false;

            try
            {
                List<byte> packet = new();
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(dataTypes.GetString(command));
                packet.AddRange(DataTypes.GetVarInt((int)mode));
                packet.Add((byte)flags);
                SendPacket(PacketTypesOut.UpdateSign, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendWindowConfirmation(byte windowID, short actionID, bool accepted)
        {
            try
            {
                var packet = new List<byte>() { windowID };
                packet.AddRange(dataTypes.GetShort(actionID));
                packet.Add(accepted ? (byte)1 : (byte)0);
                SendPacket(PacketTypesOut.WindowConfirmation, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SelectTrade(int selectedSlot)
        {
            // MC 1.13 or greater
            if (protocolVersion < MC_1_13_Version) return false;

            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetVarInt(selectedSlot));
                SendPacket(PacketTypesOut.SelectTrade, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendSpectate(Guid uuid)
        {
            // MC 1.8 or greater
            if (protocolVersion < MC_1_8_Version) return false;

            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetUUID(uuid));
                SendPacket(PacketTypesOut.Spectate, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendPlayerSession(PlayerKeyPair? playerKeyPair)
        {
            if (playerKeyPair is null || !isOnlineMode)
                return false;

            if (protocolVersion >= MC_1_19_3_Version)
            {
                try
                {
                    List<byte> packet = new();

                    packet.AddRange(DataTypes.GetUUID(chatUuid));
                    packet.AddRange(DataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds()));
                    packet.AddRange(DataTypes.GetVarInt(playerKeyPair.PublicKey.Key.Length));
                    packet.AddRange(playerKeyPair.PublicKey.Key);
                    packet.AddRange(DataTypes.GetVarInt(playerKeyPair.PublicKey.SignatureV2!.Length));
                    packet.AddRange(playerKeyPair.PublicKey.SignatureV2);

                    log.Debug(
                        $"SendPlayerSession MessageUUID = {chatUuid.ToString()},  len(PublicKey) = {playerKeyPair.PublicKey.Key.Length}, len(SignatureV2) = {playerKeyPair.PublicKey.SignatureV2!.Length}");

                    SendPacket(PacketTypesOut.PlayerSession, packet);
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
                catch (System.IO.IOException)
                {
                    return false;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }

            return false;
        }

        public bool SendRenameItem(string itemName)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(dataTypes.GetString(itemName.Length > 50 ? itemName[..50] : itemName));
                SendPacket(PacketTypesOut.NameItem, packet);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendCookieResponse(string name, byte[]? data)
        {
            try
            {
                var packet = new List<byte>();
                var hasPayload = data is not null;
                packet.AddRange(dataTypes.GetString(name)); // Identifier
                packet.AddRange(dataTypes.GetBool(hasPayload)); // Has payload

                if (hasPayload)
                    packet.AddRange(dataTypes.GetArray(data!)); // Payload Data Array Size + Data Array

                switch (currentState)
                {
                    case CurrentState.Login:
                        SendPacket(0x04, packet);
                        break;

                    case CurrentState.Configuration:
                        SendPacket(ConfigurationPacketTypesOut.CookieResponse, packet);
                        break;

                    case CurrentState.Play:
                        SendPacket(PacketTypesOut.CookieResponse, packet);
                        break;
                }

                McClient.Instance?.DeleteCookie(name);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendKnownDataPacks(List<(string, string, string)> knownDataPacks)
        {
            try
            {
                var packet = new List<byte>();
                packet.AddRange(DataTypes.GetVarInt(knownDataPacks.Count)); // Known Packs Count
                foreach (var dataPack in knownDataPacks)
                {
                    packet.AddRange(dataTypes.GetString(dataPack.Item1));
                    packet.AddRange(dataTypes.GetString(dataPack.Item2));
                    packet.AddRange(dataTypes.GetString(dataPack.Item3));
                }

                switch (currentState)
                {
                    case CurrentState.Configuration:
                        SendPacket(ConfigurationPacketTypesOut.KnownDataPacks, packet);
                        break;

                    case CurrentState.Play:
                        SendPacket(PacketTypesOut.KnownDataPacks, packet);
                        break;
                }

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private byte[] GenerateSalt()
        {
            var salt = new byte[8];
            randomGen.GetNonZeroBytes(salt);
            return salt;
        }

        private static long GetNanos()
        {
            var nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }
    }

    internal enum CurrentState
    {
        Login = 0,
        Configuration,
        Play,
        Transfer
    }
}
