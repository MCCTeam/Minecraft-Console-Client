using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient
{
    public class ReplayHandler
    {
        private string path = @"recording.tmcpr";

        private DataTypes dataTypes;
        private int protocolVersion;
        private BinaryWriter streamWriter;
        private DateTime now;

        public ReplayHandler(int protocolVersion)
        {
            this.dataTypes = new DataTypes(protocolVersion);
            this.protocolVersion = protocolVersion;
            if (File.Exists(path))
                File.Delete(path);
            streamWriter = new BinaryWriter(new FileStream(path, FileMode.Create));
            now = DateTime.Now;
        }

        public void AddPacket(int packetID, IEnumerable<byte> packetData, bool isLogin, bool isInbound)
        {
            if (!PacketFilter(packetID, isLogin, isInbound))
                return;
            // build raw packet
            // format: packetID + packetData
            List<byte> rawPacket = new List<byte>();
            rawPacket.AddRange(dataTypes.GetVarInt(packetID).ToArray());
            rawPacket.AddRange(packetData.ToArray());
            // build format
            // format: timestamp + packetLength + RawPacket
            List<byte> line = new List<byte>();
            int nowTime = Convert.ToInt32((DateTime.Now - now).TotalMilliseconds);
            line.AddRange(BitConverter.GetBytes((Int32)nowTime).Reverse().ToArray());
            line.AddRange(BitConverter.GetBytes((Int32)rawPacket.Count).Reverse().ToArray());
            line.AddRange(rawPacket.ToArray());
            byte[] buf = line.ToArray();
            streamWriter.Write(buf);
            streamWriter.Flush();
        }

        private bool PacketFilter(int packetID, bool isLogin, bool isInbound)
        {
            if (isLogin) // compression is ignored
                return false;
            else return true;
        }
    }
}
