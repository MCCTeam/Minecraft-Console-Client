# Alerts

Watches chat and game events, then sends notifications to the destinations that you select.

| Package ID | Plugin release | Manifest capabilities |
| --- | --- | --- |
| `alerts` | `3.0.2` | `commands` |

The plugin lives in the separate [Marketplace repository](https://github.com/MCCTeam/Marketplace/tree/master/src/Alerts). It is not built into MCC. Its declared game/resource usage is `chat`, `files`, `network`. These declarations describe usage, not permission boundaries.

Declared plugin providers:

| Provider | Version range | Requirement |
| --- | --- | --- |
| `llm-core` | `^0.2.0` | Optional |

Required providers must resolve before activation. Optional providers do not install automatically. Incompatible optional providers remain unavailable.

## Install and activate

Use the [plugin management guide](../managing.md) to install a released archive or local package. A source checkout by itself is not a compiled plugin package.

The official marketplace is registered by default. Enter:

```text
/plugins install alerts@official 3.0.2
/plugins enable alerts
/plugins info alerts
```

1. Check that `/plugins list` shows `alerts` as loaded.
2. Open `plugins/userdata/alerts/settings.toml` under your plugin root.
3. Change the settings for your task.
4. Save the file.
5. Enter `/plugins reload alerts`.

The plugin root normally sits beside the configurations directory. `MCC_PLUGINS` can select another root. Never edit a manifest under `versions/` to enable a plugin.

## First working example

Apply these values to the existing settings file. Keep unrelated values unless the example says otherwise. Replace example IDs, names, and coordinates with your own values.

```toml
Enabled = true
LogToFile = true
LogFile = "alerts-log.txt"
[[Rule]]
Name = "help-request"
Match = ["help"]
Routes = ["file"]
Beep = false
```

Ask another player to send a chat line that contains `help`. Check the console notification and the configured alert log.

```text
alerts smart <off|rules|full|status>
alerts batch <on|off|status>
alerts rules
alerts events
alerts routes
alerts routes set <rule> <routes>
alerts routes mode <fixed|model>
alerts test <line>
```

## Settings reference

The values below come from the repository bundled settings. The installed package can carry different defaults for another release.

| Setting | Bundled value | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Controls the plugin behavior after activation. |
| `UseRegex` | `false` | Default for rules. Any rule with Regex = true uses .NET regular expressions. |
| `Excludes` | `[ "joined the game", "left the game" ]` | Case-insensitive suppression substrings. Any match here cancels the alert. |
| `RouteMode` | `"fixed"` | fixed sends every alert to all its routes. model lets the judge pick a subset. |
| `PriorityPatterns` | `[ "whispers:" ]` | Lines matching one of these skip the batch queue and judge at once. |
| `LogToFile` | `false` | Append matched lines to the log file in this plugin's data/ folder. |
| `LogFile` | `"alerts-log.txt"` | Log file name, resolved inside data/. |
| `Discord_Target` | `""` | Where the Discord alert goes: route:name and dm:owner-id entries, comma-separated. Empty means every channel that takes game chat. |
| `Rule.Name` | `"admin-watch"` | Rule name, shown in listings and the log. |
| `Rule.Match` | `[ "alert", "admin", "whisper" ]` | Trigger substrings, or regexes with Regex = true. |
| `Rule.Routes` | `[ "desktop", "discord" ]` | Any of desktop, discord, telegram, file, beep. Empty means all. |
| `Rule.Beep` | `true` | Beep through the host when this rule fires. |
| `Rule.Smart` | `false` | Judge confirms against the prompt before alerting. |
| `Rule.Regex` | `false` | Read this rule's Match list as .NET regular expressions. |
| `Rule.Prompt` | `""` | Per-rule prompt override. Empty means the global Smart.Prompt. |
| `Rule.PromptFile` | `""` | A file in data/ with the prompt. Wins over Prompt when set. |
| `Smart.Mode` | `"off"` | off fires rules directly. rules confirms Smart rules. full also judges the rest. |
| `Smart.Model` | `"local/qwen3-8b"` | Judge model as provider/model-id. |
| `Smart.Confidence` | `0.7` | Verdicts below this are dropped in full mode. |
| `Smart.Prompt` | `""` | What to watch for, in plain words. Empty means the built-in default. |
| `Smart.PromptFile` | `""` | A file in data/ with the prompt. Wins over Prompt when set. |
| `Smart.CatchAllRoutes` | `[ "desktop" ]` | Where full-mode verdicts go. |
| `Smart.Batch` | `false` | Judge low-priority lines in batches to save calls. |
| `Smart.BatchSize` | `10` | Flush the batch at this many lines. |
| `Smart.BatchSeconds` | `15` | Flush the batch after this many seconds. |
| `Smart.Context` | `[ "nick", "position", "server" ]` | Facts sent with each judge call. Empty means chat only. |
| `Smart.Owner` | `""` | The bot owner's name, sent as context when set. |
| `Smart.HistoryLines` | `5` | Trailing chat lines sent as context. |

## Requirements and limits

Desktop notifications depend on the operating system and host. Discord and Telegram routes need their corresponding bridge plugins. Smart matching needs `llm-core` and sends configured chat/context to the selected provider.

## Detailed operation

### Rules

One `[[Rule]]` block is one watch. Give it a name. List the words that
set it off. Big and small letters do not matter. The rule always reads
the full chat line, name of the sender included.

```
[[Rule]]
Name = "admin-watch"
Match = [ "admin", "moderator" ]
Routes = [ "desktop", "discord" ]
Beep = true
```

Anyone who writes "admin" or "moderator" sets it off. You get a screen
popup. The line goes to Discord. Your computer beeps.

Copy the block for each new watch. Ten small rules beat one giant
list. Each rule carries its own destinations. You see at a glance
where each ping goes.

```
[[Rule]]
Name = "theft"
Match = [ "stole", "grief", "raided" ]
Routes = [ "discord", "telegram" ]
Beep = true
```

### Fancy matching

Plain words match parts of words. That misfires. "admin" fires for
"administrator shop open". Patterns fix this.

Set `Regex = true` on the rule. The `Match` list becomes patterns
instead of plain words. Big and small letters still do not matter.

```
[[Rule]]
Name = "help-call"
Match = [ "\\bhelp\\b" ]
Routes = [ "desktop" ]
Regex = true
```

Write two backslashes in the file where you mean one. The settings
file eats one. `\bhelp\b` fires on "help!" and skips "helper". A
pattern with a typo logs one warning and never fires. Typos cannot
break the plugin.

New to patterns? Watch this short video, then test your work against
the official reference:

- https://youtu.be/IzBk58Eorr4
- https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference

Set `UseRegex = true` at the top to flip every rule at once. Leave it
off and flag single rules instead. A rule cannot opt out once the
global switch is on.

### Excludes

`Excludes` is the silence list. Any line matching it stays quiet, even
with a trigger word in it. It runs before every rule. It costs
nothing.

```
Excludes = [ "joined the game", "left the game" ]
```

Join messages repeat names all day. Silence the noisy ones. Keep this
list short. Each entry is one more line that can never ping you.

### Routes

Routes decide where one ping goes. Five exist. Mix them freely per
rule. Leave `Routes` empty to use all five.

- `desktop` shows a popup on your screen. Linux and Windows only.
- `discord` sends the line to DiscordBridge. That plugin must run.
- `telegram` sends the line to TelegramBridge. Same deal.
- `file` writes the line to the log file. Needs `LogToFile = true`.
- `beep` beeps. The rule's own `Beep` flag gates it.

```
Routes = [ "desktop", "telegram" ]
```

The console always prints the ping too. Routes add destinations. They
never remove the console line.

### Events

Some things never arrive as chat. Nobody types your death. A second
table watches game events directly.

```
[[EventRule]]
Name = "death"
On = [ "died", "respawned" ]
Routes = [ "desktop", "discord", "telegram" ]
Beep = true
CooldownSeconds = 0
```

Known event names: `died`, `respawned`, `explosion`, `hurt`,
`health-below-N`, `disconnected`, `difficulty`. The health one takes a
number. Health runs 0 to 20, so `health-below-6` fires at three hearts
or less.

Explosions come in bursts. Pain spams. `CooldownSeconds` caps one rule
to one ping per window. The example below stays quiet for a minute no
matter how many blocks go off.

```
[[EventRule]]
Name = "explosions"
On = [ "explosion" ]
Routes = [ "desktop", "discord" ]
CooldownSeconds = 60
```

Events always fire at once. No judge confirms them first. A death needs no
second opinion. Rain and thunder have no event in the game yet, so they
cannot trigger rules today.

### Smart matching

Keywords are exact and blind. "admin" cannot tell a cry for help from
an ad. An AI judge can. Smart matching sends a chat line to an AI
model and asks one question. Does this deserve a ping.

You need the llm-core plugin for this. Put it in your plugins folder
and turn it on. Alerts keeps working without it. Rules without the
`Smart` flag fire exactly as before.

Three modes exist. Off fires rules directly and spends nothing. Rules
mode asks the judge to confirm each `Smart` rule hit. `Full` mode also
judges every other line against your prompt.

```
[Smart]
Mode = "rules"
Model = "local/qwen3-8b"
Confidence = 0.7
Prompt = "Alert me about griefing, theft, and admins speaking to me. Ignore shop ads and minigame scores."
```

`Model` names the AI that judges, as `provider/model-id`. Small local
models judge fine and cost nothing. Ask them with `/llm model list`.
`Confidence` drops unsure answers in `Full` mode. Start at 0.7. Lower
it when real pings go missing. Raise it when noise gets through.

Mark one rule smart like this. The rest stay instant and free.

```
[[Rule]]
Name = "theft"
Match = [ "stole", "grief" ]
Routes = [ "discord", "telegram" ]
Smart = true
Prompt = "Alert only on real theft against me or my builds. Ignore shop trade chatter."
```

The rule prompt beats the shared one. A long prompt belongs in a file
instead. `PromptFile = "theft.md"` reads
`plugins/userdata/alerts/data/theft.md`. The file wins
over the text whenever both exist.

No core with smart rules on means one warning at startup. Rules fire
directly meanwhile. A wrong model name fails each call with a named
error in the log. `alerts smart status` shows the last error and when
it happened.

### Context

One bare line often needs its surroundings. The plugin can attach
facts to each judge call. Pick what travels. Empty means chat only.
Removed means never sent. Missing means left out, never guessed.

```
[Smart]
Context = [ "nick", "position", "server", "health", "nearby" ]
```

| Key | What the judge learns |
| --- | --- |
| `nick` | Your bot's name. |
| `position` | Where the bot stands, as X Y Z numbers. |
| `place` | The world name, like `minecraft:overworld`. |
| `server` | The server address and port. |
| `world` | The world's clock, in game ticks. |
| `time` | Same as `world`. Either word works. |
| `health` | Hearts and food bars. |
| `food` | Food bar, saturation, and air bubbles. |
| `armor` | Which armor pieces the bot wears. |
| `effects` | Active potion effects. |
| `hands` | The held item and the off-hand item. |
| `inventory` | The fullest pockets, as item counts. |
| `biome` | The biome at the bot's feet. |
| `looking` | The block under the bot's feet. |
| `nearby` | Nearby players with distances. |
| `hostiles` | Nearby monsters with distances. |
| `regulars` | Names the bot hears often. |
| `history` | The last few chat lines. |
| `cooldowns` | Pings that fired lately. Stops repeats. |
| `lag` | Server speed and your ping. |
| `population` | How many players are online. |
| `owner` | Your name, from the `Owner` setting. |

Game chat itself always travels as plain chat text, never as orders.
A hostile line cannot rewrite the facts or the prompt. Outside models
read whatever you list, so keep the list small on purpose.

### Batching

Every judge call costs time and often money. Batching groups quiet
lines into one call. Urgent lines skip the queue. Anything matching
`PriorityPatterns` or holding your name judges at once.

```
[Smart]
Batch = true
BatchSize = 10
BatchSeconds = 15
```

The batch sends at ten lines or fifteen seconds, whichever hits first.
The queue holds 200 lines, then drops the oldest with a warning.
Disconnecting sends what waits. Your chat never freezes waiting.

### Route mode

Fixed mode sends every ping to all its routes. Model mode lets the
judge pick some of them per ping. Theft goes to Telegram. Chatter stays
a popup. Your prompt teaches the split.

```
RouteMode = "model"
```

The judge only picks from the rule routes. It cannot invent a new
destination. An empty or unknown answer falls back to the full list.

### Commands

`alerts rules` lists chat rules with hit counts. `alerts events` lists
event rules with cooldown state. `alerts routes` shows every route in
use. `alerts smart status` shows judge calls, verdicts, errors, and the
last failure. `alerts batch status` shows queue depth and drops.

```
alerts smart rules
alerts routes set theft desktop telegram
alerts routes mode model
alerts batch on
```

`alerts routes set` rewrites one rule routes and saves the file. It
refuses unknown names and shows the valid set. `alerts test` tries one
line on the judge. Tune prompts with it. No ping fires.

```
alerts test "Steve whispered: meet me at spawn with diamonds"
```

The answer shows the verdict, the sureness, the routes, and the
reason. Adjust the prompt until the answers look right. Then go live.

### Costs

Direct rules cost nothing. Each judge call spends computer time and
often money. Three dials control the bill. Smart flags choose which
rules spend. Batching packs lines into fewer calls. The core budget
caps each plugin per day. Watch `alerts smart status` for a week, then
set ceilings you believe.

## When it does not work

1. Enter `/plugins info alerts`.
2. Read any activation error.
3. Enter `/plugins doctor`.
4. Check the required game features and external service configuration.
5. Check `Enabled` in the user settings.
6. Reload only this plugin.

If its command is unknown, the plugin is usually not loaded. A loaded plugin can still have its behavior disabled in settings. These are separate states.

To stop the plugin, enter `/plugins disable alerts`. To remove its package while retaining user data, enter `/plugins uninstall alerts`. Add `purge` only when you intend to delete its settings and data too.

[All official plugins](../official.md) · [Plugin management](../managing.md) · [Make a plugin](../development/index.md)
