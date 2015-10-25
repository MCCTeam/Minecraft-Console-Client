using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers.Forge
{
    /// <summary>
    /// Copy of the forge enum for client states.
    /// https://github.com/MinecraftForge/MinecraftForge/blob/ebe9b6d4cbc4a5281c386994f1fbda04df5d2e1f/src/main/java/net/minecraftforge/fml/common/network/handshake/FMLHandshakeClientState.java
    /// </summary>
    enum FMLHandshakeClientState : byte
    {
        START,
        HELLO,
        WAITINGSERVERDATA,
        WAITINGSERVERCOMPLETE,
        PENDINGCOMPLETE,
        COMPLETE,
        DONE,
        ERROR
    }
}
