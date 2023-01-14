using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using MinecraftClient.Proxy;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Create a new http request and optionally with proxy according to setting
    /// </summary>
    public class ProxiedWebRequest
    {
        public interface ITcpFactory
        {
            TcpClient CreateTcpClient(string host, int port);
        };

        private readonly string httpVersion = "HTTP/1.1";

        private ITcpFactory? tcpFactory;
        private bool isProxied = false; // Send absolute Url in request if true

        private readonly Uri uri;
        private string Host { get { return uri.Host; } }
        private int Port { get { return uri.Port; } }
        private string Path { get { return uri.PathAndQuery; } }
        private string AbsoluteUrl { get { return uri.AbsoluteUri; } }
        private bool IsSecure { get { return uri.Scheme == "https"; } }

        public NameValueCollection Headers = new();

        public string UserAgent { get { return Headers.Get("User-Agent") ?? String.Empty; } set { Headers.Set("User-Agent", value); } }
        public string Accept { get { return Headers.Get("Accept") ?? String.Empty; } set { Headers.Set("Accept", value); } }
        public string Cookie { set { Headers.Set("Cookie", value); } }

        /// <summary>
        /// Set to true to tell the http client proxy is enabled
        /// </summary>
        public bool IsProxy { get { return isProxied; } set { isProxied = value; } }
        public bool Debug { get { return Settings.Config.Logging.DebugMessages; } }

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
        /// Create a new http request with custom tcp client
        /// </summary>
        /// <param name="tcpFactory">Tcp factory to be used</param>
        /// <param name="url">Target URL</param>
        public ProxiedWebRequest(ITcpFactory tcpFactory, string url) : this(url)
        {
            this.tcpFactory = tcpFactory;
        }

        /// <summary>
        /// Create a new http request with custom tcp client and cookies
        /// </summary>
        /// <param name="tcpFactory">Tcp factory to be used</param>
        /// <param name="url">Target URL</param>
        /// <param name="cookies">Cookies to use</param>
        public ProxiedWebRequest(ITcpFactory tcpFactory, string url, NameValueCollection cookies) : this(url, cookies)
        {
            this.tcpFactory = tcpFactory;
        }

        /// <summary>
        /// Setup some basic headers
        /// </summary>
        private void SetupBasicHeaders()
        {
            Headers.Add("Host", Host);
            Headers.Add("User-Agent", "MCC/1.0");
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
                string.Format("{0} {1} {2}", method.ToUpper(), isProxied ? AbsoluteUrl : Path, httpVersion) // Request line
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
            if (Debug)
            {
                foreach (string l in requestMessage)
                {
                    ConsoleIO.WriteLine("< " + l);
                }
            }
            Response response = Response.Empty();

            // FIXME: Use TcpFactory interface to avoid direct usage of the ProxyHandler class
            // TcpClient client = tcpFactory.CreateTcpClient(Host, Port);
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

            // Read response
            int statusCode = ReadHttpStatus(stream);
            var headers = ReadHeader(stream);
            string? rbody;
            if (headers.Get("transfer-encoding") == "chunked")
            {
                rbody = ReadBodyChunked(stream);
            }
            else
            {
                rbody = ReadBody(stream, int.Parse(headers.Get("content-length") ?? "0"));
            }
            if (headers.Get("set-cookie") != null)
            {
                response.Cookies = ParseSetCookie(headers.GetValues("set-cookie") ?? Array.Empty<string>());
            }
            response.Body = rbody ?? "";
            response.StatusCode = statusCode;
            response.Headers = headers;

            try
            {
                stream.Close();
                client.Close();
            }
            catch { }

            return response;
        }

        /// <summary>
        /// Read HTTP response line from a Stream
        /// </summary>
        /// <param name="s">Stream to read</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException">If server return unknown data</exception>
        private static int ReadHttpStatus(Stream s)
        {
            var httpHeader = ReadLine(s); // http header line
            if (httpHeader.StartsWith("HTTP/1.1") || httpHeader.StartsWith("HTTP/1.0"))
            {
                return int.Parse(httpHeader.Split(' ')[1], NumberStyles.Any, CultureInfo.CurrentCulture);
            }
            else
            {
                throw new InvalidDataException("Unexpect data from server");
            }
        }

        /// <summary>
        /// Read HTTP headers from a Stream
        /// </summary>
        /// <param name="s">Stream to read</param>
        /// <returns>Headers in lower-case</returns>
        private static NameValueCollection ReadHeader(Stream s)
        {
            var headers = new NameValueCollection();
            // Read headers
            string header;
            do
            {
                header = ReadLine(s);
                if (!String.IsNullOrEmpty(header))
                {
                    var tmp = header.Split(new char[] { ':' }, 2);
                    var name = tmp[0].ToLower();
                    var value = tmp[1].Trim();
                    headers.Add(name, value);
                }
            }
            while (!String.IsNullOrEmpty(header));
            return headers;
        }

        /// <summary>
        /// Read HTTP body from a Stream
        /// </summary>
        /// <param name="s">Stream to read</param>
        /// <param name="length">Length of the body (the Content-Length header)</param>
        /// <returns>Body or null if length is zero</returns>
        private static string? ReadBody(Stream s, int length)
        {
            if (length > 0)
            {
                byte[] buffer = new byte[length];
                int r = 0;
                while (r < length)
                {
                    var read = s.Read(buffer, r, length - r);
                    r += read;
                    Thread.Sleep(50);
                }
                return Encoding.UTF8.GetString(buffer);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Read HTTP chunked body from a Stream
        /// </summary>
        /// <param name="s">Stream to read</param>
        /// <returns>Body or empty string if nothing is received</returns>
        private static string ReadBodyChunked(Stream s)
        {
            List<byte> buffer1 = new();
            while (true)
            {
                string l = ReadLine(s);
                int size = Int32.Parse(l, NumberStyles.HexNumber);
                if (size == 0)
                    break;
                byte[] buffer2 = new byte[size];
                int r = 0;
                while (r < size)
                {
                    var read = s.Read(buffer2, r, size - r);
                    r += read;
                    Thread.Sleep(50);
                }
                ReadLine(s);
                buffer1.AddRange(buffer2);
            }
            return Encoding.UTF8.GetString(buffer1.ToArray());
        }

        /// <summary>
        /// Parse the Set-Cookie header value into NameValueCollection. Cookie options are ignored
        /// </summary>
        /// <param name="headerValue">Array of value strings</param>
        /// <returns>Parsed cookies</returns>
        private static NameValueCollection ParseSetCookie(IEnumerable<string> headerValue)
        {
            NameValueCollection cookies = new();
            foreach (var value in headerValue)
            {
                string[] cookie = value.Split(';'); // cookie options are ignored
                string[] tmp = cookie[0].Split(new char[] { '=' }, 2); // Split first '=' only
                string[] options = cookie[1..];
                string cname = tmp[0].Trim();
                string cvalue = tmp[1].Trim();
                // Check expire
                bool isExpired = false;
                foreach (var option in options)
                {
                    var tmp2 = option.Trim().Split(new char[] { '=' }, 2);
                    // Check for Expires=<date> and Max-Age=<number>
                    if (tmp2.Length == 2)
                    {
                        var optName = tmp2[0].Trim().ToLower();
                        var optValue = tmp2[1].Trim();
                        switch (optName)
                        {
                            case "expires":
                                {
                                    if (DateTime.TryParse(optValue, out var expDate))
                                    {
                                        if (expDate < DateTime.Now)
                                            isExpired = true;
                                    }
                                    break;
                                }
                            case "max-age":
                                {
                                    if (int.TryParse(optValue, out var expInt))
                                    {
                                        if (expInt <= 0)
                                            isExpired = true;
                                    }
                                    break;
                                }
                        }
                    }
                    if (isExpired)
                        break;
                }
                if (!isExpired)
                    cookies.Add(cname, cvalue);
            }
            return cookies;
        }

        /// <summary>
        /// Read a line from a Stream
        /// </summary>
        /// <remarks>
        /// Line break by \r\n and they are not included in returned string
        /// </remarks>
        /// <param name="s">Stream to read</param>
        /// <returns>String</returns>
        private static string ReadLine(Stream s)
        {
            List<byte> buffer = new();
            byte c;
            while (true)
            {
                int b = s.ReadByte();
                if (b == -1)
                    break;
                c = (byte)b;
                if (c == '\n')
                {
                    if (buffer.Last() == '\r')
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                        break;
                    }
                }
                buffer.Add(c);
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
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