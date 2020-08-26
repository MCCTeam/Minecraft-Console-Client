using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.PacketPalettes;

namespace MinecraftClient.Protocol.Handlers
{
    public class PacketTypeHandler
    {
        private int protocol;

        /// <summary>
        /// Initialize the handler
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        public PacketTypeHandler(int protocol)
        {
            this.protocol = protocol;
        }
        /// <summary>
        /// Initialize the handler
        /// </summary>
        public PacketTypeHandler() { }

        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler()
        {
            return GetTypeHandler(this.protocol);
        }
        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler(int protocol)
        {
            if (protocol > Protocol18Handler.MC1162Version)
                throw new NotImplementedException("Please update packet type palette for this Minecraft version. See PacketTypePalette.cs");
            if (protocol <= Protocol18Handler.MC18Version)
                return new PacketPalette17();
            else if (protocol <= Protocol18Handler.MC1112Version)
                return new PacketPalette110();
            else if (protocol <= Protocol18Handler.MC112Version)
                return new PacketPalette112();
            else if (protocol <= Protocol18Handler.MC1122Version)
                return new PacketPalette1122();
            else if (protocol <= Protocol18Handler.MC114Version)
                return new PacketPalette113();
            else if (protocol <= Protocol18Handler.MC115Version)
                return new PacketPalette114();
            else if (protocol <= Protocol18Handler.MC1152Version)
                return new PacketPalette115();
            else if (protocol <= Protocol18Handler.MC1161Version)
                return new PacketPalette116();
            else return new PacketPalette1162();
        }
    }
}
