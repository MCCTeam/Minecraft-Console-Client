---
title: Usage
---

# Usage

How to run the program:

- [Running on Windows](#windows)
- [Running on Linux, macOS](#linux-macos)
- [Running using Docker](#docker)

Using the command line parameters:

- [Examples](#quick-usage-of-mcc-with-examples)
- [Command line parameters](#command-line-parameters)
- [Internal commands](#internal-commands)

## Windows

Simply run `MinecraftClient.exe`

<div class="custom-container tip"><p class="custom-container-title">Tip</p>

**On Windows it's best using [Windows Terminal](https://docs.microsoft.com/en-us/windows/terminal/install) for the best experience and looks. Some features like emojis in the [`/chunk`](#chunk) command do not work in CMD or Powershell 5**

</div>

## Linux, macOS

To run the client you need to type the following command in your terminal emulator:

```bash
./MinecraftClient
```

If you want to keep it running in the background you can use `screen` (Linux only)

Example:

```bash
# Start the screen
screen -S mcc

# Run it
./MinecraftClient

# Detach from the screen by pressing CTRL + A + D

# Re-attach if you want access again
screen -r mcc
```

_Learn more on how to use the screen command: [YouTube](https://www.youtube.com/watch?v=_ZJiEX4rmN4)_

## Docker

See [Run using Docker](./installation.md#using-docker)

## Command-line usage

**Minecraft Console Client** has a number of useful command-line parameters. This section covers the most important ones.

### For people not familiar with the command line

<details>
<summary>Introduction to command-line basics</summary>

For people who are not familiar with the usage of programs in the command line (terminal emulators), here we will explain what every single thing means, if you're already experienced you can skip this.

In command line (terminal emulators) you can run programs by specifying their name and hitting enter, usually programs have additional way of being configured, started or provided some additional data in a different manner, this is achieved by using command line parameters.

Command line parameters are written after the name of the program, they're separated by spaces and they can have a few different formats, examples:

- `someparameter`
- `-some-parameter`
- `--some-other-parameter`
- `--some-setting="some value"`
- `-a=5`

Parameters with a single dash (`-`) are usually used for a single letter (short-hand) parameters, while the ones with a double dash (`--`) are being used for parameters with a longer/full name.

When you are reading examples, you will often see something like this: `<something here>`, this means that this is a place holder and it should be changed with some value, excluding the `<` and the `>`.

For example `<username>` you need to change to an username of your liking, example: `notch` (`<` and `>` should not be included).

`[` and `]` mean that a parameter is an optional one.

They also can hold some values, example from the MCC:

```bash
MinecraftClient.exe --debugmessages=false
```

When a parameter has a textual value that includes one more spaces, you will need to wrap it the value in double quotes (`"`), example: `--some-parameter="some text here with spaces in it"`

Here is an example for using a `--help` command line parameter for MCC that will print out a page on how to use MCC from the command line:

```bash
MinecraftClient.exe --help
```

MCC also supports a few maintenance and debugging switches such as `--upgrade`, `--force-upgrade`, `--generate`, `--keyboard-debug`, `BasicIO`, and `BasicIO-NoColor`.

</details>

### Quick usage of MCC with examples

<div class="custom-container note"><p class="custom-container-title">Note</p>

**On Linux and macOS, you need to type: `./MinecraftClient` instead of `MinecraftClient.exe`**

</div>

```bash
MinecraftClient.exe --help
MinecraftClient.exe <username> <password> <server>
MinecraftClient.exe --setting=value [--other settings]
MinecraftClient.exe --section.setting=value [--other settings]
MinecraftClient.exe <settings-file.ini> [--other settings]
```

<div class="custom-container note"><p class="custom-container-title">Note</p>

**Microsoft accounts use the OAuth 2.0 device code flow and do not require a password on the command line. MCC will display a code and a URL for you to sign in through your browser (with full 2FA support). You can simply omit the password or use `""` as a placeholder.**

</div>

Examples:

```bash
# Microsoft account: connect to a server (you will sign in via device code in your browser)
MinecraftClient.exe player@example.com "" mc.someserver.com:25565

# Offline account: connect with a chosen username
MinecraftClient.exe Steve - mc.someserver.com:25565

# Overriding a setting from MinecraftClient.ini using a command-line parameter
MinecraftClient.exe --debugmessages=false

# Providing a custom settings file and overriding the language to Chinese
MinecraftClient.exe CustomSettingsFile.ini --language=zh
```

### Rules of using the command line parameters

You can mix and match arguments by following these rules:

- First positional argument may be either the login or a settings file
- Other positional arguments are read in order: login, password, server, command
- Arguments starting with `--` can be in any order and position

Examples and further explanations:

```bash
MinecraftClient.exe <login> <password> <server>
```

- This will automatically connect you to the chosen server.
- You may omit password and/or server to specify e.g. only the login
- For Microsoft accounts, password is not required (device code flow is used). Use `""` as a placeholder if you need to specify a server.
- To specify offline mode with no password, use `-` as password.

```bash
MinecraftClient.exe <login> <password> <server> "/mycommand"
```

- This will automatically send `/mycommand` to the server and close.
- To send several commands or stay connected, use the `ScriptScheduler` bot instead.

```bash
MinecraftClient.exe <myconfig.ini>
```

- This will load the specified configuration file
- If the file contains login / server ip, it will automatically connect.
- For Microsoft accounts, authentication happens through the device code flow (no password needed in the file).

```bash
MinecraftClient.exe --setting=value [--other settings]
```

- Specify settings on the command-line, see possible value in the configuration file
- Use `--section.setting=value` for settings outside the `[Main]` section
- Example: `--antiafk.enabled=true` for enabling the `AntiAFK` bot

```bash
MinecraftClient.exe <myconfig.ini> <login> <password> <server> [--other settings]
```

- Load the specified configuration file and override some settings from the file

## Internal Commands

These commands can be performed from the chat prompt, scripts or remote control.

From chat prompt, commands must by default be prepended with a slash, eg. `/quit`.

In scripts and remote control, no slash is needed to perform the command, eg. `quit`.

<div class="custom-container note"><p class="custom-container-title">Note</p>

**Some commands may not be documented yet or are defined in description of Chat Bots, use `/help` to list them all, or you can contribute to this page.**

</div>

<details>
<summary><code>animation</code></summary>

- **Description:**

  Swing your main or off hand.

- **Usage:**

  ```
  /animation <mainhand|offhand>
  ```

</details>

<details>
<summary><code>achievement</code></summary>

- **Description:**

  Show the achievements or advancements currently known to MCC.

  On Minecraft `1.8` to `1.11.2`, MCC tracks legacy achievements such as `achievement.openInventory`.

  On Minecraft `1.12+`, MCC tracks advancements such as `minecraft:story/root`.

- **Usage:**

  ```
  /achievement
  /achievement list
  /achievement locked
  /achievement unlocked
  ```

- **Examples:**

  List everything MCC currently knows:

  ```
  /achievement
  ```

  Show only incomplete entries:

  ```
  /achievement locked
  ```

  Show only completed entries:

  ```
  /achievement unlocked
  ```

- **Notes:**

  The command only shows data the server has already sent to MCC.

  Legacy achievements do not include titles or descriptions in the protocol, so older servers usually show the raw id instead.

</details>

<details>
<summary><code>book</code></summary>

<div class="custom-container note"><p class="custom-container-title">Note</p>

**You need to have [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this to work.**

</div>

- **Description:**

  Read the book in your main hand, or edit it if it is a writable book.

  In TUI mode, `/book read` opens a page viewer instead of printing the whole book to chat. The same viewer also opens automatically when the server tells the client to open a book.

  If you are holding a writable book, you can replace all pages, update one page, insert a page, delete a page, and sign the finished book.

- **Usage:**

  Read the current book or a single page:

  ```
  /book read [page]
  ```

  Replace the whole writable book from inline text or a file:

  ```
  /book write text <text>
  /book write file <path>
  ```

  Open the TUI editor or edit specific pages from the command line:

  ```
  /book edit
  /book edit page <page> <text>
  /book edit insert <page> <text>
  /book edit delete <page>
  ```

  Sign the writable book in your main hand:

  ```
  /book sign <title>
  ```

- **Notes:**

  `read` works with a writable book or a written book in your main hand.

  `write`, `edit`, and `sign` require a writable book in your main hand.

  Use `\n` for line breaks and `\f` for page breaks when passing inline text.

  MCC checks page count, page length, and title length against the current protocol before sending the packet.

  The interactive editor is only available in TUI mode.

  In the TUI book view, `PageUp`/`PageDown` switches pages.

- **Examples:**

  Read the held book:

  ```
  /book read
  ```

  Show only page 2:

  ```
  /book read 2
  ```

  Write two pages from the command line:

  ```
  /book write text First page\nSecond line\fSecond page
  ```

  Load the book text from a file:

  ```
  /book write file ./letter.txt
  ```

  Replace page 3:

  ```
  /book edit page 3 Updated text for page three
  ```

  Insert a new page before page 2:

  ```
  /book edit insert 2 This page goes before the old page 2
  ```

  Delete page 4:

  ```
  /book edit delete 4
  ```

  Sign the current writable book:

  ```
  /book sign Meeting Notes
  ```

</details>

<details>
<summary><code>bed</code></summary>

- **Description:**

  Allows you to make the bot sleep easily, all about sleeping in one command.

- **Usage:**

  Basic usage: `bed leave|sleep <x> <y> <z>|sleep <radius>`

- **Examples:**

  Leave a bed:

  ```
  /bed leave
  ```

  Sleep in a bed on 124 84 76:

  ```
  /bed sleep 124 84 76
  ```

  Sleep in a bed using relative coordinates:

  ```
  /bed sleep ~ ~ ~-2
  ```

  Automatically find a bed in radius of 50 blocks and sleep in it:

  ```
  /bed sleep 50
  ```

</details>

<details>
<summary><code>blockinfo</code></summary>

<div class="custom-container note"><p class="custom-container-title">Note</p>

**You need to have [Terrain And Movements](configuration.md#terrainandmovements) enabled in order for this to work.**

</div>

- **Description:**

  Reports the block type at the given position.

  If you use the `-s` option, it also reports the surrounding block types.

- **Usage:**

  Basic usage:

  ```
  /blockinfo <x> <y> <z> [-s]
  ```

</details>

<details>
<summary><code>bots</code></summary>

- **Description:**

  Allows you to list and unload a specific bot or all bots.

  Useful when debugging and developing scripts.

- **Usage:**

  ```
  /bots <list|unload <bot name|all>>
  ```

- **Examples:**

  Unload a bot called CustomScript

  ```
  /bots unload CustomScript
  ```

  Unload all bots

  ```
  /bots unload all
  ```

</details>

<details>
<summary><code>changeslot</code></summary>

- **Description:**

  Change your selected slot in the hotbar.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this to work.**

  </div>

- **Usage:**

  ```
  /changeslot <1-9>
  ```

</details>

<details>
<summary><code>chunk</code></summary>

- **Description:**

  Displays the chunk loading status in a nice way.

  <div class="custom-container warning"><p class="custom-container-title">Warning</p>

  **To use this feature you need to enable the [Terrain and Movements](configuration.md#terrainandmovements)**

  </div>

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need a terminal with emoji support, like Powershell 7, Windows Terminal or Alacritty, if you do not want emoji support and want to use cmd or powershell 5, disable emojis with: [`enableemoji`](configuration.md#enableemoji)**

  </div>

- **Usage:**

  ```
  /chunk status [chunkX chunkZ|locationX locationY locationZ]
  ```

  How it looks:

  ![Chunk status](/images/guide/ChunkStatus.png)

</details>

<details>
<summary><code>dig</code></summary>

- **Description:**

  Dig a block on a specific coordinate.

- **Usage:**

  ```
  /dig <x> <y> <z>
  ```

- **Example:**

  ```
  /dig 127 63 12
  ```

  Using relative coordinates:

  ```
  /dig ~ ~-1 ~2
  ```

</details>

<details>
<summary><code>dropitem</code></summary>

- **Description:**

  Drop all items of a specific type from your inventory.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this to work.**

  </div>

- **Usage:**

  ```
  /dropitem <itemtype>
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **All item types can be found [here](https://mccteam.github.io/r/item/#L12).**

  </div>

- **Example:**

  ```
  /dropitem diamond
  ```

</details>

<details>
<summary><code>enchant</code></summary>

<div class="custom-container note"><p class="custom-container-title">Note</p>

**You need to have [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this to work.**

</div>

- **Description:**

  Allows you to enchant items in an enchanting table.

  You need to first open an enchanting table and then place and item that you want to enchant and lapis in the enchanting table, and then you can execute the command.

  To open an enchanting table you can use the [`useblock`](#useblock) command.

- **Usage:**

  Basic usage:

  ```
  /enchant <top|middle|bottom>
  ```

</details>

<details>
<summary><code>effects</code></summary>

- **Description:**

  Lists the status effects currently applied to your player.

- **Usage:**

  ```
  /effects
  ```

</details>

<details>
<summary><code>entity</code></summary>

- **Description:**

  Attack an entity, use an entity or get a list of entities around you.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Inventory Handling](configuration.md#inventoryhandling) and [Entity Handling](configuration.md#entityhandling) enabled in order for this to work.**

  </div>

- **Usage:**

  Basic usage:

  ```
  /entity <id|entitytype> <attack|use>
  ```

  Get a list of entities around you:

  ```
  /entity
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **All entity types can be found [here](https://mccteam.github.io/r/entity/#L15).**

  </div>

- **Examples:**

  Attack a Zombie:

  ```
  /entity Zombie attack
  ```

</details>

<details>
<summary><code>execif</code></summary>

- **Description:**

  Allows you to execute a command if a specific condition is met.

  The condition is a C# expression and the local variables you set using [`set`](#set), [`setrnd`](#setrnd) or the configuration file can be used.

  The condition is always returned as a boolean, so only comparison can be done, if needed cast the expression result to bool.

  Also the instance of MCC is available with `MCC`.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **All local variables are treated as strings in the app, when comparing their values, you can use `<variable> == "<value>"`, or better use [`.Equals`](https://www.programiz.com/csharp-programming/library/string/equals) method**

  </div>

- **Usage:**

  Basic usage: `/execif <condition (C# expression)> <command>`

- **Examples:**

  Setting a variable and using it:

  ```
  /set test=Something
  /execif 'test == "Something"' "send Success!"
  ```

  ```
  /set test2=1
  /execif 'test2 == "1"' "send Success 2!"
  ```

  Basic C# expression:

  ```
  /execif "1 + 2 + 3 == 6" "send Success!"
  ```

  Using MCC class:

  ```
  /execif "MCC.GetHealth() == 20.0" "send Success!"
  ```

  Using in combination with [`execmulti`](#execmulti):

  ```
  /execif "1 == 1" "execmulti send 1 -> send 2 -> send 3"
  ```

- **Note**

  **You can use single quote (`'`) to wrap your expression if the expression contains double quote (`"`)**

  **Adding back-slash (`\`) before the double quote will also work (`/execif "test == \"Something\"" "send Success!"`)**

</details>

<details>
<summary><code>execmulti</code></summary>

- **Description:**

  Allows you to execute multiple commands in succession on a single line, useful for debugging or when using [`execif`](#execif)

  Commands are separated by `->`

- **Usage:**

  Basic usage: `execmulti <command 1> -> <command 2> -> <command 3> -> ...`

- **Examples:**

  ```
  /execmulti send 1 -> send 2 -> send 3 -> sneak
  ```

</details>

<details>
<summary><code>quit</code></summary>

- **Alias:** `exit`

- **Description:**

  Disconnect from the server and close the application

</details>

<details>
<summary><code>reco</code></summary>

- **Description:**

  Disconnect and reconnect to the server

- **Usage:**

  ```
  /reco [account]
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **`[account]` is an account alias defined in accounts file, for more info check out [accountlist](configuration.html#accountlist)**

  </div>

</details>

<details>
<summary><code>reload</code></summary>

- **Description:**

  Reloads the active configuration file and chat bots.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **Some settings are not reloaded because they are used before client initialization. Settings passed on the command line also override file values.**

  </div>

- **Usage:**

  ```
  /reload
  ```

</details>

<details>
<summary><code>recipebook</code></summary>

- **Description:**

  List unlocked recipe book entries and ask the server to place one of them into the active crafting inventory.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this command to work.**

  </div>

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **`craft` and `craftall` need an active player crafting grid, crafting table, furnace, blast furnace, smoker, or stonecutter inventory.**

  </div>

  <div class="custom-container warning"><p class="custom-container-title">Warning</p>

  **Recipe book crafting is supported on Minecraft `1.13+`.**

  </div>

  `list` shows the recipe book entries MCC is currently tracking.

  On newer versions, the list can contain numeric display ids instead of plain recipe names. If you see something like `838: Oak Planks`, use `838` with `craft` or `craftall`.

  `craft` and `craftall` send a recipe-book request to the server. They do not automatically take the result item for you. After the recipe appears in the active inventory, take the output slot the same way you would handle any other inventory action.

- **Usage:**

  ```
  /recipebook list
  ```

  ```
  /recipebook craft <recipe id>
  ```

  ```
  /recipebook craftall <recipe id>
  ```

- **Examples:**

  Show the currently tracked recipe book entries:

  ```
  /recipebook list
  ```

  Request one recipe placement:

  ```
  /recipebook craft minecraft:oak_planks
  ```

  On newer versions, use the numeric id shown by `/recipebook list`:

  ```
  /recipebook craftall 838
  ```

  If the recipe is placed in the player crafting grid, take the result from slot `0`:

  ```
  /inventory player click 0
  ```

</details>

<details>
<summary><code>connect</code></summary>

- **Description:**

  Go to the given server and resume the script

- **Usage:**

  ```
  /connect <server> [account]
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **`<server>` is either a server IP or a server alias defined in servers file, for more info check out [serverlist](configuration.html#serverlist)**

  </div>

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **`[account]` is an account alias defined in accounts file, for more info check out [accountlist](configuration.html#accountlist)**

  </div>

</details>

<details>
<summary><code>script</code></summary>

- **Description:**

  Run a script containing a list of commands

- **Usage:**

  ```
  /script <script name>
  ```

</details>

<details>
<summary><code>send</code></summary>

- **Description:**

  Send a message or a command to the server

- **Usage:**

  ```
  /send <text>
  ```

</details>

<details>
<summary><code>respawn</code></summary>

- **Description:**

  Use this to respawn if you are dead (like clicking "respawn" in-game)

- **Usage:**

  ```
  /respawn
  ```

</details>

<details>
<summary><code>log</code></summary>

- **Description:**

  Display some text in the console (useful for scripts)

- **Usage:**

  ```
  /log <text>
  ```

- Example:

  ```
  /log this is some text
  ```

</details>

<details>
<summary><code>list</code></summary>

- **Description:**

  List players logged in to the server (uses tab list info sent by server)

- **Usage:**

  ```
  /list
  ```

</details>

<details>
<summary><code>tab</code></summary>

- **Description:**

  Show the current player tab list in a more detailed format than `/list`.

  In the classic console, `/tab` prints a colored table with ping and player names. Team prefixes, suffixes, and display names are applied when the server sends them.

  In TUI mode, `/tab` opens a live overlay that refreshes automatically while it is visible. Press `Esc` to close it.

  If you want a separate team column, enable [Console.TabList.ShowTeams](configuration.md#showteams).

- **Usage:**

  ```
  /tab
  ```

- **Notes:**

  - `/tab` uses the tab list information sent by the server, so players hidden from the server tab list will not appear here.
  - The TUI overlay follows the live player list, so joins, leaves, ping updates, and scoreboard team updates show up without reopening it.

</details>

<details>
<summary><code>set</code></summary>

- **Description:**

  Set a value which can be used as `%variable%` in further commands

- **Usage:**

  ```
  /set <variable>=<value>
  ```

- **Examples:**

  ```
  /set abc=123
  ```

</details>

<details>
<summary><code>setrnd</code></summary>

- **Description:**

  Set a `%variable%` randomly to one of the provided values

- **Usage:**

  ```
  /setrnd <variable> string1 "\"string2\" string3"
  ```

- **Examples:**

  ```
  /setrnd <variable> -7 to 10
  ```

  (Set a `%variable%` to a number from -7 to 10)

</details>

<details>
<summary><code>sneak</code></summary>

- **Description:**

  Toggle sneaking.

- **Usage:**

  ```
  /Sneak
  ```

</details>

<details>
<summary><code>tps</code></summary>

- **Description:**

  Get the server TPS (Ticks Per Second).

- **Usage:**

  ```
  /tps
  ```

</details>

<details>
<summary><code>teams</code></summary>

- **Description:**

  List all scoreboard teams the server has sent, along with their members and settings.

- **Usage:**

  ```
  /teams
  ```

- **Example output:**

  ```
  Team 'RedTeam' (display: RedTeam, color: 12, prefix: '', suffix: '', nameTagVisibility: always, collisionRule: always, friendlyFire: True, seeInvisibles: True)
    Members (2): Steve, Alex
  Team 'BlueTeam' (display: BlueTeam, color: 9, prefix: '', suffix: '', nameTagVisibility: always, collisionRule: always, friendlyFire: True, seeInvisibles: True)
    No members.
  ```

</details>

<details>
<summary><code>useitem</code></summary>

- **Description:**

  Use the item in your hand, including use-on-block actions like shovel flattening.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this to work.**

  </div>

  <div class="custom-container warning"><p class="custom-container-title">Warning</p>

  **The [Inventory Handling](configuration.md#inventoryhandling) is currently supported on `1.8+` and is unavailable on `1.4.6 - 1.7.10`.**

  </div>

- **Usage:**

  ```
  /useitem
  ```

  Use the item on a specific block:

  ```
  /useitem <x> <y> <z>
  ```

</details>

<details>
<summary><code>useblock</code></summary>

- **Description:**

  Place a block from a hand on a specific coordinate or open an inventory:

  - chest/trap chest
  - furnace
  - brewing stand
  - dispenser/dropper
  - hopper
  - shulker
  - loom

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Inventory Handling](configuration.md#inventoryhandling) and [Terrain and Movements](configuration.md#terrainandmovements) enabled in order for this to work.**

  </div>

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **Not all inventories have a GUI representation in an ASCII art format.**

  </div>

  <div class="custom-container warning"><p class="custom-container-title">Warning</p>

  **The [Inventory Handling](configuration.md#inventoryhandling) is currently supported on `1.8+` and is unavailable on `1.4.6 - 1.7.10`.**

  </div>

- **Usage:**

  ```
  /useblock <x> <y> <z>
  ```

- **Example:**

  ```
  /useblock 43 72 7
  ```

</details>

<details>
<summary><code>follow</code></summary>

- **Description:**

  Make the bot follow a player.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **This command is available only when the [Follow Player](chat-bots.md#follow-player) chat bot is enabled.**

  </div>

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this to work.**

  </div>

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Entity Handling](configuration.md#entityhandling) enabled in order for this to work.**

  </div>

- **Usage:**

  ```
  /follow <player name|stop>
  ```

- **Example:**

  ```
  /follow milutinke
  ```

</details>

<details>
<summary><code>wait</code></summary>

- **Description:**

  Wait X ticks (20 ticks = ~1 second. Only for scripts)

- **Usage:**

  Fixed time:

  ```
  /wait <time>
  ```

  Random time:

  ```
  /wait <minimum time> to <maximum time>
  ```

  <div class="custom-container tip"><p class="custom-container-title">Tip</p>

  **You can use `-` instead of `to`**

  </div>

- **Examples:**

  ```
  /wait 20
  ```

  ```
  /wait 20 to 100
  ```

  ```
  /wait 20-35
  ```

</details>

<details>
<summary><code>move</code></summary>

- **Description:**

  Used for moving when terrain and movements feature is enabled.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Terrain and Movements](configuration.md#terrainandmovements) enabled in order for this to work.**

  </div>

  <div class="custom-container warning"><p class="custom-container-title">Warning</p>

  **The [Terrain and Movements](configuration.md#terrainandmovements) is currently not supported in `1.4.6 - 1.6`.**

  </div>

- **Usage:**

  ```
  /move <on|off|get|up|down|east|west|north|south|center|x y z|gravity [on|off]> [-f]: walk or start walking. "-f": force unsafe movements like falling or touching fire
  ```

- **Examples:**

  Enable gravity

  ```
  /move gravity on
  ```

  Move to coordinates:

  ```
  /move 125 72 34
  ```

  Move to a center of a block:

  ```
  /move center
  ```

</details>

<details>
<summary><code>nameitem</code></summary>

- **Description:**

  This command allows you to name an item when you have an Anvil inventory open and an item in the first slot (slot number 0),

  After you place an item in the first slot of the anvil, use this command, and then do a click on the slot 2 to get an item from the anvil, then do a click on an empty slot in your inventory.

- **Usage:**

  ```
  /nameitem <name of the item>
  ```

- **Example:**

  ```
  /nameitem My super duper sword 2000
  ```

- **Full Example with anvil:**

  ```
  # Open an anvil
  /useblock 12 74 321

  # Click on an axe in slot 12
  /inventory container click 12

  # Put an axe to the slot 0 in anvil
  /inventory container click 0

  # Set the new name
  /nameitem My fancy axe

  # Click on the axe in slot 2 in the anvil
  /inventory container click 2

  # Put the axe back in your inventory in slot 12
  /inventory container click 12

  # Close the anvil
  /inventory container close
  ```

</details>

<details>
<summary><code>look</code></summary>

- **Description:**

  Used for looking at direction when terrain and movements is enabled

- **Usage:**

  ```
  /look <x y z|yaw pitch|up|down|east|west|north|south>
  ```

- **Examples:**

  ```
  /look up
  ```

  ```
  /look east
  ```

</details>

<details>
<summary><code>inventory</code></summary>

- **Description:**

  Used for inventory manipulation.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **You need to have [Inventory Handling](configuration.md#inventoryhandling) enabled in order for this to work.**

  </div>

  <div class="custom-container warning"><p class="custom-container-title">Warning</p>

  **The [Inventory Handling](configuration.md#inventoryhandling) is currently supported on `1.8+` and is unavailable on `1.4.6 - 1.7.10`.**

  </div>

  MCC defines inventories as containers internally, so player's inventory, chests, droppers, dispensers, hoppers, chest minecarts, barrels, furnaces, etc... are all considered a container, and each one of them has it's ID, the words container and inventory can be used interchangeably.

  Inventory has slots and each one of them has an id.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **This command DOES NOT physically open a container (eg. chest), for that you need to use [`useblock`](#useblock) command first.**

  </div>

  An example of player inventory with annotated IDs in ASCII art and a list of items:

  ![Player Inventory](/images/guide/PlayerInventory.png "Player Inventory")

- **Usage:**

  Basic usage:

  ```
  /inventory <player|container|<id>> <action> [action parameters] | /inventory <inventories/i> | /inventory <search/s> <item type> [amount]
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **player and container can be simplified with p and c accordingly**

  </div>

  Actions:

  - `click`
  - `drop`

  Show/Preview items in an inventory:

  ```
  /inventory <player|id>
  ```

  Click/Shift-Click on an item in an inventory:

  ```
  /inventory <player|container|<id>> <click> <slot id> [left|right|middle|Shift|ShiftRight]
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **The default click is left click**

  </div>

  Close an inventory:

  ```
  /inventory <player|container|<id>> close
  ```

  Drop item(s) from an inventory:

  ```
  /inventory <player|id> drop <slot id> <number of items|all>
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **To drop all items from a slot, you can use: `all`**

  </div> 

  Give an item to the player inventory from a creative menu when in the creative mode:

  ```
  /inventory creativegive <slot id> <item type> <amount>
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **To find item types, check out [this list](https://mccteam.github.io/r/item/#L12)**

  </div>

  Delete an item from a player's inventory when in the creative mode:

  ```
  /inventory creativedelete <slot id>
  ```

  Show all available inventories:

  ```
  /inventory inventories
  ```

  Search for an item of specified type in available inventories:

  ```
  /inventory search <item type>
  ```

- **Examples:**

  Show player's inventory:

  ```
  /inventory player
  ```

  Show/Preview items in an inventory using an id:

  ```
  /inventory 3
  ```

  Click on an item in player's inventory in slot number/id `36`:

  ```
  /inventory player click 36
  ```

  Right-Click on an item in slot number/id `4` in an inventory with an id `2`:

  ```
  /inventory 2 click 4 right
  ```

  Close an inventory with an id `2`:

  ```
  /inventory 2 close
  ```

  Drop a single item from a player's inventory in slot number/id `36`:

  ```
  /inventory player drop 36 1
  ```

  Drop all items from a player's inventory in slot number/id `37`:

  ```
  /inventory player drop 37 all
  ```

  Give an item to the player inventory from a creative menu when in the creative mode:

  ```
  /inventory creativegive 36 diamondblock 64
  ```

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **To find item types, check out [this list](https://mccteam.github.io/r/item/#L12)**

  </div>

  Delete an item from a player's inventory in slot number/id `36` when in the creative mode:

  ```
  /inventory creativedelete 36
  ```

  Search for 10 Slime Blocks in available inventories:

  ```
  /inventory s SlimeBlock 10
  ```

</details>

<details>
<summary><code>debug</code></summary>

- **Description:**

  Toggle debug messages, useful for chatbot developers.

</details>

<details>
<summary><code>help</code></summary>

- **Description:**

  Show commands help.

  <div class="custom-container note"><p class="custom-container-title">Note</p>

  **Use "/send /help" for server help**

  </div>

</details>
