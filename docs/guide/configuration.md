---
title: Configuration
redirectFrom: 
    - "/g/conf/index.html"
    - "/g/conf.html"
---

# Configuration

**Minecraft Console Client** can be configured through both [command-line parameters](usage.md#command-line-parameters) and the configuration file.

By default, MCC stores its settings in `MinecraftClient.ini`, which is created the first time you run the program. You can also pass a custom configuration file path as the first argument when starting MCC. See [Usage](usage.md#quick-usage-of-mcc-with-examples) for examples.

<div class="custom-container warning"><p class="custom-container-title">Warning</p>


</div>

## Notes

-   Some less common settings are not repeated here. The generated config file contains inline descriptions for every setting.
-   Bot-specific settings are documented in [Chat Bots](chat-bots.md).

## Configuration File

### Format

The configuration file uses the [TOML format](https://toml.io/en/). Options are key-value pairs grouped into sections.

Sections are defined between square brackets, for example `[This is a section]`.

Settings are written as key-value pairs, with the key and value separated by `=`, for example `some-setting = "some value"`.

Lines starting with `#` are comments, they do not have an effect on the configuration of the program, their purpose is purely a descriptive one.

**For the full syntax and data types, see the [official TOML documentation](https://toml.io/en/v1.0.0).**

Full Example:

```toml
[SectionNameHere]
Setting_Name = "this is some name"
Setting_Something = 15

[OtherSection]
# This is a comment explaining what this setting/option does
Other_Setting = true  # This also is a comment

[ThirdSection]
Section_Enabled = true
colors = [ "red", "yellow", "green" ]

[ThirdSection.Subsection]
Coordinate = { x = 145, y = 64, z = 2045 }
```

## Main Section

### Main General section

-   **Section header:** `Main.General`

#### `Account`

-   **Description:**

    This setting is where you need to provide your in-game name (for offline accounts) or email for Microsoft accounts (Mojang accounts do not work anymore) and your password (if using an offline account, use `-` for the password).

-   **Format:**

    `Account = { Login = "<email>", Password = "<password>" }`

-   **Type:** `inline table`

-   **Example:**

    `Account = { Login = "some.random.player@gmail.com", Password = "myEpicPassword123" }`

#### `Server`

-   **Description:**

    This is the setting where you provide the address of the game server, "Host" can be filled in with domain name or IP address. (The "Port" field can be deleted, it will be resolved automatically)

    Host can also fill in the nickname of the server in the "Server List" below.

-   **Format:** `Server = { Host = "<ip>", Port = <port> }`

-   **Type:** `inline table`

-   **Example:**

    ```
    Server = { Host = "mysupercoolserver.com" }
    ```

    ```
    Server = { Host = "192.168.1.27", Port = 12345 }
    ```

    ```
    Server = { Host = "ServerAlias1" }
    ```

#### `AccountType`

-   **Description:**

    This setting defines the account type: `mojang`, `microsoft`, or `yggdrasil`.

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Use `microsoft` for normal Microsoft accounts. `yggdrasil` is for custom authlib/Yggdrasil servers.**

    </div>

-   **Type:** `string`

-   **Default:** `microsoft`

-   **Example:**

    ```
    AccountType = "microsoft"
    ```

#### `Method`

-   **Description:**

    This setting is where you define the way you will sign in with your Microsoft account, available options are `mcc` and `browser`.

-   **Type:** `string`

-   **Default:** `mcc`

-   **Example:**

    ```
    Method = "mcc"
    ```

#### `AuthServer`

-   **Description:**

    This setting is used when `AccountType` is set to `yggdrasil`. It points MCC at the authlib/Yggdrasil server used for login and session checks.

    You can provide either just the host name or a `host:port` pair. If the port is omitted, MCC uses `443`.

-   **Type:** `inline table`

-   **Default:** `{ Host = "", Port = 443 }`

-   **Example:**

    ```
    AuthServer = { Host = "auth.example.com", Port = 443 }
    ```

#### `AuthUser`

-   **Description:**

    This setting allows for Yggdrasil authlib multi-user selection. It selects which profile MCC should use when the authlib/Yggdrasil server returns multiple available profiles. Leave it empty to pick the profile interactively.

-   **Type:** `string`

-   **Default:** `""`

-   **Example:**

    ```
    AuthUser = "SomePlayer"
    ```

### Main Advanced section

-   **Section header:** `Main.Advanced`

#### `Language`

-   **Description:**

    This setting is where you define which language you want to use.

    When connecting to 1.6+ servers, you will need a translation file to display properly some chat messages.These files describe how some messages should be printed depending on your preferred language.

    The client will automatically load `en_GB.lang` from your Minecraft folder if Minecraft is installed on your computer, or download it from Mojang's servers. You may choose another language in the configuration file.

    To find your language code, check [this list](https://mccteam.github.io/r/l-code.html).

-   **Type:** `string`

-   **Default:** `en_us`

-   **Example:**

    ```
    Language = "en_us"
    ```

#### `EnableSentry`

-   **Description:**

    Set this to `false` to opt out of Sentry error reporting.

-   **Type:** `boolean`

-   **Default:** `true`

#### `LoadMccTranslation`

-   **Description:**

    Set this to `false` to keep MCC in English even when translated strings are available.

-   **Type:** `boolean`

-   **Default:** `true`

#### `ConsoleTitle`

-   **Description:**

    This setting is where you can change the title of the program window if you want to. You can use the variables in it.

-   **Type:** `string`

-   **Default:** `"%username%@%serverip% - Minecraft Console Client"`

-   **Example:**

    ```
    ConsoleTitle = "%username%@%serverip% - Minecraft Console Client"
    ```

#### `InternalCmdChar`

-   **Description:**

    This setting is where you can change the prefix character of internal MCC commands.

    Available options:

    -   `none`
    -   `slash`
    -   `backslash`

-   **Type:** `string`

-   **Default:** `slash`

-   **Example:**

    ```
    InternalCmdChar = "slash"
    ```

#### `MessageCooldown`

-   **Description:**

    This setting is where you can change the minimum delay in seconds between messages to avoid being kicked for spam.

-   **Type:** `float`

-   **Default:** `1.0`

#### `BotOwners`

-   **Description:**

    This setting is where you can set the owners of the bots/client which can be used by some plugins. The names are separated as strings within an array, separated by commas.

-   **Format:**

    ```
    BotOwners = [ "<nick>", "<nick>", ... ]
    ```

-   **Type:** `array of strings`

-   **Default:** `[ "Player1", "Player2", ]`

-   **Example:**

    ```
    BotOwners = [ "milutinke", "bradbyte", "BruceChen", ]
    ```

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **Admins can impersonate players on versions older than 1.19**

    </div>


#### `MinecraftVersion`

-   **Description:**

    This setting is where you can set the version you are playing on.

-   **Format:** `MinecraftVersion = "<version>"`

-   **Type:** `string`

-   **Version format:** `1.X.X`

-   **Type:** `string`

-   **Default:** `auto`

-   **Example:**

    ```
    MinecraftVersion = "1.18.2"
    ```

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Current code support is `1.4.6` through `26.1`.**

    </div>

#### `EnableForge`

-   **Description:**

    This setting is where you can define if you're playing on a forge server.

-   **Type:** `string`

-   **Available options:**

    -   `auto`
    -   `no`
    -   `force`

-   **Default:** `no`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Force-enabling only works for MC 1.13 +**

    </div>

#### `BrandInfo`

-   **Description:**

    This setting is where you can change how MCC identifies itself to the server. It can be whatever you like, example: `vanilla`, `mcc`, `empty`.

-   **Type:** `string`

-   **Default:** `mcc`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **For playing on Hypixel you need to use `vanilla`**

    </div>

#### `ChatbotLogFile`

-   **Description:**

    This setting is where you can set the path to the file which will contain the logs, leave empty for no log file.

-   **Type:** `string`

-   **Default:** Empty

-   **Example:**

    ```
    ChatbotLogFile = "my-log.txt"
    ```

#### `PrivateMsgsCmdName`

-   **Description:**

    The name of the command which is used for remote control of the bot.

-   **Type:** `string`

-   **Default:** `tell`

#### `ShowSystemMessages`

-   **Description:**

    This setting is where you can define if you want to see the system messages (example command block outputs) if you're an OP.

-   **Type:** `boolean`

-   **Default:** `true`

#### `ShowXPBarMessages`

-   **Description:**

    This setting is where you can define if you want to see the Boss XP Bar messages.

-   **Type:** `boolean`

-   **Default:** `true`

    > **Note: Can create a spam if there is a bunch of withers**

#### `ShowChatLinks`

-   **Description:**

    This setting is where you can define if you want to decode links embedded in chat messages and show them in console.

-   **Type:** `boolean`

-   **Default:** `true`

#### `ShowInventoryLayout`

-   **Description:**

    This setting is where you can define if you want to have the MCC show you the inventory in a form of an ASCII art when using the `/inventory` internal command.

    How it looks like:

    ![ASCII Art here](/images/guide/PlayerInventory.png "ASCII Art here")

-   **Type:** `boolean`

-   **Default:** `true`

#### `TerrainAndMovements`

-   **Description:**

    This setting is where you can set if you want to enable terrain movement, so you can use command like `/move` and some bots.

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **This feature is currently not supported in `1.4.6 - 1.6`.**

    </div>

-   **Type:** `boolean`

-   **Default:** `false`

<div class="custom-container tip"><p class="custom-container-title">Tip</p>

**Sometimes the latest versions might not support this straight away, since Mojang often makes changes to this.**

</div>

#### `InventoryHandling`

-   **Description:**

    This setting is where you can set if you want to enable inventory handling using the `/inventory` command.

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **This feature is currently not supported in `1.4.6 - 1.9`. But we are working on getting it supported in 1.8 and 1.9.**

    </div>

-   **Type:** `boolean`

-   **Default:** `false`

#### `EntityHandling`

-   **Description:**

    This setting is where you can set if you want to enable interactions with entities such as players, mobs, minecarts, etc..

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **This feature is currently not supported in `1.4.6 - 1.7`.**

    </div>

-   **Type:** `boolean`

-   **Default:** `false`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Sometimes the latest versions might not support this straight away, since Mojang often makes changes to this.**

    </div>

#### `SessionCache`

-   **Description:**

    This setting is where you can define is you want your session info to be stored on the disk or in memory, or not to be stored (this will make you login every time which will add some time to the process).

    You can disable this by using `none`.

    The `disk` option will save your login authorization token on the disk, but this can be a bit of a security risk if someone else has access to your folder where you have MCC installed.

    The `memory` will last until you close down the program.

-   **Type:** `string`

-   **Default:** `disk`

#### `ProfileKeyCache`

-   **Description:**

    Same as `SessionCache` but for your profile keys which are used for chat signing and validation.

-   **Type:** `string`

-   **Default:** `disk`

#### `ResolveSrvRecords`

-   **Description:**

    Use `no`, `fast` (5s timeout), or `yes`.

    Required for joining some servers.

-   **Type:** `string`

-   **Default:** `fast`

#### `PlayerHeadAsIcon`

-   **Description:**

    This setting allows you to set the icon of the program to be the head of your in-game skin.

-   **Type:** `boolean`

-   **Default:** `true`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Only works on Windows XP-8 or Windows 10 with old console**

    </div>

#### `ExitOnFailure`

-   **Description:**

    This setting allows you to define if your want to disable pauses on error, for using MCC in non-interactive scripts

-   **Type:** `boolean`

-   **Default:** `false`

#### `CacheScript`

-   **Description:**

    This setting allows you to define if your want to have MCC cache compiled scripts for faster load on low-end devices.

-   **Type:** `boolean`

-   **Default:** `true`

#### `Timestamps`

-   **Description:**

    This setting allows you to define if your want to have MCC prepend timestamps to chat messages.

-   **Type:** `boolean`

-   **Default:** `false`

#### `AutoRespawn`

-   **Description:**

    This setting allows you to define if your want to auto respawn if you die.

-   **Type:** `boolean`

-   **Default:** `false`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Make sure the spawn point is safe**

    </div>

#### `MinecraftRealms`

-   **Description:**

    This setting allows you to define if your want to enable support for joining Minecraft Realms.

-   **Type:** `boolean`

-   **Default:** `false`

#### `MoveHeadWhileWalking`

-   **Description:**

    This setting allows you to define if your want to enable head movement while walking to avoid anti-cheat triggers

-   **Type:** `boolean`

-   **Default:** `true`

#### `TcpTimeout`

-   **Description:**

    This setting allows you to define a custom timeout period in seconds. Use only if you know what you're doing.

-   **Type:** `integer`

-   **Default:** `30`

#### `EnableEmoji`

-   **Description:**

    This setting allows you to disable emojis in the [`chunk`](usage.md#chunk) command.

-   **Type:** `boolean`

-   **Default:** `true`

#### `MovementSpeed`

-   **Description:**

    This setting allows you to change the movement speed of the bot.

-   **Type:** `integer`

-   **Default:** `2`

<div class="custom-container warning"><p class="custom-container-title">Warning</p>

**A movement speed higher than 2 may be considered cheating by some plugins.**

</div>

#### `IgnoreInvalidPlayerName`

-   **Description:**

    Minecraft player name can only consist of English letters, numbers, and underscore symbols. Other name will be considered as invalid and ignored by default.

-   **Type:** `boolean`

-   **Default:** `true`

### Account List section

-   **Section header:** `Main.Advanced.AccountList`

-   **Description:**

    This section allows you to add multiple accounts so you can switch easily between them on the fly.

-   **Usage examples:**

    `/connect <serverip> Player1`

-   **Type:** `array of inline tables`

-   **Format:**

    ```toml
    <account nick> = { Login = "<email>", Password = "<password>" }
    ```

-   **Examples:**

    ```toml
    Player1 = { Login = "playerone@email.com", Password = "thepassword" }
    ```

### Server List section

-   **Section header:** `Main.Advanced.ServerList`

-   **Description:**

    This section allows you to add multiple server aliases which enables fast and easy switching between servers. Aliases cannot contain dots or spaces, and the name "localhost" cannot be used as an alias.

-   **Usage examples:**

    `/connect Server2`

-   **Type:** `array of inline tables`

-   **Format:**

    ```toml
    <server alias> = { Host = "<ip>", Port = <port> }
    ```

-   **Examples:**

    ```toml
    ServerAlias1 = { Host = "mc.awesomeserver.com" }
    ServerAlias2 = { Host = "192.168.1.27", Port = 12345 }
    ```

### Signature section

-   **Section header:** `Signature`

-   **Description:**

    Affects only Minecraft 1.19+.

    This section contains settings related to a new chat reporting (signing and verifying) feature introduced by Mojang.

#### `LoginWithSecureProfile`

-   **Description:**

    Microsoft accounts only. If disabled, will not be able to sign chat and join servers configured with `enforce-secure-profile=true`

-   **Type:** `boolean`

-   **Default:** `true`

#### `SignChat`

-   **Description:**

    Whether to sign the chat sent from the MCC.

-   **Type:** `boolean`

-   **Default:** `true`

#### `SignMessageInCommand`

-   **Description:**

    Whether to sign the messages contained in the commands sent by the MCC.

    For example, the message in `/msg` and `/me`

-   **Type:** `boolean`

-   **Default:** `true`

#### `MarkLegallySignedMsg`

-   **Description:**

    Use green color block to mark chat with legitimate signatures.

-   **Type:** `boolean`

-   **Default:** `true`

#### `MarkModifiedMsg`

-   **Description:**

    Use yellow color block to mark chat that have been modified by the server.

-   **Type:** `boolean`

-   **Default:** `true`

#### `MarkIllegallySignedMsg`

-   **Description:**

    Use red color block to mark chat without legitimate signature.

-   **Type:** `boolean`

-   **Default:** `true`

#### `MarkSystemMessage`

-   **Description:**

    Use gray color block to mark system message (always without signature).

-   **Type:** `boolean`

-   **Default:** `true`

#### `ShowModifiedChat`

-   **Description:**

    Set to true to display messages modified by the server, false to display the original signed messages.

-   **Type:** `boolean`

-   **Default:** `true`

#### `ShowIllegalSignedChat`

-   **Description:**

    Whether to display chat and messages in commands without legal signature.

-   **Type:** `boolean`

-   **Default:** `true`

### App Vars values section

-   **Section header:** `AppVar.VarStirng`

#### `DebugMessages`

-   **Description:**

    This setting allows you to define if your want to see debug messages while the client is running, this is useful when there is a bug and you want to report a problem, or if you're developing a script/bot and you want to debug it.

-   **Type:** `boolean`

-   **Default:** `false`

#### `ChatMessages`

-   **Description:**

    This setting allows you to define if your want to see chat messages.

-   **Type:** `boolean`

-   **Default:** `true`

#### `InfoMessages`

-   **Description:**

    This setting allows you to define if your want to see info messages.

    Most of the messages from MCC.

-   **Type:** `boolean`

-   **Default:** `true`

#### `WarningMessages`

-   **Description:**

    This setting allows you to define if your want to see warning messages.

-   **Type:** `boolean`

-   **Default:** `true`

#### `ErrorMessages`

-   **Description:**

    This setting allows you to define if your want to see error messages.

-   **Type:** `boolean`

-   **Default:** `true`

#### `ChatFilterRegex`

-   **Description:**

    This setting allows you to define if your want to filter chat messages being logged using a Regex expression.

    More on Regex [here](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference).

-   **Type:** `string`

-   **Default:** `.*`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Not filtering anything by default**

    </div>

#### `DebugFilterRegex`

-   **Description:**

    This setting allows you to define if your want to filter debug messages being logged using a Regex expression.

    More on Regex [here](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference).

-   **Type:** `string`

-   **Default:** `.*`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **Not filtering anything by default**

    </div>

#### `FilterMode`

-   **Description:**

    Can be `disable`, `blacklist` or `whitelist`

    "disable" will disable the filter, `blacklist` hides the messages, while the `whitelist` shows the messages that match the Regex expression that you've defined.

-   **Type:** `string`

-   **Default:** `disable`

#### `LogToFile`

-   **Description:**

    This setting allows you to define if your want to log messages to a file.

-   **Type:** `boolean`

-   **Default:** `false`

#### `LogFile`

-   **Description:**

    This setting allows you to define a path to a file where you want to log messages if you have enabled logging to a file with `LogToFile = true`.

-   **Type:** `string`

-   **Default:** `console-log.txt`

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **%username% and %serverip% will be substituted with your username and the IP address of the server you are connected to. So you can use something like: `console-log-%username%-%serverip%.txt`**

    </div>

#### `PrependTimestamp`

-   **Description:**

    This setting allows you to define if your want prepend timestamps to messages that are written to the log file.

-   **Type:** `boolean`

-   **Default:** `false`

#### `SaveColorCodes`

-   **Description:**

    This setting allows you to define if your want keep the server color codes in the logged messages.

    Example of a color coded message: `§bsome message`

-   **Type:** `boolean`

-   **Default:** `false`

## App Vars section

-   **Section header:** `AppVar`

-   **Description:**

    This section allows you to define your own custom settings/variables which you can use in scripts, bots or other setting fields.

    To define a variable/setting, simply make a new line with the following format under the `[AppVar.VarStirng]` section:

    <div class="custom-container tip"><p class="custom-container-title">Tip</p>

    **`%username%`, `%serverip%`, `%datetime%` are reserved variables**

    </div>

-   **Section header:** `AppVar.VarStirng`

-   **Examples:**

    ```
    your_var = "your_value"
    "your var 2" = "your value 2"
    ```

## Console section

-   **Section header:** `Console`

-   **Description:**

    Console-related settings for input handling and command suggestions.

### Console General section

-   **Section header:** `Console.General`

#### `ConsoleColorMode`

-   **Description:**

    Use `disable`, `legacy_4bit`, `vt100_4bit`, `vt100_8bit`, or `vt100_24bit`.

    If the terminal shows garbled escape sequences like `←[0m`, try `legacy_4bit` or disable color output.

-   **Type:** `string`

-   **Default:** `vt100_24bit`

#### `Display_Input`

-   **Description:**

    Set this to `false` if you do not want MCC to echo the current input line while typing.

-   **Type:** `boolean`

-   **Default:** `true`

#### `History_Input_Records`

-   **Description:**

    Maximum number of remembered console input lines.

-   **Type:** `integer`

-   **Default:** `32`

### Console CommandSuggestion section

-   **Section header:** `Console.CommandSuggestion`

-   **Description:**

    Command completion suggestions in the console.

#### `Enable`

-   **Description:**

    Set this to `false` to disable command completion suggestions.

-   **Type:** `boolean`

-   **Default:** `true`

#### `Enable_Color`

-   **Description:**

    Enables colored suggestions when the terminal color mode supports it.

-   **Type:** `boolean`

-   **Default:** `true`

#### `Use_Basic_Arrow`

-   **Description:**

    Use this if the suggestion arrows are not displayed correctly in your terminal.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Max_Suggestion_Width`

-   **Description:**

    Maximum width of the suggestion popup.

-   **Type:** `integer`

-   **Default:** `30`

#### `Max_Displayed_Suggestions`

-   **Description:**

    Maximum number of suggestions shown at once.

-   **Type:** `integer`

-   **Default:** `6`

#### Color fields

-   **Description:**

    The suggestion text, tooltip, and arrow colors are stored as hex color strings such as `#f8fafc`.

    MCC validates these values on startup and falls back to built-in defaults if a color string is invalid.

## Proxy section

-   **Section header:** `Proxy`

-   **Description:**

    Connect to a server via a proxy instead of connecting directly.

#### `Enabled_Login`

-   **Description:**

    If Mojang session services or Microsoft login services are blocked on your network or your ip is blacklisted or rate limited by Microsoft, set the value to `true`.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Enabled_Update`

-   **Description:**

    Use the proxy when MCC checks for updates.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Enabled_Ingame`

-   **Description:**

    Whether to connect to the game server through a proxy.

    If connecting to a port 25565 (Minecraft) is blocked on your network, set the value to `true` to login and connect using the proxy.

-   **Type:** `boolean`

-   **Default:** `false`

    <div class="custom-container warning"><p class="custom-container-title">Warning</p>

    **Make sure your server rules allow Proxies or VPNs before setting the setting to `true`, or you may face consequences!**

    </div>

#### `Server`

-   **Description:**

    The proxy server IP and port.

    Proxy server must allow HTTPS for login, and non-443 ports for playing.

-   **Format:**

    ```
    Server = { Host = "<ip>", Port = <port> }
    ```

-   **Default:** `{ Host = "0.0.0.0", Port = 8080 }`

#### `Proxy_Type`

-   **Description:**

    The type of your proxy.

    Available options:

    -   `HTTP`
    -   `SOCKS4`
    -   `SOCKS4a`
    -   `SOCKS5`

-   **Type:** `string`

-   **Default:** `HTTP`

#### `Username`

-   **Description:**

    The proxy account username.

    Only needed for password protected proxies.

-   **Default:** `` ``

#### `Password`

-   **Description:**

    The proxy account password.

    Only needed for password protected proxies.

-   **Default:** `` ``

## MCSettings section

-   **Section header:** `MCSettings`

-   **Description:**

    Client settings related to language, render distance, difficulty, chat and skins.

#### `Enabled`

-   **Description:**

    This setting allows you to specify if you want to use settings from this section.

-   **Type:** `boolean`

-   **Default:** `true`

#### `Locale`

-   **Description:**

    Use any language implemented in Minecraft

-   **Type:** `string`

-   **Default:** `en_US`

#### `RenderDistance`

-   **Description:**

    Render distance in chunks: `0 - 255`

-   **Type:** `integer`

-   **Default:** `8`

#### `Difficulty`

-   **Description:**

    Available options:

    -   `peaceful`
    -   `easy`
    -   `normal`
    -   `difficult`

-   **Type:** `string`

-   **Default:** `peaceful`

#### `ChatMode`

-   **Description:**

    This setting allows you to effectively mute yourself.

    Available options:

    -   `enabled` (You can chat)
    -   `commands` (You can only do commands)
    -   `disabled`

-   **Type:** `string`

-   **Default:** `enabled`

#### `ChatColors`

-   **Description:**

    This setting allows you to disable chat colors.

-   **Type:** `boolean`

-   **Default:** `true`

#### `MainHand`

-   **Description:**

    This setting allows you to specify your main hand.

-   **Available values:** `right` and `left`

-   **Type:** `string`

-   **Default:** `left`

## MCSettings Skin section

-   **Section header:** `MCSettings.Skin`

-   **Description:**

    Skin options.

#### `Cape`

-   **Description:**

    This setting allows you to specify if you want to have your skin cape shown.

-   **Type:** `boolean`

-   **Default:** `true`

#### `Hat`

-   **Description:**

    This setting allows you to specify if you want to have your skin hat shown.

-   **Type:** `boolean`

-   **Default:** `true`

#### `Jacket`

-   **Description:**

    This setting allows you to specify if you want to have your skin jacket shown.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Sleeve_Left`

-   **Description:**

    This setting allows you to specify if you want to have your left sleeve shown.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Sleeve_Right`

-   **Description:**

    This setting allows you to specify if you want to have your right sleeve shown.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Pants_Left`

-   **Description:**

    This setting allows you to specify if you want to have your left part of the pants shown.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Pants_Right`

-   **Description:**

    This setting allows you to specify if you want to have your right part of the pants shown.

-   **Type:** `boolean`

-   **Default:** `false`

## Chat Format section

-   **Section header:** `ChatFormat`

-   **Description:**

    The MCC does it best to detect chat messages, but some server have unusual chat formats.

    When this happens, you'll need to configure the chat format yourself using settings from this section.

    The MCC uses Regular Expressions (Regex) to detect the chat formatting, in case that you're not familiar with Regex you can use the following resources to learn it and test it out:

    -   Crash courses:
        -   [Regex video tutorial by Web Dev Simplified](https://www.youtube.com/watch?v=rhzKDrUiJVk)
        -   [Regex on paper by Crack Concepts](https://www.youtube.com/watch?v=9RksQ5YT7FM)
    -   In-depth tutorials:

        -   [Quite a long and detailed tutorial by Svetlin Nakov](https://www.youtube.com/watch?v=DS9IO0W7-0Q)
        -   [Microsoft Documentation on Regex](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference)

    -   Testing Regex expressions online:
        -   [https://regex101.com/](https://regex101.com/)
        -   [https://regexr.com/](https://regexr.com/)

#### `Builtins`

-   **Description:**

    This setting allows you to define if your want use the default chat formats.

    Set to `false` to avoid conflicts with custom formats.

-   **Type:** `boolean`

-   **Default:** `true`

#### `UserDefined`

-   **Description:**

    This setting allows you to define if your want to use the custom chat formats defined bellow using Regex.

    Set to `true` to use the custom formats defined in `Public`, `Private` and `TeleportRequest`.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Public`

-   **Description:**

    This setting allows you to specify a custom chat message format using Regex (Regular expressions).

    More on Regex [here](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference).

    Only works when `Builtins` is set to `false`.

-   **Type:** `string`

-   **Default:** `Public = "^<([a-zA-Z0-9_]+)> (.+)$"`

#### `Private`

-   **Description:**

    This setting allows you to specify a custom chat message format for private messages using Regex (Regular expressions).

    More on Regex [here](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference).

    Only works when `Builtins` is set to `false`.

-   **Type:** `string`

-   **Default:** `Private = "^([a-zA-Z0-9_]+) whispers to you: (.+)$"`

#### `TeleportRequest`

-   **Description:**

    This setting allows you to specify a custom chat message format for a Teleport request using Regex (Regular expressions).

    More on Regex [here](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference).

    Only works when `Builtins` is set to `false`.

-   **Type:** `string`

-   **Default:** `TeleportRequest = '^([a-zA-Z0-9_]+) has requested (?:to|that you) teleport to (?:you|them)\.$'`

## Chat Bot section

-   **Section header:** `ChatBot`

-   **Description:**

    This top-level section groups the built-in bot configs that ship with MCC.

    The detailed options for each bot are documented in [Chat Bots](chat-bots.md), so this page only covers the shared runtime and client settings.
