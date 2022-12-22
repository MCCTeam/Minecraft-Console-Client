using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MinecraftClient.Protocol.ProfileKey;
using static MinecraftClient.Settings;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.GeneralConfig;

namespace MinecraftClient.Protocol
{
    static class Microsoft
    {
        private const string clientId = "54473e32-df8f-42e9-a649-9419b0dab9d3";
        private const string tokenUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        private const string signinUrl = $"https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&redirect_uri=https%3A%2F%2Fmccteam.github.io%2Fredirect.html&scope=XboxLive.signin%20offline_access%20openid%20email&prompt=select_account&response_mode=fragment";
        private const string certificates = "https://api.minecraftservices.com/player/certificates";

        public static string SignInUrl { get { return signinUrl; } }

        /// <summary>
        /// Get a sign-in URL with email field pre-filled
        /// </summary>
        /// <param name="loginHint">Login Email</param>
        /// <returns>Sign-in URL with email pre-filled</returns>
        public static string GetSignInUrlWithHint(string loginHint)
        {
            return $"{SignInUrl}&login_hint={Uri.EscapeDataString(loginHint)}";
        }

        /// <summary>
        /// Request access token by auth code
        /// </summary>
        /// <param name="code">Auth code obtained after user signing in</param>
        /// <returns>Access token and refresh token</returns>
        public static async Task<LoginResponse> RequestAccessTokenAsync(HttpClient httpClient, string code)
        {
            FormUrlEncodedContent postData = new(new KeyValuePair<string, string>[]
            {
                new("client_id", clientId),
                new("grant_type", "authorization_code"),
                new("redirect_uri", "https://mccteam.github.io/redirect.html"),
                new("code", code),
            });
            return await RequestTokenAsync(httpClient, postData);
        }

        /// <summary>
        /// Request access token by refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>Access token and new refresh token</returns>
        public static async Task<LoginResponse> RefreshAccessTokenAsync(HttpClient httpClient, string refreshToken)
        {
            FormUrlEncodedContent postData = new(new KeyValuePair<string, string>[]
            {
                new("client_id", clientId),
                new("grant_type", "refresh_token"),
                new("redirect_uri", "https://mccteam.github.io/redirect.html"),
                new("refresh_token", refreshToken),
            });
            return await RequestTokenAsync(httpClient, postData);
        }

        private record TokenInfo
        {
            public string? token_type { init; get; }
            public string? scope { init; get; }
            public int expires_in { init; get; }
            public int ext_expires_in { init; get; }
            public string? access_token { init; get; }
            public string? refresh_token { init; get; }
            public string? id_token { init; get; }
            public string? error { init; get; }
            public string? error_description { init; get; }
        }

        private record JwtPayloadInIdToken
        {
            public string? ver { init; get; }
            public string? iss { init; get; }
            public string? sub { init; get; }
            public string? aud { init; get; }
            public long exp { init; get; }
            public long iat { init; get; }
            public long nbf { init; get; }
            public string? email { init; get; }
            public string? tid { init; get; }
            public string? aio { init; get; }
        }

        /// <summary>
        /// Perform request to obtain access token by code or by refresh token
        /// </summary>
        /// <param name="postData">Complete POST data for the request</param>
        /// <returns></returns>
        private static async Task<LoginResponse> RequestTokenAsync(HttpClient httpClient, FormUrlEncodedContent postData)
        {
            using HttpResponseMessage response = await httpClient.PostAsync(tokenUrl, postData);

            TokenInfo jsonData = (await response.Content.ReadFromJsonAsync<TokenInfo>())!;

            // Error handling
            if (!string.IsNullOrEmpty(jsonData.error))
            {
                throw new Exception(jsonData.error_description);
            }
            else
            {
                // Extract email from JWT
                Stream payload = JwtPayloadDecode.GetPayload(jsonData.id_token!);
                JwtPayloadInIdToken jsonPayload = (await JsonSerializer.DeserializeAsync<JwtPayloadInIdToken>(payload))!;

                return new LoginResponse()
                {
                    Email = jsonPayload.email!,
                    AccessToken = jsonData.access_token!,
                    RefreshToken = jsonData.refresh_token!,
                    ExpiresIn = jsonData.expires_in,
                };
            }
        }

        private record ProfileKeyResult
        {
            public KeyPair? keyPair { init; get; }
            public string? publicKeySignature { init; get; }
            public string? publicKeySignatureV2 { init; get; }
            public DateTime expiresAt { init; get; }
            public DateTime refreshedAfter { init; get; }

            public record KeyPair
            {
                public string? privateKey { init; get; }
                public string? publicKey { init; get; }
            }
        }

        /// <summary>
        /// Request the key to be used for message signing.
        /// </summary>
        /// <param name="accessToken">Access token in session</param>
        /// <returns>Profile key</returns>
        public static async Task<PlayerKeyPair?> RequestProfileKeyAsync(HttpClient httpClient, string accessToken)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Post, certificates);

                request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));

                using HttpResponseMessage response = await httpClient.SendAsync(request);

                if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLine(response.ToString());

                ProfileKeyResult jsonData = (await response.Content.ReadFromJsonAsync<ProfileKeyResult>())!;

                PublicKey publicKey = new(jsonData.keyPair!.publicKey!, jsonData.publicKeySignature, jsonData.publicKeySignatureV2);

                PrivateKey privateKey = new(jsonData.keyPair!.privateKey!);

                return new PlayerKeyPair(publicKey, privateKey, jsonData.expiresAt, jsonData.refreshedAfter);
            }
            catch (HttpRequestException e)
            {
                ConsoleIO.WriteLineFormatted("§cFetch profile key failed: " + e.Message);
                if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§c" + e.StackTrace);
                return null;
            }
        }

        public static void OpenBrowser(string link)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var ps = new ProcessStartInfo(link)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };

                    Process.Start(ps);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", link);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", link);
                }
                else
                {
                    ConsoleIO.WriteLine("Platform not supported, open up the link manually: " + link);
                }
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLine("Cannot open browser\n" + e.Message + "\n" + e.StackTrace);
            }
        }

        public struct LoginResponse
        {
            public string Email;
            public string AccessToken;
            public string RefreshToken;
            public int ExpiresIn;
        }
    }

    static partial class XboxLive
    {
        internal const string UserAgent = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        private const string xsts = "https://xsts.auth.xboxlive.com/xsts/authorize";
        private const string xbl = "https://user.auth.xboxlive.com/user/authenticate";
        private const string authorize = "https://login.live.com/oauth20_authorize.srf?client_id=000000004C12AE6F&redirect_uri=https://login.live.com/oauth20_desktop.srf&scope=service::user.auth.xboxlive.com::MBI_SSL&display=touch&response_type=token&locale=en";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string SignInUrl { get { return authorize; } }

        private record AuthPayload
        {
            public Propertie? Properties { init; get; }
            public string? RelyingParty { init; get; }
            public string? TokenType { init; get; }

            public record Propertie
            {
                public string? AuthMethod { init; get; }
                public string? SiteName { init; get; }
                public string? RpsTicket { init; get; }
                public string? SandboxId { init; get; }
                public string[]? UserTokens { init; get; }
            }
        }

        private record AuthResult
        {
            public DateTime IssueInstant { init; get; }
            public DateTime NotAfter { init; get; }
            public string? Token { init; get; }
            public DisplayClaim? DisplayClaims { init; get; }

            public record DisplayClaim
            {
                public Dictionary<string, string>[]? xui { init; get; }
            }
        }

        private record AuthError
        {
            public string? Identity { init; get; }
            public long XErr { init; get; }
            public string? Message { init; get; }
            public string? Redirect { init; get; }
        }

        /// <summary>
        /// Pre-authentication
        /// </summary>
        /// <remarks>This step is to get the login page for later use</remarks>
        /// <returns></returns>
        public static async Task<PreAuthResponse> PreAuthAsync(HttpClient httpClient)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(authorize);

            string html = await response.Content.ReadAsStringAsync();

            string PPFT = GetPpftRegex().Match(html).Groups[1].Value;

            string urlPost = GetUrlPostRegex().Match(html).Groups[1].Value;

            if (string.IsNullOrEmpty(PPFT) || string.IsNullOrEmpty(urlPost))
            {
                throw new Exception("Fail to extract PPFT or urlPost");
            }

            return new PreAuthResponse()
            {
                UrlPost = urlPost,
                PPFT = PPFT,
                Cookie = new()// response.Cookies
            };
        }

        /// <summary>
        /// Perform login request
        /// </summary>
        /// <remarks>This step is to send the login request by using the PreAuth response</remarks>
        /// <param name="email">Microsoft account email</param>
        /// <param name="password">Account password</param>
        /// <param name="preAuth"></param>
        /// <returns></returns>
        public static async Task<Microsoft.LoginResponse> UserLoginAsync(HttpClient httpClient, string email, string password, PreAuthResponse preAuth)
        {
            FormUrlEncodedContent postData = new(new KeyValuePair<string, string>[]
            {
                new("login", email),
                new("loginfmt", email),
                new("passwd", password),
                new("PPFT", preAuth.PPFT),
            });

            using HttpResponseMessage response = await httpClient.PostAsync(preAuth.UrlPost, postData);

            if (Config.Logging.DebugMessages)
                ConsoleIO.WriteLine(response.ToString());

            if (response.IsSuccessStatusCode)
            {
                string hash = response.RequestMessage!.RequestUri!.Fragment[1..];

                if (string.IsNullOrEmpty(hash))
                    throw new Exception("Cannot extract access token");

                var dict = Request.ParseQueryString(hash);

                return new Microsoft.LoginResponse()
                {
                    Email = email,
                    AccessToken = dict["access_token"],
                    RefreshToken = dict["refresh_token"],
                    ExpiresIn = int.Parse(dict["expires_in"])
                };
            }
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                if (GetTwoFARegex().IsMatch(body))
                {
                    // TODO: Handle 2FA
                    throw new Exception("2FA enabled but not supported yet. Use browser sign-in method or try to disable 2FA in Microsoft account settings");
                }
                else if (GetInvalidAccountRegex().IsMatch(body))
                {
                    throw new Exception("Invalid credentials. Check your credentials");
                }
                else
                {
                    throw new Exception("Unexpected response. Check your credentials. Response code: " + response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Xbox Live Authenticate
        /// </summary>
        /// <param name="loginResponse"></param>
        /// <returns></returns>
        public static async Task<XblAuthenticateResponse> XblAuthenticateAsync(HttpClient httpClient, Microsoft.LoginResponse loginResponse)
        {
            string accessToken;
            if (Config.Main.General.Method == LoginMethod.browser)
            {
                // Our own client ID must have d= in front of the token or HTTP status 400
                // "Stolen" client ID must not have d= in front of the token or HTTP status 400
                accessToken = "d=" + loginResponse.AccessToken;
            }
            else
            {
                accessToken = loginResponse.AccessToken;
            }

            AuthPayload payload = new()
            {
                Properties = new AuthPayload.Propertie()
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = accessToken,
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT",
            };

            using StringContent httpContent = new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

            httpContent.Headers.Add("x-xbl-contract-version", "0");

            using HttpResponseMessage response = await httpClient.PostAsync(xbl, httpContent);

            if (Config.Logging.DebugMessages)
                ConsoleIO.WriteLine(response.ToString());

            if (response.IsSuccessStatusCode)
            {
                AuthResult jsonData = (await response.Content.ReadFromJsonAsync<AuthResult>())!;

                return new XblAuthenticateResponse()
                {
                    Token = jsonData.Token!,
                    UserHash = jsonData.DisplayClaims!.xui![0]["uhs"],
                };
            }
            else
            {
                throw new Exception("XBL Authentication failed, code = " + response.StatusCode.ToString());
            }
        }

        /// <summary>
        /// XSTS Authenticate
        /// </summary>
        /// <remarks>(Don't ask me what is XSTS, I DONT KNOW)</remarks>
        /// <param name="xblResponse"></param>
        /// <returns></returns>
        public static async Task<XSTSAuthenticateResponse> XSTSAuthenticateAsync(HttpClient httpClient, XblAuthenticateResponse xblResponse)
        {
            AuthPayload payload = new()
            {
                Properties = new AuthPayload.Propertie()
                {
                    SandboxId = "RETAIL",
                    UserTokens = new string[] { xblResponse.Token },
                },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT",
            };

            using StringContent httpContent = new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

            httpContent.Headers.Add("x-xbl-contract-version", "1");

            using HttpResponseMessage response = await httpClient.PostAsync(xsts, httpContent);

            if (Config.Logging.DebugMessages)
                ConsoleIO.WriteLine(response.ToString());

            if (response.IsSuccessStatusCode)
            {
                AuthResult jsonData = (await response.Content.ReadFromJsonAsync<AuthResult>())!;

                return new XSTSAuthenticateResponse()
                {
                    Token = jsonData.Token!,
                    UserHash = jsonData.DisplayClaims!.xui![0]["uhs"],
                };
            }
            else
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    AuthError jsonData = (await response.Content.ReadFromJsonAsync<AuthError>())!;
                    if (jsonData.XErr == 2148916233)
                        throw new Exception("The account doesn't have an Xbox account");
                    else if (jsonData.XErr == 2148916235)
                        throw new Exception("The account is from a country where Xbox Live is not available/banned");
                    else if (jsonData.XErr == 2148916236 || jsonData.XErr == 2148916237)
                        throw new Exception("The account needs adult verification on Xbox page. (South Korea)");
                    else if (jsonData.XErr == 2148916238)
                        throw new Exception("The account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult");
                    else
                        throw new Exception("Unknown XSTS error code: " + jsonData.XErr.ToString() + ", Check " + jsonData.Redirect);
                }
                else
                {
                    throw new Exception("XSTS Authentication failed");
                }
            }
        }

        public struct PreAuthResponse
        {
            public string UrlPost;
            public string PPFT;
            public NameValueCollection Cookie;
        }

        public struct XblAuthenticateResponse
        {
            public string Token;
            public string UserHash;
        }

        public struct XSTSAuthenticateResponse
        {
            public string Token;
            public string UserHash;
        }

        [GeneratedRegex("sFTTag:'.*value=\"(.*)\"\\/>'")]
        private static partial Regex GetPpftRegex();

        [GeneratedRegex("urlPost:'(.+?(?='))")]
        private static partial Regex GetUrlPostRegex();

        [GeneratedRegex("identity\\/confirm")]
        private static partial Regex GetConfirmRegex();

        [GeneratedRegex("Sign in to", RegexOptions.IgnoreCase, "zh-CN")]
        private static partial Regex GetInvalidAccountRegex();

        [GeneratedRegex("Help us protect your account", RegexOptions.IgnoreCase, "zh-CN")]
        private static partial Regex GetTwoFARegex();
    }

    static class MinecraftWithXbox
    {
        private const string profile = "https://api.minecraftservices.com/minecraft/profile";
        private const string ownership = "https://api.minecraftservices.com/entitlements/mcstore";
        private const string loginWithXbox = "https://api.minecraftservices.com/authentication/login_with_xbox";

        private record LoginPayload
        {
            public string? identityToken { init; get; }
        }

        private record LoginResult
        {
            public string? username { init; get; }
            public string[]? roles { init; get; }
            public string? access_token { init; get; }
            public string? token_type { init; get; }
            public int expires_in { init; get; }
        }

        private record GameOwnershipResult
        {
            public Dictionary<string, string>[]? items { init; get; }
            public string? signature { init; get; }
            public string? keyId { init; get; }
        }

        private record GameProfileResult
        {
            public string? id { init; get; }
            public string? name { init; get; }
            public Dictionary<string, string>[]? skins { init; get; }
            public Dictionary<string, string>[]? capes { init; get; }
            /* Error */
            public string? path { init; get; }
            public string? errorType { init; get; }
            public string? error { init; get; }
            public string? errorMessage { init; get; }
            public string? developerMessage { init; get; }
        }

        /// <summary>
        /// Login to Minecraft using the XSTS token and user hash obtained before
        /// </summary>
        /// <param name="userHash"></param>
        /// <param name="xstsToken"></param>
        /// <returns></returns>
        public static async Task<string> LoginWithXboxAsync(HttpClient httpClient, string userHash, string xstsToken)
        {
            LoginPayload payload = new()
            {
                identityToken = $"XBL3.0 x={userHash};{xstsToken}",
            };

            using StringContent httpContent = new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.PostAsync(loginWithXbox, httpContent);

            if (Config.Logging.DebugMessages)
                ConsoleIO.WriteLine(response.ToString());

            LoginResult jsonData = (await response.Content.ReadFromJsonAsync<LoginResult>())!;

            return jsonData.access_token!;
        }

        /// <summary>
        /// Check if user own Minecraft by access token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns>True if the user own the game</returns>
        public static async Task<bool> CheckUserHasGameAsync(HttpClient httpClient, string accessToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, ownership);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));

            using HttpResponseMessage response = await httpClient.SendAsync(request);

            if (Config.Logging.DebugMessages)
                ConsoleIO.WriteLine(response.ToString());

            GameOwnershipResult jsonData = (await response.Content.ReadFromJsonAsync<GameOwnershipResult>())!;

            return jsonData.items!.Length > 0;
        }

        public static async Task<UserProfile> GetUserProfileAsync(HttpClient httpClient, string accessToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, profile);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));

            using HttpResponseMessage response = await httpClient.SendAsync(request);

            if (Config.Logging.DebugMessages)
                ConsoleIO.WriteLine(response.ToString());

            GameProfileResult jsonData = (await response.Content.ReadFromJsonAsync<GameProfileResult>())!;

            if (!string.IsNullOrEmpty(jsonData.error))
                throw new Exception($"{jsonData.errorType}: {jsonData.error}. {jsonData.errorMessage}");

            return new UserProfile()
            {
                UUID = jsonData.id!,
                UserName = jsonData.name!,
            };
        }

        public struct UserProfile
        {
            public string UUID;
            public string UserName;
        }
    }

    /// <summary>
    /// Helper class
    /// </summary>
    static class Request
    {
        static public Dictionary<string, string> ParseQueryString(string query)
        {
            return query.Split('&')
                .ToDictionary(c => c.Split('=')[0],
                              c => Uri.UnescapeDataString(c.Split('=')[1]));
        }
    }
}
