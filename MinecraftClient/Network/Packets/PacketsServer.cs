using System;
using System.Linq;
using MinecraftClient.Network.IO;

// Parsing response from server
namespace MinecraftClient.Network.Packets
{
    [Packet(ID = Packet.LoginSuccess)]
    public class LoginSuccess : IPacket
    {
        //public string UUID { get; set; }
        //public string Username { get; set; }

        public byte Id { get { return (byte)GetType().GetCustomAttributes(false).OfType<PacketAttribute>().Single().ID; } }

        public void ReadPacket(MinecraftStream stream)
        {
            //UUID = stream.ReadString();
            //Username = stream.ReadString();
        }

        public void WritePacket(MinecraftStream stream)
        {
            throw new NotImplementedException();
        }
    }

    [Packet(ID = Packet.EncryptionRequest)]
    public class EncryptionRequest : IPacket
    {
        public string ServerID { get; set; }
        public short PublicKeyLength { get; set; }
        public byte[] PublicKey { get; set; }
        public short VerifyTokenLength { get; set; }
        public byte[] VerifyToken { get; set; }

        public byte Id
        {
            get { return (byte)GetType().GetCustomAttributes(false).OfType<PacketAttribute>().Single().ID; }
        }

        public void ReadPacket(MinecraftStream stream)
        {
            ServerID = stream.ReadString();
            PublicKeyLength = stream.ReadInt16();
            PublicKey = stream.ReadUInt8Array(PublicKeyLength);
            VerifyTokenLength = stream.ReadInt16();
            VerifyToken = stream.ReadUInt8Array(VerifyTokenLength);
        }

        public void WritePacket(MinecraftStream stream)
        {
            throw new NotImplementedException();
        }
    }

    [Packet(ID = Packet.JoinGame)]
    public class JoinGame : IPacket
    {
        public int EntityID { get; set; }
        public byte Gamemode { get; set; }
        public byte Dimension { get; set; }
        public byte Difficulty { get; set; }
        public byte MaxPlayers { get; set; }
        public string LevelType { get; set; }

        public byte Id
        {
            get { return (byte)GetType().GetCustomAttributes(false).OfType<PacketAttribute>().Single().ID; }
        }

        public void ReadPacket(MinecraftStream stream)
        {
            EntityID = stream.ReadInt32();
            Gamemode = stream.ReadUInt8();
            Dimension = stream.ReadUInt8();
            Difficulty = stream.ReadUInt8();
            MaxPlayers = stream.ReadUInt8();
            LevelType = stream.ReadString();
        }

        public void WritePacket(MinecraftStream stream)
        {
            throw new NotImplementedException();
        }
    }

    [Packet(ID = Packet.Particle)]
    public class Particle : IPacket
    {
        public string ParticleName { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float OffsetZ { get; set; }
        public float ParticleData { get; set; }
        public int NumberOfParticles { get; set; }

        public byte Id
        {
            get { return (byte)GetType().GetCustomAttributes(false).OfType<PacketAttribute>().Single().ID; }
        }

        public void ReadPacket(MinecraftStream stream)
        {
            ParticleName = stream.ReadString();
            X = stream.ReadSingle();
            Y = stream.ReadSingle();
            Z = stream.ReadSingle();
            OffsetX = stream.ReadSingle();
            OffsetY = stream.ReadSingle();
            OffsetZ = stream.ReadSingle();
            ParticleData = stream.ReadSingle();
            NumberOfParticles = stream.ReadInt32();
        }

        public void WritePacket(MinecraftStream stream)
        {
            throw new NotImplementedException();
        }
    }

    [Packet(ID = Packet.DisplayScoreboard)]
    public class DisplayScoreboard : IPacket
    {
        public byte Position { get; set; }
        public string ScoreName { get; set; }

        public byte Id
        {
            get { return (byte)GetType().GetCustomAttributes(false).OfType<PacketAttribute>().Single().ID; }
        }

        public void ReadPacket(MinecraftStream stream)
        {
            Position = stream.ReadUInt8();
            ScoreName = stream.ReadString();
        }

        public void WritePacket(MinecraftStream stream)
        {
            throw new NotImplementedException();
        }
    }
    

}
