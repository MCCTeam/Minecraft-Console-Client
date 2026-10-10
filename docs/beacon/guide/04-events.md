# Chapter 4: Answer an event

An event is a notification from MCC. A chat event contains a player name and a message. An event handler runs for a matching notification.

You will answer the exact message `!hello`. This chapter needs a connected MCC client and another player on your test server.

## Create a handler

1. Create `scripts/greeter.bcn` beside your configuration directory.
2. Copy this complete script into the file.

```beacon
# beacon 1
# needs: chat.send
on chat as e when trim(lower(e.message)) is "!hello"
  whisper e.player "Hello, {e.player}!"
end on
```

`# needs: chat.send` declares the capability used by `whisper`. It does not establish a connection.

`as e` names the event record. `e.message` reads the message. `e.player` reads the sender. `when` filters the record.

`lower` makes the comparison case-insensitive. `trim` removes surrounding whitespace. Exact equality excludes `!hello someone`.

## Check loading first

1. Check the file outside MCC.

```sh
./Mcc.Cli lint /full/path/mcc-data/scripts/greeter.bcn
./Mcc.Cli run /full/path/mcc-data/scripts/greeter.bcn
```

A successful offline run produces no greeting. Loading registers the handler. The greeting needs a chat event.

## Check the event on your server

1. Connect MCC to a server that you control.
2. Type this command in MCC.

```text
/scripts run greeter
```

3. Send ` !HELLO ` from another Minecraft player.
4. Check that the player receives a private greeting.
5. Send `!hello someone` from that player.
6. Check that the helper does not answer the second message.
7. Stop the script.

```text
/scripts stop greeter
```

The expected reply to a player named Alice is `Hello, Alice!`. Server-specific private-message support can affect delivery.

If the reply is absent, inspect MCC's diagnostics. Check that the script is running and the incoming message produces parsed chat.

An offline host cannot create this live chat event. Advanced authors can simulate events with DMCBK's test host. See [testing boundaries](../hosting-and-testing.md).

Next: [Chapter 5: Add scheduled work](05-time.md).
