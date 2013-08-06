using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient
{
    /// <summary>
    /// This class parses JSON chat data from MC 1.6+ and returns the appropriate string to be printed.
    /// </summary>

    static class ChatParser
    {
        /// <summary>
        /// The main function to convert text from MC 1.6+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="json">JSON serialized text</param>
        /// <returns>Returns the translated text</returns>

        public static string ParseText(string json)
        {
            int cursorpos = 0;
            JSONData jsonData = String2Data(json, ref cursorpos);
            return JSONData2String(jsonData).Replace("u0027", "'");
        }

        /// <summary>
        /// An internal class to store unserialized JSON data
        /// The data can be an object, an array or a string
        /// </summary>

        private class JSONData
        {
            public enum DataType { Object, Array, String };
            private DataType type;
            public DataType Type { get { return type; } }
            public Dictionary<string, JSONData> Properties;
            public List<JSONData> DataArray;
            public string StringValue;
            public JSONData(DataType datatype)
            {
                type = datatype;
                Properties = new Dictionary<string, JSONData>();
                DataArray = new List<JSONData>();
                StringValue = String.Empty;
            }
        }

        /// <summary>
        /// Get the classic color tag corresponding to a color name
        /// </summary>
        /// <param name="colorname">Color Name</param>
        /// <returns>Color code</returns>

        private static string color2tag(string colorname)
        {
            switch (colorname.ToLower())
            {
                case "black": return "§0";
                case "dark_blue": return "§1";
                case "dark_green": return "§2";
                case "dark_cyan": return "§3";
                case "dark_cyanred": return "§4";
                case "dark_magenta": return "§5";
                case "dark_yellow": return "§6";
                case "gray": return "§7";
                case "dark_gray": return "§8";
                case "blue": return "§9";
                case "green": return "§a";
                case "cyan": return "§b";
                case "red": return "§c";
                case "magenta": return "§d";
                case "yellow": return "§e";
                case "white": return "§f";
                default: return "";
            }
        }

        /// <summary>
        /// Rules for text translation
        /// </summary>

        private static bool init = false;
        private static Dictionary<string, string> TranslationRules = new Dictionary<string, string>();
        public static void InitTranslations() { if (!init) { InitRules(); init = true; } }
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

            //Load an external dictionnary of translation rules
            if (System.IO.File.Exists(Settings.TranslationsFile))
            {
                string[] translations = System.IO.File.ReadAllLines(Settings.TranslationsFile);
                foreach (string line in translations)
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

                Console.ForegroundColor = ConsoleColor.DarkGray;
                ConsoleIO.WriteLine("Translations file loaded.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else //No external dictionnary found.
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                ConsoleIO.WriteLine("MC 1.6+ warning: Translations file \"" + Settings.TranslationsFile + "\" not found."
                + "\nYou can pick a translation file from .minecraft\\assets\\lang\\"
                + "\nCopy to the same folder as MinecraftClient & rename to \"" + Settings.TranslationsFile + "\""
                + "\nSome messages won't be properly printed without this file.");
                Console.ForegroundColor = ConsoleColor.Gray;
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
            if (!init) { InitRules(); init = true; }
            if (TranslationRules.ContainsKey(rulename))
            {
                if ((TranslationRules[rulename].IndexOf("%1$s") >= 0 && TranslationRules[rulename].IndexOf("%2$s") >= 0)
                    && (TranslationRules[rulename].IndexOf("%1$s") > TranslationRules[rulename].IndexOf("%2$s")))
                {
                    while (using_data.Count < 2) { using_data.Add(""); }
                    string tmp = using_data[0];
                    using_data[0] = using_data[1];
                    using_data[1] = tmp;
                }
                string[] syntax = TranslationRules[rulename].Split(new string[] { "%s", "%d", "%1$s", "%2$s" }, StringSplitOptions.None);
                while (using_data.Count < syntax.Length - 1) { using_data.Add(""); }
                string[] using_array = using_data.ToArray();
                string translated = "";
                for (int i = 0; i < syntax.Length - 1; i++)
                {
                    translated += syntax[i];
                    translated += using_array[i];
                }
                translated += syntax[syntax.Length - 1];
                return translated;
            }
            else return "[" + rulename + "] " + String.Join(" ", using_data);
        }

        /// <summary>
        /// Parse a JSON string to build a JSON object
        /// </summary>
        /// <param name="toparse">String to parse</param>
        /// <param name="cursorpos">Cursor start (set to 0 for function init)</param>
        /// <returns></returns>

        private static JSONData String2Data(string toparse, ref int cursorpos)
        {
            try
            {
                JSONData data;
                switch (toparse[cursorpos])
                {
                    //Object
                    case '{':
                        data = new JSONData(JSONData.DataType.Object);
                        cursorpos++;
                        while (toparse[cursorpos] != '}')
                        {
                            if (toparse[cursorpos] == '"')
                            {
                                JSONData propertyname = String2Data(toparse, ref cursorpos);
                                if (toparse[cursorpos] == ':') { cursorpos++; } else { /* parse error ? */ }
                                JSONData propertyData = String2Data(toparse, ref cursorpos);
                                data.Properties[propertyname.StringValue] = propertyData;
                            }
                            else cursorpos++;
                        }
                        cursorpos++;
                        break;

                    //Array
                    case '[':
                        data = new JSONData(JSONData.DataType.Array);
                        cursorpos++;
                        while (toparse[cursorpos] != ']')
                        {
                            if (toparse[cursorpos] == ',') { cursorpos++; }
                            JSONData arrayItem = String2Data(toparse, ref cursorpos);
                            data.DataArray.Add(arrayItem);
                        }
                        cursorpos++;
                        break;

                    //String
                    case '"':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        while (toparse[cursorpos] != '"')
                        {
                            if (toparse[cursorpos] == '\\') { cursorpos++; }
                            data.StringValue += toparse[cursorpos];
                            cursorpos++;
                        }
                        cursorpos++;
                        break;

                    //Boolean : true
                    case 't':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        if (toparse[cursorpos] == 'r') { cursorpos++; }
                        if (toparse[cursorpos] == 'u') { cursorpos++; }
                        if (toparse[cursorpos] == 'e') { cursorpos++; data.StringValue = "true"; }
                        break;

                    //Boolean : false
                    case 'f':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        if (toparse[cursorpos] == 'a') { cursorpos++; }
                        if (toparse[cursorpos] == 'l') { cursorpos++; }
                        if (toparse[cursorpos] == 's') { cursorpos++; }
                        if (toparse[cursorpos] == 'e') { cursorpos++; data.StringValue = "false"; }
                        break;

                    //Unknown data
                    default:
                        cursorpos++;
                        return String2Data(toparse, ref cursorpos);
                }
                return data;
            }
            catch (IndexOutOfRangeException)
            {
                return new JSONData(JSONData.DataType.String);
            }
        }

        /// <summary>
        /// Use a JSON Object to build the corresponding string
        /// </summary>
        /// <param name="data">JSON object to convert</param>
        /// <returns>returns the Minecraft-formatted string</returns>

        private static string JSONData2String(JSONData data)
        {
            string colorcode = "";
            switch (data.Type)
            {
                case JSONData.DataType.Object:
                    if (data.Properties.ContainsKey("color"))
                    {
                        colorcode = color2tag(JSONData2String(data.Properties["color"]));
                    }
                    if (data.Properties.ContainsKey("text"))
                    {
                        return colorcode + JSONData2String(data.Properties["text"]) + colorcode;
                    }
                    else if (data.Properties.ContainsKey("translate"))
                    {
                        List<string> using_data = new List<string>();
                        if (data.Properties.ContainsKey("using"))
                        {
                            JSONData[] array = data.Properties["using"].DataArray.ToArray();
                            for (int i = 0; i < array.Length; i++)
                            {
                                using_data.Add(JSONData2String(array[i]));
                            }
                        }
                        return colorcode + TranslateString(JSONData2String(data.Properties["translate"]), using_data) + colorcode;
                    }
                    else return "";

                case JSONData.DataType.Array:
                    string result = "";
                    foreach (JSONData item in data.DataArray)
                    {
                        result += JSONData2String(item);
                    }
                    return result;

                case JSONData.DataType.String:
                    return data.StringValue;
            }

            return "";
        }
    }
}
