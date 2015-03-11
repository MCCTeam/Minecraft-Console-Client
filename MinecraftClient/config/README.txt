==================================================================
 Minecraft Client v1.8.2 for Minecraft 1.4.6 to 1.8.3 - By ORelio
==================================================================

Thanks for dowloading Minecraft Console Client!

Minecraft Console Client is a lightweight app able to connect to any minecraft server,
both offline and online mode. It enables you to send commands and receive text messages
in a fast and easy way without having to open the main Minecraft game.

============
 How to use
============

First, extract the archive if not already extracted.
On Windows, simply open MinecraftClient.exe by double-clicking on it.
On Mac or Linux, open a terminal in this folder and run "mono MinecraftClient.exe".

===========================================
 Using Configuration files & Enabling bots
===========================================

Simply open the INI file with a text editor and change the values.
To enable a bot change the "enabled" value in the INI file from "false" to "true".
You will still be able to send and receive chat messages when a bot is loaded.
You can remove or comment some lines from the INI file to use the default values instead.
You can have several INI files and drag & drop one of them over MinecraftClient.exe

====================
 Command-line usage
====================

> MinecraftClient.exe username password server
This will automatically connect you to the chosen server.

> MinecraftClient.exe username password server "/mycommand"
This will automatically send "/mycommand" to the server and close.
To send several commands and maybe stay connected, use the Scripting bot instead.

> MinecraftClient.exe myconfig.ini
This will load the specified configuration file
If the file contains login / password / server ip, it will automatically connect.

> MinecraftClient.exe myconfig.ini othername otherpassword otherIP
Load the specified configuration file and override some settings from the file.

===================
 Internal commands
===================

These commands can be performed from the chat prompt, scripts or remote control.
From chat prompt, commands must by default be prepended with a slash, eg. /quit
In scripts and remote control, no slash is needed to perform the command.

 - quit or exit: disconnect from the server and close the application
 - reco [account] : disconnect and reconnect to the server
 - connect <server> [account] : go to the given server and resume the script
 - script <script name> : run a script containing a list of commands
 - send <text> : send a message or a command to the server
 - respawn : Use this to respawn if you are dead (like clicking "respawn" ingame)
 - log <text> : display some text in the console (useful for scripts)
 - set varname=value : set a value which can be used as %varname% in further commands
 - wait <time> : wait X ticks (10 ticks = ~1 second. Only for scripts)
 - help : show command help. Tip: Use "/send /help" for server help

[account] is an account alias defined in accounts file, read more below.
<server> is either a server IP or a server alias defined in servers file

===========================
 Servers and Accounts file
===========================

These two files can be used to store info about accounts and server, and give them aliases.
The purpose of this is to give them an easy-to-remember alias and to avoid typing account passwords.
As what you are typing can be read by the server admin if using the remote control feature,
using aliases is really important for privacy and for safely switching between accounts.
To use these files, simply take a look at sample-accounts.txt and sample-servers.txt.
Once you have created your files, fill the 'accountlist' and 'serverlist' fields in INI file.

============================
 How to write a script file
============================

A script can be launched by using /script <filename> in the client
The client will automatically look for your script in the current directory or "scripts" subfolder.
If the file extension is .txt, you may omit it and the client will still find the script

Regarding the script file, it is a text file with one instruction per line.
Any line beginning with "#" is ignored and treated as a comment.
Allowed instructions are given in "Internal commands" section.

Application variables defined using the 'set' command or [AppVars] INI section can be used.
The following read-only variables can also be used: %username%, %serverip%, %serverport%

==========================
 Using HTTP/Socks proxies
==========================

If you are on a restricted network you might want to use some HTTP or SOCKS proxies.
To do so, find a proxy, enable proxying in INI file and fill in the relevant settings.
Proxy with username/password authentication are supported but have not been tested.

=============================================
 Connecting to servers when ping is disabled
=============================================

On some server, the server list ping feature has been disabled, which prevents Minecraft Console Client
from pinging the server to determine the Minecraft version to use. To connect to this kind of servers,
find out which Minecraft version is running on the server, and fill in the 'mcversion' field in INI file.
This will disable the ping step while connecting, but requires you to manually provide the version to use.

=========================
 About translation files
=========================

When connecting to 1.6+ servers, you will need a translation file to display properly some chat messages.
These files describe how some messages should be printed depending on your preferred language.
The client will automatically load en_GB.lang from your Minecraft folder if Minecraft is installed on your
computer, or download it from Mojang's servers. You may choose another language in the config file.

======================
 Using the Alerts bot
======================

Write in alerts.txt the words you want the console to beep/alert you on.
Write in alerts-exclude.txt the words you want NOT to be alerted on.
For example write Yourname in alerts and <Yourname> in alerts-exclude.txt

=========================
 Using the AutoRelog bot
=========================

Write in kickmessages.txt some words, such as "Restarting" for example.
If the kick message contains one of them, you will be automatically re-connected
A kick message "Connection has been lost." is generated by the console itself when connection is lost.
A kick message "Login failed." is generated the same way when it failed to login to the server.
A kick message "Failed to ping this IP." is generated when it failed to ping the server.
You can use them for reconnecting when connection is lost or the login failed.

============================
 Using the Script Scheduler
============================

The script scheduler allows you to perform scripts on various events.
Simply enable the ScriptScheduler bot and specify a tasks file in your INI file.
Please read sample-tasks.ini for learning how to make your own task file.

========================
 Using the hangman game
========================

Use "/tell <bot username> start" to start the game.
Don't forget to add your username in botowners INI setting if you want it to obey.
Edit the provided configuration files to customize the words and the owners.

==========================
 Using the Remote Control
==========================

When the remote control bot is enabled, you can send commands to your bot using whispers.
Don't forget to add your username in botowners INI setting if you want it to obey.
To perform a command simply do the following: /tell <yourbot> <thecommand>
Where <thecommand> is an internal command as described in "Internal commands" section.
You can remotely send chat messages or commands using /tell <yourbot> send <thetext>
Remote control system can auto-accept /tpa and /tpahere requests from the bot owners.
Auto-accept can be disabled or extended to requests from anyone in remote control configuration.

=========================
 Disclaimer & Last words
=========================

Even if everything should work, I am not responsible of any damage this app could cause to your computer or your server.
This app does not steal your password. If you don't trust it, don't use it or check & compile the source code.

Also, remember that when you connect to a server with this program, you will appear where you left the last time.
This means that you can die if you log in in an unsafe place on a survival server!
Use the script scheduler bot to send a teleport command after logging in.

You can find more info at:
http://www.minecraftforum.net/topic/1314800-/

+--------------------+
| © 2012-2015 ORelio |
+--------------------+
