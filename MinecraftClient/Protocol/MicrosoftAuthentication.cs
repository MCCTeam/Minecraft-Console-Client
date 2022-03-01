using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Diagnostics;

namespace MinecraftClient.Protocol
{
    static class Microsoft
    {
        private static readonly string clientId = "54473e32-df8f-42e9-a649-9419b0dab9d3";
        private static readonly string signinUrl = string.Format("https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id={0}&response_type=code&redirect_uri=https%3A%2F%2Fmccteam.github.io%2Fredirect.html&scope=XboxLive.signin%20offline_access%20openid%20email&prompt=select_account&response_mode=fragment", clientId);
        private static readonly string tokenUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";

        public static string SignInUrl { get { return signinUrl; } }

        /// <summary>
        /// Get a sign-in URL with email field pre-filled
        /// </summary>
        /// <param name="loginHint">Login Email</param>
        /// <returns>Sign-in URL with email pre-filled</returns>
        public static string GetSignInUrlWithHint(string loginHint)
        {
            return SignInUrl + "&login_hint=" + Uri.EscapeDataString(loginHint);
        }

        /// <summary>
        /// Request access token by auth code
        /// </summary>
        /// <param name="code">Auth code obtained after user signing in</param>
        /// <returns>Access token and refresh token</returns>
        public static LoginResponse RequestAccessToken(string code)
        {
            string postData = "client_id={0}&grant_type=authorization_code&redirect_uri=https%3A%2F%2Fmccteam.github.io%2Fredirect.html&code={1}";
            postData = string.Format(postData, clientId, code);
            return RequestToken(postData);
        }

        /// <summary>
        /// Request access token by refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>Access token and new refresh token</returns>
        public static LoginResponse RefreshAccessToken(string refreshToken)
        {
            string postData = "client_id={0}&grant_type=refresh_token&redirect_uri=https%3A%2F%2Fmccteam.github.io%2Fredirect.html&refresh_token={1}";
            postData = string.Format(postData, clientId, refreshToken);
            return RequestToken(postData);
        }

        /// <summary>
        /// Perform request to obtain access token by code or by refresh token 
        /// </summary>
        /// <param name="postData">Complete POST data for the request</param>
        /// <returns></returns>
        private static LoginResponse RequestToken(string postData)
        {
            var request = new ProxiedWebRequest(tokenUrl);
            request.UserAgent = "MCC/" + Program.Version;
            var response = request.Post("application/x-www-form-urlencoded", postData);
            var jsonData = Json.ParseJson(response.Body);

            // Error handling
            if (jsonData.Properties.ContainsKey("error"))
            {
                throw new Exception(jsonData.Properties["error_description"].StringValue);
            }
            else
            {
                string accessToken = jsonData.Properties["access_token"].StringValue;
                string refreshToken = jsonData.Properties["refresh_token"].StringValue;
                int expiresIn = int.Parse(jsonData.Properties["expires_in"].StringValue);
                
                // Extract email from JWT
                string payload = JwtPayloadDecode.GetPayload(jsonData.Properties["id_token"].StringValue);
                var jsonPayload = Json.ParseJson(payload);
                string email = jsonPayload.Properties["email"].StringValue;
                return new LoginResponse()
                {
                    Email = email,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = expiresIn
                };
            }
        }

        public static void OpenBrowser(string link)
        {
            try
            {
                Process.Start(link);
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

    static class XboxLive
    {
        private static string authorize = "https://login.live.com/oauth20_authorize.srf?client_id=000000004C12AE6F&redirect_uri=https://login.live.com/oauth20_desktop.srf&scope=service::user.auth.xboxlive.com::MBI_SSL&display=touch&response_type=token&locale=en";
        private static string xbl = "https://user.auth.xboxlive.com/user/authenticate";
        private static string xsts = "https://xsts.auth.xboxlive.com/xsts/authorize";

        private static string userAgent = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        private static Regex ppft = new Regex("sFTTag:'.*value=\"(.*)\"\\/>'");
        private static Regex urlPost = new Regex("urlPost:'(.+?(?=\'))");
        private static Regex confirm = new Regex("identity\\/confirm");
        private static Regex invalidAccount = new Regex("Sign in to", RegexOptions.IgnoreCase);
        private static Regex twoFA = new Regex("Help us protect your account", RegexOptions.IgnoreCase);

        public static string SignInUrl { get { return authorize; } }

        /// <summary>
        /// Pre-authentication
        /// </summary>
        /// <remarks>This step is to get the login page for later use</remarks>
        /// <returns></returns>
        public static PreAuthResponse PreAuth()
        {
            var request = new ProxiedWebRequest(authorize);
            request.UserAgent = userAgent;
            var response = request.Get();

            string html = response.Body;

            string PPFT = ppft.Match(html).Groups[1].Value;
            string urlPost = XboxLive.urlPost.Match(html).Groups[1].Value;

            if (string.IsNullOrEmpty(PPFT) || string.IsNullOrEmpty(urlPost))
            {
                throw new Exception("Fail to extract PPFT or urlPost");
            }
            //Console.WriteLine("PPFT: {0}", PPFT);
            //Console.WriteLine();
            //Console.WriteLine("urlPost: {0}", urlPost);

            return new PreAuthResponse()
            {
                UrlPost = urlPost,
                PPFT = PPFT,
                Cookie = response.Cookies
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
        public static Microsoft.LoginResponse UserLogin(string email, string password, PreAuthResponse preAuth)
        {
            var request = new ProxiedWebRequest(preAuth.UrlPost, preAuth.Cookie);
            request.UserAgent = userAgent;

            string postData = "login=" + Uri.EscapeDataString(email)
                 + "&loginfmt=" + Uri.EscapeDataString(email)
                 + "&passwd=" + Uri.EscapeDataString(password)
                 + "&PPFT=" + Uri.EscapeDataString(preAuth.PPFT);

            var response = request.Post("application/x-www-form-urlencoded", postData);

            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }

            if (response.StatusCode >= 300 && response.StatusCode <= 399)
            {
                string url = response.Headers.Get("Location");
                string hash = url.Split('#')[1];

                var request2 = new ProxiedWebRequest(url);
                var response2 = request2.Get();

                if (response2.StatusCode != 200)
                {
                    throw new Exception("Authentication failed");
                }

                if (string.IsNullOrEmpty(hash))
                {
                    throw new Exception("Cannot extract access token");
                }
                var dict = Request.ParseQueryString(hash);

                //foreach (var pair in dict)
                //{
                //    Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
                //}

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
                if (twoFA.IsMatch(response.Body))
                {
                    // TODO: Handle 2FA
                    throw new Exception("2FA enabled but not supported yet. Use browser sign-in method or try to disable 2FA in Microsoft account settings");
                }
                else if (invalidAccount.IsMatch(response.Body))
                {
                    throw new Exception("Invalid credentials. Check your credentials");
                }
                else throw new Exception("Unexpected response. Check your credentials. Response code: " + response.StatusCode);
            }
        }

        /// <summary>
        /// Xbox Live Authenticate
        /// </summary>
        /// <param name="loginResponse"></param>
        /// <returns></returns>
        public static XblAuthenticateResponse XblAuthenticate(Microsoft.LoginResponse loginResponse)
        {
            var request = new ProxiedWebRequest(xbl);
            request.UserAgent = userAgent;
            request.Accept = "application/json";
            request.Headers.Add("x-xbl-contract-version", "0");

            var accessToken = loginResponse.AccessToken;
            if (Settings.LoginMethod == "browser")
            {
                // Our own client ID must have d= in front of the token or HTTP status 400
                // "Stolen" client ID must not have d= in front of the token or HTTP status 400
                accessToken = "d=" + accessToken;
            }

            string payload = "{"
                + "\"Properties\": {"
                + "\"AuthMethod\": \"RPS\","
                + "\"SiteName\": \"user.auth.xboxlive.com\","
                + "\"RpsTicket\": \"" + accessToken + "\""
                + "},"
                + "\"RelyingParty\": \"http://auth.xboxlive.com\","
                + "\"TokenType\": \"JWT\""
                + "}";
            var response = request.Post("application/json", payload);
            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }
            if (response.StatusCode == 200)
            {
                string jsonString = response.Body;
                //Console.WriteLine(jsonString);

                Json.JSONData json = Json.ParseJson(jsonString);
                string token = json.Properties["Token"].StringValue;
                string userHash = json.Properties["DisplayClaims"].Properties["xui"].DataArray[0].Properties["uhs"].StringValue;
                return new XblAuthenticateResponse()
                {
                    Token = token,
                    UserHash = userHash
                };
            }
            else
            {
                throw new Exception("XBL Authentication failed");
            }
        }

        /// <summary>
        /// XSTS Authenticate
        /// </summary>
        /// <remarks>(Don't ask me what is XSTS, I DONT KNOW)</remarks>
        /// <param name="xblResponse"></param>
        /// <returns></returns>
        public static XSTSAuthenticateResponse XSTSAuthenticate(XblAuthenticateResponse xblResponse)
        {
            var request = new ProxiedWebRequest(xsts);
            request.UserAgent = userAgent;
            request.Accept = "application/json";
            request.Headers.Add("x-xbl-contract-version", "1");

            string payload = "{"
                + "\"Properties\": {"
                + "\"SandboxId\": \"RETAIL\","
                + "\"UserTokens\": ["
                + "\"" + xblResponse.Token + "\""
                + "]"
                + "},"
                + "\"RelyingParty\": \"rp://api.minecraftservices.com/\","
                + "\"TokenType\": \"JWT\""
                + "}";
            var response = request.Post("application/json", payload);
            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }
            if (response.StatusCode == 200)
            {
                string jsonString = response.Body;
                Json.JSONData json = Json.ParseJson(jsonString);
                string token = json.Properties["Token"].StringValue;
                string userHash = json.Properties["DisplayClaims"].Properties["xui"].DataArray[0].Properties["uhs"].StringValue;
                return new XSTSAuthenticateResponse()
                {
                    Token = token,
                    UserHash = userHash
                };
            }
            else
            {
                if (response.StatusCode == 401)
                {
                    Json.JSONData json = Json.ParseJson(response.Body);
                    if (json.Properties["XErr"].StringValue == "2148916233")
                    {
                        throw new Exception("The account doesn't have an Xbox account");
                    }
                    else if (json.Properties["XErr"].StringValue == "2148916238")
                    {
                        throw new Exception("The account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult");
                    }
                    else throw new Exception("Unknown XSTS error code: " + json.Properties["XErr"].StringValue);
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
    }

    static class MinecraftWithXbox
    {
        private static string loginWithXbox = "https://api.minecraftservices.com/authentication/login_with_xbox";
        private static string ownership = "https://api.minecraftservices.com/entitlements/mcstore";
        private static string profile = "https://api.minecraftservices.com/minecraft/profile";

        /// <summary>
        /// Login to Minecraft using the XSTS token and user hash obtained before
        /// </summary>
        /// <param name="userHash"></param>
        /// <param name="xstsToken"></param>
        /// <returns></returns>
        public static string LoginWithXbox(string userHash, string xstsToken)
        {
            var request = new ProxiedWebRequest(loginWithXbox);
            request.Accept = "application/json";

            string payload = "{\"identityToken\": \"XBL3.0 x=" + userHash + ";" + xstsToken + "\"}";
            var response = request.Post("application/json", payload);

            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }

            string jsonString = response.Body;
            Json.JSONData json = Json.ParseJson(jsonString);
            return json.Properties["access_token"].StringValue;
        }

        /// <summary>
        /// Check if user own Minecraft by access token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns>True if the user own the game</returns>
        public static bool UserHasGame(string accessToken)
        {
            var request = new ProxiedWebRequest(ownership);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));
            var response = request.Get();

            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }

            string jsonString = response.Body;
            Json.JSONData json = Json.ParseJson(jsonString);
            return json.Properties["items"].DataArray.Count > 0;
        }

        public static UserProfile GetUserProfile(string accessToken)
        {
            var request = new ProxiedWebRequest(profile);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));
            var response = request.Get();

            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }

            string jsonString = response.Body;
            Json.JSONData json = Json.ParseJson(jsonString);
            return new UserProfile()
            {
                UUID = json.Properties["id"].StringValue,
                UserName = json.Properties["name"].StringValue
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
