using System;
using MinecraftClient.Network.IO;

namespace MinecraftClient.Network.Packets
{
    public interface IPacket
    {
        byte Id { get; }
        void ReadPacket(MinecraftStream stream);
        void WritePacket(MinecraftStream stream);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class PacketAttribute : Attribute
    {
        public Packet ID;
    }
}
