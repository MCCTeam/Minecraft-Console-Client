using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using MinecraftClient.Proxy;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// HTTP client with automatic proxy support based on application settings.
    /// Backed by System.Net.Http.HttpClient with SocketsHttpHandler.
    /// </summary>
    public class ProxiedWebRequest
    {
        private const int DefaultConnectTimeoutSeconds = 30;

        private readonly Uri _uri;

        public NameValueCollection Headers { get; } = new();

        public string UserAgent
        {
            get => Headers.Get("User-Agent") ?? string.Empty;
            set => Headers.Set("User-Agent", value);
        }

        public string Accept
        {
            get => Headers.Get("Accept") ?? string.Empty;
            set => Headers.Set("Accept", value);
        }

        public string Cookie
        {
            set => Headers.Set("Cookie", value);
        }

        public bool Debug => Settings.Config.Logging.DebugMessages;

        /// <summary>
        /// Create a new HTTP request
        /// </summary>
        /// <param name="url">Target URL</param>
        public ProxiedWebRequest(string url)
        {
            _uri = new Uri(url);
            SetupBasicHeaders();
        }

        /// <summary>
        /// Create a new HTTP request with cookies
        /// </summary>
        /// <param name="url">Target URL</param>
        /// <param name="cookies">Cookies to include in the request</param>
        public ProxiedWebRequest(string url, NameValueCollection cookies)
        {
            _uri = new Uri(url);
            Headers.Add("Cookie", GetCookieString(cookies));
            SetupBasicHeaders();
        }

        private void SetupBasicHeaders()
        {
            Headers.Add("Host", _uri.Host);
            Headers.Add("User-Agent", "MCC/1.0");
            Headers.Add("Accept", "*/*");
        }

        /// <summary>
        /// Perform GET request. Proxy is handled automatically.
        /// </summary>
        public Response Get() => Send(HttpMethod.Get);

        /// <summary>
        /// Perform POST request. Proxy is handled automatically.
        /// </summary>
        /// <param name="contentType">The content type of request body</param>
        /// <param name="body">Request body</param>
        public Response Post(string contentType, string body) => Send(HttpMethod.Post, contentType, body);

        /// <summary>
        /// Send an HTTP request. Proxy is configured automatically from Settings.
        /// </summary>
        private Response Send(HttpMethod method, string? contentType = null, string? body = null)
        {
            using var handler = CreateHandler();
            using var client = new HttpClient(handler);

            using var request = new HttpRequestMessage(method, _uri);

            // Apply custom headers (skip content-level headers)
            foreach (string key in Headers)
            {
                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                    continue;

                request.Headers.TryAddWithoutValidation(key, Headers[key]);
            }

            if (body is not null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "text/plain");
            }

            if (Debug)
            {
                ConsoleIO.WriteLine($"< {method} {_uri}");
                foreach (string key in Headers)
                    ConsoleIO.WriteLine($"< {key}: {Headers[key]}");
            }

            try
            {
                using var httpResponse = client.Send(request);
                using var stream = httpResponse.Content.ReadAsStream();
                using var reader = new System.IO.StreamReader(stream);
                string responseBody = reader.ReadToEnd();

                var responseHeaders = new NameValueCollection();
                foreach (var header in httpResponse.Headers)
                    foreach (var val in header.Value)
                        responseHeaders.Add(header.Key.ToLowerInvariant(), val);
                foreach (var header in httpResponse.Content.Headers)
                    foreach (var val in header.Value)
                        responseHeaders.Add(header.Key.ToLowerInvariant(), val);

                var cookies = new NameValueCollection();
                foreach (System.Net.Cookie cookie in handler.CookieContainer.GetCookies(_uri))
                {
                    if (!cookie.Expired)
                        cookies.Add(cookie.Name, cookie.Value);
                }

                return new Response((int)httpResponse.StatusCode, responseBody, responseHeaders, cookies);
            }
            catch (HttpRequestException ex)
            {
                if (Debug)
                    ConsoleIO.WriteLine("HTTP error: " + ex.Message);
                return Response.Empty();
            }
        }

        /// <summary>
        /// Create a SocketsHttpHandler with proxy support from ProxyHandler settings.
        /// </summary>
        private static SocketsHttpHandler CreateHandler()
        {
            var handler = new SocketsHttpHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = false,
                ConnectTimeout = TimeSpan.FromSeconds(DefaultConnectTimeoutSeconds),
            };

            if (ProxyHandler.Config.Enabled_Login)
            {
                string proxyScheme = ProxyHandler.Config.Proxy_Type switch
                {
                    ProxyHandler.Configs.ProxyType.SOCKS4 => "socks4",
                    ProxyHandler.Configs.ProxyType.SOCKS4a => "socks4a",
                    ProxyHandler.Configs.ProxyType.SOCKS5 => "socks5",
                    _ => "http"
                };

                var proxyUri = new Uri($"{proxyScheme}://{ProxyHandler.Config.Server.Host}:{ProxyHandler.Config.Server.Port}");
                var proxy = new WebProxy(proxyUri);

                if (!string.IsNullOrWhiteSpace(ProxyHandler.Config.Username) &&
                    !string.IsNullOrWhiteSpace(ProxyHandler.Config.Password))
                {
                    proxy.Credentials = new NetworkCredential(
                        ProxyHandler.Config.Username,
                        ProxyHandler.Config.Password);
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            return handler;
        }

        /// <summary>
        /// Build a cookie header value from a NameValueCollection.
        /// </summary>
        private static string GetCookieString(NameValueCollection cookies)
        {
            var sb = new StringBuilder();
            foreach (string key in cookies)
            {
                sb.Append($"{key}={cookies[key]}; ");
            }
            string result = sb.ToString();
            return result.Length >= 2 ? result[..^2] : result;
        }

        /// <summary>
        /// Basic HTTP response object.
        /// </summary>
        public record Response(int StatusCode, string Body, NameValueCollection Headers, NameValueCollection Cookies)
        {
            public static Response Empty() =>
                new(204, "", new NameValueCollection(), new NameValueCollection());

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Status code: " + StatusCode);
                sb.AppendLine("Headers:");
                foreach (string key in Headers)
                    sb.AppendLine($"  {key}: {Headers[key]}");
                if (Cookies.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Cookies: ");
                    foreach (string key in Cookies)
                        sb.AppendLine($"  {key}={Cookies[key]}");
                }
                if (Body != "")
                {
                    sb.AppendLine();
                    sb.AppendLine(Body.Length > 200 ? "Body: (Truncated to 200 characters)" : "Body: ");
                    sb.AppendLine(Body.Length > 200 ? Body[..200] + "..." : Body);
                }
                return sb.ToString();
            }
        }
    }
}