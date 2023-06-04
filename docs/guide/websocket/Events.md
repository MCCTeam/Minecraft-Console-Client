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