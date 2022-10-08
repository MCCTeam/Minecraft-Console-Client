using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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
        private static readonly string translationFilePath = "lang" + Path.DirectorySeparatorChar + "mcc";
        private static readonly Regex translationKeyRegex = new(@"\(\[(.*?)\]\)", RegexOptions.Compiled); // Extract string inside ([ ])

        /// <summary>
        /// Return a tranlation for the requested text. Support string formatting
        /// </summary>
        /// <param name="msgName">text identifier</param>
        /// <returns>returns translation for this identifier</returns>
        public static string Get(string msgName, params object?[] args)
        {
            if (translations.ContainsKey(msgName))
            {
                if (args.Length > 0)
                    return string.Format(translations[msgName], args);
                else
                    return translations[msgName];
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
        public static string TryGet(string msgName, params object?[] args)
        {
            if (translations.ContainsKey(msgName))
                return Get(msgName, args);
            else
                return msgName;
        }

        /// <summary>
        /// Return a tranlation for the requested text. Support string formatting. If not found, return the original text
        /// </summary>
        /// <param name="msgName">text identifier</param>
        /// <param name="args"></param>
        /// <returns>Translated text or original text if not found</returns>
        /// <remarks>Useful when not sure msgName is a translation mapping key or a normal text</remarks>
        public static string? GetOrNull(string msgName, params object?[] args)
        {
            if (translations.ContainsKey(msgName))
                return Get(msgName, args);
            else
                return null;
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
            string[] engLang = DefaultConfigResource.Translation_en.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); // use embedded translations
            translations = ParseTranslationContent(engLang);
        }

        public static Tuple<string, string[]> GetTranslationPriority()
        {
            string gameLanguage = "en_gb";
            List<string> name = new();

            string systemLanguage = string.IsNullOrWhiteSpace(Program.ActualCulture.Name)
                    ? Program.ActualCulture.Parent.Name
                    : Program.ActualCulture.Name;

            switch (systemLanguage)
            {
                case "af":
                case "af-ZA":
                    gameLanguage = "af_za";
                    break;
                case "ar":
                case "ar-AE":
                case "ar-BH":
                case "ar-DZ":
                case "ar-EG":
                case "ar-IQ":
                case "ar-JO":
                case "ar-KW":
                case "ar-LB":
                case "ar-LY":
                case "ar-MA":
                case "ar-OM":
                case "ar-QA":
                case "ar-SA":
                case "ar-SY":
                case "ar-TN":
                case "ar-YE":
                    gameLanguage = "ar_sa";
                    break;
                case "az":
                case "az-Cyrl-AZ":
                case "az-Latn-AZ":
                    gameLanguage = "az_az";
                    break;
                case "be":
                case "be-BY":
                    gameLanguage = "be_by";
                    break;
                case "bg":
                case "bg-BG":
                    gameLanguage = "bg_bg";
                    break;
                case "bs-Latn-BA":
                    gameLanguage = "bs_ba";
                    break;
                case "ca":
                case "ca-ES":
                    gameLanguage = "ca_es";
                    break;
                case "cs":
                case "cs-CZ":
                    gameLanguage = "cs_cz";
                    break;
                case "cy-GB":
                    gameLanguage = "cy_gb";
                    break;
                case "da":
                case "da-DK":
                    gameLanguage = "da_dk";
                    break;
                case "de":
                case "de-DE":
                case "de-LI":
                case "de-LU":
                    gameLanguage = "de_de";
                    name.Add("de");
                    break;
                case "de-AT":
                    gameLanguage = "de_at";
                    name.Add("de");
                    break;
                case "de-CH":
                    gameLanguage = "de_ch";
                    name.Add("de");
                    break;
                case "dv":
                case "dv-MV":
                    break;
                case "el":
                case "el-GR":
                    gameLanguage = "el_gr";
                    break;
                case "en":
                case "en-029":
                case "en-BZ":
                case "en-IE":
                case "en-JM":
                case "en-PH":
                case "en-TT":
                case "en-ZA":
                case "en-ZW":
                case "en-GB":
                    gameLanguage = "en_gb";
                    break;
                case "en-AU":
                    gameLanguage = "en_au";
                    break;
                case "en-CA":
                    gameLanguage = "en_ca";
                    break;
                case "en-US":
                    gameLanguage = "en_us";
                    break;
                case "en-NZ":
                    gameLanguage = "en_nz";
                    break;
                case "es":
                case "es-BO":
                case "es-CO":
                case "es-CR":
                case "es-DO":
                case "es-ES":
                case "es-GT":
                case "es-HN":
                case "es-NI":
                case "es-PA":
                case "es-PE":
                case "es-PR":
                case "es-PY":
                case "es-SV":
                    gameLanguage = "es_es";
                    break;
                case "es-AR":
                    gameLanguage = "es_ar";
                    break;
                case "es-CL":
                    gameLanguage = "es_cl";
                    break;
                case "es-EC":
                    gameLanguage = "es_ec";
                    break;
                case "es-MX":
                    gameLanguage = "es_mx";
                    break;
                case "es-UY":
                    gameLanguage = "es_uy";
                    break;
                case "es-VE":
                    gameLanguage = "es_ve";
                    break;
                case "et":
                case "et-EE":
                    gameLanguage = "et_ee";
                    break;
                case "eu":
                case "eu-ES":
                    gameLanguage = "eu_es";
                    break;
                case "fa":
                case "fa-IR":
                    gameLanguage = "fa_ir";
                    break;
                case "fi":
                case "fi-FI":
                    gameLanguage = "fi_fi";
                    break;
                case "fo":
                case "fo-FO":
                    gameLanguage = "fo_fo";
                    break;
                case "fr":
                case "fr-BE":
                case "fr-FR":
                case "fr-CH":
                case "fr-LU":
                case "fr-MC":
                    gameLanguage = "fr_fr";
                    name.Add("fr");
                    break;
                case "fr-CA":
                    gameLanguage = "fr_ca";
                    name.Add("fr");
                    break;
                case "gl":
                case "gl-ES":
                    gameLanguage = "gl_es";
                    break;
                case "gu":
                case "gu-IN":
                    break;
                case "he":
                case "he-IL":
                    gameLanguage = "he_il";
                    break;
                case "hi":
                case "hi-IN":
                    gameLanguage = "hi_in";
                    break;
                case "hr":
                case "hr-BA":
                case "hr-HR":
                    gameLanguage = "hr_hr";
                    break;
                case "hu":
                case "hu-HU":
                    gameLanguage = "hu_hu";
                    break;
                case "hy":
                case "hy-AM":
                    gameLanguage = "hy_am";
                    break;
                case "id":
                case "id-ID":
                    gameLanguage = "id_id";
                    break;
                case "is":
                case "is-IS":
                    gameLanguage = "is_is";
                    break;
                case "it":
                case "it-CH":
                case "it-IT":
                    gameLanguage = "it_it";
                    break;
                case "ja":
                case "ja-JP":
                    gameLanguage = "ja_jp";
                    break;
                case "ka":
                case "ka-GE":
                    gameLanguage = "ka_ge";
                    break;
                case "kk":
                case "kk-KZ":
                    gameLanguage = "kk_kz";
                    break;
                case "kn":
                case "kn-IN":
                    gameLanguage = "kn_in";
                    break;
                case "kok":
                case "kok-IN":
                    break;
                case "ko":
                case "ko-KR":
                    gameLanguage = "ko_kr";
                    break;
                case "ky":
                case "ky-KG":
                    break;
                case "lt":
                case "lt-LT":
                    gameLanguage = "lt_lt";
                    break;
                case "lv":
                case "lv-LV":
                    gameLanguage = "lv_lv";
                    break;
                case "mi-NZ":
                    break;
                case "mk":
                case "mk-MK":
                    gameLanguage = "mk_mk";
                    break;
                case "mn":
                case "mn-MN":
                    gameLanguage = "mn_mn";
                    break;
                case "mr":
                case "mr-IN":
                    break;
                case "ms":
                case "ms-BN":
                case "ms-MY":
                    gameLanguage = "ms_my";
                    break;
                case "mt-MT":
                    gameLanguage = "mt_mt";
                    break;
                case "nb-NO":
                    break;
                case "nl":
                case "nl-NL":
                    gameLanguage = "nl_nl";
                    break;
                case "nl-BE":
                    gameLanguage = "nl_be";
                    break;
                case "nn-NO":
                    gameLanguage = "nn_no";
                    break;
                case "no":
                    gameLanguage = "no_no‌";
                    break;
                case "ns-ZA":
                    break;
                case "pa":
                case "pa-IN":
                    break;
                case "pl":
                case "pl-PL":
                    gameLanguage = "pl_pl‌";
                    break;
                case "pt":
                case "pt-PT":
                    gameLanguage = "pt_pt‌";
                    break;
                case "pt-BR":
                    gameLanguage = "pt_br‌";
                    break;
                case "quz-BO":
                    break;
                case "quz-EC":
                    break;
                case "quz-PE":
                    break;
                case "ro":
                case "ro-RO":
                    gameLanguage = "ro_ro‌";
                    break;
                case "ru":
                case "ru-RU":
                    gameLanguage = "ru_ru";
                    name.Add("ru");
                    break;
                case "sa":
                case "sa-IN":
                    break;
                case "se-FI":
                case "se-NO":
                case "se-SE":
                    gameLanguage = "se_no";
                    break;
                case "sk":
                case "sk-SK":
                    gameLanguage = "sk_sk";
                    break;
                case "sl":
                case "sl-SI":
                    gameLanguage = "sl_si";
                    break;
                case "sma-NO":
                    break;
                case "sma-SE":
                    break;
                case "smj-NO":
                    break;
                case "smj-SE":
                    break;
                case "smn-FI":
                    break;
                case "sms-FI":
                    break;
                case "sq":
                case "sq-AL":
                    gameLanguage = "sq_al";
                    break;
                case "sr":
                case "sr-Cyrl-BA":
                case "sr-Cyrl-CS":
                case "sr-Latn-BA":
                case "sr-Latn-CS":
                    gameLanguage = "sr_sp";
                    break;
                case "sv":
                case "sv-FI":
                case "sv-SE":
                    gameLanguage = "sv_se";
                    break;
                case "sw":
                case "sw-KE":
                    break;
                case "syr":
                case "syr-SY":
                    break;
                case "ta":
                case "ta-IN":
                    gameLanguage = "ta_in";
                    break;
                case "te":
                case "te-IN":
                    break;
                case "th":
                case "th-TH":
                    gameLanguage = "th_th";
                    break;
                case "tn-ZA":
                    break;
                case "tr":
                case "tr-TR":
                    gameLanguage = "tr_tr";
                    break;
                case "tt":
                case "tt-RU":
                    gameLanguage = "tt_ru";
                    break;
                case "uk":
                case "uk-UA":
                    gameLanguage = "uk_ua";
                    break;
                case "ur":
                case "ur-PK":
                    break;
                case "uz":
                case "uz-Cyrl-UZ":
                case "uz-Latn-UZ":
                    break;
                case "vi":
                case "vi-VN":
                    gameLanguage = "vi_vn";
                    name.Add("vi");
                    break;
                case "xh-ZA":
                    break;
                case "zh-Hans": /* CurrentCulture.Parent.Name */
                case "zh":
                case "zh-CN":
                case "zh-CHS":
                case "zh-SG":
                    gameLanguage = "zh_cn";
                    name.Add("zh_Hans");
                    name.Add("zh_Hant");
                    break;
                case "zh-Hant": /* CurrentCulture.Parent.Name */
                case "zh-HK":
                case "zh-CHT":
                case "zh-MO":
                    gameLanguage = "zh_hk";
                    name.Add("zh_Hant");
                    name.Add("zh_Hans");
                    break;
                case "zh-TW":
                    gameLanguage = "zh_tw";
                    name.Add("zh_Hant");
                    name.Add("zh_Hans");
                    break;
                case "zu-ZA":
                    break;
            }

            name.Add("en");

            return new(gameLanguage, name.ToArray());
        }

        public static string[] GetTranslationPriority(string gameLanguage)
        {
            List<string> name = new();

            switch (gameLanguage)
            {
                case "de_at":
                case "de_ch":
                case "de_de":
                    name.Add("de");
                    break;
                case "en_au":
                case "en_ca":
                case "en_gb":
                case "en_nz":
                case "en_pt":
                case "en_ud":
                case "en_us":
                    break;
                case "fr_ca":
                case "fr_fr":
                    name.Add("fr");
                    break;
                case "ru_ru":
                    name.Add("ru");
                    break;
                case "vi_vn":
                    name.Add("vi");
                    break;
                case "zh_cn":
                    name.Add("zh_Hans");
                    name.Add("zh_Hant");
                    break;
                case "zh_hk":
                case "zh_tw":
                    name.Add("zh_Hant");
                    name.Add("zh_Hans");
                    break;
            }

            name.Add("en");

            return name.ToArray();
        }

        /// <summary>
        /// Load translation files
        /// </summary>
        public static void LoadTranslationFile(string[] languageList)
        {
            translations = new();

            /*
             * External translation files
             * These files are loaded from the installation directory as:
             * Lang/abc.ini, e.g. Lang/eng.ini which is the default language file
             * Useful for adding new translations of fixing typos without recompiling
             */
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string langDir = baseDir + ((baseDir.EndsWith(Path.DirectorySeparatorChar) ? String.Empty : Path.DirectorySeparatorChar) +
                translationFilePath + Path.DirectorySeparatorChar);

            foreach (string lang in languageList)
            {
                bool fileLoaded = false;
                string langFileName = string.Format("{0}{1}.ini", langDir, lang);

                if (File.Exists(langFileName)) // Language set in ini config
                {
                    fileLoaded = true;
                    Dictionary<string, string> trans = ParseTranslationContent(File.ReadAllLines(langFileName));
                    foreach ((string key, string value) in trans)
                        if (!string.IsNullOrWhiteSpace(value) && !translations.ContainsKey(key))
                            translations.Add(key, value);
                }

                string? resourseLangFile = DefaultConfigResource.ResourceManager.GetString("Translation_" + lang);
                if (resourseLangFile != null)
                {
                    fileLoaded = true;
                    Dictionary<string, string> trans = ParseTranslationContent(resourseLangFile.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
                    foreach ((string key, string value) in trans)
                        if (!string.IsNullOrWhiteSpace(value) && !translations.ContainsKey(key))
                            translations.Add(key, value);
                }

                if (!fileLoaded)
                {
                    if (Settings.Config.Logging.DebugMessages)
                        ConsoleIO.WriteLogLine("[Translations] No translation file found for " + lang + ". (Looked '" + langFileName + "'");
                }
            }
        }

        /// <summary>
        /// Parse the given array to translation map
        /// </summary>
        /// <param name="content">Content of the translation file (in ini format)</param>
        private static Dictionary<string, string> ParseTranslationContent(string[] content)
        {
            Dictionary<string, string> translations = new();
            foreach (string lineRaw in content)
            {
                string line = lineRaw.Trim();
                if (line.Length < 3)
                    continue;
                if (!char.IsLetterOrDigit(line[0])) // ignore comment line started with #
                    continue;

                int index = line.IndexOf('=');
                if (index != -1 && line.Length > (index + 1))
                    translations[line[..index]] = line[(index + 1)..].Replace("\\n", "\n");
            }
            return translations;
        }

        public static void TrimAllTranslations()
        {
            string[] transEn = DefaultConfigResource.ResourceManager.GetString("Translation_en")!
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string lang in new string[] { "de", "fr", "ru", "vi", "zh_Hans", "zh_Hant" })
            {
                Dictionary<string, string> trans = ParseTranslationContent(
                    DefaultConfigResource.ResourceManager.GetString("Translation_" + lang)!
                        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                );
                string fileName = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar + lang + ".ini";
                if (File.Exists(fileName))
                {
                    string backupFilePath = Path.ChangeExtension(fileName, ".backup.ini");
                    try { File.Copy(fileName, backupFilePath, true); }
                    catch (Exception ex)
                    {
                        ConsoleIO.WriteLineFormatted(Translations.TryGet("config.backup.fail", backupFilePath));
                        ConsoleIO.WriteLine(ex.Message);
                        return;
                    }
                }
                using FileStream file = File.OpenWrite(fileName);
                int total = 0, translated = 0;
                for (int i = 0; i < transEn.Length; ++i)
                {
                    string line = transEn[i].Trim();
                    int index = transEn[i].IndexOf('=');
                    if (line.Length < 3 || !char.IsLetterOrDigit(line[0]) || index == -1 || line.Length <= (index + 1))
                    {
                        file.Write(Encoding.UTF8.GetBytes(line));
                    }
                    else
                    {
                        string key = line[..index];
                        file.Write(Encoding.UTF8.GetBytes(key));
                        file.Write(Encoding.UTF8.GetBytes("="));
                        if (trans.TryGetValue(key, out string? value))
                        {
                            file.Write(Encoding.UTF8.GetBytes(value.Replace("\n", "\\n")));
                            ++total;
                            ++translated;
                        }
                        else
                        {
                            ++total;
                        }
                    }
                    file.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
                }
                ConsoleIO.WriteLine(string.Format("Language {0}: Translated {1} of {2}, {3:0.00}%", lang, translated, total, 100.0 * (double)translated / total));
            }
        }

        #region Console writing method wrapper

        /// <summary>
        /// Translate the key and write the result to the standard output, without newline character
        /// </summary>
        /// <param name="key">Translation key</param>
        public static void Write(string key)
        {
            ConsoleIO.WriteLine(Get(key));
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