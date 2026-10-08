# Chapter 9: Run the complete helper

You will combine a setting, saved state, a chat event, and a command. This helper answers `!hello` and reports the number of greetings.

## Create the complete file

1. Create `welcome-helper.bcn`.
2. Copy this complete script into the file.

```beacon
# beacon 1
# needs: chat.send
# setting prefix = "Hello" ; Greeting text
# desc: Greet players and count greetings.
# example: /guide-stats
set greetings to saved("greetings") or 0
on chat as e when trim(lower(e.message)) is "!hello"
  set greetings to greetings + 1
  save "greetings" to greetings
  whisper e.player "{settings.prefix}, {e.player}!"
end on
command "/guide-stats"
  show "Greetings: {greetings}"
end command
```

The top-level assignment restores the count during loading. The handler increments the count only for a matching event. The command displays the current script global.

The filter accepts an exact request. It does not authenticate a sender or grant an administration permission. The shared chat bucket limits outgoing messages.

## Install the file in MCC

Put the file beside your selected configuration directory:

```text
mcc-data/
├── configurations/
│   ├── client.toml
│   └── beacon/
│       ├── welcome-helper.settings.toml
│       └── welcome-helper.toml
└── scripts/
    └── welcome-helper.bcn
```

MCC creates the state file when `save` executes. The settings overlay and state file serve different purposes.

1. Start MCC with your configuration directory.

```sh
./Mcc.Cli --configurations /full/path/mcc-data/configurations
```

2. Connect to your test server using the [connection guide](../../client/index.md).
3. Type `/scripts run welcome-helper` in MCC.

Discovery does not automatically start a file. Loading registers this file's chat handler and internal command.

## Check the behavior

1. Connect the client to your test server.
2. Load the helper.
3. Send `!hello` from another player.
4. Check the private reply, such as `Hello, Alice!`.
5. Run the internal `/guide-stats` command.
6. Check local output for `Greetings: 1`.
7. Reload the script.
8. Run `/guide-stats` again.
9. Check that the saved count remains `1`.

Server-specific private-message support can affect `whisper`. Inspect host diagnostics when the requested reply does not arrive.

## Stop and edit

Use `/scripts stop welcome-helper` to remove its registrations. Use `/scripts reload welcome-helper` after an edit.

Use `/scripts config welcome-helper prefix Welcome` to change the greeting. Reload the script before checking the new reply.

Reload creates new globals and handlers. Saved state restores the count. Removing the source file does not automatically remove the user's saved state or settings.

Your first helper is complete. Add one feature at a time. Use [recipes](../recipes.md) for inventory reports, world searches, dialogs, and optional plugin calls.

Return to the [chapter index](index.md).
