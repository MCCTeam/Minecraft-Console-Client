using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.PacketTypes.Palettes;

namespace MinecraftClient.Protocol.Handlers.PacketTypes
{
    public class PacketTypeHandler
    {
        private int protocol;
        public PacketTypeHandler(int protocol)
        {
            this.protocol = protocol;
        }

        public PacketTypePalette GetTypeHandler()
        {
            if (protocol <= Protocol18Handler.MC18Version)
                return new PacketPalette18();
            else if (protocol <= Protocol18Handler.MC1112Version)
                return new PacketPalette111();
            else if (protocol <= Protocol18Handler.MC112Version)
                return new PacketPalette112();
            else if (protocol <= Protocol18Handler.MC1122Version)
                return new PacketPalette1122();
            else if (protocol <= Protocol18Handler.MC114Version)
                return new PacketPalette1132();
            else if (protocol <= Protocol18Handler.MC115Version)
                return new PacketPalette1144();
            else if (protocol <= Protocol18Handler.MC1152Version)
                return new PacketPalette1152();
            else if (protocol <= Protocol18Handler.MC1161Version)
                return new PacketPalette1161();
            else return new PacketPalette1162();
        }
    }
}
