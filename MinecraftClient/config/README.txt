==================================================================================
 Minecraft Client v1.13.0 for Minecraft 1.4.6 to 1.13.2 - By ORelio & Contributors
==================================================================================

Thanks for dowloading Minecraft Console Client!

Minecraft Console Client is a lightweight app able to connect to any minecraft server,
both offline and online mode. It enables you to send commands and receive text messages
in a fast and easy way without having to open the main Minecraft game.

============
 How to use
============

First, extract the archive if not already extracted.
On Windows, simply open MinecraftClient.exe by double-clicking on it.
On Mac or Linux you need to install the Mono Runtime:
 - On Mac: http://www.mono-project.com/download/#download-mac
 - On Linux: sudo apt-get install mono-runtime libmono-reflection-cil
Then, open a terminal in this folder and run "mono MinecraftClient.exe".
If you cannot authenticate on Mono, you'll need to run "mozroots --import --ask-remove" once.
If Mono crashes, retry with mono-complete instead of mono-runtime. Mono v4.0 to 4.2 is recommended.

===========================================
 Using Configuration files & Enabling bots
===========================================

Simply open the INI configuration file with a text editor and change the values.
To enable a bot change the "enabled" value in the INI file from "false" to "true".
You will still be able to send and receive chat messages when a bot is loaded.
You can remove or comment some lines from the INI file to use the default values instead.
You can have several INI files and drag & drop one of them over MinecraftClient.exe

====================
 Command-line usage
====================

> MinecraftClient.exe username password server
This will automatically connect you to the chosen server.
To specify a server and ask password interactively, use "" as password.
To specify offline mode with no password, use "-" as password.

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
 - list : list players logged in to the server (uses tab list info sent by server)
 - set varname=value : set a value which can be used as %varname% in further commands
 - wait <time> : wait X ticks (10 ticks = ~1 second. Only for scripts)
 - move : used for moving when terrain and movements feature is enabled
 - look : used for looking at direction when terrain and movements is enabled
 - debug : toggle debug messages, useful for chatbot developers
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

======================================
 Interacting with the Minecraft world
======================================

By default, Minecraft Console Client cannot interact with the world around you.
However for some versions of the game you can enable the terrainandmovements setting.

This feature will allow you to properly fall on ground, pickup items and move around.
There is a C# API for reading terrain data around the player and moving from C# scripts.

Please note that this requires much more RAM to store all the terrain data, a bit more CPU
to process all of this, and slightly more bandwidth as locations updates are
sent back to the server in a spammy way (that's how Minecraft works).

============================
 How to write a script file
============================

A script file can be launched by using /script <filename> in the client's command prompt.
The client will automatically look for your script in the current directory or "scripts" subfolder.
If the file extension is .txt or .cs, you may omit it and the client will still find the script.

Regarding the script file, it is a text file with one instruction per line.
Any line beginning with "#" is ignored and treated as a comment.
Allowed instructions are given in "Internal commands" section.

Application variables defined using the 'set' command or [AppVars] INI section can be used.
The following read-only variables can also be used: %username%, %serverip%, %serverport%

==========================
 How to write a C# script
===========================

If you are experienced with C#, you may also write a C# script.
That's a bit more involved, but way more powerful than regular scripts.
You can look at the provided sample C# scripts for getting started.

C# scripts can be used for creating your own ChatBot without recompiling the whole project.
These bots are embedded in a script file, which is compiled and loaded on the fly.
ChatBots can access plugin channels for communicating with some server plugins.

For knowing everything the API has to offer, you can look at CSharpRunner.cs and ChatBot.cs.
The latest version for these files can be found on the GitHub repository.

==========================
 Using HTTP/Socks proxies
==========================

If you are on a restricted network you might want to use some HTTP or SOCKS proxies.
To do so, find a proxy, enable proxying in INI file and fill in the relevant settings.
Proxies with username/password authentication are supported but have not been tested.
Not every proxy will work for playing Minecraft, because of port 80/443 web browsing restrictions.
However you can choose to use a proxy for login only, most proxies should work in this mode.

=============================================
 Connecting to servers when ping is disabled
=============================================

On some servers, the server list ping feature has been disabled, which prevents Minecraft Console Client
from pinging the server to determine the Minecraft version to use. To connect to this kind of servers,
find out which Minecraft version is running on the server, and fill in the 'mcversion' field in INI file.
This will disable the ping step while connecting, but requires you to manually provide the version to use.
Recent versions of Minecraft Console Client may also prompt you for MC version in case of ping failure.

=========================
 About translation files
=========================

When connecting to 1.6+ servers, you will need a translation file to display properly some chat messages.
These files describe how some messages should be printed depending on your preferred language.
The client will automatically load en_GB.lang from your Minecraft folder if Minecraft is installed on your
computer, or download it from Mojang's servers. You may choose another language in the configuration file.

=========================
 Detecting chat messages
=========================

Minecraft Console Client can parse messages from the server in order to detect private and public messages.
This is useful for reacting to messages eg when using the AutoRespond, Hangman game, or RemoteControl bots.
However, for unusual chat formats, so you may need to tinker with the ChatFormat section of the config file.
Building regular expressions can be a bit tricky, so you might want to try them out eg on regex101.com

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
If the kick message contains one of them, you will automatically be re-connected.
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
Edit the provided configuration files to customize the words and the bot owners.
If it doesn't respond to bot owners, read the "Detecting chat messages" section.

==========================
 Using the Remote Control
==========================

When the remote control bot is enabled, you can send commands to your bot using whispers.
Don't forget to add your username in botowners INI setting if you want it to obey.
If it doesn't respond to bot owners, read the "Detecting chat messages" section.
Please note that server admins can read what you type and output from the bot.

To perform a command simply do the following: /tell <yourbot> <thecommand>
Where <thecommand> is an internal command as described in "Internal commands" section.
You can remotely send chat messages or commands using /tell <yourbot> send <thetext>

Remote control system can by default auto-accept /tpa and /tpahere requests from the bot owners.
Auto-accept can be disabled or extended to requests from anyone in remote control configuration.

===============================
 Using the AutoRespond feature
===============================

The AutoRespond bot allows you to automatically react on specific chat messages or server announcements.
You can use either a string to detect in chat messages, or an advanced regular expression.
For more information about how to define match rules, please refer to sample-matches.ini

============
 Disclaimer
============

Even if everything should work, I am not responsible of any damage this app could cause to your computer or your server.
This app does not steal your password. If you don't trust it, don't use it or check & compile from the source code.

Also, remember that when you connect to a server with this program, you will appear where you left the last time.
This means that you can die if you log in in an unsafe place on a survival server!
Use the script scheduler bot to send a teleport command after logging in.

=========
 License
=========

Minecraft Console Client is a totally free of charge, open source project.
Source code is available at https://github.com/ORelio/Minecraft-Console-Client

Unless specifically stated, source code is from me or contributors, and available under CDDL-1.0.
More info about CDDL-1.0: http://qstuff.blogspot.fr/2007/04/why-cddl.html
Full license at http://opensource.org/licenses/CDDL-1.0

=========
 Credits
=========

Even though I'm the main author of Minecraft Console Client, many features
would not have been possible without the help of talented contributors:

Ideas

  ambysdotnet, Awpocalypse, azoundria, bearbear12345, bSun0000, Cat7373, dagonzaros, Dids,
  Elvang, fuckofftwice, GeorgH93, initsuj, JamieSinn, joshbean39, LehmusFIN, maski, medxo,
  mobdon, MousePak, TNT-UP, TorchRJ, yayes2, Yoann166, ZizzyDizzyMC

Bug Hunters

  1092CQ, ambysdotnet, bearbear12345, c0dei, Cat7373, Darkaegis, dbear20, DigitalSniperz,
  doranchak, drXor, FantomHD, gerik43, ibspa, iTzMrpitBull, JamieSinn, k3ldon, KenXeiko,
  link3321, lyze237, mattman00000, Nicconyancat, Pokechu22, ridgewell, Ryan6578, Solethia,
  TNT-UP, TorchRJ, TRTrident, WeedIsGood, xp9kus, Yoann166

Code contributions

  Allyoutoo, Aragas, Bancey, bearbear12345, corbanmailloux, dbear20, dogwatch, initsuj,
  JamieSinn, justcool393, lokulin, maxpowa, medxo, Pokechu22, repository, TheMeq, TheSnoozer,
  vkorn, v1RuX, ZizzyDizzyMC

Libraries

  Minecraft Console Client also borrows code from the following libraries:

  -----------------------------------------------------------------
    Name           Purpose             Author             License
  -----------------------------------------------------------------
    Biko           Proxy handling      Benton Stark       MIT
    BouncyCastle   CFB-8 AES on Mono   The Legion         MIT
    Heijden.Dns    DNS SRV Lookup      Geoffrey Huntley   MIT
    DotNetZip      Zlib compression    Dino Chiesa        MS-PL
  -----------------------------------------------------------------

=========
 Support
=========

If you still have any question after reading this file, you can get support here:

 - General Questions: http://www.minecraftforum.net/topic/1314800-/
 - Bugs & Issues: https://github.com/ORelio/Minecraft-Console-Client/issues

Like Minecraft Console Client? You can buy me a coffee here:

 - https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=EALHERGB9DQY8

Code contributions, bug reports and any kind of comments are also highly appreciated :)

+-----------------------------------+
| © 2012-2019 ORelio & Contributors |
+-----------------------------------+
