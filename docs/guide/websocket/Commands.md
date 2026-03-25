# WebSocket Commands

Commands are JSON objects sent over the WebSocket connection.
Each command produces a response through the [`OnWsCommandResponse`](Events.md#onwscommandresponse) event.

```json
{
  "command": "CommandName",
  "requestId": "unique-id",
  "parameters": []
}
```

## Protocol Commands

These commands manage the WebSocket session itself.

### `Authenticate`

Authenticate with the configured password.
Must be called before any other command (except `ChangeSessionId`).

**Parameters:**

| Index | Type   | Description |
| ----- | ------ | ----------- |
| 0     | string | Password    |

**Example:**

```json
{
  "command": "Authenticate",
  "requestId": "auth-001",
  "parameters": ["your-password-here"]
}
```

### `ChangeSessionId`

Rename the current session. Can be called without authentication.
The new ID must be 1-32 characters and not already taken.

**Parameters:**

| Index | Type   | Description    |
| ----- | ------ | -------------- |
| 0     | string | New session ID |

**Example:**

```json
{
  "command": "ChangeSessionId",
  "requestId": "rename-001",
  "parameters": ["my-bot"]
}
```

## Logging Commands

### `LogToConsole`

Log a message to the MCC console.

**Parameters:**

| Index | Type   | Description |
| ----- | ------ | ----------- |
| 0     | string | Message     |

### `LogDebugToConsole`

Log a debug message to the MCC console (only visible in debug mode).

**Parameters:**

| Index | Type   | Description |
| ----- | ------ | ----------- |
| 0     | string | Message     |

### `LogToConsoleTranslated`

Log a translated message using an MCC translation key.

**Parameters:**

| Index | Type   | Description     |
| ----- | ------ | --------------- |
| 0     | string | Translation key |

### `LogDebugToConsoleTranslated`

Log a translated debug message.

**Parameters:**

| Index | Type   | Description     |
| ----- | ------ | --------------- |
| 0     | string | Translation key |

## Session Commands

### `ReconnectToTheServer`

Reconnect to the Minecraft server.

**Parameters:**

| Index | Type | Description                          |
| ----- | ---- | ------------------------------------ |
| 0     | int  | Extra reconnect attempts (default 3) |
| 1     | int  | Delay in seconds (default 0)         |

### `DisconnectAndExit`

Disconnect from the server and shut down MCC.
No parameters.

## Chat Commands

### `SendPrivateMessage`

Send a private message to a player.

**Parameters:**

| Index | Type   | Description |
| ----- | ------ | ----------- |
| 0     | string | Player name |
| 1     | string | Message     |

## Script Commands

### `RunScript`

Run an MCC script file.

**Parameters:**

| Index | Type   | Description |
| ----- | ------ | ----------- |
| 0     | string | File name   |

## World and Terrain Commands

### `GetTerrainEnabled`

Check if terrain handling is enabled.
No parameters. Returns `{ "enabled": true/false }`.

### `SetTerrainEnabled`

Enable or disable terrain handling.

**Parameters:**

| Index | Type | Description |
| ----- | ---- | ----------- |
| 0     | bool | Enabled     |

### `GetWorld`

Check if world data is available.
No parameters. Returns `{ "available": true }` if terrain is enabled.

### `DigBlock`

Break a block at the given coordinates.
Validates the block is within 6 blocks and is not air.

**Parameters:**

| Index | Type   | Description                       |
| ----- | ------ | --------------------------------- |
| 0     | double | X coordinate                      |
| 1     | double | Y coordinate                      |
| 2     | double | Z coordinate                      |
| 3     | string | Direction (optional, e.g. "Down") |

The `Direction` parameter accepts string names: `Down`, `Up`, `North`, `South`, `West`, `East`.

## Entity Commands

### `GetEntityHandlingEnabled`

Check if entity handling is enabled.
No parameters. Returns `{ "enabled": true/false }`.

### `GetEntities`

Get all tracked entities.
No parameters. Returns a dictionary of entity ID to entity object.

Entity types are serialized as string names (e.g., `"Zombie"`, `"Player"`).

### `InteractEntity`

Interact with an entity.

**Parameters:**

| Index | Type   | Description                                           |
| ----- | ------ | ----------------------------------------------------- |
| 0     | int    | Entity ID                                             |
| 1     | string | Interaction type (`Interact`, `Attack`, `InteractAt`) |
| 2     | string | Hand (optional, `MainHand` or `OffHand`)              |

### `SendEntityAction`

Send an entity action.

**Parameters:**

| Index | Type   | Description                                        |
| ----- | ------ | -------------------------------------------------- |
| 0     | string | Action type (e.g. `StartSneaking`, `StopSneaking`) |

### `Sneak`

Toggle sneaking.

**Parameters:**

| Index | Type | Description                  |
| ----- | ---- | ---------------------------- |
| 0     | bool | true to sneak, false to stop |

## Movement Commands

### `GetCurrentLocation`

Get the player's current location.
No parameters. Returns a location object with `x`, `y`, `z`.

### `MoveToLocation`

Move the player to a location using pathfinding.

**Parameters:**

| Index | Type   | Description                      |
| ----- | ------ | -------------------------------- |
| 0     | double | X coordinate                     |
| 1     | double | Y coordinate                     |
| 2     | double | Z coordinate                     |
| 3     | bool   | Allow unsafe (optional, false)   |
| 4     | bool   | Allow direct teleport (optional) |
| 5     | int    | Max offset (optional, 0)         |
| 6     | int    | Min offset (optional, 0)         |

### `ClientIsMoving`

Check if the client is currently moving.
No parameters. Returns `{ "moving": true/false }`.

### `LookAtLocation`

Make the player look at coordinates.

**Parameters:**

| Index | Type   | Description |
| ----- | ------ | ----------- |
| 0     | double | X           |
| 1     | double | Y           |
| 2     | double | Z           |

## Player Info Commands

### `GetUsername`

Get the player's username.
No parameters. Returns `{ "username": "..." }`.

### `GetUserUUID`

Get the player's UUID.
No parameters. Returns `{ "uuid": "..." }`.

### `GetGamemode`

Get the current gamemode.
No parameters. Returns `{ "gamemode": 0 }`.

### `GetYaw`

Get the player's yaw rotation.
No parameters. Returns `{ "yaw": 0.0 }`.

### `GetPitch`

Get the player's pitch rotation.
No parameters. Returns `{ "pitch": 0.0 }`.

### `GetOnlinePlayers`

Get a list of online player names.
No parameters. Returns a string array.

### `GetOnlinePlayersWithUUID`

Get online players with their UUIDs.
No parameters. Returns a dictionary of UUID to player name.

### `GetPlayersLatency`

Get latency information for online players.
No parameters.

## Server Info Commands

### `GetServerHost`

Get the server hostname.
No parameters. Returns `{ "host": "..." }`.

### `GetServerPort`

Get the server port.
No parameters. Returns `{ "port": 25565 }`.

### `GetServerTPS`

Get the server TPS (ticks per second).
No parameters. Returns `{ "tps": 20.0 }`.

### `GetTimestamp`

Get the current timestamp.
No parameters. Returns `{ "timestamp": "..." }`.

### `GetProtocolVersion`

Get the Minecraft protocol version.
No parameters. Returns `{ "protocolVersion": 769 }`.

### `GetMaxChatMessageLength`

Get the maximum chat message length.
No parameters. Returns `{ "length": 256 }`.

## Inventory Commands

### `GetInventoryEnabled`

Check if inventory handling is enabled.
No parameters. Returns `{ "enabled": true/false }`.

### `GetPlayerInventory`

Get the player's inventory.
No parameters. Returns the full inventory container with items.

Item types are serialized as string names (e.g., `"DiamondSword"`, `"Stone"`).

### `GetInventories`

Get all open inventories.
No parameters.

### `WindowAction`

Perform a window/inventory action.

**Parameters:**

| Index | Type   | Description                                                   |
| ----- | ------ | ------------------------------------------------------------- |
| 0     | int    | Inventory ID                                                  |
| 1     | int    | Slot ID                                                       |
| 2     | string | Action type (e.g. `LeftClick`, `RightClick`, `DropItemStack`) |

### `ChangeSlot`

Change the active hotbar slot.

**Parameters:**

| Index | Type  | Description       |
| ----- | ----- | ----------------- |
| 0     | short | Slot number (0-8) |

### `GetCurrentSlot`

Get the currently selected hotbar slot.
No parameters. Returns `{ "slot": 0 }`.

### `SetSlot`

Set the active slot (legacy command).

**Parameters:**

| Index | Type | Description |
| ----- | ---- | ----------- |
| 0     | int  | Slot number |

### `ClearInventories`

Clear tracked inventory state.
No parameters.

### `CloseInventory`

Close an inventory window.

**Parameters:**

| Index | Type | Description  |
| ----- | ---- | ------------ |
| 0     | int  | Inventory ID |

## Creative Mode Commands

### `CreativeGive`

Give an item in creative mode.

**Parameters:**

| Index | Type   | Description                                |
| ----- | ------ | ------------------------------------------ |
| 0     | int    | Slot ID                                    |
| 1     | string | Item type (e.g. `"DiamondSword"` or `798`) |
| 2     | int    | Count                                      |

### `CreativeDelete`

Delete an item from a slot in creative mode.

**Parameters:**

| Index | Type | Description |
| ----- | ---- | ----------- |
| 0     | int  | Slot ID     |

## Block Interaction Commands

### `SendPlaceBlock`

Place a block.

**Parameters:**

| Index | Type   | Description                                  |
| ----- | ------ | -------------------------------------------- |
| 0     | double | X coordinate                                 |
| 1     | double | Y coordinate                                 |
| 2     | double | Z coordinate                                 |
| 3     | string | Direction (e.g. `"Up"`)                      |
| 4     | string | Hand (optional, `"MainHand"` or `"OffHand"`) |

### `SendAnimation`

Play arm swing animation.

**Parameters:**

| Index | Type   | Description                           |
| ----- | ------ | ------------------------------------- |
| 0     | string | Hand (optional, default `"MainHand"`) |

### `UseItemInHand`

Use the item currently held.
No parameters.

### `UpdateSign`

Update text on a sign.

**Parameters:**

| Index | Type   | Description  |
| ----- | ------ | ------------ |
| 0     | double | X coordinate |
| 1     | double | Y coordinate |
| 2     | double | Z coordinate |
| 3     | string | Line 1       |
| 4     | string | Line 2       |
| 5     | string | Line 3       |
| 6     | string | Line 4       |

### `UpdateCommandBlock`

Update a command block.

**Parameters:**

| Index | Type   | Description                                      |
| ----- | ------ | ------------------------------------------------ |
| 0     | double | X coordinate                                     |
| 1     | double | Y coordinate                                     |
| 2     | double | Z coordinate                                     |
| 3     | string | Command                                          |
| 4     | string | Mode (e.g. `"Sequence"`, `"Auto"`, `"Redstone"`) |
| 5     | string | Flags                                            |

## Trading Commands

### `SelectTrade`

Select a villager trade.

**Parameters:**

| Index | Type | Description |
| ----- | ---- | ----------- |
| 0     | int  | Trade index |

### `Respawn`

Respawn after death.
No parameters.

## Mapping Commands (New)

These commands let clients query enum mappings dynamically at runtime, so they do not need to maintain hardcoded numeric ID tables that break across MCC versions. For background, see [issue #2805](https://github.com/MCCTeam/Minecraft-Console-Client/issues/2805).

### `GetItemTypeMappings`

Get a dictionary of all ItemType names to their numeric IDs.
No parameters. Returns `{ "DiamondSword": 798, "Stone": 1, ... }`.

### `GetEntityTypeMappings`

Get a dictionary of all EntityType names to their numeric IDs.
No parameters. Returns `{ "Player": 128, "Zombie": 119, ... }`.
