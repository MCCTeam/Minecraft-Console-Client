using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Starksoft.Net.Proxy;

namespace MinecraftClient.Proxy
{
    /// <summary>
    /// Automatically handle proxies according to the app Settings.
    /// Note: Underlying proxy handling is taken from Starksoft, LLC's Biko Library.
    /// This library is open source and provided under the MIT license. More info at biko.codeplex.com.
    /// </summary>

    public static class ProxyHandler
    {
        public enum Type { HTTP, SOCKS4, SOCKS4a, SOCKS5 };

        private static ProxyClientFactory factory = new ProxyClientFactory();
        private static IProxyClient proxy;
        private static bool proxy_ok = false;

        /// <summary>
        /// Create a regular TcpClient or a proxied TcpClient according to the app Settings.
        /// </summary>
        /// <param name="host">Target host</param>
        /// <param name="port">Target port</param>
        /// <param name="login">True if the purpose is logging in to a Minecraft account</param>

        public static TcpClient newTcpClient(string host, int port, bool login = false)
        {
            try
            {
                if (login ? Settings.ProxyEnabledLogin : Settings.ProxyEnabledIngame)
                {
                    ProxyType innerProxytype = ProxyType.Http;

                    switch (Settings.proxyType)
                    {
                        case Type.HTTP: innerProxytype = ProxyType.Http; break;
                        case Type.SOCKS4: innerProxytype = ProxyType.Socks4; break;
                        case Type.SOCKS4a: innerProxytype = ProxyType.Socks4a; break;
                        case Type.SOCKS5: innerProxytype = ProxyType.Socks5; break;
                    }

                    if (Settings.ProxyUsername != "" && Settings.ProxyPassword != "")
                    {
                        proxy = factory.CreateProxyClient(innerProxytype, Settings.ProxyHost, Settings.ProxyPort, Settings.ProxyUsername, Settings.ProxyPassword);
                    }
                    else proxy = factory.CreateProxyClient(innerProxytype, Settings.ProxyHost, Settings.ProxyPort);

                    if (!proxy_ok)
                    {
                        ConsoleIO.WriteLineFormatted("§8Connected to proxy " + Settings.ProxyHost + ':' + Settings.ProxyPort);
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
