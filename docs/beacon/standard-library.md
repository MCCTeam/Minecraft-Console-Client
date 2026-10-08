# Beacon standard library

A function returns a value. Call it with parentheses, such as `len(names)`. A namespace groups related functions, such as `inv.count("torch")`.

Square brackets in signatures below mark optional arguments. Do not type those signature brackets around an argument.

## Text and conversion

| Function | Result and behavior |
| --- | --- |
| `text(value)` | Convert a value to display text. |
| `number(value)` | Convert suitable text or a value to a number. Invalid input can fail. |
| `yesno(value)` | Return whether the value is neither `no` nor `none`. |
| `len(value)` | Count text characters, list items, or map entries. |
| `lower(text)`, `upper(text)` | Change text case using invariant rules. |
| `trim(text)`, `trim_start(text)`, `trim_end(text)` | Remove whitespace from both ends, the start, or the end. |
| `split(text, separator)` | Return a list of text parts. |
| `join(list, separator)` | Return display text joined with the separator. |
| `slice(text_or_list, start[, end])` | Return a range. Positions start at zero. The end is exclusive. |
| `replace(text, old, new)` | Replace every exact occurrence. The old text must be non-empty. |
| `replace_first(text, old, new)` | Replace the first exact occurrence. |
| `index_of(text, needle[, start])` | Return the first zero-based position or `none`. |
| `pad_start(text, width[, pad])`, `pad_end(text, width[, pad])` | Pad to the chosen width. The default pad is a space. |
| `repeat_str(text, count)` | Repeat text a non-negative number of times. |
| `escape_regex(text)` | Escape text for use as a literal regular-expression component. |
| `json_parse(text)` | Parse JSON objects as maps and arrays as lists. |
| `json_stringify(value)` | Encode a value as JSON. The runtime limits nesting to 32 levels. |

```beacon
# beacon 1
set names to split("Alice,Bob", ",")
assert(len(names) is 2, "two names")
show join(names, " and ")
show slice("Minecraft", 0, 4)
```

Expected output is `Alice and Bob`, then `Mine`.

## Collections and matching

| Function | Result and behavior |
| --- | --- |
| `sort(list)` | Return a sorted copy. All items must be numbers or all must be text. |
| `reverse(text_or_list)` | Return a reversed copy. |
| `unique(list)` | Remove repeated values and preserve first-seen order. |
| `keys(map)` | Return keys in ordinal key order. |
| `values(map)` | Return values in ordinal key order. |
| `has_key(map, key)` | Return yes/no for a text key. |
| `match(text, pattern)` | Return the first match record, or `none`. |
| `match_all(text, pattern)` | Return a list of match records. |

A regular expression describes a text pattern. A slash-delimited pattern such as `/[0-9]+/` can match digits.

Patterns can fail to compile or exceed the matching timeout. Catch those failures when the pattern comes from user input.

## Numbers and randomness

| Function | Result and behavior |
| --- | --- |
| `min(values...)`, `max(values...)` | Return the lowest or highest number. A numeric list is also accepted. |
| `clamp(value, low, high)` | Keep a number within the bounds. |
| `round(value[, digits])` | Round a number. Half values round away from zero. |
| `abs(value)` | Return the non-negative magnitude. |
| `log(value[, base])` | Return the natural logarithm, or use an explicit positive base other than one. |
| `random(max)` | Return an integer from zero through `max - 1`. `max` must be a positive integer. |
| `pick(list)` | Return a random item, or `none` for an empty list. |
| `chance(probability)` | Return yes/no. Probability must be between zero and one. |
| `assert(condition[, label])` | Return `yes`, or raise an error when the condition is `no`. |

```beacon
# beacon 1
assert(round(2.5) is 3, "rounding")
assert(clamp(25, 0, 20) is 20, "bounds")
set roll to random(6) + 1
assert(roll >= 1 and roll <= 6, "die range")
show "Rolled {roll}"
```

Run with `--seed 42` to repeat the random choice during offline tests.

## State, command input, and tasks

| Function or value | Meaning |
| --- | --- |
| `saved(key)` | Return a saved value, or `none` when absent. |
| `save key to value` | Store a value under that text key. This is a statement. |
| `settings.name` | Read a declared setting with the user's overlay. |
| `shared["feature.key"]` | Read a RAM-only value shared by scripts. |
| `arg(name)` | Read a script command's text argument, or `none`. |
| `tasks()` | List active background task records, including IDs. |
| `chat_bucket()` | Read `burst`, `window_seconds`, `available`, and `muted`. |

Use `lock shared` for shared updates. [State and integrations](state-and-integrations.md) explains storage and provider lifetimes.

## Game reads

| Function | What it reads |
| --- | --- |
| `online_players([count])` | A bounded page of observed player names. |
| `chat_history([count])` | Bounded recent observed chat text. |
| `last_from(player)` | Last observed text from that player, or `none`. |
| `count_matching(text, minutes)` | Recent observed chat containing the text, without case sensitivity. |
| `server.scoreboard()` | A scoreboard snapshot. |
| `server.bossbars()` | Observed bossbar rows. |
| `server.score(objective, holder)` | One observed scoreboard value. |
| `world.block_at(x, y, z)` | One loaded block observation. |
| `world.light_at(x, y, z)` | One loaded light observation. |
| `world.biome_at(x, y, z)` | One loaded biome observation. |
| `world.sign_text(x, y, z)` | Observed sign text. |
| `world.find_blocks(type, radius, maximum)` | Nearby tracked blocks that match a type. |
| `world.find_signs(text, radius, maximum)` | Nearby tracked signs that match text. |
| `world.dig(x, y, z[, face])` | Request digging and return a `broken`/`detail` record. |
| `world.place(x, y, z[, face])` | Request placement against the selected face. |
| `world.use(x, y, z)` | Request opening a container and return its observation or `none`. |
| `world.looking_at([range])` | The current block hit observation, when available. |
| `entities.near([radius[, maximum]])` | Nearby tracked entity rows, nearest first. |
| `entities.of_type(type[, radius[, maximum]])` | Nearby rows for one entity type. |
| `entities.by_id(id)` | A currently tracked entity row, or `none`. |
| `entities.nearest([type[, radius]])` | The nearest matching row, or `none`. |
| `entities.count([type[, radius]])` | Count matching tracked entities. |
| `craft_list()` | Available recipe identifiers observed by the host. |

`world.dig`, `world.place`, `world.use`, and `world.looking_at` use the `world.write` gate. The first three can change the server.

Game reads need the corresponding tracking configuration. Empty observations do not prove that a resource is absent from the server.

The [game reference](events-and-game.md) explains capabilities, events, and action limits.

## Inventory, containers, trading, and enchanting

| Function | Purpose |
| --- | --- |
| `inv.list()` | Read inventory slot rows. |
| `inv.count(selector)` | Count items that match a selector. |
| `inv.has(selector[, count])` | Test whether enough matching items exist. |
| `inv.find(selector)`, `inv.find_all(selector)` | Read matching slot rows. |
| `inv.selected()` | Read the selected hotbar slot. |
| `inv.armor()` | Read armor slot rows. |
| `inv.container()` | Read the current container observation. |
| `inv.select(slot)` | Select a hotbar slot from zero through eight. |
| `inv.drop()`, `inv.drop_stack()` | Drop one selected item or its stack. |
| `inv.move(from, to)` | Move between inventory slots. `to` can be a supported named slot. |
| `inv.click(slot[, mode])` | Request a container click. |
| `inv.take(slot[, count])` | Take items from an open container slot. |
| `inv.put(slot)` | Put an inventory slot into the open container. |
| `trade.list()` | Read the open trade offers. |
| `trade.select(index)` | Select an offer. |
| `trade.buy(index[, count])` | Buy units and return the completed count. |
| `enchant.options()` | Read available enchanting choices. |
| `enchant.choose(choice)` | Select a choice, such as `"top"`. |
| `craft_one(recipe)` | Request one craft and return yes/no. |
| `eat()` | Request use of the best available food. |
| `use_in_hand()` | Request use of the held item. |

A selector is item text, such as `"torch"`, or a criteria map. Common map fields include `type`, `name`, `name_contains`, `lore_contains`, and `min_count`.

Action methods request a change. Read the next observed inventory before deciding that the server completed it.

## Movement and entity actions

| Function | Purpose |
| --- | --- |
| `move_goto(x, y, z[, options])` | Move toward coordinates and finish on arrival. A position map is also accepted. |
| `move_follow(player_or_entity)` | Continue following until cancellation or failure. |
| `stop_moving()` | Cancel steering. |
| `look_at(x, y, z)` | Look toward coordinates. An entity row is also accepted. |
| `attack(entity)` | Request an attack on a tracked entity. |
| `interact(entity)` | Request interaction with a tracked entity. |

Options for `move_goto` include `tolerance`, `sneak`, and `sprint`. Start with a simple destination in loaded terrain.

The newest movement request replaces the previous request. Catch failures when a target disappears or a session ends.

## Server dialogs

| Function | Purpose |
| --- | --- |
| `dialog.show()` | Read the active dialog. |
| `dialog.set(key, value)` | Set one input value. |
| `dialog.click(button)` | Click a one-based button number. |
| `dialog.answer(values[, button])` | Submit a map of input values and click a button. |
| `dialog.close()` | Close the active dialog. |

Input keys remain more stable than translated titles. Do not print private form values into chat.

## Time

`time.stamp` supplies Unix time in seconds. `time.now` supplies `HH:mm` text. `time.format(stamp, format)` returns formatted text. `time.ago(stamp)` describes elapsed time.

Timers use `in ... seconds do` and `every ... seconds`. `wait` pauses a task. See [Chapter 5](guide/05-time.md).

## Files and HTTP

| Function | Capability | Result |
| --- | --- | --- |
| `file_read(path)` | `fs.read` | Read text inside `scripts/data`. |
| `file_write(path, text)` | `fs.write` | Write text inside `scripts/data` and return `yes`. |
| `http_get(url)` | `net.fetch` | Fetch response text from an allowed HTTPS host. |
| `http_post(url, body)` | `net.fetch` | Post text and return response text. |

These calls need normal MCC configuration. The offline runner does not provide a persistent data directory or a network allowlist.

Network calls also require the destination in `configurations/beacon.toml`. See [file and network configuration](state-and-integrations.md#files-and-network).

## Read-only value maps

| Value | Fields or content |
| --- | --- |
| `me` | `name`, `health`, `max_health`, `food`, `saturation`, `air`, `xp_level`, `armor`, `gamemode`, `yaw`, `pitch`, `ping`, `is_sneaking`, `pos`, `effects` |
| `me.pos` | `x`, `y`, `z`, when available |
| `me.effects` | Rows containing `name`, `level`, and `seconds_left` |
| `server` | `online_count`, `tps`, `mspt`, `protocol`, `ip`, `port`, `version_name`, `max_players`, `motd`, `day_time`, `day`, `weather`, plus observed scoreboards and bossbars |
| `game` | Current `protocol` and supported `protocols` |
| `time` | `hour`, `minute`, `stamp`, `now`, `date`, `today` |
| `beacon` | `lib`, the current standard-library version |
| `items`, `effects`, `enchants` | Named game identifiers for the observed protocol, with offline fallback tables |
| `settings` | Scalar values declared by the current script's header |
| `online_players` | An observed list of player names. The function form also accepts a count. |

A field can be `none` until MCC receives the relevant observation. `me.uuid` is currently unavailable and remains `none`.

Do not assign to these read-only maps. Use documented action functions to request game changes.

```beacon
# beacon 1
show "Beacon library: {beacon.lib}"
show "Clock: {time.now}"
if me.health is set then
  show "Health: {me.health}"
end if
```

The offline clock uses a repeatable test value. It does not show the real server's time.
