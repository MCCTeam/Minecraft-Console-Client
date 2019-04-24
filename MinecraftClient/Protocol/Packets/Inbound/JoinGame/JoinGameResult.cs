namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal struct JoinGameResult: IInboundData
    {
        public int Dimension { get; internal set; }
        public int ViewDistance { get; internal set; }
    }
}