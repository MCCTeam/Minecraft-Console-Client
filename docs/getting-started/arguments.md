# Startup arguments

Startup arguments run before the MCC prompt opens. They differ from commands typed inside MCC.

```text
Mcc.Cli [username] [password|-] [host[:port]]
        [--configurations <folder>] [-v <version>]
        [--auth <mode>] [--auth-server <url>]
        [--section.setting=value]
        [--console.table.key=value]
```

Use `./Mcc.Cli` on Linux/macOS and `.\Mcc.Cli.exe` in Windows PowerShell.

## Positional arguments

| Position | Value | Meaning |
| --- | --- | --- |
| 1 | `username` | Account login hint or offline player name |
| 2 | `password` or `-` | `-` selects offline login unless explicit `--auth` overrides it |
| 3 | `host[:port]` | Server address, with port `25565` when omitted |

Fill earlier positions to supply a later position. A non-`-` password is accepted but not used by the current login flow. Online authentication is interactive. Do not pass a Microsoft password on the command line.

Private offline server:

```bash
./Mcc.Cli GuideBot - localhost:25565
```

Microsoft login:

```bash
./Mcc.Cli GuideBot - play.example.net --auth microsoft
```

Explicit `--auth microsoft` wins over the `-` sentinel. Prefer a saved account when possible.

## Options

| Option | Behavior |
| --- | --- |
| `--help`, `-h`, `-?`, `/?` | Print friendly help and exit without generating configuration |
| `--help-short` | Print compact help and exit, even when full help is also requested |
| `--configurations <folder>` | Select the configuration folder |
| `--configurations=<folder>` | Equivalent long-option form |
| `-v <version>` | Select an explicit Minecraft version |
| `--auth <mode>` | Select `offline`, `microsoft`, `microsoft-browser`, or `yggdrasil` |
| `--auth-server <url>` | Select the Yggdrasil authentication provider |
| `--exercise smoke` | Run a diagnostic game exercise after joining, then exit |
| `--section.setting=value` | Override one supported `client.toml` setting for this run |
| `--console.table.key=value` | Override one `console.toml` setting for this run |
| `--validate-plugin <folder>` | Validate a plugin folder without loading it |
| `--validate-marketplace <source>` | Validate a schema-2 marketplace index and release catalogues |

The parser also accepts `--auth=...`, `--auth-server=...`, and `--exercise=...`. A configuration folder is an option, not a positional argument.

## Set temporary options

```bash
./Mcc.Cli --configurations ./configurations --connection.auto-connect=false
./Mcc.Cli --console.General.ConsoleMode=tui
./Mcc.Cli --console.General.Glyphs=ascii --console.General.ConsoleColorMode=disable
./Mcc.Cli --connection.version=1.21.5
```

Client setting names ignore case and remove hyphens. For console settings, use the documented table and key.

Precedence is file defaults, selected account/server, positional values, then dotted overrides. If a dotted override repeats, the later value wins. Overrides do not edit files.

Disabling terrain alone is not a complete chat-only configuration. Physics needs terrain. Pathfinding needs physics. See [gameplay switches](../client/configuration.md#gameplay).

## Supported dotted overrides

The portable binder supports a subset of the file schema. An unknown path produces a warning and has no effect.

| Table | Supported keys |
| --- | --- |
| `connection` | `host`, `port`, `version`, `srvresolve`, `tcptimeout`, `brand`, `autoconnect` |
| `connection.reconnect` | `maxattempts`, `delayseconds`, `backofffactor`, `maxdelayseconds` |
| `gameplay` | `terrain`, `inventory`, `entity`, `physics`, `pathfinding`, `autorespawn`, `moveheadwhilewalking`, `movementspeed`, `temporaryfixbadpacket`, `ignoreinvalidplayername`, `showeffectmessages` |
| `chat` | `messagecooldown`, `maxchatmessagelength`, `privatemessagecommand` |
| `clientsettings` | `enabled`, `locale`, `renderdistance`, `difficulty`, `chatmode`, `chatcolors`, `mainhand` |
| `localization` | `language` |
| `logging` | `debugmessages`, `packetdebugmessages`, `chatmessages`, `infomessages`, `warningmessages`, `errormessages`, `logtofile`, `logfile`, `filtermode`, `prependtimestamp`, `savecolorcodes` |
| `permissions` | `commandprefix` |

For example, `--connection.reconnect.maxattempts=5` is supported. Change diagnostics, plugin crash limits, signature settings, filters, and unlisted keys in their TOML files instead.

The console binder supports its General, CommandSuggestion, Minimap, and TabList keys. Prefix a console path with `console`, as in `--console.Minimap.Zoom=2`.

## Beacon tools without a connection

The first argument selects an offline tool:

```bash
./Mcc.Cli lint ./scripts/hello.bcn
./Mcc.Cli run ./scripts/hello.bcn
./Mcc.Cli format ./scripts/hello.bcn --check
```

These tools run before configuration loading. `run` uses an inert game host. It cannot prove that a real server action works. Read [Beacon](../beacon/index.md) for tool options and the tutorial.

## Validate packages and catalogues

```bash
./Mcc.Cli --validate-plugin ./my-plugin
./Mcc.Cli --validate-marketplace ./my-marketplace/marketplace/mcc-marketplace.toml
```

The marketplace check reads release metadata. It does not install every release or execute plugins. Plugin validation inspects structure and the manifest. Live loading remains a separate test.

## Exit codes

| Normal client code | Meaning |
| --- | --- |
| `0` | Clean exit |
| `1` | Invalid startup arguments or configuration |
| `2` | Minecraft version resolution failed |
| `3` | Connection failed or the scripted session lost its connection |
| `4` | Authentication or login failed |

`/exit <code>` can supply a custom code. A remote disconnect in an interactive session can leave the prompt open for `/reco` or `/connect`.

Beacon tools use their own exit-code contract. Do not interpret an offline lint result as a connection result.
