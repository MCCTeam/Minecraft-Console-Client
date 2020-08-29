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
using MinecraftClient.Protocol.Handlers.PacketPalettes;

namespace MinecraftClient
{
    public class ReplayHandler
    {
        public string ReplayFileName = @"whhhh.mcpr";
        public string ReplayFileDirectory = @"replay_recordings";
        public MetaDataHandler MetaData;

        private readonly string recordingTmpFileName = @"recording.tmcpr";
        private readonly string temporaryCache = @"recording_cache";
        private DataTypes dataTypes;
        private PacketTypePalette packetType;
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
        private bool notSpawned = true;

        public ReplayHandler(int protocolVersion)
        {
            Initialize(protocolVersion);
        }
        public ReplayHandler(int protocolVersion, string serverName, string recordingDirectory = @"replay_recordings")
        {
            Initialize(protocolVersion);
            this.MetaData.serverName = serverName;
            ReplayFileDirectory = recordingDirectory;
        }
        private void Initialize(int protocolVersion)
        {
            this.dataTypes = new DataTypes(protocolVersion);
            this.packetType = new PacketTypeHandler().GetTypeHandler(protocolVersion);
            this.protocolVersion = protocolVersion;

            if (!Directory.Exists(ReplayFileDirectory))
                Directory.CreateDirectory(ReplayFileDirectory);
            if (!Directory.Exists(temporaryCache))
                Directory.CreateDirectory(temporaryCache);

            recordStream = new BinaryWriter(new FileStream(temporaryCache + "\\" + recordingTmpFileName, FileMode.Create));
            recordStartTime = DateTime.Now;

            MetaData = new MetaDataHandler();
            MetaData.date = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            MetaData.protocol = protocolVersion;
            MetaData.mcversion = ProtocolNumberToVersion(protocolVersion);

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
            var now = DateTime.Now;
            ReplayFileName = string.Format("{0}_{1}_{2}_{3}_{4}_{5}.mcpr", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second); // yyyy_mm_dd_hh_mm_ss
            using (Stream recordingFile = new FileStream(temporaryCache + "\\" + recordingTmpFileName, FileMode.Open)) // what if I want to save replay while MCC running? thread-safe?
            using (Stream metaDatFile = new FileStream(temporaryCache + "\\" + MetaData.MetaDataFileName, FileMode.Open))
            using (ZipOutputStream zs = new ZipOutputStream(ReplayFileDirectory + "\\" + ReplayFileName))
            {
                zs.PutNextEntry(recordingTmpFileName);
                recordingFile.CopyTo(zs);
                zs.PutNextEntry(MetaData.MetaDataFileName);
                metaDatFile.CopyTo(zs);
                zs.Close();
            }
            File.Delete(temporaryCache + "\\" + recordingTmpFileName);
            File.Delete(temporaryCache + "\\" + MetaData.MetaDataFileName);
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
            PacketTypesIn pType = packetType.GetIncommingTypeById(packetID);
            // Login success. Get player UUID
            if (isLogin && packetID == 0x02)
            {
                Guid uuid;
                if (protocolVersion < Protocol18Handler.MC116Version)
                {
                    if (Guid.TryParse(dataTypes.ReadNextString(p), out uuid))
                    {
                        SetClientPlayerUUID(uuid);
                        WriteDebugLog("User UUID: " + uuid.ToString());
                    }
                }
                else
                {
                    var uuid2 = dataTypes.ReadNextUUID(p);
                    SetClientPlayerUUID(uuid2);
                    WriteDebugLog("User UUID: " + uuid2.ToString());
                }
            }
            // Get client player location for calculating movement delta later
            if (pType == PacketTypesIn.PlayerPositionAndLook)
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
                //AddPacket(GetSpawnPlayerPacketID(protocolVersion),
                //    GetSpawnPlayerPacket(playerEntityID, playerUUID, playerLastPosition, playerLastPitch, playerLastYaw));
                //MetaData.AddPlayerUUID(playerUUID);
            }
            //if (pType == PacketTypesIn.PlayerInfo)
            //{
            //    if (notSpawned)
            //    {
            //        bool shouldAdd = false;
            //        // parse player info
            //        int action = dataTypes.ReadNextVarInt(p);
            //        int count = dataTypes.ReadNextVarInt(p);
            //        if (action == 0 && count > 0)
            //        {
            //            for (int i = 0; i < count; i++)
            //            {
            //                Guid uuid = dataTypes.ReadNextUUID(p);
            //                dataTypes.ReadNextString(p); // player name
            //                int propCount = dataTypes.ReadNextVarInt(p);
            //                for (int j = 0; j < propCount; j++)
            //                {
            //                    dataTypes.ReadNextString(p);
            //                    dataTypes.ReadNextString(p);
            //                    if (dataTypes.ReadNextBool(p))
            //                        dataTypes.ReadNextString(p);
            //                }
            //                dataTypes.ReadNextVarInt(p);
            //                dataTypes.ReadNextVarInt(p);
            //                if (dataTypes.ReadNextBool(p))
            //                    dataTypes.ReadNextString(p);
            //                if (uuid == playerUUID)
            //                    shouldAdd = true;
            //            }
            //        }

            //        if (shouldAdd)
            //        {
            //            AddPacket(packetType.GetIncommingIdByType(PacketTypesIn.SpawnPlayer),
            //                GetSpawnPlayerPacket(playerEntityID, playerUUID, playerLastPosition, playerLastPitch, playerLastYaw));
            //            notSpawned = false;
            //        }
            //    }
            //}
            //if (pType == PacketTypesIn.Respawn)
            //{
            //    if (!notSpawned)
            //    {
            //        AddPacket(packetType.GetIncommingIdByType(PacketTypesIn.SpawnPlayer),
            //                GetSpawnPlayerPacket(playerEntityID, playerUUID, playerLastPosition, playerLastPitch, playerLastYaw));
            //    }
            //}
        }

        /// <summary>
        /// Handle outbound packet (i.e. client player movement)
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        /// <param name="isLogin"></param>
        private void HandleOutBoundPacket(int packetID, IEnumerable<byte> packetData, bool isLogin)
        {
            var packetType = this.packetType.GetOutgoingTypeById(packetID);
            if (packetType == PacketTypesOut.PlayerPosition
                || packetType == PacketTypesOut.PlayerPositionAndRotation)
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

        private static string ProtocolNumberToVersion(int protocol)
        {
            switch (protocol) 
            {
                case 4: return "1.7.2";
                case 5: return "1.7.6";
                case 47: return "1.8";
                case 107: return "1.9";
                case 108: return "1.9.1";
                case 109: return "1.9.2";
                case 110: return "1.9.3";
                case 210: return "1.10";
                case 315: return "1.11";
                case 316: return "1.11.1";
                case 335: return "1.12";
                case 338: return "1.12.1";
                case 340: return "1.12.2";
                case 393: return "1.13";
                case 401: return "1.13.1";
                case 404: return "1.13.2";
                case 477: return "1.14";
                case 480: return "1.14.1";
                case 485: return "1.14.2";
                case 490: return "1.14.3";
                case 498: return "1.14.4";
                case 573: return "1.15";
                case 575: return "1.15.1";
                case 578: return "1.15.2";
                case 735: return "1.16";
                case 736: return "1.16.1";
                case 751: return "1.16.2";
                default: return "0.0";
            }
        }

        #endregion
    }

    public class MetaDataHandler
    {
        public readonly string MetaDataFileName = @"metaData.json";
        public readonly string temporaryCache = @"recording_cache";

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
            File.WriteAllText(temporaryCache + "\\" + MetaDataFileName, ToJson());
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
