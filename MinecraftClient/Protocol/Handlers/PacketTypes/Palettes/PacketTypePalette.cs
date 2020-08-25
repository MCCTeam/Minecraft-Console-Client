using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.PacketTypes.Palettes
{
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
        /// Dump the inbound packet ID mapping to console
        /// </summary>
        public void DumpInboundPacketId()
        {
            for (int i = 0; i < GetListIn().Count; i++)
            {
                ConsoleIO.WriteLine("0x" + i.ToString("X2") + " " + GetListIn()[i]);
            }
        }

        /// <summary>
        /// Dump the outbound packet ID mapping to console
        /// </summary>
        public void DumpOutboundPacketId()
        {
            for (int i = 0; i < GetListOut().Count; i++)
            {
                ConsoleIO.WriteLine("0x" + i.ToString("X2") + " " + GetListOut()[i]);
            }
        }
    }
}
