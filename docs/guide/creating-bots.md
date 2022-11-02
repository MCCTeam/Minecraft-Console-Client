# Creating Chat Bots

-   [Notes](#notes)
-   [Requirements](#requirements)
-   [Quick Introduction](#quick-introduction)
-   [Examples](#examples)
-   [C# API](#c#-api)

## Notes

> **ℹ️ NOTE: For now this page contains only the bare basics of the Chat Bot API, enough of details to teach you how to make basic Chat Bots. For more details you need to take a look at the [ChatBot.cs](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Scripting/ChatBot.cs) and [Examples](#examples). This page will be improved in the future.**

**Minecraft Console Client** has a rich C# API which allows you to create Chat Bots (effectively plugins) which can help you create complex automations which normal scripts may not be able to do.

## Requirements

-   A basic knowledge of C# programming language
-   A text editor

If you're not familiar with the C# programming language, we suggest taking a look at the following resources:

Crash courses:

-   [C# Crash Course playlist by Teddy Smit](https://www.youtube.com/watch?v=67oWw9TanOk&list=PL82C6-O4XrHfoN_Y4MwGvJz5BntiL0z0D)

More in-depth:

-   [Learn C# Youtube Playlist by Microsoft](https://www.youtube.com/playlist?list=PLdo4fOcmZ0oVxKLQCHpiUWun7vlJJvUiN)
-   [Getting started with C# (An index of tutorials and the documentation) by Microsoft](https://docs.microsoft.com/en-us/dotnet/csharp/)

## Quick Introduction

This introduction assumes that you have the basic knowledge of C#.

> **ℹ️ NOTE: Here we will use terms Chat Bot and Script interchangeably**

Create a new empty file and name it `ExampleChatBot.cs` in the same folder where you have your MCC installed.

Paste the following example code:

```csharp
//MCCScript 1.0

MCC.LoadBot(new ExampleChatBot());

//MCCScript Extensions

// The code and comments above are defining a "Script Metadata" section

// Every single chat bot (script) must be a class which extends the ChatBot class.
// Your class must be instantiates in the "Script Metadata" section and passed to MCC.LoadBot function.
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

If you did everything right you should see: `[Example Chat Bot] An example Chat Bot has been initialised!` message appear in your console log.

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

`//MCCScript Extensions` marks the end of the **Script Metadata** section, this must be defined before a Chat Bot (Script) class.

In order for your Chat Bot (Script) to properly load in-between the `//MCCScript 1.0` and the `//MCCScript Extensions` lines you must instantiate your Chat Bot (Script) class and pass it to the `MCC.LoadBot` function.

Example code:

```
MCC.LoadBot(new YourChatBotClassNameHere());
```

**Script Metadata** section allows for including C# packages and libraries with: `//using <namespace>` and `/dll <dll name>`.

> **ℹ️ NOTE: Avoid adding whitespace between `//` and keywords**

By the default the following packages are loaded:

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

After the end of the **Script Metadata** section, you basically can define any number of classes you like, the only limitation is that the main class of your Chat Bot (Script) must extend `ChatBot` class.

There are no required methods, everything is optional.

When the Chat Bot (Script) has been initialized for the first time the `Initialize` method will be called.
In it you can initialize variables, eg. Dictionaries, etc..

> **ℹ️ NOTE: For allocating resources like a database connection, we recommend allocating them in `AfterGameJoined` and freeing them in `OnDisconnect`**.

## Examples

You can find a lot of examples in our Git Hub Repository at [ChatBots](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/ChatBots) and [config](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config).

## C# API

As of the time of writing, the C# API has been changed in forks that are yet to be merged, so for now you can use the [ChatBot.cs](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Scripting/ChatBot.cs) for reference.

Each method is well documented with standard C# documentation comments.

In the future we will make a script to auto-generate this section based on the documentation in the code.
