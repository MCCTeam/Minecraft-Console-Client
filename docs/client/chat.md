# Chat and command input

By default, MCC uses `/` as its local command prefix. It handles a recognized MCC command before sending anything to the server.

| Input | Default behavior |
| --- | --- |
| `Hello everyone` | Server chat |
| `/health` | Local MCC command |
| `/unknown-server-command` | Server command when MCC has no such command |
| `//health` | Send `/health` to the server |
| `/send /health` | Send `/health` to the server explicitly |
| `/log checkpoint` | Print locally without sending |

A recognized command with invalid arguments returns a local usage error. An unrecognized name can reach the server. A close spelling match can show a suggestion before forwarding.

## Send chat

```text
Hello from MCC
/send Hello from MCC
```

Both examples send chat. `/send` is useful when another interface needs an explicit command. Sending depends on connection state and the outgoing chat queue.

## Send a private message

Use the private-message command accepted by your server:

```text
//tell Alex Hello
```

The example sends the server's `/tell` command. Some servers use `/msg`, `/w`, or another name. `Chat.PrivateMessageCommand` supplies the name used by integrations that need this setting.

## Use variables

```text
/set home=150 80 380
/move %home%
```

MCC expands client variables in local command text. Variables hold strings and compare names without case sensitivity. Use letters, numbers, and underscores. For example, use `guide_count` instead of `guide.count`.

`/set` changes the runtime store. Add a value to `[Variables]` in `client.toml` when you want it on later starts. These variables are separate from Beacon globals and plugin storage.

## Change the prefix

In `client.toml`:

```toml
[Permissions]
CommandPrefix = "backslash"
```

After applying the configuration, `\help` is a local command and `/help` goes to the server. In backslash mode, `\/help` also explicitly sends `/help`.

`CommandPrefix = "none"` interprets ordinary lines as local commands. Use `send hello` for chat. A leading `/` goes directly to the server in that mode.

The examples in these docs use `slash`. Adjust them when you choose a different prefix.

## Hide chat without disconnecting

```text
/console-chat off
/console-chat on
```

This changes host display for the current run. The client still receives chat, and scripts or plugins can still process it. Change `console.toml [General] DisplayChat` for a persistent preference.

## Understand chat markers

Minecraft versions with signed chat can distinguish verified, unverified, modified, or insecure messages. MCC can display status markers according to `[Chat.Signature]` settings.

A server can suppress a message. MCC reports that a message was withheld without reproducing its content. A gap in the received chat stream is a separate diagnostic. A marker explains available evidence, not a reason to assume the message's author is trustworthy.

## Message rate and length

`Chat.MessageCooldown` defaults to one second. The queue and protocol limits affect send timing. Long-message handling depends on the selected protocol and configured length.

A server can reject chat, filter content, impose cooldowns, or disconnect repeated senders. A successful local enqueue is not proof that another player received the message.
