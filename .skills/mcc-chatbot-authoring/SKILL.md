---
name: mcc-chatbot-authoring
description: Create, modify, repair, and wire Minecraft Console Client ChatBots and standalone `/script` bots. Use this whenever the user wants an MCC bot, C# script bot, chat or event handlers, periodic automation, movement logic, inventory logic, plugin-channel handling, or asks to fix or port an existing bot; default to standalone `//MCCScript` bots unless the user explicitly asks for a built-in MCC bot or repo wiring.
---

# MCC ChatBot Authoring

Implement MCC chat bots against the bundled MCC authoring reference. Do not invent methods, lifecycle hooks, or registration steps.

Always read:
- `references/authoring-reference.md`

Load only as needed:
- `references/pattern-cookbook.md` for concrete standalone examples
- `assets/script-chatbot-template.cs` for the default standalone `/script` path
- `assets/builtin-chatbot-template.cs` only when the user explicitly requests a built-in bot

If the current workspace contains an MCC checkout, verify final names and signatures against local sources before editing. The skill should still work without those files.
If there is no MCC checkout available, rely on the bundled reference and cookbook as the full source of truth for authoring patterns.

## Choose the bot type first

1. Default to a standalone script bot loaded with `/script`.
2. Only choose a built-in bot when the user explicitly asks for a compiled MCC bot, repo wiring, automatic config loading, or changes under the built-in bot system.
3. If the prompt is ambiguous, infer the likely target from commands, requested output files, or phrasing, state the assumption briefly, and proceed.
4. If a user says only "make a bot", do not create a built-in bot.

## Source priority

When the local MCC checkout is available, prefer these sources in this order:
1. `MinecraftClient/Scripting/ChatBot.cs` and current files under `MinecraftClient/ChatBots/`
2. the bundled `references/authoring-reference.md`
3. the bundled `references/pattern-cookbook.md`
4. older `MinecraftClient/config/` sample bots only for ideas, not as the default scaffold

If an older sample conflicts with the current built-in bots, follow the current built-in bots.
If the local checkout is not available, do not block on missing repo files. Use the bundled references directly.

## Hard rules

- Only use lifecycle hooks and helpers documented in the bundled reference or verified in the target codebase.
- Do not send chat from `Initialize()`. Use `AfterGameJoined()` once the session can send messages.
- Prefer the current Brigadier command-registration pattern for built-in bots. Do not introduce `ChatBotCommand` unless the surrounding code already uses it.
- For message parsing, normalize with `GetVerbatim(text)` before `IsChatMessage(...)` or `IsPrivateMessage(...)`.
- Clean up everything you register or start: commands, plugin channels, threads, timers, and movement locks.
- If a built-in bot or long-running automation controls movement, follow a movement-lock pattern and release it on every stop path. Do not add `BotMovementLock` to a simple standalone `/script` bot unless the prompt or surrounding code explicitly needs shared movement coordination.
- For built-in bots, follow the host codebase's localization and config-comment conventions instead of scattering hardcoded user-facing text.
- For new code, prefer `Initialize()` over constructors for prerequisite checks and unload decisions.
- In this repo, built-in bot wiring usually means edits in `MinecraftClient/Settings.cs` and `MinecraftClient/McClient.cs` in addition to the bot class.
- For repair tasks, preserve the existing bot type and file layout unless the user explicitly asks for a conversion or restructure.

## Standalone script bots

Use the exact MCC metadata format from the bundled reference.
This is the default path for new work.

The script should usually:
- keep `Initialize()` for cheap setup only
- use `GetText(...)`, `AfterGameJoined()`, and other event hooks for live behavior
- log with `LogToConsole(...)`
- send server chat or commands with `SendText(...)`
- use `PerformInternalCommand(...)` only for MCC internal commands
- add `//using MinecraftClient.Inventory` in metadata when the script uses inventory types explicitly
- reuse the standalone snippets in `references/pattern-cookbook.md` before inventing new scaffolding
- keep load instructions explicit, usually `/script FileName.cs`

## Built-in bots

Built-in bots usually need three pieces:
- the bot class itself
- config wiring in the chat-bot config model
- bot registration in the load flow

If the codebase exposes commands, follow the built-in command and unload pattern from the bundled reference. If it exposes new settings or status text, follow the codebase's localization and config-comment patterns.

When working in this checkout, built-in bot delivery usually needs:
- a new file under `MinecraftClient/ChatBots/`
- a config property inside `Settings.ChatBotConfigHealper.ChatBotConfig`
- a `BotLoad(new YourBot())` line inside `McClient.RegisterBots(...)`
- literal code snippets or patch hunks for the `Settings.cs` property and the `McClient.cs` registration line, not only prose notes

## Repair flow

When the user asks to fix or debug a bot:
- identify whether it is standalone or built-in and keep that shape unless told otherwise
- remove the broken pattern first, then preserve the intended behavior
- check especially for these regressions: `SendText(...)` in `Initialize()`, raw formatted chat parsing, inventory snapshot mutation, missing command unregister, missing plugin-channel unregister, and unreleased movement locks
- reuse the local repo's modern pattern instead of patching around a legacy helper when the helper is no longer current

## Delivery checklist

Before finishing, verify:
- the class inherits `ChatBot`
- the chosen overrides exist in the MCC ChatBot API
- standalone script metadata is exact if this is a `/script` bot
- built-in bots are fully wired into config and registration if needed
- all command registrations, background work, and movement locks are released
- files and namespaces match the surrounding codebase

## Output

When you implement or modify a bot:
- state whether it is a standalone script bot or built-in bot
- list the files you changed
- mention any required config keys or the MCC command used to load it
- when built-in wiring is involved, show the exact inserted code lines or patch hunks for `Settings.cs` and `McClient.cs`
- call out assumptions briefly if the user did not specify bot type or trigger behavior
