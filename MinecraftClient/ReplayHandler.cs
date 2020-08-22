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
        private BinaryWriter streamWriter;
        private DateTime now;

        public ReplayHandler(int protocolVersion)
        {
            this.dataTypes = new DataTypes(protocolVersion);
            if (File.Exists(path))
                File.Delete(path);
            streamWriter = new BinaryWriter(new FileStream(path, FileMode.Create));
            now = DateTime.Now;
        }

        public void AddPacket(int packetID, IEnumerable<byte> packetData)
        {
            // build raw packet
            List<byte> rawPacket = new List<byte>();
            rawPacket.AddRange(dataTypes.GetVarInt(packetID));
            rawPacket.AddRange(dataTypes.GetVarInt(packetData.Count()));
            rawPacket.AddRange(packetData.ToArray());
            // build format
            List<byte> line = new List<byte>();
            int nowTime = Convert.ToInt32((DateTime.Now - now).TotalMilliseconds);
            line.AddRange(BitConverter.GetBytes((Int32)nowTime));
            line.AddRange(BitConverter.GetBytes((Int32)rawPacket.Count));
            line.AddRange(rawPacket.ToArray());
            byte[] buf = line.ToArray();
            streamWriter.Write(buf);
            streamWriter.Flush();
        }
    }
}
