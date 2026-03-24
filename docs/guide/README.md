---
title: About & Features
---

# Introduction

- [Introduction](#introduction)
  - [About](#about)
  - [Features](#features)
  - [Why Minecraft Console Client?](#why-minecraft-console-client)
  - [Quick Intro](#quick-intro)
    - [The list of the tutorials:](#the-list-of-the-tutorials)
  - [Getting Help](#getting-help)
    - [Before getting help](#before-getting-help)
  - [Bugs, Ideas, Feature Requests](#bugs-ideas-feature-requests)
    - [Before submitting](#before-submitting)
  - [AI-Assisted Development](#ai-assisted-development)
  - [Notes on some features](#notes-on-some-features)
    - [Inventory, Terrain and Entity Handling](#inventory-terrain-and-entity-handling)
    - [Path-Finding and Physics](#path-finding-and-physics)
  - [Credits](#credits)
  - [Disclaimer](#disclaimer)
  - [License](#license)

## About

**Minecraft Console Client (MCC)** is a lightweight, cross-platform, open-source **Minecraft** TUI client for **Java Edition**. It lets you connect to Minecraft Java servers, send commands, and receive text messages without launching the main game.

It also includes built-in automation for administration and utility work, plus an extensible C# API for creating bots and runtime scripts.

It was originally made by [ORelio](https://github.com/ORelio) in 2012 on the [Minecraft Forum](http://www.minecraftforum.net/topic/1314800-/), now it's maintained by him and many other contributors from the community.

## Features

-   Chat

    -   Send and receive chat messages
    -   [Log chat history](chat-bots.md#chat-log)
    -   [Get alerted on certain keywords](chat-bots.md#alerts)
    -   [Auto Respond](chat-bots.md#auto-respond)

-   Microsoft account authentication with 2FA support (OAuth 2.0 device code flow)
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

_Note: Some of these features are disabled by default. You need to enable them in the configuration file, and some also require additional setup._

## Why Minecraft Console Client?

-   Easy to use
-   Helpful community
-   Open-Source
-   Fast performance
-   Easy Scripting/Automation
-   Cross-Platform
-   Docker Support
-   10 years of continuous development
-   Active contributors
-   Widely used

## Quick Intro

If you do not want to read through the documentation right away, the community has made a few short introduction videos for **Minecraft Console Client**.

### The list of the tutorials:

Installation:

-   [Installation on Windows by Daenges](https://www.youtube.com/watch?v=BkCqOCa2uQw)
-   [Installation on Windows + Auto AFK and More by Dexter113](https://www.youtube.com/watch?v=FxJ0KFIHDrY)

Using Commands, Scripts and other features:

-   [Minecraft Console Client | Tutorial | Commands, Scripts, AppVars, Matches, Tasks and C# Scripts by Daenges](https://youtu.be/JbDpwwETEnU)
-   [Console Client Tutorial - Scripting by Zixxter](https://www.youtube.com/watch?v=XE7rYBFJxn0)

## Getting Help

MCC has an active community, and the GitHub Discussions section is the best place to ask for help.

Click [here](https://github.com/MCCTeam/Minecraft-Console-Client/discussions) to access it.

### Before getting help

-   **Please use the search option here or in the discussion section and read the documentation so we avoid duplicate questions. Thank you!**
-   **Please be kind and patient, respect others as they're the ones using their time to help you**

## Bugs, Ideas, Feature Requests

Bug reports, ideas, and feature requests all go through the [Issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues) section of our [GitHub repository](https://github.com/MCCTeam/Minecraft-Console-Client).

Before opening a new issue, search both the documentation and the `Issues` section to avoid duplicates.

If you do not find anything similar, click `New issue` and choose the appropriate template.

If you are reporting a bug, be as specific as possible. Explain how to reproduce it, attach screenshots and logs, and make sure debug logging is enabled before collecting them.

### Before submitting

-   **Please use the search option here or in the `Issues` section and read the documentation so we avoid duplicate questions/ideas/reports. Thank you!**
-   **Please be kind, patient and respect others. Thank you!**

## AI-Assisted Development

If you want the repeatable agent workflow used by maintainers, start with [AI-Assisted Development](ai-assisted-development.md).

## Notes on some features

### Inventory, Terrain and Entity Handling

MCC currently supports Minecraft versions `1.4.6` through `26.1`.

Feature support still depends on protocol version:

- Inventory handling is supported on `1.8+`.
- Terrain handling is supported on `1.7.2+`.
- Entity handling is supported on `1.8+`.

These features may lag behind brand-new Minecraft releases when Mojang changes the protocol or registries in a major way.

If there was a major game update, and the MCC hasn't been updated to support these features, if you're a programmer, feel free to contribute to the project.

### Path-Finding and Physics

MCC now uses A* path-finding together with a physics-based movement system for movement and collision handling.

What is supported and works:
- Terrain navigation with A* path-finding and physics-driven movement
- Collision-aware movement using real block shapes
- Automatic jumping when the path requires moving up
- Step-up movement for slabs and similar low obstacles
- Sneaking and sprinting
- Movement physics in water and lava
- Climbing up and down ladders and all types of vines
- Gravity, friction, and block speed modifiers such as ice, soul sand, soul soil, and honey blocks

Current limitations:
- Path-finding is still block-based, so very complex terrain can still fail
- Automatic route planning still avoids underwater routes by default, so this is not a full swimming path-finder yet
- Knockback and other external velocity effects are not simulated yet

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

1092CQ, ambysdotnet, bearbear12345, c0dei, Cat7373, Chtholly, Darkaegis, dbear20, DigitalSniperz, doranchak, drXor, FantomHD, gerik43, ibspa, iTzMrpitBull, JamieSinn, k3ldon, KenXeiko, link3321, lyze237, mattman00000, Nicconyancat, Pokechu22, ridgewell, Ryan6578, Solethia, TNT-UP, TorchRJ, TRTrident, WeedIsGood, xp9kus, Yoann166 and [many more](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+%5BBUG%5D+is%3Aopen+).

**Contributors**

Allyoutoo, Aragas, Bancey, bearbear12345, corbanmailloux, Daenges, dbear20, dogwatch, initsuj, JamieSinn, justcool393, lokulin, maxpowa, medxo, milutinke, Pokechu22, ReinforceZwei, repository, TheMeq, TheSnoozer, vkorn, v1RuX, yunusemregul, ZizzyDizzyMC, BruceChenQAQ, bradbyte _... And all the [GitHub contributors](https://github.com/MCCTeam/Minecraft-Console-Client/graphs/contributors)!_

**Libraries:**

Minecraft Console Client also borrows code from the following libraries:

| Name        | Purpose          | Author           | License |
| ----------- | ---------------- | ---------------- | ------- |
| Biko        | Proxy handling   | Benton Stark     | MIT     |
| Heijden.Dns | DNS SRV Lookup   | Geoffrey Huntley | MIT     |
| DotNetZip   | Zlib compression | Dino Chiesa      | MS-PL   |

## Disclaimer

Even if everything should work, we are not responsible for any damage this app could cause to your computer or your server.

This app does not steal your password. If you don't trust it, don't use it or check & compile from the source code.

Also, remember that when you connect to a server with this program, you will appear where you left the last time.

This means that **you can die if you log in in an unsafe place on a survival server!**

Use the script scheduler bot to send a teleport command after logging in.

We remind you that **you may get banned** by your server for using this program. Use accordingly with server rules.

## License

Minecraft Console Client is a totally free of charge, open source project.

The source code is available at the [GitHub repository](https://github.com/MCCTeam/Minecraft-Console-Client)

Unless specifically stated, source code is from the MCC Team or Contributors, and available under CDDL-1.0.

More info about CDDL-1.0: [http://qstuff.blogspot.fr/2007/04/why-cddl.html](http://qstuff.blogspot.fr/2007/04/why-cddl.html)

Full license at [http://opensource.org/licenses/CDDL-1.0](http://opensource.org/licenses/CDDL-1.0)
