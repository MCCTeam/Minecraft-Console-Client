# Your first session

This tutorial uses an interactive terminal. It connects one client, sends one message, and stops cleanly. You do not need to edit TOML first.

## 1. Open MCC

1. Open a terminal in your extracted MCC folder.
2. Start the client with a writable configuration folder.

Linux or macOS:

```bash
./Mcc.Cli --configurations ./configurations
```

Windows PowerShell:

```powershell
.\Mcc.Cli.exe --configurations .\configurations
```

For a source build, use `mcc-run --configurations /absolute/path/to/configurations` after `mcc-build`.

MCC creates missing `client.toml`, `accounts.toml`, `servers.toml`, and `console.toml` files in the selected folder.

## 2. Select an account

When no usable account exists, MCC asks which account type to use.

- Choose Microsoft for a normal Minecraft account.
- Choose offline for a private server that permits offline login.
- Choose Yggdrasil only when your server uses a custom authentication provider.

Selecting Microsoft identifies the authentication method. In classic mode, sign-in starts when you connect. In the TUI, selection can start sign-in immediately. MCC saves the account after successful authentication.

For offline login, enter a player name with at most 16 characters. MCC needs no password. This mode does not authenticate a Microsoft account.

If no server is configured, MCC opens an idle prompt. The client can show help and load plugins in this state.

## 3. Connect

Type this command inside MCC. Replace the sample address with your server address:

```text
/connect play.example.net:25565
```

The default Minecraft Java port is `25565`. You can omit that port when your server uses it.

For Microsoft authentication, complete the displayed sign-in steps:

1. Open the URL that MCC shows.
2. Enter the displayed device code on the Microsoft page.
3. Complete authentication in the browser.
4. Return to MCC.

A cached session can skip this browser step. The TUI can complete it during account selection.

Wait for the connection status. The English log includes `Server was successfully joined` after the client enters the game. A connection error appears in the log instead.

## 4. Send a message

Type a plain line inside MCC:

```text
Hello from MCC!
```

MCC sends it as server chat. Other players can see it if the server permits chat.

```text
/list
/health
/help
```

`/list` shows players. `/health` shows your status. `/help` lists the commands available in this client.

## 5. Distinguish local and server commands

MCC handles recognized commands such as `/help` locally. The server does not receive those commands.

Use two slashes to run a server command that shares an MCC command name:

```text
//help
```

MCC sends `/help` to the server. You can also use `/send /help`. A misspelled command can reach the server. Check the spelling when the server reports an unknown command.

## 6. Save the server

```text
/servers add survival play.example.net:25565
/connect survival
```

The first command writes the named server to `servers.toml`. It also selects that entry. If the name already exists, the command replaces it.

## 7. Stop MCC

```text
/exit
```

The alias `/quit` also stops MCC. Use the slash with the default command prefix. A bare `exit` is ordinary chat in that configuration.

Wait for the process to close. MCC disconnects and disposes scripts and plugins. If diagnostics are enabled, the final log shows the session bundle path.

## Try the terminal UI

```bash
./Mcc.Cli --configurations ./configurations --console.General.ConsoleMode=tui
```

Use the equivalent `.\Mcc.Cli.exe` command in PowerShell. Inside the TUI, run `/mcc-menu` or press `Ctrl+M`.

Continue with [accounts](../client/accounts.md), [configuration](../client/configuration.md), or [Beacon](../beacon/index.md).
