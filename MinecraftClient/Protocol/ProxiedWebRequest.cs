using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using MinecraftClient.Proxy;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Create a new http request and optionally with proxy according to setting
    /// </summary>
    public class ProxiedWebRequest
    {
        private readonly string httpVersion = "HTTP/1.0"; // Use 1.0 here because 1.1 server may send chunked data

        private readonly Uri uri;
        private string Host { get { return uri.Host; } }
        private int Port { get { return uri.Port; } }
        private string Path { get { return uri.PathAndQuery; } }
        private bool IsSecure { get { return uri.Scheme == "https"; } }

        public NameValueCollection Headers = new();

        public string UserAgent { get { return Headers.Get("User-Agent") ?? String.Empty; } set { Headers.Set("User-Agent", value); } }
        public string Accept { get { return Headers.Get("Accept") ?? String.Empty; } set { Headers.Set("Accept", value); } }
        public string Cookie { set { Headers.Set("Cookie", value); } }

        /// <summary>
        /// Create a new http request
        /// </summary>
        /// <param name="url">Target URL</param>
        public ProxiedWebRequest(string url)
        {
            uri = new Uri(url);
            SetupBasicHeaders();
        }

        /// <summary>
        /// Create a new http request with cookies
        /// </summary>
        /// <param name="url">Target URL</param>
        /// <param name="cookies">Cookies to use</param>
        public ProxiedWebRequest(string url, NameValueCollection cookies)
        {
            uri = new Uri(url);
            Headers.Add("Cookie", GetCookieString(cookies));
            SetupBasicHeaders();
        }

        /// <summary>
        /// Setup some basic headers
        /// </summary>
        private void SetupBasicHeaders()
        {
            Headers.Add("Host", Host);
            Headers.Add("User-Agent", "MCC/" + Program.Version);
            Headers.Add("Accept", "*/*");
            Headers.Add("Connection", "close");
        }

        /// <summary>
        /// Perform GET request and get the response. Proxy is handled automatically
        /// </summary>
        /// <returns></returns>
        public Response Get()
        {
            return Send("GET");
        }

        /// <summary>
        /// Perform POST request and get the response. Proxy is handled automatically
        /// </summary>
        /// <param name="contentType">The content type of request body</param>
        /// <param name="body">Request body</param>
        /// <returns></returns>
        public Response Post(string contentType, string body)
        {
            Headers.Add("Content-Type", contentType);
            // Calculate length
            Headers.Add("Content-Length", Encoding.UTF8.GetBytes(body).Length.ToString());
            return Send("POST", body);
        }

        /// <summary>
        /// Send a http request to the server. Proxy is handled automatically
        /// </summary>
        /// <param name="method">Method in string representation</param>
        /// <param name="body">Optional request body</param>
        /// <returns></returns>
        private Response Send(string method, string body = "")
        {
            List<string> requestMessage = new()
            {
                string.Format("{0} {1} {2}", method.ToUpper(), Path, httpVersion) // Request line
            };
            foreach (string key in Headers) // Headers
            {
                var value = Headers[key];
                requestMessage.Add(string.Format("{0}: {1}", key, value));
            }
            requestMessage.Add(""); // <CR><LF>
            if (body != "")
            {
                requestMessage.Add(body);
            }
            else requestMessage.Add(""); // <CR><LF>
            if (Settings.DebugMessages)
            {
                foreach (string l in requestMessage)
                {
                    ConsoleIO.WriteLine("< " + l);
                }
            }
            Response response = Response.Empty();
            AutoTimeout.Perform(() =>
            {
                TcpClient client = ProxyHandler.NewTcpClient(Host, Port, true);
                Stream stream;
                if (IsSecure)
                {
                    stream = new SslStream(client.GetStream());
                    ((SslStream)stream).AuthenticateAsClient(Host, null, SslProtocols.Tls12, true); // Enable TLS 1.2. Hotfix for #1774
                }
                else
                {
                    stream = client.GetStream();
                }
                string h = string.Join("\r\n", requestMessage.ToArray());
                byte[] data = Encoding.ASCII.GetBytes(h);
                stream.Write(data, 0, data.Length);
                stream.Flush();
                StreamReader sr = new(stream);
                string rawResult = sr.ReadToEnd();
                response = ParseResponse(rawResult);
                try
                {
                    sr.Close();
                    stream.Close();
                    client.Close();
                }
                catch { }
            },
            TimeSpan.FromSeconds(30));
            return response;
        }

        /// <summary>
        /// Parse a raw response string to response object
        /// </summary>
        /// <param name="raw">raw response string</param>
        /// <returns></returns>
        private Response ParseResponse(string raw)
        {
            int statusCode;
            string responseBody = "";
            NameValueCollection headers = new();
            NameValueCollection cookies = new();
            if (raw.StartsWith("HTTP/1.1") || raw.StartsWith("HTTP/1.0"))
            {
                Queue<string> msg = new(raw.Split(new string[] { "\r\n" }, StringSplitOptions.None));
                statusCode = int.Parse(msg.Dequeue().Split(' ')[1]);

                while (msg.Peek() != "")
                {
                    string[] header = msg.Dequeue().Split(new char[] { ':' }, 2); // Split first ':' only
                    string key = header[0].ToLower(); // Key is case-insensitive
                    string value = header[1];
                    if (key == "set-cookie")
                    {
                        string[] cookie = value.Split(';'); // cookie options are ignored
                        string[] tmp = cookie[0].Split(new char[] { '=' }, 2); // Split first '=' only
                        string cname = tmp[0].Trim();
                        string cvalue = tmp[1].Trim();
                        cookies.Add(cname, cvalue);
                    }
                    else
                    {
                        headers.Add(key, value.Trim());
                    }
                }
                msg.Dequeue();
                if (msg.Count > 0)
                    responseBody = msg.Dequeue();

                return new Response(statusCode, responseBody, headers, cookies);
            }
            else
            {
                return new Response(520 /* Web Server Returned an Unknown Error */, "", headers, cookies);
            }
        }

        /// <summary>
        /// Get the cookie string representation to use in header
        /// </summary>
        /// <param name="cookies"></param>
        /// <returns></returns>
        private static string GetCookieString(NameValueCollection cookies)
        {
            var sb = new StringBuilder();
            foreach (string key in cookies)
            {
                var value = cookies[key];
                sb.Append(string.Format("{0}={1}; ", key, value));
            }
            string result = sb.ToString();
            return result.Remove(result.Length - 2); // Remove "; " at the end
        }

        /// <summary>
        /// Basic response object
        /// </summary>
        public class Response
        {
            public int StatusCode;
            public string Body;
            public NameValueCollection Headers;
            public NameValueCollection Cookies;

            public Response(int statusCode, string body, NameValueCollection headers, NameValueCollection cookies)
            {
                StatusCode = statusCode;
                Body = body;
                Headers = headers;
                Cookies = cookies;
            }

            /// <summary>
            /// Get an empty response object
            /// </summary>
            /// <returns></returns>
            public static Response Empty()
            {
                return new Response(204 /* No content */, "", new NameValueCollection(), new NameValueCollection());
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Status code: " + StatusCode);
                sb.AppendLine("Headers:");
                foreach (string key in Headers)
                {
                    sb.AppendLine(string.Format("  {0}: {1}", key, Headers[key]));
                }
                if (Cookies.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Cookies: ");
                    foreach (string key in Cookies)
                    {
                        sb.AppendLine(string.Format("  {0}={1}", key, Cookies[key]));
                    }
                }
                if (Body != "")
                {
                    sb.AppendLine();
                    if (Body.Length > 200)
                    {
                        sb.AppendLine("Body: (Truncated to 200 characters)");
                    }
                    else sb.AppendLine("Body: ");
                    sb.AppendLine(Body.Length > 200 ? Body[..200] + "..." : Body);
                }
                return sb.ToString();
            }
        }
    }
}
