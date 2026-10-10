# Servers and reconnects

MCC can use a saved server name or a direct `host[:port]` address. The standard Java server port is `25565`.

## Save two servers

Create or edit `servers.toml` in your configuration folder:

```toml
Active = "survival"

[[Server]]
Name = "survival"
Host = "play.example.net"
Port = 25565
Version = "auto"
Kind = "normal"

[[Server]]
Name = "local"
Host = "localhost"
Port = 25566
Version = "auto"
Kind = "normal"
```

The sample public domain is a placeholder. Replace it with your server. `Active` selects the server by its local `Name`.

Inside MCC:

```text
/connect local
/connect survival
```

A direct address also works:

```text
/connect localhost:25566
```

## Add a server from MCC

```text
/servers add local localhost:25566
```

This command writes the entry and selects it. It overwrites an entry with the same name. It does not connect automatically. Run `/connect local` when you are ready.

The TUI's server picker can connect once or save and connect. Closing the picker returns you to the idle client.

## Start without connecting

Put this in `client.toml`:

```toml
[Connection]
AutoConnect = false
```

Or use a temporary startup override:

```bash
./Mcc.Cli --connection.auto-connect=false
```

MCC loads plugins and opens its prompt. `/connect` starts a connection. `/reco` connects to the selected server when one exists.

A missing server address also creates an idle client. An account must still be configured or selected through the interactive first-run prompt.

## Version detection

`Version = "auto"` asks the server which protocol it uses. Keep this default unless detection fails or a proxy reports the wrong version.

To select a version for one run:

```bash
./Mcc.Cli -v 1.21.5
```

To save a version, change the selected server entry's `Version`. Supported protocols come from UMPK's version catalogue. The startup banner reports this build's range. Do not assume a release supports a version only because its name looks newer.

## Automatic reconnect

These are the default reconnect settings in `client.toml`:

```toml
[Connection.Reconnect]
MaxAttempts = 0
DelaySeconds = 5.0
BackoffFactor = 1.0
MaxDelaySeconds = 120.0
RetryOnKick = true
```

`MaxAttempts = 0` disables automatic reconnect. A negative value permits unlimited retries. A positive value limits attempts.

For bounded retries after a network failure:

```toml
[Connection.Reconnect]
MaxAttempts = 5
DelaySeconds = 5.0
BackoffFactor = 2.0
MaxDelaySeconds = 60.0
RetryOnKick = false
```

The delay increases by the backoff factor, up to the maximum. `RetryOnKick = false` prevents an automatic return after a server kick. It still permits retries after other eligible disconnects.

Use `/reco` for an immediate manual reconnect. An interactive client can retain its prompt after a disconnect. A file-input session exits when the remote session ends.

## DNS and proxies

`client.toml [Connection] SrvResolve` controls Minecraft DNS SRV lookup. The default `fast` mode limits the lookup before using the literal endpoint. Use `no` to skip SRV lookup.

Proxy settings belong in `accounts.toml`:

```toml
[Proxy]
EnabledLogin = false
EnabledIngame = true
Host = "127.0.0.1"
Port = 1080
Kind = "socks5"
Username = ""
Password = ""
```

Supported proxy kinds are `http`, `socks4`, `socks4a`, and `socks5`. Enable only the traffic you need. A proxy that permits web authentication may not permit Minecraft game traffic.

## Realms

Add a Realm entry to `servers.toml`:

```toml
[[Server]]
Name = "my-realm"
Kind = "realm"
RealmWorld = "world-name-or-numeric-id"
```

Replace the selector with the Realm's name or numeric ID. Use `Kind = "realm"`, singular. Set `Active = "my-realm"` at the file root to select it at startup.

An eligible Microsoft account must have access to the world. Host and port are omitted because the Realms service resolves the endpoint.

Use the connection error and the selected entry to check Realms access. Do not change a normal public server to `realm` merely because it uses Microsoft login.
