# Game features

A command can be valid while its action is unavailable. MCC needs a live session, enabled features, relevant server data, and server permission.

Use [configuration](configuration.md#gameplay) to enable features. Use `/help <command>` to inspect the current command's requirements.

## Player status

```text
/health
/effects
/tps
/list
/tab
/teams
/scoreboard
```

Health includes food and experience information. Effects show active status effects. `/tps` estimates server tick rate from received updates. A missing or expired sample means unknown, not zero TPS.

`/list` prints player names. `/tab` provides the server's richer tab-list data and opens an overlay in TUI mode. The server can control which names or decorations it sends.

## Move and look

First inspect your position:

```text
/move get
/look north
```

To walk to a destination:

```text
/move 150 80 380
```

Those numbers are an example destination. Use coordinates from your own test world. MCC plans movement through terrain it knows. It cannot plan reliably through chunks the server has not sent.

The `-f` flag relaxes hazard checks for falls, fire, and lava. It does not make solid blocks occupiable. Inspect the path before using it.

`/move on` and `/move off` control the terrain, physics, and pathfinding features. They do not mean "hold forward" and "release forward". Enabling modules can require a new session. `/move gravity off` changes client gravity behavior, not server permission to fly.

Read [move](../commands/move.md) and [look](../commands/look.md) before automating movement.

## Inspect world data

```text
/blockinfo 150 79 380
/blockinfo 150 79 380 -s
/chunk status
/chunk ui
```

Block information reports the client's current world snapshot. `-s` also reports six adjacent blocks. A missing chunk is not evidence that the block is air.

The chunk view shows loaded data near the client. It does not force the server to send every chunk.

## Interact with blocks and items

```text
/changeslot 1
/useitem
/useblock 150 79 380
/dig 150 79 380
```

These actions operate on the selected item and a real target. Reach, game mode, server protection, and block behavior matter. A reported packet send does not guarantee that the server changed the world.

`/changeslot` uses hotbar numbers 1 to 9. Inventory window slots use their own indices. Do not interchange the two.

## Containers and crafting

1. Move near a chest or workstation.
2. Use `/useblock <x> <y> <z>` on it.
3. Inspect `/inventory inventories`.
4. Read `/inventory container list`.
5. Select a slot action only after checking its index.

```text
/inventory player list
/inventory container list
/inventory container click 3
/inventory container close
```

Inventory clicks change server-side state. Slot 3 is an example, not a universal chest position. The menu type controls its slot layout.

`/recipebook list` shows unlocked recipes. `/recipebook craft <recipe>` asks the current menu to place ingredients. It is not a complete craft-and-collect workflow. Open the appropriate menu and collect the output when needed.

`/enchant` needs an enchanting menu. `/nameitem` needs an anvil. Read their command pages for preconditions.

## Entities

```text
/entity
/entity near
/entity ui
```

MCC tracks entities sent to this client. It does not see every entity on the server. Network entity IDs belong to the current session and can become invalid.

Type selectors can match multiple entities. `/entity near <type> attack` chooses the nearest matching target. Review the [entity reference](../commands/entity.md) before using attacks.

## Books, beds, dialogs, and advancements

| Feature | Command | Requirement |
| --- | --- | --- |
| Books | `/book read` | A suitable book in the main hand |
| Bed | `/bed sleep <x> <y> <z>` | A reachable bed and server acceptance |
| Server dialogs | `/dialog show` | A currently received dialog on a supporting protocol |
| Advancements | `/achievement list` | Advancement data decoded for the selected protocol |

Signing a writable book makes its content permanent. Read [book](../commands/book.md) before signing.

An unavailable advancement browser means the current protocol lacks the required structured data. It does not mean the player completed no advancements.

## Use a private test world

Check new automation on a server you control. Use known coordinates, inventory contents, and permissions. Stop scripts or plugins before changing their assumptions.

For recurring actions, use a [Beacon script](../beacon/index.md) or an [official plugin](../plugins/index.md). The command dispatcher and game APIs still enforce feature and connection requirements.
