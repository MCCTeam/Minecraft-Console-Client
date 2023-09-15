using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MinecraftClient
{
    /// <summary>
    /// INI File tools for parsing and generating user-friendly INI files
    /// By ORelio (c) 2014-2020 - CDDL 1.0
    /// </summary>
    static class INIFile
    {
        /// <summary>
        /// Parse a INI file into a dictionary.
        /// Values can be accessed like this: dict["section"]["setting"]
        /// </summary>
        /// <param name="iniFile">INI file to parse</param>
        /// <param name="lowerCase">INI sections and keys will be converted to lowercase unless this parameter is set to false</param>
        /// <exception cref="IOException">If failed to read the file</exception>
        /// <returns>Parsed data from INI file</returns>
        public static Dictionary<string, Dictionary<string, string>> ParseFile(string iniFile, bool lowerCase = true)
        {
            return ParseFile(File.ReadAllLines(iniFile, Encoding.UTF8), lowerCase);
        }

        /// <summary>
        /// Parse a INI file into a dictionary.
        /// Values can be accessed like this: dict["section"]["setting"]
        /// </summary>
        /// <param name="lines">INI file content to parse</param>
        /// <param name="lowerCase">INI sections and keys will be converted to lowercase unless this parameter is set to false</param>
        /// <exception cref="IOException">If failed to read the file</exception>
        /// <returns>Parsed data from INI file</returns>
        public static Dictionary<string, Dictionary<string, string>> ParseFile(IEnumerable<string> lines, bool lowerCase = true)
        {
            var iniContents = new Dictionary<string, Dictionary<string, string>>();
            string iniSection = "default";
            foreach (string lineRaw in lines)
            {
                string line = lineRaw.Split('#')[0].Trim();
                if (line.Length > 0 && line[0] != ';')
                {
                    if (line[0] == '[' && line[^1] == ']')
                    {
                        iniSection = line[1..^1];
                        if (lowerCase)
                            iniSection = iniSection.ToLower();
                    }
                    else
                    {
                        string argName = line.Split('=')[0];
                        if (lowerCase)
                            argName = argName.ToLower();
                        if (line.Length > (argName.Length + 1))
                        {
                            string argValue = line[(argName.Length + 1)..];
                            if (!iniContents.ContainsKey(iniSection))
                                iniContents[iniSection] = new Dictionary<string, string>();
                            iniContents[iniSection][argName] = argValue;
                        }
                    }
                }
            }
            return iniContents;
        }

        /// <summary>
        /// Write given data into an INI file
        /// </summary>
        /// <param name="iniFile">File to write into</param>
        /// <param name="contents">Data to put into the file</param>
        /// <param name="description">INI file description, inserted as a comment on first line of the INI file</param>
        /// <param name="autoCase">Automatically change first char of section and keys to uppercase</param>
        public static void WriteFile(string iniFile, Dictionary<string, Dictionary<string, string>> contents, string? description = null, bool autoCase = true)
        {
            File.WriteAllLines(iniFile, Generate(contents, description, autoCase), Encoding.UTF8);
        }

        /// <summary>
        /// Generate given data into the INI format
        /// </summary>
        /// <param name="contents">Data to put into the INI format</param>
        /// <param name="description">INI file description, inserted as a comment on first line of the INI file</param>
        /// <param name="autoCase">Automatically change first char of section and keys to uppercase</param>
        /// <returns>Lines of the INI file</returns>
        public static string[] Generate(Dictionary<string, Dictionary<string, string>> contents, string? description = null, bool autoCase = true)
        {
            List<string> lines = new();
            if (!String.IsNullOrWhiteSpace(description))
                lines.Add('#' + description);
            foreach (var section in contents)
            {
                if (lines.Count > 0)
                    lines.Add("");
                if (!String.IsNullOrEmpty(section.Key))
                {
                    lines.Add("[" + (autoCase ? char.ToUpper(section.Key[0]) + section.Key[1..] : section.Key) + ']');
                    foreach (var item in section.Value)
                        if (!String.IsNullOrEmpty(item.Key))
                            lines.Add((autoCase ? char.ToUpper(item.Key[0]) + item.Key[1..] : item.Key) + '=' + item.Value);
                }
            }
            return lines.ToArray();
        }

        /// <summary>
        /// Convert an integer from a string or return 0 if failed to parse
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <returns>Int value</returns>
        public static int Str2Int(string str)
        {
            try
            {
                return Convert.ToInt32(str);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Convert a 0/1 or True/False value to boolean
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <returns>Boolean value</returns>
        public static bool Str2Bool(string str)
        {
            return str.ToLower() == "true" || str == "1";
        }
    }
}
