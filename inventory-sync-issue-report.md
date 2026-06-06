# MCC inventory sync issues report

## Sources read

- GitHub issue #3112: https://github.com/MCCTeam/Minecraft-Console-Client/issues/3112
- GitHub issue #3126: https://github.com/MCCTeam/Minecraft-Console-Client/issues/3126
- GitHub issue #3124: https://github.com/MCCTeam/Minecraft-Console-Client/issues/3124
- Related issue referenced by #3124: https://github.com/MCCTeam/Minecraft-Console-Client/issues/3119
- Commit inspected: `3c59bfe13abc18291506bec59118b5bd7509f4eb` (`Experimental inventory sync`)

## Short version

There are three separate but related problems in the reports:

1. Before commit `3c59bfe`, `/inventory container list` could be correct while `/inventory player list` stayed stale. The script used `GetPlayerInventory()`, so it believed moved or trashed items were still in the player inventory and repeated the same actions.
2. Commit `3c59bfe` tried to fix that by mirroring the player-inventory part of an open container back into inventory id `0`. That addressed the stale player list in the first test, but it introduced or exposed `x0` ghost items because the mirrored player inventory can share the same `Item` object reference as the open container, and later local shift-click prediction can mutate that object to `Count = 0`.
3. Issue #3124 is mainly a Script Scheduler/reconnect duplication issue. It matters here because the posted `WarpAFK.cs` scripts show the user process and the workaround they wrote for the stale inventory problem, but the scheduler bug itself is not fixed by `3c59bfe`.

## What the issue is exactly

MCC keeps inventories in a dictionary keyed by server window id. Inventory id `0` is the player inventory. Open containers use separate ids, for example the `/trash` or `/çöp` GUI and the `/is chest N` GUI.

In Minecraft protocol, an open container window contains both:

- the container's own top slots
- a mirrored copy of the player's normal 36 inventory slots

For example:

- A 27-slot chest (`Generic_9x3`) has 63 total slots in MCC: 27 chest slots plus 36 mirrored player slots.
- A 36-slot trash GUI (`Generic_9x4`) has 72 total slots in MCC: 36 trash slots plus 36 mirrored player slots.
- The mirrored player slots begin after the container's own top slots.

The user's old script scans `GetPlayerInventory()` to decide what to do, then it clicks the mirrored player slot in the open container:

- For `/çöp`, the GUI has 36 top slots, so `containerSlot = playerSlot + 27`. Example: player slot `36` maps to container slot `63`.
- For `/is chest N`, the GUI has 27 top slots, so `containerSlot = playerSlot + 18`. Example: player slot `36` maps to container slot `54`.

Before `3c59bfe`, `DoWindowAction()` updated only the clicked container object. If the script clicked the open trash/chest container, MCC's current container snapshot was updated, but inventory id `0` was not updated from that container's mirrored player slots. Therefore:

- `/inventory container list` showed the item removed.
- `/inventory player list` still showed the old item.
- `GetPlayerInventory()` returned the stale id `0` state.
- The bot returned to trash/storage mode and clicked the same logical item slots again.

This is the core bug from #3112.

## How to reproduce #3112 manually by hand

These steps avoid the automation script and use only MCC commands. They are based on the user's server and process from #3112.

Prerequisites:

- Connect MCC to `play.ronemacraft.com`.
- Use Minecraft `1.20.4`.
- Use a build before the experimental inventory sync fix, for example the reported MCC version 445 or another build before `3c59bfe`.
- Have inventory handling enabled.
- Have at least one item that the server can move into the trash GUI or an island chest.

Trash GUI reproduction:

1. Stand still in the Opskyblock/AFK area where the user observed the problem.
2. Put an ore item in the normal player inventory, for example `EmeraldOre`, `IronOre`, `GoldOre`, or `DiamondOre`.
3. Run `/inventory player list`.
4. Note the player slot that contains the ore. For a hotbar item this is usually in slots `36` through `44`.
5. Send `/çöp` or the server's `/trash` alias.
6. Wait until MCC logs that a virtual trash inventory opened.
7. Run `/inventory container list`.
8. Find the same item in the container's mirrored player section. For the 36-slot trash GUI, use `containerSlot = playerSlot + 27`.
9. Run `/inventory container click <containerSlot> ShiftClick`.
10. Run `/inventory container list` again. The container view should show that the item was removed or moved.
11. Run `/inventory player list`.
12. On the broken builds, the player list can still show the item in the old player slot.
13. Close the container with `/inventory container close`.
14. Open `/çöp` again and repeat the same click. The server-side item is already gone, but MCC still thinks it exists because id `0` is stale.

Island chest reproduction:

1. Put any non-ore item in the player inventory.
2. Run `/inventory player list` and note the player slot.
3. Send `/opskyblock` if needed by the server flow.
4. Send `/is chest 21`.
5. Wait until the chest opens.
6. For the 27-slot chest GUI, compute `containerSlot = playerSlot + 18`.
7. Run `/inventory container click <containerSlot> ShiftClick`.
8. Run `/inventory container list`. The open container view should reflect the move.
9. Run `/inventory player list`. On affected builds, the item can still be listed in the player inventory.
10. The user's script then sees `hasOther = true`, tries the next chest, and repeats until its own workaround stops it.

Why moving or warping seems to fix it:

- The user said `/spawn` to `/warp afk5` refreshes `/inventory player list`.
- That is consistent with the server resending player inventory or slot state during a world/lobby/teleport transition.
- It is not a useful workaround for the user because leaving the AFK location resets the 30-minute reward counter.

## How issue #3124 fits in

Issue #3124 is not the same inventory defect. It is a continuation of #3119, where Script Scheduler can re-run login-triggered scripts during `/reco` or reconnect spam while old script instances keep running.

The relevant Script Scheduler config from #3124 is:

- `Trigger_On_Login = true`
- One task sends `/login password`.
- Another task runs `script WarpAFK.cs`.

Manual scheduler reproduction:

1. Enable `ChatBot.ScriptScheduler`.
2. Add a login-triggered task that starts a script, like the `WarpAFK.cs` shown in #3124.
3. Connect to the server.
4. Repeatedly run `/reco`, or force several disconnect/reconnect cycles.
5. Watch the log for multiple script instances continuing at once.
6. Expected behavior would be that old script instances are stopped before a new login-triggered instance starts.
7. Actual behavior from #3119/#3124 was repeated commands such as `/opskyblock`, `/pay`, `/is chest 21`, and `/warp afk5`, sometimes before MCC considers itself fully connected.

Why this matters for the inventory report:

- The first `WarpAFK.cs` in #3124 contains slot-recorder logic (`doneSlotTypes`, `confirmedPSlots`, `failedPSlots`) specifically because `/inventory player list` was unreliable.
- The later `WarpAFK.cs` posted in #3124 is simpler because the user believed the new player inventory sync worked. It goes back to scanning `GetPlayerInventory()` for `PlayerItems()`.
- That means the experimental inventory sync directly changed how the script could be written, even though the reconnect duplication is a separate bug.

## What changed in commit 3c59bfe

Commit `3c59bfe13abc18291506bec59118b5bd7509f4eb` is titled `Experimental inventory sync`.

Files changed:

- `MinecraftClient/McClient.cs`
- `MinecraftClient/Resources/Translations/Translations.resx`
- `MinecraftClient/Resources/Translations/Translations.Designer.cs`

Main inventory changes:

1. Fixed a merge-order bug in `TryMergeSlot()`.
   - Before the commit, the full-merge branch set `item.Count = 0` before adding it to the destination stack. That meant the destination received `0`.
   - After the commit, it adds `item.Count` to `curItem.Count` first, then sets `item.Count = 0`.
   - Current code: `MinecraftClient/McClient.cs`, around lines 1998-2014.

2. Added mirrored player-inventory range detection.
   - `TryGetMirroredPlayerInventoryRange()` assumes non-player containers end with 36 mirrored player inventory slots.
   - `TryGetMirroredPlayerInventorySlot()` maps a container window slot back to a player inventory slot using `playerInventorySlot = windowSlot - firstWindowSlot + 9`.
   - Current code: `MinecraftClient/McClient.cs`, around lines 2065-2095.

3. Added `SetPlayerInventorySlot()`.
   - This updates inventory id `0` for a mapped player slot.
   - It removes the slot when the incoming item is null or empty.
   - It skips work when `AreSameInventorySlot()` says the old and new slot are equivalent.
   - Current code: `MinecraftClient/McClient.cs`, around lines 2097-2125.

4. Added sync from open containers to player inventory id `0`.
   - `SyncPlayerInventorySlotFromWindow()` syncs one mapped mirrored slot.
   - `SyncPlayerInventorySlotsFromWindow()` syncs all mirrored slots.
   - `DoWindowAction()` now calls `SyncPlayerInventorySlotsFromWindow(inventory)` before sending the click packet.
   - `OnWindowItems()` now syncs the whole mirrored range and dispatches `OnInventoryUpdate(0)` if changed.
   - `OnSetSlot()` now syncs a mapped mirrored slot and dispatches `OnInventoryUpdate(0)` if changed.
   - Current code: `MinecraftClient/McClient.cs`, around lines 2128-2152, 2896, and 3931-3989.

5. Added empty-item filtering to `OnWindowItems()`.
   - The commit removes items from incoming full window contents when `Item.IsEmpty` is true.
   - Current code: `MinecraftClient/McClient.cs`, around lines 3935-3937.

6. Added cursor cleanup for left/right click predictions.
   - If cursor slot `-1` reaches zero count, it is removed.
   - Current code: `MinecraftClient/McClient.cs`, around lines 2219-2221 and 2280-2282.

7. Added shift-click cleanup.
   - If a shift-clicked source item reaches zero count and is still present in the clicked container dictionary, it is removed.
   - Current code: `MinecraftClient/McClient.cs`, around lines 2868-2872.

Translation changes:

- `cmd.inventory.shiftclick` changed from `Shift clicking slot {0} in window #{1}` to `Shift`.
- `cmd.inventory.shiftrightclick` changed from `Shift right-clicking slot {0} in window #{1}` to `Shift right`.
- This fixes the nested format-string problem from #3126. The outer message is `{0} clicking slot {1} in window #{2}`, so the action label must not contain its own `{0}` and `{1}` placeholders.
- Current command formatting is in `MinecraftClient/Commands/Inventory.cs`, around lines 355-365.

## Why the commit helped #3112

The main stale-list failure was that id `0` did not follow the mirrored player section inside nonzero container windows.

The new sync code makes this happen:

1. Open `/çöp` or `/is chest N`.
2. The open container's full slot list includes the player's mirrored 36 slots.
3. `OnWindowItems()` copies that mirrored section into player inventory id `0`.
4. After a click, `DoWindowAction()` locally predicts the container change and syncs mirrored slots into id `0`.
5. Bot scripts reading `GetPlayerInventory()` now see the result of the container click.

This is why the test build mentioned in #3112 appeared to work for the user's old script.

## Why the commit did not fully work

The commit can leave `x0` items in `/inventory player list`.

The most likely concrete cause is object aliasing plus the equality check in `SetPlayerInventorySlot()`.

Detailed flow:

1. `OnWindowItems()` receives an open container, for example `/is chest 21`.
2. `SyncPlayerInventorySlotsFromWindow()` maps the mirrored player slots from that container into inventory id `0`.
3. `SetPlayerInventorySlot()` stores the same `Item` object reference from the container dictionary into the player inventory dictionary. It does not clone the item.
4. The user shift-clicks a mirrored player slot in the open container.
5. If the destination already has the same stackable item, `TryMergeSlot()` fully merges the source stack.
6. In that full-merge branch, the code sets the source `item.Count = 0`.
7. Because player inventory id `0` may hold the same `Item` object reference, the player inventory entry now also has `Count = 0`.
8. `DoWindowAction()` removes the source slot from the open container and calls `SyncPlayerInventorySlotsFromWindow()`.
9. The sync sees the window slot is now empty and tries to set the mapped player slot to `null`.
10. `SetPlayerInventorySlot()` calls `AreSameInventorySlot(previousItem, null)`.
11. `AreSameInventorySlot()` returns true when the previous item is empty and the new item is null.
12. Because it returns true, `SetPlayerInventorySlot()` exits early and does not remove the existing dictionary entry.
13. `/inventory player list` iterates the dictionary directly and prints the item, so it appears as `x0`.

This matches #3126 very closely:

- The user bought one grassy soil from `/market` or `/shop`.
- The script stored it in `/is chest`.
- The first item may have moved into an empty chest slot, so no `x0` appeared.
- The user bought the same item again.
- The second item could merge into the existing chest stack, causing `TryMergeSlot()` to set the source count to zero.
- The player inventory dictionary retained the zero-count item, so `/inventory player list` showed `x0`.

It also explains why the issue is intermittent:

- Moving into an empty destination slot uses `StoreInNewSlot()`, which removes the source slot but does not set the source item count to zero.
- Fully merging into an existing stack uses `TryMergeSlot()`, which does set the source item count to zero.
- Therefore the visible result depends on whether the item moved into an empty slot or merged into an existing compatible stack.

## How to reproduce #3126 manually by hand

Use the experimental build containing `3c59bfe`.

Server-specific reproduction from the issue:

1. Connect to `play.ronemacraft.com` on Minecraft `1.20.4`.
2. Go to the Opskyblock area where `/market` or `/shop` is available.
3. Buy exactly one item from `/market` or `/shop`. The user specifically mentioned grassy soil.
4. Run `/inventory player list` and note the player slot containing the item.
5. Open an island chest with `/is chest 21`.
6. Use `/inventory container list` to confirm the item appears in the mirrored player section.
7. Compute the container slot as `playerSlot + 18` for a 27-slot chest.
8. Run `/inventory container click <containerSlot> ShiftClick`.
9. Close the container with `/inventory container close`.
10. Run `/inventory player list`. The item may disappear correctly.
11. Buy the same item again, exactly one item.
12. Open the same `/is chest 21` again.
13. Shift-click the second item into the chest using the same slot mapping.
14. Run `/inventory player list`.
15. If the second item merged into the existing stack in the chest, the player list can show that item as `x0`.

Generic reproduction that should not require this specific server:

1. Use a build containing `3c59bfe`.
2. Connect to any server where inventory actions work.
3. Put a partial stack of a stackable item in a chest, for example 1 dirt in a chest slot.
4. Put 1 dirt in the player's inventory.
5. Open the chest.
6. Run `/inventory player list` and note the player slot of the dirt.
7. For a normal 27-slot chest, compute `containerSlot = playerSlot + 18`.
8. Run `/inventory container click <containerSlot> ShiftClick`.
9. Run `/inventory player list` quickly after the click.
10. The expected correct result is no dirt in the old player slot.
11. The suspected broken result is a retained entry with `x0 Dirt`.

If this generic reproduction does not show `x0`, add logging or inspect the dictionaries immediately after `DoWindowAction()`. Server correction packets can sometimes mask the display timing, but the code path itself can retain a zero-count item.

## Is this just an issue on this server?

The stale `/inventory player list` issue is not purely a RonemaCraft server bug. It is a real MCC state-model gap:

- MCC had separate snapshots for player inventory id `0` and open container ids.
- Open containers include mirrored player inventory slots.
- Before the commit, MCC did not sync those mirrored slots back into id `0`.
- Any server or plugin GUI that updates only the open container window can expose this mismatch.

RonemaCraft makes the issue easy to notice because:

- It uses plugin GUIs for `/çöp`, `/is chest`, `/market`, and daily rewards.
- The user's bot depends on `GetPlayerInventory()` while staying stationary in an AFK area.
- Warping away refreshes inventory but resets the AFK reward counter, so the stale state persists long enough to hurt the workflow.

The `x0` issue from `3c59bfe` is also not inherently server-only. It follows from MCC's own local prediction and reference sharing. The server's workflow makes it reproducible because buying the same item twice and storing it into the same chest naturally creates a merge-into-existing-stack case.

The server can still affect how often the bug appears:

- Some servers send extra full inventory refreshes that mask stale id `0` state.
- Some plugin GUIs send unusual item stacks, custom names, or zero-count placeholders.
- Some server flows delay or omit player-window updates while only updating the open plugin window.

So the right classification is:

- #3112: general MCC design gap, exposed strongly by this server's plugin GUI workflow.
- #3126: general bug in the experimental fix, easiest to trigger with this server's repeated market/chest process.
- #3124/#3119: Script Scheduler/reconnect lifecycle bug, separate from inventory sync.

## How the inventory system worked before this commit

Before `3c59bfe`, the relevant flow was:

1. MCC initializes the inventory dictionary with player inventory id `0`.
   - `ClearInventories()` creates `inventories[0] = new Container(0, ContainerType.PlayerInventory, "Player Inventory")`.
   - Current code is around `MinecraftClient/McClient.cs` lines 2960-2970.

2. When the server opens a container, MCC adds another `Container` under that server window id.
   - The container type comes from the protocol menu type id.
   - The title comes from the packet title.
   - For 1.20.4, this happens through `OpenWindow` handling in `Protocol18.cs`.

3. Each `Container` has a flat `Items` dictionary.
   - Key: slot id.
   - Value: `Item`.
   - Empty slots are normally absent from the dictionary.

4. Container slot counts include the player's mirrored inventory section.
   - `Generic_9x3` is 63 total slots: 27 top slots plus 36 player slots.
   - `Generic_9x4` is 72 total slots: 36 top slots plus 36 player slots.
   - `Generic_9x6` is 90 total slots: 54 top slots plus 36 player slots.
   - `PlayerInventory` is 46 total slots.
   - Current values are in `MinecraftClient/Inventory/ContainerTypeExtensions.cs`, around lines 10-40.

5. `/inventory player list` reads only inventory id `0`.
   - It iterates `inventory.Items` directly.
   - It prints every dictionary entry, including an item whose count is zero.
   - Current command code is around `MinecraftClient/Commands/Inventory.cs` lines 320-327.

6. `/inventory container list` reads the highest open inventory id when no id is supplied.
   - This is usually the foreground open server GUI.
   - It does not read inventory id `0`.

7. `OnWindowItems()` replaced only the addressed inventory's item dictionary.
   - Before this commit, it did not filter zero-count items.
   - Before this commit, it did not sync mirrored player slots into inventory id `0`.
   - Before this commit, it dispatched only `OnInventoryUpdate(inventoryID)`.

8. `OnSetSlot()` updated only the addressed inventory id, except special protocol cases.
   - `inventoryID == 254` maps to player inventory id `0`.
   - `inventoryID == 255` and slot `-1` maps to the cursor item stored in player inventory slot `-1`.
   - Newer 1.21.2+ `SetPlayerInventory` packets map directly to id `0`, but the reported server uses 1.20.4, so that packet path is not available.

9. `DoWindowAction()` locally predicted the result of clicks before sending the click packet.
   - For a nonzero window id, it modified that open container's `Items`.
   - It did not also update player inventory id `0`.
   - It sent `ClickWindow` with the calculated changed slots and state id.

10. Bot APIs reflect this model.
   - `GetPlayerInventory()` returns inventory id `0`.
   - `GetInventories()` returns the full dictionary of player plus open containers.
   - `OnInventoryUpdate(int inventoryId)` tells bots which inventory id changed.
   - Before the commit, a container mirror change did not imply an `OnInventoryUpdate(0)` event.

The practical consequence before the commit:

- If a script needed live item state while a container was open, `/inventory container list` or `GetInventories()[openContainerId]` was more reliable than `GetPlayerInventory()`.
- Scripts had to manually translate mirrored container slots back to player slots, exactly as the user's workaround did.
- `GetPlayerInventory()` was safe only after the server actually refreshed inventory id `0`.

## Suggested direction for a real fix

This report is not a patch, but the code evidence points to a safer direction:

1. Do not store the same mutable `Item` instance in both the open container dictionary and player inventory id `0`.
   - Clone items when syncing from a container mirror into player inventory.

2. Make `SetPlayerInventorySlot()` remove an existing dictionary entry when the new item is null or empty, even if the old entry is already empty.
   - The dictionary state matters. An empty item object in the dictionary is not equivalent to no dictionary entry for display and script behavior.

3. Consider filtering empty items when listing inventories too.
   - The data model should not keep `x0` entries, but display code can defensively skip `item.IsEmpty`.

4. Keep the mirror sync idea, but add tests around these cases:
   - Shift-click mirrored player slot into an empty container slot.
   - Shift-click mirrored player slot into an existing compatible stack.
   - Shift-click one item twice into the same chest stack.
   - Open container `WindowItems` sync followed by local `DoWindowAction()`.
   - Server `OnSetSlot(windowId, sourceSlot, null, stateId)` after local prediction already changed the source item to zero.

5. Keep #3124 separate.
   - Fixing inventory sync should not be expected to fix Script Scheduler duplicate script instances after reconnect.
