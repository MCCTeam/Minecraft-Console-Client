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
        private const string KeysCacheFilePlaintext = "ProfileKeyCache.ini";

        private static FileMonitor? cachemonitor;
        private static readonly Dictionary<string, PlayerKeyPair> keys = new();
        private static readonly Timer updatetimer = new(100);
        private static readonly List<KeyValuePair<string, PlayerKeyPair>> pendingadds = new();
        private static readonly BinaryFormatter formatter = new();

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
        /// <param name="playerKeyPair">User keys</param>
        public static void Store(string login, PlayerKeyPair playerKeyPair)
        {
            if (Contains(login))
            {
                keys[login] = playerKeyPair;
            }
            else
            {
                keys.Add(login, playerKeyPair);
            }

            if (Settings.ProfileKeyCaching == CacheType.Disk && updatetimer.Enabled == true)
            {
                pendingadds.Add(new KeyValuePair<string, PlayerKeyPair>(login, playerKeyPair));
            }
            else if (Settings.ProfileKeyCaching == CacheType.Disk)
            {
                SaveToDisk();
            }
        }

        /// <summary>
        /// Retrieve keys for the given login.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <returns>PlayerKeyPair for given login</returns>
        public static PlayerKeyPair Get(string login)
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
        private static void HandlePending(object? sender, ElapsedEventArgs e)
        {
            updatetimer.Stop();
            LoadFromDisk();

            foreach (KeyValuePair<string, PlayerKeyPair> pending in pendingadds.ToArray())
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
                        if (!line.TrimStart().StartsWith("#"))
                        {

                            int separatorIdx = line.IndexOf('=');
                            if (separatorIdx >= 1 && line.Length > separatorIdx + 1)
                            {
                                string login = line[..separatorIdx];
                                string value = line[(separatorIdx + 1)..];
                                try
                                {
                                    PlayerKeyPair playerKeyPair = PlayerKeyPair.FromString(value);
                                    keys[login] = playerKeyPair;
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted(Translations.Get("cache.loaded_keys", playerKeyPair.ExpiresAt.ToString()));
                                }
                                catch (InvalidDataException e)
                                {
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted(Translations.Get("cache.ignore_string_keys", value, e.Message));
                                }
                                catch (FormatException e)
                                {
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted(Translations.Get("cache.ignore_string_keys", value, e.Message));
                                }
                                catch (ArgumentNullException e)
                                {
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted(Translations.Get("cache.ignore_string_keys", value, e.Message));

                                }
                            }
                            else if (Settings.DebugMessages)
                            {
                                ConsoleIO.WriteLineFormatted(Translations.Get("cache.ignore_line_keys", line));
                            }
                        }
                    }
                }
                catch (IOException e)
                {
                    ConsoleIO.WriteLineFormatted(Translations.Get("cache.read_fail_plain_keys", e.Message));
                }
            }

            return keys.Count > 0;
        }

        /// <summary>
        /// Saves player's keypair from KeysCache into cache file.
        /// </summary>
        private static void SaveToDisk()
        {
            if (Settings.DebugMessages)
                Translations.WriteLineFormatted("cache.saving_keys");

            List<string> KeysCacheLines = new()
            {
                "# Generated by MCC v" + Program.Version + " - Keep it secret & Edit at own risk!",
                "# ProfileKey=PublicKey(base64),PublicKeySignature(base64),PublicKeySignatureV2(base64),PrivateKey(base64),ExpiresAt,RefreshAfter"
            };
            foreach (KeyValuePair<string, PlayerKeyPair> entry in keys)
                KeysCacheLines.Add(entry.Key + '=' + entry.Value.ToString());

            try
            {
                FileMonitor.WriteAllLinesWithRetries(KeysCacheFilePlaintext, KeysCacheLines);
            }
            catch (IOException e)
            {
                ConsoleIO.WriteLineFormatted(Translations.Get("cache.save_fail_keys", e.Message));
            }
        }
    }
}
