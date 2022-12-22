using System;
using System.Net;
using System.Net.Http;
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
            [NonSerialized] // Compatible with old settings.
            public bool? Enabled_Login = false, Enabled_Ingame = false, Enabled_Update = false;

            [TomlInlineComment("$Proxy.Ingame_Proxy$")]
            public ProxyPreferenceType Ingame_Proxy = ProxyPreferenceType.disable;

            [TomlInlineComment("$Proxy.Login_Proxy$")]
            public ProxyPreferenceType Login_Proxy = ProxyPreferenceType.follow_system;

            [TomlInlineComment("$Proxy.MCC_Update_Proxy$")]
            public ProxyPreferenceType MCC_Update_Proxy = ProxyPreferenceType.follow_system;

            [TomlInlineComment("$Proxy.Server$")]
            public ProxyInfoConfig Server = new("0.0.0.0", 8080);

            [TomlInlineComment("$Proxy.Proxy_Type$")]
            public ProxyType Proxy_Type = ProxyType.HTTP;

            [TomlInlineComment("$Proxy.Username$")]
            public string Username = string.Empty;

            [TomlInlineComment("$Proxy.Password$")]
            public string Password = string.Empty;

            public void OnSettingUpdate()
            {
                { // Compatible with old settings.
                    if (Enabled_Login.HasValue && Enabled_Login.Value)
                        Login_Proxy = ProxyPreferenceType.custom;
                    if (Enabled_Ingame.HasValue && Enabled_Ingame.Value)
                        Ingame_Proxy = ProxyPreferenceType.custom;
                    if (Enabled_Update.HasValue && Enabled_Update.Value)
                        MCC_Update_Proxy = ProxyPreferenceType.custom;
                }
            }

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

            public enum ProxyPreferenceType { custom, follow_system, disable };
        }

        public enum ClientType { Ingame, Login, Update };

        private static readonly ProxyClientFactory factory = new();
        private static IProxyClient? proxy;
        private static bool proxy_ok = false;

        /// <summary>
        /// Create a regular TcpClient or a proxied TcpClient according to the app Settings.
        /// </summary>
        /// <param name="host">Target host</param>
        /// <param name="port">Target port</param>
        /// <param name="login">True if the purpose is logging in to a Minecraft account</param>

        public static TcpClient NewTcpClient(string host, int port, ClientType clientType)
        {
            if (clientType == ClientType.Update)
                throw new NotSupportedException();
            try
            {
                Configs.ProxyPreferenceType proxyPreference = clientType == ClientType.Ingame ? Config.Ingame_Proxy : Config.Login_Proxy;
                if (proxyPreference == Configs.ProxyPreferenceType.custom)
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
                else if (proxyPreference == Configs.ProxyPreferenceType.follow_system)
                {
                    Uri? webProxy = WebRequest.GetSystemWebProxy().GetProxy(new("http://" + host));
                    if (webProxy != null)
                    {
                        proxy = factory.CreateProxyClient(ProxyType.Http, webProxy.Host, webProxy.Port);

                        if (!proxy_ok)
                        {
                            ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.proxy_connected, webProxy.Host, webProxy.Port));
                            proxy_ok = true;
                        }

                        return proxy.CreateConnection(host, port);
                    }
                    else
                    {
                        return new TcpClient(host, port);
                    }
                }
                else
                {
                    return new TcpClient(host, port);
                }
            }
            catch (ProxyException e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.Message);
                proxy = null;
                throw new SocketException((int)SocketError.HostUnreachable);
            }
        }

        public static HttpClient NewHttpClient(ClientType clientType, HttpClientHandler? httpClientHandler = null)
        {
            if (clientType == ClientType.Ingame)
                throw new NotSupportedException();

            httpClientHandler ??= new();
            AddProxySettings(clientType, ref httpClientHandler);
            return new HttpClient(httpClientHandler);
        }

        public static void AddProxySettings(ClientType clientType, ref HttpClientHandler httpClientHandler)
        {
            if (clientType == ClientType.Ingame)
                throw new NotSupportedException();

            Configs.ProxyPreferenceType proxyPreference = clientType == ClientType.Login ? Config.Login_Proxy : Config.MCC_Update_Proxy;

            if (proxyPreference == Configs.ProxyPreferenceType.custom)
            {
                httpClientHandler ??= new();

                string proxyAddress;
                if (!string.IsNullOrWhiteSpace(Settings.Config.Proxy.Username) && !string.IsNullOrWhiteSpace(Settings.Config.Proxy.Password))
                    proxyAddress = string.Format("{0}://{3}:{4}@{1}:{2}",
                        Settings.Config.Proxy.Proxy_Type.ToString().ToLower(),
                        Settings.Config.Proxy.Server.Host,
                        Settings.Config.Proxy.Server.Port,
                        Settings.Config.Proxy.Username,
                        Settings.Config.Proxy.Password);
                else
                    proxyAddress = string.Format("{0}://{1}:{2}",
                        Settings.Config.Proxy.Proxy_Type.ToString().ToLower(),
                        Settings.Config.Proxy.Server.Host, Settings.Config.Proxy.Server.Port);

                httpClientHandler.Proxy = new WebProxy(proxyAddress, true);
                httpClientHandler.UseProxy = true;
            }
            else if (proxyPreference == Configs.ProxyPreferenceType.follow_system)
            {
                httpClientHandler.Proxy = WebRequest.GetSystemWebProxy();
                httpClientHandler.UseProxy = true;
            }
            else if (proxyPreference == Configs.ProxyPreferenceType.disable)
            {
                httpClientHandler ??= new();
                httpClientHandler.UseProxy = false;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
