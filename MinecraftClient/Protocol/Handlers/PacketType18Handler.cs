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
        private bool forgeEnabled = false;

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
        /// <param name="protocol">Protocol version to use</param>
        /// <param name="forgeEnabled">Is forge enabled or not</param>
        public PacketTypeHandler(int protocol, bool forgeEnabled)
        {
            this.protocol = protocol;
            this.forgeEnabled = forgeEnabled;
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
            PacketTypePalette p;
            if (protocol > Protocol18Handler.MC1182Version)
                throw new NotImplementedException(Translations.Get("exception.palette.packet"));
            if (protocol <= Protocol18Handler.MC18Version)
                p = new PacketPalette17();
            else if (protocol <= Protocol18Handler.MC1112Version)
                p = new PacketPalette110();
            else if (protocol <= Protocol18Handler.MC112Version)
                p = new PacketPalette112();
            else if (protocol <= Protocol18Handler.MC1122Version)
                p = new PacketPalette1122();
            else if (protocol <= Protocol18Handler.MC114Version)
                p = new PacketPalette113();
            else if (protocol <= Protocol18Handler.MC115Version)
                p = new PacketPalette114();
            else if (protocol <= Protocol18Handler.MC1152Version)
                p = new PacketPalette115();
            else if (protocol <= Protocol18Handler.MC1161Version)
                p = new PacketPalette116();
            else if (protocol <= Protocol18Handler.MC1165Version)
                p = new PacketPalette1162();
            else if (protocol <= Protocol18Handler.MC1171Version)
                p = new PacketPalette117();
            else
                p = new PacketPalette118();

            p.SetForgeEnabled(this.forgeEnabled);
            return p;
        }
    }
}
