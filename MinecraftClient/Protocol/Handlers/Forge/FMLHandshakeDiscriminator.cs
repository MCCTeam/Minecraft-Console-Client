using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Forge
{
    /// <summary>
    /// Different "discriminator byte" values for the forge handshake.
    /// https://github.com/MinecraftForge/MinecraftForge/blob/ebe9b6d4cbc4a5281c386994f1fbda04df5d2e1f/src/main/java/net/minecraftforge/fml/common/network/handshake/FMLHandshakeCodec.java
    /// </summary>
    enum FMLHandshakeDiscriminator : byte
    {
        ServerHello = 0,
        ClientHello = 1,
        ModList = 2,
        RegistryData = 3,
        HandshakeAck = 255, //-1
        HandshakeReset = 254, //-2
    }
}
