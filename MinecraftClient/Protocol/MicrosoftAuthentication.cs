using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace MinecraftClient.Protocol
{
    static class Microsoft
    {
        private static readonly string clientId = "54473e32-df8f-42e9-a649-9419b0dab9d3";
        private static readonly string signinUrl = string.Format("https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id={0}&response_type=code&redirect_uri=https%3A%2F%2Fmccteam.github.io%2Fredirect.html&scope=XboxLive.signin%20offline_access%20openid%20email&prompt=select_account&response_mode=fragment", clientId);
        private static readonly string tokenUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        private static readonly string deviceCodeUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";

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
        /// Initiate the OAuth 2.0 device code flow.
        /// Returns a device code response containing the user code and verification URI.
        /// </summary>
        /// <returns>Device code response for user to complete authentication</returns>
        public static DeviceCodeResponse RequestDeviceCode()
        {
            string postData = string.Format("client_id={0}&scope=XboxLive.signin%20offline_access%20openid%20email", clientId);

            var request = new ProxiedWebRequest(deviceCodeUrl)
            {
                UserAgent = "MCC/" + Program.Version
            };
            var response = request.Post("application/x-www-form-urlencoded", postData);
            var jsonData = Json.ParseJson(response.Body);

            if (jsonData?["error"] is not null)
            {
                throw new Exception(jsonData["error_description"]!.GetStringValue());
            }

            return new DeviceCodeResponse()
            {
                DeviceCode = jsonData!["device_code"]!.GetStringValue(),
                UserCode = jsonData["user_code"]!.GetStringValue(),
                VerificationUri = jsonData["verification_uri"]!.GetStringValue(),
                ExpiresIn = int.Parse(jsonData["expires_in"]!.GetStringValue(), NumberStyles.Any, CultureInfo.CurrentCulture),
                Interval = int.Parse(jsonData["interval"]!.GetStringValue(), NumberStyles.Any, CultureInfo.CurrentCulture),
                Message = jsonData["message"]!.GetStringValue()
            };
        }

        /// <summary>
        /// Poll the token endpoint until the user completes device code authentication.
        /// Handles authorization_pending, slow_down, and expiration.
        /// </summary>
        /// <param name="deviceCode">Device code from <see cref="RequestDeviceCode"/></param>
        /// <param name="expiresIn">Expiration time in seconds</param>
        /// <param name="interval">Polling interval in seconds</param>
        /// <returns>Login response with access token and refresh token</returns>
        public static LoginResponse PollDeviceCodeToken(string deviceCode, int expiresIn, int interval)
        {
            // Per OAuth 2.0 device code spec, server may respond with "slow_down" requiring
            // the client to increase its polling interval by this amount
            const int SlowDownIncrementSeconds = 5;

            string postData = string.Format(
                "client_id={0}&grant_type=urn:ietf:params:oauth:grant-type:device_code&device_code={1}",
                clientId, deviceCode);

            var stopwatch = Stopwatch.StartNew();
            int pollInterval = interval;

            while (stopwatch.Elapsed.TotalSeconds < expiresIn)
            {
                Thread.Sleep(pollInterval * 1000);

                var request = new ProxiedWebRequest(tokenUrl)
                {
                    UserAgent = "MCC/" + Program.Version
                };
                var response = request.Post("application/x-www-form-urlencoded", postData);
                var jsonData = Json.ParseJson(response.Body);

                if (jsonData?["error"] is not null)
                {
                    string error = jsonData["error"]!.GetStringValue();

                    if (error == "authorization_pending")
                    {
                        // User hasn't completed auth yet, keep polling
                        continue;
                    }
                    else if (error == "slow_down")
                    {
                        // Server asked us to slow down
                        pollInterval += SlowDownIncrementSeconds;
                        continue;
                    }
                    else if (error == "expired_token")
                    {
                        throw new Exception("Device code expired. Please try again.");
                    }
                    else if (error == "authorization_declined")
                    {
                        throw new Exception("Authorization was declined by the user.");
                    }
                    else
                    {
                        throw new Exception(jsonData["error_description"]!.GetStringValue());
                    }
                }

                // Success - parse the token response
                string accessToken = jsonData!["access_token"]!.GetStringValue();
                string refreshToken = jsonData["refresh_token"]!.GetStringValue();
                int tokenExpiresIn = int.Parse(jsonData["expires_in"]!.GetStringValue(), NumberStyles.Any, CultureInfo.CurrentCulture);

                // Extract email from JWT id_token
                string payload = JwtPayloadDecode.GetPayload(jsonData["id_token"]!.GetStringValue());
                var jsonPayload = Json.ParseJson(payload);
                string email = jsonPayload!["email"]!.GetStringValue();

                return new LoginResponse()
                {
                    Email = email,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = tokenExpiresIn
                };
            }

            throw new Exception("Device code authentication timed out.");
        }

        /// <summary>
        /// Perform request to obtain access token by code or by refresh token
        /// </summary>
        /// <param name="postData">Complete POST data for the request</param>
        /// <returns></returns>
        private static LoginResponse RequestToken(string postData)
        {
            var request = new ProxiedWebRequest(tokenUrl)
            {
                UserAgent = "MCC/" + Program.Version
            };
            var response = request.Post("application/x-www-form-urlencoded", postData);
            var jsonData = Json.ParseJson(response.Body);

            // Error handling
            if (jsonData?["error"] is not null)
            {
                throw new Exception(jsonData["error_description"]!.GetStringValue());
            }
            else
            {
                string accessToken = jsonData!["access_token"]!.GetStringValue();
                string refreshToken = jsonData["refresh_token"]!.GetStringValue();
                int expiresIn = int.Parse(jsonData["expires_in"]!.GetStringValue(), NumberStyles.Any, CultureInfo.CurrentCulture);

                // Extract email from JWT
                string payload = JwtPayloadDecode.GetPayload(jsonData["id_token"]!.GetStringValue());
                var jsonPayload = Json.ParseJson(payload);
                string email = jsonPayload!["email"]!.GetStringValue();
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var ps = new ProcessStartInfo(link)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };

                    Process.Start(ps);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", link);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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

        public struct DeviceCodeResponse
        {
            public string DeviceCode;
            public string UserCode;
            public string VerificationUri;
            public int ExpiresIn;
            public int Interval;
            public string Message;
        }
    }

    static class XboxLive
    {
        private static readonly string xbl = "https://user.auth.xboxlive.com/user/authenticate";
        private static readonly string xsts = "https://xsts.auth.xboxlive.com/xsts/authorize";

        private static readonly string userAgent = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        /// <summary>
        /// Xbox Live Authenticate
        /// </summary>
        /// <param name="loginResponse"></param>
        /// <returns></returns>
        public static XblAuthenticateResponse XblAuthenticate(Microsoft.LoginResponse loginResponse)
        {
            var request = new ProxiedWebRequest(xbl)
            {
                UserAgent = userAgent,
                Accept = "application/json"
            };
            request.Headers.Add("x-xbl-contract-version", "0");

            // OAuth tokens from our own client ID require "d=" prefix for XBL authentication
            var accessToken = "d=" + loginResponse.AccessToken;

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
            if (Settings.Config.Logging.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }
            if (response.StatusCode == 200)
            {
                string jsonString = response.Body;
                var json = Json.ParseJson(jsonString);
                string token = json!["Token"]!.GetStringValue();
                string userHash = json["DisplayClaims"]!["xui"]![0]!["uhs"]!.GetStringValue();
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
        /// <remarks>Xbox Secure Token Service - exchanges XBL token for a service-specific XSTS token</remarks>
        /// <param name="xblResponse"></param>
        /// <returns></returns>
        public static XSTSAuthenticateResponse XSTSAuthenticate(XblAuthenticateResponse xblResponse)
        {
            var request = new ProxiedWebRequest(xsts)
            {
                UserAgent = userAgent,
                Accept = "application/json"
            };
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
            if (Settings.Config.Logging.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }
            if (response.StatusCode == 200)
            {
                string jsonString = response.Body;
                var json = Json.ParseJson(jsonString);
                string token = json!["Token"]!.GetStringValue();
                string userHash = json["DisplayClaims"]!["xui"]![0]!["uhs"]!.GetStringValue();
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
                    var json = Json.ParseJson(response.Body);
                    if (json!["XErr"]!.GetStringValue() == "2148916233")
                    {
                        throw new Exception("The account doesn't have an Xbox account");
                    }
                    else if (json["XErr"]!.GetStringValue() == "2148916238")
                    {
                        throw new Exception("The account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult");
                    }
                    else throw new Exception("Unknown XSTS error code: " + json["XErr"]!.GetStringValue());
                }
                else
                {
                    throw new Exception("XSTS Authentication failed");
                }
            }
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
        private static readonly string loginWithXbox = "https://api.minecraftservices.com/authentication/login_with_xbox";
        private static readonly string ownership = "https://api.minecraftservices.com/entitlements/mcstore";
        private static readonly string profile = "https://api.minecraftservices.com/minecraft/profile";

        /// <summary>
        /// Login to Minecraft using the XSTS token and user hash obtained before
        /// </summary>
        /// <param name="userHash"></param>
        /// <param name="xstsToken"></param>
        /// <returns></returns>
        public static string LoginWithXbox(string userHash, string xstsToken)
        {
            var request = new ProxiedWebRequest(loginWithXbox)
            {
                Accept = "application/json"
            };

            string payload = "{\"identityToken\": \"XBL3.0 x=" + userHash + ";" + xstsToken + "\"}";
            var response = request.Post("application/json", payload);

            if (Settings.Config.Logging.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }

            string jsonString = response.Body;
            var json = Json.ParseJson(jsonString);

            return json!["access_token"]!.GetStringValue();
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

            if (Settings.Config.Logging.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }

            string jsonString = response.Body;
            var json = Json.ParseJson(jsonString);
            return json!["items"]!.AsArray().Count > 0;
        }

        public static UserProfile GetUserProfile(string accessToken)
        {
            var request = new ProxiedWebRequest(profile);
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));
            var response = request.Get();

            if (Settings.Config.Logging.DebugMessages)
            {
                ConsoleIO.WriteLine(response.ToString());
            }

            string jsonString = response.Body;
            var json = Json.ParseJson(jsonString);
            return new UserProfile()
            {
                UUID = json!["id"]!.GetStringValue(),
                UserName = json["name"]!.GetStringValue()
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
