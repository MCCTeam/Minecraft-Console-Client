# Chat Bots

-   [About](#about)
-   [List of built-in Chat Bots](#list-of-built-in-chat-bots)
-   [Creating your own](creating-bots.md)

## About

**Minecraft Console Client** has a number of default built in Chat Bots (Scripts/Plugins) which allow for various types of automation.

> **⚠️ IMPORTANT WARNING: Recently we have changed the configuration format from INI to TOML, this part of the documentation has only been partially updated, it's work in progress, for the time being please refer to the `MinecraftClient.ini` for setting names, the descriptions and options should be up to date in most cases, but not guaranteed.**

> **ℹ️ NOTE: Settings refer to settings in the [configuration file](configuration.md)**

## List of built-in Chat Bots

-   [Alerts](#alerts)
-   [Anti AFK](#anti-afk)
-   [Auto Attack](#auto-attack)
-   [Auto Craft](#auto-craft)
-   [Auto Dig](#auto-dig)
-   [Auto Drop](#auto-drop)
-   [Auto Eat](#auto-eat)
-   [Auto Fishing](#auto-fishing)
-   [Auto Relog](#auto-relog)
-   [Auto Respond](#auto-respond)
-   [Chat Log](#chat-log)
-   [Follow Player](#follow-player)
-   [Hangman](#hangman)
-   [Mailer](#mailer)
-   [Map](#map)
-   [PlayerList Logger](#playerlist-logger)
-   [Remote Control](#remote-control)
-   [Replay Mod](#replay-mod)
-   [Script Scheduler](#script-scheduler)

## Alerts

-   **Description:**

    Get alerted when specified words are detected in the chat

    Useful for moderating your server or detecting when someone is talking to you.

-   **Settings:**

    **Section:** **`ChatBot.Alerts`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Alerts Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Beep_Enabled`

    -   **Description:**

        This setting specifies if you want to hear a beep when you get an alert.

        > **ℹ️ NOTE: This might not work depending on your system or a console (terminal emulator).**

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `Trigger_By_Words`

    -   **Description:**

        Triggers an alert after receiving a specified keyword.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Trigger_By_Rain`

    -   **Description:**

        Trigger alerts when it rains and when it stops.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Trigger_By_Thunderstorm`

    -   **Description:**

        Triggers alerts at the beginning and end of thunderstorms.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Log_To_File`

    -   **Description:**

        Should the Alerts Chat Bot log alerts into a file.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Log_File`

    -   **Description:**

        A path to the file where alerts will be logged if `Log_To_File` is set to `true`.

    -   **Type:** `string`

    -   **Default:** `"alerts-log.txt"`

    #### `Matches`

    -   **Description:**

        List of words/strings to alert you on.

    -   **Type:** `array of strings`

    -   **Example**:

        ```toml
        Matches = [ "Yourname", " whispers ", "-> me", "admin", ".com", ]
        ```

    #### `Excludes`

    -   **Description:**

        List of words/strings to NOT alert you on.

    -   **Type:** `array of strings`

    -   **Example**:

        ```toml
        Excludes = [ "myserver.com", "Yourname>:", "Player Yourname", "Yourname joined", "Yourname left", "[Lockette] (Admin)", " Yourname:", "Yourname is", ]
        ```

## Anti AFK

-   **Description:**

    Send a command and sneak on a regular or random basis or make the bot walk around randomly to avoid automatic AFK disconnection.

-   **Settings:**

    **Section:** **`ChatBot.AntiAFK`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Anti AFK Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Delay`

    -   **Description:**

        The time interval for execution in seconds.

        If the `min` and `max` are the same, the time interval will be consistent.
        However if they are not the same, the plugin will choose a random number between `min` and `max`, this is useful if you want to have a random interval to trick anti afk plugins.

    -   **Format:** `{ min = <seconds>, max = <seconds> }`

    -   **Type:** `inline table with min and max fields which have type of double`

    -   **Default:** `{ min = 60.0, max = 60.0 }`

    #### `Command`

    -   **Description:**

        Command to be sent.

    -   **Type:** `string`

    -   **Default:** `/ping`

    #### `Use_Sneak`

    -   **Description:**

        Sometimes you can trick plugins with sneaking or command might not be enough, enable it if you need it.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Use_Terrain_Handling`

    -   **Description:**

        Should the bot use [Terrain Handling](configuration.md#terrainandmovements) instead of the command method.

        This will enable your bot to randomly move about, thus a better anti afk effect.

        > **ℹ️ NOTE: You need to enable [Terrain Handling](configuration.md#terrainandmovements) in the settings and it's recommended to put the bot into an enclosure not to wander off. (Recommended size 5x5x5)**

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Walk_Range`

    -   **Description:**

        The range which bot will use to walk around (-X to +X and -Z to +Z, Y is not used).

        The bigger the slower the bot might be at calculating the path, recommended 2-5.

    -   **Default:** `5`

    #### `Walk_Retries`

    -   **Description:**

        This is the number of times the bot will try to pathfind, if he can't find a valid path for 20 times, he will use the command method.

        > **ℹ️ NOTE: This happens on each trigger of the task, so it does not permanently switch to alternative method.**

    -   **Default:** `20`

## Auto Attack

-   **Description:**

    Automatically attacks mobs around you, you can configure it to attack both hostile and passive mobs and only certain mobs or all mobs.

    > **ℹ️ NOTE: You need to have [inventoryhandling](configuration.md#inventoryhandling) and [entityhandling](configuration.md#entityhandling) enabled in order for this bot to work.**

-   **Settings:**

    **Section:** **`ChatBot.AutoAttack`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Attack Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Mode`

    -   **Description:**

        Available values:

        -   `single`

            Target one mob per attack.

        -   `multi`

            Target all mobs in range per attack.

    -   **Type:** `string`

    -   **Default:** `single`

    #### `Priority`

    -   **Description:**

        Available values:

        -   `health` (prioritize targeting mobs with lower health)
        -   `distance` (prioritize targeting mobs closer to you)

    -   **Type:** `string`

    -   **Default:** `distance`

    #### `Cooldown_Time`

    -   **Description:**

        How long to wait between each attack in seconds.

        To enable it, set `Custom` (boolean) to `true` and change `value` (double) to your preferred value (eg. `1.5`).

        By the default, this is disabled and the MCC calculates it based on the server TPS.

    -   **Format:** `Cooldown_Time = { Custom = <is enabled (true|false)>, value = <seconds (double)> }`

    -   **Type:** `inline table`

    -   **Example:** `Cooldown_Time = { Custom = true, value = 1.5 }`

    -   **Default:** `{ Custom = false, value = 1.0 }`

    #### `Interaction`

    -   **Description:**

        Available values:

        -   `Attack`

            Just attack a mob. (Default)

        -   `Interact`

            Just interact with a mob.

        -   `InteractAt`

            Interact with and attack a mob.

    -   **Type:** `string`

    -   **Default:** `Attack`

    #### `Attack_Hostile`

    -   **Description:**

        This setting specifies if the Auto Attack Chat Bot should attack hostile mobs.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `Attack_Passive`

    -   **Description:**

        This setting specifies if the Auto Attack Chat Bot should attack passive mobs.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `List_Mode`

    -   **Description:**

        This setting specifies which mode of the list should Auto Attack Chat Bot use for `Entites_List` setting.

    -   **Available values:** `whitelist` (only attack specified mobs) and `blacklist` (do not attack specified mobs).

    -   **Type:** `string`

    -   **Default:** `whitelist`

    #### `Entites_List`

    -   **Description:**

        A list of mobs which are either whitelisted or blacklisted, the mode is set in `List_Mode` setting.

        You can find the full list of mobs [here](https://bit.ly/3Rg68lp).

    -   **Format:** `["<entity type>", "<entity type>", ...]`

    -   **Type:** `array of strings`

    -   **Example:** `[ "Spider", "Skeleton", "Pig", ]`

    -   **Default:** `[ "Zombie", "Cow", ]`

## Auto Craft

-   **Description:**

    Automatically craft items in your inventory or in a crafting table.

    > **ℹ️ NOTE: You need to have [inventoryhandling](configuration.md#inventoryhandling) enabled in order for basic crafting in the inventory to work, in addition if you want to use a crafting table, you need to enable [terrainandmovements](configuration.md#terrainandmovements) in order for bot to be able to reach the crafting table.**

-   **Commands:**

    -   `/autocraft list`

        List all loaded recipes.

    -   `/autocraft start <name>`

        Start the crafting process with the given recipe name you had defined.

    -   `/autocraft stop`

        Stop the crafting process.

    -   `/autocraft help`

        In-game help command.

-   **Settings:**

    **Section:** **`ChatBot.AutoCraft`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Craft Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `CraftingTable`

    -   **Description:**

        This setting specifies the location of the crafting table.

    -   **Type/Format:**

        This setting is an of an `inline table` type that has the following sub-options/settings;

        -   `x` - X coordinate, the type is `double` (eg. `123.0`)

        -   `y` - Y coordinate, the type is `double` (eg. `64.0`)

        -   `z` - Z coordinate, the type is `double` (eg. `456.0`)

    -   **Example:**

        ```toml
        CraftingTable = { X = 123.0, Y = 65.0, Z = 456.0 }
        ```

    #### `OnFailure`

    -   **Description:**

        This setting specifies what the Auto Craft Chat Bot should do on failure.

        Failure can happen when there are no materials available or when a crafting table can't be reached.

    -   **Available values:** `abort` and `wait`.

    -   **Type:** `string`

    -   **Default:** `abort`

    ### Defining a recipe

    The recipes are defines as a separate new sub-section `[[ChatBot.AutoCraft.Recipes]]` of the `[ChatBot.AutoCraft]` section.

    The `[[ChatBot.AutoCraft.Recipes]]` section needs to contain the following settings:

    -   `Name`

        The name of your recipe, can be whatever you like.

        **Type**: `string`

    -   `Type`

        **Avaliable values:** `player` and `table`

        > **ℹ️ NOTE: If you're using `table` you need to set the `CraftingTable` setting.**

    -   `Result`

        This is the type of resulting item.

        **Type:** `string`

        **Example:** `"StoneBricks"`

    -   `Slots`

        This setting is an array/list of material names (strings) that go into an each slot (max 9 elements).
        Empty slots should be marked with `"Null"`

        **Type:** `array of strings`

        **Format:**

        ```toml
        Slots = [ "<material/item type>", "<material/item type>", ... ]
        ```

        > **ℹ️ NOTE: If you have a case where you have to leave some fields empty, use `"Null"` to mark them as empty. Example for stone bricks: `Slots = [ "Stone", "Stone", "Null", "Stone", "Stone", "Null", "Null", "Null", "Null", ]`**

        > **ℹ️ NOTE: All item types can be found [here](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Inventory/ItemType.cs).**

        **Slots are indexed as following:**

        **`2x2` (Player)**

        ```cs
        ╔═══╦═══╗
        ║ 1 ║ 2 ║
        ╠═══╬═══╣
        ║ 3 ║ 4 ║
        ╚═══╩═══╝
        ```

        **`3x3` (Crafting Table)**

        ```cs
        ╔═══╦═══╦═══╗
        ║ 1 ║ 2 ║ 3 ║
        ╠═══╬═══╬═══╣
        ║ 4 ║ 5 ║ 6 ║
        ╠═══╬═══╬═══╣
        ║ 7 ║ 8 ║ 9 ║
        ╚═══╩═══╩═══╝
        ```

    **Full Examples:**

    ```toml
    # Stone Bricks using the player inventory
    [[ChatBot.AutoCraft.Recipes]]
    Name = "Recipe-Name-1"
    Type = "player"
    Result = "StoneBricks"
    Slots = [ "Stone", "Stone", "Stone", "Stone", ]

    # Stone Bricks using a crafting table
    [[ChatBot.AutoCraft.Recipes]]
    Name = "Recipe-Name-2"
    Type = "table"
    Result = "StoneBricks"
    Slots = [ "Stone", "Stone", "Null", "Stone", "Stone", "Null", "Null", "Null", "Null", ]
    ```

    > **ℹ️ NOTE: Make sure to provide materials for your bot by placing them in inventory first.**

## Auto Dig

-   **Description:**

    Automatically digs block on specified locations.

    > **ℹ️ NOTE: You need to have [inventoryhandling](configuration.md#inventoryhandling) and [terrainandmovements](configuration.md#terrainandmovements) enabled in order for this bot to work.**

    > **ℹ️ NOTE: Since MCC does not yet support accurate calculation of the collision volume of blocks, all blocks are considered as complete cubes when obtaining the position of the lookahead.**

-   **Commands:**

    -   `/digbot start` - Starts the digging

    -   `/digbot stop` - Stops the digging

-   **Settings:**

    **Section:** **`ChatBot.AutoDig`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Dig Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Mode`

    -   **Description:**

        This setting specifies in which mode the Auto Dig Chat Bot will operate.

    -   **Available values:**

    -   `lookat`

        Digs the block that the bot is looking at.

    -   `fixedpos`

        Digs the block in a fixed location/position/coordinate.

    -   `both`

        Dig only when the block you are looking at is in the "Locations" list.

    -   **Type:** `string`

    -   **Default:** `lookat`

    #### `Locations`

    -   **Description:**

        This setting specifies an array/list of locations which the bot will dig out.

    -   **Type/Format:**

        The type of this setting is an array of inline table which has the following sub-options/settings:

        -   `x` - X coordinate, the type is `double` (eg. `123.45`)

        -   `y` - Y coordinate, the type is `double` (eg. `64.0`)

        -   `z` - Z coordinate, the type is `double` (eg. `234.5`)

    -   **Full example:**

        ```toml
        Locations = [
           { x = 123.5, y = 64.0, z = 234.5 },
           { x = 124.5, y = 63.0, z = 235.5 },
        ]
        ```

    #### `Location_Order`

    -   **Description:**

        This setting specifies in which order the Auto Dig Chat Bot will dig blocks.

    -   **Available values:**

        -   `distance`

            Digs the block closest to the bot.

        -   `index`

            Digs blocks in the list order.

    -   **Type:** `string`

    -   **Default:** `distance`

    #### `Auto_Start_Delay`

    -   **Description:**

        How many seconds to wait after entering the game to start digging automatically.

        Set to `-1` to disable the automatic start.

    -   **Type:** `float`

    -   **Default:** `3.0`

    #### `Dig_Timeout`

    -   **Description:**

        If mining a block takes longer than this value, a new attempt will be made to find a block to mine.

    -   **Type:** `float`

    -   **Default:** `60.0`

    #### `Log_Block_Dig`

    -   **Description:**

        This setting specifies whether to output logs in to the console when digging blocks.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `List_Type`

    -   **Description:**

        This setting specifies the mode at which the `Blocks` setting is operating.

    -   **Available values:** `whitelist` (only dig specified blocks) and `blacklist` (do not dig specified blocks).

    -   **Type:** `string`

    -   **Default:** `whitelist`

    #### `Blocks`

    -   **Description:**

        This setting specifies the list of blocks which either should not should not be dug out.

        **The list of block types can be found [here](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Mapping/Material.cs).**

    -   **Format:** `[ "<block type>", "<block type>", ...]`

    -   **Type:** `array of strings`

    -   **Example:** `Blocks = [ "DiamondOre", "RedstoneOre", "EmeraldOre", "RedstoneBlock" ]`

    -   **Default:** `[ "Cobblestone", "Stone", ]`

## Auto Drop

-   **Description:**

    Automatically drop items you don't need from the inventory.

    > **ℹ️ NOTE: You need to have [inventoryhandling](configuration.md#inventoryhandling) enabled in order for this bot to work**

-   **Settings:**

    **Section:** **`ChatBot.AutoDrop`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Drop Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Mode`

    -   **Description:**

        This setting specifies the mode of the auto dropping.

        Available values:

        -   `include`

            This mode will drop any items specified in the list in the `Items` setting.

        -   `exclude`

            This mode will drop any other items than specified in the list in the `Items` setting.

            So it would keep the items specified in the list.

        -   `everything`

            Drop any item regardless of the items listed in the `Items` setting.

    -   **Type:** `string`

    -   **Default:** `include`

    #### `Items`

    -   **Description:**

        This setting is where you can specify the list of items which you want to drop, or keep.

        > **ℹ️ NOTE: All item types can be found [here](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/Inventory/ItemType.cs).**

    -   **Format:** `[ "<item type>", "<item type>", ...]`

    -   **Type:** `array of strings`

    -   **Example:** `[ "Totem", "GlassBottle", ]`

    -   **Default:** `[ "Cobblestone", "Dirt", ]`

## Auto Eat

-   **Description:**

    Automatically eat food when your Hunger value is low.

    > **ℹ️ NOTE: You need to have [inventoryhandling](configuration.md#inventoryhandling) enabled in order for this bot to work**

-   **Settings:**

    **Section:** **`ChatBot.AutoEat`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Eat Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Threshold`

    -   **Description:**

        Threshold bellow which the bot will auto eat.

    -   **Type:** `integer`

    -   **Default:** `6`

## Auto Fishing

-   **Description:**

    Automatically catch fish using a fishing rod.

    > **ℹ️ NOTE: You need to have [entityhandling](configuration.md#entityhandling) enabled in order for this bot to work.**

    > **ℹ️ NOTE: To use the automatic rod switching and durability check feature, you need to enable [inventoryhandling](configuration.md#inventoryhandling).**

    > **ℹ️ NOTE: Note: To adjust the position or angle after catching a fish, you need to enable [terrainandmovements](configuration.md#terrainandmovements).**

    > **ℹ️ NOTE: A fishing rod with **Mending enchantment** is strongly recommended.**

    **Steps for using this bot (with the default setting)**

    1. Hold a fishing rod and aim towards the sea before login with MCC
    2. Make sure `AutoFish` is `enabled` in config file
    3. Login with MCC
    4. You will be able to see the log "Fishing will start in 3.0 second(s).".

-   **Settings:**

    **Section:** **`ChatBot.AutoFishing`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Fishing Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Antidespawn`

    -   **Description:**

        This option may be used in some special cases, so if it has not been modified before, leave the default value.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Mainhand`

    -   **Description:**

        Whether to use the main hand or off hand to hold the rod.

    -   **Available values:**

        -   `true` (Main Hand)
        -   `false` (Off Hand)

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `Auto_Start`

    -   **Description:**

        Whether to start fishing automatically after joining the game or switching worlds.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `Cast_Delay`

    -   **Description:**

        Wait how many seconds after successfully catching a fish before recasting the rod.

    -   **Type:** `float`

    -   **Default:** `0.4`

    #### `Fishing_Delay`

    -   **Description:**

        Effective only when `auto_start = true`.

        After joining the game or switching worlds, wait how many seconds before starting to fish automatically.

    -   **Type:** `float`

    -   **Default:** `3.0`

    #### `Fishing_Timeout`

    -   **Description:**

        How long the fish bite is not detected is considered a timeout. It will re-cast after the timeout.

    -   **Type:** `float`

    -   **Default:** `300.0`

    #### `Durability_Limit`

    -   **Description:**

        Will not use rods with less durability than this (full durability is 64).

        Set to zero to disable this feature.

        **Type/Available values:** An integer number from `0` to `64`.

    -   **Default:** `2`

    #### `Auto_Rod_Switch`

    -   **Description:**

        Switch to a new rod from inventory after the current rod is unavailable.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `Stationary_Threshold`

    -   **Description:**

        For each movement of the fishhook entity (entity movement packet), if the distance on both X and Z axes is below this threshold it will be considered as stationary.

        This is to avoid being detected as a bite during the casting of the hook.

        **If set too high, it will cause the rod to be reeled in while casting.**

        **If set too low, it will result in not detecting a bite.**

    -   **Type:** `float`

    -   **Default:** `0.001`

    #### `Hook_Threshold`

    -   **Description:**

        For each movement of the fishhook entity (entity movement packet), if it is stationary (check `stationary_threshold`) and its movement on the Y-axis is greater than this threshold, it will be considered to have caught a fish.

        If it is set too high, it will cause normal bites to be ignored.

        If set too low, it can cause small fluctuations in the hook to be recognized as bites.

    -   **Type:** `float`

    -   **Default:** `0.2`

    #### `Log_Fish_Bobber`

    -   **Description:**

        When turned on it will be print a log every time a fishhook entity movement packet is received.

        If auto-fishing does not work as expected, turn this option on to adjust `stationary_threshold` and `hook_threshold`, or create an issue and attach these logs.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Enable_Move`

    -   **Description:**

        Some plugins do not allow the player to fish in one place for a long time. This setting allows the player to change position/angle after each catch.

        Each position is added as a new `[[ChatBot.AutoFishing.Movements]]` subsection, more on that bellow.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    ### Adding a position/movement

    Each position/movement is added as a new `[[ChatBot.AutoFishing.Movements]]` subsection of `[ChatBot.AutoFishing]`.

    **Avaliable settings/options:**

    -   `XYZ`

        This setting specifies at location the bot should move to.

        The type of this setting is `inline table`, that has the following sub-settings/options:

        -   `x` - X coordinate, the type is `double` (eg. `123.0`)

        -   `y` - Y coordinate, the type is `double` (eg. `64.0`)

        -   `z` - Z coordinate, the type is `double` (eg. `-654.0`)

        **Example**:

        ```toml
        XYZ = { x = 123.0, y = 64.0, z = -654.0 }
        ```

    -   `facing`

        This setting specifies at which angle the bot will look at when he arrives to this position/location.

        The type of this setting is `inline table`, that has the following sub-settings/options:

        -   `yaw` - The type is `double` (eg. `12.34`)

        -   `pitch` - The type is `double` (eg. `-23.45`)

        **Example**:

        ```toml
        facing = { yaw = 12.34, pitch = -23.45 }
        ```

    #### Full example

    ```toml
    [[ChatBot.AutoFishing.Movements]]
    facing = { yaw = 12.34, pitch = -23.45 }

    [[ChatBot.AutoFishing.Movements]]
    XYZ = { x = 123.45, y = 64.0, z = -654.32 }
    facing = { yaw = -25.14, pitch = 36.25 }
    ```

## Auto Relog

-   **Description:**

    Make MCC automatically relog when disconnected by the server, for example because the server is restating.

-   **Settings:**

    **Section:** **`ChatBot.AutoRelog`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Relog Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Delay`

    -   **Description:**

        The delay time before joining the server.

        If the `min` and `max` are the same, the time will be consistent, however, if you want a random time, you can set `min` and `max` to different values to get a random time.
        The time format is in seconds, and the type is double. (eg. `37.0`)

    -   **Format:** `{ min = <seconds (double)>, max = <seconds (double)> }`

    -   **Type:** `inline table`

    -   **Example:** `{ min = 8.0, max = 60.0 }`

    -   **Default:** `{ min = 3.0, max = 3.0 }`

    #### `Retries`

    -   **Description:**

        Number of retries.

        Use `-1` for infinite retries.

        > **ℹ️ NOTE: This might get you banned by the server owners.**

    -   **Default:** `-1`

    #### `Ignore_Kick_Message`

    -   **Description:**

        This settings specifies if the `Kick_Messages` setting will be ignored, if set to `true` it will auto relog regardless of the kick messages.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Kick_Messages`

    -   **Description:**

        A list of words which should trigger the Auto Reconnect Chat Bot.

    -   **Format:** `[ "<keyword>", "<keyword>", ... ]`

    -   **Type:** `array of strings`

    -   **Default:** `[ "Connection has been lost", "Server is restarting", "Server is full", "Too Many people", ]`

## Auto Respond

-   **Description:**

    Run commands or send messages automatically when a specified pattern is detected in the chat.

    > **⚠️ WARNING: Server admins can spoof PMs (`/tellraw`, `/nick`) so enable `AutoRespond` only if you trust server admins.**

    > **⚠️ WARNING: This bot may get spammy depending on your rules, although the global [messagecooldown](configuration.md#messagecooldown) setting can help you avoiding accidental spam.**

-   **Settings:**

    **Section:** **`ChatBot.AutoRespond`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Respond Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Matches_File`

    -   **Description:**

        This setting specifies the path to the file which contains the list of rules for detecting of keywords and responding on them.

        To find out how to configure the rules, take a look at the [`sample-matches.ini`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-matches.ini) which has very detailed examples and a lot of comments.

        _PS: In the future we will document the rules here with examples too._

        > **ℹ️ NOTE: This file is not created by default, we recommend making a clone of the [`sample-matches.ini`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-matches.ini) and changing it according to your needs.**

    -   **Type:** `string`

    -   **Default:** `matches.ini`

    #### `Match_Colors`

    -   **Description:**

        This setting specifies if the Auto Respond Chat Bot should keep the color formatting send by the server.

        You can use this when you need to match text by colors.

        List of all color codes: [here](https://minecraft.tools/en/color-code.php)

        > **ℹ️ NOTE: This feature uses the `§` symbol for color matching**

    -   **Type:** `boolean`

    -   **Default:** `true`

## Chat Log

-   **Description:**

    Make MCC log chat messages into a file.

-   **Settings:**

    **Section:** **`ChatBot.ChatLog`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Chat Log Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Default:** `false`

    #### `Add_DateTime`

    -   **Description:**

        This setting specifies if the Chat Log should prepend timestamps to the logged messages.

    -   **Available values:** `true` and `false`.

    -   **Default:** `true`

    #### `Log_File`

    -   **Description:**

        This setting specifies the name of the Chat Log file that will be created.

    -   **Default:** `chatlog-%username%-%serverip%.txt`

    #### `Filter`

    -   **Description:**

        Type of messages to be logged into the file.

        Available values:

        -   `all`

            All text from the console

        -   `messages`

            All messages, including system, plugin channel, player and server.

        -   `chat`

            Only chat messages.

        -   `private`

            Only private messages.

        -   `internal`

            Only internal messages and commands.

    -   **Default:** `messages`

## Follow player

-   **Description:**

    This bot enables you to make a bot follow a specific player.

    > **ℹ️ NOTE: The bot can be slow at times, you need to walk with a normal speed and to sometimes stop for it to be able to keep up with you, it's similar to making animals follow you when you're holding food in your hand. This is due to a slow pathfinding algorithm, we're working on getting a better one. You can tweak the update limit and find what works best for you. (NOTE: Do not but a very low one, because you might achieve the opposite, this might clog the thread for terrain handling) and thus slow the bot even more.**

    > **ℹ️ NOTE: You need to have [terrainandmovements](configuration.md#terrainandmovements) and [entityhandling](configuration.md#entityhandling) enabled in order for this bot to work.**

-   **Settings:**

    **Section:** **`ChatBot.FollowPlayer`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Follow Player Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Update_Limit`

    -   **Description:**

        The rate at which the bot does calculations (second).

        You can tweak this if you feel the bot is too slow.

    -   **Type:** `float`

    -   **Default:** `1.5`

    #### `Stop_At_Distance`

    -   **Description:**

        Do not follow the player if he is in the range of `X` blocks (prevents the bot from pushing a player in an infinite loop).

    -   **Type:** `float`

    -   **Default:** `3.0`

## Hangman

-   **Description:**

    Hangman game is one of the first bots ever written for MCC, to demonstrate ChatBot capabilities.

    Create a file with words to guess (examples: [`words-en.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/hangman-en.txt), [`words-fr.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/hangman-fr.txt)) and set it in config inside the `[Hangman]` section.

    Also set `enabled` to `true`, then, add your username in the `botowners` INI setting, and finally, connect to the server and use `/tell <bot username> start` to start the game.

    > **ℹ️ NOTE: If the bot does not respond to bot owners, see the [Detecting chat messages](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#detecting-chat-messages) section.**

-   **Settings:**

    **Section:** **`ChatBot.HangmanGame`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Hangman Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Default:** `false`

    #### `English`

    -   **Description:**

        This setting specifies if the Hangman Chat Bot should use English.

    -   **Available values:** `true` and `false`.

    -   **Default:** `true`

    #### `FileWords_EN`

    -   **Description:**

        This setting specifies the path to the file which Hangman will use for the list of words, each word is added on a separate line.

        > **ℹ️ NOTE: This settings file is for English and is not created by the default**

    -   **Default:** `hangman-en.txt`
    -   **Example**: [`words-en.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/hangman-en.txt)

    #### `FileWords_FR`

    -   **Description:**

        This setting is same as the above but for French.

        > **ℹ️ NOTE: This settings file is for French and is not created by the default**

    -   **Default:** `hangman-fr.txt`
    -   **Example**: [`words-fr.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/hangman-fr.txt)

## Mailer

-   **Description:**

    Relay messages between players and servers, like a mail plugin.

    This bot can store messages when the recipients are offline, and send them when they join the server.

    The Mailer bot can store and relay mails much like Essential's `/mail` command.

    -   `/tell <Bot> mail [RECIPIENT] [MESSAGE]`: Save your message for future delivery
    -   `/tell <Bot> tellonym [RECIPIENT] [MESSAGE]`: Same, but the recipient will receive an anonymous mail

    The bot will automatically deliver the mail when the recipient is online.
    The bot also offers a /mailer command from the MCC command prompt:

    -   `/mailer getmails`

        Show all mails in the console.

    -   `/mailer addignored [NAME]`

        Prevent a specific player from sending mails.

    -   `/mailer removeignored [NAME]`

        Lift the mailer restriction for this player.

    -   `/mailer getignored`

        Show all ignored players.

    > **⚠️WARNING: The bot identifies players by their name (Not by UUID!). A nickname plugin or a Minecraft rename may cause mails going to the wrong player! Never write something to the bot you wouldn't say in the normal chat (You have been warned!).**

    > **⚠️WARNING: Server admins can spoof PMs (`/tellraw`, `/nick`) so enable `RemoteControl` only if you trust server admins.**

    **Mailer Network:**

    -   The Mailer bot can relay messages between servers.

    -   To set up a network of two or more bots, launch several instances with the bot activated and the same database.

    -   If you launch two instances from one .exe they should synchronize automatically to the same file.

*   **Settings:**

    **Section:** **`ChatBot.Mailer`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Mailer Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `DatabaseFile`

    -   **Description:**

        This setting specifies the path to the file where the Mailer Chat Bot will store the mails.

        This file will be auto created by the Mailer Chat Bot.

    -   **Default:** `MailerDatabase.ini`

    #### `IgnoreListFile`

    -   **Description:**

        This setting specifies the path to the file where the Mailer Chat Bot will load people who are to be ignored by the Chat Bot.
        If you want to prevent someone from using this chat bot, add him in this file by writing his nickname on a new line.

        This file will be auto created by the Mailer Chat Bot.

    -   **Default:** `MailerIgnoreList.ini`

    #### `PublicInteractions`

    -   **Description:**

        This setting specifies if the Mailer Chat Bot should be interacted with in the public chat (in addition to private messages).

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `MaxMailsPerPlayer`

    -   **Description:**

        This setting specifies how many mails the Mailer Chat Bot should store per player at maximum.

    -   **Type:** `integer`

    -   **Default:** `10`

    #### `MaxDatabaseSize`

    -   **Description:**

        This setting specifies the maximum database file size of Mailer Chat Bot in Kilobytes.

    -   **Type:** `integer`

    -   **Default:** `10000` (10 MB)

    #### `MailRetentionDays`

    -   **Description:**

        This setting specifies how long should the Mailer Chat Bot save/store messages for (in days).

    -   **Type:** `integer`

    -   **Default:** `30`

## Map

-   **Description:**

    This Chat Bot allows you to render items maps into `.jpg` images.

    This is useful for solving captchas on servers which require it, or saving the map art into an image.

    The maps are **rendered** into `Rendered_Maps` folder.

    > **⚠️WARNING: This bot has only been tested on Windows 10, it may not work on Linux or Mac OS due to .NET BitMap API. We're looking forward to swap the underlaying Bitmap API dependency with a library.**

-   **Commands:**

    When enabled will add the `/maps` command.

    **Usage**:

    ```
    /maps <list/render <id>> | maps <l/r <id>>
    ```

-   **Settings:**

    **Section:** **`ChatBot.Map`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Map Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Should_Resize`

    -   **Description:**

        This setting specifies if the Map Chat Bot should resize the image.

        The default map size is `128x128`.

        > **ℹ️ NOTE: The bigger the size, the less is the quality.**

        > **ℹ️ NOTE: For upscaling your maps you could use (getting a bit better quality): https://deepai.org/machine-learning-model/torch-srgan**

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Resize_To`

    -   **Description:**

        Which size the map should be resized to if `Should_Resize` is `true`.

    -   **Type:** `integer`

    -   **Default:** `256`

    #### `Auto_Render_On_Update`

    -   **Description:**

        This setting specifies if the Map Chat Bot should automatically render maps as they're received from the servers.

        > **⚠️WARNING: On some versions older than 1.17 this could cause some performance issue on older hardware if there a lot of maps being rendered, since map updates are sent multiple times a second. Be careful.**

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Delete_All_On_Unload`

    -   **Description:**

        This setting specifies if the Map Chat Bot should automatically delete rendered maps when un-loaded or reloaded.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `Notify_On_First_Update`

    -   **Description:**

        This setting specifies if the Map Chat Bot should notify you when it got a map from the server for the first time.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

## PlayerList Logger
-   **Description:**

    Log the list of players periodically into a textual file.

-   **Settings:**

    **Section:** **`ChatBot.PlayerListLogger`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the PlayerList Logger Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Default:** `false`

    #### `File`

    -   **Description:**

        This setting specifies the name of the player list Log file that will be created.

    -   **Default:** `playerlog.txt`

    #### `Delay`

    -   **Description:**

        Save the list of players every how many seconds.

    -   **Type:** `float`

    -   **Default:** `60.0`

## Remote Control

-   **Description:**

    Send MCC console commands to your bot through server PMs (`/tell`).

    You need to have [ChatFormat](configuration.md#chat-format) working correctly and add yourself in [botowners](configuration.md#botowners) to use the bot.

    > **⚠️WARNING: Server admins can spoof PMs (`/tellraw`, `/nick`) so enable `RemoteControl` only if you trust server admins.**

-   **Settings:**

    **Section:** **`ChatBot.RemoteControl`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Remote Control Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `AutoTpaccept`

    -   **Description:**

        This setting specifies if the Remote Control Chat Bot should automatically accept teleport requests.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `AutoTpaccept_Everyone`

    -   **Description:**

        This setting specifies if the Remote Control Chat Bot should automatically accept teleport requests from everyone.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

## Replay Capture

-   **Description:**

    Enable recording of the game (`/replay start`) and replay it later using the Replay Mod (https://www.replaymod.com/).

    > **⚠️ IMPORTANT: This bot does not work for 1.19, we need maintainers for it.**

    > **ℹ️ NOTE: Please note that due to technical limitations, the client player (you) will not be shown in the replay file**

    > **⚠️ WARNING: You SHOULD use `/replay stop` or exit the program gracefully with `/quit` OR THE REPLAY FILE MAY GET CORRUPT!**

-   **Settings:**

    **Section:** **`ChatBot.ReplayCapture`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Replay Mod Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Backup_Interval`

    -   **Description:**

        This setting specifies the time interval in seconds when the replay file should be auto-saved.

        Use `-1` to disable.

    -   **Type:** `float`

    -   **Default:** `300.0`

## Script Scheduler

-   **Description:**

    Schedule commands and scripts to launch on various events such as server join, date/time or time interval.

-   **Settings:**

    **Section:** **`ChatBot.ScriptScheduler`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Script Scheduler Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    ### Defining a task

    -   **Description:**

        Each task is defined as a new subsection `[[ChatBot.ScriptScheduler.TaskList]]` of the section: `[ChatBot.ScriptScheduler]`.

        **Subsection format:**

        ```toml
        [[ChatBot.ScriptScheduler.TaskList]]
        <setting> = <value>
        <setting> = <value>
        ```

        > **ℹ️ NOTE: It is recommended that you align subsections to the right by one tab or 4 spaces for better readability.**

        **Avaliable settings/options:**

        -   `Trigger_On_First_Login`

            Will trigger the task when you login the first time.

            **Available values**: `true` and `false`

            **Type**: `boolean`

        -   `Trigger_On_Login`

            Will trigger the task each time you login.

            **Available values**: `true` and `false`

            **Type**: `boolean`

        -   `Trigger_On_Times`

            This will enable the task to trigger at exact time(s) you want.

            The type of this setting is `inline table`, that has the following sub-settings/options:

            -   `Enable` - Enables/Disables the setting (Boolean, so either `true` or `false`)

            -   `Times` - An array/list of times on which the task should run/trigger (each element is of the [Local Time](https://toml.io/en/v1.0.0#local-time) type, eg. `14:00:00`, so: `hours:minutes:seconds`)

            **Example**:

            ```toml
            Trigger_On_Times = { Enable = true, Times = [ 14:00:00, 22:35:8] }
            ```

        -   `Trigger_On_Interval`

            This will enable the task to trigger at certain interval which you've defined.

            The type of this setting is `inline table`, that has the following sub-settings/options:

            -   `Enable` - Enables/Disables the setting (Boolean, so either `true` or `false`)

            -   `MinTime` - Time in seconds (the type is `double`, eg. `3.14`)

            -   `MaxTime` - Time in seconds (the type is `double`, eg. `3.14`)

            **If `MinTime` and `MaxTime` are the same, the interval will be consistent, however if they are not, the ChatBot will generate a random interval in between those two numbers provided, each time the task is run.**

            **Example**:

            ```toml
            Trigger_On_Interval = { Enable = true, MinTime = 30.0, MaxTime = 160.0 }
            ```

    ### Full example

    ```toml
    [ChatBot.ScriptScheduler]
    Enabled = true

        [[ChatBot.ScriptScheduler.TaskList]]
        Task_Name = "Task Name 1"
        Trigger_On_First_Login = false
        Trigger_On_Login = false
        Trigger_On_Times = { Enable = true, Times = [ 14:00:00, ] }
        Trigger_On_Interval = { Enable = true, MinTime = 3.6, MaxTime = 4.8 }
        Action = "send /hello"

        [[ChatBot.ScriptScheduler.TaskList]]
        Task_Name = "Task Name 2"
        Trigger_On_First_Login = false
        Trigger_On_Login = true
        Trigger_On_Times = { Enable = false, Times = [ ] }
        Trigger_On_Interval = { Enable = false, MinTime = 1.0, MaxTime = 10.0 }
        Action = "send /login pass"
    ```
