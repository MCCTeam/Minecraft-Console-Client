using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using MinecraftClient.Protocol;
using MinecraftClient.Proxy;
using Tomlet;
using Tomlet.Attributes;
using Tomlet.Models;
using static MinecraftClient.Settings.AppVarConfigHelper;
using static MinecraftClient.Settings.ChatBotConfigHealper;
using static MinecraftClient.Settings.ChatFormatConfigHelper;
using static MinecraftClient.Settings.HeadCommentHealper;
using static MinecraftClient.Settings.LoggingConfigHealper;
using static MinecraftClient.Settings.MainConfigHealper;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.AdvancedConfig;
using static MinecraftClient.Settings.MCSettingsConfigHealper;
using static MinecraftClient.Settings.SignatureConfigHelper;

namespace MinecraftClient
{
    public static class Settings
    {
        private const int CommentsAlignPosition = 45;
        private readonly static Regex CommentRegex = new(@"^(.*)\s?#\s\$([\w\.]+)\$\s*$$", RegexOptions.Compiled);

        //Other Settings
        public static string TranslationsFile_FromMCDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\.minecraft\assets\objects\48\482e0dae05abfa35ab5cb076e41fda77b4fb9a08"; //MC 1.19 en_GB.lang
        public static string TranslationsFile_Website_Index = "https://piston-meta.mojang.com/v1/packages/b5c7548ddb9e584e84a5f762da5b78211c715a63/1.19.json";
        public static string TranslationsFile_Website_Download = "http://resources.download.minecraft.net";

        public const string TranslationDocUrl = "https://mccteam.github.io/guide/contibuting.html#translations";
        public const string GithubReleaseUrl = "https://github.com/MCCTeam/Minecraft-Console-Client/releases";
        public const string GithubLatestReleaseUrl = GithubReleaseUrl + "/latest";

        public static GlobalConfig Config = new();

        public static class InternalConfig
        {
            public static string ServerIP = String.Empty;

            public static ushort ServerPort = 25565;

            public static string Username = string.Empty;

            public static string Password = string.Empty;

            public static string MinecraftVersion = string.Empty;

            public static bool InteractiveMode = true;

            public static bool GravityEnabled = true;
        }

        public class GlobalConfig
        {
            [TomlPrecedingComment("$config.Head$")]
            public HeadComment Head
            {
                get { return HeadCommentHealper.Config; }
                set { HeadCommentHealper.Config = value; HeadCommentHealper.Config.OnSettingUpdate(); }
            }

            public MainConfig Main
            {
                get { return MainConfigHealper.Config; }
                set { MainConfigHealper.Config = value; MainConfigHealper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$config.Signature$")]
            public SignatureConfig Signature
            {
                get { return SignatureConfigHelper.Config; }
                set { SignatureConfigHelper.Config = value; SignatureConfigHelper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$config.Logging$")]
            public LoggingConfig Logging
            {
                get { return LoggingConfigHealper.Config; }
                set { LoggingConfigHealper.Config = value; LoggingConfigHealper.Config.OnSettingUpdate(); }
            }

            public AppVarConfig AppVar
            {
                get { return AppVarConfigHelper.Config; }
                set { AppVarConfigHelper.Config = value; AppVarConfigHelper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$config.Proxy$")]
            public ProxyHandler.Configs Proxy
            {
                get { return ProxyHandler.Config; }
                set { ProxyHandler.Config = value; ProxyHandler.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$config.MCSettings$")]
            public MCSettingsConfig MCSettings
            {
                get { return MCSettingsConfigHealper.Config; }
                set { MCSettingsConfigHealper.Config = value; MCSettingsConfigHealper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$config.ChatFormat$")]
            public ChatFormatConfig ChatFormat
            {
                get { return ChatFormatConfigHelper.Config; }
                set { ChatFormatConfigHelper.Config = value; ChatFormatConfigHelper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$config.ChatBot$")]
            public ChatBotConfig ChatBot
            {
                get { return ChatBotConfigHealper.Config; }
                set { ChatBotConfigHealper.Config = value; }
            }

        }

        public static Tuple<bool, bool> LoadFromFile(string filepath)
        {
            TomlDocument document;
            try
            {
                document = TomlParser.ParseFile(filepath);
                Config = TomletMain.To<GlobalConfig>(document);
            }
            catch (Exception ex)
            {
                try
                {
                    // The old configuration file has been backed up as A.
                    string configString = File.ReadAllText(filepath);
                    if (configString.Contains("Some settings missing here after an upgrade?"))
                    {
                        string newFilePath = Path.ChangeExtension(filepath, ".backup.ini");
                        File.Copy(filepath, newFilePath, true);
                        ConsoleIO.WriteLineFormatted("§cPlease use the newly generated MinecraftClient.ini");
                        ConsoleIO.WriteLineFormatted("§cThe old MinecraftClient.ini has been backed up as " + newFilePath);
                        ConsoleIO.WriteLine(Translations.GetOrNull("mcc.run_with_default_settings") ?? "\nMCC is running with default settings.");
                        return new(false, true);
                    }
                }
                catch { }
                ConsoleIO.WriteLineFormatted(Translations.GetOrNull("config.load.fail") ?? "§cFailed to load settings:§r");
                ConsoleIO.WriteLine(ex.GetFullMessage());
                return new(false, false);
            }
            return new(true, false);
        }

        public static void WriteToFile(string filepath, bool backupOldFile)
        {
            string tomlString = TomletMain.TomlStringFrom(Config);

            string[] tomlList = tomlString.Split('\n');
            StringBuilder newConfig = new();
            foreach (string line in tomlList)
            {
                Match matchComment = CommentRegex.Match(line);
                if (matchComment.Success && matchComment.Groups.Count == 3)
                {
                    string config = matchComment.Groups[1].Value, comment = matchComment.Groups[2].Value;
                    if (config.Length > 0)
                        newConfig.Append(config).Append(' ', Math.Max(1, CommentsAlignPosition - config.Length) - 1);
                    newConfig.Append("# ").AppendLine(Translations.TryGet(comment).ReplaceLineEndings());
                }
                else
                {
                    newConfig.AppendLine(line);
                }
            }

            bool needUpdate = true;
            byte[] newConfigByte = Encoding.UTF8.GetBytes(newConfig.ToString());
            if (File.Exists(filepath))
            {
                try
                {
                    if (new FileInfo(filepath).Length == newConfigByte.Length)
                        if (File.ReadAllBytes(filepath).SequenceEqual(newConfigByte))
                            needUpdate = false;
                }
                catch { }
            }

            if (needUpdate)
            {
                bool backupSuccessed = true;
                if (backupOldFile && File.Exists(filepath))
                {
                    string backupFilePath = Path.ChangeExtension(filepath, ".backup.ini");
                    try { File.Copy(filepath, backupFilePath, true); }
                    catch (Exception ex)
                    {
                        backupSuccessed = false;
                        ConsoleIO.WriteLineFormatted(Translations.TryGet("config.backup.fail", backupFilePath));
                        ConsoleIO.WriteLine(ex.Message);
                    }
                }

                if (backupSuccessed)
                {
                    try { File.WriteAllBytes(filepath, newConfigByte); }
                    catch (Exception ex)
                    {
                        ConsoleIO.WriteLineFormatted(Translations.TryGet("config.write.fail", filepath));
                        ConsoleIO.WriteLine(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Load settings from the command line
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <exception cref="System.ArgumentException">Thrown on invalid arguments</exception>
        public static void LoadArguments(string[] args)
        {
            int positionalIndex = 0;

            foreach (string argument in args)
            {
                if (argument.StartsWith("--"))
                {
                    //Load settings as --setting=value and --section.setting=value
                    if (!argument.Contains('='))
                        throw new ArgumentException(Translations.Get("error.setting.argument_syntax", argument));
                    throw new NotImplementedException();
                }
                else if (argument.StartsWith("-") && argument.Length > 1)
                {
                    //Keep single dash arguments as unsupported for now (future use)
                    throw new ArgumentException(Translations.Get("error.setting.argument_syntax", argument));
                }
                else
                {
                    switch (positionalIndex)
                    {
                        case 0: 
                            Config.Main.General.Account.Login = argument;
                            break;
                        case 1: 
                            InternalConfig.Password = argument; 
                            break;
                        case 2: 
                            Config.Main.SetServerIP(new MainConfig.ServerInfoConfig(argument), true); 
                            break;
                        case 3: 
                            // SingleCommand = argument; 
                            break;
                    }
                    positionalIndex++;
                }
            }
        }

        public static class HeadCommentHealper
        {
            public static HeadComment Config = new();

            [TomlDoNotInlineObject]
            public class HeadComment
            {
                [TomlProperty("Current Version")]
                public string CurrentVersion { get; set; } = Program.BuildInfo ?? "Development Build";
                
                [TomlProperty("Latest Version")]
                public string LatestVersion { get; set; } = "Unknown";

                public void OnSettingUpdate()
                {
                    CurrentVersion = Program.BuildInfo ?? "Development Build";
                    LatestVersion ??= "Unknown";
                }
            }
        }

        public static class MainConfigHealper
        {
            public static MainConfig Config = new();

            [TomlDoNotInlineObject]
            public class MainConfig
            {
                public GeneralConfig General = new();

                [TomlPrecedingComment("$config.Main.Advanced$")]
                public AdvancedConfig Advanced = new();


                [NonSerialized]
                public static readonly string[] AvailableLang =
                {
                    "af_za", "ar_sa", "ast_es", "az_az", "ba_ru", "bar", "be_by", "bg_bg", "br_fr", "brb", "bs_ba", "ca_es", 
                    "cs_cz", "cy_gb", "da_dk", "de_at", "de_ch", "de_de", "el_gr", "en_au", "en_ca", "en_gb", "en_nz", "eo_uy", 
                    "es_ar", "es_cl", "es_ec", "es_es", "es_mx", "es_uy", "es_ve", "esan", "et_ee", "eu_es", "fa_ir", "fi_fi", 
                    "fil_ph", "fo_fo", "fr_ca", "fr_fr", "fra_de", "fur_it", "fy_nl", "ga_ie", "gd_gb", "gl_es", "haw_us", "he_il", 
                    "hi_in", "hr_hr", "hu_hu", "hy_am", "id_id", "ig_ng", "io_en", "is_is", "isv", "it_it", "ja_jp", "jbo_en", 
                    "ka_ge", "kk_kz", "kn_in", "ko_kr", "ksh", "kw_gb", "la_la", "lb_lu", "li_li", "lmo", "lt_lt", "lv_lv", "lzh", 
                    "mk_mk", "mn_mn", "ms_my", "mt_mt", "nds_de", "nl_be", "nl_nl", "nn_no", "oc_fr", "ovd", "pl_pl", "pt_br", 
                    "pt_pt", "qya_aa", "ro_ro", "rpr", "ru_ru", "se_no", "sk_sk", "sl_si", "so_so", "sq_al", "sr_sp", "sv_se", 
                    "sxu", "szl", "ta_in", "th_th", "tl_ph", "tlh_aa", "tok", "tr_tr", "tt_ru", "uk_ua", "val_es", "vec_it", 
                    "vi_vn", "yi_de", "yo_ng", "zh_cn", "zh_hk", "zh_tw", "zlm_arab"
                };

                /// <summary>
                /// Load server information in ServerIP and ServerPort variables from a "serverip:port" couple or server alias
                /// </summary>
                /// <returns>True if the server IP was valid and loaded, false otherwise</returns>
                public bool SetServerIP(ServerInfoConfig serverInfo, bool checkAlias)
                {
                    string serverStr = ToLowerIfNeed(serverInfo.Host);
                    string[] sip = serverStr.Split(new[] { ":", "：" }, StringSplitOptions.None);
                    string host = sip[0];
                    ushort port = 25565;

                    if (sip.Length > 1)
                    {
                        if (serverInfo.Port != null)
                        {
                            port = (ushort)serverInfo.Port;
                        }
                        else
                        {
                            try { port = Convert.ToUInt16(sip[1]); }
                            catch (FormatException) { return false; }
                        }
                    }

                    if (host == "localhost" || host.Contains('.'))
                    {
                        //Server IP (IP or domain names contains at least a dot)
                        if (sip.Length == 1 && serverInfo.Port == null && host.Contains('.') && host.Any(c => char.IsLetter(c)) &&
                            Settings.Config.Main.Advanced.ResolveSrvRecords != MainConfigHealper.MainConfig.AdvancedConfig.ResolveSrvRecordType.no)
                            //Domain name without port may need Minecraft SRV Record lookup
                            ProtocolHandler.MinecraftServiceLookup(ref host, ref port);
                        InternalConfig.ServerIP = host;
                        InternalConfig.ServerPort = port;
                        return true;
                    }
                    else if (checkAlias && Advanced.ServerList.TryGetValue(serverStr, out ServerInfoConfig serverStr2))
                    {
                        return SetServerIP(serverStr2, false);
                    }

                    return false;
                }

                public void OnSettingUpdate()
                {
                    ConsoleIO.EnableTimestamps = Advanced.Timestamps;

                    InternalConfig.InteractiveMode = !Advanced.ExitOnFailure;

                    General.Account.Login ??= string.Empty;
                    General.Account.Password ??= string.Empty;

                    General.Server.Host ??= string.Empty;

                    if (Advanced.MessageCooldown < 0)
                        Advanced.MessageCooldown = 0;

                    if (Advanced.TcpTimeout < 1)
                        Advanced.TcpTimeout = 1;

                    if (Advanced.MovementSpeed < 1)
                        Advanced.MovementSpeed = 1;

                    Advanced.Language = Regex.Replace(Advanced.Language, @"[^-^_^\w^*\d]", string.Empty).Replace('-', '_');
                    Advanced.Language = ToLowerIfNeed(Advanced.Language);
                    if (!AvailableLang.Contains(Advanced.Language))
                    {
                        Advanced.Language = Translations.GetTranslationPriority().Item1;
                        ConsoleIO.WriteLogLine("[Settings] " + (Translations.GetOrNull("config.Main.Advanced.language.invaild") ?? "The language code is invalid."));
                    }

                    if (!string.IsNullOrWhiteSpace(General.Server.Host))
                    {
                        string[] sip = General.Server.Host.Split(new[] { ":", "：" }, StringSplitOptions.None);
                        General.Server.Host = sip[0];

                        if (sip.Length > 1)
                        {
                            try { General.Server.Port = Convert.ToUInt16(sip[1]); }
                            catch (FormatException) { }
                        }
                    }

                    SetServerIP(General.Server, true);

                    for (int i = 0; i < Advanced.BotOwners.Count; ++i)
                        Advanced.BotOwners[i] = ToLowerIfNeed(Advanced.BotOwners[i]);
                }

                [TomlDoNotInlineObject]
                public class GeneralConfig
                {
                    [TomlInlineComment("$config.Main.General.account$")]
                    public AccountInfoConfig Account = new(string.Empty, string.Empty);

                    [TomlInlineComment("$config.Main.General.login$")]
                    public ServerInfoConfig Server = new(string.Empty);

                    [TomlInlineComment("$config.Main.General.server_info$")]
                    public LoginType AccountType = LoginType.microsoft;

                    [TomlInlineComment("$config.Main.General.method$")]
                    public LoginMethod Method = LoginMethod.mcc;

                    public enum LoginType { mojang, microsoft };

                    public enum LoginMethod { mcc, browser };
                }

                [TomlDoNotInlineObject]
                public class AdvancedConfig
                {
                    [TomlInlineComment("$config.Main.Advanced.language$")]
                    public string Language = "en_gb";

                    // [TomlInlineComment("$config.Main.Advanced.console_title$")]
                    public string ConsoleTitle = "%username%@%serverip% - Minecraft Console Client";

                    [TomlInlineComment("$config.Main.Advanced.internal_cmd_char$")]
                    public InternalCmdCharType InternalCmdChar = InternalCmdCharType.slash;

                    [TomlInlineComment("$config.Main.Advanced.message_cooldown$")]
                    public double MessageCooldown = 1.0;

                    [TomlInlineComment("$config.Main.Advanced.bot_owners$")]
                    public List<string> BotOwners = new() { "Player1", "Player2" };

                    [TomlInlineComment("$config.Main.Advanced.mc_version$")]
                    public string MinecraftVersion = "auto";

                    [TomlInlineComment("$config.Main.Advanced.mc_forge$")]
                    public ForgeConfigType EnableForge = ForgeConfigType.auto;

                    [TomlInlineComment("$config.Main.Advanced.brand_info$")]
                    public BrandInfoType BrandInfo = BrandInfoType.mcc;

                    [TomlInlineComment("$config.Main.Advanced.chatbot_log_file$")]
                    public string ChatbotLogFile = "";

                    [TomlInlineComment("$config.Main.Advanced.private_msgs_cmd_name$")]
                    public string PrivateMsgsCmdName = "tell";

                    [TomlInlineComment("$config.Main.Advanced.show_system_messages$")]
                    public bool ShowSystemMessages = true;

                    [TomlInlineComment("$config.Main.Advanced.show_xpbar_messages$")]
                    public bool ShowXPBarMessages = true;

                    [TomlInlineComment("$config.Main.Advanced.show_chat_links$")]
                    public bool ShowChatLinks = true;

                    [TomlInlineComment("$config.Main.Advanced.show_inventory_layout$")]
                    public bool ShowInventoryLayout = true;

                    [TomlInlineComment("$config.Main.Advanced.terrain_and_movements$")]
                    public bool TerrainAndMovements = false;

                    [TomlInlineComment("$config.Main.Advanced.inventory_handling$")]
                    public bool InventoryHandling = false;

                    [TomlInlineComment("$config.Main.Advanced.entity_handling$")]
                    public bool EntityHandling = false;

                    [TomlInlineComment("$config.Main.Advanced.session_cache$")]
                    public CacheType SessionCache = CacheType.disk;

                    [TomlInlineComment("$config.Main.Advanced.profilekey_cache$")]
                    public CacheType ProfileKeyCache = CacheType.disk;

                    [TomlInlineComment("$config.Main.Advanced.resolve_srv_records$")]
                    public ResolveSrvRecordType ResolveSrvRecords = ResolveSrvRecordType.fast;

                    [TomlPrecedingComment("$config.Main.Advanced.account_list$")]
                    public Dictionary<string, AccountInfoConfig> AccountList = new() {
                        { "AccountNikename1", new AccountInfoConfig("playerone@email.com", "thepassword") },
                        { "AccountNikename2", new AccountInfoConfig("TestBot", "-") },
                    };

                    [TomlPrecedingComment("$config.Main.Advanced.server_list$")]
                    public Dictionary<string, ServerInfoConfig> ServerList = new() {
                        { "ServerAlias1", new ServerInfoConfig("mc.awesomeserver.com") },
                        { "ServerAlias2", new ServerInfoConfig("192.168.1.27", 12345) },
                    };

                    [TomlInlineComment("$config.Main.Advanced.player_head_icon$")]
                    public bool PlayerHeadAsIcon = true;

                    [TomlInlineComment("$config.Main.Advanced.exit_on_failure$")]
                    public bool ExitOnFailure = false;

                    [TomlInlineComment("$config.Main.Advanced.script_cache$")]
                    public bool CacheScript = true;

                    [TomlInlineComment("$config.Main.Advanced.timestamps$")]
                    public bool Timestamps = false;

                    [TomlInlineComment("$config.Main.Advanced.auto_respawn$")]
                    public bool AutoRespawn = false;

                    [TomlInlineComment("$config.Main.Advanced.minecraft_realms$")]
                    public bool MinecraftRealms = false;

                    [TomlInlineComment("$config.Main.Advanced.move_head_while_walking$")]
                    public bool MoveHeadWhileWalking = true;

                    [TomlInlineComment("$config.Main.Advanced.timeout$")]
                    public int TcpTimeout = 30;

                    [TomlInlineComment("$config.Main.Advanced.enable_emoji$")]
                    public bool EnableEmoji = true;

                    [TomlInlineComment("$config.Main.Advanced.movement_speed$")]
                    public int MovementSpeed = 2;

                    /// <summary>
                    /// Load login/password using an account alias
                    /// </summary>
                    /// <returns>True if the account was found and loaded</returns>
                    public bool SetAccount(string accountAlias)
                    {
                        if (AccountList.TryGetValue(accountAlias, out AccountInfoConfig accountInfo))
                        {
                            Settings.Config.Main.General.Account = accountInfo;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    public enum InternalCmdCharType { none, slash, backslash };

                    public enum BrandInfoType { mcc, vanilla, empty };

                    public enum CacheType { none, memory, disk };

                    public enum ResolveSrvRecordType { no, fast, yes };

                    public enum ForgeConfigType { no, auto, force };
                }

                public struct AccountInfoConfig
                {
                    public string Login = string.Empty, Password = string.Empty;

                    public AccountInfoConfig(string Login)
                    {
                        this.Login = Login;
                        this.Password = "-";
                    }

                    public AccountInfoConfig(string Login, string Password)
                    {
                        this.Login = Login;
                        this.Password = Password;
                    }
                }

                public struct ServerInfoConfig
                {
                    public string Host = string.Empty;
                    public ushort? Port = null;

                    public ServerInfoConfig(string Host)
                    {
                        string[] sip = Host.Split(new[] { ":", "：" }, StringSplitOptions.None);
                        this.Host = sip[0];

                        if (sip.Length > 1)
                        {
                            try { this.Port = Convert.ToUInt16(sip[1]); }
                            catch (FormatException) { }
                        }
                    }

                    public ServerInfoConfig(string Host, ushort Port)
                    {
                        this.Host = Host.Split(new[] { ":", "：" }, StringSplitOptions.None)[0];
                        this.Port = Port;
                    }
                }
            }
        }

        public static class SignatureConfigHelper
        {
            public static SignatureConfig Config = new();

            [TomlDoNotInlineObject]
            public class SignatureConfig
            {
                [TomlInlineComment("$config.Signature.LoginWithSecureProfile$")]
                public bool LoginWithSecureProfile = true;

                [TomlInlineComment("$config.Signature.SignChat$")]
                public bool SignChat = true;

                [TomlInlineComment("$config.Signature.SignMessageInCommand$")]
                public bool SignMessageInCommand = true;

                [TomlInlineComment("$config.Signature.MarkLegallySignedMsg$")]
                public bool MarkLegallySignedMsg = false;

                [TomlInlineComment("$config.Signature.MarkModifiedMsg$")]
                public bool MarkModifiedMsg = true;

                [TomlInlineComment("$config.Signature.MarkIllegallySignedMsg$")]
                public bool MarkIllegallySignedMsg = true;

                [TomlInlineComment("$config.Signature.MarkSystemMessage$")]
                public bool MarkSystemMessage = false;

                [TomlInlineComment("$config.Signature.ShowModifiedChat$")]
                public bool ShowModifiedChat = true;

                [TomlInlineComment("$config.Signature.ShowIllegalSignedChat$")]
                public bool ShowIllegalSignedChat = true;

                public void OnSettingUpdate() { }
            }
        }

        public static class LoggingConfigHealper
        {
            public static LoggingConfig Config = new();

            [TomlDoNotInlineObject]
            public class LoggingConfig
            {
                [TomlInlineComment("$config.Logging.DebugMessages$")]
                public bool DebugMessages = false;

                [TomlInlineComment("$config.Logging.ChatMessages$")]
                public bool ChatMessages = true;

                [TomlInlineComment("$config.Logging.InfoMessages$")]
                public bool InfoMessages = true;

                [TomlInlineComment("$config.Logging.WarningMessages$")]
                public bool WarningMessages = true;

                [TomlInlineComment("$config.Logging.ErrorMessages$")]
                public bool ErrorMessages = true;

                [TomlInlineComment("$config.Logging.ChatFilter$")]
                public string ChatFilterRegex = @".*";

                [TomlInlineComment("$config.Logging.DebugFilter$")]
                public string DebugFilterRegex = @".*";

                [TomlInlineComment("$config.Logging.FilterMode$")]
                public FilterModeEnum FilterMode = FilterModeEnum.disable;

                [TomlInlineComment("$config.Logging.LogToFile$")]
                public bool LogToFile = false;

                [TomlInlineComment("$config.Logging.LogFile$")]
                public string LogFile = @"console-log.txt";

                [TomlInlineComment("$config.Logging.PrependTimestamp$")]
                public bool PrependTimestamp = false;

                [TomlInlineComment("$config.Logging.SaveColorCodes$")]
                public bool SaveColorCodes = false;

                public void OnSettingUpdate() { }

                public enum FilterModeEnum { disable, blacklist, whitelist }
            }
        }

        public static class AppVarConfigHelper
        {
            public static AppVarConfig Config = new();

            [TomlDoNotInlineObject]
            public class AppVarConfig
            {
                [TomlPrecedingComment("$config.AppVars.Variables$")]
                private readonly Dictionary<string, string> VarStirng = new() {
                    { "your_var", "your_value" },
                    { "your var 2", "your value 2" },
                };

                public void OnSettingUpdate() { }


                [NonSerialized]
                private readonly Dictionary<string, object> VarObject = new();

                [NonSerialized]
                readonly object varLock = new();

                /// <summary>
                /// Set a custom %variable% which will be available through expandVars()
                /// </summary>
                /// <param name="varName">Name of the variable</param>
                /// <param name="varData">Value of the variable</param>
                /// <returns>True if the parameters were valid</returns>
                public bool SetVar(string varName, object varData)
                {
                    varName = Settings.ToLowerIfNeed(new string(varName.TakeWhile(char.IsLetterOrDigit).ToArray()));
                    if (varName.Length > 0)
                    {
                        bool isString = varData.GetType() == typeof(string);
                        lock (varLock)
                        {
                            if (isString)
                            {
                                if (VarObject.ContainsKey(varName))
                                    VarObject.Remove(varName);
                                VarStirng[varName] = (string)varData;
                            }
                            else
                            {
                                if (VarStirng.ContainsKey(varName))
                                    VarStirng.Remove(varName);
                                VarObject[varName] = varData;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;  
                    }
                }

                /// <summary>
                /// Get a custom %variable% or null if the variable does not exist
                /// </summary>
                /// <param name="varName">Variable name</param>
                /// <returns>The value or null if the variable does not exists</returns>
                public object? GetVar(string varName)
                {
                    if (VarStirng.TryGetValue(varName, out string? valueString))
                        return valueString;
                    else if (VarObject.TryGetValue(varName, out object? valueObject))
                        return valueObject;
                    else
                        return null;
                }

                /// <summary>
                /// Get a custom %variable% or null if the variable does not exist
                /// </summary>
                /// <param name="varName">Variable name</param>
                /// <returns>The value or null if the variable does not exists</returns>
                public bool TryGetVar(string varName, [NotNullWhen(true)] out object? varData)
                {
                    if (VarStirng.TryGetValue(varName, out string? valueString))
                    {
                        varData = valueString;
                        return true;
                    }
                    else if (VarObject.TryGetValue(varName, out object? valueObject))
                    {
                        varData = valueObject;
                        return true;
                    }
                    else
                    {
                        varData = null;
                        return false;
                    }
                }

                /// <summary>
                /// Get a dictionary containing variables (names and value)
                /// </summary>
                /// <returns>A IDictionary<string, object> containing a name and a vlaue key pairs of variables</returns>
                public Dictionary<string, object> GetVariables()
                {
                    Dictionary<string, object> res =  new(VarObject);
                    foreach ((string varName, string varData) in VarStirng)
                        res.Add(varName, varData);
                    return res;
                }

                /// <summary>
                /// Replace %variables% with their value from global AppVars
                /// </summary>
                /// <param name="str">String to parse</param>
                /// <param name="localContext">Optional local variables overriding global variables</param>
                /// <returns>Modifier string</returns>
                public string ExpandVars(string str, Dictionary<string, object>? localVars = null)
                {
                    StringBuilder result = new();
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (str[i] == '%')
                        {
                            bool varname_ok = false;
                            StringBuilder var_name = new();

                            for (int j = i + 1; j < str.Length; j++)
                            {
                                if (!char.IsLetterOrDigit(str[j]) && str[j] != '_')
                                {
                                    if (str[j] == '%')
                                        varname_ok = var_name.Length > 0;
                                    break;
                                }
                                else var_name.Append(str[j]);
                            }

                            if (varname_ok)
                            {
                                string varname = var_name.ToString();
                                string varname_lower = Settings.ToLowerIfNeed(varname);
                                i = i + varname.Length + 1;

                                switch (varname_lower)
                                {
                                    case "username": result.Append(InternalConfig.Username); break;
                                    case "login": result.Append(Settings.Config.Main.General.Account.Login); break;
                                    case "serverip": result.Append(InternalConfig.ServerIP); break;
                                    case "serverport": result.Append(InternalConfig.ServerPort); break;
                                    case "datetime":
                                        DateTime time = DateTime.Now;
                                        result.Append(String.Format("{0}-{1}-{2} {3}:{4}:{5}",
                                            time.Year.ToString("0000"),
                                            time.Month.ToString("00"),
                                            time.Day.ToString("00"),
                                            time.Hour.ToString("00"),
                                            time.Minute.ToString("00"),
                                            time.Second.ToString("00")));

                                        break;
                                    default:
                                        if (localVars != null && localVars.ContainsKey(varname_lower))
                                            result.Append(localVars[varname_lower].ToString());
                                        else if (TryGetVar(varname_lower, out object? var_value))
                                            result.Append(var_value.ToString());
                                        else
                                            result.Append("%" + varname + '%');
                                        break;
                                }
                            }
                            else result.Append(str[i]);
                        }
                        else result.Append(str[i]);
                    }
                    return result.ToString();
                }
            }
        }

        public static class MCSettingsConfigHealper
        {
            public static MCSettingsConfig Config = new();

            [TomlDoNotInlineObject]
            public class MCSettingsConfig
            {
                [TomlInlineComment("$config.MCSettings.Enabled$")]
                public bool Enabled = true;

                [TomlInlineComment("$config.MCSettings.Locale$")]
                public string Locale = "en_US";

                [TomlInlineComment("$config.MCSettings.RenderDistance$")]
                public byte RenderDistance = 8;

                [TomlInlineComment("$config.MCSettings.Difficulty$")]
                public DifficultyType Difficulty = DifficultyType.peaceful;

                [TomlInlineComment("$config.MCSettings.ChatMode$")]
                public ChatModeType ChatMode = ChatModeType.enabled;

                [TomlInlineComment("$config.MCSettings.ChatColors$")]
                public bool ChatColors = true;

                [TomlInlineComment("$config.MCSettings.MainHand$")]
                public MainHandType MainHand = MainHandType.left;

                public SkinInfo Skin = new();

                public void OnSettingUpdate() { }

                public enum DifficultyType { peaceful, easy, normal, difficult };

                public enum ChatModeType { enabled, commands, disabled };

                public enum MainHandType { left, right };

                public struct SkinInfo
                {
                    public bool Cape = true, Hat = true, Jacket = false;
                    public bool Sleeve_Left = false, Sleeve_Right = false;
                    public bool Pants_Left = false, Pants_Right = false;

                    public SkinInfo() { }

                    public SkinInfo(bool Cape, bool Hat, bool Jacket, bool Sleeve_Left, bool Sleeve_Right, bool Pants_Left, bool Pants_Right)
                    {
                        this.Cape = Cape;
                        this.Hat = Hat;
                        this.Jacket = Jacket;
                        this.Sleeve_Left = Sleeve_Left;
                        this.Sleeve_Right = Sleeve_Right;
                        this.Pants_Left = Pants_Left;
                        this.Pants_Right = Pants_Right;
                    }

                    public byte GetByte()
                    {
                        return (byte)(
                              ((Cape ? 1 : 0) << 0)
                            | ((Jacket ? 1 : 0) << 1)
                            | ((Sleeve_Left ? 1 : 0) << 2)
                            | ((Sleeve_Right ? 1 : 0) << 3)
                            | ((Pants_Left ? 1 : 0) << 4)
                            | ((Pants_Right ? 1 : 0) << 5)
                            | ((Hat ? 1 : 0) << 6)
                        );
                    }
                }
            }
        }

        public static class ChatFormatConfigHelper
        {
            public static ChatFormatConfig Config = new();

            [TomlDoNotInlineObject]
            public class ChatFormatConfig
            {
                [TomlInlineComment("$config.ChatFormat.Builtins$")]
                public bool Builtins = true;

                [TomlInlineComment("$config.ChatFormat.UserDefined$")]
                public bool UserDefined = false;

                public string Public = @"^<([a-zA-Z0-9_]+)> (.+)$";

                public string Private = @"^([a-zA-Z0-9_]+) whispers to you: (.+)$";

                public string TeleportRequest = @"^([a-zA-Z0-9_]+) has requested (?:to|that you) teleport to (?:you|them)\.$";

                public void OnSettingUpdate() { }
            }
        }

        public static class ChatBotConfigHealper
        {
            public static ChatBotConfig Config = new();

            [TomlDoNotInlineObject]
            public class ChatBotConfig
            {
                [TomlPrecedingComment("$config.ChatBot.Alerts$")]
                public ChatBots.Alerts.Configs Alerts
                {
                    get { return ChatBots.Alerts.Config; }
                    set { ChatBots.Alerts.Config = value; ChatBots.Alerts.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AntiAfk$")]
                public ChatBots.AntiAFK.Configs AntiAFK
                {
                    get { return ChatBots.AntiAFK.Config; }
                    set { ChatBots.AntiAFK.Config = value; ChatBots.AntiAFK.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AutoAttack$")]
                public ChatBots.AutoAttack.Configs AutoAttack
                {
                    get { return ChatBots.AutoAttack.Config; }
                    set { ChatBots.AutoAttack.Config = value; ChatBots.AutoAttack.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AutoCraft$")]
                public ChatBots.AutoCraft.Configs AutoCraft
                {
                    get { return ChatBots.AutoCraft.Config; }
                    set { ChatBots.AutoCraft.Config = value; ChatBots.AutoCraft.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AutoDig$")]
                public ChatBots.AutoDig.Configs AutoDig
                {
                    get { return ChatBots.AutoDig.Config; }
                    set { ChatBots.AutoDig.Config = value; ChatBots.AutoDig.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AutoDrop$")]
                public ChatBots.AutoDrop.Configs AutoDrop
                {
                    get { return ChatBots.AutoDrop.Config; }
                    set { ChatBots.AutoDrop.Config = value; ChatBots.AutoDrop.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AutoEat$")]
                public ChatBots.AutoEat.Configs AutoEat
                {
                    get { return ChatBots.AutoEat.Config; }
                    set { ChatBots.AutoEat.Config = value; ChatBots.AutoEat.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AutoFishing$")]
                public ChatBots.AutoFishing.Configs AutoFishing
                {
                    get { return ChatBots.AutoFishing.Config; }
                    set { ChatBots.AutoFishing.Config = value; ChatBots.AutoFishing.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AutoRelog$")]
                public ChatBots.AutoRelog.Configs AutoRelog
                {
                    get { return ChatBots.AutoRelog.Config; }
                    set { ChatBots.AutoRelog.Config = value; ChatBots.AutoRelog.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.AutoRespond$")]
                public ChatBots.AutoRespond.Configs AutoRespond
                {
                    get { return ChatBots.AutoRespond.Config; }
                    set { ChatBots.AutoRespond.Config = value; ChatBots.AutoRespond.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.ChatLog$")]
                public ChatBots.ChatLog.Configs ChatLog
                {
                    get { return ChatBots.ChatLog.Config; }
                    set { ChatBots.ChatLog.Config = value; ChatBots.ChatLog.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.FollowPlayer$")]
                public ChatBots.FollowPlayer.Configs FollowPlayer
                {
                    get { return ChatBots.FollowPlayer.Config; }
                    set { ChatBots.FollowPlayer.Config = value; ChatBots.FollowPlayer.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.HangmanGame$")]
                public ChatBots.HangmanGame.Configs HangmanGame
                {
                    get { return ChatBots.HangmanGame.Config; }
                    set { ChatBots.HangmanGame.Config = value; ChatBots.HangmanGame.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.Mailer$")]
                public ChatBots.Mailer.Configs Mailer
                {
                    get { return ChatBots.Mailer.Config; }
                    set { ChatBots.Mailer.Config = value; ChatBots.Mailer.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.Map$")]
                public ChatBots.Map.Configs Map
                {
                    get { return ChatBots.Map.Config; }
                    set { ChatBots.Map.Config = value; ChatBots.Map.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.PlayerListLogger$")]
                public ChatBots.PlayerListLogger.Configs PlayerListLogger
                {
                    get { return ChatBots.PlayerListLogger.Config; }
                    set { ChatBots.PlayerListLogger.Config = value; ChatBots.PlayerListLogger.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.RemoteControl$")]
                public ChatBots.RemoteControl.Configs RemoteControl
                {
                    get { return ChatBots.RemoteControl.Config; }
                    set { ChatBots.RemoteControl.Config = value; ChatBots.RemoteControl.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.ReplayCapture$")]
                public ChatBots.ReplayCapture.Configs ReplayCapture
                {
                    get { return ChatBots.ReplayCapture.Config; }
                    set { ChatBots.ReplayCapture.Config = value; ChatBots.ReplayCapture.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$config.ChatBot.ScriptScheduler$")]
                public ChatBots.ScriptScheduler.Configs ScriptScheduler
                {
                    get { return ChatBots.ScriptScheduler.Config; }
                    set { ChatBots.ScriptScheduler.Config = value; ChatBots.ScriptScheduler.Config.OnSettingUpdate(); }
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static string ToLowerIfNeed(string str)
        {
            const string lookupStringL = "---------------------------------!-#$%&-()*+,-./0123456789:;<=>?@abcdefghijklmnopqrstuvwxyz[-]^_`abcdefghijklmnopqrstuvwxyz{|}~-";

            bool needLower = false;
            foreach (Char c in str)
            {
                if (Char.IsUpper(c))
                {
                    needLower = true;
                    break;
                }
            }

            if (needLower)
            {
                StringBuilder sb = new(str);
                for (int i = 0; i < str.Length; ++i)
                    if (char.IsUpper(sb[i]))
                        sb[i] = lookupStringL[sb[i]];
                return sb.ToString();
            }
            else
            {
                return str;
            }
        }

        public static bool CheckUpdate(string? current, string? latest)
        {
            if (current == null || latest == null)
                return false;
            Regex reg = new(@"\w+\sbuild\s(\d+),\sbuilt\son\s(\d{4})[-\/\.\s]?(\d{2})[-\/\.\s]?(\d{2}).*");
            Regex reg2 = new(@"\w+\sbuild\s(\d+),\sbuilt\son\s\w+\s(\d{2})[-\/\.\s]?(\d{2})[-\/\.\s]?(\d{4}).*");

            DateTime? curTime = null, latestTime = null;

            Match curMatch = reg.Match(current);
            if (curMatch.Success && curMatch.Groups.Count == 5)
            {
                try { curTime = new(int.Parse(curMatch.Groups[2].Value), int.Parse(curMatch.Groups[3].Value), int.Parse(curMatch.Groups[4].Value)); }
                catch { curTime = null; }
            }
            if (curTime == null)
            {
                curMatch = reg2.Match(current);
                try { curTime = new(int.Parse(curMatch.Groups[4].Value), int.Parse(curMatch.Groups[3].Value), int.Parse(curMatch.Groups[2].Value)); }
                catch { curTime = null; }
            }
            if (curTime == null)
                return false;

            Match latestMatch = reg.Match(latest);
            if (latestMatch.Success && latestMatch.Groups.Count == 5)
            {
                try { latestTime = new(int.Parse(latestMatch.Groups[2].Value), int.Parse(latestMatch.Groups[3].Value), int.Parse(latestMatch.Groups[4].Value)); }
                catch { latestTime = null; }
            }
            if (latestTime == null)
            {
                latestMatch = reg2.Match(latest);
                try { latestTime = new(int.Parse(latestMatch.Groups[4].Value), int.Parse(latestMatch.Groups[3].Value), int.Parse(latestMatch.Groups[2].Value)); }
                catch { latestTime = null; }
            }
            if (latestTime == null)
                return false;

            int curBuildId, latestBuildId;
            try
            {
                curBuildId = int.Parse(curMatch.Groups[1].Value);
                latestBuildId = int.Parse(latestMatch.Groups[1].Value);
            }
            catch { return false; }

            if (latestTime > curTime)
                return true;
            else if (latestTime >= curTime && latestBuildId > curBuildId)
                return true;
            else
                return false;
        }

        public static int DoubleToTick(double time)
        {
            time = Math.Min(int.MaxValue / 10, time);
            return (int)Math.Round(time * 10);
        }
    }

    public static class InternalCmdCharTypeExtensions
    {
        public static char ToChar(this InternalCmdCharType type)
        {
            return type switch
            {
                InternalCmdCharType.none => ' ',
                InternalCmdCharType.slash => '/',
                InternalCmdCharType.backslash => '\\',
                _ => '/',
            };
        }

        public static string ToLogString(this InternalCmdCharType type)
        {
            return type switch
            {
                InternalCmdCharType.none => string.Empty,
                InternalCmdCharType.slash => @"/",
                InternalCmdCharType.backslash => @"\",
                _ => @"/",
            };
        }
    }
    
    public static class BrandInfoTypeExtensions
    {
        public static string? ToBrandString(this BrandInfoType info)
        {
            return info switch
            {
                BrandInfoType.mcc => "Minecraft-Console-Client/" + Program.Version,
                BrandInfoType.vanilla => "vanilla",
                BrandInfoType.empty => null,
                _ => null,
            };
        }
    }

    public static class ExceptionExtensions
    {
        public static string GetFullMessage(this Exception ex)
        {
            return ex.InnerException == null
                 ? ex.Message
                 : ex.Message + "\n --> " + ex.InnerException.GetFullMessage();
        }
    }
}
