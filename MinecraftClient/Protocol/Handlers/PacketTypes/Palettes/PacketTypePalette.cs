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

        public PacketTypesIn GetIncommingTypeById(int packetId)
        {
            return GetListIn()[packetId];
        }

        public int GetIncommingIdByType(PacketTypesIn packetType)
        {
            return reverseMappingIn[packetType];
        }

        public PacketTypesOut GetOutgoingTypeById(int packetId)
        {
            return GetListOut()[packetId];
        }

        public int GetOutgoingIdByType(PacketTypesOut packetType)
        {
            return reverseMappingOut[packetType];
        }
    }
}
