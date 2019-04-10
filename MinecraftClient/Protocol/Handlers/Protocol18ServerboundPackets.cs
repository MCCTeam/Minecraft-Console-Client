using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation of the Serverbound Keep Alive Packet
    /// https://wiki.vg/Protocol#Keep_Alive_.28serverbound.29
    /// </summary>
    class ServerKeepAlive
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x00;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x0B;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x0C;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x0B;
            else if (protocol < PacketUtils.MC17w46aVersion)
                return 0x0A;
            else if (protocol < PacketUtils.MC113pre4Version)
                return 0x0B;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x0C;
            else
                return 0x0E;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Resource Pack Status Packet
    /// https://wiki.vg/Protocol#Resource_Pack_Status
    /// </summary>
    class ServerResourcePackStatus
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x19;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x16;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x18;
            else if (protocol < PacketUtils.MC17w46aVersion)
                return 0x17;
            else if (protocol < PacketUtils.MC113pre4Version)
                return 0x18;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x1B;
            else
                return 0x1D;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Chat Message Packet
    /// https://wiki.vg/Protocol#Chat_Message_.28serverbound.29
    /// </summary>
    class ServerChatMessage
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x01;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x02;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x03;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x02;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x01;
            else
                return 0x02;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Client Status Packet
    /// https://wiki.vg/Protocol#Client_Status
    /// </summary>
    class ServerClientStatus
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x16;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x03;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x04;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x03;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x02;
            else
                return 0x03;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Client Settings Packet
    /// https://wiki.vg/Protocol#Client_Settings
    /// </summary>
    class ServerClientSettings
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x15;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x04;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x05;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x04;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x03;
            else
                return 0x04;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Plugin Message Packet
    /// https://wiki.vg/Protocol#Plugin_Message_.28serverbound.29
    /// </summary>
    class ServerPluginMessage
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x17;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x09;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x0A;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x09;
            else if (protocol < PacketUtils.MC17w46aVersion)
                return 0x08;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x09;
            else
                return 0x0A;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Tab-Complete Packet
    /// https://wiki.vg/Protocol#Tab-Complete_.28serverbound.29
    /// </summary>
    class ServerTabComplete
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x14;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x01;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x02;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x01;
            else if (protocol < PacketUtils.MC17w46aVersion)
                // throw new InvalidOperationException("TabComplete was accidentely removed in protocol " + protocol + ". Please use a more recent version.");
                return -1;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x04;
            else
                return 0x05;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Player Position Packet
    /// https://wiki.vg/Protocol#Player_Position
    /// </summary>
    class ServerPlayerPosition
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x04;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x0C;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x0D;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x0E;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x0D;
            else if (protocol < PacketUtils.MC17w46aVersion)
                return 0x0C;
            else if (protocol < PacketUtils.MC113pre4Version)
                return 0x0D;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x0E;
            else
                return 0x10;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Player Position And Look Packet
    /// https://wiki.vg/Protocol#Player_Position_And_Look_.28serverbound.29
    /// </summary>
    class ServerPlayerPositionAndLook
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x06;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x0D;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x0E;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x0F;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x0E;
            else if (protocol < PacketUtils.MC17w46aVersion)
                return 0x0D;
            else if (protocol < PacketUtils.MC113pre4Version)
                return 0x0E;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x0F;
            else
                return 0x11;
        }
    }

    /// <summary>
    /// Implementation of the Serverbound Teleport Confirm Packet
    /// https://wiki.vg/Protocol#Teleport_Confirm
    /// </summary>
    class ServerTeleportConfirm
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                // throw new InvalidOperationException("Teleport confirm is not supported in protocol " + protocol);
                return -1;
            else
                return 0x00;
        }
    }
}
