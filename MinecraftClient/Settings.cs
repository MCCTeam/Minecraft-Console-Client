﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MinecraftClient.Protocol;
using MinecraftClient.Proxy;
using Tomlet;
using Tomlet.Attributes;
using Tomlet.Models;
using static MinecraftClient.Settings.AppVarConfigHelper;
using static MinecraftClient.Settings.ChatBotConfigHealper;
using static MinecraftClient.Settings.ChatFormatConfigHelper;
using static MinecraftClient.Settings.ConsoleConfigHealper;
using static MinecraftClient.Settings.HeadCommentHealper;
using static MinecraftClient.Settings.LoggingConfigHealper;
using static MinecraftClient.Settings.MainConfigHealper;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.AdvancedConfig;
using static MinecraftClient.Settings.MCSettingsConfigHealper;
using static MinecraftClient.Settings.SignatureConfigHelper;

namespace MinecraftClient
{
    public static class Settings
    {
        private const int CommentsAlignPosition = 45;
        private readonly static Regex CommentRegex = new(@"^(.*)\s?#\s\$([\w\.]+)\$\s*$$", RegexOptions.Compiled);

        // Other Settings
        public const string TranslationsFile_Version = "1.19.3";
        public const string TranslationsFile_Website_Index = "https://piston-meta.mojang.com/v1/packages/c492375ded5da34b646b8c5c0842a0028bc69cec/2.json";
        public const string TranslationsFile_Website_Download = "https://resources.download.minecraft.net";

        public const string TranslationProjectUrl = "https://crwd.in/minecraft-console-client";

        public static GlobalConfig Config = new();

        public static class InternalConfig
        {
            public static string ServerIP = String.Empty;

            public static ushort ServerPort = 25565;

            public static AccountInfoConfig Account = new();

            public static string Username = string.Empty;

            public static string MinecraftVersion = string.Empty;

            public static bool InteractiveMode = true;

            public static bool GravityEnabled = true;

            public static bool KeepAccountSettings = false;

            public static bool KeepServerSettings = false;
        }

        public class GlobalConfig
        {
            [TomlPrecedingComment("$Head$")]
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

            [TomlPrecedingComment("$Signature$")]
            public SignatureConfig Signature
            {
                get { return SignatureConfigHelper.Config; }
                set { SignatureConfigHelper.Config = value; SignatureConfigHelper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$Logging$")]
            public LoggingConfig Logging
            {
                get { return LoggingConfigHealper.Config; }
                set { LoggingConfigHealper.Config = value; LoggingConfigHealper.Config.OnSettingUpdate(); }
            }

            public ConsoleConfig Console
            {
                get { return ConsoleConfigHealper.Config; }
                set { ConsoleConfigHealper.Config = value; ConsoleConfigHealper.Config.OnSettingUpdate(); }
            }

            public AppVarConfig AppVar
            {
                get { return AppVarConfigHelper.Config; }
                set { AppVarConfigHelper.Config = value; AppVarConfigHelper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$Proxy$")]
            public ProxyHandler.Configs Proxy
            {
                get { return ProxyHandler.Config; }
                set { ProxyHandler.Config = value; ProxyHandler.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$MCSettings$")]
            public MCSettingsConfig MCSettings
            {
                get { return MCSettingsConfigHealper.Config; }
                set { MCSettingsConfigHealper.Config = value; MCSettingsConfigHealper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$ChatFormat$")]
            public ChatFormatConfig ChatFormat
            {
                get { return ChatFormatConfigHelper.Config; }
                set { ChatFormatConfigHelper.Config = value; ChatFormatConfigHelper.Config.OnSettingUpdate(); }
            }

            [TomlPrecedingComment("$ChatBot$")]
            public ChatBotConfig ChatBot
            {
                get { return ChatBotConfigHealper.Config; }
                set { ChatBotConfigHealper.Config = value; }
            }

        }

        public static Tuple<bool, bool> LoadFromFile(string filepath, bool keepAccountAndServerSettings = false)
        {
            bool keepAccountSettings = InternalConfig.KeepAccountSettings;
            bool keepServerSettings = InternalConfig.KeepServerSettings;
            if (keepAccountAndServerSettings)
                InternalConfig.KeepAccountSettings = InternalConfig.KeepServerSettings = true;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            TomlDocument document;
            try
            {
                document = TomlParser.ParseFile(filepath);
                Thread.CurrentThread.CurrentCulture = Program.ActualCulture;

                Config = TomletMain.To<GlobalConfig>(document);
            }
            catch (Exception ex)
            {
                Thread.CurrentThread.CurrentCulture = Program.ActualCulture;
                try
                {
                    // The old configuration file has been backed up as A.
                    string configString = File.ReadAllText(filepath);
                    if (configString.Contains("Some settings missing here after an upgrade?"))
                    {
                        string newFilePath = Path.ChangeExtension(filepath, ".old.ini");
                        File.Copy(filepath, newFilePath, true);
                        ConsoleIO.WriteLineFormatted("§c" + Translations.mcc_use_new_config);
                        ConsoleIO.WriteLineFormatted("§c" + string.Format(Translations.mcc_backup_old_config, newFilePath));
                        return new(false, true);
                    }
                }
                catch { }
                ConsoleIO.WriteLineFormatted("§c" + Translations.config_load_fail);
                ConsoleIO.WriteLine(ex.GetFullMessage());
                return new(false, false);
            }
            finally
            {
                if (!keepAccountSettings)
                    InternalConfig.KeepAccountSettings = false;
                if (!keepServerSettings)
                    InternalConfig.KeepServerSettings = false;
            }
            return new(true, false);
        }

        public static void WriteToFile(string filepath, bool backupOldFile)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string tomlString = TomletMain.TomlStringFrom(Config);
            Thread.CurrentThread.CurrentCulture = Program.ActualCulture;

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
                    string? comment_trans = ConfigComments.ResourceManager.GetString(comment);
                    if (string.IsNullOrEmpty(comment_trans))
                        newConfig.Append("# ").AppendLine(comment.ReplaceLineEndings());
                    else
                        newConfig.Append("# ").AppendLine(comment_trans.Replace("\n", "\n# ").ReplaceLineEndings());
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
                        ConsoleIO.WriteLineFormatted("§c" + string.Format(Translations.config_backup_fail, backupFilePath));
                        ConsoleIO.WriteLine(ex.Message);
                    }
                }

                if (backupSuccessed)
                {
                    try { File.WriteAllBytes(filepath, newConfigByte); }
                    catch (Exception ex)
                    {
                        ConsoleIO.WriteLineFormatted("§c" + string.Format(Translations.config_write_fail, filepath));
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
            bool skipPassword = false;

            foreach (string argument in args)
            {
                if (argument.StartsWith("--"))
                {
                    //Load settings as --setting=value and --section.setting=value
                    if (!argument.Contains('='))
                        throw new ArgumentException(string.Format(Translations.error_setting_argument_syntax, argument));
                    throw new NotImplementedException();
                }
                else if (argument.StartsWith("-") && argument.Length > 1)
                {
                    //Keep single dash arguments as unsupported for now (future use)
                    throw new ArgumentException(string.Format(Translations.error_setting_argument_syntax, argument));
                }
                else
                {
                    switch (positionalIndex)
                    {
                        case 0:
                            if (Config.Main.Advanced.AccountList.TryGetValue(argument, out AccountInfoConfig accountInfo))
                            {
                                InternalConfig.Account = accountInfo;
                                skipPassword = true;
                            }
                            else
                            {
                                InternalConfig.Account.Login = argument;
                            }
                            InternalConfig.KeepAccountSettings = true;
                            break;
                        case 1:
                            if (!skipPassword)
                                InternalConfig.Account.Password = argument;
                            break;
                        case 2:
                            Config.Main.SetServerIP(new MainConfig.ServerInfoConfig(argument), true);
                            InternalConfig.KeepServerSettings = true;
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

                [TomlPrecedingComment("$Main.Advanced$")]
                public AdvancedConfig Advanced = new();


                [NonSerialized]
                public static readonly string[] AvailableLang =
                {
                    "af_za", "ar_sa", "ast_es", "az_az", "ba_ru", "bar", "be_by", "bg_bg",
                    "br_fr", "brb", "bs_ba", "ca_es", "cs_cz", "cy_gb", "da_dk", "de_at",
                    "de_ch", "de_de", "el_gr", "en_au", "en_ca", "en_gb", "en_nz", "en_pt",
                    "en_ud", "en_us", "enp", "enws", "eo_uy", "es_ar", "es_cl", "es_ec",
                    "es_es", "es_mx", "es_uy", "es_ve", "esan", "et_ee", "eu_es", "fa_ir",
                    "fi_fi", "fil_ph", "fo_fo", "fr_ca", "fr_fr", "fra_de", "fur_it", "fy_nl",
                    "ga_ie", "gd_gb", "gl_es", "haw_us", "he_il", "hi_in", "hr_hr", "hu_hu",
                    "hy_am", "id_id", "ig_ng", "io_en", "is_is", "isv", "it_it", "ja_jp",
                    "jbo_en", "ka_ge", "kk_kz", "kn_in", "ko_kr", "ksh", "kw_gb", "la_la",
                    "lb_lu", "li_li", "lmo", "lol_us", "lt_lt", "lv_lv", "lzh", "mk_mk",
                    "mn_mn", "ms_my", "mt_mt", "nah", "nds_de", "nl_be", "nl_nl", "nn_no",
                    "no_no", "oc_fr", "ovd", "pl_pl", "pt_br", "pt_pt", "qya_aa", "ro_ro",
                    "rpr", "ru_ru", "ry_ua", "se_no", "sk_sk", "sl_si", "so_so", "sq_al",
                    "sr_sp", "sv_se", "sxu", "szl", "ta_in", "th_th", "tl_ph", "tlh_aa",
                    "tok", "tr_tr", "tt_ru", "uk_ua", "val_es", "vec_it", "vi_vn", "yi_de",
                    "yo_ng", "zh_cn", "zh_hk", "zh_tw", "zlm_arab"
                };

                /// <summary>
                /// Load server information in ServerIP and ServerPort variables from a "serverip:port" couple or server alias
                /// </summary>
                /// <returns>True if the server IP was valid and loaded, false otherwise</returns>
                public bool SetServerIP(ServerInfoConfig serverInfo, bool checkAlias)
                {
                    string[] sip = serverInfo.Host.Split(new[] { ":", "：" }, StringSplitOptions.None);
                    string host = ToLowerIfNeed(sip[0]);
                    ushort port = 25565;

                    if (serverInfo.Port.HasValue)
                    {
                        port = serverInfo.Port.Value;
                    }
                    else if (sip.Length > 1)
                    {
                        try { port = Convert.ToUInt16(sip[1]); }
                        catch (FormatException) { return false; }
                    }

                    if (host == "localhost" || host.Contains('.'))
                    {
                        //Server IP (IP or domain names contains at least a dot)
                        if (sip.Length == 1 && !serverInfo.Port.HasValue && host.Contains('.') && host.Any(c => char.IsLetter(c)) &&
                            Settings.Config.Main.Advanced.ResolveSrvRecords != ResolveSrvRecordType.no)
                            //Domain name without port may need Minecraft SRV Record lookup
                            ProtocolHandler.MinecraftServiceLookup(ref host, ref port);
                        InternalConfig.ServerIP = host;
                        InternalConfig.ServerPort = port;
                        return true;
                    }
                    else if (checkAlias && Advanced.ServerList.TryGetValue(sip[0], out ServerInfoConfig serverStr2))
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
                    if (!InternalConfig.KeepAccountSettings)
                    {
                        if (Advanced.AccountList.TryGetValue(General.Account.Login, out AccountInfoConfig account))
                            InternalConfig.Account = account;
                        else
                            InternalConfig.Account = General.Account;
                    }

                    General.Server.Host ??= string.Empty;

                    if (Advanced.MessageCooldown < 0)
                        Advanced.MessageCooldown = 0;

                    if (Advanced.TcpTimeout < 1)
                        Advanced.TcpTimeout = 1;

                    if (Advanced.MovementSpeed < 1)
                        Advanced.MovementSpeed = 1;

                    if (!Advanced.LoadMccTranslation)
                    {
                        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
                        CultureInfo.DefaultThreadCurrentCulture = culture;
                        CultureInfo.DefaultThreadCurrentUICulture = culture;
                        Program.ActualCulture = culture;
                        Thread.CurrentThread.CurrentCulture = culture;
                        Thread.CurrentThread.CurrentUICulture = culture;
                    }

                    Advanced.Language = Regex.Replace(Advanced.Language, @"[^-^_^\w^*\d]", string.Empty).Replace('-', '_');
                    Advanced.Language = ToLowerIfNeed(Advanced.Language);
                    if (!AvailableLang.Contains(Advanced.Language))
                    {
                        Advanced.Language = GetDefaultGameLanguage();
                        ConsoleIO.WriteLogLine("[Settings] " + Translations.config_Main_Advanced_language_invaild);
                    }

                    if (!InternalConfig.KeepServerSettings)
                    {
                        if (!string.IsNullOrWhiteSpace(General.Server.Host))
                        {
                            string[] sip = General.Server.Host.Split(new[] { ":", "：" }, StringSplitOptions.None);
                            General.Server.Host = sip[0];
                            InternalConfig.ServerIP = General.Server.Host;

                            if (sip.Length > 1)
                            {
                                try { General.Server.Port = Convert.ToUInt16(sip[1]); }
                                catch (FormatException) { }
                            }
                        }

                        if (General.Server.Port.HasValue)
                            InternalConfig.ServerPort = General.Server.Port.Value;
                        else
                            SetServerIP(General.Server, true);
                    }

                    for (int i = 0; i < Advanced.BotOwners.Count; ++i)
                        Advanced.BotOwners[i] = ToLowerIfNeed(Advanced.BotOwners[i]);

                    if (Advanced.MinTerminalWidth < 1)
                        Advanced.MinTerminalWidth = 1;
                    if (Advanced.MinTerminalHeight < 1)
                        Advanced.MinTerminalHeight = 1;

                    if (Advanced.TemporaryFixBadpacket && !Advanced.TerrainAndMovements)
                    {
                        Advanced.TerrainAndMovements = true;
                        ConsoleIO.WriteLineFormatted("§c[Settings]You need to enable TerrainAndMovements before enabling TemporaryFixBadpacket.");
                    }
                }

                [TomlDoNotInlineObject]
                public class GeneralConfig
                {
                    [TomlInlineComment("$Main.General.account$")]
                    public AccountInfoConfig Account = new(string.Empty, string.Empty);

                    [TomlInlineComment("$Main.General.login$")]
                    public ServerInfoConfig Server = new(string.Empty);

                    [TomlInlineComment("$Main.General.server_info$")]
                    public LoginType AccountType = LoginType.microsoft;

                    [TomlInlineComment("$Main.General.method$")]
                    public LoginMethod Method = LoginMethod.mcc;
                    [TomlInlineComment("$Main.General.AuthlibServer$")]
                    public AuthlibServer AuthServer = new(string.Empty);
                  

                    public enum LoginType { mojang, microsoft,yggdrasil };

                    public enum LoginMethod { mcc, browser };
                }

                [TomlDoNotInlineObject]
                public class AdvancedConfig
                {
                    [TomlInlineComment("$Main.Advanced.language$")]
                    public string Language = "en_us";

                    [TomlInlineComment("$Main.Advanced.LoadMccTrans$")]
                    public bool LoadMccTranslation = true;

                    // [TomlInlineComment("$Main.Advanced.console_title$")]
                    public string ConsoleTitle = "%username%@%serverip% - Minecraft Console Client";

                    [TomlInlineComment("$Main.Advanced.internal_cmd_char$")]
                    public InternalCmdCharType InternalCmdChar = InternalCmdCharType.slash;

                    [TomlInlineComment("$Main.Advanced.message_cooldown$")]
                    public double MessageCooldown = 1.0;

                    [TomlInlineComment("$Main.Advanced.bot_owners$")]
                    public List<string> BotOwners = new() { "Player1", "Player2" };

                    [TomlInlineComment("$Main.Advanced.mc_version$")]
                    public string MinecraftVersion = "auto";

                    [TomlInlineComment("$Main.Advanced.mc_forge$")]
                    public ForgeConfigType EnableForge = ForgeConfigType.no;

                    [TomlInlineComment("$Main.Advanced.brand_info$")]
                    public BrandInfoType BrandInfo = BrandInfoType.mcc;

                    [TomlInlineComment("$Main.Advanced.chatbot_log_file$")]
                    public string ChatbotLogFile = "";

                    [TomlInlineComment("$Main.Advanced.private_msgs_cmd_name$")]
                    public string PrivateMsgsCmdName = "tell";

                    [TomlInlineComment("$Main.Advanced.show_system_messages$")]
                    public bool ShowSystemMessages = true;

                    [TomlInlineComment("$Main.Advanced.show_xpbar_messages$")]
                    public bool ShowXPBarMessages = true;

                    [TomlInlineComment("$Main.Advanced.show_chat_links$")]
                    public bool ShowChatLinks = true;

                    [TomlInlineComment("$Main.Advanced.show_inventory_layout$")]
                    public bool ShowInventoryLayout = true;

                    [TomlInlineComment("$Main.Advanced.terrain_and_movements$")]
                    public bool TerrainAndMovements = false;

                    [TomlInlineComment("$Main.Advanced.move_head_while_walking$")]
                    public bool MoveHeadWhileWalking = true;

                    [TomlInlineComment("$Main.Advanced.movement_speed$")]
                    public int MovementSpeed = 2;

                    [TomlInlineComment("$Main.Advanced.temporary_fix_badpacket$")]
                    public bool TemporaryFixBadpacket = false;

                    [TomlInlineComment("$Main.Advanced.inventory_handling$")]
                    public bool InventoryHandling = false;

                    [TomlInlineComment("$Main.Advanced.entity_handling$")]
                    public bool EntityHandling = false;

                    [TomlInlineComment("$Main.Advanced.session_cache$")]
                    public CacheType SessionCache = CacheType.disk;

                    [TomlInlineComment("$Main.Advanced.profilekey_cache$")]
                    public CacheType ProfileKeyCache = CacheType.disk;

                    [TomlInlineComment("$Main.Advanced.resolve_srv_records$")]
                    public ResolveSrvRecordType ResolveSrvRecords = ResolveSrvRecordType.fast;

                    [TomlPrecedingComment("$Main.Advanced.account_list$")]
                    public Dictionary<string, AccountInfoConfig> AccountList = new() {
                        { "AccountNikename1", new AccountInfoConfig("playerone@email.com", "thepassword") },
                        { "AccountNikename2", new AccountInfoConfig("TestBot", "-") },
                    };

                    [TomlPrecedingComment("$Main.Advanced.server_list$")]
                    public Dictionary<string, ServerInfoConfig> ServerList = new() {
                        { "ServerAlias1", new ServerInfoConfig("mc.awesomeserver.com") },
                        { "ServerAlias2", new ServerInfoConfig("192.168.1.27", 12345) },
                    };

                    [TomlInlineComment("$Main.Advanced.player_head_icon$")]
                    public bool PlayerHeadAsIcon = true;

                    [TomlInlineComment("$Main.Advanced.exit_on_failure$")]
                    public bool ExitOnFailure = false;

                    [TomlInlineComment("$Main.Advanced.script_cache$")]
                    public bool CacheScript = true;

                    [TomlInlineComment("$Main.Advanced.timestamps$")]
                    public bool Timestamps = false;

                    [TomlInlineComment("$Main.Advanced.auto_respawn$")]
                    public bool AutoRespawn = false;

                    [TomlInlineComment("$Main.Advanced.minecraft_realms$")]
                    public bool MinecraftRealms = false;

                    [TomlInlineComment("$Main.Advanced.timeout$")]
                    public int TcpTimeout = 30;

                    [TomlInlineComment("$Main.Advanced.enable_emoji$")]
                    public bool EnableEmoji = true;

                    [TomlInlineComment("$Main.Advanced.MinTerminalWidth$")]
                    public int MinTerminalWidth = 16;

                    [TomlInlineComment("$Main.Advanced.MinTerminalHeight$")]
                    public int MinTerminalHeight = 10;

                    [TomlInlineComment("$Main.Advanced.ignore_invalid_playername$")]
                    public bool IgnoreInvalidPlayerName = true;

                    /// <summary>
                    /// Load login/password using an account alias
                    /// </summary>
                    /// <returns>True if the account was found and loaded</returns>
                    public bool SetAccount(string accountAlias)
                    {
                        if (AccountList.TryGetValue(accountAlias, out AccountInfoConfig accountInfo))
                        {
                            InternalConfig.Account = accountInfo;
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
                public struct AuthlibServer
                {
                    public string Host = string.Empty;
                    public int Port = 443;

                    public AuthlibServer(string Host)
                    {
                        string[] sip = Host.Split(new[] { ":", "：" }, StringSplitOptions.None);
                        this.Host = sip[0];

                        if (sip.Length > 1)
                        {
                            try { this.Port = Convert.ToUInt16(sip[1]); }
                            catch (FormatException) { }
                        }
                    }

                    public AuthlibServer(string Host, ushort Port)
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
                [TomlInlineComment("$Signature.LoginWithSecureProfile$")]
                public bool LoginWithSecureProfile = true;

                [TomlInlineComment("$Signature.SignChat$")]
                public bool SignChat = true;

                [TomlInlineComment("$Signature.SignMessageInCommand$")]
                public bool SignMessageInCommand = true;

                [TomlInlineComment("$Signature.MarkLegallySignedMsg$")]
                public bool MarkLegallySignedMsg = true;

                [TomlInlineComment("$Signature.MarkModifiedMsg$")]
                public bool MarkModifiedMsg = true;

                [TomlInlineComment("$Signature.MarkIllegallySignedMsg$")]
                public bool MarkIllegallySignedMsg = true;

                [TomlInlineComment("$Signature.MarkSystemMessage$")]
                public bool MarkSystemMessage = true;

                [TomlInlineComment("$Signature.ShowModifiedChat$")]
                public bool ShowModifiedChat = true;

                [TomlInlineComment("$Signature.ShowIllegalSignedChat$")]
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
                [TomlInlineComment("$Logging.DebugMessages$")]
                public bool DebugMessages = false;

                [TomlInlineComment("$Logging.ChatMessages$")]
                public bool ChatMessages = true;

                [TomlInlineComment("$Logging.InfoMessages$")]
                public bool InfoMessages = true;

                [TomlInlineComment("$Logging.WarningMessages$")]
                public bool WarningMessages = true;

                [TomlInlineComment("$Logging.ErrorMessages$")]
                public bool ErrorMessages = true;

                [TomlInlineComment("$Logging.ChatFilter$")]
                public string ChatFilterRegex = @".*";

                [TomlInlineComment("$Logging.DebugFilter$")]
                public string DebugFilterRegex = @".*";

                [TomlInlineComment("$Logging.FilterMode$")]
                public FilterModeEnum FilterMode = FilterModeEnum.disable;

                [TomlInlineComment("$Logging.LogToFile$")]
                public bool LogToFile = false;

                [TomlInlineComment("$Logging.LogFile$")]
                public string LogFile = @"console-log.txt";

                [TomlInlineComment("$Logging.PrependTimestamp$")]
                public bool PrependTimestamp = false;

                [TomlInlineComment("$Logging.SaveColorCodes$")]
                public bool SaveColorCodes = false;

                public void OnSettingUpdate() { }

                public enum FilterModeEnum { disable, blacklist, whitelist }
            }
        }

        public static class ConsoleConfigHealper
        {
            public static ConsoleConfig Config = new();

            [TomlDoNotInlineObject]
            public class ConsoleConfig
            {
                public MainConfig General = new();

                [TomlPrecedingComment("$Console.CommandSuggestion$")]
                public CommandSuggestionConfig CommandSuggestion = new();

                public void OnSettingUpdate()
                {
                    // Reader
                    ConsoleInteractive.ConsoleReader.DisplayUesrInput = General.Display_Input;

                    // Writer
                    ConsoleInteractive.ConsoleWriter.EnableColor = General.ConsoleColorMode != ConsoleColorModeType.disable;

                    ConsoleInteractive.ConsoleWriter.UseVT100ColorCode = General.ConsoleColorMode != ConsoleColorModeType.legacy_4bit;

                    // Buffer
                    General.History_Input_Records =
                        ConsoleInteractive.ConsoleBuffer.SetBackreadBufferLimit(General.History_Input_Records);

                    // Suggestion
                    if (General.ConsoleColorMode == ConsoleColorModeType.disable)
                        CommandSuggestion.Enable_Color = false;

                    ConsoleInteractive.ConsoleSuggestion.EnableColor = CommandSuggestion.Enable_Color;

                    ConsoleInteractive.ConsoleSuggestion.Enable24bitColor = General.ConsoleColorMode == ConsoleColorModeType.vt100_24bit;

                    ConsoleInteractive.ConsoleSuggestion.UseBasicArrow = CommandSuggestion.Use_Basic_Arrow;

                    CommandSuggestion.Max_Suggestion_Width =
                        ConsoleInteractive.ConsoleSuggestion.SetMaxSuggestionLength(CommandSuggestion.Max_Suggestion_Width);

                    CommandSuggestion.Max_Displayed_Suggestions =
                        ConsoleInteractive.ConsoleSuggestion.SetMaxSuggestionCount(CommandSuggestion.Max_Displayed_Suggestions);

                    // Suggestion color settings
                    {
                        if (!CheckColorCode(CommandSuggestion.Text_Color))
                        {
                            ConsoleIO.WriteLine(string.Format(Translations.config_commandsuggestion_illegal_color, "CommandSuggestion.TextColor", CommandSuggestion.Text_Color));
                            CommandSuggestion.Text_Color = "#f8fafc";
                        }
                        if (!CheckColorCode(CommandSuggestion.Text_Background_Color))
                        {
                            ConsoleIO.WriteLine(string.Format(Translations.config_commandsuggestion_illegal_color, "CommandSuggestion.TextBackgroundColor", CommandSuggestion.Text_Background_Color));
                            CommandSuggestion.Text_Background_Color = "#64748b";
                        }
                        if (!CheckColorCode(CommandSuggestion.Highlight_Text_Color))
                        {
                            ConsoleIO.WriteLine(string.Format(Translations.config_commandsuggestion_illegal_color, "CommandSuggestion.HighlightTextColor", CommandSuggestion.Highlight_Text_Color));
                            CommandSuggestion.Highlight_Text_Color = "#334155";
                        }
                        if (!CheckColorCode(CommandSuggestion.Highlight_Text_Background_Color))
                        {
                            ConsoleIO.WriteLine(string.Format(Translations.config_commandsuggestion_illegal_color, "CommandSuggestion.HighlightTextBackgroundColor", CommandSuggestion.Highlight_Text_Background_Color));
                            CommandSuggestion.Highlight_Text_Background_Color = "#fde047";
                        }
                        if (!CheckColorCode(CommandSuggestion.Tooltip_Color))
                        {
                            ConsoleIO.WriteLine(string.Format(Translations.config_commandsuggestion_illegal_color, "CommandSuggestion.TooltipColor", CommandSuggestion.Tooltip_Color));
                            CommandSuggestion.Tooltip_Color = "#7dd3fc";
                        }
                        if (!CheckColorCode(CommandSuggestion.Highlight_Tooltip_Color))
                        {
                            ConsoleIO.WriteLine(string.Format(Translations.config_commandsuggestion_illegal_color, "CommandSuggestion.HighlightTooltipColor", CommandSuggestion.Highlight_Tooltip_Color));
                            CommandSuggestion.Highlight_Tooltip_Color = "#3b82f6";
                        }
                        if (!CheckColorCode(CommandSuggestion.Arrow_Symbol_Color))
                        {
                            ConsoleIO.WriteLine(string.Format(Translations.config_commandsuggestion_illegal_color, "CommandSuggestion.ArrowSymbolColor", CommandSuggestion.Arrow_Symbol_Color));
                            CommandSuggestion.Arrow_Symbol_Color = "#d1d5db";
                        }

                        ConsoleInteractive.ConsoleSuggestion.SetColors(
                            CommandSuggestion.Text_Color, CommandSuggestion.Text_Background_Color,
                            CommandSuggestion.Highlight_Text_Color, CommandSuggestion.Highlight_Text_Background_Color,
                            CommandSuggestion.Tooltip_Color, CommandSuggestion.Highlight_Tooltip_Color,
                            CommandSuggestion.Arrow_Symbol_Color);
                    }
                }

                private static bool CheckColorCode(string? input)
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return false;
                    if (input.Length < 6 || input.Length > 7)
                        return false;
                    if (input.Length == 6 && input[0] == '#')
                        return false;
                    if (input.Length == 7 && input[0] != '#')
                        return false;
                    try
                    {
                        Convert.ToInt32(input.Length == 7 ? input[1..] : input, 16);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                [TomlDoNotInlineObject]
                public class MainConfig
                {
                    [TomlInlineComment("$Console.General.ConsoleColorMode$")]
                    public ConsoleColorModeType ConsoleColorMode = ConsoleColorModeType.vt100_24bit;

                    [TomlInlineComment("$Console.General.Display_Input$")]
                    public bool Display_Input = true;

                    [TomlInlineComment("$Console.General.History_Input_Records$")]
                    public int History_Input_Records = 32;
                }

                [TomlDoNotInlineObject]
                public class CommandSuggestionConfig
                {
                    [TomlInlineComment("$Console.CommandSuggestion.Enable$")]
                    public bool Enable = true;

                    public bool Enable_Color = true;

                    [TomlInlineComment("$Console.CommandSuggestion.Use_Basic_Arrow$")]
                    public bool Use_Basic_Arrow = false;

                    public int Max_Suggestion_Width = 30;

                    public int Max_Displayed_Suggestions = 6;

                    public string Text_Color = "#f8fafc";

                    public string Text_Background_Color = "#64748b";

                    public string Highlight_Text_Color = "#334155";

                    public string Highlight_Text_Background_Color = "#fde047";

                    public string Tooltip_Color = "#7dd3fc";

                    public string Highlight_Tooltip_Color = "#3b82f6";

                    public string Arrow_Symbol_Color = "#d1d5db";
                }

                public enum ConsoleColorModeType { disable, legacy_4bit, vt100_4bit, vt100_8bit, vt100_24bit };
            }
        }

        public static class AppVarConfigHelper
        {
            public static AppVarConfig Config = new();

            [TomlDoNotInlineObject]
            public class AppVarConfig
            {
                [TomlPrecedingComment("$AppVars.Variables$")]
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
                    Dictionary<string, object> res = new(VarObject);
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
                                    case "login": result.Append(InternalConfig.Account.Login); break;
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
                [TomlInlineComment("$MCSettings.Enabled$")]
                public bool Enabled = true;

                [TomlInlineComment("$MCSettings.Locale$")]
                public string Locale = "en_US";

                [TomlInlineComment("$MCSettings.RenderDistance$")]
                public byte RenderDistance = 8;

                [TomlInlineComment("$MCSettings.Difficulty$")]
                public DifficultyType Difficulty = DifficultyType.peaceful;

                [TomlInlineComment("$MCSettings.ChatMode$")]
                public ChatModeType ChatMode = ChatModeType.enabled;

                [TomlInlineComment("$MCSettings.ChatColors$")]
                public bool ChatColors = true;

                [TomlInlineComment("$MCSettings.MainHand$")]
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
                [TomlInlineComment("$ChatFormat.Builtins$")]
                public bool Builtins = true;

                [TomlInlineComment("$ChatFormat.UserDefined$")]
                public bool UserDefined = false;

                public string Public = @"^<([a-zA-Z0-9_]+)> (.+)$";

                public string Private = @"^([a-zA-Z0-9_]+) whispers to you: (.+)$";

                public string TeleportRequest = @"^([a-zA-Z0-9_]+) has requested (?:to|that you) teleport to (?:you|them)\.$";

                public void OnSettingUpdate()
                {
                    if (UserDefined)
                    {
                        bool checkResult = true;

                        try { _ = new Regex(Public); }
                        catch (ArgumentException)
                        {
                            checkResult = false;
                            ConsoleIO.WriteLineFormatted("§c[Settings]Illegal regular expression: ChatFormat.Public = " + Public);
                        }

                        try { _ = new Regex(Private); }
                        catch (ArgumentException)
                        {
                            checkResult = false;
                            ConsoleIO.WriteLineFormatted("§c[Settings]Illegal regular expression: ChatFormat.Private = " + Private);
                        }

                        try { _ = new Regex(TeleportRequest); }
                        catch (ArgumentException)
                        {
                            checkResult = false;
                            ConsoleIO.WriteLineFormatted("§c[Settings]Illegal regular expression: ChatFormat.TeleportRequest = " + TeleportRequest);
                        }

                        if (!checkResult)
                        {
                            UserDefined = false;
                            ConsoleIO.WriteLineFormatted("§c[Settings]ChatFormat: User-defined regular expressions are disabled.");
                        }
                    }
                }
            }
        }

        public static class ChatBotConfigHealper
        {
            public static ChatBotConfig Config = new();

            [TomlDoNotInlineObject]
            public class ChatBotConfig
            {
                [TomlPrecedingComment("$ChatBot.Alerts$")]
                public ChatBots.Alerts.Configs Alerts
                {
                    get { return ChatBots.Alerts.Config; }
                    set { ChatBots.Alerts.Config = value; ChatBots.Alerts.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AntiAfk$")]
                public ChatBots.AntiAFK.Configs AntiAFK
                {
                    get { return ChatBots.AntiAFK.Config; }
                    set { ChatBots.AntiAFK.Config = value; ChatBots.AntiAFK.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AutoAttack$")]
                public ChatBots.AutoAttack.Configs AutoAttack
                {
                    get { return ChatBots.AutoAttack.Config; }
                    set { ChatBots.AutoAttack.Config = value; ChatBots.AutoAttack.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AutoCraft$")]
                public ChatBots.AutoCraft.Configs AutoCraft
                {
                    get { return ChatBots.AutoCraft.Config; }
                    set { ChatBots.AutoCraft.Config = value; ChatBots.AutoCraft.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AutoDig$")]
                public ChatBots.AutoDig.Configs AutoDig
                {
                    get { return ChatBots.AutoDig.Config; }
                    set { ChatBots.AutoDig.Config = value; ChatBots.AutoDig.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AutoDrop$")]
                public ChatBots.AutoDrop.Configs AutoDrop
                {
                    get { return ChatBots.AutoDrop.Config; }
                    set { ChatBots.AutoDrop.Config = value; ChatBots.AutoDrop.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AutoEat$")]
                public ChatBots.AutoEat.Configs AutoEat
                {
                    get { return ChatBots.AutoEat.Config; }
                    set { ChatBots.AutoEat.Config = value; ChatBots.AutoEat.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AutoFishing$")]
                public ChatBots.AutoFishing.Configs AutoFishing
                {
                    get { return ChatBots.AutoFishing.Config; }
                    set { ChatBots.AutoFishing.Config = value; ChatBots.AutoFishing.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AutoRelog$")]
                public ChatBots.AutoRelog.Configs AutoRelog
                {
                    get { return ChatBots.AutoRelog.Config; }
                    set { ChatBots.AutoRelog.Config = value; ChatBots.AutoRelog.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.AutoRespond$")]
                public ChatBots.AutoRespond.Configs AutoRespond
                {
                    get { return ChatBots.AutoRespond.Config; }
                    set { ChatBots.AutoRespond.Config = value; ChatBots.AutoRespond.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.ChatLog$")]
                public ChatBots.ChatLog.Configs ChatLog
                {
                    get { return ChatBots.ChatLog.Config; }
                    set { ChatBots.ChatLog.Config = value; ChatBots.ChatLog.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.DiscordBridge$")]
                public ChatBots.DiscordBridge.Configs DiscordBridge
                {
                    get { return ChatBots.DiscordBridge.Config; }
                    set { ChatBots.DiscordBridge.Config = value; ChatBots.DiscordBridge.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.Farmer$")]
                public ChatBots.Farmer.Configs Farmer
                {
                    get { return ChatBots.Farmer.Config; }
                    set { ChatBots.Farmer.Config = value; ChatBots.Farmer.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.FollowPlayer$")]
                public ChatBots.FollowPlayer.Configs FollowPlayer
                {
                    get { return ChatBots.FollowPlayer.Config; }
                    set { ChatBots.FollowPlayer.Config = value; ChatBots.FollowPlayer.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.HangmanGame$")]
                public ChatBots.HangmanGame.Configs HangmanGame
                {
                    get { return ChatBots.HangmanGame.Config; }
                    set { ChatBots.HangmanGame.Config = value; ChatBots.HangmanGame.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.Mailer$")]
                public ChatBots.Mailer.Configs Mailer
                {
                    get { return ChatBots.Mailer.Config; }
                    set { ChatBots.Mailer.Config = value; ChatBots.Mailer.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.Map$")]
                public ChatBots.Map.Configs Map
                {
                    get { return ChatBots.Map.Config; }
                    set { ChatBots.Map.Config = value; ChatBots.Map.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.PlayerListLogger$")]
                public ChatBots.PlayerListLogger.Configs PlayerListLogger
                {
                    get { return ChatBots.PlayerListLogger.Config; }
                    set { ChatBots.PlayerListLogger.Config = value; ChatBots.PlayerListLogger.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.RemoteControl$")]
                public ChatBots.RemoteControl.Configs RemoteControl
                {
                    get { return ChatBots.RemoteControl.Config; }
                    set { ChatBots.RemoteControl.Config = value; ChatBots.RemoteControl.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.ReplayCapture$")]
                public ChatBots.ReplayCapture.Configs ReplayCapture
                {
                    get { return ChatBots.ReplayCapture.Config; }
                    set { ChatBots.ReplayCapture.Config = value; ChatBots.ReplayCapture.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.ScriptScheduler$")]
                public ChatBots.ScriptScheduler.Configs ScriptScheduler
                {
                    get { return ChatBots.ScriptScheduler.Config; }
                    set { ChatBots.ScriptScheduler.Config = value; ChatBots.ScriptScheduler.Config.OnSettingUpdate(); }
                }

                [TomlPrecedingComment("$ChatBot.TelegramBridge$")]
                public ChatBots.TelegramBridge.Configs TelegramBridge
                {
                    get { return ChatBots.TelegramBridge.Config; }
                    set { ChatBots.TelegramBridge.Config = value; ChatBots.TelegramBridge.Config.OnSettingUpdate(); }
                }
                
                [TomlPrecedingComment("$ChatBot.ItemsCollector$")]
                public ChatBots.ItemsCollector.Configs ItemsCollector
                {
                    get { return ChatBots.ItemsCollector.Config; }
                    set { ChatBots.ItemsCollector.Config = value; ChatBots.ItemsCollector.Config.OnSettingUpdate(); }
                }
                
                [TomlPrecedingComment("$ChatBot.WebSocketBot$")]
                public ChatBots.WebSocketBot.Configs WebSocketBot
                {
                    get { return ChatBots.WebSocketBot.Config!; }
                    set { ChatBots.WebSocketBot.Config = value; }
                }
            }
        }

        public static string GetDefaultGameLanguage()
        {
            string gameLanguage = "en_us";
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
                    break;
                case "de-AT":
                    gameLanguage = "de_at";
                    break;
                case "de-CH":
                    gameLanguage = "de_ch";
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
                case "en-US":
                    gameLanguage = "en_us";
                    break;
                case "en-GB":
                    gameLanguage = "en_gb";
                    break;
                case "en-AU":
                    gameLanguage = "en_au";
                    break;
                case "en-CA":
                    gameLanguage = "en_ca";
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
                    break;
                case "fr-CA":
                    gameLanguage = "fr_ca";
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
                    break;
                case "xh-ZA":
                    break;
                case "zh-Hans": /* CurrentCulture.Parent.Name */
                case "zh":
                case "zh-CN":
                case "zh-CHS":
                case "zh-SG":
                    gameLanguage = "zh_cn";
                    break;
                case "zh-Hant": /* CurrentCulture.Parent.Name */
                case "zh-HK":
                case "zh-CHT":
                case "zh-MO":
                    gameLanguage = "zh_hk";
                    break;
                case "zh-TW":
                    gameLanguage = "zh_tw";
                    break;
                case "zu-ZA":
                    break;
            }
            return gameLanguage;
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
            string msg = ex.Message.Replace("+", "->");
            return ex.InnerException == null
                 ? msg
                 : msg + "\n --> " + ex.InnerException.GetFullMessage();
        }
    }
}
