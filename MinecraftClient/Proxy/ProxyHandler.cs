using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Starksoft.Net.Proxy;
using System.IO;
using System.Text.RegularExpressions;

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
            if (Settings.ProxySwitcher)
            {
                if (!File.Exists("ProxySettings.ini"))
                {
                    Dictionary<string, Dictionary<string, string>> Content = new Dictionary<string, Dictionary<string, string>>();
                    Dictionary<string, string> Content2 = new Dictionary<string, string>();
                    Content2.Add("GoodProxy", "");
                    Content2.Add("Proxy", "");
                    Content.Add("Settings", Content2);
                    INIFile.WriteFile("ProxySettings.ini", Content);
                }

                Dictionary<string, Dictionary<string, string>> Content3 = INIFile.ParseFile("ProxySettings.ini");
                string lastgoodproxy = "";
                string proxies = "";
                if (Content3.ContainsKey("settings"))
                {
                    Dictionary<string, string> Content4 = Content3["settings"];
                    if (Content4.ContainsKey("goodproxy"))
                    {
                        lastgoodproxy = Content4["goodproxy"];
                    }
                    if (Content4.ContainsKey("proxy"))
                    {
                        proxies = Content4["proxy"];
                    }
                }
                string[] pp = proxies.Split(',');
                foreach (string proxyst in pp)
                {
                    if (proxyst != "")
                    {
                        string proxyhost = "";
                        int proxyport = 80;
                        if (lastgoodproxy == "")
                        {
                            proxyhost = Regex.Match(proxyst, "(.*):(.*):(.*)").Groups[1].Value;
                            proxyport = int.Parse(Regex.Match(proxyst, "(.*):(.*):(.*)").Groups[2].Value);
                            Enum.TryParse(Regex.Match(proxyst, "(.*):(.*):(.*)").Groups[3].Value, out Settings.proxyType);
                        }
                        else
                        {
                            proxyhost = Regex.Match(lastgoodproxy, "(.*):(.*):(.*)").Groups[1].Value;
                            proxyport = int.Parse(Regex.Match(lastgoodproxy, "(.*):(.*):(.*)").Groups[2].Value);
                            Enum.TryParse(Regex.Match(lastgoodproxy, "(.*):(.*):(.*)").Groups[3].Value, out Settings.proxyType);
                        }
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
                                proxy = factory.CreateProxyClient(innerProxytype, proxyhost, proxyport);

                                if (!proxy_ok)
                                {
                                    ConsoleIO.WriteLineFormatted("§8Connected to proxy " + proxyhost + ':' + proxyport);
                                    proxy_ok = true;
                                }
                                Dictionary<string, Dictionary<string, string>> Content = new Dictionary<string, Dictionary<string, string>>();
                                Dictionary<string, string> Content2 = new Dictionary<string, string>();
                                Content2.Add("GoodProxy", proxyhost + ":" + proxyport + ":" + Regex.Match(proxyst, "(.*):(.*):(.*)").Groups[3].Value);
                                Content2.Add("Proxy", proxies);
                                Content.Add("Settings", Content2);
                                INIFile.WriteFile("ProxySettings.ini", Content);
                                return proxy.CreateConnection(host, port);
                            }
                            else return new TcpClient(host, port);
                        }
                        catch (ProxyException e)
                        {
                            Dictionary<string, Dictionary<string, string>> Content = new Dictionary<string, Dictionary<string, string>>();
                            Dictionary<string, string> Content2 = new Dictionary<string, string>();
                            Content2.Add("GoodProxy", "");
                            Content2.Add("Proxy", proxies);
                            Content.Add("Settings", Content2);
                            INIFile.WriteFile("ProxySettings.ini", Content);
                            ConsoleIO.WriteLineFormatted("§8" + e.Message);
                            proxy = null;
                            proxy_ok = false;
                        }
                    }
                }
                return new TcpClient(host, port);
            }
            else
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
}
