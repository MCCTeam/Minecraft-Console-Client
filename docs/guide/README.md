# Introduction

-   [About](#about)
-   [Quick Intro (YouTube Videos)](#quick-intro)
-   [Features](#features)
-   [Why Minecraft Console Client?](#why-minecraft-console-client)
-   [Getting Help](#getting-help)
-   [Submitting a bug report or an idea/feature-request](#bugs-ideas-feature-requests)
-   [Important notes on some features](#notes-on-some-features)
-   [Credits](#credits)
-   [Disclaimer](#disclaimer)
-   [License](#license)

## About

**Minecraft Console Client (MCC)** is a lightweight cross-platform open-source **Minecraft** TUI client for **Java edition** that allows you to connect to any Minecraft Java server, send commands and receive text messages in a fast and easy way without having to open the main Minecraft game.

It also provides various automations that you can enable for administration and other purposes, as well as extensible C# API for creating Bots.

It was originally made by [ORelio](https://github.com/ORelio) in 2012 on the [Minecraft Forum](http://www.minecraftforum.net/topic/1314800-/), now it's maintained by him and many other contributors from the community.

## Features

-   Chat

    -   Send and receive chat messages
    -   [Log chat history](chat-bots.md#chat-log)
    -   [Get alerted on certain keywords](chat-bots.md#alerts)
    -   [Auto Respond](chat-bots.md#auto-respond)

-   [Anti AFK](chat-bots.md#anti-afk)
-   [Auto Relog](chat-bots.md#auto-relog)
-   [Script Scheduler](chat-bots.md#script-scheduler)
-   [Remote Control](chat-bots.md#remote-control)
-   [Auto Respond](chat-bots.md#auto-respond)
-   [Auto Attack](chat-bots.md#auto-attack)
-   [Auto Fishing](chat-bots.md#auto-fishing)
-   [Auto Eat](chat-bots.md#auto-eat)
-   [Auto Craft](chat-bots.md#auto-craft)
-   [Mailer Bot](chat-bots.md#mailer)
-   [Auto Drop](chat-bots.md#auto-drop)
-   [Replay Mod](chat-bots.md#replay-mod)
-   [API for creating Bots in C#](creating-bots.md#creating-chat-bots)
-   [Docker Support](installation.md#using-docker)
-   [Inventory Handling](usage.md#inventory)
-   [Terrain Traversing](usage.md#move)
-   Entity Handling

_NOTE: Some of mentioned features are disabled by default and you will have to turn them on in the configuration file and some may require additional configuration on your part for your specific usage._

## Why Minecraft Console Client?

-   Easy to use
-   Helpful community
-   Open-Source
-   Fast performance
-   Cross-Platform
-   Docker Support
-   10 years of continuous development
-   Active contributors
-   Widely used

## Quick Intro

Don't have time to read through the documentation, we got you, our community has made some simple introduction videos about the **Minecraft Console Client**.

### The list of the tutorials:

Installation:

-   [Installation on Windows by Daenges](https://www.youtube.com/watch?v=BkCqOCa2uQw)
-   [Installation on Windows + Auto AFK and More by Dexter113](https://www.youtube.com/watch?v=FxJ0KFIHDrY)

Using Commands, Scripts and other features:

-   [Minecraft Console Client | Tutorial | Commands, Scripts, AppVars, Matches, Tasks and C# Scripts by Daenges](https://youtu.be/JbDpwwETEnU)
-   [Console Client Tutorial - Scripting by Zixxter](https://www.youtube.com/watch?v=XE7rYBFJxn0)

## Getting Help

MCC has a community that is willing to help, we have a Discussions section in out Git Hub repository.

Click [here](https://github.com/MCCTeam/Minecraft-Console-Client/discussions) to access it.

### Before getting help

-   **Please use the search option here or in the discussion section and read the documentation so we avoid duplicate questions. Thank you!**
-   **Please be kind and patient, respect others as they're the ones using their time to help you**

## Bugs, Ideas, Feature Requests

Bug reporting, idea submitting or feature requesting are done in the [Issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues) section of our [Github repository](<[here](https://github.com/MCCTeam/Minecraft-Console-Client)>).

Navigate to the Issues section, search for a bug, idea or a feature using the search option here in the documentation and in the `Issues` section on Git Hub before making your own.

If you haven't found anything similar, go ahead and click on the `New issue` button, then choose what you want to do.

If you're reporting a bug, please be descriptive as much as possible, try to explain how to re-create the bug, attack screenshots and logs, make sure that you have [`debugmessages`](configuration.me#debugmessages) set to `true` before sending a bug report or taking a screenshot.

### Before submitting

-   **Please use the search option here or in the `Issues` section and read the documentation so we avoid duplicate questions/ideas/reports. Thank you!**
-   **Please be kind, patient and respect others. Thank you!**

## Notes on some features

### Inventory, Terrain and Entity Handling

Inventory handling is currently not supported in versions: `1.4.6 - 1.9`

Terrain handling is currently not supported in versions: `1.4.6 - 1.6`

Entity handling is currently not supported in versions: `1.4.6 - 1.9` (but `1.8` and `1.9` are being worked on, almost at the working state, only `EntityMetadata` packet remains to be fixed)

There features might not always be implemented in the latest version of the game, since they're often subjected to major changes by Mojang, and we need some time to figure out what has changed and to implement the required changes.

If there was a major game update, and the MCC hasn't been updated to support these features, if you're a programmer, feel free to contribute to the project.

## Credits

_Project initiated by [ORelio](https://github.com/ORelio) in 2012 on the [Minecraft Forum](http://www.minecraftforum.net/topic/1314800-/)._

Many features would not have been possible without the help of our talented community:

**Maintainers**

ORelio, ReinforceZwei, milutinke, BruceChenQAQ, bradbyte

**Ideas**

ambysdotnet, Awpocalypse, azoundria, bearbear12345, bSun0000, Cat7373, dagonzaros, Dids,
Elvang, fuckofftwice, GeorgH93, initsuj, JamieSinn, joshbean39, LehmusFIN, maski, medxo,
mobdon, MousePak, TNT-UP, TorchRJ, yayes2, Yoann166, ZizzyDizzyMC and [many more](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+%5BIdea%5D+is%3Aopen).

**Bug Hunters**

1092CQ, ambysdotnet, bearbear12345, c0dei, Cat7373, Chtholly, Darkaegis, dbear20,
DigitalSniperz, doranchak, drXor, FantomHD, gerik43, ibspa, iTzMrpitBull, JamieSinn,
k3ldon, KenXeiko, link3321, lyze237, mattman00000, Nicconyancat, Pokechu22, ridgewell,
Ryan6578, Solethia, TNT-UP, TorchRJ, TRTrident, WeedIsGood, xp9kus, Yoann166 and [many more](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+%5BBUG%5D+is%3Aopen+).

**Contributors**

Allyoutoo, Aragas, Bancey, bearbear12345, corbanmailloux, Daenges, dbear20, dogwatch,
initsuj, JamieSinn, justcool393, lokulin, maxpowa, medxo, milutinke, Pokechu22,
ReinforceZwei, repository, TheMeq, TheSnoozer, vkorn, v1RuX, yunusemregul, ZizzyDizzyMC,
BruceChenQAQ, bradbyte
_... And all the [GitHub contributors](https://github.com/MCCTeam/Minecraft-Console-Client/graphs/contributors)!_

**Libraries:**

Minecraft Console Client also borrows code from the following libraries:

| Name         | Purpose           | Author           | License |
| ------------ | ----------------- | ---------------- | ------- |
| Biko         | Proxy handling    | Benton Stark     | MIT     |
| Heijden.Dns  | DNS SRV Lookup    | Geoffrey Huntley | MIT     |
| DotNetZip    | Zlib compression  | Dino Chiesa      | MS-PL   |

## Disclaimer

Even if everything should work, we are not responsible for any damage this app could cause to your computer or your server.
This app does not steal your password. If you don't trust it, don't use it or check & compile from the source code.

Also, remember that when you connect to a server with this program, you will appear where you left the last time.
This means that **you can die if you log in in an unsafe place on a survival server!**
Use the script scheduler bot to send a teleport command after logging in.

We remind you that **you may get banned** by your server for using this program. Use accordingly with server rules.

## License

Minecraft Console Client is a totally free of charge, open source project.
The source code is available at [Github Repository](https://github.com/MCCTeam/Minecraft-Console-Client)

Unless specifically stated, source code is from the MCC Team or Contributors, and available under CDDL-1.0.
More info about CDDL-1.0: [http://qstuff.blogspot.fr/2007/04/why-cddl.html](http://qstuff.blogspot.fr/2007/04/why-cddl.html)
Full license at [http://opensource.org/licenses/CDDL-1.0](http://opensource.org/licenses/CDDL-1.0)
