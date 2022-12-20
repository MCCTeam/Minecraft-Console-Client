using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Scripting;
using static MinecraftClient.Settings;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.AdvancedConfig;

namespace MinecraftClient.Protocol.Session
{
    /// <summary>
    /// Handle sessions caching and storage.
    /// </summary>
    public static partial class SessionCache
    {
        public class Cache
        {
            [JsonInclude]
            public Dictionary<string, SessionToken> SessionTokens = new();

            [JsonInclude]
            public Dictionary<string, PlayerKeyPair> ProfileKeys = new();

            [JsonInclude]
            public Dictionary<string, ServerInfo> ServerKeys = new();

            public record ServerInfo
            {
                public ServerInfo(string serverIDhash, byte[] serverPublicKey)
                {
                    ServerIDhash = serverIDhash;
                    ServerPublicKey = serverPublicKey;
                }

                public string? ServerIDhash { init; get; }
                public byte[]? ServerPublicKey { init; get; }
            }
        }

        private static Cache cache = new();

        private const string SessionCacheFileJson = "SessionCache.json";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };


        public static async Task ReadCacheSessionAsync()
        {
            if (File.Exists(SessionCacheFileJson))
            {
                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted(string.Format(Translations.cache_loading_session, SessionCacheFileJson));

                FileStream fileStream = File.OpenRead(SessionCacheFileJson);

                try
                {
                    Cache? diskCache = (Cache?)await JsonSerializer.DeserializeAsync(fileStream, typeof(Cache), JsonOptions);

                    if (diskCache != null)
                    {
                        cache = diskCache;

                        if (Config.Logging.DebugMessages)
                            ConsoleIO.WriteLineFormatted(string.Format(Translations.cache_loaded, cache.SessionTokens.Count, cache.ProfileKeys.Count));
                    }
                }
                catch (IOException e)
                {
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.cache_read_fail_plain, e.Message));
                }
                catch (JsonException e)
                {
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.cache_read_fail_plain, e.Message));
                }

                await fileStream.DisposeAsync();
            }
        }

        /// <summary>
        /// Retrieve whether SessionCache contains a session for the given login.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <returns>TRUE if session is available</returns>
        public static bool ContainsSession(string login)
        {
            return cache.SessionTokens.ContainsKey(login);
        }

        /// <summary>
        /// Retrieve a session token for the given login.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <returns>SessionToken for given login</returns>
        public static Tuple<SessionToken?, PlayerKeyPair?> GetSession(string login)
        {
            cache.SessionTokens.TryGetValue(login, out SessionToken? sessionToken);
            cache.ProfileKeys.TryGetValue(login, out PlayerKeyPair? playerKeyPair);
            return new(sessionToken, playerKeyPair);
        }

        public static Cache.ServerInfo? GetServerInfo(string server)
        {
            if (cache.ServerKeys.TryGetValue(server, out Cache.ServerInfo? info))
                return info;
            else
                return null;
        }

        /// <summary>
        /// Store a session and save it to disk if required.
        /// </summary>
        /// <param name="login">User login used with Minecraft.net</param>
        /// <param name="newSession">User session token used with Minecraft.net</param>
        public static async Task StoreSessionAsync(string login, SessionToken? sessionToken, PlayerKeyPair? profileKey)
        {
            if (sessionToken != null)
                cache.SessionTokens[login] = sessionToken;
            if (profileKey != null)
                cache.ProfileKeys[login] = profileKey;

            if (Config.Main.Advanced.SessionCache == CacheType.disk)
                await SaveToDisk();
        }

        public static void StoreServerInfo(string server, string ServerIDhash, byte[] ServerPublicKey)
        {
            cache.ServerKeys[server] = new(ServerIDhash, ServerPublicKey);
        }

        /// <summary>
        /// Saves SessionToken's from SessionCache into cache file.
        /// </summary>
        private static async Task SaveToDisk()
        {
            if (Config.Logging.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8" + Translations.cache_saving, acceptnewlines: true);

            foreach ((string login, SessionToken session) in cache.SessionTokens)
            {
                if (!GetJwtRegex().IsMatch(session.ID))
                    cache.SessionTokens.Remove(login);
                else if (!ChatBot.IsValidName(session.PlayerName))
                    cache.SessionTokens.Remove(login);
                else if (!Guid.TryParseExact(session.PlayerID, "N", out _))
                    cache.SessionTokens.Remove(login);
                else if (!Guid.TryParseExact(session.ClientID, "N", out _))
                    cache.SessionTokens.Remove(login);
                // No validation on refresh token because it is custom format token (not Jwt)
            }

            foreach ((string login, PlayerKeyPair profileKey) in cache.ProfileKeys)
            {
                if (profileKey.NeedRefresh())
                    cache.ProfileKeys.Remove(login);
            }

            try
            {
                FileStream fileStream = File.Open(SessionCacheFileJson, FileMode.Create);

                await fileStream.WriteAsync(Encoding.UTF8.GetBytes($"/* Generated by MCC v{Program.Version} - Keep it secret & Edit at own risk! */{Environment.NewLine}"));

                await JsonSerializer.SerializeAsync(fileStream, cache, typeof(Cache), JsonOptions);

                await fileStream.FlushAsync();
                fileStream.Close();
                await fileStream.DisposeAsync();
            }
            catch (IOException e)
            {
                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.cache_save_fail, e.Message));
            }
            catch (JsonException e)
            {
                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.cache_save_fail, e.Message));
            }
        }

        [GeneratedRegex("^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+$", RegexOptions.Compiled)]
        private static partial Regex GetJwtRegex();
    }
}
