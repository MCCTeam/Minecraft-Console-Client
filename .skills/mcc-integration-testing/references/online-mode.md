# Online-Mode Notes

Use this flow only when the user explicitly asks for Microsoft login or wants to validate against an online-mode server.

## Launch mode

- Prefer `BasicIO-NoColor` in a real TTY so the Microsoft device-code prompt is easy to read and copy.
- Do not use `MCC_FILE_INPUT=1` during the auth step. It is for scripted command injection, not interactive login.
- Avoid `nohup`. Use `tmux` for long-running sessions that still need a TTY.
- Start from a clean temp config when possible. If the temp config is copied from a user-local `MinecraftClient.ini`, inspect `ChatBot.ScriptScheduler` and `ChatBot.DiscordRpc` before the run.

## Session cache behavior

- `dotnet run --project MinecraftClient ...` uses the repo root for `SessionCache.db` and `ProfileKeyCache.ini`.
- The compiled binary under `MinecraftClient/bin/<Config>/net10.0/` uses that output directory instead.
- If a session exists in one location and not the other, sync the cache files before assuming login is broken.

## Account settings

- `Account.Login` must be populated for MCC to look up a cached Microsoft session.
- The cached key is the username form MCC stored, typically the lowercase username, not necessarily the email address.
- MCC rewrites `MinecraftClient.ini` on clean exit, so generate a temp config per run and do not edit it while MCC is still running.

## Auth prompt handling

- Do not send a bare Enter to dismiss `Password(invisible):` or `Paste your code here:` prompts. That can trigger offline fallback.
- For interactive online-mode runs, wait for the device code prompt and relay the code to the user exactly as shown.
- After the user completes login, continue the test in the same TTY session or restart into file-driven mode if the workflow requires automation.

## Join-time noise

- Real user configs may contain enabled bots or task lists that were harmless in offline testing but are noisy in online-mode validation.
- The most common examples are:
  - `ChatBot.ScriptScheduler` task lists that send `/hello`, `/login ...`, or other automatic commands on login or on an interval
  - `ChatBot.DiscordRpc`, which is not harmful to server state but adds log noise and extra background activity
- If the goal is protocol or feature validation, suppress these before the run or treat their output as non-test noise.

## Server settings

- For realistic online-mode testing, keep `online-mode=true`.
- Keep `enforce-secure-profile=true` unless the test explicitly targets insecure-profile behavior.

## Command reminders

- With `InternalCmdChar = "slash"`:
  - `/health`, `/pos`, `/inventory`, `/entity` are MCC internal commands.
  - `/send /list` and `/send /give ...` are server commands.
  - bare text is regular chat sent to the server.
