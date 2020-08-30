using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    public class ReplayCaptor : ChatBot
    {
        ReplayHandler replay;

        public override void Initialize()
        {
            SetNetworkPacketEventEnabled(true);
            replay = new ReplayHandler(GetProtocolVersion());
            replay.MetaData.serverName = GetServerHost() + GetServerPort();
        }

        public override void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
        {
            replay.AddPacket(packetID, packetData, isLogin, isInbound);
        }
    }
}
