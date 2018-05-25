using System;
using System.Collections.Generic;
using System.IO;

namespace MinecraftClient.Protocol.Session
{
    [Serializable]
    public class SessionToken
    {
        public string ID { get; set; }
        public string PlayerName { get; set; }
        public string PlayerID { get; set; }
        public string ClientID { get; set; }

        public SessionToken()
        {
            ID = String.Empty;
            PlayerName = String.Empty;
            PlayerID = String.Empty;
            ClientID = String.Empty;
        }

        public override string ToString()
        {
            return String.Join(",", ID, PlayerName, PlayerID, ClientID);
        }

        public static SessionToken FromString(string tokenString)
        {
            string[] fields = tokenString.Split(',');
            if (fields.Length < 4)
                throw new InvalidDataException("Invalid string format");

            SessionToken session = new SessionToken();
            session.ID = fields[0];
            session.PlayerName = fields[1];
            session.PlayerID = fields[2];
            session.ClientID = fields[3];

            Guid temp;
            if (!Guid.TryParseExact(session.ID, "N", out temp))
                throw new InvalidDataException("Invalid session ID");
            if (!ChatBot.IsValidName(session.PlayerName))
                throw new InvalidDataException("Invalid player name");
            if (!Guid.TryParseExact(session.PlayerID, "N", out temp))
                throw new InvalidDataException("Invalid player ID");
            if (!Guid.TryParseExact(session.ClientID, "N", out temp))
                throw new InvalidDataException("Invalid client ID");

            return session;
        }
    }
}
