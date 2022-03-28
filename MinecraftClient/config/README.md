Minecraft Console Client User Manual
======

**Thanks for dowloading Minecraft Console Client!**

Minecraft Console Client is a lightweight app able to connect to any minecraft server,
both offline and online mode. It enables you to send commands and receive text messages
in a fast and easy way without having to open the main Minecraft game.

How to use
------

First, extract the archive if not already extracted.
On Windows, simply open MinecraftClient.exe by double-clicking on it.
On Mac or Linux you need to install the Mono Runtime:
 - On Mac: http://www.mono-project.com/download/#download-mac
 - On Linux: sudo apt-get install mono-runtime libmono-reflection-cil
Then, open a terminal in this folder and run "mono MinecraftClient.exe".
If you cannot authenticate on Mono because you have TLS/HTTPS/Certificate errors, you'll need to run `mozroots --import --ask-remove` once or install `ca-certificates-mono` (See [#1708](https://github.com/MCCTeam/Minecraft-Console-Client/issues/1708#issuecomment-893768862)).
If Mono crashes, retry with `mono-complete` instead of `mono-runtime`. Use at least Mono v4.0.

Docker
------

Using Docker do the following:

**Building the Image:**
```bash
# Using HTTPS
git clone https://github.com/MCCTeam/Minecraft-Console-Client.git

# Using SSH
git clone git@github.com:MCCTeam/Minecraft-Console-Client.git

cd Minecraft-Console-Client/Docker

docker build -t minecraft-console-client:latest .
```

**Start the container using Docker:**
```bash
# You could also ignore the -v parameter if you dont want to mount the volume that is up to you. If you don't it's harder to edit the .ini file if thats something you want to do
docker run -it -v <PATH_ON_YOUR_MACHINE_TO_MOUNT>:/opt/data minecraft-console-client:latest
```
Now you could login and the Client is running. To detach from the Client but still keep it running in the Background press: `CTRL + P`, `CTRL + Q`.
To reattach use the `docker attach` command.

**Start the container using docker-compose:**

By default, the volume of the container gets mapped into a new folder named `data` in the same folder the `docker-compose.yml` is stored.

If you don't want to map a volume, you have to comment out or delete the entire volumes section:
```yml
#volumes:
#- './data:/opt/data'
```
Make sure you are in the directory the `docker-compose.yml` is stored before you attempt to start. If you do so, you can start the container:
```bash
docker-compose run MCC
```
Remember to remove the container after usage:
```bash
docker-compose down
```

If you use the INI file and entered your data (username, password, server) there, you can start your container using
```bash
docker-compose up
docker-compose up -d #for deamonized running in the background
```
Note that you won't be able to interact with the client using `docker-compose up`. If you want that functionality, please use the first method: `docker-compose run MCC`.
As above, you can stop and remove the container using
```bash
docker-compose down
```

Using Configuration files & Enabling bots
------

Simply open the INI configuration file with a text editor and change the values.
To enable a bot change the `enabled` value in the INI file from `false` to `true`.
You will still be able to send and receive chat messages when a bot is loaded.
You can remove or comment some lines from the INI file to use the default values instead.
You can have several INI files and drag & drop one of them over MinecraftClient.exe

Command-line usage
------

Quick usage:

```
MinecraftClient.exe --help
MinecraftClient.exe <username> <password> <server>
MinecraftClient.exe <username> <password> <server> "/mycommand"
MinecraftClient.exe --setting=value [--other settings]
MinecraftClient.exe --section.setting=value [--other settings]
MinecraftClient.exe <settings-file.ini> [--other settings]
```
You can mix and match arguments by following theses rules:
 * First positional argument may be either the login or settings file
 * Other positional arguments are read in order: login, password, server, command
 * Arguments starting with `--` can be in any order and position

Examples and further explanations:

```
MinecraftClient.exe <login> <password> <server>
```

* This will automatically connect you to the chosen server.
* You may omit password and/or server to specify e.g. only the login
* To specify a server but ask password interactively, use `""` as password.
* To specify offline mode with no password, use `-` as password.

```
MinecraftClient.exe <login> <password> <server> "/mycommand"
```

* This will automatically send `/mycommand` to the server and close.
* To send several commands and/or stay connected, use the ScriptScheduler bot instead.

```
MinecraftClient.exe <myconfig.ini>
```

* This will load the specified configuration file
* If the file contains login / password / server ip, it will automatically connect.

```
MinecraftClient.exe --setting=value [--other settings]
```

* Specify settings on the command-line, see possible value in the configuration file
* Use `--section.setting=value` for settings outside the `[Main]` section
* Example: `--antiafk.enabled=true` for enabling the AntiAFK bot

```
MinecraftClient.exe <myconfig.ini> <login> <password> <server> [--other settings]
```

* Load the specified configuration file and override some settings from the file

Internal commands
------

These commands can be performed from the chat prompt, scripts or remote control.
From chat prompt, commands must by default be prepended with a slash, eg. `/quit`.
In scripts and remote control, no slash is needed to perform the command, eg. `quit`.

 - `quit` or `exit`: disconnect from the server and close the application
 - `reco [account]`: disconnect and reconnect to the server
 - `connect <server> [account]`: go to the given server and resume the script
 - `script <script name>`: run a script containing a list of commands
 - `send <text>`: send a message or a command to the server
 - `respawn`: Use this to respawn if you are dead (like clicking "respawn" ingame)
 - `log <text>`: display some text in the console (useful for scripts)
 - `list`: list players logged in to the server (uses tab list info sent by server)
 - `set varname=value`: set a value which can be used as `%varname%` in further commands
 - `setrnd variable string1 "\"string2\" string3"`: set a `%variable%` to one of the provided values
 - `setrnd variable -7to10`: set a `%variable%` to a number from -7 to 9
 - `wait <time>`: wait X ticks (10 ticks = ~1 second. Only for scripts)
 - `move`: used for moving when terrain and movements feature is enabled
 - `look`: used for looking at direction when terrain and movements is enabled
 - `debug`: toggle debug messages, useful for chatbot developers
 - `help`: show command help. Tip: Use "/send /help" for server help
 - Some commands may not be documented yet, use `help` to list them all.

`[account]` is an account alias defined in accounts file, read more below.

`<server>` is either a server IP or a server alias defined in servers file

Servers and Accounts file
------

These two files can be used to store info about accounts and server, and give them aliases.
The purpose of this is to give them an easy-to-remember alias and to avoid typing account passwords.

As what you are typing can be read by the server admin if using the remote control feature,
using aliases is really important for privacy and for safely switching between accounts.

To use these files, simply take a look at [`sample-accounts.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-accounts.txt) and [`sample-servers.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-servers.txt).
Once you have created your files, fill the `accountlist` and `serverlist` fields in INI file.

Interacting with the Minecraft world
------

By default, Minecraft Console Client cannot interact with the world around you.
However, for some versions of the game you can enable the `terrainandmovements` setting.

This feature will allow you to properly fall on ground, pickup items and move around.
There is a C# API for reading terrain data around the player and moving from C# scripts.

Please note that this requires much more RAM to store all the terrain data, a bit more CPU
to process all of this, and slightly more bandwidth as locations updates are
sent back to the server in a spammy way (that's how Minecraft works).

How to write a script file
------

A script file can be launched by using `/script <filename>` in the client's command prompt.
The client will automatically look for your script in the current directory or `scripts` subfolder.
If the file extension is `.txt` or `.cs`, you may omit it and the client will autodectect the extension.

Regarding the script file, it is a text file with one instruction per line.
Any line beginning with `#` is ignored and treated as a comment.
Allowed instructions are given in [Internal commands](#internal-commands) section.

Application variables defined using the `set` command or `[AppVars]` INI section can be used.
The following read-only variables can also be used: %username%, %login%, %serverip%, %serverport%

How to write a C# script
------

If you are experienced with C#, you may also write a C# script.
That's a bit more involved, but way more powerful than regular scripts.
You can look at the provided sample C# scripts for getting started.

C# scripts can be used for creating your own ChatBot without recompiling the whole project.
These bots are embedded in a script file, which is compiled and loaded on the fly.
ChatBots can access plugin channels for communicating with some server plugins.

For knowing everything the API has to offer, you can look at [`CSharpRunner.cs`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/CSharpRunner.cs) and [`ChatBot.cs`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/ChatBot.cs).

The structure of the C# file must be like this:
```csharp
//MCCScript 1.0

MCC.LoadBot(<instance of your class which extends the ChatBot class>);

//MCCScript Extensions

<your class code here>
```
The first line always needs to be `//MCCScript 1.0` comment, as the program requires it to determine the version of the script.

Everything between `//MCCScript 1.0` and `//MCCScript Extensions` comments will be treated as code, that part of the code will be inserted into a class method at compile time. The main part of the script has access to the [`CSharpRunner.cs`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/CSharpRunner.cs) API while the ChatBot defined in the Extensions section will use the [`ChatBot.cs`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/ChatBot.cs) API.

You can include standard .NET libraries/namespaces using the following syntax: `//using <name>;`. Example: `//using System.Net;`

Some sample scripts and optional Chatbots are made available in the [`config`](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config) folder.

Using HTTP/Socks proxies
------

If you are on a restricted network you might want to use some HTTP or SOCKS proxies.
To do so, find a proxy, enable proxying in INI file and fill in the relevant settings.
Proxies with username/password authentication are supported but have not been tested.
Not every proxy will work for playing Minecraft, because of port 80/443 web browsing restrictions.
However, you can choose to use a proxy for login only, most proxies should work in this mode.

Connecting to servers when ping is disabled
------

On some servers, the server list ping feature has been disabled, which prevents Minecraft Console Client
from pinging the server to determine the Minecraft version to use. To connect to this kind of servers,
find out which Minecraft version is running on the server, and fill in the `mcversion` field in INI file.
This will disable the ping step while connecting, but requires you to manually provide the version to use.
Recent versions of Minecraft Console Client may also prompt you for MC version in case of ping failure.

About translation files
------

When connecting to 1.6+ servers, you will need a translation file to display properly some chat messages.
These files describe how some messages should be printed depending on your preferred language.
The client will automatically load en_GB.lang from your Minecraft folder if Minecraft is installed on your
computer, or download it from Mojang's servers. You may choose another language in the configuration file.

Detecting chat messages
------

Minecraft Console Client can parse messages from the server in order to detect private and public messages.
This is useful for reacting to messages eg when using the AutoRespond, Hangman game, or RemoteControl bots.
However, for unusual chat formats, you may need to tinker with the ChatFormat section `MinecraftClient.ini`. This section defines the chat format by the means of regular expressions. Building regular expressions can be a bit tricky, so you might want to try them out eg on https://regex101.com - See also issue [#1640](https://github.com/MCCTeam/Minecraft-Console-Client/issues/1640) for more explanations on regular expressions. You can test that your MCC instance properly detects chat messages using [`sample-script-with-chatbot.cs`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-script-with-chatbot.cs).

About Replay Mod feature
------
Replay Mod is _A Minecraft Mod to record, relive and share your experience._ You can see more at https://www.replaymod.com/

MCC supports recording and saving your game to a file which can be used by Replay Mod. You can simply enable ReplayMod in the `.ini` setting to use this feature. The only limitation is the client player (you) will not be shown in the replay. Please note that when recording is in progress, you SHOULD exit MCC with the `/quit` command or use `/replay stop` command before closing MCC as your replay may become corrupt if you force-close MCC with CTRL+C or the [X] button.

Using the Alerts bot
------

Write in [`alerts.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/alerts.txt) the words you want the console to beep/alert you on.
Write in [`alerts-exclude.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/alerts-exclude.txt) the words you want NOT to be alerted on.
For example write `Yourname` in `alerts.txt` and `<Yourname>` in `alerts-exclude.txt` to avoid alerts when you are talking.

Using the AutoRelog bot
------

Write in [`kickmessages.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/kickmessages.txt) some words, such as `Restarting` for example.
If the kick message contains one of them, you will automatically be re-connected.

- A kick message `Connection has been lost.` is generated by the console itself when connection is lost.
- A kick message `Login failed.` is generated the same way when it failed to login to the server.
- A kick message `Failed to ping this IP.` is generated when it failed to ping the server.

You can use them for reconnecting when connection is lost or the login failed.

If you want to always reconnect, set `ignorekickmessage=true` in `MinecraftClient.ini`. Use at own risk! Server staff might not appreciate auto-relog on manual kicks, so always keep server rules in mind when configuring your client.

Using the Script Scheduler / Task Scheduler
------

The script scheduler allows you to perform scripts or internal commands on various events such as log on server, time interval, or fixed date and time. Simply enable the ScriptScheduler bot and specify a tasks file in your INI file. Please read [`sample-tasks.ini`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-tasks.ini) for learning how to make your own task file.

Using the Hangman game
------

Hangman game is one of the first bots ever written for MCC, to demonstrate ChatBot capabilities.
Create a file with words to guess (examples: [`words-en.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/hangman-en.txt), [`words-fr.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/hangman-fr.txt)) and set it in config inside the `[Hangman]` section. Also set `enabled` to `true`. Then, add your username in the `botowners` INI setting, and finally, connect to the server and use `/tell <bot username> start` to start the game. If the bot does not respond to bot owners, see the [Detecting chat messages](#detecting-chat-messages) section.

Using the Remote Control
------

When the remote control bot is enabled, you can send commands to your bot using whispers.
Don't forget to add your username in the `botowners` INI setting if you want it to obey.
If it does not respond to bot owners, read the [Detecting chat messages](#detecting-chat-messages) section.
**Please note that server admins can read what you type and output from the bot. They can also impersonate bot owners with `/nick`, so do not use Remote Control if you do not trust server admins. See [#1142](https://github.com/MCCTeam/Minecraft-Console-Client/issues/1142) for more info.**

To perform a command simply do the following: `/tell <yourbot> <thecommand>`
Where `<thecommand>` is an internal command as described in [Internal commands](#internal-commands) section.
You can remotely send chat messages or commands using `/tell <yourbot> send <thetext>`

Remote control system can by default auto-accept `/tpa` and `/tpahere` requests from the bot owners.
Auto-accept can be disabled or extended to requests from anyone in remote control configuration.

Using the AutoRespond feature
------

The AutoRespond bot allows you to automatically react on specific chat messages or server announcements.
You can use either a string to detect in chat messages, or an advanced regular expression.
For more information about how to define match rules, please refer to [`sample-matches.ini`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-matches.ini).
See [Detecting chat messages](#detecting-chat-messages) if your messages are not detected or to learn more about regular expressions.

Using the Auto Attack
------

The AutoAttack bot allows you to automatically attack mobs around you within radius of 4 block.
To use this bot, you will need to enable **Entity Handling** in the configuration file first.

Using the Auto Fishing
------

The AutoFish bot can automatically fish for you.
To use this bot, you will need to enable **Entity Handling** in the configuration file first.
If you want to get an alert message when the fishing rod was broken, enable **Inventory Handling** in the configuration file.
A fishing rod with **Mending enchantment** is strongly recommended.

Steps for using this bot:
1. Hold a fishing rod and aim towards the sea before login with MCC
2. Make sure AutoFish is enabled in config file
3. Login with MCC
4. Do `/useitem` and you should see "threw a fishing rod"
5. To stop fishing, do `/useitem` again

Using the Mailer bot
------

The Mailer bot can store and relay mails much like Essential's /mail command.

* `/tell <Bot> mail [RECIPIENT] [MESSAGE]`: Save your message for future delivery
* `/tell <Bot> tellonym [RECIPIENT] [MESSAGE]`: Same, but the recipient will receive an anonymous mail

The bot will automatically deliver the mail when the recipient is online.
The bot also offers a /mailer command from the MCC command prompt:

* `/mailer getmails`: Show all mails in the console
* `/mailer addignored [NAME]`: Prevent a specific player from sending mails
* `/mailer removeignored [NAME]`: Lift the mailer restriction for this player
* `/mailer getignored`: Show all ignored players

**CAUTION: The bot identifies players by their name (Not by UUID!).**
A nickname plugin or a minecraft rename may cause mails going to the wrong player!
Never write something to the bot you wouldn't say in the normal chat (You have been warned!)

**Mailer Network:** The Mailer bot can relay messages between servers.
To set up a network of two or more bots, launch several instances with the bot activated and the same database.
If you launch two instances from one .exe they should syncronize automatically to the same file.

Using the AutoCraft bot
------

The AutoCraft bot can automatically craft items for you as long as you have defined the item recipe.
The bot will automatically generate a default configuration file on first launch, when `enabled` is set to `true` in the `[AutoCraft]` section in config. To use this bot, you will also need to enable **Inventory Handling** in the configuration file.

Useful commands description:
* `/autocraft reload`: Reload the config from disk. You can load your edited AutoCraft config without restarting the client.
* `/autocraft resetcfg`: Reset your AutoCraft config back to default. Use with care!
* `/autocraft list`: List all loaded recipes.
* `/autocraft start <name>`: Start the crafting process with the given recipe name you had defined.
* `/autocraft stop`: Stop the crafting process.
* `/autocraft help`: In-game help command.

How to define a recipe?

_Example_
```md
[Recipe]
name=whatever          # name could be whatever you like. This field must be defined first
type=player            # crafting table type: player or table
result=StoneButton     # the resulting item

# define slots with their deserved item
slot1=stone            # slot start with 1, count from left to right, top to bottom
# For the naming of the items, please see
# https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Inventory/ItemType.cs
```

1. You need to give your recipe a **name** so that you could start them later by name.
2. The size of crafting area needed for your recipe, 2x2 (player inventory) or 3x3 (crafting table). If you need to use the crafting table, make sure to set the **table coordinate** in the `[AutoCraft]` section.
3. The expected crafting result.

Then you need to define the position of each crafting materials. 
Slots are indexed as follow:

2x2
```
╔═══╦═══╗
║ 1 ║ 2 ║
╠═══╬═══╣
║ 3 ║ 4 ║
╚═══╩═══╝
```
3x3
```
╔═══╦═══╦═══╗
║ 1 ║ 2 ║ 3 ║
╠═══╬═══╬═══╣
║ 4 ║ 5 ║ 6 ║
╠═══╬═══╬═══╣
║ 7 ║ 8 ║ 9 ║ 
╚═══╩═══╩═══╝
```
Simply use `slotIndex=MaterialName` to define material.  
 e.g. `slot1=coal` and `slot3=stick` will craft a torch.

For the naming of items, please see [ItemType.cs](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Inventory/ItemType.cs) for all the material names.

After you finished writing your config, you can use `/autocraft start <recipe name>` to start crafting. Make sure to provide materials for your bot by placing them in inventory first.

Disclaimer
------

Even if everything should work, we are not responsible for any damage this app could cause to your computer or your server.
This app does not steal your password. If you don't trust it, don't use it or check & compile from the source code.

Also, remember that when you connect to a server with this program, you will appear where you left the last time.
This means that **you can die if you log in in an unsafe place on a survival server!**
Use the script scheduler bot to send a teleport command after logging in.

We remind you that **you may get banned** by your server for using this program. Use accordingly with server rules.

License
------

Minecraft Console Client is a totally free of charge, open source project.
Source code is available at https://github.com/MCCTeam/Minecraft-Console-Client

Unless specifically stated, source code is from the MCC Team or Contributors, and available under CDDL-1.0.
More info about CDDL-1.0: http://qstuff.blogspot.fr/2007/04/why-cddl.html
Full license at http://opensource.org/licenses/CDDL-1.0

Credits
------

_Project initiated by [ORelio](https://github.com/ORelio) in 2012 on the [Minecraft Forum](http://www.minecraftforum.net/topic/1314800-/)._

Many features would not have been possible without the help of our talented community:

**Maintainers**

  ORelio
  ReinforceZwei

**Ideas**

  ambysdotnet, Awpocalypse, azoundria, bearbear12345, bSun0000, Cat7373, dagonzaros, Dids,
  Elvang, fuckofftwice, GeorgH93, initsuj, JamieSinn, joshbean39, LehmusFIN, maski, medxo,
  mobdon, MousePak, TNT-UP, TorchRJ, yayes2, Yoann166, ZizzyDizzyMC

**Bug Hunters**

  1092CQ, ambysdotnet, bearbear12345, c0dei, Cat7373, Chtholly, Darkaegis, dbear20,
  DigitalSniperz, doranchak, drXor, FantomHD, gerik43, ibspa, iTzMrpitBull, JamieSinn,
  k3ldon, KenXeiko, link3321, lyze237, mattman00000, Nicconyancat, Pokechu22, ridgewell,
  Ryan6578, Solethia, TNT-UP, TorchRJ, TRTrident, WeedIsGood, xp9kus, Yoann166

**Contributors**

  Allyoutoo, Aragas, Bancey, bearbear12345, corbanmailloux, Daenges, dbear20, dogwatch,
  initsuj, JamieSinn, justcool393, lokulin, maxpowa, medxo, milutinke, Pokechu22,
  ReinforceZwei, repository, TheMeq, TheSnoozer, vkorn, v1RuX, yunusemregul, ZizzyDizzyMC
  _... And all the [GitHub contributors](https://github.com/MCCTeam/Minecraft-Console-Client/graphs/contributors)!_

**Libraries:**

  Minecraft Console Client also borrows code from the following libraries:

| Name         | Purpose           | Author           | License |
|--------------|-------------------|------------------|---------|
| Biko         | Proxy handling    | Benton Stark     | MIT     |
| BouncyCastle | CFB-8 AES on Mono | The Legion       | MIT     |
| Heijden.Dns  | DNS SRV Lookup    | Geoffrey Huntley | MIT     |
| DotNetZip    | Zlib compression  | Dino Chiesa      | MS-PL   |

**Support:**

If you still have any question after reading this file, you can get support here:

 - GitHub Issues: https://github.com/MCCTeam/Minecraft-Console-Client/issues (You can contact the MCC Team here)
 - Minecraft Forums: http://www.minecraftforum.net/topic/1314800-/ (Thead not maintained by the MCC Team anymore)

Code contributions, bug reports and any kind of comments are also highly appreciated :)

_© 2012-2020 ORelio_
_© 2020-2021 The MCC Team_
