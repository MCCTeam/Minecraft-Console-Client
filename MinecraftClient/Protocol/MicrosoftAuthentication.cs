using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

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

        /// <summary>
        /// Pre-authentication
        /// </summary>
        /// <remarks>This step is to get the login page for later use</remarks>
        /// <returns></returns>
        public PreAuthResponse PreAuth()
        {
            var request = Request.Create(authorize);
            request.UserAgent = userAgent;
            request.CookieContainer = new CookieContainer();
            var response = (HttpWebResponse)request.GetResponse();

            string html = Request.ReadBody(response);

            string PPFT = ppft.Match(html).Groups[1].Value;
            string urlPost = this.urlPost.Match(html).Groups[1].Value;

            if (string.IsNullOrEmpty(PPFT) || string.IsNullOrEmpty(urlPost))
            {
                throw new Exception("Fail to extract PPFT or urlPost");
            }
            //Console.WriteLine("PPFT: {0}", PPFT);
            //Console.WriteLine();
            //Console.WriteLine("urlPost: {0}", urlPost);

            response.Close();

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
            var request = Request.Create(preAuth.UrlPost);
            request.UserAgent = userAgent;
            request.AllowAutoRedirect = false; // Need to save the redirect URL
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(preAuth.Cookie);

            string postData = "login=" + Uri.EscapeDataString(email)
                 + "&loginfmt=" + Uri.EscapeDataString(email)
                 + "&passwd=" + Uri.EscapeDataString(password)
                 + "&PPFT=" + Uri.EscapeDataString(preAuth.PPFT);
            byte[] data = Encoding.UTF8.GetBytes(postData);

            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            if ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399)
            {
                string url = response.Headers.Get("Location");
                string hash = url.Split('#')[1];

                var request2 = Request.Create(url);
                var response2 = (HttpWebResponse)request2.GetResponse();

                if ((int)response2.StatusCode != 200)
                {
                    throw new Exception("Authentication failed");
                }

                if (string.IsNullOrEmpty(hash))
                {
                    if (confirm.IsMatch(Request.ReadBody(response2)))
                    {
                        throw new Exception("Activity confirmation required");
                    }
                    else throw new Exception("Invalid credentials or 2FA enabled");
                }
                var dict = Request.ParseQueryString(hash);

                //foreach (var pair in dict)
                //{
                //    Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
                //}

                response.Close();
                response2.Close();

                return new UserLoginResponse()
                {
                    AccessToken = dict["access_token"],
                    RefreshToken = dict["refresh_token"],
                    ExpiresIn = int.Parse(dict["expires_in"])
                };
            }
            else
            {
                throw new Exception("Unexpected response. Check your credentials");
            }
        }

        /// <summary>
        /// Xbox Live Authenticate
        /// </summary>
        /// <param name="loginResponse"></param>
        /// <returns></returns>
        public XblAuthenticateResponse XblAuthenticate(UserLoginResponse loginResponse)
        {
            var request = Request.Create(xbl);
            request.Method = "POST";
            request.UserAgent = userAgent;
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers.Add("Accept-Encoding", "gzip");
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

            byte[] data = Encoding.UTF8.GetBytes(payload);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                string jsonString = Request.ReadBody(response);
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
            catch (WebException)
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
            var request = Request.Create(xsts);
            request.Method = "POST";
            request.UserAgent = userAgent;
            request.ContentType = "application/json";
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
            byte[] data = Encoding.UTF8.GetBytes(payload);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                string jsonString = Request.ReadBody(response);
                Json.JSONData json = Json.ParseJson(jsonString);
                string token = json.Properties["Token"].StringValue;
                string userHash = json.Properties["DisplayClaims"].Properties["xui"].DataArray[0].Properties["uhs"].StringValue;
                return new XSTSAuthenticateResponse()
                {
                    Token = token,
                    UserHash = userHash
                };
            }
            catch (WebException err)
            {
                var resp = (HttpWebResponse)err.Response;
                if ((int)resp.StatusCode == 401)
                {
                    Json.JSONData json = Json.ParseJson(Request.ReadBody(resp));
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
            public CookieCollection Cookie;
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
            var request = Request.Create(loginWithXbox);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";

            string payload = "{\"identityToken\": \"XBL3.0 x=" + userHash + ";" + xstsToken + "\"}";
            byte[] data = Encoding.UTF8.GetBytes(payload);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();

            string jsonString = Request.ReadBody(response);
            // See https://github.com/ORelio/Sharp-Tools/issues/1
            jsonString = jsonString.Replace("[ ]", "[]");
            Json.JSONData json = Json.ParseJson(jsonString);
            response.Close();
            return json.Properties["access_token"].StringValue;
        }

        /// <summary>
        /// Check if user own Minecraft by access token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns>True if the user own the game</returns>
        public bool UserHasGame(string accessToken)
        {
            var request = Request.Create(ownership);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));
            var response = (HttpWebResponse)request.GetResponse();
            string jsonString = Request.ReadBody(response);
            // See https://github.com/ORelio/Sharp-Tools/issues/1
            jsonString = jsonString.Replace("[ ]", "[]");
            Json.JSONData json = Json.ParseJson(jsonString);
            response.Close();
            return json.Properties["items"].DataArray.Count > 0;
        }

        public UserProfile GetUserProfile(string accessToken)
        {
            var request = Request.Create(profile);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));
            var response = (HttpWebResponse)request.GetResponse();
            string jsonString = Request.ReadBody(response);
            // See https://github.com/ORelio/Sharp-Tools/issues/1
            jsonString = jsonString.Replace("[ ]", "[]");
            Json.JSONData json = Json.ParseJson(jsonString);
            response.Close();
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
        static public HttpWebRequest Create(string url)
        {
            return (HttpWebRequest)WebRequest.Create(url);
        }

        static public string ReadBody(WebResponse e)
        {
            using (var sr = new StreamReader(e.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }

        static public Dictionary<string, string> ParseQueryString(string query)
        {
            return query.Split('&')
                .ToDictionary(c => c.Split('=')[0],
                              c => Uri.UnescapeDataString(c.Split('=')[1]));
        }
    }
}
