# /inventory

View your items, inspect an open container, and send inventory actions to the server.

Type the examples in MCC's input prompt. The examples use the default `/` command prefix.

## Before you start

Connect to a server. Enable inventory handling in `client.toml`:

```toml
[Gameplay]
Inventory = true
```

Edit the existing `[Gameplay]` table. Reconnect after changing this setting. See [configuration](../client/configuration.md#gameplay).

Start with `/inventory player list`. It shows the items and slot numbers that you can use in later commands.

## Syntax

```text
/inventory
/inventory inventories
/inventory player|container|<id> list
/inventory player|container|<id> open
/inventory player|container|<id> close
/inventory player|container|<id> click <slot> [action]
/inventory player|container|<id> drop <slot> [all]
/inventory search <item> [count]
/inventory creativegive <slot> <item> [count]
/inventory creativedelete <slot>
```

Choose one value between the `|` characters. For example, use `player`, `container`, or a numeric window ID. Do not type the `|` characters.

Replace `<slot>`, `<id>`, and `<item>` with actual values. Square brackets mark an optional argument. Do not type the brackets.

## Choose a window

A window is your player inventory or a container that the server opened, such as a chest.

| Window selector | Meaning | Example |
| --- | --- | --- |
| `player` or `p` | Your player inventory, always window 0. | `/inventory player list` |
| `container` or `c` | The currently open container. | `/inventory container list` |
| A numeric ID | An available window listed by `/inventory inventories`. | `/inventory 2 list` |

The example ID `2` works only when window 2 is available. Container IDs can change each time you open a container.

For player click and drop examples, close any container first. While a container is open, use its slot numbers and the `container` selector. Inventory actions use the active server menu.

## Command forms

In this table, `<window>` means `player`, `container`, or an available numeric window ID.

| Form | Meaning |
| --- | --- |
| `/inventory` | List available windows and show basic usage. |
| `/inventory inventories` | List available windows by ID. |
| `/inventory <window> list` | Show the selected window's non-empty slots and items. |
| `/inventory <window> open` | Open the selected window's interactive TUI view. |
| `/inventory <window> close` | Close the active server container. |
| `/inventory <window> click <slot> [action]` | Send a click action. The default is `left`. |
| `/inventory <window> drop <slot> [all]` | Drop one item, or the whole stack with `all`. |
| `/inventory search <item> [count]` | Find matching items in your inventory and the open container. |
| `/inventory creativegive <slot> <item> [count]` | Replace a player inventory slot in creative mode. The default count is 1. |
| `/inventory creativedelete <slot>` | Clear a player inventory slot in creative mode. |

## List windows

```text
/inventory
/inventory inventories
```

The first command lists window IDs and titles, followed by basic usage. The second command lists IDs and titles without that usage line.

Your player inventory has ID `0`. After you open a chest, the list also includes the chest's window ID.

## List items and slot numbers

```text
/inventory player list
/inventory p list
/inventory 0 list
```

These commands show the same player inventory. Each non-empty slot includes its slot number, item, and count.

For an open container:

```text
/inventory container list
/inventory c list
/inventory 2 list
```

The first two commands select the current container. The third selects window 2 explicitly. Replace `2` with the ID from `/inventory inventories`.

You can also type `/inventory 0` to list your player inventory. A bare container ID, such as `/inventory 2`, requests its TUI view.

### Understand slot numbers

Slot numbers are menu positions. They are different from the hotbar numbers 1 through 9 used by [`/changeslot`](changeslot.md).

The player inventory uses these positions:

| Slot | Position |
| --- | --- |
| `0` | Crafting result |
| `1` through `4` | Player crafting grid |
| `5` through `8` | Armor |
| `9` through `35` | Main inventory |
| `36` through `44` | Hotbar |
| `45` | Offhand |

For example, player slot `36` is the first hotbar position. `/changeslot 1` selects that hotbar position.

Container layouts use different slot numbers. Read `/inventory container list` before acting on a chest, furnace, or other menu. Never reuse a player slot number without checking the container layout.

## Open the inventory view

These examples require MCC's TUI mode:

```text
/inventory player open
/inventory 0 open
```

Both commands request the interactive player inventory view. In the classic console, use `list` instead.

To view an open container:

```text
/inventory container open
/inventory 2 open
```

Replace `2` with the current container ID. The container must already be open on the server.

`open` does not interact with a chest in the world. To open a nearby chest, use [`/useblock`](useblock.md) with its coordinates first:

```text
/useblock 150 79 380
/inventory container list
/inventory container open
```

Replace the coordinates with your chest's position. Wait for MCC to report the open container before you run the inventory commands.

## Click a slot

A left click picks up a stack or places the stack held by the inventory cursor. The cursor is the item stack between pickup and placement.

With no container open, pick up a stack from player slot 36:

```text
/inventory player click 36
```

To move that stack into an empty player slot 37, send a second click after the server updates the inventory:

```text
/inventory player click 37 left
/inventory player list
```

The explicit `left` action has the same meaning as the default action. If the destination contains items, the result depends on those items.

Use a right click to split a stack or place a single cursor item, according to the current cursor state:

```text
/inventory player click 36 right
/inventory container click 3 right
```

The container example requires an open container. Check its slot 3 before you click it.

To request a quick transfer from an open container:

```text
/inventory container click 3 shift
/inventory container click 3 shiftright
```

Both actions use the same quick-transfer operation in the current client. The menu and server decide the destination.

To send a middle click:

```text
/inventory player click 36 middle
```

The server and game mode determine whether a middle click has an effect.

| Action | Also accepted | Operation |
| --- | --- | --- |
| `left` | `leftclick` | Left click. |
| `right` | `rightclick` | Right click. |
| `middle` | `mid`, `middleclick` | Middle click. |
| `shift` | `shiftclick` | Quick transfer. |
| `shiftright` | `shiftrightclick` | Quick transfer. |

## Drop items

Dropping removes items from a slot and requests that the server drop them into the world. Check the slot before you use either form.

With no container open:

```text
/inventory player drop 36
```

This drops one item from the first player hotbar slot.

```text
/inventory player drop 36 all
```

This drops the whole stack from that slot. An empty slot produces an error.

For an open container:

```text
/inventory container drop 3
/inventory container drop 3 all
```

These are alternative examples. The first drops one item from container slot 3. The second drops its entire stack.

## Search for an item

```text
/inventory search minecraft:diamond
/inventory search minecraft:cobblestone
```

These commands search your player inventory and the currently open container. Results identify the window, slot number, item, and stack count.

Use an optional count to find stacks with exactly that many items:

```text
/inventory search minecraft:cobblestone 64
/inventory search minecraft:diamond 1
```

The first finds full stacks of 64 cobblestone. The second finds diamond stacks containing exactly one diamond.

The count is 1 through 64. It does not mean a minimum count or a total across several slots. Search only reads inventory state.

## Set a slot in creative mode

`creativegive` replaces the contents of a player inventory slot. It requires creative mode and server acceptance.

With no container open:

```text
/inventory creativegive 36 minecraft:diamond
```

This requests one diamond in the first hotbar slot. The default count is 1.

```text
/inventory creativegive 37 minecraft:stone 64
/inventory player list
```

This requests 64 stone in the second hotbar slot. The list command lets you check the server's result.

Use a count from 1 through 64. Check the slot first because this command replaces its existing contents.

## Clear a slot in creative mode

These are independent examples:

```text
/inventory creativedelete 36
/inventory creativedelete 37
```

The first clears the first hotbar slot. The second clears the second hotbar slot. Both require creative mode.

## Close a container

```text
/inventory container close
/inventory 2 close
```

Use either command to close the active container. The numeric form requires window 2 to be available.

Closing a container ends that server menu. To interact with it again, open the block again with `/useblock`.

The `close` operation acts on the active server container. Using `player close` does not select a different container to close.

## Common problems

| Problem | What to check |
| --- | --- |
| Inventory handling is disabled. | Enable `[Gameplay] Inventory` in `client.toml`. Reconnect. |
| MCC is not connected. | Connect before you use inventory commands. |
| No container is available. | Interact with a nearby container. Wait for the server to open it. |
| A numeric window ID is unavailable. | Run `/inventory inventories` again. Use the current ID. |
| `open` reports that the TUI is required. | Start MCC in TUI mode, or use `list` in the classic console. |
| Search finds no items. | Check the item identifier. Remove the count filter to match any stack size. |
| A creative action fails. | Check your game mode and the server's restrictions. |
| A click affects an unexpected slot. | Use the active menu's slot numbers. Check whether a container is open. |

Inventory actions are requests to the server. Check the next inventory update before you assume that an action succeeded.

[All commands](index.md) · [Troubleshooting](../troubleshooting/index.md)
