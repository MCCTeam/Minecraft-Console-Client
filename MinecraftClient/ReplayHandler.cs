using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MinecraftClient.Protocol.Handlers;
using System.Runtime.InteropServices;
using System.IO.Compression;

namespace MinecraftClient
{
    public class ReplayHandler
    {
        private string path = @"recording.tmcpr";

        private DataTypes dataTypes;
        private int protocolVersion;
        private BinaryWriter streamWriter;
        private DateTime recordStartTime;
        private DateTime lastPacketTime;

        public MetaDataHandler MetaData;

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
            if (File.Exists(path))
                File.Delete(path);
            streamWriter = new BinaryWriter(new FileStream(path, FileMode.Create));
            recordStartTime = DateTime.Now;

            MetaData = new MetaDataHandler();
            MetaData.date = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            MetaData.protocol = protocolVersion;
        }

        ~ReplayHandler()
        {
            OnShutDown();
        }

        public void AddPacket(int packetID, IEnumerable<byte> packetData, bool isLogin, bool isInbound)
        {
            if (!PacketShouldSave(packetID, isLogin, isInbound))
                return;
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
            byte[] buf = line.ToArray();
            streamWriter.Write(buf);
            //streamWriter.Flush();
        }

        public void OnPlayerSpawn(Guid uuid, string name)
        {
            // Metadata has a field for storing uuid for all players entered client render range
        }

        public void CloseStream()
        {
            try
            {
                streamWriter.Flush();
                streamWriter.Close();
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
            CloseStream();

            ZipStorer zip = ZipStorer.Create("whhhh.mcpr");
            zip.AddFile(ZipStorer.Compression.Store, "metadata.json", "metadata.json");
            zip.AddFile(ZipStorer.Compression.Store, "recording.tmcpr", "recording.tmcpr");
            zip.Close();
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
                    return true;
                else return false;
            }
        }
    }

    public class MetaDataHandler
    {
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
        public string[] players; // Array of UUIDs of all players which can be seen in the replay

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

        public void SaveToFile()
        {
            File.WriteAllText("metadata.json", ToJson());
        }

        private string GetPlayersArray()
        {
            if (players == null)
                return "[]";
            string[] tmp = new string[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                tmp[i] = "\"" + players[i] + "\"";
            }
            return "[" + string.Join(",", tmp) + "]";
        }
    }
}
