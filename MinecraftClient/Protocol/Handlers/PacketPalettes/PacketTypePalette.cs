using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.PacketPalettes
{
    /// <summary>
    /// Packet type palette
    /// </summary>
    /// <remarks>
    /// Steps for implementing palette for new Minecraft version:
    /// - Check out https://wiki.vg/Pre-release_protocol to see if there is any packet got added/removed
    /// - Add new packet type to PacketTypesIn.cs and PacketTypesOut.cs (if any)
    /// - Create a new PacketPaletteXXX.cs by copying the latest version of existing PacketPaletteXXX.cs
    /// - Apply change to the copied PacketPaletteXXX.cs by:
    ///    > Inserting new packet type to the correct position
    ///    > Removing packet type that got deleted
    /// - Check the new packet IDs to make sure they are implemented correctly by calling these dumping methods:
    ///    > PacketTypePalette.DumpInboundPacketId()
    ///    > PacketTypePalette.DumpOutboundPacketId()
    /// 
    /// The way how Mojang change the packet ID is simple: 
    ///  * Either adding/removing a packet from middle and cause packet ID below it get shifted
    ///  * Append a new packet at the end (but this is rare)
    /// </remarks>
    public abstract class PacketTypePalette
    {
        protected abstract List<PacketTypesIn> GetListIn();
        protected abstract List<PacketTypesOut> GetListOut();

        private Dictionary<PacketTypesIn, int> reverseMappingIn = new Dictionary<PacketTypesIn, int>();

        private Dictionary<PacketTypesOut, int> reverseMappingOut = new Dictionary<PacketTypesOut, int>();

        public PacketTypePalette()
        {
            for (int i = 0; i < GetListIn().Count; i++)
            {
                reverseMappingIn[GetListIn()[i]] = i;
            }
            for (int i = 0; i < GetListOut().Count; i++)
            {
                reverseMappingOut[GetListOut()[i]] = i;
            }
        }

        /// <summary>
        /// Get incomming packet type by packet ID
        /// </summary>
        /// <param name="packetId">packet ID</param>
        /// <returns>Packet type</returns>
        public PacketTypesIn GetIncommingTypeById(int packetId)
        {
            return GetListIn()[packetId];
        }

        /// <summary>
        /// Get incomming packet ID by packet type
        /// </summary>
        /// <param name="packetType">Packet type</param>
        /// <returns>packet ID</returns>
        public int GetIncommingIdByType(PacketTypesIn packetType)
        {
            return reverseMappingIn[packetType];
        }

        /// <summary>
        /// Get outgoing packet type by packet ID
        /// </summary>
        /// <param name="packetId">Packet ID</param>
        /// <returns>Packet type</returns>
        public PacketTypesOut GetOutgoingTypeById(int packetId)
        {
            return GetListOut()[packetId];
        }

        /// <summary>
        /// Get outgoing packet ID by packet type
        /// </summary>
        /// <param name="packetType">Packet type</param>
        /// <returns>Packet ID</returns>
        public int GetOutgoingIdByType(PacketTypesOut packetType)
        {
            return reverseMappingOut[packetType];
        }

        /// <summary>
        /// Dump the inbound packet ID mapping to the console
        /// </summary>
        public void DumpInboundPacketId()
        {
            for (int i = 0; i < GetListIn().Count; i++)
            {
                ConsoleIO.WriteLine("0x" + i.ToString("X2") + " " + GetListIn()[i]);
            }
        }
        /// <summary>
        /// Dump the inbound packet ID mapping to a file
        /// </summary>
        /// <param name="path">The file name</param>
        public void DumpInboundPacketId(string path)
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < GetListIn().Count; i++)
            {
                ids.Add("0x" + i.ToString("X2") + " " + GetListIn()[i]);
            }
            File.WriteAllText(path, string.Join("\r\n", ids));
        }

        /// <summary>
        /// Dump the outbound packet ID mapping to the console
        /// </summary>
        public void DumpOutboundPacketId()
        {
            for (int i = 0; i < GetListOut().Count; i++)
            {
                ConsoleIO.WriteLine("0x" + i.ToString("X2") + " " + GetListOut()[i]);
            }
        }
        /// <summary>
        /// Dump the outbound packet ID mapping to a file
        /// </summary>
        /// <param name="path">The file name</param>
        public void DumpOutboundPacketId(string path)
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < GetListOut().Count; i++)
            {
                ids.Add("0x" + i.ToString("X2") + " " + GetListOut()[i]);
            }
            File.WriteAllText(path, string.Join("\r\n", ids));
        }
    }
}
