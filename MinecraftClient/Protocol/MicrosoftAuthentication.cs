using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace MinecraftClient.Protocol
{
    class XboxLive
    {
        private readonly string authorize = "https://login.live.com/oauth20_authorize.srf?client_id=000000004C12AE6F&redirect_uri=https://login.live.com/oauth20_desktop.srf&scope=service::user.auth.xboxlive.com::MBI_SSL&display=touch&response_type=token&locale=en";
        private readonly string xbl = "https://user.auth.xboxlive.com/user/authenticate";
        private readonly string xsts = "https://xsts.auth.xboxlive.com/xsts/authorize";

        private readonly string userAgent = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        private Regex ppft = new Regex("sFTTag:'.*value=\"(.*)\"\\/>'");
        private Regex urlPost = new Regex("urlPost:'(.+?(?=\'))");
        private Regex confirm = new Regex("identity\\/confirm");
        private Regex invalidAccount = new Regex("Sign in to", RegexOptions.IgnoreCase);
        private Regex twoFA = new Regex("Help us protect your account", RegexOptions.IgnoreCase);

        public string SignInUrl { get { return authorize; } }

        /// <summary>
        /// Pre-authentication
        /// </summary>
        /// <remarks>This step is to get the login page for later use</remarks>
        /// <returns></returns>
        public PreAuthResponse PreAuth()
        {
            var request = new ProxiedWebRequest(authorize);
            request.UserAgent = userAgent;
            var response = request.Get();

            string html = response.Body;

            string PPFT = ppft.Match(html).Groups[1].Value;
            string urlPost = this.urlPost.Match(html).Groups[1].Value;

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
        public UserLoginResponse UserLogin(string email, string password, PreAuthResponse preAuth)
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

                return new UserLoginResponse()
                {
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
        public XblAuthenticateResponse XblAuthenticate(UserLoginResponse loginResponse)
        {
            var request = new ProxiedWebRequest(xbl);
            request.UserAgent = userAgent;
            request.Accept = "application/json";
            request.Headers.Add("x-xbl-contract-version", "0");

            string payload = "{"
                + "\"Properties\": {"
                + "\"AuthMethod\": \"RPS\","
                + "\"SiteName\": \"user.auth.xboxlive.com\","
                + "\"RpsTicket\": \"" + loginResponse.AccessToken + "\""
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
        public XSTSAuthenticateResponse XSTSAuthenticate(XblAuthenticateResponse xblResponse)
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

        public struct UserLoginResponse
        {
            public string AccessToken;
            public string RefreshToken;
            public int ExpiresIn;
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

    class MinecraftWithXbox
    {
        private readonly string loginWithXbox = "https://api.minecraftservices.com/authentication/login_with_xbox";
        private readonly string ownership = "https://api.minecraftservices.com/entitlements/mcstore";
        private readonly string profile = "https://api.minecraftservices.com/minecraft/profile";

        /// <summary>
        /// Login to Minecraft using the XSTS token and user hash obtained before
        /// </summary>
        /// <param name="userHash"></param>
        /// <param name="xstsToken"></param>
        /// <returns></returns>
        public string LoginWithXbox(string userHash, string xstsToken)
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
        public bool UserHasGame(string accessToken)
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

        public UserProfile GetUserProfile(string accessToken)
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
