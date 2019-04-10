using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation of the Clientbound Keep Alive Packet
    /// https://wiki.vg/Protocol#Keep_Alive_.28clientbound.29
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Join Game Packet
    /// https://wiki.vg/Protocol#Join_Game
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Chat Message Packet
    /// https://wiki.vg/Protocol#Chat_Message_.28clientbound.29
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Respawn Packet
    /// https://wiki.vg/Protocol#Respawn
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Player Position And Look Packet
    /// https://wiki.vg/Protocol#Player_Position_And_Look_.28clientbound.29
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Chunk Data Packet
    /// https://wiki.vg/Protocol#Chunk_Data
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Multi Block Change Packet
    /// https://wiki.vg/Protocol#Multi_Block_Change
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Block Change Packet
    /// https://wiki.vg/Protocol#Block_Change
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Map Chunk Bulk Packet
    /// https://wiki.vg/index.php?title=Protocol&oldid=7368#Map_Chunk_Bulk
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Unload Chunk Packet
    /// https://wiki.vg/Protocol#Unload_Chunk
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Player Info Packet
    /// https://wiki.vg/Protocol#Player_Info
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Tab-Complete Packet
    /// https://wiki.vg/Protocol#Tab-Complete_.28clientbound.29
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Plugin Message Packet
    /// https://wiki.vg/Protocol#Plugin_Message_.28clientbound.29
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Disconnect (play) Packet
    /// https://wiki.vg/Protocol#Disconnect_.28play.29
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Set Compression Packet
    /// https://wiki.vg/index.php?title=Protocol&oldid=7368#Set_Compression
    /// </summary>
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

    /// <summary>
    /// Implementation of the Clientbound Resource Pack Send Packet
    /// https://wiki.vg/Protocol#Resource_Pack_Send
    /// </summary>
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
