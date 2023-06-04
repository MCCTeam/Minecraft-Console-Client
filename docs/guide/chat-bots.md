---
title: Chat Bots
redirectFrom:
    - "/g/bots/index.html"
    - "/g/bots.html"
---

# Chat Bots

-   [About](#about)
-   [List of built-in Chat Bots](#list-of-built-in-chat-bots)
-   [Creating your own](creating-bots.md)

## About

**Minecraft Console Client** has a number of default built in Chat Bots (Scripts/Plugins) which allow for various types of automation.

<div class="custom-container warning"><p class="custom-container-title">Warning</p>

**Recently we have changed the configuration format from INI to TOML, this part of the documentation has only been partially updated, it's work in progress, for the time being please refer to the `MinecraftClient.ini` for setting names, the descriptions and options should be up to date in most cases, but not guaranteed.**

</div>

<div class="custom-container tip"><p class="custom-container-title">Tip</p>

**Settings refer to settings in the [configuration file](configuration.md)**

</div>

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
-   [Discord Bridge](#discord-bridge)
-   [Farmer](#farmer)
-   [Follow Player](#follow-player)
-   [Hangman](#hangman)
-   [Mailer](#mailer)
-   [Map](#map)
-   [PlayerList Logger](#playerlist-logger)
-   [Remote Control](#remote-control)
-   [Replay Mod](#replay-mod)
-   [Script Scheduler](#script-scheduler)
-   [Telegram Bridge](#telegram-bridge)
-   [Items Collector](#items-collector)
-   [WebSocket](#websocket-chat-bot)

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

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **This might not work depending on your system or a console (terminal emulator).**

    </div>

    -   **Description:**

        This setting specifies if you want to hear a beep when you get an alert.

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

        If the `min` and `max` are the same, the time interval will be consistent. However if they are not the same, the plugin will choose a random number between `min` and `max`, this is useful if you want to have a random interval to trick anti afk plugins.

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

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **You need to enable [Terrain Handling](configuration.md#terrainandmovements) in the settings and it's recommended to put the bot into an enclosure not to wander off. (Recommended size 5x5x5)**

    </div>

    -   **Description:**

        Should the bot use [Terrain Handling](configuration.md#terrainandmovements) instead of the command method.

        This will enable your bot to randomly move about, thus a better anti afk effect.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Walk_Range`

    -   **Description:**

        The range which bot will use to walk around (-X to +X and -Z to +Z, Y is not used).

        The bigger the slower the bot might be at calculating the path, recommended 2-5.

    -   **Default:** `5`

    #### `Walk_Retries`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **This happens on each trigger of the task, so it does not permanently switch to alternative method.**

    </div>

    -   **Description:**

        This is the number of times the bot will try to pathfind, if he can't find a valid path for 20 times, he will use the command method.

    -   **Default:** `20`

## Auto Attack

<div class="custom-container tip"><p class="custom-container-title">Tip</p>

**You need to have [inventoryhandling](configuration.md#inventoryhandling) and [entityhandling](configuration.md#entityhandling) enabled in order for this bot to work.**

</div>

-   **Description:**

    Automatically attacks mobs around you, you can configure it to attack both hostile and passive mobs and only certain mobs or all mobs.

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

        You can find the full list of mobs [here](https://mccteam.github.io/r/entity/#L15).

    -   **Format:** `["<entity type>", "<entity type>", ...]`

    -   **Type:** `array of strings`

    -   **Example:** `[ "Spider", "Skeleton", "Pig", ]`

    -   **Default:** `[ "Zombie", "Cow", ]`

## Auto Craft

<div class="custom-container tip"><p class="custom-container-title">Tip</p>

**You need to have [inventoryhandling](configuration.md#inventoryhandling) enabled in order for basic crafting in the inventory to work, in addition if you want to use a crafting table, you need to enable [terrainandmovements](configuration.md#terrainandmovements) in order for bot to be able to reach the crafting table.**

</div>

-   **Description:**

    Automatically craft items in your inventory or in a crafting table.

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
    
    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **If you're using `table` you need to set the `CraftingTable` setting.**

    </div>

    The recipes are defines as a separate new sub-section `[[ChatBot.AutoCraft.Recipes]]` of the `[ChatBot.AutoCraft]` section.

    The `[[ChatBot.AutoCraft.Recipes]]` section needs to contain the following settings:

    -   `Name`

        The name of your recipe, can be whatever you like.

        **Type**: `string`

    -   `Type`

        **Available values:** `player` and `table`

    -   `Result`

        This is the type of resulting item.

        **Type:** `string`

        **Example:** `"StoneBricks"`

    -   `Slots`

        This setting is an array/list of material names (strings) that go into an each slot (max 9 elements). Empty slots should be marked with `"Null"`

        **Type:** `array of strings`

        **Format:**

        ```toml
        Slots = [ "<material/item type>", "<material/item type>", ... ]
        ```


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

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **If you have a case where you have to leave some fields empty, use `"Null"` to mark them as empty. Example for stone bricks: `Slots = [ "Stone", "Stone", "Null", "Stone", "Stone", "Null", "Null", "Null", "Null", ]`**

    **All item types can be found [here](https://mccteam.github.io/r/item/#L12).**

    **Make sure to provide materials for your bot by placing them in inventory first.**

    </div>

## Auto Dig

-   **Description:**

    Automatically digs block on specified locations.

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **You need to have [inventoryhandling](configuration.md#inventoryhandling) and [terrainandmovements](configuration.md#terrainandmovements) enabled in order for this bot to work.**

    </div>

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Since MCC does not yet support accurate calculation of the collision volume of blocks, all blocks are considered as complete cubes when obtaining the position of the lookahead.**

    </div>

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

        **The list of block types can be found [here](https://mccteam.github.io/r/block/#L15).**

    -   **Format:** `[ "<block type>", "<block type>", ...]`

    -   **Type:** `array of strings`

    -   **Example:** `Blocks = [ "DiamondOre", "RedstoneOre", "EmeraldOre", "RedstoneBlock" ]`

    -   **Default:** `[ "Cobblestone", "Stone", ]`

## Auto Drop

-   **Description:**

    Automatically drop items you don't need from the inventory.

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **You need to have [inventoryhandling](configuration.md#inventoryhandling) enabled in order for this bot to work**

    </div>

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

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **All item types can be found [here](https://mccteam.github.io/r/item/#L12).**

    </div>

    -   **Description:**

        This setting is where you can specify the list of items which you want to drop, or keep.


    -   **Format:** `[ "<item type>", "<item type>", ...]`

    -   **Type:** `array of strings`

    -   **Example:** `[ "Totem", "GlassBottle", ]`

    -   **Default:** `[ "Cobblestone", "Dirt", ]`

## Auto Eat

-   **Description:**

    Automatically eat food when your Hunger value is low.

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **You need to have [inventoryhandling](configuration.md#inventoryhandling) enabled in order for this bot to work**

    </div>

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

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **You need to have [entityhandling](configuration.md#entityhandling) enabled in order for this bot to work.**

    </div>

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **To use the automatic rod switching and durability check feature, you need to enable [inventoryhandling](configuration.md#inventoryhandling).**

    </div>

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Note: To adjust the position or angle after catching a fish, you need to enable [terrainandmovements](configuration.md#terrainandmovements).**

    </div>

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **A fishing rod with **Mending enchantment** is strongly recommended.**

    </div>

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

    **Available settings/options:**

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

        If the `min` and `max` are the same, the time will be consistent, however, if you want a random time, you can set `min` and `max` to different values to get a random time. The time format is in seconds, and the type is double. (eg. `37.0`)

    -   **Format:** `{ min = <seconds (double)>, max = <seconds (double)> }`

    -   **Type:** `inline table`

    -   **Example:** `{ min = 8.0, max = 60.0 }`

    -   **Default:** `{ min = 3.0, max = 3.0 }`

    #### `Retries`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **This might get you banned by the server owners.**

    </div>

    -   **Description:**

        Number of retries.

        Use `-1` for infinite retries.

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

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **Server admins can spoof PMs (`/tellraw`, `/nick`) so enable `AutoRespond` only if you trust server admins.**

    </div>

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **This bot may get spammy depending on your rules, although the global [messagecooldown](configuration.md#messagecooldown) setting can help you avoiding accidental spam.**

    </div>

-   **Settings:**

    **Section:** **`ChatBot.AutoRespond`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Auto Respond Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Matches_File`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **This file is not created by default, we recommend making a clone of the [`sample-matches.ini`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-matches.ini) and changing it according to your needs.**

    </div>

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **If you want to use variables from this chat bot in scripts, currently that does not work. You will have to use a C# script in that case. We are working on getting this functionality back.**

    </div>

    -   **Description:**

        This setting specifies the path to the file which contains the list of rules for detecting of keywords and responding on them.

        To find out how to configure the rules, take a look at the [`sample-matches.ini`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/sample-matches.ini) which has very detailed examples and a lot of comments.

        _PS: In the future we will document the rules here with examples too._

    -   **Type:** `string`

    -   **Default:** `matches.ini`

    #### `Match_Colors`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **This feature uses the `§` symbol for color matching**

    </div>

    -   **Description:**

        This setting specifies if the Auto Respond Chat Bot should keep the color formatting send by the server.

        You can use this when you need to match text by colors.

        List of all color codes: [here](https://minecraft.tools/en/color-code.php)

    -   **Type:** `boolean`

    -   **Default:** `false`

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

## Discord Bridge

-   **Description:**

    This Chat Bot allows you to send and receive messages and MCC commands via a Discord channel.

-   **Setup:**

    In order for this to work you must create a Discord bot on the [Discord Developers portal](https://discord.com/developers/applications/).

    First go to [Discord Developers portal](https://discord.com/developers/applications/), click on **New Application**, fill out the name of your bot and confirm the terms of service and click **Create**.

    ![Image](/images/guide/Discord_Create_Application.png)

    Copy the **Application ID** and save it somewhere.

    Click on the **Bot** tab in the left menu.

    Click on **Add Bot**

    ![Image](/images/guide/Discord_Add_Bot.png)

    Click on the **Reset Token** button and copy the generated token, then paste it in the `Token` field in the MCC configuration.

    Enable `Message Content Intent`, `Server Members Intent` and `Presence Intent`.

    ![Image](/images/guide/Discord_Reset_Token.png)
    ![Image](https://i.pics.rs/AAhyx.png)

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **Token is what gives you access to the Bot, do not share it with anyone and keep it safe!**

    </div>

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **You must Enable `Message Content Intent`, `Server Members Intent` and `Presence Intent` for the bot to work!**

    </div>

    Then go to [Discord Permissions Calculator](https://discordapi.com/permissions.html).
    Paste the **Application Id** that you've copied into the **Client ID** field, then Check/Enable the **Administrator** field in General Permissions section.
    Finally click on the **Link** down bellow and invite the Bot on to a server you want to interact with the MCC on.

    ![Image](/images/guide/Discord_Permissions.png)

    Go to your Discord Client and go to **Settings -> Advanced**, Enable **Developer Mode**.

    Then **right click** on a server where you invited the bot to in the server list and click on **Copy ID**, paste the copied id in `GuildId` in your MCC configuration.

    Then **right click** on a channel where you want to interact with the bot and click on **Copy ID**, paste the copied id in `ChannelId` in your MCC configuration.

    Send a message in that channel and **right click** on your nick and click **Copy ID** and paste the copied id in `OwnersIds` list setting in your MCC configuration.

    Enable the bot by setting `Enabled` to `true` in your MCC configuration and start the MCC.

-   **Usage:**

    To send a message simply type it out in the Discord channel and press enter.

    To execute a MCC command, you must prefix it with a dot (`.`).
    Example: `.move 145 64 832`

-   **Settings:**

    **Section:** **`ChatBot.DiscordBrdige`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Discord Bridge Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Token`

    -   **Description:**

        This is the token of your Discord bot.

    -   **Type:** `string`

    #### `GuildId`

    -   **Description:**

        This is the ID of your server/guild where you have invited the bot to.

    -   **Type:** `unsigned long`

    #### `ChannelId`

    -   **Description:**

        This is the ID of a channel on your server/guild where you want to interact with the bot.

    -   **Type:** `unsigned long`

    #### `OwnersIds`

    -   **Description:**

        This is a list of Discord user IDs which can interact with the bot.

    -   **Type:** `list/array of: unsigned long`

    #### `PrivateMessageFormat`

    -   **Description:**

        This is a format that will be used when someone has sent you a private message on the server.

        Parts of the message that are between `{` and `}` will be replaced by the Chat Bot during runtime, you should not change them in any way!

        For example `{message}` will be replaced with an actual message, `{username}` will be replaced with the username of the person who sent a message on the server and `{timestamp}` will be replaced with the current date and time.

        For Discord message formatting/styling, refer to [this guide](https://www.writebots.com/discord-text-formatting/).

    -   **Type:** `string`

    -   **Default:** `**[Private Message]** {username}: {message}`

    #### `PublicMessageFormat`

    -   **Description:**

        This is a format that will be used when sending a public message to the Discord channel.

        Parts of the message that are between `{` and `}` will be replaced by the Chat Bot during runtime, you should not change them in any way!

        For example `{message}` will be replaced with an actual message, `{username}` will be replaced with the username of the person who sent a message on the server and `{timestamp}` will be replaced with the current date and time.

        For Discord message formatting/styling, refer to [this guide](https://www.writebots.com/discord-text-formatting/).

    -   **Type:** `string`

    -   **Default:** `{username}: {message}`

    #### `TeleportRequestMessageFormat`

    -   **Description:**

        This is a format that will be used when someone has sent you a Teleport Request.

        Parts of the message that are between `{` and `}` will be replaced by the Chat Bot during runtime, you should not change them in any way!

        For example `{message}` will be replaced with an actual message, `{username}` will be replaced with the username of the person who sent a message on the server and `{timestamp}` will be replaced with the current date and time.

        For Discord message formatting/styling, refer to [this guide](https://www.writebots.com/discord-text-formatting/).

    -   **Type:** `string`

    -   **Default:** `A new Teleport Request from **{username}**!`

## Farmer

<div class="custom-container tip"><p class="custom-container-title">Tip</p>

**You need to have [Terrain And Movements](configuration.md#terrainandmovements) and [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this bot to work.**

</div>

<div class="custom-container warning"><p class="custom-container-title">Warning</p>

**This a newly added bot, it is not perfect and was only tested in 1.19.2, there are some minor issues with it and you should treat it as an experimental bot.**

</div>

-   **Description:**

    This bot can farm crops for you.
    When you start it it will plant, break and bonemeal crops in order.

    Supported crops:

    -   Beetroot
    -   Carrot
    -   Melon
    -   Netherwart
    -   Pumpkin
    -   Potato
    -   Wheat

    **Current list of issues:**

    -   Sometimes the bot will not bone meal carrots/potatoes or melon/pumpkin stems (you will see it in a pattern of crops that have not been bonemealed)
    -   Sometimes the bot can jump on to the crops and break the farmland when coming form a different height, it's advised to keep the farming area flat and fenced off so the items to not fly out of the farming area
    -   If you have a farming platform that is 1 block thick and has air bellow, make it a few blocks thick because the bot can fall through sometimes when logging in and standing on farmland
    -   Sometimes the bot can be kicked for "invalid movement" packets when farming netherwart on soul sand, we haven't been able to figure why this happens in some parts of the world, while on other it's completely fine, it's advised to keep the farming area small and flat.

    _We're working on solving these issues._

    **What the bot does not do as of the time of writing, but are planned features:**

    -   Does not collect items which fly off to the side, (it's advised to fence off the farming area with 2 high wall)
    -   Does not put items to the chest once the inventory is full
    -   Does not warn you when the inventory is full
    -   Does not refill inventory with seeds or bonemeal from chests by it self.

    > **ℹ️ NOTE: The default radius of scanning is `30` blocks, we suggest that you do not use radius too big because it might slow down the bot. The bigger the radius, the slower the scanning and processing is.**

-   **Commands:**

    When enabled will add the `/farmer` command.

    **Usage**:

    ```
    /farmer <start <crop type> [radius:<radius = 30>] [unsafe:<true/false>] [teleport:<true/false>] [debug:<true/false>]|stop>
    ```

    _Options marked with `[` and `]` are optional and in case of this command can have whatever order you prefer after the `<crop type>` field._

    _Options that have `=` means that the value after the `=` is a default value, in case of this command the default radius is 30 blocks._

    **Examples:**

    Farming `wheat` in a radius of `40` blocks.

    ```
    /farmer start wheat radius:40
    ```

    Farming `melon` with debug output and direct teleporting:

    ```
    /farmer start melon debug:true teleport:true
    ```

    Stopping the bot:

    ```
    /farmer stop
    ```

-   **Settings:**

    **Section:** **`ChatBot.Farmer`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Farmer Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Delay_Between_Tasks`

    -   **Description:**

        This setting specifies the delay in seconds between each task performed by the bot.

    -   **Type:** `integer`

    -   **Default:** `1`

    -   **Minimum:** `1`

## Follow player

-   **Description:**

    This bot enables you to make a bot follow a specific player.

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **The bot can be slow at times, you need to walk with a normal speed and to sometimes stop for it to be able to keep up with you, it's similar to making animals follow you when you're holding food in your hand. This is due to a slow pathfinding algorithm, we're working on getting a better one. You can tweak the update limit and find what works best for you. (NOTE: Do not but a very low one, because you might achieve the opposite, this might clog the thread for terrain handling) and thus slow the bot even more.**

    </div>

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **You need to have [terrainandmovements](configuration.md#terrainandmovements) and [entityhandling](configuration.md#entityhandling) enabled in order for this bot to work.**

    </div>

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

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **If the bot does not respond to bot owners, see the [Detecting chat messages](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#detecting-chat-messages) section.**

    </div>

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

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **This settings file is for English and is not created by the default**

    </div>

    -   **Description:**

        This setting specifies the path to the file which Hangman will use for the list of words, each word is added on a separate line.

    -   **Default:** `hangman-en.txt`
    -   **Example**: [`words-en.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/hangman-en.txt)

    #### `FileWords_FR`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **This settings file is for French and is not created by the default**

    </div>

    -   **Description:**

        This setting is same as the above but for French.

    -   **Default:** `hangman-fr.txt`
    -   **Example**: [`words-fr.txt`](https://github.com/MCCTeam/Minecraft-Console-Client/blob/master/MinecraftClient/config/hangman-fr.txt)

## Mailer

-   **Description:**

    Relay messages between players and servers, like a mail plugin.

    This bot can store messages when the recipients are offline, and send them when they join the server.

    The Mailer bot can store and relay mails much like Essential's `/mail` command.

    -   `/tell <Bot> mail [RECIPIENT] [MESSAGE]`: Save your message for future delivery
    -   `/tell <Bot> tellonym [RECIPIENT] [MESSAGE]`: Same, but the recipient will receive an anonymous mail

    The bot will automatically deliver the mail when the recipient is online. The bot also offers a /mailer command from the MCC command prompt:

    -   `/mailer getmails`

        Show all mails in the console.

    -   `/mailer addignored [NAME]`

        Prevent a specific player from sending mails.

    -   `/mailer removeignored [NAME]`

        Lift the mailer restriction for this player.

    -   `/mailer getignored`

        Show all ignored players.

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **The bot identifies players by their name (Not by UUID!). A nickname plugin or a Minecraft rename may cause mails going to the wrong player! Never write something to the bot you wouldn't say in the normal chat (You have been warned!).**

    </div>

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **Server admins can spoof PMs (`/tellraw`, `/nick`) so enable `RemoteControl` only if you trust server admins.**

    </div>

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

        This setting specifies the path to the file where the Mailer Chat Bot will load people who are to be ignored by the Chat Bot. If you want to prevent someone from using this chat bot, add him in this file by writing his nickname on a new line.

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

    This Chat Bot allows you to render items maps in the console, to `.bmp` images and to relay them to Discord using the [Discord Bridge](#discord-bridge) Chat Bot.

    This is useful for solving captchas on servers which require it, or saving the map art into an image.

    The maps are **rendered** into `Rendered_Maps` folder which will be auto created in the same folder where the client executable is located.

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

    #### `Render_In_Console`

    -   **Description:**

        This setting specifies if the Map Chat Bot should render the map in the console.

        It is recommended to use something like Power Shell for the best map quality (at least for Windows users).

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `Save_To_File`

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **If you want the Discord relay feature, you must enable this setting!**

    </div>

    -   **Description:**

        This setting specifies if the Map Chat Bot should render the map and save it into a file (`.bmp` format)

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Auto_Render_On_Update`

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **On some versions older than 1.17 this could cause some performance issue on older hardware if there a lot of maps being rendered, since map updates are sent multiple times a second. Be careful.**

    </div>

    -   **Description:**

        This setting specifies if the Map Chat Bot should automatically render maps as they're received from the servers.

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

    #### `Rasize_Rendered_Image`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **The bigger the size, the less is the quality.**

    </div>

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **For upscaling your maps you could use (getting a bit better quality): https://deepai.org/machine-learning-model/torch-srgan**

    </div>

    -   **Description:**

        This setting specifies if the Map Chat Bot should resize the rendered image (the one that is saved to a file).

        This is useful if you're relying map images to Discord via the [Discord Bridge](#discord-bridge) Chat Bot.

        The default map size is `128x128`.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Resize_To`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Might be a bit slow on less powerful systems when rendering a lot of maps. Lower down the resolution if you have any performance issues. If your system is not that powerful and can't handle it, use external tools for upscaling and resizing.**

    </div>

    -   **Description:**

        Which size the map should be resized to if `Rasize_Rendered_Image` is `true`.


    -   **Type:** `integer`

    -   **Default:** `512`

    #### `Send_Rendered_To_Discord`

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **The [Discord Bridge](#discord-bridge) Chat Bot must be enabled and configured!**

    </div>

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **You need to enable `Save_To_File` in order for this to work.**

    </div>

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Sometimes when the client connects, the [Discord Bridge](#discord-bridge) will be loaded a tiny bit after. Rendered map images are queued up and sent in order as soon as the [Discord Bridge](#discord-bridge) is ready and connected.**

    </div>

    -   **Description:**

        Send a rendered map (saved to a file) to a Discord channel via the [Discord Bridge](#discord-bridge) Chat Bot.


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

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **Server admins can spoof PMs (`/tellraw`, `/nick`) so enable `RemoteControl` only if you trust server admins.**

    </div>

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

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **This bot does not work for 1.19, we need maintainers for it.**

    </div>

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Please note that due to technical limitations, the client player (you) will not be shown in the replay file**

    </div>

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **You SHOULD use `/replay stop` or exit the program gracefully with `/quit` OR THE REPLAY FILE MAY GET CORRUPT!**

    </div>

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

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **It is recommended that you align subsections to the right by one tab or 4 spaces for better readability.**

    </div>

    -   **Description:**

        Each task is defined as a new subsection `[[ChatBot.ScriptScheduler.TaskList]]` of the section: `[ChatBot.ScriptScheduler]`.

        **Subsection format:**

        ```toml
        [[ChatBot.ScriptScheduler.TaskList]]
        <setting> = <value>
        <setting> = <value>
        ```

        **Available settings/options:**

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

## Telegram Bridge

-   **Description:**

    This bot allows you to send and receive messages and commands via a Telegram Bot DM or to receive messages in a Telegram channel.

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **You can't send messages and commands from a group channel, you can only send them in the bot DM, but you can get the messages from the client in a group channel.**

    </div>

-   **Setup:**

    1. First you need to create a Telegram bot and obtain an API key, to do so, go to Telegram and find @botfather
    2. Click on `Start` button and read the bot reply, then type `/newbot`, the Botfather will guide you through the bot creation.
    3. Once you create the bot, copy the **API key** that you have gotten, and put it into the `Token` field of `ChatBot.TelegramBridge` section (this section).
    4. Then launch the client and go to Telegram, find your newly created bot by searching for it with its username, and open a DM with it.
    5. Click on `Start` button and type and send the following command `.chatid` to obtain the chat id. 
    6. Copy the chat id number (eg. `2627844670`) and paste it in the `ChannelId` field and add it to the `Authorized_Chat_Ids` field (in this section) (an id in "Authorized_Chat_Ids" field is a number/long, not a string!), then save the file.
    Now you can use the bot using it's DM.

    <div class="custom-container danger"><p class="custom-container-title">Danger</p>

    **Do not share your API key with anyone else as it will give them the control over your bot. Save it securely.**

    </div>

    <div class="custom-container danger"><p class="custom-container-title">Danger</p>

    **If you do not add the id of your chat DM with the bot to the "Authorized_Chat_Ids" field, ayone who finds your bot via search will be able to execute commands and send messages!**

    </div>

    <div class="custom-container danger"><p class="custom-container-title">Danger</p>

    **An id pasted in to the "Authorized_Chat_Ids" should be a number/long, not a string!**

    </div>

-   **Settings:**

    **Section:** **`ChatBot.TelegramBridge`**

    #### `Enabled`

    -   **Description:**

        This setting specifies if the Telegram Bridge Chat Bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Token`

    -   **Description:**

        Your Telegram Bot token.

    -   **Type:** `string`

    -   **Default:** empty

    #### `ChannelId`

    -   **Description:**

        An ID of a channel where you want to interact with the MCC using the bot.

    -   **Type:** `string`

    -   **Default:** empty

    #### `Authorized_Chat_Ids`

    -   **Description:**

        A list of Chat IDs that are allowed to send messages and execute commands. 
        To get an id of your chat DM with the bot use `.chatid` bot command in Telegram.

    -   **Type:** `array of strings`

    -   **Default:** empty

    #### `Message_Send_Timeout`

    -   **Description:**

        How long to wait (in seconds) if a message can not be sent to Telegram before canceling the task (minimum 1 second).

    -   **Type:** `integer`

    -   **Default:** 3

    **Message Formats**

    Words wrapped with `{` and `}` are going to be replaced during the code execution, do not change them!
    For example, `{message}` is going to be replace with an actual message, `{username}` will be replaced with an username, `{timestamp}` with the current time.
    For Telegram message formatting, check the [following](https://mccteam.github.io/r/tg-fmt.html).

    #### `PrivateMessageFormat`

    -   **Description:**

        A format that is used to display a private chat message on the minecraft server, in a Telegram channel.

    -   **Type:** `string`

    -   **Default:** `*(Private Message)* {username}: {message}`

    #### `PublicMessageFormat`

    -   **Description:**

        A format that is used to display a public chat message on the minecraft server, in a Telegram channel.

    -   **Type:** `string`

    -   **Default:** `{username}: {message}`

    #### `TeleportRequestMessageFormat`

    -   **Description:**

        A format that is used to display a teleport request on the minecraft server, in a Telegram channel.

    -   **Type:** `string`

    -   **Default:** `A new Teleport Request from **{username}**!`


## Items Collector

-   **Description:**

    Collect items on the ground using this Chat Bot.

-   **Settings:**

    **Section:** **`ChatBot.ItemsCollector`**

    #### `Enabled`

    -   **Description:**
  
        This setting specifies if the Items Collector chat bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Collect_All_Item_Types`

    -   **Description:**

        Specifies if the bot will collect all items, regardless of their type. 
        If you want to use the whitelisted item types, disable this by setting it to `false`.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Items_Whitelist`

    -   **Description:**

        In this list you can specify which items the bot will collect. 
        To enable this, set the `Collect_All_Item_Types` to false. 

    <div class="custom-container warning"><p class="custom-container-title">Note</p>

    **This does not prevent the bot from accidentally picking up other items, it only goes to positions where it finds the whitelisted items**

    </div>

    -   **Available values:** [Item Type List](https://raw.githubusercontent.com/MCCTeam/Minecraft-Console-Client/master/MinecraftClient/Inventory/ItemType.cs)

    -   **Type:** `array of strings with item names`

    -   **Default:** `[ "Diamond", "NetheriteIngot" ]`

    #### `Delay_Between_Tasks`

    -   **Description:**

        Delay in milliseconds between bot scanning items (Recommended: 300-500)

    -   **Type:** `integer`

    -   **Default:** `300`

    #### `Collection_Radius`

    -   **Description:**

        The radius of blocks in which bot will look for items to collect.

    -   **Type:** `double`

    -   **Default:** `30.0`

    #### `Always_Return_To_Start`

    -   **Description:**

        Specifies if the bot will return to it's starting position after there are no items to collect.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`

    #### `Prioritize_Clusters`

    -   **Description:**

        Specifies if the bot will go after clustered items instead for the closest ones.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `true`


## WebSocket Chat Bot

-   **Description:**

    This chat bot allows you to remotely execute commands on the MCC and make Chat Bots in other programming languages over Web Socket.

    You can make your own library to do this, or use the reference implementation one which has been writen in TypeScript/JavaScript: [MCC.js](https://github.com/milutinke/MCC.js)

    If you want to write your own library, you can follow this guide on the protocol specification and avaliable events and commands: [WebSocket Chat Bot Guide](websocket/README.md)

-   **Settings:**

    **Section:** **`ChatBot.WebSocketBot`**

    #### `Enabled`

    -   **Description:**
  
        This setting specifies if the Web Socket chat bot is enabled.

    -   **Available values:** `true` and `false`.

    -   **Type:** `boolean`

    -   **Default:** `false`

    #### `Ip`

    -   **Description:**

        The IP address that Websocket server will be bound to.

    -   **Type:** `string`

    -   **Default:** `127.0.0.1` (localhost)

    #### `Port`

    -   **Description:**

        The Port that Websocket server will be bound to.

    -   **Type:** `number`

    -   **Default:** `8043`

    #### `Password`

    -   **Description:**

        A password that will be used to authenticate on thw Websocket server 
        
        **It is recommended to change the default password and to set a strong one**

    -   **Type:** `string`

    -   **Default:** `wspass12345`

    #### `DebugMode`

    -   **Description:**

        This setting is for developers who are developing a library that uses this chat bot to remotely execute procedures/commands/functions.

    -   **Type:** `boolean`

    -   **Default:** `false`