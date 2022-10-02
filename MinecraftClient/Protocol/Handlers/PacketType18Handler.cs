using System;
using MinecraftClient.Protocol.Handlers.PacketPalettes;

namespace MinecraftClient.Protocol.Handlers
{
    public class PacketTypeHandler
    {
        private readonly int protocol;
        private readonly bool forgeEnabled = false;

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
            return GetTypeHandler(protocol);
        }
        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler(int protocol)
        {
            PacketTypePalette p;
            if (protocol > Protocol18Handler.MC_1_19_2_Version)
                throw new NotImplementedException(Translations.Get("exception.palette.packet"));

            if (protocol <= Protocol18Handler.MC_1_8_Version)
                p = new PacketPalette17();
            else if (protocol <= Protocol18Handler.MC_1_11_2_Version)
                p = new PacketPalette110();
            else if (protocol <= Protocol18Handler.MC_1_12_Version)
                p = new PacketPalette112();
            else if (protocol <= Protocol18Handler.MC_1_12_2_Version)
                p = new PacketPalette1122();
            else if (protocol < Protocol18Handler.MC_1_14_Version)
                p = new PacketPalette113();
            else if (protocol <= Protocol18Handler.MC_1_15_Version)
                p = new PacketPalette114();
            else if (protocol <= Protocol18Handler.MC_1_15_2_Version)
                p = new PacketPalette115();
            else if (protocol <= Protocol18Handler.MC_1_16_1_Version)
                p = new PacketPalette116();
            else if (protocol <= Protocol18Handler.MC_1_16_5_Version)
                p = new PacketPalette1162();
            else if (protocol <= Protocol18Handler.MC_1_17_1_Version)
                p = new PacketPalette117();
            else if (protocol <= Protocol18Handler.MC_1_18_2_Version)
                p = new PacketPalette118();
            else if (protocol <= Protocol18Handler.MC_1_19_Version)
                p = new PacketPalette119();
            else
                p = new PacketPalette1192();

            p.SetForgeEnabled(forgeEnabled);
            return p;
        }
    }
}
