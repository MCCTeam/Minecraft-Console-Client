# Logs and session diagnostics

MCC can keep a text log and a diagnostic bundle. They have different purposes. Text logs help you read events. Diagnostic bundles include structured context and optional raw packet data.

## Configure recording

In `client.toml`:

```toml
[Diagnostics]
Enabled = true
CapturePackets = true
MaxCaptureMegabytes = 20
KeepSessions = 5
```

These are the defaults. Set `CapturePackets=false` to keep diagnostics without raw packet capture. Set `Enabled=false` to disable the bundle.

The packet recorder stops at 20 MiB or 20 minutes, whichever limit it reaches first. A lower configured size stops earlier. `KeepSessions` controls retained completed session archives.

## Find the bundle

MCC writes session data beneath `configurations/logs`, using your selected configuration folder. On a clean shutdown, it compresses the session and prints the archive path.

1. Stop MCC with `/exit`.
2. Read the final diagnostics-path message.
3. Open that folder.
4. Select the archive for the failing session.

If the process crashed before packaging, an unpacked session folder can remain. Keep that folder when reporting the crash.

## Understand the contents

A bundle can include environment details, client configuration summaries, server information, logs, crash details, and packet-capture chunks. Some server information appears only after the client remains connected long enough to receive it.

The recorder can follow reconnects within the client run. It does not promise a full capture after the time or size limit.

## Review before sharing

Diagnostic data can contain account or player names, server addresses, chat, and raw packet payloads. Packet captures are not automatically anonymous.

1. Extract the archive to a private folder.
2. Read the human-readable reports and logs.
3. Remove credentials and private data from the copy you will share.
4. Decide whether raw capture files are appropriate to share.
5. Keep the original privately if maintainers need more evidence later.

Do not publish `accounts.toml`, `.env`, or the authentication cache to explain a connection failure. Redact secrets while preserving relevant setting names and types.

## Enable a text log

```toml
[Logging]
LogToFile = true
LogFile = "console-log.txt"
PrependTimestamp = true
SaveColorCodes = false
```

A relative filename resolves beneath the configuration folder. An absolute filename uses that path directly. File logging is separate from diagnostic recording.

`/debug on` enables runtime debug messages. `/debug off` disables them. `/debug state` prints a state summary without requiring continuous verbose output.

## Report a reproducible problem

Include the exact MCC build, operating system, server version, classic/TUI mode, command or script, expected behavior, and actual behavior. Include the first relevant error, not only the final disconnect.

A small script and a private test-world setup are easier to reproduce than a description of a long automation run.
