using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

        public static void ReadChatType(Dictionary<string, object> registryCodec)
        {
            Dictionary<int, MessageType> chatTypeDictionary = ChatId2Type ?? new();
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
            if (Config.Signature.ShowModifiedChat && message.unsignedContent != null)
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
            return colorname.ToLower() switch
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

        /// <summary>
        /// Specify whether translation rules have been loaded
        /// </summary>
        private static bool RulesInitialized = false;

        /// <summary>
        /// Set of translation rules for formatting text
        /// </summary>
        private static Dictionary<string, string> TranslationRules = new();

        /// <summary>
        /// Initialize translation rules.
        /// Necessary for properly printing some chat messages.
        /// </summary>
        public static void InitTranslations() { if (!RulesInitialized) { InitRules(); RulesInitialized = true; } }

        /// <summary>
        /// Internal rule initialization method. Looks for local rule file or download it from Mojang asset servers.
        /// </summary>
        private static void InitRules()
        {
            if (Config.Main.Advanced.Language == "en_us")
            {
                TranslationRules = JsonSerializer.Deserialize<Dictionary<string, string>>((byte[])MinecraftAssets.ResourceManager.GetObject("en_us.json")!)!;
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
                    TranslationRules = JsonSerializer.Deserialize<Dictionary<string, string>>(File.OpenRead(languageFilePath))!;
                }
                catch (IOException) { }
                catch (JsonException) { }
            }

            if (TranslationRules.TryGetValue("Version", out string? version) && version == Settings.TranslationsFile_Version)
            {
                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted(Translations.chat_loaded, acceptnewlines: true);
                return;
            }

            // Try downloading language file from Mojang's servers?
            ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.chat_download, Config.Main.Advanced.Language));
            HttpClient httpClient = new();
            try
            {
                Task<string> fetch_index = httpClient.GetStringAsync(TranslationsFile_Website_Index);
                fetch_index.Wait();
                Match match = Regex.Match(fetch_index.Result, $"minecraft/lang/{Config.Main.Advanced.Language}.json" + @""":\s\{""hash"":\s""([\d\w]{40})""");
                fetch_index.Dispose();
                if (match.Success && match.Groups.Count == 2)
                {
                    string hash = match.Groups[1].Value;
                    string translation_file_location = TranslationsFile_Website_Download + '/' + hash[..2] + '/' + hash;
                    if (Config.Logging.DebugMessages)
                        ConsoleIO.WriteLineFormatted(string.Format(Translations.chat_request, translation_file_location));

                    Task<Dictionary<string, string>?> fetckFileTask = httpClient.GetFromJsonAsync<Dictionary<string, string>>(translation_file_location);
                    fetckFileTask.Wait();
                    if (fetckFileTask.Result != null && fetckFileTask.Result.Count > 0)
                    {
                        TranslationRules = fetckFileTask.Result;
                        TranslationRules["Version"] = TranslationsFile_Version;
                        File.WriteAllText(languageFilePath, JsonSerializer.Serialize(TranslationRules, typeof(Dictionary<string, string>)), Encoding.UTF8);

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
                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.chat_save_fail, languageFilePath), acceptnewlines: true);
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

            TranslationRules = JsonSerializer.Deserialize<Dictionary<string, string>>((byte[])MinecraftAssets.ResourceManager.GetObject("en_us.json")!)!;
            ConsoleIO.WriteLine(Translations.chat_use_default);
        }

        public static string? TranslateString(string rulename)
        {
            if (TranslationRules.TryGetValue(rulename, out string? result))
                return result;
            else
                return null;
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
            if (!RulesInitialized) { InitRules(); RulesInitialized = true; }
            if (TranslationRules.ContainsKey(rulename))
            {
                int using_idx = 0;
                string rule = TranslationRules[rulename];
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

        /// <summary>
        /// Use a JSON Object to build the corresponding string
        /// </summary>
        /// <param name="data">JSON object to convert</param>
        /// <param name="colorcode">Allow parent color code to affect child elements (set to "" for function init)</param>
        /// <param name="links">Container for links from JSON serialized text</param>
        /// <returns>returns the Minecraft-formatted string</returns>
        private static string JSONData2String(Json.JSONData data, string colorcode, List<string>? links)
        {
            string extra_result = "";
            switch (data.Type)
            {
                case Json.JSONData.DataType.Object:
                    if (data.Properties.ContainsKey("color"))
                    {
                        colorcode = Color2tag(JSONData2String(data.Properties["color"], "", links));
                    }
                    if (data.Properties.ContainsKey("clickEvent") && links != null)
                    {
                        Json.JSONData clickEvent = data.Properties["clickEvent"];
                        if (clickEvent.Properties.ContainsKey("action")
                            && clickEvent.Properties.ContainsKey("value")
                            && clickEvent.Properties["action"].StringValue == "open_url"
                            && !string.IsNullOrEmpty(clickEvent.Properties["value"].StringValue))
                        {
                            links.Add(clickEvent.Properties["value"].StringValue);
                        }
                    }
                    if (data.Properties.ContainsKey("extra"))
                    {
                        Json.JSONData[] extras = data.Properties["extra"].DataArray.ToArray();
                        foreach (Json.JSONData item in extras)
                            extra_result = extra_result + JSONData2String(item, colorcode, links) + "§r";
                    }
                    if (data.Properties.ContainsKey("text"))
                    {
                        return colorcode + JSONData2String(data.Properties["text"], colorcode, links) + extra_result;
                    }
                    else if (data.Properties.ContainsKey("translate"))
                    {
                        List<string> using_data = new();
                        if (data.Properties.ContainsKey("using") && !data.Properties.ContainsKey("with"))
                            data.Properties["with"] = data.Properties["using"];
                        if (data.Properties.ContainsKey("with"))
                        {
                            Json.JSONData[] array = data.Properties["with"].DataArray.ToArray();
                            for (int i = 0; i < array.Length; i++)
                            {
                                using_data.Add(JSONData2String(array[i], colorcode, links));
                            }
                        }
                        return colorcode + TranslateString(JSONData2String(data.Properties["translate"], "", links), using_data) + extra_result;
                    }
                    else return extra_result;

                case Json.JSONData.DataType.Array:
                    string result = "";
                    foreach (Json.JSONData item in data.DataArray)
                    {
                        result += JSONData2String(item, colorcode, links);
                    }
                    return result;

                case Json.JSONData.DataType.String:
                    return colorcode + data.StringValue;
            }

            return "";
        }
    }
}
