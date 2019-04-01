using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers
{
    class ClientKeepAlive
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x00;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x1F;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x20;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x1F;
            else if (protocol < PacketUtils.MC18w01aVersion)
                return 0x20;
            else
                return 0x21;
        }
    }

    class ClientJoinGame
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x01;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x23;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x24;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x23;
            else if (protocol < PacketUtils.MC18w01aVersion)
                return 0x24;
            else
                return 0x25;
        }
    }

    class ClientChatMessage
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x02;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x0F;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x10;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x0F;
            else
                return 0x0E;
        }
    }

    class ClientRespawn
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x07;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x33;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x35;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x34;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x35;
            else if (protocol < PacketUtils.MC18w01aVersion)
                return 0x36;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x37;
            else
                return 0x38;
        }
    }

    class ClientPlayerPositionAndLook
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x08;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x2E;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x2F;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x2E;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x2F;
            else if (protocol < PacketUtils.MC18w01aVersion)
                return 0x30;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x31;
            else
                return 0x32;
        }
    }

    class ClientChunkData
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x21;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x20;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x21;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x20;
            else if (protocol < PacketUtils.MC17w46aVersion)
                return 0x21;
            else
                return 0x22;
        }
    }

    class ClientMultiBlockChange
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x22;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x10;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x11;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x10;
            else
                return 0x0F;
        }
    }

    class ClientBlockChange
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x23;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x0B;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x0C;
            else
                return 0x0B;
        }
    }

    class ClientMapChunkBulk
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x26;
            else
                //MapChunkBulk removed in 1.9
                return -1;
        }
    }

    class ClientUnloadChunk
    {
        public static int getPacketID(int protocol)
        {
            //UnloadChunk does not exists prior to 1.9
            if (protocol < PacketUtils.MC17w13aVersion)
                return 0x1D;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x1E;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x1D;
            else if (protocol < PacketUtils.MC18w01aVersion)
                return 0x1E;
            else
                return 0x1F;
        }
    }

    class ClientPlayerListUpdate
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x38;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x2D;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x2E;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x2D;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x2E;
            else if (protocol < PacketUtils.MC18w01aVersion)
                return 0x2F;
            else
                return 0x30;
        }
    }

    class ClientTabCompleteResult
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x3A;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x0E;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x0F;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x0E;
            else if (protocol < PacketUtils.MC17w46aVersion)
                //TabCompleteResult accidentely removed
                return -1;
            else
                return 0x10;
        }
    }

    class ClientPluginMessage
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x3F;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x18;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x19;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x18;
            else
                return 0x19;
        }
    }

    class ClientKickPacket
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x40;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x1A;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x1B;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x1A;
            else
                return 0x1B;
        }
    }

    class ClientNetworkCompressionTreshold
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x46;
            else
                //NetworkCompressionTreshold removed in 1.9
                return -1;
        }
    }

    class ClientResourcePackSend
    {
        public static int getPacketID(int protocol)
        {
            if (protocol < PacketUtils.MC19Version)
                return 0x48;
            else if (protocol < PacketUtils.MC17w13aVersion)
                return 0x32;
            else if (protocol < PacketUtils.MC112pre5Version)
                return 0x34;
            else if (protocol < PacketUtils.MC17w31aVersion)
                return 0x33;
            else if (protocol < PacketUtils.MC17w45aVersion)
                return 0x34;
            else if (protocol < PacketUtils.MC18w01aVersion)
                return 0x35;
            else if (protocol < PacketUtils.MC113pre7Version)
                return 0x36;
            else
                return 0x37;
        }
    }
}
