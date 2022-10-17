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
using MinecraftClient.Protocol.Keys;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using static MinecraftClient.Settings;

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

        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private int autocomplete_transaction_id = 0;
        private readonly List<string> autocomplete_result = new();
        private readonly Dictionary<int, short> window_actions = new();
        private bool login_phase = true;
        private readonly int protocolVersion;
        private int currentDimension;
        private bool isOnlineMode = false;
        private readonly BlockingCollection<Tuple<int, Queue<byte>>> packetQueue = new();
        private float LastYaw, LastPitch;

        private int pendingAcknowledgments = 0;
        private readonly LastSeenMessagesCollector lastSeenMessagesCollector = new(5);
        private LastSeenMessageList.Entry? lastReceivedMessage = null;
        readonly Protocol18Forge pForge;
        readonly Protocol18Terrain pTerrain;
        readonly IMinecraftComHandler handler;
        readonly EntityPalette entityPalette;
        readonly ItemPalette itemPalette;
        readonly PacketTypePalette packetPalette;
        readonly SocketWrapper socketWrapper;
        readonly DataTypes dataTypes;
        Tuple<Thread, CancellationTokenSource>? netMain = null; // main thread
        Tuple<Thread, CancellationTokenSource>? netReader = null; // reader thread
        readonly ILogger log;
        readonly RandomNumberGenerator randomGen;

        public Protocol18Handler(TcpClient Client, int protocolVersion, IMinecraftComHandler handler, ForgeInfo? forgeInfo)
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

            if (handler.GetTerrainEnabled() && protocolVersion > MC_1_19_2_Version)
            {
                log.Error(Translations.Get("extra.terrainandmovement_disabled"));
                handler.SetTerrainEnabled(false);
            }

            if (handler.GetInventoryEnabled() && (protocolVersion < MC_1_10_Version || protocolVersion > MC_1_19_2_Version))
            {
                log.Error(Translations.Get("extra.inventory_disabled"));
                handler.SetInventoryEnabled(false);
            }
            if (handler.GetEntityHandlingEnabled() && (protocolVersion < MC_1_10_Version || protocolVersion > MC_1_19_2_Version))
            {
                log.Error(Translations.Get("extra.entity_disabled"));
                handler.SetEntityHandlingEnabled(false);
            }

            // Block palette
            if (protocolVersion > MC_1_19_2_Version && handler.GetTerrainEnabled())
                throw new NotImplementedException(Translations.Get("exception.palette.block"));

            if (protocolVersion >= MC_1_19_Version)
                Block.Palette = new Palette119();
            else if (protocolVersion >= MC_1_17_Version)
                Block.Palette = new Palette117();
            else if (protocolVersion >= MC_1_16_Version)
                Block.Palette = new Palette116();
            else if (protocolVersion >= MC_1_15_Version)
                Block.Palette = new Palette115();
            else if (protocolVersion >= MC_1_14_Version)
                Block.Palette = new Palette114();
            else if (protocolVersion >= MC_1_13_Version)
                Block.Palette = new Palette113();
            else
                Block.Palette = new Palette112();

            // Entity palette
            if (protocolVersion > MC_1_19_2_Version && handler.GetEntityHandlingEnabled())
                throw new NotImplementedException(Translations.Get("exception.palette.entity"));

            if (protocolVersion >= MC_1_19_Version)
                entityPalette = new EntityPalette119();
            else if (protocolVersion >= MC_1_17_Version)
                entityPalette = new EntityPalette117();
            else if (protocolVersion >= MC_1_16_2_Version)
                entityPalette = new EntityPalette1162();
            else if (protocolVersion >= MC_1_16_Version)
                entityPalette = new EntityPalette1161();
            else if (protocolVersion >= MC_1_15_Version)
                entityPalette = new EntityPalette115();
            else if (protocolVersion >= MC_1_14_Version)
                entityPalette = new EntityPalette114();
            else if (protocolVersion >= MC_1_13_Version)
                entityPalette = new EntityPalette113();
            else
                entityPalette = new EntityPalette112();

            // Item palette
            if (protocolVersion > MC_1_19_2_Version && handler.GetInventoryEnabled())
                throw new NotImplementedException(Translations.Get("exception.palette.item"));

            if (protocolVersion >= MC_1_19_Version)
                itemPalette = new ItemPalette119();
            else if (protocolVersion >= MC_1_18_1_Version)
                itemPalette = new ItemPalette118();
            else if (protocolVersion >= MC_1_17_Version)
                itemPalette = new ItemPalette117();
            else if (protocolVersion >= MC_1_16_2_Version)
                itemPalette = new ItemPalette1162();
            else if (protocolVersion >= MC_1_16_1_Version)
                itemPalette = new ItemPalette1161();
            else
                itemPalette = new ItemPalette115();

            // MessageType 
            // You can find it in https://wiki.vg/Protocol#Player_Chat_Message or /net/minecraft/network/message/MessageType.java
            if (this.protocolVersion >= MC_1_19_2_Version)
                ChatParser.ChatId2Type = new()
                {
                    { 0,  ChatParser.MessageType.CHAT },
                    { 1,  ChatParser.MessageType.SAY_COMMAND },
                    { 2,  ChatParser.MessageType.MSG_COMMAND_INCOMING },
                    { 3,  ChatParser.MessageType.MSG_COMMAND_OUTGOING },
                    { 4,  ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING },
                    { 5,  ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING },
                    { 6,  ChatParser.MessageType.EMOTE_COMMAND },
                };
            else if (this.protocolVersion == MC_1_19_Version)
                ChatParser.ChatId2Type = new()
                {
                    { 0,  ChatParser.MessageType.CHAT },
                    { 1,  ChatParser.MessageType.RAW_MSG },
                    { 2,  ChatParser.MessageType.RAW_MSG },
                    { 3,  ChatParser.MessageType.SAY_COMMAND },
                    { 4,  ChatParser.MessageType.MSG_COMMAND_INCOMING },
                    { 5,  ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING },
                    { 6,  ChatParser.MessageType.EMOTE_COMMAND },
                    { 7,  ChatParser.MessageType.RAW_MSG },
                };
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>
        private void Updater(object? o)
        {
            CancellationToken cancelToken = (CancellationToken)o!;

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

                    while (packetQueue.TryTake(out Tuple<int, Queue<byte>>? packetInfo))
                    {
                        (int packetID, Queue<byte> packetData) = packetInfo;
                        HandlePacket(packetID, packetData);

                        if (stopWatch.Elapsed.Milliseconds >= 100)
                        {
                            handler.OnUpdate();
                            stopWatch.Restart();
                        }
                    }

                    int sleepLength = 100 - stopWatch.Elapsed.Milliseconds;
                    if (sleepLength > 0)
                        Thread.Sleep(sleepLength);
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (NullReferenceException) { }

            if (cancelToken.IsCancellationRequested)
                return;

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        /// <summary>
        /// Read and decompress packets.
        /// </summary>
        internal void PacketReader(object? o)
        {
            CancellationToken cancelToken = (CancellationToken)o!;
            while (socketWrapper.IsConnected() && !cancelToken.IsCancellationRequested)
            {
                try
                {
                    while (socketWrapper.HasDataAvailable())
                    {
                        packetQueue.Add(ReadNextPacket());

                        if (cancelToken.IsCancellationRequested)
                            break;
                    }
                }
                catch (System.IO.IOException) { break; }
                catch (SocketException) { break; }
                catch (NullReferenceException) { break; }
                catch (Ionic.Zlib.ZlibException) { break; }

                if (cancelToken.IsCancellationRequested)
                    break;

                Thread.Sleep(10);
            }
            packetQueue.CompleteAdding();
        }

        /// <summary>
        /// Read the next packet from the network
        /// </summary>
        /// <param name="packetID">will contain packet ID</param>
        /// <param name="packetData">will contain raw packet Data</param>
        internal Tuple<int, Queue<byte>> ReadNextPacket()
        {
            int size = dataTypes.ReadNextVarIntRAW(socketWrapper); //Packet size
            Queue<byte> packetData = new(socketWrapper.ReadDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (protocolVersion >= MC_1_8_Version
                && compression_treshold > 0)
            {
                int sizeUncompressed = dataTypes.ReadNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData = new(uncompressed);
                }
            }

            int packetID = dataTypes.ReadNextVarInt(packetData); //Packet ID

            if (handler.GetNetworkPacketCaptureEnabled())
            {
                List<byte> clone = packetData.ToList();
                handler.OnNetworkPacket(packetID, clone, login_phase, true);
            }

            return new(packetID, packetData);
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        internal bool HandlePacket(int packetID, Queue<byte> packetData)
        {
            try
            {
                if (login_phase)
                {
                    switch (packetID) //Packet IDs are different while logging in
                    {
                        case 0x03:
                            if (protocolVersion >= MC_1_8_Version)
                                compression_treshold = dataTypes.ReadNextVarInt(packetData);
                            break;
                        case 0x04:
                            int messageId = dataTypes.ReadNextVarInt(packetData);
                            string channel = dataTypes.ReadNextString(packetData);
                            List<byte> responseData = new();
                            bool understood = pForge.HandleLoginPluginRequest(channel, packetData, ref responseData);
                            SendLoginPluginResponse(messageId, understood, responseData.ToArray());
                            return understood;
                        default:
                            return false; //Ignored packet
                    }
                }
                // Regular in-game packets
                else switch (packetPalette.GetIncommingTypeById(packetID))
                    {
                        case PacketTypesIn.KeepAlive:
                            SendPacket(PacketTypesOut.KeepAlive, packetData);
                            handler.OnServerKeepAlive();
                            break;
                        case PacketTypesIn.Ping:
                            SendPacket(PacketTypesOut.Pong, packetData);
                            break;
                        case PacketTypesIn.JoinGame:
                            handler.OnGameJoined();
                            int playerEntityID = dataTypes.ReadNextInt(packetData);
                            handler.OnReceivePlayerEntityID(playerEntityID);

                            if (protocolVersion >= MC_1_16_2_Version)
                                dataTypes.ReadNextBool(packetData);                       // Is hardcore - 1.16.2 and above

                            handler.OnGamemodeUpdate(Guid.Empty, dataTypes.ReadNextByte(packetData));

                            if (protocolVersion >= MC_1_16_Version)
                            {
                                dataTypes.ReadNextByte(packetData);                       // Previous Gamemode - 1.16 and above
                                int worldCount = dataTypes.ReadNextVarInt(packetData);    // Dimension Count (World Count) - 1.16 and above
                                for (int i = 0; i < worldCount; i++)
                                    dataTypes.ReadNextString(packetData);                 // Dimension Names (World Names) - 1.16 and above
                                var registryCodec = dataTypes.ReadNextNbt(packetData);    // Registry Codec (Dimension Codec) - 1.16 and above
                                if (handler.GetTerrainEnabled())
                                    World.StoreDimensionList(registryCodec);
                            }

                            // Current dimension
                            //   String: 1.19 and above
                            //   NBT Tag Compound: [1.16.2 to 1.18.2]
                            //   String identifier: 1.16 and 1.16.1
                            //   varInt: [1.9.1 to 1.15.2]
                            //   byte: below 1.9.1
                            string? dimensionTypeName = null;
                            Dictionary<string, object>? dimensionType = null;
                            if (protocolVersion >= MC_1_16_Version)
                            {
                                if (protocolVersion >= MC_1_19_Version)
                                    dimensionTypeName = dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                else if (protocolVersion >= MC_1_16_2_Version)
                                    dimensionType = dataTypes.ReadNextNbt(packetData);        // Dimension Type: NBT Tag Compound
                                else
                                    dataTypes.ReadNextString(packetData);
                                currentDimension = 0;
                            }
                            else if (protocolVersion >= MC_1_9_1_Version)
                                currentDimension = dataTypes.ReadNextInt(packetData);
                            else
                                currentDimension = (sbyte)dataTypes.ReadNextByte(packetData);

                            if (protocolVersion < MC_1_14_Version)
                                dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below

                            if (protocolVersion >= MC_1_16_Version)
                            {
                                string dimensionName = dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above
                                if (handler.GetTerrainEnabled())
                                {
                                    if (protocolVersion >= MC_1_16_2_Version && protocolVersion <= MC_1_18_2_Version)
                                    {
                                        World.StoreOneDimension(dimensionName, dimensionType!);
                                        World.SetDimension(dimensionName);
                                    }
                                    else if (protocolVersion >= MC_1_19_Version)
                                    {
                                        World.SetDimension(dimensionTypeName!);
                                    }
                                }
                            }

                            if (protocolVersion >= MC_1_15_Version)
                                dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above
                            if (protocolVersion >= MC_1_16_2_Version)
                                dataTypes.ReadNextVarInt(packetData);         // Max Players - 1.16.2 and above
                            else
                                dataTypes.ReadNextByte(packetData);           // Max Players - 1.16.1 and below
                            if (protocolVersion < MC_1_16_Version)
                                dataTypes.SkipNextString(packetData);         // Level Type - 1.15 and below
                            if (protocolVersion >= MC_1_14_Version)
                                dataTypes.ReadNextVarInt(packetData);         // View distance - 1.14 and above
                            if (protocolVersion >= MC_1_18_1_Version)
                                dataTypes.ReadNextVarInt(packetData);         // Simulation Distance - 1.18 and above
                            if (protocolVersion >= MC_1_8_Version)
                                dataTypes.ReadNextBool(packetData);           // Reduced debug info - 1.8 and above
                            if (protocolVersion >= MC_1_15_Version)
                                dataTypes.ReadNextBool(packetData);           // Enable respawn screen - 1.15 and above
                            if (protocolVersion >= MC_1_16_Version)
                            {
                                dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                                dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                            }
                            if (protocolVersion >= MC_1_19_Version)
                            {
                                bool hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                                if (hasDeathLocation)
                                {
                                    dataTypes.SkipNextString(packetData); // Death dimension name: Identifier
                                    dataTypes.ReadNextLocation(packetData); // Death location
                                }
                            }
                            break;
                        case PacketTypesIn.DeclareCommands:
                            if (protocolVersion >= MC_1_19_Version)
                                DeclareCommands.Read(dataTypes, packetData);
                            break;
                        case PacketTypesIn.ChatMessage:
                            int messageType = 0;

                            if (protocolVersion <= MC_1_18_2_Version) // 1.18 and bellow
                            {
                                string message = dataTypes.ReadNextString(packetData);

                                Guid senderUUID;
                                if (protocolVersion >= MC_1_8_Version)
                                {
                                    //Hide system messages or xp bar messages?
                                    messageType = dataTypes.ReadNextByte(packetData);
                                    if ((messageType == 1 && !Config.Main.Advanced.ShowSystemMessages)
                                        || (messageType == 2 && !Config.Main.Advanced.ShowSystemMessages))
                                        break;

                                    if (protocolVersion >= MC_1_16_5_Version)
                                        senderUUID = dataTypes.ReadNextUUID(packetData);
                                    else senderUUID = Guid.Empty;
                                }
                                else
                                    senderUUID = Guid.Empty;

                                handler.OnTextReceived(new(message, true, messageType, senderUUID));
                            }
                            else if (protocolVersion == MC_1_19_Version) // 1.19
                            {
                                string signedChat = dataTypes.ReadNextString(packetData);

                                bool hasUnsignedChatContent = dataTypes.ReadNextBool(packetData);
                                string? unsignedChatContent = hasUnsignedChatContent ? dataTypes.ReadNextString(packetData) : null;

                                messageType = dataTypes.ReadNextVarInt(packetData);
                                if ((messageType == 1 && !Config.Main.Advanced.ShowSystemMessages)
                                        || (messageType == 2 && !Config.Main.Advanced.ShowXPBarMessages))
                                    break;

                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                string senderDisplayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData));

                                bool hasSenderTeamName = dataTypes.ReadNextBool(packetData);
                                string? senderTeamName = hasSenderTeamName ? ChatParser.ParseText(dataTypes.ReadNextString(packetData)) : null;

                                long timestamp = dataTypes.ReadNextLong(packetData);

                                long salt = dataTypes.ReadNextLong(packetData);

                                byte[] messageSignature = dataTypes.ReadNextByteArray(packetData);

                                bool verifyResult;
                                if (!isOnlineMode)
                                    verifyResult = false;
                                else if (senderUUID == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    verifyResult = player != null && player.VerifyMessage(signedChat, timestamp, salt, ref messageSignature);
                                }

                                ChatMessage chat = new(signedChat, true, messageType, senderUUID, unsignedChatContent, senderDisplayName, senderTeamName, timestamp, messageSignature, verifyResult);
                                handler.OnTextReceived(chat);
                            }
                            else // 1.19.1 +
                            {
                                byte[]? precedingSignature = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextByteArray(packetData) : null;
                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                byte[] headerSignature = dataTypes.ReadNextByteArray(packetData);

                                string signedChat = dataTypes.ReadNextString(packetData);
                                string? decorated = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextString(packetData) : null;

                                long timestamp = dataTypes.ReadNextLong(packetData);
                                long salt = dataTypes.ReadNextLong(packetData);

                                int lastSeenMessageListLen = dataTypes.ReadNextVarInt(packetData);
                                LastSeenMessageList.Entry[] lastSeenMessageList = new LastSeenMessageList.Entry[lastSeenMessageListLen];
                                for (int i = 0; i < lastSeenMessageListLen; ++i)
                                {
                                    Guid user = dataTypes.ReadNextUUID(packetData);
                                    byte[] lastSignature = dataTypes.ReadNextByteArray(packetData);
                                    lastSeenMessageList[i] = new(user, lastSignature);
                                }
                                LastSeenMessageList lastSeenMessages = new(lastSeenMessageList);

                                string? unsignedChatContent = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextString(packetData) : null;

                                int filterEnum = dataTypes.ReadNextVarInt(packetData);
                                if (filterEnum == 2) // PARTIALLY_FILTERED
                                    dataTypes.ReadNextULongArray(packetData);

                                int chatTypeId = dataTypes.ReadNextVarInt(packetData);
                                string chatName = dataTypes.ReadNextString(packetData);
                                string? targetName = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextString(packetData) : null;

                                Dictionary<string, Json.JSONData> chatInfo = Json.ParseJson(chatName).Properties;
                                string senderDisplayName = (chatInfo.ContainsKey("insertion") ? chatInfo["insertion"] : chatInfo["text"]).StringValue;
                                string? senderTeamName = null;
                                ChatParser.MessageType messageTypeEnum = ChatParser.ChatId2Type!.GetValueOrDefault(chatTypeId, ChatParser.MessageType.CHAT);
                                if (targetName != null &&
                                    (messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING || messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING))
                                    senderTeamName = Json.ParseJson(targetName).Properties["with"].DataArray[0].Properties["text"].StringValue;

                                if (string.IsNullOrWhiteSpace(senderDisplayName))
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    if (player != null && (player.DisplayName != null || player.Name != null) && string.IsNullOrWhiteSpace(senderDisplayName))
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
                                else if (senderUUID == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    if (player == null || !player.IsMessageChainLegal())
                                        verifyResult = false;
                                    else
                                    {
                                        bool lastVerifyResult = player.IsMessageChainLegal();
                                        verifyResult = player.VerifyMessage(signedChat, timestamp, salt, ref headerSignature, ref precedingSignature, lastSeenMessages);
                                        if (lastVerifyResult && !verifyResult)
                                            log.Warn(Translations.Get("chat.message_chain_broken", senderDisplayName));
                                    }
                                }

                                ChatMessage chat = new(signedChat, false, chatTypeId, senderUUID, unsignedChatContent, senderDisplayName, senderTeamName, timestamp, headerSignature, verifyResult);
                                if (isOnlineMode && !chat.LacksSender())
                                    Acknowledge(chat);
                                handler.OnTextReceived(chat);
                            }
                            break;
                        case PacketTypesIn.MessageHeader:
                            if (protocolVersion >= MC_1_19_2_Version)
                            {
                                byte[]? precedingSignature = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextByteArray(packetData) : null;
                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                byte[] headerSignature = dataTypes.ReadNextByteArray(packetData);
                                byte[] bodyDigest = dataTypes.ReadNextByteArray(packetData);

                                bool verifyResult;

                                if (!isOnlineMode)
                                    verifyResult = false;
                                else if (senderUUID == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);

                                    if (player == null || !player.IsMessageChainLegal())
                                        verifyResult = false;
                                    else
                                    {
                                        bool lastVerifyResult = player.IsMessageChainLegal();
                                        verifyResult = player.VerifyMessageHead(ref precedingSignature, ref headerSignature, ref bodyDigest);
                                        if (lastVerifyResult && !verifyResult)
                                            log.Warn(Translations.Get("chat.message_chain_broken", player.Name));
                                    }
                                }
                            }
                            break;
                        case PacketTypesIn.Respawn:
                            string? dimensionTypeNameRespawn = null;
                            Dictionary<string, object>? dimensionTypeRespawn = null;
                            if (protocolVersion >= MC_1_16_Version)
                            {
                                if (protocolVersion >= MC_1_19_Version)
                                    dimensionTypeNameRespawn = dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                else if (protocolVersion >= MC_1_16_2_Version)
                                    dimensionTypeRespawn = dataTypes.ReadNextNbt(packetData);        // Dimension Type: NBT Tag Compound
                                else
                                    dataTypes.ReadNextString(packetData);
                                currentDimension = 0;
                            }
                            else
                            {   // 1.15 and below
                                currentDimension = dataTypes.ReadNextInt(packetData);
                            }

                            if (protocolVersion >= MC_1_16_Version)
                            {
                                string dimensionName = dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above
                                if (handler.GetTerrainEnabled())
                                {
                                    if (protocolVersion >= MC_1_16_2_Version && protocolVersion <= MC_1_18_2_Version)
                                    {
                                        World.StoreOneDimension(dimensionName, dimensionTypeRespawn!);
                                        World.SetDimension(dimensionName);
                                    }
                                    else if (protocolVersion >= MC_1_19_Version)
                                    {
                                        World.SetDimension(dimensionTypeNameRespawn!);
                                    }
                                }
                            }

                            if (protocolVersion < MC_1_14_Version)
                                dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
                            if (protocolVersion >= MC_1_15_Version)
                                dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above
                            dataTypes.ReadNextByte(packetData);               // Gamemode
                            if (protocolVersion >= MC_1_16_Version)
                                dataTypes.ReadNextByte(packetData);           // Previous Game mode - 1.16 and above
                            if (protocolVersion < MC_1_16_Version)
                                dataTypes.SkipNextString(packetData);         // Level Type - 1.15 and below
                            if (protocolVersion >= MC_1_16_Version)
                            {
                                dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                                dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                                dataTypes.ReadNextBool(packetData);           // Copy metadata - 1.16 and above
                            }
                            if (protocolVersion >= MC_1_19_Version)
                            {
                                bool hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                                if (hasDeathLocation)
                                {
                                    dataTypes.ReadNextString(packetData);     // Death dimension name: Identifier
                                    dataTypes.ReadNextLocation(packetData);   // Death location
                                }
                            }
                            handler.OnRespawn();
                            break;
                        case PacketTypesIn.PlayerPositionAndLook:
                            {
                                // These always need to be read, since we need the field after them for teleport confirm
                                double x = dataTypes.ReadNextDouble(packetData);
                                double y = dataTypes.ReadNextDouble(packetData);
                                double z = dataTypes.ReadNextDouble(packetData);
                                Location location = new(x, y, z);
                                float yaw = dataTypes.ReadNextFloat(packetData);
                                float pitch = dataTypes.ReadNextFloat(packetData);
                                byte locMask = dataTypes.ReadNextByte(packetData);

                                // entity handling require player pos for distance calculating
                                if (handler.GetTerrainEnabled() || handler.GetEntityHandlingEnabled())
                                {
                                    if (protocolVersion >= MC_1_8_Version)
                                    {
                                        Location current = handler.GetCurrentLocation();
                                        location.X = (locMask & 1 << 0) != 0 ? current.X + x : x;
                                        location.Y = (locMask & 1 << 1) != 0 ? current.Y + y : y;
                                        location.Z = (locMask & 1 << 2) != 0 ? current.Z + z : z;
                                    }
                                }

                                if (protocolVersion >= MC_1_9_Version)
                                {
                                    int teleportID = dataTypes.ReadNextVarInt(packetData);

                                    if (teleportID < 0) { yaw = LastYaw; pitch = LastPitch; }
                                    else { LastYaw = yaw; LastPitch = pitch; }

                                    handler.UpdateLocation(location, yaw, pitch);

                                    // Teleport confirm packet
                                    SendPacket(PacketTypesOut.TeleportConfirm, dataTypes.GetVarInt(teleportID));
                                    SendLocationUpdate(location, true, yaw, pitch, true);
                                    if (teleportID == 1)
                                        SendLocationUpdate(location, true, yaw, pitch, true);
                                }
                                else
                                {
                                    handler.UpdateLocation(location, yaw, pitch);
                                    LastYaw = yaw; LastPitch = pitch;
                                }

                                if (protocolVersion >= MC_1_17_Version)
                                    dataTypes.ReadNextBool(packetData); // Dismount Vehicle    - 1.17 and above
                            }
                            break;
                        case PacketTypesIn.ChunkData:
                            if (handler.GetTerrainEnabled())
                            {
                                Interlocked.Increment(ref handler.GetWorld().chunkCnt);
                                Interlocked.Increment(ref handler.GetWorld().chunkLoadNotCompleted);

                                int chunkX = dataTypes.ReadNextInt(packetData);
                                int chunkZ = dataTypes.ReadNextInt(packetData);
                                if (protocolVersion >= MC_1_17_Version)
                                {
                                    ulong[]? verticalStripBitmask = null;

                                    if (protocolVersion == MC_1_17_Version || protocolVersion == MC_1_17_1_Version)
                                        verticalStripBitmask = dataTypes.ReadNextULongArray(packetData); // Bit Mask Length  and  Primary Bit Mask

                                    dataTypes.ReadNextNbt(packetData); // Heightmaps

                                    if (protocolVersion == MC_1_17_Version || protocolVersion == MC_1_17_1_Version)
                                    {
                                        int biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length
                                        for (int i = 0; i < biomesLength; i++)
                                            dataTypes.SkipNextVarInt(packetData); // Biomes
                                    }

                                    int dataSize = dataTypes.ReadNextVarInt(packetData); // Size

                                    pTerrain.ProcessChunkColumnData(chunkX, chunkZ, verticalStripBitmask, packetData);
                                    Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);

                                    // Block Entity data: ignored
                                    // Light data: ignored
                                }
                                else
                                {
                                    bool chunksContinuous = dataTypes.ReadNextBool(packetData);
                                    if (protocolVersion >= MC_1_16_Version && protocolVersion <= MC_1_16_1_Version)
                                        dataTypes.ReadNextBool(packetData); // Ignore old data - 1.16 to 1.16.1 only
                                    ushort chunkMask = protocolVersion >= MC_1_9_Version
                                        ? (ushort)dataTypes.ReadNextVarInt(packetData)
                                        : dataTypes.ReadNextUShort(packetData);
                                    if (protocolVersion < MC_1_8_Version)
                                    {
                                        ushort addBitmap = dataTypes.ReadNextUShort(packetData);
                                        int compressedDataSize = dataTypes.ReadNextInt(packetData);
                                        byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                                        byte[] decompressed = ZlibUtils.Decompress(compressed);

                                        pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap, currentDimension == 0, chunksContinuous, currentDimension, new Queue<byte>(decompressed));
                                        Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);
                                    }
                                    else
                                    {
                                        if (protocolVersion >= MC_1_14_Version)
                                            dataTypes.ReadNextNbt(packetData);  // Heightmaps - 1.14 and above
                                        int biomesLength = 0;
                                        if (protocolVersion >= MC_1_16_2_Version)
                                            if (chunksContinuous)
                                                biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length - 1.16.2 and above
                                        if (protocolVersion >= MC_1_15_Version && chunksContinuous)
                                        {
                                            if (protocolVersion >= MC_1_16_2_Version)
                                            {
                                                for (int i = 0; i < biomesLength; i++)
                                                {
                                                    // Biomes - 1.16.2 and above
                                                    // Don't use ReadNextVarInt because it cost too much time
                                                    dataTypes.SkipNextVarInt(packetData);
                                                }
                                            }
                                            else dataTypes.DropData(1024 * 4, packetData); // Biomes - 1.15 and above
                                        }
                                        int dataSize = dataTypes.ReadNextVarInt(packetData);

                                        pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, currentDimension, packetData);
                                        Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);
                                    }
                                }
                            }
                            break;
                        case PacketTypesIn.MapData:
                            if (protocolVersion < MC_1_8_Version)
                                break;

                            int mapid = dataTypes.ReadNextVarInt(packetData);
                            byte scale = dataTypes.ReadNextByte(packetData);


                            // 1.9 +
                            bool trackingPosition = true;

                            // 1.14+
                            bool locked = false;

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

                            int iconcount = 0;
                            List<MapIcon> icons = new();

                            // 1,9 + = needs tracking position to be true to get the icons
                            if (protocolVersion <= MC_1_16_5_Version || trackingPosition)
                            {
                                iconcount = dataTypes.ReadNextVarInt(packetData);

                                for (int i = 0; i < iconcount; i++)
                                {
                                    MapIcon mapIcon = new();

                                    // 1.8 - 1.13
                                    if (protocolVersion < MC_1_13_2_Version)
                                    {
                                        byte directionAndtype = dataTypes.ReadNextByte(packetData);
                                        byte direction, type;

                                        // 1.12.2+
                                        if (protocolVersion >= MC_1_12_2_Version)
                                        {
                                            direction = (byte)(directionAndtype & 0xF);
                                            type = (byte)((directionAndtype >> 4) & 0xF);
                                        }
                                        else // 1.8 - 1.12
                                        {
                                            direction = (byte)((directionAndtype >> 4) & 0xF);
                                            type = (byte)(directionAndtype & 0xF);
                                        }

                                        mapIcon.Type = (MapIconType)type;
                                        mapIcon.Direction = direction;
                                    }

                                    // 1.13.2+
                                    if (protocolVersion >= MC_1_13_2_Version)
                                        mapIcon.Type = (MapIconType)dataTypes.ReadNextVarInt(packetData);

                                    mapIcon.X = dataTypes.ReadNextByte(packetData);
                                    mapIcon.Z = dataTypes.ReadNextByte(packetData);

                                    // 1.13.2+
                                    if (protocolVersion >= MC_1_13_2_Version)
                                    {
                                        mapIcon.Direction = dataTypes.ReadNextByte(packetData);

                                        if (dataTypes.ReadNextBool(packetData)) // Has Display Name?
                                            mapIcon.DisplayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    }

                                    icons.Add(mapIcon);
                                }
                            }

                            byte columnsUpdated = dataTypes.ReadNextByte(packetData); // width
                            byte rowsUpdated = 0; // height
                            byte mapCoulmnX = 0;
                            byte mapRowZ = 0;
                            byte[]? colors = null;

                            if (columnsUpdated > 0)
                            {
                                rowsUpdated = dataTypes.ReadNextByte(packetData); // height
                                mapCoulmnX = dataTypes.ReadNextByte(packetData);
                                mapRowZ = dataTypes.ReadNextByte(packetData);
                                colors = dataTypes.ReadNextByteArray(packetData);
                            }

                            handler.OnMapData(mapid, scale, trackingPosition, locked, icons, columnsUpdated, rowsUpdated, mapCoulmnX, mapRowZ, colors);
                            break;
                        case PacketTypesIn.TradeList:
                            if ((protocolVersion >= MC_1_14_Version) && (handler.GetInventoryEnabled()))
                            {
                                // MC 1.14 or greater
                                int windowID = dataTypes.ReadNextVarInt(packetData);
                                int size = dataTypes.ReadNextByte(packetData);
                                List<VillagerTrade> trades = new();
                                for (int tradeId = 0; tradeId < size; tradeId++)
                                {
                                    VillagerTrade trade = dataTypes.ReadNextTrade(packetData, itemPalette);
                                    trades.Add(trade);
                                }
                                VillagerInfo villagerInfo = new()
                                {
                                    Level = dataTypes.ReadNextVarInt(packetData),
                                    Experience = dataTypes.ReadNextVarInt(packetData),
                                    IsRegularVillager = dataTypes.ReadNextBool(packetData),
                                    CanRestock = dataTypes.ReadNextBool(packetData)
                                };
                                handler.OnTradeList(windowID, trades, villagerInfo);
                            }
                            break;
                        case PacketTypesIn.Title:
                            if (protocolVersion >= MC_1_8_Version)
                            {
                                int action2 = dataTypes.ReadNextVarInt(packetData);
                                string titletext = String.Empty;
                                string subtitletext = String.Empty;
                                string actionbartext = String.Empty;
                                string json = String.Empty;
                                int fadein = -1;
                                int stay = -1;
                                int fadeout = -1;
                                if (protocolVersion >= MC_1_10_Version)
                                {
                                    if (action2 == 0)
                                    {
                                        json = titletext;
                                        titletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    }
                                    else if (action2 == 1)
                                    {
                                        json = subtitletext;
                                        subtitletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    }
                                    else if (action2 == 2)
                                    {
                                        json = actionbartext;
                                        actionbartext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    }
                                    else if (action2 == 3)
                                    {
                                        fadein = dataTypes.ReadNextInt(packetData);
                                        stay = dataTypes.ReadNextInt(packetData);
                                        fadeout = dataTypes.ReadNextInt(packetData);
                                    }
                                }
                                else
                                {
                                    if (action2 == 0)
                                    {
                                        json = titletext;
                                        titletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    }
                                    else if (action2 == 1)
                                    {
                                        json = subtitletext;
                                        subtitletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                    }
                                    else if (action2 == 2)
                                    {
                                        fadein = dataTypes.ReadNextInt(packetData);
                                        stay = dataTypes.ReadNextInt(packetData);
                                        fadeout = dataTypes.ReadNextInt(packetData);
                                    }
                                }
                                handler.OnTitle(action2, titletext, subtitletext, actionbartext, fadein, stay, fadeout, json);
                            }
                            break;
                        case PacketTypesIn.MultiBlockChange:
                            if (handler.GetTerrainEnabled())
                            {
                                if (protocolVersion >= MC_1_16_2_Version)
                                {
                                    long chunkSection = dataTypes.ReadNextLong(packetData);
                                    int sectionX = (int)(chunkSection >> 42);
                                    int sectionY = (int)((chunkSection << 44) >> 44);
                                    int sectionZ = (int)((chunkSection << 22) >> 42);
                                    dataTypes.ReadNextBool(packetData); // Useless boolean (Related to light update)
                                    int blocksSize = dataTypes.ReadNextVarInt(packetData);
                                    for (int i = 0; i < blocksSize; i++)
                                    {
                                        ulong chunkSectionPosition = (ulong)dataTypes.ReadNextVarLong(packetData);
                                        int blockId = (int)(chunkSectionPosition >> 12);
                                        int localX = (int)((chunkSectionPosition >> 8) & 0x0F);
                                        int localZ = (int)((chunkSectionPosition >> 4) & 0x0F);
                                        int localY = (int)(chunkSectionPosition & 0x0F);

                                        Block block = new((ushort)blockId);
                                        int blockX = (sectionX * 16) + localX;
                                        int blockY = (sectionY * 16) + localY;
                                        int blockZ = (sectionZ * 16) + localZ;

                                        Location location = new(blockX, blockY, blockZ);

                                        handler.OnBlockChange(location, block);
                                    }
                                }
                                else
                                {
                                    int chunkX = dataTypes.ReadNextInt(packetData);
                                    int chunkZ = dataTypes.ReadNextInt(packetData);
                                    int recordCount = protocolVersion < MC_1_8_Version
                                        ? (int)dataTypes.ReadNextShort(packetData)
                                        : dataTypes.ReadNextVarInt(packetData);

                                    for (int i = 0; i < recordCount; i++)
                                    {
                                        byte locationXZ;
                                        ushort blockIdMeta;
                                        int blockY;

                                        if (protocolVersion < MC_1_8_Version)
                                        {
                                            blockIdMeta = dataTypes.ReadNextUShort(packetData);
                                            blockY = (ushort)dataTypes.ReadNextByte(packetData);
                                            locationXZ = dataTypes.ReadNextByte(packetData);
                                        }
                                        else
                                        {
                                            locationXZ = dataTypes.ReadNextByte(packetData);
                                            blockY = (ushort)dataTypes.ReadNextByte(packetData);
                                            blockIdMeta = (ushort)dataTypes.ReadNextVarInt(packetData);
                                        }

                                        int blockX = locationXZ >> 4;
                                        int blockZ = locationXZ & 0x0F;

                                        Location location = new(chunkX, chunkZ, blockX, blockY, blockZ);
                                        Block block = new(blockIdMeta);
                                        handler.OnBlockChange(location, block);
                                    }
                                }
                            }
                            break;
                        case PacketTypesIn.ServerData:
                            string motd = "-";
                            bool hasMotd = dataTypes.ReadNextBool(packetData);
                            if (hasMotd)
                                motd = ChatParser.ParseText(dataTypes.ReadNextString(packetData));

                            string iconBase64 = "-";
                            bool hasIcon = dataTypes.ReadNextBool(packetData);
                            if (hasIcon)
                                iconBase64 = dataTypes.ReadNextString(packetData);

                            bool previewsChat = dataTypes.ReadNextBool(packetData);

                            handler.OnServerDataRecived(hasMotd, motd, hasIcon, iconBase64, previewsChat);
                            break;
                        case PacketTypesIn.BlockChange:
                            if (handler.GetTerrainEnabled())
                            {
                                if (protocolVersion < MC_1_8_Version)
                                {
                                    int blockX = dataTypes.ReadNextInt(packetData);
                                    int blockY = dataTypes.ReadNextByte(packetData);
                                    int blockZ = dataTypes.ReadNextInt(packetData);
                                    short blockId = (short)dataTypes.ReadNextVarInt(packetData);
                                    byte blockMeta = dataTypes.ReadNextByte(packetData);

                                    Location location = new(blockX, blockY, blockZ);
                                    Block block = new(blockId, blockMeta);
                                    handler.OnBlockChange(location, block);
                                }
                                else
                                {
                                    Location location = dataTypes.ReadNextLocation(packetData);
                                    Block block = new((ushort)dataTypes.ReadNextVarInt(packetData));
                                    handler.OnBlockChange(location, block);
                                }
                            }
                            break;
                        case PacketTypesIn.SetDisplayChatPreview:
                            bool previewsChatSetting = dataTypes.ReadNextBool(packetData);
                            handler.OnChatPreviewSettingUpdate(previewsChatSetting);
                            break;
                        case PacketTypesIn.ChatPreview:
                            int queryID = dataTypes.ReadNextInt(packetData);
                            bool componentIsPresent = dataTypes.ReadNextBool(packetData);

                            // Currently noy implemented
                            log.Debug("New chat preview: ");
                            log.Debug(">> Query ID: " + queryID);
                            log.Debug(">> Component is present: " + componentIsPresent);
                            if (componentIsPresent)
                            {
                                string message = dataTypes.ReadNextString(packetData);
                                log.Debug(">> Component: " + ChatParser.ParseText(message));
                                //handler.OnTextReceived(message, true);
                            }

                            break;
                        case PacketTypesIn.ChatSuggestions:
                            break;
                        case PacketTypesIn.MapChunkBulk:
                            if (protocolVersion < MC_1_9_Version && handler.GetTerrainEnabled())
                            {
                                int chunkCount;
                                bool hasSkyLight;
                                Queue<byte> chunkData = packetData;

                                //Read global fields
                                if (protocolVersion < MC_1_8_Version)
                                {
                                    chunkCount = dataTypes.ReadNextShort(packetData);
                                    int compressedDataSize = dataTypes.ReadNextInt(packetData);
                                    hasSkyLight = dataTypes.ReadNextBool(packetData);
                                    byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                                    byte[] decompressed = ZlibUtils.Decompress(compressed);
                                    chunkData = new Queue<byte>(decompressed);
                                }
                                else
                                {
                                    hasSkyLight = dataTypes.ReadNextBool(packetData);
                                    chunkCount = dataTypes.ReadNextVarInt(packetData);
                                }

                                //Read chunk records
                                int[] chunkXs = new int[chunkCount];
                                int[] chunkZs = new int[chunkCount];
                                ushort[] chunkMasks = new ushort[chunkCount];
                                ushort[] addBitmaps = new ushort[chunkCount];
                                for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                                {
                                    chunkXs[chunkColumnNo] = dataTypes.ReadNextInt(packetData);
                                    chunkZs[chunkColumnNo] = dataTypes.ReadNextInt(packetData);
                                    chunkMasks[chunkColumnNo] = dataTypes.ReadNextUShort(packetData);
                                    addBitmaps[chunkColumnNo] = protocolVersion < MC_1_8_Version
                                        ? dataTypes.ReadNextUShort(packetData)
                                        : (ushort)0;
                                }

                                //Process chunk records
                                for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                                {
                                    pTerrain.ProcessChunkColumnData(chunkXs[chunkColumnNo], chunkZs[chunkColumnNo], chunkMasks[chunkColumnNo], addBitmaps[chunkColumnNo], hasSkyLight, true, currentDimension, chunkData);
                                    Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);
                                }

                            }
                            break;
                        case PacketTypesIn.UnloadChunk:
                            if (protocolVersion >= MC_1_9_Version && handler.GetTerrainEnabled())
                            {
                                int chunkX = dataTypes.ReadNextInt(packetData);
                                int chunkZ = dataTypes.ReadNextInt(packetData);

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
                                byte reason = dataTypes.ReadNextByte(packetData);
                                float state = dataTypes.ReadNextFloat(packetData);

                                handler.OnGameEvent(reason, state);
                            }
                            break;
                        case PacketTypesIn.PlayerInfo:
                            if (protocolVersion >= MC_1_8_Version)
                            {
                                int action = dataTypes.ReadNextVarInt(packetData);                                      // Action Name
                                int numberOfPlayers = dataTypes.ReadNextVarInt(packetData);                             // Number Of Players 

                                for (int i = 0; i < numberOfPlayers; i++)
                                {
                                    Guid uuid = dataTypes.ReadNextUUID(packetData);                                     // Player UUID

                                    switch (action)
                                    {
                                        case 0x00: //Player Join (Add player since 1.19)
                                            string name = dataTypes.ReadNextString(packetData);                         // Player name
                                            int propNum = dataTypes.ReadNextVarInt(packetData);                         // Number of properties in the following array

                                            // Property: Tuple<Name, Value, Signature(empty if there is no signature)
                                            // The Property field looks as in the response of https://wiki.vg/Mojang_API#UUID_to_Profile_and_Skin.2FCape
                                            const bool useProperty = false;
#pragma warning disable CS0162 // Unreachable code detected
                                            Tuple<string, string, string?>[]? properties = useProperty ?
                                                new Tuple<string, string, string?>[propNum] : null;
                                            for (int p = 0; p < propNum; p++)
                                            {
                                                string propertyName = dataTypes.ReadNextString(packetData);             // Name: String (32767)
                                                string val = dataTypes.ReadNextString(packetData);                      // Value: String (32767)
                                                string? propertySignature = null;
                                                if (dataTypes.ReadNextBool(packetData))                                 // Is Signed
                                                    propertySignature = dataTypes.ReadNextString(packetData);           // Signature: String (32767)
                                                if (useProperty)
                                                    properties![p] = new(propertyName, val, propertySignature);
                                            }
#pragma warning restore CS0162 // Unreachable code detected

                                            int gameMode = dataTypes.ReadNextVarInt(packetData);                        // Gamemode
                                            handler.OnGamemodeUpdate(uuid, gameMode);

                                            int ping = dataTypes.ReadNextVarInt(packetData);                            // Ping

                                            string? displayName = null;
                                            if (dataTypes.ReadNextBool(packetData))                                     // Has display name
                                                displayName = dataTypes.ReadNextString(packetData);                     // Display name

                                            // 1.19 Additions
                                            long? keyExpiration = null;
                                            byte[]? publicKey = null, signature = null;
                                            if (protocolVersion >= MC_1_19_Version)
                                            {
                                                if (dataTypes.ReadNextBool(packetData))                                 // Has Sig Data (if true, red the following fields)
                                                {
                                                    keyExpiration = dataTypes.ReadNextLong(packetData);                 // Timestamp

                                                    int publicKeyLength = dataTypes.ReadNextVarInt(packetData);         // Public Key Length 
                                                    if (publicKeyLength > 0)
                                                        publicKey = dataTypes.ReadData(publicKeyLength, packetData);    // Public key

                                                    int signatureLength = dataTypes.ReadNextVarInt(packetData);         // Signature Length 
                                                    if (signatureLength > 0)
                                                        signature = dataTypes.ReadData(signatureLength, packetData);    // Public key
                                                }
                                            }

                                            handler.OnPlayerJoin(new PlayerInfo(uuid, name, properties, gameMode, ping, displayName, keyExpiration, publicKey, signature));
                                            break;
                                        case 0x01: //Update gamemode
                                            handler.OnGamemodeUpdate(uuid, dataTypes.ReadNextVarInt(packetData));
                                            break;
                                        case 0x02: //Update latency
                                            int latency = dataTypes.ReadNextVarInt(packetData);
                                            handler.OnLatencyUpdate(uuid, latency); //Update latency;
                                            break;
                                        case 0x03: //Update display name
                                            if (dataTypes.ReadNextBool(packetData))
                                            {
                                                PlayerInfo? player = handler.GetPlayerInfo(uuid);
                                                if (player != null)
                                                    player.DisplayName = dataTypes.ReadNextString(packetData);
                                                else
                                                    dataTypes.SkipNextString(packetData);
                                            }
                                            break;
                                        case 0x04: //Player Leave
                                            handler.OnPlayerLeave(uuid);
                                            break;
                                        default:
                                            //Unknown player list item type
                                            break;
                                    }
                                }
                            }
                            else //MC 1.7.X does not provide UUID in tab-list updates
                            {
                                string name = dataTypes.ReadNextString(packetData);
                                bool online = dataTypes.ReadNextBool(packetData);
                                short ping = dataTypes.ReadNextShort(packetData);
                                Guid FakeUUID = new(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                                if (online)
                                    handler.OnPlayerJoin(new PlayerInfo(name, FakeUUID));
                                else handler.OnPlayerLeave(FakeUUID);
                            }
                            break;
                        case PacketTypesIn.TabComplete:
                            if (protocolVersion >= MC_1_13_Version)
                            {
                                autocomplete_transaction_id = dataTypes.ReadNextVarInt(packetData);
                                dataTypes.ReadNextVarInt(packetData); // Start of text to replace
                                dataTypes.ReadNextVarInt(packetData); // Length of text to replace
                            }

                            int autocomplete_count = dataTypes.ReadNextVarInt(packetData);
                            autocomplete_result.Clear();

                            for (int i = 0; i < autocomplete_count; i++)
                            {
                                autocomplete_result.Add(dataTypes.ReadNextString(packetData));
                                if (protocolVersion >= MC_1_13_Version)
                                {
                                    // Skip optional tooltip for each tab-complete result
                                    if (dataTypes.ReadNextBool(packetData))
                                        dataTypes.SkipNextString(packetData);
                                }
                            }

                            autocomplete_received = true;
                            break;
                        case PacketTypesIn.PluginMessage:
                            String channel = dataTypes.ReadNextString(packetData);
                            // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
                            if (protocolVersion < MC_1_8_Version)
                                pForge.ReadNextVarShort(packetData);
                            handler.OnPluginChannelMessage(channel, packetData.ToArray());
                            return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                        case PacketTypesIn.Disconnect:
                            handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                            return false;
                        case PacketTypesIn.SetCompression:
                            if (protocolVersion >= MC_1_8_Version && protocolVersion < MC_1_9_Version)
                                compression_treshold = dataTypes.ReadNextVarInt(packetData);
                            break;
                        case PacketTypesIn.OpenWindow:
                            if (handler.GetInventoryEnabled())
                            {
                                if (protocolVersion < MC_1_14_Version)
                                {
                                    // MC 1.13 or lower
                                    byte windowID = dataTypes.ReadNextByte(packetData);
                                    string type = dataTypes.ReadNextString(packetData).Replace("minecraft:", "").ToUpper();
                                    ContainerTypeOld inventoryType = (ContainerTypeOld)Enum.Parse(typeof(ContainerTypeOld), type);
                                    string title = dataTypes.ReadNextString(packetData);
                                    byte slots = dataTypes.ReadNextByte(packetData);
                                    Container inventory = new(windowID, inventoryType, ChatParser.ParseText(title));
                                    handler.OnInventoryOpen(windowID, inventory);
                                }
                                else
                                {
                                    // MC 1.14 or greater
                                    int windowID = dataTypes.ReadNextVarInt(packetData);
                                    int windowType = dataTypes.ReadNextVarInt(packetData);
                                    string title = dataTypes.ReadNextString(packetData);
                                    Container inventory = new(windowID, windowType, ChatParser.ParseText(title));
                                    handler.OnInventoryOpen(windowID, inventory);
                                }
                            }
                            break;
                        case PacketTypesIn.CloseWindow:
                            if (handler.GetInventoryEnabled())
                            {
                                byte windowID = dataTypes.ReadNextByte(packetData);
                                lock (window_actions) { window_actions[windowID] = 0; }
                                handler.OnInventoryClose(windowID);
                            }
                            break;
                        case PacketTypesIn.WindowItems:
                            if (handler.GetInventoryEnabled())
                            {
                                byte windowId = dataTypes.ReadNextByte(packetData);
                                int stateId = -1;
                                int elements = 0;

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
                                for (int slotId = 0; slotId < elements; slotId++)
                                {
                                    Item? item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                    if (item != null)
                                        inventorySlots[slotId] = item;
                                }

                                if (protocolVersion >= MC_1_17_1_Version) // Carried Item - 1.17.1 and above
                                    dataTypes.ReadNextItemSlot(packetData, itemPalette);

                                handler.OnWindowItems(windowId, inventorySlots, stateId);
                            }
                            break;
                        case PacketTypesIn.WindowProperty:
                            byte containerId = dataTypes.ReadNextByte(packetData);
                            short propertyId = dataTypes.ReadNextShort(packetData);
                            short propertyValue = dataTypes.ReadNextShort(packetData);

                            handler.OnWindowProperties(containerId, propertyId, propertyValue);
                            break;
                        case PacketTypesIn.SetSlot:
                            if (handler.GetInventoryEnabled())
                            {
                                byte windowID = dataTypes.ReadNextByte(packetData);
                                int stateId = -1;
                                if (protocolVersion >= MC_1_17_1_Version)
                                    stateId = dataTypes.ReadNextVarInt(packetData); // State ID - 1.17.1 and above
                                short slotID = dataTypes.ReadNextShort(packetData);
                                Item? item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                handler.OnSetSlot(windowID, slotID, item, stateId);
                            }
                            break;
                        case PacketTypesIn.WindowConfirmation:
                            if (handler.GetInventoryEnabled())
                            {
                                byte windowID = dataTypes.ReadNextByte(packetData);
                                short actionID = dataTypes.ReadNextShort(packetData);
                                bool accepted = dataTypes.ReadNextBool(packetData);
                                if (!accepted)
                                    SendWindowConfirmation(windowID, actionID, true);
                            }
                            break;
                        case PacketTypesIn.ResourcePackSend:
                            string url = dataTypes.ReadNextString(packetData);
                            string hash = dataTypes.ReadNextString(packetData);
                            bool forced = true; // Assume forced for MC 1.16 and below
                            if (protocolVersion >= MC_1_17_Version)
                            {
                                forced = dataTypes.ReadNextBool(packetData);
                                bool hasPromptMessage = dataTypes.ReadNextBool(packetData);   // Has Prompt Message (Boolean) - 1.17 and above
                                if (hasPromptMessage)
                                    dataTypes.SkipNextString(packetData); // Prompt Message (Optional Chat) - 1.17 and above
                            }
                            // Some server plugins may send invalid resource packs to probe the client and we need to ignore them (issue #1056)
                            if (!url.StartsWith("http") && hash.Length != 40) // Some server may have null hash value
                                break;
                            //Send back "accepted" and "successfully loaded" responses for plugins or server config making use of resource pack mandatory
                            byte[] responseHeader = Array.Empty<byte>();
                            if (protocolVersion < MC_1_10_Version) //MC 1.10 does not include resource pack hash in responses
                                responseHeader = dataTypes.ConcatBytes(dataTypes.GetVarInt(hash.Length), Encoding.UTF8.GetBytes(hash));
                            SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(3))); //Accepted pack
                            SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(0))); //Successfully loaded
                            break;
                        case PacketTypesIn.SpawnEntity:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                Entity entity = dataTypes.ReadNextEntity(packetData, entityPalette, false);
                                handler.OnSpawnEntity(entity);
                            }
                            break;
                        case PacketTypesIn.EntityEquipment:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int entityid = dataTypes.ReadNextVarInt(packetData);
                                if (protocolVersion >= MC_1_16_Version)
                                {
                                    bool hasNext;
                                    do
                                    {
                                        byte bitsData = dataTypes.ReadNextByte(packetData);
                                        //  Top bit set if another entry follows, and otherwise unset if this is the last item in the array
                                        hasNext = (bitsData >> 7) == 1;
                                        int slot2 = bitsData >> 1;
                                        Item? item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                        handler.OnEntityEquipment(entityid, slot2, item);
                                    } while (hasNext);
                                }
                                else
                                {
                                    int slot2 = dataTypes.ReadNextVarInt(packetData);
                                    Item? item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                    handler.OnEntityEquipment(entityid, slot2, item);
                                }
                            }
                            break;
                        case PacketTypesIn.SpawnLivingEntity:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                Entity entity = dataTypes.ReadNextEntity(packetData, entityPalette, true);
                                // packet before 1.15 has metadata at the end
                                // this is not handled in dataTypes.ReadNextEntity()
                                // we are simply ignoring leftover data in packet
                                handler.OnSpawnEntity(entity);
                            }
                            break;
                        case PacketTypesIn.SpawnPlayer:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int EntityID = dataTypes.ReadNextVarInt(packetData);
                                Guid UUID = dataTypes.ReadNextUUID(packetData);
                                double X = dataTypes.ReadNextDouble(packetData);
                                double Y = dataTypes.ReadNextDouble(packetData);
                                double Z = dataTypes.ReadNextDouble(packetData);
                                byte Yaw = dataTypes.ReadNextByte(packetData);
                                byte Pitch = dataTypes.ReadNextByte(packetData);

                                Location EntityLocation = new(X, Y, Z);

                                handler.OnSpawnPlayer(EntityID, UUID, EntityLocation, Yaw, Pitch);
                            }
                            break;
                        case PacketTypesIn.EntityEffect:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int entityid = dataTypes.ReadNextVarInt(packetData);
                                Inventory.Effects effect = Effects.Speed;
                                int effectId = protocolVersion >= MC_1_18_2_Version ?
                                    dataTypes.ReadNextVarInt(packetData) : dataTypes.ReadNextByte(packetData);
                                if (Enum.TryParse(effectId.ToString(), out effect))
                                {
                                    int amplifier = dataTypes.ReadNextByte(packetData);
                                    int duration = dataTypes.ReadNextVarInt(packetData);
                                    byte flags = dataTypes.ReadNextByte(packetData);

                                    bool hasFactorData = false;
                                    Dictionary<string, object>? factorCodec = null;

                                    if (protocolVersion >= MC_1_19_Version)
                                    {
                                        hasFactorData = dataTypes.ReadNextBool(packetData);
                                        if (hasFactorData)
                                            factorCodec = dataTypes.ReadNextNbt(packetData);
                                    }

                                    handler.OnEntityEffect(entityid, effect, amplifier, duration, flags, hasFactorData, factorCodec);
                                }
                            }
                            break;
                        case PacketTypesIn.DestroyEntities:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int entityCount = 1; // 1.17.0 has only one entity per packet
                                if (protocolVersion != MC_1_17_Version)
                                    entityCount = dataTypes.ReadNextVarInt(packetData); // All other versions have a "count" field
                                int[] entityList = new int[entityCount];
                                for (int i = 0; i < entityCount; i++)
                                {
                                    entityList[i] = dataTypes.ReadNextVarInt(packetData);
                                }
                                handler.OnDestroyEntities(entityList);
                            }
                            break;
                        case PacketTypesIn.EntityPosition:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int EntityID = dataTypes.ReadNextVarInt(packetData);
                                Double DeltaX = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                                Double DeltaY = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                                Double DeltaZ = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                                bool OnGround = dataTypes.ReadNextBool(packetData);
                                DeltaX /= (128 * 32);
                                DeltaY /= (128 * 32);
                                DeltaZ /= (128 * 32);
                                handler.OnEntityPosition(EntityID, DeltaX, DeltaY, DeltaZ, OnGround);
                            }
                            break;
                        case PacketTypesIn.EntityPositionAndRotation:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int EntityID = dataTypes.ReadNextVarInt(packetData);
                                Double DeltaX = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                                Double DeltaY = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                                Double DeltaZ = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                                byte _yaw = dataTypes.ReadNextByte(packetData);
                                byte _pitch = dataTypes.ReadNextByte(packetData);
                                bool OnGround = dataTypes.ReadNextBool(packetData);
                                DeltaX /= (128 * 32);
                                DeltaY /= (128 * 32);
                                DeltaZ /= (128 * 32);
                                handler.OnEntityPosition(EntityID, DeltaX, DeltaY, DeltaZ, OnGround);
                            }
                            break;
                        case PacketTypesIn.EntityProperties:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int EntityID = dataTypes.ReadNextVarInt(packetData);
                                int NumberOfProperties = protocolVersion >= MC_1_17_Version ? dataTypes.ReadNextVarInt(packetData) : dataTypes.ReadNextInt(packetData);
                                Dictionary<string, Double> keys = new();
                                for (int i = 0; i < NumberOfProperties; i++)
                                {
                                    string _key = dataTypes.ReadNextString(packetData);
                                    Double _value = dataTypes.ReadNextDouble(packetData);

                                    List<double> op0 = new();
                                    List<double> op1 = new();
                                    List<double> op2 = new();
                                    int NumberOfModifiers = dataTypes.ReadNextVarInt(packetData);
                                    for (int j = 0; j < NumberOfModifiers; j++)
                                    {
                                        dataTypes.ReadNextUUID(packetData);
                                        Double amount = dataTypes.ReadNextDouble(packetData);
                                        byte operation = dataTypes.ReadNextByte(packetData);
                                        switch (operation)
                                        {
                                            case 0: op0.Add(amount); break;
                                            case 1: op1.Add(amount); break;
                                            case 2: op2.Add(amount + 1); break;
                                        }
                                    }
                                    if (op0.Count > 0) _value += op0.Sum();
                                    if (op1.Count > 0) _value *= 1 + op1.Sum();
                                    if (op2.Count > 0) _value *= op2.Aggregate((a, _x) => a * _x);
                                    keys.Add(_key, _value);
                                }
                                handler.OnEntityProperties(EntityID, keys);
                            }
                            break;
                        case PacketTypesIn.EntityMetadata:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int EntityID = dataTypes.ReadNextVarInt(packetData);
                                Dictionary<int, object?> metadata = dataTypes.ReadNextMetadata(packetData, itemPalette);

                                int healthField; // See https://wiki.vg/Entity_metadata#Living_Entity
                                if (protocolVersion > MC_1_19_2_Version)
                                    throw new NotImplementedException(Translations.Get("exception.palette.healthfield"));
                                else if (protocolVersion >= MC_1_17_Version) // 1.17 and above
                                    healthField = 9;
                                else if (protocolVersion >= MC_1_14_Version) // 1.14 and above
                                    healthField = 8;
                                else if (protocolVersion >= MC_1_10_Version) // 1.10 and above
                                    healthField = 7;
                                else
                                    throw new NotImplementedException(Translations.Get("exception.palette.healthfield"));

                                if (metadata.TryGetValue(healthField, out object? healthObj) && healthObj != null && healthObj.GetType() == typeof(float))
                                    handler.OnEntityHealth(EntityID, (float)healthObj);

                                handler.OnEntityMetadata(EntityID, metadata);
                            }
                            break;
                        case PacketTypesIn.EntityStatus:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int entityId = dataTypes.ReadNextInt(packetData);
                                byte status = dataTypes.ReadNextByte(packetData);
                                handler.OnEntityStatus(entityId, status);
                            }
                            break;
                        case PacketTypesIn.TimeUpdate:
                            long WorldAge = dataTypes.ReadNextLong(packetData);
                            long TimeOfday = dataTypes.ReadNextLong(packetData);
                            handler.OnTimeUpdate(WorldAge, TimeOfday);
                            break;
                        case PacketTypesIn.SystemChat:
                            string systemMessage = dataTypes.ReadNextString(packetData);
                            int msgType = dataTypes.ReadNextVarInt(packetData);
                            if ((msgType == 1 && !Config.Main.Advanced.ShowSystemMessages))
                                break;
                            handler.OnTextReceived(new(systemMessage, true, msgType, Guid.Empty, true));
                            break;
                        case PacketTypesIn.EntityTeleport:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int EntityID = dataTypes.ReadNextVarInt(packetData);
                                Double X = dataTypes.ReadNextDouble(packetData);
                                Double Y = dataTypes.ReadNextDouble(packetData);
                                Double Z = dataTypes.ReadNextDouble(packetData);
                                byte EntityYaw = dataTypes.ReadNextByte(packetData);
                                byte EntityPitch = dataTypes.ReadNextByte(packetData);
                                bool OnGround = dataTypes.ReadNextBool(packetData);
                                handler.OnEntityTeleport(EntityID, X, Y, Z, OnGround);
                            }
                            break;
                        case PacketTypesIn.UpdateHealth:
                            float health = dataTypes.ReadNextFloat(packetData);
                            int food;
                            if (protocolVersion >= MC_1_8_Version)
                                food = dataTypes.ReadNextVarInt(packetData);
                            else
                                food = dataTypes.ReadNextShort(packetData);
                            dataTypes.ReadNextFloat(packetData); // Food Saturation
                            handler.OnUpdateHealth(health, food);
                            break;
                        case PacketTypesIn.SetExperience:
                            float experiencebar = dataTypes.ReadNextFloat(packetData);
                            int level = dataTypes.ReadNextVarInt(packetData);
                            int totalexperience = dataTypes.ReadNextVarInt(packetData);
                            handler.OnSetExperience(experiencebar, level, totalexperience);
                            break;
                        case PacketTypesIn.Explosion:
                            Location explosionLocation = new(dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData));

                            float explosionStrength = dataTypes.ReadNextFloat(packetData);
                            int explosionBlockCount = protocolVersion >= MC_1_17_Version
                                ? dataTypes.ReadNextVarInt(packetData)
                                : dataTypes.ReadNextInt(packetData);

                            for (int i = 0; i < explosionBlockCount; i++)
                                dataTypes.ReadData(3, packetData);

                            float playerVelocityX = dataTypes.ReadNextFloat(packetData);
                            float playerVelocityY = dataTypes.ReadNextFloat(packetData);
                            float playerVelocityZ = dataTypes.ReadNextFloat(packetData);

                            handler.OnExplosion(explosionLocation, explosionStrength, explosionBlockCount);
                            break;
                        case PacketTypesIn.HeldItemChange:
                            byte slot = dataTypes.ReadNextByte(packetData);
                            handler.OnHeldItemChange(slot);
                            break;
                        case PacketTypesIn.ScoreboardObjective:
                            string objectivename = dataTypes.ReadNextString(packetData);
                            byte mode = dataTypes.ReadNextByte(packetData);
                            string objectivevalue = String.Empty;
                            int type2 = -1;
                            if (mode == 0 || mode == 2)
                            {
                                objectivevalue = dataTypes.ReadNextString(packetData);
                                type2 = dataTypes.ReadNextVarInt(packetData);
                            }
                            handler.OnScoreboardObjective(objectivename, mode, objectivevalue, type2);
                            break;
                        case PacketTypesIn.UpdateScore:
                            string entityname = dataTypes.ReadNextString(packetData);
                            int action3 = protocolVersion >= MC_1_18_2_Version
                                    ? dataTypes.ReadNextVarInt(packetData)
                                    : dataTypes.ReadNextByte(packetData);
                            string objectivename2 = string.Empty;
                            int value = -1;
                            if (action3 != 1 || protocolVersion >= MC_1_8_Version)
                                objectivename2 = dataTypes.ReadNextString(packetData);
                            if (action3 != 1)
                                value = dataTypes.ReadNextVarInt(packetData);
                            handler.OnUpdateScore(entityname, action3, objectivename2, value);
                            break;
                        case PacketTypesIn.BlockChangedAck:
                            handler.OnBlockChangeAck(dataTypes.ReadNextVarInt(packetData));
                            break;
                        case PacketTypesIn.BlockBreakAnimation:
                            if (handler.GetEntityHandlingEnabled() && handler.GetTerrainEnabled())
                            {
                                int playerId = dataTypes.ReadNextVarInt(packetData);
                                Location blockLocation = dataTypes.ReadNextLocation(packetData);
                                byte stage = dataTypes.ReadNextByte(packetData);
                                handler.OnBlockBreakAnimation(playerId, blockLocation, stage);
                            }
                            break;
                        case PacketTypesIn.EntityAnimation:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int playerId2 = dataTypes.ReadNextVarInt(packetData);
                                byte animation = dataTypes.ReadNextByte(packetData);
                                handler.OnEntityAnimation(playerId2, animation);
                            }
                            break;
                        default:
                            return false; //Ignored packet
                    }
                return true; //Packet processed
            }
            catch (Exception innerException)
            {
                if (innerException is ThreadAbortException || innerException is SocketException || innerException.InnerException is SocketException)
                    throw; //Thread abort or Connection lost rather than invalid data
                throw new System.IO.InvalidDataException(
                    Translations.Get("exception.packet_process",
                        packetPalette.GetIncommingTypeById(packetID),
                        packetID,
                        protocolVersion,
                        login_phase,
                        innerException.GetType()),
                    innerException);
            }
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
            catch { }
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
        /// Send a packet to the server. Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="packetID">packet ID</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetID, IEnumerable<byte> packetData)
        {
            if (handler.GetNetworkPacketCaptureEnabled())
            {
                List<byte> clone = packetData.ToList();
                handler.OnNetworkPacket(packetID, clone, login_phase, false);
            }
            // log.Info("[C -> S] Sending packet " + packetID + " > " + dataTypes.ByteArrayToString(packetData.ToArray()));

            //The inner packet
            byte[] the_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(packetID), packetData.ToArray());

            if (compression_treshold > 0) //Compression enabled?
            {
                if (the_packet.Length >= compression_treshold) //Packet long enough for compressing?
                {
                    byte[] compressed_packet = ZlibUtils.Compress(the_packet);
                    the_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), compressed_packet);
                }
                else
                {
                    byte[] uncompressed_length = dataTypes.GetVarInt(0); //Not compressed (short packet)
                    the_packet = dataTypes.ConcatBytes(uncompressed_length, the_packet);
                }
            }

            //log.Debug("[C -> S] Sending packet " + packetID + " > " + dataTypes.ByteArrayToString(dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), the_packet)));
            socketWrapper.SendDataRAW(dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), the_packet));
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login(PlayerKeyPair? playerKeyPair, SessionToken session)
        {
            byte[] protocol_version = dataTypes.GetVarInt(protocolVersion);
            string server_address = pForge.GetServerAddress(handler.GetServerHost());
            byte[] server_port = dataTypes.GetUShort((ushort)handler.GetServerPort());
            byte[] next_state = dataTypes.GetVarInt(2);
            byte[] handshake_packet = dataTypes.ConcatBytes(protocol_version, dataTypes.GetString(server_address), server_port, next_state);
            SendPacket(0x00, handshake_packet);

            List<byte> fullLoginPacket = new();
            fullLoginPacket.AddRange(dataTypes.GetString(handler.GetUsername()));                             // Username
            if (protocolVersion >= MC_1_19_Version)
            {
                if (playerKeyPair == null)
                    fullLoginPacket.AddRange(dataTypes.GetBool(false));                                       // Has Sig Data
                else
                {
                    fullLoginPacket.AddRange(dataTypes.GetBool(true));                                        // Has Sig Data
                    fullLoginPacket.AddRange(dataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds()));   // Expiration time
                    fullLoginPacket.AddRange(dataTypes.GetArray(playerKeyPair.PublicKey.Key));                // Public key received from Microsoft API
                    if (protocolVersion >= MC_1_19_2_Version)
                        fullLoginPacket.AddRange(dataTypes.GetArray(playerKeyPair.PublicKey.SignatureV2!));   // Public key signature received from Microsoft API
                    else
                        fullLoginPacket.AddRange(dataTypes.GetArray(playerKeyPair.PublicKey.Signature!));     // Public key signature received from Microsoft API
                }
            }
            if (protocolVersion >= MC_1_19_2_Version)
            {
                Guid uuid = handler.GetUserUuid();
                if (uuid == Guid.Empty)
                    fullLoginPacket.AddRange(dataTypes.GetBool(false));                                       // Has UUID
                else
                {
                    fullLoginPacket.AddRange(dataTypes.GetBool(true));                                        // Has UUID
                    fullLoginPacket.AddRange(dataTypes.GetUUID(uuid));                                        // UUID
                }
            }
            SendPacket(0x00, fullLoginPacket);

            while (true)
            {
                (int packetID, Queue<byte> packetData) = ReadNextPacket();
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x01) //Encryption request
                {
                    isOnlineMode = true;
                    string serverID = dataTypes.ReadNextString(packetData);
                    byte[] serverPublicKey = dataTypes.ReadNextByteArray(packetData);
                    byte[] token = dataTypes.ReadNextByteArray(packetData);
                    return StartEncryption(handler.GetUserUuidStr(), handler.GetSessionID(), token, serverID, serverPublicKey, playerKeyPair, session);
                }
                else if (packetID == 0x02) //Login successful
                {
                    log.Info(Translations.Get("mcc.server_offline"));
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                    {
                        log.Error(Translations.Get("error.forge"));
                        return false;
                    }

                    StartUpdating();
                    return true; //No need to check session or start encryption
                }
                else HandlePacket(packetID, packetData);
            }
        }

        /// <summary>
        /// Start network encryption. Automatically called by Login() if the server requests encryption.
        /// </summary>
        /// <returns>True if encryption was successful</returns>
        private bool StartEncryption(string uuid, string sessionID, byte[] token, string serverIDhash, byte[] serverPublicKey, PlayerKeyPair? playerKeyPair, SessionToken session)
        {
            RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverPublicKey)!;
            byte[] secretKey = CryptoHandler.ClientAESPrivateKey ?? CryptoHandler.GenerateAESPrivateKey();

            log.Debug(Translations.Get("debug.crypto"));

            if (serverIDhash != "-")
            {
                log.Info(Translations.Get("mcc.session"));

                bool needCheckSession = true;
                if (session.ServerPublicKey != null && session.SessionPreCheckTask != null
                    && serverIDhash == session.ServerIDhash && Enumerable.SequenceEqual(serverPublicKey, session.ServerPublicKey))
                {
                    session.SessionPreCheckTask.Wait();
                    if (session.SessionPreCheckTask.Result) // PreCheck Successed
                        needCheckSession = false;
                }

                if (needCheckSession)
                {
                    string serverHash = CryptoHandler.GetServerHash(serverIDhash, serverPublicKey, secretKey);

                    if (ProtocolHandler.SessionCheck(uuid, sessionID, serverHash))
                    {
                        session.ServerIDhash = serverIDhash;
                        session.ServerPublicKey = serverPublicKey;
                        SessionCache.Store(InternalConfig.Login.ToLower(), session);
                    }
                    else
                    {
                        handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, Translations.Get("mcc.session_fail"));
                        return false;
                    }
                }
            }

            // Encryption Response packet
            List<byte> encryptionResponse = new();
            encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(secretKey, false)));     // Shared Secret
            if (protocolVersion >= Protocol18Handler.MC_1_19_Version)
            {
                if (playerKeyPair == null)
                {
                    encryptionResponse.AddRange(dataTypes.GetBool(true));                              // Has Verify Token
                    encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(token, false))); // Verify Token
                }
                else
                {
                    byte[] salt = GenerateSalt();
                    byte[] messageSignature = playerKeyPair.PrivateKey.SignData(dataTypes.ConcatBytes(token, salt));

                    encryptionResponse.AddRange(dataTypes.GetBool(false));                            // Has Verify Token
                    encryptionResponse.AddRange(salt);                                                // Salt
                    encryptionResponse.AddRange(dataTypes.GetArray(messageSignature));                // Message Signature
                }
            }
            else
            {
                encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(token, false)));    // Verify Token
            }
            SendPacket(0x01, encryptionResponse);

            //Start client-side encryption
            socketWrapper.SwitchToEncrypted(secretKey); // pre switch

            //Process the next packet
            int loopPrevention = UInt16.MaxValue;
            while (true)
            {
                (int packetID, Queue<byte> packetData) = ReadNextPacket();
                if (packetID < 0 || loopPrevention-- < 0) // Failed to read packet or too many iterations (issue #1150)
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, Translations.Get("error.invalid_encrypt"));
                    return false;
                }
                else if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x02) //Login successful
                {
                    Guid uuidReceived;
                    if (protocolVersion >= Protocol18Handler.MC_1_16_Version)
                        uuidReceived = dataTypes.ReadNextUUID(packetData);
                    else
                        uuidReceived = Guid.Parse(dataTypes.ReadNextString(packetData));
                    string userName = dataTypes.ReadNextString(packetData);
                    Tuple<string, string, string>[]? playerProperty = null;
                    if (protocolVersion >= Protocol18Handler.MC_1_19_Version)
                    {
                        int count = dataTypes.ReadNextVarInt(packetData); // Number Of Properties
                        playerProperty = new Tuple<string, string, string>[count];
                        for (int i = 0; i < count; ++i)
                        {
                            string name = dataTypes.ReadNextString(packetData);
                            string value = dataTypes.ReadNextString(packetData);
                            bool isSigned = dataTypes.ReadNextBool(packetData);
                            string signature = isSigned ? dataTypes.ReadNextString(packetData) : String.Empty;
                            playerProperty[i] = new Tuple<string, string, string>(name, value, signature);
                        }
                    }
                    handler.OnLoginSuccess(uuidReceived, userName, playerProperty);

                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                    {
                        log.Error(Translations.Get("error.forge_encrypt"));
                        return false;
                    }

                    StartUpdating();
                    return true;
                }
                else HandlePacket(packetID, packetData);
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
        IEnumerable<string> IAutoComplete.AutoComplete(string BehindCursor)
        {

            if (String.IsNullOrEmpty(BehindCursor))
                return Array.Empty<string>();

            byte[] transaction_id = dataTypes.GetVarInt(autocomplete_transaction_id);
            byte[] assume_command = new byte[] { 0x00 };
            byte[] has_position = new byte[] { 0x00 };

            byte[] tabcomplete_packet = Array.Empty<byte>();

            if (protocolVersion >= MC_1_8_Version)
            {
                if (protocolVersion >= MC_1_13_Version)
                {
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, transaction_id);
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, dataTypes.GetString(BehindCursor));
                }
                else
                {
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, dataTypes.GetString(BehindCursor));

                    if (protocolVersion >= MC_1_9_Version)
                    {
                        tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, assume_command);
                    }

                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, has_position);
                }
            }
            else
            {
                tabcomplete_packet = dataTypes.ConcatBytes(dataTypes.GetString(BehindCursor));
            }

            autocomplete_received = false;
            autocomplete_result.Clear();
            autocomplete_result.Add(BehindCursor);
            SendPacket(PacketTypesOut.TabComplete, tabcomplete_packet);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            ThreadStart start = new(delegate
                        {
                            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
                            if (autocomplete_result.Count > 0)
                                ConsoleIO.WriteLineFormatted("§8" + String.Join(" ", autocomplete_result), false);
                        });
            Thread t1 = new(start);
            t1.Start();
            return autocomplete_result;
        }

        /// <summary>
        /// Ping a Minecraft server to get information about the server
        /// </summary>
        /// <returns>True if ping was successful</returns>
        public static bool DoPing(string host, int port, ref int protocolVersion, ref ForgeInfo? forgeInfo)
        {
            string version = "";
            TcpClient tcp = ProxyHandler.NewTcpClient(host, port);
            tcp.ReceiveTimeout = 30000; // 30 seconds
            tcp.ReceiveBufferSize = 1024 * 1024;
            SocketWrapper socketWrapper = new(tcp);
            DataTypes dataTypes = new(MC_1_8_Version);

            byte[] packet_id = dataTypes.GetVarInt(0);
            byte[] protocol_version = dataTypes.GetVarInt(-1);
            byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
            byte[] next_state = dataTypes.GetVarInt(1);
            byte[] packet = dataTypes.ConcatBytes(packet_id, protocol_version, dataTypes.GetString(host), server_port, next_state);
            byte[] tosend = dataTypes.ConcatBytes(dataTypes.GetVarInt(packet.Length), packet);

            socketWrapper.SendDataRAW(tosend);

            byte[] status_request = dataTypes.GetVarInt(0);
            byte[] request_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(status_request.Length), status_request);

            socketWrapper.SendDataRAW(request_packet);

            int packetLength = dataTypes.ReadNextVarIntRAW(socketWrapper);
            if (packetLength > 0) //Read Response length
            {
                Queue<byte> packetData = new(socketWrapper.ReadDataRAW(packetLength));
                if (dataTypes.ReadNextVarInt(packetData) == 0x00) //Read Packet ID
                {
                    string result = dataTypes.ReadNextString(packetData); //Get the Json data

                    if (Config.Logging.DebugMessages)
                    {
                        // May contain formatting codes, cannot use WriteLineFormatted
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        ConsoleIO.WriteLine(result);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    if (!String.IsNullOrEmpty(result) && result.StartsWith("{") && result.EndsWith("}"))
                    {
                        Json.JSONData jsonData = Json.ParseJson(result);
                        if (jsonData.Type == Json.JSONData.DataType.Object && jsonData.Properties.ContainsKey("version"))
                        {
                            Json.JSONData versionData = jsonData.Properties["version"];

                            //Retrieve display name of the Minecraft version
                            if (versionData.Properties.ContainsKey("name"))
                                version = versionData.Properties["name"].StringValue;

                            //Retrieve protocol version number for handling this server
                            if (versionData.Properties.ContainsKey("protocol"))
                                protocolVersion = int.Parse(versionData.Properties["protocol"].StringValue, NumberStyles.Any, CultureInfo.CurrentCulture);

                            // Check for forge on the server.
                            Protocol18Forge.ServerInfoCheckForge(jsonData, ref forgeInfo);

                            ConsoleIO.WriteLineFormatted(Translations.Get("mcc.server_protocol", version, protocolVersion + (forgeInfo != null ? Translations.Get("mcc.with_forge") : "")));

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return protocolVersion > MC_1_10_Version
                ? 256
                : 100;
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
                byte[] fields = dataTypes.GetAcknowledgment(acknowledgment, isOnlineMode && Config.Signature.LoginWithSecureProfile);

                SendPacket(PacketTypesOut.MessageAcknowledgment, fields);

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public LastSeenMessageList.Acknowledgment ConsumeAcknowledgment()
        {
            pendingAcknowledgments = 0;
            return new LastSeenMessageList.Acknowledgment(lastSeenMessagesCollector.GetLastSeenMessages(), lastReceivedMessage);
        }

        public void Acknowledge(ChatMessage message)
        {
            LastSeenMessageList.Entry? entry = message.ToLastSeenMessageEntry();

            if (entry != null)
            {
                lastSeenMessagesCollector.Add(entry);
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
            if (String.IsNullOrEmpty(command))
                return true;

            command = Regex.Replace(command, @"\s+", " ");
            command = Regex.Replace(command, @"\s$", string.Empty);

            log.Debug("chat command = " + command);

            try
            {
                LastSeenMessageList.Acknowledgment? acknowledgment =
                    (protocolVersion >= MC_1_19_2_Version) ? ConsumeAcknowledgment() : null;

                List<byte> fields = new();

                // Command: String
                fields.AddRange(dataTypes.GetString(command));

                // Timestamp: Instant(Long)
                DateTimeOffset timeNow = DateTimeOffset.UtcNow;
                fields.AddRange(dataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                List<Tuple<string, string>>? needSigned = null;               // List< Argument Name, Argument Value >
                if (playerKeyPair != null && isOnlineMode && protocolVersion >= MC_1_19_Version
                    && Config.Signature.LoginWithSecureProfile && Config.Signature.SignMessageInCommand)
                    needSigned = DeclareCommands.CollectSignArguments(command);

                if (needSigned == null || needSigned!.Count == 0)
                {
                    fields.AddRange(dataTypes.GetLong(0));                    // Salt: Long
                    fields.AddRange(dataTypes.GetVarInt(0));                  // Signature Length: VarInt
                }
                else
                {
                    Guid uuid = handler.GetUserUuid();
                    byte[] salt = GenerateSalt();
                    fields.AddRange(salt);                                    // Salt: Long
                    fields.AddRange(dataTypes.GetVarInt(needSigned.Count));   // Signature Length: VarInt
                    foreach ((string argName, string message) in needSigned)
                    {
                        fields.AddRange(dataTypes.GetString(argName));        // Argument name: String
                        byte[] sign = (protocolVersion >= MC_1_19_2_Version) ?
                            playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow, ref salt, acknowledgment!.lastSeen) :
                            playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow, ref salt);
                        fields.AddRange(dataTypes.GetVarInt(sign.Length));    // Signature length: VarInt
                        fields.AddRange(sign);                                // Signature: Byte Array
                    }
                }

                // Signed Preview: Boolean
                fields.AddRange(dataTypes.GetBool(false));

                if (protocolVersion >= MC_1_19_2_Version)
                {
                    // Message Acknowledgment
                    fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment!, isOnlineMode && Config.Signature.LoginWithSecureProfile));
                }

                SendPacket(PacketTypesOut.ChatCommand, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="playerKeyPair">PlayerKeyPair</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatMessage(string message, PlayerKeyPair? playerKeyPair)
        {
            if (String.IsNullOrEmpty(message))
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
                    LastSeenMessageList.Acknowledgment? acknowledgment =
                        (protocolVersion >= MC_1_19_2_Version) ? ConsumeAcknowledgment() : null;

                    // Timestamp: Instant(Long)
                    DateTimeOffset timeNow = DateTimeOffset.UtcNow;
                    fields.AddRange(dataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                    if (!isOnlineMode || playerKeyPair == null || !Config.Signature.LoginWithSecureProfile || !Config.Signature.SignChat)
                    {
                        fields.AddRange(dataTypes.GetLong(0));   // Salt: Long
                        fields.AddRange(dataTypes.GetVarInt(0)); // Signature Length: VarInt
                    }
                    else
                    {
                        // Salt: Long
                        byte[] salt = GenerateSalt();
                        fields.AddRange(salt);

                        // Signature Length & Signature: (VarInt) and Byte Array
                        Guid uuid = handler.GetUserUuid();
                        byte[] sign = (protocolVersion >= MC_1_19_2_Version) ?
                            playerKeyPair.PrivateKey.SignMessage(message, uuid, timeNow, ref salt, acknowledgment!.lastSeen) :
                            playerKeyPair.PrivateKey.SignMessage(message, uuid, timeNow, ref salt);
                        fields.AddRange(dataTypes.GetVarInt(sign.Length));
                        fields.AddRange(sign);
                    }

                    // Signed Preview: Boolean
                    fields.AddRange(dataTypes.GetBool(false));

                    if (protocolVersion >= MC_1_19_2_Version)
                    {
                        // Message Acknowledgment
                        fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment!, isOnlineMode && Config.Signature.LoginWithSecureProfile));
                    }
                }
                SendPacket(PacketTypesOut.ChatMessage, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendEntityAction(int PlayerEntityID, int ActionID)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(dataTypes.GetVarInt(PlayerEntityID));
                fields.AddRange(dataTypes.GetVarInt(ActionID));
                fields.AddRange(dataTypes.GetVarInt(0));
                SendPacket(PacketTypesOut.EntityAction, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Tell the server what client is being used to connect to the server
        /// </summary>
        /// <param name="brandInfo">Client string describing the client</param>
        /// <returns>True if brand info was successfully sent</returns>
        public bool SendBrandInfo(string brandInfo)
        {
            if (String.IsNullOrEmpty(brandInfo))
                return false;
            // Plugin channels were significantly changed between Minecraft 1.12 and 1.13
            // https://wiki.vg/index.php?title=Pre-release_protocol&oldid=14132#Plugin_Channels
            if (protocolVersion >= MC_1_13_Version)
            {
                return SendPluginChannelPacket("minecraft:brand", dataTypes.GetString(brandInfo));
            }
            else
            {
                return SendPluginChannelPacket("MC|Brand", dataTypes.GetString(brandInfo));
            }
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
        public bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(dataTypes.GetString(language));
                fields.Add(viewDistance);

                if (protocolVersion >= MC_1_9_Version)
                    fields.AddRange(dataTypes.GetVarInt(chatMode));
                else
                    fields.AddRange(new byte[] { chatMode });

                fields.Add(chatColors ? (byte)1 : (byte)0);
                if (protocolVersion < MC_1_8_Version)
                {
                    fields.Add(difficulty);
                    fields.Add((byte)(skinParts & 0x1)); //show cape
                }
                else fields.Add(skinParts);
                if (protocolVersion >= MC_1_9_Version)
                    fields.AddRange(dataTypes.GetVarInt(mainHand));
                if (protocolVersion >= MC_1_17_Version)
                {
                    if (protocolVersion >= MC_1_18_1_Version)
                        fields.Add(0); // 1.18 and above - Enable text filtering. (Always false)
                    else
                        fields.Add(1); // 1.17 and 1.17.1 - Disable text filtering. (Always true)
                }
                if (protocolVersion >= MC_1_18_1_Version)
                    fields.Add(1); // 1.18 and above - Allow server listings
                SendPacket(PacketTypesOut.ClientSettings, fields);
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
            return SendLocationUpdate(location, onGround, yaw, pitch, false);
        }

        public bool SendLocationUpdate(Location location, bool onGround, float? yaw = null, float? pitch = null, bool forceUpdate = false)
        {
            if (handler.GetTerrainEnabled())
            {
                byte[] yawpitch = Array.Empty<byte>();
                PacketTypesOut packetType = PacketTypesOut.PlayerPosition;

                if (yaw.HasValue && pitch.HasValue && (forceUpdate || yaw.Value != LastYaw || pitch.Value != LastPitch))
                {
                    yawpitch = dataTypes.ConcatBytes(dataTypes.GetFloat(yaw.Value), dataTypes.GetFloat(pitch.Value));
                    packetType = PacketTypesOut.PlayerPositionAndRotation;

                    LastYaw = yaw.Value; LastPitch = pitch.Value;
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
                        yawpitch,
                        new byte[] { onGround ? (byte)1 : (byte)0 }));
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
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
                    byte[] length = BitConverter.GetBytes((short)data.Length);
                    Array.Reverse(length);

                    SendPacket(PacketTypesOut.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), length, data));
                }
                else
                {
                    SendPacket(PacketTypesOut.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), data));
                }

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
                SendPacket(0x02, dataTypes.ConcatBytes(dataTypes.GetVarInt(messageId), dataTypes.GetBool(understood), data));
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
                fields.AddRange(dataTypes.GetVarInt(EntityID));
                fields.AddRange(dataTypes.GetVarInt(type));

                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));

                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        // TODO: Interact at block location (e.g. chest minecart)
        public bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z, int hand)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(dataTypes.GetVarInt(EntityID));
                fields.AddRange(dataTypes.GetVarInt(type));
                fields.AddRange(dataTypes.GetFloat(X));
                fields.AddRange(dataTypes.GetFloat(Y));
                fields.AddRange(dataTypes.GetFloat(Z));
                fields.AddRange(dataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
        public bool SendInteractEntity(int EntityID, int type, int hand)
        {
            try
            {
                List<byte> fields = new();
                fields.AddRange(dataTypes.GetVarInt(EntityID));
                fields.AddRange(dataTypes.GetVarInt(type));
                fields.AddRange(dataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
                packet.AddRange(dataTypes.GetVarInt(hand));
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.UseItem, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerDigging(int status, Location location, Direction face, int sequenceId)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(dataTypes.GetVarInt(status));
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(dataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.PlayerDigging, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerBlockPlacement(int hand, Location location, Direction face, int sequenceId)
        {
            if (protocolVersion < MC_1_14_Version)
                return false; // NOT IMPLEMENTED for older MC versions
            try
            {
                List<byte> packet = new();
                packet.AddRange(dataTypes.GetVarInt(hand));
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(dataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorX
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorY
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorZ
                packet.Add(0); // insideBlock = false;
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.PlayerBlockPlacement, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, Item? item, List<Tuple<short, Item?>> changedSlots, int stateId)
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
                    case WindowActionType.LeftClick: button = 0; break;
                    case WindowActionType.RightClick: button = 1; break;
                    case WindowActionType.MiddleClick: button = 2; mode = 3; break;
                    case WindowActionType.ShiftClick: button = 0; mode = 1; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.DropItem: button = 0; mode = 4; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.DropItemStack: button = 1; mode = 4; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.StartDragLeft: button = 0; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.StartDragRight: button = 4; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.StartDragMiddle: button = 8; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragLeft: button = 2; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragRight: button = 6; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragMiddle: button = 10; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.AddDragLeft: button = 1; mode = 5; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.AddDragRight: button = 5; mode = 5; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.AddDragMiddle: button = 9; mode = 5; item = new Item(ItemType.Null, 0, null); break;
                }

                List<byte> packet = new()
                {
                    (byte)windowId // Window ID
                };

                // 1.18+
                if (protocolVersion >= MC_1_18_1_Version)
                {
                    packet.AddRange(dataTypes.GetVarInt(stateId)); // State ID
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                }
                // 1.17.1
                else if (protocolVersion == MC_1_17_1_Version)
                {
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                    packet.AddRange(dataTypes.GetVarInt(stateId)); // State ID
                }
                // Older
                else
                {
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                }

                packet.Add(button); // Button

                if (protocolVersion < MC_1_17_Version)
                    packet.AddRange(dataTypes.GetShort(actionNumber));

                if (protocolVersion >= MC_1_9_Version)
                    packet.AddRange(dataTypes.GetVarInt(mode)); // Mode
                else packet.Add(mode);

                // 1.17+  Array of changed slots
                if (protocolVersion >= MC_1_17_Version)
                {
                    packet.AddRange(dataTypes.GetVarInt(changedSlots.Count)); // Length of the array
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
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool ClickContainerButton(int windowId, int buttonId)
        {
            try
            {
                List<byte> packet = new();
                packet.Add((byte)windowId);
                packet.Add((byte)buttonId);
                SendPacket(PacketTypesOut.ClickWindowButton, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendAnimation(int animation, int playerid)
        {
            try
            {
                if (animation == 0 || animation == 1)
                {
                    List<byte> packet = new();

                    if (protocolVersion < MC_1_8_Version)
                    {
                        packet.AddRange(dataTypes.GetInt(playerid));
                        packet.Add((byte)1); // Swing arm
                    }
                    else if (protocolVersion < MC_1_9_Version)
                    {
                        // No fields in 1.8.X
                    }
                    else // MC 1.9+
                    {
                        packet.AddRange(dataTypes.GetVarInt(animation));
                    }

                    SendPacket(PacketTypesOut.Animation, packet);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendUpdateSign(Location sign, string line1, string line2, string line3, string line4)
        {
            try
            {
                if (line1.Length > 23)
                    line1 = line1[..23];
                if (line2.Length > 23)
                    line2 = line1[..23];
                if (line3.Length > 23)
                    line3 = line1[..23];
                if (line4.Length > 23)
                    line4 = line1[..23];

                List<byte> packet = new();
                packet.AddRange(dataTypes.GetLocation(sign));
                packet.AddRange(dataTypes.GetString(line1));
                packet.AddRange(dataTypes.GetString(line2));
                packet.AddRange(dataTypes.GetString(line3));
                packet.AddRange(dataTypes.GetString(line4));
                SendPacket(PacketTypesOut.UpdateSign, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool UpdateCommandBlock(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            if (protocolVersion <= MC_1_13_Version)
            {
                try
                {
                    List<byte> packet = new();
                    packet.AddRange(dataTypes.GetLocation(location));
                    packet.AddRange(dataTypes.GetString(command));
                    packet.AddRange(dataTypes.GetVarInt((int)mode));
                    packet.Add((byte)flags);
                    SendPacket(PacketTypesOut.UpdateSign, packet);
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }
            else { return false; }
        }

        public bool SendWindowConfirmation(byte windowID, short actionID, bool accepted)
        {
            try
            {
                List<byte> packet = new();
                packet.Add(windowID);
                packet.AddRange(dataTypes.GetShort(actionID));
                packet.Add(accepted ? (byte)1 : (byte)0);
                SendPacket(PacketTypesOut.WindowConfirmation, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SelectTrade(int selectedSlot)
        {
            // MC 1.13 or greater
            if (protocolVersion >= MC_1_13_Version)
            {
                try
                {
                    List<byte> packet = new();
                    packet.AddRange(dataTypes.GetVarInt(selectedSlot));
                    SendPacket(PacketTypesOut.SelectTrade, packet);
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }
            else { return false; }
        }

        public bool SendSpectate(Guid UUID)
        {
            // MC 1.8 or greater
            if (protocolVersion >= MC_1_8_Version)
            {
                try
                {
                    List<byte> packet = new();
                    packet.AddRange(dataTypes.GetUUID(UUID));
                    SendPacket(PacketTypesOut.Spectate, packet);
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }
            else { return false; }
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[8];
            randomGen.GetNonZeroBytes(salt);
            return salt;
        }
    }
}
