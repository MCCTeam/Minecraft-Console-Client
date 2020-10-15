﻿using System;
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
        private static string defaultTranslation = "en.ini";
        private static bool debugMessages = true; // Settings.LoadSettings have not been called yet at the time I guess. 
                                                  // Hence Settings.DebugMessages will always return false

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

            // Two letters language name
            string systemLanguage = string.IsNullOrEmpty(CultureInfo.CurrentCulture.Parent.Name) // Parent.Name might be empty
                ? CultureInfo.CurrentCulture.Name
                : CultureInfo.CurrentCulture.Parent.Name;
            string langDir = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar;
            string langFileSystemLanguage = langDir + systemLanguage + ".ini";
            string langFileDefault = langDir + defaultTranslation;

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
            string defaultPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar + defaultTranslation;

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
mcc.handshake=§8Handshake successful. (Server ID: {0})


[debug]
# Messages from MCC Debug Mode
debug.color_test=Color test: Your terminal should display {0}
debug.session_cache_ok=§8Session data has been successfully loaded from disk.
debug.session_cache_fail=§8No sessions could be loaded from disk
debug.session_id=Success. (session ID: {0})
debug.crypto=§8Crypto keys & hash generated.
debug.request=§8Performing request to {0}

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
error.invalid_response=§8Invalid response to Handshake packet
error.invalid_encrypt=§8Invalid response to StartEncryption packet
error.version_different=§8Server reports a different version than manually set. Login may not work.
error.no_version_report=§8Server does not report its protocol version, autodetection will not work.
error.connection_timeout=§8A timeout occured while attempting to connect to this IP.
error.forge=§8Forge Login Handshake did not complete successfully
error.forge_encrypt=§8Forge StartEncryption Handshake did not complete successfully
error.setting.str2int=Failed to convert '{0}' into an int. Please check your settings.
error.http_code=§8Got error code from server: {0}
error.auth=§8Got error code from server while refreshing authentication: {0}

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
extra.terrainandmovement_required=Please enable Terrain and Movements in the config file first.
# Inventory
extra.inventory_enabled=Inventory handling is now enabled.
extra.inventory_disabled=§cInventories are currently not handled for that MC version.
extra.inventory_required=Please enable InventoryHandling in the config file first.
extra.inventory_interact=Use /inventory to interact with it.
extra.inventory_open=Inventory # {0} opened: {1}
extra.inventory_close=Inventory # {0} closed.
# Entity
extra.entity_disabled=§cEntities are currently not handled for that MC version.
extra.entity_required=Please enable EntityHandling in the config file first.


[forge]
# Messages from Forge handler
forge.version=§8Forge protocol version : {0}
forge.send_mod=§8Sending falsified mod list to server...
forge.accept=§8Accepting server mod list...
forge.registry=§8Received registry with {0} entries
forge.registry_2=§8Received registry {0} with {1} entries
forge.accept_registry=§8Accepting server registries...
forge.complete=Forge server connection complete!
forge.with_mod=§8Server is running Forge with {0} mods.
forge.no_mod=§8Server is running Forge without mods.
forge.mod_list=§8Mod list:
# FML2
forge.fml2.mod=§8Received FML2 Server Mod List
forge.fml2.mod_send=§8Sending back FML2 Client Mod List
forge.fml2.registry=§8Acknowledging FML2 Server Registry: {0}
forge.fml2.config=§8Acknowledging FML2 Server Config: {0}
forge.fml2.unknown=§8Got Unknown FML2 Handshake message no. {0}
forge.fml2.unknown_channel=§8Ignoring Unknown FML2 LoginMessage channel: {0}

[cache]
# Session Cache
cache.loading=§8Loading Minecraft profiles: {0}
cache.loaded=§8Loaded session: {0}:{1}
cache.converting=§8Converting session cache from disk: {0}
cache.read_fail=§8Failed to read serialized session cache from disk: {0}
cache.malformed=§8Got malformed data while reading serialized session cache from disk: {0}
cache.loading_session=§8Loading session cache from disk: {0}
cache.ignore_string=§8Ignoring session token string '{0}': {1}
cache.ignore_line=§8Ignoring invalid session token line: {0}
cache.read_fail_plain=§8Failed to read session cache from disk: {0}
cache.saving=§8Saving session cache to disk
cache.save_fail=§8Failed to write session cache to disk: {0}

[proxy]
proxy.connected=§8Connected to proxy {0}:{1}

[chat]
# Chat Parser
chat.download=§8Downloading '{0}.lang' from Mojang servers...
chat.request=§8Performing request to {0}
chat.done=§8Done. File saved as '{0}'
chat.fail=§8Failed to download the file.
chat.from_dir=§8Defaulting to en_GB.lang from your Minecraft directory.
chat.loaded=§8Translations file loaded.
chat.not_found=§8Translations file not found: '{0}'\nSome messages won't be properly printed without this file.

[general]
# General message/information (i.e. Done)
general.done=Done
general.fail=Fail
general.bot_unload=This bot will be unloaded.
general.available_cmd=Available commands: {0}


[cmd]
# Commands. Naming style: cmd.<className>.<msg...>

# Animation
cmd.animation.desc=Swing your arm.

# ChangeSlot
cmd.changeSlot.desc=Change hotbar
cmd.changeSlot.nan=Could not change slot: Not a Number
cmd.changeSlot.changed=Changed to slot {0}
cmd.changeSlot.fail=Could not change slot

# Connect
cmd.connect.desc=connect to the specified server.
cmd.connect.unknown=Unknown account '{0}'.
cmd.connect.invalid_ip=Invalid server IP '{0}'.

# Debug
cmd.debug.desc=toggle debug messages.
cmd.debug.state_on=Debug messages are now ON
cmd.debug.state_off=Debug messages are now OFF

# Dig
cmd.dig.desc=attempt to break a block
cmd.dig.too_far=You are too far away from this block.
cmd.dig.no_block=No block at this location (Air)
cmd.dig.dig=Attempting to dig block at {0} {1} {2}
cmd.dig.fail=Failed to start digging block.

# Entitycmd
cmd.entityCmd.attacked=Entity attacked
cmd.entityCmd.used=Entity used
cmd.entityCmd.not_found=Entity not found
# How to map translation for EntityCmd......

# Exit
cmd.exit.desc=disconnect from the server.

# Health
cmd.health.desc=Display Health and Food saturation.
cmd.health.response=Health: {0}, Saturation: {1}, Level: {2}, TotalExperience: {3}

# Inventory
cmd.inventory.desc=Inventory command
cmd.inventory.creative_done=Requested {0} x{1} in slot #{2}
cmd.inventory.creative_fail=Failed to request Creative Give
cmd.inventory.need_creative=You need Gamemode Creative
cmd.inventory.container_not_found=Cannot find container, please retry with explicit ID
cmd.inventory.close=Closing Inventoy #{0}
cmd.inventory.close_fail=Failed to close Inventory #{0}
cmd.inventory.not_exist=Inventory #{0} do not exist
cmd.inventory.inventory=Inventory
cmd.inventory.inventories=Inventories
cmd.inventory.hotbar=Your selected hotbar is {0}
cmd.inventory.damage=Damage
cmd.inventory.left=Left
cmd.inventory.right=Right
cmd.inventory.middle=Middle
cmd.inventory.clicking={0} clicking slot {1} in window #{2}
cmd.inventory.no_item=No item in slot #{0}
cmd.inventory.drop=Dropped 1 item from slot #{0}
cmd.inventory.drop_stack=Dropped whole item stack from slot #{0}
# Inventory Help
cmd.inventory.help.basic=Basic usage
cmd.inventory.help.available=Available actions
cmd.inventory.help.help=\n{0} Use '/inventory help <action>' for action help.\nCreative mode give: {1}\n'player' and 'container' can be simplified to 'p' and 'c'.\nNote that parameters in '[]' are optional.
cmd.inventory.help.usage=Usage
cmd.inventory.help.list=List your inventory.
cmd.inventory.help.close=Close an opened container.
cmd.inventory.help.click=Click on an item.
cmd.inventory.help.drop=Drop an item from inventory.
cmd.inventory.help.unknown=Unknown action. 

# List
cmd.list.desc=get the player list.
cmd.list.players=PlayerList: {0}

# Log
cmd.log.desc=log some text to the console.

# Look
cmd.look.desc=look at direction or coordinates.
cmd.look.unknown=Unknown direction '{0}'
cmd.look.at=Looking at YAW: {0} PITCH: {1}
cmd.look.block=Looking at {0}

# Move
cmd.move.desc=walk or start walking.
cmd.move.enable=Enabling Terrain and Movements on next server login, respawn or world change.
cmd.move.disable=Disabling Terrain and Movements.
cmd.move.moving=Moving {0}
cmd.move.dir_fail=Cannot move in that direction.
cmd.move.walk=Walking to {0}
cmd.move.fail=Failed to compute path to {0}

# Reco
cmd.reco.desc=restart and reconnect to the server.

# Respawn
cmd.respawn.desc=Use this to respawn if you are dead.
cmd.respawn.done=You have respawned.

# Script
cmd.script.desc=run a script file.

# Send
cmd.send.desc=send a chat message or command.

# Set
cmd.set.desc=set a custom %variable%.
cmd.set.format=variable name must be A-Za-z0-9.

# Sneak
cmd.sneak.desc=Toggles sneaking
cmd.sneak.on=You are sneaking now
cmd.sneak.off=You aren't sneaking anymore

# Tps
cmd.tps.desc=Display server current tps (tick per second). May not be accurate
cmd.tps.current=Current tps

# Useblock
cmd.useblock.desc=Place a block or open chest

# Useitem
cmd.useitem.desc=Use (left click) an item on the hand
cmd.useitem.use=Used an item


[bot]
# ChatBots. Naming style: bot.<className>.<msg...>

# AutoAttack
bot.autoAttack.mode=Unknown attack mode: {0}. Using single mode as default.
bot.autoAttack.priority=Unknown priority: {0}. Using distance priority as default.

# AutoCraft
bot.autoCraft.cmd=Auto-crafting ChatBot command
bot.autoCraft.alias=Auto-crafting ChatBot command alias
bot.autoCraft.cmd.list=Total {0} recipes loaded: {1}
bot.autoCraft.cmd.resetcfg=Resetting your config to default
bot.autoCraft.recipe_not_exist=Specified recipe name does not exist. Check your config file.
bot.autoCraft.no_recipe_name=Please specify the recipe name you want to craft.
bot.autoCraft.stop=AutoCraft stopped
bot.autoCraft.available_cmd=Available commands: {0}. Use /autocraft help <cmd name> for more information. You may use /ac as command alias.
bot.autoCraft.help.load=Load the config file.
bot.autoCraft.help.list=List loaded recipes name.
bot.autoCraft.help.reload=Reload the config file.
bot.autoCraft.help.resetcfg=Write the default example config to default location.
bot.autoCraft.help.start=Start the crafting. Usage: /autocraft start <recipe name>
bot.autoCraft.help.stop=Stop the current running crafting process
bot.autoCraft.help.help=Get the command description. Usage: /autocraft help <command name>
bot.autoCraft.loaded=Successfully loaded
bot.autoCraft.start=Starting AutoCraft: {0}
bot.autoCraft.start_fail=AutoCraft cannot be started. Check your available materials for crafting {0}
bot.autoCraft.table_not_found=table not found
bot.autoCraft.close_inventory=Inventory #{0} was closed by AutoCraft
bot.autoCraft.missing_material=Missing material: {0}
bot.autoCraft.aborted=Crafting aborted! Check your available materials.
bot.autoCraft.craft_fail=Crafting failed! Waiting for more materials
bot.autoCraft.timeout=Action timeout! Reason: {0}
bot.autoCraft.error.config=Error while parsing config: {0}
bot.autoCraft.exception.empty=Empty configuration file: {0}
bot.autoCraft.exception.invalid=Invalid configuration file: {0}
bot.autoCraft.exception.item_miss=Missing item in recipe: {0}
bot.autoCraft.exception.invalid_table=Invalid tablelocation format: {0}
bot.autoCraft.exception.item_name=Invalid item name in recipe {0} at {1}
bot.autoCraft.exception.name_miss=Missing recipe name while parsing a recipe
bot.autoCraft.exception.slot=Invalid slot field in recipe: {0}
bot.autoCraft.exception.duplicate=Duplicate recipe name specified: {0}
bot.autoCraft.debug.no_config=No config found. Writing a new one.

# AutoDrop
bot.autoDrop.cmd=AutoDrop ChatBot command
bot.autoDrop.alias=AutoDrop ChatBot command alias
bot.autoDrop.on=AutoDrop enabled
bot.autoDrop.off=AutoDrop disabled
bot.autoDrop.added=Added item {0}
bot.autoDrop.incorrect_name=Incorrect item name {0}. Please try again
bot.autoDrop.removed=Removed item {0}
bot.autoDrop.not_in_list=Item not in the list
bot.autoDrop.no_item=No item in the list
bot.autoDrop.list=Total {0} in the list:\n {1}

# AutoFish
bot.autoFish.throw=Threw a fishing rod
bot.autoFish.caught=Caught a fish!
bot.autoFish.no_rod=No Fishing Rod on hand. Maybe broken?

# AutoRelog
bot.autoRelog.launch=Launching with {0} reconnection attempts
bot.autoRelog.no_kick_msg=Initializing without a kick message file
bot.autoRelog.loading=Loading messages from file: {0}
bot.autoRelog.loaded=Loaded message: {0}
bot.autoRelog.not_found=File not found: {0}
bot.autoRelog.curr_dir=Current directory was: {0}
bot.autoRelog.ignore=Disconnection initiated by User or MCC bot. Ignoring.
bot.autoRelog.disconnect_msg=Got disconnected with message: {0}
bot.autoRelog.reconnect_always=Ignoring kick message, reconnecting anyway.
bot.autoRelog.reconnect=Message contains '{0}'. Reconnecting.
bot.autoRelog.reconnect_ignore=Message not containing any defined keywords. Ignoring.
bot.autoRelog.wait=Waiting {0} seconds before reconnecting...

# ChatLog
bot.chatLog.invalid_file=Path '{0}' contains invalid characters.

# Mailer
bot.mailer.init=Initializing Mailer with settings:
bot.mailer.init.db= - Database File: {0}
bot.mailer.init.ignore= - Ignore List: {0}
bot.mailer.init.public= - Public Interactions: {0}
bot.mailer.init.max_mails= - Max Mails per Player: {0}
bot.mailer.init.db_size= - Max Database Size: {0}
bot.mailer.init.mail_retention= - Mail Retention: {0}

bot.mailer.init_fail.db_size=Cannot enable Mailer: Max Database Size must be greater than zero. Please review the settings.
bot.mailer.init_fail.max_mails=Cannot enable Mailer: Max Mails per Player must be greater than zero. Please review the settings.
bot.mailer.init_fail.mail_retention=Cannot enable Mailer: Mail Retention must be greater than zero. Please review the settings.

bot.mailer.create.db=Creating new database file: {0}
bot.mailer.create.ignore=Creating new ignore list: {0}
bot.mailer.load.db=Loading database file: {0}
bot.mailer.load.ignore=Loading ignore list: 

bot.mailer.cmd=mailer command

bot.mailer.saving=Saving message: {0}
bot.mailer.user_ignored={0} is ignored!
bot.mailer.process_mails=Looking for mails to send @ {0}
bot.mailer.delivered=Delivered: {0}

bot.mailer.cmd.getmails=--- Mails in database ---\n{0}
bot.mailer.cmd.getignored=--- Ignore list ---\n{0}
bot.mailer.cmd.ignore.added=Added {0} to the ignore list!
bot.mailer.cmd.ignore.removed=Removed {0} from the ignore list!
bot.mailer.cmd.ignore.invalid=Missing or invalid name. Usage: {0} <username>
bot.mailer.cmd.help=See usage

# ReplayCapture
bot.replayCapture.cmd=replay command
bot.replayCapture.created=Replay file created.
bot.replayCapture.stopped=Record stopped.
bot.replayCapture.restart=Record was stopped. Restart the program to start another record.

# Script
bot.script.not_found=§8[MCC] [{0}] Cannot find script file: {1}
bot.script.file_not_found=File not found: '{0}'
bot.script.fail=Script '{0}' failed to run ({1}).
bot.script.pm.loaded=Script '{0}' loaded.

# ScriptScheduler
bot.scriptScheduler.loading=Loading tasks from '{0}'
bot.scriptScheduler.not_found=File not found: '{0}'
bot.scriptScheduler.loaded_task=Loaded task:\n{0}
bot.scriptScheduler.no_trigger=This task will never trigger:\n{0}
bot.scriptScheduler.no_action=No action for task:\n{0}
bot.scriptScheduler.running_time=Time / Running action: {0}
bot.scriptScheduler.running_inverval=Interval / Running action: {0}
bot.scriptScheduler.running_login=Login / Running action: {0}
bot.scriptScheduler.task=triggeronfirstlogin: {0}\n triggeronlogin: {1}\n triggerontime: {2}\n triggeroninterval: {3}\n timevalue: {4}\n timeinterval: {5}\n action: {6}

# TestBot
bot.testBot.told=Bot: {0} told me : {1}
bot.testBot.said=Bot: {0} said : {1}
";

        #endregion
    }
}