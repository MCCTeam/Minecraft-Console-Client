using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Proxy;
using System.Net.Sockets;
using System.Net.Security;
using MinecraftClient.Protocol.Handlers.Forge;


namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Handle login, session, server ping and provide a protocol handler for interacting with a minecraft server.
    /// </summary>

    public static class ProtocolHandler
    {
        /// <summary>
        /// Retrieve information about a Minecraft server
        /// </summary>
        /// <param name="serverIP">Server IP to ping</param>
        /// <param name="serverPort">Server Port to ping</param>
        /// <param name="protocolversion">Will contain protocol version, if ping successful</param>
        /// <returns>TRUE if ping was successful</returns>

        public static bool GetServerInfo(string serverIP, ushort serverPort, ref int protocolversion, ref ForgeInfo forgeInfo)
        {
            bool success = false;
            int protocolversionTmp = 0;
            ForgeInfo forgeInfoTmp = null;
            if (AutoTimeout.Perform(() =>
            {
                try
                {
                    if (Protocol16Handler.doPing(serverIP, serverPort, ref protocolversionTmp)
                        || Protocol18Handler.doPing(serverIP, serverPort, ref protocolversionTmp, ref forgeInfoTmp))
                    {
                        success = true;
                    }
                    else ConsoleIO.WriteLineFormatted("§8Unexpected response from the server (is that a Minecraft server?)");
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted(String.Format("§8{0}: {1}", e.GetType().FullName, e.Message));
                }
            }, TimeSpan.FromSeconds(30)))
            {
                protocolversion = protocolversionTmp;
                forgeInfo = forgeInfoTmp;
                return success;
            }
            else
            {
                ConsoleIO.WriteLineFormatted("§8A timeout occured while attempting to connect to this IP.");
                return false;
            }
        }

        /// <summary>
        /// Get a protocol handler for the specified Minecraft version
        /// </summary>
        /// <param name="Client">Tcp Client connected to the server</param>
        /// <param name="ProtocolVersion">Protocol version to handle</param>
        /// <param name="Handler">Handler with the appropriate callbacks</param>
        /// <returns></returns>

        public static IMinecraftCom getProtocolHandler(TcpClient Client, int ProtocolVersion, ForgeInfo forgeInfo, IMinecraftComHandler Handler)
        {
            int[] supportedVersions_Protocol16 = { 51, 60, 61, 72, 73, 74, 78 };
            if (Array.IndexOf(supportedVersions_Protocol16, ProtocolVersion) > -1)
                return new Protocol16Handler(Client, ProtocolVersion, Handler);
            int[] supportedVersions_Protocol18 = { 4, 5, 47, 107, 108, 109, 110 };
            if (Array.IndexOf(supportedVersions_Protocol18, ProtocolVersion) > -1)
                return new Protocol18Handler(Client, ProtocolVersion, Handler, forgeInfo);
            throw new NotSupportedException("The protocol version no." + ProtocolVersion + " is not supported.");
        }

        /// <summary>
        /// Convert a human-readable Minecraft version number to network protocol version number
        /// </summary>
        /// <param name="MCVersion">The Minecraft version number</param>
        /// <returns>The protocol version number or 0 if could not determine protocol version: error, unknown, not supported</returns>

        public static int MCVer2ProtocolVersion(string MCVersion)
        {
            if (MCVersion.Contains('.'))
            {
                switch (MCVersion.Split(' ')[0].Trim())
                {
                    case "1.4.6":
                    case "1.4.7":
                        return 51;
                    case "1.5.1":
                        return 60;
                    case "1.5.2":
                        return 61;
                    case "1.6.0":
                        return 72;
                    case "1.6.1":
                    case "1.6.2":
                    case "1.6.3":
                    case "1.6.4":
                        return 73;
                    case "1.7.2":
                    case "1.7.3":
                    case "1.7.4":
                    case "1.7.5":
                        return 4;
                    case "1.7.6":
                    case "1.7.7":
                    case "1.7.8":
                    case "1.7.9":
                    case "1.7.10":
                        return 5;
                    case "1.8.0":
                    case "1.8.1":
                    case "1.8.2":
                    case "1.8.3":
                    case "1.8.4":
                    case "1.8.5":
                    case "1.8.6":
                    case "1.8.7":
                    case "1.8.8":
                    case "1.8.9":
                        return 47;
                    case "1.9.0":
                        return 107;
                    case "1.9.1":
                        return 108;
                    case "1.9.2":
                        return 109;
                    case "1.9.3":
                    case "1.9.4":
                        return 110;
                    default:
                        return 0;
                }
            }
            else
            {
                try
                {
                    return Int32.Parse(MCVersion);
                }
                catch
                {
                    return 0;
                }
            }
        }

        public enum LoginResult { OtherError, ServiceUnavailable, SSLError, Success, WrongPassword, AccountMigrated, NotPremium, LoginRequired, InvalidToken, NullError };

        /// <summary>
        /// Allows to login to a premium Minecraft account using the Yggdrasil authentication scheme.
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="pass">Password</param>
        /// <param name="accesstoken">Will contain the access token returned by Minecraft.net, if the login is successful</param>
        /// <param name="clienttoken">Will contain the client token generated before sending to Minecraft.net</param>
        /// <param name="uuid">Will contain the player's PlayerID, needed for multiplayer</param>
        /// <returns>Returns the status of the login (Success, Failure, etc.)</returns>

        public static LoginResult GetLogin(string user, string pass, out SessionToken session)
        {
            session = new SessionToken() { ClientID = Guid.NewGuid().ToString().Replace("-", "") };

            try
            {
                string result = "";

                string json_request = "{\"agent\": { \"name\": \"Minecraft\", \"version\": 1 }, \"username\": \"" + jsonEncode(user) + "\", \"password\": \"" + jsonEncode(pass) + "\", \"clientToken\": \"" + jsonEncode(session.ClientID) + "\" }";
                int code = doHTTPSPost("authserver.mojang.com", "/authenticate", json_request, ref result);
                if (code == 200)
                {
                    if (result.Contains("availableProfiles\":[]}"))
                    {
                        return LoginResult.NotPremium;
                    }
                    else
                    {
                        string[] temp = result.Split(new string[] { "accessToken\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { session.ID = temp[1].Split('"')[0]; }
                        temp = result.Split(new string[] { "name\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { session.PlayerName = temp[1].Split('"')[0]; }
                        temp = result.Split(new string[] { "availableProfiles\":[{\"id\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { session.PlayerID = temp[1].Split('"')[0]; }
                        return LoginResult.Success;
                    }
                }
                else if (code == 403)
                {
                    if (result.Contains("UserMigratedException"))
                    {
                        return LoginResult.AccountMigrated;
                    }
                    else return LoginResult.WrongPassword;
                }
                else if (code == 503)
                {
                    return LoginResult.ServiceUnavailable;
                }
                else
                {
                    ConsoleIO.WriteLineFormatted("§8Got error code from server: " + code);
                    return LoginResult.OtherError;
                }
            }
            catch (System.Security.Authentication.AuthenticationException)
            {
                return LoginResult.SSLError;
            }
            catch (System.IO.IOException e)
            {
                if (e.Message.Contains("authentication"))
                {
                    return LoginResult.SSLError;
                }
                else return LoginResult.OtherError;
            }
            catch
            {
                return LoginResult.OtherError;
            }
        }

        /// <summary>
        /// Validates whether accessToken must be refreshed
        /// </summary>
        /// <param name="accesstoken">Will contain the cached access token previously returned by Minecraft.net</param>
        /// <param name="clienttoken">Will contain the cached client token created on login</param>
        /// <returns>Returns the status of the token (Valid, Invalid, etc.)</returns>
        /// 
        public static LoginResult GetTokenValidation(SessionToken session)
        {
            try
            {
                string result = "";
                string json_request = "{\"accessToken\": \"" + jsonEncode(session.ID) + "\", \"clientToken\": \"" + jsonEncode(session.ClientID) + "\" }";
                int code = doHTTPSPost("authserver.mojang.com", "/validate", json_request, ref result);
                if (code == 204)
                {
                    return LoginResult.Success;
                }
                else if (code == 403)
                {
                    return LoginResult.LoginRequired;
                }
                else
                {
                    return LoginResult.OtherError;
                }
            }
            catch
            {
                return LoginResult.OtherError;
            }
        }

        /// <summary>
        /// Refreshes invalid token
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="accesstoken">Will contain the new access token returned by Minecraft.net, if the refresh is successful</param>
        /// <param name="clienttoken">Will contain the client token generated before sending to Minecraft.net</param>
        /// <param name="uuid">Will contain the player's PlayerID, needed for multiplayer</param>
        /// <returns>Returns the status of the new token request (Success, Failure, etc.)</returns>
        ///
        public static LoginResult GetNewToken(SessionToken currentsession, out SessionToken newsession)
        {
            newsession = new SessionToken();
            try
            {
                string result = "";
                string json_request = "{ \"accessToken\": \"" + jsonEncode(currentsession.ID) + "\", \"clientToken\": \"" + jsonEncode(currentsession.ClientID) + "\", \"selectedProfile\": { \"id\": \"" + jsonEncode(currentsession.PlayerID) + "\", \"name\": \"" + jsonEncode(currentsession.PlayerName) + "\" } }";
                int code = doHTTPSPost("authserver.mojang.com", "/refresh", json_request, ref result);
                if (code == 200)
                {
                    if (result == null)
                    {
                        return LoginResult.NullError;
                    }
                    else {
                        string[] temp = result.Split(new string[] { "accessToken\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { newsession.ID = temp[1].Split('"')[0]; }
                        temp = result.Split(new string[] { "clientToken\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { newsession.ClientID = temp[1].Split('"')[0]; }
                        temp = result.Split(new string[] { "name\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { newsession.PlayerName = temp[1].Split('"')[0]; }
                        temp = result.Split(new string[] { "selectedProfile\":[{\"id\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { newsession.PlayerID = temp[1].Split('"')[0]; }
                        return LoginResult.Success;
                    }
                }
                else if (code == 403 && result.Contains("InvalidToken"))
                {
                    return LoginResult.InvalidToken;
                }
                else
                {
                    ConsoleIO.WriteLineFormatted("§8Got error code from server while refreshing authentication: " + code);
                    return LoginResult.OtherError;
                }
            }
            catch
            {
                return LoginResult.OtherError;
            }
        }

        /// <summary>
        /// Check session using Mojang's Yggdrasil authentication scheme. Allows to join an online-mode server
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="accesstoken">Session ID</param>
        /// <param name="serverhash">Server ID</param>
        /// <returns>TRUE if session was successfully checked</returns>

        public static bool SessionCheck(string uuid, string accesstoken, string serverhash)
        {
            try
            {
                string result = "";
                string json_request = "{\"accessToken\":\"" + accesstoken + "\",\"selectedProfile\":\"" + uuid + "\",\"serverId\":\"" + serverhash + "\"}";
                int code = doHTTPSPost("sessionserver.mojang.com", "/session/minecraft/join", json_request, ref result);
                return (result == "");
            }
            catch { return false; }
        }

        public static void RealmsListWorlds(string username, string uuid, string accesstoken)
        {
            string result = "";
            string cookies = String.Format("sid=token:{0}:{1};user={2};version={3}", accesstoken, uuid, username, Program.MCHighestVersion);
            doHTTPSGet("mcoapi.minecraft.net", "/worlds", cookies, ref result);
            Console.WriteLine(result);
        }

        /// <summary>
        /// Make a HTTPS GET request to the specified endpoint of the Mojang API
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="cookies">Cookies for making the request</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>

        private static int doHTTPSGet(string host, string endpoint, string cookies, ref string result)
        {
            List<String> http_request = new List<string>();
            http_request.Add("GET " + endpoint + " HTTP/1.1");
            http_request.Add("Cookie: " + cookies);
            http_request.Add("Cache-Control: no-cache");
            http_request.Add("Pragma: no-cache");
            http_request.Add("Host: " + host);
            http_request.Add("User-Agent: Java/1.6.0_27");
            http_request.Add("Accept-Charset: ISO-8859-1,UTF-8;q=0.7,*;q=0.7");
            http_request.Add("Connection: close");
            http_request.Add("");
            return doHTTPSRequest(http_request, host, ref result);
        }

        /// <summary>
        /// Make a HTTPS POST request to the specified endpoint of the Mojang API
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="request">Request payload</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>

        private static int doHTTPSPost(string host, string endpoint, string request, ref string result)
        {
            List<String> http_request = new List<string>();
            http_request.Add("POST " + endpoint + " HTTP/1.1");
            http_request.Add("Host: " + host);
            http_request.Add("User-Agent: MCC/" + Program.Version);
            http_request.Add("Content-Type: application/json");
            http_request.Add("Content-Length: " + Encoding.ASCII.GetBytes(request).Length);
            http_request.Add("Connection: close");
            http_request.Add("");
            http_request.Add(request);
            return doHTTPSRequest(http_request, host, ref result);
        }

        /// <summary>
        /// Manual HTTPS request since we must directly use a TcpClient because of the proxy.
        /// This method connects to the server, enables SSL, do the request and read the response.
        /// </summary>
        /// <param name="headers">Request headers and optional body (POST)</param>
        /// <param name="host">Host to connect to</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>

        private static int doHTTPSRequest(List<string> headers, string host, ref string result)
        {
            string postResult = null;
            int statusCode = 520;
            AutoTimeout.Perform(() =>
            {
                TcpClient client = ProxyHandler.newTcpClient(host, 443, true);
                SslStream stream = new SslStream(client.GetStream());
                stream.AuthenticateAsClient(host);
                stream.Write(Encoding.ASCII.GetBytes(String.Join("\r\n", headers.ToArray())));
                System.IO.StreamReader sr = new System.IO.StreamReader(stream);
                string raw_result = sr.ReadToEnd();
                if (raw_result.StartsWith("HTTP/1.1"))
                {
                    postResult = raw_result.Substring(raw_result.IndexOf("\r\n\r\n") + 4);
                    statusCode = Settings.str2int(raw_result.Split(' ')[1]);
                }
                else statusCode = 520; //Web server is returning an unknown error
            }, TimeSpan.FromSeconds(30));
            result = postResult;
            return statusCode;
        }

        /// <summary>
        /// Encode a string to a json string.
        /// Will convert special chars to \u0000 unicode escape sequences.
        /// </summary>
        /// <param name="text">Source text</param>
        /// <returns>Encoded text</returns>

        private static string jsonEncode(string text)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in text)
            {
                if ((c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z'))
                {
                    result.Append(c);
                }
                else
                {
                    result.AppendFormat(@"\u{0:x4}", (int)c);
                }
            }

            return result.ToString();
        }
    }
}
