# Events and game APIs

Event handlers run when the host delivers an event. They can filter its fields and apply a cooldown before performing work.

The current dispatcher acquires a named cooldown before evaluating `when`. A record that fails the filter can still consume the cooldown.

## React to chat

```beacon
# beacon 1
# needs: chat.send
on chat as e when e.message contains "!help"
  whisper e.player "Available commands: !help and !rules."
end on
on join
  say "Welcome, {player}!"
end on
```

Without `as e`, the handler uses the name `event` for its record. Event fields also work as bare names inside handlers. They take precedence over globals.

`join` and `leave` follow the server tab list. They do not describe every entity entering render distance.

| Event | Useful fields or behavior |
| --- | --- |
| `chat` | `player`, `message`, `raw`, `is_private` |
| `whisper` | `player`, `message`, `raw` |
| `server_message` | `text`, `translation_key` |
| `raw_chat` | Untouched `raw`, plus `category`, `sender`, `body`, `verified` and protocol metadata |
| `player_list` | `players`, `count`, `header`, `footer` |
| `container_open` | `window`, `title`, `kind` |
| `container_close` | `window` |
| `entity_add`, `entity_remove` | Entity identity, type, name, position, distance and pose |
| `dialog` | `title`, `body`, `inputs`, `buttons`, `input_keys`, `registry_id` |
| `start` | Script loaded |
| `login`, `logout`, `disconnect`, `reconnect` | Connection lifecycle |
| `death`, `respawn`, `health`, `hunger`, `inventory`, `kick`, `tps` | Game or session changes |

`raw_chat` observes messages alongside parsed dispatch. It never suppresses them. `stop event` only suppresses hooks that support suppression.

Entity events require `entity.read`. The first entity poll establishes a baseline. It does not emit additions for every already-tracked entity.

## Timers

```beacon
# beacon 1
in 5 seconds do
  show "One-shot timer"
end in
every 60 seconds
  show "Recurring timer"
end every
```

A one-shot body runs once. Reconnect cancels pending one-shots and waits. A missed recurring tick runs at most once before cadence resumes.

A handler can use `cooldown 300 seconds named "warning"`. The cooldown limits repeated handling, independently of chat throttling.

## Send the right type of output

| Operation | Capability | Behavior |
| --- | --- | --- |
| `show "text"` | None | Local script output |
| `say "text"` | `chat.send` | Public chat, without a leading slash |
| `whisper "Steve" "text"` | `chat.send` | Private message |
| `server "/home"` | `server.send` | Server command, with a leading slash |
| `set result to mcc "help"` | `mcc.run` | Internal command output as text |
| `disconnect "Finished"` | `server.disconnect` | Leave the server and stop automatic reconnect |

1. Use `server` for server commands.
2. Use `show` for multiline internal-command output.
3. Add delays when sending repeated messages.

All scripts share a chat allowance of eight messages per ten seconds. Excess messages queue and produce warnings. `chat_bucket()` exposes the current bucket state.

## Read and act on game state

| Area | Examples | Requirements |
| --- | --- | --- |
| Player | `me.health`, `me.food`, `me.pos`, `me.effects` | A session for live values |
| Server | `server.tps`, `server.protocol`, `server.score("kills", "Alice")` | Tracked server state |
| Inventory reads | `inv.list`, `inv.count`, `inv.has`, `inv.container` | `inventory.read`, inventory tracking |
| Inventory actions | `inv.select`, `inv.drop`, `inv.move`, `inv.take`, `inv.put` | `inventory.write` and negotiated action support |
| World reads | `world.block_at`, `world.sign_text` | `world.read`, terrain tracking |
| World search | `world.find_blocks`, `world.find_signs` | `world.search`, terrain tracking |
| World actions | `world.dig`, `world.place`, `world.use`, `world.looking_at` | `world.write`, gameplay gates and mutation allowance |
| Entity reads | `entities.near`, `entities.by_id`, `entities.nearest` | `entity.read`, entity tracking |
| Entity actions | `attack`, `interact`, entity following | `entity.write`, action support |
| Dialogs | `dialog.show`, `dialog.answer`, `dialog.close` | `dialog.read` or `dialog.write` |

Capabilities do not enable tracking or make an unsupported protocol action available. Missing game prerequisites can still cause a catchable failure.

`world.find_blocks("chest", 16, 10)` finds nearby tracked blocks. Searches cap radius at 32 blocks and results at 64.

`entities.near(radius, max)` returns tracked rows, nearest first. Radius defaults to 64 and caps at 128. Results cap at 64.

Entity IDs last for one session. Do not persist an ID for use after reconnect.

## Movement and containers

`move_goto` and `move_follow` require the `movement` capability. `move_goto(120, 65, -40, {tolerance: 2, sneak: no})` finishes on arrival. `move_follow("Steve")` continues until cancellation.

The newest movement request wins. The previous request receives a superseded error. `stop_moving()` cancels steering.

1. Open a container before trading or enchanting.
2. Wait for `container_open` before reading its slots.
3. Catch failures if the window closes during an action.

`trade.list()` reads offers. `trade.buy(index, count)` returns completed units. `enchant.options()` reads choices. `enchant.choose("top")` selects one.

## Server dialogs

1. Match a stable input key in the `dialog` event.
2. Use `dialog.answer` to submit values and press a button in one operation.
3. Keep passwords out of chat and local output.

Button numbers start at one. Titles can change with translation. Input keys provide a more stable filter.

## Execution limits

Event work receives 100,000 fuel units and a five-second wall-clock budget. The runtime limits function call depth to 256.

World mutations have a per-script allowance of eight actions per ten seconds. A refusal reports a retry delay. A successful local action is not proof that a server accepted the result.

Next: [State and integrations](state-and-integrations.md).

## Event fields are observations

A handler receives values from the event that triggered it. Those values describe that notification. Later reads such as `me.pos` can describe a newer observation.

A player's chat name is not automatically an administrator identity. A script that accepts remote commands needs its own explicit authorization rule. A simple greeting does not need that authority.

Filters should match the input you intend to accept. `contains "!help"` also matches longer messages. Use `trim(lower(e.message)) is "!help"` for an exact command word.

## Handle absent observations

A tracked value can be unavailable before login or before its first update. Check absence before formatting or calculating from it.

```beacon
# beacon 1
if me.pos is set then
  show "Position is available"
else
  show "Position is not available yet"
end if
```

This script reports the available branch. Offline execution reports `Position is not available yet`. It does not enable movement or terrain tracking.

## Inventory selectors

An inventory selector can be a simple item name, such as `"torch"`. Matching accepts the optional `minecraft:` prefix. It treats underscores, spaces, and hyphens as equivalent for basic names.

A selector map can add criteria, including `type`, `name`, `name_contains`, `lore_contains`, and `min_count`. Use a simple selector first. Add criteria only when several item variants need separation.

Reading a slot snapshot does not move an item. An inventory action requests a server-side change. After the action, check the observed inventory again when your next decision depends on it.

## World and entity searches

A world search sees terrain that the client tracks. It does not scan the entire server. A missing result can mean that the block is absent or that the client does not know that area.

An entity search sees tracked entities. An entity can disappear before the action uses its ID. Catch this failure and obtain a fresh observation before retrying.

Start with read-only searches. Test writes separately on a controlled server. The [nearby chests and census recipes](recipes.md) show complete read workflows.

## Session transitions

A client can outlive one connection. Disconnect invalidates session-specific data and cancels work tied to that session. Reconnect does not preserve old entity IDs or open windows.

Load-time `start` and session `login` describe different moments. Place session work in the appropriate event or invoke it only after the host reports connection.

For an exact chat filter and a complete event test, read [Chapter 4](guide/04-events.md).

## Complete built-in event fields

Missing optional values appear as `none`. Check them before performing numeric calculations or reading nested fields.

| Event | Fields |
| --- | --- |
| `chat` | `player`, `message`, `raw`, `is_private` |
| `whisper` | `player`, `message`, `raw` |
| `server_message` | `text`, `translation_key` |
| `raw_chat` | `raw`, `category`, `sender`, `sender_id`, `chat_type`, `target`, `body`, `translation_key`, `verified` |
| `join`, `leave` | `player` |
| `death` | `player`, `cause` |
| `respawn` | `player` |
| `health` | `health`, `max_health`, `change` |
| `hunger` | `food`, `saturation`, `change` |
| `inventory` | `slots_changed`, a list of numeric slot IDs |
| `start`, `login`, `logout`, `reconnect` | No built-in payload fields |
| `disconnect`, `kick` | `reason` |
| `tps` | `tps`, `mspt`, each possibly `none` |
| `player_list` | `players`, `count`, `header`, `footer` |
| `container_open` | `window`, `title`, `kind` |
| `container_close` | `window` |
| `entity_add`, `entity_remove` | `id`, `uuid`, `type`, `name`, `custom_name`, `is_player`, `x`, `y`, `z`, `distance`, `yaw`, `pitch`, `pose`, `on_ground` |
| `dialog` | `title`, `body`, `inputs`, `buttons`, `input_keys`, `registry_id` |

Dialog input rows contain `key`, `label`, `kind`, and `value`. Button rows contain `index` and `label`.

An entity removal event contains the last known row. It does not make the removed entity available for actions.

Plugins can publish additional named events. Their plugin documentation defines those fields and the required capability.

## Suppression and cooldowns

`stop event` ends the current handler. For `chat`, `whisper`, and `server_message`, it can suppress the triggering message in MCC's presentation path.

It does not cancel a message already delivered to the server. `raw_chat` remains observable and cannot be suppressed.

Other hooks record a suppression note but do not cancel the underlying game event. Use `stop event` there only to end the handler.

A named cooldown belongs to a script and name. Two handlers with the same cooldown name share its window within that script.

The runtime acquires the cooldown before evaluating `when`. A non-matching notification can therefore consume the window. Account for this when choosing a filter.
