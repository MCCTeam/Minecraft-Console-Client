# /entity

Inspect entities that MCC tracks, or send an attack or interaction request.

An entity is a player, mob, vehicle, dropped item, or another object that the server tracks separately from blocks.

Type the examples in MCC's input prompt. They use the default `/` command prefix.

## Before you start

Connect to a server. Enable entity handling in `client.toml`:

```toml
[Gameplay]
Entity = true
```

Edit the existing `[Gameplay]` table. Reconnect after changing it. See [configuration](../client/configuration.md#gameplay).

MCC can inspect only entities that the server sent to its tracker. A list is not a search of the whole world.

For actions, approach the target first. These commands do not move you toward it. The server still controls reach, permissions, and the result.

## Syntax

```text
/entity
/entity ui
/entity near
/entity <id|type>
/entity <id|type> list
/entity <id|type> attack [-a]
/entity <id|type> use
/entity near <id|type>
/entity near <id|type> list
/entity near <id|type> attack [-a]
/entity near <id|type> use
```

Choose an ID or a type where the syntax shows `<id|type>`. Do not type the brackets or `|` character.

- An ID selects one tracked entity, such as `21339`.
- A type selects a kind of entity, such as `minecraft:zombie`. Short names such as `zombie` also work.
- `near` selects the nearest matching entity for a type action. It does not define a fixed search radius.

## Choose the right selector

| Command | Target or result |
| --- | --- |
| `/entity` | List all tracked entities. |
| `/entity near` | Describe the nearest tracked entity of any type. |
| `/entity minecraft:zombie` | Describe the nearest tracked zombie. |
| `/entity minecraft:zombie list` | List all tracked zombies. |
| `/entity near minecraft:zombie list` | Describe the nearest tracked zombie. |
| `/entity 21339` | Describe entity 21339. |
| `/entity 21339 use` | Interact with entity 21339 only. |
| `/entity near minecraft:cow use` | Interact with the nearest tracked cow. |
| `/entity minecraft:cow use` | Request an interaction with every tracked cow. |

The IDs in this guide are examples. Replace them with IDs from your current entity list. IDs can change after reconnecting or when an entity appears again.

Adding `near` to an ID does not change its target. An ID already identifies one entity.

## Example: check what is near your base

List the entities that MCC currently knows about:

```text
/entity
```

The output includes entity IDs, types, distances, and positions. It lists nearer entities first. A name column appears when tracked entities have player or custom names.

To inspect the closest one:

```text
/entity near
```

This shows details without attacking or interacting. Details can include a name, equipment, pose, distance, and position, depending on the entity data.

For example, if the closest entity is a cow in your farm, you can check its ID and location before using another command.

## Example: find the zombies outside your shelter

List all tracked zombies:

```text
/entity minecraft:zombie list
```

Compare their distances and positions. To inspect only the nearest zombie:

```text
/entity near minecraft:zombie
```

The shorter form has the same inspection behavior:

```text
/entity zombie
```

Neither command attacks. The difference between `zombie` and `zombie list` is important: the first describes one nearest match, while the second lists all matches.

## Example: inspect a specific named animal

Suppose `/entity` lists your named cow with ID `21501`. Inspect that exact cow:

```text
/entity 21501
```

You can also use the explicit `list` action:

```text
/entity 21501 list
```

Both commands show details for that ID. Selecting by ID avoids choosing another cow that happens to be closer.

Custom names and player names are not direct selectors for this command. Find the entity's ID in the list instead.

## Example: attack one nearby zombie

Approach the zombie. Face it with a clear line of sight. Select a weapon from your hotbar, then request one attack:

```text
/changeslot minecraft:iron_sword
/entity near minecraft:zombie attack
```

The sword must already be in your hotbar. See [`/changeslot`](changeslot.md) if it is in your main inventory instead.

For an ordinary `near` attack, MCC first considers matching entities in front of you. It chooses the nearest of those and checks line of sight.

This command sends one attack request. It does not keep attacking, chase the zombie, or guarantee a kill.

To attack a specific zombie shown as ID `21339`:

```text
/entity 21339
/entity 21339 attack
```

Inspect the ID first to check its type and location. An ID attack still requires the target to be in front and visible unless you use `-a`.

## Example: target several zombies

If you intend to request attacks on multiple tracked zombies:

```text
/entity minecraft:zombie list
/entity minecraft:zombie attack
```

Without `near`, the attack action considers every matching tracked zombie. Normal attacks skip targets behind you or blocked from view.

This is not a nearest-target shortcut. Some tracked zombies can be outside server reach. The server can reject those requests.

Use `near` or an ID when you intend to attack only one entity.

### What `-a` changes

The flag skips MCC's direction and line-of-sight checks:

```text
/entity 21339 attack -a
```

For example, this requests an attack on that ID even if it is behind you. It does not rotate you toward the target.

You can combine the flag with a nearest-type selector:

```text
/entity near minecraft:zombie attack -a
```

With `-a`, this chooses the nearest tracked zombie without the usual direction filter. The flag does not bypass server reach, walls, protection rules, or permissions.

## Example: interact with a villager

Approach the villager you want to trade with. Suppose its ID is `21804`:

```text
/entity 21804
/entity 21804 use
```

`use` sends a main-hand interaction, similar to a right click. If that villager can trade and the server accepts the interaction, it can open a trading window.

After the server opens the window, inspect it with:

```text
/inventory container list
```

To select the nearest villager instead of a specific ID:

```text
/entity near minecraft:villager use
```

Use an ID when several villagers stand together and you need a particular one.

## Example: feed a cow with wheat

Place wheat in your hotbar. Stand within interaction reach of the cow:

```text
/changeslot minecraft:wheat
/entity near minecraft:cow
/entity near minecraft:cow use
```

`use` interacts with the nearest tracked cow using the wheat in your main hand. The server decides the result from the cow's state and the held item.

If two cows have IDs `21501` and `21502`, you can target them separately:

```text
/entity 21501 use
/entity 21502 use
```

Replace both IDs with your cows' current IDs. Feeding can start breeding only when the server's animal conditions permit it. Wait for the inventory update between interactions because feeding can consume wheat.

The type-wide form `/entity minecraft:cow use` requests interactions with every tracked cow. It can consume items on more than one eligible animal.

## Example: look for dropped items

After mining, inspect tracked dropped-item entities:

```text
/entity minecraft:item list
/entity near minecraft:item
```

These commands show item-entity locations and details. They do not collect the items or filter by the item stack's contents.

To collect an item, move within pickup range and let the server handle pickup. `/entity ... use` is not an item-pickup command.

## Example: use the TUI entity browser

In TUI mode:

```text
/entity ui
```

The browser shows tracked entities and their details. You can filter categories and sort the list. It also provides interaction and attack actions for selected entities.

The classic console does not provide this browser. Use `/entity`, type-filtered lists, and ID commands there.

## Common problems

| Problem | What to check |
| --- | --- |
| Entity handling is disabled. | Enable `[Gameplay] Entity` in `client.toml`. Reconnect. |
| No matching entity is found. | Check the type or ID. The server must send the entity to MCC before it appears in the tracker. |
| An ID no longer works. | Run `/entity` again. The entity can disappear or receive a different ID. |
| An attack says the target is not in front. | Face the target. [`/look`](look.md) changes your orientation. |
| An attack says the target is not visible. | Check for a blocked line of sight and terrain handling. |
| A request has no effect on the server. | Check reach, held item, target state, and server rules. A sent request does not guarantee an accepted action. |
| A type action affects several entities. | Add `near` or use a specific ID. |
| `ui` requires the TUI. | Start MCC in TUI mode, or use the text commands. |

[All commands](index.md) · [Inventory](inventory.md) · [Troubleshooting](../troubleshooting/index.md)
