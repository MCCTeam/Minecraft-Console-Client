using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MinecraftClient
{
    /// <summary>
    /// Allows to localize the app in different languages
    /// </summary>
    /// <remarks>
    /// By ORelio (c) 2015-2018 - CDDL 1.0
    /// </remarks>
    public static class Translations
    {
        private static Dictionary<string, string> translations;

        /// <summary>
        /// Return a tranlation for the requested text
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

            /*
             * External translation files
             * These files are loaded from the installation directory as:
             * Lang/abc.ini, e.g. Lang/eng.ini which is the default language file
             * Useful for adding new translations of fixing typos without recompiling
             */

            string systemLanguage = CultureInfo.CurrentCulture.ThreeLetterISOLanguageName;
            string langDir = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "MCC_Lang" + Path.DirectorySeparatorChar;
            string langFileSystemLanguage = langDir + systemLanguage + ".ini";
            string langFileDefault = langDir + "eng.ini";

            string langFileToUse = langFileDefault;
            if (File.Exists(langFileSystemLanguage))
                langFileToUse = langFileSystemLanguage;

            // Write the language file for Eng if does not exist
            if (!File.Exists(langFileDefault))
                WriteDefaultLang();

            foreach (string lineRaw in File.ReadAllLines(langFileToUse, Encoding.UTF8))
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

            /* 
             * Hardcoded translation data
             * This data is used as fallback if no translation file could be loaded
             * Useful for standalone exe portable apps
             

            else if (systemLanguage == "fra")
            {
                translations["about"] = "A Propos";
                //Ajouter de nouvelles traductions ici
            }
            //Add new languages here as 'else if' blocks
            //English is the default language in 'else' block below
            else
            {
                translations["about"] = "About";
                //Add new translations here
            }
            */
        }

        public static void Write(string text)
        {
            ConsoleIO.Write(Get(text));
        }

        public static void WriteLineFormatted(string str, bool acceptnewlines = true, bool? displayTimestamp = null)
        {
            ConsoleIO.WriteLineFormatted(Get(str), acceptnewlines, displayTimestamp);
        }

        public static void WriteLine(string line, params object[] args)
        {
            if (args.Length > 0)
                ConsoleIO.WriteLine(String.Format(Get(line), args));
            else ConsoleIO.WriteLine(Get(line));
        }

        public static void WriteLogLine(string text, bool acceptnewlines = true)
        {
            ConsoleIO.WriteLogLine(Get(text), acceptnewlines);
        }

        private static void WriteDefaultLang()
        {
            string defaultPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "MCC_Lang" + Path.DirectorySeparatorChar + "eng.ini";
            string content = @"[mcc]
# Messages from MCC itself
mcc.description=Console Client for MC {0} to {1} - v{2} - By ORelio & Contributors
mcc.keyboard_debug=Keyboard debug mode: Press any key to display info
mcc.setting=[Settings] Loading Settings from {0}
mcc.login=Login :
mcc.login_basic_io=Please type the username or email of your choice.\n
mcc.password=Password : 
mcc.password_basic_io=Please type the password for {0}.\n
mcc.password_hidden=Password : <******>
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
mcc.autocomplete=autocomplete
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
debug.color_test=Color test: Your terminal should display [0123456789ABCDEF]: [§00§11§22§33§44§55§66§77§88§99§aA§bB§cC§dD§eE§fF§r]
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
            File.WriteAllText(defaultPath, content);
        }
    }
}