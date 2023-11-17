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
            PacketTypePalette p = protocol switch
            {
                > Protocol18Handler.MC_1_20_2_Version => throw new NotImplementedException(Translations
                    .exception_palette_packet),
                <= Protocol18Handler.MC_1_8_Version => new PacketPalette17(),
                <= Protocol18Handler.MC_1_11_2_Version => new PacketPalette110(),
                <= Protocol18Handler.MC_1_12_Version => new PacketPalette112(),
                <= Protocol18Handler.MC_1_12_2_Version => new PacketPalette1122(),
                < Protocol18Handler.MC_1_14_Version => new PacketPalette113(),
                < Protocol18Handler.MC_1_15_Version => new PacketPalette114(),
                <= Protocol18Handler.MC_1_15_2_Version => new PacketPalette115(),
                <= Protocol18Handler.MC_1_16_1_Version => new PacketPalette116(),
                <= Protocol18Handler.MC_1_16_5_Version => new PacketPalette1162(),
                <= Protocol18Handler.MC_1_17_1_Version => new PacketPalette117(),
                <= Protocol18Handler.MC_1_18_2_Version => new PacketPalette118(),
                <= Protocol18Handler.MC_1_19_Version => new PacketPalette119(),
                <= Protocol18Handler.MC_1_19_2_Version => new PacketPalette1192(),
                <= Protocol18Handler.MC_1_19_3_Version => new PacketPalette1193(),
                < Protocol18Handler.MC_1_20_2_Version => new PacketPalette1194(),
                _ => new PacketPalette1202()
            };

            p.SetForgeEnabled(forgeEnabled);
            return p;
        }
    }
}
