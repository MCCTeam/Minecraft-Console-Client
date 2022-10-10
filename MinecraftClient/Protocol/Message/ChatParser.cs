using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Message;
using static MinecraftClient.Settings;

namespace MinecraftClient.Protocol
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
            string chatContent = Config.Signature.ShowModifiedChat && message.unsignedContent != null ? message.unsignedContent : message.content;
            string content = message.isJson ? ParseText(chatContent, links) : chatContent;
            string sender = message.displayName!;

            string text;
            List<string> usingData = new();

            MessageType chatType;
            if (message.isSystemChat)
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
            string color = string.Empty;
            if (message.isSystemChat)
            {
                if (Config.Signature.MarkSystemMessage)
                    color = "§z §r ";     // Custom color code §z : Background Gray
            }
            else
            {
                if ((bool)message.isSignatureLegal!)
                {
                    if (Config.Signature.ShowModifiedChat && message.unsignedContent != null)
                    {
                        if (Config.Signature.MarkModifiedMsg)
                            color = "§x §r "; // Custom color code §x : Background Yellow
                    }
                    else
                    {
                        if (Config.Signature.MarkLegallySignedMsg)
                            color = "§y §r "; // Custom color code §y : Background Green
                    }
                }
                else
                {
                    if (Config.Signature.MarkIllegallySignedMsg)
                        color = "§w §r "; // Custom color code §w : Background Red
                }
            }
            return color + text;
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
        private static readonly Dictionary<string, string> TranslationRules = new();

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
            //Small default dictionnary of translation rules
            TranslationRules["chat.type.admin"] = "[%s: %s]";
            TranslationRules["chat.type.announcement"] = "§d[%s] %s";
            TranslationRules["chat.type.emote"] = " * %s %s";
            TranslationRules["chat.type.text"] = "<%s> %s";
            TranslationRules["multiplayer.player.joined"] = "§e%s joined the game.";
            TranslationRules["multiplayer.player.left"] = "§e%s left the game.";
            TranslationRules["commands.message.display.incoming"] = "§7%s whispers to you: %s";
            TranslationRules["commands.message.display.outgoing"] = "§7You whisper to %s: %s";

            //Language file in a subfolder, depending on the language setting
            if (!Directory.Exists("lang"))
                Directory.CreateDirectory("lang");

            string Language_File = "lang" + Path.DirectorySeparatorChar + Config.Main.Advanced.Language + ".lang";

            //File not found? Try downloading language file from Mojang's servers?
            if (!File.Exists(Language_File))
            {
                ConsoleIO.WriteLineFormatted(Translations.Get("chat.download", Config.Main.Advanced.Language));
                HttpClient httpClient = new();
                try
                {
                    Task<string> fetch_index = httpClient.GetStringAsync(Settings.TranslationsFile_Website_Index);
                    fetch_index.Wait();
                    string assets_index = fetch_index.Result;
                    fetch_index.Dispose();

                    string[] tmp = assets_index.Split(new string[] { "minecraft/lang/" + Config.Main.Advanced.Language.ToLower() + ".json" }, StringSplitOptions.None);
                    tmp = tmp[1].Split(new string[] { "hash\": \"" }, StringSplitOptions.None);
                    string hash = tmp[1].Split('"')[0]; //Translations file identifier on Mojang's servers
                    string translation_file_location = Settings.TranslationsFile_Website_Download + '/' + hash[..2] + '/' + hash;
                    if (Settings.Config.Logging.DebugMessages)
                        ConsoleIO.WriteLineFormatted(Translations.Get("chat.request", translation_file_location));

                    Task<string> fetch_file = httpClient.GetStringAsync(translation_file_location);
                    fetch_file.Wait();
                    string translation_file = fetch_file.Result;
                    fetch_file.Dispose();

                    StringBuilder stringBuilder = new();
                    foreach (KeyValuePair<string, Json.JSONData> entry in Json.ParseJson(translation_file).Properties)
                        stringBuilder.Append(entry.Key).Append('=').Append(entry.Value.StringValue).Append(Environment.NewLine);
                    File.WriteAllText(Language_File, stringBuilder.ToString());

                    ConsoleIO.WriteLineFormatted(Translations.Get("chat.done", Language_File));
                }
                catch
                {
                    Translations.WriteLineFormatted("chat.fail");
                }
                httpClient.Dispose();
            }

            //Download Failed? Defaulting to en_GB.lang if the game is installed
            if (!File.Exists(Language_File) //Try en_GB.lang
              && File.Exists(Settings.TranslationsFile_FromMCDir))
            {
                Language_File = Settings.TranslationsFile_FromMCDir;
                Translations.WriteLineFormatted("chat.from_dir");
            }

            //Load the external dictionnary of translation rules or display an error message
            if (File.Exists(Language_File))
            {
                foreach (var line in File.ReadLines(Language_File))
                {
                    if (line.Length > 0)
                    {
                        string[] splitted = line.Split('=');
                        if (splitted.Length == 2)
                        {
                            TranslationRules[splitted[0]] = splitted[1];
                        }
                    }
                }

                if (Settings.Config.Logging.DebugMessages)
                    Translations.WriteLineFormatted("chat.loaded");
            }
            else //No external dictionnary found.
            {
                ConsoleIO.WriteLineFormatted(Translations.Get("chat.not_found", Language_File));
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
