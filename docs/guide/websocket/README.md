# WebSocket Bot

The WebSocket Bot is an **external example bot** that lets you remotely control MCC over WebSocket.
It runs a local WebSocket server inside your MCC session, accepts commands as JSON messages, and pushes game events back to connected clients in real time.

::: warning External Bot
This bot is **not** built into MCC.
You load it as a standalone script with `/script ChatBots/WebSocketBot.cs`.
:::

## Quick Start

1. Copy `config/ChatBots/WebSocketBot.cs` into your MCC `config/ChatBots/` folder (it ships in the repo under that path).
2. Open the file and edit the line near the top:
   ```csharp
   MCC.LoadBot(new WebSocketBot("127.0.0.1", 8043, "CHANGE_THIS_PASSWORD"));
   ```
   - Replace `127.0.0.1` with the IP to bind (use `+` or `*` for all interfaces).
   - Replace `8043` with your preferred port.
   - Replace `CHANGE_THIS_PASSWORD` with a strong, unique password.
3. Optionally enable debug logging:
   ```csharp
   MCC.LoadBot(new WebSocketBot("127.0.0.1", 8043, "mypassword", debugMode: true));
   ```
4. In MCC, run: `/script ChatBots/WebSocketBot.cs`

The bot starts a WebSocket server. Connect to `ws://127.0.0.1:8043/` with any WebSocket client.

## Protocol Overview

All communication uses JSON over WebSocket text frames.

### Authentication Flow

```
Connect via WebSocket
        |
        v
(Optional) Send "ChangeSessionId" to set a friendly session name
        |
        v
Send "Authenticate" with the configured password
        |
        v
Send commands and receive events
```

### Sending Commands

Commands are JSON objects with this shape:

```json
{
  "command": "CommandName",
  "requestId": "any-unique-string",
  "parameters": [1, "text", true]
}
```

- `command` - the procedure name (case-sensitive)
- `requestId` - a client-generated ID so you can match responses to requests
- `parameters` - an ordered array of arguments (types depend on the command)

Every command produces an `OnWsCommandResponse` event with `success`, `requestId`, and optionally `message`.

### Sending Plain Text

You can also send plain text directly:

- Text starting with `/` is forwarded to MCC as an internal command (e.g., `/move north`).
- Other text is sent as chat.

### Receiving Events

Events arrive as JSON:

```json
{
  "event": "EventName",
  "data": "{ ... serialized payload ... }"
}
```

The `data` field is a JSON string that you parse separately to get the event payload.

## Enum Serialization (String Names)

All enum values (ItemType, EntityType, Direction, Hand, etc.) are serialized as **string names**, not numeric IDs.

For example, an entity of type `Zombie` appears as:

```json
{ "type": "Zombie", "location": { "x": 10, "y": 64, "z": -20 } }
```

When sending commands that accept enum parameters, you can pass **either** a string name or a numeric value:

```json
{ "command": "InteractEntity", "requestId": "abc", "parameters": [42, "Interact", "MainHand"] }
```

or:

```json
{ "command": "InteractEntity", "requestId": "abc", "parameters": [42, 0, 0] }
```

Two dedicated commands let you query the full mapping tables:

- `GetItemTypeMappings` returns `{ "DiamondSword": 798, "Stone": 1, ... }`
- `GetEntityTypeMappings` returns `{ "Player": 128, "Zombie": 119, ... }`

These are useful if your client needs a name-to-ID lookup for the current MCC version.

## Reference

- [Commands](Commands.md) - full list of available commands
- [Events](Events.md) - full list of emitted events

<div class="custom-container tip"><p class="custom-container-title">⭐ Reference Implementation: MCC.js</p>

[MCC.js](https://github.com/milutinke/MCC.js) is a Node.js/TypeScript library built for this bot. It handles authentication, JSON serialization, event subscriptions, and typed command wrappers out of the box.

If you're writing a client in JavaScript or TypeScript, start there.

</div>

## Compatibility

- Requires any MCC version that supports `/script` (standalone MCCScript 1.0 bots).
- Uses only `System.Text.Json` (built into .NET), so no extra DLLs are needed.
- Compatible with [MCC.js](https://github.com/milutinke/MCC.js) and any WebSocket client library.
