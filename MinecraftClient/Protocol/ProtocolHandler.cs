using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Proxy;
using System.Net.Sockets;
using System.Net.Security;

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
        /// <param name="protocolversion">Will contain protocol version, if ping successful</param>
        /// <param name="version">Will contain minecraft version, if ping successful</param>
        /// <returns>TRUE if ping was successful</returns>

        public static bool GetServerInfo(string serverIP, ref int protocolversion, ref string version)
        {
            try
            {
                string host; int port;
                string[] sip = serverIP.Split(':');
                host = sip[0];

                if (sip.Length == 1)
                {
                    port = 25565;
                }
                else
                {
                    try
                    {
                        port = Convert.ToInt32(sip[1]);
                    }
                    catch (FormatException) { port = 25565; }
                }

                if (Protocol16Handler.doPing(host, port, ref protocolversion, ref version))
                {
                    return true;
                }
                else if (Protocol17Handler.doPing(host, port, ref protocolversion, ref version))
                {
                    return true;
                }
                else
                {
                    ConsoleIO.WriteLineFormatted("§8Unexpected answer from the server (is that a Minecraft server ?)", false);
                    return false;
                }
            }
            catch
            {
                ConsoleIO.WriteLineFormatted("§8An error occured while attempting to connect to this IP.", false);
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

        public static IMinecraftCom getProtocolHandler(TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler)
        {
            int[] supportedVersions_Protocol16 = { 51, 60, 61, 72, 73, 74, 78 };
            if (Array.IndexOf(supportedVersions_Protocol16, ProtocolVersion) > -1)
                return new Protocol16Handler(Client, ProtocolVersion, Handler);
            int[] supportedVersions_Protocol17 = { 4, 5 };
            if (Array.IndexOf(supportedVersions_Protocol17, ProtocolVersion) > -1)
                return new Protocol17Handler(Client, ProtocolVersion, Handler);
            throw new NotSupportedException("The protocol version '" + ProtocolVersion + "' is not supported.");
        }

        public enum LoginResult { OtherError, ServiceUnavailable, SSLError, Success, WrongPassword, AccountMigrated, NotPremium };

        /// <summary>
        /// Allows to login to a premium Minecraft account using the Yggdrasil authentication scheme.
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="pass">Password</param>
        /// <param name="accesstoken">Will contain the access token returned by Minecraft.net, if the login is successful</param>
        /// <param name="uuid">Will contain the player's UUID, needed for multiplayer</param>
        /// <returns>Returns the status of the login (Success, Failure, etc.)</returns>

        public static LoginResult GetLogin(ref string user, string pass, ref string accesstoken, ref string uuid)
        {
            try
            {
                string result = "";
                string json_request = "{\"agent\": { \"name\": \"Minecraft\", \"version\": 1 }, \"username\": \"" + user + "\", \"password\": \"" + pass + "\" }";
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
                        if (temp.Length >= 2) { accesstoken = temp[1].Split('"')[0]; }
                        temp = result.Split(new string[] { "name\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { user = temp[1].Split('"')[0]; }
                        temp = result.Split(new string[] { "availableProfiles\":[{\"id\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                        if (temp.Length >= 2) { uuid = temp[1].Split('"')[0]; }
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
                    ConsoleIO.WriteLineFormatted("§8Got error code from server: " + code, false);
                    return LoginResult.OtherError;
                }
            }
            catch (System.Security.Authentication.AuthenticationException)
            {
                return LoginResult.SSLError;
            }/*
            catch
            {
                return LoginResult.OtherError;
            }*/
        }

        /// <summary>
        /// Check session using Mojang's Yggdrasil authentication scheme. Allow to join an online-mode server
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

        /// <summary>
        /// Manual HTTPS request since we must directly use a TcpClient because of the proxy.
        /// This method connects to the server, enables SSL, do the request and read the response.
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="request">Request payload</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>

        private static int doHTTPSPost(string host, string endpoint, string request, ref string result)
        {
            TcpClient client = ProxyHandler.newTcpClient(host, 443);
            SslStream stream = new SslStream(client.GetStream());
            stream.AuthenticateAsClient(host);

            List<String> http_request = new List<string>();
            http_request.Add("POST " + endpoint + " HTTP/1.1");
            http_request.Add("Host: " + host);
            http_request.Add("User-Agent: MCC/" + Program.Version);
            http_request.Add("Content-Type: application/json");
            http_request.Add("Content-Length: " + Encoding.ASCII.GetBytes(request).Length);
            http_request.Add("Connection: close");
            http_request.Add("");
            http_request.Add(request);

            stream.Write(Encoding.ASCII.GetBytes(String.Join("\r\n", http_request.ToArray())));
            System.IO.StreamReader sr = new System.IO.StreamReader(stream);
            string raw_result = sr.ReadToEnd();

            if (raw_result.StartsWith("HTTP/1.1"))
            {
                result = raw_result.Substring(raw_result.IndexOf("\r\n\r\n") + 4);
                return Settings.str2int(raw_result.Split(' ')[1]);
            }
            else return 520; //Web server is returning an unknown error
        }
    }
}
