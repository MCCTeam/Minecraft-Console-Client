using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Keys;

namespace MinecraftClient.Protocol
{
    public class PlayerInfo
    {
        public readonly Guid UUID;

        public readonly string Name;

        // Tuple<Name, Value, Signature(empty if there is no signature)
        public readonly Tuple<string, string, string>[]? Property;

        public int Gamemode;

        public int Ping;

        public string? DisplayName;

        private PublicKey? PublicKey;

        private DateTime? KeyExpiresAt;

        public PlayerInfo(Guid uuid, string name, Tuple<string, string, string>[]? property, int gamemode, int ping, string? displayName, long? timeStamp, byte[]? publicKey, byte[]? signature)
        {
            UUID = uuid;
            Name = name;
            if (property != null)
                Property = property;
            Gamemode = gamemode;
            Ping = ping;
            DisplayName = displayName;
            if (timeStamp != null && publicKey != null && signature != null)
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)timeStamp);
                KeyExpiresAt = dateTimeOffset.UtcDateTime;
                try
                {
                    PublicKey = new PublicKey(publicKey, signature);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    PublicKey = null;
                }
            }
        }

        public PlayerInfo(string name, Guid uuid)
        {
            Name = name;
            UUID = uuid;
            Gamemode = -1;
            Ping = 0;
        }

        public bool isKeyVaild()
        {
            return PublicKey != null && DateTime.Now.ToUniversalTime() > this.KeyExpiresAt;
        }
    }
}
