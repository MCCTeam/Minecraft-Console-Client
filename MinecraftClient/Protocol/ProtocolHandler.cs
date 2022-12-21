using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Description;
using DnsClient;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using PInvoke;
using static MinecraftClient.Json;
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
        public static async Task<Tuple<bool, string, ushort>> MinecraftServiceLookupAsync(string domain)
        {
            if (!string.IsNullOrEmpty(domain) && domain.Any(char.IsLetter))
            {
                CancellationTokenSource cancelToken = new(1000 *
                    (Config.Main.Advanced.ResolveSrvRecords == MainConfigHealper.MainConfig.AdvancedConfig.ResolveSrvRecordType.fast ? 10 : 30));
                try
                {
                    ConsoleIO.WriteLine(string.Format(Translations.mcc_resolve, domain));
                    var lookupClient = new LookupClient();
                    var response = await lookupClient.QueryAsync($"_minecraft._tcp.{domain}", QueryType.SRV, cancellationToken: cancelToken.Token);
                    if (!cancelToken.IsCancellationRequested && !response.HasError && response.Answers.SrvRecords().Any())
                    {
                        //Order SRV records by priority and weight, then randomly
                        var result = response.Answers.SrvRecords()
                            .OrderBy(record => record.Priority)
                            .ThenByDescending(record => record.Weight)
                            .ThenBy(record => Guid.NewGuid())
                            .First();
                        string target = result.Target.Value.Trim('.');
                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_found, target, result.Port, domain));
                        return new(true, target, result.Port);
                    }
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_not_found, domain, e.GetType().FullName, e.Message));
                }
            }
            return new(false, string.Empty, 25565);
        }

        /// <summary>
        /// Retrieve information about a Minecraft server
        /// </summary>
        /// <param name="serverIP">Server IP to ping</param>
        /// <param name="serverPort">Server Port to ping</param>
        /// <param name="protocolversion">Will contain protocol version, if ping successful</param>
        /// <returns>TRUE if ping was successful</returns>
        public static async Task<Tuple<bool, int, ForgeInfo?>> GetServerInfoAsync(string serverIP, ushort serverPort, int protocolversion)
        {
            Tuple<bool, int, ForgeInfo?>? result = null;

            CancellationTokenSource cancelTokenSource = new(1000 *
                (Config.Main.Advanced.ResolveSrvRecords == MainConfigHealper.MainConfig.AdvancedConfig.ResolveSrvRecordType.fast ? 10 : 30));
            try
            {
                result = await Protocol18Handler.DoPing(serverIP, serverPort, cancelTokenSource.Token);
                if (!result.Item1)
                    result = await Protocol16Handler.DoPing(serverIP, serverPort, cancelTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.error_connection_timeout, acceptnewlines: true);
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted(string.Format("§8{0}: {1}", e.GetType().FullName, e.Message));
            }

            if (result != null)
            {
                if (!result.Item1)
                    ConsoleIO.WriteLineFormatted("§8" + Translations.error_unexpect_response, acceptnewlines: true);

                if (protocolversion != 0 && protocolversion != result.Item2)
                {
                    ConsoleIO.WriteLineFormatted("§8" + Translations.error_version_different, acceptnewlines: true);
                    return new(true, protocolversion, result.Item3);
                }
                else
                {
                    if (result.Item2 <= 1)
                        ConsoleIO.WriteLineFormatted("§8" + Translations.error_no_version_report, acceptnewlines: true);
                    return result;
                }
            }

            return new(false, 0, null);
        }

        /// <summary>
        /// Get a protocol handler for the specified Minecraft version
        /// </summary>
        /// <param name="Client">Tcp Client connected to the server</param>
        /// <param name="ProtocolVersion">Protocol version to handle</param>
        /// <param name="Handler">Handler with the appropriate callbacks</param>
        /// <returns></returns>
        public static IMinecraftCom GetProtocolHandler(CancellationToken cancelToken, TcpClient Client, int ProtocolVersion, ForgeInfo? forgeInfo, IMinecraftComHandler Handler)
        {
            int[] supportedVersions_Protocol16 = { 51, 60, 61, 72, 73, 74, 78 };

            if (Array.IndexOf(supportedVersions_Protocol16, ProtocolVersion) > -1)
                return new Protocol16Handler(cancelToken, Client, ProtocolVersion, Handler);

            int[] supportedVersions_Protocol18 = { 4, 5, 47, 107, 108, 109, 110, 210, 315, 316, 335, 338, 340, 393, 401, 404, 477, 480, 485, 490, 498, 573, 575, 578, 735, 736, 751, 753, 754, 755, 756, 757, 758, 759, 760 };

            if (Array.IndexOf(supportedVersions_Protocol18, ProtocolVersion) > -1)
                return new Protocol18Handler(Client, ProtocolVersion, Handler, forgeInfo, cancelToken);

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
                    default:
                        return 0;
                }
            }
            else
            {
                if (int.TryParse(MCVersion, NumberStyles.Any, CultureInfo.CurrentCulture, out int versionId))
                    return versionId;
                else
                    return 0;
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
                // 47 => "1.4.2", // 47 conflicts with 1.8
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
                _ => "0.0",
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

        public enum LoginResult { OtherError, ServiceUnavailable, SSLError, Success, WrongPassword, AccountMigrated, NotPremium, LoginRequired, InvalidToken, InvalidResponse, NullError, UserCancel };
        public enum AccountType { Mojang, Microsoft };

        /// <summary>
        /// Allows to login to a premium Minecraft account using the Yggdrasil authentication scheme.
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="pass">Password</param>
        /// <param name="session">In case of successful login, will contain session information for multiplayer</param>
        /// <returns>Returns the status of the login (Success, Failure, etc.)</returns>
        public static async Task<Tuple<LoginResult, SessionToken?>> GetLoginAsync(HttpClient httpClient, string user, string pass, LoginType type)
        {
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            if (type == LoginType.microsoft)
            {
                if (Config.Main.General.Method == LoginMethod.mcc)
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"MCC/{Program.Version}");
                    return await MicrosoftMCCLoginAsync(httpClient, user, pass);
                }
                else
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(XboxLive.UserAgent);
                    return await MicrosoftBrowserLoginAsync(httpClient, user);
                }
            }
            else if (type == LoginType.mojang)
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"MCC/{Program.Version}");
                return await MojangLoginAsync(user, pass);
            }
            else
            {
                throw new InvalidOperationException("Account type must be Mojang or Microsoft");
            }
        }

        /// <summary>
        /// Login using Mojang account. Will be outdated after account migration
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private static async Task<Tuple<LoginResult, SessionToken?>> MojangLoginAsync(string user, string pass)
        {
            try
            {
                string clientID = Guid.NewGuid().ToString().Replace("-", "");
                string json_request = $"{{\"agent\": {{ \"name\": \"Minecraft\", \"version\": 1 }}, \"username\": \"{JsonEncode(user)}\", \"password\": \"{JsonEncode(pass)}\", \"clientToken\": \"{JsonEncode(clientID)}\" }}";
                (int code, string result) = await DoHTTPSPost("authserver.mojang.com", "/authenticate", json_request);
                if (code == 200)
                {
                    if (result.Contains("availableProfiles\":[]}"))
                    {
                        return new(LoginResult.NotPremium, null);
                    }
                    else
                    {
                        Json.JSONData loginResponse = Json.ParseJson(result);
                        if (loginResponse.Properties.TryGetValue("accessToken", out Json.JSONData? accessToken)
                            && loginResponse.Properties.TryGetValue("selectedProfile", out Json.JSONData? selectedProfile)
                            && selectedProfile.Properties.TryGetValue("id", out Json.JSONData? selectedProfileId)
                            && selectedProfile.Properties.TryGetValue("name", out Json.JSONData? selectedProfileName))
                        {
                            SessionToken session = new()
                            {
                                ClientID = clientID,
                                ID = accessToken.StringValue,
                                PlayerID = selectedProfileId.StringValue,
                                PlayerName = selectedProfileName.StringValue
                            };
                            return new(LoginResult.Success, session);
                        }
                        else
                            return new(LoginResult.InvalidResponse, null);
                    }
                }
                else if (code == 403)
                {
                    if (result.Contains("UserMigratedException"))
                        return new(LoginResult.AccountMigrated, null);
                    else
                        return new(LoginResult.WrongPassword, null);
                }
                else if (code == 503)
                {
                    return new(LoginResult.ServiceUnavailable, null);
                }
                else
                {
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.error_http_code, code));
                    return new(LoginResult.OtherError, null);
                }
            }
            catch (AuthenticationException e)
            {
                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                return new(LoginResult.SSLError, null);
            }
            catch (System.IO.IOException e)
            {
                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                if (e.Message.Contains("authentication"))
                    return new(LoginResult.SSLError, null);
                else
                    return new(LoginResult.OtherError, null);
            }
            catch (Exception e)
            {
                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8" + e.ToString());
                return new(LoginResult.OtherError, null);
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
        private static async Task<Tuple<LoginResult, SessionToken?>> MicrosoftMCCLoginAsync(HttpClient httpClient, string email, string password)
        {
            try
            {
                var msaResponse = await XboxLive.UserLoginAsync(httpClient, email, password, await XboxLive.PreAuthAsync(httpClient));
                // Remove refresh token for MCC sign method
                msaResponse.RefreshToken = string.Empty;
                return await MicrosoftLoginAsync(httpClient, msaResponse);
            }
            catch (Exception e)
            {
                SessionToken session = new() { ClientID = Guid.NewGuid().ToString().Replace("-", "") };
                ConsoleIO.WriteLineFormatted("§cMicrosoft authenticate failed: " + e.Message);
                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§c" + e.StackTrace);
                return new(LoginResult.WrongPassword, session); // Might not always be wrong password
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
        public static async Task<Tuple<LoginResult, SessionToken?>> MicrosoftBrowserLoginAsync(HttpClient httpClient, string? loginHint = null)
        {
            string link = string.IsNullOrEmpty(loginHint) ? Microsoft.SignInUrl : Microsoft.GetSignInUrlWithHint(loginHint);

            Microsoft.OpenBrowser(link);

            ConsoleIO.SuppressPrinting(true);
            ConsoleIO.WriteLine(Translations.mcc_browser_open, ignoreSuppress: true);
            ConsoleIO.WriteLine($"\n{link}\n", ignoreSuppress: true);

            ConsoleIO.WriteLine(Translations.mcc_browser_login_code, ignoreSuppress: true);
            string code = ConsoleIO.ReadLine();
            ConsoleIO.WriteLine(string.Format(Translations.mcc_connecting, "Microsoft"), ignoreSuppress: true);
            ConsoleIO.SuppressPrinting(false);

            var msaResponse = await Microsoft.RequestAccessTokenAsync(httpClient, code);
            return await MicrosoftLoginAsync(httpClient, msaResponse);
        }

        public static async Task<Tuple<LoginResult, SessionToken?>> MicrosoftLoginRefreshAsync(HttpClient httpClient, string refreshToken)
        {
            var msaResponse = await Microsoft.RefreshAccessTokenAsync(httpClient, refreshToken);
            return await MicrosoftLoginAsync(httpClient, msaResponse);
        }

        private static async Task<Tuple<LoginResult, SessionToken?>> MicrosoftLoginAsync(HttpClient httpClient, Microsoft.LoginResponse msaResponse)
        {
            try
            {
                var xblResponse = await XboxLive.XblAuthenticateAsync(httpClient, msaResponse);
                var xsts = await XboxLive.XSTSAuthenticateAsync(httpClient, xblResponse); // Might throw even password correct

                string accessToken = await MinecraftWithXbox.LoginWithXboxAsync(httpClient, xsts.UserHash, xsts.Token);
                bool hasGame = await MinecraftWithXbox.CheckUserHasGameAsync(httpClient, accessToken);
                if (hasGame)
                {
                    var profile = await MinecraftWithXbox.GetUserProfileAsync(httpClient, accessToken);
                    SessionToken session = new()
                    {
                        ClientID = Guid.NewGuid().ToString().Replace("-", ""),
                        PlayerName = profile.UserName,
                        PlayerID = profile.UUID,
                        ID = accessToken,
                        RefreshToken = msaResponse.RefreshToken
                    };
                    InternalConfig.Account.Login = msaResponse.Email;
                    return new(LoginResult.Success, session);
                }
                else
                {
                    return new(LoginResult.NotPremium, null);
                }
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§cMicrosoft authenticate failed: " + e.Message);
                if (Config.Logging.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§c" + e.StackTrace);
                }
                return new(LoginResult.WrongPassword, null); // Might not always be wrong password
            }
        }

        private record JwtPayloadInSessionId
        {
            public string? xuid { init; get; }
            public string? agg { init; get; }
            public string? sub { init; get; }
            public long nbf { init; get; }
            public string? auth { init; get; }
            public string[]? roles { init; get; }
            public string? iss { init; get; }
            public long exp { init; get; }
            public long iat { init; get; }
            public string? platform { init; get; }
            public string? yuid { init; get; }
        }

        /// <summary>
        /// Validates whether accessToken must be refreshed
        /// </summary>
        /// <param name="session">Session token to validate</param>
        /// <returns>Returns the status of the token (Valid, Invalid, etc.)</returns>
        public static async Task<LoginResult> GetTokenValidation(SessionToken session)
        {
            try
            {
                Stream payload = JwtPayloadDecode.GetPayload(session.ID);
                JwtPayloadInSessionId jsonPayload = (await JsonSerializer.DeserializeAsync<JwtPayloadInSessionId>(payload))!;

                var now = DateTime.Now.AddMinutes(1);
                var tokenExp = UnixTimeStampToDateTime(jsonPayload.exp);

                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLine("Access token expiration time is " + tokenExp.ToString());

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
            catch (JsonException) { }
            catch (FormatException) { }
            catch (ArgumentException) { }
            catch (IndexOutOfRangeException) { }
            return LoginResult.LoginRequired;
        }

        /// <summary>
        /// Refreshes invalid token
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="session">In case of successful token refresh, will contain session information for multiplayer</param>
        /// <returns>Returns the status of the new token request (Success, Failure, etc.)</returns>
        public static async Task<Tuple<LoginResult, SessionToken?>> GetNewToken(SessionToken currentsession)
        {
            try
            {
                string json_request = $"{{ \"accessToken\": \"{JsonEncode(currentsession.ID)}\", \"clientToken\": \"{JsonEncode(currentsession.ClientID)}\", \"selectedProfile\": {{ \"id\": \"{JsonEncode(currentsession.PlayerID)}\", \"name\": \"{JsonEncode(currentsession.PlayerName)}\" }} }}";
                (int code, string result) = await DoHTTPSPost("authserver.mojang.com", "/refresh", json_request);
                if (code == 200)
                {
                    if (result == null)
                    {
                        return new(LoginResult.NullError, null);
                    }
                    else
                    {
                        Json.JSONData loginResponse = Json.ParseJson(result);
                        if (loginResponse.Properties.TryGetValue("accessToken", out Json.JSONData? accessToken)
                            && loginResponse.Properties.TryGetValue("selectedProfile", out Json.JSONData? selectedProfile)
                            && selectedProfile.Properties.TryGetValue("id", out Json.JSONData? selectedProfileId)
                            && selectedProfile.Properties.TryGetValue("name", out Json.JSONData? selectedProfileName))
                        {

                            SessionToken session = new()
                            {
                                ID = accessToken.StringValue,
                                PlayerID = selectedProfileId.StringValue,
                                PlayerName = selectedProfileName.StringValue
                            };
                            return new(LoginResult.Success, session);
                        }
                        else
                            return new(LoginResult.InvalidResponse, null);
                    }
                }
                else if (code == 403 && result.Contains("InvalidToken"))
                {
                    return new(LoginResult.InvalidToken, null);
                }
                else
                {
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.error_auth, code));
                    return new(LoginResult.OtherError, null);
                }
            }
            catch
            {
                return new(LoginResult.OtherError, null);
            }
        }

        private record SessionCheckPayload
        {
            public string? accessToken { init; get; }
            public string? selectedProfile { init; get; }
            public string? serverId { init; get; }
        }

        private record SessionCheckFailResult
        {
            public string? error { init; get; }
            public string? path { init; get; }
        }

        /// <summary>
        /// Check session using Mojang's Yggdrasil authentication scheme. Allows to join an online-mode server
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="accesstoken">Session ID</param>
        /// <param name="serverhash">Server ID</param>
        /// <returns>TRUE if session was successfully checked</returns>
        public static async Task<Tuple<bool, string?>> SessionCheckAsync(HttpClient httpClient, string uuid, string accesstoken, string serverhash)
        {
            SessionCheckPayload payload = new()
            {
                accessToken = accesstoken,
                selectedProfile = uuid,
                serverId = serverhash,
            };

            try
            {
                using HttpRequestMessage request = new(HttpMethod.Post, "https://sessionserver.mojang.com/session/minecraft/join");

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                request.Headers.UserAgent.Clear();
                request.Headers.UserAgent.ParseAdd($"MCC/{Program.Version}");

                using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                    return new(true, null);
                else
                {
                    SessionCheckFailResult jsonData = (await response.Content.ReadFromJsonAsync<SessionCheckFailResult>())!;
                    return new(false, jsonData.error);
                }
            }
            catch (HttpRequestException e)
            {
                return new(false, $"HttpRequestException: {e.Message}");
            }
            catch (JsonException e)
            {
                return new(false, $"JsonException: {e.Message}");
            }
        }

        /// <summary>
        /// Retrieve available Realms worlds of a player and display them
        /// </summary>
        /// <param name="username">Player Minecraft username</param>
        /// <param name="uuid">Player UUID</param>
        /// <param name="accesstoken">Access token</param>
        /// <returns>List of ID of available Realms worlds</returns>
        public static async Task<List<string>> RealmsListWorldsAsync(HttpClient httpClient, string username, string uuid, string accesstoken)
        {
            List<string> realmsWorldsResult = new(); // Store world ID
            try
            {
                string cookies = String.Format("sid=token:{0}:{1};user={2};version={3}", accesstoken, uuid, username, Program.MCHighestVersion);
                (_, string result) = await DoHTTPSGet("pc.realms.minecraft.net", "/worlds", cookies);
                Json.JSONData realmsWorlds = Json.ParseJson(result);
                if (realmsWorlds.Properties.TryGetValue("servers", out Json.JSONData? servers)
                    && servers.Type == Json.JSONData.DataType.Array
                    && servers.DataArray.Count > 0)
                {
                    List<string> availableWorlds = new(); // Store string to print
                    int index = 0;
                    foreach (Json.JSONData realmsServer in servers.DataArray)
                    {
                        if (realmsServer.Properties.TryGetValue("name", out Json.JSONData? name)
                            && realmsServer.Properties.TryGetValue("owner", out Json.JSONData? owner)
                            && realmsServer.Properties.TryGetValue("id", out Json.JSONData? id)
                            && realmsServer.Properties.TryGetValue("expired", out Json.JSONData? expired)
                            && expired.StringValue == "false")
                        {
                            availableWorlds.Add($"[{index++}] {name.StringValue} ({owner.StringValue}) - {id.StringValue}");
                            realmsWorldsResult.Add(id.StringValue);
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
        public static async Task<string> GetRealmsWorldServerAddress(HttpClient httpClient, string worldId, string username, string uuid, string accesstoken)
        {
            try
            {
                string cookies = String.Format("sid=token:{0}:{1};user={2};version={3}", accesstoken, uuid, username, Program.MCHighestVersion);
                (int statusCode, string result) = await DoHTTPSGet("pc.realms.minecraft.net", $"/worlds/v1/{worldId}/join/pc", cookies);
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
        private static async Task<Tuple<int, string>> DoHTTPSGet(string host, string endpoint, string cookies)
        {
            List<string> http_request = new()
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
            return await DoHTTPSRequest(http_request, host);
        }

        /// <summary>
        /// Make a HTTPS POST request to the specified endpoint of the Mojang API
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="request">Request payload</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static async Task<Tuple<int, string>> DoHTTPSPost(string host, string endpoint, string request)
        {
            List<string> http_request = new()
            {
                $"POST {endpoint} HTTP/1.1",
                $"Host: {host}",
                $"User-Agent: MCC/{Program.Version}",
                "Content-Type: application/json",
                $"Content-Length: {Encoding.ASCII.GetBytes(request).Length}",
                "Connection: close",
                "",
                request
            };
            return await DoHTTPSRequest(http_request, host);
        }

        /// <summary>
        /// Manual HTTPS request since we must directly use a TcpClient because of the proxy.
        /// This method connects to the server, enables SSL, do the request and read the response.
        /// </summary>
        /// <param name="headers">Request headers and optional body (POST)</param>
        /// <param name="host">Host to connect to</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static async Task<Tuple<int, string>> DoHTTPSRequest(List<string> headers, string host)
        {
            string postResult = string.Empty;

            var cancelToken = new CancellationTokenSource(30 * 1000).Token;

            if (Config.Logging.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.debug_request, host));

            TcpClient client = ProxyHandler.NewTcpClient(host, 443, ProxyHandler.ClientType.Login);
            SslStream stream = new(client.GetStream());

            SslClientAuthenticationOptions sslOptions = new() // Enable TLS 1.2. Hotfix for #1780
            {
                TargetHost = host,
                ClientCertificates = null,
                EnabledSslProtocols = SslProtocols.Tls12,
                CertificateRevocationCheckMode = X509RevocationMode.Online,
                EncryptionPolicy = EncryptionPolicy.RequireEncryption,
            };
            Task sslAuth = stream.AuthenticateAsClientAsync(sslOptions, cancelToken);
            if (cancelToken.IsCancellationRequested)
                throw new TimeoutException(string.Format(Translations.mcc_network_timeout, host));

            if (Config.Logging.DebugMessages)
                foreach (string line in headers)
                    ConsoleIO.WriteLineFormatted("§8> " + line);

            await sslAuth;
            await stream.WriteAsync(Encoding.ASCII.GetBytes(string.Join("\r\n", headers.ToArray())), cancelToken);
            if (cancelToken.IsCancellationRequested)
                throw new TimeoutException(string.Format(Translations.mcc_network_timeout, host));

            using System.IO.StreamReader sr = new(stream);
            string raw_result = await sr.ReadToEndAsync(cancelToken);
            sr.Dispose();
            if (cancelToken.IsCancellationRequested)
                throw new TimeoutException(string.Format(Translations.mcc_network_timeout, host));

            if (Config.Logging.DebugMessages)
            {
                ConsoleIO.WriteLine(string.Empty);
                foreach (string line in raw_result.Split('\n'))
                    ConsoleIO.WriteLineFormatted("§8< " + line);
            }

            int statusCode;
            if (raw_result.StartsWith("HTTP/1.1"))
            {
                postResult = raw_result[(raw_result.IndexOf("\r\n\r\n") + 4)..];
                statusCode = int.Parse(raw_result.Split(' ')[1], NumberStyles.Any, CultureInfo.CurrentCulture);
            }
            else
                statusCode = 520; //Web server is returning an unknown error

            return new(statusCode, postResult);
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
