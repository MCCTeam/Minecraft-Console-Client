using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MinecraftClient
{
    /// <summary>
    /// Allows to localize MinecraftClient in different languages
    /// </summary>
    /// <remarks>
    /// By ORelio (c) 2015-2018 - CDDL 1.0
    /// </remarks>
    public static class Translations
    {
        private static Dictionary<string, string> translations;
        private static string translationFilePath = "lang" + Path.DirectorySeparatorChar + "mcc";
        private static string defaultTranslation = "en.ini";
        private static Regex translationKeyRegex = new Regex(@"\(\[(.*?)\]\)", RegexOptions.Compiled); // Extract string inside ([ ])

        /// <summary>
        /// Return a tranlation for the requested text. Support string formatting
        /// </summary>
        /// <param name="msgName">text identifier</param>
        /// <returns>returns translation for this identifier</returns>
        public static string Get(string msgName, params object[] args)
        {
            if (translations.ContainsKey(msgName))
            {
                if (args.Length > 0)
                {
                    return string.Format(translations[msgName], args);
                }
                else return translations[msgName];
            }
            return msgName.ToUpper();
        }

        /// <summary>
        /// Return a tranlation for the requested text. Support string formatting. If not found, return the original text
        /// </summary>
        /// <param name="msgName">text identifier</param>
        /// <param name="args"></param>
        /// <returns>Translated text or original text if not found</returns>
        /// <remarks>Useful when not sure msgName is a translation mapping key or a normal text</remarks>
        public static string TryGet(string msgName, params object[] args)
        {
            if (translations.ContainsKey(msgName))
                return Get(msgName, args);
            else return msgName;
        }

        /// <summary>
        /// Replace the translation key inside a sentence to translated text. Wrap the key in ([translation.key])
        /// </summary>
        /// <example>
        /// e.g.  I only want to replace ([this])
        /// would only translate "this" without touching other words.
        /// </example>
        /// <param name="msg">Sentence for replace</param>
        /// <param name="args"></param>
        /// <returns>Translated sentence</returns>
        public static string Replace(string msg, params object[] args)
        {
            string translated = translationKeyRegex.Replace(msg, new MatchEvaluator(ReplaceKey));
            if (args.Length > 0)
                return string.Format(translated, args);
            else return translated;
        }

        private static string ReplaceKey(Match m)
        {
            return Get(m.Groups[1].Value);
        }

        /// <summary>
        /// Initialize translations depending on system language.
        /// English is the default for all unknown system languages.
        /// </summary>
        static Translations()
        {
            translations = new Dictionary<string, string>();
            LoadDefaultTranslationsFile();
        }

        /// <summary>
        /// Load default translation file (English)
        /// </summary>
        /// <remarks>
        /// This will be loaded during program start up.
        /// </remarks>
        private static void LoadDefaultTranslationsFile()
        {
            string[] engLang = DefaultConfigResource.TranslationEnglish.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); // use embedded translations
            ParseTranslationContent(engLang);
        }

        /// <summary>
        /// Load translation file depends on system language or by giving a file path. Default to English if translation file does not exist
        /// </summary>
        public static void LoadExternalTranslationFile(string language)
        {
            /*
             * External translation files
             * These files are loaded from the installation directory as:
             * Lang/abc.ini, e.g. Lang/eng.ini which is the default language file
             * Useful for adding new translations of fixing typos without recompiling
             */

            // Try to convert Minecraft language file name to two letters language name
            if (language == "zh_cn")
                language = "zh-CHS";
            else if (language == "zh_tw")
                language = "zh-CHT";
            else
                language = language.Split('_')[0];

            string systemLanguage = string.IsNullOrEmpty(CultureInfo.CurrentCulture.Parent.Name) // Parent.Name might be empty
                    ? CultureInfo.CurrentCulture.Name
                    : CultureInfo.CurrentCulture.Parent.Name;
            string langDir = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar;
            string langFileSystemLanguage = langDir + systemLanguage + ".ini";
            string langFileConfigLanguage = langDir + language + ".ini";

            if (File.Exists(langFileConfigLanguage))
            {// Language set in ini config
                ParseTranslationContent(File.ReadAllLines(langFileConfigLanguage));
                return;
            }
            else
            {
                if (Settings.DebugMessages)
                    ConsoleIO.WriteLogLine("[Translations] No translation file found for " + language + ". (Looked '" + langFileConfigLanguage + "'");
            }

            if (File.Exists(langFileSystemLanguage))
            {// Fallback to system language
                ParseTranslationContent(File.ReadAllLines(langFileSystemLanguage));
                return;
            }
            else
            {
                if (Settings.DebugMessages)
                    ConsoleIO.WriteLogLine("[Translations] No translation file found for system language (" + systemLanguage + "). (Looked '" + langFileSystemLanguage + "'");
            }
        }

        /// <summary>
        /// Parse the given array to translation map
        /// </summary>
        /// <param name="content">Content of the translation file (in ini format)</param>
        private static void ParseTranslationContent(string[] content)
        {
            foreach (string lineRaw in content)
            {
                string line = lineRaw.Trim();
                if (line.Length <= 0)
                    continue;
                if (line.StartsWith("#")) // ignore comment line started with #
                    continue;
                if (line[0] == '[' && line[line.Length - 1] == ']') // ignore section
                    continue;

                string translationName = line.Split('=')[0];
                if (line.Length > (translationName.Length + 1))
                {
                    string translationValue = line.Substring(translationName.Length + 1).Replace("\\n", "\n");
                    translations[translationName] = translationValue;
                }
            }
        }

        /// <summary>
        /// Write the default translation file (English) to the disk.
        /// </summary>
        private static void WriteDefaultTranslation()
        {
            string defaultPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar + defaultTranslation;

            if (!Directory.Exists(translationFilePath))
            {
                Directory.CreateDirectory(translationFilePath);
            }
            File.WriteAllText(defaultPath, DefaultConfigResource.TranslationEnglish, Encoding.UTF8);
        }

        #region Console writing method wrapper

        /// <summary>
        /// Translate the key and write the result to the standard output, without newline character
        /// </summary>
        /// <param name="key">Translation key</param>
        public static void Write(string key)
        {
            ConsoleIO.Write(Get(key));
        }

        /// <summary>
        /// Translate the key and write a Minecraft-Like formatted string to the standard output, using §c color codes
        /// See minecraft.gamepedia.com/Classic_server_protocol#Color_Codes for more info
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="acceptnewlines">If false, space are printed instead of newlines</param>
        /// <param name="displayTimestamp">
        /// If false, no timestamp is prepended.
        /// If true, "hh-mm-ss" timestamp will be prepended.
        /// If unspecified, value is retrieved from EnableTimestamps.
        /// </param>
        public static void WriteLineFormatted(string key, bool acceptnewlines = true, bool? displayTimestamp = null)
        {
            ConsoleIO.WriteLineFormatted(Get(key), acceptnewlines, displayTimestamp);
        }

        /// <summary>
        /// Translate the key, format the result and write it to the standard output with a trailing newline. Support string formatting
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="args"></param>
        public static void WriteLine(string key, params object[] args)
        {
            if (args.Length > 0)
                ConsoleIO.WriteLine(string.Format(Get(key), args));
            else ConsoleIO.WriteLine(Get(key));
        }

        /// <summary>
        /// Translate the key and write the result with a prefixed log line. Prefix is set in LogPrefix.
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="acceptnewlines">Allow line breaks</param>
        public static void WriteLogLine(string key, bool acceptnewlines = true)
        {
            ConsoleIO.WriteLogLine(Get(key), acceptnewlines);
        }

        #endregion
    }
}