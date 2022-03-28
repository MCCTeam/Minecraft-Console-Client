using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using MinecraftClient.Proxy;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Session;
using System.Security.Authentication;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Handle login, session, server ping and provide a protocol handler for interacting with a minecraft server.
    /// </summary>
    /// <remarks>
    /// Typical update steps for marking a new Minecraft version as supported:
    ///  - Add protocol ID in GetProtocolHandler()
    ///  - Add 1.X.X case in MCVer2ProtocolVersion()
    /// </remarks>
    public static class ProtocolHandler
    {
        /// <summary>
        /// Perform a DNS lookup for a Minecraft Service using the specified domain name
        /// </summary>
        /// <param name="domain">Input domain name, updated with target host if any, else left untouched</param>
        /// <param name="port">Updated with target port if any, else left untouched</param>
        /// <returns>TRUE if a Minecraft Service was found.</returns>
        public static bool MinecraftServiceLookup(ref string domain, ref ushort port)
        {
            bool foundService = false;
            string domainVal = domain;
            ushort portVal = port;

            if (!String.IsNullOrEmpty(domain) && domain.Any(c => char.IsLetter(c)))
            {
                AutoTimeout.Perform(() =>
                {
                    try
                    {
                        Translations.WriteLine("mcc.resolve", domainVal);
                        Heijden.DNS.Response response = new Heijden.DNS.Resolver().Query("_minecraft._tcp." + domainVal, Heijden.DNS.QType.SRV);
                        Heijden.DNS.RecordSRV[] srvRecords = response.RecordsSRV;
                        if (srvRecords != null && srvRecords.Any())
                        {
                            //Order SRV records by priority and weight, then randomly
                            Heijden.DNS.RecordSRV result = srvRecords
                                .OrderBy(record => record.PRIORITY)
                                .ThenByDescending(record => record.WEIGHT)
                                .ThenBy(record => Guid.NewGuid())
                                .First();
                            string target = result.TARGET.Trim('.');
                            ConsoleIO.WriteLineFormatted(Translations.Get("mcc.found", target, result.PORT, domainVal));
                            domainVal = target;
                            portVal = result.PORT;
                            foundService = true;
                        }
                    }
                    catch (Exception e)
                    {
                        ConsoleIO.WriteLineFormatted(Translations.Get("mcc.not_found", domainVal, e.GetType().FullName, e.Message));
                    }
                }, TimeSpan.FromSeconds(Settings.ResolveSrvRecordsShortTimeout ? 10 : 30));
            }

            domain = domainVal;
            port = portVal;
            return foundService;
        }

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
                    if (Protocol18Handler.doPing(serverIP, serverPort, ref protocolversionTmp, ref forgeInfoTmp)
                        || Protocol16Handler.doPing(serverIP, serverPort, ref protocolversionTmp))
                    {
                        success = true;
                    }
                    else Translations.WriteLineFormatted("error.unexpect_response");
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted(String.Format("§8{0}: {1}", e.GetType().FullName, e.Message));
                }
            }, TimeSpan.FromSeconds(Settings.ResolveSrvRecordsShortTimeout ? 10 : 30)))
            {
                if (protocolversion != 0 && protocolversion != protocolversionTmp)
                    Translations.WriteLineFormatted("error.version_different");
                if (protocolversion == 0 && protocolversionTmp <= 1)
                    Translations.WriteLineFormatted("error.no_version_report");
                if (protocolversion == 0)
                    protocolversion = protocolversionTmp;
                forgeInfo = forgeInfoTmp;
                return success;
            }
            else
            {
                Translations.WriteLineFormatted("error.connection_timeout");
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
        public static IMinecraftCom GetProtocolHandler(TcpClient Client, int ProtocolVersion, ForgeInfo forgeInfo, IMinecraftComHandler Handler)
        {
            int[] supportedVersions_Protocol16 = { 51, 60, 61, 72, 73, 74, 78 };
            if (Array.IndexOf(supportedVersions_Protocol16, ProtocolVersion) > -1)
                return new Protocol16Handler(Client, ProtocolVersion, Handler);
            int[] supportedVersions_Protocol18 = { 4, 5, 47, 107, 108, 109, 110, 210, 315, 316, 335, 338, 340, 393, 401, 404, 477, 480, 485, 490, 498, 573, 575, 578, 735, 736, 751, 753, 754, 755, 756, 757, 758 };
            if (Array.IndexOf(supportedVersions_Protocol18, ProtocolVersion) > -1)
                return new Protocol18Handler(Client, ProtocolVersion, Handler, forgeInfo);
            throw new NotSupportedException(Translations.Get("exception.version_unsupport", ProtocolVersion));
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
                    case "1.6":
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
                    case "1.8":
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
                    case "1.9":
                    case "1.9.0":
                        return 107;
                    case "1.9.1":
                        return 108;
                    case "1.9.2":
                        return 109;
                    case "1.9.3":
                    case "1.9.4":
                        return 110;
                    case "1.10":
                    case "1.10.0":
                    case "1.10.1":
                    case "1.10.2":
                        return 210;
                    case "1.11":
                    case "1.11.0":
                        return 315;
                    case "1.11.1":
                    case "1.11.2":
                        return 316;
                    case "1.12":
                    case "1.12.0":
                        return 335;
                    case "1.12.1":
                        return 338;
                    case "1.12.2":
                        return 340;
                    case "1.13":
                        return 393;
                    case "1.13.1":
                        return 401;
                    case "1.13.2":
                        return 404;
                    case "1.14":
                    case "1.14.0":
                        return 477;
                    case "1.14.1":
                        return 480;
                    case "1.14.2":
                        return 485;
                    case "1.14.3":
                        return 490;
                    case "1.14.4":
                        return 498;
                    case "1.15":
                    case "1.15.0":
                        return 573;
                    case "1.15.1":
                        return 575;
                    case "1.15.2":
                        return 578;
                    case "1.16":
                    case "1.16.0":
                        return 735;
                    case "1.16.1":
                        return 736;
                    case "1.16.2":
                        return 751;
                    case "1.16.3":
                        return 753;
                    case "1.16.4":
                    case "1.16.5":
                        return 754;
                    case "1.17":
                        return 755;
                    case "1.17.1":
                        return 756;
                    case "1.18":
                    case "1.18.1":
                        return 757;
                    case "1.18.2":
                        return 758;
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

        /// <summary>
        /// Convert a network protocol version number to human-readable Minecraft version number
        /// </summary>
        /// <remarks>Some Minecraft versions share the same protocol number. In that case, the lowest version for that protocol is returned.</remarks>
        /// <param name="protocol">The Minecraft protocol version number</param>
        /// <returns>The 1.X.X version number, or 0.0 if could not determine protocol version</returns>
        public static string ProtocolVersion2MCVer(int protocol)
        {
            switch (protocol)
            {
                case 51: return "1.4.6";
                case 60: return "1.5.1";
                case 62: return "1.5.2";
                case 72: return "1.6";
                case 73: return "1.6.1";
                case 4: return "1.7.2";
                case 5: return "1.7.6";
                case 47: return "1.8";
                case 107: return "1.9";
                case 108: return "1.9.1";
                case 109: return "1.9.2";
                case 110: return "1.9.3";
                case 210: return "1.10";
                case 315: return "1.11";
                case 316: return "1.11.1";
                case 335: return "1.12";
                case 338: return "1.12.1";
                case 340: return "1.12.2";
                case 393: return "1.13";
                case 401: return "1.13.1";
                case 404: return "1.13.2";
                case 477: return "1.14";
                case 480: return "1.14.1";
                case 485: return "1.14.2";
                case 490: return "1.14.3";
                case 498: return "1.14.4";
                case 573: return "1.15";
                case 575: return "1.15.1";
                case 578: return "1.15.2";
                case 735: return "1.16";
                case 736: return "1.16.1";
                case 751: return "1.16.2";
                case 753: return "1.16.3";
                case 754: return "1.16.5";
                case 755: return "1.17";
                case 756: return "1.17.1";
                case 757: return "1.18.1";
                case 758: return "1.18.2";
                default: return "0.0";
            }
        }

        /// <summary>
        /// Check if we can force-enable Forge support for a Minecraft version without using server Ping
        /// </summary>
        /// <param name="protocolVersion">Minecraft protocol version</param>
        /// <returns>TRUE if we can force-enable Forge support without using server Ping</returns>
        public static bool ProtocolMayForceForge(int protocol)
        {
            return Protocol18Forge.ServerMayForceForge(protocol);
        }

        /// <summary>
        /// Server Info: Consider Forge to be enabled regardless of server Ping
        /// </summary>
        /// <param name="protocolVersion">Minecraft protocol version</param>
        /// <returns>ForgeInfo item stating that Forge is enabled</returns>
        public static ForgeInfo ProtocolForceForge(int protocol)
        {
            return Protocol18Forge.ServerForceForge(protocol);
        }

        public enum LoginResult { OtherError, ServiceUnavailable, SSLError, Success, WrongPassword, AccountMigrated, NotPremium, LoginRequired, InvalidToken, InvalidResponse, NullError, UserCancel };
        public enum AccountType { Mojang, Microsoft };

        /// <summary>
        /// Allows to login to a premium Minecraft account using the Yggdrasil authentication scheme.
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="pass">Password</param>
        /// <param name="session">In case of successful login, will contain session information for multiplayer</param>
        /// <returns>Returns the status of the login (Success, Failure, etc.)</returns>
        public static LoginResult GetLogin(string user, string pass, AccountType type, out SessionToken session)
        {
            if (type == AccountType.Microsoft)
            {
                if (Settings.LoginMethod == "mcc")
                    return MicrosoftMCCLogin(user, pass, out session);
                else
                    return MicrosoftBrowserLogin(out session, user);
            }
            else if (type == AccountType.Mojang)
            {
                return MojangLogin(user, pass, out session);
            }
            else throw new InvalidOperationException("Account type must be Mojang or Microsoft");
        }

        /// <summary>
        /// Login using Mojang account. Will be outdated after account migration
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private static LoginResult MojangLogin(string user, string pass, out SessionToken session)
        {
            session = new SessionToken() { ClientID = Guid.NewGuid().ToString().Replace("-", "") };

            try
            {
                string result = "";
                string json_request = "{\"agent\": { \"name\": \"Minecraft\", \"version\": 1 }, \"username\": \"" + JsonEncode(user) + "\", \"password\": \"" + JsonEncode(pass) + "\", \"clientToken\": \"" + JsonEncode(session.ClientID) + "\" }";
                int code = DoHTTPSPost("authserver.mojang.com", "/authenticate", json_request, ref result);
                if (code == 200)
                {
                    if (result.Contains("availableProfiles\":[]}"))
                    {
                        return LoginResult.NotPremium;
                    }
                    else
                    {
                        Json.JSONData loginResponse = Json.ParseJson(result);
                        if (loginResponse.Properties.ContainsKey("accessToken")
                            && loginResponse.Properties.ContainsKey("selectedProfile")
                            && loginResponse.Properties["selectedProfile"].Properties.ContainsKey("id")
                            && loginResponse.Properties["selectedProfile"].Properties.ContainsKey("name"))
                        {
                            session.ID = loginResponse.Properties["accessToken"].StringValue;
                            session.PlayerID = loginResponse.Properties["selectedProfile"].Properties["id"].StringValue;
                            session.PlayerName = loginResponse.Properties["selectedProfile"].Properties["name"].StringValue;
                            return LoginResult.Success;
                        }
                        else return LoginResult.InvalidResponse;
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
                    ConsoleIO.WriteLineFormatted(Translations.Get("error.http_code", code));
                    return LoginResult.OtherError;
                }
            }
            catch (System.Security.Authentication.AuthenticationException e)
            {
                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                }
                return LoginResult.SSLError;
            }
            catch (System.IO.IOException e)
            {
                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                }
                if (e.Message.Contains("authentication"))
                {
                    return LoginResult.SSLError;
                }
                else return LoginResult.OtherError;
            }
            catch (Exception e)
            {
                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                }
                return LoginResult.OtherError;
            }
        }

        /// <summary>
        /// Sign-in to Microsoft Account without using browser. Only works if 2FA is disabled.
        /// Might not work well in some rare cases.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private static LoginResult MicrosoftMCCLogin(string email, string password, out SessionToken session)
        {
            try
            {
                var msaResponse = XboxLive.UserLogin(email, password, XboxLive.PreAuth());
                // Remove refresh token for MCC sign method
                msaResponse.RefreshToken = string.Empty;
                return MicrosoftLogin(msaResponse, out session);
            }
            catch (Exception e)
            {
                session = new SessionToken() { ClientID = Guid.NewGuid().ToString().Replace("-", "") };
                ConsoleIO.WriteLineFormatted("§cMicrosoft authenticate failed: " + e.Message);
                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§c" + e.StackTrace);
                }
                return LoginResult.WrongPassword; // Might not always be wrong password
            }
        }

        /// <summary>
        /// Sign-in to Microsoft Account by asking user to open sign-in page using browser. 
        /// </summary>
        /// <remarks>
        /// The downside is this require user to copy and paste lengthy content from and to console.
        /// Sign-in page: 218 chars
        /// Response URL: around 1500 chars
        /// </remarks>
        /// <param name="session"></param>
        /// <returns></returns>
        public static LoginResult MicrosoftBrowserLogin(out SessionToken session, string loginHint = "")
        {
            if (string.IsNullOrEmpty(loginHint))
                Microsoft.OpenBrowser(Microsoft.SignInUrl);
            else
                Microsoft.OpenBrowser(Microsoft.GetSignInUrlWithHint(loginHint));
            ConsoleIO.WriteLine("Your browser should open automatically. If not, open the link below in your browser.");
            ConsoleIO.WriteLine("\n" + Microsoft.SignInUrl + "\n");

            ConsoleIO.WriteLine("Paste your code here");
            string code = ConsoleIO.ReadLine();

            var msaResponse = Microsoft.RequestAccessToken(code);
            return MicrosoftLogin(msaResponse, out session);
        }

        public static LoginResult MicrosoftLoginRefresh(string refreshToken, out SessionToken session)
        {
            var msaResponse = Microsoft.RefreshAccessToken(refreshToken);
            return MicrosoftLogin(msaResponse, out session);
        }

        private static LoginResult MicrosoftLogin(Microsoft.LoginResponse msaResponse, out SessionToken session)
        {
            session = new SessionToken() { ClientID = Guid.NewGuid().ToString().Replace("-", "") };

            try
            {
                var xblResponse = XboxLive.XblAuthenticate(msaResponse);
                var xsts = XboxLive.XSTSAuthenticate(xblResponse); // Might throw even password correct

                string accessToken = MinecraftWithXbox.LoginWithXbox(xsts.UserHash, xsts.Token);
                bool hasGame = MinecraftWithXbox.UserHasGame(accessToken);
                if (hasGame)
                {
                    var profile = MinecraftWithXbox.GetUserProfile(accessToken);
                    session.PlayerName = profile.UserName;
                    session.PlayerID = profile.UUID;
                    session.ID = accessToken;
                    session.RefreshToken = msaResponse.RefreshToken;
                    Settings.Login = msaResponse.Email;
                    return LoginResult.Success;
                }
                else
                {
                    return LoginResult.NotPremium;
                }
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§cMicrosoft authenticate failed: " + e.Message);
                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§c" + e.StackTrace);
                }
                return LoginResult.WrongPassword; // Might not always be wrong password
            }
        }

        /// <summary>
        /// Validates whether accessToken must be refreshed
        /// </summary>
        /// <param name="session">Session token to validate</param>
        /// <returns>Returns the status of the token (Valid, Invalid, etc.)</returns>
        public static LoginResult GetTokenValidation(SessionToken session)
        {
            var payload = JwtPayloadDecode.GetPayload(session.ID);
            var json = Json.ParseJson(payload);
            var expTimestamp = long.Parse(json.Properties["exp"].StringValue);
            var now = DateTime.Now;
            var tokenExp = UnixTimeStampToDateTime(expTimestamp);
            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLine("Access token expiration time is " + tokenExp.ToString());
            }
            if (now < tokenExp)
            {
                // Still valid
                return LoginResult.Success;
            }
            else
            {
                // Token expired
                return LoginResult.LoginRequired;
            }
        }

        /// <summary>
        /// Refreshes invalid token
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="session">In case of successful token refresh, will contain session information for multiplayer</param>
        /// <returns>Returns the status of the new token request (Success, Failure, etc.)</returns>
        public static LoginResult GetNewToken(SessionToken currentsession, out SessionToken session)
        {
            session = new SessionToken();
            try
            {
                string result = "";
                string json_request = "{ \"accessToken\": \"" + JsonEncode(currentsession.ID) + "\", \"clientToken\": \"" + JsonEncode(currentsession.ClientID) + "\", \"selectedProfile\": { \"id\": \"" + JsonEncode(currentsession.PlayerID) + "\", \"name\": \"" + JsonEncode(currentsession.PlayerName) + "\" } }";
                int code = DoHTTPSPost("authserver.mojang.com", "/refresh", json_request, ref result);
                if (code == 200)
                {
                    if (result == null)
                    {
                        return LoginResult.NullError;
                    }
                    else
                    {
                        Json.JSONData loginResponse = Json.ParseJson(result);
                        if (loginResponse.Properties.ContainsKey("accessToken")
                            && loginResponse.Properties.ContainsKey("selectedProfile")
                            && loginResponse.Properties["selectedProfile"].Properties.ContainsKey("id")
                            && loginResponse.Properties["selectedProfile"].Properties.ContainsKey("name"))
                        {
                            session.ID = loginResponse.Properties["accessToken"].StringValue;
                            session.PlayerID = loginResponse.Properties["selectedProfile"].Properties["id"].StringValue;
                            session.PlayerName = loginResponse.Properties["selectedProfile"].Properties["name"].StringValue;
                            return LoginResult.Success;
                        }
                        else return LoginResult.InvalidResponse;
                    }
                }
                else if (code == 403 && result.Contains("InvalidToken"))
                {
                    return LoginResult.InvalidToken;
                }
                else
                {
                    ConsoleIO.WriteLineFormatted(Translations.Get("error.auth", code));
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
                int code = DoHTTPSPost("sessionserver.mojang.com", "/session/minecraft/join", json_request, ref result);
                return (code >= 200 && code < 300);
            }
            catch { return false; }
        }

        /// <summary>
        /// Retrieve available Realms worlds of a player and display them
        /// </summary>
        /// <param name="username">Player Minecraft username</param>
        /// <param name="uuid">Player UUID</param>
        /// <param name="accesstoken">Access token</param>
        /// <returns>List of ID of available Realms worlds</returns>
        public static List<string> RealmsListWorlds(string username, string uuid, string accesstoken)
        {
            List<string> realmsWorldsResult = new List<string>(); // Store world ID
            try
            {
                string result = "";
                string cookies = String.Format("sid=token:{0}:{1};user={2};version={3}", accesstoken, uuid, username, Program.MCHighestVersion);
                DoHTTPSGet("pc.realms.minecraft.net", "/worlds", cookies, ref result);
                Json.JSONData realmsWorlds = Json.ParseJson(result);
                if (realmsWorlds.Properties.ContainsKey("servers")
                    && realmsWorlds.Properties["servers"].Type == Json.JSONData.DataType.Array
                    && realmsWorlds.Properties["servers"].DataArray.Count > 0)
                {
                    List<string> availableWorlds = new List<string>(); // Store string to print
                    int index = 0;
                    foreach (Json.JSONData realmsServer in realmsWorlds.Properties["servers"].DataArray)
                    {
                        if (realmsServer.Properties.ContainsKey("name")
                            && realmsServer.Properties.ContainsKey("owner")
                            && realmsServer.Properties.ContainsKey("id")
                            && realmsServer.Properties.ContainsKey("expired"))
                        {
                            if (realmsServer.Properties["expired"].StringValue == "false")
                            {
                                availableWorlds.Add(String.Format("[{0}] {2} ({3}) - {1}",
                                    index++,
                                    realmsServer.Properties["id"].StringValue,
                                    realmsServer.Properties["name"].StringValue,
                                    realmsServer.Properties["owner"].StringValue));
                                realmsWorldsResult.Add(realmsServer.Properties["id"].StringValue);
                            }
                        }
                    }
                    if (availableWorlds.Count > 0)
                    {
                        Translations.WriteLine("mcc.realms_available");
                        foreach (var world in availableWorlds)
                            ConsoleIO.WriteLine(world);
                        Translations.WriteLine("mcc.realms_join");
                    }
                }

            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.GetType().ToString() + ": " + e.Message);
                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.StackTrace);
                }
            }
            return realmsWorldsResult;
        }

        /// <summary>
        /// Get the server address of a Realms world by world ID
        /// </summary>
        /// <param name="worldId">The world ID of the Realms world</param>
        /// <param name="username">Player Minecraft username</param>
        /// <param name="uuid">Player UUID</param>
        /// <param name="accesstoken">Access token</param>
        /// <returns>Server address (host:port) or empty string if failure</returns>
        public static string GetRealmsWorldServerAddress(string worldId, string username, string uuid, string accesstoken)
        {
            try
            {
                string result = "";
                string cookies = String.Format("sid=token:{0}:{1};user={2};version={3}", accesstoken, uuid, username, Program.MCHighestVersion);
                int statusCode = DoHTTPSGet("pc.realms.minecraft.net", "/worlds/v1/" + worldId + "/join/pc", cookies, ref result);
                if (statusCode == 200)
                {
                    Json.JSONData serverAddress = Json.ParseJson(result);
                    if (serverAddress.Properties.ContainsKey("address"))
                        return serverAddress.Properties["address"].StringValue;
                    else
                    {
                        Translations.WriteLine("error.realms.ip_error");
                        return "";
                    }
                }
                else
                {
                    Translations.WriteLine("error.realms.access_denied");
                    return "";
                }
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.GetType().ToString() + ": " + e.Message);
                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.StackTrace);
                }
                return "";
            }
        }

        /// <summary>
        /// Make a HTTPS GET request to the specified endpoint of the Mojang API
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="cookies">Cookies for making the request</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static int DoHTTPSGet(string host, string endpoint, string cookies, ref string result)
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
            http_request.Add("");
            return DoHTTPSRequest(http_request, host, ref result);
        }

        /// <summary>
        /// Make a HTTPS POST request to the specified endpoint of the Mojang API
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="request">Request payload</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static int DoHTTPSPost(string host, string endpoint, string request, ref string result)
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
            return DoHTTPSRequest(http_request, host, ref result);
        }

        /// <summary>
        /// Manual HTTPS request since we must directly use a TcpClient because of the proxy.
        /// This method connects to the server, enables SSL, do the request and read the response.
        /// </summary>
        /// <param name="headers">Request headers and optional body (POST)</param>
        /// <param name="host">Host to connect to</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static int DoHTTPSRequest(List<string> headers, string host, ref string result)
        {
            string postResult = null;
            int statusCode = 520;
            Exception exception = null;
            AutoTimeout.Perform(() =>
            {
                try
                {
                    if (Settings.DebugMessages)
                        ConsoleIO.WriteLineFormatted(Translations.Get("debug.request", host));

                    TcpClient client = ProxyHandler.newTcpClient(host, 443, true);
                    SslStream stream = new SslStream(client.GetStream());
                    stream.AuthenticateAsClient(host, null, (SslProtocols)3072, true); // Enable TLS 1.2. Hotfix for #1780

                    if (Settings.DebugMessages)
                        foreach (string line in headers)
                            ConsoleIO.WriteLineFormatted("§8> " + line);

                    stream.Write(Encoding.ASCII.GetBytes(String.Join("\r\n", headers.ToArray())));
                    System.IO.StreamReader sr = new System.IO.StreamReader(stream);
                    string raw_result = sr.ReadToEnd();

                    if (Settings.DebugMessages)
                    {
                        ConsoleIO.WriteLine("");
                        foreach (string line in raw_result.Split('\n'))
                            ConsoleIO.WriteLineFormatted("§8< " + line);
                    }

                    if (raw_result.StartsWith("HTTP/1.1"))
                    {
                        postResult = raw_result.Substring(raw_result.IndexOf("\r\n\r\n") + 4);
                        statusCode = Settings.str2int(raw_result.Split(' ')[1]);
                    }
                    else statusCode = 520; //Web server is returning an unknown error
                }
                catch (Exception e)
                {
                    if (!(e is System.Threading.ThreadAbortException))
                    {
                        exception = e;
                    }
                }
            }, TimeSpan.FromSeconds(30));
            result = postResult;
            if (exception != null)
                throw exception;
            return statusCode;
        }

        /// <summary>
        /// Encode a string to a json string.
        /// Will convert special chars to \u0000 unicode escape sequences.
        /// </summary>
        /// <param name="text">Source text</param>
        /// <returns>Encoded text</returns>
        private static string JsonEncode(string text)
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

        /// <summary>
        /// Convert a TimeStamp (in second) to DateTime object
        /// </summary>
        /// <param name="unixTimeStamp">TimeStamp in second</param>
        /// <returns>DateTime object of the TimeStamp</returns>
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
