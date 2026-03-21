# Command Matrix

This skill uses a fixed set of stable commands for local offline integration testing.

## MCC-side commands via `mcc-cmd`

- `health`
- `list`
- `inventory player list`
- `/gamemode creative`
- `inventory creativegive 36 Diamond 16`
- `entity`
- `/time query daytime`
- `smoke_test_from_mcc_full_spectrum`

Notes:
- Lines starting with `/` are sent to the server as chat/commands.
- Non-slash lines are treated as MCC internal commands first, then fall back to chat.

## Server-side commands via `mc-rcon`

- `op CursorBot`
- `gamerule sendCommandFeedback true`
- `gamerule logAdminCommands true`
- `time set day`
- `weather clear`

## Representative entity coverage

- `execute as CursorBot at @s run summon minecraft:cow ~2 ~ ~`
- `execute as CursorBot at @s run summon minecraft:zombie ~4 ~ ~`
- `execute as CursorBot at @s run summon minecraft:creeper ~6 ~ ~`
- `execute as CursorBot at @s run summon minecraft:skeleton ~8 ~ ~`
- `execute as CursorBot at @s run summon minecraft:villager ~-2 ~ ~`
- `execute as CursorBot at @s run summon minecraft:allay ~-4 ~ ~`
- `execute as CursorBot at @s run summon minecraft:armor_stand ~ ~ ~2`

## Representative particle coverage

- `execute as CursorBot at @s run particle minecraft:happy_villager ~ ~1 ~ 0.5 0.5 0.5 0 12 force`
- `execute as CursorBot at @s run particle minecraft:end_rod ~ ~1 ~ 0.5 0.5 0.5 0.01 20 force`
- `execute as CursorBot at @s run particle minecraft:explosion ~ ~1 ~ 0 0 0 0 1 force`
- `execute as CursorBot at @s run particle minecraft:totem_of_undying ~ ~1 ~ 0.5 0.5 0.5 0.1 20 force`

## Representative sound coverage

- `execute as CursorBot at @s run playsound minecraft:entity.lightning_bolt.thunder master CursorBot ~ ~ ~ 1 1 0`
- `execute as CursorBot at @s run playsound minecraft:block.note_block.bell master CursorBot ~ ~ ~ 1 1 0`

## Explosion coverage

- `execute as CursorBot at @s run summon minecraft:tnt ~3 ~ ~`
- `execute as CursorBot at @s run summon minecraft:tnt ~6 ~ ~`
