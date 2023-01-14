using System.Net.Sockets;
using Starksoft.Aspen.Proxy;
using Tomlet.Attributes;

namespace MinecraftClient.Proxy
{
    /// <summary>
    /// Automatically handle proxies according to the app Settings.
    /// Note: Underlying proxy handling is taken from Starksoft, LLC's Biko Library.
    /// This library is open source and provided under the MIT license. More info at biko.codeplex.com.
    /// </summary>

    public static class ProxyHandler
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [TomlInlineComment("$Proxy.Enabled_Update$")]
            public bool Enabled_Update = false;

            [TomlInlineComment("$Proxy.Enabled_Login$")]
            public bool Enabled_Login = false;

            [TomlInlineComment("$Proxy.Enabled_Ingame$")]
            public bool Enabled_Ingame = false;

            [TomlInlineComment("$Proxy.Server$")]
            public ProxyInfoConfig Server = new("0.0.0.0", 8080);

            [TomlInlineComment("$Proxy.Proxy_Type$")]
            public ProxyType Proxy_Type = ProxyType.HTTP;

            [TomlInlineComment("$Proxy.Username$")]
            public string Username = "";

            [TomlInlineComment("$Proxy.Password$")]
            public string Password = "";

            public void OnSettingUpdate() { }

            public struct ProxyInfoConfig
            {
                public string Host;
                public ushort Port;

                public ProxyInfoConfig(string host, ushort port)
                {
                    Host = host;
                    Port = port;
                }
            }

            public enum ProxyType { HTTP, SOCKS4, SOCKS4a, SOCKS5 };
        }

        private static readonly ProxyClientFactory factory = new();
        private static IProxyClient? proxy;
        private static bool proxy_ok = false;

        /// <summary>
        /// Create a regular TcpClient or a proxied TcpClient according to the app Settings.
        /// </summary>
        /// <param name="host">Target host</param>
        /// <param name="port">Target port</param>
        /// <param name="login">True if the purpose is logging in to a Minecraft account</param>

        public static TcpClient NewTcpClient(string host, int port, bool login = false)
        {
            try
            {
                if (login ? Config.Enabled_Login : Config.Enabled_Ingame)
                {
                    ProxyType innerProxytype = ProxyType.Http;

                    switch (Config.Proxy_Type)
                    {
                        case Configs.ProxyType.HTTP: innerProxytype = ProxyType.Http; break;
                        case Configs.ProxyType.SOCKS4: innerProxytype = ProxyType.Socks4; break;
                        case Configs.ProxyType.SOCKS4a: innerProxytype = ProxyType.Socks4a; break;
                        case Configs.ProxyType.SOCKS5: innerProxytype = ProxyType.Socks5; break;
                    }

                    if (!string.IsNullOrWhiteSpace(Config.Username) && !string.IsNullOrWhiteSpace(Config.Password))
                        proxy = factory.CreateProxyClient(innerProxytype, Config.Server.Host, Config.Server.Port, Config.Username, Config.Password);
                    else
                        proxy = factory.CreateProxyClient(innerProxytype, Config.Server.Host, Config.Server.Port);

                    if (!proxy_ok)
                    {
                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.proxy_connected, Config.Server.Host, Config.Server.Port));
                        proxy_ok = true;
                    }

                    return proxy.CreateConnection(host, port);
                }
                else return new TcpClient(host, port);
            }
            catch (ProxyException e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.Message);
                proxy = null;
                throw new SocketException((int)SocketError.HostUnreachable);
            }
        }
    }
}
