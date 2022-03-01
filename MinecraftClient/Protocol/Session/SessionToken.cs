﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace MinecraftClient.Protocol.Session
{
    [Serializable]
    public class SessionToken
    {
        private static readonly Regex JwtRegex = new Regex("^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+$");

        public string ID { get; set; }
        public string PlayerName { get; set; }
        public string PlayerID { get; set; }
        public string ClientID { get; set; }
        public string RefreshToken { get; set; }

        public SessionToken()
        {
            ID = String.Empty;
            PlayerName = String.Empty;
            PlayerID = String.Empty;
            ClientID = String.Empty;
            RefreshToken = String.Empty;
        }

        public override string ToString()
        {
            return String.Join(",", ID, PlayerName, PlayerID, ClientID, RefreshToken);
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
            // Backward compatible with old session file without refresh token field
            if (fields.Length > 4)
                session.RefreshToken = fields[4];
            else
                session.RefreshToken = String.Empty;

            Guid temp;
            if (!JwtRegex.IsMatch(session.ID))
                throw new InvalidDataException("Invalid session ID");
            if (!ChatBot.IsValidName(session.PlayerName))
                throw new InvalidDataException("Invalid player name");
            if (!Guid.TryParseExact(session.PlayerID, "N", out temp))
                throw new InvalidDataException("Invalid player ID");
            if (!Guid.TryParseExact(session.ClientID, "N", out temp))
                throw new InvalidDataException("Invalid client ID");
            // No validation on refresh token because it is custom format token (not Jwt)

            return session;
        }
    }
}
