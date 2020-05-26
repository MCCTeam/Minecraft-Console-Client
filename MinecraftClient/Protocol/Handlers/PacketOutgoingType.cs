using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Abstract outgoing packet numbering
    /// </summary>
    /// /// <remarks>
    /// Please add new entries at the bottom of the list
    /// You'll also need to add them to Protocol18PacketTypes for all MC versions since MC 1.7
    /// </remarks>
    enum PacketOutgoingType
    {
        KeepAlive,
        ResourcePackStatus,
        ChatMessage,
        ClientStatus,
        ClientSettings,
        PluginMessage,
        TabComplete,
        EntityAction,
        PlayerPosition,
        PlayerPositionAndLook,
        TeleportConfirm,
        HeldItemChange,
        InteractEntity,
        UseItem,
        ClickWindow,
        CloseWindow,
        PlayerBlockPlacement,
        CreativeInventoryAction,
        Animation,
    }
}
