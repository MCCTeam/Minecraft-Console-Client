using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using MinecraftClient.Protocol.Session;

namespace MinecraftClient.Protocol.Keys
{
    /// <summary>
    /// Handle keys caching and storage.
    /// </summary>
    public static class KeysCache
    {
        private const string KeysCacheFilePlaintext = "KeysCache.ini";

        private static FileMonitor cachemonitor;
        private static Dictionary<string, KeysInfo> keys = new Dictionary<string, KeysInfo>();
        private static Timer updatetimer = new Timer(100);
        private static List<KeyValuePair<string, KeysInfo>> pendingadds = new List<KeyValuePair<string, KeysInfo>>();
        private static BinaryFormatter formatter = new BinaryFormatter();

        /// <summary>
        /// Retrieve whether KeysCache contains a keys for the given login.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <returns>TRUE if keys are available</returns>
        public static bool Contains(string login)
        {
            return keys.ContainsKey(login);
        }

        /// <summary>
        /// Store keys and save it to disk if required.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <param name="keys">User keys</param>
        public static void Store(string login, KeysInfo keysInfo)
        {
            if (Contains(login))
            {
                keys[login] = keysInfo;
            }
            else
            {
                keys.Add(login, keysInfo);
            }

            if (Settings.SessionCaching == CacheType.Disk && updatetimer.Enabled == true)
            {
                pendingadds.Add(new KeyValuePair<string, KeysInfo>(login, keysInfo));
            }
            else if (Settings.SessionCaching == CacheType.Disk)
            {
                SaveToDisk();
            }
        }

        /// <summary>
        /// Retrieve keys for the given login.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <returns>KeysInfo for given login</returns>
        public static KeysInfo Get(string login)
        {
            return keys[login];
        }

        /// <summary>
        /// Initialize cache monitoring to keep cache updated with external changes.
        /// </summary>
        /// <returns>TRUE if keys are seeded from file</returns>
        public static bool InitializeDiskCache()
        {
            cachemonitor = new FileMonitor(AppDomain.CurrentDomain.BaseDirectory, KeysCacheFilePlaintext, new FileSystemEventHandler(OnChanged));
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
        /// Called after timer elapsed. Reads disk cache and adds new/modified keys back.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event data</param>
        private static void HandlePending(object sender, ElapsedEventArgs e)
        {
            updatetimer.Stop();
            LoadFromDisk();

            foreach (KeyValuePair<string, KeysInfo> pending in pendingadds.ToArray())
            {
                Store(pending.Key, pending.Value);
                pendingadds.Remove(pending);
            }
        }

        /// <summary>
        /// Reads cache file and loads KeysInfos into KeysCache.
        /// </summary>
        /// <returns>True if data is successfully loaded</returns>
        private static bool LoadFromDisk()
        {
            //User-editable keys cache file in text format
            if (File.Exists(KeysCacheFilePlaintext))
            {
                if (Settings.DebugMessages)
                    ConsoleIO.WriteLineFormatted(Translations.Get("cache.loading_keys", KeysCacheFilePlaintext));

                try
                {
                    foreach (string line in FileMonitor.ReadAllLinesWithRetries(KeysCacheFilePlaintext))
                    {
                        if (!line.Trim().StartsWith("#"))
                        {
                            string[] keyValue = line.Split('=');
                            if (keyValue.Length == 2)
                            {
                                try
                                {
                                    string login = keyValue[0].ToLower();
                                    KeysInfo keysInfo = KeysInfo.FromString(keyValue[1]);
                                    keys[login] = keysInfo;
                                }
                                catch (InvalidDataException e)
                                {
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted(Translations.Get("cache.ignore_string", keyValue[1], e.Message));
                                }
                            }
                            else if (Settings.DebugMessages)
                            {
                                ConsoleIO.WriteLineFormatted(Translations.Get("cache.ignore_line", line));
                            }
                        }
                    }
                }
                catch (IOException e)
                {
                    ConsoleIO.WriteLineFormatted(Translations.Get("cache.read_fail_plain", e.Message));
                }
            }

            return keys.Count > 0;
        }

        /// <summary>
        /// Saves KeysInfo's from KeysCache into cache file.
        /// </summary>
        private static void SaveToDisk()
        {
            if (Settings.DebugMessages)
                Translations.WriteLineFormatted("cache.saving");

            List<string> KeysCacheLines = new List<string>();
            KeysCacheLines.Add("# Generated by MCC v" + Program.Version + " - DO NOT EDIT!");
            KeysCacheLines.Add("# Login=PrivateKey,PublicKey,PublicKeySignature,PublicKeySignatureV2,ExpiresAt,RefreshAfter");

            foreach (KeyValuePair<string, KeysInfo> entry in keys)
                KeysCacheLines.Add(entry.Key + '=' + entry.Value.ToString());

            try
            {
                FileMonitor.WriteAllLinesWithRetries(KeysCacheFilePlaintext, KeysCacheLines);
            }
            catch (IOException e)
            {
                ConsoleIO.WriteLineFormatted(Translations.Get("cache.save_fail", e.Message));
            }
        }
    }
}
