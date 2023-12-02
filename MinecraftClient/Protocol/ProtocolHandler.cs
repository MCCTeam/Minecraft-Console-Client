using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using DnsClient;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using static MinecraftClient.Protocol.Microsoft;
using static MinecraftClient.Settings;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.GeneralConfig;

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
                        ConsoleIO.WriteLine(string.Format(Translations.mcc_resolve, domainVal));
                        var lookupClient = new LookupClient();
                        var response = lookupClient.Query(new DnsQuestion($"_minecraft._tcp.{domainVal}", QueryType.SRV));
                        if (response.HasError != true && response.Answers.SrvRecords().Any())
                        {
                            //Order SRV records by priority and weight, then randomly
                            var result = response.Answers.SrvRecords()
                                .OrderBy(record => record.Priority)
                                .ThenByDescending(record => record.Weight)
                                .ThenBy(record => Guid.NewGuid())
                                .First();
                            string target = result.Target.Value.Trim('.');
                            ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_found, target, result.Port, domainVal));
                            domainVal = target;
                            portVal = result.Port;
                            foundService = true;
                        }
                    }
                    catch (Exception e)
                    {
                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_not_found, domainVal, e.GetType().FullName, e.Message));
                    }
                }, TimeSpan.FromSeconds(Config.Main.Advanced.ResolveSrvRecords == MainConfigHealper.MainConfig.AdvancedConfig.ResolveSrvRecordType.fast ? 10 : 30));
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
        public static bool GetServerInfo(string serverIP, ushort serverPort, ref int protocolversion, ref ForgeInfo? forgeInfo)
        {
            bool success = false;
            int protocolversionTmp = 0;
            ForgeInfo? forgeInfoTmp = null;
            if (AutoTimeout.Perform(() =>
            {
                try
                {
                    if (Protocol18Handler.DoPing(serverIP, serverPort, ref protocolversionTmp, ref forgeInfoTmp)
                        || Protocol16Handler.DoPing(serverIP, serverPort, ref protocolversionTmp))
                    {
                        success = true;
                    }
                    else
                        ConsoleIO.WriteLineFormatted("§8" + Translations.error_unexpect_response, acceptnewlines: true);
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted(String.Format("§8{0}: {1}", e.GetType().FullName, e.Message));
                }
            }, TimeSpan.FromSeconds(Config.Main.Advanced.ResolveSrvRecords == MainConfigHealper.MainConfig.AdvancedConfig.ResolveSrvRecordType.fast ? 10 : 30)))
            {
                if (protocolversion != 0 && protocolversion != protocolversionTmp)
                    ConsoleIO.WriteLineFormatted("§8" + Translations.error_version_different, acceptnewlines: true);
                if (protocolversion == 0 && protocolversionTmp <= 1)
                    ConsoleIO.WriteLineFormatted("§8" + Translations.error_no_version_report, acceptnewlines: true);
                if (protocolversion == 0)
                    protocolversion = protocolversionTmp;
                forgeInfo = forgeInfoTmp;
                return success;
            }
            else
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.error_connection_timeout, acceptnewlines: true);
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
        public static IMinecraftCom GetProtocolHandler(TcpClient Client, int ProtocolVersion, ForgeInfo? forgeInfo, IMinecraftComHandler Handler)
        {
            int[] supportedVersions_Protocol16 = { 51, 60, 61, 72, 73, 74, 78 };

            if (Array.IndexOf(supportedVersions_Protocol16, ProtocolVersion) > -1)
                return new Protocol16Handler(Client, ProtocolVersion, Handler);

            int[] supportedVersions_Protocol18 = { 4, 5, 47, 107, 108, 109, 110, 210, 315, 316, 335, 338, 340, 393, 401, 404, 477, 480, 485, 490, 498, 573, 575, 578, 735, 736, 751, 753, 754, 755, 756, 757, 758, 759, 760, 761, 762, 763, 764};

            if (Array.IndexOf(supportedVersions_Protocol18, ProtocolVersion) > -1)
                return new Protocol18Handler(Client, ProtocolVersion, Handler, forgeInfo);

            throw new NotSupportedException(string.Format(Translations.exception_version_unsupport, ProtocolVersion));
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
                    case "1.0":
                    case "1.0.0":
                    case "1.0.1":
                        return 22;
                    case "1.1":
                    case "1.1.0":
                        return 23;
                    case "1.2":
                    case "1.2.0":
                    case "1.2.1":
                    case "1.2.2":
                    case "1.2.3":
                        return 28;
                    case "1.2.4":
                    case "1.2.5":
                        return 29;
                    case "1.3":
                    case "1.3.0":
                    case "1.3.1":
                    case "1.3.2":
                        return 39;
                    case "1.4":
                    case "1.4.0":
                    case "1.4.1":
                    case "1.4.2":
                        return 48; // 47 conflicts with 1.8
                    case "1.4.3":
                        return 48;
                    case "1.4.4":
                    case "1.4.5":
                        return 49;
                    case "1.4.6":
                    case "1.4.7":
                        return 51;
                    case "1.5":
                    case "1.5.0":
                    case "1.5.1":
                        return 60;
                    case "1.5.2":
                        return 61;
                    case "1.6":
                    case "1.6.0":
                        return 72;
                    case "1.6.1":
                        return 73;
                    case "1.6.2":
                        return 74;
                    case "1.6.3":
                        return 77;
                    case "1.6.4":
                        return 78;
                    case "1.7":
                    case "1.7.0":
                    case "1.7.1":
                        return 3;
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
                    case "1.13.0":
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
                    case "1.17.0":
                        return 755;
                    case "1.17.1":
                        return 756;
                    case "1.18":
                    case "1.18.0":
                    case "1.18.1":
                        return 757;
                    case "1.18.2":
                        return 758;
                    case "1.19":
                    case "1.19.0":
                        return 759;
                    case "1.19.1":
                    case "1.19.2":
                        return 760;
                    case "1.19.3":
                        return 761;
                    case "1.19.4":
                        return 762;
                    case "1.20":
                    case "1.20.1":
                        return 763;
                    case "1.20.2":
                        return 764;
                    default:
                        return 0;
                }
            }
            else
            {
                try
                {
                    return int.Parse(MCVersion, NumberStyles.Any, CultureInfo.CurrentCulture);
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
            return protocol switch
            {
                22 => "1.0",
                23 => "1.1",
                28 => "1.2.3",
                29 => "1.2.5",
                39 => "1.3.2",
                // case 47: return "1.4.2";
                48 => "1.4.3",
                49 => "1.4.5",
                51 => "1.4.6",
                60 => "1.5.1",
                62 => "1.5.2",
                72 => "1.6",
                73 => "1.6.1",
                3 => "1.7.1",
                4 => "1.7.2",
                5 => "1.7.6",
                47 => "1.8",
                107 => "1.9",
                108 => "1.9.1",
                109 => "1.9.2",
                110 => "1.9.3",
                210 => "1.10",
                315 => "1.11",
                316 => "1.11.1",
                335 => "1.12",
                338 => "1.12.1",
                340 => "1.12.2",
                393 => "1.13",
                401 => "1.13.1",
                404 => "1.13.2",
                477 => "1.14",
                480 => "1.14.1",
                485 => "1.14.2",
                490 => "1.14.3",
                498 => "1.14.4",
                573 => "1.15",
                575 => "1.15.1",
                578 => "1.15.2",
                735 => "1.16",
                736 => "1.16.1",
                751 => "1.16.2",
                753 => "1.16.3",
                754 => "1.16.5",
                755 => "1.17",
                756 => "1.17.1",
                757 => "1.18.1",
                758 => "1.18.2",
                759 => "1.19",
                760 => "1.19.2",
                761 => "1.19.3",
                762 => "1.19.4",
                763 => "1.20",
                764 => "1.20.2",
                _ => "0.0"
            };
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

        public enum LoginResult { OtherError, ServiceUnavailable, SSLError, Success, WrongPassword, AccountMigrated, NotPremium, LoginRequired, InvalidToken, InvalidResponse, NullError, UserCancel, WrongSelection };
        public enum AccountType { Mojang, Microsoft };

        /// <summary>
        /// Allows to login to a premium Minecraft account using the Yggdrasil authentication scheme.
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="pass">Password</param>
        /// <param name="session">In case of successful login, will contain session information for multiplayer</param>
        /// <returns>Returns the status of the login (Success, Failure, etc.)</returns>
        public static LoginResult GetLogin(string user, string pass, LoginType type, out SessionToken session)
        {
            if (type == LoginType.microsoft)
            {
                if (Config.Main.General.Method == LoginMethod.mcc)
                    return MicrosoftMCCLogin(user, pass, out session);
                else
                    return MicrosoftBrowserLogin(out session, user);
            }
            else if (type == LoginType.mojang)
            {
                return MojangLogin(user, pass, out session);
            }
            else if (type == LoginType.yggdrasil)
            {
                return YggdrasiLogin(user, pass, out session);
            }
            else throw new InvalidOperationException("Account type must be Mojang or Microsoft or valid authlib 3rd Servers!");
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
                int code = DoHTTPSPost("authserver.mojang.com",443, "/authenticate", json_request, ref result);
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
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.error_http_code, code));
                    return LoginResult.OtherError;
                }
            }
            catch (System.Security.Authentication.AuthenticationException e)
            {
                if (Settings.Config.Logging.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                }
                return LoginResult.SSLError;
            }
            catch (System.IO.IOException e)
            {
                if (Settings.Config.Logging.DebugMessages)
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
                if (Settings.Config.Logging.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                }
                return LoginResult.OtherError;
            }
        }
    private static LoginResult YggdrasiLogin(string user, string pass, out SessionToken session)
        {
            session = new SessionToken() { ClientID = Guid.NewGuid().ToString().Replace("-", "") };

            try
            {
                string result = "";
                string json_request = "{\"agent\": { \"name\": \"Minecraft\", \"version\": 1 }, \"username\": \"" + JsonEncode(user) + "\", \"password\": \"" + JsonEncode(pass) + "\", \"clientToken\": \"" + JsonEncode(session.ClientID) + "\" }";
                int code = DoHTTPSPost(Config.Main.General.AuthServer.Host,Config.Main.General.AuthServer.Port, "/api/yggdrasil/authserver/authenticate", json_request, ref result);
                if (code == 200)
                {
                    if (result.Contains("availableProfiles\":[]}"))
                    {
                        return LoginResult.NotPremium;
                    }
                    else
                    {
                        Json.JSONData loginResponse = Json.ParseJson(result);
                        if (loginResponse.Properties.ContainsKey("accessToken"))
                        {
                            session.ID = loginResponse.Properties["accessToken"].StringValue;
                            if (loginResponse.Properties.ContainsKey("selectedProfile")
                                && loginResponse.Properties["selectedProfile"].Properties.ContainsKey("id")
                                && loginResponse.Properties["selectedProfile"].Properties.ContainsKey("name"))
                            {
                                session.PlayerID = loginResponse.Properties["selectedProfile"].Properties["id"].StringValue;
                                session.PlayerName = loginResponse.Properties["selectedProfile"].Properties["name"].StringValue;
                                return LoginResult.Success;
                            }
                            else
                            {
                                string availableProfiles = "";
                                foreach (Json.JSONData profile in loginResponse.Properties["availableProfiles"].DataArray)
                                {
                                    availableProfiles += " " + profile.Properties["name"].StringValue;
                                } 
                                ConsoleIO.WriteLine(Translations.mcc_avaliable_profiles + availableProfiles);

                                ConsoleIO.WriteLine(Translations.mcc_select_profile);
                                string selectedProfileName = ConsoleIO.ReadLine();
                                ConsoleIO.WriteLine(Translations.mcc_selected_profile + " " + selectedProfileName);
                                Json.JSONData? selectedProfile = null;
                                foreach (Json.JSONData profile in loginResponse.Properties["availableProfiles"].DataArray)
                                {
                                    selectedProfile = profile.Properties["name"].StringValue == selectedProfileName ? profile : selectedProfile;
                                }

                                if (selectedProfile != null) 
                                {
                                    session.PlayerID = selectedProfile.Properties["id"].StringValue;
                                    session.PlayerName = selectedProfile.Properties["name"].StringValue;
                                    SessionToken currentsession = session;
                                    return GetNewYggdrasilToken(currentsession, out session);
                                }
                                else 
                                {
                                    return LoginResult.WrongSelection;
                                }
                            }
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
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.error_http_code, code));
                    return LoginResult.OtherError;
                }
            }
            catch (System.Security.Authentication.AuthenticationException e)
            {
                if (Settings.Config.Logging.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                }
                return LoginResult.SSLError;
            }
            catch (System.IO.IOException e)
            {
                if (Settings.Config.Logging.DebugMessages)
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
                if (Settings.Config.Logging.DebugMessages)
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
                if (Settings.Config.Logging.DebugMessages)
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
            ConsoleIO.WriteLine(Translations.mcc_browser_open);
            ConsoleIO.WriteLine("\n" + Microsoft.SignInUrl + "\n");

            ConsoleIO.WriteLine(Translations.mcc_browser_login_code);
            string code = ConsoleIO.ReadLine();
            ConsoleIO.WriteLine(string.Format(Translations.mcc_connecting, "Microsoft"));

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
                    InternalConfig.Account.Login = msaResponse.Email;
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
                if (Settings.Config.Logging.DebugMessages)
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
            var expTimestamp = long.Parse(json.Properties["exp"].StringValue, NumberStyles.Any, CultureInfo.CurrentCulture);
            var now = DateTime.Now;
            var tokenExp = UnixTimeStampToDateTime(expTimestamp);
            if (Settings.Config.Logging.DebugMessages)
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
                int code = DoHTTPSPost("authserver.mojang.com",443, "/refresh", json_request, ref result);
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
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.error_auth, code));
                    return LoginResult.OtherError;
                }
            }
            catch
            {
                return LoginResult.OtherError;
            }
        }

        public static LoginResult GetNewYggdrasilToken(SessionToken currentsession, out SessionToken session)
        {
            session = new SessionToken();
            try
            {
                string result = "";
                string json_request = "{ \"accessToken\": \"" + JsonEncode(currentsession.ID) + "\", \"clientToken\": \"" + JsonEncode(currentsession.ClientID) + "\", \"selectedProfile\": { \"id\": \"" + JsonEncode(currentsession.PlayerID) + "\", \"name\": \"" + JsonEncode(currentsession.PlayerName) + "\" } }";
                int code = DoHTTPSPost(Config.Main.General.AuthServer.Host, Config.Main.General.AuthServer.Port, "/api/yggdrasil/authserver/refresh", json_request, ref result);
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
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.error_auth, code));
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
        /// <param name="type">LoginType</param>
        /// <returns>TRUE if session was successfully checked</returns>
        public static bool SessionCheck(string uuid, string accesstoken, string serverhash, LoginType type)
        {
            try
            {
                string result = "";
                string json_request = "{\"accessToken\":\"" + accesstoken + "\",\"selectedProfile\":\"" + uuid + "\",\"serverId\":\"" + serverhash + "\"}";
                string host = type == LoginType.yggdrasil ? Config.Main.General.AuthServer.Host : "sessionserver.mojang.com";
                int port = type == LoginType.yggdrasil ? Config.Main.General.AuthServer.Port : 443;
                string endpoint = type == LoginType.yggdrasil ? "/api/yggdrasil/sessionserver/session/minecraft/join" : "/session/minecraft/join";

                int code = DoHTTPSPost(host, port, endpoint, json_request, ref result);
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
            List<string> realmsWorldsResult = new(); // Store world ID
            try
            {
                string result = "";
                string cookies = String.Format("sid=token:{0}:{1};user={2};version={3}", accesstoken, uuid, username, Program.MCHighestVersion);
                DoHTTPSGet("pc.realms.minecraft.net", 443,"/worlds", cookies, ref result);
                Json.JSONData realmsWorlds = Json.ParseJson(result);
                if (realmsWorlds.Properties.ContainsKey("servers")
                    && realmsWorlds.Properties["servers"].Type == Json.JSONData.DataType.Array
                    && realmsWorlds.Properties["servers"].DataArray.Count > 0)
                {
                    List<string> availableWorlds = new(); // Store string to print
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
                        ConsoleIO.WriteLine(Translations.mcc_realms_available);
                        foreach (var world in availableWorlds)
                            ConsoleIO.WriteLine(world);
                        ConsoleIO.WriteLine(Translations.mcc_realms_join);
                    }
                }

            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.GetType().ToString() + ": " + e.Message);
                if (Settings.Config.Logging.DebugMessages)
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
                int statusCode = DoHTTPSGet("pc.realms.minecraft.net",443, "/worlds/v1/" + worldId + "/join/pc", cookies, ref result);
                if (statusCode == 200)
                {
                    Json.JSONData serverAddress = Json.ParseJson(result);
                    if (serverAddress.Properties.ContainsKey("address"))
                        return serverAddress.Properties["address"].StringValue;
                    else
                    {
                        ConsoleIO.WriteLine(Translations.error_realms_ip_error);
                        return "";
                    }
                }
                else
                {
                    ConsoleIO.WriteLine(Translations.error_realms_access_denied);
                    return "";
                }
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.GetType().ToString() + ": " + e.Message);
                if (Settings.Config.Logging.DebugMessages)
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
        private static int DoHTTPSGet(string host,int port, string endpoint, string cookies, ref string result)
        {
            List<String> http_request = new()
            {
                "GET " + endpoint + " HTTP/1.1",
                "Cookie: " + cookies,
                "Cache-Control: no-cache",
                "Pragma: no-cache",
                "Host: " + host,
                "User-Agent: Java/1.6.0_27",
                "Accept-Charset: ISO-8859-1,UTF-8;q=0.7,*;q=0.7",
                "Connection: close",
                "",
                ""
            };
            return DoHTTPSRequest(http_request, host,port, ref result);
        }

        /// <summary>
        /// Make a HTTPS POST request to the specified endpoint of the Mojang API
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="request">Request payload</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static int DoHTTPSPost(string host, int port, string endpoint, string request, ref string result)
        {
            List<String> http_request = new()
            {
                "POST " + endpoint + " HTTP/1.1",
                "Host: " + host,
                "User-Agent: MCC/" + Program.Version,
                "Content-Type: application/json",
                "Content-Length: " + Encoding.ASCII.GetBytes(request).Length,
                "Connection: close",
                "",
                request
            };
            return DoHTTPSRequest(http_request, host,port, ref result);
        }

        /// <summary>
        /// Manual HTTPS request since we must directly use a TcpClient because of the proxy.
        /// This method connects to the server, enables SSL, do the request and read the response.
        /// </summary>
        /// <param name="headers">Request headers and optional body (POST)</param>
        /// <param name="host">Host to connect to</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
       private static int DoHTTPSRequest(List<string> headers, string host,int port, ref string result)
        {
            string? postResult = null;
            int statusCode = 520;
            Exception? exception = null;
            AutoTimeout.Perform(() =>
            {
                try
                {
                    if (Settings.Config.Logging.DebugMessages)
                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.debug_request, host));

                    TcpClient client = ProxyHandler.NewTcpClient(host, port, true);
                    SslStream stream = new(client.GetStream());
                    stream.AuthenticateAsClient(host, null, SslProtocols.Tls12, true); // Enable TLS 1.2. Hotfix for #1780

                    if (Settings.Config.Logging.DebugMessages)
                        foreach (string line in headers)
                            ConsoleIO.WriteLineFormatted("§8> " + line);

                    stream.Write(Encoding.ASCII.GetBytes(String.Join("\r\n", headers.ToArray())));
                    System.IO.StreamReader sr = new(stream);
                    string raw_result = sr.ReadToEnd();

                    if (Settings.Config.Logging.DebugMessages)
                    {
                        ConsoleIO.WriteLine("");
                        foreach (string line in raw_result.Split('\n'))
                            ConsoleIO.WriteLineFormatted("§8< " + line);
                    }

                    if (raw_result.StartsWith("HTTP/1.1"))
                    {
                        statusCode = int.Parse(raw_result.Split(' ')[1], NumberStyles.Any, CultureInfo.CurrentCulture);
                        if (statusCode != 204) 
                        {
                            postResult = raw_result[(raw_result.IndexOf("\r\n\r\n") + 4)..].Split("\r\n")[1];
                        }
                        else 
                        {
                            postResult = "No Content";
                        }
                    }
                    else statusCode = 520; //Web server is returning an unknown error
                }
                catch (Exception e)
                {
                    if (e is not System.Threading.ThreadAbortException)
                    {
                        exception = e;
                    }
                }
            }, TimeSpan.FromSeconds(30));
            if (postResult != null)
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
            StringBuilder result = new();

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
            DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
