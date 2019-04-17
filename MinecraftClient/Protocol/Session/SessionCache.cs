using MinecraftClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;

namespace MinecraftClient.Protocol.Session
{
    /// <summary>
    /// Handle sessions caching and storage.
    /// </summary>
    public static class SessionCache
    {
        private const string SessionCacheFilePlaintext = "SessionCache.ini";
        private const string SessionCacheFileSerialized = "SessionCache.db";
        private static readonly string SessionCacheFileMinecraft = String.Concat(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Path.DirectorySeparatorChar,
            ".minecraft",
            Path.DirectorySeparatorChar,
            "launcher_profiles.json"
        );

        private static SessionFileMonitor cachemonitor;
        private static Dictionary<string, SessionToken> sessions = new Dictionary<string, SessionToken>();
        private static Timer updatetimer = new Timer(100);
        private static List<KeyValuePair<string, SessionToken>> pendingadds = new List<KeyValuePair<string, SessionToken>>();
        private static BinaryFormatter formatter = new BinaryFormatter();

        /// <summary>
        /// Retrieve whether SessionCache contains a session for the given login.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <returns>TRUE if session is available</returns>
        public static bool Contains(string login)
        {
            return sessions.ContainsKey(login);
        }

        /// <summary>
        /// Store a session and save it to disk if required.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <param name="session">User session token used with Minecraft.net</param>
        public static void Store(string login, SessionToken session)
        {
            if (Contains(login))
            {
                sessions[login] = session;
            }
            else
            {
                sessions.Add(login, session);
            }

            if (Settings.SessionCaching == CacheType.Disk && updatetimer.Enabled == true)
            {
                pendingadds.Add(new KeyValuePair<string, SessionToken>(login, session));
            }
            else if (Settings.SessionCaching == CacheType.Disk)
            {
                SaveToDisk();
            }
        }

        /// <summary>
        /// Retrieve a session token for the given login.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <returns>SessionToken for given login</returns>
        public static SessionToken Get(string login)
        {
            return sessions[login];
        }

        /// <summary>
        /// Initialize cache monitoring to keep cache updated with external changes.
        /// </summary>
        /// <returns>TRUE if session tokens are seeded from file</returns>
        public static bool InitializeDiskCache()
        {
            cachemonitor = new SessionFileMonitor(AppDomain.CurrentDomain.BaseDirectory, SessionCacheFilePlaintext, new FileSystemEventHandler(OnChanged));
            updatetimer.Elapsed += HandlePending;
            return LoadFromDisk();
        }

        /// <summary>
        /// Reloads cache on external cache file change.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event data</param>
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            updatetimer.Stop();
            updatetimer.Start();
        }

        /// <summary>
        /// Called after timer elapsed. Reads disk cache and adds new/modified sessions back.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event data</param>
        private static void HandlePending(object sender, ElapsedEventArgs e)
        {
            updatetimer.Stop();
            LoadFromDisk();

            foreach(KeyValuePair<string, SessionToken> pending in pendingadds.ToArray())
            {
                Store(pending.Key, pending.Value);
                pendingadds.Remove(pending);
            }
        }

        /// <summary>
        /// Reads cache file and loads SessionTokens into SessionCache.
        /// </summary>
        /// <returns>True if data is successfully loaded</returns>
        private static bool LoadFromDisk()
        {
            //Grab sessions in the Minecraft directory
            if (File.Exists(SessionCacheFileMinecraft))
            {
                if (Settings.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8Loading Minecraft profiles: " + Path.GetFileName(SessionCacheFileMinecraft));
                Json.JSONData mcSession = new Json.JSONData(Json.JSONData.DataType.String);
                try
                {
                    mcSession = Json.ParseJson(File.ReadAllText(SessionCacheFileMinecraft));
                }
                catch (IOException) { /* Failed to read file from disk -- ignoring */ }
                if (mcSession.Type == Json.JSONData.DataType.Object
                    && mcSession.Properties.ContainsKey("clientToken")
                    && mcSession.Properties.ContainsKey("authenticationDatabase"))
                {
                    Guid temp;
                    string clientID = mcSession.Properties["clientToken"].StringValue.Replace("-", "");
                    Dictionary<string, Json.JSONData> sessionItems = mcSession.Properties["authenticationDatabase"].Properties;
                    foreach (string key in sessionItems.Keys)
                    {
                        if (Guid.TryParseExact(key, "N", out temp))
                        {
                            Dictionary<string, Json.JSONData> sessionItem = sessionItems[key].Properties;
                            if (sessionItem.ContainsKey("displayName")
                                && sessionItem.ContainsKey("accessToken")
                                && sessionItem.ContainsKey("username")
                                && sessionItem.ContainsKey("uuid"))
                            {
                                string login = sessionItem["username"].StringValue.ToLower();
                                try
                                {
                                    SessionToken session = SessionToken.FromString(String.Join(",",
                                        sessionItem["accessToken"].StringValue,
                                        sessionItem["displayName"].StringValue,
                                        sessionItem["uuid"].StringValue.Replace("-", ""),
                                        clientID
                                    ));
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Loaded session: " + login + ':' + session.ID);
                                    sessions[login] = session;
                                }
                                catch (InvalidDataException) { /* Not a valid session */ }
                            }
                        }
                    }
                }
            }

            //Serialized session cache file in binary format
            if (File.Exists(SessionCacheFileSerialized))
            {
                if (Settings.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8Converting session cache from disk: " + SessionCacheFileSerialized);

                try
                {
                    using (FileStream fs = new FileStream(SessionCacheFileSerialized, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        Dictionary<string, SessionToken> sessionsTemp = (Dictionary<string, SessionToken>)formatter.Deserialize(fs);
                        foreach (KeyValuePair<string, SessionToken> item in sessionsTemp)
                        {
                            if (Settings.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8Loaded session: " + item.Key + ':' + item.Value.ID);
                            sessions[item.Key] = item.Value;
                        }
                    }
                }
                catch (IOException ex)
                {
                    ConsoleIO.WriteLineFormatted("§8Failed to read session cache from disk: " + ex.Message);
                }
                catch (SerializationException ex2)
                {
                    ConsoleIO.WriteLineFormatted("§8Got malformed data while reading session cache from disk: " + ex2.Message);
                }
            }

            //User-editable session cache file in text format
            if (File.Exists(SessionCacheFilePlaintext))
            {
                if (Settings.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8Loading session cache from disk: " + SessionCacheFilePlaintext);

                foreach (string line in File.ReadAllLines(SessionCacheFilePlaintext))
                {
                    if (!line.Trim().StartsWith("#"))
                    {
                        string[] keyValue = line.Split('=');
                        if (keyValue.Length == 2)
                        {
                            try
                            {
                                string login = keyValue[0].ToLower();
                                SessionToken session = SessionToken.FromString(keyValue[1]);
                                if (Settings.DebugMessages)
                                    ConsoleIO.WriteLineFormatted("§8Loaded session: " + login + ':' + session.ID);
                                sessions[login] = session;
                            }
                            catch (InvalidDataException e)
                            {
                                if (Settings.DebugMessages)
                                    ConsoleIO.WriteLineFormatted("§8Ignoring session token string '" + keyValue[1] + "': " + e.Message);
                            }
                        }
                        else if (Settings.DebugMessages)
                        {
                            ConsoleIO.WriteLineFormatted("§8Ignoring invalid session token line: " + line);
                        }
                    }
                }
            }

            return sessions.Count > 0;
        }

        /// <summary>
        /// Saves SessionToken's from SessionCache into cache file.
        /// </summary>
        private static void SaveToDisk()
        {
            if (Settings.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8Saving session cache to disk");

            bool fileexists = File.Exists(SessionCacheFilePlaintext);
            IOException lastEx = null;
            int attempt = 1;

            while (attempt < 4)
            {
                try
                {
                    List<string> sessionCacheLines = new List<string>();
                    sessionCacheLines.Add("# Generated by MCC v" + Program.Version + " - Edit at own risk!");
                    sessionCacheLines.Add("# Login=SessionID,PlayerName,UUID,ClientID");
                    foreach (KeyValuePair<string, SessionToken> entry in sessions)
                        sessionCacheLines.Add(entry.Key + '=' + entry.Value.ToString());
                    File.WriteAllLines(SessionCacheFilePlaintext, sessionCacheLines);
                    //if (File.Exists(SessionCacheFileSerialized))
                    //    File.Delete(SessionCacheFileSerialized);
                    return;
                }
                catch (IOException ex)
                {
                    lastEx = ex;
                    attempt++;
                    System.Threading.Thread.Sleep(new Random().Next(150, 350) * attempt); //CSMA/CD :)
                }
            }

            ConsoleIO.WriteLineFormatted("§8Failed to write session cache to disk" + (lastEx != null ? ": " + lastEx.Message : ""));
        }
    }
}
