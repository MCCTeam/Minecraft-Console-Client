using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MinecraftClient.Scripting;

namespace MinecraftClient.Protocol.Session
{
    [Serializable]
    public class SessionToken
    {
        [JsonInclude]
        [JsonPropertyName("SessionID")]
        public string ID { get; set; }

        [JsonInclude]
        [JsonPropertyName("PlayerName")]
        public string PlayerName { get; set; }

        [JsonInclude]
        [JsonPropertyName("PlayerID")]
        public string PlayerID { get; set; }

        [JsonInclude]
        [JsonPropertyName("ClientID")]
        public string ClientID { get; set; }

        [JsonInclude]
        [JsonPropertyName("RefreshToken")]
        public string RefreshToken { get; set; }

        [JsonIgnore]
        public string? ServerInfoHash = null;

        [JsonIgnore]
        public Task<Tuple<bool, string?>>? SessionPreCheckTask = null;

        public SessionToken()
        {
            ID = string.Empty;
            PlayerName = string.Empty;
            PlayerID = string.Empty;
            ClientID = string.Empty;
            RefreshToken = string.Empty;
        }
    }
}
