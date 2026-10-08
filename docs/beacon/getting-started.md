# Run your first Beacon script

Beacon runs automation inside MCC. You can write a script without learning C# or installing a compiler. MCC includes the interpreter.

A script is a UTF-8 text file with the `.bcn` extension. Its first line selects the language version.

## Write a script

1. Create a directory named `mcc-data` outside the MCC application directory.
2. Create `mcc-data/scripts`.
3. Create `mcc-data/scripts/total.bcn`.
4. Copy this complete example into the file.

```beacon
# beacon 1
set prices to [3, 5, 7]
set total to 0
for each price in prices
  set total to total + price
end for
assert(total is 15, "total")
show "Total: {total}"
```

`show` writes local output. It does not send chat. `assert` reports an error if the calculation is incorrect.

## Run without a server

In these guides, `Mcc.Cli` means the executable from the MCC distribution. On Windows PowerShell, use `.\Mcc.Cli.exe` from its directory. On Linux and macOS, use `./Mcc.Cli` from its directory.

1. Open a terminal in the MCC application directory.
2. Replace the example path with the full path to your file.
3. Check the file.

```sh
./Mcc.Cli lint /full/path/mcc-data/scripts/total.bcn
```

4. Run the file.

```sh
./Mcc.Cli run /full/path/mcc-data/scripts/total.bcn
```

The output includes:

```text
Total: 15
```

The report also contains a file status. A successful command returns exit code `0`.

On Windows PowerShell, the equivalent commands are:

```powershell
.\Mcc.Cli.exe lint "C:\MccData\scripts\total.bcn"
.\Mcc.Cli.exe run "C:\MccData\scripts\total.bcn"
```

Use your actual Windows path. Quote a path when it contains spaces. If the installer creates the `mcc` launcher, you can replace the executable with `mcc`.

A framework-dependent build uses `dotnet /full/path/Mcc.Cli.dll` instead of `./Mcc.Cli`. Repository developers can use `mcc-run` after `mcc-build`. These development helpers change the working directory. Supply full paths for scripts.

Offline execution uses an inert host, a virtual clock, and seeded randomness. It proves calculations and syntax. It cannot prove server responses, chat delivery, or game actions.

## Run in MCC

MCC discovers top-level `.bcn` files in the `scripts` directory beside the selected configuration directory:

```text
mcc-data/
├── configurations/
│   ├── client.toml
│   ├── accounts.toml
│   ├── servers.toml
│   └── console.toml
└── scripts/
    └── total.bcn
```

1. Start MCC with your configuration directory.

```sh
./Mcc.Cli --configurations /full/path/mcc-data/configurations
```

2. Type this internal command at the MCC input prompt.

```text
/scripts run total
```

3. Check the output for `Total: 15`.
4. Stop the script.

```text
/scripts stop total
```

These examples use MCC's default internal command prefix, `/`. A slash entered in MCC can select an internal command. A slash sent through Minecraft chat to a server is a different command path.

Discovery does not start a script. `/scripts list` lists running scripts. `/scripts run` loads a file and registers its handlers, timers, commands, and exports.

A game action needs a connected session and the required tracking features. A local calculation can run without connecting.

## Declare capabilities

`# needs:` lists required capabilities. `# wants:` lists optional capabilities. A capability is a named operation, such as `chat.send`.

```beacon
# beacon 1
# needs: chat.send
on chat as e when trim(lower(e.message)) is "!hello"
  whisper e.player "Hello, {e.player}!"
end on
```

The declaration does not connect MCC or grant server permissions. The server can still refuse a requested action.

## Choose a stable script ID

For `scripts/welcome.bcn`, the script ID is `welcome`. MCC uses this ID for state, settings, commands, and exports.

Renaming the file changes its discovered identity. Keep the filename stable when you want to retain saved progress.

Nested `.bcn` files can serve as imported libraries. Keep a library beside its caller, or inside that caller's `lib` directory.

Copy the [example files](../examples/beacon/README.md) when you want a ready-to-edit starting point.

Next: [Chapter 1: Create and run a file](guide/01-first-file.md).
