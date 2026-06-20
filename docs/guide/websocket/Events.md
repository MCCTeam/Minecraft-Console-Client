# WebSocket Events

Events are JSON messages pushed to all authenticated WebSocket clients.
Each event has this structure:

```json
{
  "event": "EventName",
  "data": "{ ... serialized payload ... }"
}
```

The `data` field is a JSON string. Parse it to access the event payload.

All enum values are serialized as **string names** (e.g., `"Zombie"` instead of `119`).

## Protocol Events

### `OnWsCommandResponse`

Sent after every command execution.

**Payload:**

```json
{
  "success": true,
  "requestId": "your-request-id",
  "message": "optional result or error message"
}
```

Match the `requestId` to track which command produced this response.

### `OnMccCommandResponse`

Sent when a plain-text MCC command (starting with `/`) is executed.

**Payload:**

```json
{
  "command": "move north",
  "status": "Done",
  "result": ""
}
```

### `OnGameJoined`

Sent after the client joins the server and the game session starts.
Payload: `"N/A"`

### `OnWsRestarting`

Sent when the WebSocket server is restarting (e.g., on reconnect).
Payload: `"N/A"`

### `OnWsConnectionClose`

Sent when the WebSocket server is shutting down.
Payload: `"N/A"`

## Chat Events

### `OnChatRaw`

Sent for every incoming chat message, including the raw JSON.

**Payload:**

```json
{
  "text": "Formatted text content",
  "json": "{ raw JSON from server }"
}
```

### `OnChatPublic`

Sent when a public chat message is detected.

**Payload:**

```json
{
  "sender": "PlayerName",
  "message": "Hello world",
  "rawText": "<PlayerName> Hello world"
}
```

### `OnChatPrivate`

Sent when a private message is detected.

**Payload:**

```json
{
  "sender": "PlayerName",
  "message": "Secret message",
  "rawText": "PlayerName whispers to you: Secret message"
}
```

### `OnTeleportRequest`

Sent when a teleport request is detected.

**Payload:**

```json
{
  "sender": "PlayerName",
  "rawText": "PlayerName has requested to teleport to you"
}
```

## Connection Events

### `OnDisconnect`

Sent when MCC disconnects from the server.

**Payload:**

```json
{
  "reason": "ConnectionLost",
  "message": "Connection has been lost."
}
```

Reason values: `ConnectionLost`, `UserLogout`, `InGameKick`, `LoginRejected`.

## Entity Events

Entity objects include their `type` as a string name (e.g., `"Zombie"`, `"Player"`).

### `OnEntitySpawn`

Sent when an entity spawns.

**Payload:** Full entity object.

### `OnEntityDespawn`

Sent when an entity despawns.

**Payload:** Full entity object.

### `OnEntityMove`

Sent when an entity moves.

**Payload:** Full entity object with updated location.

### `OnEntityAnimation`

Sent when an entity plays an animation.

**Payload:**

```json
{
  "entity": { ... },
  "animation": 0
}
```

### `OnEntityHealth`

Sent when an entity's health changes.

**Payload:**

```json
{
  "entity": { ... },
  "health": 20.0
}
```

### `OnEntityMetadata`

Sent when entity metadata updates.

**Payload:**

```json
{
  "entity": { ... },
  "metadata": { "0": ..., "1": ... }
}
```

### `OnEntityEquipment`

Sent when an entity's equipment changes.

**Payload:**

```json
{
  "entity": { ... },
  "slot": 0,
  "item": { "type": "DiamondSword", "count": 1, ... }
}
```

Item types are string names (e.g., `"DiamondSword"`).

### `OnEntityEffect`

Sent when an entity gets an effect.

**Payload:**

```json
{
  "entity": { ... },
  "effect": "Speed",
  "amplifier": 1,
  "duration": 600,
  "flags": 0
}
```

### `OnBlockBreakAnimation`

Sent when a block break animation plays.

**Payload:**

```json
{
  "entity": { ... },
  "location": { "x": 10, "y": 64, "z": -20 },
  "stage": 5
}
```

## Player Events

### `OnPlayerJoin`

Sent when a player joins the server.

**Payload:**

```json
{
  "uuid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "name": "PlayerName"
}
```

### `OnPlayerLeave`

Sent when a player leaves the server.

**Payload:**

```json
{
  "uuid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "name": "PlayerName"
}
```

### `OnPlayerProperty`

Sent when player properties update (e.g., speed, attack damage).

**Payload:** Dictionary of property name to value.

### `OnPlayerStatus`

Sent when the player's status changes.

**Payload:**

```json
{
  "statusId": 0
}
```

### `OnDeath`

Sent when the player dies.
Payload: `"N/A"`

### `OnRespawn`

Sent when the player respawns.
Payload: `"N/A"`

## Health and Experience Events

### `OnHealthUpdate`

Sent when the player's health or food level changes.

**Payload:**

```json
{
  "health": 20.0,
  "food": 20
}
```

### `OnSetExperience`

Sent when experience updates.

**Payload:**

```json
{
  "experienceBar": 0.5,
  "level": 10,
  "totalExperience": 200
}
```

## Game Events

### `OnGamemodeUpdate`

Sent when a player's gamemode changes.

**Payload:**

```json
{
  "playerName": "Steve",
  "uuid": "...",
  "gamemode": 1
}
```

### `OnLatencyUpdate`

Sent when a player's latency changes.

**Payload:**

```json
{
  "playerName": "Steve",
  "uuid": "...",
  "latency": 42
}
```

### `OnHeldItemChange`

Sent when the held item slot changes.

**Payload:**

```json
{
  "slot": 0
}
```

### `OnExplosion`

Sent when an explosion occurs.

**Payload:**

```json
{
  "location": { "x": 10, "y": 64, "z": -20 },
  "strength": 4.0,
  "recordcount": 12
}
```

### `OnTitle`

Sent when a title, subtitle, or action bar message is displayed.

**Payload:**

```json
{
  "action": 0,
  "titleText": "Welcome",
  "subtitleText": "",
  "actionBarText": "",
  "fadeIn": 10,
  "stay": 70,
  "fadeOut": 20,
  "json": "..."
}
```

## Server Events

### `OnServerTpsUpdate`

Sent when the server TPS updates.

**Payload:**

```json
{
  "tps": 20.0
}
```

### `OnTimeUpdate`

Sent when the world time updates.

**Payload:**

```json
{
  "worldAge": 1000000,
  "timeOfDay": 6000
}
```

### `OnInternalCommand`

Sent when an MCC internal command is executed.

**Payload:**

```json
{
  "commandName": "move",
  "commandParams": "north",
  "result": {
    "status": "Done",
    "result": ""
  }
}
```

## Inventory Events

### `OnInventoryUpdate`

Sent when an inventory's contents change.

**Payload:**

```json
{
  "inventoryId": 0
}
```

### `OnInventoryOpen`

Sent when an inventory window opens.

**Payload:**

```json
{
  "inventoryId": 1
}
```

### `OnInventoryClose`

Sent when an inventory window closes.

**Payload:**

```json
{
  "inventoryId": 1
}
```

## Scoreboard Events

### `OnScoreboardObjective`

Sent when a scoreboard objective updates.

**Payload:**

```json
{
  "objectiveName": "health",
  "mode": 0,
  "objectiveValue": "Health",
  "type": 0,
  "json": "...",
  "numberFormat": 0
}
```

### `OnUpdateScore`

Sent when a scoreboard score updates.

**Payload:**

```json
{
  "entityName": "Steve",
  "action": 0,
  "objectiveName": "health",
  "objectiveDisplayName": "Health",
  "value": 20,
  "numberFormat": 0
}
```

## Map and Trade Events

### `OnMapData`

Sent when map data updates.

**Payload:**

```json
{
  "mapId": 0,
  "scale": 1,
  "trackingPosition": true,
  "locked": false,
  "icons": [],
  "columnsUpdated": 128,
  "rowsUpdated": 128,
  "mapColumnX": 0,
  "mapRowZ": 0,
  "colors": "base64-encoded-string"
}
```

Note: `colors` is base64-encoded when present, `null` otherwise.

### `OnTradeList`

Sent when a villager trade list is received.

**Payload:**

```json
{
  "windowId": 1,
  "trades": [...],
  "villagerInfo": { ... }
}
```

## Network Events

### `OnNetworkPacket`

Sent for every network packet (when subscribed).

**Payload:**

```json
{
  "packetID": 42,
  "data": "base64-encoded-packet-data",
  "isLogin": false,
  "isInbound": true
}
```

Note: `data` is base64-encoded. This event generates heavy traffic and is mainly useful for debugging.
