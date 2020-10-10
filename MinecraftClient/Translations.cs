using System;
using System.Text;
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
        private static bool debugMessages = true; // Settings.LoadSettings have not been called yet at the time I guess. 
                                                  // Hence Settings.DebugMessages will always return false

        /// <summary>
        /// Return a tranlation for the requested text. Support string formatting
        /// </summary>
        /// <param name="msg_name">text identifier</param>
        /// <returns>returns translation for this identifier</returns>
        public static string Get(string msg_name, params object[] args)
        {
            if (translations.ContainsKey(msg_name))
            {
                if (args.Length > 0)
                {
                    return string.Format(translations[msg_name], args);
                }
                else return translations[msg_name];
            }
            return msg_name.ToUpper();
        }

        /// <summary>
        /// Initialize translations depending on system language.
        /// English is the default for all unknown system languages.
        /// </summary>
        static Translations()
        {
            translations = new Dictionary<string, string>();
            LoadTranslationsFile();
        }

        /// <summary>
        /// Load translation file depends on system language. Default to English if translation file does not exist
        /// </summary>
        private static void LoadTranslationsFile()
        {
            /*
             * External translation files
             * These files are loaded from the installation directory as:
             * Lang/abc.ini, e.g. Lang/eng.ini which is the default language file
             * Useful for adding new translations of fixing typos without recompiling
             */
            string systemLanguage = CultureInfo.CurrentCulture.ThreeLetterISOLanguageName;
            string langDir = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar;
            string langFileSystemLanguage = langDir + systemLanguage + ".ini";
            string langFileDefault = langDir + "eng.ini";

            // Write the language file for English to the disk if does not exist
            if (!File.Exists(langFileDefault))
            {
                WriteDefaultTranslation();
            }
            else // Check default language file is outdated or not and update it
            {
                string diskContent = File.ReadAllText(langFileDefault, Encoding.UTF8);
                if (diskContent.GetHashCode() != engLanguage.GetHashCode())
                {
                    if (debugMessages)
                        Console.WriteLine("Default language file is different from the newest version! Replacing it."); // How to translate this LOL
                    WriteDefaultTranslation();
                }
            }
            string[] engLang = engLanguage.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); // use embedded translations

            // Load Eng language as default
            ParseTranslationContent(engLang);

            // Then load language file for system language.
            // Missing translation key will be covered by Eng loaded above
            if (File.Exists(langFileSystemLanguage))
                ParseTranslationContent(File.ReadAllLines(langFileSystemLanguage));
            else
                if (debugMessages)
                    ConsoleIO.WriteLogLine("No translation file found for " + systemLanguage + ". (Looked '" + langFileSystemLanguage + "'");
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
            string defaultPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar + "eng.ini";

            if (!Directory.Exists(translationFilePath))
            {
                Directory.CreateDirectory(translationFilePath);
            }
            File.WriteAllText(defaultPath, engLanguage, Encoding.UTF8);
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

        #region English Translation File

        private static string engLanguage = @"[mcc]
# Messages from MCC itself
mcc.description=Console Client for MC {0} to {1} - v{2} - By ORelio & Contributors
mcc.keyboard_debug=Keyboard debug mode: Press any key to display info
mcc.setting=Loading Settings from {0}
mcc.login=Login :
mcc.login_basic_io=Please type the username or email of your choice.
mcc.password=Password : 
mcc.password_basic_io=Please type the password for {0}.
mcc.password_hidden=Password : {0}
mcc.offline=§8You chose to run in offline mode.
mcc.session_invalid=§8Cached session is invalid or expired.
mcc.session_valid=§8Cached session is still valid for {0}.
mcc.connecting=Connecting to Minecraft.net...
mcc.ip=Server IP : 
mcc.use_version=§8Using Minecraft version {0} (protocol v{1})
mcc.unknown_version=§8Unknown or not supported MC version {0}.\nSwitching to autodetection mode.
mcc.forge=Checking if server is running Forge...
mcc.resolve=Resolving {0}...
mcc.found=§8Found server {0}:{1} for domain {2}
mcc.not_found=§8Failed to perform SRV lookup for {0}\n{1}: {2}
mcc.retrieve=Retrieving Server Info...
mcc.restart=Restarting Minecraft Console Client...
mcc.restart_delay=Waiting {0} seconds before restarting...
mcc.server_version=Server version : 
mcc.disconnected=Not connected to any server. Use '{0}help' for help.
mcc.press_exit=Or press Enter to exit Minecraft Console Client.
mcc.version_supported=Version is supported.\nLogging in...
mcc.single_cmd=§7Command §8 {0} §7 sent.
mcc.joined=Server was successfully joined.\nType '{0}quit' to leave the server.
mcc.reconnect=Waiting 5 seconds ({0} attempts left)...
mcc.disconnect.lost=Connection has been lost.
mcc.disconnect.server=Disconnected by Server :
mcc.disconnect.login=Login failed :
mcc.link=Link: {0}
mcc.player_dead_respawn=You are dead. Automatically respawning after 1 second.
mcc.player_dead=You are dead. Type /respawn to respawn.
mcc.server_offline=§8Server is in offline mode.
mcc.session=Checking Session...
mcc.session_fail=Failed to check session.
mcc.server_protocol=§8Server version : {0} (protocol v{1})
mcc.with_forge=, with Forge


[debug]
# Messages from MCC Debug Mode
debug.color_test=Color test: Your terminal should display {0}
debug.session_cache_ok=§8Session data has been successfully loaded from disk.
debug.session_cache_fail=§8No sessions could be loaded from disk
debug.session_id=Success. (session ID: {0})
debug.crypto=§8Crypto keys & hash generated.

[error]
# Error messages from MCC
error.ping=Failed to ping this IP.
error.unsupported=Cannot connect to the server : This version is not supported !
error.determine=Failed to determine server version.
error.login=Minecraft Login failed : 
error.login.migrated=Account migrated, use e-mail as username.
error.login.server=Login servers are unavailable. Please try again later.
error.login.blocked=Incorrect password, blacklisted IP or too many logins.
error.login.response=Invalid server response.
error.login.premium=User not premium.
error.login.network=Network error.
error.login.ssl=SSL Error.
error.login.unknown=Unknown Error.
error.login.ssl_help=§8It appears that you are using Mono to run this program.\nThe first time, you have to import HTTPS certificates using:\nmozroots --import --ask-remove
error.login_failed=Failed to login to this server.
error.join=Failed to join this server.
error.connect=Failed to connect to this IP.
error.timeout=Connection Timeout
error.unexpect_response=§8Unexpected response from the server (is that a Minecraft server?)
error.version_different=§8Server reports a different version than manually set. Login may not work.
error.no_version_report=§8Server does not report its protocol version, autodetection will not work.
error.connection_timeout=§8A timeout occured while attempting to connect to this IP.
error.forge=§8Forge Login Handshake did not complete successfully
error.forge_encrypt=§8Forge StartEncryption Handshake did not complete successfully
error.setting.str2int=Failed to convert '{0}' into an int. Please check your settings.

[internal command]
# MCC internal help command
icmd.help=help <cmdname>: show brief help about a command.
icmd.unknown=Unknown command '{0}'. Use 'help' for command list.
icmd.list=help <cmdname>. Available commands: {0}. For server help, use '{1}send /help' instead.
icmd.error=OnInternalCommand: Got error from {0}: {1}

[exception]
# Exception messages threw by MCC
exception.user_logout=User-initiated logout should be done by calling Disconnect()
exception.unknown_direction=Unknown direction
exception.palette.block=Please update block types handling for this Minecraft version. See Material.cs
exception.palette.entity=Please update entity types handling for this Minecraft version. See EntityType.cs
exception.palette.item=Please update item types handling for this Minecraft version. See ItemType.cs
exception.palette.packet=Please update packet type palette for this Minecraft version. See PacketTypePalette.cs
exception.packet_process=Failed to process incoming packet of type {0}. (PacketID: {1}, Protocol: {2}, LoginPhase: {3}, InnerException: {4}).
exception.version_unsupport=The protocol version no.{0} is not supported.


[extra]
# Inventory, Terrain & Movements, Entity related messages
# Terrain & Movements
extra.terrainandmovement_enabled=Terrain and Movements is now enabled.
extra.terrainandmovement_disabled=§cTerrain & Movements currently not handled for that MC version.
# Inventory
extra.inventory_enabled=Inventory handling is now enabled.
extra.inventory_disabled=§cInventories are currently not handled for that MC version.
extra.inventory_interact=Use /inventory to interact with it.
extra.inventory_open=Inventory # {0} opened: {1}
extra.inventory_close=Inventory # {0} closed.
# Entity
extra.entity_disabled=§cEntities are currently not handled for that MC version.
";

        #endregion
    }
}