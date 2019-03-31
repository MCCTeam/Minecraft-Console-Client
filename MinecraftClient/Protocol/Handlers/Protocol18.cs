using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Proxy;
using System.Security.Cryptography;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Mapping;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.7.X, 1.8.X, 1.9.X, 1.10.X Protocols
    /// </summary>
    class Protocol18Handler : IMinecraftCom
    {
        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private int autocomplete_transaction_id = 0;
        private readonly List<string> autocomplete_result = new List<string>();
        private bool login_phase = true;
        private bool encrypted = false;
        private int protocolversion;

        // Server forge info -- may be null.
        private ForgeInfo forgeInfo;
        private FMLHandshakeClientState fmlHandshakeState = FMLHandshakeClientState.START;

        IMinecraftComHandler handler;
        Thread netRead;
        IAesStream s;
        TcpClient c;

        int currentDimension;

        public Protocol18Handler(TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler, ForgeInfo ForgeInfo)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            this.c = Client;
            this.protocolversion = ProtocolVersion;
            this.handler = Handler;
            this.forgeInfo = ForgeInfo;
        }

        private Protocol18Handler(TcpClient Client)
        {
            this.c = Client;
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>
        private void Updater()
        {
            try
            {
                do
                {
                    Thread.Sleep(100);
                }
                while (Update());
            }
            catch (System.IO.IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        /// <summary>
        /// Read data from the network. Should be called on a separate thread.
        /// </summary>
        /// <returns>FALSE if an error occured, TRUE otherwise.</returns>
        private bool Update()
        {
            handler.OnUpdate();
            if (c.Client == null || !c.Connected) { return false; }
            try
            {
                while (c.Client.Available > 0)
                {
                    int packetID = 0;
                    List<byte> packetData = new List<byte>();
                    readNextPacket(ref packetID, packetData);
                    handlePacket(packetID, new List<byte>(packetData));
                }
            }
            catch (SocketException) { return false; }
            catch (NullReferenceException) { return false; }
            return true;
        }

        /// <summary>
        /// Read the next packet from the network
        /// </summary>
        /// <param name="packetID">will contain packet ID</param>
        /// <param name="packetData">will contain raw packet Data</param>
        private void readNextPacket(ref int packetID, List<byte> packetData)
        {
            packetData.Clear();
            int size = readNextVarIntRAW(); //Packet size
            packetData.AddRange(readDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (protocolversion >= PacketUtils.MC18Version
                && compression_treshold > 0)
            {
                int sizeUncompressed = PacketUtils.readNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData.Clear();
                    packetData.AddRange(uncompressed);
                }
            }

            packetID = PacketUtils.readNextVarInt(packetData); //Packet ID
        }

        /// <summary>
        /// Abstract incoming packet numbering
        /// </summary>
        private enum PacketIncomingType
        {
            KeepAlive,
            JoinGame,
            ChatMessage,
            Respawn,
            PlayerPositionAndLook,
            ChunkData,
            MultiBlockChange,
            BlockChange,
            MapChunkBulk,
            UnloadChunk,
            PlayerListUpdate,
            TabCompleteResult,
            PluginMessage,
            KickPacket,
            NetworkCompressionTreshold,
            ResourcePackSend,
            UnknownPacket
        }

        /// <summary>
        /// Get abstract numbering of the specified packet ID
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="protocol">Protocol version</param>
        /// <returns>Abstract numbering</returns>
        private PacketIncomingType getPacketIncomingType(int packetID, int protocol)
        {
            // temporary workaround
            if (packetID == KeepAlive.getPacketID(protocol))
                return PacketIncomingType.KeepAlive;
            else if (packetID == JoinGame.getPacketID(protocol))
                return PacketIncomingType.JoinGame;
            else if (packetID == ChatMessage.getPacketID(protocol))
                return PacketIncomingType.ChatMessage;
            else if (packetID == Respawn.getPacketID(protocol))
                return PacketIncomingType.Respawn;
            else if (packetID == PlayerPositionAndLook.getPacketID(protocol))
                return PacketIncomingType.PlayerPositionAndLook;
            else if (packetID == ChunkData.getPacketID(protocol))
                return PacketIncomingType.ChunkData;

            if (protocol < PacketUtils.MC19Version)
            {
                switch (packetID)
                {
                    case 0x22: return PacketIncomingType.MultiBlockChange;
                    case 0x23: return PacketIncomingType.BlockChange;
                    case 0x26: return PacketIncomingType.MapChunkBulk;
                    //UnloadChunk does not exists prior to 1.9
                    case 0x38: return PacketIncomingType.PlayerListUpdate;
                    case 0x3A: return PacketIncomingType.TabCompleteResult;
                    case 0x3F: return PacketIncomingType.PluginMessage;
                    case 0x40: return PacketIncomingType.KickPacket;
                    case 0x46: return PacketIncomingType.NetworkCompressionTreshold;
                    case 0x48: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocol < PacketUtils.MC17w13aVersion)
            {
                switch (packetID)
                {
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x32: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocolversion < PacketUtils.MC112pre5Version)
            {
                switch (packetID)
                {
                    case 0x11: return PacketIncomingType.MultiBlockChange;
                    case 0x0C: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1E: return PacketIncomingType.UnloadChunk;
                    case 0x2E: return PacketIncomingType.PlayerListUpdate;
                    case 0x0F: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x34: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocol < PacketUtils.MC17w31aVersion)
            {
                switch (packetID)
                {
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x33: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocol < PacketUtils.MC17w45aVersion)
            {
                switch (packetID)
                {
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2E: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x34: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocol < PacketUtils.MC17w46aVersion)
            {
                switch (packetID)
                {
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2E: return PacketIncomingType.PlayerListUpdate;
                    //TabCompleteResult accidentely removed
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x34: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocol < PacketUtils.MC18w01aVersion)
            {
                switch (packetID)
                {
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1E: return PacketIncomingType.UnloadChunk;
                    case 0x2F: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x35: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else if (protocol < PacketUtils.MC113pre7Version)
            {
                switch (packetID)
                {
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1E: return PacketIncomingType.UnloadChunk;
                    case 0x2F: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x36: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else
            {
                switch (packetID)
                {
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1F: return PacketIncomingType.UnloadChunk;
                    case 0x30: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x37: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
        }

        /// <summary>
        /// Abstract outgoing packet numbering
        /// </summary>
        private enum PacketOutgoingType
        {
            KeepAlive,
            ResourcePackStatus,
            ChatMessage,
            ClientStatus,
            ClientSettings,
            PluginMessage,
            TabComplete,
            PlayerPosition,
            PlayerPositionAndLook,
            TeleportConfirm
        }

        /// <summary>
        /// Get packet ID of the specified outgoing packet
        /// </summary>
        /// <param name="packet">Abstract packet numbering</param>
        /// <param name="protocol">Protocol version</param>
        /// <returns>Packet ID</returns>
        private int getPacketOutgoingID(PacketOutgoingType packet, int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x00;
                    case PacketOutgoingType.ResourcePackStatus: return 0x19;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x16;
                    case PacketOutgoingType.ClientSettings: return 0x15;
                    case PacketOutgoingType.PluginMessage: return 0x17;
                    case PacketOutgoingType.TabComplete: return 0x14;
                    case PacketOutgoingType.PlayerPosition: return 0x04;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x06;
                    case PacketOutgoingType.TeleportConfirm: throw new InvalidOperationException("Teleport confirm is not supported in protocol " + protocol);
                }
            }
            else if (protocol < PacketUtils.MC17w13aVersion)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    case PacketOutgoingType.ResourcePackStatus: return 0x16;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x01;
                    case PacketOutgoingType.PlayerPosition: return 0x0C;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0D;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocolversion < PacketUtils.MC112pre5Version)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0C;
                    case PacketOutgoingType.ResourcePackStatus: return 0x18;
                    case PacketOutgoingType.ChatMessage: return 0x03;
                    case PacketOutgoingType.ClientStatus: return 0x04;
                    case PacketOutgoingType.ClientSettings: return 0x05;
                    case PacketOutgoingType.PluginMessage: return 0x0A;
                    case PacketOutgoingType.TabComplete: return 0x02;
                    case PacketOutgoingType.PlayerPosition: return 0x0D;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0E;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < PacketUtils.MC17w31aVersion)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0C;
                    case PacketOutgoingType.ResourcePackStatus: return 0x18;
                    case PacketOutgoingType.ChatMessage: return 0x03;
                    case PacketOutgoingType.ClientStatus: return 0x04;
                    case PacketOutgoingType.ClientSettings: return 0x05;
                    case PacketOutgoingType.PluginMessage: return 0x0A;
                    case PacketOutgoingType.TabComplete: return 0x02;
                    case PacketOutgoingType.PlayerPosition: return 0x0E;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0F;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < PacketUtils.MC17w45aVersion)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    case PacketOutgoingType.ResourcePackStatus: return 0x18;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x01;
                    case PacketOutgoingType.PlayerPosition: return 0x0D;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0E;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < PacketUtils.MC17w46aVersion)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0A;
                    case PacketOutgoingType.ResourcePackStatus: return 0x17;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x02;
                    case PacketOutgoingType.ClientSettings: return 0x03;
                    case PacketOutgoingType.PluginMessage: return 0x08;
                    case PacketOutgoingType.TabComplete: throw new InvalidOperationException("TabComplete was accidentely removed in protocol " + protocol + ". Please use a more recent version.");
                    case PacketOutgoingType.PlayerPosition: return 0x0C;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0D;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < PacketUtils.MC113pre4Version)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    case PacketOutgoingType.ResourcePackStatus: return 0x18;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x02;
                    case PacketOutgoingType.ClientSettings: return 0x03;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x04;
                    case PacketOutgoingType.PlayerPosition: return 0x0D;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0E;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else if (protocol < PacketUtils.MC113pre7Version)
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0C;
                    case PacketOutgoingType.ResourcePackStatus: return 0x1B;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x02;
                    case PacketOutgoingType.ClientSettings: return 0x03;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x04;
                    case PacketOutgoingType.PlayerPosition: return 0x0E;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0F;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0E;
                    case PacketOutgoingType.ResourcePackStatus: return 0x1D;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x0A;
                    case PacketOutgoingType.TabComplete: return 0x05;
                    case PacketOutgoingType.PlayerPosition: return 0x10;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x11;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }

            throw new System.ComponentModel.InvalidEnumArgumentException("Unknown PacketOutgoingType (protocol=" + protocol + ")", (int)packet, typeof(PacketOutgoingType));
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        private bool handlePacket(int packetID, List<byte> packetData)
        {
            if (login_phase)
            {
                switch (packetID) //Packet IDs are different while logging in
                {
                    case 0x03:
                        if (protocolversion >= PacketUtils.MC18Version)
                            compression_treshold = PacketUtils.readNextVarInt(packetData);
                        break;
                    default:
                        return false; //Ignored packet
                }
            }
            // Regular in-game packets
            switch (getPacketIncomingType(packetID, protocolversion))
            {
                case PacketIncomingType.KeepAlive:
                    SendPacket(PacketOutgoingType.KeepAlive, packetData);
                    break;
                case PacketIncomingType.JoinGame:
                    handler.OnGameJoined();
                    PacketUtils.readNextInt(packetData);
                    PacketUtils.readNextByte(packetData);
                    if (protocolversion >= PacketUtils.MC191Version)
                        this.currentDimension = PacketUtils.readNextInt(packetData);
                    else
                        this.currentDimension = (sbyte)PacketUtils.readNextByte(packetData);
                    PacketUtils.readNextByte(packetData);
                    PacketUtils.readNextByte(packetData);
                    PacketUtils.readNextString(packetData);
                    if (protocolversion >= PacketUtils.MC18Version)
                        PacketUtils.readNextBool(packetData);  // Reduced debug info - 1.8 and above
                    break;
                case PacketIncomingType.ChatMessage:
                    string message = PacketUtils.readNextString(packetData);
                    try
                    {
                        //Hide system messages or xp bar messages?
                        byte messageType = PacketUtils.readNextByte(packetData);
                        if ((messageType == 1 && !Settings.DisplaySystemMessages)
                            || (messageType == 2 && !Settings.DisplayXPBarMessages))
                            break;
                    }
                    catch (ArgumentOutOfRangeException) { /* No message type */ }
                    handler.OnTextReceived(message, true);
                    break;
                case PacketIncomingType.Respawn:
                    this.currentDimension = PacketUtils.readNextInt(packetData);
                    PacketUtils.readNextByte(packetData);
                    PacketUtils.readNextByte(packetData);
                    PacketUtils.readNextString(packetData);
                    break;
                case PacketIncomingType.PlayerPositionAndLook:
                    if (Settings.TerrainAndMovements)
                    {
                        double x = PacketUtils.readNextDouble(packetData);
                        double y = PacketUtils.readNextDouble(packetData);
                        double z = PacketUtils.readNextDouble(packetData);
                        byte[] yawpitch = PacketUtils.readData(8, packetData);
                        byte locMask = PacketUtils.readNextByte(packetData);

                        if (protocolversion >= PacketUtils.MC18Version)
                        {
                            Location location = handler.GetCurrentLocation();
                            location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                            location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                            location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                            handler.UpdateLocation(location, yawpitch);
                        }
                        else handler.UpdateLocation(new Location(x, y, z), yawpitch);
                    }

                    if (protocolversion >= PacketUtils.MC19Version)
                    {
                        int teleportID = PacketUtils.readNextVarInt(packetData);
                        // Teleport confirm packet
                        SendPacket(PacketOutgoingType.TeleportConfirm, PacketUtils.getVarInt(teleportID));
                    }
                    break;
                case PacketIncomingType.ChunkData:
                    if (Settings.TerrainAndMovements)
                    {
                        int chunkX = PacketUtils.readNextInt(packetData);
                        int chunkZ = PacketUtils.readNextInt(packetData);
                        bool chunksContinuous = PacketUtils.readNextBool(packetData);
                        ushort chunkMask = protocolversion >= PacketUtils.MC19Version
                            ? (ushort)PacketUtils.readNextVarInt(packetData)
                            : PacketUtils.readNextUShort(packetData);
                        if (protocolversion < PacketUtils.MC18Version)
                        {
                            ushort addBitmap = PacketUtils.readNextUShort(packetData);
                            int compressedDataSize = PacketUtils.readNextInt(packetData);
                            byte[] compressed = PacketUtils.readData(compressedDataSize, packetData);
                            byte[] decompressed = ZlibUtils.Decompress(compressed);
                            ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap, currentDimension == 0, chunksContinuous, new List<byte>(decompressed));
                        }
                        else
                        {
                            int dataSize = PacketUtils.readNextVarInt(packetData);
                            ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, packetData);
                        }
                    }
                    break;
                case PacketIncomingType.MultiBlockChange:
                    if (Settings.TerrainAndMovements)
                    {
                        int chunkX = PacketUtils.readNextInt(packetData);
                        int chunkZ = PacketUtils.readNextInt(packetData);
                        int recordCount = protocolversion < PacketUtils.MC18Version
                            ? (int)PacketUtils.readNextShort(packetData)
                            : PacketUtils.readNextVarInt(packetData);

                        for (int i = 0; i < recordCount; i++)
                        {
                            byte locationXZ;
                            ushort blockIdMeta;
                            int blockY;

                            if (protocolversion < PacketUtils.MC18Version)
                            {
                                blockIdMeta = PacketUtils.readNextUShort(packetData);
                                blockY = (ushort)PacketUtils.readNextByte(packetData);
                                locationXZ = PacketUtils.readNextByte(packetData);
                            }
                            else
                            {
                                locationXZ = PacketUtils.readNextByte(packetData);
                                blockY = (ushort)PacketUtils.readNextByte(packetData);
                                blockIdMeta = (ushort)PacketUtils.readNextVarInt(packetData);
                            }

                            int blockX = locationXZ >> 4;
                            int blockZ = locationXZ & 0x0F;
                            Block block = new Block(blockIdMeta);
                            handler.GetWorld().SetBlock(new Location(chunkX, chunkZ, blockX, blockY, blockZ), block);
                        }
                    }
                    break;
                case PacketIncomingType.BlockChange:
                    if (Settings.TerrainAndMovements)
                        if (protocolversion < PacketUtils.MC18Version)
                        {
                            int blockX = PacketUtils.readNextInt(packetData);
                            int blockY = PacketUtils.readNextByte(packetData);
                            int blockZ = PacketUtils.readNextInt(packetData);
                            short blockId = (short)PacketUtils.readNextVarInt(packetData);
                            byte blockMeta = PacketUtils.readNextByte(packetData);
                            handler.GetWorld().SetBlock(new Location(blockX, blockY, blockZ), new Block(blockId, blockMeta));
                        }
                        else handler.GetWorld().SetBlock(Location.FromLong(PacketUtils.readNextULong(packetData)), new Block((ushort)PacketUtils.readNextVarInt(packetData)));
                    break;
                case PacketIncomingType.MapChunkBulk:
                    if (protocolversion < PacketUtils.MC19Version && Settings.TerrainAndMovements)
                    {
                        int chunkCount;
                        bool hasSkyLight;
                        List<byte> chunkData = packetData;

                        //Read global fields
                        if (protocolversion < PacketUtils.MC18Version)
                        {
                            chunkCount = PacketUtils.readNextShort(packetData);
                            int compressedDataSize = PacketUtils.readNextInt(packetData);
                            hasSkyLight = PacketUtils.readNextBool(packetData);
                            byte[] compressed = PacketUtils.readData(compressedDataSize, packetData);
                            byte[] decompressed = ZlibUtils.Decompress(compressed);
                            chunkData = new List<byte>(decompressed);
                        }
                        else
                        {
                            hasSkyLight = PacketUtils.readNextBool(packetData);
                            chunkCount = PacketUtils.readNextVarInt(packetData);
                        }

                        //Read chunk records
                        int[] chunkXs = new int[chunkCount];
                        int[] chunkZs = new int[chunkCount];
                        ushort[] chunkMasks = new ushort[chunkCount];
                        ushort[] addBitmaps = new ushort[chunkCount];
                        for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                        {
                            chunkXs[chunkColumnNo] = PacketUtils.readNextInt(packetData);
                            chunkZs[chunkColumnNo] = PacketUtils.readNextInt(packetData);
                            chunkMasks[chunkColumnNo] = PacketUtils.readNextUShort(packetData);
                            addBitmaps[chunkColumnNo] = protocolversion < PacketUtils.MC18Version
                                ? PacketUtils.readNextUShort(packetData)
                                : (ushort)0;
                        }

                        //Process chunk records
                        for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                            ProcessChunkColumnData(chunkXs[chunkColumnNo], chunkZs[chunkColumnNo], chunkMasks[chunkColumnNo], addBitmaps[chunkColumnNo], hasSkyLight, true, chunkData);
                    }
                    break;
                case PacketIncomingType.UnloadChunk:
                    if (protocolversion >= PacketUtils.MC19Version && Settings.TerrainAndMovements)
                    {
                        int chunkX = PacketUtils.readNextInt(packetData);
                        int chunkZ = PacketUtils.readNextInt(packetData);
                        handler.GetWorld()[chunkX, chunkZ] = null;
                    }
                    break;
                case PacketIncomingType.PlayerListUpdate:
                    if (protocolversion >= PacketUtils.MC18Version)
                    {
                        int action = PacketUtils.readNextVarInt(packetData);
                        int numActions = PacketUtils.readNextVarInt(packetData);
                        for (int i = 0; i < numActions; i++)
                        {
                            Guid uuid = PacketUtils.readNextUUID(packetData);
                            switch (action)
                            {
                                case 0x00: //Player Join
                                    string name = PacketUtils.readNextString(packetData);
                                    int propNum = PacketUtils.readNextVarInt(packetData);
                                    for (int p = 0; p < propNum; p++)
                                    {
                                        string key = PacketUtils.readNextString(packetData);
                                        string val = PacketUtils.readNextString(packetData);
                                        if (PacketUtils.readNextBool(packetData))
                                            PacketUtils.readNextString(packetData);
                                    }
                                    PacketUtils.readNextVarInt(packetData);
                                    PacketUtils.readNextVarInt(packetData);
                                    if (PacketUtils.readNextBool(packetData))
                                        PacketUtils.readNextString(packetData);
                                    handler.OnPlayerJoin(uuid, name);
                                    break;
                                case 0x01: //Update gamemode
                                case 0x02: //Update latency
                                    PacketUtils.readNextVarInt(packetData);
                                    break;
                                case 0x03: //Update display name
                                    if (PacketUtils.readNextBool(packetData))
                                        PacketUtils.readNextString(packetData);
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
                        string name = PacketUtils.readNextString(packetData);
                        bool online = PacketUtils.readNextBool(packetData);
                        short ping = PacketUtils.readNextShort(packetData);
                        Guid FakeUUID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                        if (online)
                            handler.OnPlayerJoin(FakeUUID, name);
                        else handler.OnPlayerLeave(FakeUUID);
                    }
                    break;
                case PacketIncomingType.TabCompleteResult:
                    if (protocolversion >= PacketUtils.MC17w46aVersion)
                    {
                        autocomplete_transaction_id = PacketUtils.readNextVarInt(packetData);
                    }

                    if (protocolversion >= PacketUtils.MC17w47aVersion)
                    {
                        // Start of the text to replace - currently unused
                        PacketUtils.readNextVarInt(packetData);
                    }

                    if (protocolversion >= PacketUtils.MC18w06aVersion)
                    {
                        // Length of the text to replace - currently unused
                        PacketUtils.readNextVarInt(packetData);
                    }

                    int autocomplete_count = PacketUtils.readNextVarInt(packetData);
                    autocomplete_result.Clear();
                    for (int i = 0; i < autocomplete_count; i++)
                        autocomplete_result.Add(PacketUtils.readNextString(packetData));
                    autocomplete_received = true;

                    // In protocolversion >= MC18w06aVersion there is additional data if the match is a tooltip
                    // Don't worry about skipping remaining data since there is no useful for us (yet)
                    break;
                case PacketIncomingType.PluginMessage:
                    String channel = PacketUtils.readNextString(packetData);
                    if (protocolversion < PacketUtils.MC18Version)
                    {
                        if (forgeInfo == null)
                        {
                            // 1.7 and lower prefix plugin channel packets with the length.
                            // We can skip it, though.
                            PacketUtils.readNextShort(packetData);
                        }
                        else
                        {
                            // Forge does something even weirder with the length.
                            PacketUtils.readNextVarShort(packetData);
                        }
                    }

                    // The remaining data in the array is the entire payload of the packet.
                    handler.OnPluginChannelMessage(channel, packetData.ToArray());

                    #region Forge Login
                    if (forgeInfo != null && fmlHandshakeState != FMLHandshakeClientState.DONE)
                    {
                        if (channel == "FML|HS")
                        {
                            FMLHandshakeDiscriminator discriminator = (FMLHandshakeDiscriminator)PacketUtils.readNextByte(packetData);

                            if (discriminator == FMLHandshakeDiscriminator.HandshakeReset)
                            {
                                fmlHandshakeState = FMLHandshakeClientState.START;
                                return true;
                            }

                            switch (fmlHandshakeState)
                            {
                                case FMLHandshakeClientState.START:
                                    if (discriminator != FMLHandshakeDiscriminator.ServerHello)
                                        return false;

                                    // Send the plugin channel registration.
                                    // REGISTER is somewhat special in that it doesn't actually include length information,
                                    // and is also \0-separated.
                                    // Also, yes, "FML" is there twice.  Don't ask me why, but that's the way forge does it.
                                    string[] channels = { "FML|HS", "FML", "FML|MP", "FML", "FORGE" };
                                    SendPluginChannelPacket("REGISTER", Encoding.UTF8.GetBytes(string.Join("\0", channels)));

                                    byte fmlProtocolVersion = PacketUtils.readNextByte(packetData);

                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Forge protocol version : " + fmlProtocolVersion);

                                    if (fmlProtocolVersion >= 1)
                                        this.currentDimension = PacketUtils.readNextInt(packetData);

                                    // Tell the server we're running the same version.
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.ClientHello, new byte[] { fmlProtocolVersion });

                                    // Then tell the server that we're running the same mods.
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Sending falsified mod list to server...");
                                    byte[][] mods = new byte[forgeInfo.Mods.Count][];
                                    for (int i = 0; i < forgeInfo.Mods.Count; i++)
                                    {
                                        ForgeInfo.ForgeMod mod = forgeInfo.Mods[i];
                                        mods[i] = PacketUtils.concatBytes(PacketUtils.getString(mod.ModID), PacketUtils.getString(mod.Version));
                                    }
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.ModList,
                                                             PacketUtils.concatBytes(PacketUtils.getVarInt(forgeInfo.Mods.Count), PacketUtils.concatBytes(mods)));

                                    fmlHandshakeState = FMLHandshakeClientState.WAITINGSERVERDATA;

                                    return true;
                                case FMLHandshakeClientState.WAITINGSERVERDATA:
                                    if (discriminator != FMLHandshakeDiscriminator.ModList)
                                        return false;

                                    Thread.Sleep(2000);

                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Accepting server mod list...");
                                    // Tell the server that yes, we are OK with the mods it has
                                    // even though we don't actually care what mods it has.

                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.WAITINGSERVERDATA });

                                    fmlHandshakeState = FMLHandshakeClientState.WAITINGSERVERCOMPLETE;
                                    return false;
                                case FMLHandshakeClientState.WAITINGSERVERCOMPLETE:
                                    // The server now will tell us a bunch of registry information.
                                    // We need to read it all, though, until it says that there is no more.
                                    if (discriminator != FMLHandshakeDiscriminator.RegistryData)
                                        return false;

                                    if (protocolversion < PacketUtils.MC18Version)
                                    {
                                        // 1.7.10 and below have one registry
                                        // with blocks and items.
                                        int registrySize = PacketUtils.readNextVarInt(packetData);

                                        if (Settings.DebugMessages)
                                            ConsoleIO.WriteLineFormatted("§8Received registry with " + registrySize + " entries");

                                        fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                                    }
                                    else
                                    {
                                        // 1.8+ has more than one registry.

                                        bool hasNextRegistry = PacketUtils.readNextBool(packetData);
                                        string registryName = PacketUtils.readNextString(packetData);
                                        int registrySize = PacketUtils.readNextVarInt(packetData);
                                        if (Settings.DebugMessages)
                                            ConsoleIO.WriteLineFormatted("§8Received registry " + registryName + " with " + registrySize + " entries");
                                        if (!hasNextRegistry)
                                            fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                                    }

                                    return false;
                                case FMLHandshakeClientState.PENDINGCOMPLETE:
                                    // The server will ask us to accept the registries.
                                    // Just say yes.
                                    if (discriminator != FMLHandshakeDiscriminator.HandshakeAck)
                                        return false;
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Accepting server registries...");
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.PENDINGCOMPLETE });
                                    fmlHandshakeState = FMLHandshakeClientState.COMPLETE;

                                    return true;
                                case FMLHandshakeClientState.COMPLETE:
                                    // One final "OK".  On the actual forge source, a packet is sent from
                                    // the client to the client saying that the connection was complete, but
                                    // we don't need to do that.

                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.COMPLETE });
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLine("Forge server connection complete!");
                                    fmlHandshakeState = FMLHandshakeClientState.DONE;
                                    return true;
                            }
                        }
                    }
                    #endregion

                    return false;
                case PacketIncomingType.KickPacket:
                    handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(PacketUtils.readNextString(packetData)));
                    return false;
                case PacketIncomingType.NetworkCompressionTreshold:
                    if (protocolversion >= PacketUtils.MC18Version && protocolversion < PacketUtils.MC19Version)
                        compression_treshold = PacketUtils.readNextVarInt(packetData);
                    break;
                case PacketIncomingType.ResourcePackSend:
                    string url = PacketUtils.readNextString(packetData);
                    string hash = PacketUtils.readNextString(packetData);
                    //Send back "accepted" and "successfully loaded" responses for plugins making use of resource pack mandatory
                    byte[] responseHeader = new byte[0];
                    if (protocolversion < PacketUtils.MC110Version) //MC 1.10 does not include resource pack hash in responses
                        responseHeader = PacketUtils.concatBytes(PacketUtils.getVarInt(hash.Length), Encoding.UTF8.GetBytes(hash));
                    SendPacket(PacketOutgoingType.ResourcePackStatus, PacketUtils.concatBytes(responseHeader, PacketUtils.getVarInt(3))); //Accepted pack
                    SendPacket(PacketOutgoingType.ResourcePackStatus, PacketUtils.concatBytes(responseHeader, PacketUtils.getVarInt(0))); //Successfully loaded
                    break;
                default:
                    return false; //Ignored packet
            }
            return true; //Packet processed
        }

        /// <summary>
        /// Process chunk column data from the server and (un)load the chunk from the Minecraft world
        /// </summary>
        /// <param name="chunkX">Chunk X location</param>
        /// <param name="chunkZ">Chunk Z location</param>
        /// <param name="chunkMask">Chunk mask for reading data</param>
        /// <param name="chunkMask2">Chunk mask for some additional 1.7 metadata</param>
        /// <param name="hasSkyLight">Contains skylight info</param>
        /// <param name="chunksContinuous">Are the chunk continuous</param>
        /// <param name="cache">Cache for reading chunk data</param>
        private void ProcessChunkColumnData(int chunkX, int chunkZ, ushort chunkMask, ushort chunkMask2, bool hasSkyLight, bool chunksContinuous, List<byte> cache)
        {
            if (protocolversion >= PacketUtils.MC19Version)
            {
                // 1.9 and above chunk format
                // Unloading chunks is handled by a separate packet
                for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                {
                    if ((chunkMask & (1 << chunkY)) != 0)
                    {
                        byte bitsPerBlock = PacketUtils.readNextByte(cache);
                        bool usePalette = (bitsPerBlock <= 8);

                        int paletteLength = PacketUtils.readNextVarInt(cache);
                        int[] palette = new int[paletteLength];
                        for (int i = 0; i < paletteLength; i++)
                        {
                            palette[i] = PacketUtils.readNextVarInt(cache);
                        }

                        // Bit mask covering bitsPerBlock bits
                        // EG, if bitsPerBlock = 5, valueMask = 00011111 in binary
                        uint valueMask = (uint)((1 << bitsPerBlock) - 1);

                        ulong[] dataArray = PacketUtils.readNextULongArray(cache);

                        Chunk chunk = new Chunk();

                        if (dataArray.Length > 0)
                        {
                            for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                            {
                                for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                                {
                                    for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                                    {
                                        int blockNumber = (blockY * Chunk.SizeZ + blockZ) * Chunk.SizeX + blockX;

                                        int startLong = (blockNumber * bitsPerBlock) / 64;
                                        int startOffset = (blockNumber * bitsPerBlock) % 64;
                                        int endLong = ((blockNumber + 1) * bitsPerBlock - 1) / 64;

                                        // TODO: In the future a single ushort may not store the entire block id;
                                        // the Block code may need to change.
                                        ushort blockId;
                                        if (startLong == endLong)
                                        {
                                            blockId = (ushort)((dataArray[startLong] >> startOffset) & valueMask);
                                        }
                                        else
                                        {
                                            int endOffset = 64 - startOffset;
                                            blockId = (ushort)((dataArray[startLong] >> startOffset | dataArray[endLong] << endOffset) & valueMask);
                                        }

                                        if (usePalette)
                                        {
                                            // Get the real block ID out of the palette
                                            blockId = (ushort)palette[blockId];
                                        }

                                        chunk[blockX, blockY, blockZ] = new Block(blockId);
                                    }
                                }
                            }
                        }

                        //We have our chunk, save the chunk into the world
                        if (handler.GetWorld()[chunkX, chunkZ] == null)
                            handler.GetWorld()[chunkX, chunkZ] = new ChunkColumn();
                        handler.GetWorld()[chunkX, chunkZ][chunkY] = chunk;

                        //Skip block light
                        PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                        //Skip sky light
                        if (this.currentDimension == 0)
                            // Sky light is not sent in the nether or the end
                            PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
                    }
                }

                // Don't worry about skipping remaining data since there is no useful data afterwards in 1.9
                // (plus, it would require parsing the tile entity lists' NBT)
            }
            else if (protocolversion >= PacketUtils.MC18Version)
            {
                // 1.8 chunk format
                if (chunksContinuous && chunkMask == 0)
                {
                    //Unload the entire chunk column
                    handler.GetWorld()[chunkX, chunkZ] = null;
                }
                else
                {
                    //Load chunk data from the server
                    for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                    {
                        if ((chunkMask & (1 << chunkY)) != 0)
                        {
                            Chunk chunk = new Chunk();

                            //Read chunk data, all at once for performance reasons, and build the chunk object
                            Queue<ushort> queue = new Queue<ushort>(PacketUtils.readNextUShortsLittleEndian(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ, cache));
                            for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                                for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                                    for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                                        chunk[blockX, blockY, blockZ] = new Block(queue.Dequeue());

                            //We have our chunk, save the chunk into the world
                            if (handler.GetWorld()[chunkX, chunkZ] == null)
                                handler.GetWorld()[chunkX, chunkZ] = new ChunkColumn();
                            handler.GetWorld()[chunkX, chunkZ][chunkY] = chunk;
                        }
                    }

                    //Skip light information
                    for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                    {
                        if ((chunkMask & (1 << chunkY)) != 0)
                        {
                            //Skip block light
                            PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                            //Skip sky light
                            if (hasSkyLight)
                                PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
                        }
                    }

                    //Skip biome metadata
                    if (chunksContinuous)
                        PacketUtils.readData(Chunk.SizeX * Chunk.SizeZ, cache);
                }
            }
            else
            {
                // 1.7 chunk format
                if (chunksContinuous && chunkMask == 0)
                {
                    //Unload the entire chunk column
                    handler.GetWorld()[chunkX, chunkZ] = null;
                }
                else
                {
                    //Count chunk sections
                    int sectionCount = 0;
                    int addDataSectionCount = 0;
                    for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                    {
                        if ((chunkMask & (1 << chunkY)) != 0)
                            sectionCount++;
                        if ((chunkMask2 & (1 << chunkY)) != 0)
                            addDataSectionCount++;
                    }

                    //Read chunk data, unpacking 4-bit values into 8-bit values for block metadata
                    Queue<byte> blockTypes = new Queue<byte>(PacketUtils.readData(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount, cache));
                    Queue<byte> blockMeta = new Queue<byte>();
                    foreach (byte packed in PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache))
                    {
                        byte hig = (byte)(packed >> 4);
                        byte low = (byte)(packed & (byte)0x0F);
                        blockMeta.Enqueue(hig);
                        blockMeta.Enqueue(low);
                    }

                    //Skip data we don't need
                    PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache);          //Block light
                    if (hasSkyLight)
                        PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache);      //Sky light
                    PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * addDataSectionCount) / 2, cache);   //BlockAdd
                    if (chunksContinuous)
                        PacketUtils.readData(Chunk.SizeX * Chunk.SizeZ, cache);                                         //Biomes

                    //Load chunk data
                    for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                    {
                        if ((chunkMask & (1 << chunkY)) != 0)
                        {
                            Chunk chunk = new Chunk();

                            for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                                for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                                    for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                                        chunk[blockX, blockY, blockZ] = new Block(blockTypes.Dequeue(), blockMeta.Dequeue());

                            if (handler.GetWorld()[chunkX, chunkZ] == null)
                                handler.GetWorld()[chunkX, chunkZ] = new ChunkColumn();
                            handler.GetWorld()[chunkX, chunkZ][chunkY] = chunk;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Start the updating thread. Should be called after login success.
        /// </summary>
        private void StartUpdating()
        {
            netRead = new Thread(new ThreadStart(Updater));
            netRead.Name = "ProtocolPacketHandler";
            netRead.Start();
        }

        /// <summary>
        /// Disconnect from the server, cancel network reading.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (netRead != null)
                {
                    netRead.Abort();
                    c.Close();
                }
            }
            catch { }
        }

        /// <summary>
        /// Read some data directly from the network
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <returns>The data read from the network as an array</returns>
        private byte[] readDataRAW(int offset)
        {
            if (offset > 0)
            {
                try
                {
                    byte[] cache = new byte[offset];
                    Receive(cache, 0, offset, SocketFlags.None);
                    return cache;
                }
                catch (OutOfMemoryException) { }
            }
            return new byte[] { };
        }

        /// <summary>
        /// Read an integer from the network
        /// </summary>
        /// <returns>The integer</returns>
        private int readNextVarIntRAW()
        {
            int i = 0;
            int j = 0;
            int k = 0;
            byte[] tmp = new byte[1];
            while (true)
            {
                Receive(tmp, 0, 1, SocketFlags.None);
                k = tmp[0];
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }

        /// <summary>
        /// C-like atoi function for parsing an int from string
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <returns>Int parsed</returns>
        private static int atoi(string str)
        {
            return int.Parse(new string(str.Trim().TakeWhile(char.IsDigit).ToArray()));
        }

        /// <summary>
        /// Network reading method. Read bytes from the socket or encrypted socket.
        /// </summary>
        private void Receive(byte[] buffer, int start, int offset, SocketFlags f)
        {
            int read = 0;
            while (read < offset)
            {
                if (encrypted)
                {
                    read += s.Read(buffer, start + read, offset - read);
                }
                else read += c.Client.Receive(buffer, start + read, offset - read, f);
            }
        }

        /// <summary>
        /// Send a forge plugin channel packet ("FML|HS").  Compression and encryption will be handled automatically
        /// </summary>
        /// <param name="discriminator">Discriminator to use.</param>
        /// <param name="data">packet Data</param>
        private void SendForgeHandshakePacket(FMLHandshakeDiscriminator discriminator, byte[] data)
        {
            SendPluginChannelPacket("FML|HS", PacketUtils.concatBytes(new byte[] { (byte)discriminator }, data));
        }

        /// <summary>
        /// Send a packet to the server.  Packet ID, compression, and encryption will be handled automatically.
        /// </summary>
        /// <param name="packet">packet type</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(PacketOutgoingType packet, IEnumerable<byte> packetData)
        {
            SendPacket(getPacketOutgoingID(packet, protocolversion), packetData);
        }

        /// <summary>
        /// Send a packet to the server.  Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="packetID">packet ID</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetID, IEnumerable<byte> packetData)
        {
            //The inner packet
            byte[] the_packet = PacketUtils.concatBytes(PacketUtils.getVarInt(packetID), packetData.ToArray());

            if (compression_treshold > 0) //Compression enabled?
            {
                if (the_packet.Length >= compression_treshold) //Packet long enough for compressing?
                {
                    byte[] compressed_packet = ZlibUtils.Compress(the_packet);
                    the_packet = PacketUtils.concatBytes(PacketUtils.getVarInt(the_packet.Length), compressed_packet);
                }
                else
                {
                    byte[] uncompressed_length = PacketUtils.getVarInt(0); //Not compressed (short packet)
                    the_packet = PacketUtils.concatBytes(uncompressed_length, the_packet);
                }
            }

            SendRAW(PacketUtils.concatBytes(PacketUtils.getVarInt(the_packet.Length), the_packet));
        }

        /// <summary>
        /// Send raw data to the server. Encryption will be handled automatically.
        /// </summary>
        /// <param name="buffer">data to send</param>
        private void SendRAW(byte[] buffer)
        {
            if (encrypted)
            {
                s.Write(buffer, 0, buffer.Length);
            }
            else c.Client.Send(buffer);
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login()
        {
            byte[] protocol_version = PacketUtils.getVarInt(protocolversion);
            string server_address = handler.GetServerHost() + (forgeInfo != null ? "\0FML\0" : "");
            byte[] server_port = BitConverter.GetBytes((ushort)handler.GetServerPort()); Array.Reverse(server_port);
            byte[] next_state = PacketUtils.getVarInt(2);
            byte[] handshake_packet = PacketUtils.concatBytes(protocol_version, PacketUtils.getString(server_address), server_port, next_state);

            SendPacket(0x00, handshake_packet);

            byte[] login_packet = PacketUtils.getString(handler.GetUsername());

            SendPacket(0x00, login_packet);

            int packetID = -1;
            List<byte> packetData = new List<byte>();
            while (true)
            {
                readNextPacket(ref packetID, packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(PacketUtils.readNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x01) //Encryption request
                {
                    string serverID = PacketUtils.readNextString(packetData);
                    byte[] Serverkey = PacketUtils.readNextByteArray(protocolversion, packetData);
                    byte[] token = PacketUtils.readNextByteArray(protocolversion, packetData);
                    return StartEncryption(handler.GetUserUUID(), handler.GetSessionID(), token, serverID, Serverkey);
                }
                else if (packetID == 0x02) //Login successful
                {
                    ConsoleIO.WriteLineFormatted("§8Server is in offline mode.");
                    login_phase = false;

                    if (forgeInfo != null)
                    {
                        // Do the forge handshake.
                        if (!CompleteForgeHandshake())
                        {
                            return false;
                        }
                    }

                    StartUpdating();
                    return true; //No need to check session or start encryption
                }
                else handlePacket(packetID, packetData);
            }
        }

        /// <summary>
        /// Completes the Minecraft Forge handshake.
        /// </summary>
        /// <returns>Whether the handshake was successful.</returns>
        private bool CompleteForgeHandshake()
        {
            int packetID = -1;
            List<byte> packetData = new List<byte>();

            while (fmlHandshakeState != FMLHandshakeClientState.DONE)
            {
                readNextPacket(ref packetID, packetData);

                if (packetID == 0x40) // Disconnect
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(PacketUtils.readNextString(packetData)));
                    return false;
                }
                else
                {
                    handlePacket(packetID, packetData);
                }
            }

            return true;
        }

        /// <summary>
        /// Start network encryption. Automatically called by Login() if the server requests encryption.
        /// </summary>
        /// <returns>True if encryption was successful</returns>
        private bool StartEncryption(string uuid, string sessionID, byte[] token, string serverIDhash, byte[] serverKey)
        {
            System.Security.Cryptography.RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverKey);
            byte[] secretKey = CryptoHandler.GenerateAESPrivateKey();

            if (Settings.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8Crypto keys & hash generated.");

            if (serverIDhash != "-")
            {
                Console.WriteLine("Checking Session...");
                if (!ProtocolHandler.SessionCheck(uuid, sessionID, CryptoHandler.getServerHash(serverIDhash, serverKey, secretKey)))
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, "Failed to check session.");
                    return false;
                }
            }

            //Encrypt the data
            byte[] key_enc = PacketUtils.getArray(protocolversion, RSAService.Encrypt(secretKey, false));
            byte[] token_enc = PacketUtils.getArray(protocolversion, RSAService.Encrypt(token, false));

            //Encryption Response packet
            SendPacket(0x01, PacketUtils.concatBytes(key_enc, token_enc));

            //Start client-side encryption
            s = CryptoHandler.getAesStream(c.GetStream(), secretKey);
            encrypted = true;

            //Process the next packet
            int packetID = -1;
            List<byte> packetData = new List<byte>();
            while (true)
            {
                readNextPacket(ref packetID, packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(PacketUtils.readNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x02) //Login successful
                {
                    login_phase = false;

                    if (forgeInfo != null)
                    {
                        // Do the forge handshake.
                        if (!CompleteForgeHandshake())
                        {
                            return false;
                        }
                    }

                    StartUpdating();
                    return true;
                }
                else handlePacket(packetID, packetData);
            }
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return protocolversion >= PacketUtils.MC111Version
                ? 256
                : 100;
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatMessage(string message)
        {
            if (String.IsNullOrEmpty(message))
                return true;
            try
            {
                byte[] message_packet = PacketUtils.getString(message);
                SendPacket(PacketOutgoingType.ChatMessage, message_packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        /// <summary>
        /// Send a respawn packet to the server
        /// </summary>
        /// <returns>True if properly sent</returns>
        public bool SendRespawnPacket()
        {
            try
            {
                SendPacket(PacketOutgoingType.ClientStatus, new byte[] { 0 });
                return true;
            }
            catch (SocketException) { return false; }
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
            if (protocolversion >= PacketUtils.MC113Version)
            {
                return SendPluginChannelPacket("minecraft:brand", PacketUtils.getString(brandInfo));
            }
            else
            {
                return SendPluginChannelPacket("MC|Brand", PacketUtils.getString(brandInfo));
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
                List<byte> fields = new List<byte>();
                fields.AddRange(PacketUtils.getString(language));
                fields.Add(viewDistance);
                fields.AddRange(protocolversion >= PacketUtils.MC19Version
                    ? PacketUtils.getVarInt(chatMode)
                    : new byte[] { chatMode });
                fields.Add(chatColors ? (byte)1 : (byte)0);
                if (protocolversion < PacketUtils.MC18Version)
                {
                    fields.Add(difficulty);
                    fields.Add((byte)(skinParts & 0x1)); //show cape
                }
                else fields.Add(skinParts);
                if (protocolversion >= PacketUtils.MC19Version)
                    fields.AddRange(PacketUtils.getVarInt(mainHand));
                SendPacket(PacketOutgoingType.ClientSettings, fields);
            }
            catch (SocketException) { }
            return false;
        }

        /// <summary>
        /// Send a location update to the server
        /// </summary>
        /// <param name="location">The new location of the player</param>
        /// <param name="onGround">True if the player is on the ground</param>
        /// <param name="yawpitch">Yaw and pitch (optional and currently not parsed)</param>
        /// <returns>True if the location update was successfully sent</returns>
        public bool SendLocationUpdate(Location location, bool onGround, byte[] yawpitch = null)
        {
            if (Settings.TerrainAndMovements)
            {
                PacketOutgoingType packetType;
                if (yawpitch != null && yawpitch.Length == 8)
                {
                    packetType = PacketOutgoingType.PlayerPositionAndLook;
                }
                else
                {
                    yawpitch = new byte[0];
                    packetType = PacketOutgoingType.PlayerPosition;
                }

                try
                {
                    SendPacket(packetType, PacketUtils.concatBytes(
                        PacketUtils.getDouble(location.X),
                        PacketUtils.getDouble(location.Y),
                        protocolversion < PacketUtils.MC18Version
                            ? PacketUtils.getDouble(location.Y + 1.62)
                            : new byte[0],
                        PacketUtils.getDouble(location.Z),
                        yawpitch,
                        new byte[] { onGround ? (byte)1 : (byte)0 }));
                    return true;
                }
                catch (SocketException) { return false; }
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
                if (protocolversion < PacketUtils.MC18Version)
                {
                    byte[] length = BitConverter.GetBytes((short)data.Length);
                    Array.Reverse(length);

                    SendPacket(PacketOutgoingType.PluginMessage, PacketUtils.concatBytes(PacketUtils.getString(channel), length, data));
                }
                else
                {
                    SendPacket(PacketOutgoingType.PluginMessage, PacketUtils.concatBytes(PacketUtils.getString(channel), data));
                }

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            try
            {
                c.Close();
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }
        }

        /// <summary>
        /// Autocomplete text while typing username or command
        /// </summary>
        /// <param name="BehindCursor">Text behind cursor</param>
        /// <returns>Completed text</returns>
        IEnumerable<string> IAutoComplete.AutoComplete(string BehindCursor)
        {
            if (String.IsNullOrEmpty(BehindCursor))
                return new string[] { };

            byte[] transaction_id = PacketUtils.getVarInt(autocomplete_transaction_id);
            byte[] assume_command = new byte[] { 0x00 };
            byte[] has_position = new byte[] { 0x00 };

            byte[] tabcomplete_packet = new byte[] { };

            if (protocolversion >= PacketUtils.MC18Version)
            {
                if (protocolversion >= PacketUtils.MC17w46aVersion)
                {
                    tabcomplete_packet = PacketUtils.concatBytes(tabcomplete_packet, transaction_id);
                    tabcomplete_packet = PacketUtils.concatBytes(tabcomplete_packet, PacketUtils.getString(BehindCursor));
                }
                else
                {
                    tabcomplete_packet = PacketUtils.concatBytes(tabcomplete_packet, PacketUtils.getString(BehindCursor));

                    if (protocolversion >= PacketUtils.MC19Version)
                    {
                        tabcomplete_packet = PacketUtils.concatBytes(tabcomplete_packet, assume_command);
                    }

                    tabcomplete_packet = PacketUtils.concatBytes(tabcomplete_packet, has_position);
                }
            }
            else
            {
                tabcomplete_packet = PacketUtils.concatBytes(PacketUtils.getString(BehindCursor));
            }

            autocomplete_received = false;
            autocomplete_result.Clear();
            autocomplete_result.Add(BehindCursor);
            SendPacket(PacketOutgoingType.TabComplete, tabcomplete_packet);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
            if (autocomplete_result.Count > 0)
                ConsoleIO.WriteLineFormatted("§8" + String.Join(" ", autocomplete_result), false);
            return autocomplete_result;
        }

        /// <summary>
        /// Ping a Minecraft server to get information about the server
        /// </summary>
        /// <returns>True if ping was successful</returns>
        public static bool doPing(string host, int port, ref int protocolversion, ref ForgeInfo forgeInfo)
        {
            string version = "";
            TcpClient tcp = ProxyHandler.newTcpClient(host, port);
            tcp.ReceiveBufferSize = 1024 * 1024;

            byte[] packet_id = PacketUtils.getVarInt(0);
            byte[] protocol_version = PacketUtils.getVarInt(-1);
            byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
            byte[] next_state = PacketUtils.getVarInt(1);
            byte[] packet = PacketUtils.concatBytes(packet_id, protocol_version, PacketUtils.getString(host), server_port, next_state);
            byte[] tosend = PacketUtils.concatBytes(PacketUtils.getVarInt(packet.Length), packet);

            tcp.Client.Send(tosend, SocketFlags.None);

            byte[] status_request = PacketUtils.getVarInt(0);
            byte[] request_packet = PacketUtils.concatBytes(PacketUtils.getVarInt(status_request.Length), status_request);

            tcp.Client.Send(request_packet, SocketFlags.None);

            Protocol18Handler ComTmp = new Protocol18Handler(tcp);
            int packetLength = ComTmp.readNextVarIntRAW();
            if (packetLength > 0) //Read Response length
            {
                List<byte> packetData = new List<byte>(ComTmp.readDataRAW(packetLength));
                if (PacketUtils.readNextVarInt(packetData) == 0x00) //Read Packet ID
                {
                    string result = PacketUtils.readNextString(packetData); //Get the Json data

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
                                protocolversion = atoi(versionData.Properties["protocol"].StringValue);

                            //Automatic fix for BungeeCord 1.8 reporting itself as 1.7...
                            if (protocolversion < 47 && version.Split(' ', '/').Contains("1.8"))
                                protocolversion = ProtocolHandler.MCVer2ProtocolVersion("1.8.0");

                            // Check for forge on the server.
                            if (jsonData.Properties.ContainsKey("modinfo") && jsonData.Properties["modinfo"].Type == Json.JSONData.DataType.Object)
                            {
                                Json.JSONData modData = jsonData.Properties["modinfo"];
                                if (modData.Properties.ContainsKey("type") && modData.Properties["type"].StringValue == "FML")
                                {
                                    forgeInfo = new ForgeInfo(modData);

                                    if (forgeInfo.Mods.Any())
                                    {
                                        if (Settings.DebugMessages)
                                        {
                                            ConsoleIO.WriteLineFormatted("§8Server is running Forge. Mod list:");
                                            foreach (ForgeInfo.ForgeMod mod in forgeInfo.Mods)
                                            {
                                                ConsoleIO.WriteLineFormatted("§8  " + mod.ToString());
                                            }
                                        }
                                        else ConsoleIO.WriteLineFormatted("§8Server is running Forge.");
                                    }
                                    else forgeInfo = null;
                                }
                            }

                            ConsoleIO.WriteLineFormatted("§8Server version : " + version + " (protocol v" + protocolversion + (forgeInfo != null ? ", with Forge)." : ")."));

                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
