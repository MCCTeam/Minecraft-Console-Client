---
title: Creating Chat Bots
---

# Creating Chat Bots

- [Notes](#notes)
- [Requirements](#requirements)
- [Quick Introduction](#quick-introduction)
- [Examples](#examples)
- [AI-Assisted Bot Authoring](#ai-assisted-bot-authoring)
- [Achievements And Advancements](#achievements-and-advancements)
- [C# API](#c#-api)

## Notes

<div class="custom-container note"><p class="custom-container-title">Note</p>

**This page covers the basics of the Chat Bot API. For the full surface area, read [ChatBot.cs](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Scripting/ChatBot.cs) and the example scripts linked below.**

</div>

**Minecraft Console Client** has a rich C# API which allows you to create Chat Bots (effectively plugins) which can help you create complex automations which normal scripts may not be able to do.

## Requirements

- A basic knowledge of C# programming language
- A text editor

If you're not familiar with the C# programming language, we suggest taking a look at the following resources:

Crash courses:

- [C# Crash Course playlist by Teddy Smit](https://www.youtube.com/watch?v=67oWw9TanOk&list=PL82C6-O4XrHfoN_Y4MwGvJz5BntiL0z0D)

More in-depth:

- [Learn C# YouTube Playlist by Microsoft](https://www.youtube.com/playlist?list=PLdo4fOcmZ0oVxKLQCHpiUWun7vlJJvUiN)
- [Getting started with C# (an index of tutorials and documentation) by Microsoft](https://learn.microsoft.com/en-us/dotnet/csharp/)

## Quick Introduction

This introduction assumes that you have the basic knowledge of C#.

<div class="custom-container note"><p class="custom-container-title">Note</p>

**In this page, "Chat Bot" and "Script" are used interchangeably.**

</div>

Create a new empty file and name it `ExampleChatBot.cs` in the same folder where you have your MCC installed.

Paste the following example code:

```csharp
//MCCScript 1.0

MCC.LoadBot(new ExampleChatBot());

//MCCScript Extensions

// The code and comments above are defining a "Script Metadata" section

// Every chat bot script must define a class that extends ChatBot.
// Instantiate that class in the script metadata section and pass it to MCC.LoadBot.
class ExampleChatBot : ChatBot
{
    // This method will be called when the script has been initialized for the first time, it's called only once
    // Here you can initialize variables, eg. Dictionaries. etc...
	public override void Initialize()
	{
		LogToConsole("An example Chat Bot has been initialized!");
	}

    // This is a function that will be run when we get a chat message from a server
    // In this example it just detects the type of the message and prints it out
	public override void GetText(string text)
	{
		string message = "";
		string username = "";
		text = GetVerbatim(text);

		if (IsPrivateMessage(text, ref message, ref username))
		{
			LogToConsole(username + " has sent you a private message: " + message);
		}
		else if (IsChatMessage(text, ref message, ref username))
		{
			LogToConsole(username + " has said: " + message);
		}
	}
}
```

Start MCC, connect to a server and run the following internal command: `/script ExampleChatBot.cs`.

If everything worked, you should see `[Example Chat Bot] An example Chat Bot has been initialized!` in the console.

### Structure of Chat Bots

Chat Bot (Script) structure is the following:

```
<script metadata>
<chat bot class>
```

**Script Metadata** is a section with a custom format that mixes in C# with our format using comments.

Every single Chat Bot (Script) must have this section at the beginning in order to work.

### Script Metadata Format

`//MCCScript 1.0` marks the beginning of the **Script Metadata** section, this must always be on the first line or the Chat Bot (Script) will not load and will throw an error.

`//MCCScript Extensions` marks the end of the **Script Metadata** section. It must appear before the Chat Bot class.

To load a Chat Bot script, instantiate the bot class between `//MCCScript 1.0` and `//MCCScript Extensions`, then pass it to `MCC.LoadBot`.

Example code:

```
MCC.LoadBot(new YourChatBotClassNameHere());
```

The **Script Metadata** section also lets you include namespaces and DLL references with `//using <namespace>` and `//dll <dll name>`.

<div class="custom-container note"><p class="custom-container-title">Note</p>

**Avoid adding whitespace between `//` and keywords**

</div>

By default, the following namespaces are loaded:

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using MinecraftClient;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;
```

Example:

```csharp
//using System.Collections.Immutable
//dll MyDll.dll
```

Full Example:

```csharp
//MCCScript 1.0

//using System.Collections.Immutable
//dll MyDll.dll

MCC.LoadBot(new ExampleChatBot());

//MCCScript Extensions
```

### Chat Bot Class

After the **Script Metadata** section, you can define any number of helper classes. The main bot class must extend `ChatBot`.

There are no required methods, everything is optional.

When the Chat Bot is initialized for the first time, the `Initialize` method is called.

Use it to initialize state such as dictionaries or cached values.

<div class="custom-container note"><p class="custom-container-title">Note</p>

**For allocating resources like a database connection, we recommend allocating them in `AfterGameJoined` and freeing them in `OnDisconnect`**

</div>

## Examples

You can find more examples in the [ChatBots](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/ChatBots) and [config](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config) folders in the GitHub repository.

## AI-Assisted Bot Authoring

If you are using an AI coding agent on this repository, use the `mcc-chatbot-authoring` skill for bot work.

Skill links:

- [Browse the skill on GitHub](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/.skills/mcc-chatbot-authoring)
- [Download the skill directory](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FMCCTeam%2FMinecraft-Console-Client%2Ftree%2Fmaster%2F.skills%2Fmcc-chatbot-authoring)

This skill is meant for:

- standalone `/script` bots
- built-in MCC chat bots
- bot repairs and ports
- event handlers, movement logic, inventory logic, and plugin-channel work

Its default behavior is important: if you ask for "a bot" without saying otherwise, it should prefer a standalone `//MCCScript` bot loaded with `/script`. It should only choose a built-in bot when you explicitly ask for repo wiring, automatic config loading, or a compiled MCC bot.

The skill also follows MCC-specific rules, for example:

- do not send chat from `Initialize()`
- use `AfterGameJoined()` for chat or commands after login
- normalize chat with `GetVerbatim(text)` before `IsChatMessage(...)` or `IsPrivateMessage(...)`
- fully clean up commands, timers, plugin channels, and movement locks

### Example prompts

```text
Create a standalone MCC /script bot that watches public chat for the word "auction" and logs matching messages to the console. Use the mcc-chatbot-authoring skill.
```

```text
Fix this existing MCC script bot so it stops sending chat from Initialize() and moves the startup command to AfterGameJoined(). Use the mcc-chatbot-authoring skill.
```

```text
Make a built-in MCC chat bot named AutoTorch and wire it fully into the repo config and bot registration. Use the mcc-chatbot-authoring skill.
```

```text
Create a standalone MCC /script bot that follows private messages, uses GetVerbatim(text), and replies only to bot owners. Use the mcc-chatbot-authoring skill.
```

## Achievements And Advancements

Chat bots and C# scripts can read the current achievement state and react to updates.

Methods:

- `GetAchievements()`
- `GetUnlockedAchievements()`
- `GetLockedAchievements()`
- `OnAchievementUpdate(IReadOnlyList<Achievement> updated, IReadOnlyList<string> removedIds, bool reset)`

The `Achievement` record exposes:

- `Id` (`string`) - resource identifier, e.g. `minecraft:story/root` or `achievement.openInventory`
- `Title` (`string?`) - display name, or `null` for legacy achievements
- `Description` (`string?`) - display description, or `null` for legacy achievements
- `Type` (`AchievementType`) - `Task`, `Challenge`, `Goal`, or `Legacy`
- `IsCompleted` (`bool`) - whether all requirements have been met
- `IsHidden` (`bool`) - whether the advancement is hidden in the UI until unlocked
- `Requirements` (`IReadOnlyList<IReadOnlyList<string>>`) - OR-groups of criteria that must all be satisfied
- `CriteriaProgress` (`IReadOnlyDictionary<string, bool>`) - per-criterion completion status

Notes:

- On `1.8` to `1.11.2`, ids use the legacy `achievement.*` format.
- On `1.12+`, ids use advancement resource ids such as `minecraft:story/root`.
- Legacy achievements have `Title = null` and `Description = null` because the server does not send display metadata in the statistics packet.
- On newer versions, revoking an advancement may remove it from the current set instead of turning it into a locked entry, so `removedIds` matters.

Example:

```csharp
//MCCScript 1.0

MCC.LoadBot(new AchievementWatcher());

//MCCScript Extensions

public class AchievementWatcher : ChatBot
{
    public override void AfterGameJoined()
    {
        Achievement[] known = GetAchievements();
        LogToConsole($"Known achievements: {known.Length}");
    }

    public override void OnAchievementUpdate(IReadOnlyList<Achievement> updated, IReadOnlyList<string> removedIds, bool reset)
    {
        LogToConsole($"Achievement update: reset={reset}, updated={updated.Count}, removed={removedIds.Count}");

        foreach (Achievement achievement in updated)
        {
            string title = achievement.Title ?? achievement.Id;
            string state = achievement.IsCompleted ? "done" : "todo";
            LogToConsole($" - {title}: {state}");
        }

        foreach (string removedId in removedIds)
            LogToConsole($" - removed: {removedId}");
    }
}
```

## C# API

The authoritative reference for the C# API is [ChatBot.cs](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Scripting/ChatBot.cs).

Each method is well documented with standard C# documentation comments.

This page intentionally stays focused on the basics. For newer hooks and overloads, check the source file directly.
