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
using static MinecraftClient.Settings;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.GeneralConfig;

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

        private int compression_treshold = 0;
        private int autocomplete_transaction_id = 0;
        private readonly Dictionary<int, short> window_actions = new();
        private CurrentState currentState = CurrentState.Login;
        private readonly int protocolVersion;
        private int currentDimension;
        private bool isOnlineMode = false;
        private readonly BlockingCollection<Tuple<int, Queue<byte>>> packetQueue = new();
        private float LastYaw, LastPitch;
        private long chunkBatchStartTime;
        private double aggregatedNanosPerChunk = 2000000.0;
        private int oldSamplesWeight = 1;

        private bool receiveDeclareCommands = false, receivePlayerInfo = false;
        private object MessageSigningLock = new();
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

        public Protocol18Handler(TcpClient Client, int protocolVersion, IMinecraftComHandler handler,
            ForgeInfo? forgeInfo)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            socketWrapper = new SocketWrapper(Client);
            dataTypes = new DataTypes(protocolVersion);
            this.protocolVersion = protocolVersion;
            this.handler = handler;
            pForge = new Protocol18Forge(forgeInfo, protocolVersion, dataTypes, this, handler);
            pTerrain = new Protocol18Terrain(protocolVersion, dataTypes, handler);
            packetPalette = new PacketTypeHandler(protocolVersion, forgeInfo != null).GetTypeHandler();
            log = handler.GetLogger();
            randomGen = RandomNumberGenerator.Create();
            lastSeenMessagesCollector = protocolVersion >= MC_1_19_3_Version ? new(20) : new(5);
            chunkBatchStartTime = GetNanos();

            if (handler.GetTerrainEnabled() && protocolVersion > MC_1_20_4_Version)
            {
                log.Error($"§c{Translations.extra_terrainandmovement_disabled}");
                handler.SetTerrainEnabled(false);
            }

            if (handler.GetInventoryEnabled() &&
                protocolVersion is < MC_1_8_Version or > MC_1_20_4_Version)
            {
                log.Error($"§c{Translations.extra_inventory_disabled}");
                handler.SetInventoryEnabled(false);
            }

            if (handler.GetEntityHandlingEnabled() &&
                protocolVersion is < MC_1_8_Version or > MC_1_20_4_Version)
            {
                log.Error($"§c{Translations.extra_entity_disabled}");
                handler.SetEntityHandlingEnabled(false);
            }

            Block.Palette = protocolVersion switch
            {
                // Block palette
                > MC_1_20_4_Version when handler.GetTerrainEnabled() =>
                    throw new NotImplementedException(Translations.exception_palette_block),
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
                > MC_1_20_4_Version when handler.GetEntityHandlingEnabled() =>
                    throw new NotImplementedException(Translations.exception_palette_entity),
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
                > MC_1_20_4_Version when handler.GetInventoryEnabled() =>
                    throw new NotImplementedException(Translations.exception_palette_item),
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
                Stopwatch stopWatch = new();
                while (!packetQueue.IsAddingCompleted)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    handler.OnUpdate();
                    stopWatch.Restart();

                    while (packetQueue.TryTake(out var packetInfo))
                    {
                        var (packetId, packetData) = packetInfo;
                        HandlePacket(packetId, packetData);

                        if (stopWatch.Elapsed.Milliseconds < 100) continue;

                        handler.OnUpdate();
                        stopWatch.Restart();
                    }

                    var sleepLength = 100 - stopWatch.Elapsed.Milliseconds;
                    if (sleepLength > 0)
                        Thread.Sleep(sleepLength);
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
                catch (Ionic.Zlib.ZlibException)
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
                && compression_treshold > 0)
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
                                return understood;

                            // Ignore other packets at this stage
                            default:
                                return true;
                        }

                        break;

                    // https://wiki.vg/Protocol#Configuration
                    case CurrentState.Configuration:
                        switch (packetPalette.GetIncomingConfigurationTypeById(packetId))
                        {
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
                                var registryCodec = dataTypes.ReadNextNbt(packetData);
                                ChatParser.ReadChatType(registryCodec);

                                if (handler.GetTerrainEnabled())
                                    World.StoreDimensionList(registryCodec);

                                break;

                            case ConfigurationPacketTypesIn.RemoveResourcePack:
                                if (dataTypes.ReadNextBool(packetData)) // Has UUID
                                    dataTypes.ReadNextUUID(packetData); // UUID
                                break;

                            case ConfigurationPacketTypesIn.ResourcePack:
                                HandleResourcePackPacket(packetData);
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

                throw new System.IO.InvalidDataException(
                    string.Format(Translations.exception_packet_process,
                        packetPalette.GetIncomingTypeById(packetId),
                        packetId,
                        protocolVersion,
                        currentState == CurrentState.Login,
                        innerException.GetType()),
                    innerException);
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
                    : Array.Empty<byte>();

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
                                    packetData); // Registry Codec (Dimension Codec) - 1.16 and above
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
                        var dimensionTypeName =
                            dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
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
                    }
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
                            verifyResult = player != null && player.VerifyMessage(signedChat, timestamp, salt,
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

                        var chatInfo = Json.ParseJson(chatName).Properties;
                        var senderDisplayName = chatInfo != null && chatInfo.Count > 0
                            ? (chatInfo.ContainsKey("insertion") ? chatInfo["insertion"] : chatInfo["text"])
                            .StringValue
                            : "";
                        string? senderTeamName = null;
                        var messageTypeEnum =
                            ChatParser.ChatId2Type!.GetValueOrDefault(chatTypeId, ChatParser.MessageType.CHAT);

                        if (targetName != null &&
                            (messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING ||
                             messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING))
                            senderTeamName = Json.ParseJson(targetName).Properties["with"].DataArray[0]
                                .Properties["text"].StringValue;

                        if (string.IsNullOrWhiteSpace(senderDisplayName))
                        {
                            var player = handler.GetPlayerInfo(senderUuid);
                            if (player != null && (player.DisplayName != null || player is { Name: not null }) &&
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
                            if (player == null || !player.IsMessageChainLegal())
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
                            if (player != null && (player.DisplayName != null || player.Name != null) &&
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
                        if (!isOnlineMode || messageSignature == null)
                            verifyResult = false;
                        else
                        {
                            if (senderUuid == handler.GetUserUuid())
                                verifyResult = true;
                            else
                            {
                                var player = handler.GetPlayerInfo(senderUuid);
                                if (player == null || !player.IsMessageChainLegal())
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

                        // TODO: Write a function to use this data ? But seems not too useful
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

                            if (player == null || !player.IsMessageChainLegal())
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
                                    case >= MC_1_19_Version:
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
                    // These always need to be read, since we need the field after them for teleport confirm
                    var location = new Location(
                        dataTypes.ReadNextDouble(packetData), // X
                        dataTypes.ReadNextDouble(packetData), // Y
                        dataTypes.ReadNextDouble(packetData) // Z
                    );

                    var yaw = dataTypes.ReadNextFloat(packetData);
                    var pitch = dataTypes.ReadNextFloat(packetData);
                    var locMask = dataTypes.ReadNextByte(packetData);

                    // entity handling require player pos for distance calculating
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

                    if (protocolVersion >= MC_1_9_Version)
                    {
                        var teleportId = dataTypes.ReadNextVarInt(packetData);

                        if (teleportId < 0)
                        {
                            yaw = LastYaw;
                            pitch = LastPitch;
                        }
                        else
                        {
                            LastYaw = yaw;
                            LastPitch = pitch;
                        }

                        handler.UpdateLocation(location, yaw, pitch);

                        // Teleport confirm packet
                        SendPacket(PacketTypesOut.TeleportConfirm, DataTypes.GetVarInt(teleportId));

                        if (Config.Main.Advanced.TemporaryFixBadpacket)
                        {
                            SendLocationUpdate(location, true, yaw, pitch, true);

                            if (teleportId == 1)
                                SendLocationUpdate(location, true, yaw, pitch, true);
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

                            dataTypes.ReadNextNbt(packetData); // Heightmaps

                            if (protocolVersion is MC_1_17_Version or MC_1_17_1_Version)
                            {
                                var biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length
                                for (var i = 0; i < biomesLength; i++)
                                    dataTypes.SkipNextVarInt(packetData); // Biomes
                            }

                            var dataSize = dataTypes.ReadNextVarInt(packetData); // Size

                            pTerrain.ProcessChunkColumnData(chunkX, chunkZ, verticalStripBitmask, packetData);
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
                        if (handler.GetWorld()[chunkX, chunkZ] != null)
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
                        var numberOfActions = dataTypes.ReadNextVarInt(packetData);
                        for (var i = 0; i < numberOfActions; i++)
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
                                if (playerGet == null)
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
                            if ((actionBitset & 1 << 5) <= 0) continue;
                            player.DisplayName = dataTypes.ReadNextBool(packetData)
                                ? dataTypes.ReadNextChat(packetData)
                                : null;
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
                                        displayName = dataTypes.ReadNextString(packetData); // Display name

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
                                        if (player != null)
                                            player.DisplayName = dataTypes.ReadNextString(packetData);
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
                            Container inventory = new(windowId, windowType, ChatParser.ParseText(title));
                            handler.OnInventoryOpen(windowId, inventory);
                        }
                    }

                    break;
                case PacketTypesIn.CloseWindow:
                    if (handler.GetInventoryEnabled())
                    {
                        var windowId = dataTypes.ReadNextByte(packetData);
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
                        var windowId = dataTypes.ReadNextByte(packetData);
                        var stateId = -1;
                        var elements = 0;

                        if (protocolVersion >= MC_1_17_1_Version)
                        {
                            // State ID and Elements as VarInt - 1.17.1 and above
                            stateId = dataTypes.ReadNextVarInt(packetData);
                            elements = dataTypes.ReadNextVarInt(packetData);
                        }
                        else
                        {
                            // Elements as Short - 1.17.0 and below
                            dataTypes.ReadNextShort(packetData);
                        }

                        Dictionary<int, Item> inventorySlots = new();
                        for (var slotId = 0; slotId < elements; slotId++)
                        {
                            var item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                            if (item != null)
                                inventorySlots[slotId] = item;
                        }

                        if (protocolVersion >= MC_1_17_1_Version) // Carried Item - 1.17.1 and above
                            dataTypes.ReadNextItemSlot(packetData, itemPalette);

                        handler.OnWindowItems(windowId, inventorySlots, stateId);
                    }

                    break;
                case PacketTypesIn.WindowProperty:
                    var containerId = dataTypes.ReadNextByte(packetData);
                    var propertyId = dataTypes.ReadNextShort(packetData);
                    var propertyValue = dataTypes.ReadNextShort(packetData);
                    handler.OnWindowProperties(containerId, propertyId, propertyValue);
                    break;
                case PacketTypesIn.SetSlot:
                    if (handler.GetInventoryEnabled())
                    {
                        var windowId = dataTypes.ReadNextByte(packetData);
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
                                var slot2 = bitsData >> 1;
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
                        var effectId = protocolVersion >= MC_1_18_2_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextByte(packetData);

                        if (Enum.TryParse(effectId.ToString(), out Effects effect))
                        {
                            var amplifier = dataTypes.ReadNextByte(packetData);
                            var duration = dataTypes.ReadNextVarInt(packetData);
                            var flags = dataTypes.ReadNextByte(packetData);
                            var hasFactorData = false;
                            Dictionary<string, object>? factorCodec = null;

                            if (protocolVersion >= MC_1_19_Version)
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
                            var propertyKey = dataTypes.ReadNextString(packetData);
                            var propertyValue2 = dataTypes.ReadNextDouble(packetData);

                            List<double> op0 = new();
                            List<double> op1 = new();
                            List<double> op2 = new();

                            var numberOfModifiers = dataTypes.ReadNextVarInt(packetData);
                            for (var j = 0; j < numberOfModifiers; j++)
                            {
                                dataTypes.ReadNextUUID(packetData);
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
                            keys.Add(propertyKey, propertyValue2);
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
                            > MC_1_20_4_Version => throw new NotImplementedException(Translations
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
                    var worldAge = dataTypes.ReadNextLong(packetData);
                    var timeOfDay = dataTypes.ReadNextLong(packetData);
                    handler.OnTimeUpdate(worldAge, timeOfDay);
                    break;
                case PacketTypesIn.EntityTeleport:
                    if (handler.GetEntityHandlingEnabled())
                    {
                        var entityId = dataTypes.ReadNextVarInt(packetData);
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

                        var entityYaw = dataTypes.ReadNextByte(packetData);
                        var entityPitch = dataTypes.ReadNextByte(packetData);
                        var isOnGround = dataTypes.ReadNextBool(packetData);
                        handler.OnEntityTeleport(entityId, x, y, z, isOnGround);
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
                    if (protocolVersion >= MC_1_19_3_Version)
                        explosionLocation = new(dataTypes.ReadNextDouble(packetData),
                            dataTypes.ReadNextDouble(packetData), dataTypes.ReadNextDouble(packetData));
                    else
                        explosionLocation = new(dataTypes.ReadNextFloat(packetData),
                            dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData));

                    var explosionStrength = dataTypes.ReadNextFloat(packetData);
                    var explosionBlockCount = protocolVersion >= MC_1_17_Version
                        ? dataTypes.ReadNextVarInt(packetData)
                        : dataTypes.ReadNextInt(packetData); // Record count

                    // Records
                    for (var i = 0; i < explosionBlockCount; i++)
                        dataTypes.ReadData(3, packetData);

                    // Maybe use in the future when the physics are implemented
                    dataTypes.ReadNextFloat(packetData); // Player Motion X
                    dataTypes.ReadNextFloat(packetData); // Player Motion Y
                    dataTypes.ReadNextFloat(packetData); // Player Motion Z

                    if (protocolVersion >= MC_1_20_4_Version)
                    {
                        dataTypes.ReadNextVarInt(packetData); // Block Interaction
                        dataTypes.ReadParticleData(packetData, itemPalette); // Small Explosion Particles
                        dataTypes.ReadParticleData(packetData, itemPalette); // Large Explosion Particles

                        // Explosion Sound
                        dataTypes.ReadNextString(packetData); // Sound Name
                        var hasFixedRange = dataTypes.ReadNextBool(packetData);
                        if (hasFixedRange)
                            dataTypes.ReadNextFloat(packetData); // Range
                    }

                    handler.OnExplosion(explosionLocation, explosionStrength, explosionBlockCount);
                    break;
                case PacketTypesIn.HeldItemChange:
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

                // Temporarily disabled until I find a fix
                /*case PacketTypesIn.BlockEntityData:
                    var location_ = dataTypes.ReadNextLocation(packetData);
                    var type_ = dataTypes.ReadNextInt(packetData);
                    var nbt = dataTypes.ReadNextNbt(packetData);
                    var nbtJson = JsonConvert.SerializeObject(nbt["messages"]);

                    //log.Info($"BLOCK ENTITY DATA -> {location_.ToString()} [{type_}] -> NBT: {nbtJson}");

                    break;*/

                case PacketTypesIn.SetTickingState:
                    dataTypes.ReadNextFloat(packetData);
                    dataTypes.ReadNextBool(packetData);
                    break;

                default:
                    return false; //Ignored packet
            }

            return true; //Packet processed
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
            return netMain != null ? netMain.Item1.ManagedThreadId : -1;
        }

        /// <summary>
        /// Disconnect from the server, cancel network reading.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (netMain != null)
                {
                    netMain.Item2.Cancel();
                }

                if (netReader != null)
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
            if (handler.GetNetworkPacketCaptureEnabled())
            {
                var clone = packetData.ToList();
                handler.OnNetworkPacket(packetId, clone, currentState == CurrentState.Login, false);
            }

            //log.Info($"[C -> S] Sending packet {packetId:X} > {dataTypes.ByteArrayToString(packetData.ToArray())}");

            //The inner packet
            var thePacket = dataTypes.ConcatBytes(DataTypes.GetVarInt(packetId), packetData.ToArray());

            if (compression_treshold > 0) //Compression enabled?
            {
                thePacket = thePacket.Length >= compression_treshold
                    ? dataTypes.ConcatBytes(DataTypes.GetVarInt(thePacket.Length), ZlibUtils.Compress(thePacket))
                    : dataTypes.ConcatBytes(DataTypes.GetVarInt(0), thePacket);
            }

            //log.Debug("[C -> S] Sending packet " + packetId + " > " + dataTypes.ByteArrayToString(dataTypes.ConcatBytes(dataTypes.GetVarInt(thePacket.Length), thePacket)));
            socketWrapper.SendDataRAW(dataTypes.ConcatBytes(DataTypes.GetVarInt(thePacket.Length), thePacket));
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login(PlayerKeyPair? playerKeyPair, SessionToken session)
        {
            // 1. Send the handshake packet
            SendPacket(0x00, dataTypes.ConcatBytes(
                    // Protocol Version
                    DataTypes.GetVarInt(protocolVersion),

                    // Server Address
                    dataTypes.GetString(pForge.GetServerAddress(handler.GetServerHost())),

                    // Server Port
                    dataTypes.GetUShort((ushort)handler.GetServerPort()),

                    // Next State
                    DataTypes.GetVarInt(2)) // 2 is for the Login state
            );

            // 2. Send the Login Start packet
            List<byte> fullLoginPacket = new();
            fullLoginPacket.AddRange(dataTypes.GetString(handler.GetUsername())); // Username

            // 1.19 - 1.19.2
            if (protocolVersion is >= MC_1_19_Version and < MC_1_19_3_Version)
            {
                if (playerKeyPair == null)
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
                        return StartEncryption(handler.GetUserUuidStr(), handler.GetSessionID(),
                            Config.Main.General.AccountType, token, serverId,
                            serverPublicKey, playerKeyPair, session);
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
            byte[] serverPublicKey, PlayerKeyPair? playerKeyPair, SessionToken session)
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
                if (playerKeyPair == null)
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
            var assumeCommand = new byte[] { 0x00 };
            var hasPosition = new byte[] { 0x00 };
            var tabCompletePacket = Array.Empty<byte>();

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
            var version = "";
            var tcp = ProxyHandler.NewTcpClient(host, port);
            tcp.ReceiveTimeout = 30000; // 30 seconds
            tcp.ReceiveBufferSize = 1024 * 1024;
            SocketWrapper socketWrapper = new(tcp);
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
            if (packetLength <= 0) return false;

            // Read the Packet Id
            var packetData = new Queue<byte>(socketWrapper.ReadDataRAW(packetLength));
            if (dataTypes.ReadNextVarInt(packetData) != 0x00) return false;

            var result = dataTypes.ReadNextString(packetData); // Get the Json data

            if (Config.Logging.DebugMessages)
            {
                // May contain formatting codes, cannot use WriteLineFormatted
                Console.ForegroundColor = ConsoleColor.DarkGray;
                ConsoleIO.WriteLine(result);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            if (string.IsNullOrEmpty(result) || !result.StartsWith("{") || !result.EndsWith("}")) return false;

            var jsonData = Json.ParseJson(result);
            if (jsonData.Type != Json.JSONData.DataType.Object || !jsonData.Properties.ContainsKey("version"))
                return false;

            var versionData = jsonData.Properties["version"];

            //Retrieve display name of the Minecraft version
            if (versionData.Properties.TryGetValue("name", out var property))
                version = property.StringValue;

            //Retrieve protocol version number for handling this server
            if (versionData.Properties.TryGetValue("protocol", out var dataProperty))
                protocolVersion = int.Parse(dataProperty.StringValue,
                    NumberStyles.Any, CultureInfo.CurrentCulture);

            // Check for forge on the server.
            Protocol18Forge.ServerInfoCheckForge(jsonData, ref forgeInfo);

            ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_server_protocol, version,
                protocolVersion + (forgeInfo != null ? Translations.mcc_with_forge : "")));

            return true;
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength() => protocolVersion > MC_1_10_Version
            ? 256
            : 100;

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
            if (entry == null) return;

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
        /// Send a chat command to the server - 1.19 and above
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="playerKeyPair">PlayerKeyPair</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatCommand(string command, PlayerKeyPair? playerKeyPair)
        {
            if (string.IsNullOrEmpty(command))
                return true;

            command = Regex.Replace(command, @"\s+", " ");
            command = Regex.Replace(command, @"\s$", string.Empty);

            log.Debug($"chat command = {command}");

            try
            {
                List<Tuple<string, string>>? needSigned = null; // List< Argument Name, Argument Value >
                if (playerKeyPair != null && isOnlineMode && protocolVersion >= MC_1_19_Version
                    && Config.Signature is { LoginWithSecureProfile: true, SignMessageInCommand: true })
                    needSigned = DeclareCommands.CollectSignArguments(command);

                lock (MessageSigningLock)
                {
                    var acknowledgment1192 =
                        protocolVersion == MC_1_19_2_Version ? ConsumeAcknowledgment() : null;

                    var (acknowledgment1193, bitset1193, messageCount1193) =
                        protocolVersion >= MC_1_19_3_Version
                            ? lastSeenMessagesCollector.Collect_1_19_3()
                            : new(Array.Empty<LastSeenMessageList.AcknowledgedMessage>(), Array.Empty<byte>(), 0);

                    List<byte> fields = new();

                    // Command: String
                    fields.AddRange(dataTypes.GetString(command));

                    // Timestamp: Instant(Long)
                    var timeNow = DateTimeOffset.UtcNow;
                    fields.AddRange(DataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                    if (needSigned == null || needSigned!.Count == 0)
                    {
                        fields.AddRange(DataTypes.GetLong(0)); // Salt: Long
                        fields.AddRange(DataTypes.GetVarInt(0)); // Signature Length: VarInt
                    }
                    else
                    {
                        var uuid = handler.GetUserUuid();
                        var salt = GenerateSalt();
                        fields.AddRange(salt); // Salt: Long
                        fields.AddRange(DataTypes.GetVarInt(needSigned.Count)); // Signature Length: VarInt

                        foreach (var (argName, message) in needSigned)
                        {
                            fields.AddRange(dataTypes.GetString(argName)); // Argument name: String

                            var sign = protocolVersion switch
                            {
                                MC_1_19_Version => playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow,
                                    ref salt),
                                MC_1_19_2_Version => playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow,
                                    ref salt, acknowledgment1192!.lastSeen),
                                _ => playerKeyPair!.PrivateKey.SignMessage(message, uuid, chatUuid, messageIndex++,
                                    timeNow, ref salt, acknowledgment1193)
                            };

                            if (protocolVersion <= MC_1_19_2_Version)
                                fields.AddRange(DataTypes.GetVarInt(sign.Length)); // Signature length: VarInt

                            fields.AddRange(sign); // Signature: Byte Array
                        }
                    }

                    if (protocolVersion <= MC_1_19_2_Version)
                        fields.AddRange(dataTypes.GetBool(false)); // Signed Preview: Boolean

                    switch (protocolVersion)
                    {
                        case MC_1_19_2_Version:
                            // Message Acknowledgment (1.19.2)
                            fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment1192!,
                                isOnlineMode && Config.Signature.LoginWithSecureProfile));
                            break;
                        case >= MC_1_19_3_Version:
                            // message count
                            fields.AddRange(DataTypes.GetVarInt(messageCount1193));

                            // Acknowledged: BitSet
                            fields.AddRange(bitset1193);
                            break;
                    }

                    SendPacket(PacketTypesOut.ChatCommand, fields);
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

                        if (!isOnlineMode || playerKeyPair == null || !Config.Signature.LoginWithSecureProfile ||
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
                SendPacket(PacketTypesOut.ClientStatus, new byte[] { 0 });
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
                    : new byte[] { chatMode });

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
        /// <param name="yaw">Optional new yaw for updating player look</param>
        /// <param name="pitch">Optional new pitch for updating player look</param>
        /// <returns>True if the location update was successfully sent</returns>
        public bool SendLocationUpdate(Location location, bool onGround, float? yaw, float? pitch)
        {
            return SendLocationUpdate(location, onGround, yaw, pitch, true);
        }

        public bool SendLocationUpdate(Location location, bool onGround, float? yaw = null, float? pitch = null,
            bool forceUpdate = false)
        {
            if (handler.GetTerrainEnabled())
            {
                var yawPitch = Array.Empty<byte>();
                var packetType = PacketTypesOut.PlayerPosition;

                if (Config.Main.Advanced.TemporaryFixBadpacket)
                {
                    if (yaw.HasValue && pitch.HasValue &&
                        (forceUpdate || yaw.Value != LastYaw || pitch.Value != LastPitch))
                    {
                        yawPitch = dataTypes.ConcatBytes(dataTypes.GetFloat(yaw.Value),
                            dataTypes.GetFloat(pitch.Value));
                        packetType = PacketTypesOut.PlayerPositionAndRotation;

                        LastYaw = yaw.Value;
                        LastPitch = pitch.Value;
                    }
                }
                else
                {
                    if (yaw.HasValue && pitch.HasValue)
                    {
                        yawPitch = dataTypes.ConcatBytes(dataTypes.GetFloat(yaw.Value),
                            dataTypes.GetFloat(pitch.Value));
                        packetType = PacketTypesOut.PlayerPositionAndRotation;

                        LastYaw = yaw.Value;
                        LastPitch = pitch.Value;
                    }
                }

                try
                {
                    SendPacket(packetType, dataTypes.ConcatBytes(
                        dataTypes.GetDouble(location.X),
                        dataTypes.GetDouble(location.Y),
                        protocolVersion < MC_1_8_Version
                            ? dataTypes.GetDouble(location.Y + 1.62)
                            : Array.Empty<byte>(),
                        dataTypes.GetDouble(location.Z),
                        yawPitch,
                        new byte[] { onGround ? (byte)1 : (byte)0 })
                    );
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

                switch (protocolVersion)
                {
                    case < MC_1_9_Version:
                        packet.AddRange(dataTypes.GetLocation(location));
                        packet.Add(dataTypes.GetBlockFace(face));

                        var playerInventory = handler.GetInventory(0);

                        if (playerInventory?.Items is null)
                            return false;

                        var slotWindowIds = new int[]{ 36, 37, 38, 39, 40, 41, 42, 43, 44 }; 
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
                
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorX
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorY
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorZ
                
                if(protocolVersion >= MC_1_14_Version)
                    packet.Add(0); // insideBlock = false
                
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

                List<byte> packet = new()
                {
                    (byte)windowId // Window ID
                };

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
                        packet.AddRange(dataTypes.GetItemSlot(slot.Item2, itemPalette)); // slot Data
                    }
                }

                packet.AddRange(dataTypes.GetItemSlot(item, itemPalette)); // Carried item (Clicked item)

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

                SendPacket(PacketTypesOut.CloseWindow, new[] { (byte)windowId });
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
            if (playerKeyPair == null || !isOnlineMode)
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
        Play
    }
}