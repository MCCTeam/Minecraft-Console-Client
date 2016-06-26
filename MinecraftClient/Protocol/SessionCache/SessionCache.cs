using MinecraftClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;

namespace MinecraftClient.Protocol.SessionCache
{
    /// <summary>
    /// Handle sessions caching and storage.
    /// </summary>
     
    public static class SessionCache
    {
        private const string SessionCacheFile = "SessionCache.db";

        private static Dictionary<string, SessionToken> sessions = new Dictionary<string, SessionToken>();
        private static FileSystemWatcher cachemonitor = new FileSystemWatcher();
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
            cachemonitor.Path = AppDomain.CurrentDomain.BaseDirectory;
            cachemonitor.IncludeSubdirectories = false;
            cachemonitor.Filter = SessionCacheFile;
            cachemonitor.NotifyFilter = NotifyFilters.LastWrite;
            cachemonitor.Changed += new FileSystemEventHandler(OnChanged);
            cachemonitor.EnableRaisingEvents = true;

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
            if (File.Exists(SessionCacheFile))
            {
                try
                {
                    using (FileStream fs = new FileStream(SessionCacheFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        sessions = (Dictionary<string, SessionToken>)formatter.Deserialize(fs);
                        return true;
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Error reading cached sessions from disk: " + ex.Message);
                }
                catch (SerializationException)
                {
                    Console.WriteLine("Malformed sessions from cache file ");
                }
            }
            return false;
        }

        /// <summary>
        /// Saves SessionToken's from SessionCache into cache file.
        /// </summary>

        private static void SaveToDisk()
        {
            bool fileexists = File.Exists(SessionCacheFile);
            IOException lastEx = null;
            int attempt = 1;

            while (attempt < 4)
            {
                try
                {
                    using (FileStream fs = new FileStream(SessionCacheFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        cachemonitor.EnableRaisingEvents = false;

                        // delete existing file contents
                        if (fileexists)
                        {
                            fs.SetLength(0);
                            fs.Flush();
                        }

                        formatter.Serialize(fs, sessions);
                        cachemonitor.EnableRaisingEvents = true;
                    }
                    return;
                }
                catch (IOException ex)
                {
                    lastEx = ex;
                    attempt++;
                    System.Threading.Thread.Sleep(new Random().Next(150, 350) * attempt); //CSMA/CD :)
                }
            }

            Console.WriteLine("Error writing cached sessions to disk" + (lastEx != null ? ": " + lastEx.Message : ""));
        }
    }
}
