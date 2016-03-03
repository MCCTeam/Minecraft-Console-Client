using MinecraftClient.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MinecraftClient.Cache
{
    /// <summary>
    /// Handle sessions caching and storage.
    /// </summary>
     
    public static class SessionCache
    {
        const string filename = "cache.bin";
        private static Dictionary<string, SessionToken> sessions = new Dictionary<string, SessionToken>();
        private static FileSystemWatcher cachemonitor = new FileSystemWatcher();

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

            if (Settings.CacheType == CacheType.DISK)
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
            cachemonitor.Filter = filename;
            cachemonitor.NotifyFilter = NotifyFilters.LastWrite;
            cachemonitor.Changed += new FileSystemEventHandler(OnChanged);
            cachemonitor.EnableRaisingEvents = true;

            return LoadFromDisk();
        }

        /// <summary>
        /// Reloads cache on external cache file change.
        /// </summary>
        /// <param name="source">Sender</param>
        /// <param name="e">Event data</param>

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            LoadFromDisk();
        }

        /// <summary>
        /// Reads cache file and loads SessionTokens into SessionCache.
        /// </summary>
        /// <returns>True if data is successfully loaded</returns>

        private static bool LoadFromDisk()
        {
            if (File.Exists(filename))
            {
                try
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
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
            bool fileexists = File.Exists(filename);

            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
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

        }
    }
}
