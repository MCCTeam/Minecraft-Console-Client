# Web Socket Chat Bot documentation

This is a documentation page on the Web Socket chat bot and on how to make a library that uses web socket to execute commands in the MCC and processes events sent by the MCC.

Please read the [Important things](#important-things) before everything.

# Page index

- [Important things](#important-things)
  - [Prerequisites](#prerequisites)
  - [Limitations](#limitations)
  - [Precision of information](#precisionvalidity-of-the-information-in-this-guide)
- [How does it work?](#how-does-it-work)
- [Sending commands](#sending-commands-to-mcc)
- [Websocket Commands](#web-socket-commands)
- [Websocket Events](#mcc-events)
- [Reference Implementation](#reference-implementation)

## Reference implementation

I have made a reference implementation in TypeScript/JavaScript, it is avaliable here: 

[https://github.com/milutinke/MCC.js](https://github.com/milutinke/MCC.js)

It is great for better understanding how this works.

## Important things

### Prerequisites 

This guide/documentation assumes that you have enough of programming knowledge to know:

  - What Web Socket is
  - Basics of networking and concurency
  - What JSON is
  - What are the various data types such as boolean, integer, long, float, double, object, dictionary/hash map

Without knowing those, I highly recommend learning about those concepts before trying to implement your own library.

### Limitations

The Web Socket chat bot should be considered experimental and prone to change, it has not been fully tested and might change, keep an eye on updates on our official Discord server.

### Precision/Validity of the information in this guide

This guide has been mostly generated from the code itself, so the types are C# types, except in few cases where I have manually changed them. 

For some thing you will have to dig in to the MCC C# code of the Chat Bot and various helper classes.

**Some information sent by the MCC, for example entity metadata, block ids, item ids, or various other data is different for each Minecraft Version, thus you need to map it for each minecraft version.**

Some events might not be that useful, eg. `OnNetworkPacket`

## How does it work?

So, basically, this Web Socket Chat Bot is a chat bot that has a Web Socket server running while you're connected to a minecraft server.

It sends events, and listens for commands and responds to commands.

It has build in authentication, which requires you to send a command to authenticate if the the password is set, if it is not set, it should automatically authenticate you on the first command.

You also can name every connection (session) with an alias.

The flow of the protocol is the following:

```
Connect to the chat bot via web socket

            |
            |
           \ /
            `

Optionally set a session alias/name with "ChangeSessionId" command 
(this can be done multiple times at any point)

            |
            |
           \ /
            `

Send an "Authenticate" command if there is a password set 

            |
            |
           \ /
            `

Send commands and listen for events
```

In order to implement a library that communicates witht this chat bot, you need to make a way to send commands, remember the sent commands via the `requestId` value, and listen for `OnWsCommandResponse` event in which you need to detect if your command has been executed by looking for the `requestId` that matches the one you've sent. I also recommend you put a 5-10 seconds command execution timeout, where you discard the command if it has not been executed in the given timeout range.

## Sending commands to MCC

You can send text in the chat, execute client commands or execute remote procedures (WebSocket Chat Bot commands).

Each thing that is sent to the chat bot results in a response through the [`OnWsCommandResponse`](#onwscommandresponse) event.

### Sending chat messages

To send a chat message just send a plain text with your message to via the web socket.

### Executing client commands

To execute a client command, just send plain text with your command.

Example: `/move suth`

### Execution remote procedures (WebSocket Chat Bot commands)

In order to execute a remote procedure, you need to send a json encoded string in the following format:

```json
{
  "command": "<command name here>",
  "requestId": "<randomly generated string for identification>",
  "parameters": [ <parameter 1>, <parameter 2>, ... ]
}
```

#### `command` 

  Refers to the name of the command

#### `requestId`

  Is a unique indentifier you generate on each command, it will be returned in the response of the command execution ([`OnWsCommandResponse`](#onwscommandresponse)), use it to track if a command has been successfully executed or not, and to get the return value if it has been successfully executed. (*It's recommended to generate at least 7 characters to avoid collision, best to use an UUID format*).

#### `parameters`
  
  Are parameters (attibutes) of the procedure you're executing, they're sent as an array of data of various types, the Web Socket chat bot does parsing and conversion and returns an error if you have sent a wrong type for the given parameters, of if you haven't send enough of them.

  **Example:**

  ```json
  {
    "command": "Authenticate",
    "requestId": "8w9u60-q39ik",
    "parameters": ["wspass12345"]
  }
  ```

# Web Socket Commands

## Important

**I'll try to include a full list of commands here with full examples, but you will have to take a look at the source code from time to time to see the types you can send in more details.**

**The source code of the WebSocket Chat Bot:** [Click here](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/ChatBots/WebSocketBot.cs#L484)

## Protocol Commands

Protocol commands are commands to manipulate the protocol.

### `Authenticate`

  This command is used to authenticate if there is a password set in the Web Socket chat bot settings.

  **Parameters:**

  It takes a single parameters of a string type that contains a password.

  **Example:**

  ```json
  {
    "command": "Authenticate",
    "requestId": "a08rt980u15j890",
    "parameters": ["wspass12345"]
  }
  ```

### `ChangeSessionId`

  This command is used to change the name/alias/id of a session.

  **Parameters:**

  It takes a single parameters of a string type that contains a name.

  **Example:**

  ```json
  {
    "command": "ChangeSessionId",
    "requestId": "9845eybjb8936j0i3",
    "parameters": ["My Custom Session Name"]
  }
  ```

## Procedures

Procedures are the methods/functions you can execute on the MCC itself to interact with the minecraft server.

### - `LogToConsole`

**Description:**

Log stuff in to the MCC console.

**Parameters:**

- `message`

  **Type:** `string`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "LogToConsole",
    "requestId": "9qaeuitgng",
    "parameters": ["Some text to log..."]
}
```

### - `LogDebugToConsole`

**Description:**

Log stuff in to the MCC debug console channel.

**Parameters:**

- `message`

  **Type:** `string`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "LogDebugToConsole",
    "requestId": "yt30j83g-uq",
    "parameters": ["Some text to log..."]
}
```

### - `LogToConsoleTranslated`

**Description:**

Log a translated string in to the MCC console.

**Parameters:**

- `message`

  **Type:** `string`

**Return type:** `boolean`

```json
{
    "command": "LogToConsoleTranslated",
    "requestId": "qt089t1jh1t1t",
    "parameters": ["ChatBot.WebSocketBot.DebugMode"]
}
```

### - `LogDebugToConsoleTranslated`

**Description:**

Log a translated string in to the MCC debug console channel.

**Parameters:**

- `message`

  **Type:** `string`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "LogDebugToConsoleTranslated",
    "requestId": "gpiqahjgpag",
    "parameters": ["ChatBot.WebSocketBot.DebugMode"]
}
```

### - `ReconnectToTheServer`

**Description:**

Reconnect to the server the MCC is connected to.

**Parameters:**

- `extraAttempts`

  **Type:** `integer`

  **Note:** Use -1 for unlimited attempts number.

- `delaySeconds`

  **Type:** `integer`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "ReconnectToTheServer",
    "requestId": "098uqh3r2w0qt9",
    "parameters": [60, 360]
}
```

### - `DisconnectAndExit`

**Description:**

Disconnect MCC from the server and close the program.

**Parameters:**

- No parameters

**Example:**

```json
{
    "command": "DisconnectAndExit",
    "requestId": "89seut02349wjk",
    "parameters": []
}
```

### - `RunScript`

**Description:**

Run a MCC C# script.

**Parameters:**

- `scriptName`

  **Type:** `string`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "RunScript",
    "requestId": "q3r098qhtqj-0",
    "parameters": ["testScript.cs"]
}
```

### - `GetTerrainEnabled`

**Description:**

Check if the Terrain Handling is enabled.

**Parameters:**

- No parameters

**Return type:** `boolean`

**Example:**

```json
{
    "command": "GetTerrainEnabled",
    "requestId": "089wqejru",
    "parameters": []
}
```

### - `SetTerrainEnabled`

**Description:**

Try enabling the Terrain Handling.

**Parameters:**

- `enabled`

  **Type:** `boolean`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "SetTerrainEnabled",
    "requestId": "9uW4HT9A",
    "parameters": [true]
}
```

### - `GetEntityHandlingEnabled`

**Description:**

Check if the Entity Handling is enabled.

**Parameters:**

- No parameters

**Return type:** `boolean`

**Example:**

```json
{
    "command": "GetEntityHandlingEnabled",
    "requestId": "ua5yht9-a8u",
    "parameters": []
}
```

### - `Sneak`

**Description:**

Toggle sneak.

**Parameters:**

- `toggle`

  **Type:** `boolean`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "Sneak",
    "requestId": "iurwt8h97",
    "parameters": [true]
}
```

### - `SendEntityAction`

**Description:**

Send an entity action.

**Parameters:**

- `actionType`

  **Type:** [`EntityActionType` as a an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Protocol/EntityActionType.cs#L3)

**Return type:** `boolean`

**Example:**

```json
{
    "command": "SendEntityAction",
    "requestId": "0j5t3yb89j-q5b9j8",
    "parameters": [1]
}
```

### - `DigBlock`

**Description:**

Dig a block in the world.

**Parameters:**

- `X`

  **Type:** `double`

- `Y`

  **Type:** `double`

- `Z`

  **Type:** `double`

- `swingArms` (optional, default `true`)

  **Type:** `boolean`

- `lookAtBlock` (optional, default `true`)

  **Type:** `boolean`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "DigBlock",
    "requestId": "89q58u9qb",
    "parameters": [12.5, 72, 12.5, true, true] 
}
```

### - `SetSlot`

**Description:**

Set the current active hot bar slot.

**Parameters:**

- `slotId`

  **Type:** `integer`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "SetSlot",
    "requestId": "9hu43tv9hu4tv",
    "parameters": [1]
}
```

### - `GetWorld`

**Description:**

Get world info.

**Parameters:**

- No parameters

**Return type:** [`json encoded object with world info`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Mapping/World.cs)

**Example:**

```json
{
    "command": "GetWorld",
    "requestId": "89753q6bh756b",
    "parameters": []
}
```

### - `GetEntities`

**Description:**

Get a list of entities around the player.

**Parameters:**

- No parameters

**Return type:** [`json encoded array of Entity`](https://github.com/milutinke/MCC.js/blob/dc5ccfecb65284f021c94c8381c3d7fb4f36a2c3/src/MccTypes/Entity.ts#L130)

**Example:**

```json
{
    "command": "GetEntities",
    "requestId": "9ujrte9ujp",
    "parameters": []
}
```

### - `GetPlayersLatency`

**Description:**

Get a list of players and their latencies.

**Parameters:**

- No parameters

**Return type:** `json encoded array of player object with { "<nick>": <latency> }`

**Example:**

```json
{
    "command": "GetPlayersLatency",
    "requestId": "9uj53ybwj8945sby6",
    "parameters": []
}
```

### - `GetCurrentLocation`

**Description:**

Get the current bot location in the world.

**Parameters:**

- No parameters

**Return type:** [`json encoded Location object`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Mapping/Location.cs)

**Example:**

```json
{
    "command": "GetCurrentLocation",
    "requestId": "8953ybu896b539j8056b3",
    "parameters": []
}
```

### - `MoveToLocation`

**Description:**

Move to a location in the world.

**Parameters:**

- `X`

  **Type:** `double`

- `Y`

  **Type:** `double`

- `Z`

  **Type:** `double`

- `allowUnsafe` (optional, default: `true`)

  **Type:** `boolean`

  **Description:** Allow the bot to go through unsafe areas, warning: it might get hurt.

- `allowDirectTeleport` (optional, default: `false`)

  **Type:** `boolean`

  **Description:** Allow bot to send a teleport packet.

- `maxOffset` (optional, default: `0`)

  **Type:** `integer`

  **Description:** Maximum number of blocks from the location where the bot can stop.

- `minOfset` (optional, default: `0`)

  **Type:** `integer`

  **Description:** Minimum number of blocks from the location where the bot can stop.

**Return type:** `boolean`

**Example:**

```json
{
    "command": "MoveToLocation",
    "requestId": "853yb8u,6b589uj",
    "parameters": [12.5, 71, 142.5]
}
```

### - `ClientIsMoving`

**Description:**

Check if the bot is currently moving.

**Parameters:**

- No parameters

**Return type:** `boolean`

**Example:**

```json
{
    "command": "ClientIsMoving",
    "requestId": "539ayg88a9u63",
    "parameters": []
}
```

### - `LookAtLocation`

**Description:**

Make the bot look at a specific location.

**Parameters:**

- `X`

  **Type:** `double`

- `Y`

  **Type:** `double`

- `Z`

  **Type:** `double`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "LookAtLocation",
    "requestId": "a45g90unhu9a5t",
    "parameters": [12, 71, 134]
}
```

### - `GetTimestamp`

**Description:**

Get current time in `yyyy-MM-dd HH:mm:ss` format.

**Parameters:**

- No parameters

**Return type:** `string`

**Example:**

```json
{
    "command": "GetTimestamp",
    "requestId": "87htgqq76y8g",
    "parameters": []
}
```

### - `GetServerPort`

**Description:**

Get the current server port.

**Parameters:**

- No parameters

**Return type:** `int`

**Example:**

```json
{
    "command": "GetServerPort",
    "requestId": "89u53ybq89uqb",
    "parameters": []
}
```

### - `GetServerHost`

**Description:**

Get the current server IPv4 address.

**Parameters:**

- No parameters

**Return type:** `string`

**Example:**

```json
{
    "command": "GetServerHost",
    "requestId": "hu3ay5u9h35",
    "parameters": []
}
```

### - `GetUsername`

**Description:**

Get current logged in account username.

**Parameters:**

- No parameters

**Return type:** `string`

**Example:**

```json
{
    "command": "GetUsername",
    "requestId": "8t7fhq87q6yw",
    "parameters": []
}
```

### - `GetGamemode`

**Description:**

Get the current game mode in which the bot is.

**Parameters:**

- No parameters

**Return type:** `string`

**Example:**

```json
{
    "command": "GetGamemode",
    "requestId": "5ta309h7835ty89j70",
    "parameters": []
}
```

### - `GetYaw`

**Description:**

Get current bot yaw.

**Parameters:**

- No parameters

**Return type:** `double`

**Example:**

```json
{
    "command": "GetYaw",
    "requestId": "B9Q5G380UJQ",
    "parameters": []
}
```

### - `GetPitch`

**Description:**

Get the current bot pitch.

**Parameters:**

- No parameters

- **Return type:** `double`

**Example:**

```json
{
    "command": "GetPitch",
    "requestId": "7hm4rtv2q5Y74",
    "parameters": []
}
```

### - `GetUserUUID`

**Description:**

Get the UUID of the current account.

**Parameters:**

- No parameters

**Return type:** `string`

**Example:**

```json
{
    "command": "GetUserUUID",
    "requestId": "34tva89hq986h",
    "parameters": []
}
```

### - `GetOnlinePlayers`

**Description:**

Get a list of online players on the server.

**Parameters:**

- No parameters

**Return type:** `json encoded array of string`

**Example:**

```json
{
    "command": "GetOnlinePlayers",
    "requestId": "894tvu2u8qv6",
    "parameters": []
}
```

### - `GetOnlinePlayersWithUUID`

**Description:**

Get a list of online players on the server with their nicknames and UUIDs.

**Parameters:**

- No parameters

**Return type:** `json encoded array of object in the following format: { "<uuid string>": "<name string>" }`

**Example:**

```json
{
    "command": "GetOnlinePlayersWithUUID",
    "requestId": "903fy5tv8qwu89",
    "parameters": []
}
```

### - `GetServerTPS`

**Description:**

Get the current server TPS.

**Parameters:**

- No parameters

**Return type:** `integer`

**Example:**

```json
{
    "command": "GetServerTPS",
    "requestId": "70atv4fy7890",
    "parameters": []
}
```

### - `InteractEntity`

**Description:**

Interact with an entity.

**Parameters:**

- `entityId`

  **Type:** `integer`

- `interactionType`

  **Type:** [`InteractType` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Mapping/InteractType.cs)

- `hand` (optional)

  **Type:** [`Hand` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Inventory/Hand.cs)

  **Default value:** `0` (Main Hand)

  You can omit this parameter if you want to interact with the main hand.

**Return type:** `boolean`

**Example:**

```json
{
    "command": "InteractEntity",
    "requestId": "a34890u hgtv90h",
    "parameters": [1452, 1]
}
```

### - `CreativeGive`

**Description:**

Give an item from the Creative Inventory.

**Parameters:**

- `slot`

  **Type:** `integer`

  **Description:** The slot id in which the items will be added to.

- `itemType`

  **Type:** [`ItemType` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Inventory/ItemType.cs)

- `count`

  **Type:** `integer`

  **Description** The number of items you want to give.

- `nbt` (optional)

  **Type:** `string with json of nbt object`

  **Description** The item NBT data

**Return type:** `boolean`

**Example:**

```json
{
    "command": "CreativeGive",
    "requestId": "sedoiuneag87",
    "parameters": [12, 1, 64]
}
```

### - `CreativeDelete`

**Description:**

Clear an inventory slot of items in the Creative Mode.

**Parameters:**

- `slot`

  **Type:** `integer`

  **Description:** The slot id from which the items will be deleted from.

**Return type:** `boolean`

**Example:**

```json
{
    "command": "CreativeDelete",
    "requestId": "09hfgq9qui0gq",
    "parameters": [12]
}
```

### - `SendAnimation`

**Description:**

Send an animation, for example a hand swing.

**Parameters:**

- `hand`

  **Type:** [`Hand` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Inventory/Hand.cs)

  **Default value:** `0` (Main Hand)

  You can omit this parameter if you want to interact with the main hand.

**Return type:** `boolean`


**Example:**

```json
{
    "command": "SendAnimation",
    "requestId": "0ig09ug0iwq",
    "parameters": []
}
```

### - `SendPlaceBlock`

**Description:**

Place a block somewhere in the world.

**Parameters:**

- `X`

  **Type:** `double`

- `Y`

  **Type:** `double`

- `Z`

  **Type:** `double`

- `direction`

  **Type:** [`Direction` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Mapping/Direction.cs)

- `hand` (optional)

  **Type:** [`Hand` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Inventory/Hand.cs)

  **Default value:** `0` (Main Hand)

**Return type:** `boolean`

**Example:**

```json
{
    "command": "SendPlaceBlock",
    "requestId": "zibgweybuini9o",
    "parameters": [12, 72, 134, 4]
}
```

### - `UseItemInHand`

**Description:**

Use an item in the hand.

**Parameters:**

- No parameters

**Return type:** `boolean`

**Example:**

```json
{
    "command": "UseItemInHand",
    "requestId": "qat0qtg90gqtn",
    "parameters": []
}
```

### - `GetInventoryEnabled`

**Description:**

Check if the inventory is enabled.

**Parameters:**

- No parameters

**Return type:** `boolean`

**Example:**

```json
{
    "command": "GetInventoryEnabled",
    "requestId": "2t4q0j9qwg8h",
    "parameters": []
}
```

### - `GetPlayerInventory`

**Description:**

Get the items in the player inventory.

**Parameters:**

- No parameters

**Return type:** [`json encoded inventory/container object`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Inventory/Container.cs)

**Example:**

```json
{
    "command": "GetPlayerInventory",
    "requestId": "gbugabuiga",
    "parameters": []
}
```

### - `GetInventories`

**Description:**

Get opened inventories list and items in them.

**Parameters:**

- No parameters

**Return type:** [`json encoded array of inventory/container objects`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Inventory/Container.cs)

**Example:**

```json
{
    "command": "GetPlayerInventory",
    "requestId": "awgpawighago0ia",
    "parameters": []
}
```

### - `WindowAction`

**Description:**

Send an inventory action, for example a click.

**Parameters:**

- `windowId`

  **Type:** `integer`

  **Description:** An id of an inventory

- `slotId`

  **Type:** `integer`

  **Description** An id of an inventory slot

- `windowActionType`

  **Type:** [`WindowActionType` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Inventory/WindowActionType.cs)

**Return type:** `boolean`

**Example:**

```json
{
    "command": "WindowAction",
    "requestId": "agpoigjawg0iawg",
    "parameters": [2, 14, 1]
}
```

### - `ChangeSlot`

**Description:**

Change the currently selected hot bar slot.

**Parameters:** - `slotId`

**Type:** `integer`

**Description** An id of an inventory slot.

**Return type:** `boolean`

**Example:**

```json
{
    "command": "ChangeSlot",
    "requestId": "awdadiajh0fgi",
    "parameters": [2]
}
```

### - `GetCurrentSlot`

**Description:**

Get the currently selected hot bar slot.

**Parameters:** 

- No Parameters

**Return type:** `integer`

**Example:**

```json
{
    "command": "GetCurrentSlot",
    "requestId": "sadg0as8h",
    "parameters": []
}
```

### - `ClearInventories`

**Description:**

Clear the list of opened inventories.

**Parameters:** 

- No Parameters

**Return type:** `boolean`

**Example:**

```json
{
    "command": "ClearInventories",
    "requestId": "2ouniuowaseghbnew",
    "parameters": []
}
```

### - `UpdateSign`

**Description:**

Update the text in signs.

**Parameters:**

- `X`

  **Type:** `double`

- `Y`

  **Type:** `double`

- `Z`

  **Type:** `double`

- `line1`

  **Type:** `string`

- `line2`

  **Type:** `string`

- `line3`

  **Type:** `string`

- `line4`

  **Type:** `string`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "UpdateSign",
    "requestId": "gsisgsuig0gs",
    "parameters": [145, 67, 1234, "This is line 1", "This is line 2", "This is line 3", "This is line 4"]
}
```

### - `SelectTrade`

**Description:**
Select a villager trade.

**Parameters:**

- `selectedSlot`

  **Type:** `integer`

**Return type:** `boolean`

**Example:**

```json
{
    "command": "SelectTrade",
    "requestId": "awdpa[9doujwapdi]",
    "parameters": [2]
}
```

### - `UpdateCommandBlock`

**Description:**

Update the command block.

**Parameters:**

- `X`

  **Type:** `double`

- `Y`

  **Type:** `double`

- `Z`

  **Type:** `double`

- `command`

  **Type:** `string`

- `mode`

  **Type:** [`CommandBlockMode` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Mapping/CommandBlockMode.cs)

- `flags`

  **Type:** [`CommandBlockFlags` as an integer](https://github.com/MCCTeam/Minecraft-Console-Client/blob/5de84d7e5927062d867585d7fe0a0bba937ec039/MinecraftClient/Mapping/CommandBlockFlags.cs)

**Return type:** `boolean`

**Example:**

```json
{
    "command": "UpdateCommandBlock",
    "requestId": "aw[apkda=-pd]",
    "parameters": [56, 122, 34, "say This is a command", 4, 2]
}
```

### - `CloseInventory`

**Description:**

Close an inventory id.

**Parameters:**

- `windowId`

  **Type:** `integer`

  **Description:** Inventory Id

**Return type:** `boolean`

**Example:**

```json
{
    "command": "CloseInventory",
    "requestId": "awpkfa0phiawd",
    "parameters": [5]
}
```

### - `GetMaxChatMessageLength`

**Description:**

Get the max chat message length.

**Parameters:**

- No parameters

**Return type:** `integer`

**Example:**

```json
{
    "command": "GetMaxChatMessageLength",
    "requestId": "foajfja0fajf0i",
    "parameters": []
}
```

### - `Respawn`

**Description:**

Respawn the bot when it's dead.

**Parameters:**

- No parameters

**Return type:** `boolean`

**Example:**

```json
{
    "command": "Respawn",
    "requestId": "qawepifaihopafhio",
    "parameters": []
}
```

### - `GetProtocolVersion`

**Description:**

Get the current protocol version

**Parameters:**

No parameters

**Return type:** `integer`

**Example:**

```json
{
    "command": "GetProtocolVersion",
    "requestId": "219u2wqt-q9j-t9ujq",
    "parameters": []
}
```

# Web Socket Events (Web Socket Chat Bot protocol events)

## `OnWsCommandResponse`

  **Description:**
  
  Sent by the WebSocket Chat Bot when a command was executed.

  **Response body:**

  - `success`

    **Type:** `boolean`

    **Description:** Flags the command execution as either successful if `true` or not successful if `false`.

  - `requestId`

    **Type:** `string`

    **Description:** The request Id that was sent when the command was sent to the WebSocket Chat Bot, used to track commands. (Randomly generated on each command sending)

  - `command`

    **Type:** `string`

    **Description:** The command that was sent.

  - `result`

    **Type:** `object`

    **Description:** The value that the command has returned.

  **Example:**

  ```json
  {
    "event": "OnWsCommandResponse",
    "data": {
      "success": true,
      "requestId": "ZLxcOhfMyf4SzNCqwMTx", 
      "command": "LogToConsole", 
      "result": true
    }
  }
  ```

# MCC Events

## `OnBlockBreakAnimation`

  **Description:**

  Sent when a block is broken in the world.

  **Parameters:**

  - `Entity`

    **Type:** `Entity json encoded object`

  - `Location`

    **Type:** `Location json encoded object`

  - `stage`

    **Type:** `integer`
   
  **Example:**

  ```json
  {
    "event": "OnBlockBreakAnimation",
    "data": {}
  }
  ```

## `OnEntityAnimation`

  **Description:**

  Sent when an entity does an animation.

  **Parameters:**

  - `Entity`

    **Type:** `Entity json encoded object`

  - `animation`

    **Type:** `integer`
   
  **Example:**

  ```json
  {
    "event": "OnEntityAnimation",
    "data": {
      "entity": {
          "ID":8,
          "UUID":"8c0e3dc3-9bcc-3e03-a138-53348330d4ee",
          "Name":"someplayer",
          "CustomNameJson":null,
          "IsCustomNameVisible":false,
          "CustomName":null,
          "Latency":0,
          "Type":77,
          "Location":{
              "X":-46.08784180879593,
              "Y":68,
              "Z":147.68046873807907,
              "Status":0,
              "ChunkX":-3,
              "ChunkY":8,
              "ChunkZ":9,
              "ChunkBlockX":1,
              "ChunkBlockY":4,
              "ChunkBlockZ":3
          },
          "Yaw":178.59375,
          "Pitch":28.125,
          "ObjectData":-1,
          "Health":20,
          "Item":{
              "Type":18,
              "Count":0,
              "NBT":null,
              "IsEmpty":true,
              "DisplayName":null,
              "Lores":null,
              "Damage":0
          },
          "Pose":0,
          "Metadata":{
              "6":0
          },
          "Equipment": {}
      },
      "animation":0
    }
  }
  ```

## `OnChatPrivate`

  **Description:**

  Sent when the MCC receives a private chat message.

  **Parameters:**

  - `sender`

    **Type:** `string`

  - `message`

    **Type:** `string`

  - `rawText`

    **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnChatPublic",
    "data": {
      "sender":"milutinke",
      "message":"hey there",
      "rawText":"milutinke whispers to you: hey there"
    }
  }
  ```

## `OnChatPublic`

  **Description:**

  Sent when a public message was sent in the chat.

  **Parameters:**

  - `username`

    **Type:** `string`

  - `message`

    **Type:** `string`

  - `rawText`

  **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnChatPublic",
    "data": {
      "username":"milutinke",
      "message":"hello world",
      "rawText":"<milutinke> hello world"
    }
  }
  ```

## `OnTeleportRequest`

  **Description:**

  Sent when the bot gets a teleport request

  **Parameters:**

   - `sender`

      **Type:** `string`

  - `rawText`

      **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnTeleportRequest",
    "data": {
      "sender": "milutinke",
      "rawText": "Milutinke want's to teleport to you. Type /tpaccept to accept the teleport request."
    }
  }
  ```

## `OnChatRaw`

  **Description:**

  Sent when any kind of chat message was received by the MCC. Can contain JSON.

  **Parameters:**

  - `text`

    **Type:** `string`

  - `json`

    **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnChatRaw",
    "data": {
      "text":"someplayer has made the advancement §a[§aCover Me with Diamonds]",
      "json":"{\"translate\":\"chat.type.advancement.task\",\"with\":[{\"insertion\":\"someplayer\",\"clickEvent\":{\"action\":\"suggest_command\",\"value\":\"/tell someplayer \"},\"hoverEvent\":{\"action\":\"show_entity\",\"contents\":{\"type\":\"minecraft:player\",\"id\":\"8c0e3dc3-9bcc-3e03-a138-53348330d4ee\",\"name\":{\"text\":\"someplayer\"}}},\"text\":\"someplayer\"},{\"color\":\"green\",\"translate\":\"chat.square_brackets\",\"with\":[{\"hoverEvent\":{\"action\":\"show_text\",\"contents\":{\"color\":\"green\",\"extra\":[{\"text\":\"\\n\"},{\"translate\":\"advancements.story.shiny_gear.description\"}],\"translate\":\"advancements.story.shiny_gear.title\"}},\"translate\":\"advancements.story.shiny_gear.title\"}]}]}"
    }
  }
  ```

## `OnDisconnect`

  **Description:** 

  Sent when the bot has disconnected from a server. At this point you can't send commands to the MCC.

  **Parameters:**

  - `reason`

      **Type:** `string`

  - `message`

      **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnDisconnect",
    "data": {
      "reason": "<reason json encoded object>",
      "message": "<message json encoded object>"
    }
  }
  ```

## `OnPlayerProperty`

  **Description:**

  Sent when the server need to update a player property

  **Parameters:**

  - `prop`

      **Type:** `json encoded object of { string key: double/number value }`
   
  **Example:**

  ```json
  {
    "event": "OnPlayerProperty",
    "data": {
      "minecraft:generic.movement_speed": 0.10000000149011612
    }
  }
  ```

## `OnServerTpsUpdate`

  **Description:**

  Sent when the server TPS changes/updates.

  **Parameters:**

  - `tps`

    **Type:** `double`
   
  **Example:**

  ```json
  {
    "event": "OnServerTpsUpdate",
    "data": {
      "tps": 20.0
    }
  }
  ```

## `OnTimeUpdate`

  **Description:**

  Sent when the world time changes.

  **NOTE: Sent quite frequently.**

  **Parameters:**

  - `worldAge`

      **Type:** `long`

  - `timeOfDay`

      **Type:** `long`
   
  **Example:**

  ```json
  {
    "event": "OnTimeUpdate",
    "data": {
      "worldAge": 1719192,
      "timeOfDay": -1132
    }
  }
  ```

## `OnEntityMove`

  **Description:** 

  Sent when an entity moves.

  **NOTE: Sent quite frequently.**

  **Parameters:**

  - `Entity`
  
      **Type:** `Entity json encoded object`
   
  **Example:**

  ```json
  {
    "event": "OnEntityMove",
    "data": {
      "ID":16,
      "UUID":"00000000-0000-0000-0000-000000000000",
      "Name":null,
      "CustomNameJson":null,
      "IsCustomNameVisible":false,
      "CustomName":null,
      "Latency":0,
      "Type":14,
      "Location":{
          "X":5.5,
          "Y":-47.9375,
          "Z":204.5,
          "Status":0,
          "ChunkX":0,
          "ChunkY":1,
          "ChunkZ":12,
          "ChunkBlockX":5,
          "ChunkBlockY":0,
          "ChunkBlockZ":12
      },
      "Yaw":0,
      "Pitch":0,
      "ObjectData":0,
      "Health":1,
      "Item":{
          "Type":18,
          "Count":0,
          "NBT":null,
          "IsEmpty":true,
          "DisplayName":null,
          "Lores":null,
          "Damage":0
      },
      "Pose":0,
      "Metadata":null,
      "Equipment": {}
    }
  }
  ```

## `OnInternalCommand`

  **Description:**

  Sent when an internal MCC command has been executed.

  **Parameters:**

  - `command`

      **Type:** `string`

  - `parameters`

      **Type:** `string`

  - `result`

      **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnInternalCommand",
    "data": {
      "command": "dig -115 74 -19",
      "parameters": "-115 74 -19",
      "result": "Attempting to dig block at -114,5 74 -18,5 (Grass Block)"
    }
  }
  ```

## `OnEntitySpawn`

  **Description:**

  Sent when an entity is spawned or enters the player radius.

  **Parameters:**

  - `Entity`

    **Type:** `Entity json encoded object`
   
  **Example:**

  ```json
  {
    "event": "OnEntitySpawn",
    "data": {
      "ID":78,
      "UUID":"00000000-0000-0000-0000-000000000000",
      "Name":null,
      "CustomNameJson":null,
      "IsCustomNameVisible":false,
      "CustomName":null,
      "Latency":0,
      "Type":15,
      "Location":{
          "X":-47.5,
          "Y":68,
          "Z":146.5,
          "Status":0,
          "ChunkX":-3,
          "ChunkY":8,
          "ChunkZ":9,
          "ChunkBlockX":0,
          "ChunkBlockY":4,
          "ChunkBlockZ":2
      },
      "Yaw":30.9375,
      "Pitch":0,
      "ObjectData":0,
      "Health":1,
      "Item":{
          "Type":18,
          "Count":0,
          "NBT":null,
          "IsEmpty":true,
          "DisplayName":null,
          "Lores":null,
          "Damage":0
      },
      "Pose":0,
      "Metadata":null,
      "Equipment":{ }
    }
  }
  ```

## `OnEntityDespawn`

  **Description:**

  Sent when an entity is de-spawned or leaves the player radius.

  **Parameters:**

  - `Entity`

    **Type:** `Entity json encoded object`
   
  **Example:**

  ```json
  {
    "event": "OnEntityDespawn",
    "data": {
      "ID":15,
      "UUID":"00000000-0000-0000-0000-000000000000",
      "Name":null,
      "CustomNameJson":null,
      "IsCustomNameVisible":false,
      "CustomName":null,
      "Latency":0,
      "Type":56,
      "Location":{
          "X":-38.818737210380526,
          "Y":68,
          "Z":194.05856433486986,
          "Status":0,
          "ChunkX":-3,
          "ChunkY":8,
          "ChunkZ":12,
          "ChunkBlockX":9,
          "ChunkBlockY":4,
          "ChunkBlockZ":2
      },
      "Yaw":0,
      "Pitch":0,
      "ObjectData":0,
      "Health":1,
      "Item":{
          "Type":396,
          "Count":1,
          "NBT":{ },
          "IsEmpty":false,
          "DisplayName":null,
          "Lores":null,
          "Damage":0
      },
      "Pose":0,
      "Metadata":{
          "8":{
              "Type":396,
              "Count":1,
              "NBT":{
                  
              },
              "IsEmpty":false,
              "DisplayName":null,
              "Lores":null,
              "Damage":0
          }
      },
      "Equipment":{ }
    }
  }
  ```

### - `OnHeldItemChange`

  **Description:**

  Sent when a held item is changed.

  **Parameters:**

  - `itemSlot`

    **Type:** `integer`
   
  **Example:**

  ```json
  {
    "event": "OnHeldItemChange",
    "data": {
      "itemSlot": 1
    }
  }
  ```

### - `OnHealthUpdate`

  **Description:**

  Sent when player's health is updated.

  **Parameters:**

  - `health`

    **Type:** `float`

  - `food`

    **Type:** `int`
   
  **Example:**

  ```json
  {
    "event": "OnHealthUpdate",
    "data": {
      "health": 18,
      "food": 7
    }
  }
  ```

### - `OnExplosion`

  **Description:**

  Sent when there is an explosion.

  **Parameters:**

  - `Location`

    **Type:** `Location json encoded object`

  - `strength`

    **Type:** `float`

  - `recordCount`

    **Type:** `int`
   
  **Example:**

  ```json
  {
    "event": "OnExplosion",
    "data": {
      "location": {
        "X": -117.49000000953674,
        "Y": 66.0612500011921,
        "Z": -26.490000009536743,
        "Status": 0,
        "ChunkX": -8,
        "ChunkY": 8,
        "ChunkZ": -2, 
        "ChunkBlockX": 10,
        "ChunkBlockY": 2, 
        "ChunkBlockZ": 5
      },
      "strength": 4,
      "recordCount": 139
    }
  }
  ```

### - `OnSetExperience`

  **Description:**

  Sent when the player's experience is updated.

  **Parameters:**

  - `experienceBar`

    **Type:** `float`

  - `level`

    **Type:** `int`

- `totalExperience`

    **Type:** `int`
   
  **Example:**

  ```json
  {
    "event": "OnSetExperience",
    "data": {
      "experienceBar": 0.60504204,
      "level": 7,
      "totalExperience": 120
    }
  }
  ```

### - `OnGamemodeUpdate`

  **Description:**

  Sent when the player's game mode has changed.

  **Parameters:**

  - `playerName`

    **Type:** `string`

  - `uuid`

    **Type:** `string with UUID`

  - `gameMode`

    **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnGamemodeUpdate",
    "data": {
      "playerName": "milutinke",
      "uuid": "8c0e3dc3-9bcc-3e03-a138-53348330d4ee",
      "gameMode": "creative"
    }
  }
  ```

### - `OnLatencyUpdate`

  **Description:**

  Sent when the player's ping has changed.

  **Parameters:**

- `playerName`

    **Type:** `string`

- `uuid`

    **Type:** `string with UUID`

- `latency`

    **Type:** `int`
   
  **Example:**

  ```json
  {
    "event": "OnLatencyUpdate",
    "data": {
      "playerName": "someplayer",
      "uuid":"baa6eda2-cbc5-5119-870d-1960ce60574d",
      "latency": 14
    }
  }
  ```

### - `OnMapData`

  **Description:**

  Sent when map data is received.

  **Parameters:**

  - `mapId`

    **Type:** `int`

  - `scale`

    **Type:** `integer`

  - `trackingPosition`

    **Type:** `bool`

  - `locked`

    **Type:** `bool`

  - `icons`

    **Type:** `array of map icon object`

  - `columnsUpdated`

    **Type:** `integer`

  - `rowsUpdated`

    **Type:** `integer`

  - `mapColumnX`

    **Type:** `integer`

  - `mapRowZ`

    **Type:** `integer`

  - `colors`

    **Type:** `base 64 encoded string of colors`
   
  **Example:**

  ```json
  {
    "event": "OnMapData",
    "data": {
      "mapId": 1,
      "scale": 0,
      "trackingPosition": true,
      "locked": false,
      "icons": [],
      "columnsUpdated": 128,
      "rowsUpdated": 128,
      "mapColumnX": 0,
      "mapRowZ": 0,
      "colors": null // ommited in this example, too long
    }
  }
  ```

### - `OnTradeList`

  **Description:**

  Sent when villager's trade list has been received/updated.

  **Parameters:**

  - `windowId`

    **Type:** `int`

  - `trades`

    **Type:** `List<VillagerTrade>`

  - `villagerInfo`

    **Type:** `VillagerInfo`
   
  **Example:**

  ```json
  {
    "event": "OnTradeList",
    "data": {
     "windowId": 2,
      "trades": <trades json encoded object>,
      "villagerInfo": <villagerInfo json encoded object>
    }
  }
  ```

### - `OnTitle`

  **Description:**

  Sent when a title action has been received.

  **Parameters:**
  - `action`

    **Type:** `int`

  - `titleText`

    **Type:** `string`

  - `subtitleText`

    **Type:** `string`

  - `actionBarText`

    **Type:** `string`

  - `fadeIn`

     **Type:** `int`

  - `stay`

     **Type:** `int`

  - `fadeout`

    **Type:** `int`

  - `json_`

    **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnTitle",
    "data": {
      "action": <action json encoded object>,
      "titleText": "<titleText json encoded object>",
      "subtitleText": "<subtitleText json encoded object>",
      "actionBarText": "<actionBarText json encoded object>",
      "fadeIn": <fadeIn json encoded object>,
      "stay": <stay json encoded object>,
      "fadeout": <fadeout json encoded object>,
      "json_": "<json_ json encoded object>"
    }
  }
  ```

### - `OnEntityEquipment`

  **Description:**

  Sent when entity has changed or equipped equipment.

  **Parameters:**
  
  - `Entity`

    **Type:** `Entity json encoded object` (nullable)

  - `slot`

    **Type:** `int`

  - `item`

    **Type:** `Item?`
   
  **Example:**

  ```json
  {
   "event": "OnEntityEquipment",
    "data": {
      "entity":{
          "ID":8,
          "UUID":"8c0e3dc3-9bcc-3e03-a138-53348330d4ee",
          "Name":"someplayer",
          "CustomNameJson":null,
          "IsCustomNameVisible":false,
          "CustomName":null,
          "Latency":0,
          "Type":77,
          "Location":{
              "X":-46.88311344939438,
              "Y":68,
              "Z":146.96050249975414,
              "Status":0,
              "ChunkX":-3,
              "ChunkY":8,
              "ChunkZ":9,
              "ChunkBlockX":1,
              "ChunkBlockY":4,
              "ChunkBlockZ":2
          },
          "Yaw":178.59375,
          "Pitch":28.125,
          "ObjectData":-1,
          "Health":20,
          "Item":{
              "Type":18,
              "Count":0,
              "NBT":null,
              "IsEmpty":true,
              "DisplayName":null,
              "Lores":null,
              "Damage":0
          },
          "Pose":0,
          "Metadata":{
              "6":0
          },
          "Equipment":{
              "0":{
                  "Type":368,
                  "Count":1,
                  "NBT":{
                      "Damage":0
                  },
                  "IsEmpty":false,
                  "DisplayName":null,
                  "Lores":null,
                  "Damage":0
              }
          }
      },
      "slot":0,
      "item":{
          "Type":368,
          "Count":1,
          "NBT":{
              "Damage":0
          },
          "IsEmpty":false,
          "DisplayName":null,
          "Lores":null,
          "Damage":0
      }
    }
  }
  ```

### - `OnEntityEffect`
  **Description:**
  Sent when there are effects applied to an entity.

  **Parameters:**

  - `Entity`

    **Type:** `Entity json encoded object`

  - `effect`

    **Type:** `Effects`

  - `amplifier`

    **Type:** `int`

  - `duration`

    **Type:** `int`

  - `flags`

    **Type:** `integer`
   
  **Example:**

  ```json
  {
    "event": "OnEntityEffect",
    "data": {
      "entity": {
        "ID": 50,
        "UUID": "8c0e3dc3-9bcc-3e03-a138-53348330d4ee",
        "Name": "milutinke",
        "CustomNameJson": null,
        "IsCustomNameVisible": false,
        "CustomName": null,
        "Latency": 0,
        "Type": 77,
        "Location": {
          "X": -116.15188604696566,
          "Y": 74.79847191937456,
          "Z": -22.679173221632723,
          "Status": 0,
          "ChunkX": -8,
          "ChunkY": 8,
          "ChunkZ": -2,
          "ChunkBlockX": 11,
          "ChunkBlockY": 10,
          "ChunkBlockZ": 9
        },
        "Yaw": 330.46875,
        "Pitch": 9.84375,
        "ObjectData": -1,
        "Health": 20,
        "Item": {
          "Type": 18,
          "Count": 0,
          "NBT": null,
          "IsEmpty": true,
          "DisplayName": null,
          "Lores": null,
          "Damage": 0
        },
        "Pose": 0,
        "Metadata": {
          "9": 20,
          "11": true,
          "16": 122,
          "17": 127
        },
        "Equipment": {}
      },
      "effect": 33,
      "amplifier": 0,
      "duration": 77,
      "flags": 0
    }
  }
  ```

### - `OnScoreboardObjective`

  **Description:**

  Sent when scoreboard objective has been added.

  **Parameters:**

  - `objectiveName`

    **Type:** `string`

  - `mode`

    **Type:** `integer`

  - `objectiveValue`

    **Type:** `string`

  - `type`

    **Type:** `int`

  - `json_`

    **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnScoreboardObjective",
    "data": {
      "objectiveName": "testObj",
      "mode": 0, 
      "objectiveValue": "Test Objective",
      "type": 0,
      "rawJson": "{\"text\":\"Testobj\"}"
    }
  }
  ```

### - `OnUpdateScore`

  **Description:**

  Sent when scoreboard objective has been update/changed for an entity.

  **Parameters:**

  - `entityName`

    **Type:** `string`

  - `action`

    **Type:** `int`

  - `objectiveName`

    **Type:** `string`

  - `type`

     **Type:** `int`
   
  **Example:**

  ```json
  {
    "event": "OnUpdateScore",
    "data": {
      "entityName": "test entity",
      "action": 1,
      "objectiveName": "test_objective",
      "type": 1
    }
  }
  ```

### - `OnInventoryUpdate`

  **Description:** 

  Sent when the an inventory has been updated.

  **Parameters:**

  - `inventoryId`

    **Type:** `int`
   
  **Example:**

  ```json
  {
    "event": "OnInventoryUpdate",
    "data": {
      "inventoryId": 4
    }
  }
  ```

### - `OnInventoryOpen`

  **Description:** 

  Sent when a player opens an inventory.

  **Parameters:**

  - `inventoryId`

    **Type:** `int`
   
  **Example:**

  ```json
  {
    "event": "OnInventoryOpen",
    "data": {
      "inventoryId": 5
    }
  }
  ```

### - `OnInventoryClose`

  **Description:**

  Sent when a player/server closes an inventory.

  **Parameters:**

 - `inventoryId`

    **Type:** `int`
   
  **Example:**

  ```json
  {
    "event": "OnInventoryClose",
    "data": {
      "inventoryId": 4
    }
  }
  ```

### - `OnPlayerJoin`

  **Description:**

  Sent when a player joins the server. (Not the bot)

  **Parameters:**

  - `uuid`

    **Type:** `string with UUID`

  - `name`

    **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnPlayerJoin",
    "data": {
      "uuid": "8c0e3dc3-9bcc-3e03-a138-53348330d4ee",
      "name": "milutinke"
    }
  }
  ```

### - `OnPlayerLeave`

  **Description:**

  Sent when a player leaves the server. (Not the bot)

  **Parameters:**

  - `uuid`

    **Type:** `string with UUID`

  - `name`

    **Type:** `string`
   
  **Example:**

  ```json
  {
    "event": "OnPlayerLeave",
    "data": {
      "uuid":"8c0e3dc3-9bcc-3e03-a138-53348330d4ee",
      "name":"milutinke"
    }
  }
  ```

### - `OnDeath`

  **Description:**

  Sent when the bot dies.

  **Parameters:** None
   
  **Example:**

  ```json
  {
    "event": "OnDeath",
    "data": null
  }
  ```

### - `OnRespawn`

  **Description:**

  Sent when the bot respawns.

  **Parameters:** None
   
  **Example:**

  ```json
  {
    "event": "OnRespawn",
    "data": null
  }
  ```

### - `OnEntityHealth`

  **Description:**

  Sent when an entity health changes/updates.

  **Parameters:**

  - `Entity`

    **Type:** `Entity json encoded object` (nullable)

  - `health`

    **Type:** `float`
   
  **Example:**

  ```json
  {
    "event": "OnEntityHealth",
    "data": {
      "entity":{
          "ID":78,
          "UUID":"00000000-0000-0000-0000-000000000000",
          "Name":null,
          "CustomNameJson":null,
          "IsCustomNameVisible":false,
          "CustomName":null,
          "Latency":0,
          "Type":15,
          "Location":{
              "X":-47.5,
              "Y":68,
              "Z":146.5,
              "Status":0,
              "ChunkX":-3,
              "ChunkY":8,
              "ChunkZ":9,
              "ChunkBlockX":0,
              "ChunkBlockY":4,
              "ChunkBlockZ":2
          },
          "Yaw":30.9375,
          "Pitch":0,
          "ObjectData":0,
          "Health":3,
          "Item":{
              "Type":18,
              "Count":0,
              "NBT":null,
              "IsEmpty":true,
              "DisplayName":null,
              "Lores":null,
              "Damage":0
          },
          "Pose":0,
          "Metadata":{
              "9":4
          },
          "Equipment":{
              
          }
      },
      "health":3
    }
  }
  ```

### - `OnEntityMetadata`

  **Description:**

  Sent when entity's metadata has been received/updated/changed.

  **Parameters:**

  - `Entity`

    **Type:** `Entity json encoded object`

  - `metadata`

    **Type:** `Object of number as a key and object as value` (nullable)
   
  **Example:**

  ```json
  {
    "event": "OnEntityMetadata",
    "data": {
      "entity":{
          "ID":78,
          "UUID":"00000000-0000-0000-0000-000000000000",
          "Name":null,
          "CustomNameJson":null,
          "IsCustomNameVisible":false,
          "CustomName":null,
          "Latency":0,
          "Type":15,
          "Location":{
              "X":-47.5,
              "Y":68,
              "Z":146.5,
              "Status":0,
              "ChunkX":-3,
              "ChunkY":8,
              "ChunkZ":9,
              "ChunkBlockX":0,
              "ChunkBlockY":4,
              "ChunkBlockZ":2
          },
          "Yaw":30.9375,
          "Pitch":0,
          "ObjectData":0,
          "Health":3,
          "Item":{
              "Type":18,
              "Count":0,
              "NBT":null,
              "IsEmpty":true,
              "DisplayName":null,
              "Lores":null,
              "Damage":0
          },
          "Pose":0,
          "Metadata":{
              "9":3
          },
          "Equipment":{
              
          }
      },
      "metadata":{
          "9":3
      }
    }
  }
  ```

### - `OnPlayerStatus`

  **Description:**

  Sent when player's status has been updated/changed.

  **Parameters:**

  - `statusId`

  **Type:** `integer`
   
  **Example:**

  ```json
  {
    "event": "OnPlayerStatus",
    "data": {
      "statusId": 5
    }
  }
  ```

### - `OnNetworkPacket`

  **Description:**

  Sent when player's status has been updated/changed.

  **Parameters:**

  - `packetId`

    **Type:** `integer`

  - `isLogin`

    **Type:** `boolean`
      
    **Description:** Is the packet sent during the `login` phase. (Always `false`)

  - `isInbound`

    **Type:** `integer`

    **Description:** Is the packet sent from the server or by the MCC.

  - `packetData`

    **Type:** `array of bytes`

    **Description:** A raw byte array.