using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MinecraftClient.Protocol.Handlers;
using System.Runtime.InteropServices;
using Ionic.Zip;
using MinecraftClient.Mapping;
using Org.BouncyCastle.Crypto.Utilities;

namespace MinecraftClient
{
    public class ReplayHandler
    {
        public string RecordingTmpFileName { get { return @"recording.tmcpr"; } }
        public string ReplayFileName { get { return @"whhhh.mcpr"; } }
        public MetaDataHandler MetaData;

        private DataTypes dataTypes;
        private int protocolVersion;
        private BinaryWriter recordStream;
        private DateTime recordStartTime;
        private DateTime lastPacketTime;

        private static bool logOutput = true;

        private int playerEntityID;
        private Guid playerUUID;
        private Location playerLastPosition;
        private float playerLastYaw;
        private float playerLastPitch;

        public ReplayHandler(int protocolVersion)
        {
            Initialize(protocolVersion);
        }
        public ReplayHandler(int protocolVersion, string serverName)
        {
            Initialize(protocolVersion);
            this.MetaData.serverName = serverName;
        }
        private void Initialize(int protocolVersion)
        {
            this.dataTypes = new DataTypes(protocolVersion);
            this.protocolVersion = protocolVersion;
            recordStream = new BinaryWriter(new FileStream(RecordingTmpFileName, FileMode.Create));
            recordStartTime = DateTime.Now;

            MetaData = new MetaDataHandler();
            MetaData.date = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            MetaData.protocol = protocolVersion;

            playerLastPosition = new Location(0, 0, 0);
            WriteLog("Start recording.");
        }

        ~ReplayHandler()
        {
            OnShutDown();
        }

        public void SetClientEntityID(int entityID)
        {
            playerEntityID = entityID;
        }

        public void SetClientPlayerUUID(Guid uuid)
        {
            playerUUID = uuid;
        }

        #region File and stream handling

        public void CloseRecordStream()
        {
            try
            {
                recordStream.Flush();
                recordStream.Close();
            }
            catch { }
        }

        /// <summary>
        /// Should call once before program exit
        /// </summary>
        public void OnShutDown()
        {
            MetaData.duration = Convert.ToInt32((lastPacketTime - recordStartTime).TotalMilliseconds);
            MetaData.SaveToFile();
            CloseRecordStream();
            CreateReplayFile();
        }

        /// <summary>
        /// Create the replay file for Replay mod to read
        /// </summary>
        public void CreateReplayFile()
        {
            WriteLog("Creating replay file.");
            using (Stream recordingFile = new FileStream(RecordingTmpFileName, FileMode.Open)) // what if I want to save replay while MCC running? thread-safe?
            using (Stream metaDatFile = new FileStream(MetaData.metaDataFileName, FileMode.Open))
            using (ZipOutputStream zs = new ZipOutputStream(ReplayFileName))
            {
                zs.PutNextEntry(RecordingTmpFileName);
                recordingFile.CopyTo(zs);
                zs.PutNextEntry(MetaData.metaDataFileName);
                metaDatFile.CopyTo(zs);
                zs.Close();
            }
            WriteLog("Replay file created.");
        }

        #endregion

        #region Packet related method

        /// <summary>
        /// Add a packet from network
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        /// <param name="isLogin"></param>
        /// <param name="isInbound"></param>
        public void AddPacket(int packetID, IEnumerable<byte> packetData, bool isLogin, bool isInbound)
        {
            try 
            {
                if (!isInbound)
                {
                    HandleOutBoundPacket(packetID, packetData, isLogin);
                    return;
                } else HandleInBoundPacket(packetID, packetData, isLogin);
                if (PacketShouldSave(packetID, isLogin, isInbound))
                    AddPacket(packetID, packetData);
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLine("Exception in replay: " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Add packet directly without checking (internal use only)
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        private void AddPacket(int packetID, IEnumerable<byte> packetData)
        {
            lastPacketTime = DateTime.Now;
            // build raw packet
            // format: packetID + packetData
            List<byte> rawPacket = new List<byte>();
            rawPacket.AddRange(dataTypes.GetVarInt(packetID).ToArray());
            rawPacket.AddRange(packetData.ToArray());
            // build format
            // format: timestamp + packetLength + RawPacket
            List<byte> line = new List<byte>();
            int nowTime = Convert.ToInt32((lastPacketTime - recordStartTime).TotalMilliseconds);
            line.AddRange(BitConverter.GetBytes((Int32)nowTime).Reverse().ToArray());
            line.AddRange(BitConverter.GetBytes((Int32)rawPacket.Count).Reverse().ToArray());
            line.AddRange(rawPacket.ToArray());
            // Write out to the file
            recordStream.Write(line.ToArray());
        }

        public void OnPlayerSpawn(Guid uuid, string name)
        {
            // Metadata has a field for storing uuid for all players entered client render range
            MetaData.AddPlayerUUID(uuid);
        }

        private bool PacketShouldSave(int packetID, bool isLogin, bool isInbound)
        {
            if (!isInbound) // save inbound only
                return false;
            if (!isLogin) // save all play state packet
            {
                return true;
            }
            else
            { // is login
                if (packetID == 0x02) // login success
                {
                    // Add fake spawn player packet for spawning client player
                    return true;
                }
                else return false;
            }
        }

        /// <summary>
        /// Used to gather information needed
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        /// <param name="isLogin"></param>
        private void HandleInBoundPacket(int packetID, IEnumerable<byte> packetData, bool isLogin)
        {
            List<byte> clone = packetData.ToArray().ToList();
            Queue<byte> p = new Queue<byte>(clone);
            // Login success. Get player UUID
            if (isLogin && packetID == 0x02)
            {
                Guid uuid;
                if (protocolVersion < Protocol18Handler.MC116Version)
                {
                    if (Guid.TryParse(dataTypes.ReadNextString(p), out uuid))
                    {
                        SetClientPlayerUUID(uuid);
                    }
                }
                else
                {
                    SetClientPlayerUUID(dataTypes.ReadNextUUID(p));
                }
            }
            // Get client player location for calculating movement delta later
            if (Protocol18PacketTypes.GetPacketIncomingType(packetID, protocolVersion) == PacketIncomingType.PlayerPositionAndLook)
            {
                double x = dataTypes.ReadNextDouble(p);
                double y = dataTypes.ReadNextDouble(p);
                double z = dataTypes.ReadNextDouble(p);
                float yaw = dataTypes.ReadNextFloat(p);
                float pitch = dataTypes.ReadNextFloat(p);
                byte locMask = dataTypes.ReadNextByte(p);

                playerLastPitch = pitch;
                playerLastYaw = yaw;
                if (protocolVersion >= Protocol18Handler.MC18Version)
                {
                    playerLastPosition.X = (locMask & 1 << 0) != 0 ? playerLastPosition.X + x : x;
                    playerLastPosition.Y = (locMask & 1 << 1) != 0 ? playerLastPosition.Y + y : y;
                    playerLastPosition.Z = (locMask & 1 << 2) != 0 ? playerLastPosition.Z + z : z;
                }
                else
                {
                    playerLastPosition.X = x;
                    playerLastPosition.Y = y;
                    playerLastPosition.Z = z;
                }

                // Add spawn player for client player after 
                // we have client player location information
                AddPacket(GetSpawnPlayerPacketID(protocolVersion),
                    GetSpawnPlayerPacket(playerEntityID, playerUUID, playerLastPosition, playerLastPitch, playerLastYaw));
                MetaData.AddPlayerUUID(playerUUID);
            }
        }

        /// <summary>
        /// Handle outbound packet (i.e. client player movement)
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        /// <param name="isLogin"></param>
        private void HandleOutBoundPacket(int packetID, IEnumerable<byte> packetData, bool isLogin)
        {
            var packetType = GetPacketTypeOut(packetID, protocolVersion);
            if (packetType == PacketOutgoingType.PlayerPosition
                || packetType == PacketOutgoingType.PlayerPositionAndLook)
            {
                // translate them to incoming entitymovement packet then save them
            }
        }

        /// <summary>
        /// Translate client outgoing movement packet to incoming entitymovement packet
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        /// <returns></returns>
        private byte[] PlayerPositionPacketC2S(int packetID, IEnumerable<byte> packetData)
        {
            Queue<byte> p = new Queue<byte>(packetData);
            double x = dataTypes.ReadNextDouble(p);
            double y = dataTypes.ReadNextDouble(p);
            if (protocolVersion < Protocol18Handler.MC18Version)
                dataTypes.ReadNextDouble(p); // head position - useless
            double z = dataTypes.ReadNextDouble(p);

            Location delta = (new Location(x, y, z)) - playerLastPosition;

            return new byte[0];
        }

        private static PacketOutgoingType GetPacketTypeOut(int packetID, int protocol)
        {
            if (protocol <= Protocol18Handler.MC18Version) // MC 1.7 and 1.8
            {
                switch (packetID)
                {
                    case 0x04: return PacketOutgoingType.PlayerPosition;
                    case 0x06: return PacketOutgoingType.PlayerPositionAndLook;
                }
            }
            else if (protocol <= Protocol18Handler.MC1112Version) // MC 1.9, 1,10 and 1.11
            {
                switch (packetID)
                {
                    case 0x0C: return PacketOutgoingType.PlayerPosition;
                    case 0x0D: return PacketOutgoingType.PlayerPositionAndLook;
                }
            }
            else if (protocol <= Protocol18Handler.MC112Version) // MC 1.12
            {
                switch (packetID)
                {
                    case 0x0E: return PacketOutgoingType.PlayerPosition;
                    case 0x0F: return PacketOutgoingType.PlayerPositionAndLook;
                }
            }
            else if (protocol <= Protocol18Handler.MC1122Version) // 1.12.2
            {
                switch (packetID)
                {
                    case 0x0D: return PacketOutgoingType.PlayerPosition;
                    case 0x0E: return PacketOutgoingType.PlayerPositionAndLook;
                }
            }
            else if (protocol < Protocol18Handler.MC114Version) // MC 1.13 to 1.13.2
            {
                switch (packetID)
                {
                    case 0x10: return PacketOutgoingType.PlayerPosition;
                    case 0x11: return PacketOutgoingType.PlayerPositionAndLook;
                }
            }
            else if (protocol <= Protocol18Handler.MC1152Version) //MC 1.14 to 1.15.2
            {
                switch (packetID)
                {
                    case 0x11: return PacketOutgoingType.PlayerPosition;
                    case 0x12: return PacketOutgoingType.PlayerPositionAndLook;
                }
            }
            else if (protocol <= Protocol18Handler.MC1161Version) // MC 1.16 and 1.16.1
            {
                switch (packetID)
                {
                    case 0x12: return PacketOutgoingType.PlayerPosition;
                    case 0x13: return PacketOutgoingType.PlayerPositionAndLook;
                }
            }
            else
            {
                switch (packetID)
                {
                    case 0x12: return PacketOutgoingType.PlayerPosition;
                    case 0x13: return PacketOutgoingType.PlayerPositionAndLook;
                }
            }

            return PacketOutgoingType.KeepAlive;
        }

        private static int GetSpawnPlayerPacketID(int protocol)
        {
            if (protocol < Protocol18Handler.MC116Version)
                return 0x05;
            else return 0x04;
        }

        private byte[] GetSpawnPlayerPacket(int entityID, Guid playerUUID, Location location, double pitch, double yaw)
        {
            List<byte> packet = new List<byte>();
            packet.AddRange(dataTypes.GetVarInt(entityID));
            packet.AddRange(playerUUID.ToBigEndianBytes());
            packet.AddRange(dataTypes.GetDouble(location.X));
            packet.AddRange(dataTypes.GetDouble(location.Y));
            packet.AddRange(dataTypes.GetDouble(location.Z));
            packet.Add((byte)0);
            packet.Add((byte)0);
            return packet.ToArray();
        }

        #endregion

        #region Helper method

        private static void WriteLog(string t)
        {
            if (logOutput)
                ConsoleIO.WriteLogLine("[Replay] " + t);
        }

        private static void WriteDebugLog(string t)
        {
            if (Settings.DebugMessages && logOutput)
                WriteLog(t);
        }

        #endregion
    }

    public class MetaDataHandler
    {
        public string metaDataFileName { get { return @"metaData.json"; } }

        public bool singlePlayer = false;
        public string serverName;
        public int duration = 0; // duration of the whole replay
        public long date; // start time of the recording in unix timestamp milliseconds
        public string mcversion = "0.0"; // e.g. 1.8. TODO: Convert protocol number to MC Version string;
        public string fileFormat = "MCPR";
        public int fileFormatVersion = 14; // 14 is what I found in metadata generated in 1.15.2 replay mod
        public int protocol;
        public string generator = "MCC"; // The program which generated the file
        public int selfId = -1;
        public List<string> players; // Array of UUIDs of all players which can be seen in the replay

        public MetaDataHandler()
        {
            players = new List<string>();
        }

        /// <summary>
        /// Add a player's UUID who appeared in the replay
        /// </summary>
        /// <param name="uuid"></param>
        public void AddPlayerUUID(Guid uuid)
        {
            players.Add(uuid.ToString());
        }

        /// <summary>
        /// Export metadata to JSON string
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            return "{"
                + "\"singleplayer\":" + singlePlayer.ToString().ToLower() + ","
                + "\"serverName\":\"" + serverName + "\","
                + "\"duration\":" + duration + ","
                + "\"date\":" + date + ","
                + "\"mcversion\":\"" + mcversion + "\","
                + "\"fileFormat\":\"" + fileFormat + "\","
                + "\"fileFormatVersion\":" + fileFormatVersion + ","
                + "\"protocol\":" + protocol + ","
                + "\"generator\":\"" + generator + "\","
                + "\"selfId\":" + selfId + ","
                + "\"player\":" + GetPlayersArray()
                + "}";
        }

        /// <summary>
        /// Save metadata to disk file
        /// </summary>
        public void SaveToFile()
        {
            File.WriteAllText(metaDataFileName, ToJson());
        }

        /// <summary>
        /// Get players UUID json array string
        /// </summary>
        /// <returns></returns>
        private string GetPlayersArray()
        {
            if (players.Count == 0)
                return "[]";
            string[] tmp = players.ToArray();
            for (int i = 0; i < players.Count; i++)
            {
                tmp[i] = "\"" + players[i] + "\"";
            }
            return "[" + string.Join(",", tmp) + "]";
        }
    }

    public static class GuidExtensions
    {
        /// <summary>
        /// A CLSCompliant method to convert a Java big-endian Guid to a .NET 
        /// little-endian Guid.
        /// The Guid Constructor (UInt32, UInt16, UInt16, Byte, Byte, Byte, Byte,
        ///  Byte, Byte, Byte, Byte) is not CLSCompliant.
        /// </summary>
        public static Guid ToLittleEndian(this Guid javaGuid)
        {
            byte[] net = new byte[16];
            byte[] java = javaGuid.ToByteArray();
            for (int i = 8; i < 16; i++)
            {
                net[i] = java[i];
            }
            net[3] = java[0];
            net[2] = java[1];
            net[1] = java[2];
            net[0] = java[3];
            net[5] = java[4];
            net[4] = java[5];
            net[6] = java[7];
            net[7] = java[6];
            return new Guid(net);
        }

        /// <summary>
        /// Converts little-endian .NET guids to big-endian Java guids:
        /// </summary>
        public static Guid ToBigEndian(this Guid netGuid)
        {
            byte[] java = new byte[16];
            byte[] net = netGuid.ToByteArray();
            for (int i = 8; i < 16; i++)
            {
                java[i] = net[i];
            }
            java[0] = net[3];
            java[1] = net[2];
            java[2] = net[1];
            java[3] = net[0];
            java[4] = net[5];
            java[5] = net[4];
            java[6] = net[7];
            java[7] = net[6];
            return new Guid(java);
        }

        /// <summary>
        /// Converts little-endian .NET guids to big-endian Java guids:
        /// </summary>
        public static byte[] ToBigEndianBytes(this Guid netGuid)
        {
            byte[] java = new byte[16];
            byte[] net = netGuid.ToByteArray();
            for (int i = 8; i < 16; i++)
            {
                java[i] = net[i];
            }
            java[0] = net[3];
            java[1] = net[2];
            java[2] = net[1];
            java[3] = net[0];
            java[4] = net[5];
            java[5] = net[4];
            java[6] = net[7];
            java[7] = net[6];
            return java;
        }
    }
}
