using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers
{
    class KeepAlive
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

    class JoinGame
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

    class ChatMessage
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

    class Respawn
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

    class PlayerPositionAndLook
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
}
