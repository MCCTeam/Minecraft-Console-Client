using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Holds the structured result of a Minecraft server status (SLP) ping,
    /// including MOTD, player counts, sample player list, version, and favicon.
    /// </summary>
    public sealed class ServerStatusInfo
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; }
        public string VersionName { get; init; } = string.Empty;
        public int ProtocolVersion { get; init; }
        public int ResolvedProtocol { get; set; }
        public int OnlinePlayers { get; init; }
        public int MaxPlayers { get; init; }
        public List<SamplePlayer> SamplePlayers { get; init; } = [];
        public string MotdRaw { get; init; } = string.Empty;
        public string? FaviconBase64 { get; init; }
        public long PingMs { get; init; }

        public sealed class SamplePlayer
        {
            public string Name { get; init; } = string.Empty;
            public string Id { get; init; } = string.Empty;
        }
    }
}
