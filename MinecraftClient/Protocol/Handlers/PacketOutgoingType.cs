using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Abstract outgoing packet numbering
    /// </summary>
    enum PacketOutgoingType
    {
        KeepAlive,
        ResourcePackStatus,
        ChatMessage,
        ClientStatus,
        ClientSettings,
        PluginMessage,
        TabComplete,
        PlayerPosition,
        PlayerPositionAndLook,
        TeleportConfirm
    }
}
