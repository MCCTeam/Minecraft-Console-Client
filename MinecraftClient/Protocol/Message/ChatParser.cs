using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MinecraftClient.Settings;

namespace MinecraftClient.Protocol.Message
{
    /// <summary>
    /// This class parses JSON chat data from MC 1.6+ and returns the appropriate string to be printed.
    /// </summary>
    static class ChatParser
    {
        public enum MessageType
        {
            CHAT,
            SAY_COMMAND,
            MSG_COMMAND_INCOMING,
            MSG_COMMAND_OUTGOING,
            TEAM_MSG_COMMAND_INCOMING,
            TEAM_MSG_COMMAND_OUTGOING,
            EMOTE_COMMAND,
            RAW_MSG
        };

        public static Dictionary<int, MessageType>? ChatId2Type;

        // Used to store Chat Types in 1.20.6+
        public static void ReadChatType(Dictionary<int, string> data)
        {
            var chatTypeDictionary = ChatId2Type ?? new Dictionary<int, MessageType>();

            foreach (var (chatId, chatName) in data)
            {
                chatTypeDictionary[chatId] = chatName switch
                {
                    "minecraft:chat" => MessageType.CHAT,
                    "minecraft:emote_command" => MessageType.EMOTE_COMMAND,
                    "minecraft:msg_command_incoming" => MessageType.MSG_COMMAND_INCOMING,
                    "minecraft:msg_command_outgoing" => MessageType.MSG_COMMAND_OUTGOING,
                    "minecraft:say_command" => MessageType.SAY_COMMAND,
                    "minecraft:team_msg_command_incoming" => MessageType.TEAM_MSG_COMMAND_INCOMING,
                    "minecraft:team_msg_command_outgoing" => MessageType.TEAM_MSG_COMMAND_OUTGOING,
                    _ => MessageType.CHAT,
                };
            }

            ChatId2Type = chatTypeDictionary;
        }

        public static void ReadChatType(Dictionary<string, object> registryCodec)
        {
            Dictionary<int, MessageType> chatTypeDictionary = ChatId2Type ?? new();
            
            // Check if the chat type registry is in the correct format
            if (!registryCodec.ContainsKey("minecraft:chat_type")) {
                
                // If not, then we force the registry to be in the correct format
                if (registryCodec.ContainsKey("chat_type")) {
                    
                    foreach (var key in registryCodec.Keys.ToArray()) {
                        // Skip entries with a namespace already
                        if (key.Contains(':', StringComparison.OrdinalIgnoreCase)) continue;

                        // Assume all other entries are in the minecraft namespace
                        registryCodec["minecraft:" + key] = registryCodec[key];
                        registryCodec.Remove(key);
                    }
                }
            }
            
            var chatTypeListNbt = (object[])(((Dictionary<string, object>)registryCodec["minecraft:chat_type"])["value"]);
            foreach (var (chatName, chatId) in from Dictionary<string, object> chatTypeNbt in chatTypeListNbt
                     let chatName = (string)chatTypeNbt["name"]
                     let chatId = (int)chatTypeNbt["id"]
                     select (chatName, chatId))
            {
                chatTypeDictionary[chatId] = chatName switch
                {
                    "minecraft:chat" => MessageType.CHAT,
                    "minecraft:emote_command" => MessageType.EMOTE_COMMAND,
                    "minecraft:msg_command_incoming" => MessageType.MSG_COMMAND_INCOMING,
                    "minecraft:msg_command_outgoing" => MessageType.MSG_COMMAND_OUTGOING,
                    "minecraft:say_command" => MessageType.SAY_COMMAND,
                    "minecraft:team_msg_command_incoming" => MessageType.TEAM_MSG_COMMAND_INCOMING,
                    "minecraft:team_msg_command_outgoing" => MessageType.TEAM_MSG_COMMAND_OUTGOING,
                    _ => MessageType.CHAT,
                };
            }

            ChatId2Type = chatTypeDictionary;
        }

        /// <summary>
        /// The main function to convert text from MC 1.6+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="json">JSON serialized text</param>
        /// <param name="links">Optional container for links from JSON serialized text</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseText(string json, List<string>? links = null)
        {
            return JSONData2String(Json.ParseJson(json), "", links);
        }

        public static string ParseText(Dictionary<string, object> nbt)
        {
            return NbtToString(nbt);
        }

        /// <summary>
        /// The main function to convert text from MC 1.9+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="message">Message received</param>
        /// <param name="links">Optional container for links from JSON serialized text</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseSignedChat(ChatMessage message, List<string>? links = null)
        {
            string sender = message.isSenderJson ? ParseText(message.displayName!) : message.displayName!;
            string content;
            if (Config.Signature.ShowModifiedChat && message.unsignedContent is not null)
            {
                content = ParseText(message.unsignedContent!);
                if (string.IsNullOrEmpty(content))
                    content = message.unsignedContent!;
            }
            else
            {
                content = message.isJson ? ParseText(message.content) : message.content;
                if (string.IsNullOrEmpty(content))
                    content = message.content!;
            }

            string text;
            List<string> usingData = new();

            MessageType chatType;
            if (message.chatTypeId == -1)
                chatType = MessageType.RAW_MSG;
            else if (!ChatId2Type!.TryGetValue(message.chatTypeId, out chatType))
                chatType = MessageType.CHAT;
            switch (chatType)
            {
                case MessageType.CHAT:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.text", usingData);
                    break;
                case MessageType.SAY_COMMAND:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.announcement", usingData);
                    break;
                case MessageType.MSG_COMMAND_INCOMING:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("commands.message.display.incoming", usingData);
                    break;
                case MessageType.MSG_COMMAND_OUTGOING:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("commands.message.display.outgoing", usingData);
                    break;
                case MessageType.TEAM_MSG_COMMAND_INCOMING:
                    usingData.Add(message.teamName!);
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.team.text", usingData);
                    break;
                case MessageType.TEAM_MSG_COMMAND_OUTGOING:
                    usingData.Add(message.teamName!);
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.team.sent", usingData);
                    break;
                case MessageType.EMOTE_COMMAND:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.emote", usingData);
                    break;
                case MessageType.RAW_MSG:
                    text = content;
                    break;
                default:
                    goto case MessageType.CHAT;
            }

            return text;
        }

        /// <summary>
        /// Get the classic color tag corresponding to a color name
        /// </summary>
        /// <param name="colorname">Color Name</param>
        /// <returns>Color code</returns>
        private static string Color2tag(string colorname)
        {
            string lower = colorname.ToLower();

            if (lower.Length == 7 && lower[0] == '#' && IsHexColor(lower))
                return "§" + lower;

            return lower switch
            {
#pragma warning disable format // @formatter:off

           /*   MC 1.7+ Name   ||   MC 1.6 Name   ||   Classic tag   */
                "black"                           =>      "§0",
                "dark_blue"                       =>      "§1",
                "dark_green"                      =>      "§2",
                "dark_aqua"    or  "dark_cyan"    =>      "§3",
                "dark_red"                        =>      "§4",
                "dark_purple"  or  "dark_magenta" =>      "§5",
                "gold"         or  "dark_yellow"  =>      "§6",
                "gray"                            =>      "§7",
                "dark_gray"                       =>      "§8",
                "blue"                            =>      "§9",
                "green"                           =>      "§a",
                "aqua"         or  "cyan"         =>      "§b",
                "red"                             =>      "§c",
                "light_purple" or  "magenta"      =>      "§d",
                "yellow"                          =>      "§e",
                "white"                           =>      "§f",
                _                                 =>       "" ,

#pragma warning restore format // @formatter:on
            };
        }

        private static bool IsHexColor(string s)
        {
            for (int i = 1; i < s.Length; i++)
            {
                char c = s[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Specify whether translation rules have been loaded
        /// </summary>
        private static bool RulesInitialized = false;

        /// <summary>
        /// Set of translation rules for formatting text
        /// </summary>
        private static Dictionary<string, string> TranslationRules = new();

        private sealed class TranslationLayer(string identifier, Dictionary<string, string> translations)
        {
            public string Identifier { get; } = identifier;
            public Dictionary<string, string> Translations { get; } = translations;
        }

        private sealed class ResourcePackTranslationCacheEntry
        {
            public string CacheVersion { get; init; } = string.Empty;
            public string Language { get; init; } = string.Empty;
            public string SourceUrl { get; init; } = string.Empty;
            public string SourceHash { get; init; } = string.Empty;
            public Dictionary<string, string> Translations { get; init; } = [];
        }

        private const long MaxResourcePackDownloadBytes = 256L * 1024 * 1024;
        private const int ResourcePackDownloadBufferSize = 81920;
        private const string ResourcePackTranslationCacheVersion = "1";
        private const string ForgeModTranslationDirectory = "mods";

        private static readonly Regex ForgeModIdLineRegex =
            new(@"^\s*modId\s*=\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly List<TranslationLayer> ForgeModTranslationLayers = [];
        private static readonly List<TranslationLayer> ResourcePackTranslationLayers = [];
        private static readonly HttpClient ResourcePackHttpClient = new();

        /// <summary>
        /// Initialize translation rules.
        /// Necessary for properly printing some chat messages.
        /// </summary>
        public static void InitTranslations()
        {
            ForgeModTranslationLayers.Clear();
            ResourcePackTranslationLayers.Clear();

            if (!RulesInitialized)
            {
                InitRules();
                RulesInitialized = true;
            }
        }

        /// <summary>
        /// Internal rule initialization method. Looks for local rule file or download it from Mojang asset servers.
        /// </summary>
        private static void InitRules()
        {
            if (Config.Main.Advanced.Language == "en_us")
            {
                TranslationRules =
                    JsonSerializer.Deserialize<Dictionary<string, string>>(
                        (byte[])MinecraftAssets.ResourceManager.GetObject("en_us.json")!)!;
                return;
            }

            //Language file in a subfolder, depending on the language setting
            if (!Directory.Exists("lang"))
                Directory.CreateDirectory("lang");

            string languageFilePath = "lang" + Path.DirectorySeparatorChar + Config.Main.Advanced.Language + ".json";

            // Load the external dictionary of translation rules or display an error message
            if (File.Exists(languageFilePath))
            {
                try
                {
                    TranslationRules =
                        JsonSerializer.Deserialize<Dictionary<string, string>>(File.OpenRead(languageFilePath))!;
                }
                catch (IOException)
                {
                }
                catch (JsonException)
                {
                }
            }

            if (TranslationRules.TryGetValue("Version", out string? version) &&
                version == Settings.TranslationsFile_Version)
            {
                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted(Translations.chat_loaded, acceptnewlines: true);
                return;
            }

            // Try downloading language file from Mojang's servers?
            ConsoleIO.WriteLineFormatted(
                "§8" + string.Format(Translations.chat_download, Config.Main.Advanced.Language));
            HttpClient httpClient = new();
            try
            {
                Task<string> fetch_index = httpClient.GetStringAsync(TranslationsFile_Website_Index);
                fetch_index.Wait();
                Match match = Regex.Match(fetch_index.Result,
                    $"minecraft/lang/{Config.Main.Advanced.Language}.json" + @""":\s\{""hash"":\s""([\d\w]{40})""");
                fetch_index.Dispose();
                if (match.Success && match.Groups.Count == 2)
                {
                    string hash = match.Groups[1].Value;
                    string translation_file_location = TranslationsFile_Website_Download + '/' + hash[..2] + '/' + hash;
                    if (Config.Logging.DebugMessages)
                        ConsoleIO.WriteLineFormatted(
                            string.Format(Translations.chat_request, translation_file_location));

                    Task<Dictionary<string, string>?> fetckFileTask =
                        httpClient.GetFromJsonAsync<Dictionary<string, string>>(translation_file_location);
                    fetckFileTask.Wait();
                    if (fetckFileTask.Result is not null && fetckFileTask.Result.Count > 0)
                    {
                        TranslationRules = fetckFileTask.Result;
                        TranslationRules["Version"] = TranslationsFile_Version;
                        File.WriteAllText(languageFilePath,
                            JsonSerializer.Serialize(TranslationRules, typeof(Dictionary<string, string>)),
                            Encoding.UTF8);

                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.chat_done, languageFilePath));
                        return;
                    }

                    fetckFileTask.Dispose();
                }
                else
                {
                    ConsoleIO.WriteLineFormatted("§8" + Translations.chat_fail, acceptnewlines: true);
                }
            }
            catch (HttpRequestException)
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.chat_fail, acceptnewlines: true);
            }
            catch (IOException)
            {
                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.chat_save_fail, languageFilePath),
                    acceptnewlines: true);
            }
            catch (Exception e)
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.chat_fail, acceptnewlines: true);
                ConsoleIO.WriteLine(e.Message);
                if (Config.Logging.DebugMessages && !string.IsNullOrEmpty(e.StackTrace))
                    ConsoleIO.WriteLine(e.StackTrace);
            }
            finally
            {
                httpClient.Dispose();
            }

            TranslationRules =
                JsonSerializer.Deserialize<Dictionary<string, string>>(
                    (byte[])MinecraftAssets.ResourceManager.GetObject("en_us.json")!)!;
            ConsoleIO.WriteLine(Translations.chat_use_default);
        }

        public static string? TranslateString(string rulename)
        {
            return TryGetTranslationRule(rulename, out string? result) ? result : null;
        }

        public static void LoadResourcePackTranslations(string packIdentifier, string url, string hash)
        {
            ArgumentException.ThrowIfNullOrEmpty(packIdentifier);
            ArgumentException.ThrowIfNullOrEmpty(url);

            if (!Config.Main.Advanced.LoadResourcePackTranslations)
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? resourcePackUri)
                || resourcePackUri.Scheme is not "http" and not "https")
            {
                return;
            }

            string cacheFilePath = GetResourcePackTranslationCacheFilePath(resourcePackUri, hash);
            if (TryLoadCachedResourcePackTranslations(cacheFilePath, resourcePackUri, hash, out Dictionary<string, string>? cachedTranslations))
            {
                ReplaceResourcePackTranslations(packIdentifier, cachedTranslations);
                return;
            }

            string temporaryFilePath = Path.GetTempFileName();
            try
            {
                DownloadResourcePack(resourcePackUri, hash, temporaryFilePath);
                using FileStream resourcePackFile = File.OpenRead(temporaryFilePath);
                Dictionary<string, string> resourcePackTranslations = ExtractResourcePackTranslations(resourcePackFile);
                ReplaceResourcePackTranslations(packIdentifier, resourcePackTranslations);
                SaveCachedResourcePackTranslations(cacheFilePath, resourcePackUri, hash, resourcePackTranslations);
            }
            catch (HttpRequestException)
            {
            }
            catch (IOException)
            {
            }
            catch (InvalidDataException)
            {
            }
            catch (JsonException)
            {
            }
            finally
            {
                try
                {
                    File.Delete(temporaryFilePath);
                }
                catch (IOException)
                {
                }
            }
        }

        public static void RemoveResourcePackTranslations(string packIdentifier)
        {
            ResourcePackTranslationLayers.RemoveAll(layer =>
                layer.Identifier.Equals(packIdentifier, StringComparison.Ordinal));
        }

        public static void ClearResourcePackTranslations()
        {
            ResourcePackTranslationLayers.Clear();
        }

        public static void LoadForgeModTranslations(IEnumerable<string> modIds)
        {
            ArgumentNullException.ThrowIfNull(modIds);

            ForgeModTranslationLayers.Clear();

            if (!Config.Main.Advanced.LoadForgeModTranslations || !Directory.Exists(ForgeModTranslationDirectory))
                return;

            HashSet<string> requestedModIds = new(
                modIds
                    .Where(static modId => !string.IsNullOrWhiteSpace(modId))
                    .Select(static modId => NormalizeForgeModId(modId)),
                StringComparer.OrdinalIgnoreCase);

            if (requestedModIds.Count == 0)
                return;

            Dictionary<string, Dictionary<string, string>> translationsByModId =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (string modJarPath in Directory.EnumerateFiles(ForgeModTranslationDirectory, "*.jar")
                         .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    using FileStream modJarStream = File.OpenRead(modJarPath);
                    MergeForgeModTranslations(modJarStream, requestedModIds, translationsByModId);
                }
                catch (IOException)
                {
                }
                catch (InvalidDataException)
                {
                }
                catch (JsonException)
                {
                }
            }

            foreach (string modId in requestedModIds)
            {
                if (translationsByModId.TryGetValue(modId, out Dictionary<string, string>? translations)
                    && translations.Count > 0)
                {
                    ForgeModTranslationLayers.Add(new TranslationLayer(modId, translations));
                }
            }
        }

        /// <summary>
        /// Format text using a specific formatting rule.
        /// Example : * %s %s + ["ORelio", "is doing something"] = * ORelio is doing something
        /// </summary>
        /// <param name="rulename">Name of the rule, chosen by the server</param>
        /// <param name="using_data">Data to be used in the rule</param>
        /// <returns>Returns the formatted text according to the given data</returns>
        private static string TranslateString(string rulename, List<string> using_data)
        {
            if (!RulesInitialized)
            {
                InitRules();
                RulesInitialized = true;
            }

            if (TryGetTranslationRule(rulename, out string? rule))
            {
                int using_idx = 0;
                StringBuilder result = new();
                for (int i = 0; i < rule.Length; i++)
                {
                    if (rule[i] == '%' && i + 1 < rule.Length)
                    {
                        //Using string or int with %s or %d
                        if (rule[i + 1] == 's' || rule[i + 1] == 'd')
                        {
                            if (using_data.Count > using_idx)
                            {
                                result.Append(using_data[using_idx]);
                                using_idx++;
                                i += 1;
                                continue;
                            }
                        }

                        //Using specified string or int with %1$s, %2$s...
                        else if (char.IsDigit(rule[i + 1])
                                 && i + 3 < rule.Length && rule[i + 2] == '$'
                                 && (rule[i + 3] == 's' || rule[i + 3] == 'd'))
                        {
                            int specified_idx = rule[i + 1] - '1';
                            if (using_data.Count > specified_idx)
                            {
                                result.Append(using_data[specified_idx]);
                                using_idx++;
                                i += 3;
                                continue;
                            }
                        }
                    }

                    result.Append(rule[i]);
                }

                return result.ToString();
            }
            else return "[" + rulename + "] " + string.Join(" ", using_data);
        }

        private static bool TryGetTranslationRule(string rulename, [NotNullWhen(true)] out string? result)
        {
            for (int i = ResourcePackTranslationLayers.Count - 1; i >= 0; i--)
            {
                if (ResourcePackTranslationLayers[i].Translations.TryGetValue(rulename, out result))
                    return true;
            }

            for (int i = ForgeModTranslationLayers.Count - 1; i >= 0; i--)
            {
                if (ForgeModTranslationLayers[i].Translations.TryGetValue(rulename, out result))
                    return true;
            }

            return TranslationRules.TryGetValue(rulename, out result);
        }

        private static void DownloadResourcePack(Uri resourcePackUri, string hash, string temporaryFilePath)
        {
            using HttpResponseMessage response =
                ResourcePackHttpClient.GetAsync(resourcePackUri, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            using Stream resourcePackStream = response.Content.ReadAsStream();
            using FileStream temporaryFile = File.Create(temporaryFilePath);
            using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

            byte[] buffer = new byte[ResourcePackDownloadBufferSize];
            long totalBytes = 0;

            while (true)
            {
                int bytesRead = resourcePackStream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0)
                    break;

                totalBytes += bytesRead;
                if (totalBytes > MaxResourcePackDownloadBytes)
                    throw new InvalidDataException();

                temporaryFile.Write(buffer, 0, bytesRead);

                if (hash.Length == 40)
                    incrementalHash.AppendData(buffer, 0, bytesRead);
            }

            if (hash.Length == 40)
            {
                string downloadedHash = Convert.ToHexString(incrementalHash.GetHashAndReset());
                if (!downloadedHash.Equals(hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Resource pack hash mismatch for {resourcePackUri}. Expected {hash}, got {downloadedHash}.");
            }
        }

        private static Dictionary<string, string> ExtractResourcePackTranslations(Stream resourcePackStream)
        {
            var mergedTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
            var selectedLanguageTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
            string selectedLanguage = Config.Main.Advanced.Language;

            using ZipArchive archive = new(resourcePackStream, ZipArchiveMode.Read, leaveOpen: true);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!TryGetResourcePackLanguage(entry.FullName, out string? language))
                    continue;

                if (language.Equals("en_us", StringComparison.OrdinalIgnoreCase))
                {
                    MergeResourcePackTranslations(entry, mergedTranslations);
                }
                else if (language.Equals(selectedLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    MergeResourcePackTranslations(entry, selectedLanguageTranslations);
                }
            }

            foreach (var entry in selectedLanguageTranslations)
                mergedTranslations[entry.Key] = entry.Value;

            return mergedTranslations;
        }

        private static bool TryGetResourcePackLanguage(string entryPath, [NotNullWhen(true)] out string? language)
        {
            language = null;

            string[] pathParts = entryPath
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length != 4
                || !pathParts[0].Equals("assets", StringComparison.OrdinalIgnoreCase)
                || !pathParts[2].Equals("lang", StringComparison.OrdinalIgnoreCase)
                || !pathParts[3].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            language = Path.GetFileNameWithoutExtension(pathParts[3]);
            return !string.IsNullOrEmpty(language);
        }

        private static void MergeResourcePackTranslations(ZipArchiveEntry entry, Dictionary<string, string> translations)
        {
            using Stream entryStream = entry.Open();
            Dictionary<string, string>? entryTranslations =
                JsonSerializer.Deserialize<Dictionary<string, string>>(entryStream);

            if (entryTranslations is null)
                return;

            foreach (var (key, value) in entryTranslations)
                translations[key] = value;
        }

        private static void ReplaceResourcePackTranslations(string packIdentifier, Dictionary<string, string> translations)
        {
            RemoveResourcePackTranslations(packIdentifier);

            if (translations.Count > 0)
                ResourcePackTranslationLayers.Add(new TranslationLayer(packIdentifier, translations));
        }

        private static void MergeForgeModTranslations(Stream modJarStream, HashSet<string> requestedModIds,
            Dictionary<string, Dictionary<string, string>> translationsByModId)
        {
            using ZipArchive archive = new(modJarStream, ZipArchiveMode.Read, leaveOpen: true);

            HashSet<string> archiveModIds = GetForgeModIds(archive)
                .Where(requestedModIds.Contains)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (archiveModIds.Count == 0)
                return;

            Dictionary<string, Dictionary<string, string>> archiveTranslations = ExtractForgeModTranslations(archive, archiveModIds);
            foreach (var (modId, translations) in archiveTranslations)
                translationsByModId[modId] = translations;
        }

        private static HashSet<string> GetForgeModIds(ZipArchive archive)
        {
            ZipArchiveEntry? modsTomlEntry = archive.Entries.FirstOrDefault(entry =>
                entry.FullName.Equals("META-INF/mods.toml", StringComparison.OrdinalIgnoreCase));

            if (modsTomlEntry is null)
                return [];

            HashSet<string> modIds = new(StringComparer.OrdinalIgnoreCase);
            using StreamReader reader = new(modsTomlEntry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            while (reader.ReadLine() is string line)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith('#'))
                    continue;

                Match match = ForgeModIdLineRegex.Match(trimmedLine);
                if (match.Success)
                    modIds.Add(NormalizeForgeModId(match.Groups[1].Value));
            }

            return modIds;
        }

        private static Dictionary<string, Dictionary<string, string>> ExtractForgeModTranslations(ZipArchive archive, HashSet<string> requestedModIds)
        {
            string selectedLanguage = NormalizeLanguageCode(Config.Main.Advanced.Language);
            Dictionary<string, Dictionary<string, string>> fallbackTranslations =
                new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Dictionary<string, string>> selectedTranslations =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!TryGetForgeModLanguage(entry.FullName, out string? modId, out string? language))
                    continue;

                if (!requestedModIds.Contains(modId))
                    continue;

                if (language.Equals("en_us", StringComparison.OrdinalIgnoreCase))
                {
                    if (!fallbackTranslations.TryGetValue(modId, out Dictionary<string, string>? translations))
                    {
                        translations = new Dictionary<string, string>(StringComparer.Ordinal);
                        fallbackTranslations[modId] = translations;
                    }

                    MergeResourcePackTranslations(entry, translations);
                }
                else if (language.Equals(selectedLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    if (!selectedTranslations.TryGetValue(modId, out Dictionary<string, string>? translations))
                    {
                        translations = new Dictionary<string, string>(StringComparer.Ordinal);
                        selectedTranslations[modId] = translations;
                    }

                    MergeResourcePackTranslations(entry, translations);
                }
            }

            Dictionary<string, Dictionary<string, string>> mergedTranslations =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (string modId in requestedModIds)
            {
                Dictionary<string, string> modTranslations = new(StringComparer.Ordinal);

                if (fallbackTranslations.TryGetValue(modId, out Dictionary<string, string>? fallback))
                {
                    foreach (var (key, value) in fallback)
                        modTranslations[key] = value;
                }

                if (selectedTranslations.TryGetValue(modId, out Dictionary<string, string>? selected))
                {
                    foreach (var (key, value) in selected)
                        modTranslations[key] = value;
                }

                if (modTranslations.Count > 0)
                    mergedTranslations[modId] = modTranslations;
            }

            return mergedTranslations;
        }

        private static bool TryGetForgeModLanguage(string entryPath, [NotNullWhen(true)] out string? modId,
            [NotNullWhen(true)] out string? language)
        {
            modId = null;
            language = null;

            string[] pathParts = entryPath
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length != 4
                || !pathParts[0].Equals("assets", StringComparison.OrdinalIgnoreCase)
                || !pathParts[2].Equals("lang", StringComparison.OrdinalIgnoreCase)
                || !pathParts[3].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            modId = NormalizeForgeModId(pathParts[1]);
            language = NormalizeLanguageCode(Path.GetFileNameWithoutExtension(pathParts[3]));
            return !string.IsNullOrEmpty(modId) && !string.IsNullOrEmpty(language);
        }

        private static string NormalizeForgeModId(string modId)
        {
            return Settings.ToLowerIfNeed(modId.Trim());
        }

        private static string NormalizeLanguageCode(string language)
        {
            return Settings.ToLowerIfNeed(language.Trim()).Replace('-', '_');
        }

        private static bool TryLoadCachedResourcePackTranslations(string cacheFilePath, Uri resourcePackUri, string hash,
            [NotNullWhen(true)] out Dictionary<string, string>? translations)
        {
            translations = null;

            if (!File.Exists(cacheFilePath))
                return false;

            try
            {
                using FileStream cacheFile = File.OpenRead(cacheFilePath);
                ResourcePackTranslationCacheEntry? cacheEntry =
                    JsonSerializer.Deserialize<ResourcePackTranslationCacheEntry>(cacheFile);

                if (cacheEntry is not null
                    && cacheEntry.CacheVersion == ResourcePackTranslationCacheVersion
                    && cacheEntry.Language.Equals(Config.Main.Advanced.Language, StringComparison.OrdinalIgnoreCase)
                    && cacheEntry.SourceUrl.Equals(resourcePackUri.AbsoluteUri, StringComparison.Ordinal)
                    && cacheEntry.SourceHash.Equals(hash, StringComparison.OrdinalIgnoreCase)
                    && cacheEntry.Translations.Count > 0)
                {
                    translations = new Dictionary<string, string>(cacheEntry.Translations, StringComparer.Ordinal);
                    return true;
                }
            }
            catch (IOException)
            {
            }
            catch (JsonException)
            {
            }

            try
            {
                File.Delete(cacheFilePath);
            }
            catch (IOException)
            {
            }

            return false;
        }

        private static void SaveCachedResourcePackTranslations(string cacheFilePath, Uri resourcePackUri, string hash,
            Dictionary<string, string> translations)
        {
            if (translations.Count == 0)
                return;

            string? cacheDirectory = Path.GetDirectoryName(cacheFilePath);
            if (string.IsNullOrEmpty(cacheDirectory))
                return;

            Directory.CreateDirectory(cacheDirectory);

            ResourcePackTranslationCacheEntry cacheEntry = new()
            {
                CacheVersion = ResourcePackTranslationCacheVersion,
                Language = Config.Main.Advanced.Language,
                SourceUrl = resourcePackUri.AbsoluteUri,
                SourceHash = hash,
                Translations = new Dictionary<string, string>(translations, StringComparer.Ordinal)
            };

            File.WriteAllText(cacheFilePath, JsonSerializer.Serialize(cacheEntry), Encoding.UTF8);
        }

        private static string GetResourcePackTranslationCacheFilePath(Uri resourcePackUri, string hash)
        {
            string cacheKey = GetResourcePackTranslationCacheKey(resourcePackUri, hash);
            return Path.Combine("lang", "resourcepacks", $"{cacheKey}.{Config.Main.Advanced.Language}.json");
        }

        private static string GetResourcePackTranslationCacheKey(Uri resourcePackUri, string hash)
        {
            if (IsValidSha1(hash))
                return hash.ToLowerInvariant();

            byte[] urlHash = SHA256.HashData(Encoding.UTF8.GetBytes(resourcePackUri.AbsoluteUri));
            return "url-" + Convert.ToHexString(urlHash).ToLowerInvariant();
        }

        private static bool IsValidSha1(string hash)
        {
            return hash.Length == 40 && hash.All(Uri.IsHexDigit);
        }

        /// <summary>
        /// Mapping from JSON/NBT property names to Minecraft formatting codes (without §).
        /// Both "underlined" (canonical Minecraft name) and "underline" (alias) are supported.
        /// </summary>
        private static readonly Dictionary<string, string> FormattingCodes = new()
        {
            { "obfuscated",    "k" },
            { "bold",          "l" },
            { "strikethrough", "m" },
            { "underlined",    "n" },
            { "underline",     "n" },
            { "italic",        "o" },
        };

        /// <summary>Matches a single color code (§0-§9, §a-§f) or a hex color (§#rrggbb). Used to strip color when replacing.</summary>
        private static readonly Regex ColorCodeRegex = new(@"§(?:[0-9a-f]|#[0-9a-f]{6})", RegexOptions.Compiled);

        /// <summary>
        /// Use a JSON Object to build the corresponding string
        /// </summary>
        /// <param name="data">JSON object to convert</param>
        /// <param name="formatting">Inherited formatting codes from parent elements (set to "" for function init)</param>
        /// <param name="links">Container for links from JSON serialized text</param>
        /// <returns>returns the Minecraft-formatted string</returns>
        private static string JSONData2String(System.Text.Json.Nodes.JsonNode? data, string formatting, List<string>? links)
        {
            string extra_result = "";
            switch (data)
            {
                case System.Text.Json.Nodes.JsonObject obj:
                    if (obj.ContainsKey("color"))
                    {
                        formatting = ColorCodeRegex.Replace(formatting, "");
                        formatting += Color2tag(JSONData2String(obj["color"], "", links));
                    }

                    foreach (var (key, code) in FormattingCodes)
                    {
                        if (obj.ContainsKey(key))
                        {
                            string val = obj[key]!.GetStringValue();
                            if (val == "true")
                                formatting += "§" + code;
                            else if (val == "false")
                                formatting = formatting.Replace("§" + code, "");
                        }
                    }

                    if (obj.ContainsKey("clickEvent") && links is not null)
                    {
                        var clickEvent = obj["clickEvent"]!.AsObject();
                        if (clickEvent.ContainsKey("action")
                            && clickEvent.ContainsKey("value")
                            && clickEvent["action"]!.GetStringValue() == "open_url"
                            && !string.IsNullOrEmpty(clickEvent["value"]!.GetStringValue()))
                        {
                            links.Add(clickEvent["value"]!.GetStringValue());
                        }
                    }

                    if (obj.ContainsKey("extra"))
                    {
                        foreach (var item in obj["extra"]!.AsArray())
                            extra_result += JSONData2String(item, "§r" + formatting, links);
                    }

                    // Strip any formatting codes that appear before the last §r, since §r resets all
                    // prior formatting. The greedy .* matches up to the last §r in the string.
                    formatting = Regex.Replace(formatting, ".*(§r.*)", "$1");

                    if (obj.ContainsKey("text"))
                    {
                        // Pass "" to the leaf text node: formatting is already prepended here,
                        // and the default: case would add it a second time if we passed formatting.
                        return formatting + JSONData2String(obj["text"], "", links) + extra_result;
                    }
                    else if (obj.ContainsKey("translate"))
                    {
                        List<string> using_data = new();
                        if (obj.ContainsKey("using") && !obj.ContainsKey("with"))
                            obj["with"] = obj["using"]!.DeepClone();
                        if (obj.ContainsKey("with"))
                        {
                            foreach (var item in obj["with"]!.AsArray())
                            {
                                using_data.Add(JSONData2String(item, formatting, links));
                            }
                        }

                        return formatting +
                               TranslateString(JSONData2String(obj["translate"], "", links), using_data) +
                               extra_result;
                    }
                    else return extra_result;

                case System.Text.Json.Nodes.JsonArray arr:
                    string result = "";
                    foreach (var item in arr)
                    {
                        result += JSONData2String(item, formatting, links);
                    }

                    return result;

                default:
                    return formatting + data.GetStringValue();
            }
        }

        private static string NbtToString(Dictionary<string, object> nbt, string formatting = "")
        {
            if (nbt.Count == 1 && nbt.TryGetValue("", out object? rootMessage))
            {
                return formatting + (rootMessage?.ToString() ?? string.Empty);
            }

            string message = string.Empty;
            StringBuilder extraBuilder = new();

            // Build formatting from color and formatting flags first
            if (nbt.TryGetValue("color", out object? color))
            {
                formatting = ColorCodeRegex.Replace(formatting, "");
                formatting += Color2tag((string)color);
            }

            foreach (var (key, code) in FormattingCodes)
            {
                if (nbt.TryGetValue(key, out object? flagValue))
                {
                    bool isActive = flagValue switch
                    {
                        byte b => b > 0,
                        bool b => b,
                        _ => flagValue?.ToString()?.ToLower() == "true"
                    };
                    if (isActive)
                        formatting += "§" + code;
                    else
                        formatting = formatting.Replace("§" + code, "");
                }
            }

            // Process text
            if (nbt.TryGetValue("text", out object? textValue))
                message = textValue?.ToString() ?? string.Empty;

            // Process translate
            if (nbt.TryGetValue("translate", out object? translate))
            {
                var translateKey = (string)translate;
                List<string> translateString = new();
                if (nbt.TryGetValue("with", out object? withComponent))
                {
                    var withs = (object[])withComponent;
                    for (var i = 0; i < withs.Length; i++)
                    {
                        var withDict = withs[i] switch
                        {
                            int => new Dictionary<string, object> { { "text", $"{withs[i]}" } },
                            string => new Dictionary<string, object> { { "text", (string)withs[i] } },
                            _ => (Dictionary<string, object>)withs[i]
                        };
                        translateString.Add(NbtToString(withDict, formatting));
                    }
                }
                message = TranslateString(translateKey, translateString);
            }

            // Process extras, each starting with a reset then inheriting the current formatting
            if (nbt.TryGetValue("extra", out object? extraValue))
            {
                object[] extras = (object[])extraValue;
                for (var i = 0; i < extras.Length; i++)
                {
                    var extraDict = extras[i] switch
                    {
                        int => new Dictionary<string, object> { { "text", $"{extras[i]}" } },
                        string => new Dictionary<string, object> { { "text", (string)extras[i] } },
                        _ => (Dictionary<string, object>)extras[i]
                    };
                    extraBuilder.Append(NbtToString(extraDict, "§r" + formatting));
                }
            }

            return formatting + message + extraBuilder.ToString();
        }
    }
}
