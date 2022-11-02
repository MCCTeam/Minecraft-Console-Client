# Configuration

**Minecraft Console Client** can be both configured by the [command line parameters](usage.md#command-line-parameters) and the configuration file.

By the default all of the configurations are stored in the configuration file named `MinecraftClient.ini` which is created the first time you run the program, but you also can specify your own configuration file by providing a path to it as a first parameter when starting the MCC, check out [Usage](usage.md#quick-usage-of-mcc-with-examples) for examples.

> **⚠️ IMPORTANT WARNING: Recently we have changed the configuration format from INI to TOML, the documentation had to be updated. If you spot a mistake, please report it on our Discord or in the repository as an issue.**

## Notes

-   Some settings will be omitted from the documentation due to them being not used often, we do not want documentation to be cluttered, we advise you to manually read through the configuration file, where every setting has a description next to it.
-   Some plugin/bot related settings will be covered in the plugins section, not here

## Configuration File

### Format

The configuration file uses the [TOML format](https://toml.io/en/), all of the options are key-value pairs separated into sections.

Sections are defined in-between the square brackets (Example: `[This is a section]`), each occurrence of this marks a beginning of a new section.

The settings/options are defined as key-value pairs, where the name of the setting and the value are separated by the equals sign `=` (Example: `some-setting=some value`).

Lines starting with `#` are comments, they do not have an effect on the configuration of the program, their purpose is purely a descriptive one.

**To get familiar with all the data types and styles of settings please read the [official TOML documenation](https://toml.io/en/v1.0.0).**

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
Coordinate = { x = 145, y = 64, y = 2045 }
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

    This setting is where you define the type of your account: `mojang` or `microsoft`

    > **ℹ️ NOTE: Mojang accounts are going to stop working soon for everyone, they already are not working for some people.**

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

### Main Advanced section

-   **Section header:** `Main.Advanced`

#### `Language`

-   **Description:**

    This setting is where you define which language you want to use.

    When connecting to 1.6+ servers, you will need a translation file to display properly some chat messages.
    These files describe how some messages should be printed depending on your preferred language.

    The client will automatically load `en_GB.lang` from your Minecraft folder if Minecraft is installed on your computer, or download it from Mojang's servers. You may choose another language in the configuration file.

    To find your language code, check [this link](https://github.com/MCCTeam/Minecraft-Console-Client/discussions/2239s).

-   **Type:** `string`

-   **Default:** `en_gb`

-   **Example:**

    ```
    Language = "en_gb"
    ```

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

    > **⚠️ WARNING: Admins can impersonate players on versions older than 1.19**

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

    > **ℹ️ NOTE: MCC supports only 1.4.6 - 1.19.2**

#### `EnableForge`

-   **Description:**

    This setting is where you can define if you're playing on a forge server.

-   **Type:** `string`

-   **Available options:**

    -   `auto`
    -   `no`
    -   `force`

-   **Default:** `auto`

    > **ℹ️ NOTE: Force-enabling only works for MC 1.13 +**

#### `BrandInfo`

-   **Description:**

    This setting is where you can change how MCC identifies itself to the server.
    It can be whatever you like, example: `vanilla`, `mcc`, `empty`.

-   **Type:** `string`

-   **Default:** `mcc`

    > **ℹ️ NOTE: For playing on Hypixel you need to use `vanilla`**

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

    ![ASCII Art here](http://i.pics.rs/33yn9.png "ASCII Art here")

-   **Type:** `boolean`

-   **Default:** `true`

#### `TerrainAndMovements`

-   **Description:**

    This setting is where you can set if you want to enable terrain movement, so you can use command like `/move` and some bots.

    > **⚠️ WARNING: This feature is currently not supported in `1.4.6 - 1.6`.**

-   **Type:** `boolean`

-   **Default:** `false`

> **ℹ️ NOTE: Sometimes the latest versions might not support this straight away, since Mojang often makes changes to this.**

#### `InventoryHandling`

-   **Description:**

    This setting is where you can set if you want to enable inventory handling using the `/inventory` command.

    > **⚠️ WARNING: This feature is currently not supported in `1.4.6 - 1.9`.**

-   **Type:** `boolean`

-   **Default:** `false`

#### `EntityHandling`

-   **Description:**

    This setting is where you can set if you want to enable interactions with entities such as players, mobs, minecarts, etc..

    > **⚠️ WARNING: This feature is currently not supported in `1.4.6 - 1.9`.**

-   **Type:** `boolean`

-   **Default:** `false`

    > **ℹ️ NOTE: Sometimes the latest versions might not support this straight away, since Mojang often makes changes to this.**

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

    > **ℹ️ NOTE: Only works on Windows XP-8 or Windows 10 with old console**

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

    > **ℹ️ NOTE: Make sure the spawn point is safe**

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

> **⚠️ WARNING: A movement speed higher than 2 may be considered cheating by some plugins.**

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

-   **Default:** `false`

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

-   **Default:** `false`

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

### Logging section

-   **Section header:** `Logging`

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

    > **ℹ️ NOTE: Not filtering anything by default**

#### `DebugFilterRegex`

-   **Description:**

    This setting allows you to define if your want to filter debug messages being logged using a Regex expression.

    More on Regex [here](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference).

-   **Type:** `string`

-   **Default:** `.*`

    > **ℹ️ NOTE: Not filtering anything by default**

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

    > **ℹ️ NOTE: %username% and %serverip% will be substituted with your username and the IP address of the server you are connected to. So you can use something like: `console-log-%username%-%serverip%.txt`**

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

    > **ℹ️ NOTE: `%username%`, `%serverip%`, `%datetime%` are reserved variables**

-   **Section header:** `Logging`

-   **Examples:**

    ```
    your_var = "your_value"
    "your var 2" = "your value 2"
    ```

## Proxy section

-   **Section header:** `Proxy`

-   **Description:**

    Connect to a server via a proxy instead of connecting directly.

#### `Enabled_Login`

-   **Description:**

    If Mojang session services or Microsoft login services are blocked on your network or your ip is blacklisted or rate limited by Microsoft, set the value to
    `true`.

-   **Type:** `boolean`

-   **Default:** `false`

#### `Enabled_Ingame`

-   **Description:**

    Whether to connect to the game server through a proxy.

    If connecting to a port 25565 (Minecraft) is blocked on your network, set the value to `true` to login and connect using the proxy.

-   **Type:** `boolean`

-   **Default:** `false`

    > **⚠️ WARNING: Make sure your server rules allow Proxies or VPNs before setting the setting to `true`, or you may face consequences!**

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

    -   `HTTPT`
    -   `SOCKS4`
    -   `SOCKS4a`
    -   `SOCKS5`

-   **Type:** `string`

-   **Default:** `HTTPT`

#### `Username`

-   **Description:**

    The proxy account username.

    Only needed for password protected proxies.

-   **Default:** ` `

#### `Password`

-   **Description:**

    The proxy account password.

    Only needed for password protected proxies.

-   **Default:** ` `

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

-   **Default:** `normal`

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
