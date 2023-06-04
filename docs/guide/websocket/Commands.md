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
