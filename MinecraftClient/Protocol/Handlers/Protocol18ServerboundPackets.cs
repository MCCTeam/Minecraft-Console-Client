using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers
{
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
}
