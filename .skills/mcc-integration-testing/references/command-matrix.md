# Command Matrix

This skill uses a fixed set of stable commands for local offline integration testing.

## MCC-side commands via `mcc-cmd`

- `health`
- `list`
- `inventory player list`
- `/gamemode creative`
- `inventory creativegive 36 Diamond 16`
- `inventory creativegive 37 IronSword 1`
- `inventory creativegive 38 GoldenApple 8`
- `inventory creativeclear 38`
- `entity`
- `/time query daytime`
- `look up`
- `look down`
- `look east`
- `/gamemode survival`
- `respawn`
- `/tp CursorBot 0 -60 0`
- `smoke_test_from_mcc_full_spectrum`
- `integration_test_chat_response`

Notes:
- Lines starting with `/` are sent to the server as chat/commands.
- Non-slash lines are treated as MCC internal commands first, then fall back to chat.

## Server-side commands via `mc-rcon`

- `op CursorBot`
- `gamerule sendCommandFeedback true`
- `gamerule logAdminCommands true`
- `time set day`
- `weather clear`
- `say Hello from the server console`
- `msg CursorBot This is a private whisper`
- `effect give CursorBot minecraft:speed 30 1`
- `effect give CursorBot minecraft:regeneration 10 1`
- `kill CursorBot`

## Representative entity coverage

- `execute as CursorBot at @s run summon minecraft:cow ~2 ~ ~`
- `execute as CursorBot at @s run summon minecraft:zombie ~4 ~ ~`
- `execute as CursorBot at @s run summon minecraft:creeper ~6 ~ ~`
- `execute as CursorBot at @s run summon minecraft:skeleton ~8 ~ ~`
- `execute as CursorBot at @s run summon minecraft:villager ~-2 ~ ~`
- `execute as CursorBot at @s run summon minecraft:allay ~-4 ~ ~`
- `execute as CursorBot at @s run summon minecraft:armor_stand ~ ~ ~2`
- `execute as CursorBot at @s run summon minecraft:item_display ~-6 ~ ~ {item:{id:"minecraft:diamond",count:1}}`
- `execute as CursorBot at @s run summon minecraft:spider ~10 ~ ~`
- `execute as CursorBot at @s run summon minecraft:pig ~-8 ~ ~`

## Block placement coverage

- `execute as CursorBot at @s run fill ~1 ~ ~1 ~3 ~2 ~3 minecraft:stone`
- `execute as CursorBot at @s run setblock ~5 ~ ~5 minecraft:chest`
- `execute as CursorBot at @s run setblock ~5 ~1 ~5 minecraft:furnace`
- `execute as CursorBot at @s run setblock ~6 ~ ~5 minecraft:crafting_table`

## Dimension change coverage

- `execute in minecraft:the_nether run tp CursorBot 0 64 0`
- `execute in minecraft:overworld run tp CursorBot 0 -60 0`

## Representative particle coverage

- `execute as CursorBot at @s run particle minecraft:happy_villager ~ ~1 ~ 0.5 0.5 0.5 0 12 force`
- `execute as CursorBot at @s run particle minecraft:end_rod ~ ~1 ~ 0.5 0.5 0.5 0.01 20 force`
- `execute as CursorBot at @s run particle minecraft:explosion ~ ~1 ~ 0 0 0 0 1 force`
- `execute as CursorBot at @s run particle minecraft:totem_of_undying ~ ~1 ~ 0.5 0.5 0.5 0.1 20 force`
- `execute as CursorBot at @s run particle minecraft:flame ~ ~1 ~ 0.2 0.2 0.2 0.02 30 force`
- `execute as CursorBot at @s run particle minecraft:heart ~ ~2 ~ 0.3 0.3 0.3 0 5 force`

## Representative sound coverage

- `execute as CursorBot at @s run playsound minecraft:entity.lightning_bolt.thunder master CursorBot ~ ~ ~ 1 1 0`
- `execute as CursorBot at @s run playsound minecraft:block.note_block.bell master CursorBot ~ ~ ~ 1 1 0`
- `execute as CursorBot at @s run playsound minecraft:entity.experience_orb.pickup master CursorBot ~ ~ ~ 1 1 0`

## Explosion coverage

- `execute as CursorBot at @s run summon minecraft:tnt ~3 ~ ~`
- `execute as CursorBot at @s run summon minecraft:tnt ~6 ~ ~`

## Kill and respawn cycle

- `kill CursorBot` (via RCON, requires survival mode)
- `respawn` (via MCC command after death)
