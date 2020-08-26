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
    /// 1. Check out https://wiki.vg/Pre-release_protocol to see if there is any packet got added/removed
    /// 2. Add new packet type to PacketTypesIn.cs and PacketTypesOut.cs (if any)
    /// 3. Create a new PacketPaletteXXX.cs by copying the latest version of existing PacketPaletteXXX.cs (could reduce massive works on writing a brand new one)
    /// 4. Apply change to the copied PacketPaletteXXX.cs by either:
    ///     - Inserting new packet type to the correct position
    ///     - Removing packet type that got deleted
    ///    OR
    ///     - Changing the packet IDs manually
    /// 5. Use PacketPaletteHelper to generate a code snippet and copy the generated code snippet back to PacketPaletteXXX.cs
    ///     - Use UpdatePacketPositionToAscending() if you changed the packet IDs manually
    ///     - Use UpdatePacketIdByItemPosition() if you inserted some packet type into the dictionary
    ///    Simply add the method call in Program.cs and run the program once. The code snippet will be generated
    /// 
    /// 
    /// The way how Mojang change the packet ID is simple: 
    ///  * Either adding/removing a packet from middle and cause packet ID below it get shifted
    ///  * Append a new packet at the end (but this is rare)
    /// </remarks>
    public abstract class PacketTypePalette
    {
        protected abstract Dictionary<int, PacketTypesIn> GetListIn();
        protected abstract Dictionary<int, PacketTypesOut> GetListOut();

        private Dictionary<PacketTypesIn, int> reverseMappingIn = new Dictionary<PacketTypesIn, int>();

        private Dictionary<PacketTypesOut, int> reverseMappingOut = new Dictionary<PacketTypesOut, int>();

        public PacketTypePalette()
        {
            foreach (var p in GetListIn())
            {
                reverseMappingIn.Add(p.Value, p.Key);
            }
            foreach (var p in GetListOut())
            {
                reverseMappingOut.Add(p.Value, p.Key);
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
            foreach (var p in GetListIn())
            {
                ConsoleIO.WriteLine("0x" + p.Key.ToString("X2") + " " + p.Value);
            }
        }

        /// <summary>
        /// Dump the inbound packet ID mapping to a file
        /// </summary>
        /// <param name="path">The file name</param>
        public void DumpInboundPacketId(string path)
        {
            List<string> ids = new List<string>();
            foreach (var p in GetListOut())
            {
                ids.Add("0x" + p.Key.ToString("X2") + " " + p.Value);
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

        /// <summary>
        /// Public method for getting the type mapping
        /// </summary>
        /// <returns>PacketTypesIn with packet ID as index</returns>
        public Dictionary<int, PacketTypesIn> GetMappingIn()
        {
            return GetListIn();
        }

        /// <summary>
        /// Public method for getting the type mapping
        /// </summary>
        /// <returns>PacketTypesOut with packet ID as index</returns>
        public Dictionary<int ,PacketTypesOut> GetMappingOut()
        {
            return GetListOut();
        }
    }
}
